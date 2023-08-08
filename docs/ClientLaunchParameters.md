# Client Launch Parameters

To launch most versions of the game without any launcher you can use the following combination of launch parameters: "-robocopy -nosteam".

Older versions of the game from before BitRaider was removed require the following parameters: "-nobitraider -nosteam".

## Game-specific parameters

These require further checking for compatibility with various versions of the client.

| Parameter              | Description                                                                                 | Values                                                                            |
| ---------------------- | ------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------- |
| -defaultregion=        |                                                                                             |                                                                                   |
| -defaultcell=          |                                                                                             |                                                                                   |
| -startingregion=       |                                                                                             |                                                                                   |
| -startingcell=         |                                                                                             |                                                                                   |
| -envoverride=          |                                                                                             |                                                                                   |
| -collectionname=       |                                                                                             |                                                                                   |
| -emailaddress=         |                                                                                             |                                                                                   |
| -password=             |                                                                                             |                                                                                   |
| -twofactorauthcode=    |                                                                                             |                                                                                   |
| -twofactorauthname=    |                                                                                             |                                                                                   |
| -twofactortrustmachine |                                                                                             |                                                                                   |
| -loginasplayer         |                                                                                             |                                                                                   |
| -loginasanother        |                                                                                             |                                                                                   |
| -nosave                |                                                                                             |                                                                                   |
| -serveroverride=       |                                                                                             |                                                                                   |
| -webtoken              |                                                                                             |                                                                                   |
| -siteconfigurl=        | Specifies a custom SiteConfig URL                                                           |                                                                                   |
| -nosolidstate          | Disables Gazillion launcher integration                                                     |                                                                                   |
| -nosteam               | Disables Steam integration                                                                  |                                                                                   |
| -robocopy              | Launches the game in standalone mode (mutually exclusive with -solidstate and -steam)       |                                                                                   |
| -solidstate            | Launches the game in Gazillion launcher mode (mutually exclusive with -robocopy and -steam) |                                                                                   |
| -steam                 | Launches the game in Steam mode (mutually exclusive with -robocopy and -solidstate)         |                                                                                   |
| -skipmotioncomics      |                                                                                             |                                                                                   |
| -nocatalog             |                                                                                             |                                                                                   |
| -norostercatalog       |                                                                                             |                                                                                   |
| -displaynametoggle     |                                                                                             |                                                                                   |
| -xpnumberstoggle       |                                                                                             |                                                                                   |
| -floatingnumberstoggle |                                                                                             |                                                                                   |
| -combatlogtoggle       |                                                                                             |                                                                                   |
| -nouinotifications     |                                                                                             |                                                                                   |
| -noaccount             |                                                                                             |                                                                                   |
| -nologout              |                                                                                             |                                                                                   |
| -nonews                |                                                                                             |                                                                                   |
| -nooptions             |                                                                                             |                                                                                   |
| -nostore               |                                                                                             |                                                                                   |
| -bypasspsn             |                                                                                             |                                                                                   |
| -bypassconsoleauth     |                                                                                             |                                                                                   |
| -streaming             |                                                                                             |                                                                                   |
| -platform=             |                                                                                             | orbis, dingo                                                                      |
| -enableTracing         | Enables verbose logging                                                                     |                                                                                   |
| -log                   | Enables verbose logging and shows a log window                                              |                                                                                   |
| -LoggingLevel=         | Specifies the logging level for verbose logging.                                            | NONE, CRITICAL, FATAL, ERROR, WARNING, INFORMATION, VERBOSE, EXTRA_VERBOSE, DEBUG |
| -LoggingChannels=      | Specifies logging channels for verbose logging.                                             | See below for a full list of available channels.                                  |

### Logging Channels

You can specify channels for verbose logging using the following syntax: `-LoggingChannels=-ALL,+GAME`. Available channels:

```
ALL
ERROR
CORE
CORE_NET
CORE_JOBS_TP
GAME
PEER_CONNECTOR
DATASTORE
PROFILE
GAME_NETWORK
PAKFILE_SYSTEM
LOOT_MANAGER
GROUPING_SYSTEM
PROTOBUF_DUMPER
GAME_DATABASE
TRANSITION
AI
INVENTORY
MEMORY
MISSIONS
PATCHER
GENERATION
RESPAWN
SAVELOAD
FRONTEND
COMMUNITY
ACHIEVEMENTS
METRICS_HTTP_UPLOAD
CURRENCY_CONVERSION
MOBILE
UI
LEADERBOARD
```

## Generic Unreal Engine parameters

| Parameter        | Description |
| ---------------- | ----------- |
| -nostartupmovies |             |
| -nomovies        |             |
| -nosplash        |             |
