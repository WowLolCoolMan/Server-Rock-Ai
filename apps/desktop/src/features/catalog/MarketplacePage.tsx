import { useEffect, useMemo, useState } from "react";
import type { AgentResponse } from "../../../shared/contracts";
import { Button, Card, EmptyState, PageHeader, StatusBadge } from "../../components/ui/Primitives";
import { NavIcon, type NavIconName } from "../../components/ui/NavIcon";
import { saveVoiceSetting, selectedVoiceSettings, speakNativeResponse, stopNativeSpeech, voiceModels, voiceProfiles } from "../../services/nativeTts";

type Category = "All" | "LLMs" | "Voices" | "Image models" | "Skills" | "Tools";
type CatalogCard = { title: string; category: Exclude<Category, "All">; description: string; detail: string; icon: NavIconName; status: string; statusTone: "neutral" | "success" | "warning"; availability: string; nextStep: string };
type MarketplaceModel = { id: string; repository: string; name?: string; source: string; kind: string; downloads: number; likes: number; uncensored: boolean; tags: string[]; sizeBytes?: number | null; installRoute: string; installable: boolean; files?: Array<{ path: string; sizeBytes: number; sha256?: string | null }> };
type MarketplaceCatalog = { providers: Array<{ id: string; name: string; kinds: string[]; remote: boolean }>; voices: Array<{ id: string; name: string; description: string; runtime: string; license: string; size: string; installed: boolean; lowResource: boolean; sourceUrl: string; installable: boolean }>; skills: Array<{ id: string; name: string; description: string; category: string; permissions: string[]; source: string; installed: boolean; enabled: boolean; installable: boolean }>; tools: Array<{ id: string; name: string; description: string; category: string; access: string; builtIn: boolean; status: string; source?: string | null; installable: boolean }> };

const cards: CatalogCard[] = [
  { title: "LLMs", category: "LLMs", description: "Search Hugging Face and ModelScope for local GGUF models.", detail: "Remote catalog discovery is separated from inference. Every result shows its provider, provenance, safety label, size when available, and the verified local install route.", icon: "model", status: "Remote discovery", statusTone: "success", availability: "Hugging Face and ModelScope are queried through the Agent Host.", nextStep: "Search, filter, inspect the source, then start a verified local download." },
  { title: "Voices", category: "Voices", description: "Configure OS-native and local voice profiles.", detail: "Voice metadata comes from the host catalog. OS-native speech works now; model profiles remain explicit until their runtime is installed.", icon: "activity", status: "Local ready", statusTone: "success", availability: "OS-native speech is available on this computer.", nextStep: "Choose a profile and preview it locally." },
  { title: "Image models", category: "Image models", description: "Review image runtimes and ComfyUI workflows.", detail: "Image execution remains local through ComfyUI. The marketplace shows the capability boundary rather than pretending remote checkpoints are already installed.", icon: "image", status: "Local workflow", statusTone: "warning", availability: "ComfyUI is supported when a loopback workflow is configured.", nextStep: "Configure a local workflow in Image Studio." },
  { title: "Skills", category: "Skills", description: "Use reviewed instruction packs with explicit permissions.", detail: "Built-in skills are visible with their permission scope. Remote GitHub skill installation will only appear once immutable source, manifest, digest, and rollback checks are available.", icon: "sparkle", status: "Built in", statusTone: "success", availability: "Core ships with reviewed read-only workflow skills.", nextStep: "Open a skill to inspect its prompt and permissions." },
  { title: "Tools", category: "Tools", description: "Inspect local integrations and MCP capability boundaries.", detail: "Tool metadata is sourced from the host catalog. Built-in tools are labeled with their actual entry point; unavailable connectors do not expose fake install actions.", icon: "terminal", status: "Host catalog", statusTone: "success", availability: "Local filesystem, terminal, GitHub, image, and MCP foundations are represented.", nextStep: "Review permissions before enabling a connector." },
];

