import type { DesktopScheduledTask, DesktopTask } from "../../../shared/contracts";
import { Button, Card, PageHeader, StatusBadge } from "../../components/ui/Primitives";

export function UsagePage({ tasks, scheduled, onNavigate }: { tasks: DesktopTask[]; scheduled: DesktopScheduledTask[]; onNavigate: (route: "workbench") => void }) {
  const completedTasks = tasks.filter((task) => task.status === "completed").length;
  const activeSchedules = scheduled.filter((task) => task.status === "scheduled").length;
  return <div className="feature-page usage-page">
    <PageHeader eyebrow="USAGE / ACTIVITY" title="See what Skai knows" description="Local activity records are available here. Provider billing, token counts, and GPU telemetry stay unavailable until a runtime reports them explicitly." actions={<StatusBadge tone="warning">Telemetry partial</StatusBadge>} />
    <div className="usage-hero"><div><p className="section-kicker">LOCAL WORKSPACE</p><h3>Useful signals, no invented numbers</h3><p>Skai only displays measurements returned by the connected runtime or durable local stores. Missing measurements remain visible as unavailable.</p></div><span className="usage-hero-mark">◌</span></div>
    <div className="usage-grid">
      <Card className="usage-panel"><div className="panel-heading"><div><p className="section-kicker">MEASURED LOCALLY</p><h3>Workspace records</h3></div><StatusBadge tone="success">Available</StatusBadge></div><div className="usage-stat-grid"><div><span>Completed tasks</span><strong>{completedTasks}</strong><small>Durable Agent Workbench records</small></div><div><span>Active schedules</span><strong>{activeSchedules}</strong><small>Explicit local automation records</small></div></div></Card>
      <Card className="usage-panel"><div className="panel-heading"><div><p className="section-kicker">NOT CONNECTED</p><h3>Provider measurements</h3></div><StatusBadge tone="neutral">Unavailable</StatusBadge></div><div className="usage-unavailable"><div><strong>Token usage</strong><span>No provider reports token counts to Skai yet.</span></div><div><strong>Cost</strong><span>Local-first routing has no billing feed.</span></div><div><strong>GPU / memory</strong><span>No runtime telemetry adapter is connected.</span></div></div></Card>
    </div>
    <Card className="usage-panel usage-guidance"><div><p className="section-kicker">NEXT BEST SOURCE</p><h3>Inspect work, not guesses</h3><p>Use Agent Workbench for task lifecycle, Models for runtime inventory, and Settings for provider connection state.</p></div><Button variant="secondary" onClick={() => onNavigate("workbench")}>Open Agent Workbench</Button></Card>
  </div>;
}
