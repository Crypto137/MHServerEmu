# Initial Setup

First, you need to get the `1.52.0.1700` game client. This is the final released version of Marvel Heroes, so if you still have the game in your Steam library, you can download it from there. If you do not have the game in your Steam library, you may be able to find an archived copy of it on websites like Archive.org.

After you acquire the client, make sure to install .NET Desktop Runtime 6 if you do not have it installed already. You can download it [here](https://dotnet.microsoft.com/en-us/download/dotnet/6.0). Download [this version](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-6.0.27-windows-x64-installer) if you are not sure which one to get.

The following instructions are intended for stable builds of the server. If you feel confident in doing everything yourself, please see [Manual Setup](./ManualSetup.md).

## Setting Up

1. Download the latest [MHServerEmu stable build](https://github.com/Crypto137/MHServerEmu/releases/latest) and extract it.

2. Run the included `SetupSorcererSupreme` tool and point it to your Marvel Heroes game files. You can find them by right clicking on the game in your Steam library and choosing `Manage` -> `Browse local files`.

## Running the Server

1. Run the included `StartServers.bat` file and wait for MHServerEmu to initialize.

2. (Optional) Open http://localhost:8080/AccountManagement/Create and create an account. Note: this link is going to work only when the servers are running.

3. Run `StartClient.bat` and log in with your created account OR run `StartClientAutoLogin.bat` to play with a default account.

4. When you are done, run the `StopServers.bat` file to stop the servers.

## Updating MHServerEmu

In most cases you can update MHServerEmu simply by downloading the [latest nightly build](https://nightly.link/Crypto137/MHServerEmu/workflows/nightly-release-windows-x64/master?preview) and extracting it into the `MHServerEmu` directory, overwriting all files.

Overwriting all files is going to wipe your account data: if you would like to keep it, make sure to back up and restore the `MHServerEmu\Data\Account.db` file. Please keep in mind that the server is still early in development, and major changes are going to require account wipes.

In some cases migrating to a new version may require additional steps. These are going to be posted on our [Discord server](https://discord.gg/hjR8Bj52t3) in the #news channel.
