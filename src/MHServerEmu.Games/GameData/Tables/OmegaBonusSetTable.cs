using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData.Tables
{
    public class OmegaBonusSetTable
    {
        private readonly Dictionary<PrototypeId, OmegaBonusSetPrototype> _omegaBonusSetDict = new();

        public OmegaBonusSetTable()
        {
            AdvancementGlobalsPrototype advGlobalsProto = GameDatabase.AdvancementGlobalsPrototype;

            foreach (var omegaBonusSetRef in advGlobalsProto.OmegaBonusSets)
            {
                var omegaBonusSetProto = omegaBonusSetRef.As<OmegaBonusSetPrototype>();

                foreach (var omegaBonusRef in omegaBonusSetProto.OmegaBonuses)
                    _omegaBonusSetDict[omegaBonusRef] = omegaBonusSetProto;
            }
        }

        public OmegaBonusSetPrototype GetOmegaBonusSet(PrototypeId omegaBonusRef)
        {
            if (_omegaBonusSetDict.TryGetValue(omegaBonusRef, out OmegaBonusSetPrototype omegaBonusSet) == false)
                return null;

            return omegaBonusSet;
        }
    }
}
