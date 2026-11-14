import { useEffect, useRef, useState } from "react";
import type { AgentRequest } from "../../services/agentClient";
import type { ChatAttachment, DesktopConversation, DesktopQueuedMessage, PetActivity } from "../../../shared/contracts";
import type { Message } from "../../app/types";
import { speakConfiguredResponse, stopNativeSpeech, type CloudVoiceOptions } from "../../services/nativeTts";

type ChatWorkspaceOptions = {
  request: AgentRequest;
  connection: "checking" | "ready" | "error";
  provider: string;
  endpoint: string;
  model: string;
  cloudRouteEnabled: boolean;
  projectId: string | null;
  effortMode: string;
  webSearchEnabled: boolean;
  fullTextContextInjectionEnabled: boolean;
  cloudVoice: CloudVoiceOptions;
  onError: (message: string | null) => void;
  onPetActivity: (activity: PetActivity) => void;
};

export function useChatWorkspace({ request, connection, provider, endpoint, model, cloudRouteEnabled, projectId, effortMode, webSearchEnabled, fullTextContextInjectionEnabled, cloudVoice, onError, onPetActivity }: ChatWorkspaceOptions) {
  const [messages, setMessages] = useState<Message[]>([]);
  const [conversationId, setConversationId] = useState<string | null>(null);
  const [attachments, setAttachments] = useState<ChatAttachment[]>([]);
  const [conversations, setConversations] = useState<DesktopConversation[]>([]);
  const [queuedMessages, setQueuedMessages] = useState<DesktopQueuedMessage[]>([]);
  const [busy, setBusy] = useState(false);
  const [documentsOnlyMode, setDocumentsOnlyMode] = useState(false);
  const petTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const clearPetTimer = () => {
    if (petTimer.current) clearTimeout(petTimer.current);
    petTimer.current = null;
  };
  const transitionPet = (activity: PetActivity, returnToIdleAfterMs?: number) => {
    clearPetTimer();
    onPetActivity(activity);
    if (returnToIdleAfterMs) petTimer.current = setTimeout(() => { petTimer.current = null; onPetActivity("idle"); }, returnToIdleAfterMs);
  };
  const startPetRequest = () => {
    clearPetTimer();
    onPetActivity("walking");
    petTimer.current = setTimeout(() => { petTimer.current = null; onPetActivity("thinking"); }, 180);
  };

  useEffect(() => () => clearPetTimer(), []);

  useEffect(() => {
    if (connection !== "ready") return;
    void Promise.all([request("conversations.list", { includeArchived: false }), request("chat.queue.list")]).then(([conversationResponse, queueResponse]) => {
      if (conversationResponse.ok) {
        const loaded = (conversationResponse.result as DesktopConversation[]) ?? [];
        setConversations(loaded);
        const first = loaded[0];
        if (first) {
          setConversationId(first.id);
          setMessages(first.messages.filter((item) => item.role !== "system") as Message[]);
        }
      } else onError(conversationResponse.error?.message ?? "Conversations could not be loaded.");
      if (queueResponse.ok) setQueuedMessages((queueResponse.result as DesktopQueuedMessage[]) ?? []);
      else onError(queueResponse.error?.message ?? "Queued messages could not be loaded.");
    }).catch(() => onError("Chat workspace data could not be loaded."));
  }, [connection, onError, request]);

  const saveConversation = async (nextMessages: Message[], id = conversationId) => {
    const existingTitle = id ? conversations.find((conversation) => conversation.id === id)?.title : undefined;
    const title = existingTitle || nextMessages.find((item) => item.role === "user")?.content.slice(0, 80) || "New chat";
    const response = await request("conversation.save", { id: id ?? undefined, title, messages: nextMessages });
    if (!response.ok) { onError(response.error?.message ?? "Conversation could not be saved."); return id; }
    const savedId = (response.result as { id: string }).id;
    setConversationId(savedId);
    return savedId;
  };

  const completeRequest = (nextMessages: Message[]) => ({ provider, endpoint, model, cloudRouteEnabled, projectId: projectId ?? undefined, messages: nextMessages, documentsOnly: documentsOnlyMode, webSearch: webSearchEnabled && !documentsOnlyMode, effortMode, fullTextContextInjectionEnabled });
  const sendMessage = async (text: string, messageAttachments: ChatAttachment[]) => {
    stopNativeSpeech();
    const next = [...messages, { role: "user" as const, content: text || "Attached files", attachments: messageAttachments.length ? messageAttachments : undefined }];
    setMessages(next); setAttachments([]); setBusy(true); onError(null); startPetRequest();
    const savedId = await saveConversation(next);
    const response = await request("chat.complete", completeRequest(next));
    setBusy(false);
    if (!response.ok) { transitionPet("error", 1800); onError(response.error?.message ?? "The model request failed."); return; }
    transitionPet("success", 1800);
    const completed = [...next, { role: "assistant" as const, content: (response.result as { content: string }).content }];
    setMessages(completed); await saveConversation(completed, savedId);
    void speakConfiguredResponse(completed.at(-1)?.content ?? "", cloudVoice).catch((error: unknown) => onError(error instanceof Error ? error.message : "Voice output failed."));
  };

  const retryMessage = async (messageIndex: number) => {
    if (busy || !conversationId) return;
    const originalMessages = messages;
    const prefix = messages.slice(0, messageIndex);
    if (messages[messageIndex]?.role !== "assistant" || !prefix.some((message) => message.role === "user")) return;
    stopNativeSpeech();
    setMessages(prefix); setBusy(true); onError(null); startPetRequest();
    const response = await request("chat.complete", completeRequest(prefix));
    setBusy(false);
    if (!response.ok) { setMessages(originalMessages); transitionPet("error", 1800); onError(response.error?.message ?? "The model request failed."); return; }
    transitionPet("success", 1800);
    const completed = [...prefix, { role: "assistant" as const, content: (response.result as { content: string }).content }];
    setMessages(completed); await saveConversation(completed, conversationId);
    void speakConfiguredResponse(completed.at(-1)?.content ?? "", cloudVoice).catch((error: unknown) => onError(error instanceof Error ? error.message : "Voice output failed."));
  };

  const pickAttachments = async () => {
    const response = await request("workspace.pickFiles");
    if (!response.ok) { onError(response.error?.message ?? "File selection failed."); return; }
    const selected = (response.result as { attachments: ChatAttachment[] }).attachments ?? [];
    const extracted: ChatAttachment[] = [];
    for (const attachment of selected) {
      if (attachment.fileName.toLowerCase().endsWith(".pdf")) {
        const extraction = await request("workspace.attachment.extract", { fileName: attachment.fileName, content: attachment.content });
        if (!extraction.ok) { onError(extraction.error?.message ?? `Could not read ${attachment.fileName}.`); return; }
        extracted.push(extraction.result as ChatAttachment);
      } else extracted.push(attachment);
    }
    setAttachments((current) => [...current, ...extracted].slice(0, 5));
  };

  const queueMessage = async (text: string, messageAttachments: ChatAttachment[]) => {
    const response = await request("chat.queue.enqueue", { text: text || "Attached files", attachments: messageAttachments });
    if (!response.ok) { onError(response.error?.message ?? "Message could not be queued."); return; }
    setQueuedMessages((response.result as DesktopQueuedMessage[]) ?? []);
  };

  const removeQueuedMessage = async (index: number) => {
    const response = await request("chat.queue.remove", { index });
    if (!response.ok) { onError(response.error?.message ?? "Queued message could not be removed."); return; }
    setQueuedMessages((response.result as DesktopQueuedMessage[]) ?? []);
  };

  const exportConversations = async () => {
    const response = await request("conversations.export", { includeArchived: true });
    if (!response.ok) { onError(response.error?.message ?? "Conversation export failed."); return; }
    const result = response.result as { fileName: string; markdown: string };
    const link = document.createElement("a"); link.href = URL.createObjectURL(new Blob([result.markdown], { type: "text/markdown" })); link.download = result.fileName; link.click(); URL.revokeObjectURL(link.href);
  };

  const updateConversationMetadata = async (patch: { pinned?: boolean; archived?: boolean; title?: string }, targetId = conversationId) => {
    if (!targetId) return false;
    const response = await request("conversation.metadata.save", { id: targetId, ...patch });
    if (!response.ok) { onError(response.error?.message ?? "Conversation metadata could not be saved."); return false; }
    const refreshed = await request("conversations.list", { includeArchived: Boolean(patch.archived) });
    if (refreshed.ok) setConversations((refreshed.result as DesktopConversation[]) ?? []);
    else { onError(refreshed.error?.message ?? "Conversations could not be refreshed."); return false; }
    return true;
  };

  const forkConversation = async (throughIndex?: number) => {
    if (!conversationId) return;
    const response = await request("conversation.fork", { id: conversationId, ...(throughIndex === undefined ? {} : { throughIndex }) });
    if (!response.ok) { onError(response.error?.message ?? "Conversation could not be forked."); return; }
    const result = response.result as { id: string };
    setConversationId(result.id);
    if (throughIndex !== undefined) setMessages((current) => current.slice(0, throughIndex + 1));
    const refreshed = await request("conversations.list", { includeArchived: false });
    if (refreshed.ok) setConversations((refreshed.result as DesktopConversation[]) ?? []);
    else onError(refreshed.error?.message ?? "Conversations could not be refreshed.");
  };

  const startNewChat = () => { setConversationId(null); setMessages([]); setAttachments([]); onError(null); };
  const selectConversation = (id: string) => {
    const selected = conversations.find((conversation) => conversation.id === id);
    if (!selected) return;
    setConversationId(selected.id);
    setMessages(selected.messages.filter((item) => item.role !== "system") as Message[]);
    setAttachments([]);
    onError(null);
  };
  const activeConversation = conversations.find((conversation) => conversation.id === conversationId) ?? null;

  return { messages, conversationId, attachments, conversations, queuedMessages, busy, documentsOnlyMode, setDocumentsOnlyMode, activeConversation, setAttachments, sendMessage, retryMessage, pickAttachments, queueMessage, removeQueuedMessage, exportConversations, updateConversationMetadata, forkConversation, startNewChat, selectConversation };
}

