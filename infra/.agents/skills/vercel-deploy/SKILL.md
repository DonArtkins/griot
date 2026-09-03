---
name: vercel-deploy
description: "Deploy the Griot web app to Vercel using the Vite framework preset, per the bootcamp's exact 5-step flow."
metadata:
  version: "0.1.0"
---

# Vercel Deploy Skill

## The bootcamp's 5 steps (exact)

1. Push `web/` to GitHub.
2. Import the repository into Vercel.
3. Select the **Vite** framework preset.
4. Configure environment variables (`VITE_API_URL` → deployed API base).
5. Deploy and verify the live URL.

## Notes

- Vercel auto-detects Vite; assets served from `dist`.
- Preview deployments per PR; production on `main` (GitHub integration).
- Lighthouse budget checked after deploy (qa).
