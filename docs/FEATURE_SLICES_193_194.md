# Feature slices 193–194

## Slice 193 — Effort modes

Skai Desktop now persists Light, Standard, High, and Ultra effort choices in
local settings. The selected mode maps to a bounded `max_tokens` request budget
of 512, 1,024, 2,048, or 4,096. The setting is explicit, local, and safe for
providers that honor the OpenAI-compatible field; provider-specific reasoning
controls remain open.

## Slice 194 — Side-chat shortcut

`Ctrl+Shift+S` opens or closes the existing side-by-side chat workspace while
in Chat mode. It is a bounded keyboard entry point to the already persisted
comparison view; detached windows and independently spawned parallel threads
remain future work.

No pet files are included in either slice.
