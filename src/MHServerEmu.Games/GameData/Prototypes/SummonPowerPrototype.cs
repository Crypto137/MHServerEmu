using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Memory;
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

        public override void PostProcess()
        {
            base.PostProcess();

            if (GameDatabase.DataDirectory.PrototypeIsAbstract(DataRef)) return;

            var targetingStyleProto = GetTargetingStyle();
            if (targetingStyleProto == null) return;

            float maxRadius = 0f;

            if (SummonEntityContexts.IsNullOrEmpty()) return;

            foreach (var context in SummonEntityContexts)
            {
                if (context == null) return;

                var summonEntityProto = context.SummonEntity.As<WorldEntityPrototype>();
                if (summonEntityProto == null && context.SummonEntityRemoval == null) return;

                if (summonEntityProto is HotspotPrototype hotspotProto && hotspotProto.Bounds != null)
                {
                    if (targetingStyleProto.TargetingShape == TargetingShapeType.CircleArea)
                    {
                        var bounds = hotspotProto.Bounds;
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

            if (maxRadius > 0) Radius = maxRadius;
        }

        public bool IsPetSummoningPower()
        {
            var keywordGlobalsProto = GameDatabase.KeywordGlobalsPrototype;
            if (keywordGlobalsProto != null)
                return HasKeyword(keywordGlobalsProto.PetPowerKeyword.As<KeywordPrototype>());
            return false;
        }

        public bool IsHotspotSummoningPower()
        {
            if (SummonEntityContexts.IsNullOrEmpty()) return false;
            foreach (var context in SummonEntityContexts)
            {
                if (context == null) return false;
                var summonEntityProto = context.SummonEntity.As<WorldEntityPrototype>();
                if (summonEntityProto is HotspotPrototype)
                    return true;
            }
            return false;
        }

        public WorldEntityPrototype GetSummonEntity(int contextIndex, AssetId entityRef)
        {
            var context = GetSummonEntityContext(contextIndex);
            if (context == null || context.SummonEntity == PrototypeId.Invalid) return null;
            if (PowerUnrealOverrides.HasValue())
                foreach (var powerOverride in PowerUnrealOverrides)
                    if (powerOverride is SummonPowerOverridePrototype summonPowerOverride && summonPowerOverride.EntityArt == entityRef)
                    {
                        var summonEntity = summonPowerOverride.SummonEntity.As<WorldEntityPrototype>();
                        if (summonEntity != null)
                            return summonEntity;
                    }

            return context.SummonEntity.As<WorldEntityPrototype>();
        }

        public SummonEntityContextPrototype GetSummonEntityContext(int contextIndex)
        {
            if (SummonEntityContexts.IsNullOrEmpty()) return null;
            if (contextIndex < 0 || contextIndex >= SummonEntityContexts.Length) return null;
            var context = SummonEntityContexts[contextIndex];
            return context;
        }

        public int GetMaxNumSimultaneousSummons(PropertyCollection properties)
        {
            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, properties);
            return Eval.RunInt(SummonMaxSimultaneous, evalContext);
        }

        public int GetMaxNumSummons(PropertyCollection properties)
        {
            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, properties);
            return Eval.RunInt(SummonMax, evalContext);
        }

        public bool InSummonMaxCountWithOthers(PropertyValue powerRef)
        {
            if (SummonMaxCountWithOthers.IsNullOrEmpty()) return false;
            return SummonMaxCountWithOthers.Contains(powerRef);
        }
    }

    public class SummonPowerOverridePrototype : PowerUnrealOverridePrototype
    {
        public PrototypeId SummonEntity { get; protected set; }
    }

    public class SummonRemovalPrototype : Prototype
    {
        public PrototypeId[] FromPowers { get; protected set; }
        public PrototypeId[] Keywords { get; protected set; }
    }

    public class SummonEntityContextPrototype : Prototype
    {
        public PrototypeId SummonEntity { get; protected set; }
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
        public PrototypeId TrackInInventoryOwnerCondition { get; protected set; }
    }
}
