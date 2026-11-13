# Engineering anti-patterns and code review critique

Check every PR for:
- All logic in `MainPage.xaml.cs` or duplicated C# ContentDialog builders instead of reusable controls/services.
- Hard-coded hex/colors/fonts/spacing outside resource tokens, inconsistent Fluent icons or hidden focus visual.
- One modal per integration and inconsistent back/escape/focus, click-through behind overlays, offscreen buttons.
- UI display based on an optimistic local variable rather than backend-acknowledged operation state.
- Unbounded provider retries, fake progress percentages, duplicate messages or unintended side effects during reconnect.
- Mixed model/provider/agent/skill abstraction making one model API appear as a full coding agent.
- File access based on naïve string prefix rather than canonical path + permission scope.
- Secret values in JSON, stdout, exception text, Git remote URLs or exported diagnostics.
- Global shell interpolation with untrusted paths; no process tree kill; missing Linux path/PTY/keyring tests.
- Marketplace `git clone` for arbitrary repo without commit pin/hash/license/permissions and rollback.
- Read-only feature called complete without actions, disabled-state explanation, and meaningful empty/error.
- 'Automatic' cloud fallback that sends local private context without consent.
- Windows-only code leaking into Core; Linux support claimed from compiling under Windows or WSLg startup.
- Tests that only grep XAML/source rather than running native UI at scale/focus combinations.
- 250 unverified GitHub links called verified ready-to-install products; fabricated ratings.
- Direct transplantation of Odysseus code/assets despite explicit ideas-only constraint.

Code should be idiomatic, modular, testable, documented where needed and measured under real workloads. Do not 'de-AI' by falsifying authorship or removing licensed attribution; quality is about actual engineering evidence.
