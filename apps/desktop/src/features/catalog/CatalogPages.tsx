import { useMemo, useState } from "react";
import type { Model } from "../../app/types";
import { Button, Card, EmptyState, PageHeader, StatusBadge } from "../../components/ui/Primitives";
import { NavIcon, type NavIconName } from "../../components/ui/NavIcon";
import { saveVoiceSetting, selectedVoiceSettings, speakNativeResponse, stopNativeSpeech, voiceModels, voiceProfiles } from "../../services/nativeTts";

function modelProvider(model: string) {
  const separator = model.indexOf("/");
  return separator > 0 ? model.slice(0, separator) : "Local runtime";
}

function modelVisionLabel(vision: string) {
  return vision.toLowerCase() === "supported" ? "Vision" : "Text";
}

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

export function MarketplacePage() {
  type Category = "All" | "LLMs" | "Voices" | "Image models" | "Skills" | "Tools";
  const cards: Array<{ title: string; category: Category; description: string; detail: string; icon: NavIconName; status: string; statusTone: "neutral" | "success" | "warning"; availability: string; nextStep: string }> = [
    { title: "LLMs", category: "LLMs", description: "Review coding, chat, reasoning, and vision models.", detail: "Choose models already installed in a local runtime, or prepare a reviewed local package before it is used.", icon: "model", status: "Local source needed", statusTone: "warning", availability: "No local model catalog is connected.", nextStep: "Start a supported local runtime, then use Models to discover what is installed." },
    { title: "Voices & TTS", category: "Voices", description: "Configure voices for local conversation and narration.", detail: "Use the operating system voice service today, with optional local model profiles when their adapters are installed.", icon: "activity", status: "Local ready", statusTone: "success", availability: "OS-native speech is available on this computer.", nextStep: "Choose a voice profile here or in Settings and preview it locally." },
    { title: "Image models", category: "Image models", description: "Review checkpoints, LoRAs, and workflows.", detail: "Bring reviewed local image assets and ComfyUI workflow packs into Image Studio.", icon: "image", status: "Local source needed", statusTone: "warning", availability: "No local image catalog is connected.", nextStep: "Configure a local ComfyUI workflow in Image Studio before generating." },
    { title: "Skills", category: "Skills", description: "Review focused instruction packs for agents.", detail: "Skills are local folders with a manifest and instructions. Review them before enabling them.", icon: "sparkle", status: "Local source needed", statusTone: "warning", availability: "No local skill catalog is connected.", nextStep: "Add a reviewed local skill package before activation is offered." },
    { title: "Tools & integrations", category: "Tools", description: "Review MCP servers and local developer tools.", detail: "Connect tools explicitly, inspect their permissions, and keep ownership on this machine.", icon: "terminal", status: "Local source needed", statusTone: "warning", availability: "No local tool catalog is connected.", nextStep: "Add a reviewed local connector before permissioned activation is offered." }
  ];
  const [query, setQuery] = useState("");
  const [category, setCategory] = useState<Category>("All");
  const [selectedTitle, setSelectedTitle] = useState(cards[0].title);
  const filtered = useMemo(() => cards.filter((card) => {
    const matchesCategory = category === "All" || card.category === category;
    const haystack = `${card.title} ${card.description} ${card.detail}`.toLowerCase();
    return matchesCategory && haystack.includes(query.trim().toLowerCase());
  }), [category, query]);
  const selected = filtered.find((card) => card.title === selectedTitle) ?? filtered[0] ?? null;
  const [voiceSettings, setVoiceSettings] = useState(selectedVoiceSettings);
  const updateVoice = (key: "profileId" | "primaryModelId" | "lowResourceModelId" | "lowResourceMode" | "voiceOutputEnabled", value: string | boolean) => { saveVoiceSetting(key, value); setVoiceSettings(selectedVoiceSettings()); };

  return <div className="feature-page">
    <PageHeader eyebrow="LOCAL CATALOG" title="Extend Skai" description="Review local capabilities for models, voices, image workflows, skills, and tools. Nothing is fetched from a hosted catalog." />
    <Card className="marketplace-browser">
      <div className="marketplace-toolbar">
        <div className="marketplace-search-row">
          <label className="sr-only" htmlFor="marketplace-search">Search marketplace</label>
          <input id="marketplace-search" value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search models, voices, image packs, skills, and tools…" />
          <span>{filtered.length} {filtered.length === 1 ? "category" : "categories"}</span>
        </div>
        <div className="marketplace-filters" role="group" aria-label="Marketplace categories">
          {(["All", "LLMs", "Voices", "Image models", "Skills", "Tools"] as Category[]).map((item) => <button className={`marketplace-filter${category === item ? " is-active" : ""}`} key={item} type="button" aria-pressed={category === item} onClick={() => setCategory(item)}>{item}</button>)}
        </div>
      </div>
      <div className="marketplace-layout">
        {filtered.length === 0 ? <EmptyState title="No matching categories" description="Try another search term or clear the category filter." action={<Button variant="ghost" onClick={() => { setQuery(""); setCategory("All"); }}>Clear filters</Button>} /> : <div className="marketplace-grid" aria-label="Marketplace categories">
          {filtered.map((card) => <button className={`marketplace-card${selected?.title === card.title ? " is-selected" : ""}`} key={card.title} type="button" onClick={() => setSelectedTitle(card.title)} aria-pressed={selected?.title === card.title}>
            <span className="marketplace-card-heading"><span className="marketplace-icon"><NavIcon name={card.icon} /></span><StatusBadge tone={card.statusTone}>{card.status}</StatusBadge></span>
            <strong>{card.title}</strong>
            <span>{card.description}</span>
            <small>View capability details →</small>
          </button>)}
        </div>}
        {selected && <Card className="marketplace-detail">
          <div className="marketplace-detail-heading"><div><p className="section-kicker">SELECTED CATEGORY</p><h3>{selected.title}</h3></div><StatusBadge tone={selected.statusTone}>{selected.status}</StatusBadge></div>
          <p>{selected.detail}</p>
          <dl className="marketplace-detail-list"><div><dt>Availability</dt><dd>{selected.availability}</dd></div><div><dt>Next step</dt><dd>{selected.nextStep}</dd></div></dl>
          {selected.category === "Voices" ? <div className="marketplace-voice-controls">
            <label>Primary conversational voice<select value={voiceSettings.primaryModelId} onChange={(event) => updateVoice("primaryModelId", event.target.value)}>{voiceModels.map((voice) => <option key={voice.id} value={voice.id}>{voice.name} · {voice.size}</option>)}</select></label>
            <label>Low-resource mode voice<select value={voiceSettings.lowResourceModelId} onChange={(event) => updateVoice("lowResourceModelId", event.target.value)}>{voiceModels.map((voice) => <option key={voice.id} value={voice.id}>{voice.name} · {voice.size}</option>)}</select></label>
            <label>Voice profile<select value={voiceSettings.profileId} onChange={(event) => updateVoice("profileId", event.target.value)}>{voiceProfiles.map((profile) => <option key={profile.id} value={profile.id}>{profile.name} · {profile.description}</option>)}</select></label>
            <label className="settings-toggle"><span><strong>Speak completed replies</strong><small>Voice output stays off until you turn it on.</small></span><input type="checkbox" checked={voiceSettings.voiceOutputEnabled} onChange={(event) => updateVoice("voiceOutputEnabled", event.target.checked)} /></label>
            <label className="settings-toggle"><span><strong>Use low-resource mode</strong><small>Use the separately assigned voice model on constrained laptops.</small></span><input type="checkbox" checked={voiceSettings.lowResourceMode} onChange={(event) => updateVoice("lowResourceMode", event.target.checked)} /></label>
            <div className="marketplace-voice-actions"><Button variant="secondary" onClick={() => void speakNativeResponse("Skai voice preview. This uses the operating system voice service.", true)}>Preview OS voice</Button><Button variant="ghost" onClick={stopNativeSpeech}>Stop preview</Button></div>
            <p className="marketplace-honesty">OS-native speech is installed and active now. Kokoro and Pocket TTS are selectable profiles, but remain unavailable until their C# model-pack adapter is installed.</p>
          </div> : <p className="marketplace-honesty">Discovery is available as a local presentation. Installation and activation stay disabled until a real backend is connected.</p>}
        </Card>}
      </div>
    </Card>
  </div>;
}
