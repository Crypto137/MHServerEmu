using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Navi
{
    public class NaviSweep
    {
        private readonly NaviMesh _naviMesh;
        private readonly Region _region;
        private readonly PathFlags _pathFlags;
        private readonly float _radius;
        private readonly float _radiusSq;
        private readonly Vector3 _startLocation;
        private readonly NaviTriangle _startTriangle;
        private readonly Vector3 _dest;
        private readonly Entity _owner;
        private readonly HeightSweepType _heightSweep;
        private readonly int _maxHeight;
        private readonly int _minHeight;

        public NaviSweep(NaviMesh naviMesh, Region region, PathFlags pathFlags, float radius, Vector3 startLocation, NaviTriangle startTriangle, Vector3 dest, Entity owner, HeightSweepType heightSweep, int maxHeight, int minHeight)
        {
            _naviMesh = naviMesh;
            _region = region;
            _pathFlags = pathFlags;
            _radius = radius;
            _radiusSq = radius * radius;
            _startLocation = startLocation;
            _startTriangle = startTriangle;
            _dest = dest;
            _owner = owner;
            _heightSweep = heightSweep;
            _maxHeight = maxHeight;
            _minHeight = minHeight;
        }

        public SweepResult DoSweep(ref Vector3? resultPosition, ref Vector3? resultNormal, float padding)
        {
            var result = PerformNaviMeshSweep(ref resultPosition, ref resultNormal, padding);
            if (result == SweepResult.Failed) return result;

            if (_heightSweep != HeightSweepType.None)
            {
                Vector3 heightPosition = _dest;
                if (result == SweepResult.Clipped) heightPosition = resultPosition.Value;

                var heightResult = PerformHeightSweep(_startLocation, heightPosition, ref resultPosition, ref resultNormal);
                if (heightResult != SweepResult.Success) return heightResult;
            }
            return result;
        }

        private SweepResult PerformHeightSweep(Vector3 startPosition, Vector3 endPosition, ref Vector3? resultPosition, ref Vector3? resultNormal)
        {
            if (_region == null || _radius < 0) return SweepResult.Failed;

            Segment line = new (startPosition, endPosition);

            Vector3 velocity = line.Direction.To2D();
            if (resultPosition != null && Vector3.IsNearZero(velocity))
            {
                resultPosition = line.Start;
                return SweepResult.Success;
            }

            if (_radius > 0)
                return PerformHeightMapCircleSweep(line, ref resultPosition, ref resultNormal);
            else
            {
                float resultDist = 0.0f;
                int height = (int)startPosition.Z;
                return PerformHeightMapLineSweep(line, ref height, ref resultPosition, ref resultNormal, ref resultDist);
            }
        }

        private SweepResult PerformHeightMapLineSweep(in Segment line, ref int height, ref Vector3? resultPosition, ref Vector3? resultNormal, ref float resultDist)
        {
            if (_region == null) return SweepResult.Failed;

            Aabb sweepBound = new (Vector3.MinPerElem(line.Start, line.End), Vector3.MaxPerElem(line.Start, line.End));
            Vector3 velocity = Vector3.Normalize2D(line.Direction);

            List<HitCellInfo> hitCells = new ();
            foreach (Cell cell in _region.IterateCellsInVolume(sweepBound))
            {
                HitCellInfo info = new(cell);
                if (cell.RegionBounds.IntersectRay(line.Start, velocity, ref info.Time, out info.Point))
                    hitCells.Add(info);
            }
            hitCells.Sort();

            Segment nextLine = line;
            for(int index = 0; index < hitCells.Count; index++)
            {  
                int nextIndex = index + 1;
                if (nextIndex < hitCells.Count)
                    nextLine.End = hitCells[nextIndex].Point;

                var info = hitCells[index];
                SweepResult result = PerformHeightMapLineSweepWithinCell(nextLine, info.Cell, ref height, ref resultPosition, ref resultNormal, ref resultDist);
                resultDist += info.Time;

                if (result != SweepResult.Success) return result;
                nextLine.Start = nextLine.End;
                nextLine.End = line.End;
            }

            return SweepResult.Success;
        }

        private SweepResult PerformHeightMapLineSweepWithinCell(in Segment line, Cell cell, ref int height, ref Vector3? resultPosition, ref Vector3? resultNormal, ref float resultDist)
        {
            if (cell == null) return SweepResult.Failed;

            var cellProto = cell.Prototype;
            if (cellProto == null || cellProto.HeightMap == null || cellProto.HeightMap.HeightMapData == null) return SweepResult.Failed;

            HeightMapPrototype heightMap = cellProto.HeightMap;
            float mapX = heightMap.HeightMapSize.X;
            float mapY = heightMap.HeightMapSize.Y;

            var regionBounds = cell.RegionBounds;
            Vector3 cellMin = regionBounds.Min;
            float cellWidth = regionBounds.Width;
            float cellLength = regionBounds.Length;

            Vector3 start = line.Start - cellMin;
            Vector3 end = line.End - cellMin;

            float x0 = Math.Clamp(start.X / cellWidth * mapX, 0.0f, mapX - 1);
            float y0 = Math.Clamp(start.Y / cellLength * mapY, 0.0f, mapY - 1);
            float x1 = Math.Clamp(end.X / cellWidth * mapX, 0.0f, mapX - 1);
            float y1 = Math.Clamp(end.Y / cellLength * mapY, 0.0f, mapY - 1);

            if (MathHelper.Round(x0) == MathHelper.Round(x1) 
                && MathHelper.Round(y0) == MathHelper.Round(y1))
                return CheckHeightMapAtPoint(MathHelper.RoundDownToInt(x0), MathHelper.RoundDownToInt(y0), 
                    heightMap, ref height, cell, line, ref resultPosition, ref resultNormal, ref resultDist);

            bool swap = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
            if (swap)
            {
                (x0, y0) = (y0, x0);
                (x1, y1) = (y1, x1);
            }

            float dx = x1 - x0;
            float dy = y1 - y0;
            float gradient = dy / dx;
            int sign = gradient != 0.0f ? (int)(Math.Abs(gradient) / gradient) : 0;

            float fx0 = MathF.Floor(x0);
            float fy0 = y0 + gradient * (fx0 - x0);
            int ix0 = (int)fx0;
            int iy0 = MathHelper.RoundDownToInt(fy0);

            float fx1 = MathF.Floor(x1);
            float fy1 = y1 + gradient * (fx1 - x1);
            int ix1 = (int)fx1;
            int iy1 = MathHelper.RoundDownToInt(fy1);

            SweepResult result;
            if ((result = CheckHeightMapPair(ix0, iy0, swap, sign, heightMap, 
                ref height, cell, line, ref resultPosition, ref resultNormal, ref resultDist)) != SweepResult.Success) return result;

            if (x0 < x1)
            {
                float fy = fy0 + gradient;
                for (int x = ix0 + 1; x <= ix1 - 1; x++)
                {
                    if ((result = CheckHeightMapPair(x, MathHelper.RoundDownToInt(fy), swap, sign, heightMap, 
                        ref height, cell, line, ref resultPosition, ref resultNormal, ref resultDist)) != SweepResult.Success) return result;
                    fy += gradient;
                }
            }
            else
            {
                float fy = fy0 - gradient;
                for (int x = ix0 - 1; x >= ix1 + 1; x--)
                {
                    if ((result = CheckHeightMapPair(x, MathHelper.RoundDownToInt(fy), swap, sign, heightMap, 
                        ref height, cell, line, ref resultPosition, ref resultNormal, ref resultDist)) != SweepResult.Success) return result;
                    fy -= gradient;
                }
            }

            if ((result = CheckHeightMapPair(ix1, iy1, swap, sign, heightMap, 
                ref height, cell, line, ref resultPosition, ref resultNormal, ref resultDist)) != SweepResult.Success) return result;

            return SweepResult.Success;
        }

        private SweepResult CheckHeightMapPair(int posX, int posY, bool swap, int sign, HeightMapPrototype heightMap, ref int height, Cell cell, in Segment line, ref Vector3? resultPosition, ref Vector3? resultNormal, ref float resultDist)
        {
            Span<int> x = stackalloc int[2];
            Span<int> y = stackalloc int[2];
            Span<SweepResult> pairResult = stackalloc SweepResult[2];
            Span<float> pairDistance = stackalloc float[2];
            Span<Vector3?> pairPosition = stackalloc Vector3?[2];
            Span<Vector3?> pairNormal = stackalloc Vector3?[2];

            x[0] = posX;
            y[0] = posY;

            x[1] = x[0];
            y[1] = y[0] + sign;

            if (swap)
            {
                (x[0], y[0]) = (y[0], x[0]);
                (x[1], y[1]) = (y[1], x[1]);
            }

            for (int i = 0; i < 2; i++)
            {
                pairPosition[i] = resultPosition != null ? new() : null;
                pairNormal[i] = resultNormal != null ? new() : null;
                pairResult[i] = CheckHeightMapAtPoint(x[i], y[i], heightMap, ref height, cell, line, ref pairPosition[i], ref pairNormal[i], ref pairDistance[i]);
            }
           
            if (pairResult[0] == SweepResult.Failed || pairResult[1] == SweepResult.Failed) return SweepResult.Failed;
            if (pairResult[0] == SweepResult.Success && pairResult[1] == SweepResult.Success) return SweepResult.Success;

            int index = 0;
            if (pairResult[0] == SweepResult.HeightMap && pairResult[1] == SweepResult.HeightMap)
            {
                if (pairDistance[0] > pairDistance[1]) index = 1;
            }
            else if (pairResult[0] == SweepResult.HeightMap) index = 0;
            else if (pairResult[1] == SweepResult.HeightMap) index = 1;

            resultDist = pairDistance[index];
            if (resultPosition != null) resultPosition = pairPosition[index];
            if (resultNormal != null) resultNormal = pairNormal[index];

            return SweepResult.HeightMap;
        }

        private SweepResult CheckHeightMapAtPoint(int x, int y, HeightMapPrototype heightMap, ref int height, Cell cell, in Segment ray, ref Vector3? resultPosition, ref Vector3? resultNormal, ref float resultDistance)
        {
            if (cell == null) return SweepResult.Failed;

            int mapX = (int)heightMap.HeightMapSize.X;
            int mapY = (int)heightMap.HeightMapSize.Y;

            if (x < 0 || x >= mapX || y < 0 || y >= mapY) return SweepResult.Success; // Original code here y >= mapX !!!

            var cellBounds = cell.RegionBounds;
            short posZ = (short)MathHelper.RoundToInt(cellBounds.Center.Z);
            short hitHeight = (short)(heightMap.HeightMapData[y * mapX + x] + posZ);

            if (hitHeight != short.MinValue)
            {
                if (hitHeight > _maxHeight || hitHeight < _minHeight)
                {
                    if (resultPosition != null || resultNormal != null)
                    {
                        Vector3 cellMin = cellBounds.Min;
                        float cellWidth = cellBounds.Width;
                        float cellLength = cellBounds.Length;

                        Vector3 rayDirection = Vector3.SafeNormalize2D(ray.Direction);
                        Vector3 cellPoint = new((float)x / mapX * cellWidth, (float)y / mapY * cellLength, 0.0f);
                        Vector3 cellX = new (cellWidth / mapX, 0.0f, 0.0f);
                        Vector3 cellY = new (0.0f, cellLength / mapY, 0.0f);

                        Vector3 pointX0Y0 = cellPoint + cellMin;
                        Vector3 pointX1Y0 = pointX0Y0 + cellX;
                        Vector3 pointX1Y1 = pointX1Y0 + cellY;
                        Vector3 pointX0Y1 = pointX0Y0 + cellY;

                        float minRayDistance = float.MaxValue;
                        bool hit = false;
                        Vector3 rayStart = ray.Start;

                        float rayScale = cellWidth / mapX;
                        rayStart -= rayDirection * rayScale;

                        if (SquareHitHelper(rayStart, rayDirection, pointX0Y0, pointX0Y1, ref minRayDistance)) // X0
                        {
                            if (resultNormal != null) resultNormal = Vector3.XAxisNeg;
                            hit = true;
                        }
                        if (SquareHitHelper(rayStart, rayDirection, pointX1Y0, pointX1Y1, ref minRayDistance)) // X1
                        {
                            if (resultNormal != null) resultNormal = Vector3.XAxis;
                            hit = true;
                        }
                        if (SquareHitHelper(rayStart, rayDirection, pointX0Y0, pointX1Y0, ref minRayDistance)) // Y0
                        {
                            if (resultNormal != null) resultNormal = Vector3.YAxisNeg;
                            hit = true;
                        }
                        if (SquareHitHelper(rayStart, rayDirection, pointX0Y1, pointX1Y1, ref minRayDistance)) // Y1
                        {
                            if (resultNormal != null) resultNormal = Vector3.YAxis;
                            hit = true;
                        }

                        if (hit)
                        {
                            if (resultPosition != null)
                            {
                                resultDistance = minRayDistance - rayScale;
                                resultPosition = rayStart + (rayDirection * minRayDistance);
                            }
                            return SweepResult.HeightMap;
                        }
                        else
                            return SweepResult.Success;
                    }
                    else
                        return SweepResult.HeightMap;
                }

                height = hitHeight;
            }

            return SweepResult.Success;
        }

        private static bool SquareHitHelper(Vector3 rayStart, Vector3 rayDirection, Vector3 point0, Vector3 point1, ref float minRayDistance)
        {
            if (Segment.RayLineIntersect2D(rayStart, rayDirection, point0, point1 - point0, out float rayDistance, out float lineDistance) &&
                rayDistance >= 0.0f && (lineDistance >= 0.0f && lineDistance <= 1.0f) && rayDistance < minRayDistance)
            {
                minRayDistance = rayDistance;
                return true;
            }
            return false;
        }

        private SweepResult PerformHeightMapCircleSweep(in Segment line, ref Vector3? resultPosition, ref Vector3? resultNormal)
        {
            if (_region == null) return SweepResult.Failed;

            Aabb sweepBound = new(Vector3.MinPerElem(line.Start, line.End), Vector3.MaxPerElem(line.Start, line.End));
            Cylinder2 cylinder = new (line.Start, float.MaxValue, _radius);
            Vector3 velocity = line.Direction.To2D();
            Vector3 direction = Vector3.SafeNormalize2D(velocity);

            List<HitCellInfo> hitCells = new();
            foreach (Cell cell in _region.IterateCellsInVolume(sweepBound))
            {
                HitCellInfo info = new (cell);
                var cellBound = info.Cell.RegionBounds;
                Vector3? resultSweepNorm = info.Point;
                if (cellBound.ContainsXY(line.Start) == ContainmentType.Contains
                    || cellBound.ContainsXY(line.End) == ContainmentType.Contains
                    || cylinder.Sweep(velocity, cellBound, ref info.Time, ref resultSweepNorm))
                {
                    info.Point = resultSweepNorm.Value;
                    hitCells.Add(info);
                }
            }

            HashSet<HitHeightMapSquareInfo> heightMapHits = new ();
            hitCells.Sort();

            foreach (HitCellInfo info in hitCells)
            {
                Cell cell = info.Cell;
                SweepResult cellResult = PerformHeightMapCircleSweepWithinCell(line, cell, heightMapHits);
                if (cellResult == SweepResult.Failed) return cellResult;
            }
            if (heightMapHits.Count == 0) return SweepResult.Success;

            SweepResult result = SweepResult.Success;
            float minTime = float.MaxValue;
            Vector3 minNormal = Vector3.ZAxis;
            float time = float.MaxValue;
            Vector3 normal = Vector3.ZAxis;
            Vector3 circleStart = line.Start.To2D();
            float magnitude = Vector3.Length2D(line.Direction);

            foreach (HitHeightMapSquareInfo hitInfo in heightMapHits)
            {
                if (hitInfo.DiagHit)
                {
                    Vector3 diagStart = hitInfo.DiagStart.To2D();
                    Vector3 diagEnd = hitInfo.DiagEnd.To2D();
                    float distanceToIntersect = 0f;
                    if (Sphere.SweepSegment2d(diagStart, diagEnd, circleStart, _radius, direction, magnitude, ref distanceToIntersect, SweepSegmentFlags.None))
                    {
                        time = distanceToIntersect / magnitude;
                        if (time < minTime)
                        {
                            minTime = time;
                            minNormal = Vector3.Normalize2D(new (velocity.X > 0.0f ? -1.0f : 1.0f, velocity.Y > 0.0f ? -1.0f : 1.0f, 0.0f));
                        }
                    }
                }
                else
                {
                    Vector3? resultSweepNormal = normal;
                    if (cylinder.Sweep(velocity, hitInfo.Bounds, ref time, ref resultSweepNormal) && time >= 0.0f)
                    {
                        normal = resultSweepNormal.Value;
                        if (Vector3.Dot2D(direction, normal) < 0)
                        {
                            result = SweepResult.HeightMap;
                            if (resultPosition == null && resultNormal == null) return result;
                            if (time < minTime)
                            {
                                minTime = time;
                                minNormal = normal;
                            }
                        }
                    }
                }
            }

            if (result == SweepResult.HeightMap)
            {
                if (resultNormal != null) resultNormal = minNormal;
                if (resultPosition != null) resultPosition = line.Start + (velocity * minTime);
            }

            return result;
        }

        private SweepResult PerformHeightMapCircleSweepWithinCell(in Segment line, Cell cell, HashSet<HitHeightMapSquareInfo> heightMapHits)
        {
            if (cell == null) return SweepResult.Failed;

            Vector3 lineEnd = line.End + Vector3.SafeNormalize2D(line.Direction) * _radius;

            Vector3 direction = (lineEnd - line.Start).To2D();
            float length = Vector3.Length2D(direction);
            direction = Vector3.SafeNormalize2D(direction);

            Vector3 perpDir = Vector3.Perp2D(direction);

            var cellProto = cell.Prototype;
            if (cellProto == null || cellProto.HeightMap.HeightMapData.IsNullOrEmpty()) return SweepResult.Failed;

            float mapX = cellProto.HeightMap.HeightMapSize.X;
            float mapY = cellProto.HeightMap.HeightMapSize.Y;

            var regionBounds = cell.RegionBounds;
            Vector3 cellMin = regionBounds.Min;
            float cellWidth = regionBounds.Width;
            float cellLength = regionBounds.Length;

            CircleHeightSweepPredicate predicate = new(cell, cellProto.HeightMap, heightMapHits)
            {
                MaxHeight = _maxHeight,
                MinHeight = _minHeight,
                Dir = direction,
                Radius = _radius,
                SignX = direction.X != 0 ? (int)(Math.Abs(direction.X) / direction.X) : 0,
                SignY = direction.Y != 0 ? (int)(Math.Abs(direction.Y) / direction.Y) : 0
            };

            float step;
            if (Segment.IsNearZero(direction.X))
                step = cellLength / mapY;
            else if (Segment.IsNearZero(direction.Y))
                step = cellWidth / mapX;
            else
            {
                float gradient = direction.Y / direction.X;
                if (direction.Y > direction.X)
                    step = MathHelper.SquareRoot((1.0f / MathHelper.Square(gradient)) + MathHelper.Square(cellLength / mapY));
                else
                    step = MathHelper.SquareRoot((1.0f / MathHelper.Square(1.0f / gradient)) + MathHelper.Square(cellWidth / mapX));
            }

            float distance = -step;
            while (distance < length)
            {
                distance += step;

                if (distance > length)
                    distance = length;

                Vector3 pos = line.Start + direction * distance;
                Segment line2d = new(
                    pos + perpDir * _radius,
                    pos - perpDir * _radius);

                line2d.Start -= cellMin;
                line2d.End -= cellMin;

                line2d.Start.X /= cellWidth;
                line2d.Start.Y /= cellLength;
                line2d.End.X /= cellWidth;
                line2d.End.Y /= cellLength;

                GridRenderLine2D(line2d, mapX, mapY, predicate);
            }

            return SweepResult.Success;
        }

        private static bool GridRenderLine2D(in Segment line, float mapX, float mapY, GridRenderPredicate predicate)
        {
            float x0 = Math.Clamp(line.Start.X * mapX, 0.0f, mapX - 1);
            float y0 = Math.Clamp(line.Start.Y * mapY, 0.0f, mapY - 1);
            float x1 = Math.Clamp(line.End.X * mapX, 0.0f, mapX - 1);
            float y1 = Math.Clamp(line.End.Y * mapY, 0.0f, mapY - 1);

            if (MathHelper.RoundDownToInt(x0) == MathHelper.RoundDownToInt(x1) &&
                MathHelper.RoundDownToInt(y0) == MathHelper.RoundDownToInt(y1))
            {
                predicate.Test(MathHelper.RoundDownToInt(x0), MathHelper.RoundDownToInt(y0));
                return true;
            }

            predicate.Swap = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
            if (predicate.Swap)
            {
                (x0, y0) = (y0, x0);
                (x1, y1) = (y1, x1);
            }

            float dx = x1 - x0;
            float dy = y1 - y0;
            float gradient = dy / dx;
            predicate.Sign = gradient != 0.0f ? (int)(Math.Abs(gradient) / gradient) : 0;

            float fx0 = MathF.Floor(x0);
            float fy0 = y0 + gradient * (fx0 - x0);
            int ix0 = (int)fx0;
            int iy0 = MathHelper.RoundDownToInt(fy0);

            float fx1 = MathF.Floor(x1);
            float fy1 = y1 + gradient * (fx1 - x1);
            int ix1 = (int)fx1;
            int iy1 = MathHelper.RoundDownToInt(fy1);

            predicate.PrimaryTest(ix0, iy0);
            if (x0 < x1)
            {
                float fy = fy0 + gradient;
                for (int x = ix0 + 1; x <= ix1 - 1; x++)
                {
                    predicate.PrimaryTest(x, MathHelper.RoundDownToInt(fy));
                    fy += gradient;
                }
            }
            else
            {
                float fy = fy0 - gradient;
                for (int x = ix0 - 1; x >= ix1 + 1; x--)
                {
                    predicate.PrimaryTest(x, MathHelper.RoundDownToInt(fy));
                    fy -= gradient;
                }
            }
            predicate.PrimaryTest(ix1, iy1);
            return true;
        }

        private SweepResult PerformNaviMeshSweep(ref Vector3? resultPosition, ref Vector3? resultNormal, float padding)
        {
            if (!Vector3.IsFinite(_startLocation) || !Vector3.IsFinite(_dest)) return SweepResult.Failed;
            if (_startTriangle == null || _startTriangle.Contains(_startLocation) == false) return SweepResult.Failed;
            if (_startTriangle.TestPathFlags(_pathFlags) == false) return SweepResult.Failed;

            var start2d = _startLocation.To2D();
            var dest2d = _dest.To2D();
            var velocity = dest2d - start2d;
            float magnitudeSq = Vector3.LengthSqr(velocity);

            if (magnitudeSq < Segment.Epsilon)
            {
                resultPosition = NaviUtil.ProjectToPlane(_startTriangle, _startLocation);
                return SweepResult.Success;
            }

            float magnitude = MathHelper.SquareRoot(magnitudeSq);
            Vector3 direction = velocity / magnitude;

            Stack<NaviTriangle> triStack = new();
            NaviSerialCheck naviSerialCheck = new (_naviMesh.NaviCdt);

            triStack.Push(GetFacingStartTriangle(start2d, direction));

            float distanceToIntersect = 0.0f;
            float minDistance = float.MaxValue;
            Span<bool> testIndex = stackalloc bool[3];

            while (triStack.Count > 0)
            {
                NaviTriangle triangle = triStack.Pop();
                testIndex.Clear();

                for (int i = 0; i < 3; i++)
                {
                    NaviEdge edge = triangle.Edges[i];
                    if (edge.TestOperationSerial(naviSerialCheck) == false) continue;

                    Vector3 edgePoint0 = edge.Point(0).To2D();
                    Vector3 edgePoint1 = edge.Point(1).To2D();

                    bool intersect = false;
                    NaviTriangle oppoTriangle = edge.OpposedTriangle(triangle);

                    if (oppoTriangle == null || oppoTriangle.TestPathFlags(_pathFlags) == false || NaviEdge.IsBlockingDoorEdge(edge, _pathFlags))
                    {
                        testIndex[i] = true;
                        testIndex[(i + 1) % 3] = true;

                        if (Sphere.SweepSegment2d(edgePoint1, edgePoint0, start2d, _radius, direction, magnitude, ref distanceToIntersect, SweepSegmentFlags.Ignore))
                        {
                            if (distanceToIntersect < minDistance)
                            {
                                minDistance = distanceToIntersect;
                                if (resultNormal != null)
                                {
                                    Vector3 segmentDir = edgePoint1 - edgePoint0;
                                    bool flip = Segment.Cross2D(segmentDir, start2d - edgePoint0) > 0.0f;
                                    if (flip) segmentDir = -segmentDir;

                                    resultNormal = Vector3.SafeNormalize2D(Vector3.Perp2D(segmentDir), Vector3.ZAxis);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (_radius > 0.0f)
                        {
                            float distanceSq2D = Segment.SegmentSegmentDistanceSq2D(edgePoint0, edgePoint1, start2d, dest2d);
                            if (distanceSq2D <= _radiusSq)
                                intersect = true;
                        }
                        else
                            intersect = Segment.SegmentsIntersect2D(edgePoint0, edgePoint1, start2d, dest2d);
                    }

                    if (intersect && oppoTriangle != null)
                        triStack.Push(oppoTriangle);
                }

                for (int i = 0; i < 3; i++)
                {
                    if (testIndex[i] == false) continue;
                    Vector3 point2D = triangle.PointCW(i).Pos.To2D();

                    if (Sphere.IntersectsRay(start2d, direction, point2D, _radius, out distanceToIntersect))
                        if (distanceToIntersect < minDistance)
                        {
                            minDistance = distanceToIntersect;
                            if (resultNormal != null)
                                resultNormal = Vector3.SafeNormalize2D((start2d + direction * distanceToIntersect) - point2D, Vector3.ZAxis);
                        }
                }
            }

            SweepResult result;
            if (minDistance <= magnitude)
            {
                minDistance = Math.Max(minDistance - padding, 0.0f);
                if (minDistance < Segment.Epsilon)
                    resultPosition = _startLocation;
                else
                {
                    var position = start2d + direction * minDistance;
                    position.Z = Segment.Lerp(_startLocation.Z, _dest.Z, minDistance / magnitude);
                    resultPosition = position;
                }
                result = SweepResult.Clipped;
            }
            else
            {
                resultPosition = _dest;
                result = SweepResult.Success;
            }

            return result;
        }

        public static short InternalGetHeightAtPoint(int x0, int y0, HeightMapPrototype heightMap, Cell cell)
        {
            int mapX = (int)heightMap.HeightMapSize.X;
            int mapY = (int)heightMap.HeightMapSize.Y;
            var regionBounds = cell.RegionBounds;

            if (x0 >= 0 && x0 < mapX && y0 >= 0 && y0 < mapY)
            {
                short posZ = (short)MathHelper.RoundToInt(regionBounds.Center.Z);
                return (short)(heightMap.HeightMapData[y0 * mapX + x0] + posZ);
            }
            
            Vector3 cellMin = regionBounds.Min;
            float cellWidth = regionBounds.Width;
            float cellLength = regionBounds.Length;

            Vector3 point = new (
                cellMin.X + (x0 + 0.5f) / mapX * cellWidth,
                cellMin.Y + (y0 + 0.5f) / mapY * cellLength,
                0.0f);

            Cell pointCell = cell.Region.GetCellAtPosition(point);
            if (pointCell != null)
            {
                Vector3 position = RegionLocation.ProjectToFloor(pointCell, point);
                return (short)position.Z;
            }
            else
                return 0;
        }

        private NaviTriangle GetFacingStartTriangle(Vector3 start2d, Vector3 direction2d)
        {
            NaviTriangle triangle = _startTriangle;
            NaviPoint point = null;

            for (int index = 0; index < 3; ++index)
            {
                NaviPoint checkPoint = _startTriangle.PointCW(index);
                Vector3 position2d = checkPoint.Pos.To2D();
                if (Vector3.IsNearZero(position2d - start2d, 0.1f))
                {
                    point = checkPoint;
                    break;
                }
            }

            if (point != null)
            {
                NaviTriangle nextTriangle = _startTriangle;
                do
                {
                    int edgeIndex = nextTriangle.OpposedEdgeIndex(point);
                    NaviEdge edge = nextTriangle.Edges[edgeIndex];

                    Vector3 edgePoint0 = edge.Point(0);
                    Vector3 edgeVector = edge.Point(1) - edgePoint0;

                    if (Segment.RaySegmentIntersect2D(start2d, direction2d, edgePoint0, edgeVector, out Vector3 _))
                    {
                        triangle = nextTriangle;
                        break;
                    }

                    NaviEdge nextEdge = nextTriangle.EdgeMod(edgeIndex + 1);
                    nextTriangle = nextEdge.OpposedTriangle(nextTriangle);
                }
                while (nextTriangle != _startTriangle);
            }

            return triangle;
        }
    }

    public class CircleHeightSweepPredicate : GridRenderPredicate
    {
        public Cell Cell;
        public HeightMapPrototype HeightMap;
        public HashSet<HitHeightMapSquareInfo> HeightMapHits;        
        public int MaxHeight;
        public int MinHeight;
        public float Radius;
        public Vector3 Dir;
        public int SignX;
        public int SignY;

        public CircleHeightSweepPredicate(Cell cell, HeightMapPrototype heightMap, HashSet<HitHeightMapSquareInfo> heightMapHits)
        {
            Cell = cell;
            HeightMap = heightMap;
            HeightMapHits = heightMapHits;
        }

        public override bool Test(int x, int y)
        {
            short height = NaviSweep.InternalGetHeightAtPoint(x, y, HeightMap, Cell);

            bool hit = false;
            bool diagHit = false;
            short diagStarHeight = 0;
            short diagEndHeight = 0;

            if (HeightTest(height))
                hit = true;
            else if (SignX != 0 && SignY != 0)
            {
                diagStarHeight = NaviSweep.InternalGetHeightAtPoint(x + SignX, y, HeightMap, Cell);
                diagEndHeight = NaviSweep.InternalGetHeightAtPoint(x, y + SignY, HeightMap, Cell);
                if (HeightTest(diagStarHeight) && HeightTest(diagEndHeight))
                    diagHit = true;
            }

            if (hit || diagHit)
            {
                HitHeightMapSquareInfo hitInfo = new();
                var regionBounds = Cell.RegionBounds;
                Vector3 cellMin = regionBounds.Min;
                float cellWidth = regionBounds.Width;
                float cellLength = regionBounds.Length;
                float mapX = HeightMap.HeightMapSize.X;
                float mapY = HeightMap.HeightMapSize.Y;

                Vector3 pointX0Y0 = new (x / mapX * cellWidth, y / mapY * cellLength, 0.0f);
                pointX0Y0 += cellMin;
                pointX0Y0.Z = height;
                Vector3 cellX = new (cellWidth / mapX, 0.0f, 0.0f);
                Vector3 cellY = new (0.0f, cellLength / mapY, 0.0f);
                Vector3 pointX1Y1 = pointX0Y0 + cellX + cellY;

                hitInfo.Bounds = new (pointX0Y0, pointX1Y1);
                hitInfo.DiagHit = diagHit;

                if (diagHit)
                {
                    float diagHeight = (diagStarHeight + diagEndHeight) / 2.0f;
                    if (SignX != SignY)
                    {
                        hitInfo.DiagStart = pointX0Y0;
                        hitInfo.DiagEnd = pointX1Y1;
                    }
                    else
                    {
                        hitInfo.DiagStart = pointX0Y0 + cellY;
                        hitInfo.DiagEnd = pointX0Y0 + cellX;
                    }
                    hitInfo.DiagStart.Z = diagHeight;
                    hitInfo.DiagEnd.Z = diagHeight;
                }

                hitInfo.X = x;
                hitInfo.Y = y;

                if (hit || diagHit) HeightMapHits.Add(hitInfo);
            }
            return hit || diagHit;
        }

        private bool HeightTest(short height)
        {
            if (height == short.MinValue) 
                return MinHeight > height;
            if (height > MaxHeight || height < MinHeight) 
                return true;
            return false;
        }

    }

    public class GridRenderPredicate
    {
        public bool Swap;
        public int Sign;

        public bool PrimaryTest(int x, int y)
        {
            int x0 = x;
            int y0 = y;
            int x1 = x0;
            int y1 = y0 + Sign;

            if (Swap)
            {
                (x0, y0) = (y0, x0);
                (x1, y1) = (y1, x1);
            }

            bool result1 = Test(x0, y0);
            bool result2 = Test(x1, y1);

            return result1 || result2;
        }

        public virtual bool Test(int x0, int y0) => false;       

    }

    public struct HitHeightMapSquareInfo : IComparable<HitHeightMapSquareInfo>
    {
        public int X;
        public int Y;
        public bool DiagHit;
        public Vector3 DiagStart;
        public Vector3 DiagEnd;
        public Aabb Bounds;

        public bool Equals(HitHeightMapSquareInfo other)
        {
            return X == other.X && Y == other.Y;
        }

        public int CompareTo(HitHeightMapSquareInfo other)
        {
            return Y == other.Y ? X.CompareTo(other.X) : Y.CompareTo(other.Y);
        }
    }

    public struct HitCellInfo : IComparable<HitCellInfo>
    {
        public Cell Cell;
        public float Time;
        public Vector3 Point;

        public HitCellInfo(Cell cell)
        {
            Cell = cell;
            Time = 0.0f;
            Point = Vector3.Zero;
        }

        public int CompareTo(HitCellInfo other)
        {
            return Time.CompareTo(other.Time);
        }
    }

    public enum HeightSweepType
    {
        None,
        Constraint
    }

    public enum SweepResult
    {
        Success = 0,
        Clipped = 1,
        HeightMap = 2,
        Failed = 3
    }

    public enum PointOnLineResult
    {
        Failed,
        Success,
        Clipped
    }
}
