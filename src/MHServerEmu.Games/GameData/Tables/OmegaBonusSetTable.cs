#if !GAME_VERSION_1_53
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData.Tables
{
    public class OmegaBonusSetTable
    {
        private readonly Dictionary<PrototypeId, OmegaBonusSetPrototype> _omegaBonusSets = new();

        public OmegaBonusSetTable()
        {
            AdvancementGlobalsPrototype advGlobalsProto = GameDatabase.AdvancementGlobalsPrototype;
            if (!Verify.IsNotNull(advGlobalsProto)) return;

            foreach (OmegaBonusSetPrototype omegaBonusSetProto in advGlobalsProto.OmegaBonusSets)
            {
                foreach (OmegaBonusPrototype omegaBonusProto in omegaBonusSetProto.OmegaBonuses)
                    _omegaBonusSets[omegaBonusProto.DataRef] = omegaBonusSetProto;
            }
        }

        public OmegaBonusSetPrototype GetOmegaBonusSet(PrototypeId omegaBonusRef)
        {
            if (_omegaBonusSets.TryGetValue(omegaBonusRef, out OmegaBonusSetPrototype omegaBonusSet) == false)
                return null;

            return omegaBonusSet;
        }
    }
}
#endif
