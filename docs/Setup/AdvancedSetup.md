# Advanced Setup

Below are some of the more advanced setup topics you might be interested in. For first-time setup instructions please see [Initial Setup](./InitialSetup.md).

## Setting Up LAN Connections

There are additional steps you need to do to allow LAN connections to your server.

Before you do this, you need to know your local IP address:

1. Open the Windows Command Prompt.

2. Enter the `ipconfig` command.

3. Your local IP address should be displayed as `IPv4 Address`.

Now you need to add your local IP address to server configuration files. In the examples below we are going to use `192.168.1.2` as the local IP address.

1. Find the `ConfigOverride.ini` file in the `MHServerEmu` directory and open it with a text editor. If this file does not exist, create a new text file with this name. It should be located next to `Config.ini`.

2. Set the value of the `PublicAddress` setting under the `Frontend` section to your local IP address. Add these lines if they do no exist. It should look like this:

```ini
[Frontend]
PublicAddress=192.168.1.2
```

3. Find the `SiteConfig.xml` file in the `Apache24\htdocs` directory and open it with a text editor.

4. Set the value of the AuthServerAddress setting to your local IP address. It should look like this:

```xml
<str name="AuthServerAddress" value="192.168.1.2" />
```

When you connect to the server from another machine, use the IP address you entered in the configuration files instead of `localhost`. Using the `192.168.1.2` address as an example, your client launch argument should be `-siteconfigurl=192.168.1.2/SiteConfig.xml`.

## Setting Up Remote Connections

Setting the server up for connections outside of your local network requires the same steps as above, but instead of a local IP address you need to use a publicly accessible address or a domain name pointing to that address. You may also need to expose ports `443` for the auth server and `4306` for the frontend server. The latter port is configurable in `Config.ini`.

## Managing Accounts

You can create and manage accounts by using `!` commands in the server console or the in-game chat window. Here are some commands to get you started:

- `!account create [email] [playerName] [password]` - creates a new account with the specified email, player name, and password. Email and player name must be unique for each account.

- `!account userlevel [email] [0|1|2]` - sets user level for the specified account to user (0), moderator (1), or admin (2). Higher user levels enable additional in-game command privileges, up to being able to manage other accounts and shut down the server.

- `!account password [email] [newPassword]` - changes password for the specified account.

For a more in-depth list of commands see [Server Commands](./../ServerEmu/ServerCommands.md) or type `!commands`.

## Enabling Server Garbage Collection

When hosting a server for larger player counts (50+), it is recommended to enable .NET's server garbage collection mode.

The easiest way to enable it for MHServerEmu is to modify the `MHServerEmu.runtimeconfig.json` file located next to `MHServerEmu.exe`:

1. Open `MHServerEmu.runtimeconfig.json` with a text editor.

2. Add the following line to the `configProperties` section: `"System.GC.Server": true`.

Please keep in mind that the server garbage collection mode tends to follow the "any RAM not used is RAM wasted" approach, which can result in very heavy RAM usage, especially when the server stays up for longer periods of time. You may want to limit memory usage by adding the following line to the same `configProperties` section of `MHServerEmu.runtimeconfig.json`: `"System.GC.HeapHardLimitPercent": 80` (this will limit usage to 80% of available RAM).

You can find out more about the differences between the workstation (default) and the server modes in the [.NET documentation](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/workstation-server-gc).

## Setting Up Live Tips

The client can download additional loading screen tips from the server.

1. Copy the [LiveLoadingTips.xml](./../../assets/LiveLoadingTips.xml) file provided in this repository to `Apache24\htdocs`.

2. Set `EnableLiveTips` and `EnableLiveTipsDownloader` in `SiteConfig.xml` to `true`.

3. Set `LoadScreenTipsURL` in `SiteConfig.xml` to `http://localhost/LiveLoadingTips.xml`.

4. (Optional) Adjust `LiveTipsQueryInterval` in `SiteConfig.xml` to your preferred query interval (by default the client queries new tips every 15 minutes).

5. Edit `LiveLoadingTips.xml` to add your own tips.

For tips to actually show up they need to have text matching the client's locale. For a list of supported locale website codes see [here](./../GameData/Locale.md). The client updates tips only when the `Date` attribute of the root node of LiveLoadingTips.xml is different from the previous update.
