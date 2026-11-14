import { useEffect, useMemo, useState, type ChangeEvent } from "react";
import type { Model, ThemeMode } from "../../app/types";
import { saveVoiceSetting, selectedVoiceSettings, speakNativeResponse, stopNativeSpeech, voiceModels, voiceProfiles } from "../../services/nativeTts";
import { isLocalProvider, localProviderOrFallback } from "../../services/localOnly";
import { Button, Card, PageHeader, StatusBadge } from "../../components/ui/Primitives";
import { NavIcon, type NavIconName } from "../../components/ui/NavIcon";

const categories = ["General", "Appearance", "Pet", "Models & Providers", "Voice & Audio", "Image Generation", "Agents", "Integrations", "Privacy & Security", "Storage", "Updates", "Advanced"] as const;
type Category = typeof categories[number];
const categoryIcons: Record<Category, NavIconName> = {
  General: "settings",
  Appearance: "sparkle",
  Pet: "sparkle",
  "Models & Providers": "model",
  "Voice & Audio": "activity",
  "Image Generation": "image",
  Agents: "code",
  Integrations: "project",
  "Privacy & Security": "settings",
  Storage: "memory",
  Updates: "arrow-up-right",
  Advanced: "terminal",
};

const categorySearchText: Record<Category, string> = {
  General: "runtime provider model answer local preferred",
  Appearance: "theme color accent density backdrop reduced motion pet companion animation",
  Pet: "companion Percy sprite sheet character walk wander always on top overlay custom",
  "Models & Providers": "endpoint runtime model discover Ollama LM Studio AirLLM provider local",
  "Voice & Audio": "voice speech audio low resource profile preview text to speech TTS",
  "Image Generation": "image ComfyUI workflow prompt endpoint generation",
  Agents: "task developer diagnostics memory prompts planning",
  Integrations: "GitHub local services ComfyUI integrations repository",
  "Privacy & Security": "privacy security local storage memory credentials data boundary",
  Storage: "export backup data workspace local memory",
  Updates: "release channel automatic update delivery",
  Advanced: "developer diagnostics motion routing provider",
};

const providerChoices = [
  { id: "Ollama", label: "Ollama", detail: "Run local models through Ollama on this computer." },
  { id: "LM Studio", label: "LM Studio", detail: "Use a local LM Studio server on this computer." },
  { id: "AirLLM", label: "AirLLM", detail: "Run very large models locally with AirLLM." },
];

type SettingsProps = {
  provider: string;
  endpoint: string;
  model: string;
  models: Model[];
  themeMode: ThemeMode;
  accentColor: string;
  density: string;
  backdrop: string;
  reduceMotion: boolean;
  petVisible: boolean;
  petWander: boolean;
  petCharacter: string;
  petSpriteDataUrl: string;
  developerMode: boolean;
  memoryInjectionEnabled: boolean;
  imageEndpoint: string;
  imageWorkflow: string;
  onChange: (key: string, value: string | boolean) => void;
  onSave: () => Promise<boolean>;
  onDiscoverModels: () => void | Promise<boolean>;
  onImageChange: (key: "endpoint" | "prompt" | "workflow", value: string) => void;
  onExportMemory: () => void;
};

