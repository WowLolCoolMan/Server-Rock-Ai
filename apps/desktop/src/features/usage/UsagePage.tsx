import { Card, EmptyState, PageHeader, StatusBadge } from "../../components/ui/Primitives";

export function UsagePage() {
  return <div className="feature-page usage-page">
    <PageHeader eyebrow="LOCAL RUNTIME" title="Usage" description="Review runtime activity recorded by Skai on this computer." />
    <div className="usage-grid">
      <Card className="usage-summary"><div className="panel-heading"><div><p className="section-kicker">MEASUREMENTS</p><h3>Local-only accounting</h3></div><StatusBadge tone="neutral">No telemetry</StatusBadge></div><EmptyState title="Usage metrics are not available yet" description="The current Agent Host records task and operation provenance, but it does not expose verified token, latency, GPU-memory, or cost measurements to the Electron workspace." /></Card>
      <Card className="usage-boundary"><p className="section-kicker">BOUNDARY</p><h3>What Skai can show</h3><p>When the host exposes measured runtime data, this page will show it here. Until then, Skai deliberately avoids estimating tokens, dollars, or hardware usage from incomplete signals.</p><div className="usage-list"><span><b>Stored locally</b> Operation status and provider provenance.</span><span><b>Not inferred</b> Token counts, cost, GPU memory, or latency.</span><span><b>Cloud-safe</b> No telemetry is sent by this page.</span></div></Card>
    </div>
  </div>;
}
