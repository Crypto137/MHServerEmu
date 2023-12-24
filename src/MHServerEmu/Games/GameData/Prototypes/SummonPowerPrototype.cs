namespace MHServerEmu.Games.GameData.Prototypes
{
    public class SummonPowerPrototype : PowerPrototype
    {
        public bool AttachSummonsToTarget { get; private set; }
        public bool SummonsLiveWhilePowerActive { get; private set; }
        public SummonEntityContextPrototype[] SummonEntityContexts { get; private set; }
        public EvalPrototype SummonMax { get; private set; }
        public bool SummonMaxReachedDestroyOwner { get; private set; }
        public int SummonIntervalMS { get; private set; }
        public bool SummonRandomSelection { get; private set; }
        public bool TrackInInventory { get; private set; }
        public bool AttachSummonsToCaster { get; private set; }
        public EvalPrototype SummonMaxSimultaneous { get; private set; }
        public ulong SummonMaxCountWithOthers { get; private set; }
        public bool PersistAcrossRegions { get; private set; }
        public EvalPrototype EvalSelectSummonContextIndex { get; private set; }
        public bool UseTargetAsSource { get; private set; }
        public bool KillPreviousSummons { get; private set; }
        public bool SummonAsPopulation { get; private set; }
    }

    public class SummonPowerOverridePrototype : PowerUnrealOverridePrototype
    {
        public ulong SummonEntity { get; private set; }
    }

    public class SummonRemovalPrototype : Prototype
    {
        public ulong FromPowers { get; private set; }
        public ulong Keywords { get; private set; }
    }

    public class SummonEntityContextPrototype : Prototype
    {
        public ulong SummonEntity { get; private set; }
        public LocomotorMethod PathFilterOverride { get; private set; }
        public bool RandomSpawnLocation { get; private set; }
        public bool IgnoreBlockingOnSpawn { get; private set; }
        public bool SnapToFloor { get; private set; }
        public bool TransferMissionPrototype { get; private set; }
        public float SummonRadius { get; private set; }
        public bool EnforceExactSummonPos { get; private set; }
        public bool ForceBlockingCollisionForSpawn { get; private set; }
        public bool VisibleWhileAttached { get; private set; }
        public Vector3Prototype SummonOffsetVector { get; private set; }
        public SummonRemovalPrototype SummonEntityRemoval { get; private set; }
        public EvalPrototype[] EvalOnSummon { get; private set; }
        public float SummonOffsetAngle { get; private set; }
        public bool HideEntityOnSummon { get; private set; }
        public bool CopyOwnerProperties { get; private set; }
        public bool KillEntityOnOwnerDeath { get; private set; }
        public PowerPrototype[] PowersToAssignToOwnerOnKilled { get; private set; }
        public PowerPrototype[] PowersToUnassignFromOwnerOnEnter { get; private set; }
        public EvalPrototype EvalCanSummon { get; private set; }
        public ulong TrackInInventoryOwnerCondition { get; private set; }
    }
}
