using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData.Tables
{
    public class InfinityGemBonusTable
    {
        private readonly Dictionary<PrototypeId, InfinityGem> _gemSetBonusDict = new();
        private readonly Dictionary<InfinityGem, InfinityGem> _nextGemDict = new();

        public InfinityGemBonusTable()
        {
            AdvancementGlobalsPrototype advGlobalsProto = GameDatabase.AdvancementGlobalsPrototype;

            for (int i = 0; i < advGlobalsProto.InfinityGemSets.Length; i++)
            {
                var gemSetProto = advGlobalsProto.InfinityGemSets[i].As<InfinityGemSetPrototype>();

                foreach (var gemSetBonusRef in gemSetProto.Bonuses)
                    _gemSetBonusDict[gemSetBonusRef] = gemSetProto.Gem;

                if ((i + 1) < advGlobalsProto.InfinityGemSets.Length)
                    _nextGemDict[gemSetProto.Gem] = advGlobalsProto.InfinityGemSets[i + 1].As<InfinityGemSetPrototype>().Gem;
                else
                    _nextGemDict[gemSetProto.Gem] = advGlobalsProto.InfinityGemSets[0].As<InfinityGemSetPrototype>().Gem;
            }
        }

        public InfinityGem GetGemForPrototype(PrototypeId gemSetBonusRef)
        {
            if (_gemSetBonusDict.TryGetValue(gemSetBonusRef, out InfinityGem gem) == false)
                return InfinityGem.None;

            return gem;
        }

        public InfinityGem GetNextGem(InfinityGem gem)
        {
            if (_nextGemDict.TryGetValue(gem, out InfinityGem nextGem) == false)
                return InfinityGem.None;

            return nextGem;
        }
    }
}
