import { useEffect, useMemo, useRef, useState, type KeyboardEvent } from "react";
import type { ChatAttachment, DesktopQueuedMessage, TaskEnvironment } from "../../../shared/contracts";
import type { Message, Model, Route } from "../../app/types";
import { Button, Card, EmptyState } from "../../components/ui/Primitives";
import { ComposerCommandMenu } from "../../components/ui/ComposerCommandMenu";
import { EnvironmentPicker } from "../../components/ui/EnvironmentPicker";
import { ModalDialog } from "../../components/ui/ModalDialog";
import { OverflowMenu } from "../../components/ui/OverflowMenu";
import { ModelPicker } from "../../components/ui/ModelPicker";
import { AttachmentList } from "./AttachmentList";
import { ChatMessage } from "./ChatMessage";
import { composerCommands, type ComposerCommand } from "./composerCommands";

const SKAI_WOLF_ICON = new URL("../../../../../assets/branding/skaiwolficon.png", import.meta.url).href;
const CHAT_SCROLL_KEY_PREFIX = "skai.chat.scroll.";

function chatScrollKey(conversationId: string | null) {
  return `${CHAT_SCROLL_KEY_PREFIX}${conversationId ?? "new"}`;
}

type ChatPageProps = {
  provider: string;
  model: string;
  models: Model[];
  messages: Message[];
  busy: boolean;
  taskBusy: string | null;
  attachments: ChatAttachment[];
  queuedMessages: DesktopQueuedMessage[];
  conversationId: string | null;
  conversationTitle: string;
  pinned: boolean;
  archived: boolean;
  onSend: (text: string, attachments: ChatAttachment[]) => void;
  onQueue: (text: string, attachments: ChatAttachment[]) => void;
  onRemoveQueued: (index: number) => void;
  onAttach: () => void;
  onRemoveAttachment: (index: number) => void;
  onModelChange: (model: string) => void;
  onNavigate: (route: Route) => void;
  onPlanTask: (title: string, instruction: string, environment: TaskEnvironment) => Promise<boolean>;
  onExport: () => void;
  onFork: () => void;
  onForkFromMessage: (index: number) => void;
  onRetryMessage: (index: number) => void;
  onEditMessage: (index: number, content: string) => Promise<boolean>;
  onTogglePinned: () => void;
  onArchive: () => void;
  onRenameConversation: (title: string) => Promise<boolean>;
};

