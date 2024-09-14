# MHServerEmu

MHServerEmu is a server emulator for Marvel Heroes.

The only currently supported version of the game client is **1.52.0.1700** (also known as **2.16a**).

The latest builds are available here: [Stable](https://github.com/Crypto137/MHServerEmu/releases/latest) / [Nightly](https://nightly.link/Crypto137/MHServerEmu/workflows/nightly-release-windows-x64/master?preview). If you are setting the server up for the first time, we recommend you to start with a stable build. See [Initial Setup](./docs/Setup/InitialSetup.md) for information on how to set the server up.

We post development progress reports on our [blog](https://crypto137.github.io/MHServerEmu/). You can find additional information on various topics in the [documentation](./docs/Index.md). If you would like to discuss this project and/or help with its development, feel free to join our [Discord](https://discord.gg/hjR8Bj52t3).

**Please make sure to read our [contribution guidelines](./CONTRIBUTING.md) if you would like to participate in the development of this project.**

## Features

MHServerEmu is in active development. Currently it features:

- Playing as any hero available in version 1.52.

- Basic combat mechanics: using powers, dealing direct damage to enemies. More complex powers, such as those that rely on debuff effects or summoned allies, are currently not implemented.

- Leveling from 1 to 60.

- Summoning team-ups and vanity pets.

- AI system for non-playable characters, such as enemies and team-ups.

- Fully-featured implementation of DRAG (dynamic random area generator) with procedural enemy population spawning.

- Implementation of the loot system that uses the original loot tables for picking quality and base types, as well as rolling random affixes.

- SQLite-based persistence layer for saving accounts, player data, avatars, items, and more. An optional JSON mode for offline single-player is also available.

- Multiplayer functionality: you can see and interact with other players connected to the same server in hubs and public combat zones. Parties and coop in private instances are currently not implemented.
