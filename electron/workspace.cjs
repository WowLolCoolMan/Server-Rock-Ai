const path = require('node:path');
const fs = require('node:fs/promises');
const crypto = require('node:crypto');

const ignored = new Set(['.git', 'node_modules', '.venv', 'venv', '__pycache__', '.next', 'dist', 'build', '.cache']);
const textExtensions = new Set(['.txt','.md','.py','.js','.cjs','.mjs','.ts','.tsx','.jsx','.json','.html','.css','.scss','.yaml','.yml','.toml','.sh','.ps1','.sql','.rs','.go','.java','.cs','.cpp','.h','.hpp','.c','.xml','.svg','.ini','.env.example']);
const protectedDirectories = new Set(['.git', '.svn', '.hg', 'node_modules', 'vendor', 'bin', 'obj', 'dist', 'build', '.venv', 'venv', 'target', '.ssh', '.aws', '.azure', '.gnupg']);
const protectedNames = new Set(['.env', '.env.local', '.env.production', 'secrets.json', 'credentials.json', 'id_rsa', 'id_ed25519']);
const MAX_APPROVED_FOLDERS = 20;
const MAX_APPROVALS_BYTES = 64 * 1024;

function isWithin(root, candidate) {
  const relative = path.relative(root, candidate);
  return relative !== '' && relative !== '..' && !relative.startsWith(`..${path.sep}`) && !path.isAbsolute(relative);
}

function isProtectedPath(relativePath) {
  const parts = relativePath.split('/');
  const name = parts.at(-1) || '';
  return (name.toLowerCase().startsWith('.env') && name.toLowerCase() !== '.env.example') ||
    protectedNames.has(name.toLowerCase()) ||
    parts.slice(0, -1).some((part) => protectedDirectories.has(part.toLowerCase()));
}

async function quarantineUnreadableFile(filePath) {
  try {
    await fs.access(filePath);
    await fs.rename(filePath, `${filePath}.corrupt-${crypto.randomUUID()}`);
  } catch {
    // Missing or inaccessible files cannot be preserved here.
  }
}

async function writeAtomic(filePath, content) {
  const temporaryPath = `${filePath}.${crypto.randomUUID()}.tmp`;
  try {
    await fs.writeFile(temporaryPath, content, { encoding: 'utf8', flag: 'wx' });
    await fs.rename(temporaryPath, filePath);
  } finally {
    await fs.rm(temporaryPath, { force: true }).catch(() => {});
  }
}

async function readUtf8Limited(filePath, maxBytes) {
  if (!Number.isInteger(maxBytes) || maxBytes < 1) throw new TypeError('A positive byte limit is required.');
  const handle = await fs.open(filePath, 'r');
  try {
    const initial = await handle.stat();
    if (!initial.isFile() || initial.size > maxBytes) throw new Error('The file exceeds its byte limit.');
    const chunks = [];
    const buffer = Buffer.alloc(Math.min(64 * 1024, maxBytes + 1));
    let total = 0;
    while (true) {
      const result = await handle.read(buffer, 0, buffer.length, null);
      if (!result.bytesRead) break;
      total += result.bytesRead;
      if (total > maxBytes) throw new Error('The file exceeds its byte limit.');
      chunks.push(Buffer.from(buffer.subarray(0, result.bytesRead)));
    }
    const final = await handle.stat();
    if (final.size > maxBytes) throw new Error('The file exceeds its byte limit.');
    return Buffer.concat(chunks, total).toString('utf8');
  } finally {
    await handle.close();
  }
}

