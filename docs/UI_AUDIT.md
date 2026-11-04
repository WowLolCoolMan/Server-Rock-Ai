# UI audit: Server Rock: AI (before redesign)

Captured with `node scripts/ui-shots.cjs before` at 1440×900, 1100×750 and 800×700.
Screenshot PNGs are git-ignored and are not included in the repository, so regenerate
them locally to review the visual evidence. The audit below records the pre-redesign
observations; it is a qualitative review of that capture environment, not a cross-OS
accessibility certification. The harness now fails if a requested state cannot be
opened or interacted with, and it closes browser resources on errors.

## 1. The stylesheet is three themes stacked on top of each other

- `style.css` has **3 `:root` blocks and 2 `:root.light` blocks** (lines 1, 934, 1522 and 562, 1180).
  Each later layer re-declares most of the earlier selectors: a sage-green theme,
  then a "Codex/ChatGPT" neutral-grey and blue layer, then a "compact desktop" layer.
- **400 hard-coded hex colors (242 unique) vs 28 `var()` uses** in the merged stylesheet. Later layers hard-code dark
  greys (`#191919`, `#242424`, `#2a2a2a`, `#333`, `#444`, `#4a4a4a`…). None of them are
  theme-scoped, so they break the light theme (see §3).
- The greys don't share a ramp: the sidebar alone uses `#171815`, `#1b1b1b`, `#20211d`,
  `#202020`, `#262822`, `#292a26`. Each pair is close but not the same.
- 18 different border radii (5, 6, 7, 8, 9, 10, 11, 12, 14, 15, 16, 18, 19, 20, 999px…)
  and 16 font sizes (10–30px), with 10px and 11px used for important status text.

## 2. Accent and type don't agree

- `--accent` is declared as sage `#b3cd8b`, then overridden to blue `#477bd1`. The send button,
  user bubbles and the "Save" button are blue. The focus ring, the brand mark and the
  "local" dots are still sage/green, so two accent colors are visible at the same time.
- Fonts: `body` asks for **DM Sans**, headings ask for **Manrope**, then later layers force
  **"Segoe UI", Inter, Arial**. None are bundled and the app has no network, so nothing loads
  (`document.fonts` is empty). On this Linux machine fontconfig falls back to **Adwaita Sans**
  for some elements and **Noto Sans** for others. The look is different on every OS.
- Code uses `ui-monospace, SFMono-Regular, Consolas`, which also differs per OS.

## 3. Light theme is broken

- Background, composer, sidebar and suggestions stay dark because later layers hard-code them.
  Text switches to dark.
- Measured contrast in light mode: **welcome heading 1.13:1**, suggestion chips 1.51:1,
  privacy label 2.04:1, rail icons 2.32:1, mode switch 2.99:1. All fail WCAG AA.
- The assistant bubble becomes a white card on a dark page with dark-on-dark inline code.

## 4. Duplicate entry points

- **Settings has four entry points**: rail gear `#railSettings`, sidebar gear `#settings`,
  "Provider settings" `#openConnections`, and the composer's `Ollama ⌄` button `#provider`.
  All four open the same dialog.
- **Two "new chat" buttons**: `#newChat` in the sidebar and `＋` `#newTop` in the header.
- `#openModelLibrary` ("Browse models") and the rail's `#navLibrary` lead to the same place.

## 5. "Local / on this device" is said six times on one screen

1. Header pill: `● Local`
2. Composer context row: `⌖ This device` (`#runtimeStatus`)
3. Composer tools: `● Prompt stays on this device` (`#privacyLabel`)
4. Footer: "Chats stay on this device. Cloud providers receive the prompts you send."
5. Sidebar bottom: `● Local model · on this device` (`#sidebarConnectionLabel`)
6. Model picker rows: "On this device", plus avatar tooltip "on this device".

They use three different dot styles and two different greens.
**Test dependency:** `tests/ui.spec.cjs` asserts the exact text of `#sidebarConnectionLabel`
("Local model · on this device" / "OpenAI API · prompts leave this device") and `#privacyLabel`
("● Prompt stays on this device" / "● Prompt sent to OpenAI API"). Merging these into one
indicator means either keeping those elements in the DOM (visually hidden) or changing tests.

