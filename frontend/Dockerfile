FROM nginx:alpine

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY dist/ /usr/share/nginx/html/
COPY --chmod=755 docker-entrypoint.d/95-generate-runtime-env.sh /docker-entrypoint.d/

EXPOSE 80
