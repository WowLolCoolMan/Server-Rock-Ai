# Historical conflicts, gaps and how to resolve them

1. **Tauri/Electron vs Uno:** Older proposals mention migrating an Electron/Tauri app; current repo direction is Uno/.NET. Reject obsolete stack assumptions.
2. **250 skills vs actual installations:** 250-entry catalog is research, not proof of audited/installable/working skills. Validate individually.
3. **Three static skills vs marketplace:** Do not relabel prompt insertion as full marketplace.
4. **Chat JSON vs Chat History folder:** Existing JSON persistence is not the requested user-visible folder design; implement safe migration/export, not destructive replacement.
5. **All-in-one folder vs OS dependencies:** User allows dependencies outside root. Guarantee app-owned persistent paths, not impossible physical total isolation.
6. **API key visibility vs security:** Show where credentials are managed, not plaintext secret files.
7. **Hybrid Router vs Server Rock AI:** Separate plugin tests do not establish native integration.
8. **Codex/Claude multi-agent collaboration vs in-app multi-agent:** Two developers using different coding agents does not imply Server Rock supports parallel agents.
9. **Odysseus source reuse vs ideas-only:** User explicitly prohibits copying or incorporating Odysseus.
10. **Agent Machines:** Explicitly deferred.
11. **Source review vs runtime:** No live packaged native verification established in prior handoffs.
12. **Schedule content:** User explicitly excludes timeline estimates from this package.
13. **Uninstall scope:** Preserve user chats, keys, external models, projects and unrelated dependencies by default; optional components must be owned and removable.
14. **Auto skill routing:** Installed is not enabled; enabled is not necessarily compatible; compatible is not automatically authorized to execute tools.
15. **Prior ZIPs:** Artifact names do not guarantee their contents are included or still accessible. Never claim otherwise.
