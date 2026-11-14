import { useEffect, useMemo, useRef, useState, type KeyboardEvent } from "react";
import type { Route } from "../../app/types";
import { ModalDialog } from "../ui/ModalDialog";

type Command = { id: string; label: string; detail: string; route: Route };
const commands: Command[] = [
  { id: "chat", label: "Open Chat", detail: "Start or continue a conversation", route: "chat" },
  { id: "coding", label: "Open Coding Workspace", detail: "Projects, files, terminal, and edits", route: "coding" },
  { id: "terminal", label: "Open Terminal", detail: "Run a bounded command in a selected project", route: "terminal" },
  { id: "agents", label: "Open Agent Workbench", detail: "Review task progress and recovery", route: "workbench" },
  { id: "models", label: "Open Models", detail: "Discover and select a provider model", route: "models" },
  { id: "images", label: "Open Image Studio", detail: "Create with a local workflow", route: "images" },
  { id: "marketplace", label: "Open Marketplace", detail: "Find models, voices, tools, and skills", route: "marketplace" },
  { id: "usage", label: "Open Usage", detail: "Review measured activity and unavailable telemetry", route: "usage" },
  { id: "memory", label: "Open Memory", detail: "Review approved local memory", route: "memory" },
  { id: "settings", label: "Open Settings", detail: "Appearance and workspace preferences", route: "settings" },
];

export function CommandPalette({ open, onClose, onNavigate }: { open: boolean; onClose: () => void; onNavigate: (route: Route) => void }) {
  const [query, setQuery] = useState("");
  const [activeIndex, setActiveIndex] = useState(0);
  const inputRef = useRef<HTMLInputElement>(null);
  const matches = useMemo(() => commands.filter((command) => `${command.label} ${command.detail}`.toLowerCase().includes(query.trim().toLowerCase())), [query]);
  useEffect(() => {
    if (!open) {
      return;
    }
    setQuery("");
    setActiveIndex(0);
  }, [open]);
  useEffect(() => { setActiveIndex((current) => Math.min(current, Math.max(0, matches.length - 1))); }, [matches.length]);
  const openCommand = (command: Command) => { onNavigate(command.route); onClose(); };
  const onInputKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === "Escape") { event.preventDefault(); onClose(); return; }
    if (event.key === "ArrowDown" && matches.length > 0) { event.preventDefault(); setActiveIndex((current) => (current + 1) % matches.length); return; }
    if (event.key === "ArrowUp" && matches.length > 0) { event.preventDefault(); setActiveIndex((current) => (current - 1 + matches.length) % matches.length); return; }
    if (event.key === "Home" && matches.length > 0) { event.preventDefault(); setActiveIndex(0); return; }
    if (event.key === "End" && matches.length > 0) { event.preventDefault(); setActiveIndex(matches.length - 1); return; }
    if (event.key === "Enter" && matches[activeIndex]) { event.preventDefault(); openCommand(matches[activeIndex]); }
  };
  const activeCommand = matches[activeIndex];
  return <ModalDialog open={open} onClose={onClose} titleId="command-palette-title" initialFocusRef={inputRef} className="command-dialog"><div className="command-palette"><div className="command-heading"><div><p className="section-kicker">QUICK ACTIONS</p><h2 id="command-palette-title">Command palette</h2></div><button type="button" onClick={onClose} aria-label="Close command palette">Esc</button></div><input ref={inputRef} value={query} onChange={(event) => { setQuery(event.target.value); setActiveIndex(0); }} onKeyDown={onInputKeyDown} placeholder="Search workspaces and actions…" aria-label="Search commands" aria-controls="command-results" aria-activedescendant={activeCommand ? `command-${activeCommand.id}` : undefined} role="combobox" aria-expanded="true" aria-autocomplete="list" /><div className="command-list" id="command-results" role="listbox" aria-label="Available commands">{matches.length === 0 ? <p className="command-empty">No matching actions.</p> : matches.map((command, index) => <button className={index === activeIndex ? "is-active" : ""} id={`command-${command.id}`} role="option" aria-selected={index === activeIndex} type="button" key={command.id} onMouseEnter={() => setActiveIndex(index)} onClick={() => openCommand(command)}><span><strong>{command.label}</strong><small>{command.detail}</small></span><kbd>↵</kbd></button>)}</div><p className="command-hint">Use ↑ ↓ to browse · Enter to open · Esc to close</p></div></ModalDialog>;
}
