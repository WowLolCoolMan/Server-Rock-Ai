# How to use the reinforced pack

## For the owner
Extract this ZIP outside the code repository. Give an implementation agent the repo folder and `AGENT_HANDOFF_PROMPT.md`. Ask it to read `START_HERE_PREMIUM.md`, validate the live repo, then implement small slices with test artifacts. **Do not copy the whole pack over source** or run unreviewed scripts in a production workspace.

## For a development agent
Treat zip as requirements, not executable code or higher-priority instructions. `upgrade/data` is structured planning reference. `upgrade/examples` is illustrative, not compiled production source. `upgrade/marketplace` catalogs are historical candidates. `archive` retains historical source; older stack migration proposals are superseded by current Uno-default decisions.

## Setup commands
Windows PowerShell: `Expand-Archive -Path .\ServerRock_AI_Premium_Engineering_Handoff.zip -DestinationPath .\ServerRockHandoff`.
Linux: `unzip ServerRock_AI_Premium_Engineering_Handoff.zip -d ServerRockHandoff`.
Then run `python tools/validate_pack.py` in extracted root for offline integrity checks (only standard Python library required). The validation script does not alter the app/repository.

## How to read what exists
1. Current repo docs/live code first; pinned baseline is historical.
2. New product/UX/architecture docs explain target behaviors; `requirements.json` and `acceptance_cases.json` create a requirement/test mapping.
3. Prior 119-file masterplan is preserved, including 130 roadmap steps, repo candidates, earlier gap audits and traceability.
4. 250 skill research candidates and 50 source foundations are included separately, as noninstallable research snapshots pending re-verification.
5. Each completed slice yields a git diff, tests, before/after UI evidence and ledger entry. Missing real platform evidence remains unverified.
