# Real GitHub-backed skill marketplace

## Manifest
ID, display name, author, repository URL, exact commit/ref, license, version, minimum Server Rock version, OS/runtime requirements, entrypoints, dependencies, requested permissions, content hash, provenance, installed state.

## Lifecycle
Discover -> inspect -> compatibility scan -> trust/permission preview -> download to staging -> validate archive and paths -> verify hash/provenance -> resolve dependencies -> install atomically -> register -> execute in scoped context -> update diff -> rollback/remove.

## Updates
Compare installed pinned revision with upstream tracked ref without automatically executing upstream code. Show update available and changelog/permission changes. Never silently expand privileges. Offline installed skills remain usable when feasible.

## Routing
Model proposes skill based on task + manifest capabilities. Deterministic policy validates applicability, trust, permissions and conflicts. Show 'Using skill X because ...' and allow manual override. Never let skill text grant itself privileges.

## Skill creation
Wizard from user goal -> template -> manifest -> tests -> local preview -> package -> optional GitHub publication. Generated skill receives same security review as downloaded skills.

## Tests
Dependency conflict, incompatible OS, missing license, path traversal, unsigned source policy, update permission escalation, rollback, uninstall leftovers, offline use, malformed manifest and user revocation.
