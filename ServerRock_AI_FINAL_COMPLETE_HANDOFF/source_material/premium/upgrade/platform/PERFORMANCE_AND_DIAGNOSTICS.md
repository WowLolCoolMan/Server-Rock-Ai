# Performance envelope and professional diagnostics

Measure app startup, cold/warm navigation, scrolling 10k messages, chat stream responsiveness, model scan delay, file-tree 100k entries, Git diff of large changes, terminal stdout throughput, workflow canvas 200 nodes, marketplace catalog filtering, memory/CPU idle, and background GPU model use. Record test machine, OS, architecture, build hash, dataset, methodology and distributions (median/P95). Do not invent current values or promise unrealistic numbers.

Performance architecture: virtualize long lists, debounce search with cancellation, avoid constructing hidden UI views, lazy-load library catalogs, pool HTTP clients, keep heavy indexing/process work off UI thread, batch event projection, cap terminal buffer and log size, async file I/O, cache by stable revision, throttle expensive animations, recover from GPU OOM without freezing the desktop shell.

Diagnostics center: human-readable connection tests, OS/keyring/PTy health, storage map, agent run IDs, logs (redacted), error categories, update manifest status and a single-click sanitized preview/export. User chooses whether to share; no telemetry by default.
