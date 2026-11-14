const fs = require("node:fs");
const path = require("node:path");

const root = path.resolve(__dirname, "..");
const runtimeRoots = [
  path.join(root, "apps", "desktop", "src"),
  path.join(root, "apps", "desktop", "electron"),
  path.join(root, "apps", "desktop", "shared"),
  path.join(root, "services", "agent-host", "Protocol"),
];
const extensions = new Set([".cjs", ".cs", ".js", ".json", ".ts", ".tsx"]);
// User-facing copy may explain future cloud routes. The shipped runtime must
// still reject remote transport and must not hard-code hosted provider URLs.
const forbidden = /api\.openai\.com|api\.anthropic\.com|generativelanguage\.googleapis\.com/i;
const hits = [];

function visit(directory) {
  if (!fs.existsSync(directory)) return;
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const file = path.join(directory, entry.name);
    if (entry.isDirectory()) visit(file);
    else if (extensions.has(path.extname(entry.name).toLowerCase())) {
      const text = fs.readFileSync(file, "utf8");
      if (forbidden.test(text)) hits.push(path.relative(root, file).replaceAll("\\", "/"));
    }
  }
}

runtimeRoots.forEach(visit);

if (hits.length) {
  console.error("Local transport audit failed. Remove hard-coded hosted provider URLs from shipped source:");
  for (const file of hits) console.error(`- ${file}`);
  process.exitCode = 1;
} else {
  console.log("Local transport audit passed: shipped runtime contains no hard-coded hosted provider URLs; cloud labels remain explicit and non-routable.");
}
