# Pet companion overhaul

Working notes for the pet (desktop companion) overhaul in `native/ServerRock.Native`.
Percy, the blue cyber-wolf, is the official Skai Desktop mascot and default new-profile companion.
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

## Smoke test, round 2 (after the animation fixes)

| Check | Result | Notes |
| --- | --- | --- |
| All five pets, all seven frames | Pass | Re-cut from the sheets (`assets/pets/cut-frames.sh`); every frame shows the right pose, same size and anchor. One 2-px speck on Byte's stuck frame. |
| idle | Pass | Breathing from the feet with a 1–2 px rise. |
| thinking | Pass | Head-tilt sway with a float; bubble "Thinking…" stays visible for the whole reply (recorded, Byte). |
| coding | Pass (code) | Typing bounce. Same code path as thinking; not yet recorded in the rig. |
| coding_malicious | Fixed (code) | Jitter + pulse. Now shown while a Coding Agent command waits for approval (it would run unsandboxed), when "ask for approval before risky actions" is on; back to idle when the dialog closes. Not exercised in the rig (needs a project folder from the native picker). |
| stuck | Pass (code) | Slow droop. Stop now shows idle + "Stopped." instead. |
| success | Pass | Hops with a landing squash; back to idle after 6 s (recorded, Rocky and Byte). |
| error | Pass | Head-shake bursts; back to idle after 10 s (recorded, Rocky). |
| Transitions | Pass | 0.22 s cross-fade between frames and a 0.35 s blend between motions. All seven frames of the current pet are decoded up front. |
| Animation while hidden | Pass | One 30 fps timer, stopped when the overlay is collapsed. App CPU: 2.1% with the pet visible and idle, 0.9% hidden (same as no pet). |
| Animation while minimized | Pass (code) | Stops on `Window.VisibilityChanged`; the rig has no window manager, so minimizing could not be exercised. |
| Reduce motion | Pass (code) | No timer, resting pose, instant frame swaps. |
| Speech bubble | Pass | `Surface` bubble, `Line` border, `Ink` caption text, three lines max; grows upward so the sprite never moves; idle messages fade after 10 s. |
| Restart | Pass | Always starts idle. |

## Wander (phase 2)

Status: done and ON by default since round 4 of the smoke test. "Let the pet wander" (pet chooser
dialog) is saved as `wander` in `companion.json`; `null` means "not chosen", so the default
applies: on, unless the app's "Reduce decorative motion" or the system's animation setting
(Windows "Show animations", via `UISettings.AnimationsEnabled`) asks for less motion. The same
check also stops the state animations.

- Grid and planning: `ServerRock.Core/Services/PetWanderPlanner.cs` (`PetWanderGrid`,
  `PetWanderPlanner`, `PetWalk`), tested in `PetWanderPlannerTests.cs` with seeded randoms.
- UI: `MainPage.CompanionWander.cs`, riding on the pet animation timer (60 fps while walking).
- Blocked cells: every cell where the pet card (bubble included) would overlap the composer
  control, Send or Stop, plus 8 px clearance.

| Check | Result | Notes |
| --- | --- | --- |
| Walks cell to cell, never jumps | Pass | Logged card position every second: 78–90 px/s, continuous. Core test proves every step is one neighboring cell and the glide never exceeds its speed. |
| Render rate while walking | Pass | Uno reported 60 rendered frames/s and 60 timer ticks/s during walks (temporary counter, removed). |
| Rests 2–6 s | Pass | Core test plus log. |
| Never over the composer at 1440×900 | Pass | 0 overlapping ticks in 60 s. |
| Never over the composer at 760×560 | Pass, after fix | The shrink itself can drop the pet onto the composer; it used to take ~3 s to leave, now it starts walking off within 0.2 s. |
| Stop/resume around states | Pass (code) | Slides to a stop over 0.3 s when the state leaves idle; resumes 5 s after idle. |
| Drag/keys pause 30 s | Pass (code) | Any outside move of the card cancels the walk and waits 30 s. |
| Hidden / Reduce motion / wander off | Pass | Nothing ticks for wander; the shared timer is stopped while hidden or with Reduce motion. |

Known limits:
- While the pet is busy (thinking, coding…) it doesn't walk; if a resize drops it on the
  composer in that moment, it walks off when it's idle again.
- Rig limitation: on the headless display used here, screen captures only refresh 1–4 times a
  second (the virtual output throttles presents), so recordings look choppy even though the app
  renders at 60 fps. The smoothness evidence is the in-app frame counter and position log above,
  not the videos.

## Smoke test, round 3 (full re-run after wander)

