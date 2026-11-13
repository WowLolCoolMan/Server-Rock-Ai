import { useState } from "react";
import type { MemoryEntry } from "../../app/types";
import { Button, Card, EmptyState, PageHeader } from "../../components/ui/Primitives";

type MemoryPageProps = {
  entries: MemoryEntry[];
  busy: boolean;
  loading: boolean;
  error: string | null;
  onRetry: () => void;
  onSearch: (query: string) => void;
  onSave: (content: string, scope: string, tags: string, id?: string) => Promise<boolean>;
  onToggle: (entry: MemoryEntry) => void;
  onRemove: (entry: MemoryEntry) => Promise<boolean>;
  onExport: () => void;
};

export function MemoryPage({ entries, busy, loading, error, onRetry, onSearch, onSave, onToggle, onRemove, onExport }: MemoryPageProps) {
  const [content, setContent] = useState("");
  const [scope, setScope] = useState("global");
  const [tags, setTags] = useState("");
  const [query, setQuery] = useState("");
  const [editingId, setEditingId] = useState<string | null>(null);
  const [pendingRemoveId, setPendingRemoveId] = useState<string | null>(null);
  const [saveState, setSaveState] = useState<"idle" | "saving" | "saved">("idle");

  const resetEditor = () => {
    setContent("");
    setScope("global");
    setTags("");
    setEditingId(null);
    setSaveState("idle");
  };

  const editEntry = (entry: MemoryEntry) => {
    setEditingId(entry.id);
    setContent(entry.content);
    setScope(entry.scope);
    setTags(entry.tags.join(", "));
    setSaveState("idle");
  };

  const save = async () => {
    if (!content.trim() || busy || loading) return;
    setSaveState("saving");
    const saved = await onSave(content.trim(), scope.trim() || "global", tags, editingId ?? undefined);
    if (saved) { resetEditor(); setSaveState("saved"); } else setSaveState("idle");
  };

  const removeEntry = async (entry: MemoryEntry) => {
    if (busy) return;
    const removed = await onRemove(entry);
    if (removed) setPendingRemoveId(null);
  };

  return <div className="feature-page">
    <PageHeader eyebrow="LOCAL MEMORY" title="Review what Skai remembers" description="Only explicitly saved notes are stored. Search, edit, disable, remove, or export them at any time." actions={<Button variant="ghost" onClick={onExport}>Export backup</Button>} />
    <div className="memory-layout">
      <Card className="memory-editor">
        <div className="memory-editor-heading"><p className="section-kicker">{editingId ? "EDIT NOTE" : "SAVE A NOTE"}</p>{editingId && <Button variant="ghost" disabled={busy} onClick={resetEditor}>Cancel edit</Button>}</div>
        <textarea value={content} onChange={(event) => setContent(event.target.value)} placeholder="A preference or fact you explicitly want saved…" rows={4} aria-label="Memory content" />
        <div className="memory-inputs"><input value={scope} onChange={(event) => setScope(event.target.value)} placeholder="Scope (global or project:name)" aria-label="Memory scope" /><input value={tags} onChange={(event) => setTags(event.target.value)} placeholder="Tags, comma separated" aria-label="Memory tags" /></div>
        <div className="memory-editor-footer"><span className={`memory-save-status memory-save-${saveState}`} role="status">{saveState === "saving" ? "Saving locally…" : saveState === "saved" ? "Saved locally" : "Explicit notes only · local storage"}</span><Button variant="primary" disabled={busy || loading || saveState === "saving" || !content.trim()} onClick={() => void save()}>{saveState === "saving" ? "Saving…" : editingId ? "Update memory" : "Save memory"}</Button></div>
      </Card>
      <Card className="memory-results">
        <div className="memory-search"><input value={query} onChange={(event) => { setQuery(event.target.value); onSearch(event.target.value); }} placeholder="Search approved memory…" aria-label="Search approved memory" />{loading && <span className="inline-status">Loading memory…</span>}</div>
        {error ? <div className="surface-error" role="alert"><strong>Memory is unavailable</strong><p>{error}</p><Button variant="secondary" onClick={onRetry}>Retry</Button></div> : loading ? <div className="memory-skeleton" aria-label="Loading memory"><span /><span /><span /></div> : entries.length === 0 ? <EmptyState title="No approved memory" description="Saved notes will appear here with their scope and privacy controls." /> : entries.map((entry) => <article className={`memory-entry ${editingId === entry.id ? "is-editing" : ""}`} key={entry.id}><div><strong>{entry.content}</strong><span>{entry.scope} · {entry.tags.join(", ") || "untagged"} · updated {new Date(entry.updatedAtUtc).toLocaleDateString()}</span></div>{pendingRemoveId === entry.id ? <div className="memory-remove-confirm"><span>Remove this note?</span><Button variant="danger" disabled={busy} onClick={() => void removeEntry(entry)}>Confirm</Button><Button variant="ghost" disabled={busy} onClick={() => setPendingRemoveId(null)}>Keep</Button></div> : <div><Button variant="ghost" disabled={busy} onClick={() => editEntry(entry)}>Edit</Button><Button variant="ghost" disabled={busy} onClick={() => onToggle(entry)}>{entry.enabled ? "Disable" : "Enable"}</Button><Button variant="danger" disabled={busy} onClick={() => setPendingRemoveId(entry.id)}>Remove</Button></div>}</article>)}
      </Card>
    </div>
  </div>;
}
