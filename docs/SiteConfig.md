# SiteConfig

SiteConfig.xml is a configuration file that the client requests from a CDN on startup that includes information required for logging in, such as AuthServerAddress. SiteConfig.xml follows the same syntax as ClientConfig.xml that comes with the client.

Some versions of the game refuse to start without a SiteConfig. In some of those cases you can work around this by putting a defaultsiteconfig.xml file in the same directory as ClientConfig.xml.

## Fields

These fields were extracted from version 1.10.0.83. The differences between versions are minor, but you can get the exact fields and their default values for a specific version from a game process memory dump.

| Name                              | Type | Default Value                                               | Note                                   |
| --------------------------------- | ---- | ----------------------------------------------------------- | -------------------------------------- |
| AuthServerAddress                 | str  | auth.marvelheroes.com                                       | Default value from later versions      |
| AuthServerUrl                     | str  | /AuthServer/Login/IndexPB                                   | Default value from later versions      |
| AuthServerTicketUrl               | str  |                                                             |                                        |
| AuthServerPort                    |      | 80                                                          | The game actually always uses port 443 |
| CrashReportHost                   | str  | hq-gar-dev01                                                |                                        |
| CrashReportUrl                    | str  | /MinidumpWebService/Home/ProcessPostedDump                  |                                        |
| CrashReportPort                   |      |                                                             |                                        |
| VerifyFailureHost                 | str  | localhost                                                   |                                        |
| VerifyFailureUrl                  | str  |                                                             |                                        |
| VerifyFailurePort                 |      |                                                             |                                        |
| ReportDefectHost                  | str  |                                                             |                                        |
| ReportDefectUrl                   | str  | /BugAndFeatureWebService/NewDefect                          |                                        |
| ReportDefectPort                  |      |                                                             |                                        |
| ReportSuggestionHost              | str  |                                                             |                                        |
| ReportSuggestionUrl               | str  | /BugAndFeatureWebService/NewSuggestion                      |                                        |
| ReportSuggestionPort              |      |                                                             |                                        |
| UsePlaytestOnlyCommands           | bool |                                                             |                                        |
| OnCloseUrl                        | str  |                                                             |                                        |
| OnCloseUrlSteam                   | str  |                                                             |                                        |
| AccountWebsiteUrl                 | str  | https://www.marvelheroes.com                                |                                        |
| AccountRegistrationUrl            | str  | https://login.marvelheroes.com/registration.php?pcode=      |                                        |
| SteamAccountRegistrationUrl       | str  | https://login.marvelheroes.com/registration.php?pcode=steam |                                        |
| BitRaiderAccountRegistrationUrl   | str  |                                                             |                                        |
| SteamMicroTransactionCompleteHost | str  | partner.marvelheroes.com                                    |                                        |
| SteamMicroTransactionCompletePort |      |                                                             |                                        |
| SteamMicroTransactionCompleteUrl  | str  | /steam/finalize                                             |                                        |
| DriverUpdateURL                   | str  | https://marvelheroes.com/support/drivers                    |                                        |


