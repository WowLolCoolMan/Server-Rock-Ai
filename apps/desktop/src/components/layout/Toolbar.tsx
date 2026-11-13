import type { Route } from "../../app/types";
import { Button, StatusBadge } from "../ui/Primitives";

const titles: Record<Route, string> = { chat: "Chat", coding: "Coding workspace", settings: "Settings", models: "Models", images: "Image Studio", workbench: "Agent workbench", terminal: "Terminal", memory: "Memory" };

export function Toolbar({ route, connection, onBack, onForward, onSettings }: { route: Route; connection: "checking" | "ready" | "error"; onBack: () => void; onForward: () => void; onSettings: () => void }) {
  return <header className="app-toolbar"><div className="toolbar-history"><Button variant="ghost" aria-label="Go back" onClick={onBack}>‹</Button><Button variant="ghost" aria-label="Go forward" onClick={onForward}>›</Button></div><div className="toolbar-title"><p className="eyebrow">LOCAL WORKSPACE</p><h1>{titles[route]}</h1></div><div className="toolbar-actions"><StatusBadge tone={connection === "ready" ? "success" : connection === "error" ? "danger" : "warning"}>{connection === "ready" ? "Agent ready" : connection === "error" ? "Offline" : "Connecting"}</StatusBadge><Button variant="ghost" onClick={onSettings}>Settings</Button></div></header>;
}
