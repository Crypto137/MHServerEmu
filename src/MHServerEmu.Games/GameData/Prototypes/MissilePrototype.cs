using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)Invalid)]
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

    [AssetEnum((int)Forward)]
    public enum MissileInitialDirectionType
    {
        Forward = 0,
        Backward = 1,
        Left = 2,
        Right = 3,
        Up = 4,
        OwnersForward = 5,
    }

    [AssetEnum((int)InFrontOfOwner)]
    public enum MissileSpawnLocationType
    {
        CenteredOnOwner = 0,
        InFrontOfOwner = 1,
    }

    #endregion

    public class MissilePrototype : AgentPrototype
    {
        public PrototypeId SendOrbToPowerUser { get; protected set; }

        public TimeSpan GetSeekDelayTime()
        {
            if (BehaviorProfile != null && BehaviorProfile.Brain != PrototypeId.Invalid)
            {
                var profile = GameDatabase.GetPrototype<ProceduralProfileSeekingMissilePrototype>(BehaviorProfile.Brain);
                if (profile != null) return TimeSpan.FromMilliseconds(profile.SeekDelayMS);
            }
            return TimeSpan.Zero;
        }

        public float GetSeekDelaySpeed()
        {
            if (BehaviorProfile != null && BehaviorProfile.Brain != PrototypeId.Invalid)
            {
                var profile = GameDatabase.GetPrototype<ProceduralProfileSeekingMissilePrototype>(BehaviorProfile.Brain);
                if (profile != null) return profile.SeekDelaySpeed;
            }
            return 0;
        }
    }

    public class MissilePowerContextPrototype : Prototype
    {
        public PrototypeId Power { get; protected set; }
        public MissilePowerActivationEventType MissilePowerActivationEvent { get; protected set; }
        public EvalPrototype EvalPctChanceToActivate { get; protected set; }

        public float GetPercentChanceToActivate(PropertyCollection properties)
        {
            float pctChanceToActivate = 1.0f;
            if (EvalPctChanceToActivate != null)
            {
                EvalContextData data = new();
                data.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, properties);
                pctChanceToActivate = Eval.RunFloat(EvalPctChanceToActivate, data);
            }
            return pctChanceToActivate;
        }
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
        public PrototypeId Entity { get; protected set; }
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

        [DoNotCopy]
        public float MaximumMissileBoundsSphereRadius { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();

            MaximumMissileBoundsSphereRadius = -1.0f;
            foreach (var missileContext in MissileCreationContexts)
            {
                if (missileContext == null) return;
                float radius = missileContext.Radius;
                if (GameDatabase.DataDirectory.PrototypeIsAbstract(missileContext.Entity) == false)
                {
                    var missileEntity = missileContext.Entity.As<MissilePrototype>();
                    if (missileEntity == null) return;
                    var boundsProto = missileEntity.Bounds;
                    if (boundsProto == null) return;
                    radius = boundsProto.GetSphereRadius();
                }
                MaximumMissileBoundsSphereRadius = Math.Max(radius, MaximumMissileBoundsSphereRadius);
            }
        }
    }

    public class PublicEventTeamPrototype : Prototype
    {
        public LocaleStringId Name { get; protected set; }
    }
}
