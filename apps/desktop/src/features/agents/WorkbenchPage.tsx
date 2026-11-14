import { useEffect, useMemo, useState, type KeyboardEvent } from "react";
import type { DesktopScheduledTask, DesktopTask, ScheduledTaskDraft } from "../../../shared/contracts";
import { Button, Card, EmptyState, PageHeader } from "../../components/ui/Primitives";
import { ScheduledRunsPanel } from "./ScheduledRunsPanel";
import { WorkbenchTaskBoard, WorkbenchTaskList } from "./WorkbenchTaskViews";
import { WorkbenchTaskDetail } from "./WorkbenchTaskDetails";
import { workbenchGroups, type WorkbenchTaskTransition } from "./workbenchPresentation";

const VIEW_MODE_KEY = "skai.workbench.viewMode";
const SELECTED_TASK_KEY = "skai.workbench.selectedTask";

type WorkbenchPageProps = {
  tasks: DesktopTask[];
  scheduled: DesktopScheduledTask[];
  busyTask: string | null;
  loading: boolean;
  error: string | null;
  onRetry: () => void;
  onNavigate: (route: "chat" | "coding") => void;
  onTransition: WorkbenchTaskTransition;
  onSchedule: (draft: ScheduledTaskDraft) => Promise<boolean>;
  onScheduledTransition: (id: string, operation: "pause" | "resume" | "cancel" | "runNow") => Promise<boolean>;
};

type TaskFilter = "all" | "attention" | "active" | "completed";

