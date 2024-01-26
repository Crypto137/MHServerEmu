# Client Versions

It is possible to download various client versions from Steam if you still have the game in your library.

1. Open the Steam console: [steam://open/console](steam://open/console)

2. Enter the following command: download_depot 226320 depot_id manifest_id (for example, download_depot 226320 226321 2278274898429684559).

3. Steam will begin to download the requested client build. There will be no download progress indication.

4. The console will display a message when the download finishes. You can find the client files in the following directory %SteamInstallationDirectory%\steamapps\content\app_226320\

You can find depot and manifest ids for various builds on [SteamDB](https://steamdb.info/app/226320/depots/).

Client versions from before the Mac port was released can be found in depot [226321 (Marvel Heroes Content)](https://steamdb.info/depot/226321/). After the Mac port was released, the main depot was separated into three: [226323 (Windows Marvel Heroes)](https://steamdb.info/depot/226323/), [226322 (Mac Marvel Heroes)](https://steamdb.info/depot/226322/), and [226325 (OS Independent Data Marvel Heroes)](https://steamdb.info/depot/226325/). The first two contain only binaries for their respective versions, while the latter one contains most of the game data that is shared across both versions. Mac binaries can be especially useful for reverse engineering, since they contain function names.

Some versions of the client have also been uploaded to the [Internet Archive](https://archive.org/).

## Notable Client Versions

Some versions (1.9.0.645, 1.10.0.83, and 1.0.3753.0), have verbose logging enabled, which can be useful for development. Here are some of the more notable versions:

| Release Date | Internal Version | Official Version | Note                                   | Depot  | Manifest            |
| ------------ | ---------------- | ---------------- | -------------------------------------- | ------ | ------------------- |
| 2013.05.10   | 1.9.0.645        | -                | Final open beta weekend client         | 226321 | 796487006444451558  |
| 2013.06.04   | 1.10.0.83        | -                | Launch version                         | 226321 | 2278274898429684559 |
| 2014.06.04   | 1.23.0.23        | 2015 1.0         | 2015 rebrand initial version           | 226321 | 4047593332140705154 |
| 2014.07.10   | 1.0.3753.0       | -                | Debug build used for refactoring depot | 226323 | 2407360800135204479 |
|              |                  |                  |                                        | 226325 | 385175237809236510  |
| 2016.01.14   | 1.41.0.533       | 2015 1.85        | Final version before 2016 rebrand      | 226323 | 6180075372966345126 |
|              |                  |                  |                                        | 226322 | 6536551161247327268 |
|              |                  |                  |                                        | 226325 | 7612048985720635839 |
| 2016.12.20   | 1.48.0.1712      | 2016 1.34        | Final version before BUE               | 226323 | 5035029327181736026 |
|              |                  |                  |                                        | 226322 | 4200661803723725930 |
|              |                  |                  |                                        | 226325 | 3643670002035853472 |
| 2017.09.07   | 1.52.0.1700      | Omega 2.16a      | Final released version                 | 226323 | 8396961377069596635 |
|              |                  |                  |                                        | 226322 | 795496086210475533  |
|              |                  |                  |                                        | 226325 | 8330045504398885382 |
| 2017.11.08   | 1.53.0.203       | -                | Final public test center version       | 226323 | 5926578807282360763 |
|              |                  |                  |                                        | 226322 | 4233591427904899470 |
|              |                  |                  |                                        | 226325 | 8533865919269363573 |
