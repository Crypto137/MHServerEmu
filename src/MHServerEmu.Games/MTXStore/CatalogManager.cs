using Gazillion;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.MTXStore.Catalogs;
using MHServerEmu.Games.Social.Communities;

namespace MHServerEmu.Games.MTXStore
{
    public class CatalogManager
    {
        private const int GiftMessageMaxLength = 100;   // matching the client-side limit

        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly string MTXStoreDataDirectory = Path.Combine(FileHelper.DataDirectory, "Game", "MTXStore");

        private readonly Catalog _catalog = new();

        private long _giftingOmegaLevelRequired = 0;
        private long _giftingInfinityLevelRequired = 0;
        private ulong _currentGiftId = 1;   // used by the client to differentiate notifications

        public static CatalogManager Instance { get; } = new();

        private CatalogManager() { }

        public bool Initialize()
        {
            if (_catalog.Count != 0)
                return false;

            _catalog.Initialize();
            LoadEntries();

            var config = ConfigManager.Instance.GetConfig<MTXStoreConfig>();
            _giftingOmegaLevelRequired = config.GiftingOmegaLevelRequired;
            _giftingInfinityLevelRequired = config.GiftingInfinityLevelRequired;

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
            long clientPrice = buyItemFromCatalog.ItemUnitPrice;

            // In normal non-gift purchases the buyer is the recipient
            BuyItemResultErrorCodes result = BuyItem(player, player, skuId, clientPrice);

            player.SendMessage(NetMessageBuyItemFromCatalogResponse.CreateBuilder()
                .SetDidSucceed(result == BuyItemResultErrorCodes.BUY_RESULT_ERROR_SUCCESS)
                .SetCurrentCurrencyBalance(player.GazillioniteBalance)
                .SetErrorcode(result)
                .SetSkuId(skuId)
                .Build());

            return true;
        }

        public bool OnBuyGiftForOtherPlayer(Player buyer, NetMessageBuyGiftForOtherPlayer buyGiftForOtherPlayer)
        {
            if (buyGiftForOtherPlayer.HasSkuId == false)
                return Logger.WarnReturn(false, $"OnBuyGiftForOtherPlayer(): No SkuId received from player [{buyer}]");

            long skuId = buyGiftForOtherPlayer.SkuId;
            long clientPrice = buyGiftForOtherPlayer.ItemUnitPrice;
            string recipientName = buyGiftForOtherPlayer.RecipientName;
            string giftMessage = buyGiftForOtherPlayer.HasGiftMessage ? buyGiftForOtherPlayer.GiftMessage : null;

            Game game = buyer.Game;

            if (game.GiftingEnabled == false || buyer.IsGiftingAllowed() == false)
            {
                SendBuyGiftForOtherPlayerResponse(buyer, skuId, BuyItemResultErrorCodes.BUY_RESULT_ERROR_GIFTING_UNAVAILABLE);
                return false;
            }

            // CUSTOM: Check Omega/Infinity level requirement
            if (game.InfinitySystemEnabled)
            {
                if (_giftingInfinityLevelRequired > 0 && buyer.GetTotalInfinityPoints() < _giftingInfinityLevelRequired)
                {
                    SendBuyGiftForOtherPlayerResponse(buyer, skuId, BuyItemResultErrorCodes.BUY_RESULT_ERROR_GIFTING_UNAVAILABLE);
                    game.ChatManager.SendChatFromCustomSystem(buyer, $"Infinity level {_giftingInfinityLevelRequired} is required to send gifts.");
                    return false;
                }
            }
            else 
            {
                if (_giftingOmegaLevelRequired > 0 && buyer.GetOmegaPoints() < _giftingOmegaLevelRequired)
                {
                    SendBuyGiftForOtherPlayerResponse(buyer, skuId, BuyItemResultErrorCodes.BUY_RESULT_ERROR_GIFTING_UNAVAILABLE);
                    game.ChatManager.SendChatFromCustomSystem(buyer, $"Omega level {_giftingOmegaLevelRequired} is required to send gifts.");
                    return false;
                }
            }

            if (giftMessage != null && giftMessage.Length > GiftMessageMaxLength)
            {
                SendBuyGiftForOtherPlayerResponse(buyer, skuId, BuyItemResultErrorCodes.BUY_RESULT_ERROR_GIFT_MESSAGE_TOO_LONG);
                return false;
            }

            // Currently we allow only local synchronous gifts to nearby players.
            Player recipient = game.EntityManager.GetPlayerByName(recipientName);

            Community community = buyer.Community;
            CommunityMember recipientMember = community.GetMemberByName(recipientName);
            CommunityCircle nearbyCircle = community.GetCircle(CircleId.__Nearby);

            if (recipient == null || recipientMember == null || recipientMember.IsInCircle(nearbyCircle) == false)
            {
                SendBuyGiftForOtherPlayerResponse(buyer, skuId, BuyItemResultErrorCodes.BUY_RESULT_ERROR_UNKNOWN_RECIPIENT);
                game.ChatManager.SendChatFromCustomSystem(buyer, $"Player {recipientName} not found. You must be near the recipient player to send gifts.");
                return false;
            }

            if (recipient == buyer)
            {
                SendBuyGiftForOtherPlayerResponse(buyer, skuId, BuyItemResultErrorCodes.BUY_RESULT_ERROR_UNKNOWN);
                game.ChatManager.SendChatFromCustomSystem(buyer, $"You cannot purchase gifts for yourself.");
                return false;
            }

            // All good, do the purchase.
            BuyItemResultErrorCodes result = BuyItem(buyer, recipient, skuId, clientPrice);
            SendBuyGiftForOtherPlayerResponse(buyer, skuId, result);

            // Notify the recipient if successful.
            if (result == BuyItemResultErrorCodes.BUY_RESULT_ERROR_SUCCESS)
            {
                var giftNotification = NetMessageReceivedGift.CreateBuilder()
                    .SetSkuId((ulong)skuId)
                    .SetTransId(Interlocked.Increment(ref _currentGiftId))
                    .SetSender(buyer.GetName());

                if (giftMessage != null)
                    giftNotification.SetMessage(giftMessage);

                recipient.SendMessage(giftNotification.Build());
            }

            return true;
        }

