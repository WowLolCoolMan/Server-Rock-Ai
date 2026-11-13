import { useEffect, useMemo, useState } from "react";
import type { AgentResponse, DesktopConversation, DesktopProject, DesktopScheduledTask, DesktopTask, RuntimeHealth } from "../shared/contracts";
import { PRODUCT_DISPLAY_NAME } from "../shared/productIdentity";

type ConnectionState = "checking" | "ready" | "error";
type View = "chat" | "projects" | "workbench" | "models" | "settings";
type Message = { role: "user" | "assistant"; content: string };
type Model = { model: string; vision: string; capabilities: string[] };
const defaultEndpoint = localStorage.getItem("skai.endpoint") ?? "http://localhost:11434/v1";
const defaultModel = localStorage.getItem("skai.model") ?? "llama3.2";

export default function App() {
  const [view, setView] = useState<View>("chat");
  const [connection, setConnection] = useState<ConnectionState>("checking");
  const [health, setHealth] = useState<RuntimeHealth | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [endpoint, setEndpoint] = useState(defaultEndpoint);
  const [model, setModel] = useState(defaultModel);
  const [models, setModels] = useState<Model[]>([]);
  const [projects, setProjects] = useState<DesktopProject[]>([]);
  const [tasks, setTasks] = useState<DesktopTask[]>([]);
  const [scheduledTasks, setScheduledTasks] = useState<DesktopScheduledTask[]>([]);
  const [conversations, setConversations] = useState<DesktopConversation[]>([]);
  const [conversationId, setConversationId] = useState<string | null>(null);
  const [messages, setMessages] = useState<Message[]>([]);
  const [draft, setDraft] = useState("");
  const [busy, setBusy] = useState(false);
  const request = (method: string, params?: Record<string, unknown>) => window.serverRockAgent.request(method, params);
  const saveConnection = () => { localStorage.setItem("skai.endpoint", endpoint); localStorage.setItem("skai.model", model); };
  const discoverModels = async () => { saveConnection(); setError(null); const response = await request("models.list", { endpoint }); if (!response.ok) { setError(response.error?.message ?? "Model discovery failed."); return; } setModels((response.result as Model[]) ?? []); };
  const loadProjects = async () => { setError(null); const response = await request("projects.list"); if (!response.ok) { setError(response.error?.message ?? "Project loading failed."); return; } setProjects((response.result as DesktopProject[]) ?? []); };
  const loadWorkbench = async () => { setError(null); const [taskResponse, scheduleResponse] = await Promise.all([request("tasks.list"), request("scheduled.list")]); if (!taskResponse.ok || !scheduleResponse.ok) { setError(taskResponse.error?.message ?? scheduleResponse.error?.message ?? "Workbench loading failed."); return; } setTasks((taskResponse.result as DesktopTask[]) ?? []); setScheduledTasks((scheduleResponse.result as DesktopScheduledTask[]) ?? []); };
  const loadConversations = async () => { setError(null); const response = await request("conversations.list"); if (!response.ok) { setError(response.error?.message ?? "Conversation history failed."); return; } const loaded = (response.result as DesktopConversation[]) ?? []; setConversations(loaded); const first = loaded[0]; if (first) { setConversationId(first.id); setMessages(first.messages.filter((message) => message.role === "user" || message.role === "assistant") as Message[]); } };
  const saveConversation = async (nextMessages: Message[], id = conversationId) => { const title = nextMessages.find((message) => message.role === "user")?.content.slice(0, 80) || "New chat"; const response = await request("conversation.save", { id: id ?? undefined, title, messages: nextMessages }); if (!response.ok) throw new Error(response.error?.message ?? "Conversation could not be saved."); setConversationId((response.result as { id: string }).id); };
  const sendMessage = async () => {
    const content = draft.trim(); if (!content || busy) return;
    const next = [...messages, { role: "user" as const, content }]; setMessages(next); setDraft(""); setBusy(true); setError(null); saveConnection();
    try { await saveConversation(next); } catch (saveError) { setBusy(false); setError(saveError instanceof Error ? saveError.message : "Conversation could not be saved."); return; }
    const response = await request("chat.complete", { endpoint, model, messages: next }); setBusy(false);
    if (!response.ok) { setError(response.error?.message ?? "The model request failed."); return; }
    const completed = [...next, { role: "assistant" as const, content: (response.result as { content: string }).content }]; setMessages(completed); try { await saveConversation(completed); } catch (saveError) { setError(saveError instanceof Error ? saveError.message : "Conversation could not be saved."); }
  };
  useEffect(() => { document.title = PRODUCT_DISPLAY_NAME; request("runtime.health").then((response: AgentResponse) => { if (!response.ok) { setConnection("error"); setError(response.error?.message ?? "Agent host unavailable."); return; } setHealth(response.result as RuntimeHealth); setConnection("ready"); }).catch((requestError: unknown) => { setConnection("error"); setError(requestError instanceof Error ? requestError.message : "Agent host unavailable."); }); }, []);
  useEffect(() => { if (view === "projects" && connection === "ready") void loadProjects(); }, [view, connection]);
  useEffect(() => { if (view === "workbench" && connection === "ready") void loadWorkbench(); }, [view, connection]);
  useEffect(() => { if (connection === "ready") void loadConversations(); }, [connection]);
  const title = useMemo(() => ({ chat: "Chat", projects: "Projects", workbench: "Agent workbench", models: "Models", settings: "Settings" }[view]), [view]);
  return <div className="app-shell">
    <aside className="sidebar" aria-label="Primary navigation">
      <div className="brand-lockup"><div className="brand-mark" aria-hidden="true">SK</div><div><p className="eyebrow">WORKSPACE</p><h1>{PRODUCT_DISPLAY_NAME}</h1></div></div>
      <nav className="nav-stack">{(["chat", "projects", "workbench", "models"] as View[]).map((item) => <button key={item} className={`nav-item ${view === item ? "active" : ""}`} onClick={() => setView(item)} type="button"><span>{item === "chat" ? "⌁" : item === "projects" ? "◈" : item === "workbench" ? "◇" : "◌"}</span><span>{item === "workbench" ? "Agent workbench" : item[0].toUpperCase() + item.slice(1)}</span></button>)}</nav>
      <div className="sidebar-footer"><div className={`status-dot ${connection}`} /><span>{connection === "ready" ? "Agent host connected" : connection === "checking" ? "Connecting…" : "Agent host unavailable"}</span></div>
    </aside>
    <main className="main-canvas">
      <header className="topbar"><div><p className="eyebrow">DESKTOP WORKSPACE</p><h2>{title}</h2></div><button className="ghost-button" onClick={() => setView("settings")} type="button">Settings</button></header>
      {view === "chat" && <section className="chat-view"><div className="hero-card compact"><div className="hero-copy"><span className="pill">{model}</span><h3>What are you building today?</h3><p>Talk to a local or compatible model through the durable agent host.</p></div><div className="signal-card"><span className="signal-label">RUNTIME SIGNAL</span><strong>{connection === "ready" ? "READY" : connection.toUpperCase()}</strong><span>{health ? `${health.service} · protocol v${health.protocolVersion}` : error ?? "Waiting for the local agent host"}</span></div></div>
        <div className="conversation" aria-live="polite">{messages.length === 0 && <div className="empty-state">Your conversation will stay in this workspace.</div>}{messages.map((message, index) => <div className={`message ${message.role}`} key={`${message.role}-${index}`}><span className="message-role">{message.role === "user" ? "You" : model}</span><p>{message.content}</p></div>)}{busy && <div className="message assistant"><span className="message-role">{model}</span><p className="typing">Thinking…</p></div>}</div>
        <div className="composer"><textarea value={draft} onChange={(event) => setDraft(event.target.value)} onKeyDown={(event) => { if (event.key === "Enter" && !event.shiftKey) { event.preventDefault(); void sendMessage(); } }} placeholder="Message your model…" rows={2} /><button className="primary-button" onClick={() => void sendMessage()} disabled={busy || !draft.trim()} type="button">{busy ? "Working…" : "Send"}</button></div>{error && <p className="inline-error">{error}</p>}
      </section>}
      {view === "models" && <section className="surface-card page-card"><h3>Model connections</h3><p>Discover models from any OpenAI-compatible endpoint.</p><div className="form-row"><input value={endpoint} onChange={(event) => setEndpoint(event.target.value)} placeholder="http://localhost:11434/v1" /><button className="secondary-button" onClick={() => void discoverModels()} type="button">Discover</button></div><div className="model-list">{models.length === 0 ? <div className="empty-state">No models discovered yet.</div> : models.map((item) => <button className={`model-row ${model === item.model ? "selected" : ""}`} key={item.model} onClick={() => { setModel(item.model); saveConnection(); setView("chat"); }} type="button"><strong>{item.model}</strong><span>{item.capabilities.join(" · ") || "text"}</span></button>)}</div>{error && <p className="inline-error">{error}</p>}</section>}
      {view === "settings" && <section className="surface-card page-card"><h3>Workspace settings</h3><p>These preferences stay on this device.</p><label>Model endpoint<input value={endpoint} onChange={(event) => setEndpoint(event.target.value)} onBlur={saveConnection} /></label><label>Default model<input value={model} onChange={(event) => setModel(event.target.value)} onBlur={saveConnection} /></label><button className="primary-button" onClick={() => { saveConnection(); setView("chat"); }} type="button">Save settings</button></section>}
      {view === "projects" && <section className="surface-card page-card"><span className="pill">Persisted local projects</span><h3>{title}</h3><p>Projects are read from the local agent host. No folder contents are opened by this list view.</p><div className="project-list">{projects.length === 0 ? <div className="empty-state">No saved projects yet.</div> : projects.map((project) => <article className="project-row" key={project.id}><div><strong>{project.name}</strong><span>{project.folderPaths.length === 0 ? "No folders connected" : `${project.folderPaths.length} folder${project.folderPaths.length === 1 ? "" : "s"}`}</span></div><small>{project.gitHubRepository ?? "Local project"}</small></article>)}</div>{error && <p className="inline-error">{error}</p>}</section>}
      {view === "workbench" && <section className="surface-card page-card"><span className="pill">Persistent execution</span><h3>{title}</h3><p>Review planned work and scheduled runs without leaving the desktop surface.</p><div className="workbench-grid"><div><div className="section-kicker">AGENT TASKS</div>{tasks.length === 0 ? <div className="empty-state compact-empty">No agent tasks yet.</div> : tasks.map((task) => <article className="task-row" key={task.id}><div><strong>{task.title}</strong><span>{task.role} · {task.status}</span></div><small>{task.resultSummary ?? "Awaiting execution"}</small></article>)}</div><div><div className="section-kicker">SCHEDULED</div>{scheduledTasks.length === 0 ? <div className="empty-state compact-empty">No scheduled runs yet.</div> : scheduledTasks.map((task) => <article className="task-row" key={task.id}><div><strong>{task.prompt}</strong><span>{task.workspaceMode} · {task.status}</span></div><small>{new Date(task.dueAt).toLocaleString()}</small></article>)}</div></div>{error && <p className="inline-error">{error}</p>}</section>}
    </main>
  </div>;
}
