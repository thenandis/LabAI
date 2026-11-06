# =============================================================================
#  Multi-stage build for the Angular UI
#  Stage 1: Node builds the optimised bundle (discarded afterwards)
#  Stage 2: a tiny non-root nginx image serves the static files (~25 MB)
# =============================================================================
FROM node:22-alpine AS build
WORKDIR /app

# Install dependencies first, so this layer is cached until package*.json changes.
COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund

COPY . .
RUN npx ng build --configuration production

# -----------------------------------------------------------------------------
FROM nginxinc/nginx-unprivileged:1.27-alpine AS final
COPY nginx/default.conf /etc/nginx/conf.d/default.conf
COPY nginx/security-headers.conf /etc/nginx/snippets/security-headers.conf
COPY --from=build /app/dist/angular-ui/browser /usr/share/nginx/html
EXPOSE 8080
