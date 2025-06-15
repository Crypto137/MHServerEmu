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
        private static readonly Logger Logger = LogManager.CreateLogger();

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
                    if (proto is not ItemPrototype itemProto)
                    {
                        Logger.Warn("PickValidItem(): itemProto == null");
                        continue;
                    }

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
                    if (rarityProto == null)
                    {
                        Logger.Warn("PickValidItem(): rarityProto == null");
                        break;
                    }

                    currentArgs.Rarity = rarityProto.DowngradeTo;
                    if (currentArgs.Rarity == filterArgs.Rarity)
                    {
                        Logger.Warn($"PickValidItem(): Rarity loop detected [{currentArgs.Rarity.GetName()}]");
                        break;
                    }
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
            if (avatarProto == null) return Logger.WarnReturn(false, "BuildInventoryLootPicker(): avatarProto == null");
            if (avatarProto.EquipmentInventories == null) return Logger.WarnReturn(false, "BuildInventoryLootPicker(): avatarProto.EquipmentInventories == null");

            picker.Clear();

            foreach (AvatarEquipInventoryAssignmentPrototype equipInvAssignmentProto in avatarProto.EquipmentInventories)
            {
                if (equipInvAssignmentProto.UISlot != slot)
                    continue;

                InventoryPrototype invProto = equipInvAssignmentProto.Inventory.As<InventoryPrototype>();
                if (invProto == null) return Logger.WarnReturn(false, "BuildInventoryLootPicker(): invProto == null");

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
            // TODO: split this into individual validation checks?
            if (itemSpec.IsValid == false || itemSpec.ItemProtoRef != args.ItemProto.DataRef || itemSpec.RarityProtoRef != args.Rarity)
            {
                Logger.Warn(string.Format("UpdateAffixes(): Invalid input parameter(s):\n" +
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
                HashSet<ScopedAffixRef> affixSet = HashSetPool<ScopedAffixRef>.Instance.Get();
                result = UpdateAffixesHelper(resolver, settings, args, itemSpec, affixSet);
                HashSetPool<ScopedAffixRef>.Instance.Return(affixSet);
            }

            if (result.HasFlag(MutationResults.Error) == false)
                result |= itemSpec.OnAffixesRolled(resolver, args.RollFor);

            return result;
        }

        private static MutationResults UpdateAffixesHelper(IItemResolver resolver, LootRollSettings settings, DropFilterArguments args,
            ItemSpec itemSpec, HashSet<ScopedAffixRef> affixSet)
        {
            ItemPrototype itemProto = itemSpec.ItemProtoRef.As<ItemPrototype>();
            if (itemProto == null) return Logger.WarnReturn(MutationResults.Error, "UpdateAffixesHelper(): itemProto == null");

            // Pet affixes are rolled separately
            if (itemProto.IsPetItem)
                return ItemPrototype.UpdatePetTechAffixes(resolver.Random, args.RollFor, itemSpec);

            MutationResults result = MutationResults.None;

            AffixLimitsPrototype affixLimits = itemProto.GetAffixLimits(args.Rarity, args.LootContext);

            // Apply affixes by category
            if ((affixLimits != null && affixLimits.CategorizedAffixes.HasValue()) ||
                (settings != null && settings.AffixLimitByCategoryModifierDict.Count > 0))
            {
                Dictionary<PrototypeId, short> affixCategoryDict = DictionaryPool<PrototypeId, short>.Instance.Get();

                // Get category limits from the prototype
                if (affixLimits != null)
                {
                    foreach (CategorizedAffixEntryPrototype entry in affixLimits.CategorizedAffixes)
                    {
                        affixCategoryDict.TryGetValue(entry.Category, out short numAffixes);
                        affixCategoryDict[entry.Category] = (short)(numAffixes + entry.MinAffixes);
                    }
                }

                // Apply modifiers from loot roll settings
                if (settings != null)
                {
                    foreach (var kvp in settings.AffixLimitByCategoryModifierDict)
                    {
                        affixCategoryDict.TryGetValue(kvp.Key, out short numAffixes);
                        affixCategoryDict[kvp.Key] = (short)Math.Max(0, numAffixes + kvp.Value);
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

                DictionaryPool<PrototypeId, short>.Instance.Return(affixCategoryDict);
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
            if (affixCountNeeded <= 0)
                return Logger.WarnReturn(MutationResults.Error, $"AddAffixes(): Trying to add 0 affixes! args: {args}");

            if (itemSpec.IsValid == false) return Logger.WarnReturn(MutationResults.Error, $"AddAffixes(): itemSpec.IsValid == false");
            if (itemSpec.ItemProtoRef != args.ItemProto.DataRef) return Logger.WarnReturn(MutationResults.Error, $"AddAffixes(): itemSpec.ItemProtoRef != args.ItemProto.DataRef");

            ItemPrototype itemProto = args.ItemProto as ItemPrototype;
            if (itemProto == null) return Logger.WarnReturn(MutationResults.Error, "AddAffixes(): itemProto == null");

            if (itemProto.IsPetItem)
                return ItemPrototype.UpdatePetTechAffixes(resolver.Random, args.RollFor, itemSpec);

            HashSet<ScopedAffixRef> affixSet = HashSetPool<ScopedAffixRef>.Instance.Get();
            List<AffixCountData> affixCounts = ListPool<AffixCountData>.Instance.Get();

            try
            {
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
            finally
            {
                HashSetPool<ScopedAffixRef>.Instance.Return(affixSet);
                ListPool<AffixCountData>.Instance.Return(affixCounts);
            }
        }

        public static MutationResults AddAffix(IItemResolver resolver, DropFilterArguments args, ItemSpec itemSpec, AffixPrototype affixProto)
        {
            HashSet<ScopedAffixRef> affixSet = HashSetPool<ScopedAffixRef>.Instance.Get();
            List<AffixCountData> affixCounts = ListPool<AffixCountData>.Instance.Get();

            try
            {
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
            finally
            {
                HashSetPool<ScopedAffixRef>.Instance.Return(affixSet);
                ListPool<AffixCountData>.Instance.Return(affixCounts);
            }
        }

        public static MutationResults DropAffixes(IItemResolver resolver, DropFilterArguments args,
            ItemSpec itemSpec, AffixPosition position, AssetId[] keywords, PrototypeId[] categories)
        {
            if (itemSpec.IsValid == false) return Logger.WarnReturn(MutationResults.Error, "DropAffixes(): itemSpec.IsValid == false");

            MutationResults result = DropAffixes(resolver, itemSpec, position, keywords, categories);
            if (result.HasFlag(MutationResults.Error))
                return result;

            result |= UpdateAffixes(resolver, args, AffixCountBehavior.Keep, itemSpec, null);
            
            return result;
        }

        public static MutationResults CopyAffixes(IItemResolver resolver, DropFilterArguments args, ItemSpec sourceItemSpec,
            ItemSpec destItemSpec, AffixPosition position, AssetId[] keywords, PrototypeId[] categories, bool enforceAffixLimits)
        {
            if (destItemSpec.IsValid == false) return Logger.WarnReturn(MutationResults.Error, "CopyAffixes(): destItemSpec.IsValid == false");
            if (args.ItemProto == null) return Logger.WarnReturn(MutationResults.Error, "CopyAffixes(): args.ItemProto == null");
            if (destItemSpec.ItemProtoRef != args.ItemProto.DataRef) return Logger.WarnReturn(MutationResults.Error, "CopyAffixes(): destItemSpec.ItemProtoRef != args.ItemProto.DataRef");

            ItemPrototype itemProto = args.ItemProto as ItemPrototype;
            if (itemProto == null) return Logger.WarnReturn(MutationResults.Error, "CopyAffixes(): itemProto == null");

            if (itemProto.IsPetItem)
                return ItemPrototype.CopyPetTechAffixes(sourceItemSpec, destItemSpec, position);

            HashSet<ScopedAffixRef> affixSet = HashSetPool<ScopedAffixRef>.Instance.Get();
            List<AffixCountData> affixCounts = ListPool<AffixCountData>.Instance.Get();

            try
            {
                affixCounts.Fill(default, (int)AffixPosition.NumPositions);

                if (GetCurrentAffixStats(resolver, args, destItemSpec, affixCounts, affixSet) == false)
                    return MutationResults.Error | MutationResults.ErrorReasonAffixStats;

                AffixLimitsPrototype affixLimits = null;
                if (enforceAffixLimits)
                {
                    affixLimits = itemProto.GetAffixLimits(args.Rarity, args.LootContext);
                    if (affixLimits == null)
                        return Logger.WarnReturn(MutationResults.Error, $"CopyAffixes(): Trying to EnforceAffixLimits where there is no affix limits available! args {args}");
                }

                return CopyAffixSpecs(resolver, sourceItemSpec, destItemSpec, affixLimits, args.RollFor, keywords, position, categories, affixCounts, affixSet);
            }
            finally
            {
                HashSetPool<ScopedAffixRef>.Instance.Return(affixSet);
                ListPool<AffixCountData>.Instance.Return(affixCounts);
            }
        }

        public static MutationResults CopyBuiltinAffixes(IItemResolver resolver, DropFilterArguments args, ItemSpec sourceItemSpec,
            ItemSpec destItemSpec, AffixPosition position, AssetId[] keywords, PrototypeId[] categories, bool enforceAffixLimits)
        {
            if (destItemSpec.IsValid == false) return Logger.WarnReturn(MutationResults.Error, "CopyBuiltinAffixes(): destItemSpec.IsValid == false");
            if (destItemSpec.ItemProtoRef != args.ItemProto.DataRef) return Logger.WarnReturn(MutationResults.Error, "CopyBuiltinAffixes(): destItemSpec.ItemProtoRef != args.ItemProto.DataRef");

            ItemPrototype destItemProto = args.ItemProto as ItemPrototype;
            if (destItemProto == null) return Logger.WarnReturn(MutationResults.Error, "CopyBuiltinAffixes(): destItemProto == null");

            HashSet<ScopedAffixRef> affixSet = HashSetPool<ScopedAffixRef>.Instance.Get();
            List<AffixCountData> affixCounts = ListPool<AffixCountData>.Instance.Get();
            List<BuiltInAffixDetails> builtInAffixDetailsList = ListPool<BuiltInAffixDetails>.Instance.Get();
            List<AffixSpec> builtInAffixSpecs = ListPool<AffixSpec>.Instance.Get();

            try
            {
                affixCounts.Fill(default, (int)AffixPosition.NumPositions);

                if (GetCurrentAffixStats(resolver, args, destItemSpec, affixCounts, affixSet) == false)
                    return MutationResults.Error | MutationResults.ErrorReasonAffixStats;

                ItemPrototype sourceItemProto = sourceItemSpec.ItemProtoRef.As<ItemPrototype>();
                if (sourceItemProto == null) return Logger.WarnReturn(MutationResults.Error, "CopyBuiltinAffixes(): sourceItemProto == null");

                sourceItemProto.GenerateBuiltInAffixDetails(sourceItemSpec, builtInAffixDetailsList);

                if (builtInAffixDetailsList.Count == 0)
                    return MutationResults.None;

                foreach (BuiltInAffixDetails builtInAffixDetails in builtInAffixDetailsList)
                {
                    AffixEntryPrototype affixEntryProto = builtInAffixDetails.AffixEntryProto;
                    if (affixEntryProto == null)
                    {
                        Logger.Warn("CopyBuiltinAffixes(): affixEntryProto == null");
                        continue;
                    }

                    AffixPrototype affixProto = affixEntryProto.Affix.As<AffixPrototype>();
                    if (affixProto == null)
                    {
                        Logger.Warn("CopyBuiltinAffixes(): affixProto == null");
                        continue;
                    }

                    AffixSpec affixSpec = new(affixProto, affixEntryProto.Power, builtInAffixDetails.Seed);
                    builtInAffixSpecs.Add(affixSpec);
                }

                // This will remove any externally applied affixes (which we don't care about here)
                sourceItemSpec.SetAffixes(builtInAffixSpecs);

                AffixLimitsPrototype affixLimits = null;
                if (enforceAffixLimits)
                {
                    affixLimits = destItemProto.GetAffixLimits(args.Rarity, args.LootContext);
                    if (affixLimits == null)
                        return Logger.WarnReturn(MutationResults.Error, $"CopyBuiltinAffixes(): Trying to EnforceAffixLimits where there is no affix limits available! args {args}");

                }

                return CopyAffixSpecs(resolver, sourceItemSpec, destItemSpec, affixLimits, args.RollFor, keywords, position, categories, affixCounts, affixSet);
            }
            finally
            {
                HashSetPool<ScopedAffixRef>.Instance.Return(affixSet);
                ListPool<AffixCountData>.Instance.Return(affixCounts);
                ListPool<BuiltInAffixDetails>.Instance.Return(builtInAffixDetailsList);
                ListPool<AffixSpec>.Instance.Return(builtInAffixSpecs);
            }
        }

        public static MutationResults ReplaceAffixes(IItemResolver resolver, DropFilterArguments args, ItemSpec sourceItemSpec,
            ItemSpec destItemSpec, AffixPosition position, AssetId[] keywords, PrototypeId[] categories, bool enforceAffixLimits)
        {
            if (destItemSpec.IsValid == false) return Logger.WarnReturn(MutationResults.Error, "ReplaceAffixes(): destItemSpec.IsValid == false");

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
                if (affixes == null) return Logger.WarnReturn(0u, "BuildAffixPickers(): affixes == null");

                for (int i = 0; i < affixes.Count; i++)
                {
                    AffixPrototype affixProtoIt = affixes[i];
                    if (affixProtoIt == null)
                    {
                        Logger.Warn("BuildAffixPickers(): affixProtoIt == null");
                        continue;
                    }

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
            //Logger.Trace($"AddCategorizedAffixesToItemSpec(): {categoryProto} (x{numAffixesNeeded})");

            IReadOnlyList<AffixPrototype> affixPool = GameDataTables.Instance.LootPickingTable.GetAffixesByCategory(categoryProto);
            if (affixPool == null)
                return Logger.WarnReturn(MutationResults.Error, $"AddCategorizedAffixesToItemSpec(): Failed to get available affixes in category: {categoryProto}.");

            Picker<AffixPrototype> affixPicker = new(resolver.Random);
            TryAddAffixesToPicker(args, null, keywords, resolver.Region, affixPool, affixPicker);

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
                if (categoryIt == null)
                {
                    Logger.Warn("AddCategorizedAffixesToItemSpec(): categoryIt == null");
                    continue;
                }

                result |= AddCategorizedAffixesToItemSpec(resolver, args, categoryIt, affixCountNeeded, itemSpec, affixSet, keywords);
            }

            return result;
        }

        private static MutationResults AddPositionAffixesToItemSpec(IItemResolver resolver, DropFilterArguments args, AffixPosition affixPosition,
            int affixCountNeeded, ItemSpec itemSpec, HashSet<ScopedAffixRef> affixSet, AssetId[] keywords = null, PrototypeId[] categories = null)
        {
            //Logger.Trace($"AddPositionAffixesToItemSpec(): {affixPosition} (x{numAffixesNeeded})");

            IReadOnlyList<AffixPrototype> affixPool = GameDataTables.Instance.LootPickingTable.GetAffixesByPosition(affixPosition);
            if (affixPool == null)
                return Logger.WarnReturn(MutationResults.Error, $"AddCategorizedAffixesToItemSpec(): Failed to get available affixes in position: {affixPosition}.");

            Picker<AffixPrototype> affixPicker = new(resolver.Random);
            TryAddAffixesToPicker(args, categories, keywords, resolver.Region, affixPool, affixPicker);

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
                IReadOnlyList<AffixPrototype> affixPool = GameDataTables.Instance.LootPickingTable.GetAffixesByKeyword(keywordIt);
                if (affixPool == null)
                    return Logger.WarnReturn(MutationResults.Error, $"AddKeywordAffixesToItemSpec(): Failed to get available affixes for keyword: {keywordIt.GetName()}.");

                TryAddAffixesToPicker(args, null, keywords, resolver.Region, affixPool, affixPicker);
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

            if (affixCountNeeded != affixCountAdded)
                Logger.Warn($"ValidateAddAffixCount(): The pool of affixes is too small for these parameters! affixCountAdded={affixCountAdded}, affixCountNeeded={affixCountNeeded}");
        }

        private static MutationResults DropAffixes(IItemResolver resolver, ItemSpec itemSpec, AffixPosition position, AssetId[] keywords, PrototypeId[] categories)
        {
            // NOTE: This is used by public DropAffixes() and ReplaceAffixes() functions.

            MutationResults result = MutationResults.None;
            
            List<AffixSpec> filteredAffixSpecs = ListPool<AffixSpec>.Instance.Get();

            bool hasKeywords = keywords.HasValue();
            bool hasCategories = categories.HasValue();

            IReadOnlyList<AffixSpec> affixSpecs = itemSpec.AffixSpecs;
            for (int i = 0; i < affixSpecs.Count; i++)
            {
                AffixSpec affixSpec = affixSpecs[i];
                
                if (affixSpec.IsValid == false)
                {
                    Logger.Warn($"DropAffixes(): Invalid affix prototype in item!\nItem: {itemSpec}\nResolver: {resolver}");
                    result = MutationResults.Error;
                    break;
                }

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

            // Overwrite affixes with our filtered list if everything is okay
            if (result.HasFlag(MutationResults.Error) == false)
                itemSpec.SetAffixes(filteredAffixSpecs);

            ListPool<AffixSpec>.Instance.Return(filteredAffixSpecs);
            return result;
        }

        private static MutationResults CopyAffixSpecs(IItemResolver resolver, ItemSpec sourceItemSpec, ItemSpec destItemSpec,
            AffixLimitsPrototype affixLimits, PrototypeId rollFor, AssetId[] keywords, AffixPosition position, PrototypeId[] categories,
            List<AffixCountData> affixCounts, HashSet<ScopedAffixRef> affixSet)
        {
            bool hasKeywords = keywords.HasValue();
            bool hasCategories = categories.HasValue();

            MutationResults result = MutationResults.None;

            List<AffixSpec> affixSpecsToAdd = ListPool<AffixSpec>.Instance.Get();
            List<int> addedPositionCounts = ListPool<int>.Instance.Get();
            Dictionary<AffixCategoryPrototype, int> addedCategoryCounts = DictionaryPool<AffixCategoryPrototype, int>.Instance.Get();

            addedPositionCounts.Fill(0, (int)AffixPosition.NumPositions);

            IReadOnlyList<AffixSpec> sourceAffixSpecs = sourceItemSpec.AffixSpecs;
            for (int i = 0; i < sourceAffixSpecs.Count; i++)
            {
                AffixSpec sourceAffixSpecIt = sourceAffixSpecs[i];
                AffixPrototype affixProto = sourceAffixSpecIt.AffixProto;

                if (affixProto == null)
                {
                    Logger.Warn("CopyAffixSpecs(): affixProto == null");
                    result = MutationResults.Error;
                    goto end;
                }

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
                            Logger.Warn($"CopyAffixSpecs(): Invalid DuplicateHandlingBehavior {affixProto.DuplicateHandlingBehavior} for {affixProto}");
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
                    {
                        addedCategoryCounts.TryGetValue(categoryProto, out int count);
                        addedCategoryCounts[categoryProto] = count + 1;
                    }
                    else
                    {
                        addedPositionCounts[(int)affixProto.Position]++;
                    }
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

            end:
            ListPool<AffixSpec>.Instance.Return(affixSpecsToAdd);
            ListPool<int>.Instance.Return(addedPositionCounts);
            DictionaryPool<AffixCategoryPrototype, int>.Instance.Return(addedCategoryCounts);
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

            for (int i = 0; i < itemSpec.AffixSpecs.Count; i++)
            {
                AffixSpec affixSpecIt = itemSpec.AffixSpecs[i];
                if (affixSpecIt.IsValid == false)
                    return Logger.WarnReturn(false, $"GetCurrentAffixStats(): Invalid affix spec: affixSpec=[{affixSpecIt}] args=[{args}] itemSpec=[{itemSpec}]");

                affixSet.Add(new(affixSpecIt.AffixProto.DataRef, affixSpecIt.ScopeProtoRef));

                AffixPosition affixPos = affixSpecIt.AffixProto.Position;
                int affixPosIndex = (int)affixPos;
                if (affixPosIndex < 0 || affixPosIndex >= affixCounts.Count)
                {
                    Logger.Warn($"GetCurrentAffixStats(): Invalid affix position on item! itemSpec=[{itemSpec}]");
                    continue;
                }

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
                return Logger.WarnReturn(false, $"GetCurrentAffixStats(): Item has both an externally applied visual affix and the no-visuals metadata affix! itemSpec: {itemSpec}");

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
            if (currentWeekday == Weekday.All) return Logger.WarnReturn(false, "GetLastLootCooldownRolloverWallTime(): weekday == Weekday.All");

            PropertyList.Iterator itRolloverTimeProp = properties.IteratePropertyRange(PropertyEnum.LootCooldownRolloverWallTime);

            if (itRolloverTimeProp.GetEnumerator().MoveNext() == false)
                return Logger.WarnReturn(false, "GetLastLootCooldownRolloverWallTime(): No properties to iterate");

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
