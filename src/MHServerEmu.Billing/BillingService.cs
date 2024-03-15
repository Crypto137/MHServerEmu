using Gazillion;
using MHServerEmu.Billing.Catalogs;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Tcp;
using MHServerEmu.Frontend;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.PlayerManagement;

namespace MHServerEmu.Billing
{
    public class BillingService : IGameService
    {
        private const ushort MuxChannel = 1;

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

        public void Handle(ITcpClient tcpClient, GameMessage message)
        {
            var client = (FrontendClient)tcpClient;

            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageGetCatalog:
                    if (message.TryDeserialize<NetMessageGetCatalog>(out var getCatalog))
                        OnGetCatalog(client, getCatalog);
                    break;

                case ClientToGameServerMessage.NetMessageGetCurrencyBalance:
                    OnGetCurrencyBalance(client);
                    break;

                case ClientToGameServerMessage.NetMessageBuyItemFromCatalog:
                    if (message.TryDeserialize<NetMessageBuyItemFromCatalog>(out var buyItemFromCatalog))
                        OnBuyItemFromCatalog(client, buyItemFromCatalog);
                    break;

                default:
                    Logger.Warn($"Handle(): Received unhandled message {(ClientToGameServerMessage)message.Id} (id {message.Id})");
                    break;
            }
        }

        public void Handle(ITcpClient client, IEnumerable<GameMessage> messages)
        {
            foreach (GameMessage message in messages)
                Handle(client, message);
        }

        public string GetStatus()
        {
            return $"Running";
        }

        #endregion

        private void OnGetCatalog(FrontendClient client, NetMessageGetCatalog getCatalog)
        {
            // Bail out if the client already has an up to date catalog
            if (getCatalog.TimestampSeconds == _catalog.TimestampSeconds && getCatalog.TimestampMicroseconds == _catalog.TimestampMicroseconds)
                return;

            // Send the current catalog
            client.SendMessage(MuxChannel, _catalog.ToNetMessageCatalogItems(false));
        }

        private void OnGetCurrencyBalance(FrontendClient client)
        {
            client.SendMessage(MuxChannel, NetMessageGetCurrencyBalanceResponse.CreateBuilder()
                .SetCurrencyBalance(_currencyBalance)
                .Build());
        }

        private void OnBuyItemFromCatalog(FrontendClient client, NetMessageBuyItemFromCatalog buyItemFromCatalog)
        {
            Logger.Info($"Received NetMessageBuyItemFromCatalog");
            Logger.Trace(buyItemFromCatalog.ToString());

            // HACK: change costume when a player "buys" a costume
            var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as PlayerManagerService;
            var game = playerManager.GetGameByPlayer(client);
            var playerConnection = game.NetworkManager.GetPlayerConnection(client);
            var player = playerConnection.Player;
            var avatar = player.CurrentAvatar;

            CatalogEntry entry = _catalog.GetEntry(buyItemFromCatalog.SkuId);
            if (entry == null || entry.GuidItems.Length == 0)
            {
                SendBuyItemResponse(client, false, BuyItemResultErrorCodes.BUY_RESULT_ERROR_UNKNOWN, buyItemFromCatalog.SkuId);
                return;
            }

            var costumePrototype = entry.GuidItems[0].ItemPrototypeRuntimeIdForClient.As<CostumePrototype>();
            if (costumePrototype == null || costumePrototype.UsableBy != avatar.BaseData.PrototypeId)
            {
                SendBuyItemResponse(client, false, BuyItemResultErrorCodes.BUY_RESULT_ERROR_UNKNOWN, buyItemFromCatalog.SkuId);
                return;
            }

            // Update player and avatar properties
            avatar.Properties[PropertyEnum.CostumeCurrent] = costumePrototype.DataRef;
            player.Properties[PropertyEnum.AvatarLibraryCostume, 0, avatar.BaseData.PrototypeId] = costumePrototype.DataRef;

            // Send client property updates (TODO: Remove this when we have those generated automatically)
            // Avatar entity
            client.SendMessage(MuxChannel, Property.ToNetMessageSetProperty(
                avatar.Properties.ReplicationId, new(PropertyEnum.CostumeCurrent), entry.GuidItems[0].ItemPrototypeRuntimeIdForClient));

            // Player entity
            PropertyParam enumValue = Property.ToParam(PropertyEnum.AvatarLibraryCostume, 1, avatar.BaseData.PrototypeId);

            client.SendMessage(MuxChannel, Property.ToNetMessageSetProperty(
                player.Properties.ReplicationId, new(PropertyEnum.AvatarLibraryCostume, 0, enumValue), costumePrototype.DataRef));

            // Send buy response
            SendBuyItemResponse(client, true, BuyItemResultErrorCodes.BUY_RESULT_ERROR_SUCCESS, buyItemFromCatalog.SkuId);
        }

        private void SendBuyItemResponse(FrontendClient client, bool didSucceed, BuyItemResultErrorCodes errorCode, long skuId)
        {
            client.SendMessage(MuxChannel, NetMessageBuyItemFromCatalogResponse.CreateBuilder()
                .SetDidSucceed(didSucceed)
                .SetCurrentCurrencyBalance(_currencyBalance)
                .SetErrorcode(errorCode)
                .SetSkuId(skuId)
                .Build());
        }
    }
}
