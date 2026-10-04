# Builds the site and serves it with Caddy, which also forwards /api and /hubs to the API over Railway's
# private network, so the browser talks to one origin (no CORS, and the refresh cookie stays SameSite=Strict).
#   docker build -t home-services-backoffice frontend/backoffice
FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM caddy:2-alpine
COPY Caddyfile /etc/caddy/Caddyfile
COPY --from=build /app/dist /srv
EXPOSE 8080
