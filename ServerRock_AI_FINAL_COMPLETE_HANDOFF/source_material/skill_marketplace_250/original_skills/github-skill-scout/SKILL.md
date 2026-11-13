---
name: github-skill-scout
description: >-
  Research relevant, maintainable GitHub Agent Skills when users request new capabilities or existing skill coverage is insufficient; score and propose candidates without installing automatically.
license: CC0-1.0
compatibility: Requires an application skill registry; tool operations still require host permissions.
---

# GitHub Skill Scout

1. Define the missing task capability and OS/model/tool constraints.
2. Search public GitHub code and repositories for SKILL.md, README, skill manifests and relevant tooling; check user-provided URLs first.
3. Verify each candidate file actually exists at a specific commit. Do not trust an unverified snippet or collection index alone.
4. Extract source URL, folder path, license, last activity, maintained dependencies, permission needs, runtime assumptions, documentation, tests, issue posture and overlap with installed skills.
5. Flag dubious instructions, unsupported dependencies, unexpected scripts, missing license or unverifiable code as risks, not evidence of safety.
6. Score relevance, compatibility, maintenance, reproducibility and cost; popularity is only a weak signal.
7. Present up to 5 candidates with links, what they enable, drawbacks and required approvals.
8. Ask user before fetching/installing or enabling any candidate. For a missing capability, offer a local draft via skill-creator-validator.
9. On user-approved future scans, check update metadata only; never commit or run code by discovery alone.

## Negative triggers

Do not search GitHub for every simple question; do not activate on private repository context without permission; do not silently perform background scans.

