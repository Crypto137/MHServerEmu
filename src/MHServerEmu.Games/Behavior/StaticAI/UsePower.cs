using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class UsePower : IAIState
    {
        public static UsePower Instance { get; } = new();
        private UsePower() { }

        public void End(AIController ownerController, StaticBehaviorReturnType state)
        {
            throw new NotImplementedException();
        }

        public void Start(in IStateContext context)
        {
            throw new NotImplementedException();
        }

        public StaticBehaviorReturnType Update(in IStateContext context)
        {
            throw new NotImplementedException();
        }

        public bool Validate(in IStateContext context)
        {
            throw new NotImplementedException();
        }
    }

    public struct UsePowerContext : IStateContext
    {
        public AIController OwnerController { get; set; }
        public PrototypeId Power;
        public SelectEntityContextPrototype SecondaryTargetSelection;
        public bool RequireOriPriorToActivate;
        public bool ForceIgnoreLOS;
        public bool ForceCheckTargetRegionLocation;
        public bool ChooseRandomTargetPosition;
        public bool TargetsWorldPosition;
        public bool UseMainTargetForAOEActivation;
        public bool ForceInvalidTargetActivation;
        public bool AllowMovementClipping;
        public bool IgnoreOutOfPositionFailure;
        public float TargetAngleOffset;
        public float TargetOffset;
        public float OwnerOffset;
        public float OrientationThreshold;
        public float OffsetVarianceMagnitude;
        public float MinDistanceFromOwner;
        public float MaxDistanceToTarget;
        public float MinDistanceToTarget;
        public PrototypeId[] DifficultyTierRestrictions;

        public UsePowerContext(AIController ownerController, UsePowerContextPrototype proto)
        {
            OwnerController = ownerController;
            ChooseRandomTargetPosition = proto.ChooseRandomTargetPosition;
            ForceIgnoreLOS = proto.ForceIgnoreLOS;
            ForceCheckTargetRegionLocation = proto.ForceCheckTargetRegionLocation;
            OffsetVarianceMagnitude = proto.OffsetVarianceMagnitude;
            OwnerOffset = proto.OwnerOffset;
            Power = proto.Power;
            RequireOriPriorToActivate = proto.RequireOriPriorToActivate;
            OrientationThreshold = proto.OrientationThreshold;
            TargetAngleOffset = proto.TargetAngleOffset;
            TargetOffset = proto.TargetOffset;
            TargetsWorldPosition = proto.TargetsWorldPosition;
            SecondaryTargetSelection = proto.SecondaryTargetSelection;
            UseMainTargetForAOEActivation = proto.UseMainTargetForAOEActivation;
            MinDistanceFromOwner = proto.MinDistanceFromOwner;
            ForceInvalidTargetActivation = proto.ForceInvalidTargetActivation;
            AllowMovementClipping = proto.AllowMovementClipping;
            IgnoreOutOfPositionFailure = proto.IgnoreOutOfPositionFailure;
            MaxDistanceToTarget = proto.MaxDistanceToTarget;
            MinDistanceToTarget = proto.MinDistanceToTarget;
            DifficultyTierRestrictions = proto.DifficultyTierRestrictions;
        }
    }

    public enum PowerUseResult
    {
        Success = 0,
        Cooldown = 1,
        RestrictiveCondition = 2,
        BadTarget = 3,
        AbilityMissing = 4,
        TargetIsMissing = 5,
        InsufficientCharges = 6,
        InsufficientEndurance = 7,
        InsufficientSecondaryResource = 8,
        PowerInProgress = 9,
        OutOfPosition = 10,
        SummonSimultaneousLimit = 11,
        SummonLifetimeLimit = 12,
        WeaponMissing = 13,
        RegionRestricted = 14,
        NoFlyingUse = 15,
        ExtraActivationFailed = 16,
        GenericError = 17,
        OwnerNotSimulated = 18,
        OwnerDead = 19,
        ItemUseRestricted = 20,
        MinimumReactivateTime = 21,
        DisabledByLiveTuning = 22,
        NotAllowedByTransformMode = 23,
        FullscreenMovie = 24,
        ForceFailed = 25,
    }

}
