# Manual Setup

This page contains instructions for the manual setup of the server. This is intended for more advanced users. If you are trying to set the server up for the first time, please see [Initial Setup](./InitialSetup.md).

## Setting Up the Server

You need to set up a web server to serve SiteConfig.xml and redirect login requests. For this guide we are going to use Apache.

1. Download Apache Win64 [here](https://www.apachelounge.com/download/). You can use any other version of Apache you prefer, as long as it has OpenSSL support enabled.

2. Extract the ```Apache24``` folder in the archive to ```C:\Apache24```. If you would like to run it from another directory, make sure to edit the `SRVROOT` variable value in `Apache24\conf\httpd.conf`.

3. Open `Apache24\conf\httpd.conf` with any text editor and uncomment (by removing the # symbol) the following six lines: `LoadModule rewrite_module modules/mod_rewrite.so`, `LoadModule proxy_module modules/mod_proxy.so`,  `LoadModule proxy_http_module modules/mod_proxy_http.so`,  `LoadModule ssl_module modules/mod_ssl.so`, `LoadModule socache_shmcb_module modules/mod_socache_shmcb.so`, and `Include conf/extra/httpd-ssl.conf`. Make sure you remove only the # symbol and not the entire line!

4. Open ```Apache24\conf\extra\httpd-ssl.conf``` with any text editor, find the `<VirtualHost _default_:443>` section, and add the following two lines to it:
   `RewriteEngine on` and `RewriteRule ^/AuthServer(.*) http://localhost:8080$1 [P]`.

5. Put the [server.crt](./../../assets/ssl/server.crt) and [server.key](./../../assets/ssl/server.key) files provided in this repository in `Apache24\conf`. Alternatively, you can generate your own SSL certificate.

6. Put the [SiteConfig.xml](./../../assets/SiteConfig.xml) file provided in this repository in ```Apache24\htdocs```.

7. Download the latest MHServerEmu nightly build [here](https://nightly.link/Crypto137/MHServerEmu/workflows/nightly-release-windows-x64/master?preview) and extract it wherever you like. Alternatively, you can build the source code yourself with Visual Studio or any other tool you prefer.

8. Copy `Calligraphy.sip` and `mu_cdata.sip` located in `Marvel Heroes\Data\Game` to `MHServerEmu\Data\Game`.

## Running the Server

Now you can actually start everything and get in-game.

1. Start Apache by running ```Apache24\bin\httpd.exe```.

2. Start MHServerEmu and wait for it to load.

3. Open the following link in your web browser and create your account: [http://localhost:8080/AccountManagement/Create](http://localhost:8080/AccountManagement/Create). This link is going to work only when MHServerEmu is fully up and running.

4. Launch the game with the following argument: `-siteconfigurl=localhost/SiteConfig.xml`.

5. Log in with the email / password combination you used for account creation.

If everything works correctly, the server should display client connection information.

*Note: you can launch the game without Steam by running MarvelHeroesOmega.exe with the following arguments: -robocopy -nosteam.*

You can customize how the emulator functions by editing the `Config.ini` file. See [Advanced Setup](./AdvancedSetup.md) for more advanced setup topics.
