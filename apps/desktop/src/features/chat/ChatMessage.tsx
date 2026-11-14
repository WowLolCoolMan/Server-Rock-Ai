import { useEffect, useState } from "react";
import type { ChatAttachment } from "../../../shared/contracts";
import type { Message } from "../../app/types";
import { Button } from "../../components/ui/Primitives";
import { MessageContent } from "./MessageContent";

type ChatMessageProps = {
  message: Message;
  index: number;
  model: string;
  busy: boolean;
  conversationId: string | null;
  copied: boolean;
  onCopy: (content: string, index: number) => void;
  onRetry: (index: number) => void;
  onFork: (index: number) => void;
  onEdit: (index: number, content: string) => Promise<boolean>;
};

function Attachments({ attachments }: { attachments?: ChatAttachment[] }) {
  return attachments?.length ? <div className="message-attachments">{attachments.map((attachment) => <span key={attachment.fileName}>{attachment.fileName}</span>)}</div> : null;
}

export function ChatMessage({ message, index, model, busy, conversationId, copied, onCopy, onRetry, onFork, onEdit }: ChatMessageProps) {
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState(message.content);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!editing) setDraft(message.content);
  }, [editing, message.content]);

  const saveEdit = async () => {
    const content = draft.trim();
    if (!content || saving || busy) return;
    setSaving(true);
    const saved = await onEdit(index, content);
    setSaving(false);
    if (saved) setEditing(false);
  };

  return <article className={`message-bubble ${message.role}`}>
    <div className="message-meta"><span>{message.role === "user" ? "You" : model}</span><div className="message-actions">
      <Button variant="ghost" onClick={() => onCopy(message.content, index)}>{copied ? "Copied" : "Copy"}</Button>
      {message.role === "user" && <Button variant="ghost" disabled={busy || !conversationId || saving} onClick={() => setEditing(true)}>Edit</Button>}
      {message.role === "assistant" && <Button variant="ghost" disabled={busy} onClick={() => onRetry(index)}>Retry</Button>}
      <Button variant="ghost" onClick={() => onFork(index)} disabled={!conversationId}>Fork from here</Button>
    </div></div>
    {editing ? <div className="message-edit-form"><textarea value={draft} onChange={(event) => setDraft(event.target.value)} rows={4} aria-label="Edit user message" autoFocus /><div className="message-edit-actions"><Button variant="ghost" type="button" disabled={saving} onClick={() => { setDraft(message.content); setEditing(false); }}>Cancel</Button><Button variant="primary" type="button" disabled={saving || !draft.trim()} onClick={() => void saveEdit()}>{saving ? "Regenerating…" : "Save & regenerate"}</Button></div></div> : <MessageContent content={message.content} />}
    <Attachments attachments={message.attachments} />
  </article>;
}
