import type { ComposerCommand } from "../../features/chat/composerCommands";

export function ComposerCommandMenu({ commands, activeIndex, onActiveIndexChange, onSelect }: { commands: ComposerCommand[]; activeIndex: number; onActiveIndexChange: (index: number) => void; onSelect: (command: ComposerCommand) => void }) {
  if (commands.length === 0) return <div className="composer-command-menu composer-command-menu-empty" id="composer-command-menu" role="status">No local commands match.</div>;
  return <div className="composer-command-menu" id="composer-command-menu" role="listbox" aria-label="Composer commands">{commands.map((command, index) => <button className={index === activeIndex ? "is-active" : ""} id={`composer-command-${command.id}`} key={command.id} type="button" role="option" aria-selected={index === activeIndex} onMouseEnter={() => onActiveIndexChange(index)} onClick={() => onSelect(command)}><span><strong>{command.label}</strong><small>{command.detail}</small></span><kbd>{command.shortcut}</kbd></button>)}</div>;
}
