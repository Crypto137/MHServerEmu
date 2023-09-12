# SiteConfig

SiteConfig.xml is a configuration file that the client requests from a CDN on startup that includes information required for logging in, such as AuthServerAddress. SiteConfig.xml follows the same syntax as ClientConfig.xml that comes with the client.

Some versions of the game refuse to start without a SiteConfig. In some of those cases you can work around this by putting a defaultsiteconfig.xml file in the same directory as ClientConfig.xml.

## Fields

These fields were extracted from version 1.52.0.1700. You can get the exact fields and their default values for a specific version from a game process memory dump. Where possible the default values were filled in with information from an archived test center SiteConfig.

| Name                                | Type | Default Value                                                                                     | Note                                                         |
| ----------------------------------- | ---- | ------------------------------------------------------------------------------------------------- | ------------------------------------------------------------ |
| AuthServerAddress                   | str  | auth.marvelheroes.com                                                                             |                                                              |
| AuthServerUrl                       | str  | /AuthServer/Login/IndexPB                                                                         |                                                              |
| AuthServerTicketUrl                 | str  |                                                                                                   |                                                              |
| AuthServerPort                      | int  | 443                                                                                               | Shipping builds seem to be hardcoded to port 443             |
| CrashReportHost                     | str  | auth.marvelheroes.com                                                                             | Embedded value is hq-gar-dev01                               |
| CrashReportUrl                      | str  | /CrashDumpFrontend/Multipart/ProcessCrashReport                                                   | Embedded value is /MinidumpWebService/Home/ProcessPostedDump |
| CrashReportPort                     | int  | 80                                                                                                |                                                              |
| VerifyFailureHost                   | str  | hq-sis01-qa01.hq-california.com                                                                   | Embedded value is localhost                                  |
| VerifyFailureUrl                    | str  | /CrashDumpFrontend/Multipart/ProcessVerifyReport                                                  |                                                              |
| VerifyFailurePort                   | int  | 80                                                                                                |                                                              |
| ReportSuggestionHost                | str  | auth.marvelheroes.com                                                                             |                                                              |
| ReportSuggestionUrl                 | str  | /BugAndFeatureWebService/NewSuggestion                                                            |                                                              |
| ReportSuggestionPort                | int  | 80                                                                                                |                                                              |
| OnCloseUrl                          | str  |                                                                                                   |                                                              |
| OnCloseUrlSteam                     | str  |                                                                                                   |                                                              |
| AccountWebsiteUrl                   | str  | https://www.marvelheroes.com                                                                      |                                                              |
| AccountRegistrationUrl              | str  | https://login.marvelheroes.com/registration.php?pcode=                                            |                                                              |
| SteamAccountRegistrationUrl         | str  | https://login.marvelheroes.com/registration.php?pcode=steam                                       |                                                              |
| SteamMicroTransactionCompleteHost   | str  | partner.marvelheroes.com                                                                          |                                                              |
| SteamMicroTransactionCompleteUrl    | str  | /steam/finalize                                                                                   |                                                              |
| SteamMicroTransactionCompletePort   | int  | 443                                                                                               |                                                              |
| SteamMicroTransactionCompleteUseSsl | bool | true                                                                                              |                                                              |
| DriverUpdateURL                     | str  | https://marvelheroes.com/support/drivers                                                          |                                                              |
| AllowNonSteamStoreInSteamBuild      | bool |                                                                                                   |                                                              |
| InGameHelpURL                       | str  | https://d5.parature.com/ics/support/default.asp?deptID=15144                                      |                                                              |
| LowMemoryURL                        | str  | https://forums.marvelheroes.com/discussion/comment/1179527/#Comment_1179527                       |                                                              |
| ForgotPasswordURL                   | str  | https://login.marvelheroes.com/forgotpassword.php                                                 |                                                              |
| VerifyEmailURL                      | str  | https://login.marvelheroes.com                                                                    |                                                              |
| ChangePasswordURL                   | str  | https://login.marvelheroes.com/dashboard/changepassword.php                                       |                                                              |
| EnableLiveTips                      | bool |                                                                                                   |                                                              |
| EnableLiveTipsDownloader            | bool |                                                                                                   |                                                              |
| LoadScreenTipsURL                   | str  | http://cdn.marvelheroes.com/marvelheroes/liveloadingtips.xml                                      |                                                              |
| LiveTipsQueryInterval               |      |                                                                                                   |                                                              |
| ConfirmEmailFAQ                     |      |                                                                                                   |                                                              |
| YouTubeVideoPlayer                  | str  | http://storecdn.marvelheroes.com/cdn/youtubePlayer.html?videoId=%S&height=%d&width=%d&protoId=%S  |                                                              |
| YouTubeVideoPlayerPlaylist          | str  | http://storecdn.marvelheroes.com/cdn/youtubePlayer.html?playlist=%S&height=%d&width=%d&protoId=%S |                                                              |
| BinkMovieUrl                        | str  | http://storecdn.marvelheroes.com/cdn/videos/%shttp://storecdn.marvelheroes.com/cdn/videos/%s      |                                                              |
| EnabledLocales                      | str  | chi.all;deu.all;eng.all;fra.all;jpn.all;kor.all;por.all;rus.all;sg1.all;sg2.all;sg3.all;spa.all   |                                                              |
| LoginDownloadURL                    | str  |                                                                                                   |                                                              |
| LoginStreamURL                      | str  |                                                                                                   |                                                              |
| CatalogDownloadHost                 | str  |                                                                                                   |                                                              |

## Removed Fields

Some fields are only present in early versions of the game.

| Name                            | Type | Default Value                      | Note |
| ------------------------------- | ---- | ---------------------------------- | ---- |
| ReportDefectHost                | str  |                                    |      |
| ReportDefectUrl                 | str  | /BugAndFeatureWebService/NewDefect |      |
| ReportDefectPort                | int  |                                    |      |
| UsePlaytestOnlyCommands         | bool |                                    |      |
| BitRaiderAccountRegistrationUrl | str  |                                    |      |
