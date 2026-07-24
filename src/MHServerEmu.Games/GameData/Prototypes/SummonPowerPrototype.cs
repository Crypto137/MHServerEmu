using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class SummonPowerPrototype : PowerPrototype
    {
        public bool AttachSummonsToTarget { get; protected set; }
        public bool SummonsLiveWhilePowerActive { get; protected set; }
        public SummonEntityContextPrototype[] SummonEntityContexts { get; protected set; }
        public EvalPrototype SummonMax { get; protected set; }
        public bool SummonMaxReachedDestroyOwner { get; protected set; }
        public int SummonIntervalMS { get; protected set; }
        public bool SummonRandomSelection { get; protected set; }
        public bool TrackInInventory { get; protected set; }
        public bool AttachSummonsToCaster { get; protected set; }
        public EvalPrototype SummonMaxSimultaneous { get; protected set; }
        public PrototypeId[] SummonMaxCountWithOthers { get; protected set; }
        public bool PersistAcrossRegions { get; protected set; }
        public EvalPrototype EvalSelectSummonContextIndex { get; protected set; }
        public bool UseTargetAsSource { get; protected set; }
        public bool KillPreviousSummons { get; protected set; }
        public bool SummonAsPopulation { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();

            if (GameDatabase.DataDirectory.PrototypeIsAbstract(DataRef))
                return;

            TargetingStylePrototype targetingStyleProto = GetTargetingStyle();
            if (!Verify.IsNotNull(targetingStyleProto)) return;

            float maxRadius = 0f;

            if (SummonEntityContexts.IsNullOrEmpty())
                return;

            foreach (SummonEntityContextPrototype context in SummonEntityContexts)
            {
                if (!Verify.IsNotNull(context)) return;

                WorldEntityPrototype summonEntityProto = context.SummonEntity;
                if (!Verify.IsTrue(summonEntityProto != null || context.SummonEntityRemoval != null)) return;

                if (summonEntityProto is HotspotPrototype hotspotProto && hotspotProto.Bounds != null)
                {
                    if (targetingStyleProto.TargetingShape == TargetingShapeType.CircleArea)
                    {
                        BoundsPrototype bounds = hotspotProto.Bounds;
                        if (bounds is CapsuleBoundsPrototype capsuleBounds)
                        {
                            if (capsuleBounds.Radius > maxRadius)
                                maxRadius = capsuleBounds.Radius;
                        }
                        else if (bounds is SphereBoundsPrototype sphereBounds)
                        {
                            if (sphereBounds.Radius > maxRadius)
                                maxRadius = sphereBounds.Radius;
                        }
                    }
                }
            }

            if (maxRadius > 0)
                Radius = maxRadius;
        }

        public bool IsPetSummoningPower()
        {
            KeywordGlobalsPrototype keywordGlobalsProto = GameDatabase.KeywordGlobalsPrototype;
            if (!Verify.IsNotNull(keywordGlobalsProto)) return false;

            return HasKeyword(keywordGlobalsProto.PetPowerKeyword);
        }

        public bool IsHotspotSummoningPower()
        {
            if (SummonEntityContexts.IsNullOrEmpty())
                return false;

            foreach (SummonEntityContextPrototype context in SummonEntityContexts)
            {
                if (!Verify.IsNotNull(context)) return false;

                if (context.SummonEntity == null)
                    continue;

                if (context.SummonEntity is HotspotPrototype)
                    return true;
            }

            return false;
        }

        public WorldEntityPrototype GetSummonEntity(int contextIndex, AssetId entityRef)
        {
            SummonEntityContextPrototype context = GetSummonEntityContext(contextIndex);
            if (!Verify.IsNotNull(context)) return null;
            if (!Verify.IsNotNull(context.SummonEntity)) return null;

            if (PowerUnrealOverrides.HasValue())
            {
                foreach (PowerUnrealOverridePrototype powerOverride in PowerUnrealOverrides)
                {
                    if (powerOverride is not SummonPowerOverridePrototype summonPowerOverride)
                        continue;

                    if (summonPowerOverride.EntityArt != entityRef)
                        continue;

                    if (summonPowerOverride.SummonEntity == null)
                        continue;

                    return summonPowerOverride.SummonEntity;
                }
            }

            return context.SummonEntity;
        }

        public SummonEntityContextPrototype GetSummonEntityContext(int contextIndex)
        {
            if (!Verify.IsTrue(SummonEntityContexts.HasValue())) return null;
            if (!Verify.IsTrue(contextIndex >= 0 && contextIndex < SummonEntityContexts.Length)) return null;

            SummonEntityContextPrototype context = SummonEntityContexts[contextIndex];
            if (!Verify.IsNotNull(context)) return null;

            return context;
        }

        public int GetMaxNumSimultaneousSummons(PropertyCollection properties)
        {
            if (!Verify.IsNotNull(SummonMaxSimultaneous)) return 0;

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, properties);
            return Eval.RunInt(SummonMaxSimultaneous, evalContext);
        }

        public int GetMaxNumSummons(PropertyCollection properties)
        {
            if (!Verify.IsNotNull(SummonMax)) return 0;

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, properties);
            return Eval.RunInt(SummonMax, evalContext);
        }

        public bool InSummonMaxCountWithOthers(PropertyValue powerRef)
        {
            return SummonMaxCountWithOthers.HasValue() && SummonMaxCountWithOthers.Contains(powerRef);
        }
    }

    public class SummonPowerOverridePrototype : PowerUnrealOverridePrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public WorldEntityPrototype SummonEntity { get; protected set; }
    }

    public class SummonRemovalPrototype : Prototype
    {
        public PrototypeId[] FromPowers { get; protected set; }
        public PrototypeId[] Keywords { get; protected set; }
    }

    public class SummonEntityContextPrototype : Prototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public WorldEntityPrototype SummonEntity { get; protected set; }
        public LocomotorMethod PathFilterOverride { get; protected set; }
        public bool RandomSpawnLocation { get; protected set; }
        public bool IgnoreBlockingOnSpawn { get; protected set; }
        public bool SnapToFloor { get; protected set; }
        public bool TransferMissionPrototype { get; protected set; }
        public float SummonRadius { get; protected set; }
        public bool EnforceExactSummonPos { get; protected set; }
        public bool ForceBlockingCollisionForSpawn { get; protected set; }
        public bool VisibleWhileAttached { get; protected set; }
        public Vector3Prototype SummonOffsetVector { get; protected set; }
        public SummonRemovalPrototype SummonEntityRemoval { get; protected set; }
        public EvalPrototype[] EvalOnSummon { get; protected set; }
        public float SummonOffsetAngle { get; protected set; }
        public bool HideEntityOnSummon { get; protected set; }
        public bool CopyOwnerProperties { get; protected set; }
        public bool KillEntityOnOwnerDeath { get; protected set; }
        public PowerPrototype[] PowersToAssignToOwnerOnKilled { get; protected set; }
        public PowerPrototype[] PowersToUnassignFromOwnerOnEnter { get; protected set; }
        public EvalPrototype EvalCanSummon { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public ConditionPrototype TrackInInventoryOwnerCondition { get; protected set; }
    }
}
