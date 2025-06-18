using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Loot;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class DropRestrictionPrototype : Prototype
    {
        //---

        public virtual bool Adjust(DropFilterArguments filterArgs, ref RestrictionTestFlags adjustResultFlags, RestrictionTestFlags flagsToAdjust = RestrictionTestFlags.All)
        {
            return flagsToAdjust.HasFlag(RestrictionTestFlags.Output) || Allow(filterArgs, flagsToAdjust);
        }

        public virtual bool Allow(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags = RestrictionTestFlags.All)
        {
            return (filterArgs.LootContext & LootContext.CashShop) == filterArgs.LootContext;
        }

        public virtual bool AllowAsCraftingInput(LootCloneRecord lootCloneRecord, RestrictionTestFlags restrictionFlags = RestrictionTestFlags.All)
        {
            return Allow(lootCloneRecord, restrictionFlags);
        }
    }

    public class ConditionalRestrictionPrototype : DropRestrictionPrototype
    {
        public DropRestrictionPrototype[] Apply { get; protected set; }
        public LootContext[] ApplyFor { get; protected set; }
        public DropRestrictionPrototype[] Else { get; protected set; }

        //---

        private LootContext _lootContextFlags = LootContext.None;

        public override void PostProcess()
        {
            base.PostProcess();

            if (ApplyFor.IsNullOrEmpty())
                return;

            foreach (LootContext context in ApplyFor)
                _lootContextFlags |= context;
        }

        public override bool Adjust(DropFilterArguments filterArgs, ref RestrictionTestFlags adjustResultFlags, RestrictionTestFlags flagsToAdjust)
        {
            if ((filterArgs.LootContext & _lootContextFlags) == filterArgs.LootContext)
            {
                if (Apply.IsNullOrEmpty())
                    return true;

                foreach (DropRestrictionPrototype restrictionProto in Apply)
                {
                    if (restrictionProto.Adjust(filterArgs, ref adjustResultFlags, flagsToAdjust) == false)
                        return false;
                }
            }
            else
            {
                if (Else.IsNullOrEmpty())
                    return true;

                foreach (DropRestrictionPrototype restrictionProto in Else)
                {
                    if (restrictionProto.Adjust(filterArgs, ref adjustResultFlags, flagsToAdjust) == false)
                        return false;
                }
            }

            return true;
        }

        public override bool Allow(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags)
        {
            if ((filterArgs.LootContext & _lootContextFlags) == filterArgs.LootContext)
            {
                if (Apply.IsNullOrEmpty())
                    return true;

                foreach (DropRestrictionPrototype restrictionProto in Apply)
                {
                    if (restrictionProto.Allow(filterArgs, restrictionFlags) == false)
                        return false;
                }
            }
            else
            {
                if (Else.IsNullOrEmpty())
                    return true;

                foreach (DropRestrictionPrototype restrictionProto in Else)
                {
                    if (restrictionProto.Allow(filterArgs, restrictionFlags) == false)
                        return false;
                }
            }

            return true;
        }
    }

    public class ContextRestrictionPrototype : DropRestrictionPrototype
    {
        public LootContext[] UsableFor { get; protected set; }

        //---

        private LootContext _lootContextFlags = LootContext.None;

        public override void PostProcess()
        {
            base.PostProcess();

            if (UsableFor.IsNullOrEmpty())
                return;

            // Always allow cash shop items
            _lootContextFlags = LootContext.CashShop;

            foreach (LootContext context in UsableFor)
                _lootContextFlags |= context;
        }

        public override bool Allow(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags)
        {
            return (filterArgs.LootContext & _lootContextFlags) == filterArgs.LootContext;
        }
    }

    public class ItemTypeRestrictionPrototype : DropRestrictionPrototype
    {
        public PrototypeId[] AllowedTypes { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override bool Allow(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags)
        {
            if (AllowedTypes.IsNullOrEmpty())
                return false;

            if (restrictionFlags.HasFlag(RestrictionTestFlags.ItemType) == false)
                return true;

            Prototype itemProto = filterArgs.ItemProto;
            if (itemProto == null) return Logger.WarnReturn(false, "Allow(): itemProto == null");

            DataDirectory dataDirectory = DataDirectory.Instance;
            foreach (PrototypeId allowedTypeRef in AllowedTypes)
            {
                Blueprint blueprint = dataDirectory.GetBlueprint((BlueprintId)allowedTypeRef);
                if (blueprint == null)
                {
                    Logger.Warn("Allow(): blueprint == null");
                    continue;
                }

                if (dataDirectory.PrototypeIsChildOfBlueprint(itemProto.DataRef, blueprint.Id))
                    return true;
            }

            return false;
        }
    }

    public class ItemParentRestrictionPrototype : DropRestrictionPrototype
    {
        public PrototypeId[] AllowedParents { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override bool Allow(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags)
        {
            if (AllowedParents.IsNullOrEmpty())
                return false;

            if (restrictionFlags.HasFlag(RestrictionTestFlags.ItemParent) == false)
                return true;

            Prototype itemProto = filterArgs.ItemProto;
            if (itemProto == null) return Logger.WarnReturn(false, "Allow(): itemProto == null");

            DataDirectory dataDirectory = DataDirectory.Instance;
            foreach (PrototypeId allowedTypeRef in AllowedParents)
            {
                if (dataDirectory.PrototypeIsAPrototype(itemProto.DataRef, allowedTypeRef))
                    return true;
            }

            return false;
        }
    }

    public class HasAffixInPositionRestrictionPrototype : DropRestrictionPrototype
    {
        public AffixPosition Position { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override bool Allow(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags)
        {
            return Logger.WarnReturn(false, "Allow(): HasAffixInPosition DropRestriction is only supported in CraftingInput right-hand structs!");
        }

        public override bool AllowAsCraftingInput(LootCloneRecord lootCloneRecord, RestrictionTestFlags restrictionTestFlags)
        {
            // Check applied affixes
            foreach (AffixRecord affixRecord in lootCloneRecord.AffixRecords)
            {
                AffixPrototype affixProto = affixRecord.AffixProtoRef.As<AffixPrototype>();
                if (affixProto == null)
                {
                    Logger.Warn("AllowAsCraftingInput(): affixProto == null");
                    continue;
                }

                if (affixProto.Position == Position)
                    return true;
            }

            // Check built-in affixes
            ItemPrototype itemProto = lootCloneRecord.ItemProto as ItemPrototype;
            if (itemProto == null) return Logger.WarnReturn(false, "AllowAsCraftingInput(): itemProto == null");

            if (itemProto.AffixesBuiltIn.HasValue())
            {
                foreach (AffixEntryPrototype affixEntryProto in itemProto.AffixesBuiltIn)
                {
                    AffixPrototype affixProto = affixEntryProto.Affix.As<AffixPrototype>();
                    if (affixProto == null)
                    {
                        Logger.Warn("AllowAsCraftingInput(): affixProto == null");
                        continue;
                    }

                    if (affixProto.Position == Position)
                        return true;
                }
            }

            return false;
        }
    }

    public class HasVisualAffixRestrictionPrototype : DropRestrictionPrototype
    {
        public bool MustHaveNoVisualAffixes { get; protected set; }
        public bool MustHaveVisualAffix { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override bool Allow(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags)
        {
            return Logger.WarnReturn(false, "Allow(): HasVisualAffix DropRestriction is only supported in CraftingInput right-hand structs!");
        }

        public override bool AllowAsCraftingInput(LootCloneRecord lootCloneRecord, RestrictionTestFlags restrictionTestFlags)
        {
            if (restrictionTestFlags.HasFlag(RestrictionTestFlags.VisualAffix) == false)
                return true;

            PrototypeId noVisualsAffix = GameDatabase.GlobalsPrototype.ItemNoVisualsAffix;

            bool hasVisualAffix = false;
            bool hasNoVisualsAffix = false;

            // Check applied affixes
            foreach (AffixRecord record in lootCloneRecord.AffixRecords)
            {
                if (record.AffixProtoRef == noVisualsAffix)
                {
                    hasNoVisualsAffix = true;
                }
                else if (hasVisualAffix == false)
                {
                    AffixPrototype affixProto = record.AffixProtoRef.As<AffixPrototype>();
                    if (affixProto == null)
                    {
                        Logger.Warn("AllowAsCraftingInput(): affixProto == null");
                        continue;
                    }

                    if (affixProto.Position == AffixPosition.Visual)
                        hasVisualAffix = true;
                }
            }

            // Check built-in affixes if needed
            if (hasVisualAffix == false && hasNoVisualsAffix == false)
            {
                ItemPrototype itemProto = lootCloneRecord.ItemProto as ItemPrototype;
                if (itemProto == null) return Logger.WarnReturn(false, "AllowAsCraftingInput(): itemProto == null");

                if (itemProto.AffixesBuiltIn.HasValue())
                {
                    foreach (AffixEntryPrototype affixEntryProto in itemProto.AffixesBuiltIn)
                    {
                        AffixPrototype affixProto = affixEntryProto.Affix.As<AffixPrototype>();
                        if (affixProto == null)
                        {
                            Logger.Warn("AllowAsCraftingInput(): affixProto == null");
                            continue;
                        }

                        if (affixProto.Position == AffixPosition.Visual)
                        {
                            hasVisualAffix = true;
                            break;
                        }
                    }
                }
            }

            if (MustHaveVisualAffix)
                return hasVisualAffix && hasNoVisualsAffix == false;

            if (MustHaveNoVisualAffixes)
                return hasVisualAffix == false || hasNoVisualsAffix;

            return false;
        }
    }

    public class LevelRestrictionPrototype : DropRestrictionPrototype
    {
        public int LevelMin { get; protected set; }
        public int LevelRange { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();

            LevelMin = Math.Max(LevelMin, 1);
            LevelRange = Math.Max(LevelRange, -1);
        }

        public override bool Adjust(DropFilterArguments filterArgs, ref RestrictionTestFlags adjustResultFlags, RestrictionTestFlags flagsToAdjust)
        {
            if (Allow(filterArgs, flagsToAdjust))
                return true;

            if (adjustResultFlags.HasFlag(RestrictionTestFlags.OutputLevel) || flagsToAdjust.HasFlag(RestrictionTestFlags.Output))
                return true;

            if (flagsToAdjust.HasFlag(RestrictionTestFlags.Level) == false)
                return false;

            filterArgs.Level = Math.Max(filterArgs.Level, LevelMin);

            if (LevelRange >= 0)
                filterArgs.Level = Math.Min(filterArgs.Level, LevelMin + LevelRange);

            adjustResultFlags |= RestrictionTestFlags.Level;
            
            return true;
        }

        public override bool Allow(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags)
        {
            if (restrictionFlags.HasFlag(RestrictionTestFlags.Level) == false)
                return true;

            int level = filterArgs.Level;

            return level >= LevelMin && (LevelRange < 0 || level <= LevelMin + LevelRange);
        }
    }

    public class OutputLevelPrototype : DropRestrictionPrototype
    {
        public int Value { get; protected set; }
        public bool UseAsFilter { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();
        }

        public override bool Adjust(DropFilterArguments filterArgs, ref RestrictionTestFlags adjustResultFlags, RestrictionTestFlags flagsToAdjust)
        {
            if (flagsToAdjust.HasFlag(RestrictionTestFlags.Level))
            {
                adjustResultFlags |= RestrictionTestFlags.OutputLevel;

                if (filterArgs.Level != Value)
                {
                    adjustResultFlags |= RestrictionTestFlags.Level;
                    filterArgs.Level = Value;
                }
            }

            return true;
        }

        public override bool Allow(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags)
        {
            return UseAsFilter == false || restrictionFlags.HasFlag(RestrictionTestFlags.Level) == false || filterArgs.Level == Value;
        }
    }

    public class OutputRankPrototype : DropRestrictionPrototype
    {
        public int Value { get; protected set; }
        public bool UseAsFilter { get; protected set; }

        //---

        public override bool Adjust(DropFilterArguments filterArgs, ref RestrictionTestFlags adjustResultFlags, RestrictionTestFlags flagsToAdjust)
        {
            if (flagsToAdjust.HasFlag(RestrictionTestFlags.Rank))
            {
                adjustResultFlags |= RestrictionTestFlags.OutputRank;

                if (filterArgs.Rank != Value)
                {
                    adjustResultFlags |= RestrictionTestFlags.Rank;
                    filterArgs.Rank = Value;
                }
            }

            return true;
        }

        public override bool Allow(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags)
        {
            return UseAsFilter == false || restrictionFlags.HasFlag(RestrictionTestFlags.Rank) == false ||
                MathHelper.BitTestAll(Value, filterArgs.Rank);
        }
    }

    public class OutputRarityPrototype : DropRestrictionPrototype
    {
        public PrototypeId Value { get; protected set; }
        public bool UseAsFilter { get; protected set; }

        //---

        public override bool Adjust(DropFilterArguments filterArgs, ref RestrictionTestFlags adjustResultFlags, RestrictionTestFlags flagsToAdjust)
        {
            if (flagsToAdjust.HasFlag(RestrictionTestFlags.Rarity))
            {
                adjustResultFlags |= RestrictionTestFlags.OutputRarity;

                if (filterArgs.Rarity != Value)
                {
                    adjustResultFlags |= RestrictionTestFlags.Rarity;
                    filterArgs.Rarity = Value;
                }
            }

            return true;
        }

        public override bool Allow(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags)
        {
            return UseAsFilter == false || restrictionFlags.HasFlag(RestrictionTestFlags.Rarity) == false || filterArgs.Rarity == Value;
        }
    }

    public class RarityRestrictionPrototype : DropRestrictionPrototype
    {
        public PrototypeId[] AllowedRarities { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override void PostProcess()
        {
            base.PostProcess();

            if (AllowedRarities.HasValue())
                Array.Sort(AllowedRarities, CompareRarityRefs);
        }

        public override bool Adjust(DropFilterArguments filterArgs, ref RestrictionTestFlags adjustResultFlags, RestrictionTestFlags flagsToAdjust)
        {
            if (Allow(filterArgs, flagsToAdjust))
                return true;

            if (adjustResultFlags.HasFlag(RestrictionTestFlags.OutputRarity) || flagsToAdjust.HasFlag(RestrictionTestFlags.Output))
                return true;

            if (flagsToAdjust.HasFlag(RestrictionTestFlags.Rarity) == false)
                return false;

            adjustResultFlags |= RestrictionTestFlags.Rarity;

            RarityPrototype rarityProto = filterArgs.Rarity.As<RarityPrototype>();
            if (rarityProto != null)
            {
                int tier = rarityProto.Tier;
                RarityPrototype lowestRarityProto = AllowedRarities[0].As<RarityPrototype>();
                
                // Clamp args rarity to the range defined in this prototype
                if (tier < lowestRarityProto.Tier)
                {
                    filterArgs.Rarity = AllowedRarities[0];
                }
                else
                {
                    RarityPrototype highestRarityProto = AllowedRarities[^1].As<RarityPrototype>();
                    if (tier > highestRarityProto.Tier)
                    {
                        filterArgs.Rarity = AllowedRarities[^1];
                    }
                    else
                    {
                        while (rarityProto != null)
                        {
                            filterArgs.Rarity = rarityProto.DowngradeTo;
                            
                            if (AllowedRarities.Contains(filterArgs.Rarity))
                                break;

                            rarityProto = filterArgs.Rarity.As<RarityPrototype>();
                        }
                    }
                }
            }

            if (rarityProto == null || filterArgs.Rarity == PrototypeId.Invalid)
                filterArgs.Rarity = AllowedRarities[0];

            return true;
        }

        public override bool Allow(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags)
        {
            if (restrictionFlags.HasFlag(RestrictionTestFlags.Rarity) == false)
                return true;

            return AllowedRarities != null && AllowedRarities.Contains(filterArgs.Rarity);
        }

        private static int CompareRarityRefs(PrototypeId leftProtoRef, PrototypeId rightProtoRef)
        {
            RarityPrototype leftProto = leftProtoRef.As<RarityPrototype>();
            RarityPrototype rightProto = rightProtoRef.As<RarityPrototype>();

            // Left downgrades to right: left > right
            if (RarityDowngradesTo(leftProto, rightProto))
                return 1;
            
            // Right downgrades to left: left < right
            if (RarityDowngradesTo(rightProto, leftProto))
                return -1;

            // Rarities don't downgrade to one or the other: left == right
            return 0;
        }

        private static bool RarityDowngradesTo(RarityPrototype left, RarityPrototype right)
        {
            while (left != null)
            {
                if (left == right)
                    return true;

                left = left.DowngradeTo.As<RarityPrototype>();
            }

            return false;
        }

    }

    public class RankRestrictionPrototype : DropRestrictionPrototype
    {
        public int AllowedRanks { get; protected set; }

        //---

        public override bool Adjust(DropFilterArguments filterArgs, ref RestrictionTestFlags adjustResultFlags, RestrictionTestFlags flagsToAdjust)
        {
            if (Allow(filterArgs, flagsToAdjust) == false || (flagsToAdjust.HasFlag(RestrictionTestFlags.Rank) && filterArgs.Rank == 0))
            {
                if (adjustResultFlags.HasFlag(RestrictionTestFlags.OutputRank) || flagsToAdjust.HasFlag(RestrictionTestFlags.Output))
                    return true;

                if (flagsToAdjust.HasFlag(RestrictionTestFlags.Rank) == false)
                    return false;

                adjustResultFlags |= RestrictionTestFlags.Rank;
                filterArgs.Rank = MathHelper.BitfieldGetLS1B(AllowedRanks);
            }

            return true;
        }

        public override bool Allow(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags)
        {
            return restrictionFlags.HasFlag(RestrictionTestFlags.Rank) == false || MathHelper.BitTestAll(AllowedRanks, filterArgs.Rank);
        }
    }

    public class RestrictionListPrototype : DropRestrictionPrototype
    {
        public DropRestrictionPrototype[] Children { get; protected set; }

        //---

        public override bool Adjust(DropFilterArguments filterArgs, ref RestrictionTestFlags adjustResultFlags, RestrictionTestFlags flagsToAdjust)
        {
            if (Children.IsNullOrEmpty())
                return false;

            foreach (DropRestrictionPrototype dropRestrictionProto in Children)
            {
                if (dropRestrictionProto.Adjust(filterArgs, ref adjustResultFlags, flagsToAdjust) == false)
                    return false;
            }

            return true;
        }

        public override bool Allow(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags)
        {
            if (Children.IsNullOrEmpty())
                return false;

            foreach (DropRestrictionPrototype dropRestrictionProto in Children)
            {
                if (dropRestrictionProto.Allow(filterArgs, restrictionFlags) == false)
                    return false;
            }

            return true;
        }
    }

    public class SlotRestrictionPrototype : DropRestrictionPrototype
    {
        public EquipmentInvUISlot[] AllowedSlots { get; protected set; }

        //---

        public override bool Adjust(DropFilterArguments filterArgs, ref RestrictionTestFlags adjustResultFlags, RestrictionTestFlags flagsToAdjust)
        {
            if (Allow(filterArgs, flagsToAdjust) == false)
            {
                if (flagsToAdjust.HasFlag(RestrictionTestFlags.Output))
                    return true;

                if (flagsToAdjust.HasFlag(RestrictionTestFlags.Slot) == false || AllowedSlots.IsNullOrEmpty())
                    return false;

                adjustResultFlags |= RestrictionTestFlags.Slot;
                filterArgs.Slot = AllowedSlots[0];
            }

            return true;
        }

        public override bool Allow(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags)
        {
            if (AllowedSlots.IsNullOrEmpty())
                return false;

            return restrictionFlags.HasFlag(RestrictionTestFlags.Slot) == false || AllowedSlots.Contains(filterArgs.Slot);
        }
    }

    public class UsableByRestrictionPrototype : DropRestrictionPrototype
    {
        public PrototypeId[] Avatars { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override bool Allow(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags)
        {
            if (Avatars.IsNullOrEmpty())
                return false;

            if (restrictionFlags.HasFlag(RestrictionTestFlags.UsableBy) == false)
                return true;

            if (filterArgs.LootContext == LootContext.Crafting)
            {
                if (filterArgs.RollFor != PrototypeId.Invalid && Avatars.Contains(filterArgs.RollFor) == false)
                    return false;
            }
            else
            {
                if (filterArgs.RollFor == PrototypeId.Invalid)
                    Logger.Warn($"Allow(): RollFor is invalid, but context is not Crafting! RestrictionTestFlags=[{restrictionFlags}] Args=[{filterArgs}]");
            }

            ItemPrototype itemProto = filterArgs.ItemProto as ItemPrototype;
            if (itemProto == null) return Logger.WarnReturn(false, "Allow(): itemProto == null");

            foreach (PrototypeId avatarProtoRef in Avatars)
            {
                AgentPrototype agentProto = avatarProtoRef.As<AgentPrototype>();
                if (itemProto.IsUsableByAgent(agentProto))
                    return true;
            }

            return false;
        }
    }

    public class DistanceRestrictionPrototype : DropRestrictionPrototype
    {
        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override bool Allow(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags)
        {
            if (Segment.IsNearZero(filterArgs.DropDistanceSq))
                return true;

            LootGlobalsPrototype lootGlobalsProto = GameDatabase.LootGlobalsPrototype;
            if (lootGlobalsProto == null) return Logger.WarnReturn(false, "Allow(): lootGlobalsProto == null");

            float dropDistanceThresholdSq = lootGlobalsProto.DropDistanceThreshold * lootGlobalsProto.DropDistanceThreshold;
            return dropDistanceThresholdSq > filterArgs.DropDistanceSq;
        }
    }
}
