using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Behavior.ProceduralAI
{
    public class FastMoveToPath
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public static bool GetDestination(out Vector3 destination, Agent agent)
        {
            destination = Vector3.Zero;
            if (agent == null || agent.CanMove() == false || agent.IsInWorld == false) return false;

            Region region = agent.Region;
            AIController ownerController = agent.AIController;
            if (ownerController == null) return false;

            var collection = ownerController.Blackboard.PropertyCollection;
            PathCache pathCache = region.PathCache;

            int pathNodeIndex = collection[PropertyEnum.AIMoveToCurrentPathNodeIndex];
            bool reverse = collection[PropertyEnum.AIMoveToPathNodeReverse];
            int group = collection[PropertyEnum.AIMoveToPathNodeSetGroup];
            PathMethod method = (PathMethod)(int)collection[PropertyEnum.AIMoveToPathNodeSetMethod];
            float advanceThreshold = collection[PropertyEnum.AIMoveToPathNodeAdvanceThres];
            Vector3 agentPosition = agent.RegionLocation.Position;

            if (pathNodeIndex == -1)
            {
                bool foundStartNode = pathCache.UpdateCurrentPathNode(ref pathNodeIndex, ref destination, ref reverse, group,
                    method, agentPosition, advanceThreshold );

                if (foundStartNode == false)
                {
                    Logger.Warn($"Failed to find a path with group Id in region: {agent}");
                    return false;
                }
            }

            if (pathNodeIndex != -1)
            {
                collection[PropertyEnum.AIMoveToCurrentPathNodeIndex] = pathNodeIndex;
                collection[PropertyEnum.AIMoveToPathNodeReverse] = reverse;

                if (pathCache.IsLastNode(group, pathNodeIndex, method))
                {
                    float distanceSq = Vector3.DistanceSquared(agentPosition, destination);
                    return distanceSq >= MathHelper.Square(advanceThreshold);
                }
                return true;
            }
            return false;
        }

        public static bool Update(Agent agent)
        {
            Locomotor locomotor = agent.Locomotor;
            AIController ownerController = agent.AIController;
            if (ownerController == null)
                return false;

            var collection = ownerController.Blackboard.PropertyCollection;
            if (GetDestination(out Vector3 destination, agent))
            {
                LocomotionOptions locomotionOptions = new () 
                {
                    RepathDelay = TimeSpan.FromSeconds(1.0),
                    PathGenerationFlags = PathGenerationFlags.IncompletedPath
                };

                if (locomotor.SupportsWalking && collection[PropertyEnum.AIMoveToPathNodeWalk])
                    locomotionOptions.Flags = LocomotionFlags.IsWalking;

                return locomotor.PathTo(destination, locomotionOptions);
            }
            else
            {
                Region region = agent.Region;
                int group = collection[PropertyEnum.AIMoveToPathNodeSetGroup];
                PathMethod method = (PathMethod)(int)collection[PropertyEnum.AIMoveToPathNodeSetMethod];
                int node = collection[PropertyEnum.AIMoveToCurrentPathNodeIndex];
                if (region.PathCache.IsLastNode(group, node, method) == false && agent.CanMove()) return false;

                locomotor.Stop();
                return true;
            }
        }
    }

    public class FastMoveToTarget
    {
        public static void Update(Agent agent, float range)
        {
            Locomotor locomotor = agent.Locomotor;            
            if (GetDestination(out _, out ulong targetId, agent))
                locomotor.FollowEntity(targetId, range);
            else
                locomotor.Stop();
        }

        public static bool GetDestination(out Vector3 destination, out ulong targetId, Agent agent)
        {
            destination = Vector3.Zero;
            targetId = 0;
            if (agent == null || agent.CanMove() == false || agent.IsInWorld == false) return false;

            AIController ownerController = agent.AIController;
            targetId = ownerController.Blackboard.PropertyCollection[PropertyEnum.AIRawTargetEntityID];
            if (targetId == 0) return false;

            WorldEntity target = ownerController.Game.EntityManager.GetEntity<WorldEntity>(targetId);
            if (target == null || target.IsInWorld == false) return false;

            destination = target.RegionLocation.Position;
            return true;
        }
    }
}
