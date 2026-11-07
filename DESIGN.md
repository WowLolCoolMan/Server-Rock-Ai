# Server Rock: AI – native design system

Design rules for the Uno/XAML app in `native/ServerRock.Native`. Follow them for every UI change so the app stays consistent. Product intent and reference screenshots are in `docs/UI_REFERENCE_GUIDE.md`.

## Where the tokens live
- **`App.xaml`** holds every color, font size, font family, radius and shared style. It is the only place a hex color should appear.
- **`UiTheme.cs`** gives C#-built UI the same tokens: `UiTheme.Brush("InkBrush")`, `UiTheme.FontSize("SmallFontSize")`, `UiTheme.CodeFont`, `UiTheme.Style("SectionLabelStyle")`. It also has shared behaviors: `Standardize(dialog)`, `ApplySegmentState`, `ApplyNavItemState`, `ApplyAccent`.
- If you need a color or size that doesn't exist, add a token to `App.xaml` (and a line here). Don't write `ColorHelper.FromArgb` or `#hex` in a view. Older code still does that and is being migrated chunk by chunk.

## Color
| Token | Use |
|---|---|
| `Canvas` | App background behind panels |
| `Surface` | Main content, cards, dialogs, composer |
| `SurfaceAlt` | Quiet cards inside a surface (settings cards, suggestion chips) |
| `Sidebar` | Navigation pane |
| `Line` / `LineSoft` | Borders. Use `LineSoft` for separators inside a card |
| `Ink` | Primary text |
| `MutedInk` | Secondary text, descriptions, footnotes. Passes AA (≥4.5:1) on Surface and Sidebar |
| `SubtleInk` | Decoration and disabled only. Never put information in it |
| `Accent` | Filled actions with white text (send, primary dialog action). 4.6:1 with white |
| `AccentHover` / `AccentPressed` | Hover and pressed states of accent fills |
| `AccentInk` | Accent-colored text: links, selected nav text |
| `AccentSoft` | Selected-row background, soft highlights |
| `Success` / `Warning` / `Danger` | Status dots and destructive actions. `WarningSoft`/`WarningLine`/`WarningInk` are for caution panels (e.g. "commands are not sandboxed") |

**Accent choices** (Settings → Appearance) are darkened so white text on them stays at or above 4.5:1. `UiTheme.ApplyAccent` updates the accent brushes in place, so anything bound to them follows.

**Theme:** light only. A dark theme would be a new feature. If one is added, put the second palette in `ThemeDictionaries` under the same token names.

## Type
| Token | px | Use |
|---|---|---|
| `CaptionFontSize` | 11 | Card descriptions, metadata, timestamps |
| `SmallFontSize` | 12 | Status lines, context buttons, page descriptions |
| `BodyFontSize` | 14 | Default UI text |
| `ReadingFontSize` | 15 | Assistant replies (long-form reading) |
| `SubtitleFontSize` | 19 | Sub-headings |
| `TitleFontSize` | 23 | Page or section titles inside dialogs |
| `DisplayFontSize` | 30 | Empty-state headline |

- Use the system UI font. For code, use `CodeFontFamily`, a fallback list (Cascadia Mono → Consolas → DejaVu Sans Mono → Liberation Mono → monospace), so Linux doesn't fall back to a proportional font.
- **Section labels** (`SectionLabelStyle`): 10 px uppercase, letter-spaced, `MutedInk`. Use them for groups such as PERSONAL, MODEL CONNECTION, CONVERSATION FOLDERS.

## Components
- **Primary action:** one per surface. Use `PrimaryActionButtonStyle` (based on WinUI `AccentButtonStyle`, so hover and pressed states keep the accent).
- **Dialogs:** call `UiTheme.Standardize(dialog)` on every `ContentDialog`. The primary button then gets `DialogPrimaryButtonStyle` (accent, same size as Cancel).
  - A dialog with nothing to confirm has one close button labelled **Done**. Never show two buttons that do the same thing.
  - Wide content must pass `maxWidth` (and `maxHeight` if needed), because ContentDialog otherwise caps at 548 px and clips.
- **Segmented switch** (Chatbot / Coding Agent): the track is `LineSoft`. The selected half is a raised `Surface` segment with `Ink` semibold text. The other half is flat `MutedInk`. Use `UiTheme.ApplySegmentState`.
- **Vertical navigation** (Settings sections): the selected item is `AccentSoft` background with `AccentInk` semibold text. Idle items are transparent with `MutedInk` text. Use `UiTheme.ApplyNavItemState`.
- **Cards:** `SurfaceAlt` background, `LineSoft` border, radius 9–12, padding 12. The title is 13 px semibold `Ink`, the description is caption `MutedInk`, and the control sits on the right.
- **Context buttons** (`ContextButtonStyle`): small bordered buttons for secondary actions near content.

## Honesty rules (from docs/NATIVE_MIGRATION.md, applied to UI)
- Never show success for something that didn't happen. Integrations that aren't connected are labelled **Not available yet**, and working ones are **Built in** with where to find them.
- Don't show the same status in many places. Model connection identity belongs in the header pill and the composer label.
- Labels say what a control does ("New chat", not "Add window").

## Layout
- The sidebar is 276 px. Below the compact breakpoint it becomes a drawer behind the ☰ button.
- The reading column is up to 800 px. The composer should line up with it.
- Spacing steps: 4, 6, 8, 10, 12, 14, 18, 24 px. Prefer `Spacing` on panels over per-child margins.

## Verifying UI changes
Build `net10.0-desktop`, run the Core checks and `npm run audit:native-ui`. Then look at the running app at a large size (1440×900) and a small one (760×560). On Linux you can do this headless; see the screenshot rig in the notes file of the polish work.
