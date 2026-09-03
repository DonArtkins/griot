# Deployment Targets

| App | Artifact | Host | Release step |
|---|---|---|---|
| backend | Docker image (multi-stage .NET 8, port 8080) | Railway (primary) · Render (fallback) · Azure App Service (variant) | `dotnet ef database update` as release command |
| web | `dist` (Vite build) | Vercel (Vite preset) | Vercel build/deploy via GitHub integration |
| mcp | Docker image (Streamable HTTP, port 3001) | Railway | container start |
| ai | Trigger.dev deployment | Trigger cloud (or self-hosted Railway) | `trigger.dev deploy` |
| mobile | APK/AAB | CI artifact → Play/APK | Docker-pinned Flutter build job |

Host matrix rationale (from research): Railway chosen as Docker-native + one-click DB add-ons; Render as fallback; Azure documented as an optional stretch (`az webapp up`).
