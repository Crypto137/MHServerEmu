# MTX Embedded Browser API

The in-game store UI uses a CEF-based web browser that can load HTML pages. These pages can interact with the store UI via a JavaScript API.

## API Calls

Calls are generally made with onclick events (e.g. ```<a onclick="myApi.UpdateWalletBalance()">```). You can find an example of a store home page [here](https://github.com/Crypto137/MHServerEmu/tree/master/assets/store/mhgame_store_home).

| Call                                                   | Description                                                                                                   |
| ------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------- |
| myApi.OpenCatalogToItem(string pageName, string skuId) | Opens the specified page / item. See the table below for page names. SkuId should be in hex (e.g. 0x1CA0070). |
| myApi.CloseAddGPanel(int arg0)                         | Closes the add G panel. If arg0 is 1 calls UpdateWalletBalance.                                               |
| myApi.UpdateWalletBalance()                            | Sends a NetMessageGetCurrencyBalance to the server.                                                           |
| myApi.BuyBundleFromJS(string skuId)                    |                                                                                                               |
| myApi.ReloadAddGPage()                                 |                                                                                                               |

## Store Page Names

| In-Game Name | Internal Name |
| ------------ | ------------- |
| Home         |               |
| Heroes       | HeroesPage    |
| Costumes     | CostumesPage  |
| Items        | BoostsPage    |
| Team Ups     | TeamUpsPage   |
| Cards        |               |
| Specials     | SpecialsPage  |
| Bundles      | BundlesPage   |


