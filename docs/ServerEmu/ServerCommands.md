# Server Commands

This list was automatically generated on `2025.08.16 18:28:24 UTC` using server version `0.7.0`.

To see an up to date list of all commands, type !commands in the server console or the in-game chat. When invoking a command from in-game your account has to meet the user level requirement for the command.

## Account
Account management commands.

| Command                                         | Description                                       | User Level | Invoker Type  |
| ----------------------------------------------- | ------------------------------------------------- | ---------- | ------------  |
| !account ban [email]                            | Bans the specified account.                       | Moderator  | Any           |
| !account create [email] [playerName] [password] | Creates a new account.                            | Any        | Any           |
| !account download                               | Downloads a JSON copy of the current account.     | Any        | Client        |
| !account info                                   | Shows information for the logged in account.      | Any        | Client        |
| !account password [email] [password]            | Changes password for the specified account.       | Any        | Any           |
| !account playername [email] [playername]        | Changes player name for the specified account.    | Any        | Any           |
| !account unban [email]                          | Unbans the specified account.                     | Moderator  | Any           |
| !account unwhitelist [email]                    | Removes the specified account from the whitelist. | Admin      | Any           |
| !account userlevel [email] [0/1/2]              | Changes user level for the specified account.     | Admin      | Any           |
| !account verify [email] [password]              | Checks if an email/password combination is valid. | Admin      | Any           |
| !account whitelist [email]                      | Whitelists the specified account.                 | Admin      | Any           |

## Achievement
Commands related to the achievement system.

| Command                | Description                                 | User Level | Invoker Type  |
| ---------------------- | ------------------------------------------- | ---------- | ------------  |
| !achievement info [id] | Outputs info for the specified achievement. | Admin      | Any           |

## AOI
Commands for interacting with the invoker player's area of interest (AOI).

| Command             | Description                                          | User Level | Invoker Type  |
| ------------------- | ---------------------------------------------------- | ---------- | ------------  |
| !aoi print          | Prints player AOI information to the server console. | Admin      | Client        |
| !aoi refs           | Prints interest references for the current player.   | Admin      | Client        |
| !aoi update         | Forces AOI proximity update.                         | Admin      | Client        |
| !aoi volume [value] | Changes player AOI volume size.                      | Any        | Client        |

## Boost
Commands for boosting the stats of the invoker player's current avatar.

| Command                 | Description                                         | User Level | Invoker Type  |
| ----------------------- | --------------------------------------------------- | ---------- | ------------  |
| !boost damage [1-10000] | Sets DamagePctBonus for the current avatar.         | Admin      | Client        |
| !boost vsboss [1-10000] | Sets DamagePctBonusVsBosses for the current avatar. | Admin      | Client        |

## Client
Commands for interacting with connected clients.

| Command                   | Description                                                        | User Level | Invoker Type  |
| ------------------------- | ------------------------------------------------------------------ | ---------- | ------------  |
| !client info [sessionId]  | Prints information about the client with the specified session id. | Admin      | Any           |
| !client kick [playerName] | Disconnects the client with the specified player name.             | Moderator  | Any           |

## Debug
Debug commands for development.

| Command                      | Description                                                                                                             | User Level | Invoker Type   |
| ---------------------------- | ----------------------------------------------------------------------------------------------------------------------- | ---------- | -------------  |
| !debug ai                    | No description available.                                                                                               | Admin      | Client         |
| !debug area                  | Shows current area.                                                                                                     | Any        | Client         |
| !debug cell                  | Shows current cell.                                                                                                     | Any        | Client         |
| !debug compactloh            | Requests the garbage collector to compact the large object heap (LOH) during the next full-blocking garbage collection. | Admin      | Any            |
| !debug crashgame             | Crashes the current game instance.                                                                                      | Admin      | Client         |
| !debug crashserver           | Crashes the entire server.                                                                                              | Admin      | ServerConsole  |
| !debug difficulty            | Shows information about the current difficulty level.                                                                   | Any        | Client         |
| !debug forcegc               | Requests the garbage collector to perform a collection.                                                                 | Admin      | Any            |
| !debug getconditionlist      | Gets a list of all conditions tracked by the ConditionPool in the current game.                                         | Moderator  | Client         |
| !debug geteventpoolreport    | Returns a report representing the state of the ScheduledEventPool in the current game.                                  | Moderator  | Client         |
| !debug metagame [on/off]     | No description available.                                                                                               | Admin      | Any            |
| !debug navi2obj [PathFlags]  | Default PathFlags is Walk, can be [None/Fly/Power/Sight].                                                               | Admin      | Client         |
| !debug region                | Shows current region.                                                                                                   | Any        | Client         |
| !debug seed                  | Shows current seed.                                                                                                     | Any        | Client         |
| !debug setmarker [MarkerRef] | No description available.                                                                                               | Admin      | Any            |
| !debug spawn [on/off]        | No description available.                                                                                               | Admin      | Any            |
| !debug test                  | Runs test code.                                                                                                         | Admin      | Any            |

