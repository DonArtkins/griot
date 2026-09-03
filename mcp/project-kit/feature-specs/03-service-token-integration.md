# Feature 03 - Service-Token Integration

## Type

NEW FEATURE ([own-stack])

## What This Delivers

The real GraphQL client wired with `GRIOT_SERVICE_TOKEN`, so every tool executes against the backend as the restricted `ai-agent` principal.

## Dependencies

- Feature 02. Backend feature 09 (token handler) + 05 (GraphQL).

## Context To Read First

- `mcp/project-kit/context/security.md`

## Files Owned

- `mcp/src/lib/graphql.ts`

## Files

CREATE: typed GraphQL client with Bearer `GRIOT_SERVICE_TOKEN`, query/mutation helpers, error mapping.

## Implementation Notes

- Token from env (`GRIOT_API_URL`, `GRIOT_SERVICE_TOKEN`); never logs it.
- All tool calls now round-trip through the backend - audit rows flow into ActivityLogs.

## Separation of Concerns

- Transport/auth isolated in `lib/graphql.ts`; tools stay pure by receiving the client.

## Docker & Deploy

- Env added to the container runtime (feature 04).

## Acceptance Criteria

- [ ] Tools execute against the backend with the service token; write tools respect ai-agent scope
- [ ] Failed auth surfaces clean MCP errors
