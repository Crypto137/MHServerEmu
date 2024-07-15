using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Loot
{
    /// <summary>
    /// A basic implementation of <see cref="IItemResolver"/>.
    /// </summary>
    public class ItemResolver : IItemResolver
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Picker<AvatarPrototype> _avatarPicker;

        private readonly List<ItemSpec> _pendingItemList = new();
        private readonly List<ItemSpec> _processedItemList = new();

        public GRandom Random { get; }
        public LootContext LootContext { get; private set; }
        public Player Player { get; private set; }

        public IEnumerable<ItemSpec> ProcessedItems { get => _processedItemList; }
        public int ProcessedItemCount { get => _processedItemList.Count; }

        public ItemResolver(GRandom random)
        {
            Random = random;

            _avatarPicker = new(random);
            foreach (PrototypeId avatarProtoRef in DataDirectory.Instance.IteratePrototypesInHierarchy<AvatarPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                // We could filter by avatarProto.ShowInRosterIfLocked instead of hardcoding,
                // but then we won't get random drops for "removed" heroes.
                if (avatarProtoRef == (PrototypeId)6044485448390219466) continue;   //zzzBrevikOLD.prototype

                AvatarPrototype avatarProto = avatarProtoRef.As<AvatarPrototype>();
                _avatarPicker.Add(avatarProto);
            }
        }

        public void SetContext(LootContext lootContext, Player player)
        {
            _pendingItemList.Clear();
            _processedItemList.Clear();

            LootContext = lootContext;
            Player = player;
        }

        public LootRollResult PushItem(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags, int stackCount, IEnumerable<LootMutationPrototype> mutations)
        {
            ItemSpec itemSpec = new(filterArgs.ItemProto.DataRef, filterArgs.Rarity, filterArgs.Level,
                0, Array.Empty<AffixSpec>(), Random.Next(), PrototypeId.Invalid);

            _pendingItemList.Add(itemSpec);

            return LootRollResult.Success;
        }

        public LootRollResult PushCurrency(WorldEntityPrototype worldEntityProto, DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags,
            LootDropChanceModifiers dropChanceModifiers, int stackCount)
        {
            //Logger.Debug($"PushCurrency(): {worldEntityProto}");
            return LootRollResult.NoRoll;
        }

        public void PushLootNodeCallback()
        {
            Logger.Debug($"PushLootNodeCallback()");
        }

        public void PushCraftingCallback()
        {
            Logger.Debug($"PushCraftingCallback()");
        }

        public int ResolveLevel(int level, bool useLevelVerbatim)
        {
            // NOTE: In version 1.52 MobLevelToItemLevel.curve is empty, so we can always treat this as if useLevelVerbatim is set.
            // If we were to support older versions (most likely predating level scaling) we would have to properly implement this.
            return level;
        }

        public AvatarPrototype ResolveAvatarPrototype(AvatarPrototype usableAvatarProto, bool forceUsable, float usablePercent)
        {
            // Check if we can just use the provided avatar proto as is
            if (usableAvatarProto != null && (forceUsable || Random.NextFloat() < usablePercent))
                return usableAvatarProto;

            // Pick a random avatar otherwise
            return _avatarPicker.Pick();
        }

        public AgentPrototype ResolveTeamUpPrototype(AgentPrototype usableTeamUpProto, float usablePercent)
        {
            return usableTeamUpProto;
        }

        public PrototypeId ResolveRarity(HashSet<PrototypeId> rarityFilter, int level, ItemPrototype itemProto)
        {
            Picker<PrototypeId> rarityPicker = new(Random);

            DropFilterArguments filterArgs = itemProto != null ? new() : null;

            foreach (PrototypeId rarityProtoRef in DataDirectory.Instance.IteratePrototypesInHierarchy<RarityPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                // Skip rarities that don't match the provided filter
                if (rarityFilter.Count > 0 && rarityFilter.Contains(rarityProtoRef) == false)
                    continue;

                // Skip rarities that don't match the provided item prototype
                if (itemProto != null)
                {
                    filterArgs.Rarity = rarityProtoRef;
                    if (itemProto.IsDroppableForRestrictions(filterArgs, RestrictionTestFlags.Rarity) == false)
                        continue;
                }

                RarityPrototype rarityProto = rarityProtoRef.As<RarityPrototype>();
                if (rarityProto == null)
                {
                    Logger.Warn("ResolveRarity(): rarityProto == null");
                    continue;
                }

                rarityPicker.Add(rarityProtoRef, (int)rarityProto.GetWeight(level));
            }

            if (rarityPicker.GetNumElements() == 0)
                return PrototypeId.Invalid;

            return rarityPicker.Pick();
        }

        public bool CheckDropPercent(LootRollSettings settings, float noDropPercent)
        {
            return Random.NextFloat() < 1f - noDropPercent;
        }

        public bool CheckItem(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags, bool arg2)
        {
            ItemPrototype itemProto = filterArgs.ItemProto as ItemPrototype;
            if (itemProto == null) return Logger.WarnReturn(false, $"CheckItem(): itemProto == null");

            if (itemProto.ApprovedForUse() == false)
                return false;

            if (itemProto.IsDroppableForRestrictions(filterArgs, restrictionFlags) == false)
                return false;

            return true;
        }

        public void ClearPending()
        {
            _pendingItemList.Clear();
        }

        public bool ProcessPending(LootRollSettings settings)
        {
            foreach (ItemSpec itemSpec in _pendingItemList)
                _processedItemList.Add(itemSpec);

            _pendingItemList.Clear();
            return true;
        }
    }
}
