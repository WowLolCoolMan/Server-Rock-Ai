import { useCallback, useEffect, useMemo, useState } from "react";
import type { AgentResponse, DesktopConversation, DesktopScheduledTask, DesktopTask, RuntimeHealth, ScheduledTaskDraft, TaskEnvironment } from "../../shared/contracts";
import { PRODUCT_DISPLAY_NAME } from "../../shared/productIdentity";
import type { AppRouteActions, AppRouteState } from "./AppRouteView";
import { routeFromLocation, navigate } from "./navigation";
import type { ConnectionState, MemoryEntry, Model, Route, ThemeMode } from "./types";
import { createAgentClient } from "../services/agentClient";
import { CLOUD_ENDPOINT_ERROR, isAllowedModelEndpoint, isLocalEndpoint, isRemoteEndpoint, LOCAL_ENDPOINT_ERROR, localEndpointOrFallback } from "../services/localOnly";
import { useChatWorkspace } from "../features/chat/useChatWorkspace";
import { useCodingWorkspace } from "../features/coding/useCodingWorkspace";

export type DesktopWorkspace = {
  routeState: AppRouteState;
  routeActions: AppRouteActions;
  route: Route;
  connection: ConnectionState;
  health: RuntimeHealth | null;
  error: string | null;
  conversations: DesktopConversation[];
  conversationQuery: string;
  setConversationQuery: (query: string) => void;
  sidebarCollapsed: boolean;
  toggleSidebar: () => void;
  commandOpen: boolean;
  openCommandPalette: () => void;
  closeCommandPalette: () => void;
  startNewChat: () => void;
  selectConversation: (id: string) => void;
  updateConversationMetadata: (id: string, patch: { pinned?: boolean; archived?: boolean; title?: string }) => Promise<boolean>;
  onNavigate: (route: Route) => void;
  petVisible: boolean;
  petWander: boolean;
  setPetVisible: (value: boolean) => void;
  setPetWander: (value: boolean) => void;
};

const defaultEndpoint = localEndpointOrFallback(localStorage.getItem("skai.endpoint"), "http://localhost:11434/v1");
const defaultModel = localStorage.getItem("skai.model") ?? "llama3.2";
const defaultProvider = localStorage.getItem("skai.provider") ?? "Ollama";

