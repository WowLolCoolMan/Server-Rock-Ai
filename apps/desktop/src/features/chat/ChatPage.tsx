import { useEffect, useRef, useState } from "react";
import type {
  ChatAttachment,
  DesktopProject,
  DesktopQueuedMessage,
  DesktopTask,
  TaskEnvironment,
} from "../../../shared/contracts";
import type { Message, Model } from "../../app/types";
import {
  Button,
  Card,
  EmptyState,
} from "../../components/ui/Primitives";
import { EnvironmentPicker } from "../../components/ui/EnvironmentPicker";
import { ModalDialog } from "../../components/ui/ModalDialog";
import { MessageContent } from "./MessageContent";

const SKAI_WOLF_ICON = new URL(
  "../../../../../assets/branding/skaiwolficon.png",
  import.meta.url,
).href;

type ChatPageProps = {
  provider: string;
  model: string;
  models: Model[];
  messages: Message[];
  busy: boolean;
  taskBusy: string | null;
  tasks: DesktopTask[];
  selectedProject: DesktopProject | null;
  attachments: ChatAttachment[];
  queuedMessages: DesktopQueuedMessage[];
  conversationId: string | null;
  conversationTitle: string;
  pinned: boolean;
  archived: boolean;
  effortMode: string;
  fullTextContextInjectionEnabled: boolean;
  webSearchEnabled: boolean;
  documentsOnlyMode: boolean;
  onSend: (text: string, attachments: ChatAttachment[]) => void;
  onQueue: (text: string, attachments: ChatAttachment[]) => void;
  onRemoveQueued: (index: number) => void;
  onAttach: () => void;
  onRemoveAttachment: (index: number) => void;
  onModelChange: (model: string) => void;
  onSettingChange: (key: string, value: string | boolean) => void;
  onDocumentsOnlyChange: (value: boolean) => void;
  onNavigate: (
    route: "coding" | "images" | "marketplace" | "workbench",
  ) => void;
  onPlanTask: (
    title: string,
    instruction: string,
    environment: TaskEnvironment,
  ) => Promise<boolean>;
  onExport: () => void;
  onFork: () => void;
  onForkFromMessage: (index: number) => void;
  onRetryMessage: (index: number) => void;
  onTogglePinned: () => void;
  onArchive: () => void;
  onRenameConversation: (title: string) => Promise<boolean>;
};

