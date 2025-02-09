using System.Text.Json;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData.Tables
{
    public class LootPickingTable
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<AffixPosition, List<AffixPrototype>> _affixPositionDict = new();

        // For some reason the client here uses PrototypeDataRef instead of AssetRef as key here,
        // even though in affix prototypes keywords are stored as AssetRefs. Is this a mistake?
        private readonly Dictionary<AssetId, List<AffixPrototype>> _affixKeywordDict = new();

        private readonly Dictionary<PrototypeId, List<AffixPrototype>> _affixCategoryDict = new();

        private readonly Dictionary<LootPickingPair, List<PickerElement>> _pickerDict = new();

        private readonly Dictionary<PrototypeId, float> _lootDropWeightMultiplierOverrides = new(); // CUSTOM

        public LootPickingTable()
        {
            foreach (var affixRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<AffixPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                // Populate position -> AffixPrototype collection lookup
                var affixProto = affixRef.As<AffixPrototype>();
                if (affixProto.Weight <= 0 || affixProto.Position == AffixPosition.None) continue;

                if (_affixPositionDict.TryGetValue(affixProto.Position, out List<AffixPrototype> positionAffixList) == false)
                {
                    positionAffixList = new();
                    _affixPositionDict.Add(affixProto.Position, positionAffixList);
                }

                positionAffixList.Add(affixProto);

                // Populate keyword Asset Ref -> AffixPrototype collection lookup
                if (affixProto.Keywords == null || affixProto.Keywords.Length == 0) continue;
                foreach (var keywordAssetRef in affixProto.Keywords)
                {
                    if (_affixKeywordDict.TryGetValue(keywordAssetRef, out List<AffixPrototype> keywordAffixList) == false)
                    {
                        keywordAffixList = new();
                        _affixKeywordDict.Add(keywordAssetRef, keywordAffixList);
                    }

                    keywordAffixList.Add(affixProto);
                }
            }

            // Populate category -> AffixPrototype collection lookup
            LootGlobalsPrototype lootGlobalsProto = GameDatabase.LootGlobalsPrototype;
            foreach (AffixCategoryTableEntryPrototype affixCategoryTableEntry in lootGlobalsProto.AffixCategoryTable)
            {
                // We skip a lot of client checks here by assuming our data is valid
                List<AffixPrototype> categoryAffixList = new();
                _affixCategoryDict.Add(affixCategoryTableEntry.Category, categoryAffixList);

                foreach (var affixRef in affixCategoryTableEntry.Affixes)
                {
                    var affixProto = affixRef.As<AffixPrototype>();
                    categoryAffixList.Add(affixProto);
                }
            }

            // CUSTOM: Load loot drop weight multiplier overrides
            string lootDropWeightMultiplierOverridesDirectory = Path.Combine(FileHelper.DataDirectory, "Game");

            foreach (string filePath in FileHelper.GetFilesWithPrefix(lootDropWeightMultiplierOverridesDirectory, "LootDropWeightMultiplierOverrides", "json"))
            {
                LootDropWeightMultiplierOverride[] overrides = FileHelper.DeserializeJson<LootDropWeightMultiplierOverride[]>(filePath);
                if (overrides == null)
                {
                    Logger.Warn($"LootPickingTable(): Failed to parse LootDropWeightMultiplier overrides from {filePath}");
                    continue;
                }

                Logger.Trace($"Parsed LootDropWeightMultiplier overrides from {Path.GetFileName(filePath)}");

                foreach (var @override in overrides)
                {
                    string itemPrototype = @override.ItemPrototype;
                    float lootDropWeightMultiplier = @override.LootDropWeightMultiplier;

                    PrototypeId itemProtoRef = GameDatabase.GetPrototypeRefByName(itemPrototype);
                    if (itemProtoRef == PrototypeId.Invalid)
                    {
                        Logger.Warn($"LootPickingTable(): Failed to add a LootDropWeightMultiplier override - {itemPrototype} is not a valid prototype name");
                        continue;
                    }

                    if (DataDirectory.Instance.PrototypeIsA<ItemPrototype>(itemProtoRef) == false)
                    {
                        Logger.Warn($"LootPickingTable(): Failed to add a LootDropWeightMultiplier override - {itemPrototype} is not an ItemPrototype");
                        continue;
                    }

                    Logger.Trace($"Added LootDropWeightMultiplier override: {itemPrototype} = {lootDropWeightMultiplier}f");
                    _lootDropWeightMultiplierOverrides[itemProtoRef] = lootDropWeightMultiplier;
                }
            }
        }

        public IReadOnlyList<AffixPrototype> GetAffixesByPosition(AffixPosition position)
        {
            if (_affixPositionDict.TryGetValue(position, out List<AffixPrototype> affixList) == false)
                return null;

            return affixList;
        }

        public IReadOnlyList<AffixPrototype> GetAffixesByKeyword(AssetId keywordAssetRef)
        {
            if (_affixKeywordDict.TryGetValue(keywordAssetRef, out List<AffixPrototype> affixList) == false)
                return null;

            return affixList;
        }

        public IReadOnlyList<AffixPrototype> GetAffixesByCategory(AffixCategoryPrototype categoryProto)
        {
            if (_affixCategoryDict.TryGetValue(categoryProto.DataRef, out List<AffixPrototype> affixList) == false)
                return null;

            return affixList;
        }

        public void GetConcreteLootPicker(Picker<Prototype> pickerToFill, PrototypeId lootTypeProtoRef, AgentPrototype agentProto)
        {
            PrototypeId agentProtoRef = agentProto != null ? agentProto.DataRef : PrototypeId.Invalid;
            LootPickingPair key = new(lootTypeProtoRef, agentProtoRef);

            List<PickerElement> pickerElementList;

            // See if we already have picker data for this combination
            lock (_pickerDict)
                _pickerDict.TryGetValue(key, out pickerElementList);

            // Generate picker data if we don't have it already
            if (pickerElementList == null)
            {
                pickerElementList = new();
                BlueprintId itemBlueprintRef = DataDirectory.Instance.GetPrototypeBlueprintDataRef(lootTypeProtoRef);

                // Iterate all items that use the item ref's blueprint
                foreach (PrototypeId lootProtoRef in DataDirectory.Instance.IteratePrototypesInHierarchy(itemBlueprintRef, PrototypeIterateFlags.NoAbstractApprovedOnly))
                {
                    Prototype lootProto = GameDatabase.GetPrototype<Prototype>(lootProtoRef);
                    int weight = 100;   // 100 is the default weight

                    // What we are picking may not be an item? When?
                    if (lootProto is ItemPrototype itemProto)
                    {
                        // CUSTOM: Check if we have a weight multiplier override for this item, otherwise use the one from the item prototype
                        if (_lootDropWeightMultiplierOverrides.TryGetValue(lootProtoRef, out float weightMultiplier) == false)
                            weightMultiplier = itemProto.LootDropWeightMultiplier;

                        // Skip items that have a 0 weight multiplier
                        if (Segment.IsNearZero(weightMultiplier))
                            continue;

                        // NOTE: agentProto based skip happens only if there is no custom drop weight multiplier, is this correct?
                        if (Segment.EpsilonTest(weightMultiplier, 1f) == false)
                            weight = Math.Max(1, (int)(weight * weightMultiplier));
                        else if (agentProto != null && itemProto.IsDroppableForAgent(agentProto) == false)
                            continue;
                    }

                    pickerElementList.Add(new(lootProto, weight));
                }

                pickerElementList.Sort((a, b) => b.Weight.CompareTo(a.Weight));

                lock (_pickerDict)
                {
                    // Check to make sure the list for this combination wasn't added by another game thread
                    if (_pickerDict.ContainsKey(key) == false)
                        _pickerDict.Add(key, pickerElementList);
                }
            }

            // Fill the output picker
            foreach (PickerElement element in pickerElementList)
                pickerToFill.Add(element.Prototype, element.Weight);
        }

        private readonly struct LootPickingPair
        {
            public readonly PrototypeId LootProtoRef;
            public readonly PrototypeId AgentProtoRef;

            public LootPickingPair(PrototypeId lootProtoRef, PrototypeId agentProtoRef)
            {
                LootProtoRef = lootProtoRef;
                AgentProtoRef = agentProtoRef;
            }
        }

        private readonly struct PickerElement
        {
            public readonly Prototype Prototype;
            public readonly int Weight;

            public PickerElement(Prototype prototype, int weight)
            {
                Prototype = prototype;
                Weight = weight;
            }
        }

        private class LootDropWeightMultiplierOverride
        {
            public string ItemPrototype { get; init; }
            public float LootDropWeightMultiplier { get; init; }
        }
    }
}