function createWorkspace(profilePath) {
  const approvalsPath = path.join(profilePath, 'authorized-project-folders.json');
  let approved = null;
  async function loadApproved() {
    if (approved) return approved;
    try {
      const stat = await fs.stat(approvalsPath);
      if (!stat.isFile() || stat.size > MAX_APPROVALS_BYTES) throw new Error('Authorized folder data is too large.');
      const raw = await readUtf8Limited(approvalsPath, MAX_APPROVALS_BYTES);
      const parsed = JSON.parse(raw);
      if (!Array.isArray(parsed) || parsed.length > MAX_APPROVED_FOLDERS || parsed.some((folder) => typeof folder !== 'string' || folder.length > 4096))
        throw new Error('Authorized folder data is invalid.');
      approved = new Set(parsed);
    }
    catch (error) {
      if (error?.code !== 'ENOENT') await quarantineUnreadableFile(approvalsPath);
      approved = new Set();
    }
    return approved;
  }
  async function approveFolder(folder) {
    const canonical = await fs.realpath(folder);
    const stat = await fs.stat(canonical);
    if (!stat.isDirectory()) throw new Error('Choose a folder for this project.');
    const folders = await loadApproved();
    folders.add(canonical);
    if (folders.size > MAX_APPROVED_FOLDERS) throw new Error(`You can authorize at most ${MAX_APPROVED_FOLDERS} project folders.`);
    await fs.mkdir(profilePath, { recursive: true });
    await writeAtomic(approvalsPath, JSON.stringify([...folders], null, 2));
    return canonical;
  }
  async function getRoots(folders) {
    if (!Array.isArray(folders) || !folders.length || folders.length > 20) throw new Error('Select a project source folder first.');
    const allow = await loadApproved(), roots = [];
    for (const folder of folders) {
      if (typeof folder !== 'string' || folder.length > 4096) continue;
      let real;
      try { real = await fs.realpath(folder); } catch { continue; }
      try {
        if (allow.has(real) && (await fs.stat(real)).isDirectory()) roots.push(real);
      } catch {
        // A folder can disappear or become inaccessible after authorization.
      }
    }
    if (!roots.length) throw new Error('Use Add a folder on this computer to authorize a project folder.');
    return roots;
  }
  async function readFiles(folders) {
    const roots = await getRoots(folders), files = [];
    let total = 0;
    async function walk(root, dir) {
      if (files.length >= 100 || total >= 350000) return;
      let entries;
      try { entries = await fs.readdir(dir, { withFileTypes: true }); }
      catch { return; }
      for (const entry of entries) {
        if (files.length >= 100 || total >= 350000) break;
        if (entry.isSymbolicLink() || ignored.has(entry.name) || entry.name.startsWith('.')) continue;
        const full = path.join(dir, entry.name);
        if (entry.isDirectory()) {
          await walk(root, full);
          continue;
        }
        if (!entry.isFile() || !textExtensions.has(path.extname(entry.name).toLowerCase())) continue;
        let stat;
        try { stat = await fs.stat(full); } catch { continue; }
        if (!stat.size || stat.size > 45000 || total + stat.size > 350000) continue;
        let content;
        try { content = await readUtf8Limited(full, 45000); } catch { continue; }
        if (content.includes('\u0000')) continue;
        const relative = path.relative(root, full).split(path.sep).join('/');
        files.push({ path: relative, content }); total += Buffer.byteLength(content, 'utf8');
      }
    }
    for (const root of roots) await walk(root, root);
    return { roots, files };
  }
  async function writeFile(folders, relativePath, content) {
    const [root] = await getRoots(folders);
    if (typeof relativePath !== 'string' || relativePath.length > 512 || typeof content !== 'string' || Buffer.byteLength(content, 'utf8') > 1024 * 1024) throw new Error('File path or content is invalid or too large.');
    const normalized = relativePath.replaceAll('\\', '/').replace(/^file:/i, '').trim();
    if (!normalized || normalized.startsWith('/') || /^[a-z]:/i.test(normalized) || normalized.split('/').some(part => !part || part === '.' || part === '..')) throw new Error('File path must stay inside the selected project folder.');
    if (isProtectedPath(normalized)) throw new Error('That path is protected from agent edits.');
    const destination = path.resolve(root, ...normalized.split('/'));
    if (!isWithin(root, destination)) throw new Error('File path must stay inside the selected project folder.');
    await fs.mkdir(path.dirname(destination), { recursive: true });
    const parent = await fs.realpath(path.dirname(destination));
    if (parent !== root && !isWithin(root, parent)) throw new Error('File path resolves outside the selected project folder.');
    try {
      const existing = await fs.lstat(destination);
      if (!existing.isFile() || existing.isSymbolicLink()) throw new Error('The destination must be a regular file, not a link or special file.');
    } catch (error) {
      if (error.code !== 'ENOENT') throw error;
    }
    const temp = path.join(parent, `.server-rock-${crypto.randomUUID()}.tmp`);
    try {
      await fs.writeFile(temp, content, { encoding: 'utf8', flag: 'wx' });
      await fs.rename(temp, destination);
    } catch (error) {
      await fs.rm(temp, { force: true }).catch(() => {});
      throw error;
    }
    return { path: destination, relativePath: normalized };
  }
  return { approveFolder, readFiles, writeFile };
}

module.exports = { createWorkspace, readUtf8Limited };
