using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData.Tables;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Properties.Evals;

namespace MHServerEmu.Games.GameData.Prototypes
{
    // TODO: Add pooling for temporary ItemSpec instances if it creates a problem with garbage collection.

    public class LootMutationPrototype : Prototype
    {
        //---

        public virtual MutationResults Mutate(LootRollSettings settings, IItemResolver resolver, LootCloneRecord lootCloneRecord)
        {
            return MutationResults.None;
        }

        public virtual void OnItemCreated(Item item)
        {
        }

        protected MutationResults FinalizeMutation(IItemResolver resolver, LootCloneRecord lootCloneRecord)
        {
            if (resolver.CheckItem(lootCloneRecord, lootCloneRecord.RestrictionFlags, true) == false)
                return MutationResults.Error;

            ItemSpec itemSpec = new(lootCloneRecord);
            MutationResults affixResults = LootUtilities.UpdateAffixes(resolver, lootCloneRecord, AffixCountBehavior.Keep, itemSpec, null);

            if (affixResults != MutationResults.None && affixResults.HasFlag(MutationResults.Error) == false)
                lootCloneRecord.SetAffixes(itemSpec.AffixSpecs);

            return affixResults;
        }
    }

    public class LootAddAffixesPrototype : LootMutationPrototype
    {
        public AssetId[] Keywords { get; protected set; }
        public short Count { get; protected set; }
        public AffixPosition Position { get; protected set; }
        public PrototypeId[] Categories { get; protected set; }       // VectorPrototypeRefPtr AffixCategoryPrototype 

        //---

        public override MutationResults Mutate(LootRollSettings settings, IItemResolver resolver, LootCloneRecord lootCloneRecord)
        {
            ItemSpec itemSpec = new(lootCloneRecord);
            MutationResults affixResults = LootUtilities.AddAffixes(resolver, lootCloneRecord, Count, itemSpec, Position, Categories, Keywords, settings);
            
            if (affixResults.HasFlag(MutationResults.Error))
                return MutationResults.Error;

            lootCloneRecord.SetAffixes(itemSpec.AffixSpecs);

            return affixResults;
        }
    }

    public class LootApplyNoVisualsOverridePrototype : LootMutationPrototype
    {
        //---

        public override MutationResults Mutate(LootRollSettings settings, IItemResolver resolver, LootCloneRecord lootCloneRecord)
        {
            ItemSpec itemSpec = new(lootCloneRecord);

            bool result = itemSpec.DisableEquipEngineEffects();

            lootCloneRecord.SetAffixes(itemSpec.AffixSpecs);

            return result ? MutationResults.Changed : MutationResults.None;
        }
    }

    public class LootMutateBindingPrototype : LootMutationPrototype
    {
        public LootBindingType Binding { get; protected set; }

        //---

        public override MutationResults Mutate(LootRollSettings settings, IItemResolver resolver, LootCloneRecord lootCloneRecord)
        {
            ItemSpec itemSpec = new(lootCloneRecord);

            bool result = false;

            switch (Binding)
            {
                case LootBindingType.None:
                    if (itemSpec.SetBindingState(false))
                    {
                        lootCloneRecord.RestrictionFlags &= ~RestrictionTestFlags.UsableBy;
                        result = true;
                    }

                    break;

                case LootBindingType.TradeRestricted:
                    result = itemSpec.SetTradeRestricted(true, false);
                    break;

                case LootBindingType.TradeRestrictedRemoveBinding:
                    if (itemSpec.SetTradeRestricted(true, true))
                    {
                        lootCloneRecord.RestrictionFlags &= ~RestrictionTestFlags.UsableBy;
                        result = true;
                    }
                    break;

                case LootBindingType.Avatar:
                    PrototypeId equippableBy = itemSpec.EquippableBy;
                    PrototypeId avatarProtoRef = equippableBy != PrototypeId.Invalid ? equippableBy : lootCloneRecord.RollFor;
                    result = itemSpec.SetBindingState(true, avatarProtoRef);                    
                    break;
            }

            lootCloneRecord.SetAffixes(itemSpec.AffixSpecs);

            return result ? MutationResults.Changed : MutationResults.None;
        }
    }

