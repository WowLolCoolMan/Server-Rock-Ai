import { useEffect, useMemo, useState } from "react";
import type { Model } from "../../app/types";
import { Button, Card, EmptyState, PageHeader, StatusBadge } from "../../components/ui/Primitives";
import { saveVoiceSetting, selectedVoiceSettings, speakNativeResponse, stopNativeSpeech, voiceModels, voiceProfiles } from "../../services/nativeTts";

function modelProvider(model: string) {
  const separator = model.indexOf("/");
  return separator > 0 ? model.slice(0, separator) : "Local runtime";
}

function modelVisionLabel(vision: string) {
  return vision.toLowerCase() === "supported" ? "Vision" : "Text";
}

type MarketplaceModel = { id: string; kind: string; provider: string; task: string; downloads: number; likes: number; sizeBytes: number; quantizations: string[]; uncensored: boolean; tags: string[]; url: string };
type MarketplaceSkill = { id: string; name: string; description: string; stars: number; url: string; defaultBranch: string; lane: string; laneDescription: string };
type MarketplaceTool = { id: string; name: string; category: string; description: string; access: string; status: string; builtIn: boolean; whereToFind?: string | null };
type MarketplaceMcp = { id: string; name: string; transport: string; description: string; source: string; status: string };
type InstalledSkill = { id: string; name: string; version: string; publisher: string; sourceRef: string; packageSha256: string; requiredPermissions: string[]; enabled: boolean; canRollback: boolean };
type SkillPreview = { id: string; name: string; version: string; publisher: string; packageSha256: string; requiredPermissions: string[]; isUpdate: boolean; existingVersion?: string | null; existingPackageSha256?: string | null; sourceRef: string };
type SkillInspection = InstalledSkill & { instructions: string };
type RemoteSkillDetails = {
  id: string;
  name: string;
  publisher: string;
  description: string;
  stars: number;
  forks: number;
  sourceUrl: string;
  defaultBranch: string;
  revision: string;
  license?: string | null;
  skillMarkdown?: string | null;
  manifest?: Record<string, unknown> | null;
  manifestStatus: string;
  skillStatus: string;
  dependencies: string[];
  declaredTools: string[];
  requiredPermissions: string[];
  compatibility: string[];
  remoteInstallAvailable: boolean;
  installBoundary: string;
};

export function ModelsPage({ models, onDiscover, onSelect }: { models: Model[]; onDiscover: () => void; onSelect: (model: string) => void }) {
  const [query, setQuery] = useState(""); const filtered = useMemo(() => models.filter((model) => `${model.model} ${model.capabilities.join(" ")}`.toLowerCase().includes(query.trim().toLowerCase())), [models, query]);
  return <div className="feature-page"><PageHeader eyebrow="MODEL LIBRARY" title="Choose your model" description="Choose models already running on this computer, or prepare a very large local model through AirLLM." actions={<><StatusBadge tone={models.length ? "success" : "neutral"}>{models.length ? "Registry ready" : "No models loaded"}</StatusBadge><Button variant="primary" onClick={onDiscover}>↻ Discover local models</Button></>} /><Card className="catalog-card"><div className="catalog-toolbar"><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search local models…" aria-label="Search local models" /><span>{filtered.length} available</span></div>{models.length === 0 ? <EmptyState title="No local models discovered" description="Start Ollama, LM Studio, or another compatible server on this computer, then discover its models. For a very large model, choose AirLLM in the local runtime settings." action={<Button variant="secondary" onClick={onDiscover}>Discover local models</Button>} /> : filtered.length === 0 ? <EmptyState title="No matching local models" description="Try a different model name or capability." /> : <div className="catalog-list">{filtered.map((model) => <button className="catalog-row" key={model.model} onClick={() => onSelect(model.model)} type="button"><span className="catalog-icon">◌</span><span className="catalog-row-copy"><strong>{model.model}</strong><small>{modelProvider(model.model)} · {modelVisionLabel(model.vision)} · {model.capabilities.join(" · ") || "Text generation"}</small></span><span className="catalog-row-meta"><StatusBadge tone="success">On this computer</StatusBadge><span className="catalog-action">Use model →</span></span></button>)}</div>}</Card></div>;
}

