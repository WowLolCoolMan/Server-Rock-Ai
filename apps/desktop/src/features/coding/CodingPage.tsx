import type { DesktopGitStatus, DesktopProject, DesktopProjectFile, TaskEnvironment } from "../../../shared/contracts";
import { useEffect, useMemo, useRef, useState } from "react";
import { Button, Card, EmptyState, PageHeader } from "../../components/ui/Primitives";
import { ModalDialog } from "../../components/ui/ModalDialog";
import { EnvironmentPicker } from "../../components/ui/EnvironmentPicker";
import { TerminalPanel } from "../terminal/TerminalPanel";

type DiffRow = { kind: "removed" | "added" | "unchanged"; line: string };

function terminalStorageKey(projectId: string | null) {
  return `skai.coding.terminalOpen.${projectId ?? "none"}`;
}

function createDiff(before: string, after: string): DiffRow[] {
  const beforeLines = before.split("\n");
  const afterLines = after.split("\n");
  let prefix = 0;
  while (prefix < beforeLines.length && prefix < afterLines.length && beforeLines[prefix] === afterLines[prefix]) prefix += 1;
  let suffix = 0;
  while (suffix < beforeLines.length - prefix && suffix < afterLines.length - prefix && beforeLines[beforeLines.length - 1 - suffix] === afterLines[afterLines.length - 1 - suffix]) suffix += 1;
  const rows: DiffRow[] = beforeLines.slice(prefix, beforeLines.length - suffix).map((line) => ({ kind: "removed", line }));
  rows.push(...afterLines.slice(prefix, afterLines.length - suffix).map((line) => ({ kind: "added" as const, line })));
  return rows.length ? rows : [{ kind: "unchanged", line: "No text changes" }];
}

type CodingPageProps = {
  projects: DesktopProject[];
  selectedProject: DesktopProject | null;
  projectFiles: DesktopProjectFile[];
  gitStatus: DesktopGitStatus | null;
  selectedFile: string | null;
  fileDraft: string;
  fileOriginal: string;
  fileSummary: string | null;
  fileBusy: boolean;
  terminalBusy: boolean;
  terminalOutput: string | null;
  terminalExitCode: number | null;
  onPickFolder: () => Promise<string | null>;
  onSaveProject: (name: string, instructions: string, folders: string[]) => Promise<boolean>;
  onOpenProject: (project: DesktopProject) => void;
  onRefreshGitStatus: () => void;
  onOpenFile: (relativePath: string) => void;
  onCloseFile: () => void;
  onDraftChange: (value: string) => void;
  onSaveFile: () => void;
  onUndoFile: () => void;
  onRunTerminal: (projectId: string, command: string, cwd: string) => void;
  onPlanTask: (title: string, instruction: string, environment: TaskEnvironment) => Promise<boolean>;
  onNavigate: (route: "terminal" | "workbench") => void;
};

