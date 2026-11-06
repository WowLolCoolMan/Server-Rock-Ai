# Server Rock: AI design rules

Rules for the Electron UI (`index.html`, `style.css`). Read this before changing styles.
Product intent and reference screenshots live in `docs/UI_REFERENCE_GUIDE.md`.

Status: the overall visual direction is not chosen yet (see `.ui-shots/directions/` proposals).
Until it is, polish within the current look and follow the rules below. Each polish round
adds the tokens and rules it needs here.

## Tokens

All colors come from CSS custom properties defined twice in `style.css`: `:root` (dark, default)
and `:root.light`. Never hard-code a hex value in a component rule; add a token to both blocks.
There are no hex values outside the two token blocks. Tints use `color-mix()` on a token
(e.g. `color-mix(in srgb, var(--danger) 14%, transparent)`) instead of a new hex.
Neutral surfaces are the blue-gray scale `--bg` < `--surface`/`--side` < `--card` < `--surface-raised`,
with `--surface-soft` and `--nav-selected` for hover and selected fills. Don't add standalone grays.

| Token | Use |
|---|---|
| `--bg` | Main canvas |
| `--side` / `--surface` | Rail and sidebar |
| `--surface-raised` | Raised controls: buttons, cards, menus |
| `--surface-soft` | Hover and pressed fills, table headers |
| `--input-bg` | Text inputs |
| `--line` / `--border-strong` | Default border / hover or emphasized border |
| `--text` / `--muted` | Body text / secondary text (both pass AA on every surface) |
| `--accent` | The one accent color: primary actions, selection, quote rules |
| `--user` | User message bubble |
| `--focus` | Focus ring |
| `--success` / `--danger` | Status colors |
| `--code-bg` / `--code-bar` | Code block body / its toolbar |
| `--code-text` / `--code-muted` | Code text / code language label |
| `--inline-code-bg` | Inline `code` chip |
| `--link` | Links inside messages |
| `--nav-hover` / `--nav-selected` | Rail and sidebar rows: hover fill / selected fill (selected is one step stronger) |
| `--shadow-pop` | Elevation for every menu and popover (dark needs a much deeper shadow) |
| `--icon-mic` / `--icon-stop` | SVG masks drawn in `currentColor`, for buttons whose text glyph app.js writes |
| `--font-mono` | All code, in every theme: portable stack, never Windows-only faces |
| `--primary` / `--primary-hover` / `--on-primary` | The one filled-button color (Send and every `.save` button) and its white text |
| `--warning` | Status color between success and danger (e.g. a remote host dot) |
| `--overlay` | Backdrop behind dialogs and the mobile drawer |
| `--shadow-color-sm` / `-md` / `-lg` | Shadow colors used inside `box-shadow` values (lighter and bluer in light) |
| `--art-*` | Illustration palette for the Images page thumbnails (`.art-*`). Drawings, not UI: same in both themes, set only in `:root` |

## Type

- One bundled pair, loaded offline from `node_modules/@fontsource` at the top of `style.css`:
  `--font-sans` = IBM Plex Sans (UI, headings) and `--font-mono` = IBM Plex Mono (code, terminal, `/`).
  Never name a system font (Segoe UI, Inter, Arial, system-ui) in a rule.
- Weights: 400 body, 500 small emphasis, 600 headings and labels, 700 strong headings. The bundle ships
  only these four (plus 400–600 for mono), so don't use 650, 750 or `bold`.
- Every `font-size` uses the scale; no px sizes in rules:

| Token | Size | Use |
|---|---|---|
| `--text-xs` | 11px | Eyebrow labels, sidebar section labels, badges (never smaller) |
| `--text-sm` | 12px | Secondary lines, hints, chips |
| `--text-ui` | 13px | Menu items, compact controls, code |
| `--text-base` | 14px | Body and most controls |
| `--text-md` | 16px | Large controls, reply `h3` |
| `--text-lg` | 18px | Icon glyphs, reply `h2` |
| `--text-xl` | 20px | Dialog titles |
| `--text-2xl` | 24px | Small page titles, the phone welcome heading |
| `--text-3xl` | 30px | Page titles (Images, Activity, AI SuperMarket) |

## Labels

- Every small label above a heading uses `.eyebrow`: 600 `--text-xs`, `.08em` tracking, uppercase via CSS
  (so the markup can say "Projects"), `--accent`.
- Sidebar section labels (PROJECTS, LIBRARY, RECENTS) use the same type in `--muted`.

## Contrast

