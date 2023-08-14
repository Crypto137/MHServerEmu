# Setup

First you need to get the 1.52.0.1700 client. This is the final released version of Marvel Heroes, If you still have the game in your Steam library, you can download it from there.

*Note: it is also possible to download other versions of the client from Steam. See [ClientVersions.md](https://github.com/Crypto137/MHServerEmu/blob/master/docs/ClientVersions.md) for details.*

After getting the client, you need to set up a web server to serve SiteConfig.xml and redirect login requests. For this guide we're going to use Apache.

1. Download Apache 2.4.x OpenSSL 1.1.1 VS17 [here](https://www.apachehaus.com/cgi-bin/download.plx). You can use any other version of Apache you prefer, as long as it has OpenSSL support enabled.

2. Extract the ```Apache24``` folder in the archive to the root directory on any drive (e.g. ```C:\Apache24```).

3. Open `Apache24\conf\httpd.conf` with any text editor and uncomment (by removing #) the following three lines: `LoadModule rewrite_module modules/mod_rewrite.so`, `LoadModule proxy_module modules/mod_proxy.so`, and `LoadModule proxy_http_module modules/mod_proxy_http.so`.

4. Open ```Apache24\conf\extra\httpd-ahssl.conf``` with any text editor, find the `<VirtualHost _default_:443>` section, and add the following two lines to it:
   `RewriteEngine on` and `RewriteRule ^/AuthServer(.*) http://%{HTTP_HOST}:8080$1 [P]`.

5. Put [SiteConfig.xml](https://github.com/Crypto137/MHServerEmu/blob/master/assets/SiteConfig.xml) provided in this repository in ```Apache24\htdocs```.

6. Open ```ClientConfig.xml``` located in ```Marvel Heroes\Data\Configs``` with any text editor and replace the ```SiteConfigLocation``` value with ```localhost/SiteConfig.xml```

7. Compile MHServerEmu with Visual Studio or any other tool you prefer.

8. Copy `Calligraphy.sip` and `mu_cdata.sip` located in `Marvel Heroes\Data\Game` to `MHServerEmu\Assets\GPAK`. Make sure to copy these files to where your compiled emulator is (e.g. `src\MHServerEmu\bin\Debug\net6.0\Assets\GPAK`), and not to one of the source directories.

Now you can actually start everything and get in-game.

1. Start Apache by running ```Apache24\bin\httpd.exe```.

2. Start MHServerEmu.

3. Launch the game.

4. Log in with any email and password.

If everything works correctly, the server should display client connection information.

*Note: you can launch the game without Steam by running MarvelHeroesOmega.exe with the following arguments: -robocopy -nosteam.*

You can customize how the emulator functions, including changing your starter hero, by editing the `Config.ini` file.
