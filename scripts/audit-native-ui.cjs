const fs = require("node:fs");
const path = require("node:path");

const root = path.resolve(__dirname, "..");
const xamlPath = path.join(root, "native", "ServerRock.Native", "MainPage.xaml");
const codePath = path.join(root, "native", "ServerRock.Native", "MainPage.xaml.cs");
const xaml = fs.readFileSync(xamlPath, "utf8");
const code = fs.readFileSync(codePath, "utf8");

const requiredXaml = [
  ['Connection setup action', 'AutomationProperties.Name="Connect a model"'],
  ['Navigation menu', 'AutomationProperties.Name="Open navigation menu"'],
  ['Message composer', 'AutomationProperties.Name="Message composer"'],
  ['Composer responsive hook', 'SizeChanged="ComposerToolbarSizeChanged"'],
  ['Coding workspace responsive hook', 'SizeChanged="CodingWorkspaceToolbarSizeChanged"'],
];
const requiredCode = [
  ['Model source label', 'AutomationProperties.SetName(source, "Model source")'],
  ['Model search label', 'AutomationProperties.SetName(query, "Search models")'],
  ['Model results label', 'AutomationProperties.SetName(list, "Model search results")'],
  ['Project name label', 'AutomationProperties.SetName(name, "Project name")'],
  ['Project folder action label', 'AutomationProperties.SetName(addFolder, "Add local project folder")'],
  ['Live model status', 'AutomationProperties.SetLiveSetting(status, AutomationLiveSetting.Polite)'],
  ['Folder toggle focus tracking', '_chatFolderToggleButtons[folder] = open;'],
  ['Folder toggle focus restoration', 'FocusChatFolderToggleOrFallback(folder)'],
  ['Archived toggle focus restoration', 'DispatcherQueue.TryEnqueue(FocusArchivedChatsToggleOrFallback);'],
];

const missing = [
  ...requiredXaml.filter(([, text]) => !xaml.includes(text)),
  ...requiredCode.filter(([, text]) => !code.includes(text)),
];

if (missing.length) {
  console.error("Native UI audit failed:");
  for (const [label, text] of missing) console.error(`- ${label}: missing ${text}`);
  process.exit(1);
}

console.log(`Native UI audit passed (${requiredXaml.length + requiredCode.length} contracts).`);
