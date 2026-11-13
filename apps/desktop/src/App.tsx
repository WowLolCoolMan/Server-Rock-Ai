import { useEffect, useState } from "react";
import type { AgentResponse, RuntimeHealth } from "../shared/contracts";
import { PRODUCT_DISPLAY_NAME } from "../shared/productIdentity";

type ConnectionState = "checking" | "ready" | "error";

export default function App() {
  const [connection, setConnection] = useState<ConnectionState>("checking");
  const [health, setHealth] = useState<RuntimeHealth | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    document.title = PRODUCT_DISPLAY_NAME;
    window.serverRockAgent
      .request("runtime.health")
      .then((response: AgentResponse) => {
        if (!response.ok) {
          setConnection("error");
          setError(response.error?.message ?? "Agent host returned an unknown error.");
          return;
        }
        setHealth(response.result as RuntimeHealth);
        setConnection("ready");
      })
      .catch((requestError: unknown) => {
        setConnection("error");
        setError(requestError instanceof Error ? requestError.message : "Agent host request failed.");
      });
  }, []);

  return (
    <div className="app-shell">
      <aside className="sidebar" aria-label="Primary navigation">
        <div className="brand-lockup">
          <div className="brand-mark" aria-hidden="true">SR</div>
          <div>
            <p className="eyebrow">WORKSPACE</p>
            <h1>{PRODUCT_DISPLAY_NAME}</h1>
          </div>
        </div>
        <nav className="nav-stack">
          <button className="nav-item active" type="button" aria-current="page">⌁ <span>Chat</span></button>
          <button className="nav-item" type="button">◈ <span>Projects</span></button>
          <button className="nav-item" type="button">◇ <span>Agent workbench</span></button>
          <button className="nav-item" type="button">◌ <span>Models</span></button>
        </nav>
        <div className="sidebar-footer">
          <div className={`status-dot ${connection}`} aria-label={`Agent host ${connection}`} />
          <span>{connection === "ready" ? "Agent host connected" : connection === "checking" ? "Connecting…" : "Agent host unavailable"}</span>
        </div>
      </aside>

      <main className="main-canvas">
        <header className="topbar">
          <div>
            <p className="eyebrow">DESKTOP PROTOTYPE · STAGE 1</p>
            <h2>One workspace for every model and agent.</h2>
          </div>
          <button className="ghost-button" type="button">Settings</button>
        </header>

        <section className="hero-card" aria-labelledby="hero-title">
          <div className="hero-copy">
            <span className="pill">Electron + React shell</span>
            <h3 id="hero-title">A durable execution core behind a fast, expressive desktop.</h3>
            <p>Stage 1 establishes the renderer boundary without replacing the working native app. The next slice will connect real model discovery and chat streaming through this host.</p>
            <div className="action-row">
              <button className="primary-button" type="button">Start a conversation</button>
              <button className="secondary-button" type="button">Open project</button>
            </div>
          </div>
          <div className="signal-card" aria-label="Agent host health">
            <span className="signal-label">RUNTIME SIGNAL</span>
            <strong>{connection === "ready" ? "READY" : connection.toUpperCase()}</strong>
            <span>{health ? `${health.service} · protocol v${health.protocolVersion}` : error ?? "Waiting for the local agent host"}</span>
            {health && <div className="capability-list">{health.capabilities.map((capability) => <span key={capability}>{capability}</span>)}</div>}
          </div>
        </section>

        <section className="workspace-grid" aria-label="Upcoming workspace surfaces">
          <article className="surface-card"><span className="card-icon">⌁</span><h4>Chat</h4><p>Streaming conversations with explicit model and privacy state.</p></article>
          <article className="surface-card"><span className="card-icon">◇</span><h4>Agent workbench</h4><p>Plans, approvals, artifacts and recovery independent of the renderer.</p></article>
          <article className="surface-card"><span className="card-icon">◌</span><h4>Media engines</h4><p>Voice and image workloads can share a controlled runtime scheduler.</p></article>
        </section>
      </main>
    </div>
  );
}
