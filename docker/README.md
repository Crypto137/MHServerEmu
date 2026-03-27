# MHServerEmu Docker Setup

Run MHServerEmu on a Linux server using Docker with automatic SSL via Let's Encrypt.

| Container | Purpose | Ports |
|-----------|---------|-------|
| `mhserveremu` | Game server + web API/dashboard | 4306 (TCP), 8080 (HTTP) |
| `nginx` | SSL termination, serves SiteConfig.xml, proxies auth | 80, 443 |
| `certbot` | Auto-renews Let's Encrypt certificates | - |

## Prerequisites

- Linux server with Docker and Docker Compose installed
- A domain name pointing to your server (required for Let's Encrypt)
- Ports 80, 443, and 4306 open in your firewall
- Game data files: `mu_cdata.sip` and `Calligraphy.sip` from a Marvel Heroes game installation

## Quick Start

### 1. Clone and configure

```bash
git clone https://github.com/Crypto137/MHServerEmu.git
cd MHServerEmu
```

Edit `assets/SiteConfig.xml` — set your domain:
```xml
<str name="AuthServerAddress" value="mh.yourdomain.com" />
```

Edit `docker/Config.ini` — set your domain:
```ini
[Frontend]
PublicAddress=mh.yourdomain.com
```

### 2. Add game data files

The server requires pak files from a Marvel Heroes game installation. Copy them into `Data/Game/`:

```bash
mkdir -p Data/Game
cp /path/to/your/game/Data/Game/mu_cdata.sip Data/Game/
cp /path/to/your/game/Data/Game/Calligraphy.sip Data/Game/
```

The server will not start without these files.

### 3. Initialize Let's Encrypt certificates

```bash
chmod +x docker/init-letsencrypt.sh
./docker/init-letsencrypt.sh mh.yourdomain.com your@email.com
```

This will:
- Create a temporary self-signed cert
- Start nginx
- Request a real certificate from Let's Encrypt
- Reload nginx with the valid cert

### 4. Start all services

```bash
docker compose up -d
```

The server is now running. Certificates auto-renew every 12 hours via the certbot container, and nginx reloads every 6 hours to pick up renewed certs.

## Self-Signed Certificates (LAN / No Domain)

If you don't have a domain and just want to run on a local network:

### 1. Generate self-signed certs

```bash
mkdir -p docker/certbot/conf/live
openssl req -x509 -nodes -newkey rsa:4096 -days 365 \
  -keyout docker/certbot/conf/live/privkey.pem \
  -out docker/certbot/conf/live/fullchain.pem \
  -subj "/CN=mhserveremu"
```

### 2. Configure for LAN

Edit `assets/SiteConfig.xml`:
```xml
<str name="AuthServerAddress" value="192.168.1.100" />
```

Edit `docker/Config.ini`:
```ini
[Frontend]
PublicAddress=192.168.1.100
```

### 3. Start without certbot

```bash
docker compose up -d mhserveremu nginx
```

## Connecting Game Clients

Launch the game client with:
```
MarvelHeroesOmega.exe -siteconfigurl=mh.yourdomain.com/SiteConfig.xml
```

For LAN with self-signed certs:
```
MarvelHeroesOmega.exe -siteconfigurl=192.168.1.100/SiteConfig.xml
```

## Configuration

### Custom Config.ini

A Docker-ready `docker/Config.ini` is included and mounted automatically. It has two critical changes from the default:

| Setting | Default | Docker | Why |
|---------|---------|--------|-----|
| `[WebFrontend] Address` | `localhost` | `+` | HttpListener crashes with `localhost` on Linux |
| `[Frontend] PublicAddress` | `127.0.0.1` | `0.0.0.0` | Must be reachable by game clients |

Edit `docker/Config.ini` directly to customize game options, logging, player limits, and other settings. Changes take effect after restarting the container:
```bash
docker compose restart mhserveremu
```

### ConfigOverride.ini

You can also mount a `ConfigOverride.ini` which takes precedence over `Config.ini`:

```yaml
volumes:
  - ./docker/ConfigOverride.ini:/app/ConfigOverride.ini
```

## Data Persistence

Player data (SQLite databases, backups) is stored in the `mhserveremu-data` Docker volume mounted at `/app/Data`.

To back up:
```bash
docker run --rm -v mhserveremu-data:/data -v $(pwd):/backup alpine tar czf /backup/mhserveremu-backup.tar.gz /data
```

To restore:
```bash
docker run --rm -v mhserveremu-data:/data -v $(pwd):/backup alpine tar xzf /backup/mhserveremu-backup.tar.gz -C /
```

## Managing the Server

```bash
# View logs
docker compose logs -f mhserveremu

# Stop all services
docker compose down

# Rebuild after code changes
docker compose up -d --build

# Check certificate status
docker compose run --rm certbot certificates

# Force certificate renewal
docker compose run --rm certbot renew --force-renewal
```

## Ports Reference

| Port | Protocol | Service | Required |
|------|----------|---------|----------|
| 80 | TCP/HTTP | nginx (ACME challenge + redirect) | Yes (for Let's Encrypt) |
| 443 | TCP/HTTPS | nginx (auth proxy + SiteConfig) | Yes |
| 4306 | TCP | MHServerEmu game server | Yes |
| 8080 | TCP/HTTP | MHServerEmu web dashboard | Optional (can be internal only) |

To keep the dashboard internal-only, remove the `8080:8080` port mapping from `docker-compose.yml`. Nginx proxies auth traffic internally.

## Troubleshooting

**Certbot fails with "connection refused"**
- Ensure port 80 is open in your firewall and not blocked by your hosting provider.

**Game client can't connect**
- Verify port 4306 is open.
- Check that `PublicAddress` in Config.ini matches what clients use.
- Check that `AuthServerAddress` in SiteConfig.xml matches your domain/IP.

**"SSL handshake failed" in client**
- For self-signed certs, the game client may reject them. Use Let's Encrypt when possible.

**Server boot-loops with "mu_cdata.sip and/or Calligraphy.sip are missing"**
- Copy the game data pak files to `Data/Game/` on the host. See step 2 of Quick Start.
- Verify the volume mount exists in `docker-compose.yml`: `./Data/Game:/app/Data/Game:ro`

**Server crashes with "System.Net.HttpListenerException: The request is not supported"**
- `[WebFrontend] Address` must be `+` (not `localhost` or `0.0.0.0`) in Docker. The bundled `docker/Config.ini` has this set correctly. If you're using a custom config, change `Address=localhost` to `Address=+`.

**Dashboard not loading**
- Ensure `[WebFrontend] Address` is set to `+`, not `localhost`.