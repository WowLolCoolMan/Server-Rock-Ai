# Pet companion overhaul

Working notes for the pet (desktop companion) overhaul in `native/ServerRock.Native`.
Pet code: `MainPage.Companion.cs`, `CompanionStateStore.cs`, the `Companion*` elements in
`MainPage.xaml`, the pet buttons in `SidebarView.xaml`, and the artwork in `assets/pets/`.

## How the smoke test is run

Linux, `net10.0-desktop` Debug build. The app runs on a private display (a headless
`kwin_wayland --virtual` with a rootful `Xwayland :7`), so it never opens on the desktop and
synthetic input (XTEST) works. A small fake OpenAI-compatible server on `127.0.0.1` drives the
real chat path: a normal prompt streams a reply (thinking → success), a "slow" prompt streams
for ~8 s, a "fail" prompt returns HTTP 500 (error), and a "hang" prompt never answers (Stop).
Every store points at a scratch profile through the `SRAI_*_PATH` variables, so no real
settings are touched. Screenshots and recordings go to `.pet-shots/` (git-excluded).

## Smoke test, round 1 (before fixes)

| Check | Result | Notes |
| --- | --- | --- |
| All five pets load | Pass (with bad frames) | Every pet shows, but most state frames are the wrong picture or cut in half (see bugs 1–2). |
| idle | Fail | Static image. Rocky's idle is a different, full-size render, so it's drawn smaller than his other frames. |
| thinking | Fail | Static. Rocky's frame is cut through the middle of two robots. |
| coding | Fail | Static. Wrong picture for 4 of 5 pets (Byte, Sprout, Ember show the dizzy or evil frame). |
| coding_malicious | Fail | Static, wrong picture for most pets, and nothing in the app ever sets this state. |
| stuck | Fail | Static, wrong picture (Byte wears sunglasses, Sprout shows a heart). Also shown when the user presses Stop, with the text "the response timed out". |
| success | Fail | Static, Rocky's frame is cropped. Never returns to idle. |
| error | Fail | Static, wrong picture for 4 of 5 pets. Never returns to idle. |
| Animation runs while hidden or minimized | N/A | There is no animation at all, so nothing runs. |
| Transitions | Fail | The image source is swapped in one frame: a hard cut, no cross-fade. |
| State returns to idle | Fail | success, error and stuck stay until the next prompt; error even survives a restart. |
| Speech bubble | Fail | Plain text with no background straight over the conversation, hard to read over messages; it never clears; hex colors in XAML. |
| Drag | Pass | Moves with the pointer and saves the position. |
| Keyboard moves | Pass (code review) | Arrow keys move 16 px, Shift 48 px, then save. |
| Hide / show | Pass | Sidebar button and × both work, label follows. |
| Resize 1440×900 ↔ 760×560 | Pass | Compact layout parks the pet under the header and restores the saved spot afterwards. |
| Restart remembers pet and position | Pass, with bug | Pet and position come back, but so does the last state (bug 4). |
| Custom sprites, valid / invalid path | Pass (code review) | Validation rejects relative paths, wrong extensions, missing and >8 MB files. |
| Corrupt `companion.json` | Pass | Falls back to defaults, leaves the file untouched and stops saving for the session. |

Outside pet scope, noted only: in the rig, Stop did not cancel a request whose server had not
sent response headers yet (the request stayed "Thinking…" until the server connection dropped).
Not investigated further; not changed.

## Bugs and causes

1. **Wrong atlas grid for Rocky.** `assets/pets/rocky-sprite-sheet.png` is a 3×3 sheet, but the
   frames were cut as if it were 4×4, so `rocky-thinking/coding/coding_malicious/stuck/success/error.png`
   each show slices of two or four robots. `rocky-idle.png` is a separate 1261×1247 render.
2. **Wrong cells for the other pets.** The README cell table (idle 0, thinking 4, coding 6,
   coding_malicious 7, stuck 11, success 12, error 8) only fits Nova's sheet; each sheet has its
   own layout, and the shipped crops don't follow the table either (for example `byte-success.png`
   is Byte's warning-sign cell 12, `sprout-success.png` is the crying cell 12).
3. **No animation.** `UpdateCompanionDisplay` (`MainPage.Companion.cs:38`) only swaps
   `CompanionImage.Source` (`ResolveCompanionAsset`, line 50). There is no storyboard, timer or
   transform anywhere in the pet code, so every state is a frozen picture and every change is a cut.
4. **States never go back to idle, and the state is saved.** `SetCompanionState`
   (`MainPage.Companion.cs:72`) stores `CurrentState` in `_companionState`, which `SaveCompanionState`
   writes to `companion.json` (`CompanionStateStore.cs:13`, kept by `Normalize` at line 85). Nothing
   resets success/error/stuck, so the last state sticks and is restored on the next launch.
5. **Stop shows "stuck".** `MainPage.Chat.cs:309` sets `stuck` and "the response timed out" for both
   a timeout and a user pressing Stop.
6. **coding_malicious is never used.** No caller passes it.
7. **Speech bubble readability.** `CompanionSpeech` (`MainPage.xaml:38`) has no background and uses
   hex colors (`#28474e`, `#527177`, `#6f878c` on lines 38, 41, 43) against DESIGN.md; the text is
   never cleared (`UpdateCompanionSpeech`, `MainPage.Companion.cs:90`).

## Fix plan

To stay out of the way of other work on `MainPage.Companion.cs`, new behavior goes into new pet
files (`MainPage.CompanionAnimation.cs`, later `MainPage.CompanionWander.cs`) and the existing
files only get small hooks.

1. Re-cut every state frame from its sheet with the correct grid and a per-pet cell map, trimmed
   and padded to the same square size so frames line up; document the maps in `assets/pets/README.md`.
2. Give each state its own looping motion on a transform (idle breathing, thinking sway, coding
   quick bob, coding_malicious jitter, stuck slow droop, success hop, error shake), and cross-fade
   between frames with a second image layer.
3. Stop all animation while the pet is hidden, blocked by the drawer, or the window is minimized;
   honor Reduce motion (no loops, instant swaps).
4. Return success/error/stuck to idle after a few seconds; never restore a non-idle state on launch.
5. Stop → idle with "Stopped." instead of stuck.
6. Speech bubble on `Surface` with a `Line` border, token text colors, and an auto-hide.

## Wander (phase 2)

Not started.

## Push log

| Time (local) | Commit | What |
| --- | --- | --- |
| 23:49 | 2582816, 32c4668 | Smoke test round 1, bug list and fix plan; one-line Linux fix for the Markdown export file-name test (owner's file, see commit). |
