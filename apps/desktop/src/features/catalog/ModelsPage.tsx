import { useMemo, useState } from "react";
import type { Model } from "../../app/types";
import { Button, Card, EmptyState, PageHeader, StatusBadge } from "../../components/ui/Primitives";
import { NavIcon } from "../../components/ui/NavIcon";

function modelProvider(model: string) {
  const separator = model.indexOf("/");
  return separator > 0 ? model.slice(0, separator) : "Local runtime";
}

function modelVisionLabel(vision: string) {
  return vision.toLowerCase() === "supported" ? "Vision" : "Text";
}

export function ModelsPage({ models, onDiscover, onSelect }: { models: Model[]; onDiscover: () => void | Promise<boolean>; onSelect: (model: string) => void }) {
  const [query, setQuery] = useState("");
  const [discovering, setDiscovering] = useState(false);
  const [discoveryState, setDiscoveryState] = useState<"idle" | "success" | "error">("idle");
  const filtered = useMemo(() => models.filter((model) => `${model.model} ${model.capabilities.join(" ")}`.toLowerCase().includes(query.trim().toLowerCase())), [models, query]);
  const discover = async () => {
    if (discovering) return;
    setDiscovering(true);
    try {
      const result = await onDiscover();
      setDiscoveryState(result === false ? "error" : "success");
    } catch {
      setDiscoveryState("error");
    } finally {
      setDiscovering(false);
    }
  };
  const registryStatus = discovering
    ? { tone: "warning" as const, label: "Discovering" }
    : discoveryState === "error"
      ? { tone: "danger" as const, label: "Needs attention" }
      : models.length
        ? { tone: "success" as const, label: "Registry ready" }
        : { tone: "neutral" as const, label: "No models loaded" };

  return <div className="feature-page"><PageHeader eyebrow="MODEL LIBRARY" title="Choose your model" description="Choose models already running on this computer, or prepare a very large local model through AirLLM." actions={<><StatusBadge tone={registryStatus.tone}>{registryStatus.label}</StatusBadge><Button variant="primary" disabled={discovering} onClick={() => void discover()}><NavIcon name="refresh" />{discovering ? "Discovering…" : "Discover local models"}</Button></>} /><Card className="catalog-card"><div className="catalog-toolbar"><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search local models…" aria-label="Search local models" /><span>{filtered.length} available</span>{discoveryState === "error" && <span className="catalog-discovery-status" role="status">Last inventory shown; refresh failed.</span>}</div>{models.length === 0 ? <EmptyState title="No local models discovered" description="Start Ollama, LM Studio, or another compatible server on this computer, then discover its models. For a very large model, choose AirLLM in the local runtime settings." action={<Button variant="secondary" disabled={discovering} onClick={() => void discover()}><NavIcon name="refresh" />{discovering ? "Discovering…" : "Discover local models"}</Button>} /> : filtered.length === 0 ? <EmptyState title="No matching local models" description="Try a different model name or capability." action={<Button variant="secondary" onClick={() => setQuery("")}>Clear search</Button>} /> : <div className="catalog-list">{filtered.map((model) => <button className="catalog-row" key={model.model} onClick={() => onSelect(model.model)} type="button"><span className="catalog-icon"><NavIcon name="model" /></span><span className="catalog-row-copy"><strong>{model.model}</strong><small>{modelProvider(model.model)} · {modelVisionLabel(model.vision)} · {model.capabilities.join(" · ") || "Text generation"}</small></span><span className="catalog-row-meta"><StatusBadge tone="success">On this computer</StatusBadge><span className="catalog-action">Use model <NavIcon name="arrow-right" /></span></span></button>)}</div>}</Card></div>;
}
