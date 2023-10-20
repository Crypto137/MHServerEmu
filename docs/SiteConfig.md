# SiteConfig

SiteConfig.xml is a configuration file that the client requests from the CDN on startup that includes information required for logging in, such as AuthServerAddress. SiteConfig.xml follows the same syntax as [ClientConfig.xml](./ClientConfig.md) that comes with the client.

Early versions of the game cannot start without a SiteConfig. You can work around this by putting a defaultsiteconfig.xml file in the same directory as ClientConfig.xml.

## Fields

The fields and their default values are embedded in the executable. Some fields have alternate values from an archived test center SiteConfig.

| Name                                | Type | Default Value                                                                                     | Note                                                             |
| ----------------------------------- | ---- | ------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------- |
| AuthServerAddress                   | str  | auth.marvelheroes.com                                                                             | Archived value: auth.testcenter.marvelheroes.com                 |
| AuthServerUrl                       | str  | /AuthServer/Login/IndexPB                                                                         |                                                                  |
| AuthServerTicketUrl                 | str  |                                                                                                   |                                                                  |
| AuthServerPort                      | str  | 443                                                                                               | Always 443 in Shipping builds                                    |
| CrashReportHost                     | str  | hq-gar-dev01                                                                                      | Archived value: auth.testcenter.marvelheroes.com                 |
| CrashReportUrl                      | str  | /MinidumpWebService/Home/ProcessPostedDump                                                        | Archived value: /CrashDumpFrontend/Multipart/ProcessCrashReport  |
| CrashReportPort                     | int  | 80                                                                                                |                                                                  |
| VerifyFailureHost                   | str  | localhost                                                                                         | Archived value: hq-sis01-qa01.hq-california.com                  |
| VerifyFailureUrl                    | str  | /                                                                                                 | Archived value: /CrashDumpFrontend/Multipart/ProcessVerifyReport |
| VerifyFailurePort                   | int  | 80                                                                                                |                                                                  |
| ReportSuggestionHost                | str  | hq-gar-dev01                                                                                      | Archived value: auth.testcenter.marvelheroes.com                 |
| ReportSuggestionUrl                 | str  | /BugAndFeatureWebService/NewSuggestion                                                            |                                                                  |
| ReportSuggestionPort                | int  | 80                                                                                                |                                                                  |
| OnCloseUrl                          | str  |                                                                                                   |                                                                  |
| OnCloseUrlSteam                     | str  |                                                                                                   |                                                                  |
| AccountWebsiteUrl                   | str  | https://www.marvelheroes.com                                                                      |                                                                  |
| AccountRegistrationUrl              | str  | https://login.marvelheroes.com/registration.php?pcode=                                            |                                                                  |
| SteamAccountRegistrationUrl         | str  | https://login.marvelheroes.com/registration.php?pcode=steam                                       |                                                                  |
| SteamMicroTransactionCompleteHost   | str  | partner.marvelheroes.com                                                                          | Beta 1.9 value: hq-gar-dev01                                     |
| SteamMicroTransactionCompletePort   | int  | 443                                                                                               |                                                                  |
| SteamMicroTransactionCompleteUrl    | str  | /steam/finalize                                                                                   | Beta 1.9 value: /echowebservice                                  |
| SteamMicroTransactionCompleteUseSsl | bool | true                                                                                              |                                                                  |
| DriverUpdateURL                     | str  | https://marvelheroes.com/support/drivers                                                          |                                                                  |
| AllowNonSteamStoreInSteamBuild      | bool | false                                                                                             |                                                                  |
| InGameHelpURL                       | str  | https://d5.parature.com/ics/support/default.asp?deptID=15144                                      |                                                                  |
| LowMemoryURL                        | str  | https://forums.marvelheroes.com/discussion/comment/1179527/#Comment_1179527                       |                                                                  |
| ForgotPasswordURL                   | str  | https://login.marvelheroes.com/forgotpassword.php                                                 |                                                                  |
| VerifyEmailURL                      | str  | https://login.marvelheroes.com                                                                    |                                                                  |
| ChangePasswordURL                   | str  | https://login.marvelheroes.com/dashboard/changepassword.php                                       |                                                                  |
| EnableLiveTips                      | bool | false                                                                                             |                                                                  |
| EnableLiveTipsDownloader            | bool | false                                                                                             |                                                                  |
| LoadScreenTipsURL                   | str  | http://cdn.marvelheroes.com/marvelheroes/liveloadingtips.xml                                      |                                                                  |
| LiveTipsQueryInterval               | int  | 15                                                                                                |                                                                  |
| ConfirmEmailFAQ                     | str  | https://d5.parature.com/ics/support/default.asp?deptID=15144                                      |                                                                  |
| YouTubeVideoPlayer                  | str  | http://storecdn.marvelheroes.com/cdn/youtubePlayer.html?videoId=%S&height=%d&width=%d&protoId=%S  |                                                                  |
| YouTubeVideoPlayerPlaylist          | str  | http://storecdn.marvelheroes.com/cdn/youtubePlayer.html?playlist=%S&height=%d&width=%d&protoId=%S |                                                                  |
| BinkMovieUrl                        | str  | http://storecdn.marvelheroes.com/cdn/videos/%s                                                    |                                                                  |
| EnabledLocales                      | str  | chi.all;deu.all;eng.all;fra.all;jpn.all;kor.all;por.all;rus.all;sg1.all;sg2.all;sg3.all;spa.all   |                                                                  |
| LoginDownloadURL                    | str  |                                                                                                   |                                                                  |
| LoginStreamURL                      | str  |                                                                                                   |                                                                  |
| CatalogDownloadHost                 | str  |                                                                                                   |                                                                  |
| ServerStatusUrlPC                   | str  | https://marvelheroes.com/status/PC                                                                | Added in 1.53                                                    |
| ServerStatusUrlPS4                  | str  | https://marvelheroes.com/status/PS4                                                               | Added in 1.53                                                    |
| ServerStatusUrlXBoxOne              | str  | https://marvelheroes.com/status/XBOXONE                                                           | Added in 1.53                                                    |
| ServerStatusMessageUrl              | str  | https://marvelheroes.com/platform                                                                 | Added in 1.53                                                    |

## Removed Fields

Some fields are only present in early versions of the game.

| Name                            | Type | Default Value                                          | Note                                             |
| ------------------------------- | ---- | ------------------------------------------------------ | ------------------------------------------------ |
| ReportDefectHost                | str  | localhost                                              | Archived value: auth.testcenter.marvelheroes.com |
| ReportDefectUrl                 | str  | /BugAndFeatureWebService/NewDefect                     |                                                  |
| ReportDefectPort                | int  | 80                                                     |                                                  |
| UsePlaytestOnlyCommands         | bool | false                                                  |                                                  |
| BitRaiderAccountRegistrationUrl | str  | https://login.marvelheroes.com/registration.php?pcode= |                                                  |
