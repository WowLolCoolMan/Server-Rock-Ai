import type { DesktopProject } from "../../../shared/contracts";
import { PageHeader } from "../../components/ui/Primitives";
import { TerminalPanel } from "./TerminalPanel";

export function TerminalPage({ projects, initialProjectId, busy, output, exitCode, onRun }: { projects: DesktopProject[]; initialProjectId?: string | null; busy: boolean; output: string | null; exitCode: number | null; onRun: (projectId: string, command: string, cwd: string) => void }) {
  return <div className="feature-page"><PageHeader eyebrow="PROJECT TERMINAL" title="Run with intent" description="Commands run only inside a selected project and remain explicitly user-triggered." /><TerminalPanel projects={projects} initialProjectId={initialProjectId} busy={busy} output={output} exitCode={exitCode} onRun={onRun} /></div>;
}
