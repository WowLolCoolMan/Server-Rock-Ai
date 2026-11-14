import { useEffect, useState } from "react";
import type { DesktopProject, DesktopProjectFile } from "../../../shared/contracts";
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
    setSelectedProject(project); setSelectedFile(null); setFileDraft(""); setFileOriginal(""); setFileHash(null); setFileUndoId(null);
    const response = await request("project.files", { id: project.id });
    if (!response.ok) { onError(response.error?.message ?? "Project browser failed."); return; }
    const result = response.result as { files: DesktopProjectFile[]; protectedFilesSkipped: number; hitEntryLimit: boolean };
    setProjectFiles(result.files ?? []);
    setFileSummary(`${result.files.length} readable files · ${result.protectedFilesSkipped} protected names hidden${result.hitEntryLimit ? " · entry limit reached" : ""}`);
    onNavigate("coding");
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

  return { projects, selectedProject, projectFiles, selectedFile, fileDraft, fileOriginal, fileSummary, fileBusy, terminalBusy, terminalOutput, terminalExitCode, pickProjectFolder, saveProject, selectProject, openProjectFile, closeProjectFile, setFileDraft, saveProjectFile, undoProjectFile, runTerminal };
}
