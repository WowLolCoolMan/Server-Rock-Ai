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
  messages: Array<{ role: "user" | "assistant" | "system"; content: string }>;
  updatedAt: string;
  pinned: boolean;
  tags: string[];
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

export type DesktopScheduledTask = {
  id: string;
  prompt: string;
  workspaceMode: string;
  dueAt: string;
  repeatEveryMinutes?: number | null;
  status: string;
  outcome?: string | null;
};

export interface AgentBridge {
  request(method: string, params?: Record<string, unknown>): Promise<AgentResponse>;
}

declare global {
  interface Window {
    serverRockAgent: AgentBridge;
  }
}
