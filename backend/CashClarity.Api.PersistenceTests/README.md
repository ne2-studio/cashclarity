# CashClarity persistence tests

Repository tests against real Postgres via Testcontainers.

Use this suite for behavior fakes cannot prove: EF SQL translation, migrations, constraints,
cascade/set-null delete behavior, and provider-specific data types.

Run:

```bash
../scripts/verify backend-persistence
```
