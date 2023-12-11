using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Generators.Prototypes;

namespace MHServerEmu.Games.Regions
{
    public class TuningTable
    {
        private Region _region;
        private ulong _tuningRef;
        private TuningPrototype _tuningProto;

        public TuningTable(Region region)
        {
            _region = region;

            // TODO init difficultyGlobals
        }

        public void SetTuningTable(ulong tuningTable)
        {
            if (_tuningRef != tuningTable)
            {
                _tuningRef = tuningTable;
                _tuningProto = GameDatabase.GetPrototype<TuningPrototype>(tuningTable);
            }
        }

    }
}