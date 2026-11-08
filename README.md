# MHServerEmu

MHServerEmu is a server emulator for Marvel Heroes.

The following versions of the game client are supported:

- **1.52.0.1700** - Full Support

- **1.48.0.1712** (Pre-BUE) - Preliminary Support

- **1.53.0.203** (Test Center) - Preliminary Support

- **1.4.0.26** (PS4) - Preliminary Support

We post development progress reports on our [blog](https://crypto137.github.io/MHServerEmu/). You can find additional information on various topics in the [documentation](./docs/Index.md). If you would like to discuss this project and/or help with its development, feel free to join our [Discord](https://discord.gg/hjR8Bj52t3).

## Download

We provide two kinds of builds: stable and nightly.

|                   | Stable         | Nightly               |
| ----------------- | -------------- | --------------------- |
| **Features**      | Fewer          | More                  |
| **Stability**     | High           | Medium                |
| **Platforms**     | Windows        | Windows / Linux       |
| **Configuration** | Pre-Configured | Just the Server Files |

If you are setting the server up for the first time and/or unsure which one to use, we recommend you to start with a stable build. See [Initial Setup](./docs/Setup/InitialSetup.md) for information on how to set the server up.

You can always upgrade from stable to nightly simply by downloading the latest nightly build and overwriting your stable files.

### Stable

[![Stable Release](https://img.shields.io/github/v/release/Crypto137/MHServerEmu?include_prereleases)](https://github.com/Crypto137/MHServerEmu/releases)

### Nightly

#### Windows
[![Nightly Release (Windows 1.52)](https://github.com/Crypto137/MHServerEmu/actions/workflows/nightly-release-windows-v52.yml/badge.svg)](https://nightly.link/Crypto137/MHServerEmu/workflows/nightly-release-windows-v52/master?preview)

[![Nightly Release (Windows 1.48)](https://github.com/Crypto137/MHServerEmu/actions/workflows/nightly-release-windows-v48.yml/badge.svg)](https://nightly.link/Crypto137/MHServerEmu/workflows/nightly-release-windows-v48/master?preview)

[![Nightly Release (Windows 1.53)](https://github.com/Crypto137/MHServerEmu/actions/workflows/nightly-release-windows-v53.yml/badge.svg)](https://nightly.link/Crypto137/MHServerEmu/workflows/nightly-release-windows-v53/master?preview)

[![Nightly Release (Windows PS4)](https://github.com/Crypto137/MHServerEmu/actions/workflows/nightly-release-windows-ps4.yml/badge.svg)](https://nightly.link/Crypto137/MHServerEmu/workflows/nightly-release-windows-ps4/master?preview)

#### Linux
Please note that Linux nightly builds come bundled with the x64 version of the .NET runtime. If you are running it on a different architecture or already have the .NET runtime installed via your distribution's packet manager, you should probably build the server from source code.

[![Nightly Release (Linux 1.52) (Self-Contained x64)](https://github.com/Crypto137/MHServerEmu/actions/workflows/nightly-release-linux-v52-self-contained-x64.yml/badge.svg)](https://nightly.link/Crypto137/MHServerEmu/workflows/nightly-release-linux-v52-self-contained-x64/master?preview)

[![Nightly Release (Linux 1.48) (Self-Contained x64)](https://github.com/Crypto137/MHServerEmu/actions/workflows/nightly-release-linux-v48-self-contained-x64.yml/badge.svg)](https://nightly.link/Crypto137/MHServerEmu/workflows/nightly-release-linux-v48-self-contained-x64/master?preview)

[![Nightly Release (Linux 1.53) (Self-Contained x64)](https://github.com/Crypto137/MHServerEmu/actions/workflows/nightly-release-linux-v53-self-contained-x64.yml/badge.svg)](https://nightly.link/Crypto137/MHServerEmu/workflows/nightly-release-linux-v53-self-contained-x64/master?preview)

[![Nightly Release (Linux PS4) (Self-Contained x64)](https://github.com/Crypto137/MHServerEmu/actions/workflows/nightly-release-linux-ps4-self-contained-x64.yml/badge.svg)](https://nightly.link/Crypto137/MHServerEmu/workflows/nightly-release-linux-ps4-self-contained-x64/master?preview)

## FAQ

**Is the game fully playable?**

All systems and content that were in the game when it was shut down in 2017 have been restored.

**Where can I download the game client?**

We do not provide download links for the game client for legal reasons. If you have played the game through Steam when it was live, you should be able to download it in your Steam library.

**How to update the server?**

Download the latest stable or nightly build and overwrite your existing files. Nightly builds can be potentially unstable, so it is recommended to back up your account database file located in `MHServerEmu\Data\Account.db` before updating.

**Are you going to support other versions of the game, like the ones from before the Biggest Update Ever (BUE) came out?**

Preliminary support for game versions 1.48 (pre-BUE) and 1.53 (final test center version) is available. Support for these versions is still early, and you are likely to encounter game breaking bugs. We recommend you to use 1.52 if you do not have a preference for a specific version of the game.

Some early work has also been done to support version 1.10 from mid 2013. You can find the code for it in the [MHServerEmu2013](https://github.com/Crypto137/MHServerEmu2013) repository.

**Are you going to add new content to the game (heroes, team-ups, powers, etc.)?**

The scope of this project is restoring the game to its original state. We do not have any plans to create custom content. However, all of our research on the game is completely open-source, and it can be potentially used by others in such endeavors.

**Are you going to make improvements to the game client (e.g. upgrade graphics)?**

No, we do not touch the client side of the game in any way. This project is a recreation of only the server backend needed to run the game.

**I have problems with setting the server up.**

Feel free to join our [Discord](https://discord.gg/hjR8Bj52t3) and ask for help in the `#setup-help` channel.
