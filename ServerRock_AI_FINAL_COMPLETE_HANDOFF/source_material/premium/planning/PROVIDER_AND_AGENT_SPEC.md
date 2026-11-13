# Providers, routing and agents

- Provider registry with endpoint, authentication reference, capabilities, health, model inventory and rate-limit metadata.
- Model selection displays actual provider, local/cloud status, capability limitations and expected data egress.
- Fallback rules explicit and sticky per session; cloud-to-local may be automatic only under configured policy, local-to-cloud requires clear consent if data would leave device.
- Support streamed tokens/events, cancellation, retries only when safe, circuit breakers and useful errors.
- Parallel agents need independent contexts, task ownership, budgets, cancellation and shared diff review.
- Agent editing flow: inspect files -> plan -> propose diff -> approval based on risk -> apply -> build/test -> summarize actual results -> rollback or repair.
- No agent claims it executed commands or inspected files unless tool evidence exists.
- Context inspector shows included files, exclusions, estimated token budget and truncation; never fabricate complete context coverage.
- External Ollama, llama.cpp, LM Studio, ComfyUI and Hugging Face integrations should prefer supported APIs, not uncontrolled binaries.
