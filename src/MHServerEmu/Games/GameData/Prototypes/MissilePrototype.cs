namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    public enum PowerActivationEventType
    {
        Invalid = 0,
        OnBounce = 1,
        OnCollide = 2,
        OnCollideWithWorld = 3,
        OnLifespanExpired = 4,
        OnReturned = 5,
        OnReturning = 6,
        OnOutOfWorld = 7,
    }

    public enum InitialDirectionType
    {
        Forward = 0,
        Backward = 1,
        Left = 2,
        Right = 3,
        Up = 4,
        OwnersForward = 5,
    }

    public enum SpawnLocationType
    {
        CenteredOnOwner = 0,
        InFrontOfOwner = 1,
    }

    #endregion

    public class MissilePrototype : AgentPrototype
    {
        public ulong SendOrbToPowerUser { get; set; }
    }

    public class MissilePowerContextPrototype : Prototype
    {
        public ulong Power { get; set; }
        public PowerActivationEventType MissilePowerActivationEvent { get; set; }
        public EvalPrototype EvalPctChanceToActivate { get; set; }
    }

    public class GravitatedMissileContextPrototype : Prototype
    {
        public float Gravity { get; set; }
        public int NumBounces { get; set; }
        public float OnBounceCoefficientOfRestitution { get; set; }
        public int OnBounceRandomDegreeFromForward { get; set; }
    }

    public class MissileCreationContextPrototype : Prototype
    {
        public bool IndependentClientMovement { get; set; }
        public bool IsReturningMissile { get; set; }
        public bool ReturningMissileExplodeOnCollide { get; set; }
        public bool OneShot { get; set; }
        public ulong Entity { get; set; }
        public Vector3Prototype CreationOffset { get; set; }
        public float SizeIncreasePerSec { get; set; }
        public bool IgnoresPitch { get; set; }
        public float Radius { get; set; }
        public InitialDirectionType InitialDirection { get; set; }
        public Rotator3Prototype InitialDirectionAxisRotation { get; set; }
        public Rotator3Prototype InitialDirectionRandomVariance { get; set; }
        public SpawnLocationType SpawnLocation { get; set; }
        public bool InterpolateRotationSpeed { get; set; }
        public float InterpolateRotMultByDist { get; set; }
        public float InterpolateOvershotAccel { get; set; }
        public bool NoCollide { get; set; }
        public MissilePowerContextPrototype[] PowerList { get; set; }
        public bool ReturnWeaponOnlyOnMiss { get; set; }
        public float RadiusEffectOverride { get; set; }
        public bool InfiniteLifespan { get; set; }
        public int LifespanOverrideMS { get; set; }
        public GravitatedMissileContextPrototype GravitatedContext { get; set; }
        public int RandomPickWeight { get; set; }
        public bool KilledOnOverlappingCollision { get; set; }
        public bool Ghost { get; set; }
        public bool CreationOffsetCheckLOS { get; set; }
    }

    public class MissilePowerPrototype : PowerPrototype
    {
        public MissileCreationContextPrototype[] MissileCreationContexts { get; set; }
        public bool MissileAllowCreationAfterPwrEnds { get; set; }
        public bool MissileUsesActualTargetPos { get; set; }
        public bool MissileSelectRandomContext { get; set; }
        public EvalPrototype EvalSelectMissileContextIndex { get; set; }
    }

    public class PublicEventTeamPrototype : Prototype
    {
        public ulong Name { get; set; }
    }
}