export function ImagesPage({ endpoint, prompt, workflow, busy, images, onChange, onGenerate }: { endpoint: string; prompt: string; workflow: string; busy: boolean; images: Array<{ url: string; fileName: string }>; onChange: (key: "endpoint" | "prompt" | "workflow", value: string) => void; onGenerate: () => void }) {
  const promptRecipes = [
    { title: "Product scene", prompt: "A clean studio product photograph with soft directional light, subtle shadows, and a refined editorial backdrop." },
    { title: "Character portrait", prompt: "A cinematic character portrait with expressive lighting, detailed materials, and a calm atmospheric background." },
    { title: "Concept landscape", prompt: "A wide concept-art landscape with layered depth, dramatic natural light, and a strong visual focal point." }
  ];
  const readiness = busy
    ? { label: "Generating", tone: "warning" as const, detail: "ComfyUI is processing the current workflow." }
    : !endpoint.trim()
      ? { label: "Endpoint required", tone: "warning" as const, detail: "Add the address of your local ComfyUI instance." }
      : !prompt.trim()
        ? { label: "Prompt required", tone: "warning" as const, detail: "Describe the image you want to create." }
        : !workflow.trim()
          ? { label: "Workflow required", tone: "warning" as const, detail: "Paste a workflow JSON payload before generating." }
          : { label: "Ready", tone: "success" as const, detail: "The local image bridge has everything it needs to run." };

  return <div className="feature-page"><PageHeader eyebrow="IMAGE STUDIO" title="Create visual work" description="A dedicated surface for local image generation, separated from conversation controls." actions={<StatusBadge tone={readiness.tone}>{readiness.label}</StatusBadge>} /><div className="image-studio-layout"><Card className="form-card image-editor-card"><div className="studio-panel-heading"><div><p className="section-kicker">LOCAL IMAGE WORKFLOW</p><h3>Build a generation</h3></div><span className="studio-bridge-label">COMFYUI BRIDGE</span></div><div className="form-grid"><label>ComfyUI endpoint<input value={endpoint} onChange={(event) => onChange("endpoint", event.target.value)} placeholder="http://127.0.0.1:8188" aria-describedby="image-endpoint-help" /><small id="image-endpoint-help">The local address where ComfyUI is listening.</small></label><label>Prompt<textarea value={prompt} onChange={(event) => onChange("prompt", event.target.value)} placeholder="Describe what you want to create…" rows={4} aria-describedby="image-prompt-help" /><small id="image-prompt-help">Use a recipe below as a starting point, then make it yours.</small></label><div className="prompt-recipes" aria-label="Prompt recipes"><span className="field-caption">START WITH A RECIPE</span><div>{promptRecipes.map((recipe) => <Button key={recipe.title} variant="ghost" className="prompt-recipe" onClick={() => onChange("prompt", recipe.prompt)}>{recipe.title}</Button>)}</div></div><label>Workflow JSON<textarea value={workflow} onChange={(event) => onChange("workflow", event.target.value)} placeholder='{"prompt":"{{prompt}}"}' rows={6} aria-describedby="image-workflow-help" /><small id="image-workflow-help">Use a ComfyUI API workflow with <code>{"{{prompt}}"}</code> where the prompt should be inserted.</small></label></div><div className="studio-readiness"><StatusBadge tone={readiness.tone}>{readiness.label}</StatusBadge><span>{readiness.detail}</span></div><div className="form-footer"><span>Outputs are visible for this session; durable history is not claimed.</span><Button variant="primary" disabled={busy || !endpoint.trim() || !prompt.trim() || !workflow.trim()} onClick={onGenerate}>{busy ? "Generating…" : "Generate image"}</Button></div></Card><Card className="image-history"><div className="panel-heading"><div><p className="section-kicker">GENERATION HISTORY</p><h3>Recent outputs</h3></div><span className="panel-count">{images.length}</span></div>{images.length === 0 ? <EmptyState title="No generated images" description="Your local outputs will appear here after a successful generation." /> : <div className="image-gallery">{images.map((image) => <figure key={image.url}><img src={image.url} alt={image.fileName} /><figcaption>{image.fileName}</figcaption></figure>)}</div>}</Card></div></div>;
}

