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
                InfinityGemSetPrototype gemSetProto = advGlobalsProto.InfinityGemSets[i];

                foreach (InfinityGemBonusPrototype gemSetBonusProto in gemSetProto.Bonuses)
                    _gemSetBonusDict[gemSetBonusProto.DataRef] = gemSetProto.Gem;

                if ((i + 1) < advGlobalsProto.InfinityGemSets.Length)
                    _nextGemDict[gemSetProto.Gem] = advGlobalsProto.InfinityGemSets[i + 1].Gem;
                else
                    _nextGemDict[gemSetProto.Gem] = advGlobalsProto.InfinityGemSets[0].Gem;
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
