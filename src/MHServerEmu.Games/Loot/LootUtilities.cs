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
                HashSet<ScopedAffixRef> affixSet = new();
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

        private static MutationResults AddCategorizedAffixesToItemSpec(IItemResolver resolver, DropFilterArguments args, AffixCategoryPrototype categoryProto, 
            int numAffixesNeeded, ItemSpec itemSpec, HashSet<ScopedAffixRef> affixSet, IEnumerable<AssetId> keywords = null)
        {
            //Logger.Trace($"AddCategorizedAffixesToItemSpec(): {categoryProto} (x{numAffixesNeeded})");

            IEnumerable<AffixPrototype> affixPool = GameDataTables.Instance.LootPickingTable.GetAffixesByCategory(categoryProto);
            if (affixPool == null)
                return Logger.WarnReturn(MutationResults.Error, $"AddCategorizedAffixesToItemSpec(): Failed to get available affixes in category: {categoryProto}.");

            Picker<AffixPrototype> affixPicker = new(resolver.Random);
            TryAddAffixesToPicker(args, null, keywords, resolver.Region, affixPool, affixPicker);

            MutationResults result = MutationResults.None;
            int numAffixesAdded = 0;

            for (int i = 0; i < numAffixesNeeded; i++)
            {
                AffixSpec affixSpec = new();
                result |= affixSpec.RollAffix(resolver.Random, args.RollFor, itemSpec, affixPicker, affixSet);

                if (result.HasFlag(MutationResults.Error) == false)
                {
                    itemSpec.AddAffixSpec(affixSpec);
                    numAffixesAdded++;
                }
            }

            ValidateAddAffixCount(numAffixesAdded, numAffixesNeeded);
            return result;
        }

        private static MutationResults AddPositionAffixesToItemSpec(IItemResolver resolver, DropFilterArguments args, AffixPosition affixPosition,
            int numAffixesNeeded,  ItemSpec itemSpec, HashSet<ScopedAffixRef> affixSet, IEnumerable<AssetId> keywords = null,
            IEnumerable<AffixCategoryPrototype> categories = null)
        {
            //Logger.Trace($"AddPositionAffixesToItemSpec(): {affixPosition} (x{numAffixesNeeded})");

            IEnumerable<AffixPrototype> affixPool = GameDataTables.Instance.LootPickingTable.GetAffixesByPosition(affixPosition);
            if (affixPool == null)
                return Logger.WarnReturn(MutationResults.Error, $"AddCategorizedAffixesToItemSpec(): Failed to get available affixes in position: {affixPosition}.");

            Picker<AffixPrototype> affixPicker = new(resolver.Random);
            TryAddAffixesToPicker(args, categories, keywords, resolver.Region, affixPool, affixPicker);

            MutationResults result = MutationResults.None;
            int numAffixesAdded = 0;

            for (int i = 0; i < numAffixesNeeded; i++)
            {
                AffixSpec affixSpec = new();
                result |= affixSpec.RollAffix(resolver.Random, args.RollFor, itemSpec, affixPicker, affixSet);

                if (result.HasFlag(MutationResults.Error) == false)
                {
                    itemSpec.AddAffixSpec(affixSpec);
                    numAffixesAdded++;
                }
            }

            ValidateAddAffixCount(numAffixesAdded, numAffixesNeeded);
            return result;
        }

        private static void TryAddAffixesToPicker(DropFilterArguments args, IEnumerable<AffixCategoryPrototype> categories, IEnumerable<AssetId> keywords,
            Region region, IEnumerable<AffixPrototype> affixPool, Picker<AffixPrototype> affixPicker)
        {
            foreach (AffixPrototype affixProto in affixPool)
            {
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

        private static void ValidateAddAffixCount(int numAffixesAdded, int numAffixesNeeded)
        {
            // Most of the arguments in this function are unused in the client, so we have just a simple
            // needed / added count check.

            if (numAffixesNeeded != numAffixesAdded)
                Logger.Warn($"ValidateAddAffixCount(): The pool of affixes is too small for these parameters! numAffixesAdded={numAffixesAdded}, numAffixesNeeded={numAffixesNeeded}");
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
    }
}
