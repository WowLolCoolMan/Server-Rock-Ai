# Mandatory for coding agents: strict independent implementation

1. Work exclusively against the Server Rock workspace and first-party dependencies it already authorizes.
2. Treat `inspiration/26_IDEAS_NOT_IMPLEMENTATIONS.md` as product brainstorming, not reference code.
3. Do not clone, fetch, import, translate, port, vendor, copy, screenshot, theme-match, or link Odysseus or its subprojects.
4. For each requested feature, write a plain user story, Server Rock-specific data contract, state transitions, tests, and UI acceptance behavior **before** coding.
5. Implement independently with C#/.NET 10/Uno and local first-party tests, using existing interfaces where appropriate.
6. No hidden background effects: all side effects are tied to explicit run/operation IDs, user authorization, status and verification.
7. Prioritize reliability: fix P0 regressions; fix popups, focus, interaction layers and routed pages; only then expand capabilities.
8. In every PR, list changed files, any new NuGet packages, permissions, external calls, tests and screenshots. Verify that Odysseus did not become a dependency or source.
9. If a proposed solution requires taking code from Odysseus, **stop that approach** and create an original alternative; do not ask the user to accept a code port as a shortcut.
10. Completion requires observed functionality, not merely compiling or displaying placeholder buttons.
