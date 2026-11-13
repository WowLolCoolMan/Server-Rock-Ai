import { useEffect, useState } from "react";
import type { DesktopProject } from "../../../shared/contracts";
import { Button, Card } from "../../components/ui/Primitives";

type TerminalPanelProps = {
  projects: DesktopProject[];
  initialProjectId?: string | null;
  busy: boolean;
  output: string | null;
  exitCode: number | null;
  compact?: boolean;
  onRun: (projectId: string, command: string, cwd: string) => void;
};

export function TerminalPanel({ projects, initialProjectId, busy, output, exitCode, compact = false, onRun }: TerminalPanelProps) {
  const [projectId, setProjectId] = useState(initialProjectId ?? projects[0]?.id ?? "");
  const [cwd, setCwd] = useState(".");
  const [command, setCommand] = useState("");

  useEffect(() => {
    if (initialProjectId) setProjectId(initialProjectId);
    else if (!projectId && projects[0]?.id) setProjectId(projects[0].id);
  }, [initialProjectId, projectId, projects]);

  const run = () => {
    if (!command.trim() || !projectId || busy) return;
    onRun(projectId, command.trim(), cwd.trim() || ".");
  };

  return <Card className={compact ? "terminal-card terminal-panel" : "terminal-card"}>
    <div className="terminal-panel-heading">
      <div><p className="section-kicker">PROJECT TERMINAL</p><h3>{compact ? "Run beside your files" : "Run with intent"}</h3></div>
      <span className="terminal-safety-note">Bounded to this project</span>
    </div>
    <div className="terminal-controls">
      <label>Project<select value={projectId} onChange={(event) => setProjectId(event.target.value)}><option value="">Select a project</option>{projects.map((project) => <option key={project.id} value={project.id}>{project.name}</option>)}</select></label>
      <label>Working directory<input value={cwd} onChange={(event) => setCwd(event.target.value)} aria-label="Working directory" /></label>
    </div>
    <div className="terminal-command-row">
      <input value={command} onChange={(event) => setCommand(event.target.value)} onKeyDown={(event) => { if (event.key === "Enter") run(); }} placeholder="git status" aria-label="Terminal command" />
      <Button variant="primary" disabled={busy || !command.trim() || !projectId} onClick={run}>{busy ? "Running…" : "Run command"}</Button>
    </div>
    {output !== null && <pre className={exitCode === 0 ? "terminal-result" : "terminal-result terminal-failure"}><span>exit {exitCode}</span>{output}</pre>}
  </Card>;
}
