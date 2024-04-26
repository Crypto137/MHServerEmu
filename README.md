# MHServerEmu

MHServerEmu is a server emulator for Marvel Heroes.

The only currently supported version of the game client is **1.52.0.1700** (also known as **2.16a**).

The latest builds are available here: [Stable](https://github.com/Crypto137/MHServerEmu/releases/latest) / [Nightly](https://nightly.link/Crypto137/MHServerEmu/workflows/nightly-release-windows-x64/master?preview). If you are setting the server up for the first time, we recommend you to start with a stable build.

We post development progress reports on our [blog](https://crypto137.github.io/MHServerEmu/). You can find additional information on various topics in the [documentation](./docs/Index.md). If you would like to discuss this project and/or help with its development, feel free to join our [Discord](https://discord.gg/hjR8Bj52t3).

**Please make sure to read our [contribution guidelines](./CONTRIBUTING.md) if you would like to participate in the development of this project.**

## Features

MHServerEmu is in early stages of development. Currently it features:

- Client-server network protocol implementation.

- Basic multiplayer functionality: handling multiple clients, remote connections, chat.

- Implementation of the proprietary static game data management system used by the game.

- Fully-featured implementation of DRAG (dynamic random area generator).

- Spawning of entities, including NPCs, enemies, and interactable objects, across the entire game.

- Hero and costume selection.

- Rudimentary implementation of hero powers.

- Account system with simple web API for registering new accounts.

## Setup

See [Initial Setup](./docs/Setup/InitialSetup.md) for information on how to set the server up.
