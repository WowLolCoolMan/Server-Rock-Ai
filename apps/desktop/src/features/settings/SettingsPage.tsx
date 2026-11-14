import { useMemo, useState } from "react";
import type { Model, ThemeMode } from "../../app/types";
import { saveVoiceSetting, selectedVoiceSettings, speakNativeResponse, stopNativeSpeech, voiceModels, voiceProfiles } from "../../services/nativeTts";
import { Button, Card, PageHeader, StatusBadge } from "../../components/ui/Primitives";

const categories = ["General", "Appearance", "Models & Providers", "Voice & Audio", "Image Generation", "Agents", "Integrations", "Privacy & Security", "Storage", "Updates", "Advanced"] as const;
type Category = typeof categories[number];

const providerChoices = [
  { id: "Ollama", label: "Local models", detail: "Run on this computer with Ollama and AirLLM." },
  { id: "OpenAI API", label: "OpenAI API", detail: "Use an OpenAI API key. Secure key storage is not connected yet." },
  { id: "ChatGPT", label: "ChatGPT sign-in", detail: "Use ChatGPT account access. This requires a separate sign-in flow." },
  { id: "Custom", label: "Another cloud service", detail: "OpenAI-compatible cloud endpoints are planned for a later release." },
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
  developerMode: boolean;
  memoryInjectionEnabled: boolean;
  imageEndpoint: string;
  imageWorkflow: string;
  onChange: (key: string, value: string | boolean) => void;
  onSave: () => Promise<boolean>;
  onDiscoverModels: () => void;
  onImageChange: (key: "endpoint" | "prompt" | "workflow", value: string) => void;
  onExportMemory: () => void;
};

