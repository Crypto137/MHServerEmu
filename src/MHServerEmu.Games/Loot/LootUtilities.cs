using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Tables;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Loot
{
    public static class LootUtilities
    {
        public static bool PickValidItem(IItemResolver resolver, Picker<Prototype> basePicker, AgentPrototype teamUpProto, DropFilterArguments filterArgs,
            ref ItemPrototype pickedItemProto, RestrictionTestFlags restrictionFlags, ref PrototypeId? rarityProtoRef)
        {
            pickedItemProto = null;

            using DropFilterArguments currentArgs = ObjectPoolManager.Instance.Get<DropFilterArguments>();
            DropFilterArguments.Initialize(currentArgs, filterArgs);    // Copy arguments to compare to what we started with

            while (pickedItemProto == null && (restrictionFlags.HasFlag(RestrictionTestFlags.Rarity) == false || currentArgs.Rarity != PrototypeId.Invalid))
            {
                Picker<Prototype> iterationPicker = new(basePicker);

                while (iterationPicker.PickRemove(out Prototype proto))
                {
                    ItemPrototype itemProto = proto as ItemPrototype;
                    if (!Verify.IsNotNull(itemProto))
                        continue;

                    currentArgs.ItemProto = itemProto;
                    currentArgs.RollFor = itemProto.GetRollForAgent(currentArgs.RollFor, teamUpProto);

                    if (resolver.CheckItem(currentArgs, restrictionFlags))
                    {
                        pickedItemProto = itemProto;
                        if (rarityProtoRef != null)
                            rarityProtoRef = currentArgs.Rarity;
                        break;
                    }
                }

                // Check other rarities if we have one a base one provided
                if (rarityProtoRef == null)
                    break;

                if (pickedItemProto == null && restrictionFlags.HasFlag(RestrictionTestFlags.Rarity))
                {
                    RarityPrototype rarityProto = currentArgs.Rarity.As<RarityPrototype>();
                    if (!Verify.IsNotNull(rarityProto))
                        break;

                    currentArgs.Rarity = rarityProto.DowngradeTo;
                    if (!Verify.IsTrue(currentArgs.Rarity != filterArgs.Rarity, $"Rarity loop detected [{currentArgs.Rarity.GetName()}]"))
                        break;
                }
            }

            return pickedItemProto != null;
        }

        public static bool PickValidItem(IItemResolver resolver, Picker<Prototype> basePicker, AgentPrototype teamUpProto, DropFilterArguments filterArgs,
            out ItemPrototype pickedItemProto)
        {
            pickedItemProto = null;
            PrototypeId? rarityProtoRef = null;
            return PickValidItem(resolver, basePicker, teamUpProto, filterArgs, ref pickedItemProto, RestrictionTestFlags.All, ref rarityProtoRef);
        }

        public static bool BuildInventoryLootPicker(Picker<Prototype> picker, PrototypeId avatarProtoRef, EquipmentInvUISlot slot)
        {
            AvatarPrototype avatarProto = avatarProtoRef.As<AvatarPrototype>();
            if (!Verify.IsNotNull(avatarProto)) return false;
            if (!Verify.IsTrue(avatarProto.EquipmentInventories.HasValue())) return false;

            picker.Clear();

            foreach (AvatarEquipInventoryAssignmentPrototype equipInvAssignmentProto in avatarProto.EquipmentInventories)
            {
                if (equipInvAssignmentProto.UISlot != slot)
                    continue;

                InventoryPrototype invProto = equipInvAssignmentProto.Inventory.As<InventoryPrototype>();
                if (!Verify.IsNotNull(invProto)) return false;

                foreach (PrototypeId typeRef in invProto.EntityTypeFilter)
                    GameDataTables.Instance.LootPickingTable.GetConcreteLootPicker(picker, typeRef, avatarProto);

                break;
            }

            return true;
        }

        #region Affixes

        public static MutationResults UpdateAffixes(IItemResolver resolver, DropFilterArguments args, AffixCountBehavior affixCountBehavior,
            ItemSpec itemSpec, LootRollSettings settings)
        {
            if (itemSpec.IsValid == false || itemSpec.ItemProtoRef != args.ItemProto.DataRef || itemSpec.RarityProtoRef != args.Rarity)
            {
                Verify.IsTrue(false, string.Format("Invalid input parameter(s) to UpdateAffixes():\n" +
                    "ItemSpec is valid: {0} [{1}]\n" +
                    "rollFor is valid: {2} [{3}]\n" +
                    "ItemSpec item/rarity matches DropFilterArgs: {4}\n" +
                    "args.ItemRef=[{5}], args.Rarity=[{6}]",
                    itemSpec.IsValid.ToString(),            // 0
                    itemSpec.ToString(),                    // 1
                    args.RollFor != PrototypeId.Invalid,    // 2
                    args.RollFor.GetName(),                 // 3
                    itemSpec.ItemProtoRef == args.ItemProto.DataRef && itemSpec.RarityProtoRef == args.Rarity,  // 4
                    args.ItemProto.ToString(),              // 5
                    args.Rarity.GetName()));                // 6

                return MutationResults.Error;
            }

            MutationResults result = MutationResults.None;

            if (affixCountBehavior == AffixCountBehavior.Roll)
            {
                using var affixSetHandle = HashSetPool<ScopedAffixRef>.Instance.Get(out HashSet<ScopedAffixRef> affixSet);
                result = UpdateAffixesHelper(resolver, settings, args, itemSpec, affixSet);
            }

            if (result.HasFlag(MutationResults.Error) == false)
                result |= itemSpec.OnAffixesRolled(resolver, args.RollFor);

            return result;
        }

        private static MutationResults UpdateAffixesHelper(IItemResolver resolver, LootRollSettings settings, DropFilterArguments args,
            ItemSpec itemSpec, HashSet<ScopedAffixRef> affixSet)
        {
            ItemPrototype itemProto = itemSpec.ItemProtoRef.As<ItemPrototype>();
            if (!Verify.IsNotNull(itemProto)) return MutationResults.Error;

            // Pet affixes are rolled separately
            if (itemProto.IsPetItem)
                return ItemPrototype.UpdatePetTechAffixes(resolver.Random, args.RollFor, itemSpec);

            MutationResults result = MutationResults.None;

            AffixLimitsPrototype affixLimits = itemProto.GetAffixLimits(args.Rarity, args.LootContext);

            // Apply affixes by category
            if ((affixLimits != null && affixLimits.CategorizedAffixes.HasValue()) ||
                (settings != null && settings.AffixLimitByCategoryModifierDict.Count > 0))
            {
                using var affixCategoryDictHandle = DictionaryPool<PrototypeId, short>.Instance.Get(out Dictionary<PrototypeId, short> affixCategoryDict);

                // Get category limits from the prototype
                if (affixLimits != null)
                {
                    foreach (CategorizedAffixEntryPrototype entry in affixLimits.CategorizedAffixes)
                        affixCategoryDict.GetValueRefOrAddDefault(entry.Category) += entry.MinAffixes;
                }

                // Apply modifiers from loot roll settings
                if (settings != null)
                {
                    foreach (var kvp in settings.AffixLimitByCategoryModifierDict)
                    {
                        ref short numAffixes = ref affixCategoryDict.GetValueRefOrAddDefault(kvp.Key);
                        numAffixes = (short)Math.Max(0, numAffixes + kvp.Value);
                    }
                }

                // Add categorized affixes
                foreach (var kvp in affixCategoryDict)
                {
                    AffixCategoryPrototype categoryProto = kvp.Key.As<AffixCategoryPrototype>();

                    short numAffixesCurrent = itemSpec.NumAffixesOfCategory(categoryProto);
                    int numAffixesNeeded = kvp.Value - numAffixesCurrent;
                    if (numAffixesNeeded > 0)
                        result |= AddCategorizedAffixesToItemSpec(resolver, args, categoryProto, numAffixesNeeded, itemSpec, affixSet);
                }
            }

            // Apply affixes by position
            if (affixLimits == null)
                return result;

            for (AffixPosition affixPosition = (AffixPosition)1; affixPosition < AffixPosition.NumPositions; affixPosition++)
            {
                // Skip slots that don't have positional affixes
                switch (affixPosition)
                {
                    case AffixPosition.Blessing:
                    case AffixPosition.Runeword:
                    case AffixPosition.PetTech1:
                    case AffixPosition.PetTech2:
                    case AffixPosition.PetTech3:
                    case AffixPosition.PetTech4:
                    case AffixPosition.PetTech5:
                        continue;
                }

                short affixLimitsMax = affixLimits.GetMax(affixPosition, settings);
                if (affixLimitsMax == 0)
                    continue;

                short affixLimitsMin = affixLimits.GetMin(affixPosition, settings);
                short numAffixesCurrent = itemSpec.NumAffixesOfPosition(affixPosition);
                int numAffixesNeeded = resolver.Random.Next(affixLimitsMin, affixLimitsMax + 1) - numAffixesCurrent;

                if (numAffixesNeeded > 0)
                    result |= AddPositionAffixesToItemSpec(resolver, args, affixPosition, numAffixesNeeded, itemSpec, affixSet);
            }

            return result;
        }

        public static MutationResults AddAffixes(IItemResolver resolver, DropFilterArguments args, short affixCountNeeded,
            ItemSpec itemSpec, AffixPosition position, PrototypeId[] categories, AssetId[] keywords, LootRollSettings settings)
        {
            if (!Verify.IsTrue(affixCountNeeded > 0, $"Trying to add 0 affixes! args: {args}"))
                return MutationResults.Error;

            if (!Verify.IsTrue(itemSpec.IsValid)) return MutationResults.Error;
            if (!Verify.IsTrue(itemSpec.ItemProtoRef == args.ItemProto.DataRef)) return MutationResults.Error;

            ItemPrototype itemProto = args.ItemProto as ItemPrototype;
            if (!Verify.IsNotNull(itemProto)) return MutationResults.Error;

            if (itemProto.IsPetItem)
                return ItemPrototype.UpdatePetTechAffixes(resolver.Random, args.RollFor, itemSpec);

            using var affixSetHandle = HashSetPool<ScopedAffixRef>.Instance.Get(out HashSet<ScopedAffixRef> affixSet);
            using var affixCountsHandle = ListPool<AffixCountData>.Instance.Get(out List<AffixCountData> affixCounts);

            affixCounts.Fill(default, (int)AffixPosition.NumPositions);

            if (GetCurrentAffixStats(resolver, args, itemSpec, affixCounts, affixSet) == false)
                return MutationResults.Error | MutationResults.ErrorReasonAffixStats;

            AffixLimitsPrototype affixLimits = itemProto.GetAffixLimits(args.Rarity, args.LootContext);

            MutationResults result = MutationResults.None;

            // Add position / category / keyword affixes based on what has been provided
            if (position != AffixPosition.None)
            {
                // Check limits
                if (affixLimits != null)
                {
                    if (itemSpec.NumAffixesOfPosition(position) + affixCountNeeded > affixLimits.GetMax(position, settings))
                        return MutationResults.Error;
                }

                result |= AddPositionAffixesToItemSpec(resolver, args, position, affixCountNeeded, itemSpec, affixSet, keywords, categories);
            }
            else if (categories.HasValue())
            {
                // Check limits
                if (affixLimits != null)
                {
                    foreach (PrototypeId categoryProtoRef in categories)
                    {
                        AffixCategoryPrototype categoryProto = categoryProtoRef.As<AffixCategoryPrototype>();
                        if (itemSpec.NumAffixesOfCategory(categoryProto) + affixCountNeeded > affixLimits.GetMax(categoryProto, settings))
                            return MutationResults.Error;
                    }
                }

                result |= AddCategorizedAffixesToItemSpec(resolver, args, categories, affixCountNeeded, itemSpec, affixSet, keywords);
            }
            else if (keywords.HasValue())
            {
                result |= AddKeywordAffixesToItemSpec(resolver, args, keywords, affixCountNeeded, itemSpec, affixSet);
            }

            if (result.HasFlag(MutationResults.Error) == false)
                result |= itemSpec.OnAffixesRolled(resolver, args.RollFor);

            return result;
        }

        public static MutationResults AddAffix(IItemResolver resolver, DropFilterArguments args, ItemSpec itemSpec, AffixPrototype affixProto)
        {
            using var affixSetHandle = HashSetPool<ScopedAffixRef>.Instance.Get(out HashSet<ScopedAffixRef> affixSet);
            using var affixCountsHandle = ListPool<AffixCountData>.Instance.Get(out List<AffixCountData> affixCounts);

            affixCounts.Fill(default, (int)AffixPosition.NumPositions);

            if (GetCurrentAffixStats(resolver, args, itemSpec, affixCounts, affixSet) == false)
                return MutationResults.Error;

            Picker<AffixPrototype> picker = new(resolver.Random);
            picker.Add(affixProto, 100);

            AffixSpec affixSpec = new();
            MutationResults result = affixSpec.RollAffix(resolver.Random, args.RollFor, itemSpec, picker, affixSet);
                
            if (result.HasFlag(MutationResults.Error))
                return result;

            itemSpec.AddAffixSpec(affixSpec);
            result |= itemSpec.OnAffixesRolled(resolver, args.RollFor);

            return result;
        }

        public static MutationResults DropAffixes(IItemResolver resolver, DropFilterArguments args,
            ItemSpec itemSpec, AffixPosition position, AssetId[] keywords, PrototypeId[] categories)
        {
            if (!Verify.IsTrue(itemSpec.IsValid)) return MutationResults.Error;

            MutationResults result = DropAffixes(resolver, itemSpec, position, keywords, categories);
            if (result.HasFlag(MutationResults.Error))
                return result;

            result |= UpdateAffixes(resolver, args, AffixCountBehavior.Keep, itemSpec, null);
            
            return result;
        }

        public static MutationResults CopyAffixes(IItemResolver resolver, DropFilterArguments args, ItemSpec sourceItemSpec,
            ItemSpec destItemSpec, AffixPosition position, AssetId[] keywords, PrototypeId[] categories, bool enforceAffixLimits)
        {
            if (!Verify.IsTrue(destItemSpec.IsValid)) return MutationResults.Error;
            if (!Verify.IsTrue(destItemSpec.ItemProtoRef == args.ItemProto.DataRef)) return MutationResults.Error;
            if (!Verify.IsNotNull(args.ItemProto)) return MutationResults.Error;

            ItemPrototype itemProto = args.ItemProto as ItemPrototype;
            if (!Verify.IsNotNull(itemProto)) return MutationResults.Error;

            if (itemProto.IsPetItem)
                return ItemPrototype.CopyPetTechAffixes(sourceItemSpec, destItemSpec, position);

            using var affixSetHandle = HashSetPool<ScopedAffixRef>.Instance.Get(out HashSet<ScopedAffixRef> affixSet);
            using var affixCountsHandle = ListPool<AffixCountData>.Instance.Get(out List<AffixCountData> affixCounts);

            affixCounts.Fill(default, (int)AffixPosition.NumPositions);

            if (GetCurrentAffixStats(resolver, args, destItemSpec, affixCounts, affixSet) == false)
                return MutationResults.Error | MutationResults.ErrorReasonAffixStats;

            AffixLimitsPrototype destAffixLimits = null;
            if (enforceAffixLimits)
            {
                destAffixLimits = itemProto.GetAffixLimits(args.Rarity, args.LootContext);
                if (!Verify.IsNotNull(destAffixLimits, $"Trying to EnforceAffixLimits where there is no affix limits available! args {args}"))
                    return MutationResults.Error;
            }

            return CopyAffixSpecs(resolver, sourceItemSpec, destItemSpec, destAffixLimits, args.RollFor, keywords, position, categories, affixCounts, affixSet);
        }

        public static MutationResults CopyBuiltinAffixes(IItemResolver resolver, DropFilterArguments args, ItemSpec sourceItemSpec,
            ItemSpec destItemSpec, AffixPosition position, AssetId[] keywords, PrototypeId[] categories, bool enforceAffixLimits)
        {
            if (!Verify.IsTrue(destItemSpec.IsValid)) return MutationResults.Error;
            if (!Verify.IsTrue(destItemSpec.ItemProtoRef == args.ItemProto.DataRef)) return MutationResults.Error;

            ItemPrototype destItemProto = args.ItemProto as ItemPrototype;
            if (!Verify.IsNotNull(destItemProto)) return MutationResults.Error;

            using var affixSetHandle = HashSetPool<ScopedAffixRef>.Instance.Get(out HashSet<ScopedAffixRef> affixSet);
            using var affixCountsHandle = ListPool<AffixCountData>.Instance.Get(out List<AffixCountData> affixCounts);
            using var builtInAffixDetailsListHandle = ListPool<BuiltInAffixDetails>.Instance.Get(out List<BuiltInAffixDetails> builtInAffixDetailsList);
            using var builtInAffixSpecsHandle = ListPool<AffixSpec>.Instance.Get(out List<AffixSpec> builtInAffixSpecs);

            affixCounts.Fill(default, (int)AffixPosition.NumPositions);

            if (GetCurrentAffixStats(resolver, args, destItemSpec, affixCounts, affixSet) == false)
                return MutationResults.Error | MutationResults.ErrorReasonAffixStats;

            ItemPrototype sourceItemProto = sourceItemSpec.ItemProtoRef.As<ItemPrototype>();
            if (!Verify.IsNotNull(sourceItemProto)) return MutationResults.Error;

            sourceItemProto.GenerateBuiltInAffixDetails(sourceItemSpec, builtInAffixDetailsList);

            if (builtInAffixDetailsList.Count == 0)
                return MutationResults.None;

            foreach (BuiltInAffixDetails builtInAffixDetails in builtInAffixDetailsList)
            {
                AffixEntryPrototype affixEntryProto = builtInAffixDetails.AffixEntryProto;
                if (!Verify.IsNotNull(affixEntryProto))
                    continue;

                AffixPrototype affixProto = affixEntryProto.Affix.As<AffixPrototype>();
                if (!Verify.IsNotNull(affixProto))
                    continue;

                AffixSpec affixSpec = new(affixProto, affixEntryProto.Power, builtInAffixDetails.Seed);
                builtInAffixSpecs.Add(affixSpec);
            }

            // This will remove any externally applied affixes (which we don't care about here)
            sourceItemSpec.SetAffixes(builtInAffixSpecs);

            AffixLimitsPrototype destAffixLimits = null;
            if (enforceAffixLimits)
            {
                destAffixLimits = destItemProto.GetAffixLimits(args.Rarity, args.LootContext);
                if (!Verify.IsNotNull(destAffixLimits, $"Trying to EnforceAffixLimits where there is no affix limits available! args {args}"))
                    return MutationResults.Error;
            }

            return CopyAffixSpecs(resolver, sourceItemSpec, destItemSpec, destAffixLimits, args.RollFor, keywords, position, categories, affixCounts, affixSet);
        }

        public static MutationResults ReplaceAffixes(IItemResolver resolver, DropFilterArguments args, ItemSpec sourceItemSpec,
            ItemSpec destItemSpec, AffixPosition position, AssetId[] keywords, PrototypeId[] categories, bool enforceAffixLimits)
        {
            if (!Verify.IsTrue(destItemSpec.IsValid)) return MutationResults.Error;

            MutationResults result = DropAffixes(resolver, destItemSpec, position, keywords, categories);
            if (result.HasFlag(MutationResults.Error))
                return result;

            result |= CopyAffixes(resolver, args, sourceItemSpec, destItemSpec, position, keywords, categories, enforceAffixLimits);

            return result;
        }
        
        public static uint BuildAffixPickers(AffixPickerTable pickerTable, DropFilterArguments args, AssetId[] keywords, Region region)
        {
            uint duplicateMask = 0;     // Cleared bit indicates that the position has an affix with DuplicateHandlingBehavior set to Append

            for (AffixPosition position = AffixPosition.None + 1; position < AffixPosition.NumPositions; position++)
            {
                duplicateMask |= 1u << (int)position;

                Picker<AffixPrototype> picker = pickerTable.GetPicker(position);
                if (picker == null)
                    continue;

                IReadOnlyList<AffixPrototype> affixes = GameDataTables.Instance.LootPickingTable.GetAffixesByPosition(position);
                if (!Verify.IsNotNull(affixes)) return 0;

                for (int i = 0; i < affixes.Count; i++)
                {
                    AffixPrototype affixProtoIt = affixes[i];
                    if (!Verify.IsNotNull(affixProtoIt))
                        continue;

                    if (affixProtoIt.AllowAttachment(args) == false || affixProtoIt.HasKeywords(keywords, true) == false)
                        continue;

                    if (affixProtoIt is AffixRegionRestrictedPrototype regionAffixProto)
                    {
                        if (region == null || regionAffixProto.MatchesRegion(region) == false)
                            continue;
                    }

                    if (affixProtoIt.DuplicateHandlingBehavior == DuplicateHandlingBehavior.Append)
                        duplicateMask &= ~(1u << (int)position);

                    picker.Add(affixProtoIt, affixProtoIt.Weight);
                }
            }

            return duplicateMask;
        }

        private static MutationResults AddCategorizedAffixesToItemSpec(IItemResolver resolver, DropFilterArguments args, AffixCategoryPrototype categoryProto, 
            int affixCountNeeded, ItemSpec itemSpec, HashSet<ScopedAffixRef> affixSet, AssetId[] keywords = null)
        {
            IReadOnlyList<AffixPrototype> affixes = GameDataTables.Instance.LootPickingTable.GetAffixesByCategory(categoryProto);
            if (!Verify.IsNotNull(affixes, $"Failed to get available affixes in category: {categoryProto}."))
                return MutationResults.Error;

            Picker<AffixPrototype> affixPicker = new(resolver.Random);
            TryAddAffixesToPicker(args, null, keywords, resolver.Region, affixes, affixPicker);

            MutationResults result = MutationResults.None;
            int affixCountAdded = 0;

            for (int i = 0; i < affixCountNeeded; i++)
            {
                AffixSpec affixSpec = new();
                result |= affixSpec.RollAffix(resolver.Random, args.RollFor, itemSpec, affixPicker, affixSet);

                if (result.HasFlag(MutationResults.Error) == false)
                {
                    itemSpec.AddAffixSpec(affixSpec);
                    affixCountAdded++;
                }
            }

            ValidateAddAffixCount(affixCountAdded, affixCountNeeded);
            return result;
        }

        private static MutationResults AddCategorizedAffixesToItemSpec(IItemResolver resolver, DropFilterArguments args, PrototypeId[] categories,
            int affixCountNeeded, ItemSpec itemSpec, HashSet<ScopedAffixRef> affixSet, AssetId[] keywords = null)
        {
            MutationResults result = MutationResults.None;

            foreach (PrototypeId categoryProtoRef in categories)
            {
                AffixCategoryPrototype categoryIt = categoryProtoRef.As<AffixCategoryPrototype>();
                if (!Verify.IsNotNull(categoryIt))
                    continue;

                result |= AddCategorizedAffixesToItemSpec(resolver, args, categoryIt, affixCountNeeded, itemSpec, affixSet, keywords);
            }

            return result;
        }

        private static MutationResults AddPositionAffixesToItemSpec(IItemResolver resolver, DropFilterArguments args, AffixPosition affixPosition,
            int affixCountNeeded, ItemSpec itemSpec, HashSet<ScopedAffixRef> affixSet, AssetId[] keywords = null, PrototypeId[] categories = null)
        {
            IReadOnlyList<AffixPrototype> affixes = GameDataTables.Instance.LootPickingTable.GetAffixesByPosition(affixPosition);
            if (!Verify.IsNotNull(affixes, $"Failed to get available affixes in position: {affixPosition}."))
                return MutationResults.Error;

            Picker<AffixPrototype> affixPicker = new(resolver.Random);
            TryAddAffixesToPicker(args, categories, keywords, resolver.Region, affixes, affixPicker);

            MutationResults result = MutationResults.None;
            int affixCountAdded = 0;

            for (int i = 0; i < affixCountNeeded; i++)
            {
                AffixSpec affixSpec = new();
                result |= affixSpec.RollAffix(resolver.Random, args.RollFor, itemSpec, affixPicker, affixSet);

                if (result.HasFlag(MutationResults.Error) == false)
                {
                    itemSpec.AddAffixSpec(affixSpec);
                    affixCountAdded++;
                }
            }

            ValidateAddAffixCount(affixCountAdded, affixCountNeeded);
            return result;
        }

        private static MutationResults AddKeywordAffixesToItemSpec(IItemResolver resolver, DropFilterArguments args, AssetId[] keywords,
            int affixCountNeeded, ItemSpec itemSpec, HashSet<ScopedAffixRef> affixSet)
        {
            Picker<AffixPrototype> affixPicker = new(resolver.Random);

            foreach (AssetId keywordIt in keywords)
            {
                IReadOnlyList<AffixPrototype> affixes = GameDataTables.Instance.LootPickingTable.GetAffixesByKeyword(keywordIt);
                if (!Verify.IsNotNull(affixes, $"Failed to get available affixes for keyword: {keywordIt.GetName()}."))
                    return MutationResults.Error;

                TryAddAffixesToPicker(args, null, keywords, resolver.Region, affixes, affixPicker);
            }

            MutationResults result = MutationResults.None;
            int affixCountAdded = 0;

            for (int i = 0; i < affixCountNeeded; i++)
            {
                AffixSpec affixSpec = new();
                result |= affixSpec.RollAffix(resolver.Random, args.RollFor, itemSpec, affixPicker, affixSet);

                if (result.HasFlag(MutationResults.Error) == false)
                {
                    itemSpec.AddAffixSpec(affixSpec);
                    affixCountAdded++;
                }
            }

            ValidateAddAffixCount(affixCountAdded, affixCountNeeded);
            return result;
        }

        private static void TryAddAffixesToPicker(DropFilterArguments args, PrototypeId[] categories, AssetId[] keywords,
            Region region, IReadOnlyList<AffixPrototype> affixPool, Picker<AffixPrototype> affixPicker)
        {
            int count = affixPool.Count;

            for (int i = 0; i < count; i++)
            {
                AffixPrototype affixProto = affixPool[i];

                if (affixProto.Weight <= 0)
                    continue;

                if (affixProto.AllowAttachment(args) == false)
                    continue;

                if (affixProto.HasKeywords(keywords, true) == false)
                    continue;

                if (affixProto.HasAnyCategory(categories) == false)
                    continue;

                if (affixProto is AffixRegionRestrictedPrototype regionRestrictedAffixProto)
                {
                    if (region == null || regionRestrictedAffixProto.MatchesRegion(region) == false)
                        continue;
                }

                affixPicker.Add(affixProto, affixProto.Weight);
            }
        }

        private static void ValidateAddAffixCount(int affixCountAdded, int affixCountNeeded)
        {
            // Most of the arguments in this function are unused in the client, so we have just a simple
            // needed / added count check.

            Verify.IsTrue(affixCountAdded == affixCountNeeded, $"The pool of affixes is too small for these parameters! affixCountAdded={affixCountAdded}, affixCountNeeded={affixCountNeeded}");
        }

        private static MutationResults DropAffixes(IItemResolver resolver, ItemSpec itemSpec, AffixPosition position, AssetId[] keywords, PrototypeId[] categories)
        {
            // NOTE: This is used by public DropAffixes() and ReplaceAffixes() functions.

            MutationResults result = MutationResults.None;
            
            using var filteredAffixSpecsHandle = ListPool<AffixSpec>.Instance.Get(out List<AffixSpec> filteredAffixSpecs);

            bool hasKeywords = keywords.HasValue();
            bool hasCategories = categories.HasValue();

            IReadOnlyList<AffixSpec> affixSpecs = itemSpec.AffixSpecs;
            for (int i = 0; i < affixSpecs.Count; i++)
            {
                AffixSpec affixSpec = affixSpecs[i];
                if (!Verify.IsNotNull(affixSpec.AffixProto, $"Invalid affix prototype in item!\nItem: {itemSpec}\nResolver: {resolver}"))
                    return MutationResults.Error;

                bool shouldDrop = true;

                // Metadata affixes are never dropped
                if (affixSpec.AffixProto.Position == AffixPosition.Metadata)
                    shouldDrop = false;

                // Check position
                if (shouldDrop && position != AffixPosition.None && affixSpec.AffixProto.Position != position)
                    shouldDrop = false;

                // Check keywords
                if (shouldDrop && hasKeywords && affixSpec.AffixProto.HasKeywords(keywords, true) == false)
                    shouldDrop = false;

                // Check categories
                if (shouldDrop && hasCategories && affixSpec.AffixProto.HasAnyCategory(categories) == false)
                    shouldDrop = false;

                // Not adding the affix to the filtered list drops it
                if (shouldDrop)
                    result |= MutationResults.AffixChange;
                else
                    filteredAffixSpecs.Add(affixSpec);
            }

            itemSpec.SetAffixes(filteredAffixSpecs);
            return result;
        }

        private static MutationResults CopyAffixSpecs(IItemResolver resolver, ItemSpec sourceItemSpec, ItemSpec destItemSpec,
            AffixLimitsPrototype affixLimits, PrototypeId rollFor, AssetId[] keywords, AffixPosition position, PrototypeId[] categories,
            List<AffixCountData> affixCounts, HashSet<ScopedAffixRef> affixSet)
        {
            bool hasKeywords = keywords.HasValue();
            bool hasCategories = categories.HasValue();

            MutationResults result = MutationResults.None;

            using var affixSpecsToAddHandle = ListPool<AffixSpec>.Instance.Get(out List<AffixSpec> affixSpecsToAdd);
            using var addedPositionCountsHandle = ListPool<int>.Instance.Get(out List<int> addedPositionCounts);
            using var addedCategoryCountsHandle = DictionaryPool<AffixCategoryPrototype, int>.Instance.Get(out Dictionary<AffixCategoryPrototype, int> addedCategoryCounts);

            addedPositionCounts.Fill(0, (int)AffixPosition.NumPositions);

            IReadOnlyList<AffixSpec> sourceAffixSpecs = sourceItemSpec.AffixSpecs;
            for (int i = 0; i < sourceAffixSpecs.Count; i++)
            {
                AffixSpec sourceAffixSpecIt = sourceAffixSpecs[i];
                AffixPrototype affixProto = sourceAffixSpecIt.AffixProto;
                if (!Verify.IsNotNull(affixProto)) return MutationResults.Error;

                // Filter affixes by provided position / keywords / categories
                if (position != AffixPosition.None && affixProto.Position != position)
                    continue;

                if (hasKeywords && affixProto.HasKeywords(keywords, true) == false)
                    continue;

                AffixCategoryPrototype categoryProto = null;
                if (hasCategories)
                {
                    categoryProto = affixProto.GetFirstCategoryMatch(categories);
                    if (categoryProto == null)
                        continue;
                }

                // Copy the affix
                AffixSpec affixSpecCopy = new(sourceAffixSpecIt);
                if (affixSpecCopy.SetScope(resolver.Random, rollFor, destItemSpec, affixSet, BehaviorOnPowerMatch.Ignore).HasFlag(MutationResults.Error))
                {
                    result |= MutationResults.Error;
                    break;
                }

                // Check for duplicates
                if (affixSet.Contains(new(affixProto.DataRef, affixSpecCopy.ScopeProtoRef)))
                {
                    switch (affixProto.DuplicateHandlingBehavior)
                    {
                        case DuplicateHandlingBehavior.Fail:
                            result |= MutationResults.Error;
                            break;

                        case DuplicateHandlingBehavior.Ignore:
                            break;

                        case DuplicateHandlingBehavior.Overwrite:
                        case DuplicateHandlingBehavior.Append:
                            Verify.IsTrue(false, $"Invalid DuplicateHandlingBehavior {affixProto.DuplicateHandlingBehavior} for {affixProto}");
                            result |= MutationResults.Error;
                            break;
                    }

                    if (result.HasFlag(MutationResults.Error))
                        break;
                }
                else
                {
                    // Allow the affix to be copied
                    affixSet.Add(new(affixSpecCopy.AffixProto.DataRef, affixSpecCopy.ScopeProtoRef));
                    affixSpecsToAdd.Add(affixSpecCopy);

                    if (categoryProto != null)
                        addedCategoryCounts.GetValueRefOrAddDefault(categoryProto)++;
                    else
                        addedPositionCounts[(int)affixProto.Position]++;
                }
            }

            // Check limits if needed
            if (affixLimits != null)
            {
                // Position limits
                for (AffixPosition positionIt = 0; positionIt < AffixPosition.NumPositions; positionIt++)
                {
                    int i = (int)positionIt;

                    if (addedPositionCounts[i] == 0)
                        continue;

                    if (affixCounts[i].AffixCount + addedPositionCounts[i] > affixLimits.GetMax(positionIt, null))
                    {
                        result |= MutationResults.Error;
                        break;
                    }
                }

                // Category limits
                if (addedCategoryCounts.Count > 0)
                {
                    foreach (var kvp in addedCategoryCounts)
                    {
                        if (destItemSpec.NumAffixesOfCategory(kvp.Key) + kvp.Value > affixLimits.GetMax(kvp.Key, null))
                        {
                            result |= MutationResults.Error;
                            break;
                        }
                    }
                }
            }

            // Add affix copies if there are no issues
            if (result.HasFlag(MutationResults.Error) == false)
                destItemSpec.AddAffixSpecs(affixSpecsToAdd);

            return result;
        }

        private static bool GetCurrentAffixStats(IItemResolver resolver, DropFilterArguments args, ItemSpec itemSpec,
            List<AffixCountData> affixCounts, HashSet<ScopedAffixRef> affixSet)
        {
            affixSet.Clear();

            if (itemSpec.AffixSpecs.Count == 0)
                return true;

            bool hasNoVisualsAffix = false;
            bool hasVisualAffix = false;

            GlobalsPrototype globalsProto = GameDatabase.GlobalsPrototype;
            if (!Verify.IsNotNull(globalsProto)) return false;

            for (int i = 0; i < itemSpec.AffixSpecs.Count; i++)
            {
                AffixSpec affixSpecIt = itemSpec.AffixSpecs[i];
                if (!Verify.IsTrue(affixSpecIt.IsValid, $"invalid affix spec: affixSpec=[{affixSpecIt}] args=[{args}] itemSpec=[{itemSpec}]"))
                    return false;

                affixSet.Add(new(affixSpecIt.AffixProto.DataRef, affixSpecIt.ScopeProtoRef));

                AffixPosition affixPos = affixSpecIt.AffixProto.Position;
                int affixPosIndex = (int)affixPos;
                if (!Verify.IsTrue(affixPosIndex >= 0 && affixPosIndex < affixCounts.Count, $"Invalid affix position on item! itemSpec=[{itemSpec}]"))
                    continue;

                switch (affixPos)
                {
                    case AffixPosition.Metadata:
                        break;

                    case AffixPosition.Visual:
                        if (affixSpecIt.AffixProto.DataRef == globalsProto.ItemNoVisualsAffix)
                            hasNoVisualsAffix = true;
                        else
                            hasVisualAffix = true;
                        break;

                    default:
                        affixCounts[affixPosIndex] = affixCounts[affixPosIndex].IncrementCount();
                        break;
                }
            }

            if (hasVisualAffix && hasNoVisualsAffix)
            {
                Verify.IsTrue(false, $"item has both an externally applied visual affix and the no-visuals metadata affix! itemSpec: {itemSpec}");
                return false;
            }

            return true;
        }

        #endregion

        #region Time

        public static bool GetLastLootCooldownRolloverWallTime(PropertyCollection properties, TimeSpan currentTime, out TimeSpan lastRolloverTime)
        {
            lastRolloverTime = default;

            // NOTE: Properties store time with millisecond precision, so we need to reduce the precision here.
            currentTime = TimeSpan.FromMilliseconds((long)currentTime.TotalMilliseconds);

            // Remove the day part from the current time for calculations
            TimeSpan currentTime24Hr = new(0, currentTime.Hours, currentTime.Minutes, currentTime.Seconds, currentTime.Milliseconds);

            Weekday currentWeekday = GetCurrentWeekday(false);
            if (!Verify.IsTrue(currentWeekday != Weekday.All)) return false;

            PropertyList.Iterator itRolloverTimeProp = properties.IteratePropertyRange(PropertyEnum.LootCooldownRolloverWallTime);
            if (!Verify.IsTrue(itRolloverTimeProp.GetEnumerator().MoveNext())) return false;

            TimeSpan lastRolloverTimeDelta = TimeSpan.FromDays(7 + 1);  // 7 days per week + 1, a rollover must have happened within period

            foreach (var kvp in itRolloverTimeProp)
            {
                // Get rollover time and weekday from the property
                float wallClockTime24HrHours = kvp.Value;
                wallClockTime24HrHours = Math.Clamp(wallClockTime24HrHours, 0f, 23.99f);
                TimeSpan wallClockTime24Hr = TimeSpan.FromHours(wallClockTime24HrHours);

                Property.FromParam(kvp.Key, 1, out int wallClockTimeDayInt);
                Weekday wallClockTimeDay = (Weekday)wallClockTimeDayInt;

                TimeSpan newLastRolloverTimeDelta;

                if (wallClockTimeDay == Weekday.All)
                {
                    // Daily loot
                    if (currentTime24Hr > wallClockTime24Hr)
                    {
                        // Daily rollover has already happened
                        newLastRolloverTimeDelta = currentTime24Hr - wallClockTime24Hr;
                    }
                    else
                    {
                        // Daily rollover has not happened yet
                        newLastRolloverTimeDelta = currentTime24Hr + (TimeSpan.FromDays(1) - wallClockTime24Hr);
                    }
                }
                else
                {
                    // Weekly loot

                    // Calculate how many days (if any) have passed since the last rollover
                    int daysDelta = 0;

                    if (wallClockTimeDay < currentWeekday)
                    {
                        // Weekly rollover has already happened on a previous day
                        daysDelta = currentWeekday - wallClockTimeDay;
                    }
                    else if (wallClockTimeDay > currentWeekday)
                    {
                        // Weekly rollover will happen another day after this one
                        daysDelta = 7 + (currentWeekday - wallClockTimeDay);    // This will be negative, so we are subtracting from 7
                    }
                    else
                    {
                        // Weekly rollover is today and has not happened yet
                        // newLastRolloverTimeDelta will be negative, so we will end up with 6 days + leftovers
                        if (currentTime24Hr <= wallClockTime24Hr)
                            daysDelta = 7;
                    }

                    // Calculate 24 hour delta (this will be negative if the rollover has not happened yet)
                    newLastRolloverTimeDelta = currentTime24Hr - wallClockTime24Hr;

                    // Add the days part to the delta
                    if (daysDelta > 0)
                        newLastRolloverTimeDelta += TimeSpan.FromDays(daysDelta);
                }

                // Update the most recent rollover time delta if needed
                if (newLastRolloverTimeDelta < lastRolloverTimeDelta)
                    lastRolloverTimeDelta = newLastRolloverTimeDelta;
            }

            // Subtract the delta from the current time to get the time of the most recent rollover
            lastRolloverTime = currentTime - lastRolloverTimeDelta;
            return true;
        }

        public static Weekday GetCurrentWeekday(bool applyTimeZoneOffset)
        {
            TimeSpan currentTime = Clock.UnixTime;
            if (applyTimeZoneOffset)
                currentTime += TimeSpan.FromHours(GameDatabase.GlobalsPrototype.TimeZone);

            return GetWeekday(currentTime);
        }

        public static Weekday GetWeekday(TimeSpan unixTime)
        {
            // System.DayOfWeek is compatible with Gazillion's Weekday enum, so we can just cast it
            // instead of implementing Gazillion::DateTime::GetGMTimeInfo().
            return (Weekday)Clock.UnixTimeToDateTime(unixTime).DayOfWeek;
        }

        #endregion

        private struct AffixCountData
        {
            public short AffixCount = 0;
            public short AffixesNeeded = 0;

            public AffixCountData(short affixCount = 0, short affixesNeeded = 0)
            {
                AffixCount = affixCount;
                AffixesNeeded = affixesNeeded;
            }

            public override string ToString()
            {
                return $"{AffixCount}/{AffixesNeeded}";
            }

            public AffixCountData IncrementCount()
            {
                return new((short)(AffixCount + 1), AffixesNeeded);
            }
        }
    }
}
