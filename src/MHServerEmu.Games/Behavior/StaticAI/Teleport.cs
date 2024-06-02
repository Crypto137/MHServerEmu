using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class Teleport : IAIState
    {
        public static readonly Logger Logger = LogManager.CreateLogger();
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

                if (agent.ChangeRegionPosition(spawnPoint, agentOrientation) == false)
                {
                    Logger.Warn($"[{agent}] tried to leash teleport but was unsuccessful at position {spawnPoint} with region id {agent.Region.Id}.");
                    return failResult;
                }
            }
            else if (teleportContext.TeleportType == TeleportType.AssistedEntity)
            {
                WorldEntity assistedEntity = ownerController.AssistedEntity;
                if (assistedEntity == null)
                    return Logger.WarnReturn(failResult, $"[{agent}] We shouldn't be trying to teleport to the assisted entity if it doesn't exist");
                if (assistedEntity.IsInWorld == false)
                    return Logger.WarnReturn(failResult, $"[{agent}] We shouldn't be trying to teleport to the assisted entity [{assistedEntity}] when it's not in the world");

                Orientation assitedOrientation = assistedEntity.Orientation;
                Vector3 assistedPosition = assistedEntity.RegionLocation.Position;

                if (agent.CanPowerTeleportToPosition(assistedPosition) == false) return failResult;

                if (teleportContext.OwnerController.ActivePowerRef != PrototypeId.Invalid)
                    ownerController.AttemptActivatePower(teleportContext.PrototypeId, agent.Id, agent.RegionLocation.Position);

                if (agent.ChangeRegionPosition(assistedPosition, assitedOrientation, ChangePositionFlags.Teleport) == false)
                    return Logger.WarnReturn(failResult, $"[{agent}] tried to teleport to assisted entity position but was unsuccessful at position {assistedPosition} with region id {agent.Region.Id}.");
            }

            return StaticBehaviorReturnType.Completed;
        }

        public bool Validate(in IStateContext context) => true;
    }

    public struct TeleportContext : IStateContext
    {
        public AIController OwnerController { get; set; }
        public TeleportType TeleportType;
        public PrototypeId PrototypeId;

        public TeleportContext(AIController ownerController, TeleportContextPrototype proto)
        {
            OwnerController = ownerController;
            TeleportType = proto.TeleportType;
            PrototypeId = proto.DataRef;
        }
    }

}
