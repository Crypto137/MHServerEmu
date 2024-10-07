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
    /// A basic implementation of <see cref="IItemResolver"/>.
    /// </summary>
    public class ItemResolver : IItemResolver
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Picker<AvatarPrototype> _avatarPicker;

        private readonly int _itemLevelMin;
        private readonly int _itemLevelMax;

        private readonly List<PendingItem> _pendingItemList = new();
        private readonly List<LootResult> _processedItemList = new();

        public GRandom Random { get; }
        public LootContext LootContext { get; private set; }
        public Player Player { get; private set; }
        public Region Region { get => Player?.GetRegion(); }

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

            LootContext = lootContext;
            Player = player;
        }

        #region Push Functions

        // These functions are used to "push" intermediary data from rolling loot tables

        public LootRollResult PushItem(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags, int stackCount, IEnumerable<LootMutationPrototype> mutations)
        {
            if (CheckItem(filterArgs, restrictionFlags, false) == false)
                return LootRollResult.NoRoll;

            ItemSpec itemSpec = new(filterArgs.ItemProto.DataRef, filterArgs.Rarity, filterArgs.Level,
                0, Array.Empty<AffixSpec>(), Random.Next(), PrototypeId.Invalid);

            LootResult lootResult = new(itemSpec);
            PendingItem pendingItem = new(lootResult, filterArgs.RollFor);
            _pendingItemList.Add(pendingItem);

            return LootRollResult.Success;
        }

        public LootRollResult PushCurrency(WorldEntityPrototype worldEntityProto, DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags,
            LootDropChanceModifiers dropChanceModifiers, int stackCount)
        {
            Logger.Debug($"PushCurrency(): {worldEntityProto} filters=[{restrictionFlags}] mods=[{dropChanceModifiers}]");
            return LootRollResult.NoRoll;
        }

        public LootRollResult PushAgent(PrototypeId agentProtoRef, int level, RestrictionTestFlags restrictionFlags)
        {
            if (agentProtoRef == PrototypeId.Invalid)
                return Logger.WarnReturn(LootRollResult.Failure, "PushAgent(): agentProtoRef == PrototypeId.Invalid");

            // TODO: check restrictionFlags

            AgentSpec agentSpec = new(agentProtoRef, level, 0);
            LootResult lootResult = new(agentSpec);
            PendingItem pendingItem = new(lootResult);
            _pendingItemList.Add(pendingItem);

            Logger.Debug($"PushAgent(): {agentSpec} [{restrictionFlags}]");
            return LootRollResult.Success;
        }

        public LootRollResult PushCredits(int amount)
        {
            // TODO: Credits bonuses
            LootResult lootResult = new(LootType.Credits, amount);
            PendingItem pendingItem = new(lootResult);
            _pendingItemList.Add(pendingItem);
            return LootRollResult.Success;
        }

        public LootRollResult PushPowerPoints(int amount)
        {
            Logger.Debug($"PushPowerPoints(): {amount}");
            return LootRollResult.NoRoll;
        }

        public LootRollResult PushHealthBonus(int amount)
        {
            Logger.Debug($"PushHealthBonus(): {amount}");
            return LootRollResult.NoRoll;
        }

        public LootRollResult PushEnduranceBonus(int amount)
        {
            Logger.Debug($"PushEnduranceBonus(): {amount}");
            return LootRollResult.NoRoll;
        }

        public LootRollResult PushXP(CurveId xpCurveRef, int amount)
        {
            // TODO: XP bonuses
            LootResult lootResult = new(xpCurveRef, amount);
            PendingItem pendingItem = new(lootResult);
            _pendingItemList.Add(pendingItem);
            return LootRollResult.Success;
        }

        public LootRollResult PushLootNodeCallback()
        {
            Logger.Debug($"PushLootNodeCallback()");
            return LootRollResult.NoRoll;
        }

        public LootRollResult PushCraftingCallback()
        {
            Logger.Debug($"PushCraftingCallback()");
            return LootRollResult.NoRoll;
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

        public bool CheckDropPercent(LootRollSettings settings, float noDropPercent)
        {
            // Do not drop if there are any hard restrictions (this should have already been handled when selecting the loot table node)
            if (settings.IsRestrictedByLootDropChanceModifier())
                return Logger.WarnReturn(false, $"CheckDropPercent(): Restricted by loot drop chance modifiers [{settings.DropChanceModifiers}]");

            // Do not drop cooldown-based loot for now
            if (settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.CooldownOncePerXHours))
                return Logger.WarnReturn(false, "CheckDropPercent(): Unimplemented modifier CooldownOncePerXHours");

            if (settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.CooldownOncePerRollover))
                return Logger.WarnReturn(false, "CheckDropPercent(): Unimplemented modifier CooldownOncePerRollover");

            if (settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.CooldownByChannel))
                return Logger.WarnReturn(false, "CheckDropPercent(): Unimplemented modifier CooldownByChannel");

            // Start with a base drop chance based on the specified NoDrop percent
            float dropChance = 1f - noDropPercent;

            // Apply live tuning multiplier
            dropChance *= LiveTuningManager.GetLiveGlobalTuningVar(GlobalTuningVar.eGTV_LootDropRate);

            // Apply difficulty multiplier
            if (settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.DifficultyTierNoDropModified))
                dropChance *= settings.NoDropModifier;

            // Add more multipliers here as needed

            // Check the final chance
            return Random.NextFloat() < dropChance;
        }

        public bool CheckItem(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags, bool arg2)
        {
            ItemPrototype itemProto = filterArgs.ItemProto as ItemPrototype;
            if (itemProto == null) return Logger.WarnReturn(false, $"CheckItem(): itemProto == null");

            if (itemProto.ApprovedForUse() == false)
                return false;

            if (itemProto.IsLiveTuningEnabled() == false)
                return false;

            if (itemProto.IsDroppableForRestrictions(filterArgs, restrictionFlags) == false)
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

                using LootCloneRecord args = ObjectPoolManager.Instance.Get<LootCloneRecord>();
                LootCloneRecord.Initialize(args, LootContext, itemSpec, pendingItem.RollFor);

                MutationResults result = LootUtilities.UpdateAffixes(this, args, AffixCountBehavior.Roll, itemSpec, settings);

                if (result.HasFlag(MutationResults.Error))
                    Logger.Warn($"ProcessPending(): Error when rolling affixes, result={result}");

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
