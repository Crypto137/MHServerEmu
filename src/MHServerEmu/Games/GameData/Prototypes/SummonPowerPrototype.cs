namespace MHServerEmu.Games.GameData.Prototypes
{
    public class SummonPowerPrototype : PowerPrototype
    {
        public bool AttachSummonsToTarget { get; set; }
        public bool SummonsLiveWhilePowerActive { get; set; }
        public SummonEntityContextPrototype[] SummonEntityContexts { get; set; }
        public EvalPrototype SummonMax { get; set; }
        public bool SummonMaxReachedDestroyOwner { get; set; }
        public int SummonIntervalMS { get; set; }
        public bool SummonRandomSelection { get; set; }
        public bool TrackInInventory { get; set; }
        public bool AttachSummonsToCaster { get; set; }
        public EvalPrototype SummonMaxSimultaneous { get; set; }
        public ulong SummonMaxCountWithOthers { get; set; }
        public bool PersistAcrossRegions { get; set; }
        public EvalPrototype EvalSelectSummonContextIndex { get; set; }
        public bool UseTargetAsSource { get; set; }
        public bool KillPreviousSummons { get; set; }
        public bool SummonAsPopulation { get; set; }
    }

    public class SummonPowerOverridePrototype : PowerUnrealOverridePrototype
    {
        public ulong SummonEntity { get; set; }
    }

    public class SummonRemovalPrototype : Prototype
    {
        public ulong FromPowers { get; set; }
        public ulong Keywords { get; set; }
    }

    public class SummonEntityContextPrototype : Prototype
    {
        public ulong SummonEntity { get; set; }
        public Method PathFilterOverride { get; set; }
        public bool RandomSpawnLocation { get; set; }
        public bool IgnoreBlockingOnSpawn { get; set; }
        public bool SnapToFloor { get; set; }
        public bool TransferMissionPrototype { get; set; }
        public float SummonRadius { get; set; }
        public bool EnforceExactSummonPos { get; set; }
        public bool ForceBlockingCollisionForSpawn { get; set; }
        public bool VisibleWhileAttached { get; set; }
        public Vector3Prototype SummonOffsetVector { get; set; }
        public SummonRemovalPrototype SummonEntityRemoval { get; set; }
        public EvalPrototype[] EvalOnSummon { get; set; }
        public float SummonOffsetAngle { get; set; }
        public bool HideEntityOnSummon { get; set; }
        public bool CopyOwnerProperties { get; set; }
        public bool KillEntityOnOwnerDeath { get; set; }
        public PowerPrototype[] PowersToAssignToOwnerOnKilled { get; set; }
        public PowerPrototype[] PowersToUnassignFromOwnerOnEnter { get; set; }
        public EvalPrototype EvalCanSummon { get; set; }
        public ulong TrackInInventoryOwnerCondition { get; set; }
    }
}
