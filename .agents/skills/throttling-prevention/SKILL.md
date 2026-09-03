---
name: throttling-prevention
description: "API throttling prevention and recovery for AI agents. Apply on all agent-assisted work on the Griot repo to stay within LLM provider rate limits."
metadata:
  version: "0.1.0"
---

# Throttling Prevention & Recovery

## Recovery (on "request was throttled")

1. **PAUSE** — stop making tool calls immediately.
2. **WAIT** — `sleep 5` minimum.
3. **BATCH** — combine remaining small operations into fewer, larger ones.
4. **VERIFY** — confirm the last operation completed before continuing.

## Prevention (mandatory)

- Batch file operations: read/write multiple related files in single calls.
- Use glob + `find`/`grep` in single shell commands instead of many tool calls.
- Consolidate writes: plan all changes, then execute in batches.
- Insert deliberate 2–3 s pauses roughly every 5 calls on long tasks.
- Check state before reading a file to avoid redundant reads.
- Move rapid-iteration work into executable scripts (single shell call).
- Checkpoint progress: write incremental state so recovery doesn't require re-reading everything.

## Rules

- Never make 10+ rapid sequential tool calls without a pause.
- Never retry immediately after throttling.
- Never read the same file twice in quick succession.
- Never give up after one throttle error — implement recovery and continue.
