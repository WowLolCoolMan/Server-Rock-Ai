import type { ChatAttachment } from "../../../shared/contracts";
import { Button } from "../../components/ui/Primitives";

function isImage(attachment: ChatAttachment) {
  return attachment.mediaType === "image/png" || attachment.mediaType === "image/jpeg";
}

function attachmentKind(attachment: ChatAttachment) {
  if (isImage(attachment)) return "Image";
  if (attachment.fileName.toLowerCase().endsWith(".pdf")) return "PDF · text extracted locally";
  return attachment.mediaType || "Local file";
}

function attachmentPreview(attachment: ChatAttachment) {
  if (isImage(attachment) && attachment.content) return <img src={`data:${attachment.mediaType};base64,${attachment.content}`} alt="" />;
  if (attachment.content && !attachment.fileName.toLowerCase().endsWith(".pdf")) {
    const snippet = attachment.content.replace(/\s+/g, " ").trim().slice(0, 96);
    if (snippet) return <span className="attachment-snippet">{snippet}{attachment.content.length > 96 ? "…" : ""}</span>;
  }
  return <span className="attachment-file-mark">{attachment.fileName.split(".").pop()?.slice(0, 4).toUpperCase() || "FILE"}</span>;
}

export function AttachmentList({ attachments, onRemove }: { attachments: ChatAttachment[]; onRemove?: (index: number) => void }) {
  if (attachments.length === 0) return null;
  return <div className={`attachment-list ${onRemove ? "attachment-list-composer" : "attachment-list-message"}`} aria-label={onRemove ? "Attached files" : "Message attachments"}>
    {attachments.map((attachment, index) => <article className="attachment-card" key={`${attachment.fileName}-${index}`}>
      <div className="attachment-preview" aria-hidden="true">{attachmentPreview(attachment)}</div>
      <div className="attachment-copy"><strong title={attachment.fileName}>{attachment.fileName}</strong><small>{attachmentKind(attachment)}</small></div>
      {onRemove && <Button variant="ghost" type="button" className="attachment-remove" onClick={() => onRemove(index)} aria-label={`Remove ${attachment.fileName}`}>×</Button>}
    </article>)}
  </div>;
}
