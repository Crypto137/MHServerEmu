using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Regions
{
    public class TuningTable
    {
        public static readonly Logger Logger = LogManager.CreateLogger();

        private Region _region;
        private PrototypeId _tuningRef;
        private TuningPrototype _tuningProto;
        private int _difficultyIndexMin;
        private int _difficultyIndexMax;

        public TuningPrototype Prototype { get => _tuningProto; }

        public TuningTable(Region region)
        {
            _region = region;

            DifficultyGlobalsPrototype difficultyGlobals = GameDatabase.DifficultyGlobalsPrototype;
            if (difficultyGlobals == null) return;

            var difficultyIndexC = GameDatabase.GetCurve(difficultyGlobals.DifficultyIndexDamageDefaultPtoM);
            if (difficultyIndexC != null)
            {
                _difficultyIndexMin = difficultyIndexC.MinPosition;
                _difficultyIndexMax = difficultyIndexC.MaxPosition;
            }
            else 
                Logger.Error("Failed to retrieve DifficultyIndexDamageDefaultPtoM from DifficultyGlobals! Is it set?");
        }

        public void SetTuningTable(PrototypeId tuningTable)
        {
            if (_tuningRef != tuningTable)
            {
                _tuningRef = tuningTable;
                _tuningProto = GameDatabase.GetPrototype<TuningPrototype>(tuningTable);
            }
        }

        internal RankPrototype RollRank(List<RankPrototype> ranks, HashSet<PrototypeId> overrides)
        {
            throw new NotImplementedException();
        }
    }
}