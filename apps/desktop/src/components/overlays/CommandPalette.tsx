import { useEffect, useMemo, useState } from "react";
import type { Route } from "../../app/types";

type Command = { id: string; label: string; detail: string; route: Route };
const commands: Command[] = [
  { id: "chat", label: "Open Chat", detail: "Start or continue a conversation", route: "chat" },
  { id: "coding", label: "Open Coding Workspace", detail: "Projects, files, terminal, and edits", route: "coding" },
  { id: "agents", label: "Open Agent Workbench", detail: "Review task progress and recovery", route: "workbench" },
  { id: "models", label: "Open Models", detail: "Discover and select a provider model", route: "models" },
  { id: "images", label: "Open Image Studio", detail: "Create with a local workflow", route: "images" },
  { id: "marketplace", label: "Open Marketplace", detail: "Browse models, skills, and integrations", route: "marketplace" },
  { id: "memory", label: "Open Memory", detail: "Review approved local memory", route: "memory" },
  { id: "settings", label: "Open Settings", detail: "Appearance and workspace preferences", route: "settings" },
];

export function CommandPalette({ open, onClose, onNavigate }: { open: boolean; onClose: () => void; onNavigate: (route: Route) => void }) {
  const [query, setQuery] = useState("");
  const matches = useMemo(() => commands.filter((command) => `${command.label} ${command.detail}`.toLowerCase().includes(query.trim().toLowerCase())), [query]);
  useEffect(() => { if (open) setQuery(""); }, [open]);
  if (!open) return null;
  return <dialog className="command-dialog" open aria-modal="true" aria-labelledby="command-palette-title"><div className="command-palette"><div className="command-heading"><div><p className="section-kicker">QUICK ACTIONS</p><h2 id="command-palette-title">Command palette</h2></div><button type="button" onClick={onClose} aria-label="Close command palette">Esc</button></div><input autoFocus value={query} onChange={(event) => setQuery(event.target.value)} onKeyDown={(event) => { if (event.key === "Escape") onClose(); if (event.key === "Enter" && matches[0]) { onNavigate(matches[0].route); onClose(); } }} placeholder="Search workspaces and actions…" aria-label="Search commands" /><div className="command-list">{matches.length === 0 ? <p className="command-empty">No matching actions.</p> : matches.map((command) => <button type="button" key={command.id} onClick={() => { onNavigate(command.route); onClose(); }}><span><strong>{command.label}</strong><small>{command.detail}</small></span><kbd>↵</kbd></button>)}</div><p className="command-hint">Use ↑ ↓ to browse · Enter to open · Esc to close</p></div></dialog>;
}
