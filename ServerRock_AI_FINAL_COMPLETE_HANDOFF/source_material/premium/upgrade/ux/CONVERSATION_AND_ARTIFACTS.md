# Conversation UX: precision specification

1. Every conversation has stable UUID, title, project ID optional, pinned/archived/temporary state, created/updated timestamps, branches/parents, active model identity and origin.
2. Present user/assistant/tool messages with clear hierarchy. Collapsible tool trace shows input summary, user-approved scope, status, output excerpt and audit record; never expose hidden model chain-of-thought.
3. Composer includes attachments, local/cloud status, model profile, tools, optional effort only if backend supports, context indicator only if measured/trusted, send/stop and queued state. No decorative capability switches.
4. Streaming must handle partial UTF-8, split structured tool calls, reconnects, retries, cancellation and duplicate suppression; persist meaningful completion boundaries.
5. Chat list: search, folders, pins, sort by last activity, context menu, keyboard focus, archive/restore and empty states. Per-workspace recent chats stay distinct without duplicating session data.
6. Temporary chats are truly ephemeral, with explicit warning about non-persistence; queued exports/remote sends remain subject to user action.
7. Attachments: safe type/mime sniff, size limit, hash, source path vs copied snapshot, extraction metadata, prompt inclusion preview, consent to upload to remote providers. PDFs may lack OCR; label unsupported scanned pages.
8. Code content: copy, save, open as file, show diff, terminal output association; rendered Markdown never executes HTML/JS; links checked before opening.
9. Branch and edit flows preserve original turns, model identity and attachments; display forks without rewriting prior history.
10. Search indexing uses user-selected local storage; exclude temporary/private content as defined by explicit policy. No telemetry unless user opts in.
11. Recovery after crash loads persisted turns, marks incomplete stream as interrupted, and offers explicit resend—not automatic cloud reupload.
12. Event stream records operation state separately from message text so UI can reconstruct true progress.