        private static void SendBuyGiftForOtherPlayerResponse(Player buyer, long skuId, BuyItemResultErrorCodes result)
        {
            buyer.SendMessage(NetMessageBuyGiftForOtherPlayerResponse.CreateBuilder()
                .SetDidSucceed(result == BuyItemResultErrorCodes.BUY_RESULT_ERROR_SUCCESS)
                .SetCurrentCurrencyBalance(buyer.GazillioniteBalance)
                .SetErrorcode(result)
                .SetSkuid(skuId)
                .Build());
        }

        #endregion

        private BuyItemResultErrorCodes BuyItem(Player buyer, Player recipient, long skuId, long clientPrice)
        {
            // Make sure the player has already finished the tutorial, which could unlock characters depending on server settings.
            if (buyer.HasFinishedTutorial() == false)
                return BuyItemResultErrorCodes.BUY_RESULT_ERROR_UNKNOWN;

            // Validate the order
            CatalogEntry entry = null;

            lock (_catalog)
                entry = _catalog.GetEntry(skuId);

            if (entry == null || entry.GuidItems.IsNullOrEmpty() || entry.LocalizedEntries.IsNullOrEmpty())
                return BuyItemResultErrorCodes.BUY_RESULT_ERROR_UNKNOWN;

            LocalizedCatalogEntry localizedEntry = entry.LocalizedEntries[0];
            long itemPrice = localizedEntry.ItemPrice;

            // Do not allow the purchase if the price changed since the client requested it.
            if (clientPrice != itemPrice)
                return BuyItemResultErrorCodes.BUY_RESULT_ERROR_PRICE_MISMATCH;

            long balance = buyer.GazillioniteBalance;
            if (itemPrice > balance)
                return BuyItemResultErrorCodes.BUY_RESULT_ERROR_INSUFFICIENT_BALANCE;

            if (entry.GuidItems.Length == 1)
            {
                // For individual purchases it's all or nothing with early out if failed to fulfill.
                BuyItemResultErrorCodes result = AcquireCatalogGuid(recipient, entry.GuidItems[0], false);
                if (result != BuyItemResultErrorCodes.BUY_RESULT_ERROR_SUCCESS)
                    return result;
            }
            else
            {
                // Allow partial fulfillment of bundles (e.g. stash already owned)
                foreach (CatalogGuidEntry catalogItemEntry in entry.GuidItems)
                {
                    BuyItemResultErrorCodes result = AcquireCatalogGuid(recipient, catalogItemEntry, true);
                    switch (result)
                    {
                        case BuyItemResultErrorCodes.BUY_RESULT_ERROR_SUCCESS:
                        case BuyItemResultErrorCodes.BUY_RESULT_ERROR_ALREADY_HAVE_STASH_INV:
                        case BuyItemResultErrorCodes.BUY_RESULT_ERROR_ALREADY_HAVE_PERMABUFF:
                            // this is fine
                            break;

                        default:
                            // this is not fine
                            Logger.Warn($"BuyItem(): Partial fulfillment of sku! skuId={skuId}, entry={catalogItemEntry}, buyer=[{buyer}], recipient=[{recipient}]", LogCategory.MTXStore);
                            break;
                    }
                }
            }

            // Adjust currency balance (do not allow negative balance in case somebody figures out some kind of exploit to get here)
            balance = Math.Max(balance - itemPrice, 0);
            buyer.GazillioniteBalance = balance;
            Logger.Trace($"OnBuyItemFromCatalog(): Player [{buyer}] purchased [skuId={skuId}, itemPrice={itemPrice}] for recipient [{recipient}]. Balance={balance}", LogCategory.MTXStore);

            return BuyItemResultErrorCodes.BUY_RESULT_ERROR_SUCCESS;
        }

