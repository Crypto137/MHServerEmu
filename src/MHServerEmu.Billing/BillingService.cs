using Gazillion;
using MHServerEmu.Billing.Catalogs;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Tcp;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;
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
            var client = (FrontendClient)tcpClient;

            // This is pretty rough, we need a better way of handling this
            var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as PlayerManagerService;
            var game = playerManager.GetGameByPlayer(client);
            var playerConnection = game.NetworkManager.GetPlayerConnection(client);

            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageGetCatalog:            OnGetCatalog(playerConnection, message); break;
                case ClientToGameServerMessage.NetMessageGetCurrencyBalance:    OnGetCurrencyBalance(playerConnection, message); break;
                case ClientToGameServerMessage.NetMessageBuyItemFromCatalog:    OnBuyItemFromCatalog(playerConnection, message); break;

                default: Logger.Warn($"Handle(): Unhandled {(ClientToGameServerMessage)message.Id} [{message.Id}]"); break;
            }
        }

        public string GetStatus()
        {
            return $"Catalog Entries: {_catalog.Entries.Length}";
        }

        #endregion

        private bool OnGetCatalog(PlayerConnection playerConnection, MailboxMessage message)
        {
            var getCatalog = message.As<NetMessageGetCatalog>();
            if (getCatalog == null) return Logger.WarnReturn(false, $"OnGetCatalog(): Failed to retrieve message");

            // Bail out if the client already has an up to date catalog
            if (getCatalog.TimestampSeconds == _catalog.TimestampSeconds && getCatalog.TimestampMicroseconds == _catalog.TimestampMicroseconds)
                return true;

            // Send the current catalog
            playerConnection.SendMessage(_catalog.ToNetMessageCatalogItems(false));
            return true;
        }

        private void OnGetCurrencyBalance(PlayerConnection playerConnection, MailboxMessage message)
        {
            playerConnection.SendMessage(NetMessageGetCurrencyBalanceResponse.CreateBuilder()
                .SetCurrencyBalance(_currencyBalance)
                .Build());
        }

        private bool OnBuyItemFromCatalog(PlayerConnection playerConnection, MailboxMessage message)
        {
            var buyItemFromCatalog = message.As<NetMessageBuyItemFromCatalog>();
            if (buyItemFromCatalog == null) return Logger.WarnReturn(false, $"OnBuyItemFromCatalog(): Failed to retrieve message");

            Logger.Info($"Received NetMessageBuyItemFromCatalog");
            Logger.Trace(buyItemFromCatalog.ToString());

            var player = playerConnection.Player;

            CatalogEntry entry = _catalog.GetEntry(buyItemFromCatalog.SkuId);
            if (entry == null || entry.GuidItems.Length == 0)
            {
                SendBuyItemResponse(playerConnection, false, BuyItemResultErrorCodes.BUY_RESULT_ERROR_UNKNOWN, buyItemFromCatalog.SkuId);
                return true;
            }

            PrototypeId itemProtoRef = entry.GuidItems[0].ItemPrototypeRuntimeIdForClient;
            if (GameDatabase.DataDirectory.PrototypeIsA<CostumePrototype>(itemProtoRef))
            {
                // HACK: change costume when a player "buys" a costume
                Avatar avatar = player.GetAvatar(itemProtoRef.As<CostumePrototype>().UsableBy);
                if (avatar == null)
                {
                    SendBuyItemResponse(playerConnection, false, BuyItemResultErrorCodes.BUY_RESULT_ERROR_UNKNOWN, buyItemFromCatalog.SkuId);
                    return true;
                }

                // Update player and avatar properties
                avatar.Properties[PropertyEnum.CostumeCurrent] = itemProtoRef;
                player.Properties[PropertyEnum.AvatarLibraryCostume, 0, avatar.PrototypeDataRef] = itemProtoRef;
            }
            else if (GameDatabase.DataDirectory.PrototypeIsA<ItemPrototype>(itemProtoRef))
            {
                // Give the player the item they are trying to "buy"
                player.Game.LootManager.GiveItem(player, itemProtoRef);
            }
            else
            {
                // Return error if this SKU is not an item
                SendBuyItemResponse(playerConnection, false, BuyItemResultErrorCodes.BUY_RESULT_ERROR_UNKNOWN, buyItemFromCatalog.SkuId);
                return true;
            }

            // Send buy response
            SendBuyItemResponse(playerConnection, true, BuyItemResultErrorCodes.BUY_RESULT_ERROR_SUCCESS, buyItemFromCatalog.SkuId);
            return true;
        }

        private void SendBuyItemResponse(PlayerConnection playerConnection, bool didSucceed, BuyItemResultErrorCodes errorCode, long skuId)
        {
            playerConnection.SendMessage(NetMessageBuyItemFromCatalogResponse.CreateBuilder()
                .SetDidSucceed(didSucceed)
                .SetCurrentCurrencyBalance(_currencyBalance)
                .SetErrorcode(errorCode)
                .SetSkuId(skuId)
                .Build());
        }
    }
}
