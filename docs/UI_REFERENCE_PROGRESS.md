# UI reference implementation progress

The full reference set and owner requirements are documented in [`UI_REFERENCE_GUIDE.md`](UI_REFERENCE_GUIDE.md). This page tracks shipped behavior against that guide so developers and coding agents can continue the work without treating a visual resemblance as feature completion.

## Current slice

| Area | Implemented behavior | Evidence | Remaining work |
|---|---|---|---|
| GitHub repository entry | The sidebar opens the repository panel even when disconnected. The panel explains the connection requirement and has an explicit path to GitHub setup. | Playwright: `GitHub repository shortcut opens an explicit connection state instead of redirecting to settings`; the existing authenticated clone/push flow remains covered. | Test the disconnected and signed-in flow in a clean packaged-app profile. Improve empty, permission-denied, and network-error presentation. |
| Chat organization | Hover/overflow and right-click open the same menu. Rename, pin, unread/read, move to project, archive/restore, and confirmed permanent deletion operate on local chat data. Archived chats remain recoverable. | Playwright: `chat context menu actions rename, pin, move, archive, restore, and delete local chats`. | Add keyboard context-menu invocation, focus loop/arrow navigation, and test larger project lists and narrow windows. Share/open-in-new-window remain unimplemented until there are real workflows behind them. |
| Mini & Pets | An account popover exposes show/hide and customization. Original local companion choices persist on this device; no companion network service is used. | Playwright: `local pet companion can be shown, customized, and hidden without a network dependency`. | Replace the starter character artwork with reviewed original pixel-art assets; test placement on chat, image, activity, and terminal pages so it never blocks controls. |
| Responsive layout and basics | Main page navigation, settings entry, provider/model controls, model scan, image generation, project file access, and GitHub push review have UI coverage. | `npm test` passed: 72 UI/regression checks and 6 updater checks. `npm run audit:ui` passed wide, desktop, tablet, mobile, and small-mobile viewports. `npm run audit:electron` passed Electron shell checks. | Manual interaction and release packaging still need testing for every target; the automated browser checks do not prove model availability or OS installer behavior. |

## Known product mismatch

The current app update screen and Electron IPC still refer to a Server Rock Core signed update endpoint. The owner explicitly says the AI app is independent of Server Rock services and has asked for updates attached to the GitHub repository. Replace the Core updater with a GitHub Releases flow only after verifying platform assets, checksums, stable/beta selection, offline/no-release states, and installer behavior. The current public source repository is the update source in the existing release workflow; if the source repository becomes private, users will need a separate public distribution repository or another explicitly chosen public feed. Never embed a write token in the app.

## Next slice

Implement GitHub release discovery and verified download behavior, remove Server Rock Core runtime references, and add tests for stable/beta/no-release/platform mismatch/checksum failure. Then verify update behavior against the actual assets produced by the release workflow, including AppImage, Debian package, Windows installer, and the exact Windows/Linux versions tested. Keep the app's update UI under Settings → App updates and describe the update source there in plain language.
