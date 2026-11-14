import { useRef, useState } from "react";
import type { DesktopGitStatus, DesktopProject, DesktopProjectFile, DesktopTask, TaskEnvironment } from "../../../shared/contracts";
import { Button, Card, EmptyState, PageHeader } from "../../components/ui/Primitives";
import { ModalDialog } from "../../components/ui/ModalDialog";
import { NavIcon } from "../../components/ui/NavIcon";
import { CodingSessionView } from "./CodingSessionView";

type CodingPageProps = {
  projects: DesktopProject[];
  selectedProject: DesktopProject | null;
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
  onRefreshGitStatus: () => void;
  onPickFolder: () => Promise<string | null>;
  onSaveProject: (name: string, instructions: string, folders: string[]) => Promise<boolean>;
  onOpenProject: (project: DesktopProject) => void;
  onOpenFile: (relativePath: string) => void;
  onCloseFile: () => void;
  onDraftChange: (value: string) => void;
  onSaveFile: () => void;
  onUndoFile: () => void;
  onRunTerminal: (projectId: string, command: string, cwd: string) => void;
  onPlanTask: (title: string, instruction: string, environment: TaskEnvironment) => Promise<boolean>;
  onNavigate: (route: "terminal" | "workbench") => void;
};

export function CodingPage({ projects, selectedProject, tasks, projectFiles, selectedFile, fileDraft, fileOriginal, fileSummary, fileBusy, fileUndoAvailable, terminalBusy, terminalOutput, terminalExitCode, gitStatus, gitBusy, onRefreshGitStatus, onPickFolder, onSaveProject, onOpenProject, onOpenFile, onCloseFile, onDraftChange, onSaveFile, onUndoFile, onRunTerminal, onPlanTask, onNavigate }: CodingPageProps) {
  const [dialogOpen, setDialogOpen] = useState(false);
  const [name, setName] = useState("");
  const [instructions, setInstructions] = useState("");
  const [folders, setFolders] = useState<string[]>([]);
  const [saving, setSaving] = useState(false);
  const projectNameRef = useRef<HTMLInputElement>(null);

  const createProject = async () => {
    if (!name.trim() || saving) return;
    setSaving(true);
    const saved = await onSaveProject(name.trim(), instructions.trim(), folders);
    setSaving(false);
    if (saved) { setName(""); setInstructions(""); setFolders([]); setDialogOpen(false); }
  };

  const addFolder = async () => {
    const folder = await onPickFolder();
    if (folder && !folders.includes(folder)) setFolders((current) => [...current, folder]);
  };

  return <div className={`feature-page coding-page ${selectedProject ? "coding-page-active" : ""}`}>
    <PageHeader className={selectedProject ? "page-header-coding-session" : ""} eyebrow="CODING WORKSPACE" title={selectedProject?.name ?? "Build with context"} description={selectedProject ? "A focused local session for files, tasks, terminal output, and reviewable edits." : "A local project workspace for planning tasks, reviewing edits, and running guarded commands."} actions={<Button variant="primary" onClick={() => setDialogOpen(true)}><NavIcon name="plus" />New project</Button>} />
    <div className="coding-session-bar" aria-label="Coding session status"><div className="coding-session-copy"><span className="coding-session-dot" aria-hidden="true" /><strong>Local coding session</strong><span>Files stay in the folders you choose.</span></div><div className="coding-shortcuts"><span><kbd>Ctrl P</kbd> Find file</span><span><kbd>Ctrl J</kbd> Terminal</span><span><kbd>Ctrl S</kbd> Save file</span></div></div>
    {!selectedProject && <div className="workspace-grid coding-overview-grid">
      <Card className="workspace-main coding-projects-panel"><div className="panel-heading"><div><p className="section-kicker">PROJECTS</p><h3>Continue building</h3><p className="panel-subtitle">Choose a bounded local workspace to give the agent context.</p></div><span className="panel-count">{projects.length} {projects.length === 1 ? "project" : "projects"}</span></div>{projects.length === 0 ? <EmptyState title="Create your first project" description="Connect one or more folders, then keep the task, files, and review surface together." action={<Button variant="secondary" onClick={() => setDialogOpen(true)}><NavIcon name="plus" />Create project</Button>} /> : <div className="project-list">{projects.map((project) => <button className="project-card" key={project.id} onClick={() => onOpenProject(project)} type="button"><span className="project-icon"><NavIcon name="project" /></span><span><strong>{project.name}</strong><small>{project.folderPaths.length ? `${project.folderPaths.length} connected folder${project.folderPaths.length === 1 ? "" : "s"}` : "No folders connected"}</small></span><span className="project-arrow"><NavIcon name="arrow-right" /></span></button>)}</div>}</Card>
      <Card className="workspace-side coding-flow-panel"><div className="coding-flow-heading"><div><p className="section-kicker">WORKFLOW</p><h3>From task to review</h3></div><span className="coding-flow-mark" aria-hidden="true"><NavIcon name="arrow-up-right" /></span></div><div className="flow-step"><b>01</b><span><strong>Select a project</strong><small>Keep context explicit and bounded.</small></span></div><div className="flow-step"><b>02</b><span><strong>Plan a local task</strong><small>Describe the outcome before tools run.</small></span></div><div className="flow-step"><b>03</b><span><strong>Review the diff</strong><small>Every write stays visible and reversible.</small></span></div></Card>
    </div>}
    {selectedProject && <CodingSessionView projects={projects} selectedProject={selectedProject} tasks={tasks} projectFiles={projectFiles} selectedFile={selectedFile} fileDraft={fileDraft} fileOriginal={fileOriginal} fileSummary={fileSummary} fileBusy={fileBusy} fileUndoAvailable={fileUndoAvailable} terminalBusy={terminalBusy} terminalOutput={terminalOutput} terminalExitCode={terminalExitCode} gitStatus={gitStatus} gitBusy={gitBusy} onRefreshGitStatus={onRefreshGitStatus} onOpenProject={onOpenProject} onOpenFile={onOpenFile} onCloseFile={onCloseFile} onDraftChange={onDraftChange} onSaveFile={onSaveFile} onUndoFile={onUndoFile} onRunTerminal={onRunTerminal} onPlanTask={onPlanTask} onNavigate={onNavigate} />}
    <ModalDialog open={dialogOpen} onClose={() => setDialogOpen(false)} titleId="new-project-title" initialFocusRef={projectNameRef}><form onSubmit={(event) => { event.preventDefault(); void createProject(); }}><div className="dialog-heading"><div><p className="section-kicker">NEW PROJECT</p><h3 id="new-project-title">Connect a workspace</h3><p>Only the folders you select are exposed to the project.</p></div><button type="button" onClick={() => setDialogOpen(false)} aria-label="Close dialog">×</button></div><label>Project name<input ref={projectNameRef} value={name} onChange={(event) => setName(event.target.value)} placeholder="My project" /></label><label>Instructions<textarea value={instructions} onChange={(event) => setInstructions(event.target.value)} placeholder="Optional instructions for this project" rows={3} /></label><div className="dialog-folders"><div className="dialog-folders-heading"><span>Connected folders</span><Button variant="secondary" type="button" onClick={() => void addFolder()}>Add folder</Button></div>{folders.length === 0 ? <p>No folders connected yet.</p> : folders.map((folder) => <div className="dialog-folder" key={folder}><span>{folder}</span><button type="button" onClick={() => setFolders((current) => current.filter((item) => item !== folder))}>Remove</button></div>)}</div><div className="dialog-actions"><Button variant="ghost" type="button" onClick={() => setDialogOpen(false)}>Cancel</Button><Button variant="primary" type="submit" disabled={saving || !name.trim()}>{saving ? "Saving…" : "Save project"}</Button></div></form></ModalDialog>
  </div>;
}
