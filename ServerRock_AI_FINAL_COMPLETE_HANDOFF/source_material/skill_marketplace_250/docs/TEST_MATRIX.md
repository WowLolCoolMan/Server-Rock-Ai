# Release and marketplace verification matrix

## Prototype status

`python3 -m unittest discover -s tests -v` passed **6/6 tests** on packaging host: manifest count and uniqueness, required Shubhamsaboo skills, traversal blocking, tamper detection, symlink blocking and approval requirement. **No live GitHub clone/install/update integration test has been executed in this environment; DNS/network unavailable in local container.** No Server Rock native build was run.

## Must-run acceptance

| Area | Windows | Linux | Failure simulation |
|---|---|---|---|
| Skill catalog search / offline cache | Win11 | Ubuntu/Debian/Fedora/Arch | Empty catalog, damaged JSON |
| GitHub installation | Git for Windows | system git | no git, auth prompts, 404, rate limit, slow network |
| Git subtree update detection | yes | yes | unrelated repo commit, removed subfolder, shallow clone missing tree |
| Integrity + permissions | yes | yes | symlink, traversal, huge file, .git, Windows junction, malicious post-install |
| Atomic update / rollback | yes | yes | cancel, crash mid-copy, user-modified skill, existing backup |
| Model routing | Ollama/LM Studio + cloud | llama.cpp/Ollama + cloud | tool-less model, context truncated, JSON malformed |
| ComfyUI workflow | local endpoint | local endpoint | missing nodes, canceled queue, invalid workflow |
| Hugging Face catalog | model formats | model formats | incompatible model, license/gating, insufficient disk/VRAM |
| GUI focus and accessibility | Narrator / keyboard | Orca / keyboard | narrow screen, scaling, content overflow |
| Packaging | clean install + rollback | real distro install + rollback | unsigned manifest, bad SHA, interrupted installer |

## Product-level “good desktop AI app” exit gates

- Signing, release artifacts, real install/uninstall/rollback, data retention and platform support all verified against exact published binaries.
- Manual and automated user workflows pass: first run, offline chat, cloud consent, model switch, image generation, attachments, GitHub, project read/edit, skills install/use/update, error recovery.
- UI accessible by keyboard and assistive tech, with no clipped modal content or hidden navigation.
- Negative security tests show no script execution on import and no prompt-based permission escalation.
- No false success badges for unavailable plugins, missing models, failed downloads or updates.
- Every release gate has traceable evidence: build hash, test result, OS/runtime, timestamp, issue disposition.
