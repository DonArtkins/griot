# Week 05 — Deployment & DevOps Prompts

**Week goal (from the roadmap):** Frontend → Vercel (Vite preset), backend Dockerization (production Dockerfile), Docker Compose (API + SQL Server), GitHub Actions CI/CD; optional Docker Hub / Azure / Railway / Render.

## Status

Not started — write the prompts when Feature 07 (AI layer) exists. Draft on the agent prompts below:

> Build **Feature 08 + 09** (`feature-specs/08-ci-cd-pipeline-github-actions.md`, `09-deployment-and-hosting.md`). Multi-stage backend Dockerfile (sdk:8.0 → aspnet:8.0, port 8080), compose `api` + `mcp` services, GitHub Actions (test gate with SQL Server 2022 service container → parallel app jobs → deploy on `main`), Vercel import steps for `web/` (Vite preset), Railway deployment with migrations as the release command. Everything in `docs/DEPLOYMENT.md` with a per-app runbook and the `.env.example` matrix.