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
// Cloud model routes are now explicit features: the renderer may show the
// canonical OpenAI route, while custom routes remain user-entered and the
// host enforces HTTPS, consent, and credential storage. Keep unrelated hosted
// integrations out of shipped source until their contracts are implemented.
const forbidden = /api\.anthropic\.com|generativelanguage\.googleapis\.com/i;
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
  console.log("Transport audit passed: cloud model routing is explicit and limited to the implemented HTTPS provider boundary.");
}
