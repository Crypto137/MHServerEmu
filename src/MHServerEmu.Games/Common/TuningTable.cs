using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Common
{
    public class TuningTable
    {
        // NOTE: In the client this class is referenced as D:\mirrorBuilds_source05\MarvelGame_v52\Source\Game\Game\Combat\TuningTable.cpp,
        // but it's awkward for namespaces and classes to use the same names in C#, so we moved both combat classes to Common.

        public static readonly Logger Logger = LogManager.CreateLogger();

        private Region _region;
        private PrototypeId _tuningRef;
        private TuningPrototype _tuningProto;
        private int _difficultyIndexMin;
        private int _difficultyIndexMax;
        private int _difficultyIndex;

        public TuningPrototype Prototype { get => _tuningProto; }
        public int DifficultyIndex { get => _difficultyIndex > 0 ? _difficultyIndex : 0; set => SetDifficultyIndex(value, true); }

        public TuningTable(Region region)
        {
            _region = region;

            DifficultyGlobalsPrototype difficultyGlobals = GameDatabase.DifficultyGlobalsPrototype;
            if (difficultyGlobals == null) return;

            Curve difficultyIndexC = GameDatabase.GetCurve(difficultyGlobals.DifficultyIndexDamageDefaultPtoM);
            if (difficultyIndexC != null)
            {
                _difficultyIndexMin = difficultyIndexC.MinPosition;
                _difficultyIndexMax = difficultyIndexC.MaxPosition;
            }
            else
            {
                Logger.Warn("TuningTable(): Failed to retrieve DifficultyIndexDamageDefaultPtoM from DifficultyGlobals! Is it set?");
            }
        }

        public void SetTuningTable(PrototypeId tuningTable)
        {
            if (_tuningRef != tuningTable)
            {
                _tuningRef = tuningTable;
                _tuningProto = GameDatabase.GetPrototype<TuningPrototype>(tuningTable);
            }
        }

        public RankPrototype RollRank(List<RankPrototype> ranks, bool noAffixes)
        {
            var rank = GameDatabase.PopulationGlobalsPrototype.GetRankByEnum(Rank.Popcorn);

            if (ranks.Any(r => r.Rank != Rank.Popcorn) == false)
            {
                var picker = _tuningProto.BuildRankPicker(_region.DifficultyTierRef, _region.Game.Random, noAffixes);
                if (picker.Empty() == false) picker.Pick(out rank);
            }

            return rank;
        }

        public void SetDifficultyIndex(int difficultyIndex, bool broadcast)
        {
            int oldIndex = DifficultyIndex;
            _difficultyIndex = Math.Clamp(difficultyIndex, _difficultyIndexMin, _difficultyIndexMax);
            if (oldIndex != _difficultyIndex && broadcast)
                BroadcastChange(oldIndex, _difficultyIndex);
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
            DifficultyGlobalsPrototype difficultyGlobals = GameDatabase.DifficultyGlobalsPrototype;
            if (difficultyGlobals == null) return Logger.WarnReturn(0.0f, "GetIndexEnemyDamageBonus(): difficultyGlobal == null");

            Curve difficultyIndexDamageCurve = GameDatabase.GetCurve(difficultyGlobals.DifficultyIndexDamageDefaultMtoP);
            if (difficultyIndexDamageCurve == null) return Logger.WarnReturn(0.0f, "GetIndexEnemyDamageBonus(): difficultyIndexDamageCurve == null");

            return difficultyIndexDamageCurve.GetAt(DifficultyIndex);
        }

        public float GetIndexEnemyDamageResistance()
        {
            DifficultyGlobalsPrototype difficultyGlobals = GameDatabase.DifficultyGlobalsPrototype;
            if (difficultyGlobals == null) return Logger.WarnReturn(0.0f, "GetIndexEnemyDamageResistance(): difficultyGlobal == null");

            Curve difficultyIndexDamageCurve = GameDatabase.GetCurve(difficultyGlobals.DifficultyIndexDamageDefaultPtoM);
            if (difficultyIndexDamageCurve == null) return Logger.WarnReturn(0.0f, "GetIndexEnemyDamageResistance(): difficultyIndexDamageCurve == null");

            return difficultyIndexDamageCurve.GetAt(DifficultyIndex);
        }

        public float GetIndexXPBonus()
        {
            if (Prototype == null) return Logger.WarnReturn(0.0f, "GetIndexXPBonus(): Prototype == null");

            Curve modifierCurve = GameDatabase.GetCurve(Prototype.PlayerXPByDifficultyIndexCurve);
            if (modifierCurve == null) return Logger.WarnReturn(0.0f, "GetIndexXPBonus(): modifierCurve == null");

            int difficultyIndex = Math.Clamp(DifficultyIndex, modifierCurve.MinPosition, modifierCurve.MaxPosition);
            return modifierCurve.GetAt(difficultyIndex);
        }

        public float GetIndexLootBonus()
        {
            if (Prototype == null) return Logger.WarnReturn(0.0f, "GetIndexLootBonus(): Prototype == null");

            Curve modifierCurve = GameDatabase.GetCurve(Prototype.LootFindByDifficultyIndexCurve);
            if (modifierCurve == null) return Logger.WarnReturn(0.0f, "GetIndexLootBonus(): modifierCurve == null");

            int difficultyIndex = Math.Clamp(DifficultyIndex, modifierCurve.MinPosition, modifierCurve.MaxPosition);
            return modifierCurve.GetAt(difficultyIndex);
        }

        /// <summary>
        /// Returns a damage multiplier based on the current difficulty and the number of nearby players.
        /// </summary>
        public float GetDamageMultiplier(bool isPlayerDamage, Rank targetRank, Vector3 targetPosition)
        {
            // TODO

            // Return something just for testing
            if (isPlayerDamage)
                return _tuningProto.TuningDamagePlayerToMobDCL;
            else
                return _tuningProto.TuningDamageMobToPlayerDCL;
        }

        private void BroadcastChange(int oldDifficultyIndex, int newDifficultyIndex)
        {
            // TODO
            Logger.Debug($"BroadcastChange(): [{_region}] - {oldDifficultyIndex} => {newDifficultyIndex}");
        }
    }
}