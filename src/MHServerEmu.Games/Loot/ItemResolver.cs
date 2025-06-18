using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot.Specs;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Loot
{
    /// <summary>
    /// A general-purpose implementation of <see cref="IItemResolver"/>.
    /// </summary>
    public class ItemResolver : IItemResolver, IPoolable, IDisposable
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly int _itemLevelMin;
        private readonly int _itemLevelMax;

        private readonly List<ItemSpec> _cloneSourceList = new();

        private readonly List<PendingItem> _pendingItemList = new();
        private readonly List<LootResult> _processedItemList = new();

        private readonly ItemResolverContext _context = new();

        private Picker<int> _levelOffsetPicker;
        private Picker<AvatarPrototype> _avatarPicker;

        public GRandom Random { get; private set; }
        public LootResolverFlags Flags { get; private set; }

        // CUSTOM: See LootTablePrototype.PickLiveTuningNodes() for why we need this
        public LootContext LootContext { get => LootContextOverride != LootContext.None ? LootContextOverride : _context.LootContext; }
        public LootContext LootContextOverride { get; set; }
        public Player Player { get => _context.Player; }
        public Region Region { get => _context.Region; }

        public bool IsInPool { get; set; }

        public ItemResolver()
        {
            // Cache item level limits from the ItemLevel property prototype
            // For reference, this is 1-75 in 1.52, but it was 1-100 in 1.10
            PropertyInfoPrototype propertyInfoProto = GameDatabase.PropertyInfoTable.LookupPropertyInfo(PropertyEnum.ItemLevel).Prototype;
            _itemLevelMin = (int)propertyInfoProto.Min;
            _itemLevelMax = (int)propertyInfoProto.Max;
        }

        public void Initialize(GRandom random)
        {
            Random = random;

            // NOTE: We have to rebuild pickers on each initialization because they use the same GRandom as the resolver.
            // TODO: Move this to the constructor when we implement poolable pickers with reassignable GRandom instances.

            // Cache mob level to item level offset picker
            // NOTE: In version 1.52 the only possible offset is 3, but it's more varied in older versions of the game (e.g. 1.10).
            _levelOffsetPicker = new(random);
            Curve curve = GameDatabase.LootGlobalsPrototype.LootLevelDistribution.AsCurve();
            for (int i = curve.MinPosition; i <= curve.MaxPosition; i++)
                _levelOffsetPicker.Add(i, curve.GetIntAt(i));

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
        }

        public void ResetForPool()
        {
            _cloneSourceList.Clear();
            _context.Clear();
            _avatarPicker = default;

            Random = default;
            Flags = default;

            LootContextOverride = default;
        }

        public void Dispose()
        {
            ObjectPoolManager.Instance.Return(this);
        }

        /// <summary>
        /// Resets this <see cref="ItemResolver"/> and sets new rolling context.
        /// </summary>
        public void SetContext(LootContext lootContext, Player player, WorldEntity sourceEntity = null)
        {
            _pendingItemList.Clear();
            _processedItemList.Clear();

            _context.Set(lootContext, player, sourceEntity);
        }

        public void SetContext(Mission mission, Player player)
        {
            _pendingItemList.Clear();
            _processedItemList.Clear();

            _context.Set(mission, player);
        }

        public void SetFlags(LootResolverFlags flags, bool value)
        {
            if (value)
                Flags |= flags;
            else
                Flags &= ~flags;
        }

        #region Push Functions

        // These functions are used to "push" intermediary data from rolling loot tables.
        // Order is the same as fields in NetStructLootResultSummary.

        public LootRollResult PushItem(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags, int stackCount, LootMutationPrototype[] mutations)
        {
            if (CheckItem(filterArgs, restrictionFlags, false, stackCount) == false)
                return LootRollResult.Failure;

            ItemSpec itemSpec = new(filterArgs.ItemProto.DataRef, filterArgs.Rarity, filterArgs.Level,
                0, Array.Empty<AffixSpec>(), Random.Next(), PrototypeId.Invalid);

            itemSpec.StackCount = stackCount;

            LootResult lootResult = new(itemSpec);
            PendingItem pendingItem = new(lootResult, filterArgs.RollFor, mutations, false);
            _pendingItemList.Add(pendingItem);

            return LootRollResult.Success;
        }

        public LootRollResult PushClone(LootCloneRecord lootCloneRecord)
        {
            if (CheckItem(lootCloneRecord, lootCloneRecord.RestrictionFlags) == false)
                return LootRollResult.Failure;

            LootResult lootResult = new(lootCloneRecord.ToItemSpec());
            PendingItem pendingItem = new(lootResult, PrototypeId.Invalid, null, true);
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
            amount = _context.ScaleCredits(amount);

            if (amount > 0)
            {
                LootResult lootResult = new(LootType.Credits, amount);
                _pendingItemList.Add(new(lootResult));
            }

            return LootRollResult.Success;
        }

        public LootRollResult PushXP(CurveId xpCurveRef, int amount)
        {
            amount = _context.ScaleExperience(amount);

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
            LootResult lootResult = new(lootMutationProto);
            _pendingItemList.Add(new(lootResult));
            return LootRollResult.Success;
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
                if (CheckItem(filterArgs, restrictionFlags, false, stackCount) == false)
                    return LootRollResult.Failure;
            }
            else
            {
                return Logger.WarnReturn(LootRollResult.Failure, $"PushCurrency(): Unsupported currency entity prototype {worldEntityProto}");
            }

            if (worldEntityProto.GetCurrency(out PrototypeId currencyRef, out int amount) == false)
                return LootRollResult.Failure;

            amount = _context.ScaleCurrency(currencyRef, amount * stackCount);
            CurrencySpec currencySpec = new(worldEntityProto.DataRef, currencyRef, amount);
            LootResult lootResult = new(currencySpec);
            _pendingItemList.Add(new(lootResult));

            return LootRollResult.Success;
        }

        #endregion

        #region Resolving

        // Resolve functions are helper functions for rolling loot given the context set for this item resolver

        public int ResolveLevel(int level, bool useLevelVerbatim)
        {
            // NOTE: In 1.52 this offsets by +3 if not using level verbatim
            if (useLevelVerbatim == false && _levelOffsetPicker.Pick(out int offset))
                level += offset;

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
            using DropFilterArguments filterArgs = ObjectPoolManager.Instance.Get<DropFilterArguments>();
            DropFilterArguments.Initialize(filterArgs, LootContext);

            List<RarityEntry> rarityEntryList = ListPool<RarityEntry>.Instance.Get();
            float weightSum = 0f;

            foreach (PrototypeId rarityProtoRef in DataDirectory.Instance.IteratePrototypesInHierarchy<RarityPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                // Skip rarities that don't match the provided filter
                if (rarityFilter != null && rarityFilter.Count > 0 && rarityFilter.Contains(rarityProtoRef) == false)
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

                RarityEntry entry = new(rarityProto, level);
                weightSum += entry.Weight;
                rarityEntryList.Add(entry);
            }

            PrototypeId pickedRarityProtoRef = PrototypeId.Invalid;

            if (rarityEntryList.Count > 0)
            {
                Picker<PrototypeId> rarityPicker = new(Random);
                _context.FillRarityPicker(rarityPicker, rarityEntryList, weightSum);
                pickedRarityProtoRef = rarityPicker.Pick();
            }

            ListPool<RarityEntry>.Instance.Return(rarityEntryList);
            return pickedRarityProtoRef;
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

        public bool CheckItem(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags, bool arg2 = false, int stackCount = 1)
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

        #region Clone Source Management

        public bool InitializeCloneRecordFromSource(int index, LootCloneRecord lootCloneRecord)
        {
            if (index >= _cloneSourceList.Count)
                return false;

            ItemSpec cloneSource = _cloneSourceList[index];
            if (cloneSource == null)    // If this check triggers, we probably need to replace nulls with dummy specs (see SetCloneSource() below)
                return Logger.WarnReturn(false, "InitializeRecordFromCloneSource(): cloneSource == null");

            LootCloneRecord.Initialize(lootCloneRecord, LootContext, cloneSource, PrototypeId.Invalid);
            return true;
        }

        public void SetCloneSource(int index, ItemSpec itemSpec)
        {
            while (_cloneSourceList.Count <= index)
                _cloneSourceList.Add(null); // null here represents auto populated slots

            _cloneSourceList[index] = itemSpec;
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
                // Check vaporization
                LootContext context = LootContext;
                bool isVaporized = false;

                if (settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.PreviewOnly) == false &&
                    (context == LootContext.Drop || context == LootContext.MissionReward))
                {
                    isVaporized = LootVaporizer.ShouldVaporizeLootResult(settings.Player, pendingItem.LootResult, pendingItem.RollFor);
                }

                switch (pendingItem.LootResult.Type)
                {
                    case LootType.Item:
                        {
                            // Roll affixes for new items that didn't get vaporized
                            ItemSpec itemSpec = pendingItem.LootResult.ItemSpec;

                            if (pendingItem.IsClone == false && isVaporized == false)
                            {
                                using LootCloneRecord affixArgs = ObjectPoolManager.Instance.Get<LootCloneRecord>();
                                LootCloneRecord.Initialize(affixArgs, context, itemSpec, pendingItem.RollFor);

                                MutationResults affixResult = LootUtilities.UpdateAffixes(this, affixArgs, AffixCountBehavior.Roll, itemSpec, settings);

                                if (affixResult.HasFlag(MutationResults.Error))
                                    return Logger.WarnReturn(false, $"ProcessPending(): Error when rolling affixes, result={affixResult}");
                            }

                            // Apply mutations (if any)
                            if (pendingItem.Mutations.HasValue())
                            {
                                using LootCloneRecord mutationArgs = ObjectPoolManager.Instance.Get<LootCloneRecord>();
                                LootCloneRecord.Initialize(mutationArgs, LootContext, itemSpec, pendingItem.RollFor);

                                MutationResults mutationResult = MutationResults.None;
                                
                                foreach (LootMutationPrototype lootMutationProto in pendingItem.Mutations)
                                {
                                    mutationResult |= lootMutationProto.Mutate(settings, this, mutationArgs);
                                    if (mutationResult.HasFlag(MutationResults.Error))
                                        return Logger.WarnReturn(false, $"ProcessPending(): Error when applying mutations, result={mutationResult}");
                                }

                                // Replace the item spec if any mutations were applied
                                if (mutationResult != MutationResults.None)
                                {
                                    if (CheckItem(mutationArgs, mutationArgs.RestrictionFlags))
                                        itemSpec.Set(mutationArgs);
                                    else
                                        return Logger.WarnReturn(false, "ProcessPending(): Mutations failed to pass CheckItem()");
                                }
                            }

                            // Modify the item spec using output "restrictions" (OutputLevelPrototype, OutputRarityPrototype)
                            ItemPrototype itemProto = itemSpec.ItemProtoRef.As<ItemPrototype>();
                            RestrictionTestFlags flagsToAdjust = RestrictionTestFlags.Level | RestrictionTestFlags.Rarity | RestrictionTestFlags.Output;

                            using DropFilterArguments restrictionArgs = ObjectPoolManager.Instance.Get<DropFilterArguments>();
                            DropFilterArguments.Initialize(restrictionArgs, itemProto, itemSpec.EquippableBy, itemSpec.ItemLevel, itemSpec.RarityProtoRef, 0, EquipmentInvUISlot.Invalid, context);

                            itemProto.MakeRestrictionsDroppable(restrictionArgs, flagsToAdjust, out RestrictionTestFlags adjustResultFlags);

                            if (adjustResultFlags.HasFlag(RestrictionTestFlags.OutputLevel))
                                itemSpec.ItemLevel = restrictionArgs.Level;

                            if (adjustResultFlags.HasFlag(RestrictionTestFlags.OutputRarity))
                                itemSpec.RarityProtoRef = restrictionArgs.Rarity;

                            // Push the final processed item
                            _processedItemList.Add(new(itemSpec, isVaporized));
                        }

                        break;

                    case LootType.Credits:
                        _processedItemList.Add(new(LootType.Credits, pendingItem.LootResult.Amount, isVaporized));
                        break;

                    default:
                        // Non-item loot does not need additional processing
                        _processedItemList.Add(pendingItem.LootResult);
                        break;
                }
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
            public readonly LootResult LootResult;
            public readonly PrototypeId RollFor;
            public readonly LootMutationPrototype[] Mutations;
            public readonly bool IsClone;

            public PendingItem(in LootResult lootResult)
            {
                LootResult = lootResult;
                RollFor = PrototypeId.Invalid;
                Mutations = null;
                IsClone = false;
            }

            public PendingItem(in LootResult lootResult, PrototypeId rollFor, LootMutationPrototype[] mutations, bool isClone)
            {
                LootResult = lootResult;
                RollFor = rollFor;
                Mutations = mutations;
                IsClone = isClone;
            }
        }
    }
}
