# Server Commands

To see an up to date list of all commands, type !commands in the server console or the in-game chat. When invoking a command from in-game your account has to meet the user level requirement for the command.

**NOTE: This list of commands is very outdated. Use `!commands` to see an up to date list for your version of the server.**

## General Commands

| Command         | Description                                   | User Level |
| --------------- | --------------------------------------------- | ---------- |
| !commands       | Shows a list of all available commands.       | All        |
| !help [command] | Shows a description of the specified command. | All        |

## Account Commands

| Command                                         | Description                                                                                                   | User Level |
| ----------------------------------------------- | ------------------------------------------------------------------------------------------------------------- | ---------- |
| !account create [email] [playerName] [password] | Creates a new account using the specified information. Email and player name must be unique for each account. | All        |
| !account playername [email] [playername]        | Changes player name for the specified account. Regular users can change name only for their own account.      | All        |
| !account password [email] [newPassword]         | Changes password for the specified account. Regular users can change password only for their own account.     | All        |
| !account userlevel [0\|1\|2]                    | Sets user level for the specified account to user (0), moderator (1), or admin (2).                           | Admin      |
| !account verify [email] [password]              | Checks if the provided email-password combination is valid.                                                   | Admin      |
| !account ban [email]                            | Bans the specified account.                                                                                   | Moderator  |
| !account unban [email]                          | Unbans the specified account.                                                                                 | Moderator  |
| !account info                                   | Shows account information for the logged in client (in-game only).                                            | All        |

## Achievement Commands

| Command                  | Description                                    | User Level |
| ------------------------ | ---------------------------------------------- | ---------- |
| !achievement unlock [id] | Unlocks the specified achievement.             | All        |
| !achievement info [id]   | Shows details about the specified achievement. | All        |

## Client Commands

| Command                                                 | Description                                      | User Level |
| ------------------------------------------------------- | ------------------------------------------------ | ---------- |
| !client info [sessionId]                                | Shows information about the specified client.    | Admin      |
| !client kick [playerName]                               | Kicks the specified player from the game.        | Moderator  |
| !client send [sessionId] [messageName] [messageContent] | Sends a network message to the specified client. | Admin      |

## Debug Commands

| Command                                  | Description                                                               | User Level |
| ---------------------------------------- | ------------------------------------------------------------------------- | ---------- |
| !debug test                              | Working as intended.                                                      | Admin      |
| !debug cell                              | Shows current cell.                                                       | All        |
| !debug seed                              | Shows current seed.                                                       | All        |
| !debug area                              | Shows current area.                                                       | All        |
| !debug region                            | Shows current region.                                                     | All        |
| !debug isblocked [entityId1] [entityId2] | Checks if an entity is blocked by bounds of another entity.               | All        |
| !debug near [radius]                     | Shows entities and markers within specified radius. Default value is 100. | All        |
| !debug marker [markerId]                 | Displays information about the specified marker.                          | All        |
| !debug entity [entityId]                 | Displays information about the specified entity.                          | All        |

## Lookup Commands

| Command                     | Description                              | User Level |
| --------------------------- | ---------------------------------------- | ---------- |
| !lookup costume [pattern]   | Searches for costume prototypes by name. | All        |
| !lookup region [pattern]    | Searches for region prototypes by name.  | All        |
| !lookup blueprint [pattern] | Searches for blueprints by name.         | All        |
| !lookup assettype [pattern] | Searches for asset types by name.        | All        |
| !lookup asset [pattern]     | Searches for assets by name.             | All        |

## Misc Commands

| Command   | Description                                            | User Level |
| --------- | ------------------------------------------------------ | ---------- |
| !tower    | Changes region to Avengers Tower (original).           | All        |
| !jail     | Changes region to East Side: Detention Facility (old). | All        |
| !position | Shows current position.                                | All        |
| !dance    | Performs the dance emote.                              | All        |
| !tp       | Teleports the player within the current region.        | All        |

## Packet Commands

| Command       | Description                            | User Level |
| ------------- | -------------------------------------- | ---------- |
| !packet parse | Parses messages from all packet files. | Admin      |

## Player Commands

| Command                 | Description                                                                                       | User Level |
| ----------------------- | ------------------------------------------------------------------------------------------------- | ---------- |
| !player avatar [avatar] | Changes player avatar                                                                             | All        |
| !player aoi [value]     | Changes player AOI volume size. Range: [1600..5000].                                              | All        |
| !player aoi reset       | Resets player AOI volume size to 3200.                                                            | All        |
| !player costume [name]  | Changes costume for the current avatar.                                                           | All        |
| !player costume reset   | Resets the applied costume for the current avatar.                                                | All        |
| !player costume default | Applies the default costume to the current avatar. In most cases this reverts any visual updates. | All        |
| !player omegapoints     | Maxes out Omega points. This requires Infinity to be disabled.                                    | All        |
| !player infinitypoints  | Maxes out Infinity points. This requires Infinity to be enabled.                                  | All        |
| !player fixmana         | Fixes missing primary resource visuals in the UI.                                                 | All        |

## Region Commands

| Command                    | Description                                                                                                                                                                          | User Level |
| -------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ---------- |
| !region warp [name]        | Moves the player to the specified region.                                                                                                                                            | All        |
| !region warp [name] unsafe | Moves the player to the specified region, ignoring warnings. NOTE: This will cause the client to get stuck in an infinite loading screen if it is missing the assets for the region! | Admin      |
| !region reload             | Reloads the current region.                                                                                                                                                          | All        |

## Server Commands

| Command          | Description                      | User Level |
| ---------------- | -------------------------------- | ---------- |
| !server status   | Shows server status information. | All        |
| !server shutdown | Shuts down the server.           | Admin      |
