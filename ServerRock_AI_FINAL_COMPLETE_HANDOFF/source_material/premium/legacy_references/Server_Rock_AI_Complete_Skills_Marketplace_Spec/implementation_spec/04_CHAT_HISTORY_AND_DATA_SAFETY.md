# Chat history
One user-accessible folder titled **Chat History**, with one-click 'Open Chat History Folder', selectable location, migration and full-text search. Suggested:
Chat History/Conversations/<stable-id>/conversation.json
Chat History/Conversations/<stable-id>/conversation.md
Chat History/Conversations/<stable-id>/attachments/
Chat History/Archived/
Chat History/Exports/
Chat History/Backups/

Use transactional database as canonical indexed store if appropriate; generate consistent JSON/Markdown exports, with stable IDs and atomic writes. Migrate existing Electron and Uno histories without overwriting; test backups/restores and corruption recovery. Preserve private credentials and sensitive tool outputs by default; redact secrets in exports. Local-first, optional sync only by explicit choice. On uninstall, default to preserve user conversations and offer separate data deletion with a clear preview.
