import type { ChatAttachment, DesktopQueuedMessage } from "../../../shared/contracts";
import { Button, Card } from "../../components/ui/Primitives";

type ChatComposerProps = {
  draft: string;
  busy: boolean;
  attachments: ChatAttachment[];
  queuedMessages: DesktopQueuedMessage[];
  onDraftChange: (value: string) => void;
  onSubmit: () => void;
  onAttach: () => void;
  onRemoveAttachment: (index: number) => void;
  onRemoveQueued: (index: number) => void;
};

export function ChatComposer({ draft, busy, attachments, queuedMessages, onDraftChange, onSubmit, onAttach, onRemoveAttachment, onRemoveQueued }: ChatComposerProps) {
  return <Card className="composer-card">
    {queuedMessages.length > 0 && <div className="queued-messages" aria-label="Queued messages">
      <div className="queued-heading"><span className="section-kicker">UP NEXT</span><span>{queuedMessages.length}/8 queued</span></div>
      {queuedMessages.map((queued) => <div className="queued-message" key={queued.index}>
        <span>{queued.text}</span>
        {queued.attachments?.length ? <small>{queued.attachments.map((attachment) => attachment.fileName).join(", ")}</small> : null}
        <button type="button" onClick={() => onRemoveQueued(queued.index)} aria-label={`Remove queued message ${queued.index + 1}`}>×</button>
      </div>)}
    </div>}
    {attachments.length > 0 && <div className="attachment-strip" aria-label="Attached files">{attachments.map((attachment, index) => <span key={`${attachment.fileName}-${index}`}>
      {attachment.fileName}<button type="button" onClick={() => onRemoveAttachment(index)} aria-label={`Remove ${attachment.fileName}`}>×</button>
    </span>)}</div>}
    <form onSubmit={(event) => { event.preventDefault(); onSubmit(); }}>
      <textarea value={draft} onChange={(event) => onDraftChange(event.target.value)} onKeyDown={(event) => {
        if (event.key === "Enter" && !event.shiftKey) { event.preventDefault(); onSubmit(); }
      }} placeholder={busy ? "Type a follow-up while the model responds…" : "Message your model…"} aria-label="Message your model" rows={3} />
      <div className="composer-actions">
        <Button variant="ghost" type="button" onClick={onAttach} disabled={attachments.length >= 5}>Attach</Button>
        <div><span className="composer-hint">Enter to {busy ? "queue" : "send"} · Shift+Enter for a new line</span><Button variant={busy ? "secondary" : "primary"} type="submit" disabled={!draft.trim() && attachments.length === 0}>{busy ? "Queue ↗" : "Send ↗"}</Button></div>
      </div>
    </form>
  </Card>;
}
