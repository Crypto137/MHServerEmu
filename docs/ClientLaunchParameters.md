# Client Launch Parameters

To launch most versions of the game without any launcher you can use the following combination of launch parameters: "-robocopy -nosteam".

Older versions of the game from before BitRaider was removed require the following parameters: "-nobitraider -nosteam".

## Game-specific parameters

These require further checking for compatibility with various versions of the client.

| Parameter              | Description                                                                                 |
| ---------------------- | ------------------------------------------------------------------------------------------- |
| -defaultregion=        |                                                                                             |
| -defaultcell=          |                                                                                             |
| -startingregion=       |                                                                                             |
| -startingcell=         |                                                                                             |
| -envoverride=          |                                                                                             |
| -collectionname=       |                                                                                             |
| -emailaddress=         |                                                                                             |
| -password=             |                                                                                             |
| -twofactorauthcode=    |                                                                                             |
| -twofactorauthname=    |                                                                                             |
| -twofactortrustmachine |                                                                                             |
| -loginasplayer         |                                                                                             |
| -loginasanother        |                                                                                             |
| -nosave                |                                                                                             |
| -serveroverride=       |                                                                                             |
| -webtoken              |                                                                                             |
| -siteconfigurl=        | Specifies a custom SiteConfig URL                                                           |
| -nosolidstate          | Disables Gazillion launcher integration                                                     |
| -nosteam               | Disables Steam integration                                                                  |
| -robocopy              | Launches the game in standalone mode (mutually exclusive with -solidstate and -steam)       |
| -solidstate            | Launches the game in Gazillion launcher mode (mutually exclusive with -robocopy and -steam) |
| -steam                 | Launches the game in Steam mode (mutually exclusive with -robocopy and -solidstate)         |
| -skipmotioncomics      |                                                                                             |
| -nocatalog             |                                                                                             |
| -norostercatalog       |                                                                                             |
| -displaynametoggle     |                                                                                             |
| -xpnumberstoggle       |                                                                                             |
| -floatingnumberstoggle |                                                                                             |
| -combatlogtoggle       |                                                                                             |
| -nouinotifications     |                                                                                             |
| -noaccount             |                                                                                             |
| -nologout              |                                                                                             |
| -nonews                |                                                                                             |
| -nooptions             |                                                                                             |
| -nostore               |                                                                                             |
| -bypasspsn             |                                                                                             |
| -bypassconsoleauth     |                                                                                             |
| -streaming             |                                                                                             |
| -platform              | Possible values: orbis, dingo                                                               |
| -enableTracing         | Enables verbose logging                                                                     |
| -log                   | Enables verbose logging and shows a log window                                              |

## Generic Unreal Engine parameters

| Parameter        | Description |
| ---------------- | ----------- |
| -nostartupmovies |             |
| -nomovies        |             |
| -nosplash        |             |
