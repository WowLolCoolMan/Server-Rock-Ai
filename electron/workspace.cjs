const path = require('node:path');
const fs = require('node:fs/promises');

const ignored = new Set(['.git', 'node_modules', '.venv', 'venv', '__pycache__', '.next', 'dist', 'build', '.cache']);
const textExtensions = new Set(['.txt','.md','.py','.js','.cjs','.mjs','.ts','.tsx','.jsx','.json','.html','.css','.scss','.yaml','.yml','.toml','.sh','.ps1','.sql','.rs','.go','.java','.cs','.cpp','.h','.hpp','.c','.xml','.svg','.ini','.env.example']);

function createWorkspace(profilePath) {
  const approvalsPath = path.join(profilePath, 'authorized-project-folders.json');
  let approved = null;
  async function loadApproved() {
    if (approved) return approved;
    try { approved = new Set(JSON.parse(await fs.readFile(approvalsPath, 'utf8'))); }
    catch { approved = new Set(); }
    return approved;
  }
  async function approveFolder(folder) {
    const canonical = await fs.realpath(folder);
    const stat = await fs.stat(canonical);
    if (!stat.isDirectory()) throw new Error('Choose a folder for this project.');
    const folders = await loadApproved();
    folders.add(canonical);
    await fs.mkdir(profilePath, { recursive: true });
    await fs.writeFile(approvalsPath, JSON.stringify([...folders], null, 2), 'utf8');
    return canonical;
  }
  async function getRoots(folders) {
    if (!Array.isArray(folders) || !folders.length || folders.length > 20) throw new Error('Select a project source folder first.');
    const allow = await loadApproved(), roots = [];
    for (const folder of folders) {
      if (typeof folder !== 'string' || folder.length > 4096) continue;
      let real;
      try { real = await fs.realpath(folder); } catch { continue; }
      if (allow.has(real) && (await fs.stat(real)).isDirectory()) roots.push(real);
    }
    if (!roots.length) throw new Error('Use Add a folder on this computer to authorize a project folder.');
    return roots;
  }
  async function readFiles(folders) {
    const roots = await getRoots(folders), files = [];
    let total = 0;
    async function walk(root, dir) {
      if (files.length >= 100 || total >= 350000) return;
      const entries = await fs.readdir(dir, { withFileTypes: true });
      for (const entry of entries) {
        if (files.length >= 100 || total >= 350000) break;
        if (entry.isSymbolicLink() || ignored.has(entry.name) || entry.name.startsWith('.')) continue;
        const full = path.join(dir, entry.name);
        if (entry.isDirectory()) { await walk(root, full); continue; }
        if (!entry.isFile() || !textExtensions.has(path.extname(entry.name).toLowerCase())) continue;
        const stat = await fs.stat(full);
        if (!stat.size || stat.size > 45000 || total + stat.size > 350000) continue;
        const content = await fs.readFile(full, 'utf8');
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
    const destination = path.resolve(root, ...normalized.split('/'));
    if (!destination.startsWith(root + path.sep)) throw new Error('File path must stay inside the selected project folder.');
    await fs.mkdir(path.dirname(destination), { recursive: true });
    const parent = await fs.realpath(path.dirname(destination));
    if (parent !== root && !parent.startsWith(root + path.sep)) throw new Error('File path resolves outside the selected project folder.');
    const temp = destination + '.server-rock-tmp';
    await fs.writeFile(temp, content, 'utf8');
    await fs.rename(temp, destination);
    return { path: destination, relativePath: normalized };
  }
  return { approveFolder, readFiles, writeFile };
}

module.exports = { createWorkspace };
