import type { ChatAttachment, RuntimeHealth } from "../../shared/contracts";

export type ConnectionState = "checking" | "ready" | "error";
export type Route = "chat" | "coding" | "settings" | "models" | "images" | "marketplace" | "workbench" | "terminal" | "memory";
export type View = Route;
export type Message = { role: "user" | "assistant"; content: string; attachments?: ChatAttachment[] };
export type Model = { model: string; vision: string; capabilities: string[] };
export type MemoryEntry = { id: string; scope: string; content: string; source: string; approvalId: string; tags: string[]; enabled: boolean; updatedAtUtc: string };

export type AppHealth = RuntimeHealth | null;
