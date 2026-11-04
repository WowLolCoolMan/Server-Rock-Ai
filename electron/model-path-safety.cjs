const path = require("node:path");

function resolveModelFilePath(root, repositoryPath) {
  if (typeof repositoryPath !== "string" || repositoryPath.length > 1024 || repositoryPath.includes("\0") || repositoryPath.includes("\\"))
    throw Error("Invalid file path in model repository.");
  const parts = repositoryPath.split("/");
  if (!parts.length || parts.some((part) => !part || part === "." || part === ".."))
    throw Error("Invalid file path in model repository.");
  const base = path.resolve(root);
  const destination = path.resolve(base, ...parts);
  const relative = path.relative(base, destination);
  if (!relative || relative === ".." || relative.startsWith(`..${path.sep}`) || path.isAbsolute(relative))
    throw Error("Invalid file path in model repository.");
  return destination;
}

async function ensureNoSymlinkComponents(root, target, fs) {
  const base = path.resolve(root);
  const destination = path.resolve(target);
  const relative = path.relative(base, destination);
  if (!relative || relative === ".." || relative.startsWith(`..${path.sep}`) || path.isAbsolute(relative))
    throw Error("Model path leaves the managed model directory.");
  try {
    if ((await fs.lstat(base)).isSymbolicLink())
      throw Error("Model directory contains a symbolic link.");
  } catch (error) {
    if (error.code !== "ENOENT") throw error;
  }
  let current = base;
  for (const part of relative.split(path.sep)) {
    current = path.join(current, part);
    try {
      if ((await fs.lstat(current)).isSymbolicLink())
        throw Error("Model path contains a symbolic link.");
    } catch (error) {
      if (error.code === "ENOENT") break;
      throw error;
    }
  }
}

module.exports = { resolveModelFilePath, ensureNoSymlinkComponents };
