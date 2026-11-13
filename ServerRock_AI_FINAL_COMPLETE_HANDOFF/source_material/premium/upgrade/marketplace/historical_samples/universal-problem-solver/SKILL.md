---
name: universal-problem-solver
description: >-
  Turn ambiguous or complex user goals into small, verified, permission-bounded tasks. Use when no single existing skill adequately covers the task. Do not trigger for simple questions.
license: CC0-1.0
compatibility: Requires an application skill registry; tool operations still require host permissions.
---

# Universal Problem Solver

1. Clarify the deliverable and user constraints from available context; ask only an essential question when indispensable.
2. Decide whether direct answering is faster and sufficient; if so, do not invoke another skill.
3. Identify required domain knowledge, tools, software, licenses and risks. Discover available capabilities, do not invent them.
4. Search installed skill metadata. If no appropriate skill is found, propose a researched skill source or an original draft; do not auto-install.
5. Set measurable success criteria and smallest reversible implementation steps. Prefer one verified subsystem at a time.
6. Request approval before writing project files, installing dependencies, executing arbitrary commands, or sending private material off device.
7. Execute only approved capabilities, capture outputs and errors, validate the artifact itself.
8. Report completed work, evidence, remaining limitations, and recommended next step. Do not claim untested success.

## Negative triggers

Do not activate for short, clear general questions, ordinary conversation, or a task already served by an explicitly chosen specialized skill. Do not override safety, permission and privacy controls.

## Test prompts

Positive: “Build a full research-and-edit pipeline for this project and test what you changed.”
Negative: “What is the keyboard shortcut to copy on Windows?”
