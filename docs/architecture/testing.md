# Testing strategy

Choose the smallest test that can detect the failure.

| Change | Primary verification |
|---|---|
| Backend application/API logic | `backend/CashClarity.Api.Tests` |
| Backend repository behavior only real Postgres can catch | `backend/CashClarity.Api.PersistenceTests` |
| Built backend image/public HTTP surface | `backend/acceptance-tests` |
| Frontend pure logic/API/store behavior | Vitest unit project |
| Frontend isolated component contracts | Storybook Vitest addon |
| Frontend visual acceptance | Playwright against packaged frontend + `cashclarity-api-lite` |

## Canonical commands

Run from repository root:

| Command | Coverage |
|---|---|
| `./scripts/verify backend` | Restore, Release build, backend unit tests |
| `./scripts/verify backend-persistence` | Repository tests against real Postgres via Testcontainers |
| `./scripts/verify backend-acceptance` | Built backend Docker image, black-box HTTP smoke |
| `./scripts/verify frontend` | Typecheck, production build, Vitest, Storybook executable specs |
| `./scripts/verify frontend-acceptance` | Playwright visual suite against frontend image + API lite |
| `./scripts/verify all` | Full local verification |

GitHub Actions use the same `./scripts/verify` commands before publishing images.

For manual local environments, use `./scripts/run-env`; see
[`docs/operations/local-development.md`](../operations/local-development.md).

## Backend

- Unit tests reference backend source and use the same `InMemoryFinanceRepository` that powers
  `CashClarity.Api.Lite`.
- Persistence tests are separate from `CashClarity.slnx` because they require Docker/Postgres.
  Add tests here only for EF translation, migrations, constraints, or delete behavior.
- Acceptance tests have their own solution and no project reference to backend source. They test
  the built image through HTTP.

## Frontend

- Vitest `unit` runs in Node by default. Component tests can opt into jsdom per file.
- Storybook stories are executable specs through `@storybook/addon-vitest`.
- Playwright acceptance stores screenshot baselines and runs against packaged artifacts, not Vite
  dev source.
