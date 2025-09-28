﻿using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)Invalid)]
    public enum DEPRECATEDDifficultyMode
    {
        Invalid = -1,
        Normal = 0,
        Heroic = 1,
        SuperHeroic = 2,
    }

    #endregion

    public class RegionDifficultySettingsPrototype : Prototype
    {
        public PrototypeId DifficultyTable { get; protected set; }
        public int DifficultyIndex { get; protected set; }          // V48
    }

    public class NumNearbyPlayersDmgByRankPrototype : Prototype
    {
        public Rank Rank { get; protected set; }
        public CurveId MobToPlayerCurve { get; protected set; }
        public CurveId PlayerToMobCurve { get; protected set; }
    }

    public class DifficultyIndexDamageByRankPrototype : Prototype
    {
        public Rank Rank { get; protected set; }
        public CurveId MobToPlayerCurve { get; protected set; }
        public CurveId PlayerToMobCurve { get; protected set; }
    }

    public class TuningDamageByRankPrototype : Prototype
    {
        public Rank Rank { get; protected set; }
        public float TuningMobToPlayer { get; protected set; }
        public float TuningPlayerToMob { get; protected set; }
    }

    public class NegStatusRankCurveEntryPrototype : Prototype
    {
        public Rank Rank { get; protected set; }
        public CurveId TenacityModifierCurve { get; protected set; }
    }

    public class NegStatusPropCurveEntryPrototype : Prototype
    {
        public PrototypeId NegStatusProp { get; protected set; }
        public NegStatusRankCurveEntryPrototype[] RankEntries { get; protected set; }

        //---

        public CurveId GetCurveRefForRank(Rank rank)
        {
            int index = (int)rank;
            if (index < 0 || index >= RankEntries.Length)
                return CurveId.Invalid;

            NegStatusRankCurveEntryPrototype entry = RankEntries[index];
            if (entry.Rank != rank)
                return CurveId.Invalid;

            return entry.TenacityModifierCurve;
        }
    }

    public class DifficultyPrototype : Prototype
    {
        public LocaleStringId Name { get; protected set; }
        public float PlayerInflictedDamageTimerSec { get; protected set; }
        public float PlayerNearbyRange { get; protected set; }
        public NegStatusPropCurveEntryPrototype[] NegativeStatusCurves { get; protected set; }
        public float PlayerXPNearbyRange { get; protected set; }    // V48
        public CurveId LootFindByLevelDeltaCurve { get; protected set; }
        public CurveId SpecialItemFindByLevelDeltaCurve { get; protected set; }
        public CurveId LootFindByDifficultyIndexCurve { get; protected set; }
        public CurveId PlayerXPByDifficultyIndexCurve { get; protected set; }
        public PrototypeId DeathPenaltyCondition { get; protected set; }
        public CurveId PctXPFromParty { get; protected set; }
        public CurveId PctXPFromRaid { get; protected set; }
        public PrototypeId Tier { get; protected set; }
        public bool UseTierMinimapColor { get; protected set; }
        public DifficultyRankEntryPrototype[] RankAffixTable { get; protected set; }
        public float PctXPMultiplier { get; protected set; }
        public bool NumNearbyPlayersScalingEnabled { get; protected set; }
        public float TuningDamageMobToPlayer { get; protected set; }
        public float TuningDamageMobToPlayerDCL { get; protected set; }
        public float TuningDamagePlayerToMob { get; protected set; }
        public float TuningDamagePlayerToMobDCL { get; protected set; }
        public TuningDamageByRankPrototype[] TuningDamageByRank { get; protected set; }
        public TuningDamageByRankPrototype[] TuningDamageByRankDCL { get; protected set; }

        public Picker<RankPrototype> BuildRankPicker(GRandom random, bool noAffixes)
        {
            Picker<RankPrototype> picker = new(random);
            var table = GetRankAffixTable();

            if (table != null)
                foreach (var entry in table)
                    if (entry.Weight > 0 && (noAffixes || entry.GetMaxAffixes() > 0))
                        picker.Add(entry.Rank.As<RankPrototype>(), entry.Weight);

            return picker;
        }

        public DifficultyRankEntryPrototype[] GetRankAffixTable()
        {
            return RankAffixTable;
        }

        public DifficultyRankEntryPrototype GetDifficultyRankEntry(RankPrototype rankProto)
        {
            var table = GetRankAffixTable();
            if (table != null)
                foreach (var entry in table)
                {
                    var entryRank = entry.Rank.As<RankPrototype>();
                    if (entryRank != null && entryRank.Rank == rankProto.Rank)
                        return entry;
                }
            return null;
        }
    }

    public class DifficultyModePrototype : Prototype
    {
        public AssetId IconPath { get; protected set; }
        public LocaleStringId Name { get; protected set; }
        public DEPRECATEDDifficultyMode DifficultyModeEnum { get; protected set; }
        public int MigrationUnlocksAtLevel { get; protected set; }
        public PrototypeId UnlockNotification { get; protected set; }
        public PrototypeId TextStyle { get; protected set; }
    }

    public class DifficultyTierPrototype : Prototype
    {
        public int Tier { get; protected set; }
        public AssetId MinimapNameColor { get; protected set; }

        //--

        public static bool InRange(PrototypeId value, PrototypeId min, PrototypeId max)
        {
            if (min == PrototypeId.Invalid && max == PrototypeId.Invalid) return true;

            var valueProto = GameDatabase.GetPrototype<DifficultyTierPrototype>(value);
            if (valueProto == null) return false;

            var minProto = GameDatabase.GetPrototype<DifficultyTierPrototype>(min);
            if (minProto != null && valueProto.Tier < minProto.Tier) return false;
            var maxProto = GameDatabase.GetPrototype<DifficultyTierPrototype>(max);
            if (maxProto != null && valueProto.Tier > maxProto.Tier) return false;

            return true;
        }

        public static bool InRange(DifficultyTierPrototype valueProto, DifficultyTierPrototype minProto, DifficultyTierPrototype maxProto)
        {
            if (valueProto == null) return false;
            if (minProto == null && maxProto == null) return true;
            if (minProto != null && valueProto.Tier < minProto.Tier) return false;
            if (maxProto != null && valueProto.Tier > maxProto.Tier) return false;
            return true;
        }
    }

    public class RankAffixEntryPrototype : Prototype
    {
        public PrototypeId AffixTable { get; protected set; }
        public int ChancePct { get; protected set; }

        [DoNotCopy]
        public EnemyBoostSetPrototype AffixTablePrototype { get => AffixTable.As<EnemyBoostSetPrototype>(); }

        public PrototypeId RollAffix(GRandom random, HashSet<PrototypeId> affixes, HashSet<PrototypeId> exclude)
        {
            var affixTableProto = AffixTablePrototype;
            if (affixTableProto != null && random.NextPct(ChancePct))
            {
                Picker<PrototypeId> picker = new(random);

                if (affixes.Count > 0)
                    foreach (var affixRef in affixes)
                        if (affixTableProto.Contains(affixRef))
                            picker.Add(affixRef);

                if (picker.Pick(out PrototypeId pickRef))
                    return pickRef;

                if (affixTableProto.Modifiers.HasValue())
                    foreach (var affix in affixTableProto.Modifiers)
                        if (exclude.Contains(affix) == false)
                            picker.Add(affix);

                if (picker.Pick(out pickRef))
                    return pickRef;
            }

            return PrototypeId.Invalid;
        }
    }

    public class DifficultyGlobalsPrototype : Prototype
    {
        public CurveId MobConLevelCurve { get; protected set; }
        public RegionDifficultySettingsPrototype RegionSettingsDefault { get; protected set; }
        public RegionDifficultySettingsPrototype RegionSettingsDefaultPCZ { get; protected set; }
        public CurveId NumNearbyPlayersDmgDefaultMtoP { get; protected set; }
        public CurveId NumNearbyPlayersDmgDefaultPtoM { get; protected set; }
        public NumNearbyPlayersDmgByRankPrototype[] NumNearbyPlayersDmgByRank { get; protected set; }
        public NumNearbyPlayersDmgByRankPrototype[] NumNearbyPlayersDmgByRankPCZ { get; protected set; }
        public CurveId DifficultyIndexDamageDefaultMtoP { get; protected set; }
        public CurveId DifficultyIndexDamageDefaultPtoM { get; protected set; }
        public DifficultyIndexDamageByRankPrototype[] DifficultyIndexDamageByRank { get; protected set; }
        public float PvPDamageMultiplier { get; protected set; }
        public float PvPCritDamageMultiplier { get; protected set; }
        public CurveId PvPDamageScalarFromLevelCurve { get; protected set; }
        public CurveId TeamUpDamageScalarFromLevelCurve { get; protected set; }
        public EvalPrototype EvalDamageLevelDeltaMtoP { get; protected set; }
        public EvalPrototype EvalDamageLevelDeltaPtoM { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public float GetTeamUpDamageScalar(int combatLevel)
        {
            Curve teamUpDamageScalarCurve = TeamUpDamageScalarFromLevelCurve.AsCurve();
            if (teamUpDamageScalarCurve == null) return Logger.WarnReturn(1f, "GetTeamUpDamageScalar(): teamUpDamageScalarCurve == null");

            return teamUpDamageScalarCurve.GetAt(combatLevel);
        }
    }
}
