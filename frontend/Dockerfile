FROM nginx:alpine

# Refresh Alpine packages so the base image doesn't ship stale, patched CVEs
# (e.g. util-linux/libuuid) that upstream already fixed in the repositories.
RUN apk update && apk upgrade --no-cache

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY dist/ /usr/share/nginx/html/
COPY --chmod=755 docker-entrypoint.d/95-generate-runtime-env.sh /docker-entrypoint.d/

EXPOSE 80
