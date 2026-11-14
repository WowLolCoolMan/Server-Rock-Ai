import type { DesktopProject } from "../../../shared/contracts";
import { PageHeader } from "../../components/ui/Primitives";
import { TerminalPanel } from "./TerminalPanel";

export function TerminalPage({ projects, initialProjectId, busy, output, exitCode, sessionId, screen, onRun, onStartSession, onWriteSession, onRefreshSession, onCloseSession, onOpenCoding }: { projects: DesktopProject[]; initialProjectId?: string | null; busy: boolean; output: string | null; exitCode: number | null; sessionId: string | null; screen: string | null; onRun: (projectId: string, command: string, cwd: string) => void; onStartSession: (projectId: string, cwd: string) => Promise<boolean>; onWriteSession: (text: string) => Promise<boolean>; onRefreshSession: () => Promise<boolean>; onCloseSession: () => Promise<boolean>; onOpenCoding: () => void }) {
  return <div className="feature-page"><PageHeader eyebrow="PROJECT TERMINAL" title="Run with intent" description="Commands run only inside a selected project and remain explicitly user-triggered." /><TerminalPanel projects={projects} initialProjectId={initialProjectId} busy={busy} output={output} exitCode={exitCode} sessionId={sessionId} screen={screen} onRun={onRun} onStartSession={onStartSession} onWriteSession={onWriteSession} onRefreshSession={onRefreshSession} onCloseSession={onCloseSession} onOpenCoding={onOpenCoding} /></div>;
}
