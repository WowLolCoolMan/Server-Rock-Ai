import type { AgentResponse } from "../../shared/contracts";

export type AgentRequest = (method: string, params?: Record<string, unknown>) => Promise<AgentResponse>;

export function createAgentClient(request: AgentRequest) {
  return {
    request,
    async get<T>(method: string, params?: Record<string, unknown>) {
      return request(method, params) as Promise<AgentResponse & { result?: T }>;
    },
  };
}
