# CashClarity backend acceptance tests

Black-box tests for the built backend Docker image. The suite receives the image through
`BACKEND_IMAGE`, starts real Postgres with Testcontainers, and talks to the API over HTTP.

Rules:

- No `ProjectReference` to backend source.
- No `WebApplicationFactory`.
- Assert observable HTTP behavior only.
- Keep domain/repository behavior in unit or persistence tests.

Run:

```bash
../scripts/verify backend-acceptance
```
