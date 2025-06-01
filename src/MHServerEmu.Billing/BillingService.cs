using Gazillion;
using MHServerEmu.Billing.Catalogs;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Frontend;
using MHServerEmu.Games;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.MTXStore;
using MHServerEmu.Games.Network;
using MHServerEmu.PlayerManagement;

namespace MHServerEmu.Billing
{
    // TODO: Move message handling / order fullfillment to Games

    public class BillingService : IGameService
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly string BillingDataDirectory = Path.Combine(FileHelper.DataDirectory, "Billing");

        private readonly Catalog _catalog;

        public BillingService()
        {
            var config = ConfigManager.Instance.GetConfig<BillingConfig>();

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

        public void ReceiveServiceMessage<T>(in T message) where T : struct, IGameServiceMessage
        {
            switch (message)
            {
                case GameServiceProtocol.RouteMessage routeMailboxMessage:
                    OnRouteMailboxMessage(routeMailboxMessage);
                    break;

                default:
                    Logger.Warn($"ReceiveServiceMessage(): Unhandled service message type {typeof(T).Name}");
                    break;
            }
        }

        public string GetStatus()
        {
            return $"Catalog Entries: {_catalog.Entries.Length}";
        }

        private void OnRouteMailboxMessage(in GameServiceProtocol.RouteMessage routeMailboxMessage)
        {
            FrontendClient client = (FrontendClient)routeMailboxMessage.Client;
            MailboxMessage message = routeMailboxMessage.Message;

            // This is pretty rough, we need a better way of handling this
            // TODO: Move this to Games, use BillingService just as a source for catalog data
            PlayerManagerService playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as PlayerManagerService;
            Game game = playerManager.GetGameByPlayer(client);
            PlayerConnection playerConnection = game.NetworkManager.GetNetClient(client);
            Player player = playerConnection.Player;

            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageGetCatalog:            OnGetCatalog(player, message); break;           // 68
                case ClientToGameServerMessage.NetMessageGetCurrencyBalance:    OnGetCurrencyBalance(player, message); break;   // 69
                case ClientToGameServerMessage.NetMessageBuyItemFromCatalog:    OnBuyItemFromCatalog(player, message); break;   // 70

                default: Logger.Warn($"Handle(): Unhandled {(ClientToGameServerMessage)message.Id} [{message.Id}]"); break;
            }
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
                .SetCurrencyBalance(player.GazillioniteBalance)
                .Build());
        }

        private bool OnBuyItemFromCatalog(Player player, MailboxMessage message)
        {
            var buyItemFromCatalog = message.As<NetMessageBuyItemFromCatalog>();
            if (buyItemFromCatalog == null) return Logger.WarnReturn(false, $"OnBuyItemFromCatalog(): Failed to retrieve message");

            long skuId = buyItemFromCatalog.SkuId;
            BuyItemResultErrorCodes result = BuyItem(player, skuId);
            SendBuyItemResponse(player, result, skuId);
            return true;
        }

        private BuyItemResultErrorCodes BuyItem(Player player, long skuId)
        {
            BuyItemResultErrorCodes result = BuyItemResultErrorCodes.BUY_RESULT_ERROR_UNKNOWN;

            // Make sure the player has already finished the tutorial, which could unlock characters depending on server settings.
            if (player.HasFinishedTutorial() == false)
                return result;

            // Validate the order
            CatalogEntry entry = _catalog.GetEntry(skuId);
            if (entry == null || entry.GuidItems.Length == 0)
                return result;

            if (entry.LocalizedEntries.IsNullOrEmpty())
                return result;

            LocalizedCatalogEntry localizedEntry = entry.LocalizedEntries[0];
            long itemPrice = localizedEntry.ItemPrice;

            long balance = player.GazillioniteBalance;
            if (itemPrice > balance)
                return BuyItemResultErrorCodes.BUY_RESULT_ERROR_INSUFFICIENT_BALANCE;

            Prototype catalogItemProto = entry.GuidItems[0].ItemPrototypeRuntimeIdForClient.As<Prototype>();
            if (catalogItemProto == null)
                return result;

            // Fullfill
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

                case AvatarPrototype avatarProto:
                    PrototypeId avatarProtoRef = avatarProto.DataRef;
                    if (player.HasAvatarFullyUnlocked(avatarProtoRef))
                        result = BuyItemResultErrorCodes.BUY_RESULT_ERROR_ALREADY_HAVE_AVATAR;
                    else if (player.UnlockAvatar(avatarProtoRef, true))
                        result = BuyItemResultErrorCodes.BUY_RESULT_ERROR_SUCCESS;
                    break;

                case AgentTeamUpPrototype teamUpProto:
                    PrototypeId teamUpProtoRef = teamUpProto.DataRef;
                    if (player.IsTeamUpAgentUnlocked(teamUpProtoRef))
                        result = BuyItemResultErrorCodes.BUY_RESULT_ERROR_ALREADY_HAVE_AVATAR;
                    else if (player.UnlockTeamUpAgent(teamUpProtoRef, true))
                        result = BuyItemResultErrorCodes.BUY_RESULT_ERROR_SUCCESS;
                    break;

                default:
                    // Return error for unhandled SKU types
                    Logger.Warn($"OnBuyItemFromCatalog(): Unimplemented catalog item type {catalogItemProto.GetType().Name} for {catalogItemProto}", LogCategory.MTXStore);
                    break;
            }

            if (result != BuyItemResultErrorCodes.BUY_RESULT_ERROR_SUCCESS)
                return result;

            // Adjust currency balance (do not allow negative balance in case somebody figures out some kind of exploit to get here)
            balance = Math.Max(balance - itemPrice, 0);
            player.GazillioniteBalance = balance;
            Logger.Trace($"OnBuyItemFromCatalog(): Player [{player}] purchased [skuId={skuId}, catalogItemProto={catalogItemProto}, itemPrice={itemPrice}]. Balance={balance}", LogCategory.MTXStore);

            return BuyItemResultErrorCodes.BUY_RESULT_ERROR_SUCCESS;
        }

        private void SendBuyItemResponse(Player player, BuyItemResultErrorCodes errorCode, long skuId)
        {
            player.SendMessage(NetMessageBuyItemFromCatalogResponse.CreateBuilder()
                .SetDidSucceed(errorCode == BuyItemResultErrorCodes.BUY_RESULT_ERROR_SUCCESS)
                .SetCurrentCurrencyBalance(player.GazillioniteBalance)
                .SetErrorcode(errorCode)
                .SetSkuId(skuId)
                .Build());
        }
    }
}
