# Setup

First you need to get the 1.52.0.1700 client. If you still have Marvel Heroes in your Steam library, you can download it from there.

*Note: it is also possible to download other versions of the client from Steam. See [ClientVersions.md](https://github.com/Crypto137/MHServerEmu/blob/master/docs/ClientVersions.md) for details.*

After getting the client you need to set up a web server to serve SiteConfig.xml and redirect the https authorization request to localhost:8080. You can either set the server up yourself, or use a solution stack such as WAMP.

1. Set up your preferred web server.

2. Enable HTTPS support ([here](https://gist.github.com/danieldogeanu/081dc198a2d727afd6bf01174990ee8d) are instructions for WAMP)

3. Add the following lines to your virtual host configuration in httpd-ssl.conf:

```
RewriteEngine on
RewriteRule ^/marvelheroes(.*) http://%{HTTP_HOST}:8080$1
```

Now you can actually redirect the client to your server and connect.

1. Put [SiteConfig.xml](https://github.com/Crypto137/MHServerEmu/blob/master/assets/SiteConfig.xml) provided in this repository in your web server's www folder.

2. Edit your ClientConfig.xml file located in "Marvel Heroes\Data\Configs" and replace the SiteConfigLocation value with "localhost/SiteConfig.xml".

3. Compile and run MHServerEmu.

4. Launch the game.

5. Log in with any email and password.

If everything works correctly, the server should display client connection information.

*Note: you can launch the game without Steam by running MarvelHeroesOmega.exe with the following arguments: -solidstate -nobitraider -nosteam. There will be a DownloadChunkManifest error, but the game will start anyway.*
