using Gazillion;
using MHServerEmu.Billing.Catalogs;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.PlayerManagement.Accounts.DBModels;

namespace MHServerEmu.Billing
{
    public class BillingService : IMessageHandler
    {
        private const ushort MuxChannel = 1;

        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly string BillingDataDirectory = Path.Combine(FileHelper.DataDirectory, "Billing");

        private readonly Catalog _catalog;

        public BillingService()
        {
            _catalog = FileHelper.DeserializeJson<Catalog>(Path.Combine(BillingDataDirectory, "Catalog.json"));

            // Apply a patch to the catalog if it's enabled and there's one
            if (ConfigManager.Billing.ApplyCatalogPatch)
            {
                string patchPath = Path.Combine(BillingDataDirectory, "CatalogPatch.json");
                if (File.Exists(patchPath))
                {
                    CatalogEntry[] catalogPatch = FileHelper.DeserializeJson<CatalogEntry[]>(patchPath);
                    _catalog.ApplyPatch(catalogPatch);
                }
            }

            // Override store urls if enabled
            if (ConfigManager.Billing.OverrideStoreUrls)
            {
                _catalog.Urls[0].StoreHomePageUrl = ConfigManager.Billing.StoreHomePageUrl;
                _catalog.Urls[0].StoreBannerPageUrls[0].Url = ConfigManager.Billing.StoreHomeBannerPageUrl;
                _catalog.Urls[0].StoreBannerPageUrls[1].Url = ConfigManager.Billing.StoreHeroesBannerPageUrl;
                _catalog.Urls[0].StoreBannerPageUrls[2].Url = ConfigManager.Billing.StoreCostumesBannerPageUrl;
                _catalog.Urls[0].StoreBannerPageUrls[3].Url = ConfigManager.Billing.StoreBoostsBannerPageUrl;
                _catalog.Urls[0].StoreBannerPageUrls[4].Url = ConfigManager.Billing.StoreChestsBannerPageUrl;
                _catalog.Urls[0].StoreBannerPageUrls[5].Url = ConfigManager.Billing.StoreSpecialsBannerPageUrl;
                _catalog.Urls[0].StoreRealMoneyUrl = ConfigManager.Billing.StoreRealMoneyUrl;
            }

            Logger.Info($"Initialized store catalog with {_catalog.Entries.Length} entries");
        }

        public void Handle(FrontendClient client, GameMessage message)
        {
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
                    Logger.Warn($"Received unhandled message {(ClientToGameServerMessage)message.Id} (id {message.Id})");
                    break;
            }
        }

        public void Handle(FrontendClient client, IEnumerable<GameMessage> messages)
        {
            foreach (GameMessage message in messages) Handle(client, message);
        }

        private void OnGetCatalog(FrontendClient client, NetMessageGetCatalog getCatalog)
        {
            // Bail out if the client already has an up to date catalog
            if (getCatalog.TimestampSeconds == _catalog.TimestampSeconds && getCatalog.TimestampMicroseconds == _catalog.TimestampMicroseconds)
                return;

            // Send the current catalog
            client.SendMessage(MuxChannel, new(_catalog.ToNetMessageCatalogItems(false)));
        }

        private void OnGetCurrencyBalance(FrontendClient client)
        {
            client.SendMessage(MuxChannel, new(NetMessageGetCurrencyBalanceResponse.CreateBuilder()
                .SetCurrencyBalance(ConfigManager.Billing.CurrencyBalance)
                .Build()));
        }

        private void OnBuyItemFromCatalog(FrontendClient client, NetMessageBuyItemFromCatalog buyItemFromCatalog)
        {
            Logger.Info($"Received NetMessageBuyItemFromCatalog");
            Logger.Trace(buyItemFromCatalog.ToString());

            // HACK: change costume when a player "buys" a costume
            DBAvatar currentAvatar = client.Session.Account.CurrentAvatar;

            CatalogEntry entry = _catalog.GetEntry(buyItemFromCatalog.SkuId);
            if (entry == null || entry.GuidItems.Length == 0)
            {
                SendBuyItemResponse(client, false, BuyItemResultErrorCodes.BUY_RESULT_ERROR_UNKNOWN, buyItemFromCatalog.SkuId);
                return;
            }

            var costumePrototype = entry.GuidItems[0].ItemPrototypeRuntimeIdForClient.As<CostumePrototype>();
            if (costumePrototype == null || costumePrototype.UsableBy != (PrototypeId)currentAvatar.Prototype)
            {
                SendBuyItemResponse(client, false, BuyItemResultErrorCodes.BUY_RESULT_ERROR_UNKNOWN, buyItemFromCatalog.SkuId);
                return;
            }

            // Get replication id for the client avatar
            ulong replicationId = (ulong)currentAvatar.Prototype.ToPropertyCollectionReplicationId();

            currentAvatar.Costume = (ulong)costumePrototype.DataRef;

            // Send NetMessageSetProperty message with a CostumeCurrent property for the purchased costume
            client.SendMessage(MuxChannel, new(
                Property.ToNetMessageSetProperty(replicationId, new(PropertyEnum.CostumeCurrent), entry.GuidItems[0].ItemPrototypeRuntimeIdForClient)
                ));

            // Update library
            PropertyParam enumValue = Property.ToParam(PropertyEnum.AvatarLibraryCostume, 1, (PrototypeId)currentAvatar.Prototype);

            client.SendMessage(MuxChannel, new(
                Property.ToNetMessageSetProperty(9078332, new(PropertyEnum.AvatarLibraryCostume, 0, enumValue), costumePrototype.DataRef)));

            SendBuyItemResponse(client, true, BuyItemResultErrorCodes.BUY_RESULT_ERROR_SUCCESS, buyItemFromCatalog.SkuId);
        }

        private void SendBuyItemResponse(FrontendClient client, bool didSucceed, BuyItemResultErrorCodes errorCode, long skuId)
        {
            client.SendMessage(MuxChannel, new(NetMessageBuyItemFromCatalogResponse.CreateBuilder()
                .SetDidSucceed(didSucceed)
                .SetCurrentCurrencyBalance(ConfigManager.Billing.CurrencyBalance)
                .SetErrorcode(errorCode)
                .SetSkuId(skuId)
                .Build()));
        }
    }
}
