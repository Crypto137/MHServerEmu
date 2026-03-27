#!/bin/bash
# Initializes Let's Encrypt certificates for MHServerEmu.
# Run this ONCE before starting docker compose for the first time with Let's Encrypt.
#
# Usage: ./docker/init-letsencrypt.sh yourdomain.com [your@email.com]

set -e

DOMAIN=${1:?Usage: $0 <domain> [email]}
EMAIL=${2:-""}
COMPOSE_FILE="docker-compose.yml"
DATA_PATH="./docker/certbot"

if [ -d "$DATA_PATH/conf/live/$DOMAIN" ]; then
  echo "Certificates already exist for $DOMAIN. Delete $DATA_PATH to re-initialize."
  exit 0
fi

echo "### Creating required directories..."
mkdir -p "$DATA_PATH/conf/live"
mkdir -p "$DATA_PATH/www"

echo "### Downloading recommended TLS parameters..."
curl -s https://raw.githubusercontent.com/certbot/certbot/master/certbot-nginx/certbot_nginx/_internal/tls_configs/options-ssl-nginx.conf > "$DATA_PATH/conf/options-ssl-nginx.conf"
curl -s https://raw.githubusercontent.com/certbot/certbot/master/certbot/certbot/ssl-dhparams.pem > "$DATA_PATH/conf/ssl-dhparams.pem"

echo "### Creating temporary self-signed certificate for $DOMAIN..."
mkdir -p "$DATA_PATH/conf/live/$DOMAIN"
openssl req -x509 -nodes -newkey rsa:4096 -days 1 \
  -keyout "$DATA_PATH/conf/live/$DOMAIN/privkey.pem" \
  -out "$DATA_PATH/conf/live/$DOMAIN/fullchain.pem" \
  -subj "/CN=$DOMAIN" 2>/dev/null

echo "### Starting nginx with temporary certificate..."
docker compose -f "$COMPOSE_FILE" up -d nginx

echo "### Requesting Let's Encrypt certificate for $DOMAIN..."
# Build certbot arguments
CERTBOT_ARGS="certonly --webroot -w /var/www/certbot --force-renewal"
CERTBOT_ARGS="$CERTBOT_ARGS -d $DOMAIN --non-interactive --agree-tos"

if [ -n "$EMAIL" ]; then
  CERTBOT_ARGS="$CERTBOT_ARGS --email $EMAIL"
else
  CERTBOT_ARGS="$CERTBOT_ARGS --register-unsafely-without-email"
fi

docker compose -f "$COMPOSE_FILE" run --rm certbot $CERTBOT_ARGS

echo "### Reloading nginx with real certificate..."
docker compose -f "$COMPOSE_FILE" exec nginx nginx -s reload

echo ""
echo "### Done! Let's Encrypt certificates installed for $DOMAIN"
echo "### Run 'docker compose up -d' to start all services."