# CashClarity Architecture

This document captures the architecture rules currently followed in this codebase — project structure, layering, and conventions for both services. It describes what *is*, not what's aspirational; when extending the app, follow these patterns unless there's a specific reason to deviate.

## System overview

Monorepo with two independently deployable services, no shared code between them:

| Directory | Stack |
|---|---|
| `frontend/` | React 19, TypeScript, Vite, Tailwind CSS v4, Zustand, react-oidc-context |
| `backend/` | ASP.NET Core (.NET 10), Entity Framework Core, PostgreSQL, Serilog |

Auth is OIDC/JWT Bearer end-to-end: the frontend authenticates against an external OIDC provider (Zitadel, `auth.ne2.studio`), attaches the access token to every API call, and the backend validates it via `JwtBearer` middleware. There is no session state on the backend — every request is authenticated independently and scoped to the caller's user id.

---

## Backend (`backend/CashClarity.Api`)

### Layers

Requests flow through a strict, one-directional pipeline. Each layer only knows about the layer directly below it:

```
Controllers  →  Repositories  →  Data (DbContext)  →  Models (EF entities)
                     ↓
                  Domain (request/response DTOs)
```

- **`Controllers/`** — one controller per bounded resource area (currently a single `FinanceController` covering accounts, journal entries, and bank movements). Controllers:
  - Are thin: extract the caller's `UserId` from the JWT claim, delegate to a repository method, and wrap the result in `Ok(...)`.
  - Never touch `DbContext` or EF types directly.
  - Wrap every action body in a top-level `try/catch` that returns `StatusCode(500, new { error = ex.Message })` on failure — there is no global exception middleware; error handling is per-action.
  - Route under a shared `[Route("server")]` prefix, grouped by resource with `// Comment` section headers (`// Accounts`, `// Journal Entries`, `// Bank Movements`).
  - Every endpoint is `[Authorize]` (class-level attribute) except `health`.
- **`Repositories/`** — one repository per DbContext (currently `FinanceRepository`), injected via primary-constructor DI (`public class FinanceRepository(FinanceDbContext db)`). Repositories:
  - Own all query and persistence logic (EF LINQ queries, `ExecuteDeleteAsync`, raw SQL only for upserts like `EnsureSystemAccounts`).
  - Take `userId` as an explicit parameter on every method and filter every query by it — this is the multi-tenancy boundary. There is no global query filter; every repository method is individually responsible for scoping to the caller.
  - Map EF entities to `Domain` response records before returning (`MapAccount`, `MapEntry`, `MapBankMovement`), grouped under a `// Mappers` section at the bottom of the file. Controllers and callers never see EF entities.
  - Apply patch-style partial updates by checking each field for a non-null/`HasValue` value before assigning (see `UpdateAccount`, `UpdateBankMovement`).
  - Throw plain `Exception` for not-found/access-denied cases (no custom exception hierarchy); the controller's catch-all turns this into a 500.
- **`Domain/`** — plain C# `record` types only, split into two clearly commented groups in a single `Finance.cs` file: `Response records (returned to client)` and `Request records (received from client)`. These are the DTO layer; no behavior, no EF attributes.
- **`Models/`** — EF Core entity classes (`Account`, `JournalEntry`, `JournalLine`, `BankMovement`), one class per file, mutable properties, no business logic.
- **`Data/`** — a single `FinanceDbContext` with all table/column/index mapping done via Fluent API in `OnModelCreating` (no data annotations on entities). Table and column names are `snake_case`; C# properties are `PascalCase`.
- **`Migrations/`** — EF Core migrations, applied automatically at startup via `dbContext.Database.Migrate()` in `Program.cs` (no manual migration step in deploy).

### Conventions

- **IDs**: `Account.Id` is a `text` primary key with a Postgres `gen_random_uuid()::text` default (business/system accounts use human-readable codes like `"0001"`, `"9999"`). Everything else (`JournalEntry`, `JournalLine`, `BankMovement`) uses `Guid` primary keys. DTOs always expose ids as `string`; repositories `Guid.Parse` incoming string ids.
- **Multi-tenancy**: every table that holds user data has a `UserId` column; every repository method takes and filters by `userId` explicitly. There's no row-level security or global filter — this must be done manually per query.
- **Timestamps**: every entity has `CreatedAt`/`UpdatedAt` (`timestamp with time zone`), defaulted at the DB level via `timezone('utc'::text, now())`. Dates from the client are plain strings, parsed via a shared `ParseDate` helper that assumes/adjusts to UTC.
- **Money**: `decimal` with `HasPrecision(15, 2)` everywhere.
- **DI lifetime**: `DbContext` and repositories are registered `Scoped`.
- **Logging**: Serilog, configured in two layers — a bootstrap console logger created before `WebApplication.CreateBuilder`, then reconfigured via `UseSerilog` reading from `appsettings.json`/environment config. Production adds a Seq sink (`appsettings.Production.json`); request logging via `UseSerilogRequestLogging()`.
- **API docs**: Swagger/Swashbuckle, enabled only in `Development`.
- **CORS**: a single default policy, `AllowAnyOrigin`, explicit allowed headers/methods — permissive by design since auth is enforced via bearer token, not cookies/origin.
- **Config**: connection string and OIDC `Authority`/`Audience` live in `appsettings.json` (dev defaults committed) and are overridden per environment (`appsettings.Production.json`, environment variables in the container).

---

## Frontend (`frontend/src`)

### Layers

