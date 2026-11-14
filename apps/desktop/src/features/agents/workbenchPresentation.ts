export const workbenchGroups = [
  { key: "proposed", label: "Proposed" },
  { key: "queued", label: "Queued" },
  { key: "running", label: "Running" },
  { key: "verifying", label: "Verifying" },
  { key: "awaiting_approval", label: "Awaiting approval" },
  { key: "blocked", label: "Blocked" },
  { key: "completed", label: "Completed" },
  { key: "failed", label: "Failed" },
  { key: "cancelled", label: "Cancelled" },
] as const;

export type WorkbenchTaskTransition = (id: string, operation: "queue" | "approve" | "deny" | "cancel" | "retry") => void;

export function nextStepFor(status: string) {
  switch (status) {
    case "proposed": return { label: "Ready to queue", description: "Review the brief, then queue this local task when you are ready for execution." };
    case "queued": return { label: "Ready for local execution", description: "This task is queued for the local model. Run it when you are ready; no cloud model or remote execution is used." };
    case "running": return { label: "Execution in progress", description: "The local agent is working. Results will appear here when the host reports completion." };
    case "verifying": return { label: "Verifying result", description: "The local host is checking that the result and artifact record are complete before marking the task done." };
    case "awaiting_approval": return { label: "Approval required", description: "Review the task brief before allowing the local executor to start." };
    case "blocked": return { label: "Retry is available", description: "Resolve the blocking condition, then retry to place this task back in the local queue." };
    case "failed": return { label: "Review and re-plan", description: "The current host does not expose failed-task retry. Review the result and create a new local plan from Chat or Coding Workspace." };
    case "completed": return { label: "Task complete", description: "The recorded result is preserved here for review." };
    case "cancelled": return { label: "Task cancelled", description: "No work will run for this task unless you create a new local plan." };
    default: return { label: "Review task status", description: "The local host has not reported a next action for this task yet." };
  }
}

export function toneFor(status: string) {
  if (status === "failed") return "danger" as const;
  if (status === "blocked" || status === "awaiting_approval") return "warning" as const;
  if (status === "completed") return "success" as const;
  return "neutral" as const;
}

export function labelFor(status: string) {
  return status.replaceAll("_", " ");
}

export function timeLabel(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.valueOf()) ? "Updated time unavailable" : `Updated ${date.toLocaleString()}`;
}