    public class LootClampLevelPrototype : LootMutationPrototype
    {
        public int MaxLevel { get; protected set; }
        public int MinLevel { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();

            MinLevel = Math.Max(0, MinLevel);

            if (MaxLevel != 0)
                MaxLevel = Math.Max(MinLevel, MaxLevel);
        }

        public override MutationResults Mutate(LootRollSettings settings, IItemResolver resolver, LootCloneRecord lootCloneRecord)
        {
            int level = lootCloneRecord.Level;

            level = MaxLevel > 0
                ? Math.Clamp(level, MinLevel, MaxLevel)
                : Math.Max(level, MinLevel);

            if (level == lootCloneRecord.Level)
                return MutationResults.None;

            lootCloneRecord.Level = level;
            lootCloneRecord.Rank = 0;

            return FinalizeMutation(resolver, lootCloneRecord) | MutationResults.Changed;
        }
    }

    public class LootCloneAffixesPrototype : LootMutationPrototype
    {
        public AssetId[] Keywords { get; protected set; }
        public int SourceIndex { get; protected set; }
        public AffixPosition Position { get; protected set; }
        public bool EnforceAffixLimits { get; protected set; }
        public PrototypeId[] Categories { get; protected set; }    // VectorPrototypeRefPtr AffixCategoryPrototype 

        //---

        public override MutationResults Mutate(LootRollSettings settings, IItemResolver resolver, LootCloneRecord lootCloneRecord)
        {
            using LootCloneRecord sourceRecord = ObjectPoolManager.Instance.Get<LootCloneRecord>();

            if (SourceIndex < 0 || resolver.InitializeCloneRecordFromSource(SourceIndex, sourceRecord) == false)
                return MutationResults.Error;

            ItemSpec sourceItemSpec = new(sourceRecord);
            ItemSpec destItemSpec = new(lootCloneRecord);

            MutationResults result = LootUtilities.CopyAffixes(resolver, lootCloneRecord, sourceItemSpec, destItemSpec, Position, Keywords, Categories, EnforceAffixLimits);
            if (result.HasFlag(MutationResults.Error))
                return MutationResults.Error;

            lootCloneRecord.SetAffixes(destItemSpec.AffixSpecs);

            return result;
        }
    }

    public class LootCloneBuiltinAffixesPrototype : LootMutationPrototype
    {
        public AssetId[] Keywords { get; protected set; }
        public int SourceIndex { get; protected set; }
        public AffixPosition Position { get; protected set; }
        public bool EnforceAffixLimits { get; protected set; }
        public PrototypeId[] Categories { get; protected set; }    // VectorPrototypeRefPtr AffixCategoryPrototype 

        //---

        public override MutationResults Mutate(LootRollSettings settings, IItemResolver resolver, LootCloneRecord lootCloneRecord)
        {
            using LootCloneRecord sourceRecord = ObjectPoolManager.Instance.Get<LootCloneRecord>();

            if (SourceIndex < 0 || resolver.InitializeCloneRecordFromSource(SourceIndex, sourceRecord) == false)
                return MutationResults.Error;

            ItemSpec sourceItemSpec = new(sourceRecord);
            ItemSpec destItemSpec = new(lootCloneRecord);

            MutationResults result = LootUtilities.CopyBuiltinAffixes(resolver, lootCloneRecord, sourceItemSpec, destItemSpec, Position, Keywords, Categories, EnforceAffixLimits);
            if (result.HasFlag(MutationResults.Error))
                return MutationResults.Error;

            lootCloneRecord.SetAffixes(destItemSpec.AffixSpecs);

            return result;
        }
    }

    public class LootCloneLevelPrototype : LootMutationPrototype
    {
        public int SourceIndex { get; protected set; }

        //---

