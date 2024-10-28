using Gazillion;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot.Specs;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Loot
{
    /// <summary>
    /// A general-purpose implementation of <see cref="IItemResolver"/>.
    /// </summary>
    public class ItemResolver : IItemResolver
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Picker<AvatarPrototype> _avatarPicker;

        private readonly int _itemLevelMin;
        private readonly int _itemLevelMax;

        private readonly List<PendingItem> _pendingItemList = new();
        private readonly List<LootResult> _processedItemList = new();

        private readonly ItemResolverContext _context = new();

        public GRandom Random { get; }

        public LootContext LootContext { get => _context.LootContext; }
        public Player Player { get => _context.Player; }
        public Region Region { get => _context.Region; }

        public ItemResolver(GRandom random)
        {
            Random = random;

            // Cache avatar picker for smart loot
            _avatarPicker = new(random);
            foreach (PrototypeId avatarProtoRef in DataDirectory.Instance.IteratePrototypesInHierarchy<AvatarPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                // We could filter by avatarProto.ShowInRosterIfLocked instead of hardcoding,
                // but then we won't get random drops for "removed" heroes.
                if (avatarProtoRef == (PrototypeId)6044485448390219466) continue;   //zzzBrevikOLD.prototype

                AvatarPrototype avatarProto = avatarProtoRef.As<AvatarPrototype>();
                _avatarPicker.Add(avatarProto);
            }

            // Cache item level limits from the ItemLevel property prototype
            // For reference, this is 1-75 in 1.52, but it was 1-100 in 1.10
            PropertyInfoPrototype propertyInfoProto = GameDatabase.PropertyInfoTable.LookupPropertyInfo(PropertyEnum.ItemLevel).Prototype;
            _itemLevelMin = (int)propertyInfoProto.Min;
            _itemLevelMax = (int)propertyInfoProto.Max;
        }

        /// <summary>
        /// Resets this <see cref="ItemResolver"/> and sets new rolling context.
        /// </summary>
        public void SetContext(LootContext lootContext, Player player)
        {
            _pendingItemList.Clear();
            _processedItemList.Clear();

            _context.Set(lootContext, player);
        }

        #region Push Functions

        // These functions are used to "push" intermediary data from rolling loot tables.
        // Order is the same as fields in NetStructLootResultSummary.

        public LootRollResult PushItem(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags, int stackCount, IEnumerable<LootMutationPrototype> mutations)
        {
            if (CheckItem(filterArgs, restrictionFlags, false) == false)
                return LootRollResult.Failure;

            ItemSpec itemSpec = new(filterArgs.ItemProto.DataRef, filterArgs.Rarity, filterArgs.Level,
                0, Array.Empty<AffixSpec>(), Random.Next(), PrototypeId.Invalid);

            LootResult lootResult = new(itemSpec);
            PendingItem pendingItem = new(lootResult, filterArgs.RollFor);
            _pendingItemList.Add(pendingItem);

            return LootRollResult.Success;
        }

        public LootRollResult PushAgent(PrototypeId agentProtoRef, int level, RestrictionTestFlags restrictionFlags)
        {
            if (CheckAgent(agentProtoRef, restrictionFlags) == false)
                return LootRollResult.Failure;

            AgentSpec agentSpec = new(agentProtoRef, level, 0);
            LootResult lootResult = new(agentSpec);
            _pendingItemList.Add(new(lootResult));

            return LootRollResult.Success;
        }

        public LootRollResult PushCredits(int amount)
        {
            // TODO: Credits bonuses

            if (amount > 0)
            {
                LootResult lootResult = new(LootType.Credits, amount);
                _pendingItemList.Add(new(lootResult));
            }

            return LootRollResult.Success;
        }

        public LootRollResult PushXP(CurveId xpCurveRef, int amount)
        {
            // TODO: XP bonuses

            if (amount > 0)
            {
                LootResult lootResult = new(xpCurveRef, amount);
                _pendingItemList.Add(new(lootResult));
            }

            return LootRollResult.Success;
        }

        public LootRollResult PushPowerPoints(int amount)
        {
            // NOTE: Unused in BUE
            if (amount > 0)
            {
                LootResult lootResult = new(LootType.PowerPoints, amount);
                _pendingItemList.Add(new(lootResult));
            }

            return LootRollResult.Success;
        }

        public LootRollResult PushHealthBonus(int amount)
        {
            // NOTE: Unused in BUE
            if (amount > 0)
            {
                LootResult lootResult = new(LootType.HealthBonus, amount);
                _pendingItemList.Add(new(lootResult));
            }

            return LootRollResult.Success;
        }

        public LootRollResult PushEnduranceBonus(int amount)
        {
            // NOTE: Unused in BUE
            if (amount > 0)
            {
                LootResult lootResult = new(LootType.EnduranceBonus, amount);
                _pendingItemList.Add(new(lootResult));
            }

            return LootRollResult.Success;
        }

        public LootRollResult PushRealMoney(LootDropRealMoneyPrototype lootDropRealMoneyProto)
        {
            LootResult lootResult = new(lootDropRealMoneyProto);
            _pendingItemList.Add(new(lootResult));
            return LootRollResult.Success;
        }

        public LootRollResult PushLootNodeCallback(LootNodePrototype callbackNodeProto)
        {
            LootResult lootResult = new(callbackNodeProto);
            _pendingItemList.Add(new(lootResult));
            return LootRollResult.Success;
        }

        public LootRollResult PushCraftingCallback(LootMutationPrototype lootMutationProto)
        {
            // TODO
            Logger.Debug($"PushCraftingCallback()");
            return LootRollResult.NoRoll;
        }

        public LootRollResult PushVanityTitle(PrototypeId vanityTitleProtoRef)
        {
            LootResult lootResult = new(vanityTitleProtoRef);
            _pendingItemList.Add(new(lootResult));
            return LootRollResult.Success;
        }

        public LootRollResult PushVendorXP(PrototypeId vendorProtoRef, int xpAmount)
        {
            VendorXPSummary vendorXPSummary = new(vendorProtoRef, xpAmount);
            LootResult lootResult = new(vendorXPSummary);
            _pendingItemList.Add(new(lootResult));
            return LootRollResult.Success;
        }

        public LootRollResult PushCurrency(WorldEntityPrototype worldEntityProto, DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags,
            LootDropChanceModifiers dropChanceModifiers, int stackCount)
        {
            // Currency can come from agents and items
            if (worldEntityProto is AgentPrototype)
            {
                if (CheckAgent(worldEntityProto.DataRef, restrictionFlags) == false)
                    return LootRollResult.Failure;
            }
            else if (worldEntityProto is ItemPrototype)
            {
                if (CheckItem(filterArgs, restrictionFlags, false) == false)
                    return LootRollResult.Failure;
            }
            else
            {
                return Logger.WarnReturn(LootRollResult.Failure, $"PushCurrency(): Unsupported currency entity prototype {worldEntityProto}");
            }

            if (worldEntityProto.GetCurrency(out PrototypeId currencyRef, out int amount) == false)
                return LootRollResult.Failure;

            // TODO: currency bonuses
            CurrencySpec currencySpec = new(worldEntityProto.DataRef, currencyRef, amount * stackCount);
            LootResult lootResult = new(currencySpec);
            _pendingItemList.Add(new(lootResult));

            return LootRollResult.Success;
        }

        #endregion

        #region Resolving

        // Resolve functions are helper functions for rolling loot given the context set for this item resolver

        public int ResolveLevel(int level, bool useLevelVerbatim)
        {
            // NOTE: In version 1.52 MobLevelToItemLevel.curve is empty, so we can always treat this as if useLevelVerbatim is set.
            // If we were to support older versions (most likely predating level scaling) we would have to properly implement this.

            // Clamp to the range defined in the property info because some modifiers apply a bigger offset (e.g. Doom artifacts with +99 to level)
            level = Math.Clamp(level, _itemLevelMin, _itemLevelMax);
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

            using DropFilterArguments filterArgs = ObjectPoolManager.Instance.Get<DropFilterArguments>();
            DropFilterArguments.Initialize(filterArgs, LootContext);

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

        public bool CheckDropChance(LootRollSettings settings, float noDropPercent)
        {
            float dropChance = _context.GetDropChance(settings, noDropPercent);
            return Random.NextFloat() < dropChance;
        }

        public bool CheckDropCooldown(PrototypeId dropProtoRef, int count)
        {
            return _context.IsOnCooldown(dropProtoRef, count);
        }

        public bool CheckItem(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags, bool arg2, int stackCount = 1)
        {
            ItemPrototype itemProto = filterArgs.ItemProto as ItemPrototype;
            if (itemProto == null) return Logger.WarnReturn(false, $"CheckItem(): itemProto == null");

            if (itemProto.ApprovedForUse() == false)
                return false;

            if (itemProto.IsLiveTuningEnabled() == false)
                return false;

            if (itemProto.IsDroppableForRestrictions(filterArgs, restrictionFlags) == false)
                return false;

            if (restrictionFlags.HasFlag(RestrictionTestFlags.Slot))
            {
                EquipmentInvUISlot slot = filterArgs.Slot;
                if (slot != EquipmentInvUISlot.Invalid)
                {
                    AgentPrototype agentProto = filterArgs.RollFor.As<AgentPrototype>();
                    if (itemProto.GetInventorySlotForAgent(agentProto) != slot)
                        return false;
                }
            }

            if (restrictionFlags.HasFlag(RestrictionTestFlags.UsableBy))
            {
                AgentPrototype agentProto = filterArgs.RollFor.As<AgentPrototype>();
                if (itemProto.IsDroppableForAgent(agentProto) == false)
                    return false;
            }

            if (restrictionFlags.HasFlag(RestrictionTestFlags.Cooldown))
            {
                if (CheckDropCooldown(itemProto.DataRef, stackCount))
                    return false;
            }

            return true;
        }

        public bool CheckAgent(PrototypeId agentProtoRef, RestrictionTestFlags restrictionFlags)
        {
            if (agentProtoRef == PrototypeId.Invalid)
                return false;

            if (restrictionFlags.HasFlag(RestrictionTestFlags.Cooldown) && CheckDropCooldown(agentProtoRef, 1))
                return false;

            return true;
        }

        #endregion

        #region Pending Item Processing

        public void ClearPending()
        {
            _pendingItemList.Clear();
        }

        public bool ProcessPending(LootRollSettings settings)
        {
            foreach (PendingItem pendingItem in _pendingItemList)
            {
                // Non-item loot does not need additional processing
                if (pendingItem.LootResult.Type != LootType.Item)
                {
                    _processedItemList.Add(pendingItem.LootResult);
                    continue;
                }

                // Items need to have their affixes rolled
                ItemSpec itemSpec = pendingItem.LootResult.ItemSpec;

                using LootCloneRecord affixArgs = ObjectPoolManager.Instance.Get<LootCloneRecord>();
                LootCloneRecord.Initialize(affixArgs, LootContext, itemSpec, pendingItem.RollFor);

                MutationResults result = LootUtilities.UpdateAffixes(this, affixArgs, AffixCountBehavior.Roll, itemSpec, settings);

                if (result.HasFlag(MutationResults.Error))
                    Logger.Warn($"ProcessPending(): Error when rolling affixes, result={result}");

                // Modify the item spec using output "restrictions" (OutputLevelPrototype, OutputRarityPrototype)
                ItemPrototype itemProto = itemSpec.ItemProtoRef.As<ItemPrototype>();
                RestrictionTestFlags flagsToAdjust = RestrictionTestFlags.Level | RestrictionTestFlags.Rarity | RestrictionTestFlags.Output;

                using DropFilterArguments restrictionArgs = ObjectPoolManager.Instance.Get<DropFilterArguments>();
                DropFilterArguments.Initialize(restrictionArgs, itemProto, itemSpec.EquippableBy, itemSpec.ItemLevel, itemSpec.RarityProtoRef, 0, EquipmentInvUISlot.Invalid, LootContext);
                
                itemProto.MakeRestrictionsDroppable(restrictionArgs, flagsToAdjust, out RestrictionTestFlags adjustResultFlags);

                if (adjustResultFlags.HasFlag(RestrictionTestFlags.OutputLevel))
                    itemSpec.ItemLevel = restrictionArgs.Level;

                if (adjustResultFlags.HasFlag(RestrictionTestFlags.OutputRarity))
                    itemSpec.RarityProtoRef = restrictionArgs.Rarity;

                // Push the final processed item
                _processedItemList.Add(new(itemSpec));
            }

            _pendingItemList.Clear();
            return true;
        }

        public void FillLootResultSummary(LootResultSummary lootResultSummary)
        {
            foreach (LootResult lootResult in _processedItemList)
                lootResultSummary.Add(lootResult);
        }

        #endregion

        private readonly struct PendingItem
        {
            public LootResult LootResult { get; }
            public PrototypeId RollFor { get; }

            public PendingItem(in LootResult lootResult)
            {
                LootResult = lootResult;
                RollFor = PrototypeId.Invalid;
            }

            public PendingItem(in LootResult lootResult, PrototypeId rollFor)
            {
                LootResult = lootResult;
                RollFor = rollFor;
            }
        }
    }
}
