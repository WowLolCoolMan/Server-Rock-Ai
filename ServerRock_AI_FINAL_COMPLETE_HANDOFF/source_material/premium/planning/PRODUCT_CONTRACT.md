# Product contract

Server Rock AI is a transparent, user-controlled, native-feeling desktop hub for cloud and local AI models, coding agents, skills, MCP tools, and user projects. It must be understandable to non-experts and capable for power users.

## Nonnegotiables
- Preserve existing chat, model management, coding agent, recents and other functioning capabilities.
- Native Uno Platform XAML and C#/.NET 10 architecture; Windows and Linux are first-class.
- User-selected, discoverable single root for app-owned persistent files; clearly document external dependencies.
- Never silently route sensitive prompts to a remote model. Display actual provider/model and data transfer scope.
- No unapproved destructive edits, installations, code execution, network egress or credential exposure.
- Skills are real installable/versioned/permissioned components, not merely prompt presets.
- Every long-running action has progress, cancel, retry/recover, failure reason and activity record.
- No invisible page layers, dead-end navigation, ghost buttons or uncommunicated side effects.
- Portable mode and installed mode have documented differences.
- Accessibility, keyboard navigation, screen-reader labels and contrast are release requirements.
- Third-party code reuse must pass licensing and security review. Odysseus = ideas only.

## Experience principles
Show the next meaningful action. Preserve user context when navigating. Surface decisions before irreversible operations. Let users inspect artifacts, paths, logs, permissions and agent reasoning summaries without exposing hidden chain-of-thought. Provide sensible defaults and advanced controls without clutter.
