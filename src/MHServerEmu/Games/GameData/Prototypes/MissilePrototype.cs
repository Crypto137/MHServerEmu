using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum MissilePowerActivationEventType    // Powers/Types/MissilePowerActivationEvent.type
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

    [AssetEnum]
    public enum MissileInitialDirectionType
    {
        Forward = 0,
        Backward = 1,
        Left = 2,
        Right = 3,
        Up = 4,
        OwnersForward = 5,
    }

    [AssetEnum]
    public enum MissileSpawnLocationType
    {
        CenteredOnOwner = 0,
        InFrontOfOwner = 1,
    }

    #endregion

    public class MissilePrototype : AgentPrototype
    {
        public ulong SendOrbToPowerUser { get; protected set; }
    }

    public class MissilePowerContextPrototype : Prototype
    {
        public ulong Power { get; protected set; }
        public MissilePowerActivationEventType MissilePowerActivationEvent { get; protected set; }
        public EvalPrototype EvalPctChanceToActivate { get; protected set; }
    }

    public class GravitatedMissileContextPrototype : Prototype
    {
        public float Gravity { get; protected set; }
        public int NumBounces { get; protected set; }
        public float OnBounceCoefficientOfRestitution { get; protected set; }
        public int OnBounceRandomDegreeFromForward { get; protected set; }
    }

    public class MissileCreationContextPrototype : Prototype
    {
        public bool IndependentClientMovement { get; protected set; }
        public bool IsReturningMissile { get; protected set; }
        public bool ReturningMissileExplodeOnCollide { get; protected set; }
        public bool OneShot { get; protected set; }
        public ulong Entity { get; protected set; }
        public Vector3Prototype CreationOffset { get; protected set; }
        public float SizeIncreasePerSec { get; protected set; }
        public bool IgnoresPitch { get; protected set; }
        public float Radius { get; protected set; }
        public MissileInitialDirectionType InitialDirection { get; protected set; }
        public Rotator3Prototype InitialDirectionAxisRotation { get; protected set; }
        public Rotator3Prototype InitialDirectionRandomVariance { get; protected set; }
        public MissileSpawnLocationType SpawnLocation { get; protected set; }
        public bool InterpolateRotationSpeed { get; protected set; }
        public float InterpolateRotMultByDist { get; protected set; }
        public float InterpolateOvershotAccel { get; protected set; }
        public bool NoCollide { get; protected set; }
        public MissilePowerContextPrototype[] PowerList { get; protected set; }
        public bool ReturnWeaponOnlyOnMiss { get; protected set; }
        public float RadiusEffectOverride { get; protected set; }
        public bool InfiniteLifespan { get; protected set; }
        public int LifespanOverrideMS { get; protected set; }
        public GravitatedMissileContextPrototype GravitatedContext { get; protected set; }
        public int RandomPickWeight { get; protected set; }
        public bool KilledOnOverlappingCollision { get; protected set; }
        public bool Ghost { get; protected set; }
        public bool CreationOffsetCheckLOS { get; protected set; }
    }

    public class MissilePowerPrototype : PowerPrototype
    {
        public MissileCreationContextPrototype[] MissileCreationContexts { get; protected set; }
        public bool MissileAllowCreationAfterPwrEnds { get; protected set; }
        public bool MissileUsesActualTargetPos { get; protected set; }
        public bool MissileSelectRandomContext { get; protected set; }
        public EvalPrototype EvalSelectMissileContextIndex { get; protected set; }
    }

    public class PublicEventTeamPrototype : Prototype
    {
        public ulong Name { get; protected set; }
    }
}
