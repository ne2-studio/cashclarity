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

[![CI](https://github.com/ppardalj/cashclarity-frontend/actions/workflows/deploy.yml/badge.svg)](https://github.com/ppardalj/cashclarity-frontend/actions)

This contains everything you need to run your app locally.

## Run Locally

**Prerequisites:**  Node.js

1. Install dependencies:
   `npm install`
2. Run the app:
   `npm run dev`
