# MHServerEmu

MHServerEmu is a server emulator for Marvel Heroes.

The only currently supported version of the game client is **1.52.0.1700** (also known as **2.16a**) released on September 7th, 2017.

We post development progress reports on our [blog](https://crypto137.github.io/MHServerEmu/). You can find additional information on various topics in the [documentation](./docs/Index.md). If you would like to discuss this project and/or help with its development, feel free to join our [Discord](https://discord.gg/hjR8Bj52t3).

**Please make sure to read our [contribution guidelines](./CONTRIBUTING.md) if you would like to participate in the development of this project.**

## Download

We provide two kinds of builds: stable and nightly.

|                      | Stable         | Nightly               |
| -------------------- | -------------- | --------------------- |
| **Update Frequency** | Quarterly      | Daily                 |
| **Features**         | Fewer          | More                  |
| **Stability**        | High           | Medium                |
| **Platforms**        | Windows        | Windows / Linux       |
| **Configuration**    | Pre-Configured | Just the Server Files |

If you are setting the server up for the first time and/or unsure which one to use, we recommend you to start with a stable build. See [Initial Setup](./docs/Setup/InitialSetup.md) for information on how to set the server up.

You can always upgrade from stable to nightly simply by downloading the latest nightly build and overwriting your stable files.

### Stable

[![Stable Release](https://img.shields.io/github/v/release/Crypto137/MHServerEmu?include_prereleases)](https://github.com/Crypto137/MHServerEmu/releases)

### Nightly

[![Nightly Release (Windows x64)](https://github.com/Crypto137/MHServerEmu/actions/workflows/nightly-release-windows-x64.yml/badge.svg)](https://nightly.link/Crypto137/MHServerEmu/workflows/nightly-release-windows-x64/master?preview) [![Nightly Release (Linux x64)](https://github.com/Crypto137/MHServerEmu/actions/workflows/nightly-release-linux-x64.yml/badge.svg)](https://nightly.link/Crypto137/MHServerEmu/workflows/nightly-release-linux-x64/master?preview)

## Features

MHServerEmu is feature-complete as a single player experience, and we are actively working on getting the remaining multiplayer features up and running:

- Store Gifting

- Supergroups

- Matchmaking

- PvP

- Trade Window

You can find up to date information on what we are working on in [our roadmap](https://github.com/users/Crypto137/projects/5).

## FAQ

**Where can I download the game?**

We do not provide download links for the game client for legal reasons. If you have played the game through Steam when it was live, you should be able to download it in your Steam library.

**How to update the server?**

Download the latest stable or nightly build and overwrite your existing files. Nightly builds can be potentially unstable, so it is recommended to back up your account database file located in `MHServerEmu\Data\Account.db` before updating.

**Will there be any wipes?**

We plan to force a fresh start when version 1.0 comes out in early 2026. Your data will not be deleted, but it will no longer be compatible with the server. You will be able to continue using your existing data on whatever the last 0.x version is going to be.

**Are you going to support other versions of the game, like the ones from before the Biggest Update Ever (BUE) came out?**

Yes, we do plan to implement support for other versions of the game after 1.52 is fully restored. The final pre-BUE version (1.48) has the highest priority.

**Are you going to add new content to the game (heroes, team-ups, powers, etc.)?**

The scope of this project is restoring the game to its original state. We do not have any plans to create custom content. However, all of our research on the game is completely open-source, and it can be potentially used by others in such endeavors.

**Are you going to make improvements to the game client (e.g. upgrade graphics)?**

No, we do not touch the client side of the game in any way. This project is a recreation of only the server backend needed to run the game.

**I have problems with setting the server up.**

Feel free to join our [Discord](https://discord.gg/hjR8Bj52t3) and ask for help in the `#setup-help` channel.
