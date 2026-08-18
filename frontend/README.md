# CashClarity Frontend

## Verify

```bash
npm test
npm run test:storybook
../scripts/verify frontend
../scripts/verify frontend-acceptance
```

Acceptance is Playwright visual testing against the packaged frontend and
`cashclarity-api-lite`.

## Frontend Structure

- `src/routes/*Route.tsx` owns route/container concerns: Zustand stores, API-facing actions,
  router integration and screen-level data shaping.
- `src/components/*.tsx` is presentational UI. Components receive data and callbacks through
  props and must not import stores, API clients or `react-router-dom`.

[![CI](https://github.com/ppardalj/cashclarity-frontend/actions/workflows/deploy.yml/badge.svg)](https://github.com/ppardalj/cashclarity-frontend/actions)

This contains everything you need to run your app locally.

## Run Locally

**Prerequisites:**  Node.js

1. Install dependencies:
   `npm install`
2. Run the app:
   `npm run dev`
