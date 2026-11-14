import type { Route } from "../../app/types";

export type ComposerCommand = { id: string; label: string; detail: string; shortcut: string; route?: Route; opensTaskPlanner?: boolean };

export const composerCommands: ComposerCommand[] = [
  { id: "goal", label: "Plan local work", detail: "Open a review-first task brief", shortcut: "/goal", opensTaskPlanner: true },
  { id: "coding", label: "Open Coding Workspace", detail: "Projects, files, terminal, and edits", shortcut: "/coding", route: "coding" },
  { id: "workbench", label: "Open Agent Workbench", detail: "Review local task progress and recovery", shortcut: "/workbench", route: "workbench" },
  { id: "models", label: "Open Models", detail: "Discover and select a local model", shortcut: "/models", route: "models" },
  { id: "images", label: "Open Image Studio", detail: "Create with a local workflow", shortcut: "/images", route: "images" },
  { id: "marketplace", label: "Open Marketplace", detail: "Review local voices, skills, and tools", shortcut: "/marketplace", route: "marketplace" },
  { id: "memory", label: "Open Memory", detail: "Review approved local memory", shortcut: "/memory", route: "memory" },
  { id: "settings", label: "Open Settings", detail: "Appearance and workspace preferences", shortcut: "/settings", route: "settings" },
];
