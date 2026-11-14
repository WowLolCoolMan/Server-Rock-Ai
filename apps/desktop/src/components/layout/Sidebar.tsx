import { useEffect, useRef, useState, type FormEvent } from "react";
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

export function Sidebar({ route, onNavigate, conversations, query, collapsed, sidebarWidth, onSidebarWidthChange, onQueryChange, onToggleCollapsed, onNewChat, onSelectConversation, onConversationMetadata }: { route: Route; onNavigate: (route: Route) => void; conversations: DesktopConversation[]; query: string; collapsed: boolean; sidebarWidth: number; onSidebarWidthChange: (width: number) => void; onQueryChange: (query: string) => void; onToggleCollapsed: () => void; onNewChat: () => void; onSelectConversation: (id: string) => void; onConversationMetadata: (id: string, patch: ConversationPatch) => Promise<boolean> }) {
  const [menuConversationId, setMenuConversationId] = useState<string | null>(null);
  const [renameDraft, setRenameDraft] = useState("");
  const [renameBusy, setRenameBusy] = useState(false);
  const resizeState = useRef<{ startX: number; startWidth: number } | null>(null);
  const [resizing, setResizing] = useState(false);
  const menuFirstActionRef = useRef<HTMLButtonElement>(null);
  const menuRef = useRef<HTMLDivElement>(null);
  const menuTriggerRef = useRef<HTMLButtonElement>(null);
  useEffect(() => {
    if (!menuConversationId) return;
    const closeOnEscape = (event: KeyboardEvent) => { if (event.key === "Escape") setMenuConversationId(null); };
    const closeOnOutsidePointer = (event: PointerEvent) => {
      const target = event.target;
      if (target instanceof Element && (target.closest(".sidebar-context-menu") || target.closest(".sidebar-history-menu-trigger"))) return;
      setMenuConversationId(null);
    };
    document.addEventListener("keydown", closeOnEscape);
    document.addEventListener("pointerdown", closeOnOutsidePointer);
    return () => { document.removeEventListener("keydown", closeOnEscape); document.removeEventListener("pointerdown", closeOnOutsidePointer); };
  }, [menuConversationId]);
  useEffect(() => {
    if (menuConversationId) menuFirstActionRef.current?.focus();
    else menuTriggerRef.current?.focus();
  }, [menuConversationId]);
  useEffect(() => {
    if (!resizing) return;
    const onPointerMove = (event: PointerEvent) => {
      const current = resizeState.current;
      if (current) onSidebarWidthChange(current.startWidth + event.clientX - current.startX);
    };
    const stopResizing = () => { resizeState.current = null; setResizing(false); };
    window.addEventListener("pointermove", onPointerMove);
    window.addEventListener("pointerup", stopResizing, { once: true });
    window.addEventListener("blur", stopResizing, { once: true });
    return () => { window.removeEventListener("pointermove", onPointerMove); window.removeEventListener("pointerup", stopResizing); window.removeEventListener("blur", stopResizing); };
  }, [onSidebarWidthChange, resizing]);
  const openMenu = (conversation: DesktopConversation, trigger?: HTMLButtonElement | null) => { menuTriggerRef.current = trigger ?? (document.activeElement instanceof HTMLButtonElement ? document.activeElement : null); setMenuConversationId(conversation.id); setRenameDraft(conversation.title); };
  const onMenuKeyDown = (event: React.KeyboardEvent<HTMLDivElement>) => {
    const menu = menuRef.current;
    if (!menu) return;
    const items = Array.from(menu.querySelectorAll<HTMLElement>("button:not([disabled]), input:not([disabled])"));
    if (!items.length) return;
    const currentIndex = Math.max(0, items.indexOf(document.activeElement as HTMLElement));
    if (event.key !== "ArrowDown" && event.key !== "ArrowUp" && event.key !== "Home" && event.key !== "End") return;
    event.preventDefault();
    const nextIndex = event.key === "Home" ? 0 : event.key === "End" ? items.length - 1 : (currentIndex + (event.key === "ArrowDown" ? 1 : -1) + items.length) % items.length;
    items[nextIndex]?.focus();
  };
  const runMetadataAction = async (id: string, patch: ConversationPatch) => { const saved = await onConversationMetadata(id, patch); if (saved) setMenuConversationId(null); };
  const renameConversation = async (event: FormEvent<HTMLFormElement>) => { event.preventDefault(); const title = renameDraft.trim(); if (!menuConversationId || !title || renameBusy) return; setRenameBusy(true); const saved = await onConversationMetadata(menuConversationId, { title }); setRenameBusy(false); if (saved) setMenuConversationId(null); };
  const startResize = (event: React.PointerEvent<HTMLDivElement>) => { if (collapsed) return; event.preventDefault(); resizeState.current = { startX: event.clientX, startWidth: sidebarWidth }; event.currentTarget.setPointerCapture?.(event.pointerId); setResizing(true); };
  const resizeWithKeyboard = (event: React.KeyboardEvent<HTMLDivElement>) => { if (collapsed) return; const amount = event.key === "ArrowRight" ? 16 : event.key === "ArrowLeft" ? -16 : event.key === "Home" ? 220 - sidebarWidth : event.key === "End" ? 360 - sidebarWidth : 0; if (!amount) return; event.preventDefault(); onSidebarWidthChange(sidebarWidth + amount); };
  return <aside className={`app-sidebar ${collapsed ? "is-collapsed" : ""} ${resizing ? "is-resizing" : ""}`} aria-label="Primary navigation">
    <div className="sidebar-brand"><div className="brand-mark"><img src={SKAI_WOLF_ICON} alt="" aria-hidden="true" /></div><div><p className="eyebrow">WORKSPACE</p><strong>{PRODUCT_DISPLAY_NAME}</strong></div></div>
    <div className="sidebar-top-actions"><Button variant="primary" className="new-chat-button" onClick={onNewChat}>＋ <span>New chat</span></Button><button className="sidebar-collapse" type="button" onClick={onToggleCollapsed} aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"}>{collapsed ? "›" : "‹"}</button></div>
    <nav className="sidebar-nav" aria-label="Workspace"><p className="sidebar-label">WORKSPACE</p>{primary.map((item) => <button key={item.route} className={`sidebar-nav-item ${route === item.route ? "is-active" : ""}`} onClick={() => onNavigate(item.route)} type="button"><NavIcon name={item.icon} />{item.label}</button>)}</nav>
    <nav className="sidebar-nav sidebar-nav-secondary" aria-label="Utilities"><p className="sidebar-label">UTILITIES</p><button className={`sidebar-nav-item ${route === "terminal" ? "is-active" : ""}`} onClick={() => onNavigate("terminal")} type="button"><NavIcon name="terminal" />Terminal</button><button className={`sidebar-nav-item ${route === "memory" ? "is-active" : ""}`} onClick={() => onNavigate("memory")} type="button"><NavIcon name="memory" />Memory</button></nav>
    <div className="sidebar-history"><div className="sidebar-label-row"><p className="sidebar-label">RECENT CHATS</p><button type="button" aria-label="Start a new chat" onClick={onNewChat}>＋</button></div><input className="sidebar-search" value={query} onChange={(event) => onQueryChange(event.target.value)} placeholder="Search chats…" aria-label="Search chats" />{conversations.length === 0 ? <p className="sidebar-muted">No saved chats yet.</p> : conversations.filter((conversation) => conversation.title.toLowerCase().includes(query.toLowerCase())).slice(0, 6).map((conversation) => <div className="sidebar-history-row" key={conversation.id}><button className="sidebar-history-item" onClick={() => { onSelectConversation(conversation.id); onNavigate("chat"); setMenuConversationId(null); }} onContextMenu={(event) => { event.preventDefault(); openMenu(conversation, event.currentTarget); }} type="button">{conversation.title}</button><button className="sidebar-history-menu-trigger" type="button" aria-label={`Actions for ${conversation.title}`} aria-haspopup="menu" aria-expanded={menuConversationId === conversation.id} onClick={(event) => menuConversationId === conversation.id ? setMenuConversationId(null) : openMenu(conversation, event.currentTarget)}>•••</button>{menuConversationId === conversation.id && <div ref={menuRef} className="sidebar-context-menu" role="menu" aria-label={`Actions for ${conversation.title}`} onKeyDown={onMenuKeyDown}><button ref={menuFirstActionRef} type="button" role="menuitem" onClick={() => { onSelectConversation(conversation.id); onNavigate("chat"); setMenuConversationId(null); }}>Open chat</button><form onSubmit={(event) => void renameConversation(event)}><label><span className="sr-only">New name for {conversation.title}</span><input value={renameDraft} onChange={(event) => setRenameDraft(event.target.value)} maxLength={200} aria-label="New conversation name" /></label><button type="submit" role="menuitem" disabled={renameBusy || !renameDraft.trim()}>{renameBusy ? "Saving…" : "Rename"}</button></form><button type="button" role="menuitem" onClick={() => void runMetadataAction(conversation.id, { pinned: !conversation.pinned })}>{conversation.pinned ? "Unpin" : "Pin"}</button><button type="button" role="menuitem" onClick={() => void runMetadataAction(conversation.id, { archived: true })}>Archive</button></div>}</div>)}</div>
    <button className={`sidebar-settings ${route === "settings" ? "is-active" : ""}`} onClick={() => onNavigate("settings")} type="button"><NavIcon name="settings" />Settings</button>
    <div className="sidebar-resize-handle" role="separator" tabIndex={collapsed ? -1 : 0} aria-orientation="vertical" aria-label="Resize sidebar" aria-valuemin={220} aria-valuemax={360} aria-valuenow={sidebarWidth} onPointerDown={startResize} onKeyDown={resizeWithKeyboard} />
  </aside>;
}