        public override MutationResults Mutate(LootRollSettings settings, IItemResolver resolver, LootCloneRecord lootCloneRecord)
        {
            using LootCloneRecord sourceRecord = ObjectPoolManager.Instance.Get<LootCloneRecord>();

            if (SourceIndex < 0 || resolver.InitializeCloneRecordFromSource(SourceIndex, sourceRecord) == false)
                return MutationResults.Error;

            if (lootCloneRecord.Level == sourceRecord.Level)
                return MutationResults.None;

            lootCloneRecord.Level = sourceRecord.Level;
            lootCloneRecord.Rank = 0;

            return FinalizeMutation(resolver, lootCloneRecord) | MutationResults.Changed;
        }
    }

    public class LootDropAffixesPrototype : LootMutationPrototype
    {
        public AssetId[] Keywords { get; protected set; }
        public AffixPosition Position { get; protected set; }
        public PrototypeId[] Categories { get; protected set; }    // VectorPrototypeRefPtr AffixCategoryPrototype 

        //---

        public override MutationResults Mutate(LootRollSettings settings, IItemResolver resolver, LootCloneRecord lootCloneRecord)
        {
            ItemSpec itemSpec = new(lootCloneRecord);
            MutationResults affixResults = LootUtilities.DropAffixes(resolver, lootCloneRecord, itemSpec, Position, Keywords, Categories);

            if (affixResults.HasFlag(MutationResults.Error))
                return MutationResults.Error;

            lootCloneRecord.SetAffixes(itemSpec.AffixSpecs);

            return affixResults;
        }
    }

    public class LootMutateAffixesPrototype : LootMutationPrototype
    {
        public AssetId[] NewItemKeywords { get; protected set; }
        public AssetId[] OldItemKeywords { get; protected set; }
        public bool OnlyReplaceIfAllMatched { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override MutationResults Mutate(LootRollSettings settings, IItemResolver resolver, LootCloneRecord lootCloneRecord)
        {
            const uint PositionMask = (1u << (int)AffixPosition.Prefix) |
                                      (1u << (int)AffixPosition.Suffix) |
                                      (1u << (int)AffixPosition.Ultimate) |
                                      (1u << (int)AffixPosition.Cosmic) |
                                      (1u << (int)AffixPosition.Unique) |
                                      (1u << (int)AffixPosition.TeamUp);
                
            AffixPickerTable pickerTable = new();
            pickerTable.Initialize(PositionMask, resolver.Random);
            LootUtilities.BuildAffixPickers(pickerTable, lootCloneRecord, NewItemKeywords, resolver.Region);

            MutationResults result = MutationResults.None;
            ItemSpec itemSpec = new(lootCloneRecord);
            AffixSpec affixSpec = new();

            HashSet<ScopedAffixRef> affixSet = HashSetPool<ScopedAffixRef>.Instance.Get();

            try
            {
                List<AffixRecord> affixRecords = lootCloneRecord.AffixRecords;
                for (int i = 0; i < affixRecords.Count; i++)
                {
                    AffixPrototype affixProto = affixRecords[i].AffixProtoRef.As<AffixPrototype>();
                    if (affixProto == null)
                    {
                        Logger.Warn("Mutate(): affixProto == null");
                        continue;
                    }

                    if (affixProto.HasKeywords(OldItemKeywords, OnlyReplaceIfAllMatched) == false)
                        continue;

                    Picker<AffixPrototype> picker = pickerTable.GetPicker(affixProto.Position);
                    if (picker == null)
                        continue;

                    result |= affixSpec.RollAffix(resolver.Random, lootCloneRecord.RollFor, itemSpec, picker, affixSet);
                    
                    if (result.HasFlag(MutationResults.Error))
                        return result;

                    affixRecords[i] = new(affixSpec);
                    result |= MutationResults.AffixChange;
                }

                return FinalizeMutation(resolver, lootCloneRecord) | result;
            }
            finally
            {
                HashSetPool<ScopedAffixRef>.Instance.Return(affixSet);
            }
        }
    }

