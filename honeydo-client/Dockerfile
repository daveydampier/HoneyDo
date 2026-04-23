# ── Stage 1: Build ─────────────────────────────────────────────────────────
FROM node:20-alpine AS build

WORKDIR /app

# Install dependencies first (separate layer — only re-runs when package files change)
COPY package*.json ./
RUN npm ci --silent

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
