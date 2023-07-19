# SiteConfig

SiteConfig.xml is a configuration file that the client requests from a CDN on startup that includes information required for logging in, such as AuthServerAddress. SiteConfig.xml follows the same syntax as ClientConfig.xml that comes with the client.

Some versions of the game refuse to start without a SiteConfig. In some of those cases you can work around this by putting a defaultsiteconfig.xml file in the same directory as ClientConfig.xml.

## Fields

These fields were extracted from version 1.52.0.1700. You can get the exact fields and their default values for a specific version from a game process memory dump.

| Name                              | Type | Default Value                                                                                     | Note                         |
| --------------------------------- | ---- | ------------------------------------------------------------------------------------------------- | ---------------------------- |
| AuthServerAddress                 | str  | auth.marvelheroes.com                                                                             |                              |
| AuthServerUrl                     | str  | /AuthServer/Login/IndexPB                                                                         |                              |
| AuthServerTicketUrl               | str  |                                                                                                   |                              |
| AuthServerPort                    | str  | 443                                                                                               | AuthServer always uses https |
| CrashReportHost                   | str  | hq-gar-dev01                                                                                      |                              |
| CrashReportUrl                    | str  | /MinidumpWebService/Home/ProcessPostedDump                                                        |                              |
| CrashReportPort                   | str  |                                                                                                   |                              |
| VerifyFailureHost                 | str  | localhost                                                                                         |                              |
| VerifyFailureUrl                  | str  |                                                                                                   |                              |
| VerifyFailurePort                 | str  |                                                                                                   |                              |
| ReportSuggestionHost              | str  |                                                                                                   |                              |
| ReportSuggestionUrl               | str  | /BugAndFeatureWebService/NewSuggestion                                                            |                              |
| ReportSuggestionPort              | str  |                                                                                                   |                              |
| OnCloseUrl                        | str  |                                                                                                   |                              |
| OnCloseUrlSteam                   | str  |                                                                                                   |                              |
| AccountWebsiteUrl                 | str  | https://www.marvelheroes.com                                                                      |                              |
| AccountRegistrationUrl            | str  | https://login.marvelheroes.com/registration.php?pcode=                                            |                              |
| SteamAccountRegistrationUrl       | str  | https://login.marvelheroes.com/registration.php?pcode=steam                                       |                              |
| SteamMicroTransactionCompleteHost | str  | partner.marvelheroes.com                                                                          |                              |
| SteamMicroTransactionCompletePort | str  |                                                                                                   |                              |
| SteamMicroTransactionCompleteUrl  | str  | /steam/finalize                                                                                   |                              |
| DriverUpdateURL                   | str  | https://marvelheroes.com/support/drivers                                                          |                              |
| AllowNonSteamStoreInSteamBuild    | bool |                                                                                                   |                              |
| InGameHelpURL                     | str  | https://d5.parature.com/ics/support/default.asp?deptID=15144                                      |                              |
| LowMemoryURL                      | str  | https://forums.marvelheroes.com/discussion/comment/1179527/#Comment_1179527                       |                              |
| ForgotPasswordURL                 | str  | https://login.marvelheroes.com/forgotpassword.php                                                 |                              |
| VerifyEmailURL                    | str  | https://login.marvelheroes.com                                                                    |                              |
| ChangePasswordURL                 | str  | https://login.marvelheroes.com/dashboard/changepassword.php                                       |                              |
| EnableLiveTips                    | bool |                                                                                                   |                              |
| EnableLiveTipsDownloader          | bool |                                                                                                   |                              |
| LoadScreenTipsURL                 | str  | http://cdn.marvelheroes.com/marvelheroes/liveloadingtips.xml                                      |                              |
| LiveTipsQueryInterval             |      |                                                                                                   |                              |
| ConfirmEmailFAQ                   |      |                                                                                                   |                              |
| YouTubeVideoPlayer                | str  | http://storecdn.marvelheroes.com/cdn/youtubePlayer.html?videoId=%S&height=%d&width=%d&protoId=%S  |                              |
| YouTubeVideoPlayerPlaylist        | str  | http://storecdn.marvelheroes.com/cdn/youtubePlayer.html?playlist=%S&height=%d&width=%d&protoId=%S |                              |
| BinkMovieUrl                      | str  | http://storecdn.marvelheroes.com/cdn/videos/%s                                                    |                              |
| EnabledLocales                    | str  | chi.all;deu.all;eng.all;fra.all;jpn.all;kor.all;por.all;rus.all;sg1.all;sg2.all;sg3.all;spa.all   |                              |
| LoginDownloadURL                  | str  |                                                                                                   |                              |
| LoginStreamURL                    | str  |                                                                                                   |                              |
| CatalogDownloadHost               | str  |                                                                                                   |                              |

## Removed Fields

Some fields are only present in early versions of the game.

| Name                            | Type | Default Value                      | Note |
| ------------------------------- | ---- | ---------------------------------- | ---- |
| ReportDefectHost                | str  |                                    |      |
| ReportDefectUrl                 | str  | /BugAndFeatureWebService/NewDefect |      |
| ReportDefectPort                | str  |                                    |      |
| UsePlaytestOnlyCommands         | bool |                                    |      |
| BitRaiderAccountRegistrationUrl | str  |                                    |      |
