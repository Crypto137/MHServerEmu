using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.MTXStore.Catalogs;

namespace MHServerEmu.Games.MTXStore
{
    public class CatalogManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly string MTXStoreDataDirectory = Path.Combine(FileHelper.DataDirectory, "Game", "MTXStore");

        private readonly Catalog _catalog = new();

        public static CatalogManager Instance { get; } = new();

        private CatalogManager() { }

        public bool Initialize()
        {
            if (_catalog.Count != 0)
                return false;

            _catalog.Initialize();
            LoadEntries();

            return true;
        }

        public void LoadEntries()
        {
            lock (_catalog)
            {
                _catalog.ClearEntries();

                foreach (string filePath in FileHelper.GetFilesWithPrefix(MTXStoreDataDirectory, "Catalog", "json"))
                {
                    CatalogEntry[] entries = FileHelper.DeserializeJson<CatalogEntry[]>(filePath);
                    _catalog.AddEntries(entries);
                    Logger.Trace($"Parsed catalog entries from {Path.GetFileName(filePath)}");
                }

                Logger.Info($"Loaded {_catalog.Count} store catalog entries");
            }
        }

        #region Message Handling

        public bool OnGetCatalog(Player player, NetMessageGetCatalog getCatalog)
        {
            // Send the catalog only if the client is out of date.
            TimeSpan clientTimestamp = TimeSpan.FromMicroseconds(getCatalog.TimestampSeconds * 1000000 + getCatalog.TimestampMicroseconds);

            lock (_catalog)
            {
                if (clientTimestamp != _catalog.Timestamp)
                    player.SendMessage(_catalog.ToProtobuf());
            }

            return true;
        }

        public bool OnGetCurrencyBalance(Player player)
        {
            player.SendMessage(NetMessageGetCurrencyBalanceResponse.CreateBuilder()
                .SetCurrencyBalance(player.GazillioniteBalance)
                .Build());

            return true;
        }

        public bool OnBuyItemFromCatalog(Player player, NetMessageBuyItemFromCatalog buyItemFromCatalog)
        {
            if (buyItemFromCatalog.HasSkuId == false)
                return Logger.WarnReturn(false, $"OnBuyItemFromCatalog(): No SkuId received from player [{player}]");

            long skuId = buyItemFromCatalog.SkuId;

            BuyItemResultErrorCodes result = BuyItem(player, skuId);

            player.SendMessage(NetMessageBuyItemFromCatalogResponse.CreateBuilder()
                .SetDidSucceed(result == BuyItemResultErrorCodes.BUY_RESULT_ERROR_SUCCESS)
                .SetCurrentCurrencyBalance(player.GazillioniteBalance)
                .SetErrorcode(result)
                .SetSkuId(skuId)
                .Build());

            return true;
        }

        public bool OnBuyGiftForOtherPlayer(Player player, NetMessageBuyGiftForOtherPlayer buyGiftForOtherPlayer)
        {
            if (buyGiftForOtherPlayer.HasSkuId == false)
                return Logger.WarnReturn(false, $"OnBuyGiftForOtherPlayer(): No SkuId received from player [{player}]");

            long skuId = buyGiftForOtherPlayer.SkuId;

            // TODO: actual gifting
            BuyItemResultErrorCodes result = BuyItemResultErrorCodes.BUY_RESULT_ERROR_GIFTING_UNAVAILABLE;
            player.Game.ChatManager.SendChatFromCustomSystem(player, "Gifting is currently unavailable.");

            player.SendMessage(NetMessageBuyGiftForOtherPlayerResponse.CreateBuilder()
                .SetDidSucceed(result == BuyItemResultErrorCodes.BUY_RESULT_ERROR_SUCCESS)
                .SetCurrentCurrencyBalance(player.GazillioniteBalance)
                .SetErrorcode(result)
                .SetSkuid(skuId)
                .Build());

            return true;
        }

        #endregion

        private BuyItemResultErrorCodes BuyItem(Player player, long skuId)
        {
            BuyItemResultErrorCodes result = BuyItemResultErrorCodes.BUY_RESULT_ERROR_UNKNOWN;

            // Make sure the player has already finished the tutorial, which could unlock characters depending on server settings.
            if (player.HasFinishedTutorial() == false)
                return result;

            // Validate the order
            CatalogEntry entry = null;

            lock (_catalog)
                entry = _catalog.GetEntry(skuId);

            if (entry == null || entry.GuidItems.Length == 0)
                return result;

            // Bundles don't work properly yet, so disable them for now
            if (entry.Type?.Name == "Bundle")
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

                case PowerSpecPrototype powerSpecProto:
                    if (player.UnlockPowerSpecIndex(powerSpecProto.Index))
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
    }
}