export function CodingPage({ projects, selectedProject, projectFiles, gitStatus, selectedFile, fileDraft, fileOriginal, fileSummary, fileBusy, terminalBusy, terminalOutput, terminalExitCode, onPickFolder, onSaveProject, onOpenProject, onRefreshGitStatus, onOpenFile, onCloseFile, onDraftChange, onSaveFile, onUndoFile, onRunTerminal, onPlanTask, onNavigate }: CodingPageProps) {
  const [dialogOpen, setDialogOpen] = useState(false);
  const [name, setName] = useState("");
  const [instructions, setInstructions] = useState("");
  const [folders, setFolders] = useState<string[]>([]);
  const [openTabs, setOpenTabs] = useState<string[]>([]);
  const [fileQuery, setFileQuery] = useState("");
  const [terminalOpen, setTerminalOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [taskTitle, setTaskTitle] = useState("");
  const [taskInstruction, setTaskInstruction] = useState("");
  const [taskEnvironment, setTaskEnvironment] = useState<TaskEnvironment>("local");
  const [planningTask, setPlanningTask] = useState(false);
  const [panelMode, setPanelMode] = useState<"editor" | "diff">("editor");
  const hasChanges = Boolean(selectedFile && fileDraft !== fileOriginal);
  const diffRows = useMemo(() => createDiff(fileOriginal, fileDraft), [fileDraft, fileOriginal]);
  const activityItems = [
    ["Inspected project scope", selectedProject ? `${selectedProject.folderPaths.length} connected folder${selectedProject.folderPaths.length === 1 ? "" : "s"}` : "Choose a project", Boolean(selectedProject)],
    ["Loaded file context", selectedFile ? `${selectedFile}${hasChanges ? " · unsaved" : ""}` : "Select a file", Boolean(selectedFile)],
    ["Prepared terminal", terminalOpen ? (terminalBusy ? "Command running" : "Session docked") : "Session closed", terminalOpen],
    ["Review before write", hasChanges ? "Diff review required" : "No pending file edits", !hasChanges],
  ] as const;
  const activityProgress = Math.round((activityItems.filter((item) => item[2]).length / activityItems.length) * 100);
  const filteredFiles = useMemo(() => {
    const query = fileQuery.trim().toLowerCase();
    return query ? projectFiles.filter((file) => file.relativePath.toLowerCase().includes(query)) : projectFiles;
  }, [fileQuery, projectFiles]);
  const fileSearchRef = useRef<HTMLInputElement>(null);
  const projectNameRef = useRef<HTMLInputElement>(null);

  useEffect(() => { setPanelMode("editor"); }, [selectedFile, fileOriginal]);
  useEffect(() => {
    setOpenTabs([]);
    setFileQuery("");
    setTerminalOpen(selectedProject ? localStorage.getItem(terminalStorageKey(selectedProject.id)) === "true" : false);
  }, [selectedProject?.id]);
  useEffect(() => {
    if (selectedProject) localStorage.setItem(terminalStorageKey(selectedProject.id), String(terminalOpen));
  }, [selectedProject?.id, terminalOpen]);
  useEffect(() => { if (selectedFile) setOpenTabs((current) => current.includes(selectedFile) ? current : [...current, selectedFile]); }, [selectedFile]);
  useEffect(() => {
    const onShortcut = (event: KeyboardEvent) => {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "p") {
        event.preventDefault();
        fileSearchRef.current?.focus();
        fileSearchRef.current?.select();
      }
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "j" && selectedProject) {
        event.preventDefault();
        setTerminalOpen((open) => !open);
      }
    };
    window.addEventListener("keydown", onShortcut);
    return () => window.removeEventListener("keydown", onShortcut);
  }, [selectedProject]);

  const createProject = async () => {
    if (!name.trim() || saving) return;
    setSaving(true);
    const saved = await onSaveProject(name.trim(), instructions.trim(), folders);
    setSaving(false);
    if (saved) { setName(""); setInstructions(""); setFolders([]); setDialogOpen(false); }
  };

  const planTask = async () => {
    if (!selectedProject || !taskTitle.trim() || !taskInstruction.trim() || planningTask) return;
    setPlanningTask(true);
    const planned = await onPlanTask(taskTitle.trim(), `${taskInstruction.trim()}\n\nProject: ${selectedProject.name}`, taskEnvironment);
    setPlanningTask(false);
    if (planned) { setTaskTitle(""); setTaskInstruction(""); onNavigate("workbench"); }
  };

  const addFolder = async () => {
    const folder = await onPickFolder();
    if (folder && !folders.includes(folder)) setFolders((current) => [...current, folder]);
  };

  const openFile = (path: string) => {
    setOpenTabs((current) => current.includes(path) ? current : [...current, path]);
    onOpenFile(path);
  };

  const closeTab = (path: string) => {
    if (path === selectedFile && hasChanges) return;
    const index = openTabs.indexOf(path);
    const next = openTabs.filter((item) => item !== path);
    setOpenTabs(next);
    if (path === selectedFile) {
      const nextActive = next[index] ?? next[index - 1];
      if (nextActive) onOpenFile(nextActive); else onCloseFile();
    }
  };

  return <div className="feature-page coding-page">
    <PageHeader eyebrow="CODING WORKSPACE" title="Build with context" description="A local project workspace for planning tasks, reviewing edits, and running guarded commands." actions={<Button variant="primary" onClick={() => setDialogOpen(true)}>＋ New project</Button>} />
    <div className="coding-session-bar" aria-label="Coding session status"><div className="coding-session-copy"><span className="coding-session-dot" aria-hidden="true" /><strong>Local coding session</strong><span>Files stay in the folders you choose.</span></div><div className="coding-shortcuts"><span><kbd>Ctrl P</kbd> Find file</span><span><kbd>Ctrl J</kbd> Terminal</span></div></div>
    <div className="workspace-grid coding-overview-grid">
      <Card className="workspace-main coding-projects-panel"><div className="panel-heading"><div><p className="section-kicker">PROJECTS</p><h3>Continue building</h3><p className="panel-subtitle">Choose a bounded local workspace to give the agent context.</p></div><span className="panel-count">{projects.length} {projects.length === 1 ? "project" : "projects"}</span></div>{projects.length === 0 ? <EmptyState title="Create your first project" description="Connect one or more folders, then keep the task, files, and review surface together." action={<Button variant="secondary" onClick={() => setDialogOpen(true)}>Create project</Button>} /> : <div className="project-list">{projects.map((project) => <button className={`project-card ${selectedProject?.id === project.id ? "is-selected" : ""}`} key={project.id} onClick={() => onOpenProject(project)} type="button"><span className="project-icon">⌘</span><span><strong>{project.name}</strong><small>{project.folderPaths.length ? `${project.folderPaths.length} connected folder${project.folderPaths.length === 1 ? "" : "s"}` : "No folders connected"}</small></span><span className="project-arrow">→</span></button>)}</div>}</Card>
      <Card className="workspace-side coding-flow-panel"><div className="coding-flow-heading"><div><p className="section-kicker">WORKFLOW</p><h3>From task to review</h3></div><span className="coding-flow-mark" aria-hidden="true">↗</span></div><div className="flow-step"><b>01</b><span><strong>Select a project</strong><small>Keep context explicit and bounded.</small></span></div><div className="flow-step"><b>02</b><span><strong>Plan a local task</strong><small>Describe the outcome before tools run.</small></span></div><div className="flow-step"><b>03</b><span><strong>Review the diff</strong><small>Every write stays visible and reversible.</small></span></div></Card>
    </div>
    {selectedProject && <Card className="project-browser-card coding-project-browser">
      <div className="panel-heading"><div><p className="section-kicker">PROJECT FILES</p><h3>{selectedProject.name}</h3></div><div className="project-browser-actions"><span className="panel-count">{projectFiles.length}</span><Button variant={terminalOpen ? "secondary" : "ghost"} aria-pressed={terminalOpen} onClick={() => setTerminalOpen((open) => !open)}>{terminalOpen ? "Hide terminal" : "Dock terminal"}</Button><Button variant="ghost" onClick={() => onNavigate("terminal")}>Open terminal</Button><Button variant="ghost" onClick={() => onNavigate("workbench")}>Review tasks</Button></div></div>
      <div className="coding-git-status" aria-label="Git workspace status">{gitStatus ? <><div className="coding-git-status-heading"><div><span className="section-kicker">GIT REVIEW</span><strong>{gitStatus.branch}</strong><small>{gitStatus.repository} · {gitStatus.changes.length} changed {gitStatus.changes.length === 1 ? "path" : "paths"}{gitStatus.commitsAhead ? ` · ${gitStatus.commitsAhead} ahead` : ""}{gitStatus.commitsBehind ? ` · ${gitStatus.commitsBehind} behind` : ""}</small></div><div className="coding-git-status-actions"><span className={`coding-git-signal ${gitStatus.hasConflicts ? "is-warning" : gitStatus.changes.length ? "is-changed" : "is-clean"}`}>{gitStatus.hasConflicts ? "Conflicts" : gitStatus.changes.length ? "Changes" : "Clean"}</span><Button variant="ghost" onClick={onRefreshGitStatus}>Refresh</Button></div></div>{gitStatus.changes.length > 0 && <div className="coding-git-changes">{gitStatus.changes.slice(0, 6).map((change) => <span key={`${change.status}-${change.path}`} className={change.conflict ? "is-conflict" : change.protectedPath ? "is-protected" : ""}><b>{change.status}</b>{change.path}{change.originalPath ? ` ← ${change.originalPath}` : ""}</span>)}{gitStatus.changes.length > 6 && <small>+{gitStatus.changes.length - 6} more paths</small>}</div>}</> : <div className="coding-git-status-heading"><div><span className="section-kicker">GIT REVIEW</span><strong>Repository status unavailable</strong><small>This project folder is not currently recognized as a local Git repository, or Git is not installed.</small></div><Button variant="ghost" onClick={onRefreshGitStatus}>Check Git</Button></div>}</div>
      {fileSummary && <p className="file-summary">{fileSummary}</p>}
      <form className="task-composer" onSubmit={(event) => { event.preventDefault(); void planTask(); }}>
        <div className="task-composer-heading"><div><p className="section-kicker">TASK BRIEF</p><h3>Plan the next change</h3><p>Write the outcome you want, then review it in Agent Workbench before anything runs.</p></div><span className="task-composer-badge">Local only</span></div>
        <div className="task-composer-fields"><label>Task title<input value={taskTitle} onChange={(event) => setTaskTitle(event.target.value)} placeholder="Improve the project browser" /></label><label>What should happen?<textarea value={taskInstruction} onChange={(event) => setTaskInstruction(event.target.value)} placeholder="Describe the outcome, constraints, and how you will verify it." rows={3} /></label></div>
        <EnvironmentPicker value={taskEnvironment} onChange={setTaskEnvironment} hasProject={Boolean(selectedProject)} />
        <div className="task-composer-footer"><span>Creates a proposed local task. It does not start an agent or run tools.</span><Button variant="primary" type="submit" disabled={planningTask || !taskTitle.trim() || !taskInstruction.trim()}>{planningTask ? "Planning…" : "Add to task plan"}</Button></div>
      </form>
      <div className="project-workspace-layout"><div className="project-file-layout">
        <div className="project-file-explorer"><div className="file-list-toolbar"><input ref={fileSearchRef} value={fileQuery} onChange={(event) => setFileQuery(event.target.value)} placeholder="Filter files…" aria-label="Filter project files" /><kbd>Ctrl P</kbd></div><div className="project-file-list">{projectFiles.length === 0 ? <EmptyState title="No readable files" description="Protected names and unsupported files are hidden." /> : filteredFiles.length === 0 ? <EmptyState title="No matching files" description="Try a shorter path or filename." /> : filteredFiles.map((file) => <button className={selectedFile === file.relativePath ? "is-selected" : ""} key={file.relativePath} onClick={() => openFile(file.relativePath)} type="button"><span>{file.relativePath}</span><small>{file.bytes.toLocaleString()} bytes</small></button>)}</div></div>
        <div className="file-editor-panel">
          {openTabs.length > 0 && <div className="file-tabs" role="tablist" aria-label="Open project files">{openTabs.map((path) => <div className={`file-tab ${selectedFile === path ? "is-active" : ""}`} key={path}><button type="button" role="tab" aria-selected={selectedFile === path} onClick={() => openFile(path)}><span>{path}</span>{selectedFile === path && hasChanges ? <i aria-label="Unsaved changes">•</i> : null}</button><button className="file-tab-close" type="button" onClick={() => closeTab(path)} disabled={selectedFile === path && hasChanges} aria-label={`Close ${path}`}>×</button></div>)}</div>}
          {selectedFile ? <><div className="file-editor-heading"><span>{selectedFile}</span><div><Button variant={panelMode === "editor" ? "secondary" : "ghost"} onClick={() => setPanelMode("editor")}>Editor</Button><Button variant={panelMode === "diff" ? "secondary" : "ghost"} disabled={!hasChanges} onClick={() => setPanelMode("diff")}>Review diff</Button><Button variant="ghost" disabled={fileBusy} onClick={onUndoFile}>Undo</Button><Button variant="primary" disabled={fileBusy || !hasChanges} onClick={onSaveFile}>{fileBusy ? "Saving…" : "Save edit"}</Button></div></div>{panelMode === "diff" ? <div className="diff-preview" aria-label="Local diff preview">{diffRows.map((row, index) => <div className={`diff-row diff-${row.kind}`} key={`${row.kind}-${index}`}><span aria-hidden="true">{row.kind === "removed" ? "−" : row.kind === "added" ? "+" : "·"}</span><code>{row.line || " "}</code></div>)}</div> : <textarea value={fileDraft} onChange={(event) => onDraftChange(event.target.value)} aria-label={`Edit ${selectedFile}`} rows={16} />}</> : <EmptyState title="Select a file" description="Choose a readable file to preview or edit it with guarded write-back." />}
        </div>
      </div><Card className="coding-activity-panel"><div className="coding-activity-heading"><div><p className="section-kicker">WORKSPACE ACTIVITY</p><h3>{fileBusy || terminalBusy ? "Working locally" : "Ready for review"}</h3></div><span className="coding-activity-close" aria-hidden="true">×</span></div><p className="coding-activity-copy">Live state from the selected project, file editor, and bounded terminal session.</p><div className="coding-activity-progress"><span style={{ width: `${activityProgress}%` }} /></div><div className="coding-activity-meta"><span>{activityProgress}% context ready</span><span>{hasChanges ? "Review required" : "No pending edits"}</span></div><div className="coding-activity-list">{activityItems.map(([title, detail, complete]) => <div className={`coding-activity-item ${complete ? "is-complete" : ""}`} key={title}><span className="coding-activity-dot" aria-hidden="true" /><div><strong>{title}</strong><small>{detail}</small></div></div>)}</div><div className="coding-activity-note"><strong>Guarded workspace</strong><span>File writes and terminal commands remain explicit actions at the host boundary.</span></div></Card></div>
      {terminalOpen && <div className="project-dock"><TerminalPanel projects={projects} initialProjectId={selectedProject.id} busy={terminalBusy} output={terminalOutput} exitCode={terminalExitCode} compact onRun={onRunTerminal} /></div>}
    </Card>}
    <ModalDialog open={dialogOpen} onClose={() => setDialogOpen(false)} titleId="new-project-title" initialFocusRef={projectNameRef}><form onSubmit={(event) => { event.preventDefault(); void createProject(); }}><div className="dialog-heading"><div><p className="section-kicker">NEW PROJECT</p><h3 id="new-project-title">Connect a workspace</h3><p>Only the folders you select are exposed to the project.</p></div><button type="button" onClick={() => setDialogOpen(false)} aria-label="Close dialog">×</button></div><label>Project name<input ref={projectNameRef} value={name} onChange={(event) => setName(event.target.value)} placeholder="My project" /></label><label>Instructions<textarea value={instructions} onChange={(event) => setInstructions(event.target.value)} placeholder="Optional instructions for this project" rows={3} /></label><div className="dialog-folders"><div className="dialog-folders-heading"><span>Connected folders</span><Button variant="secondary" type="button" onClick={() => void addFolder()}>Add folder</Button></div>{folders.length === 0 ? <p>No folders connected yet.</p> : folders.map((folder) => <div className="dialog-folder" key={folder}><span>{folder}</span><button type="button" onClick={() => setFolders((current) => current.filter((item) => item !== folder))}>Remove</button></div>)}</div><div className="dialog-actions"><Button variant="ghost" type="button" onClick={() => setDialogOpen(false)}>Cancel</Button><Button variant="primary" type="submit" disabled={saving || !name.trim()}>{saving ? "Saving…" : "Save project"}</Button></div></form></ModalDialog>
  </div>;
}