function request(method: string, params?: Record<string, unknown>): Promise<AgentResponse> {
  return window.serverRockAgent?.request(method, params) ?? Promise.resolve({ v: 1, id: `marketplace-${method}`, ok: false, error: { code: "BRIDGE_UNAVAILABLE", message: "The Electron agent bridge is unavailable." } });
}

function formatSize(bytes?: number | null) {
  if (!bytes || bytes <= 0) return "Size unavailable";
  if (bytes >= 1024 ** 3) return `${(bytes / 1024 ** 3).toFixed(1)} GB`;
  return `${(bytes / 1024 ** 2).toFixed(0)} MB`;
}

function normalizeSource(value: string) {
  return value.toLowerCase().replace(/[^a-z]/g, "");
}

export function MarketplacePage() {
  const [query, setQuery] = useState("");
  const [category, setCategory] = useState<Category>("All");
  const [selectedTitle, setSelectedTitle] = useState(cards[0].title);
  const [voiceSettings, setVoiceSettings] = useState(selectedVoiceSettings);
  const [catalog, setCatalog] = useState<MarketplaceCatalog | null>(null);
  const [models, setModels] = useState<MarketplaceModel[]>([]);
  const [modelLoading, setModelLoading] = useState(false);
  const [modelError, setModelError] = useState<string | null>(null);
  const [uncensored, setUncensored] = useState(false);
  const [provider, setProvider] = useState("All providers");
  const [size, setSize] = useState("Any size");
  const [installing, setInstalling] = useState<string | null>(null);
  const [installStatus, setInstallStatus] = useState<string | null>(null);

  useEffect(() => {
    void request("marketplace.catalog").then((response) => {
      if (response.ok) setCatalog(response.result as MarketplaceCatalog);
      else setModelError(response.error?.message ?? "Marketplace catalog could not be loaded.");
    });
    void searchModels("");
  }, []);

  const filtered = useMemo(() => cards.filter((card) => {
    const matchesCategory = category === "All" || card.category === category;
    const haystack = `${card.title} ${card.description} ${card.detail}`.toLowerCase();
    return matchesCategory && haystack.includes(query.trim().toLowerCase());
  }), [category, query]);
  const selected = filtered.find((card) => card.title === selectedTitle) ?? filtered[0] ?? null;
  const filteredModels = useMemo(() => models.filter((model) => {
    const matchesProvider = provider === "All providers" || model.source === provider;
    const sizeLimit = size === "Any size" ? Number.POSITIVE_INFINITY : Number(size);
    const matchesSize = size === "Any size" || (model.sizeBytes != null && model.sizeBytes <= sizeLimit * 1024 ** 3);
    return matchesProvider && matchesSize;
  }), [models, provider, size]);

  async function searchModels(nextQuery = query, nextUncensored = uncensored) {
    setModelLoading(true);
    setModelError(null);
    const response = await request("marketplace.models.search", { query: nextQuery.trim(), uncensored: nextUncensored });
    setModelLoading(false);
    if (!response.ok) {
      setModelError(response.error?.message ?? "Remote model discovery failed.");
      return;
    }
    const result = response.result as { models?: MarketplaceModel[] };
    setModels(result.models ?? []);
  }

  async function installModel(model: MarketplaceModel) {
    const endpoint = localStorage.getItem("skai.endpoint") ?? "http://localhost:11434/v1";
    const source = normalizeSource(model.source);
    setInstalling(model.id);
    setInstallStatus("Starting verified local download…");
    const started = await request("marketplace.models.install.start", { source, repository: model.repository, name: model.name, endpoint, downloads: model.downloads, uncensored: model.uncensored, files: model.files });
    if (!started.ok) {
      setInstallStatus(started.error?.message ?? "The download could not start.");
      setInstalling(null);
      return;
    }
    const operationId = (started.result as { id?: string }).id;
    if (!operationId) {
      setInstallStatus("The host did not return a download operation.");
      setInstalling(null);
      return;
    }
    for (let attempt = 0; attempt < 600; attempt += 1) {
      await new Promise((resolve) => window.setTimeout(resolve, 750));
      const status = await request("marketplace.models.install.status", { id: operationId });
      if (!status.ok) {
        setInstallStatus(status.error?.message ?? "Download status was unavailable.");
        break;
      }
      const result = status.result as { status: string; progress?: string[]; error?: string };
      setInstallStatus(result.progress?.at(-1) ?? result.status);
      if (result.status === "completed" || result.status === "failed" || result.status === "cancelled") {
        if (result.error) setInstallStatus(result.error);
        break;
      }
    }
    setInstalling(null);
  }

  const updateVoice = (key: "profileId" | "primaryModelId" | "lowResourceModelId" | "lowResourceMode" | "voiceOutputEnabled", value: string | boolean) => { saveVoiceSetting(key, value); setVoiceSettings(selectedVoiceSettings()); };

  return <div className="feature-page marketplace-page">
    <PageHeader eyebrow="MARKETPLACE" title="Extend Skai" description="Search verified providers, inspect provenance, and install only through host-backed local adapters." />
    <Card className="marketplace-browser">
      <div className="marketplace-toolbar"><div className="marketplace-search-row"><label className="sr-only" htmlFor="marketplace-search">Search marketplace</label><input id="marketplace-search" value={query} onChange={(event) => setQuery(event.target.value)} onKeyDown={(event) => { if (event.key === "Enter") void searchModels(); }} placeholder="Search models, voices, skills, and tools…" /><Button variant="secondary" disabled={modelLoading} onClick={() => void searchModels()}>{modelLoading ? "Searching…" : "Search providers"}</Button><span>{filtered.length} categories · {models.length} models</span></div><div className="marketplace-filters" role="group" aria-label="Marketplace categories">{(["All", "LLMs", "Voices", "Image models", "Skills", "Tools"] as Category[]).map((item) => <button className={`marketplace-filter${category === item ? " is-active" : ""}`} key={item} type="button" aria-pressed={category === item} onClick={() => setCategory(item)}>{item}</button>)}</div></div>
      {category === "LLMs" || category === "All" ? <div className="marketplace-model-controls"><label>Provider<select value={provider} onChange={(event) => setProvider(event.target.value)}><option>All providers</option><option>Hugging Face</option><option>ModelScope</option></select></label><label>Maximum size<select value={size} onChange={(event) => setSize(event.target.value)}><option>Any size</option><option value="8">8 GB</option><option value="14">14 GB</option><option value="24">24 GB</option><option value="40">40 GB</option></select></label><label className="settings-toggle"><span><strong>Include abliterated</strong><small>Only show explicit uncensored/abliterated labels.</small></span><input type="checkbox" checked={uncensored} onChange={(event) => { setUncensored(event.target.checked); void searchModels(query, event.target.checked); }} /></label></div> : null}
      <div className="marketplace-layout"><div className="marketplace-grid" aria-label="Marketplace categories">{filtered.map((card) => <button className={`marketplace-card${selected?.title === card.title ? " is-selected" : ""}`} key={card.title} type="button" onClick={() => setSelectedTitle(card.title)} aria-pressed={selected?.title === card.title}><span className="marketplace-card-heading"><span className="marketplace-icon"><NavIcon name={card.icon} /></span><StatusBadge tone={card.statusTone}>{card.status}</StatusBadge></span><strong>{card.title}</strong><span>{card.description}</span><small>View capability details →</small></button>)}{(category === "LLMs" || category === "All") && <Card className="marketplace-remote-results"><div className="panel-heading"><div><p className="section-kicker">REMOTE MODEL SOURCES</p><h3>Local-ready models</h3></div><StatusBadge tone={modelError ? "warning" : "success"}>{modelLoading ? "Polling" : modelError ? "Partial" : "Live catalog"}</StatusBadge></div>{modelError && <p className="marketplace-honesty" role="status">{modelError}</p>}{filteredModels.length === 0 ? <EmptyState title={modelLoading ? "Polling providers…" : "No matching models"} description="Change the search or filters. Unknown sizes are hidden only when a maximum size is selected." /> : <div className="marketplace-model-list">{filteredModels.slice(0, 36).map((model) => <article className="marketplace-model-row" key={model.id}><div><strong>{model.repository}</strong><small>{model.source} · {formatSize(model.sizeBytes)} · {model.downloads.toLocaleString()} downloads · {model.tags.slice(0, 3).join(" · ") || "GGUF"}</small></div><div className="marketplace-model-actions">{model.uncensored && <StatusBadge tone="warning">Abliterated label</StatusBadge>}<Button variant="secondary" disabled={!model.installable || installing === model.id} onClick={() => void installModel(model)}>{installing === model.id ? "Installing…" : "Install locally"}</Button></div></article>)}</div>}</Card>}</div>{selected && <Card className="marketplace-detail"><div className="marketplace-detail-heading"><div><p className="section-kicker">SELECTED CAPABILITY</p><h3>{selected.title}</h3></div><StatusBadge tone={selected.statusTone}>{selected.status}</StatusBadge></div><p>{selected.detail}</p><dl className="marketplace-detail-list"><div><dt>Availability</dt><dd>{selected.availability}</dd></div><div><dt>Next step</dt><dd>{selected.nextStep}</dd></div></dl>{selected.category === "Voices" ? <div className="marketplace-voice-controls"><label>Primary conversational voice<select value={voiceSettings.primaryModelId} onChange={(event) => updateVoice("primaryModelId", event.target.value)}>{voiceModels.map((voice) => <option key={voice.id} value={voice.id}>{voice.name} · {voice.size}</option>)}</select></label><label>Low-resource mode voice<select value={voiceSettings.lowResourceModelId} onChange={(event) => updateVoice("lowResourceModelId", event.target.value)}>{voiceModels.map((voice) => <option key={voice.id} value={voice.id}>{voice.name} · {voice.size}</option>)}</select></label><label>Voice profile<select value={voiceSettings.profileId} onChange={(event) => updateVoice("profileId", event.target.value)}>{voiceProfiles.map((profile) => <option key={profile.id} value={profile.id}>{profile.name} · {profile.description}</option>)}</select></label><label className="settings-toggle"><span><strong>Speak completed replies</strong><small>Voice output stays off until you turn it on.</small></span><input type="checkbox" checked={voiceSettings.voiceOutputEnabled} onChange={(event) => updateVoice("voiceOutputEnabled", event.target.checked)} /></label><label className="settings-toggle"><span><strong>Use low-resource mode</strong><small>Use the separately assigned voice model on constrained laptops.</small></span><input type="checkbox" checked={voiceSettings.lowResourceMode} onChange={(event) => updateVoice("lowResourceMode", event.target.checked)} /></label><div className="marketplace-voice-actions"><Button variant="secondary" onClick={() => void speakNativeResponse("Skai voice preview. This uses the operating system voice service.", true)}>Preview OS voice</Button><Button variant="ghost" onClick={stopNativeSpeech}>Stop preview</Button></div></div> : selected.category === "Skills" ? <div className="marketplace-detail-list">{(catalog?.skills ?? []).map((skill) => <div key={skill.id}><dt>{skill.name}</dt><dd>{skill.description} · {skill.permissions.join(", ") || "No extra permissions"}</dd></div>)}</div> : selected.category === "Tools" ? <div className="marketplace-detail-list">{(catalog?.tools ?? []).slice(0, 8).map((tool) => <div key={tool.id}><dt>{tool.name}</dt><dd>{tool.status} · {tool.access}</dd></div>)}</div> : <p className="marketplace-honesty">{installStatus ?? "Provider results and install progress stay visible here and are never presented as complete until the host confirms them."}</p>}</Card>}</div>
    </Card>
  </div>;
}
