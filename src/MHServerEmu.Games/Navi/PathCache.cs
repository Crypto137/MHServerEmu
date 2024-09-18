using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Navi
{
    public class PathCache
    {

        private readonly Dictionary<int, List<PathNodeSetPrototype>> _pathMap;
        private readonly Dictionary<int, List<PathNodeStruct>> _pathNodeMap;

        public PathCache() { _pathMap = new(); _pathNodeMap = new(); }

        public void AppendPathCollection(PathCollectionPrototype PathNodeCollection, Vector3 offset)
        {
            if (PathNodeCollection.PathNodeSets.HasValue())
            {
                foreach (var pathNodeSet in PathNodeCollection.PathNodeSets)
                {
                    if (pathNodeSet == null) continue;

                    int group = pathNodeSet.Group;

                    if (_pathMap.TryGetValue(group, out var pathSetList) == false)
                        _pathMap[group] = pathSetList = new();

                    if (_pathNodeMap.TryGetValue(group, out var pathNodeList) == false)
                        _pathNodeMap[group] = pathNodeList = new();

                    if (pathSetList != null)
                    {
                        if (!pathSetList.Contains(pathNodeSet))
                        {
                            pathSetList.Add(pathNodeSet);
                            int id = 0;

                            foreach (var pathNodeProto in pathNodeSet.PathNodes)
                            {
                                if (pathNodeProto == null) continue;

                                PathNodeStruct pathNode = new()
                                {
                                    Id = id,
                                    Position = pathNodeProto.Position + offset
                                };
                                id++;

                                pathNodeList.Add(pathNode);
                            }
                        }
                    }
                }
            }
        }
        public bool IsLastNode(int group, int pathNode, PathMethod method)
        {

            if (method == PathMethod.ForwardLoop || method == PathMethod.ForwardBackAndForth ||
                method == PathMethod.ReverseLoop || method == PathMethod.ReverseBackAndForth) return false;

            if (_pathNodeMap.TryGetValue(group, out var pathNodeList))
            {
                if (method == PathMethod.Forward && pathNode == pathNodeList.Count - 1) return true;
                if (method == PathMethod.Reverse && pathNode == 0) return true;
            }

            return false;
        }

        public bool UpdateCurrentPathNode(ref int pathNode, ref Vector3 pathNodePosition, ref bool reverse, int group, PathMethod method, Vector3 position, float distanceThreshold)
        {
            if (_pathNodeMap.TryGetValue(group, out var pathNodeList) && pathNodeList != null && pathNodeList.Any())
            {
                int lastNode = pathNodeList.Count - 1;
                if (method == PathMethod.ForwardLoop || method == PathMethod.ReverseLoop)
                    lastNode = pathNodeList.Count >= 2 ? pathNodeList.Count : 0;

                float closestDistance = float.MaxValue;
                int closestNodeA = -1;
                int closestNodeB = -1;

                for (int i = 0; i < lastNode; ++i)
                {
                    int nodeA = i;
                    int nodeB = (i + 1) % pathNodeList.Count;
                    float distanceSq = Segment.SegmentPointDistanceSq(pathNodeList[nodeA].Position, pathNodeList[nodeB].Position, position);
                    if (distanceSq < closestDistance)
                    {
                        closestNodeA = nodeA;
                        closestNodeB = nodeB;
                        closestDistance = distanceSq;
                    }
                }

                if (closestDistance < float.MaxValue)
                {
                    int direction = 0;
                    switch (method)
                    {
                        case PathMethod.Forward:
                        case PathMethod.ForwardLoop:
                            direction = 1;
                            break;
                        case PathMethod.Reverse:
                        case PathMethod.ReverseLoop:
                            direction = -1;
                            break;
                        case PathMethod.ForwardBackAndForth:
                            direction = reverse ? -1 : 1;
                            break;
                        case PathMethod.ReverseBackAndForth:
                            direction = reverse ? 1 : -1;
                            break;
                        default:
                            break;
                    }

                    int closestNode = -1;
                    switch (direction)
                    {
                        case 1:
                            closestNode = closestNodeB;
                            break;
                        case -1:
                            closestNode = closestNodeA;
                            break;
                    }

                    if (closestNode != -1)
                    {
                        pathNodePosition = pathNodeList[closestNode].Position;

                        if (Vector3.DistanceSquared2D(position, pathNodePosition) < Math.Pow(distanceThreshold, 2))
                        {
                            if (method == PathMethod.ForwardBackAndForth || method == PathMethod.ReverseBackAndForth)
                            {
                                if (closestNode == 0 || closestNode == pathNodeList.Count - 1)
                                {
                                    direction = -direction;
                                    reverse = !reverse;
                                }
                            }

                            closestNode = (closestNode + direction + pathNodeList.Count) % pathNodeList.Count;
                            pathNodePosition = pathNodeList[closestNode].Position;
                        }

                        pathNode = closestNode;
                        return true;
                    }
                }
            }

            return false;
        }


        struct PathNodeStruct
        {
            public Vector3 Position;
            public int Id;
        }

    }

}