    public class LootMutateAvatarPrototype : LootMutationPrototype
    {
        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override MutationResults Mutate(LootRollSettings settings, IItemResolver resolver, LootCloneRecord lootCloneRecord)
        {
            AvatarPrototype rollFor = resolver.ResolveAvatarPrototype(settings.UsableAvatar, settings.ForceUsable, settings.UsablePercent);
            if (rollFor.DataRef == lootCloneRecord.RollFor)
                return MutationResults.None;

            lootCloneRecord.RollFor = rollFor.DataRef;

            MutationResults result = MutationResults.None;

            if (resolver.CheckItem(lootCloneRecord, RestrictionTestFlags.All, true) == false)
            {
                using LootCloneRecord lootCloneRecordCopy = ObjectPoolManager.Instance.Get<LootCloneRecord>();
                LootCloneRecord.Initialize(lootCloneRecordCopy, lootCloneRecord);

                result = CreateItemForAvatar(resolver, lootCloneRecordCopy.RollFor, lootCloneRecordCopy, lootCloneRecord);
                if (result.HasFlag(MutationResults.Error) == false)
                    result |= MutationResults.Changed;
            }

            return result;
        }

        private static MutationResults CreateItemForAvatar(IItemResolver resolver, PrototypeId rollFor, LootCloneRecord sourceItem, LootCloneRecord destItem)
        {
            MutationResults result = MutationResults.None;

            // Pick new base type
            Picker<Prototype> picker = new(resolver.Random);

            if (sourceItem.Slot == EquipmentInvUISlot.Invalid)
                GameDataTables.Instance.LootPickingTable.GetConcreteLootPicker(picker, sourceItem.ItemProto.DataRef, rollFor.As<AgentPrototype>());
            else
                LootUtilities.BuildInventoryLootPicker(picker, rollFor, sourceItem.Slot);

            if (LootUtilities.PickValidItem(resolver, picker, null, sourceItem, out ItemPrototype itemProto) == false)
                return MutationResults.Error;

            destItem.ItemProto = itemProto;
            destItem.EquippableBy = rollFor;
            destItem.RollFor = rollFor;

            if (itemProto != sourceItem.ItemProto)
                result |= MutationResults.ItemPrototypeChange;

            // Update affixes
            HashSet<ScopedAffixRef> affixSet = HashSetPool<ScopedAffixRef>.Instance.Get();

            ItemSpec itemSpec = new(destItem);
            for (int i = 0; i < itemSpec.AffixSpecs.Count; i++)
            {
                AffixSpec itAffixSpec = itemSpec.AffixSpecs[i];

                if (itAffixSpec.AffixProto == null || itAffixSpec.Seed == 0)
                {
                    Logger.Warn($"CreateItemForAvatar(): Invalid affix spec: affixSpec=[{itAffixSpec}], args=[{destItem}], itemSpec=[{itemSpec}]");
                    continue;
                }

                if (itAffixSpec.AffixProto.IsGemAffix)
                    continue;

                itAffixSpec.SetScope(resolver.Random, rollFor, itemSpec, affixSet, BehaviorOnPowerMatch.Cancel);
            }

            MutationResults affixResult = LootUtilities.UpdateAffixes(resolver, destItem, AffixCountBehavior.Keep, itemSpec, null);
            if (affixResult != MutationResults.None && affixResult.HasFlag(MutationResults.Error) == false)
                destItem.SetAffixes(itemSpec.AffixSpecs);

            HashSetPool<ScopedAffixRef>.Instance.Return(affixSet);
            return result | affixResult;
        }
    }

    public class LootMutateLevelPrototype : LootMutationPrototype
    {
        //---

        public override MutationResults Mutate(LootRollSettings settings, IItemResolver resolver, LootCloneRecord lootCloneRecord)
        {
            int level = resolver.ResolveLevel(settings.Level, settings.UseLevelVerbatim);
            if (lootCloneRecord.Level == level)
                return MutationResults.None;

            lootCloneRecord.Level = level;
            lootCloneRecord.Rank = 0;

            return FinalizeMutation(resolver, lootCloneRecord) | MutationResults.Changed;
        }
    }

