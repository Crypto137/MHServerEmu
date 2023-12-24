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
        public ulong SendOrbToPowerUser { get; private set; }
    }

    public class MissilePowerContextPrototype : Prototype
    {
        public ulong Power { get; private set; }
        public MissilePowerActivationEventType MissilePowerActivationEvent { get; private set; }
        public EvalPrototype EvalPctChanceToActivate { get; private set; }
    }

    public class GravitatedMissileContextPrototype : Prototype
    {
        public float Gravity { get; private set; }
        public int NumBounces { get; private set; }
        public float OnBounceCoefficientOfRestitution { get; private set; }
        public int OnBounceRandomDegreeFromForward { get; private set; }
    }

    public class MissileCreationContextPrototype : Prototype
    {
        public bool IndependentClientMovement { get; private set; }
        public bool IsReturningMissile { get; private set; }
        public bool ReturningMissileExplodeOnCollide { get; private set; }
        public bool OneShot { get; private set; }
        public ulong Entity { get; private set; }
        public Vector3Prototype CreationOffset { get; private set; }
        public float SizeIncreasePerSec { get; private set; }
        public bool IgnoresPitch { get; private set; }
        public float Radius { get; private set; }
        public MissileInitialDirectionType InitialDirection { get; private set; }
        public Rotator3Prototype InitialDirectionAxisRotation { get; private set; }
        public Rotator3Prototype InitialDirectionRandomVariance { get; private set; }
        public MissileSpawnLocationType SpawnLocation { get; private set; }
        public bool InterpolateRotationSpeed { get; private set; }
        public float InterpolateRotMultByDist { get; private set; }
        public float InterpolateOvershotAccel { get; private set; }
        public bool NoCollide { get; private set; }
        public MissilePowerContextPrototype[] PowerList { get; private set; }
        public bool ReturnWeaponOnlyOnMiss { get; private set; }
        public float RadiusEffectOverride { get; private set; }
        public bool InfiniteLifespan { get; private set; }
        public int LifespanOverrideMS { get; private set; }
        public GravitatedMissileContextPrototype GravitatedContext { get; private set; }
        public int RandomPickWeight { get; private set; }
        public bool KilledOnOverlappingCollision { get; private set; }
        public bool Ghost { get; private set; }
        public bool CreationOffsetCheckLOS { get; private set; }
    }

    public class MissilePowerPrototype : PowerPrototype
    {
        public MissileCreationContextPrototype[] MissileCreationContexts { get; private set; }
        public bool MissileAllowCreationAfterPwrEnds { get; private set; }
        public bool MissileUsesActualTargetPos { get; private set; }
        public bool MissileSelectRandomContext { get; private set; }
        public EvalPrototype EvalSelectMissileContextIndex { get; private set; }
    }

    public class PublicEventTeamPrototype : Prototype
    {
        public ulong Name { get; private set; }
    }
}
