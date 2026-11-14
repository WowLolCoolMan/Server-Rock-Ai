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
  protectedPath: boolean;
  conflict: boolean;
};

export type DesktopGitStatus = {
  branch: string;
  repository: string;
  commitsAhead: number;
  commitsBehind: number;
  hasConflicts: boolean;
  changes: DesktopGitChange[];
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
  projectId?: string | null;
  environment: TaskEnvironment;
  status: string;
  resultSummary?: string | null;
  dependsOn: string[];
  updatedAt: string;
  artifactLabels?: string[];
  workspaceChanges?: DesktopTaskWorkspaceChange[];
  workspaceCommands?: DesktopTaskWorkspaceCommand[];
  worktreePath?: string | null;
  worktreeBranch?: string | null;
  worktreeRepositoryRoot?: string | null;
};

export type DesktopTaskWorkspaceChange = { relativePath: string; content: string };
export type DesktopTaskWorkspaceCommand = { relativeWorkingDirectory: string; command: string; timeoutSeconds: number };

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

export type PetActivity = "idle" | "walking" | "thinking" | "coding" | "working" | "success" | "error";
export type PetAssetKind = "sprite" | "manifest";
export type PetPosition = { x: number; y: number };
export type PetManifest = {
  frameWidth: number;
  frameHeight: number;
  columns: number;
  rows: number;
  states: Record<string, { row: number; frames: number[]; fps: number }>;
};
export type PetSettings = {
  visible: boolean;
  wander: boolean;
  reduceMotion: boolean;
  petId: string;
  spritePath: string;
  manifestPath: string;
  position: PetPosition;
};
export type PetWindowConfig = PetSettings & {
  activity: PetActivity;
  alwaysOnTop: true;
  customSpriteDataUrl?: string;
  customManifest?: PetManifest;
};

export type PetAssetSelection = { path: string; fileName: string };

export interface PetBridge {
  configure(settings: PetSettings): void;
  setActivity(activity: PetActivity): void;
  getConfig(): Promise<PetWindowConfig>;
  onConfig(listener: (config: PetWindowConfig) => void): () => void;
  onPosition(listener: (position: PetPosition) => void): () => void;
  onVisibilityChange(listener: (visible: boolean) => void): () => void;
  pickAsset(kind: PetAssetKind): Promise<PetAssetSelection | null>;
  setPosition(position: PetPosition): void;
  dragStart(position: { screenX: number; screenY: number }): void;
  dragMove(position: { screenX: number; screenY: number }): void;
  dragEnd(): void;
  hide(): void;
}

export interface AgentBridge {
  request(method: string, params?: Record<string, unknown>): Promise<AgentResponse>;
  pet: PetBridge;
}

declare global {
  interface Window {
    serverRockAgent: AgentBridge;
  }
}
