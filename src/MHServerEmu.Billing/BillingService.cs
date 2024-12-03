using Gazillion;
using MHServerEmu.Billing.Catalogs;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Tcp;
using MHServerEmu.Frontend;
using MHServerEmu.Games;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Network;
using MHServerEmu.PlayerManagement;

namespace MHServerEmu.Billing
{
    public class BillingService : IGameService
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly string BillingDataDirectory = Path.Combine(FileHelper.DataDirectory, "Billing");

        private readonly Catalog _catalog;
        private readonly long _currencyBalance;

        public BillingService()
        {
            var config = ConfigManager.Instance.GetConfig<BillingConfig>();
            _currencyBalance = config.CurrencyBalance;

            _catalog = FileHelper.DeserializeJson<Catalog>(Path.Combine(BillingDataDirectory, "Catalog.json"));

            // Apply a patch to the catalog if it's enabled and there's one
            if (config.ApplyCatalogPatch)
            {
                string patchPath = Path.Combine(BillingDataDirectory, "CatalogPatch.json");
                if (File.Exists(patchPath))
                {
                    CatalogEntry[] catalogPatch = FileHelper.DeserializeJson<CatalogEntry[]>(patchPath);
                    _catalog.ApplyPatch(catalogPatch);
                }
            }

            // Override store urls if enabled
            if (config.OverrideStoreUrls)
            {
                _catalog.Urls[0].StoreHomePageUrl = config.StoreHomePageUrl;
                _catalog.Urls[0].StoreBannerPageUrls[0].Url = config.StoreHomeBannerPageUrl;
                _catalog.Urls[0].StoreBannerPageUrls[1].Url = config.StoreHeroesBannerPageUrl;
                _catalog.Urls[0].StoreBannerPageUrls[2].Url = config.StoreCostumesBannerPageUrl;
                _catalog.Urls[0].StoreBannerPageUrls[3].Url = config.StoreBoostsBannerPageUrl;
                _catalog.Urls[0].StoreBannerPageUrls[4].Url = config.StoreChestsBannerPageUrl;
                _catalog.Urls[0].StoreBannerPageUrls[5].Url = config.StoreSpecialsBannerPageUrl;
                _catalog.Urls[0].StoreRealMoneyUrl = config.StoreRealMoneyUrl;
            }

            Logger.Info($"Initialized store catalog with {_catalog.Entries.Length} entries");
        }

        #region IGameService Implementation

        public void Run() { }

        public void Shutdown() { }

        public void Handle(ITcpClient tcpClient, MessagePackage message)
        {
            Logger.Warn($"Handle(): Unhandled MessagePackage");
        }

        public void Handle(ITcpClient client, IEnumerable<MessagePackage> messages)
        {
            foreach (MessagePackage message in messages)
                Handle(client, message);
        }

        public void Handle(ITcpClient tcpClient, MailboxMessage message)
        {
            FrontendClient client = (FrontendClient)tcpClient;

            // This is pretty rough, we need a better way of handling this
            // TODO: Move this to Games, use BillingService just as a source for catalog data
            PlayerManagerService playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as PlayerManagerService;
            Game game = playerManager.GetGameByPlayer(client);
            PlayerConnection playerConnection = game.NetworkManager.GetPlayerConnection(client);
            Player player = playerConnection.Player;

            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageGetCatalog:            OnGetCatalog(player, message); break;           // 68
                case ClientToGameServerMessage.NetMessageGetCurrencyBalance:    OnGetCurrencyBalance(player, message); break;   // 69
                case ClientToGameServerMessage.NetMessageBuyItemFromCatalog:    OnBuyItemFromCatalog(player, message); break;   // 70

                default: Logger.Warn($"Handle(): Unhandled {(ClientToGameServerMessage)message.Id} [{message.Id}]"); break;
            }
        }

        public string GetStatus()
        {
            return $"Catalog Entries: {_catalog.Entries.Length}";
        }

        #endregion

        private bool OnGetCatalog(Player player, MailboxMessage message)
        {
            var getCatalog = message.As<NetMessageGetCatalog>();
            if (getCatalog == null) return Logger.WarnReturn(false, $"OnGetCatalog(): Failed to retrieve message");

            // Bail out if the client already has an up to date catalog
            if (getCatalog.TimestampSeconds == _catalog.TimestampSeconds && getCatalog.TimestampMicroseconds == _catalog.TimestampMicroseconds)
                return true;

            // Send the current catalog
            player.SendMessage(_catalog.ToNetMessageCatalogItems(false));
            return true;
        }

        private void OnGetCurrencyBalance(Player player, MailboxMessage message)
        {
            player.SendMessage(NetMessageGetCurrencyBalanceResponse.CreateBuilder()
                .SetCurrencyBalance(_currencyBalance)
                .Build());
        }

        private bool OnBuyItemFromCatalog(Player player, MailboxMessage message)
        {
            var buyItemFromCatalog = message.As<NetMessageBuyItemFromCatalog>();
            if (buyItemFromCatalog == null) return Logger.WarnReturn(false, $"OnBuyItemFromCatalog(): Failed to retrieve message");

            long skuId = buyItemFromCatalog.SkuId;

            BuyItemResultErrorCodes result = BuyItemResultErrorCodes.BUY_RESULT_ERROR_UNKNOWN;

            CatalogEntry entry = _catalog.GetEntry(skuId);
            if (entry != null && entry.GuidItems.Length > 0)
            {
                Prototype catalogItemProto = entry.GuidItems[0].ItemPrototypeRuntimeIdForClient.As<Prototype>();

                switch (catalogItemProto)
                {
                    case ItemPrototype itemProto:
                        // Give the player the item they are trying to "buy"
                        if (player.Game.LootManager.GiveItem(itemProto.DataRef, LootContext.CashShop, player))
                            result = BuyItemResultErrorCodes.BUY_RESULT_ERROR_SUCCESS;
                        break;

                    case PlayerStashInventoryPrototype playerStashInventoryProto:
                        // Unlock the stash tab
                        if (player.UnlockInventory(playerStashInventoryProto.DataRef))
                            result = BuyItemResultErrorCodes.BUY_RESULT_ERROR_SUCCESS;
                        break;

                    default:
                        // Return error for unhandled SKU types
                        Logger.Warn($"OnBuyItemFromCatalog(): Unimplemented catalog item type {catalogItemProto.GetType().Name} for {catalogItemProto}");
                        break;
                }

                // Log successful purchases
                if (result == BuyItemResultErrorCodes.BUY_RESULT_ERROR_SUCCESS)
                    Logger.Trace($"OnBuyItemFromCatalog(): Player [{player}] purchased skuId={skuId}, catalogItemProto={catalogItemProto}");
            }

            // Send buy response
            SendBuyItemResponse(player, result, buyItemFromCatalog.SkuId);
            return true;
        }

        private void SendBuyItemResponse(Player player, BuyItemResultErrorCodes errorCode, long skuId)
        {
            player.SendMessage(NetMessageBuyItemFromCatalogResponse.CreateBuilder()
                .SetDidSucceed(errorCode == BuyItemResultErrorCodes.BUY_RESULT_ERROR_SUCCESS)
                .SetCurrentCurrencyBalance(_currencyBalance)
                .SetErrorcode(errorCode)
                .SetSkuId(skuId)
                .Build());
        }
    }
}
