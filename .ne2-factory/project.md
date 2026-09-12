# Project context for ne2-factory

Prose context factory agents reason about. Grows as later slices move project
knowledge out of the agents. See the ne2-factory plugin's `docs/environment-contract.md`.

## Reviewer

Pedro — who an agent asks when a decision needs a human, and whose approval a change
targets.

## Default branch

`main` — trunk-based, so also the integration branch.

## Services

Independently deployable, no shared code between them.

- `backend/` — ASP.NET Core API, EF Core, PostgreSQL, acceptance tests.
- `frontend/` — React 19 application, Vite, Tailwind CSS, Zustand, Playwright.

## Documentation

Read before an architectural, persistence, deployment, auth, testing, or cross-service
change.

- `docs/ARCHITECTURE.md` — service boundaries, data flow, cross-cutting decisions.
- `docs/architecture/testing.md` — testing architecture.
- `docs/operations/local-development.md` — local environment setup.
- `docs/operations/api-lite.md` — in-memory backend used for frontend acceptance.
- `backend/README.md` — backend verification and structure.
- `frontend/README.md` — frontend verification and structure.
