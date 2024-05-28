using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class MoveTo : IAIState
    {
        public static MoveTo Instance { get; } = new();
        private MoveTo() { }

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

    public struct PathData
    {
        public int PathNodeSetGroup;
        public PathMethod PathNodeSetMethod;
    }

    public struct MoveToContext : IStateContext
    {
        public AIController OwnerController { get; set; }
        public MoveToType MoveTo;
        public PathData PathData;
        public MovementSpeedOverride MovementSpeed;
        public bool EnforceLOS;
        public bool StopLocomotorOnMoveToFail;
        public float RangeMin;
        public float RangeMax;
        public float LOSSweepPadding;

        public MoveToContext(AIController ownerController, MoveToContextPrototype proto)
        {
            OwnerController = ownerController;
            MoveTo = proto.MoveTo;
            MovementSpeed = proto.MovementSpeed;
            EnforceLOS = proto.EnforceLOS;
            RangeMin = proto.RangeMin;
            RangeMax = proto.RangeMax;
            LOSSweepPadding = proto.LOSSweepPadding;
            PathData.PathNodeSetMethod = proto.PathNodeSetMethod;
            PathData.PathNodeSetGroup = proto.PathNodeSetGroup;
            StopLocomotorOnMoveToFail = proto.StopLocomotorOnMoveToFail;
        }
    }

}