- Text must meet WCAG AA in both themes: 4.5:1 for body text, 3:1 for large text and icons.
- Check light and dark every time. The light theme is where hard-coded dark values break.

## Components

### Messages
- Assistant replies are flat (no card) in both themes. User messages use `--user`.
- Code blocks: `--code-bg` body, `--code-bar` toolbar with the language label and a Copy button,
  12px radius, 1px `--line` border. Code is 13px/1.6 `--font-mono` and scrolls sideways (no wrapping).
- Inline code: `--inline-code-bg`, 1px `--line` border, 6px radius, 0.86em `--font-mono`.
- Links use `--link` with a 1px underline that thickens on hover.
- Reply actions (Regenerate) sit left-aligned under the reply, 28px tall, 12px text,
  `--muted` text that turns `--text` on hover.
- The message list fades out over its last 20px where it scrolls under the composer.

### Header
- The chat title is 600 `--text` and truncates with an ellipsis; the actions on the right are small ghost
  controls. Settings is a toothed gear (not a sun-like circle with rays).

### Rail and sidebar
- Rows (New chat, projects, library links, chats) are 38px, 9px radius, left-aligned.
  Hover is `--nav-hover`; the selected row or rail icon is `--nav-selected` with `--text`.
  Never use `--surface-raised` here: it is white in light mode, the same as the rail.
- A selected chat fills the whole row, including its ⋯ button. The name is 600 weight.
- Section labels (PROJECTS, LIBRARY, RECENTS) have 18px above and 4px below, with any
  action (Clear) centered on the label.
- The account button is a 32px circle (`--surface-soft` fill, `--border-strong` border) with a person
  icon in `--text`. Never initials: the app has no account name, and "SR" duplicated the app mark.
- The app mark at the top of the rail is a rock icon in `--accent` on a 14% accent tint, 34px, 10px radius.
  It's the only brand element on desktop; the "Server Rock AI" wordmark shows only in the phone drawer
  (where the rail is hidden), so the mode switch keeps the full sidebar width and never wraps.
- Every icon-only rail button has a `title` tooltip and an `aria-label`.
- Project rows: a folder icon (`--icon-folder` mask, `--muted`, `--text` when selected) before the name.
  The folder count line only appears when the project has folders.
- Menus opened from the sidebar use `--surface-raised`, a `--line` border, and `--danger`
  for destructive items.

### Composer
- One surface in both themes: `--surface-raised`, 1px `--line` border, 18px radius.
- Focus shows once, on the composer: `--accent` border plus a soft 3px `--focus` ring.
  The textarea itself draws no outline.
- Tool buttons (+, mic, /) are 34px ghost buttons in `--muted`, with `--surface-soft` on hover.
  No filled tool buttons; only Send is filled.
- Never rely on emoji glyphs (Linux has no color emoji font). Hide the text and draw the icon
  with a mask (`--icon-*`) in `currentColor`. Recording uses `--danger`.
- Command and queue chips use an `--accent` tint with `--link` text.

### Menus and popovers
- Mode menu, account menu, Advanced, model picker and slash menu share one surface:
  `--surface-raised`, 1px `--line` border, 14px radius (10–12px for small menus), `--shadow-pop`.
- Items are left-aligned 13px `--text`; secondary lines are 12px `--muted`.
  Hover is `--surface-soft`; the current choice is `--nav-selected`.
- Popovers stay inside the window: Advanced is centered over its button, pinned right on phones.
- Elements nested in the rail must override the rail's icon-button rule (`.rail button`).

### Buttons
- One primary style: `--primary` fill, `--on-primary` text, `--primary-hover` on hover. Used by Send and
  every `.save` button (Save settings, Browse AI SuperMarket, Download, Create project, Commit and push…).
- Never use `--accent` as a button fill: it's for selection, focus, links and small tints.
- Text inputs, selects and lists inside dialogs use `--input-bg` with `--text`, in both themes.

### States (every interactive element)
- Hover: `--surface-raised` or `--surface-soft` fill, `--border-strong` border where bordered.
- Focus: the global `:focus-visible` ring (3px `--focus`, 2px offset). Don't remove it.
- Disabled: `opacity: .55` and `cursor: not-allowed` (global `button:disabled` rule).

## Writing CSS here

- Put a `/* ===== Area ===== */` header above each area you touch.
- Edit rules where they live instead of adding another override layer at the bottom of the
  file. The file already has several stacked layers; consolidate when you touch an area.
- Don't rename or remove IDs, classes or data attributes used by `app.js` or `tests/`. Search
  first. If an element must go, hide it and comment why it still exists.
