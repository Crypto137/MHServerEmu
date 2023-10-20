# ClientConfig

ClientConfig.xml is a configuration file that comes with the client. By default it includes SiteConfigLocation, SiteConfigLocationBackup, and KioskMode fields.

Many of these options can also be set using [launch parameters](./ClientLaunchParameters.md).

## Fields

The fields and their default values are embedded in the executable.

| Name                      | Type | Default Value                                     | Note                                                                         |
| ------------------------- | ---- | ------------------------------------------------- | ---------------------------------------------------------------------------- |
| SiteConfigLocation        | str  | cdn.marvelheroes.com/marvelheroes/siteconfig.xml  | Beta 1.9 value: http://10.10.11.31/bitraider/marvelplaytest/regionConfig.xml |
| PlatformAuthToken         | str  |                                                   | Named Base64EncodedAuthTicket in beta 1.9                                    |
| ServerOverride            | str  |                                                   |                                                                              |
| DefaultRegionOverride     | str  |                                                   |                                                                              |
| DefaultCellOverride       | str  |                                                   |                                                                              |
| EnvOverride               | str  |                                                   |                                                                              |
| AllowDebugDialogs         | bool | false                                             |                                                                              |
| EmailAddress              | str  |                                                   |                                                                              |
| Password                  | str  |                                                   |                                                                              |
| DbServerName              | str  |                                                   |                                                                              |
| CollectionName            | str  |                                                   |                                                                              |
| RequestPlayerWipe         | bool | false                                             |                                                                              |
| PowersArtMode             | bool | false                                             |                                                                              |
| RegionsArtMode            | bool | false                                             |                                                                              |
| SkipMotionComics          | bool | false                                             |                                                                              |
| KioskMode                 | bool | false                                             |                                                                              |
| PlayerImportFile          | str  |                                                   |                                                                              |
| DisplayNameToggle         | bool | false                                             |                                                                              |
| FloatingNumbersToggle     | bool | false                                             |                                                                              |
| XPNumbersToggle           | bool | false                                             |                                                                              |
| CombatLogToggle           | bool | false                                             |                                                                              |
| LogInAsAnotherPlayer      | bool | false                                             |                                                                              |
| LogInAsPlayer             | str  |                                                   |                                                                              |
| NoPersistenceThisSession  | bool | false                                             |                                                                              |
| ClientDownloader          | int  | 0                                                 |                                                                              |
| MachineId                 | str  |                                                   |                                                                              |
| NoCatalog                 | bool | false                                             |                                                                              |
| StartingRegionOverride    | str  |                                                   |                                                                              |
| StartingCellOverride      | str  |                                                   |                                                                              |
| UINotificationsDisabled   | bool | false                                             |                                                                              |
| TwoFactorAuthCode         | str  |                                                   |                                                                              |
| TwoFactorAuthName         | str  |                                                   |                                                                              |
| TwoFactorTrustMachine     | str  |                                                   |                                                                              |
| AchievementPopupsEnabled  | bool | true                                              |                                                                              |
| NoAccount                 | bool | false                                             |                                                                              |
| NoLogout                  | bool | false                                             |                                                                              |
| NoNews                    | bool | false                                             |                                                                              |
| NoOptions                 | bool | false                                             |                                                                              |
| NoStore                   | bool | false                                             |                                                                              |
| Streaming                 | bool | false                                             |                                                                              |
| SiteConfigLocationBackup  | str  | auth.marvelheroes.com/marvelheroes/siteconfig.xml |                                                                              |
| KoreanFlaggedAccount      | bool | false                                             |                                                                              |
| BypassConsoleAuth         | bool | false                                             |                                                                              |
| PresenceUpdateFrequencyMS | int  | 60000                                             |                                                                              |
| TargetingPS4Data          | bool | false                                             |                                                                              |
| TargetingXboxOneData      | bool | false                                             |                                                                              |
| NoRosterCatalog           | bool | false                                             |                                                                              |
| TitleIdOverride           | str  | CUSA06762_00                                      | PS4 store id, added in 1.53                                                  |
