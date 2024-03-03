# Client Launch Parameters

To launch most versions of the game without any launcher you can use the following combination of launch parameters: "-robocopy -nosteam".

Older versions of the game from before BitRaider was removed require the following parameters: "-nobitraider -nosteam".

Some of these parameters can be set automatically by using [Bifrost](https://github.com/Crypto137/Bifrost).

## Generic Unreal Engine parameters

| Parameter        | Description                              |
| ---------------- | ---------------------------------------- |
| -nostartupmovies | Disables logo movies on startup.         |
| -nomovies        | Disables all movies.                     |
| -nosplash        | Disables splash image on initialization. |
| -nosound         | Disables sound.                          |
| -ResX=           | Forces horizontal resolution.            |
| -ResY=           | Forces vertical resolution.              |
| -opengl          | Forces OpenGL API (buggy).               |

## Game-specific parameters

These require further checking for compatibility with various versions of the client.

### ClientApp::Initialize

| Parameter               | Description                                                                                                           | Values                                                                            |
| ----------------------- | --------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------- |
| -asserts                |                                                                                                                       |                                                                                   |
| -verifies               |                                                                                                                       |                                                                                   |
| -uploadVerifies         | Calls `DebugSetWhetherToUploadVerifies`.                                                                              |                                                                                   |
| -enablecoredumps        | Calls `DebugEnableCoreDumps`.                                                                                         |                                                                                   |
| -disableminidumps       | Calls `DebugSetMinidumpCreationPolicy`.                                                                               |                                                                                   |
| -forceLocalStackWalking | Calls `DebugSetForcedLocalStackWalkPolicy`.                                                                           |                                                                                   |
| -enableTracing          | Enables verbose logging.                                                                                              |                                                                                   |
| -log                    | Enables verbose logging and shows a log window.                                                                       |                                                                                   |
| -LoggingLevel=          | Specifies the logging level for verbose logging.                                                                      | NONE, CRITICAL, FATAL, ERROR, WARNING, INFORMATION, VERBOSE, EXTRA_VERBOSE, DEBUG |
| -LoggingChannels=       | Specifies logging channels for verbose logging.                                                                       | See below for a full list of available channels.                                  |
| -suppressall            | Disables logging.                                                                                                     |                                                                                   |
| -maxload                | Forces loading of all prototypes during game database initialization.                                                 |                                                                                   |
| -nocalligraphypak       | Disables Calligraphy pak file (`Calligraphy.sip`) loading and forces the client to load all data from `mu_cdata.sip`. |                                                                                   |
| -novalidate             |                                                                                                                       |                                                                                   |
| -nohardwareinfo         |                                                                                                                       |                                                                                   |

### ClientAppSettingsPostCoreInit::Initialize

| Parameter              | Description                                                                                  | Values       |
| ---------------------- | -------------------------------------------------------------------------------------------- | ------------ |
| -defaultregion=        |                                                                                              |              |
| -defaultcell=          |                                                                                              |              |
| -startingregion=       |                                                                                              |              |
| -startingcell=         |                                                                                              |              |
| -envoverride=          |                                                                                              |              |
| -collectionname=       |                                                                                              |              |
| -emailaddress=         | Automatically logs in on client startup using the specified email address.                   |              |
| -password=             | Specifies the password for logging in automatically with -emailaddress.                      |              |
| -twofactorauthcode=    |                                                                                              |              |
| -twofactorauthname=    |                                                                                              |              |
| -twofactortrustmachine |                                                                                              |              |
| -loginasplayer         |                                                                                              |              |
| -loginasanother        |                                                                                              |              |
| -nosave                |                                                                                              |              |
| -serveroverride=       |                                                                                              |              |
| -webtoken              |                                                                                              |              |
| -siteconfigurl=        | Specifies a custom SiteConfig URL.                                                           |              |
| -nosolidstate          | Disables Gazillion launcher integration.                                                     |              |
| -nosteam               | Disables Steam integration.                                                                  |              |
| -robocopy              | Launches the game in standalone mode (mutually exclusive with -solidstate and -steam).       |              |
| -solidstate            | Launches the game in Gazillion launcher mode (mutually exclusive with -robocopy and -steam). |              |
| -steam                 | Launches the game in Steam mode (mutually exclusive with -robocopy and -solidstate).         |              |
| -skipmotioncomics      |                                                                                              |              |
| -nocatalog             | Prevents the store catalog from loading.                                                     |              |
| -norostercatalog       |                                                                                              |              |
| -displaynametoggle     |                                                                                              |              |
| -xpnumberstoggle       |                                                                                              |              |
| -floatingnumberstoggle |                                                                                              |              |
| -combatlogtoggle       |                                                                                              |              |
| -nouinotifications     |                                                                                              |              |
| -noaccount             | Disables account-related buttons on the login screen.                                        |              |
| -nologout              | Disables the logout button.                                                                  |              |
| -nonews                | Disables community news.                                                                     |              |
| -nooptions             | Disables the options buttons.                                                                |              |
| -nostore               | Disables the store buttons.                                                                  |              |
| -bypasspsn             |                                                                                              |              |
| -bypassconsoleauth     |                                                                                              |              |
| -streaming             |                                                                                              |              |
| -platform=             |                                                                                              | orbis, dingo |

### SiteConfig::ParseCommandLine

| Parameter    | Description | Values |
| ------------ | ----------- | ------ |
| -authticket= |             |        |
| -authserver= |             |        |
| -authurl=    |             |        |

### ClientGame::Initialize

| Parameter   | Description | Values |
| ----------- | ----------- | ------ |
| -nooverwolf |             |        |

### Atlas::Initialize

| Parameter     | Description | Values |
| ------------- | ----------- | ------ |
| -nomapmarkers |             |        |

### AudioManager::Initialize

| Parameter      | Description | Values |
| -------------- | ----------- | ------ |
| -nomusic       |             |        |
| -foleyineditor |             |        |

### UnrealAudioManager::Initialize

| Parameter          | Description | Values |
| ------------------ | ----------- | ------ |
| -audioglobalfocus  |             |        |
| -audiofilepackages |             |        |

### UnrealGameAdapter::Initialize

| Parameter        | Description | Values |
| ---------------- | ----------- | ------ |
| -dependencygraph |             |        |

### UnrealGameAdapter::SendSteamUserInfo

| Parameter               | Description | Values |
| ----------------------- | ----------- | ------ |
| -steamachievementupdate |             |        |

## Logging Channels

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
