using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)NoRestriction)]
    [Flags]
    public enum RegionDirection
    {
        NoRestriction = 0,
        North = 1,
        East = 2,
        South = 4,
        West = 8,
        NorthSouth = 5,
        EastWest = 10,
    }

    #endregion

    public class SequenceRegionGeneratorPrototype : RegionGeneratorPrototype
    {
        public AreaSequenceInfoPrototype[] AreaSequence { get; protected set; }
        public PrototypeId RegionPOIPicker { get; protected set; }
        public int EndlessLevelsPerTheme { get; protected set; }
        public EndlessThemePrototype[] EndlessThemes { get; protected set; }
        public SubGenerationPrototype[] SubAreaSequences { get; protected set; }

        //---

        public EndlessThemeEntryPrototype GetEndlessGeneration(int randomSeed, int endlessLevel, int endlessLevelsTotal)
        {
            if (EndlessThemes == null || endlessLevel <= 0 || endlessLevelsTotal <= 0) return null;

            int totalThemes = EndlessThemes.Length;
            int randomIndex = randomSeed % totalThemes;
            int endlessOffset = (endlessLevel - 1) / endlessLevelsTotal;
            int selectedIndex = (randomIndex + endlessOffset) % totalThemes;

            EndlessThemePrototype EndlessTheme = EndlessThemes[selectedIndex];
            int levelInTheme = endlessLevel % endlessLevelsTotal;

            if (levelInTheme == 0)
                return EndlessTheme.TreasureRoom;
            else if (levelInTheme == endlessLevelsTotal - 1)
                return EndlessTheme.Boss;
            else
                return EndlessTheme.Normal;
        }

        public override void GetAreasInGenerator(HashSet<PrototypeId> areas)
        {
            if (AreaSequence.HasValue())
                HelperGetAreasInGenerator(AreaSequence, areas);

            if (SubAreaSequences.HasValue())
                foreach (var subAreaSequence in SubAreaSequences)
                    if (subAreaSequence != null && subAreaSequence.AreaSequence.HasValue())
                        HelperGetAreasInGenerator(subAreaSequence.AreaSequence, areas);

            if (EndlessThemes.HasValue())
                foreach (var endlessTheme in EndlessThemes)
                {
                    if (endlessTheme == null) continue;
                    if (endlessTheme.Normal != null && endlessTheme.Normal.AreaSequence.HasValue())
                        HelperGetAreasInGenerator(endlessTheme.Normal.AreaSequence, areas);
                    if (endlessTheme.Boss != null && endlessTheme.Boss.AreaSequence.HasValue())
                        HelperGetAreasInGenerator(endlessTheme.Boss.AreaSequence, areas);
                    if (endlessTheme.TreasureRoom != null && endlessTheme.TreasureRoom.AreaSequence.HasValue())
                        HelperGetAreasInGenerator(endlessTheme.TreasureRoom.AreaSequence, areas);
                }
        }

        private static void HelperGetAreasInGenerator(AreaSequenceInfoPrototype[] areaSequence, HashSet<PrototypeId> areas)
        {
            foreach (var areaProto in areaSequence)
            {
                if (areaProto == null) continue;
                if (areaProto.AreaChoices.HasValue())
                    foreach (var weightedArea in areaProto.AreaChoices)
                    {
                        if (weightedArea == null || weightedArea.Area == PrototypeId.Invalid) continue;
                        areas.Add(weightedArea.Area);
                    }

                if (areaProto.ConnectedTo.HasValue())
                    HelperGetAreasInGenerator(areaProto.ConnectedTo, areas);
            }
        }
    }

    public class SubGenerationPrototype : Prototype
    {
        public AreaSequenceInfoPrototype[] AreaSequence { get; protected set; }
        public float MinRootSeparation { get; protected set; }
        public int Tries { get; protected set; }
    }

    public class EndlessThemePrototype : Prototype
    {
        public EndlessThemeEntryPrototype Boss { get; protected set; }
        public EndlessThemeEntryPrototype Normal { get; protected set; }
        public EndlessThemeEntryPrototype TreasureRoom { get; protected set; }
    }

    public class EndlessThemeEntryPrototype : Prototype
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public AreaSequenceInfoPrototype[] AreaSequence { get; protected set; }
        public EndlessStateEntryPrototype[] Challenges { get; protected set; }

        public EndlessStateEntryPrototype GetState(int randomSeed, int endlessLevel, MetaStateChallengeTierEnum missionTier)
        {
            if (Challenges.IsNullOrEmpty()) return null;

            GRandom random = new(randomSeed + endlessLevel);
            Picker<EndlessStateEntryPrototype> picker = new (random);

            foreach (var state in Challenges)
            {
                if (state == null) continue;

                if (missionTier != MetaStateChallengeTierEnum.None && missionTier != state.Tier) continue;

                MetaStatePrototype metaState = GameDatabase.GetPrototype<MetaStatePrototype>(state.MetaState);
                if (metaState != null && !metaState.CanApplyState())
                {
                    Logger.Warn($"EndlessThemeEntryPrototype::GetState() State Disabled.");
                    continue;
                }

                picker.Add(state);
            }

            if (!picker.Empty() && picker.Pick(out EndlessStateEntryPrototype statePicked)) return statePicked;

            return null;
        }

    }

    public class EndlessStateEntryPrototype : Prototype
    {
        public PrototypeId MetaState { get; protected set; }
        public PrototypeId RegionPOIPicker { get; protected set; }
        public MetaStateChallengeTierEnum Tier { get; protected set; }
    }

    public class AreaSequenceInfoPrototype : Prototype
    {
        public WeightedAreaPrototype[] AreaChoices { get; protected set; }
        public AreaSequenceInfoPrototype[] ConnectedTo { get; protected set; }
        public short ConnectedToPicks { get; protected set; }
        public bool ConnectAllShared { get; protected set; }
        public short SharedEdgeMinimum { get; protected set; }
        public short Weight { get; protected set; }
    }

    public class WeightedAreaPrototype : Prototype
    {
        public PrototypeId Area { get; protected set; }
        public int Weight { get; protected set; }
        public RegionDirection ConnectOn { get; protected set; }
        public PrototypeId RespawnOverride { get; protected set; }
        public bool AlignedToPrevious { get; protected set; }

        public override string ToString() => $"{GameDatabase.GetFormattedPrototypeName(Area)} weight = {Weight}";
    }

}
