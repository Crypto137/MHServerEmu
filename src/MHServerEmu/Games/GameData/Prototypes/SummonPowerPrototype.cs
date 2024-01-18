namespace MHServerEmu.Games.GameData.Prototypes
{
    public class SummonPowerPrototype : PowerPrototype
    {
        public bool AttachSummonsToTarget;
        public bool SummonsLiveWhilePowerActive;
        public SummonEntityContextPrototype[] SummonEntityContexts;
        public EvalPrototype SummonMax;
        public bool SummonMaxReachedDestroyOwner;
        public int SummonIntervalMS;
        public bool SummonRandomSelection;
        public bool TrackInInventory;
        public bool AttachSummonsToCaster;
        public EvalPrototype SummonMaxSimultaneous;
        public ulong[] SummonMaxCountWithOthers;
        public bool PersistAcrossRegions;
        public EvalPrototype EvalSelectSummonContextIndex;
        public bool UseTargetAsSource;
        public bool KillPreviousSummons;
        public bool SummonAsPopulation;
        public SummonPowerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SummonPowerPrototype), proto); }
    }

    public class SummonPowerOverridePrototype : PowerUnrealOverridePrototype
    {
        public ulong SummonEntity;
        public SummonPowerOverridePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SummonPowerOverridePrototype), proto); }
    }

    public class SummonRemovalPrototype : Prototype
    {
        public ulong[] FromPowers;
        public ulong[] Keywords;
        public SummonRemovalPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SummonRemovalPrototype), proto); }
    }

    public class SummonEntityContextPrototype : Prototype
    {
        public ulong SummonEntity;
        public Method PathFilterOverride;
        public bool RandomSpawnLocation;
        public bool IgnoreBlockingOnSpawn;
        public bool SnapToFloor;
        public bool TransferMissionPrototype;
        public float SummonRadius;
        public bool EnforceExactSummonPos;
        public bool ForceBlockingCollisionForSpawn;
        public bool VisibleWhileAttached;
        public Vector3Prototype SummonOffsetVector;
        public SummonRemovalPrototype SummonEntityRemoval;
        public EvalPrototype[] EvalOnSummon;
        public float SummonOffsetAngle;
        public bool HideEntityOnSummon;
        public bool CopyOwnerProperties;
        public bool KillEntityOnOwnerDeath;
        public PowerPrototype[] PowersToAssignToOwnerOnKilled;
        public PowerPrototype[] PowersToUnassignFromOwnerOnEnter;
        public EvalPrototype EvalCanSummon;
        public ulong TrackInInventoryOwnerCondition;
        public SummonEntityContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SummonEntityContextPrototype), proto); }
    }


}
