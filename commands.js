const COMMANDS = [
  "fast",
  "plan",
  "goal",
  "status",
  "imagegen",
  "imagine",
  "review",
  "summarize",
  "explain",
  "translate",
  "help",
  "models",
  "push",
  "clear",
];
function parseSlashCommands(raw) {
  let text = raw.trim(),
    fast = false,
    image = false,
    goal = false,
    plan = false,
    status = false,
    review = false,
    summarize = false,
    explain = false,
    translate = false,
    help = false,
    models = false,
    push = false,
    clear = false;
  while (text.startsWith("/")) {
    const m = text.match(
      /^\/(fast|plan|imagegen|imagine|goal|status|review|summarize|explain|translate|help|models|push|clear)\s*/i,
    );
    if (!m) break;
    const cmd = m[1].toLowerCase();
    fast ||= cmd === "fast";
    image ||= cmd === "imagegen" || cmd === "imagine";
    goal ||= cmd === "goal";
    plan ||= cmd === "plan";
    status ||= cmd === "status";
    review ||= cmd === "review";
    summarize ||= cmd === "summarize";
    explain ||= cmd === "explain";
    translate ||= cmd === "translate";
    help ||= cmd === "help";
    models ||= cmd === "models";
    push ||= cmd === "push";
    clear ||= cmd === "clear";
    text = text.slice(m[0].length);
  }
  return {
    text,
    fast,
    image,
    goal,
    plan,
    status,
    review,
    summarize,
    explain,
    translate,
    help,
    models,
    push,
    clear,
  };
}
if (typeof module !== "undefined")
  module.exports = { parseSlashCommands, COMMANDS };
