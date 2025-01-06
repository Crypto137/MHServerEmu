# Glossary

This page provides definitions for terms used by the game internally.

- **Agent** - a world entity that can perform actions.

- **AkEvent** - a Wwise sound event, Ak stands for Audiokinetic.

- **Archive** - a proprietary data serialization format developed by Gazillion based on the underlying encoding used for protobufs.

- **Area** - an arrangement of cells.

- **Area of Interest (AOI)** - a part of the overall server-side simulation that needs to be replicated to a specific client.

- **Asset** - a value of an asset type.

- **Asset Type** - a enumerated type used by Calligraphy.

- **Avatar** - an agent that can be controlled by players.

- **Badge** - a flag that grants a player elevated privileges of some kind.

- **Behavior** - a system that handles the AI of enemies and NPCs.

- **Billing** - a service that handles interaction with the in-game store.

- **Blackout** - a boundary within the game world within which entities are prohibited from spawning under normal circumstances.

- **Blueprint** - a template for prototypes made in Calligraphy.

- **Calligraphy** - a proprietary static game data framework developed by Gazillion.

- **Cell** - the basic building block of the game world.

- **Character Level** - the level of an entity used for unlocks.

- **Circle** - a collection of players displayed in the social tab (friends, ignored, etc.).

- **Combat Level** - the level of an entity used for combat calculations. In most cases is equal to character level.

- **Community** - a collection of circles.

- **Condition** - a buff or a debuff that can be applied to an entity.

- **Curve** - a collection of numeric values.

- **Date Time** - a number of milliseconds or microseconds elapsed since `Jan 01 1970 00:00:00 GMT+0000`. Also referred to as calendar time.

- **District** - a static arrangement of cells.

- **Dynamic Combat Level (DCL)** - a level scaling system that adjusts combat calculations based on the difference in combat level between the power user and its target.

- **Dynamic Random Area Generator (DRAG)** - a system that handles procedural generation of the game world.

- **Encounter** - a static arrangement of entities to be spawned as part of the overall population.

- **Endurance** - the primary resource used for powers (e.g. spirit).

- **Entity** - a dynamic game object. Each entity contains a collection of properties.

- **Eval** - a system that evaluates formulas defined in static game data.

- **Field Group** - a collection of field values belonging to a specific blueprint.

- **Front End Server (FES)** - a server that acts as an intermediary between external connections and internal services.

- **Game Database** - a proprietary static game data management system developed by Gazillion.

- **Game Instance Server (GIS)** - a server that runs game instances.

- **Game Time** - a number of milliseconds or microseconds elapsed since `Sep 22 2012 09:31:18 GMT+0000`.

- **Grouping Manager** - a service that handles social and matchmaking features.

- **Guild** - a supergroup.

- **Hotspot** - a world entity that defines a boundary that triggers effects when entered by other world entities.

- **Hub** - a region in which combat is prohibited (also known as a town in other Diablo-like games).

- **Data GUID** - a 64-bit value representing a static data file that does not change between versions of the game.

- **Data Id** - a 64-bit hash representing a static data file derived from its file path. If a file is moved in another version of the game, its DataId is going to be different.

- **Interest** - data replicated to the client.

- **Inventory** - a container for entities (avatars are also stored in inventories).

- **Item** - a world entity that represents an item.

- **Keyword** - a tag that can be applied to a power, such as physical, melee, area, etc.

- **Kismet** - a visual scripting system that is part of Unreal Engine 3.

- **Live Tuning** - a system that overrides some of the static data contained in the game database without changing it.

- **Locomotion** - a system that handles the movement of world entities in the game world.

- **Mapped Power** - an override of an existing power in the power library with another power. Used for talents that change powers and Rogue's stolen powers.

- **Marker** - a definition for spawning objects that includes position and rotation.

- **MetaGame** - a system that handles special game modes, such as PvP or X-Defense.

- **Missile** - an agent that represents a projectile.

- **Mission** - a quest.

- **Mixin** - a field group that belongs to one of the fields contained in a prototype rather than the prototype itself.

- **Mux** - a system used by the frontend server for routing messages to appropriate services.

- **Navi** - a system that defines what sections of the game world are navigable.

- **Pak** - a proprietary file storage format developed by Gazillion. Also known as GPAK.

- **Persistence** - saving of the game state between sessions.

- **Player** - an entity representing a player's account.

- **Player Manager** - a service that manages connected players and transfers them between game instance servers as needed.

- **Population** - a system that handles the spawning of world entities.

- **Power** - an ability that can be used by an entity.

- **Private Instance (PI)** - a region that only the player and their party members can visit.

- **Prop** - a world entity that represents a background object in the game world.

- **Property** - an attribute of an entity, such as its level, health, etc.

- **Property Collection** - a collection of properties that can be aggregated with another collection.

- **Protobuf** - [Protocol Buffers](https://protobuf.dev/), a data serialization technology developed by Google.

- **Prototype** - an object representing a piece of static game data.

- **Public Combat Zone (PCZ)** - a region shared by many players.

- **Region** - a collection of areas.

- **Replication** - a process that handles synchronization of the game state between a game instance server and all clients connected ot it.

- **RHStruct** - a prototype embedded as a field of another prototype. Stands for *right-hand struct*.

- **Super Cell** - a block of cells (such as 2x2) representing a single place.

- **Transition** - a world entity that connects two places of the game world.

- **Varint** - a variable-width unsigned integer that can take 1-10 bytes. This is the primary data type used for encoding in protobufs. See [here](https://protobuf.dev/programming-guides/encoding/) for more information.

- **World Entity** - an entity that can physically exist in a game world.

- **Wwise** - Wave Works Interactive Sound Engine, audio middleware developed by Audiokinetic.

- **ZigZag** - a way of encoding signed values in unsigned integers used in protobufs. See [here](https://protobuf.dev/programming-guides/encoding/) for more information.
