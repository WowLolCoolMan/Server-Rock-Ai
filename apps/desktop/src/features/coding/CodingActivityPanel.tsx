import type { DesktopTask } from "../../../shared/contracts";
import { Button, Card, EmptyState, StatusBadge } from "../../components/ui/Primitives";

function toneFor(status: string) {
  if (status === "failed") return "danger" as const;
  if (status === "blocked" || status === "awaiting_approval") return "warning" as const;
  if (status === "completed") return "success" as const;
  return "neutral" as const;
}

function labelFor(status: string) {
  return status.replaceAll("_", " ");
}

function updatedLabel(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.valueOf()) ? "Updated time unavailable" : `Updated ${date.toLocaleString()}`;
}

export function CodingActivityPanel({ tasks, onOpenWorkbench }: { tasks: DesktopTask[]; onOpenWorkbench: () => void }) {
  const recentTasks = [...tasks].sort((left, right) => Date.parse(right.updatedAt) - Date.parse(left.updatedAt)).slice(0, 4);
  const activeCount = tasks.filter((task) => ["queued", "running"].includes(task.status)).length;
  const attentionCount = tasks.filter((task) => ["blocked", "failed", "awaiting_approval"].includes(task.status)).length;

  return <Card className="coding-activity-panel">
    <div className="coding-activity-heading"><div><p className="section-kicker">ACTIVITY</p><h3>Local agent</h3><p>Task state stays visible while you work through files.</p></div><span className="coding-activity-count">{activeCount} active</span></div>
    <div className="coding-activity-summary"><span><strong>{activeCount}</strong> active</span><span><strong>{attentionCount}</strong> needs attention</span></div>
    {recentTasks.length === 0 ? <EmptyState title="No task activity" description="Plan a local task here or from Chat. It will appear as a reviewable item in the Activity workspace." action={<Button variant="secondary" onClick={onOpenWorkbench}>Open activity</Button>} /> : <div className="coding-activity-list" aria-label="Recent local task activity">{recentTasks.map((task) => <article className="coding-activity-item" key={task.id}><div className="coding-activity-item-heading"><strong>{task.title}</strong><StatusBadge tone={toneFor(task.status)}>{labelFor(task.status)}</StatusBadge></div><small>{task.role} · {updatedLabel(task.updatedAt)}</small>{task.resultSummary && <p>{task.resultSummary}</p>}</article>)}</div>}
    {recentTasks.length > 0 && <Button variant="ghost" className="coding-activity-footer" onClick={onOpenWorkbench}>Review all activity <span aria-hidden="true">→</span></Button>}
  </Card>;
}
