import { useEffect, useRef, useState, type FormEvent } from "react";
import type { Route } from "../../app/types";
import type { DesktopConversation, DesktopProject, DesktopTask } from "../../../shared/contracts";
import { PRODUCT_DISPLAY_NAME } from "../../../shared/productIdentity";
import { NavIcon, type NavIconName } from "../ui/NavIcon";
import { Button } from "../ui/Primitives";

const SKAI_WOLF_ICON = new URL("../../../../../assets/branding/skaiwolficon.png", import.meta.url).href;
const railItems: Array<{ route: Route; icon: NavIconName; label: string }> = [
  { route: "chat", icon: "chat", label: "Home" },
  { route: "images", icon: "image", label: "Image Gen" },
  { route: "usage", icon: "activity", label: "Usage" },
];
const toolRoutes: Array<{ route: Route; label: string; detail: string; icon: NavIconName }> = [
  { route: "coding", label: "Coding workspace", detail: "Files, editor, terminal, and review", icon: "code" },
  { route: "workbench", label: "Agent workbench", detail: "Tasks, approvals, and recovery", icon: "activity" },
  { route: "models", label: "Models", detail: "Local runtimes and model selection", icon: "model" },
  { route: "marketplace", label: "Marketplace", detail: "Skills, tools, voices, and models", icon: "sparkle" },
  { route: "terminal", label: "Terminal", detail: "Project-scoped command session", icon: "terminal" },
  { route: "memory", label: "Memory", detail: "Approved local notes and documents", icon: "memory" },
  { route: "voice", label: "Voice Studio", detail: "Local speech profiles and preview", icon: "activity" },
  { route: "permissions", label: "Permissions", detail: "Review local approval boundaries", icon: "settings" },
  { route: "onboarding", label: "First-run setup", detail: "Connect a runtime and choose a model", icon: "sparkle" },
];

type ConversationPatch = { pinned?: boolean; archived?: boolean; title?: string };
type SidebarProps = {
  route: Route;
  onNavigate: (route: Route) => void;
  conversations: DesktopConversation[];
  projects: DesktopProject[];
  selectedProject: DesktopProject | null;
  tasks: DesktopTask[];
  taskBusy: string | null;
  query: string;
  collapsed: boolean;
  onQueryChange: (query: string) => void;
  onToggleSidebar: () => void;
  onNewChat: () => void;
  onSelectConversation: (id: string) => void;
  onOpenProject: (project: DesktopProject) => void;
  onConversationMetadata: (id: string, patch: ConversationPatch) => Promise<boolean>;
  onOpenCommandPalette: () => void;
};

