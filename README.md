# CashClarity

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

## Deployment

Both services are containerized. CI/CD runs on push to `main` (path-filtered per service), builds a Docker image, pushes it to GitHub Container Registry, and triggers a Coolify deploy webhook.

```bash
# Build backend image
docker build -t cashclarity-backend ./backend

# Build frontend image (requires a prior npm run build)
cd frontend && npm run build
docker build -t cashclarity-frontend ./frontend
```
