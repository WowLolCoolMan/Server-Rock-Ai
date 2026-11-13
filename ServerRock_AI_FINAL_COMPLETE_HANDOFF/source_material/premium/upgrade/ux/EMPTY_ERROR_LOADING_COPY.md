# UX writing patterns — no unexplained blank states

| Situation | Heading | Explanation | Main action |
|---|---|---|---|
| No models | Connect your first model | Server Rock can use a model running on this device or a provider you choose. | Discover local models |
| No project | Open a development project | Select a local folder or clone a repository to give a coding agent context. | Open folder |
| No chats | Start a conversation | Choose a model and send a prompt. History will appear here when saved. | New chat |
| Provider unreachable | Model connection unavailable | Connection to [exact provider/host] failed: [safe reason]. Your message is saved as a draft. | Retry connection |
| Shell command needs consent | Review command before running | This command will execute in [workspace] with [current permission scope]. | View full command |
| Stale edit proposal | Files changed since preview | A source file changed after the agent created this proposal. It has not been applied. | Compare revisions |
| Skill blocked | Skill needs review | [skill] requests new permissions or has unverified source metadata. | Review details |
| Download interrupted | Download paused | Partial verified data is retained if supported; nothing was installed. | Resume download |
| Keyring missing | Secure storage unavailable | This Linux session has no supported credential store. Cloud credentials will not be saved insecurely. | View setup help |
| Offline cloud mode | You're offline | Your cloud model is unreachable. Local models are available if configured and you choose to switch. | See local models |
| Empty workflow | No workflows yet | Create a reusable set of agent steps, approvals and tests. | Create workflow |
| Failed update | Update not installed | Verification failed; the current app and data remain unchanged. | View diagnostics |

Error copy includes: what happened, what did **not** happen, what is safe to retry, a diagnostic detail link, and next action. Toasts do not claim success until persisted result returned. No dark-pattern CTA, false '100%', or fake tests/results.
