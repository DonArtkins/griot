---
name: owasp-review
description: "OWASP Top-10 basics review for the Griot estate: injection (parameterized SQL), broken auth (refresh rotation), sensitive data (no localStorage tokens), CORS allow-list, AI principal scope."
metadata:
  version: "0.1.0"
---

# OWASP Review Skill

## Checklist (log per item)

1. Injection - every Dapper/raw-SQL call parameterized.
2. Broken auth - refresh rotation replay fails; Argon2 hashing.
3. Sensitive data - no tokens in localStorage; cookie flags HttpOnly; Secure; SameSite.
4. CORS - API only serves the Vercel origin in prod.
5. AI surface - ai-agent principal scope (no deletes/invites); prompt-injection: user text as data.

## Output

- `docs/OWASP-REVIEW.md` with findings or "no issue found" per item; re-check on each auth/guard change.
