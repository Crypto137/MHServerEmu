#!/bin/sh
# If no SSL certs exist yet (Let's Encrypt not initialized), generate self-signed ones
# so nginx can start immediately. They'll be replaced when init-letsencrypt.sh runs.

CERT_DIR="/etc/nginx/ssl/live"
CERT_FILE="$CERT_DIR/fullchain.pem"
KEY_FILE="$CERT_DIR/privkey.pem"

if [ ! -f "$CERT_FILE" ] || [ ! -f "$KEY_FILE" ]; then
    echo "No SSL certificates found. Generating temporary self-signed certificate..."
    mkdir -p "$CERT_DIR"
    apk add --no-cache openssl > /dev/null 2>&1
    openssl req -x509 -nodes -newkey rsa:2048 -days 1 \
        -keyout "$KEY_FILE" \
        -out "$CERT_FILE" \
        -subj "/CN=mhserveremu" 2>/dev/null
    echo "Temporary self-signed certificate generated. Replace with Let's Encrypt by running:"
    echo "  ./docker/init-letsencrypt.sh yourdomain.com your@email.com"
fi

# Start periodic reload in background for cert renewal, then run nginx in foreground
(while true; do sleep 6h; nginx -s reload; done) &
exec nginx -g "daemon off;"