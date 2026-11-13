# AI agent execution playbook

## For each feature
1. Locate relevant current code and tests; note what already works.
2. Define desired behavior and failure modes in user terms.
3. Identify interfaces and dependent tasks; isolate files to be edited.
4. Check upstream donor suitability; verify license before code reuse.
5. Write or update a failing test where feasible.
6. Implement smallest coherent vertical slice preserving current behavior.
7. Run focused tests, then platform smoke and broader regression tests.
8. Capture evidence and update `docs/GOAL_EXECUTION.md`.
9. Open PR with scope, user-visible change, migration impact, security impact, test commands and unresolved issues.
10. Stop for human decision on unclear product behavior, destructive changes, new secret handling or license conflict.

## Avoid
Rewriting the UI framework; broad search/replace; hidden feature flags; speculative 100-file refactors; automatic remote data transfer; importing donor repos wholesale; inventing success metrics; marking TODOs complete without tests.

## Output expectations
Code changes must be accompanied by reproducible tests and a plain-language behavior explanation. Research-only tasks produce evidence and a decision, not code for its own sake.
