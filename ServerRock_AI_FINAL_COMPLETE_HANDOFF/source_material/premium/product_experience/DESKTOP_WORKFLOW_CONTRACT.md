# Predictable workflow design contract
## State machine
Draft -> Preflight -> Preview -> AwaitApproval -> Queued -> Running -> Verifying -> Completed
Alternate: Cancelled, Failed, RolledBack, NeedsAttention. State changes recorded with operation IDs and monotonic sequencing.
## UI contract
Every screen has one clear primary action, visible active model/workspace, current permissions, status, logs, undo/retry where supported. Do not hide consequential state behind navigation. Dialogs are for focused confirmations, not whole product modules.
## Engineering contract
Separate Uno Views, ViewModels, application services, Core domain services and OS adapters. Use cancellable operations, dependency injection, explicit result types, integration tests, native visual QA and Windows/Linux packaged-build smoke.
## Definition of done
No unverified success banners, silent writes, silent cloud transfers, nonfunctional controls, unhandled restart states, inaccessible error recovery, or orphaned install artifacts.