export function SettingsPage(props: SettingsProps) {
  const [activeCategory, setActiveCategory] = useState<Category>("General");
  const [query, setQuery] = useState("");
  const [saveState, setSaveState] = useState<"idle" | "saving" | "saved">("idle");
  const [voiceSettings, setVoiceSettings] = useState(selectedVoiceSettings);
  const visibleCategories = useMemo(() => categories.filter((category) => category.toLowerCase().includes(query.trim().toLowerCase())), [query]);
  const change = (key: string, value: string | boolean) => { setSaveState("idle"); props.onChange(key, value); };
  const save = async () => { if (saveState === "saving") return; setSaveState("saving"); setSaveState(await props.onSave() ? "saved" : "idle"); };
  const updateVoice = (key: "profileId" | "primaryModelId" | "lowResourceModelId" | "lowResourceMode" | "voiceOutputEnabled", value: string | boolean) => { saveVoiceSetting(key, value); setVoiceSettings(selectedVoiceSettings()); };
  const selectedProvider = providerChoices.find((choice) => choice.id === props.provider) ?? { id: props.provider, label: props.provider || "Local models", detail: "This saved route uses a local endpoint." };
  const visibleProviderChoices = providerChoices.some((choice) => choice.id === props.provider) ? providerChoices : [selectedProvider, ...providerChoices];
  const localRoute = !["OpenAI API", "ChatGPT", "Custom"].includes(props.provider);

  return <div className="feature-page settings-page">
    <PageHeader eyebrow="CONTROL CENTER" title="Settings" description="Everything important is grouped here, with clear labels for what runs on this computer and what still needs an online connection." />
    <Card className="settings-window">
      <aside className="settings-nav"><p className="section-kicker">SETTINGS</p><input className="settings-search" value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search settings…" aria-label="Search settings" />{visibleCategories.map((category) => <button className={activeCategory === category ? "is-active" : ""} key={category} onClick={() => setActiveCategory(category)} type="button">{category}</button>)}{visibleCategories.length === 0 && <p className="settings-no-results">No categories match.</p>}</aside>
      <main className="settings-content">
        <div className="settings-section-heading"><div><p className="section-kicker">{activeCategory.toUpperCase()}</p><h3>{activeCategory}</h3><p>{categoryDescription(activeCategory)}</p></div><StatusBadge tone={activeCategory === "Integrations" && !localRoute ? "warning" : "success"}>{activeCategory === "Integrations" && !localRoute ? "Needs setup" : "Available"}</StatusBadge></div>

        {activeCategory === "General" && <><div className="settings-callout"><strong>{selectedProvider.label}</strong><span>{selectedProvider.detail}</span></div><div className="settings-field"><label>How should Skai answer?<select value={props.provider} onChange={(event) => change("provider", event.target.value)}>{visibleProviderChoices.map((choice) => <option key={choice.id} value={choice.id}>{choice.label}</option>)}</select></label><label>Preferred model<select value={props.model} onChange={(event) => change("model", event.target.value)}>{props.models.length === 0 && <option value={props.model}>{props.model}</option>}{props.models.map((item) => <option key={item.model} value={item.model}>{item.model}</option>)}</select></label></div><p className="settings-help">Local models are ready now. Cloud choices are shown so the route is understandable, but Skai will not pretend they work until their secure sign-in or key storage is installed.</p></>}

        {activeCategory === "Appearance" && <><div className="settings-field"><label>Color theme<select value={props.themeMode} onChange={(event) => change("themeMode", event.target.value)}><option value="dark">Dark</option><option value="light">Light</option><option value="system">System</option></select></label><label>Accent<select value={props.accentColor} onChange={(event) => change("accentColor", event.target.value)}><option value="cyan">Cyan</option><option value="violet">Violet</option><option value="amber">Amber</option><option value="rose">Rose</option></select></label></div><div className="settings-field"><label>Interface density<select value={props.density} onChange={(event) => change("density", event.target.value)}><option value="compact">Compact</option><option value="comfortable">Comfortable</option><option value="spacious">Spacious</option></select></label><label>Backdrop<select value={props.backdrop} onChange={(event) => change("backdrop", event.target.value)}><option value="none">None</option><option value="deep-sea">Deep sea</option><option value="deep-space">Deep space</option></select></label></div><Toggle label="Reduced motion" detail="Minimize interface animations and decorative movement." checked={props.reduceMotion} onChange={(value) => change("reduceMotion", value)} /><Toggle label="Show Skai pet" detail="Keep the companion visible above the workspace." checked={props.petVisible} onChange={(value) => change("petVisible", value)} /><Toggle label="Let the pet walk around" detail="The pet wanders gently and pauses when you drag it." checked={props.petWander} onChange={(value) => change("petWander", value)} /></>}

        {activeCategory === "Models & Providers" && <><div className="settings-field"><label>Provider<select value={props.provider} onChange={(event) => change("provider", event.target.value)}>{visibleProviderChoices.map((choice) => <option key={choice.id} value={choice.id}>{choice.label}</option>)}</select></label><label>Model endpoint<input value={props.endpoint} onChange={(event) => change("endpoint", event.target.value)} placeholder="http://localhost:11434/v1" /></label></div><div className="settings-model-actions"><Button variant="secondary" onClick={props.onDiscoverModels} disabled={!localRoute}>Discover local models</Button><span>{localRoute ? "AirLLM can take over when a selected model exceeds available memory." : "This route is not connected in the current release."}</span></div><div className="settings-provider-list">{providerChoices.map((choice) => <div className="settings-provider-row" key={choice.id}><span><strong>{choice.label}</strong><small>{choice.detail}</small></span><StatusBadge tone={choice.id === "Ollama" ? "success" : "warning"}>{choice.id === "Ollama" ? "Ready" : "Planned"}</StatusBadge></div>)}</div></>}

        {activeCategory === "Voice & Audio" && <><div className="settings-field"><label>Primary voice runtime<select value={voiceSettings.primaryModelId} onChange={(event) => updateVoice("primaryModelId", event.target.value)}>{voiceModels.map((voice) => <option key={voice.id} value={voice.id}>{voice.name}</option>)}</select></label><label>Low-resource runtime<select value={voiceSettings.lowResourceModelId} onChange={(event) => updateVoice("lowResourceModelId", event.target.value)}>{voiceModels.map((voice) => <option key={voice.id} value={voice.id}>{voice.name}</option>)}</select></label></div><div className="settings-field"><label>Voice profile<select value={voiceSettings.profileId} onChange={(event) => updateVoice("profileId", event.target.value)}>{voiceProfiles.map((profile) => <option key={profile.id} value={profile.id}>{profile.name}</option>)}</select></label><div className="settings-inline-action"><span>Preview the current system voice.</span><Button variant="secondary" onClick={() => void speakNativeResponse("Skai voice preview. Your voice settings are working.", true)}>Preview</Button><Button variant="ghost" onClick={stopNativeSpeech}>Stop</Button></div></div><Toggle label="Speak completed replies" detail="Voice output stays off until you turn it on." checked={voiceSettings.voiceOutputEnabled} onChange={(value) => updateVoice("voiceOutputEnabled", value)} /><Toggle label="Use low-resource mode" detail="Prefer the smaller CPU-first voice runtime." checked={voiceSettings.lowResourceMode} onChange={(value) => updateVoice("lowResourceMode", value)} /></>}

        {activeCategory === "Image Generation" && <><div className="settings-field"><label>Image workflow endpoint<input value={props.imageEndpoint} onChange={(event) => props.onImageChange("endpoint", event.target.value)} placeholder="http://127.0.0.1:8188" /></label><label>Workflow JSON<input value={props.imageWorkflow} onChange={(event) => props.onImageChange("workflow", event.target.value)} placeholder="Paste a ComfyUI workflow JSON" /></label></div><p className="settings-help">Image generation currently uses a local ComfyUI-compatible workflow. The Images workspace is where you run it and review results.</p></>}

        {activeCategory === "Agents" && <><Toggle label="Developer mode" detail="Show extra diagnostics and development controls." checked={props.developerMode} onChange={(value) => change("developerMode", value)} /><Toggle label="Use saved memory in prompts" detail="Inject enabled memory entries into local conversations." checked={props.memoryInjectionEnabled} onChange={(value) => change("memoryInjectionEnabled", value)} /><div className="settings-callout"><strong>Safe by default</strong><span>Agent tasks are proposed before they run, and the selected environment is shown before execution.</span></div></>}

        {activeCategory === "Integrations" && <><Integration title="OpenAI API" detail="API key sign-in and secure credential storage are not wired into this release." status="Planned" /><Integration title="ChatGPT sign-in" detail="ChatGPT account access is separate from API keys and still needs its own secure authentication flow." status="Planned" /><Integration title="ElevenLabs voice" detail="Cloud voice is planned behind the same explicit provider and consent boundary." status="Planned" /><Integration title="Local services" detail="Ollama, AirLLM fallback, native voice, and ComfyUI use local endpoints." status="Ready" /></>}

        {activeCategory === "Privacy & Security" && <><div className="settings-callout"><strong>Local route in use</strong><span>Chat, model discovery, memory, voice, and image requests stay on this computer while the provider is Local models.</span></div><p className="settings-help">A cloud provider must be selected explicitly. Skai does not silently send local data to an online service, and API keys will be handled by a secure credential store when that integration is released.</p><Toggle label="Use saved memory in prompts" detail="This is also available in Agents so the setting is easy to find." checked={props.memoryInjectionEnabled} onChange={(value) => change("memoryInjectionEnabled", value)} /></>}

        {activeCategory === "Storage" && <><div className="settings-callout"><strong>Workspace data is local</strong><span>Conversations, task plans, memory, and preferences are stored in the local Skai data directory.</span></div><div className="settings-model-actions"><Button variant="secondary" onClick={props.onExportMemory}>Export memory backup</Button><span>Exports a portable JSON backup without deleting anything.</span></div><p className="settings-help">There is no destructive “clear everything” button in the release candidate.</p></>}

        {activeCategory === "Updates" && <><div className="settings-callout"><strong>Release candidate checks</strong><span>Skai is checked with desktop typecheck, production build, host build, native build, smoke tests, and hygiene audits before release.</span></div><div className="settings-provider-list"><div className="settings-provider-row"><span><strong>Current channel</strong><small>First major release preparation</small></span><StatusBadge tone="warning">Review required</StatusBadge></div><div className="settings-provider-row"><span><strong>Automatic updates</strong><small>Update delivery is not connected yet.</small></span><StatusBadge tone="warning">Planned</StatusBadge></div></div></>}

        {activeCategory === "Advanced" && <><Toggle label="Developer mode" detail="Show extra diagnostics and development controls." checked={props.developerMode} onChange={(value) => change("developerMode", value)} /><Toggle label="Reduced motion" detail="Also controls pet wandering defaults and decorative movement." checked={props.reduceMotion} onChange={(value) => change("reduceMotion", value)} /><p className="settings-help">Advanced cloud credentials, plugins, and automatic update channels are intentionally not presented as working until their runtime contracts are complete.</p></>}

        <div className="settings-footer"><span className={`settings-save-status settings-save-${saveState}`} role="status">{saveState === "saving" ? "Saving changes…" : saveState === "saved" ? "Saved locally" : "Changes apply across the workspace."}</span><Button variant="primary" disabled={saveState === "saving"} onClick={() => void save()}>{saveState === "saving" ? "Saving…" : "Save settings"}</Button></div>
      </main>
    </Card>
  </div>;
}

