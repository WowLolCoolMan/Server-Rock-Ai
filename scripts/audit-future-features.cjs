const fs = require("node:fs");
const path = require("node:path");

const root = path.resolve(__dirname, "..");
const file = path.join(root, "docs", "FUTURE_FEATURES.md");
const text = fs.readFileSync(file, "utf8").replace(/\r\n/g, "\n");
const sectionPattern = /^## (.+)$/gm;
const itemPattern = /^- \[([ xX])\] (.+)$/gm;
const sections = [...text.matchAll(sectionPattern)].map((match) => match[1].trim());
const items = [...text.matchAll(itemPattern)].map((match) => ({
  checked: match[1].toLowerCase() === "x",
  label: match[2].trim(),
  line: text.slice(0, match.index).split("\n").length,
}));

const errors = [];
if (!fs.existsSync(file)) errors.push("docs/FUTURE_FEATURES.md is missing");
if (items.length === 0) errors.push("the future-features document has no checkbox items");
const seen = new Map();
for (const item of items) {
  if (!item.label) errors.push(`line ${item.line}: feature label is empty`);
  const key = item.label.toLocaleLowerCase("en-US");
  if (seen.has(key)) errors.push(`duplicate feature label at lines ${seen.get(key)} and ${item.line}: ${item.label}`);
  else seen.set(key, item.line);
}
if (!sections.includes("Core chat")) errors.push("Core chat section is missing");
if (!sections.includes("Agents and tools")) errors.push("Agents and tools section is missing");
if (!sections.includes("Workspace and desktop experience")) errors.push("Workspace section is missing");

if (errors.length > 0) {
  console.error("Future-features audit failed:");
  for (const error of errors) console.error(`- ${error}`);
  process.exit(1);
}

console.log(`Future-features audit passed (${items.length} items, ${sections.length} sections, ${items.filter((item) => item.checked).length} checked).`);
