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

export interface AgentBridge {
  request(method: string, params?: Record<string, unknown>): Promise<AgentResponse>;
}

declare global {
  interface Window {
    serverRockAgent: AgentBridge;
  }
}
