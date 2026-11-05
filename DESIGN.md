# Server Rock: AI design rules

Rules for the Electron UI (`index.html`, `style.css`). Read this before changing styles.
Product intent and reference screenshots live in `docs/UI_REFERENCE_GUIDE.md`.

Status: the overall visual direction is not chosen yet (see `.ui-shots/directions/` proposals).
Until it is, polish within the current look and follow the rules below. Each polish round
adds the tokens and rules it needs here.

## Tokens

All colors come from CSS custom properties defined twice in `style.css`: `:root` (dark, default)
and `:root.light`. Never hard-code a hex value in a component rule; add a token to both blocks.

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
| `--font-mono` | All code, in every theme: portable stack, never Windows-only faces |

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
