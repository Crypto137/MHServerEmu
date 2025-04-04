using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Properties;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Games.Regions;
using MHServerEmu.Core.Memory;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class MoveTo : IAIState
    {
        public static readonly Logger Logger = LogManager.CreateLogger();
        public static MoveTo Instance { get; } = new();
        private MoveTo() { }

        public void End(AIController ownerController, StaticBehaviorReturnType state)
        {
            var blackboard = ownerController.Blackboard;
            var agent = ownerController.Owner;
            if (agent == null)
            {
                Logger.Debug("Unable to get Owner from AIController.");
                return;
            }

            if (blackboard.PropertyCollection[PropertyEnum.AIMoveToType] == (int)MoveToType.PathNode)
            {
                if (state == StaticBehaviorReturnType.Completed)
                {
                    var region = agent.Region;
                    if (region == null)
                    {
                        Logger.Debug($"Agent {agent} in invalid region!");
                        return;
                    }
                    
                    var pathCache = region.PathCache;
                    int currentPathNode = blackboard.PropertyCollection[PropertyEnum.AIMoveToCurrentPathNodeIndex];
                    Vector3 currentPathPosition = blackboard.MoveToCurrentPathNodePos;
                    bool reverse = blackboard.PropertyCollection[PropertyEnum.AIMoveToPathNodeReverse];
                    int group = blackboard.PropertyCollection[PropertyEnum.AIMoveToPathNodeSetGroup];
                    var pathMethod = (PathMethod)(int)blackboard.PropertyCollection[PropertyEnum.AIMoveToPathNodeSetMethod];
                    if (pathCache.UpdateCurrentPathNode(ref currentPathNode, ref currentPathPosition, ref reverse, group, pathMethod, agent.RegionLocation.Position, 256.0f) == false)
                        Logger.Debug($"Failed to find a path with group Id in region: {agent}");

                    blackboard.MoveToCurrentPathNodePos = currentPathPosition;
                    blackboard.PropertyCollection[PropertyEnum.AIMoveToCurrentPathNodeIndex] = currentPathNode;
                    blackboard.PropertyCollection[PropertyEnum.AIMoveToPathNodeReverse] = reverse;
                }
                else
                    blackboard.PropertyCollection[PropertyEnum.AIMoveToCurrentPathNodeIndex] = -1;
            }

            var locomotor = agent.Locomotor;
            if (locomotor == null)
            {
                Logger.Debug($"Agent {agent} has no locomotor to stop!");
                return;
            }

            locomotor.Stop();

            var cachedPath = blackboard.CachedPath;
            if (cachedPath == null) return;
            cachedPath.Clear();

            if (blackboard.PropertyCollection[PropertyEnum.NavigationInfluenceBeforeMoving] && agent.CanInfluenceNavigationMesh())
                agent.EnableNavigationInfluence();

            blackboard.PropertyCollection.RemoveProperty(PropertyEnum.NavigationInfluenceBeforeMoving);
        }

        public void Start(in IStateContext context)
        {
            if (context is not MoveToContext moveToContext) return;
            var ownerController = context.OwnerController;
            if (ownerController == null) return;
            var agent = ownerController.Owner;
            if (agent == null) return;
            var locomotor = agent.Locomotor;
            if (locomotor == null) return;
            var game = agent.Game;
            if (game == null) return;
            var blackboard = ownerController.Blackboard;

            var cachedPath = blackboard.CachedPath;
            if (cachedPath == null) return;

            locomotor.Stop();

            if (cachedPath.Path.IsValid == false) return;

            blackboard.PropertyCollection[PropertyEnum.AIMoveToType] = (int)moveToContext.MoveTo;
            if (moveToContext.MoveTo == MoveToType.PathNode)
            {
                blackboard.PropertyCollection[PropertyEnum.AIMoveToPathNodeSetGroup] = moveToContext.PathData.PathNodeSetGroup;
                blackboard.PropertyCollection[PropertyEnum.AIMoveToPathNodeSetMethod] = (int)moveToContext.PathData.PathNodeSetMethod;
            }

            if (locomotor.GetCurrentRotationSpeed() > 0f)
                locomotor.LookAt(cachedPath.Path.GetFinalPosition());

            bool isWalking = ownerController.GetDesiredIsWalkingState(moveToContext.MovementSpeed);
            LocomotionOptions locomotionOptions = new();
            if (isWalking)
                locomotionOptions.Flags |= LocomotionFlags.IsWalking;

            switch (moveToContext.MoveTo)
            {
                case MoveToType.SpawnPosition:
                case MoveToType.PathNode:
                case MoveToType.DespawnPosition:
                    locomotionOptions.RepathDelay = TimeSpan.FromMilliseconds(1000);
                    locomotionOptions.PathGenerationFlags = PathGenerationFlags.IncompletedPath | PathGenerationFlags.IgnoreSweep;
                    break;
                default:
                    locomotionOptions.RepathDelay = TimeSpan.FromMilliseconds(250);
                    break;
            }

            blackboard.PropertyCollection[PropertyEnum.NavigationInfluenceBeforeMoving] = agent.HasNavigationInfluence;
            agent.DisableNavigationInfluence();

            if (locomotor.FollowPath(cachedPath, locomotionOptions) == false) return;

            if (moveToContext.MoveTo == MoveToType.Target 
                || moveToContext.MoveTo == MoveToType.AssistedEntity
                || moveToContext.MoveTo == MoveToType.InteractEntity 
                || moveToContext.MoveTo == MoveToType.DespawnPosition)
            {
                ulong followId = blackboard.PropertyCollection[PropertyEnum.AIMoveToFollowEntityId];
                var followEntity = game.EntityManager.GetEntity<WorldEntity>(followId);
                if (followEntity != null)
                {
                    float rangeStart = blackboard.PropertyCollection[PropertyEnum.AIMoveToPathingRangeStart];
                    float rangeEnd = blackboard.PropertyCollection[PropertyEnum.AIMoveToPathingRangeEnd];
                    if (locomotor.FollowEntity(followEntity.Id, rangeStart, rangeEnd, locomotionOptions, false) == false)
                        return;
                }
            }

            if (locomotor.LastGeneratedPathResult == NaviPathResult.IncompletedPath)
            {
                blackboard.PropertyCollection[PropertyEnum.AILastMoveToIncompletePathETA] = (long)locomotor.GetCurrentETA().TotalMilliseconds;
                blackboard.PropertyCollection[PropertyEnum.AILastMoveToIncompletePathTime] = (long)game.CurrentTime.TotalMilliseconds;
            }

            agent.DrawPath(EntityHelper.TestOrb.Greeen);
        }

        public StaticBehaviorReturnType Update(in IStateContext context)
        {
            var failResult = StaticBehaviorReturnType.Failed;
            if (context is not MoveToContext moveToContext) return failResult;
            var ownerController = context.OwnerController;
            if (ownerController == null) return failResult;
            var agent = ownerController.Owner;
            if (agent == null) return failResult;
            var game = agent.Game;
            if (game == null) return failResult;
            var locomotor = agent.Locomotor;
            if (locomotor == null) return failResult;
            var blackboard = ownerController.Blackboard;

            var cachedPath = blackboard.CachedPath;
            if (cachedPath == null) return failResult;

            if (cachedPath.Path.IsValid == false)
                return StaticBehaviorReturnType.Completed;

            if (locomotor.IsLooking == false)
            {
                if (locomotor.IsStuck) return failResult;
                if (locomotor.IsPathComplete())
                {
                    if (moveToContext.EnforceLOS 
                        && (moveToContext.MoveTo == MoveToType.Target 
                        || moveToContext.MoveTo == MoveToType.AssistedEntity 
                        || moveToContext.MoveTo == MoveToType.InteractEntity))                    
                    {
                        ulong followId = blackboard.PropertyCollection[PropertyEnum.AIMoveToFollowEntityId];
                        var followEntity = game.EntityManager.GetEntity<WorldEntity>(followId);

                        if (IsValidFollowEntity(followEntity) == false) return failResult;
                        if (followEntity == null) return failResult;

                        float maxPowerRadius = blackboard.PropertyCollection[PropertyEnum.AILOSMaxPowerRadius];
                        if (agent.LineOfSightTo(followEntity, maxPowerRadius, moveToContext.LOSSweepPadding) == false)
                        {
                            float rangeEnd = blackboard.PropertyCollection[PropertyEnum.AIMoveToPathingRangeEnd];
                            float rangeStart = blackboard.PropertyCollection[PropertyEnum.AIMoveToPathingRangeStart];
                            if (rangeEnd <= 2.0f) return failResult;

                            float halfRangeStart = rangeStart * 0.5f;
                            float halfRangeEnd = rangeEnd * 0.5f;

                            blackboard.PropertyCollection[PropertyEnum.AIMoveToPathingRangeStart] = rangeStart * 0.25f;
                            blackboard.PropertyCollection[PropertyEnum.AIMoveToPathingRangeEnd] = halfRangeEnd;

                            float boundsRange = agent.Bounds.Radius + followEntity.Bounds.Radius;

                            bool isWalking = ownerController.GetDesiredIsWalkingState(moveToContext.MovementSpeed);
                            var locomotionOptions = new LocomotionOptions { RepathDelay = TimeSpan.FromMilliseconds(250) };
                            if (isWalking)
                                locomotionOptions.Flags |= LocomotionFlags.IsWalking;
                            locomotor.FollowEntity(followEntity.Id, boundsRange + halfRangeStart, boundsRange + halfRangeEnd, locomotionOptions, true);

                            return StaticBehaviorReturnType.Running;
                        }
                    }

                    return StaticBehaviorReturnType.Completed;
                }
                else
                {
                    var lastPathResult = locomotor.LastGeneratedPathResult;
                    if (lastPathResult == NaviPathResult.IncompletedPath)
                    {
                        long currentTime = (long)game.CurrentTime.TotalMilliseconds;
                        if (currentTime >= (blackboard.PropertyCollection[PropertyEnum.AILastMoveToIncompletePathTime] + 500))
                        {
                            blackboard.PropertyCollection[PropertyEnum.AILastMoveToIncompletePathTime] = currentTime;
                            long currentETA = (long)locomotor.GetCurrentETA().TotalMilliseconds;
                            long prevETA = blackboard.PropertyCollection[PropertyEnum.AILastMoveToIncompletePathETA];
                            if (currentETA >= prevETA) return failResult;
                            blackboard.PropertyCollection[PropertyEnum.AILastMoveToIncompletePathETA] = currentETA;
                        }
                    }
                    else if (lastPathResult != NaviPathResult.Success)
                        return failResult;
                }
            }

            return StaticBehaviorReturnType.Running;
        }

        private static bool IsValidFollowEntity(WorldEntity entity)
        {
            if (entity == null || entity.IsInWorld == false) return false;
            if (entity is Agent agent && agent.IsDead) return false;

            return true;
        }

        public bool Validate(in IStateContext context)
        {
            if (context is not MoveToContext moveToContext) return false;
            var ownerController = moveToContext.OwnerController;
            if (ownerController == null) return false;
            var agent = ownerController.Owner;
            if (agent == null) return false;
            var blackboard = ownerController.Blackboard;

            if (agent.CanMove() == false) return false;
            var locomotor = agent.Locomotor;
            if (locomotor == null) return false;

            var pathPosition = Vector3.Zero;
            var pathFlags = PathGenerationFlags.Default;
            float rangeStart = 0f;
            float rangeEnd = 0f;
            WorldEntity followEntity = null;

            switch (moveToContext.MoveTo)
            {
                case MoveToType.SpawnPosition:

                    pathFlags = PathGenerationFlags.IncompletedPath | PathGenerationFlags.IgnoreSweep;
                    pathPosition = blackboard.SpawnPoint;

                    break;

                case MoveToType.PathNode:

                    int currentPathNode = blackboard.PropertyCollection[PropertyEnum.AIMoveToCurrentPathNodeIndex];
                    if (currentPathNode == -1)
                    {
                        var region = agent.Region;
                        if (region == null) return false;

                        Vector3 currentPathPosition = blackboard.MoveToCurrentPathNodePos;
                        bool reverse = blackboard.PropertyCollection[PropertyEnum.AIMoveToPathNodeReverse];
                        var pathCache = region.PathCache;
                        if (pathCache.UpdateCurrentPathNode(ref currentPathNode, ref currentPathPosition, ref reverse, 
                            moveToContext.PathData.PathNodeSetGroup, moveToContext.PathData.PathNodeSetMethod, 
                            agent.RegionLocation.Position, 256.0f) == false)
                        {
                            Logger.Debug($"Failed to find a path with group Id in region: {agent}");
                            return false;
                        }
                        blackboard.MoveToCurrentPathNodePos = currentPathPosition;
                        blackboard.PropertyCollection[PropertyEnum.AIMoveToCurrentPathNodeIndex] = currentPathNode;
                        blackboard.PropertyCollection[PropertyEnum.AIMoveToPathNodeReverse] = reverse;
                    }

                    if (currentPathNode == -1) return false;

                    pathFlags = PathGenerationFlags.IncompletedPath;
                    pathPosition = blackboard.MoveToCurrentPathNodePos + blackboard.SpawnOffset;

                    break;

                case MoveToType.DespawnPosition:

                    if (ownerController.TargetEntity is Transition transition)
                    {
                        if (IsValidFollowEntity(transition) == false) return false;

                        pathPosition = transition.RegionLocation.Position;
                        followEntity = transition;
                        GetPathingRangeForFollowEntity(agent, followEntity, moveToContext, ref rangeStart, ref rangeEnd);
                        blackboard.PropertyCollection[PropertyEnum.AIMoveToFollowEntityId] = transition.Id;
                    }
                    else
                    {
                        if (FindDespawnPosition(agent, ref pathPosition) == false) return false;
                        pathFlags = PathGenerationFlags.IncompletedPath;
                    }

                    break;

                default:

                    switch (moveToContext.MoveTo)
                    {
                        case MoveToType.Target:
                            followEntity = ownerController.TargetEntity;
                            break;
                        case MoveToType.AssistedEntity:
                            followEntity = ownerController.AssistedEntity;
                            break;
                        case MoveToType.InteractEntity:
                            followEntity = ownerController.InteractEntity;
                            break;
                        default:
                            Logger.Debug($"Invalid moveto target on {agent}");
                            return false;
                    }

                    if (IsValidFollowEntity(followEntity) == false) return false;

                    pathPosition = followEntity.RegionLocation.Position;
                    GetPathingRangeForFollowEntity(agent, followEntity, moveToContext, ref rangeStart, ref rangeEnd);
                    blackboard.PropertyCollection[PropertyEnum.AIMoveToFollowEntityId] = followEntity.Id;

                    break;
            }

            blackboard.PropertyCollection[PropertyEnum.AIMoveToPathingRangeStart] = rangeStart;
            blackboard.PropertyCollection[PropertyEnum.AIMoveToPathingRangeEnd] = rangeEnd;
            
            float distanceSq = Vector3.DistanceSquared2D(agent.RegionLocation.Position, pathPosition);
            if (distanceSq < MathHelper.Square(rangeStart + Locomotor.ReachedPathPointEpsilon))
                if (moveToContext.EnforceLOS == false
                    || agent.LineOfSightTo(pathPosition, blackboard.PropertyCollection[PropertyEnum.AILOSMaxPowerRadius], moveToContext.LOSSweepPadding))
                    return true;

            var cachedPath = blackboard.CachedPath;
            if (cachedPath == null) return false;

            var pathResult = locomotor.GeneratePath(cachedPath, pathPosition, pathFlags, rangeEnd, followEntity);
            if (pathResult != NaviPathResult.Success && pathResult != NaviPathResult.IncompletedPath) 
                return false;

            return true;
        }

        private static bool FindDespawnPosition(Agent agent, ref Vector3 despawnPosition)
        {
            var region = agent.RegionLocation.Region;
            if (region == null) return false;
            var cell = agent.RegionLocation.Cell;
            if (cell == null) return false;
            var cellProto = cell.Prototype;
            if (cellProto == null) return false;

            var cellType = cellProto.Type;
            var wallsType = cellProto.Walls;
            var regionBounds = cell.RegionBounds;

            var checkFlags = PathFlags.Walk;
            var center = regionBounds.Center;
            Bounds checkBounds = new(agent.Bounds);

            List<Vector3> sideList = ListPool<Vector3>.Instance.Get();
            Vector3 position;

            if (cellType.HasFlag(Cell.Type.N) || !wallsType.HasFlag(Cell.Walls.N) || wallsType == Cell.Walls.All)
            {
                checkBounds.Center = new Vector3(regionBounds.Max.X + 256.0f, center.Y, center.Z);
                if (region.ChoosePositionAtOrNearPoint(checkBounds, checkFlags, PositionCheckFlags.None, BlockingCheckFlags.None, 512.0f, out position)
                    && agent.CheckCanPathTo(position, checkFlags) == NaviPathResult.Success)
                    sideList.Add(position);
            }

            if (cellType.HasFlag(Cell.Type.S) || !wallsType.HasFlag(Cell.Walls.S) || wallsType == Cell.Walls.All)
            {
                checkBounds.Center = new Vector3(regionBounds.Min.X - 256.0f, center.Y, center.Z);
                if (region.ChoosePositionAtOrNearPoint(checkBounds, checkFlags, PositionCheckFlags.None, BlockingCheckFlags.None, 512.0f, out position)
                    && agent.CheckCanPathTo(position, checkFlags) == NaviPathResult.Success)
                    sideList.Add(position);
            }

            if (cellType.HasFlag(Cell.Type.E) || !wallsType.HasFlag(Cell.Walls.E) || wallsType == Cell.Walls.All)
            {
                checkBounds.Center = new Vector3(center.X, regionBounds.Max.Y + 256.0f, center.Z);
                if (region.ChoosePositionAtOrNearPoint(checkBounds, checkFlags, PositionCheckFlags.None, BlockingCheckFlags.None, 512.0f, out position)
                    && agent.CheckCanPathTo(position, checkFlags) == NaviPathResult.Success)
                    sideList.Add(position);
            }

            if (cellType.HasFlag(Cell.Type.W) || !wallsType.HasFlag(Cell.Walls.W) || wallsType == Cell.Walls.All)
            {
                checkBounds.Center = new Vector3(center.X, regionBounds.Min.Y - 256.0f, center.Z);
                if (region.ChoosePositionAtOrNearPoint(checkBounds, checkFlags, PositionCheckFlags.None, BlockingCheckFlags.None, 512.0f, out position)
                    && agent.CheckCanPathTo(position, checkFlags) == NaviPathResult.Success)
                    sideList.Add(position);
            }

            var agentPosition = agent.RegionLocation.Position;
            float maxDistance = float.MinValue;

            foreach (var sidePoint in sideList)
            {
                float distanceSq = Vector3.DistanceSquared2D(agentPosition, sidePoint);
                if (distanceSq > maxDistance)
                {
                    maxDistance = distanceSq;
                    despawnPosition = sidePoint;
                }
            }

            ListPool<Vector3>.Instance.Return(sideList);

            return maxDistance != float.MinValue;
        }

        private static void GetPathingRangeForFollowEntity(Agent agent, WorldEntity followEntity, MoveToContext moveToContext, ref float rangeStart, ref float rangeEnd)
        {
            var game = agent.Game;
            if (game == null)
            {
                rangeStart = 0f;
                rangeEnd = 0f;
            }
            else
            {
                float range = 0f;
                if (agent.Bounds.CollisionType == BoundsCollisionType.Blocking)
                    range += agent.Bounds.Radius + followEntity.Bounds.Radius;

                rangeStart = moveToContext.RangeMax + range;
                rangeEnd = moveToContext.RangeMin + range;
            }
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
