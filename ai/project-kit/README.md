# Project Kit - AI Agents (Griot)

The AI system ([own-stack]): Trigger.dev v3 agents, scheduled workflows, Copilot conversation engine.

**Orchestration contract** (`research/ai-integration.md` §2a — authoritative): `ai/` is a standalone compute/orchestration adapter. The .NET backend triggers tasks (server-to-server `TRIGGER_SECRET_KEY`) and is the only writer of source-of-truth data — task results are written back through the .NET API (`POST /api/webhooks/trigger` HMAC or `GRIOT_SERVICE_TOKEN` REST). Web/mobile never trigger or poll Trigger.dev — they call the .NET API; the only direct web↔Trigger channel is the read-only Copilot realtime stream (scoped access token).

```
ai/project-kit/
├── README.md
├── context/
│   ├── architecture.md
│   ├── roster.md
│   ├── security-guardrails.md
│   ├── code-standards.md
│   └── progress-tracker.md
├── feature-specs/   <- 5 specs
├── diagrams/
└── examples/
```

Reading order: root `AGENTS.md` -> `ai/AGENTS.md` -> `research/ai-integration.md` -> contexts -> spec.