export function WorkbenchPage({ tasks, scheduled, busyTask, loading, error, onRetry, onNavigate, onTransition, onSchedule, onScheduledTransition }: WorkbenchPageProps) {
  const [viewMode, setViewMode] = useState<"list" | "board">(() => localStorage.getItem(VIEW_MODE_KEY) === "board" ? "board" : "list");
  const [selectedTaskId, setSelectedTaskId] = useState<string | null>(() => localStorage.getItem(SELECTED_TASK_KEY));
  const [taskFilter, setTaskFilter] = useState<TaskFilter>("all");
  const visibleTasks = useMemo(() => tasks.filter((task) => taskFilter === "all" || taskFilter === "attention" && ["awaiting_approval", "blocked", "failed"].includes(task.status) || taskFilter === "active" && ["proposed", "queued", "running"].includes(task.status) || taskFilter === "completed" && task.status === "completed"), [taskFilter, tasks]);
  const selectedTask = visibleTasks.find((task) => task.id === selectedTaskId) ?? null;
  const orderedTaskIds = workbenchGroups.flatMap((group) => visibleTasks.filter((task) => task.status === group.key).map((task) => task.id));
  const taskCounts = useMemo(() => ({ active: tasks.filter((task) => ["queued", "running"].includes(task.status)).length, attention: tasks.filter((task) => ["awaiting_approval", "blocked", "failed"].includes(task.status)).length, completed: tasks.filter((task) => task.status === "completed").length }), [tasks]);
  const filterOptions: Array<{ id: TaskFilter; label: string; count: number }> = [{ id: "all", label: "All", count: tasks.length }, { id: "active", label: "Active", count: taskCounts.active }, { id: "attention", label: "Needs attention", count: taskCounts.attention }, { id: "completed", label: "Completed", count: taskCounts.completed }];

  useEffect(() => { if (selectedTaskId && visibleTasks.some((task) => task.id === selectedTaskId)) return; setSelectedTaskId(visibleTasks[0]?.id ?? null); }, [selectedTaskId, visibleTasks]);
  useEffect(() => { localStorage.setItem(VIEW_MODE_KEY, viewMode); }, [viewMode]);
  useEffect(() => { if (selectedTaskId) localStorage.setItem(SELECTED_TASK_KEY, selectedTaskId); else localStorage.removeItem(SELECTED_TASK_KEY); }, [selectedTaskId]);

  const handleTaskKeyDown = (event: KeyboardEvent<HTMLButtonElement>, taskId: string) => {
    const currentIndex = orderedTaskIds.indexOf(taskId);
    if (currentIndex < 0) return;
    const nextIndex = event.key === "ArrowDown" ? Math.min(currentIndex + 1, orderedTaskIds.length - 1) : event.key === "ArrowUp" ? Math.max(currentIndex - 1, 0) : event.key === "Home" ? 0 : event.key === "End" ? orderedTaskIds.length - 1 : -1;
    if (nextIndex < 0 || nextIndex === currentIndex) return;
    event.preventDefault();
    const nextId = orderedTaskIds[nextIndex];
    setSelectedTaskId(nextId);
    document.getElementById(`workbench-task-${nextId}`)?.focus();
  };

  return <div className="feature-page workbench-page">
    <PageHeader eyebrow="AGENT WORKBENCH" title="Plan, execute, verify" description="A clear task lifecycle for work that needs progress, approvals, and recovery." actions={<div className="workbench-view-actions"><Button variant={viewMode === "list" ? "secondary" : "ghost"} aria-pressed={viewMode === "list"} onClick={() => setViewMode("list")}>List view</Button><Button variant={viewMode === "board" ? "secondary" : "ghost"} aria-pressed={viewMode === "board"} onClick={() => setViewMode("board")}>Board view</Button></div>} />
    <div className="workbench-overview" aria-label="Task activity summary"><Card className="workbench-stat"><span>ACTIVE</span><strong>{taskCounts.active}</strong><small>Queued or running</small></Card><Card className="workbench-stat"><span>NEEDS ATTENTION</span><strong>{taskCounts.attention}</strong><small>Approval, blocked, or failed</small></Card><Card className="workbench-stat"><span>COMPLETED</span><strong>{taskCounts.completed}</strong><small>Finished task history</small></Card><Card className="workbench-stat"><span>SCHEDULED</span><strong>{scheduled.length}</strong><small>Local automation records</small></Card></div>
    <div className="workbench-filter-bar" role="toolbar" aria-label="Filter task activity"><span className="section-kicker">SHOW</span><div className="workbench-filters">{filterOptions.map((option) => <Button key={option.id} variant={taskFilter === option.id ? "secondary" : "ghost"} aria-pressed={taskFilter === option.id} onClick={() => setTaskFilter(option.id)}>{option.label}<span className="workbench-filter-count">{option.count}</span></Button>)}</div></div>
    {error ? <div className="surface-error workbench-error" role="alert"><strong>Agent workbench is unavailable</strong><p>{error}</p><Button variant="secondary" onClick={onRetry}>Retry</Button></div> : loading ? <div className="workbench-loading" aria-label="Loading agent workbench">{[1, 2, 3].map((item) => <Card className="loading-card" key={item}><span /><span /><span /></Card>)}</div> : viewMode === "list" ? <div className="workbench-layout"><WorkbenchTaskList tasks={visibleTasks} totalTasks={tasks.length} title={taskFilter === "all" ? "All tasks" : filterOptions.find((option) => option.id === taskFilter)?.label ?? "Tasks"} selectedTaskId={selectedTaskId} orderedTaskIds={orderedTaskIds} onSelect={setSelectedTaskId} onKeyDown={handleTaskKeyDown} onClearFilter={() => setTaskFilter("all")} /><Card className="workbench-task-detail">{selectedTask ? <WorkbenchTaskDetail task={selectedTask} busyTask={busyTask} onNavigate={onNavigate} onTransition={onTransition} /> : <EmptyState title="Select a task" description="Choose a task from the activity list to review its brief, status, and available actions." />}</Card></div> : <WorkbenchTaskBoard tasks={visibleTasks} busyTask={busyTask} onTransition={onTransition} />}
    <ScheduledRunsPanel scheduled={scheduled} busyTask={busyTask} onSchedule={onSchedule} onTransition={onScheduledTransition} />
  </div>;
}
