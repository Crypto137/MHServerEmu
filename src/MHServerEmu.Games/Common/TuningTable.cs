using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Social;

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
            float damageMult = 1f;
            damageMult *= GetRegionDifficultyDamageMultiplier(isPlayerDamage, targetRank);
            damageMult *= GetDifficultyIndexDamageMultiplier(isPlayerDamage, targetRank);
            damageMult *= GetNumNearbyPlayersDamageMultiplier(isPlayerDamage, targetRank, targetPosition);
            return damageMult;
        }

        private float GetRegionDifficultyDamageMultiplier(bool isPlayerDamage, Rank targetRank)
        {
            float difficultyMult = 1f;

            if (isPlayerDamage)
            {
                difficultyMult *= _tuningProto.TuningDamagePlayerToMobDCL;
                difficultyMult *= _region.Properties[PropertyEnum.DamageRegionPlayerToMob];
            }
            else
            {
                difficultyMult *= _tuningProto.TuningDamageMobToPlayerDCL;
                difficultyMult *= _region.Properties[PropertyEnum.DamageRegionMobToPlayer];
            }

            // Apply rank-specific multipliers
            if (_tuningProto.TuningDamageByRankDCL.HasValue())
            {
                foreach (TuningDamageByRankPrototype rankEntry in _tuningProto.TuningDamageByRankDCL)
                {
                    if (rankEntry.Rank != targetRank)
                        continue;

                    difficultyMult *= isPlayerDamage ? rankEntry.TuningPlayerToMob : rankEntry.TuningMobToPlayer;
                    break;
                }
            }

            return difficultyMult;
        }

        private float GetDifficultyIndexDamageMultiplier(bool isPlayerDamage, Rank targetRank)
        {
            DifficultyGlobalsPrototype difficultyGlobals = GameDatabase.DifficultyGlobalsPrototype;

            // Start with the default curve
            CurveId curveRef = isPlayerDamage ? difficultyGlobals.DifficultyIndexDamageDefaultPtoM : difficultyGlobals.DifficultyIndexDamageDefaultMtoP;

            // See if there are any rank overrides
            if (difficultyGlobals.DifficultyIndexDamageByRank.HasValue())
            {
                foreach (DifficultyIndexDamageByRankPrototype rankEntry in difficultyGlobals.DifficultyIndexDamageByRank)
                {
                    if (rankEntry.Rank != targetRank)
                        continue;

                    curveRef = isPlayerDamage ? rankEntry.PlayerToMobCurve : rankEntry.MobToPlayerCurve;
                    break;
                }
            }

            Curve curve = curveRef.AsCurve();
            if (curve == null) return Logger.WarnReturn(1f, "GetDifficultyIndexDamageMultiplier(): curve == null");

            int index = Math.Clamp(DifficultyIndex, curve.MinPosition, curve.MaxPosition);
            return curve.GetAt(index);
        }

        private float GetNumNearbyPlayersDamageMultiplier(bool isPlayerDamage, Rank targetRank, Vector3 targetPosition)
        {
            // Check if this region scales with the number of players
            if (_tuningProto.NumNearbyPlayersScalingEnabled == false)
                return 1f;

            DifficultyGlobalsPrototype difficultyGlobals = GameDatabase.DifficultyGlobalsPrototype;

            // Start with the default curve
            CurveId curveRef = isPlayerDamage ? difficultyGlobals.NumNearbyPlayersDmgDefaultPtoM : difficultyGlobals.NumNearbyPlayersDmgDefaultMtoP;

            // See if there are any rank overrides (public combat zones use a different set of overrides)
            NumNearbyPlayersDmgByRankPrototype[] rankOverrides = null;
            if (_region.Prototype.Behavior == RegionBehavior.PublicCombatZone && difficultyGlobals.NumNearbyPlayersDmgByRankPCZ.HasValue())
                rankOverrides = difficultyGlobals.NumNearbyPlayersDmgByRankPCZ;
            else
                rankOverrides = difficultyGlobals.NumNearbyPlayersDmgByRank;

            if (rankOverrides.HasValue())
            {
                foreach (NumNearbyPlayersDmgByRankPrototype rankEntry in rankOverrides)
                {
                    if (rankEntry.Rank != targetRank)
                        continue;

                    curveRef = isPlayerDamage ? rankEntry.PlayerToMobCurve : rankEntry.MobToPlayerCurve;
                    break;
                }
            }

            Curve curve = curveRef.AsCurve();
            if (curve == null) return Logger.WarnReturn(1f, "GetNumNearbyPlayersDamageMultiplier(): curve == null");

            int numNearbyPlayers = Power.ComputeNearbyPlayers(_region, targetPosition);
            int index = Math.Clamp(numNearbyPlayers, curve.MinPosition, curve.MaxPosition);
            return curve.GetAt(index);
        }

        private bool BroadcastChange(int oldDifficultyIndex, int newDifficultyIndex)
        {
            if (oldDifficultyIndex == newDifficultyIndex) return Logger.WarnReturn(false, "BroadcastChange(): oldDifficultyIndex == newDifficultyIndex");

            // Send a grow stronger / weaker message
            LocaleStringId messageStringId = LocaleStringId.Invalid;
            if (newDifficultyIndex > oldDifficultyIndex)
                messageStringId = GameDatabase.PopulationGlobalsPrototype.MessageEnemiesGrowStronger;
            else if (newDifficultyIndex < oldDifficultyIndex)
                messageStringId = GameDatabase.PopulationGlobalsPrototype.MessageEnemiesGrowWeaker;

            _region.Game.ChatManager.SendChatFromGameSystem(messageStringId, _region);

            // Send difficulty change
            foreach (Player player in new PlayerIterator(_region))
                player.SendRegionDifficultyChange(newDifficultyIndex);

            return true;
        }
    }
}