using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class Flock : IAIState
    {
        public static Flock Instance { get; } = new();
        private Flock() { }

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

    public struct FlockContext : IStateContext
    {
        public AIController OwnerController { get; set; }
        public float RangeMax;
        public float RangeMin;
        public float SeparationWeight;
        public float SeparationThreshold;
        public float AlignmentWeight;
        public float AlignmentThreshold;
        public float CohesionWeight;
        public float CohesionThreshold;
        public float MaxSteeringForce;
        public float ForceToLeaderWeight;
        public bool SwitchLeaderOnCompletion;
        public bool ChooseRandomPointAsDestination;
        public WanderBasePointType WanderFromPointType;
        public float WanderRadius;

        public FlockContext(AIController ownerController, FlockContextPrototype proto)
        {
            OwnerController = ownerController;
            RangeMax = proto.RangeMax;
            RangeMin = proto.RangeMin;
            SeparationWeight = proto.SeparationWeight;
            AlignmentWeight = proto.AlignmentWeight;
            CohesionWeight = proto.CohesionWeight;
            SeparationThreshold = proto.SeparationThreshold;
            AlignmentThreshold = proto.AlignmentThreshold;
            CohesionThreshold = proto.CohesionThreshold;
            MaxSteeringForce = proto.MaxSteeringForce;
            ForceToLeaderWeight = proto.ForceToLeaderWeight;
            SwitchLeaderOnCompletion = proto.SwitchLeaderOnCompletion;
            ChooseRandomPointAsDestination = proto.ChooseRandomPointAsDestination;
            WanderFromPointType = proto.WanderFromPointType;
            WanderRadius = proto.WanderRadius;
        }
    }

}