export function MarketplacePage({ onNavigate }: { onNavigate: (route: "chat" | "coding" | "models" | "images" | "workbench" | "terminal" | "memory" | "settings") => void }) {
  type Category = "All" | "LLMs" | "Voices" | "Image models" | "Skills" | "Tools";
  type ModelSize = "any" | "small" | "medium" | "large";
  const skillLanes = ["All lanes", "Research", "Coding", "Writing", "Data", "Documents", "Design", "Testing", "DevOps", "Security", "Productivity", "Creative", "Automation"];
  const cards = [
    { title: "LLMs", category: "LLMs" as const, description: "Discover and select local chat, coding, reasoning, and vision models.", detail: "Open the model library to discover a running local provider and use it in Chat.", icon: "◌", status: "Ready", action: "Open Models", route: "models" as const },
    { title: "Voices & TTS", category: "Voices" as const, description: "Preview and configure local voices for replies and narration.", detail: "OS-native speech is available now; voice profiles and low-resource settings are persisted locally.", icon: "◖", status: "Ready", action: "Open Settings", route: "settings" as const },
    { title: "Image Studio", category: "Image models" as const, description: "Generate images through a local ComfyUI workflow.", detail: "Configure a local endpoint and workflow, use a prompt recipe, and review generated outputs.", icon: "▧", status: "Ready", action: "Open Image Studio", route: "images" as const },
    { title: "Agent Workbench", category: "Tools" as const, description: "Plan tasks, queue reviewed work, and manage schedules.", detail: "Task plans and scheduled automation records are persisted locally with explicit lifecycle controls.", icon: "◇", status: "Ready", action: "Open Workbench", route: "workbench" as const },
    { title: "Coding tools", category: "Tools" as const, description: "Browse projects, edit approved files, run bounded commands, and undo changes.", detail: "The Coding Workspace and Terminal provide the current guarded project workflow.", icon: "⌘", status: "Ready", action: "Open Coding", route: "coding" as const },
    { title: "Memory", category: "Tools" as const, description: "Review, search, enable, disable, export, and remove approved memory.", detail: "Memory is explicit and local; nothing is learned or injected without the saved setting.", icon: "◈", status: "Ready", action: "Open Memory", route: "memory" as const },
    { title: "Skills", category: "Skills" as const, description: "Discover curated GitHub skills and manage reviewed local packages.", detail: "Review local Markdown-only packages, inspect permissions and digests, enable or disable them explicitly, and roll back updates.", icon: "✦", status: "Ready", action: "Review Skills", route: "workbench" as const },
    { title: "MCP & OpenAPI tools", category: "Tools" as const, description: "Browse permission-gated MCP servers and OpenAPI sources.", detail: "Catalog entries are visible now; connection and tool grants remain explicit and are never silently enabled.", icon: "⊕", status: "Catalog ready", action: "Review catalog", route: "settings" as const }
  ];
  const [query, setQuery] = useState("");
  const [category, setCategory] = useState<Category>("All");
  const [selectedTitle, setSelectedTitle] = useState(cards[0].title);
  const [models, setModels] = useState<MarketplaceModel[]>([]);
  const [skills, setSkills] = useState<MarketplaceSkill[]>([]);
  const [skillDetails, setSkillDetails] = useState<RemoteSkillDetails | null>(null);
  const [tools, setTools] = useState<MarketplaceTool[]>([]);
  const [mcpServers, setMcpServers] = useState<MarketplaceMcp[]>([]);
  const [installedSkills, setInstalledSkills] = useState<InstalledSkill[]>([]);
  const [skillPreview, setSkillPreview] = useState<SkillPreview | null>(null);
  const [skillInspection, setSkillInspection] = useState<SkillInspection | null>(null);
  const [skillSourceDirectory, setSkillSourceDirectory] = useState<string | null>(null);
  const [skillRemovalId, setSkillRemovalId] = useState<string | null>(null);
  const [skillBusy, setSkillBusy] = useState(false);
  const [skillError, setSkillError] = useState<string | null>(null);
  const [catalogBusy, setCatalogBusy] = useState(false);
  const [catalogError, setCatalogError] = useState<string | null>(null);
  const [modelSize, setModelSize] = useState<ModelSize>("any");
  const [modelQuantization, setModelQuantization] = useState("ANY");
  const [modelUncensored, setModelUncensored] = useState(false);
  const [modelSource, setModelSource] = useState("all");
  const [modelRuntime, setModelRuntime] = useState<{ provider: string; endpoint: string } | null>(null);
  const [downloadBusyId, setDownloadBusyId] = useState<string | null>(null);
  const [downloadStatus, setDownloadStatus] = useState<string | null>(null);
  const [skillLane, setSkillLane] = useState("All lanes");
  const filtered = useMemo(() => cards.filter((card) => {
    const matchesCategory = category === "All" || card.category === category;
    const haystack = `${card.title} ${card.description} ${card.detail}`.toLowerCase();
    return matchesCategory && haystack.includes(query.trim().toLowerCase());
  }), [category, query]);
  const selected = filtered.find((card) => card.title === selectedTitle) ?? filtered[0] ?? null;
  const [voiceSettings, setVoiceSettings] = useState(selectedVoiceSettings);
  const updateVoice = (key: "profileId" | "primaryModelId" | "lowResourceModelId" | "lowResourceMode" | "voiceOutputEnabled", value: string | boolean) => { saveVoiceSetting(key, value); setVoiceSettings(selectedVoiceSettings()); };
  const refreshCatalog = async () => {
    setCatalogBusy(true); setCatalogError(null);
    try {
      const request = window.serverRockAgent.request;
      const modelRequest = (kind: string) => request("marketplace.models.search", { query, kind, provider: kind === "llm" ? modelSource : "huggingface", size: modelSize, quantization: modelQuantization, uncensored: modelUncensored, limit: 100 });
      const [llmResponse, voiceResponse, imageResponse, skillResponse, toolResponse, mcpResponse] = await Promise.all([
        modelRequest("llm"), modelRequest("voice"), modelRequest("image"),
        request("marketplace.skills.search", { query, lane: skillLane === "All lanes" ? undefined : skillLane.toLowerCase() }),
        request("marketplace.tools.list", { query }),
        request("marketplace.mcp.list"),
      ]);
      const errors: string[] = [];
      const modelResponses = [llmResponse, voiceResponse, imageResponse];
      if (modelResponses.every(response => response.ok)) setModels(modelResponses.flatMap(response => (response.result as MarketplaceModel[]) ?? [])); else errors.push("Hugging Face: one or more model lanes were unavailable");
      if (skillResponse.ok) setSkills((skillResponse.result as MarketplaceSkill[]) ?? []); else errors.push(`Skills: ${skillResponse.error?.message ?? "unavailable"}`);
      if (toolResponse.ok) setTools((toolResponse.result as MarketplaceTool[]) ?? []); else errors.push(`Tools: ${toolResponse.error?.message ?? "unavailable"}`);
      if (mcpResponse.ok) setMcpServers((mcpResponse.result as MarketplaceMcp[]) ?? []); else errors.push(`MCP: ${mcpResponse.error?.message ?? "unavailable"}`);
      if (errors.length) setCatalogError(errors.join(" · ")); else setCatalogError(null);
    } catch (error) { setCatalogError(error instanceof Error ? error.message : "The marketplace could not be loaded."); }
    finally { setCatalogBusy(false); }
  };
  const loadModelRuntime = async () => {
    try {
      const response = await window.serverRockAgent.request("settings.get");
      if (response.ok) {
        const result = response.result as { provider?: string; endpoint?: string };
        if (result.provider && result.endpoint) setModelRuntime({ provider: result.provider, endpoint: result.endpoint });
      }
    } catch { /* The catalog remains usable as a source browser when settings are unavailable. */ }
  };
  const canDownloadModel = (model: MarketplaceModel) => modelRuntime !== null && (model.provider === "ModelScope" ? modelRuntime.provider.toLowerCase() === "ollama" : modelRuntime.provider.toLowerCase() === "lm studio");
  const downloadModel = async (model: MarketplaceModel) => {
    if (!modelRuntime || !canDownloadModel(model) || downloadBusyId) return;
    const provider = model.provider === "ModelScope" ? "modelscope" : "huggingface";
    setDownloadBusyId(model.id); setDownloadStatus(null); setCatalogError(null);
    try {
      const response = await window.serverRockAgent.request("marketplace.models.download", { provider, endpoint: modelRuntime.endpoint, id: model.id, quantization: modelQuantization === "ANY" ? undefined : modelQuantization, uncensored: model.uncensored });
      if (!response.ok) throw new Error(response.error?.message ?? "The model download could not be completed.");
      const result = response.result as { progress?: string };
      setDownloadStatus(`${model.id} is ready in ${modelRuntime.provider}. ${result.progress ?? "Download completed."}`);
    } catch (error) { setDownloadStatus(error instanceof Error ? error.message : "The model download could not be completed."); }
    finally { setDownloadBusyId(null); }
  };
  const loadInstalledSkills = async () => {
    try {
      const response = await window.serverRockAgent.request("skills.installed.list");
      if (!response.ok) { setSkillError(response.error?.message ?? "Installed skills could not be loaded."); return; }
      setInstalledSkills((response.result as InstalledSkill[]) ?? []);
      setSkillError(null);
    } catch (error) { setSkillError(error instanceof Error ? error.message : "Installed skills could not be loaded."); }
  };
  const previewLocalSkill = async () => {
    setSkillBusy(true); setSkillError(null);
    try {
      const folderResponse = await window.serverRockAgent.request("workspace.pickFolder");
      if (!folderResponse.ok) throw new Error(folderResponse.error?.message ?? "The skill folder picker failed.");
      const sourceDirectory = (folderResponse.result as { path?: string | null }).path;
      if (!sourceDirectory) return;
      const response = await window.serverRockAgent.request("skills.local.preview", { sourceDirectory });
      if (!response.ok) throw new Error(response.error?.message ?? "The skill package could not be previewed.");
      setSkillSourceDirectory(sourceDirectory);
      setSkillPreview(response.result as SkillPreview);
    } catch (error) { setSkillError(error instanceof Error ? error.message : "The skill package could not be previewed."); }
    finally { setSkillBusy(false); }
  };
  const installPreviewedSkill = async () => {
    if (!skillSourceDirectory || !skillPreview || skillBusy) return;
    setSkillBusy(true); setSkillError(null);
    try {
      const response = await window.serverRockAgent.request("skills.local.install", { sourceDirectory: skillSourceDirectory, expectedPackageSha256: skillPreview.packageSha256 });
      if (!response.ok) throw new Error(response.error?.message ?? "The skill package could not be installed.");
      setSkillPreview(null); setSkillSourceDirectory(null); await loadInstalledSkills();
    } catch (error) { setSkillError(error instanceof Error ? error.message : "The skill package could not be installed."); }
    finally { setSkillBusy(false); }
  };
  const setSkillEnabled = async (skill: InstalledSkill) => {
    setSkillBusy(true); setSkillError(null);
    try {
      const response = await window.serverRockAgent.request("skills.enabled.set", { id: skill.id, enabled: !skill.enabled });
      if (!response.ok) throw new Error(response.error?.message ?? "The skill state could not be changed.");
      await loadInstalledSkills();
    } catch (error) { setSkillError(error instanceof Error ? error.message : "The skill state could not be changed."); }
    finally { setSkillBusy(false); }
  };
  const rollbackSkill = async (skill: InstalledSkill) => {
    setSkillBusy(true); setSkillError(null);
    try {
      const response = await window.serverRockAgent.request("skills.rollback", { id: skill.id });
      if (!response.ok) throw new Error(response.error?.message ?? "The skill could not be rolled back.");
      await loadInstalledSkills();
    } catch (error) { setSkillError(error instanceof Error ? error.message : "The skill could not be rolled back."); }
    finally { setSkillBusy(false); }
  };
  const inspectInstalledSkill = async (skill: InstalledSkill) => {
    setSkillBusy(true); setSkillError(null);
    try {
      const response = await window.serverRockAgent.request("skills.installed.inspect", { id: skill.id });
      if (!response.ok) throw new Error(response.error?.message ?? "The installed skill could not be inspected.");
      setSkillInspection(response.result as SkillInspection);
    } catch (error) { setSkillError(error instanceof Error ? error.message : "The installed skill could not be inspected."); }
    finally { setSkillBusy(false); }
  };
  const inspectSkillRepository = async (skill: MarketplaceSkill) => {
    setSkillBusy(true); setSkillError(null); setSkillDetails(null);
    try {
      const response = await window.serverRockAgent.request("marketplace.skills.inspect", { repository: skill.id });
      if (!response.ok) throw new Error(response.error?.message ?? "The GitHub skill could not be inspected.");
      setSkillDetails(response.result as RemoteSkillDetails);
    } catch (error) { setSkillError(error instanceof Error ? error.message : "The GitHub skill could not be inspected."); }
    finally { setSkillBusy(false); }
  };
  const removeInstalledSkill = async (skill: InstalledSkill) => {
    setSkillBusy(true); setSkillError(null);
    try {
      const response = await window.serverRockAgent.request("skills.installed.remove", { id: skill.id });
      if (!response.ok) throw new Error(response.error?.message ?? "The installed skill could not be removed.");
      setSkillRemovalId(null); setSkillInspection(null); await loadInstalledSkills();
    } catch (error) { setSkillError(error instanceof Error ? error.message : "The installed skill could not be removed."); }
    finally { setSkillBusy(false); }
  };
  useEffect(() => { void refreshCatalog(); void loadInstalledSkills(); void loadModelRuntime(); }, []);

  const selectedModels = models.filter(model => model.kind === (selected?.title === "Voices & TTS" ? "voice" : selected?.title === "Image Studio" ? "image" : "llm"));
  const sizeLabel = (bytes: number) => bytes > 0 ? `${(bytes / 1073741824).toFixed(1)} GB` : "size not published";

  return <div className="feature-page marketplace-v4">
    <PageHeader eyebrow="MARKETPLACE / DISCOVER" title="Explore models, voices, skills & tools" description="Live Hugging Face and ModelScope discovery, curated GitHub skills, and local-first integrations for a stronger Skai workspace." actions={<><StatusBadge tone={catalogError ? "warning" : "success"}>{catalogBusy ? "Polling catalogs" : catalogError ? "Review catalog state" : "Catalogs ready"}</StatusBadge><Button variant="secondary" onClick={() => void refreshCatalog()} disabled={catalogBusy}>{catalogBusy ? "Polling…" : "↻ Refresh catalogs"}</Button></>} />
    <section className="marketplace-featured" aria-label="Featured marketplace collection"><div><p className="section-kicker">FEATURED COLLECTION</p><h3>Build a stronger local agent workflow</h3><p>Bring together a model that fits your hardware, a voice that fits your work, and skills you can inspect before they touch the workspace.</p></div><div className="marketplace-featured-meta"><StatusBadge tone="success">Local-first</StatusBadge><span>Hugging Face · ModelScope · GitHub · host-backed</span></div></section>
    <Card className="marketplace-browser">
      <div className="marketplace-toolbar">
        <div className="marketplace-search-row">
          <label className="sr-only" htmlFor="marketplace-search">Search marketplace</label>
          <input id="marketplace-search" value={query} onChange={(event) => setQuery(event.target.value)} onKeyDown={(event) => { if (event.key === "Enter") void refreshCatalog(); }} placeholder="Search Hugging Face, curated GitHub skills, tools…" />
          <Button variant="primary" onClick={() => void refreshCatalog()} disabled={catalogBusy}>{catalogBusy ? "Polling…" : "Search"}</Button>
          <span>{filtered.length} {filtered.length === 1 ? "category" : "categories"}</span>
        </div>
        <div className="marketplace-filters" role="group" aria-label="Marketplace categories">
          {(["All", "LLMs", "Voices", "Image models", "Skills", "Tools"] as Category[]).map((item) => <button className={`marketplace-filter${category === item ? " is-active" : ""}`} key={item} type="button" aria-pressed={category === item} onClick={() => setCategory(item)}>{item}</button>)}
        </div>
      </div>
      <div className="marketplace-layout">
        {filtered.length === 0 ? <EmptyState title="No matching categories" description="Try another search term or clear the category filter." action={<Button variant="ghost" onClick={() => { setQuery(""); setCategory("All"); }}>Clear filters</Button>} /> : <div className="marketplace-grid" aria-label="Marketplace categories">
          {filtered.map((card) => <button className={`marketplace-card${selected?.title === card.title ? " is-selected" : ""}`} key={card.title} type="button" onClick={() => setSelectedTitle(card.title)} aria-pressed={selected?.title === card.title}>
            <span className="marketplace-card-heading"><span className="marketplace-icon">{card.icon}</span><StatusBadge tone={card.status === "Ready" ? "success" : card.status === "Review only" ? "warning" : "neutral"}>{card.status}</StatusBadge></span>
            <strong>{card.title}</strong>
            <span>{card.description}</span>
            <small>{card.action} →</small>
          </button>)}
        </div>}
        {catalogError && <div className="surface-error" role="alert"><strong>Catalog refresh failed</strong><p>{catalogError}</p><Button variant="secondary" onClick={() => void refreshCatalog()}>Retry</Button></div>}
        {selected && <Card className="marketplace-detail" id="marketplace-detail-panel">
          <div className="marketplace-detail-heading"><div><p className="section-kicker">CAPABILITY</p><h3>{selected.title}</h3></div><StatusBadge tone={selected.status === "Ready" ? "success" : selected.status === "Review only" ? "warning" : "neutral"}>{selected.status}</StatusBadge></div>
          <p>{selected.detail}</p>
          <dl className="marketplace-detail-list"><div><dt>Availability</dt><dd>{selected.status === "Ready" ? "Available in this desktop build" : "Core capability present; Electron integration remains bounded"}</dd></div><div><dt>Next step</dt><dd>{selected.status === "Ready" ? `Use ${selected.action} to open the workflow.` : "Keep this capability visible until its host-backed workflow is connected."}</dd></div></dl>
          {selected.title === "Skills" || selected.title === "MCP & OpenAPI tools" ? <Button variant="secondary" onClick={() => document.getElementById("marketplace-detail-panel")?.scrollIntoView({ behavior: "smooth", block: "start" })}>{selected.action}</Button> : <Button variant={selected.status === "Ready" ? "primary" : "secondary"} onClick={() => onNavigate(selected.route)}>{selected.action}</Button>}
          {selected.title === "LLMs" || selected.title === "Voices & TTS" || selected.title === "Image Studio" ? <>
            <div className="marketplace-filter-panel">
              <div><p className="section-kicker">MODEL FILTERS</p><strong>Choose what Skai should surface</strong><small>LLMs poll Hugging Face and ModelScope; voices and image models use Hugging Face. Downloads remain user-approved.</small></div>
              {selected.title === "LLMs" && <label>Source<select value={modelSource} onChange={(event) => setModelSource(event.target.value)}><option value="all">All sources</option><option value="huggingface">Hugging Face</option><option value="modelscope">ModelScope · GGUF</option></select></label>}
              <label>Model size<select value={modelSize} onChange={(event) => setModelSize(event.target.value as ModelSize)}><option value="any">Any size</option><option value="small">Small · under 4 GB</option><option value="medium">Medium · 4–12 GB</option><option value="large">Large · 12 GB+</option></select></label>
              {selected.title === "LLMs" && <label>Quantization<select value={modelQuantization} onChange={(event) => setModelQuantization(event.target.value)}><option value="ANY">Any quantization</option><option value="Q4_K_M">Q4_K_M · balanced</option><option value="Q5_K_M">Q5_K_M · quality</option><option value="Q8_0">Q8_0 · high fidelity</option><option value="F16">F16 · full precision</option></select></label>}
              {selected.title === "LLMs" && <label className="settings-toggle"><span><strong>Show abliterated / uncensored</strong><small>Only explicitly labeled repositories are included.</small></span><input type="checkbox" checked={modelUncensored} onChange={(event) => setModelUncensored(event.target.checked)} /></label>}
              <Button variant="secondary" onClick={() => void refreshCatalog()} disabled={catalogBusy}>Apply filters</Button>
            </div>
            {downloadStatus && <div className="marketplace-download-status" role="status"><strong>Model transfer</strong><span>{downloadStatus}</span></div>}
            <CatalogList title={`${selected.title === "LLMs" ? "Multi-provider GGUF LLMs" : selected.title === "Voices & TTS" ? "Hugging Face voice models" : "Hugging Face image models"}`} loading={catalogBusy} empty="No model assets matched these filters. Try Any size, All sources, or a broader search." items={selectedModels.map(model => ({ id: `${model.provider}:${model.id}`, title: model.id, detail: `${model.provider} · ${model.task} · ${sizeLabel(model.sizeBytes)} · ${model.downloads.toLocaleString()} downloads${model.uncensored ? " · abliterated/uncensored" : ""}${model.quantizations.length ? ` · ${model.quantizations.join(", ")}` : ""}`, href: model.url, action: canDownloadModel(model) ? (downloadBusyId === model.id ? "Downloading…" : model.provider === "ModelScope" ? "Import to Ollama" : "Download to LM Studio") : `Open ${model.provider}`, onAction: canDownloadModel(model) ? () => void downloadModel(model) : undefined }))} />
          </> : selected.title === "Skills" ? <>
            <div className="marketplace-filter-panel"><div><p className="section-kicker">CURATED SKILLS</p><strong>Universal workflows authored for Skai</strong><small>Skai polls multiple GitHub discovery lanes, then keeps only public, non-fork repositories with useful community signal.</small></div><label>Skill lane<select value={skillLane} onChange={(event) => setSkillLane(event.target.value)}>{skillLanes.map(lane => <option key={lane}>{lane}</option>)}</select></label><Button variant="secondary" onClick={() => void refreshCatalog()} disabled={catalogBusy}>Poll GitHub lanes</Button></div>
            <div className="marketplace-skill-management"><div className="marketplace-skill-management-heading"><div><p className="section-kicker">LOCAL LIBRARY</p><strong>Install a reviewed Markdown skill</strong><small>Choose a local folder containing <code>skill.json</code> and <code>SKILL.md</code>. Skai validates the package, records a digest, and never executes package files.</small></div><div className="marketplace-skill-actions"><Button variant="secondary" onClick={() => void previewLocalSkill()} disabled={skillBusy}>{skillBusy ? "Checking…" : "Review local package"}</Button><Button variant="ghost" onClick={() => void loadInstalledSkills()} disabled={skillBusy}>Refresh library</Button></div></div>
            {skillError && <div className="surface-error" role="alert"><strong>Skill library needs attention</strong><p>{skillError}</p></div>}
            {skillPreview && <div className="skill-review-card"><div><p className="section-kicker">INSTALLATION REVIEW</p><h4>{skillPreview.name} <span>v{skillPreview.version}</span></h4><p>{skillPreview.publisher} · {skillPreview.isUpdate ? `updates ${skillPreview.existingVersion ?? "the installed copy"}` : "new local package"}</p></div><dl><div><dt>Permissions</dt><dd>{skillPreview.requiredPermissions.length ? skillPreview.requiredPermissions.join(" · ") : "None declared"}</dd></div><div><dt>SHA-256</dt><dd>{skillPreview.packageSha256}</dd></div><div><dt>Source</dt><dd>{skillPreview.sourceRef}</dd></div></dl><div className="marketplace-skill-actions"><Button variant="ghost" onClick={() => { setSkillPreview(null); setSkillSourceDirectory(null); }} disabled={skillBusy}>Cancel</Button><Button variant="primary" onClick={() => void installPreviewedSkill()} disabled={skillBusy}>{skillBusy ? "Installing…" : skillPreview.isUpdate ? "Install update" : "Install skill"}</Button></div></div>}
            {skillInspection && <div className="skill-inspection-card"><div className="marketplace-skill-management-heading"><div><p className="section-kicker">INSTALLED SKILL</p><h4>{skillInspection.name} <span>v{skillInspection.version}</span></h4><small>{skillInspection.publisher} · {skillInspection.sourceRef}</small></div><Button variant="ghost" onClick={() => setSkillInspection(null)}>Close</Button></div><dl><div><dt>SHA-256</dt><dd>{skillInspection.packageSha256}</dd></div><div><dt>Permissions</dt><dd>{skillInspection.requiredPermissions.length ? skillInspection.requiredPermissions.join(" · ") : "None declared"}</dd></div></dl><pre aria-label={`${skillInspection.name} instructions`}>{skillInspection.instructions}</pre></div>}
            {skillDetails && <div className="skill-details-card"><div className="marketplace-skill-management-heading"><div><p className="section-kicker">REMOTE SKILL REVIEW</p><h4>{skillDetails.name} <span>{skillDetails.publisher}</span></h4><small>{skillDetails.description || "No repository description published."}</small></div><Button variant="ghost" onClick={() => setSkillDetails(null)}>Close</Button></div><div className="skill-details-status"><StatusBadge tone={skillDetails.remoteInstallAvailable ? "success" : "warning"}>{skillDetails.remoteInstallAvailable ? "Install available" : "Review only"}</StatusBadge><span>{skillDetails.installBoundary}</span></div><dl className="skill-details-list"><div><dt>Publisher</dt><dd>{skillDetails.publisher}</dd></div><div><dt>License</dt><dd>{skillDetails.license || "Not published"}</dd></div><div><dt>Source</dt><dd><a href={skillDetails.sourceUrl} target="_blank" rel="noreferrer">{skillDetails.sourceUrl} ↗</a></dd></div><div><dt>Revision</dt><dd title={skillDetails.revision}>{skillDetails.revision.slice(0, 12)}… · {skillDetails.defaultBranch}</dd></div><div><dt>Repository signal</dt><dd>{skillDetails.stars.toLocaleString()} stars · {skillDetails.forks.toLocaleString()} forks</dd></div><div><dt>Manifest</dt><dd>{skillDetails.manifestStatus}</dd></div><div><dt>SKILL.md</dt><dd>{skillDetails.skillStatus}</dd></div><div><dt>Dependencies</dt><dd>{skillDetails.dependencies.length ? skillDetails.dependencies.join(" · ") : "None declared"}</dd></div><div><dt>Declared tools</dt><dd>{skillDetails.declaredTools.length ? skillDetails.declaredTools.join(" · ") : "None declared"}</dd></div><div><dt>Required permissions</dt><dd>{skillDetails.requiredPermissions.length ? skillDetails.requiredPermissions.join(" · ") : "None declared"}</dd></div><div><dt>Compatibility</dt><dd>{skillDetails.compatibility.length ? skillDetails.compatibility.join(" · ") : "Not declared"}</dd></div></dl><div><p className="section-kicker">REVIEWED INSTRUCTIONS</p>{skillDetails.skillMarkdown ? <pre className="skill-details-markdown" aria-label={`${skillDetails.name} SKILL.md`}>{skillDetails.skillMarkdown}</pre> : <p className="marketplace-honesty">This repository does not publish a readable <code>SKILL.md</code> at the reviewed revision.</p>}</div></div>}
            {installedSkills.length > 0 && <div className="installed-skill-list"><div className="panel-heading"><div><p className="section-kicker">INSTALLED LIBRARY</p><h3>Managed skills</h3></div><span className="panel-count">{installedSkills.length}</span></div>{installedSkills.map(skill => <div className="installed-skill-row" key={skill.id}><div><strong>{skill.name}</strong><small>{skill.publisher} · v{skill.version} · {skill.sourceRef}</small><span>{skill.requiredPermissions.length ? `Permissions: ${skill.requiredPermissions.join(" · ")}` : "No permissions declared"}</span></div><div className="marketplace-skill-actions"><StatusBadge tone={skill.enabled ? "success" : "neutral"}>{skill.enabled ? "Enabled" : "Disabled"}</StatusBadge><Button variant="ghost" onClick={() => void inspectInstalledSkill(skill)} disabled={skillBusy}>Inspect</Button><Button variant="ghost" onClick={() => void setSkillEnabled(skill)} disabled={skillBusy}>{skill.enabled ? "Disable" : "Enable"}</Button>{skill.canRollback && <Button variant="ghost" onClick={() => void rollbackSkill(skill)} disabled={skillBusy}>Rollback</Button>}{skillRemovalId === skill.id ? <><Button variant="danger" onClick={() => void removeInstalledSkill(skill)} disabled={skillBusy}>Confirm remove</Button><Button variant="ghost" onClick={() => setSkillRemovalId(null)} disabled={skillBusy}>Keep</Button></> : <Button variant="danger" onClick={() => setSkillRemovalId(skill.id)} disabled={skillBusy}>Remove</Button>}</div></div>)}</div>}
            <CatalogList title="GitHub skill repositories · curated lanes" loading={catalogBusy} empty="No curated GitHub skills matched. Try All lanes or a broader search." items={skills.map(skill => ({ id: skill.id, title: `${skill.name} · ${skill.lane}`, detail: `${skill.stars.toLocaleString()} stars · ${skill.description || skill.laneDescription}`, href: skill.url, action: "Inspect skill", onAction: () => void inspectSkillRepository(skill) }))} />
            </div>
          </> : selected.title === "MCP & OpenAPI tools" ? <CatalogList title="MCP server catalog" loading={catalogBusy} empty="No MCP server catalog entries are available." items={mcpServers.map(server => ({ id: server.id, title: server.name, detail: `${server.transport} · ${server.status} · ${server.description}`, href: server.source, action: "View source" }))} /> : selected.title === "Agent Workbench" || selected.title === "Coding tools" ? <CatalogList title="Built-in tools" loading={catalogBusy} empty="No built-in tool entries were returned." items={tools.map(tool => ({ id: tool.id, title: tool.name, detail: `${tool.category} · ${tool.status} · ${tool.description}`, href: undefined, action: tool.whereToFind ?? "Available in Skai" }))} /> : selected.category === "Voices" ? <div className="marketplace-voice-controls">
            <label>Primary conversational voice<select value={voiceSettings.primaryModelId} onChange={(event) => updateVoice("primaryModelId", event.target.value)}>{voiceModels.map((voice) => <option key={voice.id} value={voice.id}>{voice.name} · {voice.size}</option>)}</select></label>
            <label>Low-resource mode voice<select value={voiceSettings.lowResourceModelId} onChange={(event) => updateVoice("lowResourceModelId", event.target.value)}>{voiceModels.map((voice) => <option key={voice.id} value={voice.id}>{voice.name} · {voice.size}</option>)}</select></label>
            <label>Voice profile<select value={voiceSettings.profileId} onChange={(event) => updateVoice("profileId", event.target.value)}>{voiceProfiles.map((profile) => <option key={profile.id} value={profile.id}>{profile.name} · {profile.description}</option>)}</select></label>
            <label className="settings-toggle"><span><strong>Speak completed replies</strong><small>Voice output stays off until you turn it on.</small></span><input type="checkbox" checked={voiceSettings.voiceOutputEnabled} onChange={(event) => updateVoice("voiceOutputEnabled", event.target.checked)} /></label>
            <label className="settings-toggle"><span><strong>Use low-resource mode</strong><small>Use the separately assigned voice model on constrained laptops.</small></span><input type="checkbox" checked={voiceSettings.lowResourceMode} onChange={(event) => updateVoice("lowResourceMode", event.target.checked)} /></label>
            <div className="marketplace-voice-actions"><Button variant="secondary" onClick={() => void speakNativeResponse("Skai voice preview. This uses the operating system voice service.", true)}>Preview OS voice</Button><Button variant="ghost" onClick={stopNativeSpeech}>Stop preview</Button></div>
            <p className="marketplace-honesty">OS-native speech is installed and active now. Kokoro and Pocket TTS are selectable profiles, but remain unavailable until their C# model-pack adapter is installed.</p>
          </div> : <p className="marketplace-honesty">This capability is intentionally surfaced with its current boundary. The status above reflects what can be used from Electron today and what still needs a host-backed integration.</p>}
        </Card>}
      </div>
    </Card>
  </div>;
}

