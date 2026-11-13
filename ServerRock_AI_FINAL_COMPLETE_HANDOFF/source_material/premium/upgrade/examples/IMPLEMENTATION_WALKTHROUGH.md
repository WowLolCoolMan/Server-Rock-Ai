# Example vertical slice: from current native app to premium coding workspace

**Goal:** Open project → choose model/profile → ask Coding Agent to alter file → preview precise diff → explicit approve → run tests in PTY → inspect results → close/reopen → review history.

1. Audit `MainPage.CodingAgent.cs`, `.Proposals.cs`, `WorkspaceFileEditor`, `WorkspaceUndoStore`, `WorkspaceTerminalSession`, `MainPage.Chat.cs`. Write current behavior inventory; identify seams and tests.
2. Add typed `WorkspaceId`, `RunId`, `ProposalId`, `ApprovalId` and event types into Core, persist ordered events alongside existing conversation record. Do not migrate existing JSON history without full backup tests.
3. Add application use-case service for `CreateEditProposal`, `RequestApproval`, `ApplyReviewedChange`, `RunVerification`. Move sensitive checks into Core if currently only UI validation exists.
4. Make native view model observe status projection; introduce reusable `DiffReviewPanel` that can be docked in inspector. Avoid making a new unrelated page with duplicate state.
5. Add event-driven progress/approval cards in chat and Activity; reopening task reconstructs from journal, not stale label.
6. Add tests: unauthorized root denied, stale baseline denied, approve once, undo consistency, cancel, UI focus, window resize, Linux file paths.
7. Run on both Windows and Linux. Record exact command output/screenshots; mark one OS unverified if unavailable. Never say fully complete without real end-to-end run.

**What not to do:** generate a beautiful XAML diff panel then leave approval button empty, call a standalone `dotnet build` an acceptance pass, auto-copy third-party chat UI, or replace Uno with a web shell to simplify screenshots.
