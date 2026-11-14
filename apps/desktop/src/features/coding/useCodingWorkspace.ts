import { useEffect, useState } from "react";
import type { DesktopGitDiff, DesktopGitStatus, DesktopProject, DesktopProjectFile } from "../../../shared/contracts";
import type { AgentRequest } from "../../services/agentClient";

type CodingWorkspaceOptions = {
  request: AgentRequest;
  connection: "checking" | "ready" | "error";
  onError: (message: string | null) => void;
  onNavigate: (route: "coding") => void;
};

export function useCodingWorkspace({ request, connection, onError, onNavigate }: CodingWorkspaceOptions) {
  const [projects, setProjects] = useState<DesktopProject[]>([]);
  const [selectedProject, setSelectedProject] = useState<DesktopProject | null>(null);
  const [projectFiles, setProjectFiles] = useState<DesktopProjectFile[]>([]);
  const [selectedFile, setSelectedFile] = useState<string | null>(null);
  const [fileDraft, setFileDraft] = useState("");
  const [fileOriginal, setFileOriginal] = useState("");
  const [fileHash, setFileHash] = useState<string | null>(null);
  const [fileUndoId, setFileUndoId] = useState<string | null>(null);
  const [fileSummary, setFileSummary] = useState<string | null>(null);
  const [fileBusy, setFileBusy] = useState(false);
  const [terminalBusy, setTerminalBusy] = useState(false);
  const [terminalOutput, setTerminalOutput] = useState<string | null>(null);
  const [terminalExitCode, setTerminalExitCode] = useState<number | null>(null);
  const [terminalSessionId, setTerminalSessionId] = useState<string | null>(null);
  const [terminalScreen, setTerminalScreen] = useState<string | null>(null);
  const [gitStatus, setGitStatus] = useState<DesktopGitStatus | null>(null);
  const [gitBusy, setGitBusy] = useState(false);
  const [gitDiff, setGitDiff] = useState<DesktopGitDiff | null>(null);
  const [gitDiffBusy, setGitDiffBusy] = useState(false);
  const [gitCommitBusy, setGitCommitBusy] = useState(false);
  const [gitPushBusy, setGitPushBusy] = useState(false);
  const [gitPullRequestBusy, setGitPullRequestBusy] = useState(false);

  useEffect(() => {
    if (connection !== "ready") return;
    void request("projects.list").then((response) => {
      if (!response.ok) { onError(response.error?.message ?? "Projects could not be loaded."); return; }
      setProjects((response.result as DesktopProject[]) ?? []);
    }).catch(() => onError("Projects could not be loaded."));
  }, [connection, onError, request]);

  const pickProjectFolder = async () => {
    const response = await request("workspace.pickFolder");
    if (!response.ok) { onError(response.error?.message ?? "Folder picker failed."); return null; }
    return (response.result as { path?: string | null }).path ?? null;
  };

  const saveProject = async (name: string, instructions: string, folderPaths: string[]) => {
    const response = await request("project.save", { name, instructions, folderPaths });
    if (!response.ok) { onError(response.error?.message ?? "Project could not be saved."); return false; }
    const refreshed = await request("projects.list");
    if (refreshed.ok) setProjects((refreshed.result as DesktopProject[]) ?? []);
    else onError(refreshed.error?.message ?? "Projects could not be refreshed.");
    onError(null);
    return true;
  };

  const selectProject = async (project: DesktopProject) => {
    localStorage.setItem("skai.selectedProject", project.id);
    setSelectedProject(project); setSelectedFile(null); setFileDraft(""); setFileOriginal(""); setFileHash(null); setFileUndoId(null); setGitStatus(null); setGitDiff(null);
    const response = await request("project.files", { id: project.id });
    if (!response.ok) { onError(response.error?.message ?? "Project browser failed."); return; }
    const result = response.result as { files: DesktopProjectFile[]; protectedFilesSkipped: number; hitEntryLimit: boolean };
    setProjectFiles(result.files ?? []);
    setFileSummary(`${result.files.length} readable files · ${result.protectedFilesSkipped} protected names hidden${result.hitEntryLimit ? " · entry limit reached" : ""}`);
    await refreshGitStatus(project.id);
    onNavigate("coding");
  };

  const refreshGitStatus = async (projectId = selectedProject?.id) => {
    if (!projectId || gitBusy) return;
    setGitBusy(true);
    const response = await request("project.git.status", { id: projectId });
    setGitBusy(false);
    if (!response.ok) { setGitStatus(null); onError(response.error?.message ?? "Git status could not be loaded."); return; }
    setGitStatus(response.result as DesktopGitStatus);
    onError(null);
  };

  const loadGitDiff = async () => {
    if (!selectedProject || !gitStatus?.changes.length || gitDiffBusy) return;
    setGitDiffBusy(true);
    const response = await request("project.git.diff", { id: selectedProject.id, paths: gitStatus.changes.map((change) => change.path) });
    setGitDiffBusy(false);
    if (!response.ok) { onError(response.error?.message ?? "Git diff could not be loaded."); return; }
    setGitDiff(response.result as DesktopGitDiff);
    onError(null);
  };

  const commitGitChanges = async (message: string) => {
    if (!selectedProject || !gitStatus?.changes.length || !message.trim() || gitCommitBusy) return false;
    setGitCommitBusy(true);
    const response = await request("project.git.commit", { id: selectedProject.id, paths: gitStatus.changes.map((change) => change.path), message: message.trim() });
    setGitCommitBusy(false);
    if (!response.ok) { onError(response.error?.message ?? "Local Git commit was rejected."); return false; }
    setGitDiff(null);
    await refreshGitStatus(selectedProject.id);
    onError(null);
    return true;
  };

  const pushGitChanges = async () => {
    if (!selectedProject || gitPushBusy) return false;
    setGitPushBusy(true);
    const response = await request("project.git.push", { id: selectedProject.id });
    setGitPushBusy(false);
    if (!response.ok) { onError(response.error?.message ?? "Git push was rejected."); return false; }
    await refreshGitStatus(selectedProject.id);
    onError(null);
    return true;
  };

  const createGitPullRequest = async (title: string, body: string) => {
    if (!selectedProject || gitPullRequestBusy || !title.trim()) return false;
    setGitPullRequestBusy(true);
    const response = await request("project.git.pullRequest", { id: selectedProject.id, title: title.trim(), body: body.trim() });
    setGitPullRequestBusy(false);
    if (!response.ok) { onError(response.error?.message ?? "Pull request creation was unavailable."); return false; }
    onError(null);
    return true;
  };

  useEffect(() => {
    if (connection !== "ready" || selectedProject || projects.length === 0) return;
    const storedId = localStorage.getItem("skai.selectedProject");
    const storedProject = storedId ? projects.find((project) => project.id === storedId) : undefined;
    if (storedProject) void selectProject(storedProject);
  }, [connection, projects, selectedProject]);

  const openProjectFile = async (relativePath: string) => {
    if (!selectedProject) return;
    setSelectedFile(relativePath); setFileDraft(""); setFileOriginal(""); setFileHash(null); setFileUndoId(null);
    const response = await request("project.file.read", { id: selectedProject.id, relativePath });
    if (!response.ok) { onError(response.error?.message ?? "File preview failed."); return; }
    const result = response.result as { content: string; hash: string };
    setFileDraft(result.content); setFileOriginal(result.content); setFileHash(result.hash);
  };

  const closeProjectFile = () => { setSelectedFile(null); setFileDraft(""); setFileOriginal(""); setFileHash(null); setFileUndoId(null); };

  const saveProjectFile = async () => {
    if (!selectedProject || !selectedFile || !fileHash || fileBusy) return;
    setFileBusy(true);
    const response = await request("project.file.write", { id: selectedProject.id, relativePath: selectedFile, content: fileDraft, expectedHash: fileHash });
    setFileBusy(false);
    if (!response.ok) { onError(response.error?.message ?? "File edit was rejected."); return; }
    const result = response.result as { hash: string; undoId: string };
    setFileHash(result.hash); setFileOriginal(fileDraft); setFileUndoId(result.undoId); onError(null);
  };

  const undoProjectFile = async () => {
    if (!fileUndoId || fileBusy) return;
    setFileBusy(true);
    const response = await request("project.file.undo", { undoId: fileUndoId });
    setFileBusy(false);
    if (!response.ok) { onError(response.error?.message ?? "Undo was rejected."); return; }
    setFileUndoId(null);
    if (selectedFile) await openProjectFile(selectedFile);
  };

  const runTerminal = async (projectId: string, command: string, cwd: string) => {
    setTerminalBusy(true); setTerminalOutput(null);
    const response = await request("project.command.run", { id: projectId, command, relativeWorkingDirectory: cwd || ".", timeoutSeconds: 30 });
    setTerminalBusy(false);
    if (!response.ok) { onError(response.error?.message ?? "Command failed to run."); return; }
    const result = response.result as { output: string; exitCode: number; timedOut: boolean; outputTruncated: boolean };
    setTerminalOutput(`${result.output || "(no output)"}${result.timedOut ? "\n\n[command timed out]" : result.outputTruncated ? "\n\n[output truncated]" : ""}`);
    setTerminalExitCode(result.exitCode);
  };

  const startTerminalSession = async (projectId: string, cwd: string) => {
    setTerminalBusy(true);
    const response = await request("project.terminal.start", { id: projectId, relativeWorkingDirectory: cwd || ".", columns: 100, rows: 26 });
    setTerminalBusy(false);
    if (!response.ok) { onError(response.error?.message ?? "Persistent terminal could not start."); return false; }
    const result = response.result as { sessionId: string; screen: string; exitCode?: number | null };
    setTerminalSessionId(result.sessionId); setTerminalScreen(result.screen); setTerminalExitCode(result.exitCode ?? null); onError(null); return true;
  };

  const writeTerminalSession = async (text: string) => {
    if (!terminalSessionId || !text) return false;
    const response = await request("project.terminal.write", { sessionId: terminalSessionId, text });
    if (!response.ok) { onError(response.error?.message ?? "Terminal input could not be sent."); return false; }
    const result = response.result as { screen: string; exitCode?: number | null };
    setTerminalScreen(result.screen); setTerminalExitCode(result.exitCode ?? null); return true;
  };

  const refreshTerminalSession = async () => {
    if (!terminalSessionId) return false;
    const response = await request("project.terminal.read", { sessionId: terminalSessionId });
    if (!response.ok) { onError(response.error?.message ?? "Terminal screen could not be read."); return false; }
    const result = response.result as { screen: string; exitCode?: number | null };
    setTerminalScreen(result.screen); setTerminalExitCode(result.exitCode ?? null); return true;
  };

  const closeTerminalSession = async () => {
    if (!terminalSessionId) return true;
    const response = await request("project.terminal.close", { sessionId: terminalSessionId });
    if (!response.ok) { onError(response.error?.message ?? "Terminal session could not be closed."); return false; }
    setTerminalSessionId(null); setTerminalScreen(null); setTerminalExitCode(null); return true;
  };

  return { projects, selectedProject, projectFiles, selectedFile, fileDraft, fileOriginal, fileSummary, fileBusy, fileUndoAvailable: Boolean(fileUndoId), terminalBusy, terminalOutput, terminalExitCode, terminalSessionId, terminalScreen, gitStatus, gitBusy, gitDiff, gitDiffBusy, gitCommitBusy, gitPushBusy, gitPullRequestBusy, pickProjectFolder, saveProject, selectProject, openProjectFile, closeProjectFile, setFileDraft, saveProjectFile, undoProjectFile, runTerminal, startTerminalSession, writeTerminalSession, refreshTerminalSession, closeTerminalSession, refreshGitStatus, loadGitDiff, commitGitChanges, pushGitChanges, createGitPullRequest };
}
