using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData.Tables
{
    public class LootPickingTable
    {
        private readonly Dictionary<AffixPosition, List<AffixPrototype>> _affixPositionDict = new();

        // For some reason the client here uses PrototypeDataRef instead of AssetRef as key here,
        // even though in affix prototypes keywords are stored as AssetRefs. Is this a mistake?
        private readonly Dictionary<AssetId, List<AffixPrototype>> _affixKeywordDict = new();

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        private readonly Dictionary<PrototypeId, List<AffixPrototype>> _affixCategoryDict = new();
#endif

        private readonly Dictionary<LootPickingPair, List<PickerElement>> _pickerDict = new();

        public LootPickingTable()
        {
            foreach (PrototypeId affixRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<AffixPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                // Populate position -> AffixPrototype collection lookup
                AffixPrototype affixProto = affixRef.As<AffixPrototype>();
                if (affixProto.Weight <= 0 || affixProto.Position == AffixPosition.None)
                    continue;

                if (_affixPositionDict.TryGetValue(affixProto.Position, out List<AffixPrototype> positionAffixList) == false)
                {
                    positionAffixList = new();
                    _affixPositionDict.Add(affixProto.Position, positionAffixList);
                }

                positionAffixList.Add(affixProto);

                // Populate keyword Asset Ref -> AffixPrototype collection lookup
                if (affixProto.Keywords == null || affixProto.Keywords.Length == 0)
                    continue;

                foreach (AssetId keywordAssetRef in affixProto.Keywords)
                {
                    if (_affixKeywordDict.TryGetValue(keywordAssetRef, out List<AffixPrototype> keywordAffixList) == false)
                    {
                        keywordAffixList = new();
                        _affixKeywordDict.Add(keywordAssetRef, keywordAffixList);
                    }

                    keywordAffixList.Add(affixProto);
                }
            }

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            // Populate category -> AffixPrototype collection lookup
            LootGlobalsPrototype lootGlobalsProto = GameDatabase.LootGlobalsPrototype;
            foreach (AffixCategoryTableEntryPrototype affixCategoryTableEntry in lootGlobalsProto.AffixCategoryTable)
            {
                // We skip a lot of client checks here by assuming our data is valid
                List<AffixPrototype> categoryAffixList = new();
                _affixCategoryDict.Add(affixCategoryTableEntry.Category, categoryAffixList);

                foreach (PrototypeId affixRef in affixCategoryTableEntry.Affixes)
                {
                    AffixPrototype affixProto = affixRef.As<AffixPrototype>();
                    categoryAffixList.Add(affixProto);
                }
            }
#endif
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

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        public IReadOnlyList<AffixPrototype> GetAffixesByCategory(AffixCategoryPrototype categoryProto)
        {
            if (_affixCategoryDict.TryGetValue(categoryProto.DataRef, out List<AffixPrototype> affixList) == false)
                return null;

            return affixList;
        }
#endif

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
                        float weightMultiplier = itemProto.LootDropWeightMultiplier;

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

        private readonly struct LootPickingPair(PrototypeId lootProtoRef, PrototypeId agentProtoRef)
        {
            public readonly PrototypeId LootProtoRef = lootProtoRef;
            public readonly PrototypeId AgentProtoRef = agentProtoRef;
        }

        private readonly struct PickerElement(Prototype prototype, int weight)
        {
            public readonly Prototype Prototype = prototype;
            public readonly int Weight = weight;
        }
    }
}
