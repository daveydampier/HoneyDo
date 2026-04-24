# ── Stage 1: Build ─────────────────────────────────────────────────────────
FROM node:20-alpine AS build

WORKDIR /app

# Install dependencies first (separate layer — only re-runs when package files change)
COPY package*.json ./
# --legacy-peer-deps: openapi-typescript declares peer "typescript@^5.x" but
# the project uses TypeScript 6. The package works fine; the peer dep
# declaration just hasn't been updated upstream yet.
RUN npm ci --silent --legacy-peer-deps

# Copy source and build the production bundle
COPY . .
RUN npm run build


# ── Stage 2: Serve ─────────────────────────────────────────────────────────
FROM nginx:alpine AS final

# Copy the production build output
COPY --from=build /app/dist /usr/share/nginx/html

# Replace the default nginx config with our SPA + proxy config
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
