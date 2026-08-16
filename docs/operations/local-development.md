# Local development

Use the repository helper from the root:

```bash
./scripts/run-env frontend-dev
./scripts/run-env backend-dev
./scripts/run-env full-stack
./scripts/run-env cleanup
```

Modes:

| Mode | Frontend | Backend | Purpose |
|---|---|---|---|
| `frontend-dev` | `http://localhost:5173` | `http://localhost:5051` | Vite frontend against `api-lite` |
| `backend-dev` | none | `http://localhost:5050` | Local real API against Docker Postgres |
| `full-stack` | `http://localhost:3000` | `http://localhost:5051` | Packaged frontend against `api-lite` |

Logs:

```bash
./scripts/run-env logs frontend-dev
./scripts/run-env logs backend-dev backend
./scripts/run-env logs full-stack frontend
```

The helper owns only `.run/`, `docker-compose.lite.yml` services, and the
`cashclarity-run-postgres` container.