export function ChatPage({
  provider,
  model,
  models,
  messages,
  busy,
  taskBusy,
  tasks,
  selectedProject,
  attachments,
  queuedMessages,
  conversationId,
  conversationTitle,
  pinned,
  archived,
  effortMode,
  fullTextContextInjectionEnabled,
  webSearchEnabled,
  documentsOnlyMode,
  onSend,
  onQueue,
  onRemoveQueued,
  onAttach,
  onRemoveAttachment,
  onModelChange,
  onSettingChange,
  onDocumentsOnlyChange,
  onNavigate,
  onPlanTask,
  onExport,
  onFork,
  onForkFromMessage,
  onRetryMessage,
  onTogglePinned,
  onArchive,
  onRenameConversation,
}: ChatPageProps) {
  const draftStorageKey = conversationId
    ? `skai.chat.draft.${conversationId}`
    : "skai.chat.draft.new";
  const skipDraftPersistRef = useRef(false);
  const [draft, setDraft] = useState(
    () => localStorage.getItem(draftStorageKey) ?? "",
  );
  const [copiedMessage, setCopiedMessage] = useState<number | null>(null);
  const [taskTitle, setTaskTitle] = useState("");
  const [taskInstruction, setTaskInstruction] = useState("");
  const [taskEnvironment, setTaskEnvironment] =
    useState<TaskEnvironment>("local");
  const [contextOpen, setContextOpen] = useState(false);
  const [toolsOpen, setToolsOpen] = useState(false);
  const [renameOpen, setRenameOpen] = useState(false);
  const [renameDraft, setRenameDraft] = useState(conversationTitle);
  const [renameBusy, setRenameBusy] = useState(false);
  const renameInputRef = useRef<HTMLInputElement>(null);
  const restoreScrollRef = useRef(false);
  const messageEndRef = useRef<HTMLDivElement>(null);
  const scrollStorageKey = conversationId
    ? `skai.chat.scroll.${conversationId}`
    : "skai.chat.scroll.new";
  const activeTasks = tasks.filter((task) =>
    [
      "queued",
      "running",
      "executing",
      "awaiting_approval",
      "verifying",
    ].includes(task.status),
  );
  const contextRelevant =
    contextOpen ||
    Boolean(taskBusy) ||
    activeTasks.length > 0 ||
    Boolean(selectedProject);
  useEffect(() => {
    skipDraftPersistRef.current = true;
    setDraft(localStorage.getItem(draftStorageKey) ?? "");
  }, [draftStorageKey]);
  useEffect(() => {
    if (skipDraftPersistRef.current) {
      skipDraftPersistRef.current = false;
      return;
    }
    localStorage.setItem(draftStorageKey, draft);
  }, [draftStorageKey, draft]);
  useEffect(() => {
    if (!renameOpen) setRenameDraft(conversationTitle);
  }, [conversationTitle, renameOpen]);
  useEffect(() => {
    restoreScrollRef.current = true;
    requestAnimationFrame(() => {
      const list = document.querySelector<HTMLDivElement>(".message-list");
      if (!list) return;
      const saved = Number(localStorage.getItem(scrollStorageKey));
      list.scrollTop =
        Number.isFinite(saved) && saved >= 0 ? saved : list.scrollHeight;
      restoreScrollRef.current = false;
    });
  }, [scrollStorageKey]);
  useEffect(() => {
    const list = document.querySelector<HTMLDivElement>(".message-list");
    if (!list || restoreScrollRef.current) return;
    const nearBottom =
      list.scrollHeight - list.clientHeight - list.scrollTop < 80;
    if (busy || nearBottom)
      requestAnimationFrame(() => {
        list.scrollTop = list.scrollHeight;
      });
  }, [messages.length, busy]);
  useEffect(() => {
    const list = document.querySelector<HTMLDivElement>(".message-list");
    if (!list) return;
    const saveScroll = () =>
      localStorage.setItem(scrollStorageKey, String(list.scrollTop));
    list.addEventListener("scroll", saveScroll, { passive: true });
    return () => list.removeEventListener("scroll", saveScroll);
  }, [scrollStorageKey, messages.length]);
  const submit = () => {
    const text = draft.trim();
    if (!text && attachments.length === 0) return;
    if (documentsOnlyMode && attachments.length === 0) return;
    if (busy) onQueue(text, attachments);
    else onSend(text, attachments);
    setDraft("");
  };
  const copyMessage = async (content: string, index: number) => {
    if (!navigator.clipboard) return;
    try {
      await navigator.clipboard.writeText(content);
      setCopiedMessage(index);
      window.setTimeout(
        () =>
          setCopiedMessage((current) => (current === index ? null : current)),
        1400,
      );
    } catch {
      /* Clipboard access can be unavailable in a hardened desktop context. */
    }
  };
  const planTask = async () => {
    if (!taskTitle.trim() || !taskInstruction.trim() || taskBusy === "planning")
      return;
    const planned = await onPlanTask(
      taskTitle.trim(),
      taskInstruction.trim(),
      taskEnvironment,
    );
    if (planned) {
      setTaskTitle("");
      setTaskInstruction("");
      onNavigate("workbench");
    }
  };
  const renameConversation = async () => {
    const title = renameDraft.trim();
    if (!title || !conversationId || renameBusy) return;
    setRenameBusy(true);
    const saved = await onRenameConversation(title);
    setRenameBusy(false);
    if (saved) setRenameOpen(false);
  };
  return (
    <div className="feature-page chat-page">
      <div className="chat-context-bar">
        <div>
          <strong>{conversationId ? conversationTitle : "New chat"}</strong>
          <span>
            {provider} · saved on this device · keep the thread focused, then
            turn work into a reviewed task when needed.
          </span>
        </div>
        <div className="chat-header-actions">
          <select
            className="toolbar-select"
            value={model}
            onChange={(event) => onModelChange(event.target.value)}
            aria-label="Choose model"
          >
            <option value={model}>{model}</option>
            {models
              .filter((item) => item.model !== model)
              .map((item) => (
                <option key={item.model} value={item.model}>
                  {item.model}
                </option>
              ))}
          </select>
          <Button
            variant="ghost"
            aria-pressed={contextOpen}
            onClick={() => setContextOpen((open) => !open)}
          >
            {contextRelevant ? "Hide context" : "Context"}
          </Button>
          <Button
            variant="ghost"
            disabled={!conversationId}
            onClick={() => setRenameOpen(true)}
          >
            Rename
          </Button>
          <Button variant="ghost" onClick={onExport}>
            Export
          </Button>
          <Button
            variant="ghost"
            disabled={!conversationId}
            onClick={onTogglePinned}
          >
            {pinned ? "Unpin" : "Pin"}
          </Button>
          <Button variant="ghost" disabled={!conversationId} onClick={onFork}>
            Fork
          </Button>
          <Button
            variant="ghost"
            disabled={!conversationId}
            onClick={onArchive}
          >
            {archived ? "Unarchive" : "Archive"}
          </Button>
        </div>
      </div>
      <div
        className={`chat-layout ${contextRelevant ? "has-context" : "is-focused"}`}
      >
        <main className="chat-column">
          <section
            className={`conversation-canvas ${messages.length === 0 ? "is-empty" : ""}`}
            aria-label="Conversation"
          >
            {messages.length === 0 ? (
              <EmptyState
                visual={<img src={SKAI_WOLF_ICON} alt="" />}
                title="Start a conversation"
                description="Ask a question, attach a file, or jump into a coding workspace."
              />
            ) : (
              <div className="message-list">
                {messages.map((message, index) => (
                  <article
                    className={`message-bubble ${message.role}`}
                    key={`${message.role}-${index}`}
                  >
                    <div className="message-meta">
                      <span>{message.role === "user" ? "You" : model}</span>
                      <div className="message-actions">
                        <Button
                          variant="ghost"
                          onClick={() =>
                            void copyMessage(message.content, index)
                          }
                        >
                          {copiedMessage === index ? "Copied" : "Copy"}
                        </Button>
                        {message.role === "assistant" && (
                          <Button
                            variant="ghost"
                            disabled={busy}
                            onClick={() => onRetryMessage(index)}
                          >
                            Retry
                          </Button>
                        )}
                        <Button
                          variant="ghost"
                          onClick={() => onForkFromMessage(index)}
                          disabled={!conversationId}
                        >
                          Fork from here
                        </Button>
                      </div>
                    </div>
                    <MessageContent content={message.content} />
                    {message.attachments?.length ? (
                      <div className="message-attachments">
                        {message.attachments.map((attachment) => (
                          <span key={attachment.fileName}>
                            {attachment.fileName}
                          </span>
                        ))}
                      </div>
                    ) : null}
                  </article>
                ))}
                {busy && (
                  <article className="message-bubble assistant">
                    <span>{model}</span>
                    <p className="typing">Thinking…</p>
                  </article>
                )}
                <div ref={messageEndRef} />
              </div>
            )}
          </section>
          <Card className="composer-card">
            {queuedMessages.length > 0 && (
              <div className="queued-messages" aria-label="Queued messages">
                <div className="queued-heading">
                  <span className="section-kicker">UP NEXT</span>
                  <span>{queuedMessages.length}/8 queued</span>
                </div>
                {queuedMessages.map((queued) => (
                  <div className="queued-message" key={queued.index}>
                    <span>{queued.text}</span>
                    {queued.attachments?.length ? (
                      <small>
                        {queued.attachments
                          .map((attachment) => attachment.fileName)
                          .join(", ")}
                      </small>
                    ) : null}
                    <button
                      type="button"
                      onClick={() => onRemoveQueued(queued.index)}
                      aria-label={`Remove queued message ${queued.index + 1}`}
                    >
                      ×
                    </button>
                  </div>
                ))}
              </div>
            )}
            {attachments.length > 0 && (
              <div className="attachment-strip">
                {attachments.map((attachment, index) => (
                  <span key={`${attachment.fileName}-${index}`}>
                    {attachment.fileName}
                    <button
                      type="button"
                      onClick={() => onRemoveAttachment(index)}
                      aria-label={`Remove ${attachment.fileName}`}
                    >
                      ×
                    </button>
                  </span>
                ))}
              </div>
            )}
            <textarea
              value={draft}
              onChange={(event) => setDraft(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === "Enter" && !event.shiftKey) {
                  event.preventDefault();
                  submit();
                }
              }}
              placeholder={
                busy
                  ? "Type a follow-up while the model responds…"
                  : "Message your model…"
              }
              aria-label="Message your model"
              rows={3}
            />
            {toolsOpen && (
              <div className="chat-tools-panel" role="region" aria-label="Chat tools">
                <div className="chat-tools-heading">
                  <div>
                    <p className="section-kicker">CHAT TOOLS</p>
                    <strong>Shape this request</strong>
                  </div>
                  <span className="chat-tools-scope">Local preference</span>
                </div>
                <label className="chat-tool-toggle">
                  <span><strong>Documents only</strong><small>Ground the answer only in attached documents.</small></span>
                  <input type="checkbox" checked={documentsOnlyMode} onChange={(event) => onDocumentsOnlyChange(event.target.checked)} />
                </label>
                <label className="chat-tool-toggle">
                  <span><strong>Web research</strong><small>Include bounded search snippets when answering.</small></span>
                  <input type="checkbox" checked={webSearchEnabled} disabled={documentsOnlyMode} onChange={(event) => onSettingChange("webSearchEnabled", event.target.checked)} />
                </label>
                <label className="chat-tool-toggle">
                  <span><strong>Full-text attachments</strong><small>Use full bounded text instead of ranked chunks.</small></span>
                  <input type="checkbox" checked={fullTextContextInjectionEnabled} onChange={(event) => onSettingChange("fullTextContextInjectionEnabled", event.target.checked)} />
                </label>
                <label className="chat-tool-select">
                  <span><strong>Response effort</strong><small>Sets a bounded local output budget.</small></span>
                  <select value={effortMode} onChange={(event) => onSettingChange("effortMode", event.target.value)} aria-label="Response effort">
                    <option value="adaptive">Adaptive</option>
                    <option value="light">Light</option>
                    <option value="standard">Standard</option>
                    <option value="high">High</option>
                    <option value="ultra">Ultra</option>
                  </select>
                </label>
                {documentsOnlyMode && <p className="chat-tools-note">Attach at least one text or PDF document before sending. Web research is disabled in this mode.</p>}
              </div>
            )}
            <div className="composer-actions">
              <Button
                variant="ghost"
                type="button"
                onClick={onAttach}
                disabled={attachments.length >= 5}
              >
                Attach
              </Button>
              <Button
                variant="ghost"
                type="button"
                aria-expanded={toolsOpen}
                aria-pressed={toolsOpen}
                onClick={() => setToolsOpen((open) => !open)}
              >
                Tools
              </Button>
              <div>
                <span className="composer-hint">
                  Enter to {busy ? "queue" : "send"} · Shift+Enter for a new
                  line
                </span>
                <Button
                  variant={busy ? "secondary" : "primary"}
                  disabled={!draft.trim() && attachments.length === 0}
                  onClick={submit}
                >
                  {busy ? "Queue ↗" : "Send ↗"}
                </Button>
              </div>
            </div>
          </Card>
        </main>
        {contextRelevant && (
          <aside className="chat-rail">
            {selectedProject && (
              <Card className="rail-card chat-project-context">
                <p className="section-kicker">CURRENT PROJECT</p>
                <strong>{selectedProject.name}</strong>
                <span>
                  {selectedProject.folderPaths.length
                    ? `${selectedProject.folderPaths.length} folder${selectedProject.folderPaths.length === 1 ? "" : "s"} connected`
                    : "No folders connected yet"}
                </span>
                <button onClick={() => onNavigate("coding")} type="button">
                  Open coding workspace
                </button>
              </Card>
            )}
            {activeTasks.length > 0 || taskBusy ? (
              <Card className="rail-card chat-agent-activity">
                <div className="chat-activity-heading">
                  <div>
                    <p className="section-kicker">AGENT ACTIVITY</p>
                    <strong>
                      {taskBusy
                        ? "Updating local task"
                        : `${activeTasks.length} active task${activeTasks.length === 1 ? "" : "s"}`}
                    </strong>
                  </div>
                  <span className="task-composer-badge">Live</span>
                </div>
                {activeTasks.slice(0, 4).map((task) => (
                  <button
                    type="button"
                    key={task.id}
                    onClick={() => onNavigate("workbench")}
                  >
                    <strong>{task.title}</strong>
                    <span>
                      {task.status.replaceAll("_", " ")} · {task.role}
                    </span>
                  </button>
                ))}
                <button
                  type="button"
                  className="chat-activity-link"
                  onClick={() => onNavigate("workbench")}
                >
                  Open Agent Workbench
                </button>
              </Card>
            ) : null}
            {contextOpen && (
              <>
                <Card className="rail-card">
                  <p className="section-kicker">QUICK START</p>
                  <button onClick={() => onNavigate("coding")} type="button">
                    <strong>Build in a project</strong>
                    <span>Open files, terminal, and agent tasks</span>
                  </button>
                  <button onClick={() => onNavigate("images")} type="button">
                    <strong>Create an image</strong>
                    <span>Use a local image workflow</span>
                  </button>
                  <button onClick={() => onNavigate("workbench")} type="button">
                    <strong>Run an agent</strong>
                    <span>Plan, execute, and verify work</span>
                  </button>
                  <button
                    onClick={() => onNavigate("marketplace")}
                    type="button"
                  >
                    <strong>Find a model or tool</strong>
                    <span>
                      Browse voices, image models, skills, and runtimes
                    </span>
                  </button>
                </Card>
                <form
                  className="task-composer chat-task-composer"
                  onSubmit={(event) => {
                    event.preventDefault();
                    void planTask();
                  }}
                >
                  <div className="task-composer-heading">
                    <div>
                      <p className="section-kicker">TASK BRIEF</p>
                      <h3>Plan local work</h3>
                      <p>Prepare a task for review before any tools run.</p>
                    </div>
                    <span className="task-composer-badge">Local only</span>
                  </div>
                  <div className="task-composer-fields">
                    <label>
                      Task title
                      <input
                        value={taskTitle}
                        onChange={(event) => setTaskTitle(event.target.value)}
                        placeholder="Triage the next change"
                      />
                    </label>
                    <label>
                      What should happen?
                      <textarea
                        value={taskInstruction}
                        onChange={(event) =>
                          setTaskInstruction(event.target.value)
                        }
                        placeholder="Describe the outcome and how you will verify it."
                        rows={4}
                      />
                    </label>
                  </div>
                  <EnvironmentPicker
                    value={taskEnvironment}
                    onChange={setTaskEnvironment}
                  />
                  <div className="task-composer-footer">
                    <span>Creates a proposed task. Nothing runs yet.</span>
                    <Button
                      variant="primary"
                      type="submit"
                      disabled={
                        taskBusy === "planning" ||
                        !taskTitle.trim() ||
                        !taskInstruction.trim()
                      }
                    >
                      {taskBusy === "planning"
                        ? "Planning…"
                        : "Add to task plan"}
                    </Button>
                  </div>
                </form>
              </>
            )}
          </aside>
        )}
      </div>
      <ModalDialog
        open={renameOpen}
        onClose={() => setRenameOpen(false)}
        titleId="rename-conversation-title"
        initialFocusRef={renameInputRef}
      >
        <form
          onSubmit={(event) => {
            event.preventDefault();
            void renameConversation();
          }}
        >
          <div className="dialog-heading">
            <div>
              <p className="section-kicker">CONVERSATION</p>
              <h3 id="rename-conversation-title">Rename chat</h3>
              <p>Use a short name that makes this thread easy to find later.</p>
            </div>
            <button
              type="button"
              onClick={() => setRenameOpen(false)}
              aria-label="Close rename dialog"
            >
              ×
            </button>
          </div>
          <label>
            Conversation name
            <input
              ref={renameInputRef}
              value={renameDraft}
              onChange={(event) => setRenameDraft(event.target.value)}
              maxLength={200}
              autoComplete="off"
            />
          </label>
          <div className="dialog-actions">
            <Button
              variant="ghost"
              type="button"
              onClick={() => setRenameOpen(false)}
            >
              Cancel
            </Button>
            <Button
              variant="primary"
              type="submit"
              disabled={renameBusy || !renameDraft.trim()}
            >
              {renameBusy ? "Saving…" : "Save name"}
            </Button>
          </div>
        </form>
      </ModalDialog>
    </div>
  );
}
