import { useEffect, useMemo, useRef, useState, type KeyboardEvent } from "react";
import type { DesktopGitDiff, DesktopGitStatus, DesktopProject, DesktopProjectFile, DesktopTask, TaskEnvironment } from "../../../shared/contracts";
import { Button, Card, EmptyState } from "../../components/ui/Primitives";
import { EnvironmentPicker } from "../../components/ui/EnvironmentPicker";
import { NavIcon } from "../../components/ui/NavIcon";
import { TerminalPanel } from "../terminal/TerminalPanel";
import { CodingActivityPanel } from "./CodingActivityPanel";

type DiffRow = { kind: "removed" | "added" | "unchanged"; line: string };

function terminalStorageKey(projectId: string) {
  return `skai.coding.terminalOpen.${projectId}`;
}

function taskComposerStorageKey(projectId: string) {
  return `skai.coding.taskComposerOpen.${projectId}`;
}

function openTabsStorageKey(projectId: string) {
  return `skai.coding.openTabs.${projectId}`;
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

type CodingSessionViewProps = {
  projects: DesktopProject[];
  selectedProject: DesktopProject;
  tasks: DesktopTask[];
  projectFiles: DesktopProjectFile[];
  selectedFile: string | null;
  fileDraft: string;
  fileOriginal: string;
  fileSummary: string | null;
  fileBusy: boolean;
  fileUndoAvailable: boolean;
  terminalBusy: boolean;
  terminalOutput: string | null;
  terminalExitCode: number | null;
  gitStatus: DesktopGitStatus | null;
  gitBusy: boolean;
  gitDiff: DesktopGitDiff | null;
  gitDiffBusy: boolean;
  gitCommitBusy: boolean;
  gitPushBusy: boolean;
  onOpenProject: (project: DesktopProject) => void;
  onOpenFile: (relativePath: string) => void;
  onCloseFile: () => void;
  onDraftChange: (value: string) => void;
  onSaveFile: () => void;
  onUndoFile: () => void;
  onRunTerminal: (projectId: string, command: string, cwd: string) => void;
  onRefreshGitStatus: () => void;
  onLoadGitDiff: () => void;
  onCommitGitChanges: (message: string) => Promise<boolean>;
  onPushGitChanges: () => Promise<boolean>;
  onPlanTask: (title: string, instruction: string, environment: TaskEnvironment) => Promise<boolean>;
  onNavigate: (route: "terminal" | "workbench") => void;
};

export function CodingSessionView({ projects, selectedProject, tasks, projectFiles, selectedFile, fileDraft, fileOriginal, fileSummary, fileBusy, fileUndoAvailable, terminalBusy, terminalOutput, terminalExitCode, gitStatus, gitBusy, gitDiff, gitDiffBusy, gitCommitBusy, gitPushBusy, onRefreshGitStatus, onLoadGitDiff, onCommitGitChanges, onPushGitChanges, onOpenProject, onOpenFile, onCloseFile, onDraftChange, onSaveFile, onUndoFile, onRunTerminal, onPlanTask, onNavigate }: CodingSessionViewProps) {
  const [openTabs, setOpenTabs] = useState<string[]>([]);
  const [fileQuery, setFileQuery] = useState("");
  const [terminalOpen, setTerminalOpen] = useState(false);
  const [taskTitle, setTaskTitle] = useState("");
  const [taskInstruction, setTaskInstruction] = useState("");
  const [taskEnvironment, setTaskEnvironment] = useState<TaskEnvironment>("local");
  const [taskComposerOpen, setTaskComposerOpen] = useState(false);
  const [planningTask, setPlanningTask] = useState(false);
  const [panelMode, setPanelMode] = useState<"editor" | "diff">("editor");
  const [gitMessage, setGitMessage] = useState("Update local project");
  const loadedTabsProjectRef = useRef<string | null>(null);
  const skipTabPersistenceRef = useRef(false);
  const hasChanges = Boolean(selectedFile && fileDraft !== fileOriginal);
  const diffRows = useMemo(() => createDiff(fileOriginal, fileDraft), [fileDraft, fileOriginal]);
  const filteredFiles = useMemo(() => {
    const query = fileQuery.trim().toLowerCase();
    return query ? projectFiles.filter((file) => file.relativePath.toLowerCase().includes(query)) : projectFiles;
  }, [fileQuery, projectFiles]);
  const fileSearchRef = useRef<HTMLInputElement>(null);

  useEffect(() => { setPanelMode("editor"); }, [selectedFile, fileOriginal]);
  useEffect(() => {
    let restoredTabs: string[] = [];
    try {
      const storedTabs = JSON.parse(localStorage.getItem(openTabsStorageKey(selectedProject.id)) ?? "[]");
      if (Array.isArray(storedTabs)) restoredTabs = storedTabs.filter((path): path is string => typeof path === "string" && path.length > 0).slice(0, 12);
    } catch {
      restoredTabs = [];
    }
    loadedTabsProjectRef.current = selectedProject.id;
    skipTabPersistenceRef.current = true;
    setOpenTabs(restoredTabs);
    setFileQuery("");
    setTerminalOpen(localStorage.getItem(terminalStorageKey(selectedProject.id)) === "true");
    setTaskComposerOpen(localStorage.getItem(taskComposerStorageKey(selectedProject.id)) === "true");
  }, [selectedProject.id]);
  useEffect(() => {
    if (loadedTabsProjectRef.current !== selectedProject.id) return;
    if (skipTabPersistenceRef.current) {
      skipTabPersistenceRef.current = false;
      return;
    }
    localStorage.setItem(openTabsStorageKey(selectedProject.id), JSON.stringify(openTabs));
  }, [openTabs, selectedProject.id]);
  useEffect(() => { localStorage.setItem(terminalStorageKey(selectedProject.id), String(terminalOpen)); }, [selectedProject.id, terminalOpen]);
  useEffect(() => { localStorage.setItem(taskComposerStorageKey(selectedProject.id), String(taskComposerOpen)); }, [selectedProject.id, taskComposerOpen]);
  useEffect(() => { if (selectedFile) setOpenTabs((current) => current.includes(selectedFile) ? current : [...current, selectedFile]); }, [selectedFile]);
  useEffect(() => {
    const onShortcut = (event: globalThis.KeyboardEvent) => {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "p") {
        event.preventDefault();
        fileSearchRef.current?.focus();
        fileSearchRef.current?.select();
      }
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "j") {
        event.preventDefault();
        setTerminalOpen((open) => !open);
      }
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "s" && selectedFile && hasChanges && !fileBusy) {
        event.preventDefault();
        onSaveFile();
      }
    };
    window.addEventListener("keydown", onShortcut);
    return () => window.removeEventListener("keydown", onShortcut);
  }, [fileBusy, hasChanges, onSaveFile, selectedFile]);

  const planTask = async () => {
    if (!taskTitle.trim() || !taskInstruction.trim() || planningTask) return;
    setPlanningTask(true);
    const planned = await onPlanTask(taskTitle.trim(), `${taskInstruction.trim()}\n\nProject: ${selectedProject.name}`, taskEnvironment);
    setPlanningTask(false);
    if (planned) { setTaskTitle(""); setTaskInstruction(""); onNavigate("workbench"); }
  };

  const openFile = (path: string) => {
    setOpenTabs((current) => current.includes(path) ? current : [...current, path]);
    onOpenFile(path);
  };

  const focusTab = (index: number) => {
    const path = openTabs[index];
    if (!path) return;
    openFile(path);
    window.requestAnimationFrame(() => document.getElementById(`coding-file-tab-${index}`)?.focus());
  };

  const onTabKeyDown = (event: KeyboardEvent<HTMLButtonElement>, index: number) => {
    const nextIndex = event.key === "ArrowRight" ? (index + 1) % openTabs.length
      : event.key === "ArrowLeft" ? (index - 1 + openTabs.length) % openTabs.length
      : event.key === "Home" ? 0
      : event.key === "End" ? openTabs.length - 1
      : -1;
    if (nextIndex < 0 || !openTabs.length) return;
    event.preventDefault();
    focusTab(nextIndex);
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

  return <>
    <div className="coding-project-context" aria-label="Active coding project"><div><p className="section-kicker">ACTIVE PROJECT</p><strong>{selectedProject.name}</strong><span>{selectedProject.folderPaths.length} connected folder{selectedProject.folderPaths.length === 1 ? "" : "s"} · local files only</span></div><div className="coding-project-context-actions"><div className={`git-summary${gitStatus?.hasConflicts ? " has-conflicts" : ""}`} aria-live="polite"><strong>{gitStatus ? gitStatus.branch : "Git status unavailable"}</strong><span>{gitStatus ? `${gitStatus.changes.length} change${gitStatus.changes.length === 1 ? "" : "s"}${gitStatus.hasConflicts ? " · conflicts" : ""}` : "Select a Git repository to inspect changes"}</span></div><Button variant="ghost" disabled={gitBusy} onClick={onRefreshGitStatus}>{gitBusy ? "Checking…" : "Refresh Git"}</Button><label><span className="sr-only">Switch coding project</span><select value={selectedProject.id} onChange={(event) => { const project = projects.find((item) => item.id === event.target.value); if (project) onOpenProject(project); }} aria-label="Switch coding project">{projects.map((project) => <option value={project.id} key={project.id}>{project.name}</option>)}</select></label></div></div>
    <div className="coding-session-grid"><Card className="project-browser-card coding-project-browser">
      <div className="panel-heading"><div><p className="section-kicker">PROJECT FILES</p><h3>Files &amp; changes</h3></div><div className="project-browser-actions"><span className="panel-count">{projectFiles.length}</span><Button variant={terminalOpen ? "secondary" : "ghost"} aria-pressed={terminalOpen} onClick={() => setTerminalOpen((open) => !open)}>{terminalOpen ? "Hide terminal" : "Dock terminal"}</Button><Button variant="ghost" onClick={() => onNavigate("terminal")}>Open terminal</Button><Button variant="ghost" onClick={() => onNavigate("workbench")}>Review tasks</Button></div></div>
      {fileSummary && <p className="file-summary">{fileSummary}</p>}
      {gitStatus && <div className={`git-change-summary${gitStatus.hasConflicts ? " has-conflicts" : ""}`} aria-label="Local Git changes"><div><p className="section-kicker">LOCAL GIT REVIEW</p><strong>{gitStatus.changes.length ? `${gitStatus.changes.length} changed file${gitStatus.changes.length === 1 ? "" : "s"}` : "Working tree clean"}</strong><span>{gitStatus.commitsAhead} ahead · {gitStatus.commitsBehind} behind · {gitStatus.repository}</span></div>{gitStatus.changes.length > 0 && <ul>{gitStatus.changes.slice(0, 8).map((change) => <li key={`${change.status}-${change.path}`}><b>{change.status}</b><span>{change.path}</span></li>)}</ul>}{gitStatus.changes.length > 8 && <small>Showing the first 8 changes. Review is bounded to 24 files per operation.</small>}{gitStatus.hasConflicts && <small role="alert">Conflicts need review before any commit workflow can proceed.</small>}{gitStatus.changes.length > 0 && <div className="git-review-actions"><Button variant="secondary" disabled={gitDiffBusy || gitStatus.hasConflicts} onClick={onLoadGitDiff}>{gitDiffBusy ? "Loading diff…" : gitDiff ? "Refresh diff" : "Review diff"}</Button><input value={gitMessage} onChange={(event) => setGitMessage(event.target.value)} aria-label="Local Git commit message" maxLength={256} placeholder="Commit message" /><Button variant="primary" disabled={gitCommitBusy || gitStatus.hasConflicts || !gitMessage.trim()} onClick={() => void onCommitGitChanges(gitMessage)}>{gitCommitBusy ? "Committing…" : "Commit changes"}</Button></div>}{gitDiff && <pre className="git-diff-preview" aria-label="Local Git diff preview">{gitDiff.text}</pre>}</div>}
       {gitStatus && gitStatus.changes.length === 0 && gitStatus.commitsAhead > 0 && <div className="git-review-actions"><span className="settings-help">Clean working tree · push uses the configured local Git credential helper.</span><Button variant="primary" disabled={gitPushBusy || gitStatus.commitsBehind > 0} onClick={() => void onPushGitChanges()}>{gitPushBusy ? "Pushing…" : "Push local commits"}</Button></div>}
       <div className={`task-composer coding-task-composer ${taskComposerOpen ? "is-open" : ""}`}>
        <div className="task-composer-heading"><div><p className="section-kicker">TASK BRIEF</p><h3>Plan the next change</h3><p>{taskComposerOpen ? "Write the outcome you want, then review it in Agent Workbench before anything runs." : "Draft a reviewed local task without leaving this project."}</p></div><span className="task-composer-badge">Local only</span><Button variant="secondary" type="button" aria-expanded={taskComposerOpen} aria-controls="coding-task-brief" onClick={() => setTaskComposerOpen((open) => !open)}>{taskComposerOpen ? "Hide brief" : "Plan task"}</Button></div>
        {taskComposerOpen && <form id="coding-task-brief" className="task-composer-form" onSubmit={(event) => { event.preventDefault(); void planTask(); }}>
          <div className="task-composer-fields"><label>Task title<input value={taskTitle} onChange={(event) => setTaskTitle(event.target.value)} placeholder="Improve the project browser" /></label><label>What should happen?<textarea value={taskInstruction} onChange={(event) => setTaskInstruction(event.target.value)} placeholder="Describe the outcome, constraints, and how you will verify it." rows={3} /></label></div>
          <EnvironmentPicker value={taskEnvironment} onChange={setTaskEnvironment} />
          <div className="task-composer-footer"><span>Creates a proposed local task. It does not start an agent or run tools.</span><Button variant="primary" type="submit" disabled={planningTask || !taskTitle.trim() || !taskInstruction.trim()}>{planningTask ? "Planning…" : "Add to task plan"}</Button></div>
        </form>}
      </div>
      <div className="project-file-layout">
        <div className="project-file-explorer"><div className="file-list-toolbar"><input ref={fileSearchRef} value={fileQuery} onChange={(event) => setFileQuery(event.target.value)} placeholder="Filter files…" aria-label="Filter project files" /><kbd>Ctrl P</kbd></div><div className="project-file-list">{projectFiles.length === 0 ? <EmptyState title="No readable files" description="Protected names and unsupported files are hidden." /> : filteredFiles.length === 0 ? <EmptyState title="No matching files" description="Try a shorter path or filename." /> : filteredFiles.map((file) => <button className={selectedFile === file.relativePath ? "is-selected" : ""} key={file.relativePath} onClick={() => openFile(file.relativePath)} type="button"><span>{file.relativePath}</span><small>{file.bytes.toLocaleString()} bytes</small></button>)}</div></div>
        <div className="file-editor-panel">
          {openTabs.length > 0 && <div className="file-tabs" role="tablist" aria-orientation="horizontal" aria-label="Open project files">{openTabs.map((path, index) => <div className={`file-tab ${selectedFile === path ? "is-active" : ""}`} key={path}><button id={`coding-file-tab-${index}`} type="button" role="tab" tabIndex={selectedFile === path || (!selectedFile && index === 0) ? 0 : -1} aria-selected={selectedFile === path} aria-controls="coding-file-editor" onClick={() => openFile(path)} onKeyDown={(event) => onTabKeyDown(event, index)}><span>{path}</span>{selectedFile === path && hasChanges ? <i aria-label="Unsaved changes">•</i> : null}</button><button className="file-tab-close" type="button" onClick={() => closeTab(path)} disabled={selectedFile === path && hasChanges} aria-label={`Close ${path}`}>×</button></div>)}</div>}
          {selectedFile ? <><div id="coding-file-editor" className="file-editor-heading"><span>{selectedFile}</span><div><Button variant={panelMode === "editor" ? "secondary" : "ghost"} onClick={() => setPanelMode("editor")}>Editor</Button><Button variant={panelMode === "diff" ? "secondary" : "ghost"} disabled={!hasChanges} onClick={() => setPanelMode("diff")}>Review diff</Button><Button variant="ghost" disabled={fileBusy || !fileUndoAvailable} title={fileUndoAvailable ? "Undo the last saved edit" : "No saved edit to undo"} onClick={onUndoFile}>Undo</Button><Button variant="primary" disabled={fileBusy || !hasChanges} onClick={onSaveFile}>{fileBusy ? "Saving…" : "Save edit"}</Button></div></div>{panelMode === "diff" ? <div className="diff-preview" aria-label="Local diff preview">{diffRows.map((row, index) => <div className={`diff-row diff-${row.kind}`} key={`${row.kind}-${index}`}><span aria-hidden="true">{row.kind === "removed" ? "−" : row.kind === "added" ? "+" : "·"}</span><code>{row.line || " "}</code></div>)}</div> : <textarea value={fileDraft} onChange={(event) => onDraftChange(event.target.value)} aria-label={`Edit ${selectedFile}`} rows={16} />}</> : <EmptyState title="Select a file" description="Choose a readable file to preview or edit it with guarded write-back." />}
        </div>
      </div>
      {terminalOpen && <div className="project-dock"><TerminalPanel projects={projects} initialProjectId={selectedProject.id} busy={terminalBusy} output={terminalOutput} exitCode={terminalExitCode} compact onRun={onRunTerminal} /></div>}
    </Card><CodingActivityPanel tasks={tasks} onOpenWorkbench={() => onNavigate("workbench")} /></div>
  </>;
}
