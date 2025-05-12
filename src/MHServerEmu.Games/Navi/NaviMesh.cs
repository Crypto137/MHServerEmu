using System.Text;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Navi
{
    public class NaviMesh
    {
        public struct NaviMeshConnection
        {
            public NaviMesh Mesh;
            public NaviEdge Edge;
        }

        public struct ModifyMeshPatch
        {
            public Transform3 Transform;
            public NaviPatchPrototype Patch;
        }

        private static readonly Logger Logger = LogManager.CreateLogger();
        private readonly NaviSystem _navi;

        public Aabb Bounds { get; private set; }
        public NaviVertexLookupCache NaviVertexLookupCache { get; private set; }
        public NaviCdt NaviCdt { get; private set; }
        public InvasiveList<NaviTriangle> TriangleList => NaviCdt.TriangleList;

        public bool IsMeshValid { get; private set; }
        public bool IsMarkupValid { get; private set; }
        private float _padding;
        private Region _region;
        private NaviPoint[] _points;
        private List<NaviEdge> _edges;
        public Dictionary<NaviEdge, NaviMeshConnection> MeshConnections { get; private set; }
        private NaviEdge _exteriorSeedEdge;
        private List<ModifyMeshPatch> _modifyMeshPatches;
        private List<ModifyMeshPatch> _modifyMeshPatchesProjZ;


        public NaviMesh(NaviSystem navi)
        {
            _navi = navi;
            Bounds = Aabb.Zero;
            NaviVertexLookupCache = new(navi);
            NaviCdt = new(navi, NaviVertexLookupCache);
            _edges = new();
            MeshConnections = new(); 
            _modifyMeshPatches = new();
            _modifyMeshPatchesProjZ = new();
        }

        public override string ToString()
        {
            return $"region={_region}";
        }

        public void Initialize(in Aabb bounds, float padding, Region region)
        {
            Release();
            Bounds = bounds;
            _padding = padding;
            _region = region;

            if (Bounds.IsValid() == false) return;

            var globals = GameDatabase.GlobalsPrototype;
            if (globals == null) return;

            float naviBudgetArea = globals.NaviBudgetBaseCellSizeWidth * globals.NaviBudgetBaseCellSizeLength;
            float naviBudgetPoints = globals.NaviBudgetBaseCellMaxPoints / naviBudgetArea;

            float naviMeshArea = Bounds.Width * Bounds.Length;
            var maxMeshPoints = (int)(naviBudgetPoints * naviMeshArea);

            NaviVertexLookupCache.Initialize(maxMeshPoints);
            NaviCdt.Create(Bounds.Expand(padding));
            AddSuperQuad(Bounds, padding);
        }

        private void AddSuperQuad(in Aabb bounds, float padding)
        {
            float xMin = bounds.Min.X - padding;
            float xMax = bounds.Max.X + padding;
            float yMin = bounds.Min.Y - padding;
            float yMax = bounds.Max.Y + padding;

            NaviPoint p0 = new(new (xMin, yMin, 0.0f));
            NaviPoint p1 = new(new (xMax, yMin, 0.0f));
            NaviPoint p2 = new(new (xMax, yMax, 0.0f));
            NaviPoint p3 = new(new (xMin, yMax, 0.0f));

            NaviEdge e0 = new(p0, p1, NaviEdgeFlags.Constraint);
            NaviEdge e1 = new(p1, p2, NaviEdgeFlags.Constraint);
            NaviEdge e2 = new(p2, p3, NaviEdgeFlags.Constraint);
            NaviEdge e3 = new(p3, p0, NaviEdgeFlags.Constraint);

            NaviEdge e02 = new(p0, p2, NaviEdgeFlags.None);

            NaviCdt.AddTriangle(new(e0, e1, e02));
            NaviCdt.AddTriangle(new(e2, e3, e02));

            _exteriorSeedEdge = e0;
        }

        public void Release()
        {
            IsMeshValid = false;
            _modifyMeshPatches.Clear();
            _modifyMeshPatchesProjZ.Clear();
            DestroyMeshConnections();
            _edges.Clear();
            _exteriorSeedEdge = null;
            ClearGenerationCache();
            NaviCdt.Release();
        }

        private void ClearGenerationCache()
        {
            NaviVertexLookupCache.Clear();
            _points = null;
        }

        private void DestroyMeshConnections()
        {
            foreach (var connection in MeshConnections)
            {
                NaviMeshConnection meshConnection = connection.Value;
                meshConnection.Mesh.MeshConnections.Remove(meshConnection.Edge);
            }
            MeshConnections.Clear();
        }

        public bool GenerateMesh()
        {
            IsMeshValid = false;

            if (_modifyMeshPatches.Count > 0)
            {
                foreach (var patch in _modifyMeshPatches)
                    if (ModifyMesh(patch.Transform, patch.Patch, false) == false) break;
                if (_navi.CheckErrorLog(false)) return false;
            }
            _modifyMeshPatches.Clear();

            if (_modifyMeshPatchesProjZ.Count > 0)
            {
                foreach (var patch in _modifyMeshPatchesProjZ)
                    if (ModifyMesh(patch.Transform, patch.Patch, true) == false) break;
                if (_navi.CheckErrorLog(false)) return false;
            }
            _modifyMeshPatchesProjZ.Clear();
            //NaviCdt.SaveObjMesh($"{_navi.Region.PrototypeName}[All].obj", PathFlags.None);
            MarkupMesh(false);
            if (_navi.CheckErrorLog(false)) return false;

            bool removeCollinearEdges = false; 
            if (removeCollinearEdges)
            {
                NaviCdt.RemoveCollinearEdges();
                if (_navi.CheckErrorLog(false)) return false;

                MarkupMesh(false);
                if (_navi.CheckErrorLog(false)) return false;
            }

            MergeMeshConnections();
            if (_navi.CheckErrorLog(false)) return false;

            //NaviCdt.SaveObjMesh($"{_navi.Region.PrototypeName}.obj");
            IsMeshValid = true;
            return true;
        }

        public void SaveHashPoints(string fileName)
        {
            StringBuilder hashes = new();
            int id = 0;
            foreach (var point in _points)
            {
                hashes.AppendLine($"[{id++}] {point.ToHashString()}");
            }
            File.WriteAllText(fileName, hashes.ToString());
        }

        private void MergeMeshConnections() {}

        public bool ModifyMesh(in Transform3 transform, NaviPatchPrototype patch, bool projZ)
        {
            if (patch.Points.IsNullOrEmpty()) return true;

            _points = new NaviPoint[patch.Points.Length];
            foreach (var edge in patch.Edges)
            {
                NaviPoint p0 = _points[edge.Index0];
                if (p0 == null)
                {
                    Vector3 Pos0 = new (transform * new Point3(patch.Points[edge.Index0]));
                    p0 = projZ ? NaviCdt.AddPointProjZ(Pos0) : NaviCdt.AddPoint(Pos0);
                    _points[edge.Index0] = p0;
                }
                NaviPoint p1 = _points[edge.Index1];
                if (p1 == null)
                {
                    Vector3 Pos1 = new (transform * new Point3(patch.Points[edge.Index1]));
                    p1 = projZ ? NaviCdt.AddPointProjZ(Pos1) : NaviCdt.AddPoint(Pos1);
                    _points[edge.Index1] = p1;
                }

                if (_navi.HasErrors() && _navi.CheckErrorLog(false, patch.ToString())) return false;
                if (p0 == p1) continue;

                NaviCdt.AddEdge(new(p0, p1, NaviEdgeFlags.Constraint, new(edge.Flags0, edge.Flags1)));
            }

            if (_navi.HasErrors() && _navi.CheckErrorLog(false, patch.ToString())) return false;

            return true;
        }

        private void MarkupMesh(bool removeExterior)
        {
            if (removeExterior && _exteriorSeedEdge == null)  return;
            ClearMarkup();

            Stack<MarkupState> stateStack = new ();
            Stack<NaviEdge> edgeStack = new ();
            NaviTriangle triangle = _exteriorSeedEdge.Triangles[0] ?? _exteriorSeedEdge.Triangles[1];

            MarkupState state = new()
            {
                Triangle = triangle,
                FlagCounts = new()
                {
                    AddFly = 1,
                    AddPower = 1,
                    AddSight = 1
                }
            };
            stateStack.Push(state);

            NaviContentFlags contentFlags = state.FlagCounts.ToContentFlags();
            PathFlags pathFlags = ContentFlags.ToPathFlags(contentFlags);

            triangle.ContentFlagCounts = state.FlagCounts;
            triangle.PathingFlags = pathFlags;
            triangle.SetFlag(NaviTriangleFlags.Markup);

            while (stateStack.Count > 0)
            {
                state = stateStack.Pop();
                triangle = state.Triangle;
                for (int edgeIndex = 0; edgeIndex < 3; edgeIndex++)
                {
                    var edge = triangle.Edges[edgeIndex];
                    NaviTriangle opposedTriangle = edge.OpposedTriangle(triangle);
                    if (opposedTriangle == null ) continue;

                    if (opposedTriangle.TestFlag(NaviTriangleFlags.Markup) == false)
                    {
                        MarkupState stateOppo = new(state)
                        {
                            Triangle = opposedTriangle
                        };

                        if (edge.TestFlag(NaviEdgeFlags.Constraint))
                        {
                            bool sideIndex = triangle.EdgeSideFlag(edgeIndex) == 1;

                            ContentFlagCounts side0 = edge.PathingFlags.ContentFlagCounts[sideIndex ? 0 : 1];
                            ContentFlagCounts side1 = edge.PathingFlags.ContentFlagCounts[sideIndex ? 1 : 0];

                            for (int flagIndex = 0; flagIndex < ContentFlagCounts.Count; flagIndex++)
                            {
                                stateOppo.FlagCounts[flagIndex] += side0[flagIndex];
                                stateOppo.FlagCounts[flagIndex] -= side1[flagIndex];
                            }
                        }

                        contentFlags = stateOppo.FlagCounts.ToContentFlags();
                        pathFlags = ContentFlags.ToPathFlags(contentFlags);

                        opposedTriangle.ContentFlagCounts = stateOppo.FlagCounts;
                        opposedTriangle.PathingFlags = pathFlags;
                        opposedTriangle.SetFlag(NaviTriangleFlags.Markup);
                        stateStack.Push(stateOppo);
                    }

                    if (edge.TestFlag(NaviEdgeFlags.Constraint) && edge.TestFlag(NaviEdgeFlags.Door) == false)
                    {
                        bool keepEdge = false;
                        ref var triFlags = ref triangle.ContentFlagCounts;
                        ref var oppFlags = ref opposedTriangle.ContentFlagCounts;
                        if ((triFlags.RemoveWalk == 0) && (oppFlags.RemoveWalk == 0))
                            keepEdge |= (triFlags.AddWalk > 0) ^ (oppFlags.AddWalk > 0);
                        else
                            keepEdge |= (triFlags.RemoveWalk > 0) ^ (oppFlags.RemoveWalk > 0);
                        if (keepEdge == false)
                        {
                            if ((triFlags.RemoveFly == 0) && (oppFlags.RemoveFly == 0))
                                keepEdge |= (triFlags.AddFly > 0) ^ (oppFlags.AddFly > 0);
                            else
                                keepEdge |= (triFlags.RemoveFly > 0) ^ (oppFlags.RemoveFly > 0);
                        }
                        if (keepEdge == false)
                        {
                            if ((triFlags.RemovePower == 0) && (oppFlags.RemovePower == 0))
                                keepEdge |= (triFlags.AddPower > 0) ^ (oppFlags.AddPower > 0);
                            else
                                keepEdge |= (triFlags.RemovePower > 0) ^ (oppFlags.RemovePower > 0);
                        }
                        if (keepEdge == false)
                        {
                            if ((triFlags.RemoveSight == 0) && (oppFlags.RemoveSight == 0))
                                keepEdge |= (triFlags.AddSight > 0) ^ (oppFlags.AddSight > 0);
                            else
                                keepEdge |= (triFlags.RemoveSight > 0) ^ (oppFlags.RemoveSight > 0);
                        }
                        if (keepEdge == false)
                            edgeStack.Push(edge);
                    }
                }
            }
            
            while (edgeStack.Count > 0)
            {
                var edge = edgeStack.Pop();
                if (edge.TestFlag(NaviEdgeFlags.Constraint))
                    NaviCdt.RemoveEdge(edge);
            }

            if (removeExterior) _exteriorSeedEdge = null;
            ReverseMarkupMesh();
            IsMarkupValid = true;
        }

        private void ReverseMarkupMesh()
        {
            foreach (var triangle in TriangleList.Iterate())
                for (int edgeIndex = 0; edgeIndex < 3; edgeIndex++)
                {
                    NaviEdge edge = triangle.Edges[edgeIndex];
                    if (edge.TestFlag(NaviEdgeFlags.Constraint) && edge.TestFlag(NaviEdgeFlags.Door) == false)
                    {
                        int side = triangle.EdgeSideFlag(edgeIndex);                                                
                        edge.PathingFlags.Clear(side);

                        ref var triFlags = ref triangle.ContentFlagCounts;
                        ref var edgeFlags = ref edge.PathingFlags.ContentFlagCounts[side];
                        if (triFlags.RemoveWalk > 0) edgeFlags.RemoveWalk = 1;
                        else if (triFlags.AddWalk > 0) edgeFlags.AddWalk = 1;
                        if (triFlags.RemoveFly > 0) edgeFlags.RemoveFly = 1;
                        if (triFlags.RemovePower > 0) edgeFlags.RemovePower = 1;
                        if (triFlags.RemoveSight > 0) edgeFlags.RemoveSight = 1;
                    }
                }
        }

        private void ClearMarkup()
        {
            foreach (var triangle in TriangleList.Iterate())
            {
                triangle.ClearFlag(NaviTriangleFlags.Markup);
                triangle.PathingFlags = PathFlags.None;
                triangle.ContentFlagCounts.Clear();
            }
            IsMarkupValid = false;
        }

        public bool Stitch(NaviPatchPrototype patch, in Transform3 transform)
        {
            if (patch.Points.HasValue())
            {
                ModifyMeshPatch modifyMeshPatch = new()
                {
                    Transform = transform,
                    Patch = patch
                };
                _modifyMeshPatches.Add(modifyMeshPatch);
            }
            return true;
        }

        public bool StitchProjZ(NaviPatchPrototype patch, in Transform3 transform)
        {
            if (patch.Points.HasValue())
            {
                ModifyMeshPatch modifyMeshPatch = new()
                {
                    Transform = transform,
                    Patch = patch
                };
                _modifyMeshPatchesProjZ.Add(modifyMeshPatch);
            }
            return true;
        }

        public bool Contains<T>(Vector3 position, float radius, T flagsCheck) where T: IContainsPathFlagsCheck
        {
            // Inverted here for make more sense, also removed bool outValue because always return false
            if (flagsCheck.CanBypassCheck() == false) return false; 

            NaviTriangle triangle = NaviCdt.FindTriangleAtPoint(position);
            if (triangle != null)
                return NaviUtil.NaviInteriorContainsCircle(NaviCdt, position, radius, triangle, flagsCheck);

            return false;
        }

        public Vector3 ProjectToMesh(Vector3 regionPos)
        {
            if (IsMeshValid == false) return Logger.WarnReturn(Vector3.Zero, $"ProjectToMesh(): Invalid mesh in region {_region}");

            NaviTriangle triangle = NaviCdt.FindTriangleAtPoint(regionPos);
            if (triangle == null) return Logger.WarnReturn(regionPos, $"ProjectToMesh(): Failed to find triangle at point {regionPos}");

            return NaviUtil.ProjectToPlane(triangle, regionPos);
        }

        public void SetBlackOutZone(Vector3 center, float radius)
        {
            var triangle = NaviCdt.FindTriangleAtPoint(center);
            if (triangle == null) return;
            triangle.PathingFlags |= PathFlags.BlackOutZone;
            Stack<NaviTriangle> triStack = new();
            var naviSerialCheck = new NaviSerialCheck(NaviCdt);
            float radiousSq = radius * radius;
            triStack.Push(triangle);
            while (triStack.Count > 0)
            {
                var tri = triStack.Pop();
                for (int edgeIndex = 0; edgeIndex < 3; edgeIndex++)
                {
                    var edge = tri.Edges[edgeIndex];
                    if (edge.TestOperationSerial(naviSerialCheck) == false) continue;
                    var triOppo = edge.OpposedTriangle(tri);
                    if (triOppo != null && Vector3.DistanceSquared2D(center, triOppo.Centroid()) < radiousSq) 
                    {
                        triOppo.PathingFlags |= PathFlags.BlackOutZone;
                        triStack.Push(triOppo);
                    }
                }
            }
        }

        public float CalcSpawnableArea(in Aabb bound)
        {
            float spawnableArea = 0.0f;
            var triangle = NaviCdt.FindTriangleAtPoint(bound.Center);
            if (triangle == null) return spawnableArea;
            spawnableArea += triangle.CalcSpawnableArea();
            Stack<NaviTriangle> triStack = new();
            var naviSerialCheck = new NaviSerialCheck(NaviCdt);
            triStack.Push(triangle);
            while (triStack.Count > 0)
            {
                var tri = triStack.Pop();
                for (int edgeIndex = 0; edgeIndex < 3; edgeIndex++)
                {
                    var edge = tri.Edges[edgeIndex];
                    if (edge.TestOperationSerial(naviSerialCheck) == false) continue;
                    var triOppo = edge.OpposedTriangle(tri);
                    if (triOppo!= null && bound.IntersectsXY(triOppo.Centroid()))
                    {
                        spawnableArea += tri.CalcSpawnableArea();
                        triStack.Push(triOppo);
                    }
                }
            }
            return spawnableArea;
        }

        public bool AddInfluence(Vector3 position, float radius, NavigationInfluence outInfluence)
        {
            NaviPoint point = NaviCdt.AddPointProjZ(position, false);
            if (point != null)
            {
                if (point.InfluenceRef == sbyte.MaxValue) return false;
                return AddInfluenceHelper(point, radius, outInfluence);
            }
            return true;
        }

        private bool AddInfluenceHelper(NaviPoint point, float radius, NavigationInfluence outInfluence)
        {
            if (outInfluence.Point != null) return false;            
            NaviTriangle foundTriangle = NaviCdt.FindTriangleContainingVertex(point);
            if (foundTriangle == null) return false;
            if (NaviUtil.IsPointConstraint(point, foundTriangle) == false)
            {
                if (point.InfluenceRef++ == 0) point.InfluenceRadius = radius;
                outInfluence.Point = point;
                outInfluence.Triangle = foundTriangle;
            }            
            return true;
        }

        public bool UpdateInfluence(NavigationInfluence inoutInfluence, Vector3 position, float radius)
        {
            if (inoutInfluence.Point == null || inoutInfluence.Point.TestFlag(NaviPointFlags.Attached) == false) return false;
            if (inoutInfluence.Point.InfluenceRef <= 0)
            {
                NaviSystem.Logger.Warn($"UpdateInfluence failed POINT={inoutInfluence.Point}");
                return false;
            }

            NaviPoint point = NaviCdt.FindCachedPointAtPoint(position);
            if (point != null)
            {
                if (point.TestFlag(NaviPointFlags.Attached))
                {
                    if (point != inoutInfluence.Point)
                    {
                        if (RemoveInfluence(inoutInfluence) == false || AddInfluenceHelper(point, radius, inoutInfluence) == false)
                            return false;
                    }
                    else
                        point.InfluenceRadius = radius;
                }
                else
                {
                    if (RemoveInfluence(inoutInfluence) == false || AddInfluence(position, radius, inoutInfluence) == false)
                        return false;
                }
            }
            else
            {
                NaviTriangle triangle = inoutInfluence.Triangle;
                if (triangle.TestFlag(NaviTriangleFlags.Attached) == false)
                    triangle = NaviCdt.FindTriangleContainingVertex(inoutInfluence.Point);

                if (triangle == null || !triangle.Contains(inoutInfluence.Point)) return false;
                inoutInfluence.Triangle = triangle;

                if (inoutInfluence.Point.InfluenceRef == 1 &&
                    NaviCdt.AttemptCheapVertexPositionUpdate(inoutInfluence.Triangle, inoutInfluence.Point, position))
                {
                    return true;
                }
                else
                {
                    if (RemoveInfluence(inoutInfluence) == false || AddInfluence(position, radius, inoutInfluence) == false)
                        return false;
                }
            }

            return true;
        }

        public bool RemoveInfluence(NavigationInfluence inoutInfluence)
        {
            if (inoutInfluence.Point != null)
            {
                NaviPoint influencePoint = inoutInfluence.Point;
                NaviTriangle influenceTriangle = inoutInfluence.Triangle;

                inoutInfluence.Point = null;
                inoutInfluence.Triangle = null;

                if (influencePoint.TestFlag(NaviPointFlags.Attached) == false) return false;
                if (influencePoint.InfluenceRef <= 0) 
                {
                    NaviSystem.Logger.Warn($"RemoveInfluence failed POINT={influencePoint}");
                    return false;
                }

                if (--influencePoint.InfluenceRef == 0)
                {
                    NaviTriangle triangle = influenceTriangle;
                    if (triangle.TestFlag(NaviTriangleFlags.Attached) == false)
                        triangle = NaviCdt.FindTriangleContainingVertex(influencePoint);
                    if (triangle == null) return false;

                    influencePoint.InfluenceRadius = 0f;

                    NaviCdt.RemovePoint(influencePoint, triangle);
                    NaviVertexLookupCache.RemoveVertex(influencePoint);
                }
            }

            return true;
        }

        public SweepResult Sweep(Vector3 fromPosition, Vector3 toPosition, float radius, PathFlags pathFlags, ref Vector3? resultPosition, ref Vector3? resultNormal,
            float padding = 0, HeightSweepType heightSweep = HeightSweepType.None, int maxHeight = short.MaxValue, int minHeight = short.MinValue, Entity owner = null)
        {
            NaviTriangle currentTriangle = NaviCdt.FindTriangleAtPoint(fromPosition);
            if (currentTriangle == null)
            {
                Logger.Warn($"Sweep(): Navi sweep failed to find starting triangle at point: {fromPosition} for mesh: [{this}] (owner=[{owner}])");
                resultPosition = Vector3.Zero;
                return SweepResult.Failed;
            }
            if (_region == null)
            {
                resultPosition = Vector3.Zero;
                return SweepResult.Failed;
            }
            NaviSweep naviSweep = new (this, _region, pathFlags, radius, fromPosition, currentTriangle, toPosition, owner, heightSweep, maxHeight, minHeight);
            SweepResult resultSweep = naviSweep.DoSweep(ref resultPosition, ref resultNormal, padding);
            return resultSweep;
        }

        public PointOnLineResult FindPointOnLineToOccupy(ref Vector3 resultPosition, Vector3 startPosition, Vector3 desiredPosition, float maxRange,
            Bounds bounds, PathFlags pathFlags, BlockingCheckFlags blockFlags, bool skipTarget)
        {
            resultPosition = startPosition;

            if (_region == null || !Vector3.IsFinite(startPosition) || !Vector3.IsFinite(desiredPosition))
                return PointOnLineResult.Failed;

            float radius = bounds.GetRadius();
            if (radius <= 0.0f)
            {
                Logger.Debug("This implementation of FindPointOnLineToOccupy requires a radius.");
                return PointOnLineResult.Failed;
            }

            maxRange += radius;
            Vector3 targetVector = desiredPosition - startPosition;
            Vector3.SafeNormalAndLength2D(targetVector, out Vector3 targetDirection, out float targetLength);
            float targetDistance = Math.Min(targetLength, maxRange);
            Vector3 targetPosition = startPosition + targetDirection * targetDistance;

            Bounds checkBounds = new (bounds);
            float stepSize = radius / 2.0f;
            for (int step = 0; step < 250; step++)
            {
                float stepDistance = step * stepSize;
                bool stepBack = targetDistance - stepDistance > -stepSize;
                bool stepForward = skipTarget == false && targetDistance + stepDistance < maxRange + stepSize;

                if (stepBack)
                {
                    float stepBackDistance = Math.Min(stepDistance, targetDistance);
                    checkBounds.Center = targetPosition - targetDirection * stepBackDistance;
                    if (_region.IsLocationClear(checkBounds, pathFlags, PositionCheckFlags.CanBeBlockedEntity, blockFlags))
                    {
                        resultPosition = checkBounds.Center;
                        return step == 0 ? PointOnLineResult.Success : PointOnLineResult.Clipped;
                    }
                }

                if (stepForward)
                {
                    float stepForwardDistance = Math.Min(stepDistance, maxRange);
                    checkBounds.Center = targetPosition + targetDirection * stepForwardDistance;
                    if (_region.IsLocationClear(checkBounds, pathFlags, PositionCheckFlags.CanBeBlockedEntity, blockFlags))
                    {
                        resultPosition = checkBounds.Center;
                        return step == 0 ? PointOnLineResult.Success : PointOnLineResult.Clipped;
                    }
                }

                if (stepBack == false && stepForward == false)
                    return PointOnLineResult.Failed;
            }

            Logger.Debug($"NaviMesh.FindPointOnLineToOccupy loop protection fired. maxRange={maxRange} radius={radius} startPosition={startPosition} desiredPosition={desiredPosition}");
            return PointOnLineResult.Failed;
        }

        private class MarkupState
        {
            public NaviTriangle Triangle { get; set; }
            public ContentFlagCounts FlagCounts;

            public MarkupState()
            {
            }

            public MarkupState(MarkupState state)
            {
                Triangle = state.Triangle;
                FlagCounts = state.FlagCounts;
            }
        }
    }
}
