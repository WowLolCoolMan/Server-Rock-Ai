import { useState } from "react";
import type { DesktopScheduledTask, ScheduledTaskDraft } from "../../../shared/contracts";
import { Button, Card, EmptyState } from "../../components/ui/Primitives";

export function ScheduledRunsPanel({ scheduled, busyTask, onSchedule, onTransition }: { scheduled: DesktopScheduledTask[]; busyTask: string | null; onSchedule: (draft: ScheduledTaskDraft) => Promise<boolean>; onTransition: (id: string, operation: "pause" | "resume" | "cancel" | "runNow") => Promise<boolean> }) {
  const [schedulePrompt, setSchedulePrompt] = useState("");
  const [scheduleWhen, setScheduleWhen] = useState(() => new Date(Date.now() + 60 * 60 * 1000).toISOString().slice(0, 16));
  const [scheduleRepeat, setScheduleRepeat] = useState("");

  return <Card className="scheduled-card"><div className="panel-heading"><div><p className="section-kicker">SCHEDULED RUNS</p><h3>Automations</h3></div><span className="panel-count">{scheduled.length}</span></div>
    <form className="schedule-form" onSubmit={async (event) => { event.preventDefault(); if (!schedulePrompt.trim()) return; const saved = await onSchedule({ prompt: schedulePrompt.trim(), dueAt: new Date(scheduleWhen).toISOString(), repeatEveryMinutes: scheduleRepeat ? Number(scheduleRepeat) : null, workspaceMode: "chat" }); if (saved) { setSchedulePrompt(""); setScheduleRepeat(""); } }}>
      <label>Prompt<input value={schedulePrompt} onChange={(event) => setSchedulePrompt(event.target.value)} placeholder="Review new project activity" /></label>
      <label>Run at<input type="datetime-local" value={scheduleWhen} onChange={(event) => setScheduleWhen(event.target.value)} /></label>
      <label>Repeat<select value={scheduleRepeat} onChange={(event) => setScheduleRepeat(event.target.value)}><option value="">Once</option><option value="60">Every hour</option><option value="1440">Every day</option><option value="10080">Every week</option></select></label>
      <Button variant="primary" type="submit" disabled={busyTask === "schedule" || !schedulePrompt.trim()}>{busyTask === "schedule" ? "Scheduling…" : "Schedule"}</Button>
    </form>
    {scheduled.length === 0 ? <EmptyState title="No scheduled runs" description="Create a local schedule above. Scheduling records the intent; execution remains explicit until an executor is enabled." /> : scheduled.map((task) => <div className="scheduled-row" key={task.id}><div><strong>{task.prompt}</strong><span>{new Date(task.dueAt).toLocaleString()} · {task.status}{task.repeatEveryMinutes ? ` · every ${task.repeatEveryMinutes}m` : ""}</span>{task.outcome && <small>{task.outcome}</small>}</div><div className="task-card-actions">{task.status === "scheduled" && <Button variant="ghost" disabled={busyTask === task.id} onClick={() => void onTransition(task.id, "pause")}>Pause</Button>}{task.status === "paused" && <Button variant="ghost" disabled={busyTask === task.id} onClick={() => void onTransition(task.id, "resume")}>Resume</Button>}{task.status === "scheduled" && <Button variant="secondary" title="Mark this local schedule due; background execution is not enabled." aria-label={`Mark ${task.prompt} due`} disabled={busyTask === task.id} onClick={() => void onTransition(task.id, "runNow")}>Mark due</Button>}{(task.status === "scheduled" || task.status === "paused") && <Button variant="ghost" disabled={busyTask === task.id} onClick={() => void onTransition(task.id, "cancel")}>Cancel</Button>}</div></div>)}
  </Card>;
}
