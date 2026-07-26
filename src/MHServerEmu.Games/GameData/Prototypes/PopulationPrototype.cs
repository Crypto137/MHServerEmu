using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)Invalid)]
    public enum MarkerType  // SpawnMarkers/PopulationType.type? Doesn't match exactly
    {
        Invalid = 0,
        Enemies = 1,    // Officer / Trash
        Encounter = 2,
        QuestGiver = 3,
        Transition = 4,
        Prop = 5,
    }

    [AssetEnum((int)Default)]
    public enum SpawnOrientationTweak
    {
        Default,
        Offset15,
        Random,
    }

    #endregion

    public class PopulationPrototype : Prototype
    {
        public PrototypeId RespawnMethod { get; protected set; }
        public float ClusterDensityPct { get; protected set; }
        public float ClusterDensityPeak { get; protected set; }
        public float EncounterDensityBase { get; protected set; }
        public float SpawnMapDensityMin { get; protected set; }
        public float SpawnMapDensityMax { get; protected set; }
        public float SpawnMapDensityStep { get; protected set; }
        public int SpawnMapHeatReturnPerSecond { get; protected set; }
        public EvalPrototype SpawnMapHeatReturnPerSecondEval { get; protected set; }
        public float SpawnMapHeatBleed { get; protected set; }
        public float SpawnMapCrowdSupression { get; protected set; }
        public int SpawnMapCrowdSupressionStart { get; protected set; }
        public EncounterDensityOverrideEntryPrototype[] EncounterDensityOverrides { get; protected set; }
        public PopulationObjectListPrototype GlobalEncounters { get; protected set; }
        public PopulationObjectListPrototype Themes { get; protected set; }
        public int SpawnMapDistributeDistance { get; protected set; }
        public int SpawnMapDistributeSpread { get; protected set; }
        public bool SpawnMapEnabled { get; protected set; }

        //---

        public const float PopulationClusterSq = 3200.0f;

        [DoNotCopy]
        public bool UseSpawnMap { get => SpawnMapEnabled || (SpawnMapDensityMin > 0.0 && SpawnMapDensityMax > 0.0f); }

        public override void PostProcess()
        {
            base.PostProcess();

            SpawnMapDensityMin = Math.Clamp(SpawnMapDensityMin, 0.0f, 0.8f);
            SpawnMapDensityMax = Math.Clamp(SpawnMapDensityMax, 0.0f, 0.8f);
            SpawnMapHeatBleed = Math.Clamp(SpawnMapHeatBleed, 0.0f, 0.8f);
        }

        public float GetEncounterDensity(PrototypeId markerRef)
        {
            if (EncounterDensityOverrides.HasValue())
            {
                foreach (EncounterDensityOverrideEntryPrototype entry in EncounterDensityOverrides)
                {
                    if (entry.MarkerType == markerRef)
                        return entry.Density;
                }
            }
        
            return EncounterDensityBase;
        }

        public PrototypeId PickTheme(GRandom random)
        {
            if (!Verify.IsNotNull(Themes, $"Population contains no themes.\r\t{this}"))
                return PrototypeId.Invalid;

            Picker<PrototypeId> picker = new(random);
            foreach (PopulationObjectInstancePrototype instance in Themes.List)
            {
                if (!Verify.IsNotNull(instance))
                    continue;

                if (!Verify.IsTrue(instance.Object != PrototypeId.Invalid))
                    continue;

                PopulationThemePrototype themeProto = instance.Object.As<PopulationThemePrototype>();
                if (!Verify.IsNotNull(themeProto))
                    continue;

                if (instance.Weight > 0)
                    picker.Add(instance.Object, instance.Weight);
            }

            PrototypeId pickedTheme = PrototypeId.Invalid;
            if (picker.Empty() == false)
                picker.Pick(out pickedTheme);

            return pickedTheme;
        }
    }

    public class SpawnMarkerPrototype : Prototype
    {
        public MarkerType Type { get; protected set; }
        public PrototypeId Shape { get; protected set; }
        public AssetId EditorIcon { get; protected set; }
    }

    public class PopulationMarkerPrototype : SpawnMarkerPrototype
    {
    }

    public class PropMarkerPrototype : SpawnMarkerPrototype
    {
    }

    public class PopulatablePrototype : Prototype
    {
    }

    public class PopulationInfoPrototype : PopulatablePrototype
    {
        public PrototypeId[] Ranks { get; protected set; }
        public bool Unique { get; protected set; }
    }

    public class RespawnMethodPrototype : Prototype
    {
        public float PlayerPresentDeferral { get; protected set; }
        public int DeferralMax { get; protected set; }
        public float RandomTimeOffset { get; protected set; }
    }

    public class RespawnReducerByThresholdPrototype : RespawnMethodPrototype
    {
        public float BaseRespawnTime { get; protected set; }
        public float RespawnReductionThreshold { get; protected set; }
        public float ReducedRespawnTime { get; protected set; }
        public float MinimumRespawnTime { get; protected set; }
    }

    public class PopulationObjectInstancePrototype : Prototype
    {
        public short Weight { get; protected set; }
        public PrototypeId Object { get; protected set; }
#if GAME_VERSION_1_53
        public PrototypeId RestrictToDifficultyMin { get; protected set; }
        public PrototypeId RestrictToDifficultyMax { get; protected set; }
#endif

        //---

        public void GetContainedEntities(HashSet<PrototypeId> entities)
        {
            if (Object != PrototypeId.Invalid)
            {
                Prototype proto = Object.As<Prototype>();

                if (proto is PopulationObjectPrototype populationObject)
                    populationObject.GetContainedEntities(entities);
                else if (proto is PopulationObjectListPrototype populationObjectList)
                    populationObjectList.GetContainedEntities(entities);
                else
                    Verify.IsTrue(false, "Unsupported population prototype");
            }
        }
    }

    public class PopulationObjectListPrototype : Prototype
    {
        public PopulationObjectInstancePrototype[] List { get; protected set; }

        //---

        public void GetContainedEntities(HashSet<PrototypeId> entities)
        {
            if (List.HasValue())
            {
                foreach (PopulationObjectInstancePrototype objectProto in List)
                {
                    if (!Verify.IsNotNull(objectProto))
                        continue;

                    objectProto.GetContainedEntities(entities);
                }
            }
        }
    }

    public class PopulationThemePrototype : Prototype
    {
        public PopulationObjectListPrototype Enemies { get; protected set; }
        public int EnemyPicks { get; protected set; }
        public PopulationObjectListPrototype Encounters { get; protected set; }

        //---

        public void GetContainedEntities(HashSet<PrototypeId> entities)
        {
            Enemies?.GetContainedEntities(entities);
            Encounters?.GetContainedEntities(entities);
        }
    }

    public class PopulationThemeSetPrototype : Prototype
    {
        public PrototypeId[] Themes { get; protected set; }
    }

    public class EncounterDensityOverrideEntryPrototype : Prototype
    {
        public PrototypeId MarkerType { get; protected set; }
        public float Density { get; protected set; }
    }
}
