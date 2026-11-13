# Skai Desktop UI Asset Pack

Use this pack to make the desktop UI feel branded and finished without turning
the product into a marketing page. The assets already belong to the repository;
do not redraw or replace them.

## Core assets

| Asset | Use | Guardrails |
|---|---|---|
| `skaiwolficon.png` | App icon, compact brand mark, empty-state accent | Preserve transparency; use at small sizes; do not place behind dense copy. |
| `skai-desktop-mark.png` | Dark branded splash/hero surface | Use sparingly for welcome/onboarding or a quiet empty state; never as a repeated page background. |
| `skai-desktop-hero.png` | Optional wide welcome visual | Use only on an intentional welcome/landing state; crop with the open dark area kept behind text. |
| `skai-desktop-architecture.png` | Product explanation / about / help surface | Do not use in the primary chat loop. |

## Visual direction

- Deep navy/near-black foundations with electric cyan as the primary accent.
- Violet is a restrained secondary accent; warm gold belongs only to the wolf's
  artwork and should not become a general UI color.
- Use the wolf as a recognizable accent, not decoration everywhere.
- Prefer one branded visual moment per surface: icon, quiet glow, or image—not
  all three together.
- Keep content, composer, model state, and actions visually dominant.
- Avoid gradients, glass panels, oversized hero art, floating particles, and
  decorative illustrations in dense work surfaces.
- Respect reduced-motion settings and keep all assets optional for accessibility.

## Suggested placement

1. App shell: small `skaiwolficon.png` in the brand lockup.
2. Empty chat / first-run: restrained icon or a low-opacity crop of the dark
   mark, with the first useful action immediately visible.
3. Model setup: no hero artwork; use a small icon and plain-language setup
   cards.
4. Settings, projects, and workbench: no large imagery; use the accent tokens
   and small icon only where it helps orientation.

All paths are relative to `assets/branding/`.
