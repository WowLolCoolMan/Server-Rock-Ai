import type { KeyboardEvent } from "react";
import type { DesktopTask } from "../../../shared/contracts";
import { Button, Card, EmptyState, StatusBadge } from "../../components/ui/Primitives";
import { labelFor, timeLabel, toneFor, workbenchGroups, type WorkbenchTaskTransition } from "./workbenchPresentation";
import { TaskActions, TaskDetails } from "./WorkbenchTaskDetails";

type TaskSelectionProps = {
  tasks: DesktopTask[];
  totalTasks: number;
  title: string;
  selectedTaskId: string | null;
  orderedTaskIds: string[];
  onSelect: (id: string) => void;
  onKeyDown: (event: KeyboardEvent<HTMLButtonElement>, id: string) => void;
};

export function WorkbenchTaskList({ tasks, totalTasks, title, selectedTaskId, orderedTaskIds, onSelect, onKeyDown, onClearFilter }: TaskSelectionProps & { onClearFilter: () => void }) {
  return <Card className={`workbench-task-list ${tasks.length === 0 ? "is-empty" : ""}`}>
    <div className="panel-heading"><div><p className="section-kicker">TASK ACTIVITY</p><h3>{title}</h3></div><span className="panel-count">{tasks.length}</span></div>
    {totalTasks === 0 ? <EmptyState title="No tasks yet" description="Plan a local task from Chat or Coding Workspace to review it here." /> : tasks.length === 0 ? <EmptyState title="No matching tasks" description="Try another activity filter to see more of the task history." action={<Button variant="secondary" onClick={onClearFilter}>Show all tasks</Button>} /> : <div className="workbench-task-groups">{workbenchGroups.map((group) => {
      const items = tasks.filter((task) => task.status === group.key);
      return items.length ? <section className="workbench-task-group" key={group.key}><div className="workbench-task-group-heading"><span>{group.label}</span><b>{items.length}</b></div>{items.map((task) => <button className={`workbench-task-row ${selectedTaskId === task.id ? "is-selected" : ""}`} id={`workbench-task-${task.id}`} key={task.id} onClick={() => onSelect(task.id)} onKeyDown={(event) => onKeyDown(event, task.id)} aria-keyshortcuts="ArrowUp ArrowDown Home End" type="button"><span><strong>{task.title}</strong><small>{task.role} · {timeLabel(task.updatedAt)}</small></span><StatusBadge tone={toneFor(task.status)}>{group.label}</StatusBadge></button>)}</section> : null;
    })}</div>}
  </Card>;
}

export function WorkbenchTaskBoard({ tasks, busyTask, onTransition, onExecute }: { tasks: DesktopTask[]; busyTask: string | null; onTransition: WorkbenchTaskTransition; onExecute?: (id: string) => void }) {
  return <div className="workbench-board">{workbenchGroups.map((group) => {
    const items = tasks.filter((task) => task.status === group.key);
    return <Card className="task-column" key={group.key}><div className="task-column-heading"><span>{group.label}</span><b>{items.length}</b></div>{items.length === 0 ? <p className="task-column-empty">No {group.label.toLowerCase()} tasks</p> : items.map((task) => <article className="task-card" key={task.id}><strong>{task.title}</strong><span>{task.role}</span>{task.dependsOn.length > 0 && <small className="task-dependencies">Depends on {task.dependsOn.length} task{task.dependsOn.length === 1 ? "" : "s"}</small>}{task.resultSummary && <p>{task.resultSummary}</p>}<small className="task-updated">{timeLabel(task.updatedAt)}</small><StatusBadge tone={toneFor(task.status)}>{labelFor(task.status)}</StatusBadge><TaskDetails task={task} /><TaskActions task={task} busyTask={busyTask} onTransition={onTransition} onExecute={onExecute} /></article>)}</Card>;
  })}</div>;
}
