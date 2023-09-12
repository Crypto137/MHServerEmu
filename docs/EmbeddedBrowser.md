# Embedded Browser

The in-game store and the community news window use a CEF-based web browser that can load HTML pages. These pages can interact with the UI via a JavaScript API.

## API Calls

Calls are generally made with onclick events (e.g. ```<a onclick="myApi.OpenCatalogToItem('HeroesPage', '')">```). You can find an example of a store home page [here](https://github.com/Crypto137/MHServerEmu/tree/master/assets/store/mhgame_store_home).

| Call                                                   | Description                                                                                                   |
| ------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------- |
| myApi.OpenCatalogToItem(string pageName, string skuId) | Opens the specified page / item. See the table below for page names. SkuId should be in hex (e.g. 0x1CA0070). |
| myApi.CloseAddGPanel(bool success)                     | Closes the add G panel. If success is true (1) calls UpdateWalletBalance.                                     |
| myApi.UpdateWalletBalance()                            | Sends a NetMessageGetCurrencyBalance to the server.                                                           |
| myApi.BuyBundleFromJS(string skuId)                    |                                                                                                               |
| myApi.ReloadAddGPage()                                 |                                                                                                               |
| myApi.OpenExternalBrowserFromJS(string url)            | Opens a URL in the user's default web browser (news only).                                                    |
| myApi.OpenNewsUrl(string url)                          | Opens a popup in the news window (news V2 only).                                                              |
| myApi.CloseNewsUrl()                                   | Closes the news window popup (news V2 only).                                                                  |

## Store Page Names

| In-Game Name | Internal Name |
| ------------ | ------------- |
| Home         | HomePage      |
| Heroes       | HeroesPage    |
| Costumes     | CostumesPage  |
| Items        | BoostsPage    |
| Team Ups     | TeamUpsPage   |
| Cards        |               |
| Specials     | SpecialsPage  |
| Bundles      | BundlesPage   |

## Frame Sizes

- Home Page: 974x528

- Banner: 748x110

- Add G: 1050x700

- Bundles: 880x569

- Community News (Version 1): 954x641

- Community News (Version 2) Main Page 988x644

- Community News (Version 2) Popup: 650x764