export function Sidebar({ route, onNavigate, conversations, projects, selectedProject, tasks, taskBusy, query, collapsed, onQueryChange, onToggleSidebar, onNewChat, onSelectConversation, onOpenProject, onConversationMetadata, onOpenCommandPalette }: SidebarProps) {
  const [toolsOpen, setToolsOpen] = useState(false);
  const [expandedProjectIds, setExpandedProjectIds] = useState<string[]>(projects.map((project) => project.id));
  const [menuConversationId, setMenuConversationId] = useState<string | null>(null);
  const [renameDraft, setRenameDraft] = useState("");
  const [renameBusy, setRenameBusy] = useState(false);
  const menuFirstActionRef = useRef<HTMLButtonElement>(null);
  useEffect(() => { setExpandedProjectIds((current) => Array.from(new Set([...current, ...projects.map((project) => project.id)]))); }, [projects]);
  useEffect(() => {
    if (!menuConversationId) return;
    const closeOnEscape = (event: KeyboardEvent) => { if (event.key === "Escape") setMenuConversationId(null); };
    const closeOnOutsidePointer = (event: PointerEvent) => { const target = event.target; if (target instanceof Element && (target.closest(".sidebar-context-menu") || target.closest(".sidebar-history-menu-trigger"))) return; setMenuConversationId(null); };
    document.addEventListener("keydown", closeOnEscape); document.addEventListener("pointerdown", closeOnOutsidePointer);
    return () => { document.removeEventListener("keydown", closeOnEscape); document.removeEventListener("pointerdown", closeOnOutsidePointer); };
  }, [menuConversationId]);
  useEffect(() => { if (menuConversationId) menuFirstActionRef.current?.focus(); }, [menuConversationId]);
  const filteredConversations = conversations.filter((conversation) => conversation.title.toLowerCase().includes(query.trim().toLowerCase()));
  const pinnedConversations = filteredConversations.filter((conversation) => conversation.pinned);
  const recentConversations = filteredConversations.filter((conversation) => !conversation.pinned);
  const activeTasks = tasks.filter((task) => ["queued", "running", "executing", "awaiting_approval", "verifying"].includes(task.status));
  const openMenu = (conversation: DesktopConversation) => { setMenuConversationId(conversation.id); setRenameDraft(conversation.title); };
  const runMetadataAction = async (id: string, patch: ConversationPatch) => { const saved = await onConversationMetadata(id, patch); if (saved) setMenuConversationId(null); };
  const renameConversation = async (event: FormEvent<HTMLFormElement>) => { event.preventDefault(); const title = renameDraft.trim(); if (!menuConversationId || !title || renameBusy) return; setRenameBusy(true); const saved = await onConversationMetadata(menuConversationId, { title }); setRenameBusy(false); if (saved) setMenuConversationId(null); };
  const renderConversation = (conversation: DesktopConversation) => <div className="sidebar-history-row" key={conversation.id}><button className="sidebar-history-item" onClick={() => { onSelectConversation(conversation.id); onNavigate("chat"); setMenuConversationId(null); }} onContextMenu={(event) => { event.preventDefault(); openMenu(conversation); }} type="button"><span>{conversation.title}</span>{conversation.projectName ? <small>{conversation.projectName}</small> : null}</button><button className="sidebar-history-menu-trigger" type="button" aria-label={`Actions for ${conversation.title}`} aria-haspopup="menu" aria-expanded={menuConversationId === conversation.id} onClick={() => menuConversationId === conversation.id ? setMenuConversationId(null) : openMenu(conversation)}>•••</button>{menuConversationId === conversation.id && <div className="sidebar-context-menu" role="menu" aria-label={`Actions for ${conversation.title}`}><button ref={menuFirstActionRef} type="button" role="menuitem" onClick={() => { onSelectConversation(conversation.id); onNavigate("chat"); setMenuConversationId(null); }}>Open chat</button><form onSubmit={(event) => void renameConversation(event)}><label><span className="sr-only">New name for {conversation.title}</span><input value={renameDraft} onChange={(event) => setRenameDraft(event.target.value)} maxLength={200} aria-label="New conversation name" /></label><button type="submit" disabled={renameBusy || !renameDraft.trim()}>{renameBusy ? "Saving…" : "Rename"}</button></form><button type="button" role="menuitem" onClick={() => void runMetadataAction(conversation.id, { pinned: !conversation.pinned })}>{conversation.pinned ? "Unpin" : "Pin"}</button><button type="button" role="menuitem" onClick={() => void runMetadataAction(conversation.id, { archived: true })}>Archive</button></div>}</div>;
  return <><nav className="global-rail" aria-label="Global navigation"><div className="global-rail-mark"><img src={SKAI_WOLF_ICON} alt="Skai Desktop" /></div><div className="global-rail-items">{railItems.map((item) => <button key={item.route} className={`global-rail-item ${route === item.route ? "is-active" : ""}`} onClick={() => onNavigate(item.route)} type="button" aria-label={item.label} aria-current={route === item.route ? "page" : undefined}><NavIcon name={item.icon} /></button>)}</div><div className="global-rail-footer"><button className="global-rail-item global-rail-command" onClick={onOpenCommandPalette} type="button" aria-label="Open command palette">⌘</button><button className={`global-rail-item ${route === "settings" ? "is-active" : ""}`} onClick={() => onNavigate("settings")} type="button" aria-label="Settings"><NavIcon name="settings" /></button><button className="global-rail-profile" type="button" aria-label="Skai Desktop profile">SK</button></div></nav><aside className={`app-sidebar thread-sidebar ${collapsed ? "is-collapsed" : ""}`} aria-label="Projects and conversations"><div className="sidebar-brand"><div className="brand-mark"><img src={SKAI_WOLF_ICON} alt="" aria-hidden="true" /></div><div><p className="eyebrow">LOCAL WORKSPACE</p><strong>{PRODUCT_DISPLAY_NAME}</strong></div></div><div className="sidebar-workspace-row"><button type="button" className="sidebar-workspace-switcher" onClick={() => setToolsOpen((open) => !open)} aria-expanded={toolsOpen}><span><small>WORKSPACE</small><strong>{selectedProject?.name ?? "Personal workspace"}</strong></span><span aria-hidden="true">⌄</span></button><button className="sidebar-collapse" type="button" onClick={onToggleSidebar} aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"}>{collapsed ? "›" : "‹"}</button></div>{toolsOpen && <div className="sidebar-tools-menu" role="menu" aria-label="Workspace tools"><p className="sidebar-menu-kicker">OPEN IN WORKSPACE</p>{toolRoutes.map((item) => <button type="button" role="menuitem" className={route === item.route ? "is-active" : ""} key={item.route} onClick={() => { onNavigate(item.route); setToolsOpen(false); }}><NavIcon name={item.icon} /><span><strong>{item.label}</strong><small>{item.detail}</small></span></button>)}</div>}<div className="sidebar-top-actions"><Button variant="primary" className="new-chat-button" onClick={onNewChat}>＋ <span>New chat</span></Button><button className="sidebar-command-button" type="button" onClick={onOpenCommandPalette} aria-label="Open command palette">⌘K</button></div><div className="sidebar-search-wrap"><span aria-hidden="true">⌕</span><input className="sidebar-search" value={query} onChange={(event) => onQueryChange(event.target.value)} placeholder="Search conversations…" aria-label="Search conversations" /><kbd>⌘K</kbd></div><div className="sidebar-scroll"><section className="sidebar-projects" aria-label="Projects"><div className="sidebar-label-row"><p className="sidebar-label">PROJECTS</p><button type="button" onClick={() => onNavigate("coding")} aria-label="Open project workspace">＋</button></div>{projects.length === 0 ? <button type="button" className="sidebar-empty-action" onClick={() => onNavigate("coding")}>Create or connect a project</button> : projects.map((project) => { const expanded = expandedProjectIds.includes(project.id); const projectConversations = filteredConversations.filter((conversation) => conversation.projectId === project.id); return <div className="sidebar-project" key={project.id}><div className={`sidebar-project-row ${selectedProject?.id === project.id ? "is-active" : ""}`}><button type="button" className="sidebar-project-toggle" onClick={() => setExpandedProjectIds((current) => current.includes(project.id) ? current.filter((id) => id !== project.id) : [...current, project.id])} aria-label={`${expanded ? "Collapse" : "Expand"} ${project.name}`}>{expanded ? "⌄" : "›"}</button><button type="button" className="sidebar-project-name" onClick={() => { onOpenProject(project); onNavigate("coding"); }}><strong>{project.name}</strong><small>{project.folderPaths.length ? `${project.folderPaths.length} folder${project.folderPaths.length === 1 ? "" : "s"}` : "No folders"}</small></button></div>{expanded && projectConversations.length > 0 && <div className="sidebar-project-conversations">{projectConversations.map(renderConversation)}</div>}</div>; })}</section>{pinnedConversations.length > 0 && <section className="sidebar-history-section" aria-label="Pinned conversations"><p className="sidebar-label">PINNED</p>{pinnedConversations.map(renderConversation)}</section>}<section className="sidebar-history-section" aria-label="Recent conversations"><div className="sidebar-label-row"><p className="sidebar-label">RECENT</p><span className="sidebar-history-count">{recentConversations.length}</span></div>{recentConversations.length > 0 ? recentConversations.map(renderConversation) : <p className="sidebar-muted">No recent conversations.</p>}</section>{(activeTasks.length > 0 || taskBusy) && <section className="sidebar-task-status" aria-label="Background tasks"><button type="button" onClick={() => onNavigate("workbench")}><span className="sidebar-task-pulse" aria-hidden="true" /><span><strong>{taskBusy ? "Updating local task" : `${activeTasks.length} active task${activeTasks.length === 1 ? "" : "s"}`}</strong><small>Open Agent Workbench</small></span><span aria-hidden="true">›</span></button></section>}</div><div className="sidebar-footer"><span><span className="sidebar-status-dot" />Local only</span><button type="button" onClick={() => onNavigate("settings")} className={route === "settings" ? "is-active" : ""}><NavIcon name="settings" />Settings</button></div></aside></>;
}
