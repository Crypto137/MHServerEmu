# Advanced Setup

Below are some of the more advanced setup topics you might be interested in. For first-time setup instructions please see [Initial Setup](./InitialSetup.md).

## Managing Accounts

You can create and manage accounts by using ! commands in the server console or the in-game chat window. Here are some commands to get you started:

- `!account create [email] [playerName] [password]` - creates a new account with the specified email, player name, and password. Email and player name must be unique for each account.

- `!account userlevel [0|1|2]` - sets user level for the specified account to user (0), moderator (1), or admin (2). Higher user levels enable additional in-game command privileges, up to being able to manage other accounts and shut down the server.

- `!account password [email] [newPassword]` - changes password for the specified account.

For a more in-depth list of commands see [Server Commands](./../ServerEmu/ServerCommands.md) or type `!commands`.

## Setting Up Remote Connections

To allow remote clients to connect to your server you need to set up your Apache to function as a reverse proxy server.

1. Remove `RewriteEngine on` and `RewriteRule ^/AuthServer(.*) http://%{HTTP_HOST}:8080$1 [P]` from `httpd-ssl.conf`, this file is under the folder `Apache24\conf\extra`.

2. Add `ProxyPass /AuthServer http://localhost:8080` and `ProxyPassReverse /AuthServer http://localhost:8080` to the `<VirtualHost _default_:443>` section in `httpd-ssl.conf`.

3. Create a copy of your `SiteConfig.xml` in `Apache24\htdocs` and replace the `AuthServerAddress` value in it with your server's externally accessible IP address or domain name. For LAN this is something like `192.168.x.x`, and for hosting on the Internet it is going to be your server's IP address or a domain name pointing to it. Next change the line for `LoadScreenTipsURL` from `localhost` to your external IP you used above on the AuthServerAddress line. This file is under the `Apache24\htdocs` folder.

4. Replace `BindIP` in `Config.ini` under your `MHServerEmu` folder with your local IP address or `0.0.0.0`. This has to be an IP address and not a domain name.

5. Replace `PublicAddress` in `Config.ini` under your `MHServerEmu` folder with your externally accessible address (this can be an IP address or a domain name, like in `SiteConfig.xml`).

After doing the above steps you can connect to the server remotely by either editing `ClientConfig.xml` on the client's machine, or launching the game with the following parameter: `-siteconfigurl=yourserveraddress.com/SiteConfig.xml`. To connect to the server from the same machine it is being hosted on, you need to use the original `SiteConfig.xml` that points to `localhost`.

Please keep in mind that MHServerEmu is experimental software still heavily in development, and hosting a publicly available server on the Internet brings with it potential security risks.

## Running the client on Linux / Steam Deck

Client version `1.52.0.1700` currently has a compatibility issue with Wine/Proton that prevents it from encrypting session tokens, which is required for the authentication process. There is a workaround to bypass this issue:

1. Patch the 64-bit version of `MarvelHeroesOmega.exe` to bypass the error: change `75` to `EB` at `0x019B317E`. You can either do it manually with a hex edtior, or use [MHPatcher](https://github.com/Crypto137/MHPatcher) to apply `Bypass Session Token Encryption Error by FF_Lowthor`.

2. Disable session token verification. If you are playing on your own local server, you can disable it server-wide by enabling the `IgnoreSessionToken` option in `Config.ini`. If you are playing on a public server, you can disable session token verification just for your account by typing the `!account togglelinuxmode` command in-game. Please note that in order to disable session token verification on your account you need to log into the server on a Windows machine.

Keep in mind that disabling session token verification makes your account potentially more vulnerable for session hijacking. For this reason we recommend you to use locally hosted servers when playing on Linux.

## Setting Up In-Game Store and News

The client uses an embedded web browser for some of its UI panels. MHServerEmu provides some options that allow you to make use of this feature.

1. Copy the [store](https://github.com/Crypto137/MHServerEmuWebAssets/tree/master/store) folder provided in the [MHServerEmuWebAssets](https://github.com/Crypto137/MHServerEmuWebAssets) repository to `Apache24\htdocs`.

2. Set `OverrideStoreUrls` in `Config.ini` to `true`.

3. Set `StoreHomePageUrl` in `Config.ini` to `http://localhost/store`.

Restart the server, and you should be able to see an example store home page when you open the in-game store. You can set other pages by editing various URL options in `Config.ini` (e.g. `NewsUrl` to change the content of the news window). For more information on the embedded browser see [Embedded Browser](./../Web/EmbeddedBrowser.md).

Please note that the embedded browser is a 2014 version of the Chromium Embedded Framework (CEF), and using it for general web browsing is a major security risk. You should use it only for displaying the content you trust.

## Setting Up Live Tips

The client can download additional loading screen tips from the server.

1. Copy the [LiveLoadingTips.xml](./../../assets/LiveLoadingTips.xml) file provided in this repository to `Apache24\htdocs`.

2. Set `EnableLiveTips` and `EnableLiveTipsDownloader` in `SiteConfig.xml` to `true`.

3. Set `LoadScreenTipsURL` in `SiteConfig.xml` to `http://localhost/LiveLoadingTips.xml`.

4. (Optional) Adjust `LiveTipsQueryInterval` in `SiteConfig.xml` to your preferred query interval (by default the client queries new tips every 15 minutes).

5. Edit `LiveLoadingTips.xml` to add your own tips.

For tips to actually show up they need to have text matching the client's locale. For a list of supported locale website codes see [here](./../GameData/Locale.md). The client updates tips only when the `Date` attribute of the root node of LiveLoadingTips.xml is different from the previous update.