function categoryDescription(category: Category) {
  const descriptions: Record<Category, string> = { General: "Choose the route and model Skai should use first.", Appearance: "Tune the visual environment and companion behavior.", "Models & Providers": "Understand where a model runs before you connect to it.", "Voice & Audio": "Choose local speech behavior and preview it safely.", "Image Generation": "Configure the local image workflow used by the Images workspace.", Agents: "Control task planning, diagnostics, and memory injection.", Integrations: "See which external services are ready and which still need setup.", "Privacy & Security": "Make the local-versus-cloud boundary visible.", Storage: "Back up local workspace data without destructive shortcuts.", Updates: "See the release channel and update delivery status.", Advanced: "Review controls that affect diagnostics and motion." };
  return descriptions[category];
}

function Toggle({ label, detail, checked, onChange }: { label: string; detail: string; checked: boolean; onChange: (value: boolean) => void }) {
  return <label className="settings-toggle"><span><strong>{label}</strong><small>{detail}</small></span><input type="checkbox" checked={checked} onChange={(event) => onChange(event.target.checked)} /></label>;
}

function Integration({ title, detail, status }: { title: string; detail: string; status: "Ready" | "Planned" }) {
  return <div className="settings-provider-row"><span><strong>{title}</strong><small>{detail}</small></span><StatusBadge tone={status === "Ready" ? "success" : "warning"}>{status}</StatusBadge></div>;
}
