using MHServerEmu.Core.Collections;
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
        }

        public IEnumerable<AffixPrototype> GetAffixesByPosition(AffixPosition position)
        {
            if (_affixPositionDict.TryGetValue(position, out List<AffixPrototype> affixList) == false)
                return null;

            return affixList;
        }

        public IEnumerable<AffixPrototype> GetAffixesByKeyword(AssetId keywordAssetRef)
        {
            if (_affixKeywordDict.TryGetValue(keywordAssetRef, out List<AffixPrototype> affixList) == false)
                return null;

            return affixList;
        }

        public IEnumerable<AffixPrototype> GetAffixesByCategory(AffixCategoryPrototype categoryProto)
        {
            if (_affixCategoryDict.TryGetValue(categoryProto.DataRef, out List<AffixPrototype> affixList) == false)
                return null;

            return affixList;
        }

        public void GetConcreteLootPicker(Picker<Prototype> picker, PrototypeId prototypeDataRef, AgentPrototype agentProto)
        {
            throw new NotImplementedException();
        }
    }
}
