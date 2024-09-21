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
        private int _difficultyIndex;
        public int DifficultyIndex { get => _difficultyIndex > 0 ? _difficultyIndex : 0; set => SetDifficultyIndex(value, true); }

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

        public void SetDifficultyIndex(int difficultyIndex, bool broadcast)
        {
            int oldIndex = DifficultyIndex;
            _difficultyIndex = Math.Clamp(difficultyIndex, _difficultyIndexMin, _difficultyIndexMax);
            if (oldIndex != _difficultyIndex && broadcast)
                BroadcastChange(oldIndex, _difficultyIndex);
        }

        private void BroadcastChange(int oldDifficultyIndex, int newDifficultyIndex)
        {
            // TODO
        }

        public void GetUIIntArgs(List<long> intArgs)
        {
            float damage = GetIndexEnemyDamageBonus();
            float resistance = GetIndexEnemyDamageResistance();
            float xpBonus = GetIndexXPBonus();
            float lootBonus = GetIndexLootBonus();

            if (damage > 1.0f)
                intArgs.Add((long)((damage - 1.0f) * 100.0f));
            else
                intArgs.Add(0);

            if (resistance < 1.0f)
                intArgs.Add((long)((resistance > 0.0f) ? ((1.0f / resistance) - 1.0f) * 100.0f : 0.0f));
            else
                intArgs.Add(0);

            if (xpBonus > 1.0f)
                intArgs.Add((long)((xpBonus - 1.0f) * 100.0f));
            else
                intArgs.Add(0);

            if (lootBonus > 1.0f)
                intArgs.Add((long)((lootBonus - 1.0f) * 100.0f));
            else
                intArgs.Add(0);
        }

        public float GetIndexEnemyDamageBonus()
        {
            var difficultyGlobals = GameDatabase.DifficultyGlobalsPrototype;
            if (difficultyGlobals == null) return 0.0f;

            var difficultyIndexDamageCurve = GameDatabase.GetCurve(difficultyGlobals.DifficultyIndexDamageDefaultMtoP);
            if (difficultyIndexDamageCurve == null) return 0.0f;

            return difficultyIndexDamageCurve.GetAt(DifficultyIndex);
        }

        public float GetIndexEnemyDamageResistance()
        {
            var difficultyGlobals = GameDatabase.DifficultyGlobalsPrototype;
            if (difficultyGlobals == null) return 0.0f;

            var difficultyIndexDamageCurve = GameDatabase.GetCurve(difficultyGlobals.DifficultyIndexDamageDefaultPtoM);
            if (difficultyIndexDamageCurve == null) return 0.0f;

            return difficultyIndexDamageCurve.GetAt(DifficultyIndex);
        }

        public float GetIndexXPBonus()
        {
            if (Prototype == null) return 0.0f;

            var modifierCurveR = Prototype.PlayerXPByDifficultyIndexCurve;
            var modifierCurve = GameDatabase.GetCurve(modifierCurveR);
            if (modifierCurve == null) return 0.0f;

            int difficultyIndex = Math.Clamp(DifficultyIndex, modifierCurve.MinPosition, modifierCurve.MaxPosition);
            return modifierCurve.GetAt(difficultyIndex);
        }

        public float GetIndexLootBonus()
        {
            if (Prototype == null) return 0.0f;

            var modifierCurveR = Prototype.LootFindByDifficultyIndexCurve;
            var modifierCurve = GameDatabase.GetCurve(modifierCurveR);
            if (modifierCurve == null) return 0.0f;

            int difficultyIndex = Math.Clamp(DifficultyIndex, modifierCurve.MinPosition, modifierCurve.MaxPosition);
            return modifierCurve.GetAt(difficultyIndex);
        }

    }
}