## 6. Icons are mismatched Unicode glyphs

- Rail: `◈ ⌂ ▧ ◷ ▤ ⚙`. Sidebar: `＋ ▱ ▤ ⌘ ⑂ ›`. Composer: `◉ ⌄ ＋ 🎙 / ↑`. Welcome: `✳`.
  They come from different fonts, sit at different weights and baselines, and `⌘` means
  "Command" on macOS, not "provider".
- `🎙` has no color-emoji font on this machine and renders as a broken quill-like glyph.
- **`/` button (`#slashHelp`) has no click handler.** It does nothing. There is also no
  slash-command menu; typing `/fast` only shows a small "Use fast model" tag.
- app.js writes some glyphs itself: `#voice` gets `textContent = "🎙"` and `#model` gets
  `model + "　⌄"` (full-width space + caret). Replacing those with SVG needs CSS (hide the
  text, draw the icon with a mask) or a small presentation-only edit in app.js.

## 7. Layout

- **Double divider under the header**: `.top` has a border at y=52, and `#branchBar` draws
  a second one at y=67. It is `hidden` but CSS sets `display:flex`, which overrides `[hidden]`,
  so it shows as an empty 15px strip.
- **RECENTS is crammed under "GitHub repositories"**: no top margin, so the label touches
  the row above. The Library section takes space that recent chats need.
- WORKSPACE / LIBRARY / RECENTS are 12px all-caps grey labels. They are the loudest text
  in the sidebar after the mode switch.
- The sidebar mode switch (`ChatBot ⌄`) has a filled background and low-contrast text, so it
  reads as disabled. Its menu uses dim bold text (it looks inactive).
- Welcome block is absolutely positioned at `top:39–43%`, so it doesn't sit with the composer
  and drifts on tall or short windows.
- Composer context row: Provider and Model are far left, "This device" and "Advanced" far right,
  with an empty middle. "Advanced" is a bordered mini-button with no icon.
- User bubbles are saturated blue full-width bars that compete with the code blocks.
  The assistant "Regenerate" button floats right, far from the message it acts on.
- Settings dialog: buttons like "Connect", "Browse repositories", "Forget key" and
  "Continue with ChatGPT" are unstyled text. "Save settings" is a white block in dark mode
  and a grey block in light. Rows use a table-like divider under every row.

## 8. Responsive

- No collapse: at 800×700 the 255px sidebar and 54px rail still take 38% of the width, so
  the conversation is ~470px wide. There are 8 media queries across the layers (740, 900,
  640, 1000, 650px…) and they contradict each other on sidebar width.
- At 800px the welcome heading collides with the model picker.
- The settings dialog's "Save settings" is cut off at 700px tall (no scrolling body).

## 9. States and accessibility

- **Focus**: one global `:focus-visible` outline in sage `#a9c18b` (wrong accent). The textarea
  gets a box outline *inside* the composer while the composer also gets a `:focus-within`
  border, so it shows a double ring (see `slash-command` shots).
- **Hover**: only some list items and suggestions have hover. Rail buttons, header buttons,
  composer tools and dialog buttons have none.
- **Active/pressed**: none anywhere.
- **Disabled**: only `.branch-bar` and `.stop-generation` define disabled styles.
- **Send button**: white arrow on `#4b8cff` measures 3.23:1. That is OK for a large icon but
  it is the only saturated element on screen, and it ignores `--accent`.
- **Empty states**: an empty Recents list shows nothing (no hint). Model picker loading is
  plain text, and failures are a `|`-joined string ("Local hosts: … | Providers responding:
  … | | Saved models…").
- **Scrollbars**: `scrollbar-color` is hard-coded `#4a4a4a`/`#383a32`, wrong in light mode.
- **Motion**: one `transition` in the whole file. No `prefers-reduced-motion` handling (fine
  for now, but needed once motion is added).
- Small text: footer 10px (4.86:1), privacy label 11px, project meta 11px. Readable, but
  10px is below a comfortable minimum for a desktop app.

## What's working

- The structure (rail, sidebar, conversation, composer) is right for a keyboard-first
  workspace and doesn't need rethinking.
- Markdown and code blocks render cleanly with a copy toolbar.
- Dark-mode body text contrast is good (10–15:1).