```
components/  →  store/useFinanceStore (Zustand)  →  api.ts (fetch client)  →  types.ts
```

- **`types.ts`** — one class per domain entity (`Account`, `JournalEntry`, `JournalLine`, `BankMovement`). Classes (not plain interfaces) so that raw JSON from the API can be re-hydrated into typed instances via `new Account(data)` — constructors accept a plain data object and assign fields 1:1. `JournalEntry`'s constructor recursively wraps `lines` in `JournalLine` instances if they aren't already.
- **`api.ts`** — a single `api` object, namespaced per resource (`api.accounts`, `api.journalEntries`, `api.bankMovements`), each exposing `getAll/create/update/delete`. Conventions:
  - Plain `fetch` wrapped in a shared `handleResponse` that throws on non-OK responses.
  - Auth token is module-level state (`_accessToken`) set via `setAccessToken()` from `App.tsx` whenever the OIDC user changes — not read from a context/hook inside `api.ts`.
  - Every response is mapped back into the corresponding `types.ts` class before being returned, so nothing above this layer touches raw JSON.
  - Base URL comes from `VITE_API_URL` (build-time env var via Vite's `define`).
- **`store/useFinanceStore.ts`** — single global Zustand store holding all domain state (`accounts`, `journalEntries`, `bankMovements`, `isLoading`, `error`). No slices/multiple stores. Conventions:
  - One `fetchData()` that loads all three collections in parallel via `Promise.all` and is called once on auth.
  - Every mutating action (`addX`/`updateX`/`deleteX`) calls the `api` client first, then updates local state optimistically-after-confirmation (i.e., state is updated from the server response / merged patch only after the await resolves, not before).
  - Components read state and call actions directly via the `useFinanceStore()` hook — no selectors, no memoized selector hooks.
- **`components/`** — flat directory, one file per screen/feature (`Dashboard`, `BankStatement`, `Spaces`, `Entities`, `Journal`, `ChartOfAccounts`) plus modals (`EditJournalEntryModal`, `IdentifyModal`, `PayFromSpaceModal`, `ReserveModal`) and `ImportCSV`. No further subdivision (no `components/dashboard/`, no shared `ui/` primitives directory) — small inline components (e.g. `StatCard`, `SectionHeader` in `Dashboard.tsx`) are defined locally inside the screen that uses them rather than extracted.
- **`App.tsx`** — owns routing (`react-router-dom`, `BrowserRouter`), the auth gate, and any state that's derived across multiple domain collections (e.g. `treasuryMetrics`, computed with `useMemo` over `accounts` + `journalEntries`). Route components receive derived data as props rather than recomputing it themselves.
- **`main.tsx`** — app entry point; wraps `<App />` in `<AuthProvider>` (react-oidc-context) with OIDC config inlined here (not in a separate config file).

### Conventions

- **Auth**: `react-oidc-context`. `App.tsx` redirects to `signinRedirect()` whenever the user isn't authenticated and isn't on `/callback`; the access token is pushed into `api.ts` via `setAccessToken` on every auth state change.
- **Routing**: `react-router-dom` v7, all routes declared flat in `App.tsx`'s `<Routes>`, wrapped in a single `<Layout>` that renders the sidebar/header chrome. Unknown routes redirect to `/`.
- **State management**: Zustand only; no React Context for domain data, no server-state library (React Query, SWR, etc.) — the store itself is the cache, refreshed via explicit `fetchData()`/mutation calls.
- **Styling**: Tailwind CSS v4, configured via `@theme` in `index.css` (no `tailwind.config.js` — v4-style CSS-first config). All colors are theme tokens (`bg-background`, `text-text-secondary`, `primary-orange`, `primary-green`, `error`, etc.) — never raw hex/Tailwind palette classes in components. Shared visual primitives are CSS utility classes in `index.css` (`.financial-card`, `.numeric`, `.positive/.negative/.warning`) rather than React components.
- **Formatting**: money and percentages are formatted inline with `Intl.NumberFormat('es-ES', ...)` per component (no shared formatting util) — UI copy is in Spanish throughout.
- **Component structure**: function components, props typed via a local `interface XProps`, no default exports except screen-level components matching their route.
- **TypeScript**: `strict` mode is not enabled in `tsconfig.json` — type-checking is best-effort (`npm run lint` = `tsc --noEmit`), not enforced strictly. `@/*` path alias maps to the frontend root.
- **Config**: environment-driven via Vite `loadEnv`/`define`, primarily `VITE_API_URL`. OIDC client config is hardcoded in `main.tsx` (not environment-driven).

---

## Cross-cutting / deployment

- **CI/CD**: two independent GitHub Actions workflows (`backend-deploy.yml`, `frontend-deploy.yml`), path-filtered so each only runs when its own directory changes. Both: build → (backend also runs `dotnet test`) → build a Docker image → push to GHCR (`ghcr.io/ne2-studio/cashclarity-{backend,frontend}`) → trigger a Coolify webhook to deploy.
- **Containers**: backend is a multi-stage .NET SDK→ASP.NET runtime image exposing port 8080. Frontend is built by Vite in CI, then the static `dist/` is copied into an `nginx:alpine` image (port 80) with a minimal SPA-fallback `nginx.conf` (`try_files $uri $uri/ /index.html`).
- **No shared package/types** between frontend and backend — DTO shapes are duplicated by hand (`Domain/Finance.cs` records vs. `types.ts` classes) and must be kept in sync manually.