    public class OffsetLootLevelPrototype : LootMutationPrototype
    {
        public int LevelOffset { get; protected set; }

        //---

        public override MutationResults Mutate(LootRollSettings settings, IItemResolver resolver, LootCloneRecord lootCloneRecord)
        {
            int level = resolver.ResolveLevel(lootCloneRecord.Level + LevelOffset, true);
            if (lootCloneRecord.Level == level)
                return MutationResults.None;

            lootCloneRecord.Level = level;
            lootCloneRecord.Rank = 0;

            return FinalizeMutation(resolver, lootCloneRecord) | MutationResults.Changed;
        }
    }

    public class LootMutateRankPrototype : LootMutationPrototype
    {
        public int Rank { get; protected set; }

        //---

        public override MutationResults Mutate(LootRollSettings settings, IItemResolver resolver, LootCloneRecord lootCloneRecord)
        {
            if (Rank == 0)
                return MutationResults.Error;

            if (lootCloneRecord.Rank == Rank)
                return MutationResults.None;

            Picker<Prototype> concretePicker = new(resolver.Random);
            Picker<Prototype> filteredPicker = new(resolver.Random);

            if (lootCloneRecord.Slot != EquipmentInvUISlot.Invalid && lootCloneRecord.Slot != EquipmentInvUISlot.Costume)
                LootUtilities.BuildInventoryLootPicker(concretePicker, lootCloneRecord.RollFor, lootCloneRecord.Slot);
            else
                GameDataTables.Instance.LootPickingTable.GetConcreteLootPicker(concretePicker, lootCloneRecord.ItemProto.DataRef, lootCloneRecord.RollFor.As<AgentPrototype>());

            lootCloneRecord.Rank = Rank;

            Prototype pickedProto = null;
            while (concretePicker.PickRemove(out pickedProto))
            {
                const RestrictionTestFlags FilterFlags = RestrictionTestFlags.All & ~RestrictionTestFlags.Level;

                lootCloneRecord.ItemProto = pickedProto;
                if (pickedProto is ItemPrototype itemProto && itemProto.IsDroppableForRestrictions(lootCloneRecord, FilterFlags))
                    filteredPicker.Add(pickedProto);
            }

            ItemPrototype pickedItemProto = pickedProto as ItemPrototype;

            if (filteredPicker.Empty() || filteredPicker.Pick(out Prototype pickedFilteredProto) == false)
                return MutationResults.Error;

            lootCloneRecord.ItemProto = pickedFilteredProto;

            // NOTE: This is checking the last prototype added to the filtered picker, and not the prototype that was picked after filtering.
            // This does not look like it was intended, but this is how it is implemented in the client. Fix this if needed.
            if (pickedItemProto?.MakeRestrictionsDroppable(lootCloneRecord, RestrictionTestFlags.Level, out _) == false)
                return MutationResults.Error;

            return FinalizeMutation(resolver, lootCloneRecord) | MutationResults.Changed | MutationResults.ItemPrototypeChange;
        }
    }

