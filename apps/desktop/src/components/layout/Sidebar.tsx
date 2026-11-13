import { useEffect, useState, type FormEvent } from "react";
import type { Route } from "../../app/types";
import type { DesktopConversation } from "../../../shared/contracts";
import { PRODUCT_DISPLAY_NAME } from "../../../shared/productIdentity";
import { NavIcon, type NavIconName } from "../ui/NavIcon";
import { Button } from "../ui/Primitives";

const SKAI_WOLF_ICON = new URL("../../../../../assets/branding/skaiwolficon.png", import.meta.url).href;

const primary: Array<{ route: Route; icon: NavIconName; label: string }> = [
  { route: "chat", icon: "chat", label: "Chat" },
  { route: "coding", icon: "code", label: "Coding workspace" },
  { route: "workbench", icon: "activity", label: "Agent workbench" },
  { route: "models", icon: "model", label: "Models" },
  { route: "images", icon: "image", label: "Image Studio" },
  { route: "marketplace", icon: "sparkle", label: "Marketplace" },
];

type ConversationPatch = { pinned?: boolean; archived?: boolean; title?: string };

export function Sidebar({ route, onNavigate, conversations, query, collapsed, onQueryChange, onToggleCollapsed, onNewChat, onSelectConversation, onConversationMetadata }: { route: Route; onNavigate: (route: Route) => void; conversations: DesktopConversation[]; query: string; collapsed: boolean; onQueryChange: (query: string) => void; onToggleCollapsed: () => void; onNewChat: () => void; onSelectConversation: (id: string) => void; onConversationMetadata: (id: string, patch: ConversationPatch) => Promise<boolean> }) {
  const [menuConversationId, setMenuConversationId] = useState<string | null>(null);
  const [renameDraft, setRenameDraft] = useState("");
  const [renameBusy, setRenameBusy] = useState(false);
  useEffect(() => {
    if (!menuConversationId) return;
    const closeOnEscape = (event: KeyboardEvent) => { if (event.key === "Escape") setMenuConversationId(null); };
    document.addEventListener("keydown", closeOnEscape);
    return () => document.removeEventListener("keydown", closeOnEscape);
  }, [menuConversationId]);
  const openMenu = (conversation: DesktopConversation) => { setMenuConversationId(conversation.id); setRenameDraft(conversation.title); };
  const runMetadataAction = async (id: string, patch: ConversationPatch) => { const saved = await onConversationMetadata(id, patch); if (saved) setMenuConversationId(null); };
  const renameConversation = async (event: FormEvent<HTMLFormElement>) => { event.preventDefault(); const title = renameDraft.trim(); if (!menuConversationId || !title || renameBusy) return; setRenameBusy(true); const saved = await onConversationMetadata(menuConversationId, { title }); setRenameBusy(false); if (saved) setMenuConversationId(null); };
  return <aside className={`app-sidebar ${collapsed ? "is-collapsed" : ""}`} aria-label="Primary navigation">
    <div className="sidebar-brand"><div className="brand-mark"><img src={SKAI_WOLF_ICON} alt="" aria-hidden="true" /></div><div><p className="eyebrow">WORKSPACE</p><strong>{PRODUCT_DISPLAY_NAME}</strong></div></div>
    <div className="sidebar-top-actions"><Button variant="primary" className="new-chat-button" onClick={onNewChat}>＋ <span>New chat</span></Button><button className="sidebar-collapse" type="button" onClick={onToggleCollapsed} aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"}>{collapsed ? "›" : "‹"}</button></div>
    <nav className="sidebar-nav" aria-label="Workspace"><p className="sidebar-label">WORKSPACE</p>{primary.map((item) => <button key={item.route} className={`sidebar-nav-item ${route === item.route ? "is-active" : ""}`} onClick={() => onNavigate(item.route)} type="button"><NavIcon name={item.icon} />{item.label}</button>)}</nav>
    <nav className="sidebar-nav sidebar-nav-secondary" aria-label="Utilities"><p className="sidebar-label">UTILITIES</p><button className={`sidebar-nav-item ${route === "terminal" ? "is-active" : ""}`} onClick={() => onNavigate("terminal")} type="button"><NavIcon name="terminal" />Terminal</button><button className={`sidebar-nav-item ${route === "memory" ? "is-active" : ""}`} onClick={() => onNavigate("memory")} type="button"><NavIcon name="memory" />Memory</button></nav>
    <div className="sidebar-history"><div className="sidebar-label-row"><p className="sidebar-label">RECENT CHATS</p><button type="button" aria-label="Start a new chat" onClick={onNewChat}>＋</button></div><input className="sidebar-search" value={query} onChange={(event) => onQueryChange(event.target.value)} placeholder="Search chats…" aria-label="Search chats" />{conversations.length === 0 ? <p className="sidebar-muted">No saved chats yet.</p> : conversations.filter((conversation) => conversation.title.toLowerCase().includes(query.toLowerCase())).slice(0, 6).map((conversation) => <div className="sidebar-history-row" key={conversation.id}><button className="sidebar-history-item" onClick={() => { onSelectConversation(conversation.id); onNavigate("chat"); setMenuConversationId(null); }} onContextMenu={(event) => { event.preventDefault(); openMenu(conversation); }} type="button">{conversation.title}</button><button className="sidebar-history-menu-trigger" type="button" aria-label={`Actions for ${conversation.title}`} aria-haspopup="menu" aria-expanded={menuConversationId === conversation.id} onClick={() => menuConversationId === conversation.id ? setMenuConversationId(null) : openMenu(conversation)}>•••</button>{menuConversationId === conversation.id && <div className="sidebar-context-menu" role="menu" aria-label={`Actions for ${conversation.title}`}><button type="button" role="menuitem" onClick={() => { onSelectConversation(conversation.id); onNavigate("chat"); setMenuConversationId(null); }}>Open chat</button><form onSubmit={(event) => void renameConversation(event)}><label><span className="sr-only">New name for {conversation.title}</span><input value={renameDraft} onChange={(event) => setRenameDraft(event.target.value)} maxLength={200} aria-label="New conversation name" /></label><button type="submit" disabled={renameBusy || !renameDraft.trim()}>{renameBusy ? "Saving…" : "Rename"}</button></form><button type="button" role="menuitem" onClick={() => void runMetadataAction(conversation.id, { pinned: !conversation.pinned })}>{conversation.pinned ? "Unpin" : "Pin"}</button><button type="button" role="menuitem" onClick={() => void runMetadataAction(conversation.id, { archived: true })}>Archive</button></div>}</div>)}</div>
    <button className={`sidebar-settings ${route === "settings" ? "is-active" : ""}`} onClick={() => onNavigate("settings")} type="button"><NavIcon name="settings" />Settings</button>
  </aside>;
}
