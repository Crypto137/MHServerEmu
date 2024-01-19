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
        public ulong[] SummonMaxCountWithOthers { get; protected set; }
        public bool PersistAcrossRegions { get; protected set; }
        public EvalPrototype EvalSelectSummonContextIndex { get; protected set; }
        public bool UseTargetAsSource { get; protected set; }
        public bool KillPreviousSummons { get; protected set; }
        public bool SummonAsPopulation { get; protected set; }
    }

    public class SummonPowerOverridePrototype : PowerUnrealOverridePrototype
    {
        public ulong SummonEntity { get; protected set; }
    }

    public class SummonRemovalPrototype : Prototype
    {
        public ulong[] FromPowers { get; protected set; }
        public ulong[] Keywords { get; protected set; }
    }

    public class SummonEntityContextPrototype : Prototype
    {
        public ulong SummonEntity { get; protected set; }
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
        public ulong TrackInInventoryOwnerCondition { get; protected set; }
    }
}