export function useDesktopWorkspace(): DesktopWorkspace {
  const agent = useMemo(() => createAgentClient((method, params) => window.serverRockAgent.request(method, params)), []);
  const request = agent.request;
  const [route, setRoute] = useState<Route>(routeFromLocation);
  const [commandOpen, setCommandOpen] = useState(false);
  const [sidebarCollapsed, setSidebarCollapsed] = useState(localStorage.getItem("skai.sidebar.collapsed") === "true");
  const [conversationQuery, setConversationQuery] = useState("");
  const [connection, setConnection] = useState<ConnectionState>("checking");
  const [health, setHealth] = useState<RuntimeHealth | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [endpoint, setEndpoint] = useState(defaultEndpoint);
  const [model, setModel] = useState(defaultModel);
  const [provider, setProvider] = useState(defaultProvider);
  const [cloudRouteEnabled, setCloudRouteEnabled] = useState(false);
  const [credentialConfigured, setCredentialConfigured] = useState(false);
  const [credentialStorageAvailable, setCredentialStorageAvailable] = useState(false);
  const [models, setModels] = useState<Model[]>([]);
  const [tasks, setTasks] = useState<DesktopTask[]>([]);
  const [scheduledTasks, setScheduledTasks] = useState<DesktopScheduledTask[]>([]);
  const [tasksLoading, setTasksLoading] = useState(false);
  const [tasksError, setTasksError] = useState<string | null>(null);
  const [taskBusy, setTaskBusy] = useState<string | null>(null);
  const [memoryEntries, setMemoryEntries] = useState<MemoryEntry[]>([]);
  const [memoryBusy, setMemoryBusy] = useState(false);
  const [memoryLoading, setMemoryLoading] = useState(false);
  const [memoryError, setMemoryError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [themeMode, setThemeMode] = useState<ThemeMode>("dark");
  const [accentColor, setAccentColor] = useState("cyan");
  const [density, setDensity] = useState("comfortable");
  const [backdrop, setBackdrop] = useState("none");
  const [reduceMotion, setReduceMotion] = useState(false);
  const [petVisible, setPetVisible] = useState(localStorage.getItem("skai.pet.visible") !== "false");
  const [petWander, setPetWander] = useState(localStorage.getItem("skai.pet.wander") !== "false");
  const [developerMode, setDeveloperMode] = useState(false);
  const [memoryInjectionEnabled, setMemoryInjectionEnabled] = useState(false);
  const [imageEndpoint, setImageEndpoint] = useState(localEndpointOrFallback(localStorage.getItem("skai.imageEndpoint"), "http://127.0.0.1:8188"));
  const [imagePrompt, setImagePrompt] = useState("");
  const [imageWorkflow, setImageWorkflow] = useState(localStorage.getItem("skai.imageWorkflow") ?? "");
  const [imageBusy, setImageBusy] = useState(false);
  const [generatedImages, setGeneratedImages] = useState<Array<{ url: string; fileName: string }>>([]);

  const chat = useChatWorkspace({ request, connection, provider, endpoint, model, cloudRouteEnabled, onError: setError });

  const onNavigate = useCallback((nextRoute: Route) => navigate(nextRoute), []);
  const coding = useCodingWorkspace({ request, connection, onError: setError, onNavigate: () => onNavigate("coding") });
  const openCommandPalette = useCallback(() => setCommandOpen(true), []);
  const closeCommandPalette = useCallback(() => setCommandOpen(false), []);
  const toggleSidebar = useCallback(() => setSidebarCollapsed((value) => !value), []);
  const startNewChat = useCallback(() => { chat.startNewChat(); onNavigate("chat"); }, [chat, onNavigate]);
  useEffect(() => { const onPopState = () => setRoute(routeFromLocation()); window.addEventListener("popstate", onPopState); return () => window.removeEventListener("popstate", onPopState); }, []);
  useEffect(() => { document.title = `${PRODUCT_DISPLAY_NAME} · ${route}`; document.documentElement.dataset.theme = themeMode; document.documentElement.dataset.accent = accentColor; document.documentElement.dataset.density = density; document.documentElement.dataset.backdrop = backdrop; document.documentElement.dataset.reduceMotion = reduceMotion ? "true" : "false"; }, [route, themeMode, accentColor, density, backdrop, reduceMotion]);
  useEffect(() => { localStorage.setItem("skai.sidebar.collapsed", String(sidebarCollapsed)); }, [sidebarCollapsed]);
  useEffect(() => { localStorage.setItem("skai.pet.visible", String(petVisible)); }, [petVisible]);
  useEffect(() => { localStorage.setItem("skai.pet.wander", String(petWander)); }, [petWander]);
  useEffect(() => { const onShortcut = (event: KeyboardEvent) => { const modifier = event.ctrlKey || event.metaKey; const key = event.key.toLowerCase(); if (modifier && key === "k") { event.preventDefault(); openCommandPalette(); return; } if (event.key === "Escape") { closeCommandPalette(); return; } if (!modifier) return; if (key === "n") { event.preventDefault(); startNewChat(); return; } if (key === ",") { event.preventDefault(); onNavigate("settings"); return; } if (key === "b") { event.preventDefault(); toggleSidebar(); return; } if (key === "1") { event.preventDefault(); onNavigate("chat"); return; } if (key === "2") { event.preventDefault(); onNavigate("coding"); return; } if (key === "3" || (event.shiftKey && key === "l")) { event.preventDefault(); onNavigate("terminal"); } }; window.addEventListener("keydown", onShortcut); return () => window.removeEventListener("keydown", onShortcut); }, [closeCommandPalette, openCommandPalette, onNavigate, startNewChat, toggleSidebar]);
  useEffect(() => { request("runtime.health").then((response: AgentResponse) => { if (!response.ok) { setConnection("error"); setError(response.error?.message ?? "Agent host unavailable."); return; } setHealth(response.result as RuntimeHealth); setConnection("ready"); }).catch((requestError: unknown) => { setConnection("error"); setError(requestError instanceof Error ? requestError.message : "Agent host unavailable."); }); }, [request]);
  useEffect(() => { if (connection !== "ready") return; void request("settings.get").then((settingsResponse) => { if (settingsResponse.ok) { const settings = settingsResponse.result as { endpoint?: string; model?: string; provider?: string; cloudRouteEnabled?: boolean; themeMode?: string; accentColor?: string; interfaceDensity?: string; backdropPreset?: string; reduceMotion?: boolean; developerMode?: boolean; memoryInjectionEnabled?: boolean }; if (settings.endpoint && (isLocalEndpoint(settings.endpoint) || (settings.cloudRouteEnabled === true && isRemoteEndpoint(settings.endpoint)))) setEndpoint(settings.endpoint); else if (settings.endpoint) setError(isRemoteEndpoint(settings.endpoint) ? CLOUD_ENDPOINT_ERROR : LOCAL_ENDPOINT_ERROR); if (typeof settings.cloudRouteEnabled === "boolean") setCloudRouteEnabled(settings.cloudRouteEnabled); if (settings.model) setModel(settings.model); if (settings.provider) setProvider(settings.provider); if (settings.themeMode === "light" || settings.themeMode === "dark" || settings.themeMode === "system") setThemeMode(settings.themeMode); if (settings.accentColor) setAccentColor(settings.accentColor); if (settings.interfaceDensity) setDensity(settings.interfaceDensity); if (settings.backdropPreset) setBackdrop(settings.backdropPreset); if (typeof settings.reduceMotion === "boolean") setReduceMotion(settings.reduceMotion); if (typeof settings.developerMode === "boolean") setDeveloperMode(settings.developerMode); if (typeof settings.memoryInjectionEnabled === "boolean") setMemoryInjectionEnabled(settings.memoryInjectionEnabled); } else setError(settingsResponse.error?.message ?? "Workspace settings could not be loaded."); }).catch(() => setError("Workspace settings could not be loaded.")); }, [connection, request]);
  const loadWorkbench = useCallback(async () => { setTasksLoading(true); setTasksError(null); try { const [taskResponse, scheduleResponse] = await Promise.all([request("tasks.list"), request("scheduled.list")]); if (!taskResponse.ok) throw new Error(taskResponse.error?.message ?? "Agent tasks could not be loaded."); if (!scheduleResponse.ok) throw new Error(scheduleResponse.error?.message ?? "Scheduled runs could not be loaded."); setTasks((taskResponse.result as DesktopTask[]) ?? []); setScheduledTasks((scheduleResponse.result as DesktopScheduledTask[]) ?? []); } catch (loadError) { setTasksError(loadError instanceof Error ? loadError.message : "Agent workbench could not be loaded."); } finally { setTasksLoading(false); } }, [request]);
  useEffect(() => { if (connection !== "ready" || route !== "workbench") return; void loadWorkbench(); }, [connection, route, loadWorkbench]);
  useEffect(() => { if (connection !== "ready" || route !== "memory") return; setMemoryLoading(true); setMemoryError(null); void request("memory.list").then((response) => { if (!response.ok) { setMemoryError(response.error?.message ?? "Memory could not be loaded."); return; } setMemoryEntries((response.result as MemoryEntry[]) ?? []); }).catch(() => setMemoryError("Memory could not be loaded.")).finally(() => setMemoryLoading(false)); }, [connection, route, request]);

  const transitionTask = async (id: string, operation: "queue" | "cancel" | "retry") => { setTaskBusy(id); const response = await request(`tasks.${operation}`, { id }); setTaskBusy(null); if (!response.ok) { setError(response.error?.message ?? "Task could not be updated."); return; } await loadWorkbench(); };
  const scheduleTask = async (draft: ScheduledTaskDraft) => { setTaskBusy("schedule"); const response = await request("scheduled.create", draft as unknown as Record<string, unknown>); setTaskBusy(null); if (!response.ok) { setError(response.error?.message ?? "Scheduled task could not be created."); return false; } await loadWorkbench(); setError(null); return true; };
  const transitionScheduledTask = async (id: string, operation: "pause" | "resume" | "cancel" | "runNow") => { setTaskBusy(id); const response = await request(`scheduled.${operation}`, { id }); setTaskBusy(null); if (!response.ok) { setError(response.error?.message ?? "Scheduled task could not be updated."); return false; } await loadWorkbench(); return true; };
  const planTask = async (title: string, instruction: string, environment: TaskEnvironment) => { setTaskBusy("planning"); const response = await request("tasks.plan", { title, instruction, environment }); setTaskBusy(null); if (!response.ok) { setError(response.error?.message ?? "Task could not be planned."); return false; } setError(null); return true; };
  const loadMemory = async (query?: string) => { setMemoryLoading(true); setMemoryError(null); try { const response = await request(query?.trim() ? "memory.search" : "memory.list", query?.trim() ? { query: query.trim() } : undefined); if (!response.ok) throw new Error(response.error?.message ?? "Memory could not be loaded."); setMemoryEntries((response.result as MemoryEntry[]) ?? []); } catch (loadError) { setMemoryError(loadError instanceof Error ? loadError.message : "Memory could not be loaded."); } finally { setMemoryLoading(false); } };
  const saveMemory = async (content: string, scope: string, tags: string, id?: string) => { setMemoryBusy(true); const response = await request("memory.save", { ...(id ? { id } : {}), content, scope, source: "user", approvalId: `electron-${Date.now()}`, tags: tags.split(",").map((tag) => tag.trim()).filter(Boolean) }); setMemoryBusy(false); if (!response.ok) { setError(response.error?.message ?? "Memory could not be saved."); return false; } await loadMemory(); return true; };
  const toggleMemory = async (entry: MemoryEntry) => { setMemoryBusy(true); const response = await request("memory.enabled", { id: entry.id, enabled: !entry.enabled }); setMemoryBusy(false); if (!response.ok) setError(response.error?.message ?? "Memory state could not be changed."); else await loadMemory(); };
  const removeMemory = async (entry: MemoryEntry) => { setMemoryBusy(true); const response = await request("memory.remove", { id: entry.id }); setMemoryBusy(false); if (!response.ok) { setError(response.error?.message ?? "Memory could not be removed."); return false; } await loadMemory(); return true; };
  const exportMemory = async () => { const response = await request("memory.export"); if (!response.ok) { setError(response.error?.message ?? "Memory export failed."); return; } const result = response.result as { fileName: string; json: string }; const link = document.createElement("a"); link.href = URL.createObjectURL(new Blob([result.json], { type: "application/json" })); link.download = result.fileName; link.click(); URL.revokeObjectURL(link.href); };
  const credentialProviderId = (targetProvider: string) => ["OpenAI API", "Custom"].includes(targetProvider) ? targetProvider : null;
  const refreshCredentialStatus = useCallback(async (targetProvider = provider) => {
    const credentialProvider = credentialProviderId(targetProvider);
    if (!credentialProvider) { setCredentialConfigured(false); setCredentialStorageAvailable(false); return; }
    const response = await request("credentials.status", { provider: credentialProvider });
    if (!response.ok) { setCredentialConfigured(false); setCredentialStorageAvailable(false); return; }
    const result = response.result as { configured?: boolean; available?: boolean };
    setCredentialConfigured(result.configured === true);
    setCredentialStorageAvailable(result.available === true);
  }, [provider, request]);
  useEffect(() => { if (connection === "ready") void refreshCredentialStatus(); }, [connection, provider, refreshCredentialStatus]);
  const saveCredential = async (targetProvider: string, apiKey: string) => {
    const credentialProvider = credentialProviderId(targetProvider);
    if (!credentialProvider) { setError("This provider uses a separate sign-in flow and cannot accept an API key here."); return false; }
    const response = await request("credentials.save", { provider: credentialProvider, apiKey });
    if (!response.ok) { setError(response.error?.message ?? "The provider key could not be saved securely."); return false; }
    setCredentialConfigured(true); setCredentialStorageAvailable(true); setError(null); return true;
  };
  const clearCredential = async (targetProvider: string) => {
    const credentialProvider = credentialProviderId(targetProvider);
    if (!credentialProvider) return false;
    const response = await request("credentials.clear", { provider: credentialProvider });
    if (!response.ok) { setError(response.error?.message ?? "The provider key could not be removed."); return false; }
    setCredentialConfigured(false); setError(null); return true;
  };
  const discoverModels = async () => { if (!isAllowedModelEndpoint(endpoint, cloudRouteEnabled)) { setError(isRemoteEndpoint(endpoint) ? CLOUD_ENDPOINT_ERROR : LOCAL_ENDPOINT_ERROR); return; } if (provider === "ChatGPT") { setError("ChatGPT sign-in is not connected yet. Use OpenAI API or Another cloud service with an API key."); return; } const response = await request("models.list", { provider, endpoint, cloudRouteEnabled }); if (!response.ok) { setError(response.error?.message ?? "Model discovery failed."); return; } setModels((response.result as Model[]) ?? []); };
  const saveSettings = async () => { if (!isAllowedModelEndpoint(endpoint, cloudRouteEnabled)) { setError(isRemoteEndpoint(endpoint) ? CLOUD_ENDPOINT_ERROR : LOCAL_ENDPOINT_ERROR); return false; } if (isRemoteEndpoint(endpoint) && provider === "ChatGPT") { setError("ChatGPT sign-in is not connected yet. Use OpenAI API or Another cloud service with an API key."); return false; } const response = await request("settings.save", { provider, endpoint, cloudRouteEnabled, model, themeMode, accentColor, interfaceDensity: density, backdropPreset: backdrop, reduceMotion, developerMode, memoryInjectionEnabled }); if (!response.ok) { setError(response.error?.message ?? "Settings could not be saved."); return false; } localStorage.setItem("skai.provider", provider); localStorage.setItem("skai.model", model); localStorage.setItem("skai.imageEndpoint", imageEndpoint); localStorage.setItem("skai.imageWorkflow", imageWorkflow); setError(null); return true; };
  const generateImage = async () => { if (!isLocalEndpoint(imageEndpoint)) { setError(LOCAL_ENDPOINT_ERROR); return; } setImageBusy(true); setError(null); localStorage.setItem("skai.imageEndpoint", imageEndpoint); localStorage.setItem("skai.imageWorkflow", imageWorkflow); const response = await request("image.generate", { endpoint: imageEndpoint, workflow: imageWorkflow, prompt: imagePrompt }); setImageBusy(false); if (!response.ok) { setError(response.error?.message ?? "Image generation failed."); return; } const result = response.result as { images?: Array<{ url: string; fileName: string }> }; setGeneratedImages((current) => [...(result.images ?? []), ...current].slice(0, 24)); };
  const routeState: AppRouteState = { route, provider, model, models, endpoint, cloudRouteEnabled, credentialConfigured, credentialStorageAvailable, messages: chat.messages, busy: chat.busy, attachments: chat.attachments, queuedMessages: chat.queuedMessages, conversationId: chat.conversationId, conversationTitle: chat.activeConversation?.title ?? "New chat", pinned: chat.activeConversation?.pinned ?? false, archived: chat.activeConversation?.archived ?? false, projects: coding.projects, selectedProject: coding.selectedProject, projectFiles: coding.projectFiles, selectedFile: coding.selectedFile, fileDraft: coding.fileDraft, fileOriginal: coding.fileOriginal, fileSummary: coding.fileSummary, fileBusy: coding.fileBusy, terminalBusy: coding.terminalBusy, terminalOutput: coding.terminalOutput, terminalExitCode: coding.terminalExitCode, themeMode, accentColor, density, backdrop, reduceMotion, petVisible, petWander, developerMode, memoryInjectionEnabled, imageEndpoint, imagePrompt, imageWorkflow, imageBusy, generatedImages, tasks, scheduledTasks, taskBusy, tasksLoading, tasksError, memoryEntries, memoryBusy, memoryLoading, memoryError };
  const routeActions: AppRouteActions = { onNavigate, onSend: chat.sendMessage, onQueue: chat.queueMessage, onRemoveQueued: chat.removeQueuedMessage, onAttach: chat.pickAttachments, onRemoveAttachment: (index) => chat.setAttachments((current) => current.filter((_, itemIndex) => itemIndex !== index)), onModelChange: (nextModel) => { setModel(nextModel); localStorage.setItem("skai.model", nextModel); }, onProviderChange: (nextProvider) => { setProvider(nextProvider); localStorage.setItem("skai.provider", nextProvider); if (nextProvider === "OpenAI API" && isLocalEndpoint(endpoint)) setEndpoint("https://api.openai.com/v1"); if (["Ollama", "LM Studio", "AirLLM"].includes(nextProvider) && isRemoteEndpoint(endpoint)) { setEndpoint(defaultEndpoint); setCloudRouteEnabled(false); } }, onExportConversations: chat.exportConversations, onFork: () => void chat.forkConversation(), onForkFromMessage: (index) => void chat.forkConversation(index), onRetryMessage: (index) => void chat.retryMessage(index), onTogglePinned: () => void chat.updateConversationMetadata({ pinned: !(chat.activeConversation?.pinned ?? false) }), onArchive: () => void chat.updateConversationMetadata({ archived: !(chat.activeConversation?.archived ?? false) }), onRenameConversation: (title) => chat.updateConversationMetadata({ title }), onPickFolder: coding.pickProjectFolder, onSaveProject: coding.saveProject, onOpenProject: coding.selectProject, onOpenFile: (path) => void coding.openProjectFile(path), onCloseFile: () => coding.closeProjectFile(), onDraftChange: coding.setFileDraft, onSaveFile: () => void coding.saveProjectFile(), onUndoFile: () => void coding.undoProjectFile(), onRunTerminal: (projectId, command, cwd) => void coding.runTerminal(projectId, command, cwd), onPlanTask: planTask, onSettingsChange: (key, value) => { if (key === "themeMode") setThemeMode(value as ThemeMode); if (key === "accentColor") setAccentColor(String(value)); if (key === "density") setDensity(String(value)); if (key === "backdrop") setBackdrop(String(value)); if (key === "reduceMotion") setReduceMotion(Boolean(value)); if (key === "petVisible") setPetVisible(Boolean(value)); if (key === "petWander") setPetWander(Boolean(value)); if (key === "developerMode") setDeveloperMode(Boolean(value)); if (key === "memoryInjectionEnabled") setMemoryInjectionEnabled(Boolean(value)); if (key === "provider") setProvider(String(value)); if (key === "model") setModel(String(value)); if (key === "endpoint") setEndpoint(String(value)); if (key === "cloudRouteEnabled") setCloudRouteEnabled(Boolean(value)); }, onSaveSettings: saveSettings, onDiscoverModels: () => void discoverModels(), onSaveCredential: saveCredential, onClearCredential: clearCredential, onSelectModel: (nextModel) => { setModel(nextModel); onNavigate("chat"); }, onImageChange: (key, value) => { if (key === "endpoint") setImageEndpoint(value); if (key === "prompt") setImagePrompt(value); if (key === "workflow") setImageWorkflow(value); }, onGenerateImage: () => void generateImage(), onRetryWorkbench: () => void loadWorkbench(), onTransitionTask: (id, operation) => void transitionTask(id, operation), onScheduleTask: scheduleTask, onTransitionScheduledTask: transitionScheduledTask, onRetryMemory: () => void loadMemory(), onSearchMemory: (query) => void loadMemory(query), onSaveMemory: saveMemory, onToggleMemory: (entry) => void toggleMemory(entry), onRemoveMemory: removeMemory, onExportMemory: () => void exportMemory(), onRunProjectCommand: (projectId, command, cwd) => void coding.runTerminal(projectId, command, cwd) };

  return { routeState, routeActions, route, connection, health, error, conversations: chat.conversations, conversationQuery, setConversationQuery, sidebarCollapsed, toggleSidebar, commandOpen, openCommandPalette, closeCommandPalette, startNewChat, selectConversation: chat.selectConversation, updateConversationMetadata: (id, patch) => chat.updateConversationMetadata(patch, id), onNavigate, petVisible, petWander, setPetVisible, setPetWander };
}