## Entity
Entity management commands.

| Command                                   | Description                                                                                                           | User Level | Invoker Type  |
| ----------------------------------------- | --------------------------------------------------------------------------------------------------------------------- | ---------- | ------------  |
| !entity create [pattern] [count]          | Create entity near the avatar based on pattern (ignore the case) and count (default 1).                               | Admin      | Client        |
| !entity dummy [pattern]                   | Replace the training room target dummy with the specified entity.                                                     | Admin      | Client        |
| !entity info [EntityId]                   | Displays information about the specified entity.                                                                      | Any        | Client        |
| !entity isblocked [EntityId1] [EntityId2] | No description available.                                                                                             | Any        | Client        |
| !entity marker [MarkerId]                 | Displays information about the specified marker.                                                                      | Any        | Client        |
| !entity near [radius]                     | Displays all entities in a radius (default is 100).                                                                   | Any        | Client        |
| !entity selector [pattern]                | Create row entities near the avatar based on selector pattern (ignore the case).                                      | Admin      | Client        |
| !entity tp [pattern]                      | Teleports to the first entity present in the region which prototype name contains the string given (ignore the case). | Admin      | Client        |

## Instance
Commands for managing region instances.

| Command         | Description                                         | User Level | Invoker Type  |
| --------------- | --------------------------------------------------- | ---------- | ------------  |
| !instance list  | Lists instances in the player's WorldView.          | Any        | Client        |
| !instance reset | Resets private instances in the player's WorldView. | Admin      | Client        |

## Item
Commands for managing items.

| Command                      | Description                                                                | User Level | Invoker Type  |
| ---------------------------- | -------------------------------------------------------------------------- | ---------- | ------------  |
| !item cleardeliverybox       | Destroys all items contained in the delivery box inventory.                | Any        | Client        |
| !item creditchest            | Converts 500k credits to a sellable chest item.                            | Any        | Client        |
| !item destroyindestructible  | Destroys indestructible items contained in the player's general inventory. | Any        | Client        |
| !item drop [pattern] [count] | Creates and drops the specified item from the current avatar.              | Admin      | Client        |
| !item give [pattern] [count] | Creates and gives the specified item to the current player.                | Admin      | Client        |
| !item roll [pattern]         | Rolls the specified loot table.                                            | Admin      | Client        |
| !item rollall                | Rolls all loot tables.                                                     | Admin      | Client        |

## Leaderboards
Commands related to the leaderboard system

| Command                                   | Description                                           | User Level | Invoker Type  |
| ----------------------------------------- | ----------------------------------------------------- | ---------- | ------------  |
| !leaderboards all                         | Shows all leaderboards.                               | Admin      | Any           |
| !leaderboards enabled                     | Shows enabled leaderboards.                           | Admin      | Any           |
| !leaderboards instance [instanceId]       | Shows details for the specified leaderboard instance. | Admin      | Any           |
| !leaderboards leaderboard [prototypeGuid] | Shows details for the specified leaderboard.          | Admin      | Any           |
| !leaderboards now                         | Shows all active instances.                           | Admin      | Any           |
| !leaderboards reloadschedule              | Reloads leaderboard schedule from JSON.               | Admin      | Any           |

## Level
Level management commands.

| Command                 | Description                                | User Level | Invoker Type  |
| ----------------------- | ------------------------------------------ | ---------- | ------------  |
| !level awardxp [amount] | Awards the specified amount of experience. | Admin      | Client        |
| !level max              | Maxes out the current avatar's experience. | Admin      | Client        |
| !level maxinfinity      | Maxes out Infinity experience.             | Admin      | Client        |
| !level maxomega         | Maxes out Omega experience.                | Admin      | Client        |
| !level reset            | Resets the current avatar to level 1.      | Admin      | Client        |
| !level resetinfinity    | Removes all Infinity progression.          | Admin      | Client        |
| !level resetomega       | Removes all Omega progression.             | Admin      | Client        |
| !level up               | Levels up the current avatar.              | Admin      | Client        |

## Lookup
Commands for searching data refs.

| Command                     | Description                                         | User Level | Invoker Type  |
| --------------------------- | --------------------------------------------------- | ---------- | ------------  |
| !lookup asset [pattern]     | Searches assets.                                    | Admin      | Any           |
| !lookup assettype [pattern] | Searches asset types.                               | Admin      | Any           |
| !lookup blueprint [pattern] | Searches blueprints.                                | Admin      | Any           |
| !lookup costume [pattern]   | Searches prototypes that use the costume blueprint. | Admin      | Any           |
| !lookup item [pattern]      | Searches prototypes that use the item blueprint.    | Admin      | Any           |
| !lookup power [pattern]     | Searches prototypes that use the power blueprint.   | Admin      | Any           |
| !lookup region [pattern]    | Searches prototypes that use the region blueprint.  | Admin      | Any           |

## MetaGame
Commands related to the MetaGame system.