function CatalogList({ title, loading, empty, items }: { title: string; loading: boolean; empty: string; items: Array<{ id: string; title: string; detail: string; href?: string; action: string; onAction?: () => void }> }) {
  return <section className="marketplace-catalog-results"><div className="panel-heading"><div><p className="section-kicker">LIVE CATALOG</p><h3>{title}</h3></div><span className="panel-count">{loading ? "…" : items.length}</span></div>{loading ? <p className="marketplace-honesty">Loading the local catalog bridge…</p> : items.length === 0 ? <p className="marketplace-honesty">{empty}</p> : <div className="catalog-list">{items.slice(0, 30).map(item => <div className="catalog-row" key={item.id}><span className="catalog-row-copy"><strong>{item.title}</strong><small>{item.detail}</small></span>{item.onAction ? <span className="catalog-row-actions"><button className="catalog-action catalog-action-button" type="button" onClick={item.onAction}>{item.action}</button>{item.href && <a className="catalog-action catalog-source-link" href={item.href} target="_blank" rel="noreferrer">Source ↗</a>}</span> : item.href ? <a className="catalog-action" href={item.href} target="_blank" rel="noreferrer">{item.action} ↗</a> : <span className="catalog-action">{item.action}</span>}</div>)}</div>}</section>;
}
