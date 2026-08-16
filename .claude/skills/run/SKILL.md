---
name: run
description: "Provides a known, stable local CashClarity environment. Use when asked to run, start, inspect, screenshot, or manually exercise the frontend or API."
model: haiku
---

## Goal

Provide one deterministic local environment and report exactly what is running.
Do not mix verification strategy, test-suite instructions, or historical
workarounds into this skill; use `verify` for deciding evidence.

Use the helper script from the repository root:

```bash
./scripts/run-env <mode>
```

If the script fails, do not work around it by starting services manually.
Use the helper's diagnostics and cleanup command. Fix repository-owned defects
when they are part of the task; otherwise report the failure. Do not stop or
modify unrelated user processes.

## Modes

| Mode | Use when | Frontend | Backend | Docker components | Dev components |
|---|---|---|---|---|---|
| `frontend-dev` | Inspecting or changing the frontend UI | `http://localhost:5173` | `http://localhost:5051` | `api-lite` | Vite frontend server |
| `backend-dev` | Inspecting or changing the real API with fast local recompilation and real persistence | none | `http://localhost:5050` | Postgres | `dotnet run` API |
| `full-stack` | Packaged frontend inspection against the stable lite API | `http://localhost:3000` | `http://localhost:5051` | frontend, api-lite | none |

`frontend-dev` deliberately uses `cashclarity-api-lite`, not the real backend, so UI
work has a stable in-memory backend and Vite hot reload. It must not leave a
Docker frontend serving on `3000`.

`frontend-dev` is suitable for frontend UI work against the lite API contract.
It does not reproduce real persistence, storage, authentication wiring,
serialization, or built-image behavior.

## Port contract

- Frontend Vite dev: `http://localhost:5173`
- Frontend Docker image: `http://localhost:3000`
- Real backend: `http://localhost:5050`
- Lite backend: `http://localhost:5051`

Ports are fixed. The helper must fail on ambiguous conflicts instead of silently
switching ports.

Starting a mode must leave only the requested repository-owned environment
active. The helper may reconcile processes and compose stacks it owns from a
previous mode, but must not stop unrelated user processes. If an unrelated
process occupies a required port, report the conflict.

## Running

Choose the narrowest mode that matches the request:

```bash
./scripts/run-env frontend-dev
./scripts/run-env backend-dev
./scripts/run-env full-stack
```

The command waits until the requested surface is usable, not merely until its
process or TCP port exists:

- `frontend-dev`: Vite responds and `api-lite` is healthy.
- `backend-dev`: the API responds and Postgres is ready.
- `full-stack`: the packaged frontend loads and `api-lite` is healthy.

At the end, copy the concrete summary it prints, including:

```text
Mode:
Frontend:
Backend:
Frontend source:
Backend source:
Health:
Test user:
Logs:
Cleanup:
```

Clearly identify one primary URL for the requested interaction: the `Frontend`
URL when a frontend exists, otherwise the `Backend` URL. Include the remaining
URLs in the environment summary when relevant.

## Identity

Local frontend modes use `VITE_AUTH_DISABLED=true`; `api-lite` accepts the fake
bearer token emitted by the frontend. `backend-dev` runs the real API and uses
the configured JWT authority, so use it mainly for health, persistence, and
server-side debugging unless a real token is available.

## Logs and cleanup

Use the helper instead of ad hoc `docker ps` inspection:

```bash
./scripts/run-env logs frontend-dev
./scripts/run-env logs frontend-dev frontend
./scripts/run-env logs frontend-dev backend
./scripts/run-env logs backend-dev
./scripts/run-env logs backend-dev infra
./scripts/run-env logs full-stack
./scripts/run-env logs full-stack backend
./scripts/run-env cleanup
```

`cleanup` stops local dev processes and compose stacks started by the helper while
keeping named volumes.

## Advanced details

For deeper operational background, use:

- `docs/operations/api-lite.md`
- `docs/architecture/testing.md` only when selecting verification evidence
