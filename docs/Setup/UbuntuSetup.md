# Linux Setup

This guide covers running MHServerEmu on Linux (tested on Ubuntu 24.04).

## Prerequisites

- .NET 8 Desktop Runtime
- Game client files (version 1.52.0.1700)
- Apache2

## Install Dependencies

```bash
# Install .NET 8
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update
sudo apt install -y dotnet-runtime-8.0

# Install Apache
sudo apt install -y apache2
```

## Build

```bash
git clone https://github.com/Crypto137/MHServerEmu.git
cd MHServerEmu
dotnet build MHServerEmu.sln -c Release -p:Platform=x64
```

## Set Up Game Data

Copy `Calligraphy.sip` and `mu_cdata.sip` from your Marvel Heroes game files:

```bash
cp "Marvel Heroes/Data/Game/Calligraphy.sip" "MHServerEmu/Data/Game/"
cp "Marvel Heroes/Data/Game/mu_cdata.sip" "MHServerEmu/Data/Game/"
```

## Configure Apache

Enable required Apache modules:

```bash
sudo a2enmod ssl proxy proxy_http rewrite alias
```

Create `/etc/apache2/sites-available/mhserveremu.conf`:

```apache
<VirtualHost *:443>
    ServerName localhost
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
```

Generate a self-signed certificate for `localhost` and copy it into Apache:

```bash
sudo mkdir -p /etc/apache2/ssl
sudo openssl req -x509 -newkey rsa:2048 -keyout /etc/apache2/ssl/server.key \
    -out /etc/apache2/ssl/server.crt -days 3650 -nodes \
    -subj "/C=US/ST=State/L=City/O=MHServerEmu/CN=localhost"
```

Copy `SiteConfig.xml` to the Apache web root:

```bash
sudo cp -r MHServerEmu/assets/* /var/www/html/
```

Make sure `assets/SiteConfig.xml` contains these auth settings:

```xml
<str name="AuthServerAddress" value="localhost" />
<str name="AuthServerUrl" value="/AuthServer/Login/IndexPB" />
<str name="AuthServerPort" value="443" />
```

Enable the site and reload Apache:

```bash
sudo a2ensite mhserveremu
sudo systemctl reload apache2
```

## Run the Server

```bash
cd MHServerEmu/src/MHServerEmu/bin/x64/Release/net8.0
DOTNET_ROLL_FORWARD=LatestMajor dotnet MHServerEmu.dll
```

Create an account at http://localhost:8080/Dashboard/ (server must be running first).

## Connect Clients

```bash
MarvelHeroesOmega.exe -robocopy -nosteam -siteconfigurl=http://localhost/SiteConfig.xml
```

For remote clients, replace `localhost` with your server IP address in both the launch parameter and the `AuthServerAddress` value in `SiteConfig.xml`.

**Notes:**

- `-robocopy -nosteam` ensures the game runs in standalone mode without the Gazillion/Steam launcher.
- The client loads `SiteConfig.xml` over HTTP and then sends auth requests over HTTPS to `https://localhost/AuthServer/Login/IndexPB`.
- If the client is remote, use the server IP instead of `localhost` and ensure Apache is reachable on port `443`.


## Run as Service (Optional)

Create `/etc/systemd/system/mhserveremu.service`:

```ini
[Unit]
Description=MHServerEmu
After=network.target

[Service]
Type=simple
WorkingDirectory=/home/steve/code/MHServerEmu/src/MHServerEmu/bin/x64/Release/net8.0
ExecStart=/usr/bin/dotnet MHServerEmu.dll
Environment=DOTNET_ROLL_FORWARD=LatestMajor
Restart=on-failure

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl daemon-reload
sudo systemctl enable mhserveremu
sudo systemctl start mhserveremu
```

## Troubleshooting

**Server won't start:** Check logs. Common issues:
- Missing game data files
- Port 4306 or 8080 in use

**Can't connect:** Make sure Apache is running and Config.ini has correct BindIP.