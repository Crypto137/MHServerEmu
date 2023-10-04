# Server Commands

To see an up to date list of all commands, type !commands in the server console or the in-game chat. When invoking a command from in-game your account has to meet the user level requirement for the command.

## General Commands

| Command                   | Description                                   | User Level |
| ------------------------- | --------------------------------------------- | ---------- |
| !commands                 | Shows a list of all available commands.       | All        |
| !help [command]           | Shows a description of the specified command. | All        |
| !lookup costume [pattern] | Looks up a costume prototype.                 | All        |
| !lookup region [pattern]  | Looks up a region prototype                   | All        |

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

## Server Commands

| Command                                                 | Description                                       | User Level |
| ------------------------------------------------------- | ------------------------------------------------- | ---------- |
| !server status                                          | Shows server status information.                  | All        |
| !server shutdown                                        | Shuts down the server.                            | Admin      |
| !client info [sessionId]                                | Show information about the specified client.      | Admin      |
| !client kick [sessionId]                                | Kicks the specified client from the server.       | Moderator  |
| !client send [sessionId] [messageName] [messageContent] | Sends a protobuf message to the specified client. | Admin      |
| !packet parse                                           | Parses messages from all packet files.            | Admin      |
| !gpak export [entries\|data\|all]                       | Exports data from loaded GPAK files.              | Admin      |

## In-Game Commands

These commands can be invoked only from in-game.

| Command                     | Description                                  | User Level |
| --------------------------- | -------------------------------------------- | ---------- |
| !tower                      | Changes region to Avengers Tower (original). | All        |
| !doop                       | Changes region to Cosmic Doop Sector.        | All        |
| !player avatar [avatar]     | Changes player avatar.                       | All        |
| !player region [region]     | Changes player starting region.              | All        |
| !player costume [costumeId] | Changes player costume.                      | All        |
