import { useEffect, useState } from "react";
import type { DesktopProject } from "../../../shared/contracts";
import { Button, Card, EmptyState } from "../../components/ui/Primitives";

type TerminalPanelProps = {
  projects: DesktopProject[];
  initialProjectId?: string | null;
  busy: boolean;
  output: string | null;
  exitCode: number | null;
  compact?: boolean;
  onRun: (projectId: string, command: string, cwd: string) => void;
  sessionId?: string | null;
  screen?: string | null;
  onStartSession?: (projectId: string, cwd: string) => Promise<boolean>;
  onWriteSession?: (text: string) => Promise<boolean>;
  onRefreshSession?: () => Promise<boolean>;
  onCloseSession?: () => Promise<boolean>;
  onOpenCoding?: () => void;
};

export function TerminalPanel({ projects, initialProjectId, busy, output, exitCode, sessionId, screen, compact = false, onRun, onStartSession, onWriteSession, onRefreshSession, onCloseSession, onOpenCoding }: TerminalPanelProps) {
  const [projectId, setProjectId] = useState(initialProjectId ?? projects[0]?.id ?? "");
  const [cwd, setCwd] = useState(".");
  const [command, setCommand] = useState("");
  const [sessionInput, setSessionInput] = useState("");

  useEffect(() => {
    if (initialProjectId) setProjectId(initialProjectId);
    else if (!projectId && projects[0]?.id) setProjectId(projects[0].id);
  }, [initialProjectId, projectId, projects]);

  const run = () => {
    if (!command.trim() || !projectId || busy) return;
    onRun(projectId, command.trim(), cwd.trim() || ".");
  };
  const startSession = () => { if (projectId && onStartSession) void onStartSession(projectId, cwd.trim() || "."); };
  const sendSessionInput = async () => { if (!sessionInput || !sessionId || !onWriteSession) return; if (await onWriteSession(sessionInput + (sessionInput.endsWith("\n") ? "" : "\r\n"))) setSessionInput(""); };

  return <Card className={compact ? "terminal-card terminal-panel" : "terminal-card"}>
    {compact && <div className="terminal-panel-heading">
      <div><p className="section-kicker">PROJECT TERMINAL</p><h3>{compact ? "Run beside your files" : "Run with intent"}</h3></div>
      <span className="terminal-safety-note">Bounded to this project</span>
    </div>}
    {projects.length === 0 ? <EmptyState title="Select a project first" description="Terminal commands are bounded to a project. Open Coding Workspace to connect a folder before running anything." action={onOpenCoding ? <Button variant="secondary" onClick={onOpenCoding}>Open Coding Workspace</Button> : undefined} /> : <>
    <div className="terminal-controls">
      <label>Project<select value={projectId} onChange={(event) => setProjectId(event.target.value)}><option value="">Select a project</option>{projects.map((project) => <option key={project.id} value={project.id}>{project.name}</option>)}</select></label>
      <label>Working directory<input value={cwd} onChange={(event) => setCwd(event.target.value)} aria-label="Working directory" /></label>
    </div>
    <div className="terminal-command-row">
      <input value={command} onChange={(event) => setCommand(event.target.value)} onKeyDown={(event) => { if (event.key === "Enter") run(); }} placeholder="git status" aria-label="Terminal command" />
      <Button variant="primary" disabled={busy || !command.trim() || !projectId} onClick={run}>{busy ? "Running…" : "Run command"}</Button>
    </div>
    {output !== null && <pre className={exitCode === 0 ? "terminal-result" : "terminal-result terminal-failure"}><span>exit {exitCode}</span>{output}</pre>}</>}
    {projects.length > 0 && onStartSession && <div className="terminal-session"><div className="terminal-session-heading"><div><p className="section-kicker">PERSISTENT PTY</p><h3>{sessionId ? "Session connected" : "Keep a shell open"}</h3></div><span className="terminal-safety-note">{sessionId ? "Local session" : "Not started"}</span></div>{sessionId ? <><pre className="terminal-result terminal-screen" aria-live="polite">{screen || "Starting shell…"}</pre><div className="terminal-command-row"><input value={sessionInput} onChange={(event) => setSessionInput(event.target.value)} onKeyDown={(event) => { if (event.key === "Enter") void sendSessionInput(); }} placeholder="Type shell input…" aria-label="Persistent terminal input" /><Button variant="secondary" disabled={!sessionInput} onClick={() => void sendSessionInput()}>Send</Button><Button variant="ghost" onClick={() => void onRefreshSession?.()}>Refresh</Button><Button variant="danger" onClick={() => void onCloseSession?.()}>Close</Button></div></> : <div className="form-footer"><span>Interactive input persists between commands while this workspace is open.</span><Button variant="secondary" disabled={busy || !projectId} onClick={startSession}>Start persistent session</Button></div>}</div>}
  </Card>;
}
