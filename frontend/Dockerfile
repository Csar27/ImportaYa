FROM node:22-alpine AS build
WORKDIR /app

COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci

COPY frontend/ ./
RUN npm run build

FROM nginx:alpine AS runtime
COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY frontend/docker-entrypoint.sh /docker-entrypoint.d/40-importaya-config.sh
COPY --from=build /app/dist/importaya/browser /usr/share/nginx/html
RUN chmod +x /docker-entrypoint.d/40-importaya-config.sh
EXPOSE 80
