# Exact user interaction contracts and unexpected-result prevention

| Screen | Action | Loading | Success | Failure | Cancellation / retry | Background/overlay guard |
|---|---|---|---|---|---|---|
| Chat | Send | Disable duplicate send, enable Stop, show streaming state | Persist turn and sources | Preserve draft / error as event | Stop cancels only request, not run history | No auto-scroll if user reading |
| Chat | Switch model | Probe capabilities without changing active choice | Confirm selected endpoint+model | Keep previous model | Retry after connect | No silent fallback or cloud upload |
| Agent | Start task | Persist run before first tool | Show plan and active step | Step failed + recovery choices | Pause/resume persists | Navigate away without stopping execution |
| Agent | Apply patch | Show exact files/hunks/permissions | Verify content hashes and journal | Explain conflict; preserve files | Rebase/diff/decline | One approval modal; no click-through |
| Agent | Run command | Show command, cwd, scope/time/network | Exit code + result log | Timeout/exception surfaced | Kill process tree / retry scoped | Prompt injection never grants rights |
| Projects | Clone GitHub repo | Destination preflight | Add project + show root | Conflict and retry path | Cancel only clone | No stale repo dropdown selections |
| Projects | Commit selection | Show index-aware review | Only chosen hunks in commit | Workspace changed; re-review | No hidden broad reset | No unrelated staged files |
| Models | Download | Size/license/disk/runtime checks | Installed verified runtime model | Logs + partial cleanup | Resume if supported | Progress persists across navigation |
| Models | Delete | Exact model + disk impact confirmation | Remove and refresh status | No change if failed | Cancel retains model | No background delete beneath dialog |
| Skills | Install | Permissions/source/compatibility preview | Pin verified version and index | Quarantine and explain | Cancel removes temp, not installed | No third-party script auto-run |
| Skills | Update | Exact manifest/permission diff | Atomic version switch and rollback | Remain on previous working version | Explicit defer | Never auto-add permissions |
| Tasks | Schedule | Validate timezone/cadence and permissions | Show next run and history | Not scheduled if persistence failed | Pause/resume | Explain whether app must remain on |
| Images | Generate | Queue id saved; show model/workflow | Preview + provenance | Correlated failing job | Cancel exact id | No global ComfyUI interrupt of other jobs |
| Settings | Save | Validate form inline before submit | Atomic draft commit | Field errors; dialog stays open | Cancel = no changes | Never open nested blocking model modal |
| UI | Open drawer | Disable background interactions | Show list with focus | Recover to menu toggle | Escape/scrim dismiss | Scrim intercepts, companion below |
| UI | Close modal | Cancel pending callbacks | Dispose modal and restore focus | Error visible but don't leave ghost scrim | Reopen is fresh epoch | Old animation cannot close reopened modal |

## Async lifecycle rules

Each page/view model has `CancellationTokenSource` disposed when page is closed, *unless* it owns a persistent background job (then only dispose UI subscriptions). Wrap `async void` UI handlers by a single exception boundary. Prevent concurrent `ShowAsync` on same XamlRoot. Every model/task event includes owner id and monotonically increasing version; discard results from a previous model/page selection. UI notifications must be idempotent and not silently replace form errors. Never apply a result to a recycled ListView item without verifying bound stable id.

## Anti-surprise checklist for code reviewers

- A hidden page's timers cannot mutate visible route controls.
- A search response that arrives after navigating away cannot change selection.
- Every destructive action identifies affected entities and has undo policy.
- Every long-running task can be observed while another page is open.
- All modal/drawer transitions dispose hit-test surfaces and focus traps.
- No more than one active global approval modal; requests queue explicitly.
- Project context/model selection does not change on unrelated navigation.
- Error state remains visible and retains original user input.
- Permissions and privacy data route must be visible at the moment of decision.
