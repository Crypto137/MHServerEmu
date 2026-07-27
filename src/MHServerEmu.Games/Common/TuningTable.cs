using MHServerEmu.Core.Collections;
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

namespace MHServerEmu.Games.Common
{
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
    public class TuningTable
#else
    public class DifficultyTable
#endif
    {
        // NOTE: In the client this class is referenced as D:\mirrorBuilds_source05\MarvelGame_v52\Source\Game\Game\Combat\TuningTable.cpp,
        // but it's awkward for namespaces and classes to use the same names in C#, so we moved both combat classes to Common.

        // Pre-BUE this class was named DifficultyTable (and DifficultyPrototype instead of TuningPrototype respectively).

        private Region _region;
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        private PrototypeId _tuningRef;
        private TuningPrototype _tuningProto;
#else
        private PrototypeId _difficultyRef;
        private DifficultyPrototype _difficultyProto;
#endif
        private int _difficultyIndexMin;
        private int _difficultyIndexMax;
        private int _difficultyIndex;

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        public TuningPrototype Prototype { get => _tuningProto; }
#else
        public DifficultyPrototype Prototype { get => _difficultyProto; }
#endif
        public int DifficultyIndex { get => _difficultyIndex > 0 ? _difficultyIndex : 0; set => SetDifficultyIndex(value, true); }

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        public TuningTable(Region region)
#else
        public DifficultyTable(Region region)
#endif
        {
            _region = region;

            DifficultyGlobalsPrototype difficultyGlobals = GameDatabase.DifficultyGlobalsPrototype;
            if (!Verify.IsNotNull(difficultyGlobals)) return;

            Curve difficultyIndexCurve = GameDatabase.GetCurve(difficultyGlobals.DifficultyIndexDamageDefaultPtoM);
            if (Verify.IsNotNull(difficultyIndexCurve, "Failed to retrieve DifficultyIndexDamageDefaultPtoM from DifficultyGlobals! Is it set?"))
            {
                _difficultyIndexMin = difficultyIndexCurve.MinPosition;
                _difficultyIndexMax = difficultyIndexCurve.MaxPosition;
            }
        }

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        public void SetTuningTable(PrototypeId tuningRef)
        {
            if (_tuningRef != tuningRef)
            {
                _tuningRef = tuningRef;
                _tuningProto = GameDatabase.GetPrototype<TuningPrototype>(tuningRef);
            }
        }
#else
        public void SetDifficultyTable(PrototypeId difficultyRef)
        {
            if (_difficultyRef != difficultyRef)
            {
                _difficultyRef = difficultyRef;
                _difficultyProto = GameDatabase.GetPrototype<DifficultyPrototype>(difficultyRef);
            }
        }
#endif

        public RankPrototype RollRank(List<RankPrototype> ranks, bool noAffixes)
        {
            RankPrototype rank = GameDatabase.PopulationGlobalsPrototype.GetRankByEnum(Rank.Popcorn);

            if (ranks.Any(r => r.Rank != Rank.Popcorn) == false)
            {
                Picker<RankPrototype> picker = new(_region.Game.Random);
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
                _tuningProto.BuildRankPicker(_region.DifficultyTierRef, noAffixes, picker);
#else
                _difficultyProto.BuildRankPicker(noAffixes, picker);
#endif
                if (picker.Empty() == false)
                    picker.Pick(out rank);
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
            if (!Verify.IsNotNull(difficultyGlobals)) return 0f;

            Curve difficultyIndexDamageCurve = GameDatabase.GetCurve(difficultyGlobals.DifficultyIndexDamageDefaultMtoP);
            if (!Verify.IsNotNull(difficultyIndexDamageCurve)) return 0f;

            return difficultyIndexDamageCurve.GetAt(DifficultyIndex);
        }

        public float GetIndexEnemyDamageResistance()
        {
            DifficultyGlobalsPrototype difficultyGlobals = GameDatabase.DifficultyGlobalsPrototype;
            if (!Verify.IsNotNull(difficultyGlobals)) return 0f;

            Curve difficultyIndexDamageCurve = GameDatabase.GetCurve(difficultyGlobals.DifficultyIndexDamageDefaultPtoM);
            if (!Verify.IsNotNull(difficultyIndexDamageCurve)) return 0f;

            return difficultyIndexDamageCurve.GetAt(DifficultyIndex);
        }

        public float GetIndexXPBonus()
        {
            if (!Verify.IsNotNull(Prototype)) return 0f;

            Curve modifierCurve = GameDatabase.GetCurve(Prototype.PlayerXPByDifficultyIndexCurve);
            if (!Verify.IsNotNull(modifierCurve)) return 0f;

            int difficultyIndex = Math.Clamp(DifficultyIndex, modifierCurve.MinPosition, modifierCurve.MaxPosition);
            return modifierCurve.GetAt(difficultyIndex);
        }

        public float GetIndexLootBonus()
        {
            if (!Verify.IsNotNull(Prototype)) return 0f;

            Curve modifierCurve = GameDatabase.GetCurve(Prototype.LootFindByDifficultyIndexCurve);
            if (!Verify.IsNotNull(modifierCurve)) return 0f;

            int difficultyIndex = Math.Clamp(DifficultyIndex, modifierCurve.MinPosition, modifierCurve.MaxPosition);
            return modifierCurve.GetAt(difficultyIndex);
        }

        /// <summary>
        /// Returns a damage multiplier based on the current difficulty and the number of nearby players.
        /// </summary>
        public float GetDamageMultiplier(bool isPlayerDamage, Rank targetRank, Vector3 targetPosition)
        {
            float damageMult = 1f;

            // Some older regions (e.g. Regions/EndGame/Terminals/Red/ShockerSubway) don't have tuning tables assigned.
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            if (_tuningProto != null)
#else
            if (_difficultyProto != null)
#endif
            {
                damageMult *= GetRegionDifficultyDamageMultiplier(isPlayerDamage, targetRank);
                damageMult *= GetDifficultyIndexDamageMultiplier(isPlayerDamage, targetRank);
                damageMult *= GetNumNearbyPlayersDamageMultiplier(isPlayerDamage, targetRank, targetPosition);
            }

            return damageMult;
        }

        private float GetRegionDifficultyDamageMultiplier(bool isPlayerDamage, Rank targetRank)
        {
            float difficultyMult = 1f;

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            TuningPrototype tableProto = _tuningProto;
#else
            DifficultyPrototype tableProto = _difficultyProto;
#endif

            if (isPlayerDamage)
            {
                difficultyMult *= tableProto.TuningDamagePlayerToMobDCL;
                difficultyMult *= _region.Properties[PropertyEnum.DamageRegionPlayerToMob];
            }
            else
            {
                difficultyMult *= tableProto.TuningDamageMobToPlayerDCL;
                difficultyMult *= _region.Properties[PropertyEnum.DamageRegionMobToPlayer];
            }

            // Apply rank-specific multipliers
            if (tableProto.TuningDamageByRankDCL.HasValue())
            {
                foreach (TuningDamageByRankPrototype rankEntry in tableProto.TuningDamageByRankDCL)
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
            if (!Verify.IsNotNull(curve)) return 1f;

            int index = Math.Clamp(DifficultyIndex, curve.MinPosition, curve.MaxPosition);
            return curve.GetAt(index);
        }

        private float GetNumNearbyPlayersDamageMultiplier(bool isPlayerDamage, Rank targetRank, Vector3 targetPosition)
        {
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            TuningPrototype tableProto = _tuningProto;
#else
            DifficultyPrototype tableProto = _difficultyProto;
#endif

            // Check if this region scales with the number of players
            if (tableProto.NumNearbyPlayersScalingEnabled == false)
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
            if (!Verify.IsNotNull(curve)) return 1f;

            int numNearbyPlayers = Power.ComputeNearbyPlayers(_region, targetPosition);
            int index = Math.Clamp(numNearbyPlayers, curve.MinPosition, curve.MaxPosition);
            return curve.GetAt(index);
        }

        private bool BroadcastChange(int oldDifficultyIndex, int newDifficultyIndex)
        {
            if (!Verify.IsTrue(oldDifficultyIndex != newDifficultyIndex)) return false;

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