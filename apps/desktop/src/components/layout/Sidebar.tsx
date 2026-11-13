import type { Route } from "../../app/types";
import { PRODUCT_DISPLAY_NAME } from "../../../shared/productIdentity";
import { Button } from "../ui/Primitives";

const primary: Array<{ route: Route; icon: string; label: string }> = [
  { route: "chat", icon: "⌁", label: "Chat" },
  { route: "coding", icon: "⌘", label: "Coding workspace" },
  { route: "workbench", icon: "◇", label: "Agent workbench" },
  { route: "models", icon: "◌", label: "Models" },
  { route: "images", icon: "▧", label: "Image Studio" },
  { route: "marketplace", icon: "✦", label: "Marketplace" },
];

export function Sidebar({ route, onNavigate, conversations, query, collapsed, onQueryChange, onToggleCollapsed, onNewChat }: { route: Route; onNavigate: (route: Route) => void; conversations: string[]; query: string; collapsed: boolean; onQueryChange: (query: string) => void; onToggleCollapsed: () => void; onNewChat: () => void }) {
  return <aside className={`app-sidebar ${collapsed ? "is-collapsed" : ""}`} aria-label="Primary navigation">
    <div className="sidebar-brand"><div className="brand-mark" aria-hidden="true">SK</div><div><p className="eyebrow">WORKSPACE</p><strong>{PRODUCT_DISPLAY_NAME}</strong></div></div>
    <div className="sidebar-top-actions"><Button variant="primary" className="new-chat-button" onClick={onNewChat}>＋ <span>New chat</span></Button><button className="sidebar-collapse" type="button" onClick={onToggleCollapsed} aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"}>{collapsed ? "›" : "‹"}</button></div>
    <nav className="sidebar-nav" aria-label="Workspace"><p className="sidebar-label">WORKSPACE</p>{primary.map((item) => <button key={item.route} className={`sidebar-nav-item ${route === item.route ? "is-active" : ""}`} onClick={() => onNavigate(item.route)} type="button"><span aria-hidden="true">{item.icon}</span>{item.label}</button>)}</nav>
    <nav className="sidebar-nav sidebar-nav-secondary" aria-label="Utilities"><p className="sidebar-label">UTILITIES</p><button className={`sidebar-nav-item ${route === "terminal" ? "is-active" : ""}`} onClick={() => onNavigate("terminal")} type="button"><span>›_</span>Terminal</button><button className={`sidebar-nav-item ${route === "memory" ? "is-active" : ""}`} onClick={() => onNavigate("memory")} type="button"><span>◎</span>Memory</button></nav>
    <div className="sidebar-history"><div className="sidebar-label-row"><p className="sidebar-label">RECENT CHATS</p><button type="button" aria-label="Start a new chat" onClick={onNewChat}>＋</button></div><input className="sidebar-search" value={query} onChange={(event) => onQueryChange(event.target.value)} placeholder="Search chats…" aria-label="Search chats" />{conversations.length === 0 ? <p className="sidebar-muted">No saved chats yet.</p> : conversations.filter((conversation) => conversation.toLowerCase().includes(query.toLowerCase())).slice(0, 6).map((conversation) => <button className="sidebar-history-item" key={conversation} onClick={() => onNavigate("chat")} type="button">{conversation}</button>)}</div>
    <button className={`sidebar-settings ${route === "settings" ? "is-active" : ""}`} onClick={() => onNavigate("settings")} type="button"><span>⚙</span>Settings</button>
  </aside>;
}