    public class LootMutateRarityPrototype : LootMutationPrototype
    {
        public bool RerollAffixCount { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override MutationResults Mutate(LootRollSettings settings, IItemResolver resolver, LootCloneRecord lootCloneRecord)
        {
            int level = resolver.ResolveLevel(lootCloneRecord.Level, true);
            PrototypeId rarityProtoRef = resolver.ResolveRarity(settings.Rarities, level, null);

            if (lootCloneRecord.Rarity == rarityProtoRef)
                return MutationResults.None;

            lootCloneRecord.Rarity = rarityProtoRef;

            ItemPrototype itemProto = lootCloneRecord.ItemProto as ItemPrototype;
            if (itemProto == null) return Logger.WarnReturn(MutationResults.Error, "Mutate(): itemProto == null");

            if (itemProto.IsDroppableForRestrictions(lootCloneRecord, RestrictionTestFlags.Level) == false)
                itemProto.MakeRestrictionsDroppable(lootCloneRecord, RestrictionTestFlags.Level, out _);

            MutationResults result = MutationResults.Changed;

            if (RerollAffixCount)
            {
                ItemSpec itemSpec = new(lootCloneRecord);

                result |= LootUtilities.UpdateAffixes(resolver, lootCloneRecord, AffixCountBehavior.Roll, itemSpec, null);

                if (result.HasFlag(MutationResults.Error))
                    return result;

                lootCloneRecord.SetAffixes(itemSpec.AffixSpecs);
            }

            return FinalizeMutation(resolver, lootCloneRecord) | result;
        }
    }

    public class LootMutateSlotPrototype : LootMutationPrototype
    {
        public EquipmentInvUISlot Slot { get; protected set; }

        //---

        public override MutationResults Mutate(LootRollSettings settings, IItemResolver resolver, LootCloneRecord lootCloneRecord)
        {
            if (lootCloneRecord.Slot == Slot)
                return MutationResults.None;

            if (lootCloneRecord.Slot == EquipmentInvUISlot.Invalid || lootCloneRecord.Slot == EquipmentInvUISlot.Costume)
                return MutationResults.Error;

            if (Slot == EquipmentInvUISlot.Invalid || Slot == EquipmentInvUISlot.Costume)
                return MutationResults.Error;

            MutationResults result = MutationResults.Changed;

            lootCloneRecord.Slot = Slot;
            lootCloneRecord.Rank = 0;

            Picker<Prototype> picker = new(resolver.Random);
            LootUtilities.BuildInventoryLootPicker(picker, lootCloneRecord.RollFor, Slot);

            if (LootUtilities.PickValidItem(resolver, picker, null, lootCloneRecord, out ItemPrototype itemProto) == false)
                return MutationResults.Error;

            if (lootCloneRecord.ItemProto != itemProto)
            {
                lootCloneRecord.ItemProto = itemProto;
                result |= MutationResults.ItemPrototypeChange;
            }

            return FinalizeMutation(resolver, lootCloneRecord) | result;
        }
    }

    public class LootMutateBuiltinSeedPrototype : LootMutationPrototype
    {
        //---

        public override MutationResults Mutate(LootRollSettings settings, IItemResolver resolver, LootCloneRecord lootCloneRecord)
        {
            lootCloneRecord.Seed = resolver.Random.Next();
            return MutationResults.Changed;
        }
    }

    public class LootMutateAffixSeedPrototype : LootMutationPrototype
    {
        public AssetId[] Keywords { get; protected set; }
        public AffixPosition Position { get; protected set; }
        public PrototypeId[] Categories { get; protected set; }    // VectorPrototypeRefPtr AffixCategoryPrototype 

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override MutationResults Mutate(LootRollSettings settings, IItemResolver resolver, LootCloneRecord lootCloneRecord)
        {
            bool hasKeywords = Keywords.HasValue();
            bool hasCategories = Categories.HasValue();

            for (int i = 0; i < lootCloneRecord.AffixRecords.Count; i++)
            {
                AffixRecord affixRecord = lootCloneRecord.AffixRecords[i];

                AffixPrototype affixProto = affixRecord.AffixProtoRef.As<AffixPrototype>();
                if (affixProto == null) return Logger.WarnReturn(MutationResults.Error, "Mutate(): affixProto == null");

                if (affixProto.Position == AffixPosition.Metadata)
                    continue;

                if (Position != AffixPosition.None && affixProto.Position != Position)
                    continue;

                if (hasKeywords && affixProto.HasKeywords(Keywords, false) == false)
                    continue;

                if (hasCategories && affixProto.HasAnyCategory(Categories) == false)
                    continue;

                lootCloneRecord.AffixRecords[i] = affixRecord.SetSeed(resolver.Random.Next() | 1);
            }

            return MutationResults.Changed;
        }
    }

