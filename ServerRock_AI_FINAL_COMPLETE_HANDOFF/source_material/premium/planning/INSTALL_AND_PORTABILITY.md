# Installation, portability and folder visibility

Primary design: a user-selected ServerRockAI root containing app-managed binaries and persistent data in documented subfolders. Distinguish system-installed mode and portable mode; never pretend GPU drivers, system libraries, keyrings, shortcuts or external user projects physically live inside this root.

Install must create a readable folder map and a generated README-FOLDERS.md. Show root in Settings with Open Folder, Open Logs, Open Conversations, Open Models, Open Skills and Manage Credentials actions.

Move/migration workflow: discover old paths -> preview sizes and conflicts -> backup -> copy with integrity verification -> switch pointer atomically -> verify launch -> offer rollback -> only then optional source cleanup with consent.

Portable vault requires authenticated encryption and safe key derivation. OS credential stores may persist outside the root; document it. Cross-platform tests cover Unicode paths, long paths, symlinks, permissions, network drives, removable storage, read-only state, interrupted migration, upgrade and uninstall.
