#!/bin/sh
set -eu

envsubst '${VITE_API_URL} ${VITE_AUTH_DISABLED} ${VITE_OIDC_AUTHORITY} ${VITE_OIDC_CLIENT_ID} ${VITE_OIDC_CALLBACK_URI} ${VITE_OIDC_LOGOUT_REDIRECT_URI}' \
  < /usr/share/nginx/html/env-config.template.js \
  > /usr/share/nginx/html/env-config.js
