import { useEffect, useState, type KeyboardEvent } from "react";
import type { DesktopProject, DesktopScheduledTask, DesktopTask, ScheduledTaskDraft } from "../../../shared/contracts";
import { Button, Card, EmptyState, PageHeader, StatusBadge } from "../../components/ui/Primitives";

const groups = [
  { key: "proposed", label: "Proposed" },
  { key: "queued", label: "Queued" },
  { key: "running", label: "Running" },
  { key: "awaiting_approval", label: "Awaiting approval" },
  { key: "blocked", label: "Blocked" },
  { key: "completed", label: "Completed" },
  { key: "failed", label: "Failed" },
  { key: "cancelled", label: "Cancelled" },
];

const VIEW_MODE_KEY = "skai.workbench.viewMode";
const SELECTED_TASK_KEY = "skai.workbench.selectedTask";

function toneFor(status: string) {
  return status === "failed" ? "danger" as const : status === "blocked" || status === "awaiting_approval" ? "warning" as const : status === "completed" ? "success" as const : "neutral" as const;
}

function timeLabel(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.valueOf()) ? "Updated time unavailable" : `Updated ${date.toLocaleString()}`;
}

function TaskDetails({ task, openByDefault = false }: { task: DesktopTask; openByDefault?: boolean }) {
  return <details className="task-details" open={openByDefault || undefined}>
    <summary>View task details</summary>
    <dl>
      <div><dt>Task ID</dt><dd>{task.id}</dd></div>
        <div><dt>Project context</dt><dd>{task.projectId ? "Bounded read-only project sample attached" : "No project context attached"}</dd></div>
      <div><dt>Task brief</dt><dd className="task-instruction">{task.instruction}</dd></div>
      <div><dt>Role</dt><dd>{task.role}</dd></div>
      <div><dt>Dependencies</dt><dd>{task.dependsOn.length ? task.dependsOn.join(", ") : "None"}</dd></div>
      <div><dt>Result</dt><dd>{task.resultSummary || "No result recorded yet."}</dd></div>
    </dl>
    {task.status === "awaiting_approval" && <p className="task-capability-note">Execution is blocked until you approve this task. Approval only releases it to the queued state; it does not start the model.</p>}
    {task.status === "failed" && <p className="task-capability-note">Retry is only supported for blocked tasks in the current host. Review the result and create a new plan if needed.</p>}
  </details>;
}

type WorkbenchPageProps = {
  tasks: DesktopTask[];
  scheduled: DesktopScheduledTask[];
  busyTask: string | null;
  loading: boolean;
  error: string | null;
  projects: DesktopProject[];
  selectedProject: DesktopProject | null;
  conversationTitle: string;
  onNavigate: (route: "chat" | "coding") => void;
  onRetry: () => void;
  onTransition: (id: string, operation: "queue" | "cancel" | "retry") => void;
  onExecute: (id: string) => void;
  onApprove: (id: string) => void;
  onDeny: (id: string, reason: string) => void;
  onSchedule: (draft: ScheduledTaskDraft) => Promise<boolean>;
  onScheduledTransition: (id: string, operation: "pause" | "resume" | "cancel" | "runNow") => Promise<boolean>;
};

