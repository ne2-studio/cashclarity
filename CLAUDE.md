# CashClarity

CashClarity is a personal treasury management app built on double-entry
accounting. It separates real bank balance into named spaces so available cash
and committed cash stay explicit.

## Repository structure

Monorepo with two independently deployable services.

```
backend/   # ASP.NET Core API, EF Core, PostgreSQL, acceptance tests
frontend/  # React 19 application, Vite, Tailwind CSS, Zustand, Playwright
docs/      # Architecture, testing and operations documentation
scripts/   # Repo automation, including verification and local environments
```

## Documentation

Read relevant docs before architectural, persistence, deployment, auth, testing
or cross-service changes.

```
docs/architecture/testing.md
docs/operations/local-development.md
backend/README.md
frontend/README.md
```

## Branching

This project uses trunk-based development. If already on `main`, commit changes
and push straight to `main` when requested.

Commit messages must follow Conventional Commits. Use the `commit` skill for
staging and commit message selection.

## Before finishing a non-trivial coding task

Use the `verify` skill and run the narrowest matching `./scripts/verify`
command. Verification must succeed to consider the task done.
