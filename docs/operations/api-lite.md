# cashclarity-api-lite

`CashClarity.Api.Lite` is an independently built backend image for frontend acceptance tests.
It exposes the same controller routes as the real API but stores data in memory through
`InMemoryFinanceRepository`.

Build:

```bash
docker build -f backend/CashClarity.Api.Lite/Dockerfile -t cashclarity-api-lite:local backend
```

Run:

```bash
docker run --rm -p 5051:8080 cashclarity-api-lite:local
```

Notes:

- No Postgres required.
- Auth is deterministic fake auth; any request gets user `acceptance-user`, or
  `x-test-user-id` if supplied.
- State lasts for the container process lifetime.
- Frontend acceptance uses it through `docker-compose.lite.yml`.
