# Decision Record — License Choice

**Status:** Adopted · **Date:** 2026

## Options considered

| License | Use | Why considered | Why not primary |
|---|---|---|---|
| **MIT** | Permissive, OSI, ubiquitous (React, .NET, Vite… all MIT) | Max adoption, no restrictions, safest for a public capstone/portfolio | — |
| Apache-2.0 | Permissive + explicit patent grant | Patent protection | Heavier for a personal capstone; MIT is enough |
| GPL-3.0 | Copyleft | Strong share-alike | Conflicts with a portfolio/learning project's reuse |
| BUSL / Commons Clause | Source-available | Commercial control | Not OSI; hurts the "production-ready open system" story |
| Unlicense | Public domain | Max freedom | No warranty/attribution framing |

## Decision

**MIT License** — the standard license for open-source web apps and SaaS-adjacent tools. It permits anyone to use/copy/modify/merge/publish/distribute/sublicense/sell, requires the copyright notice preserved, and provides the standard no-warranty. This matches what the surrounding ecosystem (React, Vite, MUI, .NET) uses and keeps the repository deployable, shareable, and credible as a capstone/portfolio.

## Consequences

- Add a `LICENSE` file (MIT text, `Don Artkins`).
- README badge + copyright header.
- If the project later goes fully commercial, revisit Apache-2.0 or BUSL (record a new ADR).