        private static BuyItemResultErrorCodes AcquireCatalogGuid(Player player, CatalogGuidEntry guidEntry, bool allowTokenReplacements)
        {
            Prototype proto = guidEntry.ItemPrototypeRuntimeIdForClient.As<Prototype>();
            if (proto == null) return Logger.WarnReturn(BuyItemResultErrorCodes.BUY_RESULT_ERROR_UNKNOWN, "AcquireCatalogItem(): proto == null");

            for (int i = 0; i < guidEntry.Quantity; i++)
            {
                BuyItemResultErrorCodes result;

                switch (proto)
                {
                    case ItemPrototype itemProto:
                        result = AcquireItem(player, itemProto);
                        break;

                    case PlayerStashInventoryPrototype playerStashInventoryProto:
                        result = AcquirePlayerStashInventory(player, playerStashInventoryProto);
                        break;

                    case AvatarPrototype avatarProto:
                        result = AcquireAvatar(player, avatarProto, allowTokenReplacements);
                        break;

                    case AgentTeamUpPrototype teamUpProto:
                        result = AcquireTeamUp(player, teamUpProto, allowTokenReplacements);
                        break;

                    case PowerSpecPrototype powerSpecProto:
                        result = AcquirePowerSpec(player, powerSpecProto);
                        break;

                    default:
                        Logger.Warn($"AcquireCatalogItem(): Unimplemented catalog item type {proto.GetType().Name} for {proto}", LogCategory.MTXStore);
                        result = BuyItemResultErrorCodes.BUY_RESULT_ERROR_UNKNOWN;
                        break;
                }

                if (result != BuyItemResultErrorCodes.BUY_RESULT_ERROR_SUCCESS)
                    return result;
            }

            return BuyItemResultErrorCodes.BUY_RESULT_ERROR_SUCCESS;
        }

        private static BuyItemResultErrorCodes AcquireItem(Player player, ItemPrototype itemProto)
        {
            if (player.Game.LootManager.GiveItem(itemProto.DataRef, LootContext.CashShop, player) == false)
                return BuyItemResultErrorCodes.BUY_RESULT_ERROR_UNKNOWN;

            return BuyItemResultErrorCodes.BUY_RESULT_ERROR_SUCCESS;
        }

        private static BuyItemResultErrorCodes AcquirePlayerStashInventory(Player player, PlayerStashInventoryPrototype playerStashInventoryProto)
        {
            if (player.IsInventoryUnlocked(playerStashInventoryProto.DataRef))
                return BuyItemResultErrorCodes.BUY_RESULT_ERROR_ALREADY_HAVE_STASH_INV;

            if (player.UnlockInventory(playerStashInventoryProto.DataRef) == false)
                return BuyItemResultErrorCodes.BUY_RESULT_ERROR_UNKNOWN;

            return BuyItemResultErrorCodes.BUY_RESULT_ERROR_SUCCESS;
        }

