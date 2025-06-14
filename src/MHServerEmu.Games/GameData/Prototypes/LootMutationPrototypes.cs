using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData.Tables;
using MHServerEmu.Games.Loot;

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
    }

    public class LootMutateAvatarPrototype : LootMutationPrototype
    {
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
    }

    public class LootMutateSlotPrototype : LootMutationPrototype
    {
        public EquipmentInvUISlot Slot { get; protected set; }
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
    }

    public class LootEvalPrototype : LootMutationPrototype
    {
        public EvalPrototype Eval { get; protected set; }
    }
}