| Check | Result | Notes |
| --- | --- | --- |
| All five pets load | Pass | Rocky, Byte, Sprout, Nova, Ember idle frames, bubble and name. |
| Thinking → success → idle with wander on | Pass | The pet stops where it is for the prompt, thinks, hops, relaxes, and walks again ~5 s later. |
| coding | Pass (code) | Coding Agent needs a project folder from the native folder picker before it sends, which the rig can't drive; it is the same call as thinking with "coding". |
| Pet over the sidebar | Fail → fixed | The sidebar has ZIndex 2 for the compact drawer, so a pet dragged or walking over it disappeared and its bubble was cut at the sidebar edge. The pet overlay now sits above it (it is collapsed while the drawer is open). |
| "Let the pet wander" toggle | Pass | In the pet chooser; saves `"wander": true/false` immediately. |
| Window title | Note | The app's window title is now "Skai Desktop" (owner rename); the rig matches both names. |

### Full-desktop wandering (research only, not built)

The idea: a second, borderless, transparent, always-on-top window that only contains the pet,
moved around the screen instead of inside the app. What each piece needs, per platform:

| Need | Windows (Uno Win32/WinUI) | macOS (Uno AppKit) | Linux X11 (Uno X11) | Linux Wayland |
| --- | --- | --- | --- | --- |
| Always on top | `OverlappedPresenter.IsAlwaysOnTop` (Windows App SDK API; Uno maps the overlapped presenter on desktop) | Same API; needs a check that Uno's AppKit host sets an `NSWindow` level above normal | Uno's X11 presenter already reads `_NET_WM_STATE`, so `_NET_WM_STATE_ABOVE` is plausible; not confirmed in Uno's source | Not possible for normal (xdg-shell) windows; needs the `wlr-layer-shell` protocol (KDE, wlroots compositors, GNOME 47+), which Uno doesn't speak |
| Transparent background | Win32 layered window or DirectComposition; Uno only documents a window *background color* (`WindowHelper.SetBackground`), no per-pixel transparency | `NSWindow.isOpaque = false` plus a clear background; same Uno gap | 32-bit ARGB visual plus a compositing manager; same Uno gap | Alpha surfaces are normal, but only reachable through layer-shell here |
| Click-through except the sprite | `WS_EX_TRANSPARENT` toggled while the pointer is off the sprite, or a window region | `ignoresMouseEvents` toggled by hit-testing | XShape input region shaped to the sprite | `wl_surface.set_input_region` on the layer surface |
| Move freely on screen | `AppWindow.Move` | `AppWindow.Move` | `AppWindow.Move` (window manager may override) | Clients can't position windows at all; layer-shell anchors + margins only |

Verdict:
- **Windows** is feasible but needs native interop beside Uno (layered or composition window,
  extended styles), because Uno has no transparent-window API. Medium effort.
- **macOS** is feasible the same way through `NSWindow` interop. Medium effort, untested.
- **X11** is feasible with an ARGB visual and XShape, also outside Uno's API, and only looks
  right with a compositor running.
- **Wayland** (including this machine's KDE session) is not feasible with Uno today: positioning
  and always-on-top need layer-shell, which would mean a separate small GTK4/Qt helper process
  (as wanderlay and KOverlay do). Running the app under XWayland would make it behave like X11 but
  loses native Wayland input and scaling.
- So full-desktop wandering would be a per-platform feature with native code in each shell plus a
  helper on Wayland. The in-window wander covers the same "alive" feel without any of that.

Sources: [Uno: Window and WindowHelper.SetBackground](https://platform.uno/docs/articles/features/windows-ui-xaml-window.html),
[Uno: Skia desktop hosts](https://platform.uno/docs/articles/features/using-skia-desktop.html),
[Uno 5.2.80 release notes (X11 overlapped presenter)](https://newreleases.io/project/github/unoplatform/uno/release/5.2.80),
[OverlappedPresenter.IsAlwaysOnTop](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.windowing.overlappedpresenter),
[wlr-layer-shell protocol](https://wayland.app/protocols/wlr-layer-shell-unstable-v1),
[Wayland issue: no always-on-top for normal windows](https://gitlab.freedesktop.org/wayland/wayland/-/issues/305),
[wanderlay (GTK4 layer-shell overlay)](https://github.com/tilacog/wanderlay).
KOverlay (a LayerShellQt overlay) is a second example of the helper approach.

## Push log

| Time (local) | Commit | What |
| --- | --- | --- |
| 23:49 | 2582816, 32c4668 | Smoke test round 1, bug list and fix plan; one-line Linux fix for the Markdown export file-name test (owner's file, see commit). |
| 00:01 | f338d8c 7ec2887 3be18c6 c2f2735 | Re-cut frames, state animations, cross-fades, bubble, Stop/idle fixes; round-2 smoke table. |
| 00:04 | 0b6dfa6 | Wander planner + Core tests (not wired to the UI yet). |
| 00:19 | 881c19d | Wander in the UI behind "Let the pet wander" (off by default); wander smoke results. |
| 00:27 | 30a5248 | Pet drawn above the sidebar; round-3 smoke results. |
| 00:29 | 251ccce | Wander default ON (OFF with app or system reduce motion). |
| 00:31 | 5b79128 cafc825 | coding_malicious during command approval; full-desktop wander research. |
| 00:35 | fa6422c | Hide/minimize mid-walk no longer teleports; wander starts from the saved spot. |
