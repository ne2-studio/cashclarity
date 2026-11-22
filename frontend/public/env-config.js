// Overwritten at deploy time (frontend-deploy.yml) and at acceptance container start
// (docker-entrypoint.d/95-generate-runtime-env.sh).
// Empty values make runtimeConfig.ts fall back to Vite build-time values.
window.__ENV__ = {};
