---
name: skill-creator-validator
description: >-
  Draft and validate a reusable SKILL.md with trigger and non-trigger cases, dependency checks and least-privilege permissions, after repeated work or a user request for a new skill.
license: CC0-1.0
compatibility: Requires an application skill registry; tool operations still require host permissions.
---

# Skill Creator and Validator

1. Define specific job, target users, prerequisites, activation triggers and non-triggers.
2. Check registry for a matching existing skill; avoid clones.
3. Collect authoritative docs and environment versions with approved tools. Clearly separate verified information from assumptions.
4. Create in **user-owned draft folder**: SKILL.md with YAML frontmatter name and description, optional references/, scripts/, assets/, LICENSE, tests.
5. Document exact supported environments, tool permissions, privacy/network behavior, expected inputs, outputs and failure modes.
6. Run static checks: YAML syntax, path safety, namespace collision, accidental secrets, dependencies, size, supported OS, skill license and source attribution.
7. Test 10 positive examples, 5 non-trigger examples, missing tool, canceled execution and permission denial on at least one target model. Keep real pass/fail evidence.
8. Require explicit user review to enable or publish to Marketplace. Never install packages or upload source automatically.
9. Version and retain rollback information; don't silently replace a user's edits.

## Negative triggers

Never create a skill for a single trivial answer if no repeated workflow exists; never imply that a script is safe solely because YAML parsed.

