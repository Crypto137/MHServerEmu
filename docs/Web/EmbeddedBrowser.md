# Embedded Browser

Some of the in-game UI panels (TOS popup on login, store, community news) use an embedded web browser that can load HTML pages. These pages can interact with the client UI via a JavaScript API.

## Browser Versions

Originally the client used Awesomium as the browser backend, but it was replaced with Chromium Embedded Framework (CEF) in game version 1.22. Below is a list of known browser versions used by the client.

| Game Version | Browser Version          | Browser Release Date |
| ------------ | ------------------------ | -------------------- |
| 1.9-1.21     | Awesomium 1.6.5          | 2012-02-23           |
| 1.22-1.32    | CEF 3.1650.1544          | 2013-12-08           |
| 1.33         | CEF 3.2272.2035          | 2015-02-26           |
| 1.34         | CEF 3.2272.2077          | 2015-04-13           |
| 1.35-1.52    | CEF 3.1650.1639          | 2014-03-13           |
| 1.53         | CEF 3.3112.1656.g9ec3e42 | 2017-08-10           |

## API Calls

Calls are generally made with onclick events (e.g. ```<a onclick="myApi.OpenCatalogToItem('HeroesPage', '')">```). You can find an example of a store home page [here](https://github.com/Crypto137/MHServerEmuWebAssets/tree/master/store).

| Call                                                   | Description                                                                                                   |
| ------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------- |
| myApi.OpenCatalogToItem(string pageName, string skuId) | Opens the specified page / item. See the table below for page names. SkuId should be in hex (e.g. 0x1CA0070). |
| myApi.CloseAddGPanel(bool success)                     | Closes the add G panel. If success is true (1) calls UpdateWalletBalance.                                     |
| myApi.UpdateWalletBalance()                            | Sends a NetMessageGetCurrencyBalance to the server.                                                           |
| myApi.BuyBundleFromJS(string skuId)                    |                                                                                                               |
| myApi.ReloadAddGPage()                                 |                                                                                                               |
| myApi.OpenExternalBrowserFromJS(string url)            | Opens a URL in the user's default web browser (TOS and news only).                                            |
| myApi.OpenNewsUrl(string url)                          | Opens a popup in the news window (news V2 only).                                                              |
| myApi.CloseNewsUrl()                                   | Closes the news window popup (news V2 only).                                                                  |
| myApi.CloseLegalDoc(bool accepted)                     | Closes the TOS popup.                                                                                         |

## Store Page Names

| In-Game Name | Internal Name |
| ------------ | ------------- |
| Home         | HomePage      |
| Heroes       | HeroesPage    |
| Costumes     | CostumesPage  |
| Items        | BoostsPage    |
| Team Ups     | TeamUpsPage   |
| Cards        | ChestsPage    |
| Specials     | SpecialsPage  |
| Bundles      | BundlesPage   |

## Frame Sizes

The actual viewable area is slightly smaller than these.

- TOS Popup: 500x400

- Store Home Page: 974x528

- Store Banner: 748x110

- Store Add G: 1050x700

- Store Bundles: 880x569

- Community News (Version 1): 954x641

- Community News (Version 2) Main Page 988x644

- Community News (Version 2) Popup: 650x764

## Bundles

- Bundle images are downloaded and cached in `%TEMP%\MarvelHeroes`. It is possible for the client to cache an invalid bundle image, which may require clearing the cache to fix it.

- A bundle image needs to be a PNG file with its horizontal and vertical resolution being a multiple of 4. Preferred resolution is 344x128.

- `?gmode=` is appended to information page requests, indicating whether the gifting mode is enabled (0 or 1).
