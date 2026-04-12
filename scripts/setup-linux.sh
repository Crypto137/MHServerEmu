#!/bin/bash

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
BUILD_DIR="$PROJECT_DIR/src/MHServerEmu/bin/x64/Release/net8.0"
ASSETS_DIR="/var/www/html"
GAME_DATA_DIR="$BUILD_DIR/Data/Game"
SERVER_IP="${SERVER_IP:-127.0.0.1}"

show_usage() {
    cat << EOF
Usage: $0 [OPTIONS]

Options:
    --install-deps       Install .NET 8 runtime and Apache
    --build             Build the server from source
    --setup-game-data   Copy game data files (requires MARVEL_HEROES_PATH)
    --setup-apache      Configure and enable Apache
    --all              Run all setup steps
    --help             Show this help message

Environment:
    MARVEL_HEROES_PATH    Path to Marvel Heroes game installation
    SERVER_IP           Server IP address (default: localhost)
    DOTNET_SDK_PATH     Path to .NET SDK (optional)
EOF
}

install_deps() {
    echo "=== Installing dependencies ==="

    if command -v dotnet &>/dev/null; then
        echo ".NET is already installed: $(dotnet --version)"
    else
        echo "Installing .NET 8..."
        wget -q https://packages.microsoft.com/config/$(lsb_release -rs 2>/dev/null || echo "ubuntu")/24.04/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb || true
        sudo dpkg -i /tmp/packages-microsoft-prod.deb || true
        sudo apt update -qq
        sudo apt install -y -qq dotnet-runtime-8.0 || echo "Warning: Could not install dotnet-runtime-8.0, will use rollforward"
    fi

    if command -v apache2 &>/dev/null; then
        echo "Apache is already installed"
    else
        echo "Installing Apache..."
        sudo apt install -y -qq apache2
    fi

    echo "=== Dependencies installed ==="
}

build_server() {
    echo "=== Building server ==="

    cd "$PROJECT_DIR"
    dotnet build MHServerEmu.sln -c Release -p:Platform=x64

    echo "=== Build complete ==="
    echo "Output: $BUILD_DIR"
}

setup_game_data() {
    local game_path="${MARVEL_HEROES_PATH:-}"

    if [[ -z "$game_path" ]]; then
        echo "MARVEL_HEROES_PATH not set. Skipping game data setup."
        echo "Set MARVEL_HEROES_PATH and re-run with --setup-game-data"
        return
    fi

    if [[ ! -d "$game_path" ]]; then
        echo "Error: Game path does not exist: $game_path"
        exit 1
    fi

    echo "=== Setting up game data ==="
    echo "Game path: $game_path"

    local calligraphy="$game_path/Data/Game/Calligraphy.sip"
    local cdata="$game_path/Data/Game/mu_cdata.sip"

    if [[ -f "$calligraphy" ]]; then
        cp "$calligraphy" "$GAME_DATA_DIR/"
        echo "Copied Calligraphy.sip"
    else
        echo "Warning: Calligraphy.sip not found at $calligraphy"
    fi

    if [[ -f "$cdata" ]]; then
        cp "$cdata" "$GAME_DATA_DIR/"
        echo "Copied mu_cdata.sip"
    else
        echo "Warning: mu_cdata.sip not found at $cdata"
    fi

    echo "=== Game data setup complete ==="
}

setup_apache() {
    echo "=== Setting up Apache on port 443 ==="

    sudo a2enmod ssl proxy proxy_http rewrite alias
# Generate new SSL certificates
    echo "Generating new SSL certificates..."
    sudo mkdir -p /etc/apache2/ssl
    sudo openssl req -x509 -newkey rsa:2048 -keyout /etc/apache2/ssl/server.key -out /etc/apache2/ssl/server.crt -days 3650 -nodes -subj "/C=US/ST=State/L=City/O=Organization/CN=$SERVER_IP"

    # Update SiteConfig.xml with server IP
    echo "Updating SiteConfig.xml with server IP: $SERVER_IP"
    sed -i "s|<str name=\"AuthServerAddress\" value=\"[^\"]*\" />|<str name=\"AuthServerAddress\" value=\"$SERVER_IP\" />|" "$PROJECT_DIR/assets/SiteConfig.xml"

    sudo tee /etc/apache2/sites-available/mhserveremu.conf > /dev/null << EOF
<VirtualHost *:443>
    ServerName $SERVER_IP
    DocumentRoot /var/www/html

    SSLEngine on
    SSLCertificateFile /etc/apache2/ssl/server.crt
    SSLCertificateKeyFile /etc/apache2/ssl/server.key

    ProxyRequests Off
    ProxyPreserveHost On

    <Proxy *>
        Require all granted
    </Proxy>

    ProxyPass "/AuthServer/" "http://127.0.0.1:8080/AuthServer/"
    ProxyPassReverse "/AuthServer/" "http://127.0.0.1:8080/AuthServer/"
</VirtualHost>
EOF

    sudo mkdir -p "$ASSETS_DIR"
    sudo cp -r "$PROJECT_DIR/assets/"* "$ASSETS_DIR/"

    sudo a2ensite mhserveremu

    sudo systemctl reload apache2

    echo "=== Apache configured ==="
    echo "SiteConfig.xml available at: http://$SERVER_IP/SiteConfig.xml"
    echo "Auth server proxy: https://$SERVER_IP/AuthServer/"
}

run_all() {
    install_deps
    build_server
    setup_game_data
    setup_apache

    echo ""
    echo "=== Setup complete ==="
    echo "Run the server with:"
    echo "  cd $PROJECT_DIR"
    echo "  ./scripts/run-server.sh"
    echo ""
    echo "Launch the client with:"
    echo "  MarvelHeroesOmega.exe -robocopy -nosteam -siteconfigurl=http://$SERVER_IP/SiteConfig.xml"
    echo ""
    echo "For remote clients, set SERVER_IP to your server IP before running setup."
}

case "${1:-}" in
    --install-deps)
        install_deps
        ;;
    --build)
        build_server
        ;;
    --setup-game-data)
        setup_game_data
        ;;
    --setup-apache)
        setup_apache
        ;;
    --all)
        run_all
        ;;
    --help|-h)
        show_usage
        ;;
    *)
        show_usage
        ;;
esac