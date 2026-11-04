const { execFileSync } = require("node:child_process");

const tracked = execFileSync("git", ["ls-files", "-z"], { encoding: "utf8" })
  .split("\0")
  .filter(Boolean);
const forbidden = tracked.filter((file) => {
  const normalized = file.replaceAll("\\", "/");
  if (/(^|\/)\.env(?:\..*)?$/.test(normalized) && !normalized.endsWith("/.env.example") && normalized !== ".env.example") return true;
  if (/(^|\/)(?:credentials\.json|.*\.(?:pem|key|pfx|p12))$/i.test(normalized)) return true;
  if (/(^|\/)(?:bin|obj|artifacts|dist|playwright-report|test-results)(?:\/|$)/i.test(normalized)) return true;
  return false;
});

if (forbidden.length) {
  console.error("Repository hygiene check failed. Remove generated or secret-shaped tracked files:");
  for (const file of forbidden) console.error(`- ${file}`);
  process.exitCode = 1;
} else {
  console.log(`Repository hygiene check passed (${tracked.length} tracked files).`);
}
