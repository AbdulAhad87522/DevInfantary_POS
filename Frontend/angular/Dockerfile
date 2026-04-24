# ── Stage 1: Build Angular App ──────────────────────────────────────────
FROM node:20-alpine AS build
WORKDIR /app

# Copy package files
COPY package*.json ./
RUN npm ci --force

# Copy source code
COPY . .

# Build the app
RUN npm run build --prod

# ── Stage 2: Serve with Nginx ──────────────────────────────────────────
FROM nginx:alpine

# Copy built files from build stage
COPY --from=build /app/dist/angular/browser /usr/share/nginx/html

# Custom nginx config for Angular routing
RUN echo 'server { \
    listen 80; \
    server_name localhost; \
    location / { \
        root /usr/share/nginx/html; \
        index index.html index.htm; \
        try_files $uri $uri/ /index.html; \
    } \
    location /api/ { \
        proxy_pass http://backend:8080/; \
        proxy_set_header Host $host; \
        proxy_set_header X-Real-IP $remote_addr; \
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for; \
        proxy_set_header X-Forwarded-Proto $scheme; \
    } \
}' > /etc/nginx/conf.d/default.conf

EXPOSE 80