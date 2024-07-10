# MHServerEmu

MHServerEmu is a server emulator for Marvel Heroes.

The only currently supported version of the game client is **1.52.0.1700** (also known as **2.16a**).

The latest builds are available here: [Stable](https://github.com/Crypto137/MHServerEmu/releases/latest) / [Nightly](https://nightly.link/Crypto137/MHServerEmu/workflows/nightly-release-windows-x64/master?preview). If you are setting the server up for the first time, we recommend you to start with a stable build.

We post development progress reports on our [blog](https://crypto137.github.io/MHServerEmu/). You can find additional information on various topics in the [documentation](./docs/Index.md). If you would like to discuss this project and/or help with its development, feel free to join our [Discord](https://discord.gg/hjR8Bj52t3).

**Please make sure to read our [contribution guidelines](./CONTRIBUTING.md) if you would like to participate in the development of this project.**

## Features

MHServerEmu is in active development. Currently it features:

- Playing as any hero with all of their costumes available in version 1.52.

- Basic combat mechanics: using powers, dealing direct damage to enemies. More complex powers, such as those that rely on debuff effects or summoned allies, are currently not implemented.

- AI system for non-playable characters, such as enemies and team-ups.

- Fully-featured implementation of DRAG (dynamic random area generator) with procedural enemy population spawning.

- Rudimentary loot system: items drop from enemies, and you can pick them up, but they currently do not have stats, and there is no implementation for loot tables.

- Early version of the account system that saves some of your progress, such as your applied costumes and powers slotted in your action bars. Currently most data does not persist when you relog or transition between regions.

- Multiplayer functionality: you can see and interact with other players connected to the same server. Currently there is no instancing, and all regions are shared by all players.

## Setup

See [Initial Setup](./docs/Setup/InitialSetup.md) for information on how to set the server up.