    public class LootReplaceAffixesPrototype : LootMutationPrototype
    {
        public int SourceIndex { get; protected set; }
        public AssetId[] Keywords { get; protected set; }
        public AffixPosition Position { get; protected set; }
        public bool EnforceAffixLimits { get; protected set; }
        public PrototypeId[] Categories { get; protected set; }    // VectorPrototypeRefPtr AffixCategoryPrototype 

        //---

        public override MutationResults Mutate(LootRollSettings settings, IItemResolver resolver, LootCloneRecord lootCloneRecord)
        {
            using LootCloneRecord sourceRecord = ObjectPoolManager.Instance.Get<LootCloneRecord>();

            if (SourceIndex < 0 || resolver.InitializeCloneRecordFromSource(SourceIndex, sourceRecord) == false)
                return MutationResults.Error;

            ItemSpec sourceItemSpec = new(sourceRecord);
            ItemSpec destItemSpec = new(lootCloneRecord);

            MutationResults result = LootUtilities.ReplaceAffixes(resolver, lootCloneRecord, sourceItemSpec, destItemSpec, Position, Keywords, Categories, EnforceAffixLimits);
            if (result.HasFlag(MutationResults.Error))
                return MutationResults.Error;

            lootCloneRecord.SetAffixes(destItemSpec.AffixSpecs);

            return result;
        }
    }

    public class LootCloneSeedPrototype : LootMutationPrototype
    {
        public int SourceIndex { get; protected set; }

        //---

        public override MutationResults Mutate(LootRollSettings settings, IItemResolver resolver, LootCloneRecord lootCloneRecord)
        {
            using LootCloneRecord sourceRecord = ObjectPoolManager.Instance.Get<LootCloneRecord>();

            if (SourceIndex < 0 || resolver.InitializeCloneRecordFromSource(SourceIndex, sourceRecord) == false)
                return MutationResults.Error;

            lootCloneRecord.Seed = sourceRecord.Seed;

            return MutationResults.Changed;
        }
    }

    public class LootAddAffixPrototype : LootMutationPrototype
    {
        public PrototypeId Affix { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override MutationResults Mutate(LootRollSettings settings, IItemResolver resolver, LootCloneRecord lootCloneRecord)
        {
            if (Affix == PrototypeId.Invalid)
                return MutationResults.None;

            ItemSpec itemSpec = new(lootCloneRecord);

            AffixPrototype affixProto = Affix.As<AffixPrototype>();
            if (affixProto == null) return Logger.WarnReturn(MutationResults.Error, "Mutate(): affixProto == null");

            MutationResults affixResult = LootUtilities.AddAffix(resolver, lootCloneRecord, itemSpec, affixProto);
            if (affixResult.HasFlag(MutationResults.Error))
                return affixResult;

            lootCloneRecord.SetAffixes(itemSpec.AffixSpecs);

            return MutationResults.AffixChange;
        }
    }

    public class LootEvalPrototype : LootMutationPrototype
    {
        public EvalPrototype Eval { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override MutationResults Mutate(LootRollSettings settings, IItemResolver resolver, LootCloneRecord lootCloneRecord)
        {
            if (Eval == null)
                return MutationResults.None;

            if (resolver.PushCraftingCallback(this) == LootRollResult.NoRoll)
                return Logger.WarnReturn(MutationResults.Error, "Mutate(): resolver.PushCraftingCallback(this) == LootRollResult.NoRoll");

            return MutationResults.EvalChange;
        }

        public override void OnItemCreated(Item item)
        {
            if (Eval == null)
                return;

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetVar_PropertyCollectionPtr(EvalContext.Default, item?.Properties);

            if (Properties.Evals.Eval.RunBool(Eval, evalContext) == false)
                Logger.Warn($"OnItemCreated(): The LootEvalPrototype Eval failed:\n: [{Eval.ExpressionString()}]");
        }
    }
}