        private static BuyItemResultErrorCodes AcquireAvatar(Player player, AvatarPrototype avatarProto, bool allowTokenReplacements)
        {
            PrototypeId avatarProtoRef = avatarProto.DataRef;

            // Replace with token and starting costume if we are purchasing a bundle and we already have the hero.
            if (player.HasAvatarFullyUnlocked(avatarProtoRef))
            {
                if (allowTokenReplacements == false)
                    return BuyItemResultErrorCodes.BUY_RESULT_ERROR_ALREADY_HAVE_AVATAR;

                CharacterTokenPrototype tokenProto = GetCharacterTokenPrototype(avatarProtoRef);
                if (tokenProto == null)
                    return BuyItemResultErrorCodes.BUY_RESULT_ERROR_ALREADY_HAVE_AVATAR;

                CostumePrototype costumeProto = avatarProto.GetStartingCostumeForPlatform(Platforms.PC).As<CostumePrototype>();
                if (costumeProto == null)
                    return BuyItemResultErrorCodes.BUY_RESULT_ERROR_ALREADY_HAVE_AVATAR;

                var result = AcquireItem(player, tokenProto);
                if (result != BuyItemResultErrorCodes.BUY_RESULT_ERROR_SUCCESS)
                    return result;

                return AcquireItem(player, costumeProto);
            }

            // Unlock the avatar.
            if (player.UnlockAvatar(avatarProtoRef, true) == false)
                return BuyItemResultErrorCodes.BUY_RESULT_ERROR_UNKNOWN;

            return BuyItemResultErrorCodes.BUY_RESULT_ERROR_SUCCESS;
        }

        private static BuyItemResultErrorCodes AcquireTeamUp(Player player, AgentTeamUpPrototype teamUpProto, bool allowTokenReplacements)
        {
            PrototypeId teamUpProtoRef = teamUpProto.DataRef;

            // Replace with token if we are purchasing a bundle and we already have the hero.
            if (player.IsTeamUpAgentUnlocked(teamUpProtoRef))
            {
                if (allowTokenReplacements == false)
                    return BuyItemResultErrorCodes.BUY_RESULT_ERROR_ALREADY_HAVE_AVATAR;

                CharacterTokenPrototype tokenProto = GetCharacterTokenPrototype(teamUpProtoRef, CharacterTokenType.None);
                if (tokenProto == null)
                    return BuyItemResultErrorCodes.BUY_RESULT_ERROR_ALREADY_HAVE_AVATAR;

                return AcquireItem(player, tokenProto);
            }

            if (player.UnlockTeamUpAgent(teamUpProtoRef, true) == false)
                return BuyItemResultErrorCodes.BUY_RESULT_ERROR_UNKNOWN;

            return BuyItemResultErrorCodes.BUY_RESULT_ERROR_SUCCESS;
        }

        private static BuyItemResultErrorCodes AcquirePowerSpec(Player player, PowerSpecPrototype powerSpecProto)
        {
            if (player.UnlockPowerSpecIndex(powerSpecProto.Index) == false)
                return BuyItemResultErrorCodes.BUY_RESULT_ERROR_UNKNOWN;

            return BuyItemResultErrorCodes.BUY_RESULT_ERROR_SUCCESS;
        }

        private static CharacterTokenPrototype GetCharacterTokenPrototype(PrototypeId agentProtoRef)
        {
            // Prefer UnlockCharOrUpgradeUlt tokens if available.
            CharacterTokenPrototype tokenProto = GetCharacterTokenPrototype(agentProtoRef, CharacterTokenType.UnlockCharOrUpgradeUlt);

            // Fall back to UpgradeUltimateOnly tokens for "removed" heroes.
            if (tokenProto == null)
                return GetCharacterTokenPrototype(agentProtoRef, CharacterTokenType.UpgradeUltimateOnly);

            return tokenProto;
        }

        private static CharacterTokenPrototype GetCharacterTokenPrototype(PrototypeId agentProtoRef, CharacterTokenType tokenType)
        {
            foreach (PrototypeId tokenProtoRef in DataDirectory.Instance.IteratePrototypesInHierarchy<CharacterTokenPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                CharacterTokenPrototype tokenProto = tokenProtoRef.As<CharacterTokenPrototype>();

                if (tokenProto.Character != agentProtoRef)
                    continue;

                if (tokenType != CharacterTokenType.None && tokenProto.TokenType != tokenType)
                    continue;

                ItemCostPrototype itemCostProto = tokenProto.Cost;

                if (itemCostProto == null)
                    continue;

                if (itemCostProto.HasEternitySplintersComponent() == false)
                    continue;

                return tokenProto;
            }

            return null;
        }
    }
}