export function WorkbenchPage({ tasks, scheduled, busyTask, loading, error, projects, selectedProject, conversationTitle, onNavigate, onRetry, onTransition, onExecute, onApprove, onDeny, onSchedule, onScheduledTransition }: WorkbenchPageProps) {
  const [viewMode, setViewMode] = useState<"list" | "board">(() => localStorage.getItem(VIEW_MODE_KEY) === "board" ? "board" : "list");
  const [selectedTaskId, setSelectedTaskId] = useState<string | null>(() => localStorage.getItem(SELECTED_TASK_KEY));
  const selectedTask = tasks.find((task) => task.id === selectedTaskId) ?? null;
  const selectedTaskProject = selectedTask?.projectId ? projects.find((project) => project.id === selectedTask.projectId) : null;
  const [schedulePrompt, setSchedulePrompt] = useState("");
  const [scheduleWhen, setScheduleWhen] = useState(() => new Date(Date.now() + 60 * 60 * 1000).toISOString().slice(0, 16));
  const [scheduleRepeat, setScheduleRepeat] = useState("");

  const orderedTaskIds = groups.flatMap((group) => tasks.filter((task) => task.status === group.key).map((task) => task.id));

  useEffect(() => {
    if (selectedTaskId && tasks.some((task) => task.id === selectedTaskId)) return;
    setSelectedTaskId(tasks[0]?.id ?? null);
  }, [selectedTaskId, tasks]);

  useEffect(() => { localStorage.setItem(VIEW_MODE_KEY, viewMode); }, [viewMode]);
  useEffect(() => {
    if (selectedTaskId) localStorage.setItem(SELECTED_TASK_KEY, selectedTaskId);
    else localStorage.removeItem(SELECTED_TASK_KEY);
  }, [selectedTaskId]);

  const handleTaskKeyDown = (event: KeyboardEvent<HTMLButtonElement>, taskId: string) => {
    const currentIndex = orderedTaskIds.indexOf(taskId);
    if (currentIndex < 0) return;
    const nextIndex = event.key === "ArrowDown" ? Math.min(currentIndex + 1, orderedTaskIds.length - 1)
      : event.key === "ArrowUp" ? Math.max(currentIndex - 1, 0)
      : event.key === "Home" ? 0
      : event.key === "End" ? orderedTaskIds.length - 1
      : -1;
    if (nextIndex < 0 || nextIndex === currentIndex) return;
    event.preventDefault();
    const nextId = orderedTaskIds[nextIndex];
    setSelectedTaskId(nextId);
    document.getElementById(`workbench-task-${nextId}`)?.focus();
  };

  const taskActions = (task: DesktopTask) => <div className="task-card-actions">{task.status === "proposed" && <Button variant="primary" disabled={busyTask === task.id} onClick={() => onTransition(task.id, "queue")}>Request approval</Button>}{task.status === "awaiting_approval" && <><Button variant="primary" disabled={busyTask === task.id} onClick={() => onApprove(task.id)}>Approve for local run</Button><Button variant="ghost" disabled={busyTask === task.id} onClick={() => onDeny(task.id, "User denied execution from the Workbench.")}>Deny</Button></>}{task.status === "queued" && <Button variant="primary" disabled={busyTask === task.id} onClick={() => onExecute(task.id)}>{busyTask === task.id ? "Running…" : "Run locally"}</Button>}{(task.status === "proposed" || task.status === "awaiting_approval" || task.status === "queued") && <Button variant="ghost" disabled={busyTask === task.id} onClick={() => onTransition(task.id, "cancel")}>Cancel</Button>}{task.status === "blocked" && <Button variant="secondary" disabled={busyTask === task.id} onClick={() => onTransition(task.id, "retry")}>Retry</Button>}</div>;

  const queuedCount = tasks.filter((task) => task.status === "proposed" || task.status === "queued").length;
  const awaitingApprovalCount = tasks.filter((task) => task.status === "awaiting_approval").length;
  const runningCount = tasks.filter((task) => task.status === "running").length;
  const completedCount = tasks.filter((task) => task.status === "completed").length;

  return <div className="feature-page workbench-v4">
    <PageHeader eyebrow="AGENT WORKBENCH" title="Plan, execute, verify" description="A clear task lifecycle for work that needs progress, approvals, and recovery." actions={<div className="workbench-view-actions"><Button variant="ghost" onClick={() => onNavigate("chat")}>Return to conversation</Button><Button variant={viewMode === "list" ? "secondary" : "ghost"} aria-pressed={viewMode === "list"} onClick={() => setViewMode("list")}>List view</Button><Button variant={viewMode === "board" ? "secondary" : "ghost"} aria-pressed={viewMode === "board"} onClick={() => setViewMode("board")}>Board view</Button></div>} />
    {(selectedProject || conversationTitle !== "New chat") && <Card className="workbench-context-strip"><div><span className="section-kicker">ACTIVE WORKSPACE CONTEXT</span><strong>{selectedProject?.name ?? "Personal workspace"}</strong><small>{conversationTitle !== "New chat" ? `Conversation: ${conversationTitle}` : "No conversation selected"}</small></div><Button variant="secondary" onClick={() => onNavigate(selectedProject ? "coding" : "chat")}>{selectedProject ? "Open project" : "Open chat"}</Button></Card>}
    <div className="workbench-metrics" aria-label="Agent task summary"><Card><span>Queued</span><strong>{queuedCount}</strong><small>{queuedCount ? "Waiting for execution" : "No queued work"}</small></Card><Card><span>Awaiting approval</span><strong>{awaitingApprovalCount}</strong><small>{awaitingApprovalCount ? "Review required" : "No approvals pending"}</small></Card><Card><span>In progress</span><strong>{runningCount}</strong><small>{runningCount ? "Host-reported running work" : "No active execution"}</small></Card><Card><span>Completed</span><strong>{completedCount}</strong><small>{completedCount ? "Recorded task results" : "No completed tasks"}</small></Card></div>
    {error ? <div className="surface-error workbench-error" role="alert"><strong>Agent workbench is unavailable</strong><p>{error}</p><Button variant="secondary" onClick={onRetry}>Retry</Button></div> : loading ? <div className="workbench-loading" aria-label="Loading agent workbench">{[1, 2, 3].map((item) => <Card className="loading-card" key={item}><span /><span /><span /></Card>)}</div> : viewMode === "list" ? <div className="workbench-layout">
      <Card className="workbench-task-list"><div className="panel-heading"><div><p className="section-kicker">TASK ACTIVITY</p><h3>All tasks</h3></div><span className="panel-count">{tasks.length}</span></div>{tasks.length === 0 ? <EmptyState title="No tasks yet" description="Plan a local task from Chat or Coding Workspace to review it here." /> : <div className="workbench-task-groups">{groups.map((group) => { const items = tasks.filter((task) => task.status === group.key); return items.length ? <section className="workbench-task-group" key={group.key}><div className="workbench-task-group-heading"><span>{group.label}</span><b>{items.length}</b></div>{items.map((task) => <button className={`workbench-task-row ${selectedTaskId === task.id ? "is-selected" : ""}`} id={`workbench-task-${task.id}`} key={task.id} onClick={() => setSelectedTaskId(task.id)} onKeyDown={(event) => handleTaskKeyDown(event, task.id)} aria-keyshortcuts="ArrowUp ArrowDown Home End" type="button"><span><strong>{task.title}</strong><small>{task.role} · {timeLabel(task.updatedAt)}</small></span><StatusBadge tone={toneFor(task.status)}>{group.label}</StatusBadge></button>)}</section> : null; })}</div>}</Card>
      <Card className="workbench-task-detail">{selectedTask ? <><div className="workbench-detail-heading"><div><p className="section-kicker">SELECTED TASK</p><h3>{selectedTask.title}</h3><p>{selectedTask.role} · {timeLabel(selectedTask.updatedAt)}</p></div><StatusBadge tone={toneFor(selectedTask.status)}>{groups.find((group) => group.key === selectedTask.status)?.label ?? selectedTask.status}</StatusBadge></div>{selectedTask.projectId && <p className="task-dependencies">Bounded read-only context from {selectedTaskProject?.name ?? (selectedProject?.id === selectedTask.projectId ? selectedProject.name : "an unavailable project")}</p>}{selectedTask.dependsOn.length > 0 && <p className="task-dependencies">Depends on {selectedTask.dependsOn.length} task{selectedTask.dependsOn.length === 1 ? "" : "s"}</p>}{selectedTask.resultSummary && <p className="workbench-result">{selectedTask.resultSummary}</p>}<TaskDetails task={selectedTask} openByDefault />{taskActions(selectedTask)}</> : <EmptyState title="Select a task" description="Choose a task from the activity list to review its brief, status, and available actions." />}</Card>
    </div> : <div className="workbench-board">{groups.map((group) => {
      const items = tasks.filter((task) => task.status === group.key);
      return <Card className="task-column" key={group.key}><div className="task-column-heading"><span>{group.label}</span><b>{items.length}</b></div>{items.length === 0 ? <p className="task-column-empty">No {group.label.toLowerCase()} tasks</p> : items.map((task) => <article className="task-card" key={task.id}><strong>{task.title}</strong><span>{task.role}</span>{task.dependsOn.length > 0 && <small className="task-dependencies">Depends on {task.dependsOn.length} task{task.dependsOn.length === 1 ? "" : "s"}</small>}{task.resultSummary && <p>{task.resultSummary}</p>}<small className="task-updated">{timeLabel(task.updatedAt)}</small><StatusBadge tone={toneFor(task.status)}>{group.label}</StatusBadge><TaskDetails task={task} />{taskActions(task)}</article>)}</Card>;
    })}</div>}
    <Card className="scheduled-card"><div className="panel-heading"><div><p className="section-kicker">SCHEDULED RUNS</p><h3>Automations</h3></div><span className="panel-count">{scheduled.length}</span></div><form className="schedule-form" onSubmit={async (event) => { event.preventDefault(); if (!schedulePrompt.trim()) return; const saved = await onSchedule({ prompt: schedulePrompt.trim(), dueAt: new Date(scheduleWhen).toISOString(), repeatEveryMinutes: scheduleRepeat ? Number(scheduleRepeat) : null, workspaceMode: "chat" }); if (saved) { setSchedulePrompt(""); setScheduleRepeat(""); } }}><label>Prompt<input value={schedulePrompt} onChange={(event) => setSchedulePrompt(event.target.value)} placeholder="Review new project activity" /></label><label>Run at<input type="datetime-local" value={scheduleWhen} onChange={(event) => setScheduleWhen(event.target.value)} /></label><label>Repeat<select value={scheduleRepeat} onChange={(event) => setScheduleRepeat(event.target.value)}><option value="">Once</option><option value="60">Every hour</option><option value="1440">Every day</option><option value="10080">Every week</option></select></label><Button variant="primary" type="submit" disabled={busyTask === "schedule" || !schedulePrompt.trim()}>{busyTask === "schedule" ? "Scheduling…" : "Schedule"}</Button></form>{scheduled.length === 0 ? <EmptyState title="No scheduled runs" description="Create a local schedule above. Scheduling records the intent; execution remains explicit until an executor is enabled." /> : scheduled.map((task) => <div className="scheduled-row" key={task.id}><div><strong>{task.prompt}</strong><span>{new Date(task.dueAt).toLocaleString()} · {task.status}{task.repeatEveryMinutes ? ` · every ${task.repeatEveryMinutes}m` : ""}</span></div><div className="task-card-actions">{task.status === "scheduled" && <Button variant="ghost" disabled={busyTask === task.id} onClick={() => void onScheduledTransition(task.id, "pause")}>Pause</Button>}{task.status === "paused" && <Button variant="ghost" disabled={busyTask === task.id} onClick={() => void onScheduledTransition(task.id, "resume")}>Resume</Button>}{task.status === "scheduled" && <Button variant="secondary" disabled={busyTask === task.id} onClick={() => void onScheduledTransition(task.id, "runNow")}>Run now</Button>}{(task.status === "scheduled" || task.status === "paused") && <Button variant="ghost" disabled={busyTask === task.id} onClick={() => void onScheduledTransition(task.id, "cancel")}>Cancel</Button>}</div></div>)}</Card>
  </div>;
}
