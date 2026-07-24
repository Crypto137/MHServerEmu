using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.GameData.Calligraphy;

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
            if (!Verify.IsTrue(EndlessThemes.HasValue())) return null;
            if (!Verify.IsTrue(endlessLevel > 0)) return null;
            if (!Verify.IsTrue(endlessLevelsTotal > 0)) return null;

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
            {
                foreach (SubGenerationPrototype subAreaSequence in SubAreaSequences)
                {
                    if (subAreaSequence != null && subAreaSequence.AreaSequence.HasValue())
                        HelperGetAreasInGenerator(subAreaSequence.AreaSequence, areas);
                }
            }

            if (EndlessThemes.HasValue())
            {
                foreach (EndlessThemePrototype endlessProto in EndlessThemes)
                {
                    if (!Verify.IsNotNull(endlessProto))
                        continue;

                    if (endlessProto.Normal != null && endlessProto.Normal.AreaSequence.HasValue())
                        HelperGetAreasInGenerator(endlessProto.Normal.AreaSequence, areas);

                    if (endlessProto.Boss != null && endlessProto.Boss.AreaSequence.HasValue())
                        HelperGetAreasInGenerator(endlessProto.Boss.AreaSequence, areas);

                    if (endlessProto.TreasureRoom != null && endlessProto.TreasureRoom.AreaSequence.HasValue())
                        HelperGetAreasInGenerator(endlessProto.TreasureRoom.AreaSequence, areas);
                }
            }
        }

        private static void HelperGetAreasInGenerator(AreaSequenceInfoPrototype[] areaSequence, HashSet<PrototypeId> areas)
        {
            foreach (AreaSequenceInfoPrototype areaSequenceInfoProto in areaSequence)
            {
                if (!Verify.IsNotNull(areaSequenceInfoProto))
                    continue;

                if (areaSequenceInfoProto.AreaChoices.HasValue())
                {
                    foreach (WeightedAreaPrototype weightedAreaProto in areaSequenceInfoProto.AreaChoices)
                    {
                        if (!Verify.IsNotNull(weightedAreaProto))
                            continue;

                        if (!Verify.IsTrue(weightedAreaProto.Area != PrototypeId.Invalid))
                            continue;

                        areas.Add(weightedAreaProto.Area);
                    }
                }

                if (areaSequenceInfoProto.ConnectedTo.HasValue())
                    HelperGetAreasInGenerator(areaSequenceInfoProto.ConnectedTo, areas);
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
        public AreaSequenceInfoPrototype[] AreaSequence { get; protected set; }
        public EndlessStateEntryPrototype[] Challenges { get; protected set; }

        //---

        public EndlessStateEntryPrototype GetState(int randomSeed, int endlessLevel, MetaStateChallengeTierEnum missionTier)
        {
            if (Challenges.IsNullOrEmpty())
                return null;

            GRandom random = new(randomSeed + endlessLevel);
            Picker<EndlessStateEntryPrototype> picker = new(random);

            foreach (EndlessStateEntryPrototype state in Challenges)
            {
                if (state == null)
                    continue;

                if (missionTier != MetaStateChallengeTierEnum.None && missionTier != state.Tier)
                    continue;

                picker.Add(state);
            }

            if (picker.Empty() == false && picker.Pick(out EndlessStateEntryPrototype pickedState))
                return pickedState;

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

        //---

        public override string ToString()
        {
            return $"{GameDatabase.GetFormattedPrototypeName(Area)} weight = {Weight}";
        }
    }

}
