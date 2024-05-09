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

        public void Start(IStateContext context)
        {
            throw new NotImplementedException();
        }

        public StaticBehaviorReturnType Update(IStateContext context)
        {
            throw new NotImplementedException();
        }

        public bool Validate(IStateContext context)
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

}