| Command                     | Description                              | User Level | Invoker Type  |
| --------------------------- | ---------------------------------------- | ---------- | ------------  |
| !metagame event [next/stop] | Changes current event. Defaults to stop. | Admin      | Client        |

## Mission
Commands related to the mission system.

| Command                     | Description                                            | User Level | Invoker Type  |
| --------------------------- | ------------------------------------------------------ | ---------- | ------------  |
| !mission complete [pattern] | Complete the given mission.                            | Admin      | Client        |
| !mission completestory      | Set all main story missions to completed.              | Admin      | Client        |
| !mission debug [on/off]     | No description available.                              | Admin      | Any           |
| !mission info [pattern]     | Display information about the given mission.           | Any        | Client        |
| !mission region             | List all the mission prototypes in the current region. | Any        | Client        |
| !mission reset [pattern]    | Restart the given mission.                             | Admin      | Client        |
| !mission resetstory         | Reset all main story missions.                         | Any        | Client        |

## Player
Commands for managing player data for the invoker's account.

| Command                       | Description                                                                               | User Level | Invoker Type  |
| ----------------------------- | ----------------------------------------------------------------------------------------- | ---------- | ------------  |
| !player clearconditions       | Clears persistent conditions.                                                             | Any        | Client        |
| !player costume [name/reset]  | Changes costume for the current avatar.                                                   | Admin      | Client        |
| !player die                   | Kills the current avatar.                                                                 | Any        | Client        |
| !player disablevu             | Forces the fallback costume for the current hero, reverting visual updates in some cases. | Any        | Client        |
| !player givecurrency [amount] | Gives all currencies.                                                                     | Admin      | Client        |
| !player wipe [playerName]     | Wipes all progress associated with the current account.                                   | Any        | Client        |

## Power
Commands related to the power system.

| Command                   | Description                                                        | User Level | Invoker Type  |
| ------------------------- | ------------------------------------------------------------------ | ---------- | ------------  |
| !power cooldownreset      | Resets all cooldowns and charges.                                  | Admin      | Client        |
| !power forgetstolenpowers | Locks all unlocked stolen powers.                                  | Admin      | Client        |
| !power print              | Prints the power collection for the current avatar to the console. | Admin      | Client        |
| !power stealavatarpowers  | Unlocks avatar stolen powers.                                      | Admin      | Client        |
| !power stealpowers        | Unlocks all stolen powers.                                         | Admin      | Client        |

## Region
Region management commands.

| Command                 | Description                               | User Level | Invoker Type  |
| ----------------------- | ----------------------------------------- | ---------- | ------------  |
| !region generateallsafe | Generates all safe regions.               | Admin      | Client        |
| !region info            | Prints info for the current region.       | Any        | Client        |
| !region properties      | Prints properties for the current region. | Admin      | Client        |
| !region reload          | Reloads the current region.               | Admin      | Client        |
| !region warp [name]     | Warps the player to another region.       | Admin      | Client        |

## Server
Server management commands.

| Command                  | Description                               | User Level | Invoker Type   |
| ------------------------ | ----------------------------------------- | ---------- | -------------  |
| !server broadcast        | Broadcasts a notification to all players. | Admin      | Any            |
| !server reloadlivetuning | Reloads live tuning settings.             | Admin      | ServerConsole  |
| !server shutdown         | Shuts the server down.                    | Admin      | Any            |
| !server status           | Prints server status.                     | Any        | Any            |

## Store
Commands for interacting with the in-game store.

| Command              | Description                                                     | User Level | Invoker Type  |
| -------------------- | --------------------------------------------------------------- | ---------- | ------------  |
| !store addg [amount] | Adds the specified number of Gs to this account.                | Admin      | Client        |
| !store convertes     | Converts 100 Eternity Splinters to the equivalent amount of Gs. | Any        | Client        |

## Unlock
Commands for unlocking various things.

| Command           | Description            | User Level | Invoker Type  |
| ----------------- | ---------------------- | ---------- | ------------  |
| !unlock chapters  | Unlocks all chapters.  | Admin      | Client        |
| !unlock waypoints | Unlocks all waypoints. | Admin      | Client        |

## Misc

| Command              | Description                                                                                                  | User Level | Invoker Type   |
| -------------------- | ------------------------------------------------------------------------------------------------------------ | ---------- | -------------  |
| !commands            | Lists available commands.                                                                                    | Any        | Any            |
| !dance               | Performs the Dance emote (if available).                                                                     | Any        | Client         |
| !generatecommanddocs | Generates markdown documentation for all registered command groups.                                          | Any        | ServerConsole  |
| !help                | Help needs no help.                                                                                          | Any        | Any            |
| !jail                | Teleports to East Side: Detention Facility (old).                                                            | Admin      | Client         |
| !position            | Shows current position.                                                                                      | Any        | Client         |
| !tower               | Teleports to Avengers Tower (original).                                                                      | Any        | Client         |
| !tp                  | Teleports to position. Usage: tp x:+1000 (relative to current position) tp x100 y500 z10 (absolute position) | Admin      | Client         |

