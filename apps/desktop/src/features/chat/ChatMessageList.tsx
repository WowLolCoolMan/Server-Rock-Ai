import type { RefObject } from "react";
import type { ChatAttachment } from "../../../shared/contracts";
import type { Message } from "../../app/types";
import { Button, Card, EmptyState } from "../../components/ui/Primitives";
import { MessageContent } from "./MessageContent";

const SKAI_WOLF_ICON = new URL("../../../../../assets/branding/skaiwolficon.png", import.meta.url).href;

type ChatMessageListProps = {
  model: string;
  messages: Message[];
  busy: boolean;
  conversationId: string | null;
  onCopy: (content: string, index: number) => void;
  copiedMessage: number | null;
  onRetry: (index: number) => void;
  onForkFromMessage: (index: number) => void;
  messageEndRef: RefObject<HTMLDivElement | null>;
};

export function ChatMessageList({ model, messages, busy, conversationId, onCopy, copiedMessage, onRetry, onForkFromMessage, messageEndRef }: ChatMessageListProps) {
  return <Card className="conversation-card">
    <div className="conversation-header">
      <span className="conversation-context">LOCAL CHAT</span>
      <span className="conversation-count">{messages.length} messages</span>
    </div>
    {messages.length === 0 ? <EmptyState visual={<img src={SKAI_WOLF_ICON} alt="" />} title="Start a conversation" description="Ask a question, attach a file, or jump into a coding workspace." /> : <div className="message-list" aria-live="polite">
      {messages.map((message, index) => <article className={`message-bubble ${message.role}`} key={`${message.role}-${index}`}>
        <div className="message-meta">
          <span>{message.role === "user" ? "You" : model}</span>
          <div className="message-actions">
            <Button variant="ghost" onClick={() => onCopy(message.content, index)}>{copiedMessage === index ? "Copied" : "Copy"}</Button>
            {message.role === "assistant" && <Button variant="ghost" disabled={busy} onClick={() => onRetry(index)}>Retry</Button>}
            <Button variant="ghost" onClick={() => onForkFromMessage(index)} disabled={!conversationId}>Fork from here</Button>
          </div>
        </div>
        <MessageContent content={message.content} />
        {message.attachments?.length ? <div className="message-attachments">{message.attachments.map((attachment: ChatAttachment) => <span key={attachment.fileName}>{attachment.fileName}</span>)}</div> : null}
      </article>)}
      {busy && <article className="message-bubble assistant"><span>{model}</span><p className="typing">Thinking…</p></article>}
      <div ref={messageEndRef} />
    </div>}
  </Card>;
}
