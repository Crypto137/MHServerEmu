using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Tables;

namespace MHServerEmu.Games.Loot
{
    public static class LootUtilities
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static bool PickValidItem(IItemResolver resolver, Picker<Prototype> basePicker, AgentPrototype teamUpProto, DropFilterArguments filterArgs,
            ref ItemPrototype pickedItemProto, RestrictionTestFlags restrictionFlags, ref PrototypeId? rarityProtoRef)
        {
            pickedItemProto = null;
            DropFilterArguments currentArgs = new(filterArgs);     // Copy arguments to compare to what we started

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

                    if (resolver.CheckItem(currentArgs, restrictionFlags, false))
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
                HashSet<(PrototypeId, PrototypeId)> affixSet = new();
                result = UpdateAffixesHelper(resolver, settings, args, itemSpec, affixSet);
            }

            if (result.HasFlag(MutationResults.Error) == false)
                result |= itemSpec.OnAffixesRolled(resolver, args.RollFor);

            return result;
        }

        private static MutationResults UpdateAffixesHelper(IItemResolver resolver, LootRollSettings settings, DropFilterArguments args,
            ItemSpec itemSpec, HashSet<(PrototypeId, PrototypeId)> affixSet)
        {
            ItemPrototype itemProto = itemSpec.ItemProtoRef.As<ItemPrototype>();
            if (itemProto == null) return Logger.WarnReturn(MutationResults.Error, "UpdateAffixesHelper(): itemProto == null");

            if (itemProto.IsPetItem)
            {
                // TODO: ItemPrototype::UpdatePetTechAffixes()
                return Logger.WarnReturn(MutationResults.None, "UpdateAffixesHelper(): Pet affixes are not yet implemented");
            }

            MutationResults result = MutationResults.None;

            AffixLimitsPrototype affixLimits = itemProto.GetAffixLimits(args.Rarity, args.LootContext);

            // Apply affixes by category
            if ((affixLimits != null && affixLimits.CategorizedAffixes.HasValue()) ||
                (settings != null && settings.AffixLimitByCategoryModifierDict.Count > 0))
            {
                Dictionary<PrototypeId, short> affixCategoryDict = new();

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
                    int numAffixesToAdd = kvp.Value - numAffixesCurrent;
                    if (numAffixesToAdd > 0)
                        result |= AddCategorizedAffixesToItemSpec(resolver, args, categoryProto, numAffixesToAdd, itemSpec, affixSet);
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
                int numAffixesToAdd = resolver.Random.Next(affixLimitsMin, affixLimitsMax + 1) - numAffixesCurrent;

                if (numAffixesToAdd > 0)
                    result |= AddPositionAffixesToItemSpec(resolver, args, affixPosition, numAffixesToAdd, itemSpec, affixSet);
            }

            return result;
        }

        private static MutationResults AddCategorizedAffixesToItemSpec(IItemResolver resolver, DropFilterArguments args, AffixCategoryPrototype categoryProto, 
            int numAffixesToAdd, ItemSpec itemSpec, HashSet<(PrototypeId, PrototypeId)> affixSet, IEnumerable<AssetId> keywords = null)
        {
            Logger.Debug($"AddCategorizedAffixesToItemSpec(): {categoryProto} (x{numAffixesToAdd})");
            return MutationResults.None;
        }

        private static MutationResults AddPositionAffixesToItemSpec(IItemResolver resolver, DropFilterArguments args, AffixPosition affixPosition,
            int numAffixesToAdd,  ItemSpec itemSpec, HashSet<(PrototypeId, PrototypeId)> affixSet, IEnumerable<AssetId> keywords = null,
            IEnumerable<PrototypeId> categories = null)
        {
            Logger.Debug($"AddPositionAffixesToItemSpec(): {affixPosition} (x{numAffixesToAdd})");
            return MutationResults.None;
        }
    }
}
