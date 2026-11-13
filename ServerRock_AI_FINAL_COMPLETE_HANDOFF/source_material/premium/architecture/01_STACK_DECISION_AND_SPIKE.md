# Desktop framework decision record and evaluation

## Candidates
A. Keep Uno XAML + C#/.NET core; incrementally modularize view models, navigation and services.
B. Electron + React/TypeScript renderer + existing .NET core as supervised sidecar or embedded host; Node limited to shell, PTY and IPC bridge.
C. Tauri + React/TypeScript + existing .NET core; investigate Rust/sidecar and PTY integration complexity.

## Candidate React stack (only after decision)
Electron or Tauri shell; React + TypeScript; Tailwind + shadcn/ui + Radix; Zustand for ephemeral UI state; TanStack Query for server/backend state; TanStack Router for typed navigation; react-resizable-panels; Monaco for editing/diff; xterm.js + node-pty or equivalent audited PTY backend; SQLite/FTS5; gRPC or typed authenticated IPC; .NET Core application services; MCP and ACP adapters; OpenTelemetry/Serilog; Zod and backend validation; Vitest, Playwright and .NET tests; electron-builder only if Electron selected.

**Avoid** a redundant Node business-logic backend or wholesale rewrite of existing C# services. Never assume Electron guarantees polish or that Uno cannot host an editor/terminal.

## Required comparable prototype
Implement the same **working vertical slice** in the existing Uno app and a throwaway React shell: resizable Chat, Monaco-like code/diff editing, real PTY, task activity, model picker, and backend event streaming. Demonstrate with existing .NET core service interfaces; no fake buttons. Verify Windows and Linux. Compare complexity, keyboard access, accessibility, startup/idle memory, security isolation, build/packaging, developer iteration and recovery. Document actual measurements and maintenance cost. Obtain explicit user approval before framework migration.

## Decision output
ADR with recommendation, evidence, costs, compatibility and rollback plan. Until approved, keep implementing shared .NET Core abstractions and non-framework-specific tests.
