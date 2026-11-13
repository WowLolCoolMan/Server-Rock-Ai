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
  role: string;
  status: string;
  resultSummary?: string | null;
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

export type DesktopQueuedMessage = {
  index: number;
  text: string;
  attachments?: Array<{ fileName: string; mediaType?: string | null }>;
};

export interface AgentBridge {
  request(method: string, params?: Record<string, unknown>): Promise<AgentResponse>;
}

declare global {
  interface Window {
    serverRockAgent: AgentBridge;
  }
}
