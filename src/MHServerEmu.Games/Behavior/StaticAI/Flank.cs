using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class Flank : IAIState, ISingleton<Flank>
    {
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

    public struct FlankContext : IStateContext
    {
        public AIController OwnerController { get; set; }
        public float RangeMin;
        public float RangeMax;
        public float WaypointRadius;
        public float ToTargetFlankingAngle;
        public bool StopAtFlankingWaypoint;
        public int TimeoutMS;
        public bool FailOnTimeout;
        public bool RandomizeFlankingAngle;
        public FlankToType FlankTo;

        public FlankContext(AIController ownerController, FlankContextPrototype proto)
        {
            OwnerController = ownerController;
            RangeMin = proto.RangeMin;
            RangeMax = proto.RangeMax;
            WaypointRadius = proto.WaypointRadius;
            ToTargetFlankingAngle = proto.ToTargetFlankingAngle;
            StopAtFlankingWaypoint = proto.StopAtFlankingWaypoint;
            TimeoutMS = proto.TimeoutMS;
            FailOnTimeout = proto.FailOnTimeout;
            RandomizeFlankingAngle = proto.RandomizeFlankingAngle;
            FlankTo = proto.FlankTo;
        }
    }
}
