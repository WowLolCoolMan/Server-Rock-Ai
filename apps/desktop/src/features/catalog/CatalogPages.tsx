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

type MarketplaceModel = { id: string; kind: string; task: string; downloads: number; likes: number; sizeBytes: number; quantizations: string[]; uncensored: boolean; tags: string[]; url: string };
type MarketplaceSkill = { id: string; name: string; description: string; stars: number; url: string; defaultBranch: string; lane: string; laneDescription: string };
type MarketplaceTool = { id: string; name: string; category: string; description: string; access: string; status: string; builtIn: boolean; whereToFind?: string | null };
type MarketplaceMcp = { id: string; name: string; transport: string; description: string; source: string; status: string };

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

  return <div className="feature-page"><PageHeader eyebrow="IMAGE STUDIO" title="Create visual work" description="A dedicated surface for local image generation, separated from conversation controls." actions={<StatusBadge tone={readiness.tone}>{readiness.label}</StatusBadge>} /><div className="image-studio-layout"><Card className="form-card image-editor-card"><div className="studio-panel-heading"><div><p className="section-kicker">LOCAL IMAGE WORKFLOW</p><h3>Build a generation</h3></div><span className="studio-bridge-label">COMFYUI BRIDGE</span></div><div className="form-grid"><label>ComfyUI endpoint<input value={endpoint} onChange={(event) => onChange("endpoint", event.target.value)} placeholder="http://127.0.0.1:8188" aria-describedby="image-endpoint-help" /><small id="image-endpoint-help">The local address where ComfyUI is listening.</small></label><label>Prompt<textarea value={prompt} onChange={(event) => onChange("prompt", event.target.value)} placeholder="Describe what you want to create…" rows={4} aria-describedby="image-prompt-help" /><small id="image-prompt-help">Use a recipe below as a starting point, then make it yours.</small></label><div className="prompt-recipes" aria-label="Prompt recipes"><span className="field-caption">START WITH A RECIPE</span><div>{promptRecipes.map((recipe) => <Button key={recipe.title} variant="ghost" className="prompt-recipe" onClick={() => onChange("prompt", recipe.prompt)}>{recipe.title}</Button>)}</div></div><label>Workflow JSON<textarea value={workflow} onChange={(event) => onChange("workflow", event.target.value)} placeholder='{"prompt":"{{prompt}}"}' rows={6} aria-describedby="image-workflow-help" /><small id="image-workflow-help">Use a ComfyUI API workflow with <code>{"{{prompt}}"}</code> where the prompt should be inserted.</small></label></div><div className="studio-readiness"><StatusBadge tone={readiness.tone}>{readiness.label}</StatusBadge><span>{readiness.detail}</span></div><div className="form-footer"><span>Outputs stay in this local workspace history.</span><Button variant="primary" disabled={busy || !endpoint.trim() || !prompt.trim() || !workflow.trim()} onClick={onGenerate}>{busy ? "Generating…" : "Generate image"}</Button></div></Card><Card className="image-history"><div className="panel-heading"><div><p className="section-kicker">GENERATION HISTORY</p><h3>Recent outputs</h3></div><span className="panel-count">{images.length}</span></div>{images.length === 0 ? <EmptyState title="No generated images" description="Your local outputs will appear here after a successful generation." /> : <div className="image-gallery">{images.map((image) => <figure key={image.url}><img src={image.url} alt={image.fileName} /><figcaption>{image.fileName}</figcaption></figure>)}</div>}</Card></div></div>;
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
    { title: "Skills", category: "Skills" as const, description: "Review the built-in read-only skill catalog.", detail: "The Core skill catalog and permission rules exist; installation and activation are not yet host-connected in Electron.", icon: "✦", status: "Review only", action: "Open Workbench", route: "workbench" as const },
    { title: "MCP & OpenAPI tools", category: "Tools" as const, description: "Permission-gated tool protocols are implemented in Core.", detail: "MCP connection and OpenAPI discovery remain a host/UI integration slice; no tool is silently enabled.", icon: "⊕", status: "Integration pending", action: "Open Settings", route: "settings" as const }
  ];
  const [query, setQuery] = useState("");
  const [category, setCategory] = useState<Category>("All");
  const [selectedTitle, setSelectedTitle] = useState(cards[0].title);
  const [models, setModels] = useState<MarketplaceModel[]>([]);
  const [skills, setSkills] = useState<MarketplaceSkill[]>([]);
  const [tools, setTools] = useState<MarketplaceTool[]>([]);
  const [mcpServers, setMcpServers] = useState<MarketplaceMcp[]>([]);
  const [catalogBusy, setCatalogBusy] = useState(false);
  const [catalogError, setCatalogError] = useState<string | null>(null);
  const [modelSize, setModelSize] = useState<ModelSize>("any");
  const [modelQuantization, setModelQuantization] = useState("ANY");
  const [modelUncensored, setModelUncensored] = useState(false);
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
      const modelRequest = (kind: string) => request("marketplace.models.search", { query, kind, size: modelSize, quantization: modelQuantization, uncensored: modelUncensored, limit: 100 });
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
  useEffect(() => { void refreshCatalog(); }, []);

  const selectedModels = models.filter(model => model.kind === (selected.title === "Voices & TTS" ? "voice" : selected.title === "Image Studio" ? "image" : "llm"));
  const sizeLabel = (bytes: number) => bytes > 0 ? `${(bytes / 1073741824).toFixed(1)} GB` : "size not published";

  return <div className="feature-page marketplace-v4">
    <PageHeader eyebrow="MARKETPLACE / DISCOVER" title="Explore models, voices, skills & tools" description="Live Hugging Face discovery, curated GitHub skills, and local-first integrations for a stronger Skai workspace." actions={<><StatusBadge tone={catalogError ? "warning" : "success"}>{catalogBusy ? "Polling catalogs" : catalogError ? "Review catalog state" : "Catalogs ready"}</StatusBadge><Button variant="secondary" onClick={() => void refreshCatalog()} disabled={catalogBusy}>{catalogBusy ? "Polling…" : "↻ Refresh catalogs"}</Button></>} />
    <section className="marketplace-featured" aria-label="Featured marketplace collection"><div><p className="section-kicker">FEATURED COLLECTION</p><h3>Build a stronger local agent workflow</h3><p>Bring together a model that fits your hardware, a voice that fits your work, and skills you can inspect before they touch the workspace.</p></div><div className="marketplace-featured-meta"><StatusBadge tone="success">Local-first</StatusBadge><span>Hugging Face · GitHub · host-backed</span></div></section>
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
        {selected && <Card className="marketplace-detail">
          <div className="marketplace-detail-heading"><div><p className="section-kicker">CAPABILITY</p><h3>{selected.title}</h3></div><StatusBadge tone={selected.status === "Ready" ? "success" : selected.status === "Review only" ? "warning" : "neutral"}>{selected.status}</StatusBadge></div>
          <p>{selected.detail}</p>
          <dl className="marketplace-detail-list"><div><dt>Availability</dt><dd>{selected.status === "Ready" ? "Available in this desktop build" : "Core capability present; Electron integration remains bounded"}</dd></div><div><dt>Next step</dt><dd>{selected.status === "Ready" ? `Use ${selected.action} to open the workflow.` : "Keep this capability visible until its host-backed workflow is connected."}</dd></div></dl>
          <Button variant={selected.status === "Ready" ? "primary" : "secondary"} onClick={() => onNavigate(selected.route)}>{selected.action}</Button>
          {selected.title === "LLMs" || selected.title === "Voices & TTS" || selected.title === "Image Studio" ? <>
            <div className="marketplace-filter-panel">
              <div><p className="section-kicker">MODEL FILTERS</p><strong>Choose what Skai should surface</strong><small>Every lane polls Hugging Face’s public catalog; downloads remain user-approved.</small></div>
              <label>Model size<select value={modelSize} onChange={(event) => setModelSize(event.target.value as ModelSize)}><option value="any">Any size</option><option value="small">Small · under 4 GB</option><option value="medium">Medium · 4–12 GB</option><option value="large">Large · 12 GB+</option></select></label>
              {selected.title === "LLMs" && <label>Quantization<select value={modelQuantization} onChange={(event) => setModelQuantization(event.target.value)}><option value="ANY">Any quantization</option><option value="Q4_K_M">Q4_K_M · balanced</option><option value="Q5_K_M">Q5_K_M · quality</option><option value="Q8_0">Q8_0 · high fidelity</option><option value="F16">F16 · full precision</option></select></label>}
              {selected.title === "LLMs" && <label className="settings-toggle"><span><strong>Show abliterated / uncensored</strong><small>Only explicitly labeled repositories are included.</small></span><input type="checkbox" checked={modelUncensored} onChange={(event) => setModelUncensored(event.target.checked)} /></label>}
              <Button variant="secondary" onClick={() => void refreshCatalog()} disabled={catalogBusy}>Apply filters</Button>
            </div>
            <CatalogList title={`Hugging Face · ${selected.title === "LLMs" ? "GGUF LLMs" : selected.title === "Voices & TTS" ? "voice models" : "image models"}`} loading={catalogBusy} empty="No Hugging Face assets matched these filters. Try Any size or a broader search." items={selectedModels.map(model => ({ id: model.id, title: model.id, detail: `${model.task} · ${sizeLabel(model.sizeBytes)} · ${model.downloads.toLocaleString()} downloads${model.uncensored ? " · abliterated/uncensored" : ""}${model.quantizations.length ? ` · ${model.quantizations.join(", ")}` : ""}`, href: model.url, action: "Open on Hugging Face" }))} />
          </> : selected.title === "Skills" ? <>
            <div className="marketplace-filter-panel"><div><p className="section-kicker">CURATED SKILLS</p><strong>Universal workflows authored for Skai</strong><small>Skai polls multiple GitHub discovery lanes, then keeps only public, non-fork repositories with useful community signal.</small></div><label>Skill lane<select value={skillLane} onChange={(event) => setSkillLane(event.target.value)}>{skillLanes.map(lane => <option key={lane}>{lane}</option>)}</select></label><Button variant="secondary" onClick={() => void refreshCatalog()} disabled={catalogBusy}>Poll GitHub lanes</Button></div>
            <CatalogList title="GitHub skill repositories · curated lanes" loading={catalogBusy} empty="No curated GitHub skills matched. Try All lanes or a broader search." items={skills.map(skill => ({ id: skill.id, title: `${skill.name} · ${skill.lane}`, detail: `${skill.stars.toLocaleString()} stars · ${skill.description || skill.laneDescription}`, href: skill.url, action: "Review on GitHub" }))} />
          </> : selected.title === "MCP & OpenAPI tools" ? <CatalogList title="MCP server catalog" loading={catalogBusy} empty="No MCP server catalog entries are available." items={mcpServers.map(server => ({ id: server.id, title: server.name, detail: `${server.transport} · ${server.status} · ${server.description}`, href: server.source, action: "View source" }))} /> : selected.title === "Agent Workbench" ? <CatalogList title="Built-in tools" loading={catalogBusy} empty="No built-in tool entries were returned." items={tools.map(tool => ({ id: tool.id, title: tool.name, detail: `${tool.category} · ${tool.status} · ${tool.description}`, href: undefined, action: tool.whereToFind ?? "Available in Skai" }))} /> : selected.category === "Voices" ? <div className="marketplace-voice-controls">
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

function CatalogList({ title, loading, empty, items }: { title: string; loading: boolean; empty: string; items: Array<{ id: string; title: string; detail: string; href?: string; action: string }> }) {
  return <section className="marketplace-catalog-results"><div className="panel-heading"><div><p className="section-kicker">LIVE CATALOG</p><h3>{title}</h3></div><span className="panel-count">{loading ? "…" : items.length}</span></div>{loading ? <p className="marketplace-honesty">Loading the local catalog bridge…</p> : items.length === 0 ? <p className="marketplace-honesty">{empty}</p> : <div className="catalog-list">{items.slice(0, 30).map(item => <div className="catalog-row" key={item.id}><span className="catalog-row-copy"><strong>{item.title}</strong><small>{item.detail}</small></span>{item.href ? <a className="catalog-action" href={item.href} target="_blank" rel="noreferrer">{item.action} ↗</a> : <span className="catalog-action">{item.action}</span>}</div>)}</div>}</section>;
}
