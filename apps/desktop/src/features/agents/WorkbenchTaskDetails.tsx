import type { DesktopTask } from "../../../shared/contracts";
import { Button, StatusBadge } from "../../components/ui/Primitives";
import { labelFor, nextStepFor, timeLabel, toneFor, type WorkbenchTaskTransition } from "./workbenchPresentation";

export function TaskDetails({ task, openByDefault = false }: { task: DesktopTask; openByDefault?: boolean }) {
  return <details className="task-details" open={openByDefault || undefined}>
    <summary>View task details</summary>
    <dl>
      <div><dt>Task ID</dt><dd>{task.id}</dd></div>
      <div><dt>Task brief</dt><dd className="task-instruction">{task.instruction}</dd></div>
      <div><dt>Role</dt><dd>{task.role}</dd></div>
      <div><dt>Dependencies</dt><dd>{task.dependsOn.length ? task.dependsOn.join(", ") : "None"}</dd></div>
      <div><dt>Result</dt><dd>{task.resultSummary || "No result recorded yet."}</dd></div>
    </dl>
    {task.status === "awaiting_approval" && <p className="task-capability-note">Approval controls are not exposed by the current host, so this task remains informational.</p>}
    {task.status === "failed" && <p className="task-capability-note">Retry is only supported for blocked tasks in the current host. Review the result and create a new plan if needed.</p>}
  </details>;
}

export function TaskActions({ task, busyTask, onTransition }: { task: DesktopTask; busyTask: string | null; onTransition: WorkbenchTaskTransition }) {
  return <div className="task-card-actions">
    {task.status === "proposed" && <Button variant="primary" disabled={busyTask === task.id} onClick={() => onTransition(task.id, "queue")}>Queue</Button>}
    {(task.status === "proposed" || task.status === "queued") && <Button variant="ghost" disabled={busyTask === task.id} onClick={() => onTransition(task.id, "cancel")}>Cancel</Button>}
    {task.status === "blocked" && <Button variant="secondary" disabled={busyTask === task.id} onClick={() => onTransition(task.id, "retry")}>Retry</Button>}
  </div>;
}

export function WorkbenchTaskDetail({ task, busyTask, onTransition, onNavigate }: { task: DesktopTask; busyTask: string | null; onTransition: WorkbenchTaskTransition; onNavigate: (route: "chat" | "coding") => void }) {
  const nextStep = nextStepFor(task.status);
  return <>
    <div className="workbench-detail-heading"><div><p className="section-kicker">SELECTED TASK</p><h3>{task.title}</h3><p>{task.role} · {timeLabel(task.updatedAt)}</p></div><StatusBadge tone={toneFor(task.status)}>{labelFor(task.status)}</StatusBadge></div>
    {task.dependsOn.length > 0 && <p className="task-dependencies">Depends on {task.dependsOn.length} task{task.dependsOn.length === 1 ? "" : "s"}</p>}
    {task.resultSummary && <p className="workbench-result">{task.resultSummary}</p>}
    <div className={`workbench-next-step workbench-next-step-${toneFor(task.status)}`} aria-label="Next task step"><span className="section-kicker">NEXT STEP</span><strong>{nextStep.label}</strong><p>{nextStep.description}</p></div>
    <TaskDetails task={task} openByDefault />
    {task.status === "failed" && <div className="workbench-recovery"><span className="section-kicker">CONTINUE LOCALLY</span><p>Start a fresh plan with the failed brief as context. The host does not expose failed-task retry.</p><div className="workbench-recovery-actions"><Button variant="secondary" onClick={() => onNavigate("chat")}>Re-plan in Chat</Button><Button variant="ghost" onClick={() => onNavigate("coding")}>Open Coding</Button></div></div>}
    <TaskActions task={task} busyTask={busyTask} onTransition={onTransition} />
  </>;
}
