# Reference policy: ideas only, original Server Rock implementation

**Strict requirement:** Odysseus is only an example of *user-facing capabilities or UX problems worth solving*. It is **NOT** a code source, architecture blueprint, integration target, API dependency, plug-in, visual design system, or component library for Server Rock.

- Do **not** `git clone` Odysseus into the Server Rock workspace, add it as a submodule, link it as a dependency, vendor files, adapt code, translate Python/JavaScript into C#, copy its CSS/layout/source assets, reuse branding or screenshots, or provide "compatibility" with its private APIs.
- Do **not** recreate its internal classes/modules 1:1 or turn upstream bug fixes into Server Rock code changes.
- Do **not** scrape, fetch, or bundle its repository during installs, builds, tests, skill delivery, or updates.
- Do **not** use Odysseus source or assets in generated PRs. If another source must be used for an independently chosen dependency, handle it through Server Rock's ordinary dependency and license review, not through this inspiration list.
- You **may** identify a broad user outcome (for example "background tasks remain visible while navigating"), translate that into a Server Rock-specific requirements document, then implement and test it from first principles using .NET/Uno and Server Rock's own domain model.
- Keep the **end-user outcome**, not another product's implementation or unique expressive design. Server Rock should have original terminology, navigation, branding, visual layout and code.
- If another project uses a legal license that requires obligations on redistribution, don't copy its protected expression without review. This handoff includes no third-party source code, assets or styling.

**Prohibited phrasing in agent change plans:** "port Odysseus X", "integrate Odysseus", "use their classes", "copy their UI", "implement Odysseus architecture", "wire to Odysseus API".

**Required phrasing:** "Server Rock needs [specific user outcome]. Design a native, independent solution in our codebase, with tests for these observable behaviors."

Acceptance: no new dependency, code copy, asset, submodule, package, network call, API compatibility layer or build-time step referencing Odysseus. Inspiration remains in this separate documentation directory only.
