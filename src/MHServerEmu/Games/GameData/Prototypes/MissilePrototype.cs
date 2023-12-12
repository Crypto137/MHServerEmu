namespace MHServerEmu.Games.GameData.Prototypes
{
    public class MissilePrototype : AgentPrototype
    {
        public ulong SendOrbToPowerUser;
        public MissilePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissilePrototype), proto); }
    }

    public class MissilePowerContextPrototype : Prototype
    {
        public ulong Power;
        public PowerActivationEventType MissilePowerActivationEvent;
        public EvalPrototype EvalPctChanceToActivate;
        public MissilePowerContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissilePowerContextPrototype), proto); }
    }
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
    public class GravitatedMissileContextPrototype : Prototype
    {
        public float Gravity;
        public int NumBounces;
        public float OnBounceCoefficientOfRestitution;
        public int OnBounceRandomDegreeFromForward;
        public GravitatedMissileContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(GravitatedMissileContextPrototype), proto); }
    }

    public class MissileCreationContextPrototype : Prototype
    {
        public bool IndependentClientMovement;
        public bool IsReturningMissile;
        public bool ReturningMissileExplodeOnCollide;
        public bool OneShot;
        public ulong Entity;
        public Vector3Prototype CreationOffset;
        public float SizeIncreasePerSec;
        public bool IgnoresPitch;
        public float Radius;
        public InitialDirectionType InitialDirection;
        public Rotator3Prototype InitialDirectionAxisRotation;
        public Rotator3Prototype InitialDirectionRandomVariance;
        public SpawnLocationType SpawnLocation;
        public bool InterpolateRotationSpeed;
        public float InterpolateRotMultByDist;
        public float InterpolateOvershotAccel;
        public bool NoCollide;
        public MissilePowerContextPrototype[] PowerList;
        public bool ReturnWeaponOnlyOnMiss;
        public float RadiusEffectOverride;
        public bool InfiniteLifespan;
        public int LifespanOverrideMS;
        public GravitatedMissileContextPrototype GravitatedContext;
        public int RandomPickWeight;
        public bool KilledOnOverlappingCollision;
        public bool Ghost;
        public bool CreationOffsetCheckLOS;
        public MissileCreationContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissileCreationContextPrototype), proto); }
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
    public class MissilePowerPrototype : PowerPrototype
    {
        public MissileCreationContextPrototype[] MissileCreationContexts;
        public bool MissileAllowCreationAfterPwrEnds;
        public bool MissileUsesActualTargetPos;
        public bool MissileSelectRandomContext;
        public EvalPrototype EvalSelectMissileContextIndex;
        public MissilePowerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissilePowerPrototype), proto); }
    }

    public class PublicEventTeamPrototype : Prototype
    {
        public ulong Name;
        public PublicEventTeamPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PublicEventTeamPrototype), proto); }
    }
}
