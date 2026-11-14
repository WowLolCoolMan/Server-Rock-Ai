export const AGENT_PROTOCOL_VERSION = 1 as const;

export type AgentRequest = {
  v: typeof AGENT_PROTOCOL_VERSION;
  id: string;
  method: string;
  params?: Record<string, unknown>;
};

export type AgentResponse = {
  v: typeof AGENT_PROTOCOL_VERSION;
  id: string;
  ok: boolean;
  result?: unknown;
  error?: { code: string; message: string };
};

export type RuntimeHealth = {
  service: "agent-host";
  protocolVersion: typeof AGENT_PROTOCOL_VERSION;
  status: "ready";
  capabilities: string[];
};

export type DesktopProject = {
  id: string;
  name: string;
  instructions: string;
  folderPaths: string[];
  gitHubRepository?: string | null;
  updatedAt: string;
};

export type DesktopProjectFile = {
  relativePath: string;
  bytes: number;
};

export type DesktopGitChange = {
  status: string;
  path: string;
  originalPath?: string | null;
};

export type DesktopGitStatus = {
  branch: string;
  repository: string;
  commitsAhead: number;
  commitsBehind: number;
  hasConflicts: boolean;
  changes: DesktopGitChange[];
};

export type DesktopGitDiff = {
  repository: string;
  text: string;
  includedFiles: number;
  redactedFiles: number;
  truncated: boolean;
};

export type DesktopConversation = {
  id: string;
  title: string;
  messages: Array<{ role: "user" | "assistant" | "system"; content: string; attachments?: ChatAttachment[] }>;
  projectId?: string | null;
  projectName?: string | null;
  includeProjectFilesInChat?: boolean;
  updatedAt: string;
  pinned: boolean;
  archived: boolean;
  tags: string[];
  folderName?: string | null;
};

export type ChatAttachment = { fileName: string; content: string; mediaType?: string | null };

export type DesktopConversationSearchResult = DesktopConversation & {
  display: string;
};

export type DesktopTask = {
  id: string;
  title: string;
  instruction: string;
  role: string;
  status: string;
  resultSummary?: string | null;
  artifactLabels?: string[];
  dependsOn: string[];
  updatedAt: string;
};

export type TaskEnvironment = "local" | "worktree";

export type DesktopScheduledTask = {
  id: string;
  prompt: string;
  workspaceMode: string;
  dueAt: string;
  repeatEveryMinutes?: number | null;
  status: string;
  outcome?: string | null;
};

export type ScheduledTaskDraft = {
  prompt: string;
  dueAt: string;
  repeatEveryMinutes?: number | null;
  workspaceMode?: "chat" | "coding-agent";
};

export type DesktopQueuedMessage = {
  index: number;
  text: string;
  attachments?: Array<{ fileName: string; mediaType?: string | null }>;
};

export type PetMotionSettings = {
  wander: boolean;
  reduceMotion: boolean;
};

export type PetCompletionEvent = {
  text: string;
  at: number;
  kind: "success" | "error";
};

export interface PetBridge {
  setVisible(visible: boolean): void;
  setMotion(settings: PetMotionSettings): void;
  beginDrag(point: { x: number; y: number }): void;
  moveDrag(point: { x: number; y: number }): void;
  endDrag(): void;
  hide(): void;
  onMessage(handler: (event: PetCompletionEvent) => void): () => void;
  onVisibilityChange(handler: (visible: boolean) => void): () => void;
  onMotionChange(handler: (settings: PetMotionSettings) => void): () => void;
}

export interface AgentBridge {
  request(method: string, params?: Record<string, unknown>): Promise<AgentResponse>;
  pet?: PetBridge;
}

declare global {
  interface Window {
    serverRockAgent: AgentBridge;
  }
}
