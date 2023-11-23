# Game Data Overview

Marvel Heroes uses a completely custom game simulation layer developed by Gazillion. It interfaces with Unreal Engine in the client, but it can also function independently from it. This layer handles all game logic, and it also manages all game data.

Static data includes things needed to run the game, such as loot tables, damage calculation formulas, enemy placements, AI presets, and much more. This data is mirrored between the client and the server, and it is managed at runtime by the `GameDatabase` singleton class. The data is stored on disk in so-called pak files that you can read more about [here](./PakFile.md).

A piece of static data is called a *prototype*. A prototype can be as small as a variable in a damage calculation formula to as large as a definition for a region or a playable hero. Each prototype is a bound to a specific class that encapsulates it and postprocesses it after loading if needed.

Prototypes can be separated into two main categories: [Calligraphy](./Calligraphy.md) prototypes stored in `Calligraphy.sip` and [resource](./Resources.md) prototypes stored in `mu_cdata.sip`. Only cells, districts, encounters, props, prop sets, and UIs are resource prototypes, everything else (which is the vast majority of it) is Calligraphy-based.

In addition to prototypes there are also auxiliary Calligraphy data types that are used as prototype field values: curves, assets, and blueprints.

There are various ways to refer to a piece of static data that you can read more about [here](./DataReferences.md).