export function SettingsPage(props: SettingsProps) {
  const [activeCategory, setActiveCategory] = useState<Category>("General");
  const [query, setQuery] = useState("");
  const [saveState, setSaveState] = useState<"idle" | "saving" | "saved">("idle");
  const [discoveringModels, setDiscoveringModels] = useState(false);
  const [voiceSettings, setVoiceSettings] = useState(selectedVoiceSettings);
  const normalizedQuery = query.trim().toLowerCase();
  const visibleCategories = useMemo(() => categories.filter((category) => !normalizedQuery || `${category} ${categoryDescription(category)} ${categorySearchText[category]}`.toLowerCase().includes(normalizedQuery)), [normalizedQuery]);
  useEffect(() => {
    if (!normalizedQuery || visibleCategories.includes(activeCategory)) return;
    const nextCategory = visibleCategories[0];
    if (nextCategory) setActiveCategory(nextCategory);
  }, [activeCategory, normalizedQuery, visibleCategories]);
  const change = (key: string, value: string | boolean) => { setSaveState("idle"); props.onChange(key, value); };
  const save = async () => { if (saveState === "saving") return; setSaveState("saving"); setSaveState(await props.onSave() ? "saved" : "idle"); };
  const discoverModels = async () => { if (discoveringModels) return; setDiscoveringModels(true); await props.onDiscoverModels(); setDiscoveringModels(false); };
  const updateVoice = (key: "profileId" | "primaryModelId" | "lowResourceModelId" | "lowResourceMode" | "voiceOutputEnabled", value: string | boolean) => { saveVoiceSetting(key, value); setVoiceSettings(selectedVoiceSettings()); };
  const selectedProviderId = localProviderOrFallback(props.provider);
  const selectedProvider = providerChoices.find((choice) => choice.id === selectedProviderId) ?? providerChoices[0];
  const localRoute = isLocalProvider(selectedProviderId);

  return <div className="feature-page settings-page">
    <PageHeader className="settings-page-header" eyebrow="CONTROL CENTER" title="Settings" description="Tune the local runtime, appearance, voice, media, memory, and workspace behavior." />
    <Card className="settings-window">
      <aside className="settings-nav"><p className="section-kicker">SETTINGS</p><input className="settings-search" value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search settings…" aria-label="Search settings" />{normalizedQuery && <p className="settings-search-status" role="status">{visibleCategories.length} matching {visibleCategories.length === 1 ? "section" : "sections"}</p>}{visibleCategories.map((category) => <button className={activeCategory === category ? "is-active" : ""} key={category} onClick={() => setActiveCategory(category)} type="button"><NavIcon name={categoryIcons[category]} /><span>{category}</span></button>)}{visibleCategories.length === 0 && <p className="settings-no-results">No settings match. Try a label such as endpoint, motion, or memory.</p>}</aside>
      <main className="settings-content">
        <div className="settings-section-heading"><div><p className="section-kicker">{activeCategory.toUpperCase()}</p><h3>{activeCategory}</h3><p>{categoryDescription(activeCategory)}</p></div><StatusBadge tone={activeCategory === "Integrations" ? "neutral" : "success"}>{activeCategory === "Integrations" ? "Review connections" : "Available"}</StatusBadge></div>

        {activeCategory === "General" && <><div className="settings-callout"><strong>{selectedProvider.label}</strong><span>{selectedProvider.detail}</span></div><div className="settings-field"><label>How should Skai answer?<select value={selectedProviderId} onChange={(event) => change("provider", event.target.value)}>{providerChoices.map((choice) => <option key={choice.id} value={choice.id}>{choice.label}</option>)}</select></label><label>Preferred model<select value={props.model} onChange={(event) => change("model", event.target.value)}>{props.models.length === 0 && <option value={props.model}>{props.model}</option>}{props.models.map((item) => <option key={item.model} value={item.model}>{item.model}</option>)}</select></label></div><p className="settings-help">All model runtimes run on this computer. Select a local provider and configure its loopback endpoint below.</p></>}

        {activeCategory === "Appearance" && <><div className="settings-field"><label>Color theme<select value={props.themeMode} onChange={(event) => change("themeMode", event.target.value)}><option value="dark">Dark</option><option value="light">Light</option><option value="system">System</option></select></label><label>Accent<select value={props.accentColor} onChange={(event) => change("accentColor", event.target.value)}><option value="cyan">Cyan</option><option value="violet">Violet</option><option value="amber">Amber</option><option value="rose">Rose</option></select></label></div><div className="settings-field"><label>Interface density<select value={props.density} onChange={(event) => change("density", event.target.value)}><option value="compact">Compact</option><option value="comfortable">Comfortable</option><option value="spacious">Spacious</option></select></label><label>Backdrop<select value={props.backdrop} onChange={(event) => change("backdrop", event.target.value)}><option value="none">None</option><option value="deep-sea">Deep sea</option><option value="deep-space">Deep space</option></select></label></div><Toggle label="Reduced motion" detail="Minimize interface animations and decorative movement." checked={props.reduceMotion} onChange={(value) => change("reduceMotion", value)} /></>}

        {activeCategory === "Pet" && <PetSettings props={props} change={change} />}

        {activeCategory === "Models & Providers" && <><div className="settings-field"><label>Provider<select value={selectedProviderId} onChange={(event) => change("provider", event.target.value)}>{providerChoices.map((choice) => <option key={choice.id} value={choice.id}>{choice.label}</option>)}</select></label><label>Model endpoint<input value={props.endpoint} onChange={(event) => change("endpoint", event.target.value)} placeholder="http://localhost:11434/v1" /></label></div><div className="settings-model-actions"><Button variant="secondary" disabled={discoveringModels} onClick={() => void discoverModels()}>{discoveringModels ? "Discovering…" : "Discover local models"}</Button><span>AirLLM can take over when a selected model exceeds available memory.</span></div><div className="settings-provider-list">{providerChoices.map((choice) => <div className="settings-provider-row" key={choice.id}><span><strong>{choice.label}</strong><small>{choice.detail}</small></span><StatusBadge tone="neutral">Supported locally</StatusBadge></div>)}</div></>}

        {activeCategory === "Voice & Audio" && <><div className="settings-field"><label>Primary voice runtime<select value={voiceSettings.primaryModelId} onChange={(event) => updateVoice("primaryModelId", event.target.value)}>{voiceModels.map((voice) => <option key={voice.id} value={voice.id}>{voice.name}</option>)}</select></label><label>Low-resource runtime<select value={voiceSettings.lowResourceModelId} onChange={(event) => updateVoice("lowResourceModelId", event.target.value)}>{voiceModels.map((voice) => <option key={voice.id} value={voice.id}>{voice.name}</option>)}</select></label></div><div className="settings-field"><label>Voice profile<select value={voiceSettings.profileId} onChange={(event) => updateVoice("profileId", event.target.value)}>{voiceProfiles.map((profile) => <option key={profile.id} value={profile.id}>{profile.name}</option>)}</select></label><div className="settings-inline-action"><span>Preview the current system voice.</span><Button variant="secondary" onClick={() => void speakNativeResponse("Skai voice preview. Your voice settings are working.", true)}>Preview</Button><Button variant="ghost" onClick={stopNativeSpeech}>Stop</Button></div></div><Toggle label="Speak completed replies" detail="Voice output stays off until you turn it on." checked={voiceSettings.voiceOutputEnabled} onChange={(value) => updateVoice("voiceOutputEnabled", value)} /><Toggle label="Use low-resource mode" detail="Prefer the smaller CPU-first voice runtime." checked={voiceSettings.lowResourceMode} onChange={(value) => updateVoice("lowResourceMode", value)} /></>}

        {activeCategory === "Image Generation" && <><div className="settings-field"><label>Image workflow endpoint<input value={props.imageEndpoint} onChange={(event) => props.onImageChange("endpoint", event.target.value)} placeholder="http://127.0.0.1:8188" /></label><label>Workflow JSON<input value={props.imageWorkflow} onChange={(event) => props.onImageChange("workflow", event.target.value)} placeholder="Paste a ComfyUI workflow JSON" /></label></div><p className="settings-help">Image generation currently uses a local ComfyUI-compatible workflow. The Images workspace is where you run it and review results.</p></>}

        {activeCategory === "Agents" && <><Toggle label="Developer mode" detail="Show extra diagnostics and development controls." checked={props.developerMode} onChange={(value) => change("developerMode", value)} /><Toggle label="Use saved memory in prompts" detail="Inject enabled memory entries into local conversations." checked={props.memoryInjectionEnabled} onChange={(value) => change("memoryInjectionEnabled", value)} /><div className="settings-callout"><strong>Safe by default</strong><span>Agent tasks are proposed before they run, and the selected environment is shown before execution.</span></div></>}

        {activeCategory === "Integrations" && <><Integration title="GitHub project workflow" detail="A GitHub connection is not configured in this Electron surface yet. No repository access is implied." status="Needs setup" /><Integration title="Local services" detail="Ollama, LM Studio, AirLLM, native voice, and ComfyUI use local endpoints." status={localRoute ? "Available" : "Needs setup"} /></>}

        {activeCategory === "Privacy & Security" && <><div className="settings-callout"><strong>Local route in use</strong><span>Prompts, attachments, memory, voice, and image requests stay on this computer.</span></div><p className="settings-help">GitHub is used only for explicit project workflows. Local model and media endpoints are restricted to loopback addresses.</p><Toggle label="Use saved memory in prompts" detail="This is also available in Agents so the setting is easy to find." checked={props.memoryInjectionEnabled} onChange={(value) => change("memoryInjectionEnabled", value)} /></>}

        {activeCategory === "Storage" && <><div className="settings-callout"><strong>Workspace data is local</strong><span>Conversations, task plans, memory, and preferences are stored in the local Skai data directory.</span></div><div className="settings-model-actions"><Button variant="secondary" onClick={props.onExportMemory}>Export memory backup</Button><span>Exports a portable JSON backup without deleting anything.</span></div><p className="settings-help">There is no destructive “clear everything” button in the release candidate.</p></>}

        {activeCategory === "Updates" && <><div className="settings-callout"><strong>Release candidate checks</strong><span>Skai is checked with desktop typecheck, production build, host build, native build, smoke tests, and hygiene audits before release.</span></div><div className="settings-provider-list"><div className="settings-provider-row"><span><strong>Current channel</strong><small>First major release preparation</small></span><StatusBadge tone="warning">Review required</StatusBadge></div><div className="settings-provider-row"><span><strong>Automatic updates</strong><small>Update delivery is not connected yet.</small></span><StatusBadge tone="warning">Planned</StatusBadge></div></div></>}

        {activeCategory === "Advanced" && <><Toggle label="Developer mode" detail="Show extra diagnostics and development controls." checked={props.developerMode} onChange={(value) => change("developerMode", value)} /><Toggle label="Reduced motion" detail="Also controls pet wandering defaults and decorative movement." checked={props.reduceMotion} onChange={(value) => change("reduceMotion", value)} /><p className="settings-help">Provider routing remains local-only. Diagnostics and motion settings apply across the workspace.</p></>}

        <div className="settings-footer"><span className={`settings-save-status settings-save-${saveState}`} role="status">{saveState === "saving" ? "Saving changes…" : saveState === "saved" ? "Saved locally" : "Changes apply across the workspace."}</span><Button variant="primary" disabled={saveState === "saving"} onClick={() => void save()}>{saveState === "saving" ? "Saving…" : "Save settings"}</Button></div>
      </main>
    </Card>
  </div>;
}