export function ChatPage({ provider, model, models, messages, busy, taskBusy, attachments, queuedMessages, conversationId, conversationTitle, pinned, archived, onSend, onQueue, onRemoveQueued, onAttach, onRemoveAttachment, onModelChange, onNavigate, onPlanTask, onExport, onFork, onForkFromMessage, onRetryMessage, onEditMessage, onTogglePinned, onArchive, onRenameConversation }: ChatPageProps) {
  const [draft, setDraft] = useState(() => localStorage.getItem("skai.chat.draft") ?? "");
  const [copiedMessage, setCopiedMessage] = useState<number | null>(null);
  const [taskTitle, setTaskTitle] = useState("");
  const [taskInstruction, setTaskInstruction] = useState("");
  const [taskEnvironment, setTaskEnvironment] = useState<TaskEnvironment>("local");
  const [taskComposerOpen, setTaskComposerOpen] = useState(() => localStorage.getItem("skai.chat.taskComposerOpen") === "true");
  const [renameOpen, setRenameOpen] = useState(false);
  const [renameDraft, setRenameDraft] = useState(conversationTitle);
  const [renameBusy, setRenameBusy] = useState(false);
  const [showJumpToLatest, setShowJumpToLatest] = useState(false);
  const [commandIndex, setCommandIndex] = useState(0);
  const renameInputRef = useRef<HTMLInputElement>(null);
  const messageListRef = useRef<HTMLDivElement>(null);
  const previousConversationIdRef = useRef(conversationId);
  const restoreScrollPendingRef = useRef(false);
  const stickToLatestRef = useRef(true);
  const commandQuery = draft.match(/^\/([^\s]*)$/)?.[1] ?? null;
  const commandMatches = useMemo(() => commandQuery === null ? [] : composerCommands.filter((command) => `${command.label} ${command.detail} ${command.shortcut}`.toLowerCase().includes(commandQuery.toLowerCase())), [commandQuery]);

  useEffect(() => { localStorage.setItem("skai.chat.draft", draft); }, [draft]);
  useEffect(() => { localStorage.setItem("skai.chat.taskComposerOpen", String(taskComposerOpen)); }, [taskComposerOpen]);
  useEffect(() => { setCommandIndex(0); }, [commandQuery]);
  useEffect(() => { if (!renameOpen) setRenameDraft(conversationTitle); }, [conversationTitle, renameOpen]);
  useEffect(() => {
    const key = chatScrollKey(conversationId);
    return () => { localStorage.setItem(key, String(Math.max(0, Math.round(messageListRef.current?.scrollTop ?? 0)))); };
  }, [conversationId]);
  useEffect(() => {
    const node = messageListRef.current;
    if (!node) return;
    const updateScrollState = () => {
      const distanceFromLatest = node.scrollHeight - node.scrollTop - node.clientHeight;
      const atLatest = distanceFromLatest < 56;
      stickToLatestRef.current = atLatest;
      setShowJumpToLatest(!atLatest && node.scrollHeight > node.clientHeight + 12);
    };
    node.addEventListener("scroll", updateScrollState, { passive: true });
    updateScrollState();
    return () => node.removeEventListener("scroll", updateScrollState);
  }, [messages.length > 0]);
  useEffect(() => {
    const conversationChanged = previousConversationIdRef.current !== conversationId;
    if (conversationChanged) {
      previousConversationIdRef.current = conversationId;
      restoreScrollPendingRef.current = true;
    }
    if (conversationChanged || restoreScrollPendingRef.current) {
      restoreScrollPendingRef.current = false;
      const saved = Number(localStorage.getItem(chatScrollKey(conversationId)) ?? "0");
      const top = Number.isFinite(saved) ? Math.max(0, saved) : 0;
      window.requestAnimationFrame(() => { if (messageListRef.current) messageListRef.current.scrollTop = top; });
      return;
    }
    if (!stickToLatestRef.current) return;
    window.requestAnimationFrame(() => { if (messageListRef.current) messageListRef.current.scrollTo({ top: messageListRef.current.scrollHeight, behavior: "smooth" }); });
  }, [conversationId, messages.length, busy]);

  const jumpToLatest = () => {
    stickToLatestRef.current = true;
    setShowJumpToLatest(false);
    messageListRef.current?.scrollTo({ top: messageListRef.current.scrollHeight, behavior: "smooth" });
  };

  const submit = () => {
    const text = draft.trim();
    if (!text && attachments.length === 0) return;
    if (busy) onQueue(text, attachments);
    else onSend(text, attachments);
    setDraft("");
  };

  const copyMessage = async (content: string, index: number) => {
    if (!navigator.clipboard) return;
    try {
      await navigator.clipboard.writeText(content);
      setCopiedMessage(index);
      window.setTimeout(() => setCopiedMessage((current) => current === index ? null : current), 1400);
    } catch {
      // Clipboard access can be unavailable in a hardened desktop context.
    }
  };

  const planTask = async () => {
    if (!taskTitle.trim() || !taskInstruction.trim() || taskBusy === "planning") return;
    const planned = await onPlanTask(taskTitle.trim(), taskInstruction.trim(), taskEnvironment);
    if (planned) {
      setTaskTitle("");
      setTaskInstruction("");
      setTaskComposerOpen(false);
      onNavigate("workbench");
    }
  };

  const selectComposerCommand = (command: ComposerCommand) => {
    setDraft("");
    if (command.opensTaskPlanner) {
      setTaskComposerOpen(true);
      return;
    }
    if (command.route) onNavigate(command.route);
  };

  const handleComposerKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (commandQuery !== null && commandMatches.length > 0) {
      if (event.key === "ArrowDown") { event.preventDefault(); setCommandIndex((index) => Math.min(index + 1, commandMatches.length - 1)); return; }
      if (event.key === "ArrowUp") { event.preventDefault(); setCommandIndex((index) => Math.max(index - 1, 0)); return; }
      if (event.key === "Enter" && !event.shiftKey) { event.preventDefault(); selectComposerCommand(commandMatches[commandIndex]); return; }
    }
    if (event.key === "Escape" && commandQuery !== null) { event.preventDefault(); setDraft(""); return; }
    if (event.key === "Enter" && !event.shiftKey) { event.preventDefault(); submit(); }
  };

  const renameConversation = async () => {
    const title = renameDraft.trim();
    if (!title || !conversationId || renameBusy) return;
    setRenameBusy(true);
    const saved = await onRenameConversation(title);
    setRenameBusy(false);
    if (saved) setRenameOpen(false);
  };

  return <div className="feature-page chat-page">
    <div className="chat-context-bar">
      <div><p className="section-kicker">CONVERSATION</p><strong>{conversationId ? conversationTitle : "New chat"}</strong><span>{provider} · saved on this device · keep the thread focused, then turn work into a reviewed task when needed.</span></div>
      <div className="chat-header-actions"><ModelPicker model={model} provider={provider} models={models} onChange={onModelChange} onOpenModels={() => onNavigate("models")} /><Button variant="ghost" disabled={!conversationId} onClick={() => setRenameOpen(true)}>Rename</Button><Button variant="ghost" onClick={onExport}>Export</Button><OverflowMenu label="More" items={[{ label: pinned ? "Unpin chat" : "Pin chat", disabled: !conversationId, onSelect: onTogglePinned }, { label: "Fork chat", disabled: !conversationId, onSelect: onFork }, { label: archived ? "Unarchive chat" : "Archive chat", disabled: !conversationId, onSelect: onArchive }]} /></div>
    </div>
    <div className="chat-layout">
      <main className="chat-column">
        <Card className="conversation-card"><div className="conversation-header"><span className="conversation-context">LOCAL CHAT</span><span className="conversation-count">{messages.length} messages</span></div>{messages.length === 0 ? <EmptyState visual={<img src={SKAI_WOLF_ICON} alt="" />} title="Start a conversation" description="Ask a question, attach a file, or jump into a coding workspace." /> : <><div className="message-list" ref={messageListRef}>{messages.map((message, index) => <ChatMessage key={`${message.role}-${index}`} message={message} index={index} model={model} busy={busy} conversationId={conversationId} copied={copiedMessage === index} onCopy={(content, itemIndex) => void copyMessage(content, itemIndex)} onRetry={onRetryMessage} onFork={onForkFromMessage} onEdit={onEditMessage} />)}{busy && <article className="message-bubble assistant"><span>{model}</span><p className="typing">Thinking…</p></article>}</div>{showJumpToLatest && <div className="chat-latest-action"><Button variant="secondary" type="button" onClick={jumpToLatest}>Jump to latest ↓</Button></div>}</>}</Card>
        <Card className="composer-card">{commandQuery !== null && <ComposerCommandMenu commands={commandMatches} activeIndex={commandIndex} onActiveIndexChange={setCommandIndex} onSelect={selectComposerCommand} />}{queuedMessages.length > 0 && <div className="queued-messages" aria-label="Queued messages"><div className="queued-heading"><span className="section-kicker">UP NEXT</span><span>{queuedMessages.length}/8 queued</span></div>{queuedMessages.map((queued) => <div className="queued-message" key={queued.index}><span>{queued.text}</span>{queued.attachments?.length ? <small>{queued.attachments.map((attachment) => attachment.fileName).join(", ")}</small> : null}<button type="button" onClick={() => onRemoveQueued(queued.index)} aria-label={`Remove queued message ${queued.index + 1}`}>×</button></div>)}</div>}{attachments.length > 0 && <AttachmentList attachments={attachments} onRemove={onRemoveAttachment} />}<textarea value={draft} onChange={(event) => setDraft(event.target.value)} onKeyDown={handleComposerKeyDown} placeholder={busy ? "Type a follow-up while the model responds…" : "Message your model…"} aria-label="Message your model" aria-controls={commandQuery !== null ? "composer-command-menu" : undefined} rows={3} /><div className="composer-actions"><Button variant="ghost" type="button" onClick={onAttach} disabled={attachments.length >= 5}>Attach</Button><div><span className="composer-hint">Enter to {busy ? "queue" : "send"} · Shift+Enter for a new line · / for commands</span><Button variant={busy ? "secondary" : "primary"} disabled={!draft.trim() && attachments.length === 0} onClick={submit}>{busy ? "Queue ↗" : "Send ↗"}</Button></div></div></Card>
      </main>
      <aside className="chat-rail">{messages.length === 0 ? <Card className="rail-card"><p className="section-kicker">QUICK START</p><button onClick={() => onNavigate("coding")} type="button"><strong>Build in a project</strong><span>Open files, terminal, and agent tasks</span></button><button onClick={() => onNavigate("images")} type="button"><strong>Create an image</strong><span>Use a local image workflow</span></button><button onClick={() => onNavigate("workbench")} type="button"><strong>Run an agent</strong><span>Plan, execute, and verify work</span></button><button onClick={() => onNavigate("marketplace")} type="button"><strong>Find a model or tool</strong><span>Browse voices, image models, skills, and runtimes</span></button></Card> : <Card className="rail-card chat-thread-summary"><p className="section-kicker">ACTIVE THREAD</p><strong>{conversationTitle}</strong><span>{messages.length} messages · {model}</span><small>Saved locally on this device.</small></Card>}<form className={`task-composer chat-task-composer ${taskComposerOpen ? "is-open" : ""}`} onSubmit={(event) => { event.preventDefault(); void planTask(); }}><div className="task-composer-heading"><div><p className="section-kicker">TASK BRIEF</p><h3>Plan local work</h3><p>Prepare a task for review before any tools run.</p></div><div className="task-composer-heading-actions"><span className="task-composer-badge">Local only</span><Button variant="ghost" type="button" className="task-composer-toggle" aria-expanded={taskComposerOpen} aria-controls="chat-task-composer-fields" onClick={() => setTaskComposerOpen((open) => !open)}>{taskComposerOpen ? "Collapse" : "Open"}</Button></div></div>{taskComposerOpen && <div id="chat-task-composer-fields"><div className="task-composer-fields"><label>Task title<input value={taskTitle} onChange={(event) => setTaskTitle(event.target.value)} placeholder="Triage the next change" /></label><label>What should happen?<textarea value={taskInstruction} onChange={(event) => setTaskInstruction(event.target.value)} placeholder="Describe the outcome and how you will verify it." rows={4} /></label></div><EnvironmentPicker value={taskEnvironment} onChange={setTaskEnvironment} /><div className="task-composer-footer"><span>Creates a proposed task. Nothing runs yet.</span><Button variant="primary" type="submit" disabled={taskBusy === "planning" || !taskTitle.trim() || !taskInstruction.trim()}>{taskBusy === "planning" ? "Planning…" : "Add to task plan"}</Button></div></div>}</form></aside>
    </div>
    <ModalDialog open={renameOpen} onClose={() => setRenameOpen(false)} titleId="rename-conversation-title" initialFocusRef={renameInputRef}><form onSubmit={(event) => { event.preventDefault(); void renameConversation(); }}><div className="dialog-heading"><div><p className="section-kicker">CONVERSATION</p><h3 id="rename-conversation-title">Rename chat</h3><p>Use a short name that makes this thread easy to find later.</p></div><button type="button" onClick={() => setRenameOpen(false)} aria-label="Close rename dialog">×</button></div><label>Conversation name<input ref={renameInputRef} value={renameDraft} onChange={(event) => setRenameDraft(event.target.value)} maxLength={200} autoComplete="off" /></label><div className="dialog-actions"><Button variant="ghost" type="button" onClick={() => setRenameOpen(false)}>Cancel</Button><Button variant="primary" type="submit" disabled={renameBusy || !renameDraft.trim()}>{renameBusy ? "Saving…" : "Save name"}</Button></div></form></ModalDialog>
  </div>;
}
