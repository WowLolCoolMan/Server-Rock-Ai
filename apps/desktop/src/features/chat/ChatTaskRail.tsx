import { useState } from "react";
import type { TaskEnvironment } from "../../../shared/contracts";
import { Button, Card } from "../../components/ui/Primitives";
import { EnvironmentPicker } from "../../components/ui/EnvironmentPicker";

type ChatTaskRailProps = {
  taskBusy: string | null;
  onNavigate: (route: "coding" | "images" | "marketplace" | "workbench") => void;
  onPlanTask: (title: string, instruction: string, environment: TaskEnvironment) => Promise<boolean>;
};

export function ChatTaskRail({ taskBusy, onNavigate, onPlanTask }: ChatTaskRailProps) {
  const [taskTitle, setTaskTitle] = useState("");
  const [taskInstruction, setTaskInstruction] = useState("");
  const [taskEnvironment, setTaskEnvironment] = useState<TaskEnvironment>("local");
  const planTask = async () => {
    if (!taskTitle.trim() || !taskInstruction.trim() || taskBusy === "planning") return;
    const planned = await onPlanTask(taskTitle.trim(), taskInstruction.trim(), taskEnvironment);
    if (planned) { setTaskTitle(""); setTaskInstruction(""); onNavigate("workbench"); }
  };
  return <aside className="chat-rail" aria-label="Chat workspace tools">
    <Card className="rail-card"><p className="section-kicker">QUICK START</p>
      <button onClick={() => onNavigate("coding")} type="button"><strong>Build in a project</strong><span>Open files, terminal, and agent tasks</span></button>
      <button onClick={() => onNavigate("images")} type="button"><strong>Create an image</strong><span>Use a local image workflow</span></button>
      <button onClick={() => onNavigate("workbench")} type="button"><strong>Run an agent</strong><span>Plan, execute, and verify work</span></button>
      <button onClick={() => onNavigate("marketplace")} type="button"><strong>Find a model or tool</strong><span>Browse voices, image models, skills, and runtimes</span></button>
    </Card>
    <form className="task-composer chat-task-composer" onSubmit={(event) => { event.preventDefault(); void planTask(); }}>
      <div className="task-composer-heading"><div><p className="section-kicker">TASK BRIEF</p><h3>Plan local work</h3><p>Prepare a task for review before any tools run.</p></div><span className="task-composer-badge">Local only</span></div>
      <div className="task-composer-fields"><label>Task title<input value={taskTitle} onChange={(event) => setTaskTitle(event.target.value)} placeholder="Triage the next change" /></label><label>What should happen?<textarea value={taskInstruction} onChange={(event) => setTaskInstruction(event.target.value)} placeholder="Describe the outcome and how you will verify it." rows={4} /></label></div>
      <EnvironmentPicker value={taskEnvironment} onChange={setTaskEnvironment} />
      <div className="task-composer-footer"><span>Creates a proposed task. Nothing runs yet.</span><Button variant="primary" type="submit" disabled={taskBusy === "planning" || !taskTitle.trim() || !taskInstruction.trim()}>{taskBusy === "planning" ? "Planning…" : "Add to task plan"}</Button></div>
    </form>
  </aside>;
}
