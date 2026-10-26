# CashClarity

## Verification

Canonical entrypoint:

```bash
./scripts/verify all
```

See [`docs/architecture/testing.md`](docs/architecture/testing.md).

Local environments:

```bash
./scripts/run-env frontend-dev
./scripts/run-env backend-dev
./scripts/run-env full-stack
```

See [`docs/operations/local-development.md`](docs/operations/local-development.md).

Git hook:

```bash
git config core.hooksPath .githooks
```

Commit messages must follow Conventional Commits.

A personal treasury management app built on double-entry accounting. It separates your real bank balance into a pool of named "spaces" (budget buckets) so you always know how much cash is truly available versus already committed.

## Features

- **Dashboard** — real bank balance, total committed across spaces, and available cash at a glance
- **Bank statement** — import movements from CSV and reconcile them against journal entries
- **Spaces** — envelope-style budget buckets backed by accounting accounts
- **Entities** — payees and counterparties
- **Journal** — full double-entry journal with debit/credit lines
- **Chart of Accounts** — manage the account structure

## Architecture

Monorepo with two independently deployable services:

| Directory | Stack |
|-----------|-------|
| `frontend/` | React 19, TypeScript, Vite, Tailwind CSS v4, Zustand, react-oidc-context |
| `backend/` | ASP.NET Core (.NET 10), Entity Framework Core, PostgreSQL, Serilog |

Authentication is handled via OIDC (JWT Bearer). The frontend obtains a token from the OIDC provider and sends it on every API request.

## Getting started

### Prerequisites

- Node.js 22+
- .NET 10 SDK
- PostgreSQL

### Backend

```bash
cd backend

# Copy and edit connection string + auth settings
cp CashClarity.Api/appsettings.json CashClarity.Api/appsettings.Development.json

dotnet restore
dotnet run --project CashClarity.Api
```

The API starts on `http://localhost:5000`. Migrations run automatically on startup. Swagger UI is available at `/swagger` in development.

### Frontend

```bash
cd frontend

cp .env.example .env
# Set VITE_API_URL to your backend URL

npm install
npm run dev
```

The app starts on `http://localhost:3000`.

## Servidor MCP

La API expone un servidor MCP en `/mcp` (HTTP streamable). Se autentica con el mismo login OIDC que
la web, y cada endpoint REST tiene su tool:

| Endpoint REST | Tool MCP | Tipo |
|---|---|---|
| `GET /server/health` | `health_check` | lectura |
| `GET /server/workspaces` | `list_workspaces` | lectura |
| `POST /server/workspaces` | `create_workspace` | escritura |
| `GET /server/workspaces/{workspaceId}/accounts` | `list_accounts` | lectura |
| `POST /server/workspaces/{workspaceId}/accounts` | `create_account` | escritura |
| `PATCH /server/workspaces/{workspaceId}/accounts/{id}` | `update_account` | escritura |
| `DELETE /server/workspaces/{workspaceId}/accounts/{id}` | `delete_account` | destructiva |
| `GET /server/workspaces/{workspaceId}/journal-entries` | `list_journal_entries` | lectura |
| `POST /server/workspaces/{workspaceId}/journal-entries` | `create_journal_entry` | escritura |
| `PATCH /server/workspaces/{workspaceId}/journal-entries/{id}` | `update_journal_entry` | destructiva (sustituye líneas) |
| `DELETE /server/workspaces/{workspaceId}/journal-entries/{id}` | `delete_journal_entry` | destructiva |
| `GET /server/workspaces/{workspaceId}/bank-movements` | `list_bank_movements` | lectura |
| `POST /server/workspaces/{workspaceId}/bank-movements` | `create_bank_movement` | escritura |
| `PATCH /server/workspaces/{workspaceId}/bank-movements/{id}` | `update_bank_movement` | escritura |
| `DELETE /server/workspaces/{workspaceId}/bank-movements/{id}` | `delete_bank_movement` | destructiva |
| `POST /server/workspaces/{workspaceId}/bank-movements/imports/preview` | `preview_bank_movement_import` | lectura (`csvText` o `fileBase64`) |
| `POST /server/workspaces/{workspaceId}/bank-movements/imports` | `commit_bank_movement_import` | escritura |

- Registro en Claude Code y alta del Client ID en Zitadel: [`docs/user/mcp-claude-code.md`](docs/user/mcp-claude-code.md)
- Diseño, configuración y tests: [`docs/operations/mcp.md`](docs/operations/mcp.md)

## Environment variables

### Frontend (`frontend/.env`)

| Variable | Description |
|----------|-------------|
| `VITE_API_URL` | Base URL of the backend API |

### Backend (`appsettings.json`)

| Key | Description |
|-----|-------------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string |
| `Auth:Authority` | OIDC authority URL |
| `Auth:Audience` | Expected JWT audience |
| `Mcp:ResourceUrl` | Public URL of the MCP endpoint (`https://<api-host>/mcp`), recommended behind a proxy |

## Deployment

Both services are containerized. CI/CD runs on push to `main`, delegates checks to
`./scripts/verify`, validates packaged images, pushes to GitHub Container Registry,
and triggers Coolify deploy webhooks.

```bash
# Build backend image
docker build -t cashclarity-backend ./backend

# Build frontend image (requires a prior npm run build)
cd frontend && npm run build
docker build -t cashclarity-frontend ./frontend
```