function categoryDescription(category: Category) {
  const descriptions: Record<Category, string> = { General: "Choose the local runtime and model Skai should use first.", Appearance: "Tune the visual environment without changing companion settings.", Pet: "Choose, show, and personalize Percy as a separate desktop companion window.", "Models & Providers": "Choose a model runtime already running on this computer.", "Voice & Audio": "Choose local speech behavior and preview it safely.", "Image Generation": "Configure the local image workflow used by the Images workspace.", Agents: "Control task planning, diagnostics, and memory injection.", Integrations: "Review the local services and project integrations currently connected.", "Privacy & Security": "Review the local data boundary and storage controls.", Storage: "Back up local workspace data without destructive shortcuts.", Updates: "See the release channel and update delivery status.", Advanced: "Review controls that affect diagnostics and motion." };
  return descriptions[category];
}

function Toggle({ label, detail, checked, onChange }: { label: string; detail: string; checked: boolean; onChange: (value: boolean) => void }) {
  return <label className="settings-toggle"><span><strong>{label}</strong><small>{detail}</small></span><input type="checkbox" checked={checked} onChange={(event) => onChange(event.target.checked)} /></label>;
}

function PetSettings({ props, change }: { props: SettingsProps; change: (key: string, value: string | boolean) => void }) {
  const [character, setCharacter] = useState(props.petCharacter);
  const [sprite, setSprite] = useState(props.petSpriteDataUrl);
  const onSpriteChange = (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file || !file.type.startsWith("image/") || file.size > 5 * 1024 * 1024) return;
    const reader = new FileReader();
    reader.onload = () => { if (typeof reader.result === "string") { setSprite(reader.result); localStorage.setItem("skai.pet.sprite", reader.result); change("petSpriteDataUrl", reader.result); } };
    reader.readAsDataURL(file);
  };
  return <>
    <div className="settings-callout"><strong>Separate desktop companion</strong><span>Percy is its own transparent, always-on-top window. It can be dragged anywhere and stays independent from the Skai workspace.</span></div>
    <Toggle label="Show Percy" detail="Open the companion above your other windows." checked={props.petVisible} onChange={(value) => change("petVisible", value)} />
    <Toggle label="Let Percy walk around" detail="Allow gentle ambient movement while Percy is idle. Dragging always pauses movement." checked={props.petWander} onChange={(value) => change("petWander", value)} />
    <div className="settings-field"><label>Companion<select value={character} onChange={(event) => { setCharacter(event.target.value); localStorage.setItem("skai.pet.character", event.target.value); change("petCharacter", event.target.value); }}><option value="percy">Percy · Skai wolf</option><option value="byte">Byte · neon bot</option><option value="ember">Ember · ember fox</option><option value="nova">Nova · star cat</option><option value="rocky">Rocky · pocket rock</option><option value="sprout">Sprout · tiny plant</option></select></label><div className="pet-settings-preview"><img src={sprite || `/assets/pets/${character}-idle-1.png`} alt="" /><span>{sprite ? "Custom sprite sheet" : "Built-in sprite set"}</span></div></div>
    <div className="settings-provider-row pet-sprite-import"><span><strong>Use your own sprite sheet</strong><small>Import a PNG, WebP, or GIF up to 5 MB. Use a six-frame horizontal strip; it is stored locally in this profile.</small></span><label className="ui-button ui-button-secondary"><span>Choose image</span><input type="file" accept="image/png,image/webp,image/gif" onChange={onSpriteChange} /></label></div>
    {sprite && <div className="settings-inline-action"><span>Your custom sprite is active.</span><Button variant="ghost" onClick={() => { setSprite(""); localStorage.removeItem("skai.pet.sprite"); change("petSpriteDataUrl", ""); }}>Use built-in sprites</Button></div>}
  </>;
}

function Integration({ title, detail, status }: { title: string; detail: string; status: "Available" | "Needs setup" | "Planned" }) {
  return <div className="settings-provider-row"><span><strong>{title}</strong><small>{detail}</small></span><StatusBadge tone={status === "Available" ? "success" : status === "Needs setup" ? "warning" : "neutral"}>{status}</StatusBadge></div>;
}
