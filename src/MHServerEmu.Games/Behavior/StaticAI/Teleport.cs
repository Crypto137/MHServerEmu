using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class Teleport : IAIState
    {
        public static Teleport Instance { get; } = new();
        private Teleport() { }

        public void End(AIController ownerController, StaticBehaviorReturnType state) { }

        public void Start(in IStateContext context) { }

        public StaticBehaviorReturnType Update(in IStateContext context)
        {
            var failResult = StaticBehaviorReturnType.Failed;
            if (context is not TeleportContext teleportContext) return failResult;
            AIController ownerController = teleportContext.OwnerController;
            if (ownerController == null) return failResult;
            Agent agent = ownerController.Owner;
            if (agent == null) return failResult;
            Region region = agent.Region;
            if (region == null) return failResult;

            if (teleportContext.TeleportType == TeleportType.SpawnPosition)
            {
                BehaviorBlackboard blackboard = ownerController.Blackboard;
                Orientation agentOrientation = agent.Orientation;
                Vector3 spawnPoint = blackboard.SpawnPoint;

                ChangePositionResult crpResult = agent.ChangeRegionPosition(spawnPoint, agentOrientation);
                if (!Verify.IsTrue(crpResult == ChangePositionResult.PositionChanged, $"[{agent}] tried to leash teleport but was unsuccessful at position {spawnPoint} with region id {agent.Region.Id}. Failure code: {crpResult}"))
                    return failResult;
            }
            else if (teleportContext.TeleportType == TeleportType.AssistedEntity)
            {
                WorldEntity assistedEntity = ownerController.AssistedEntity;

                if (!Verify.IsNotNull(assistedEntity, $"[{agent}] We shouldn't be trying to teleport to the assisted entity if it doesn't exist"))
                    return failResult;

                if (!Verify.IsTrue(assistedEntity.IsInWorld, $"[{agent}] We shouldn't be trying to teleport to the assisted entity [{assistedEntity}] when it's not in the world"))
                    return failResult;

                Orientation assitedOrientation = assistedEntity.Orientation;
                Vector3 assistedPosition = assistedEntity.RegionLocation.Position;

                if (agent.CanPowerTeleportToPosition(assistedPosition) == false)
                    return failResult;

                ChangePositionResult crpResult = agent.ChangeRegionPosition(assistedPosition, assitedOrientation, ChangePositionFlags.Teleport);
                if (!Verify.IsTrue(crpResult == ChangePositionResult.PositionChanged, $"[{agent}] tried to teleport to assisted entity position but was unsuccessful at position {assistedPosition} with region id {agent.Region.Id}. Failure code: {crpResult}"))
                    return failResult;
            }

            return StaticBehaviorReturnType.Completed;
        }

        public bool Validate(in IStateContext context) => true;
    }

    public struct TeleportContext : IStateContext
    {
        public AIController OwnerController { get; set; }
        public TeleportType TeleportType;

        public TeleportContext(AIController ownerController, TeleportContextPrototype proto)
        {
            OwnerController = ownerController;
            TeleportType = proto.TeleportType;
        }
    }

}
