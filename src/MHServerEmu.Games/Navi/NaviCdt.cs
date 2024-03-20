using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.Navi
{
    public class NaviCdt // Constrained Delaunay Triangulation
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public const float NaviSectorSize = 1024.0f;
        public int TriangleCount { get; private set; }
        public Aabb Bounds { get; private set; }
        private float _extentsSize;

        public InvasiveList<NaviTriangle> TriangleList { get; private set; }

        private NaviSystem _navi;
        private int _sectorSize;
        private NaviTriangle[] _sectors;
        private NaviVertexLookupCache _vertexLookupCache;
        private NaviTriangle _lastTriangle;
        private int _serial;
        private int _triangleCount;

        public NaviCdt(NaviSystem navi, NaviVertexLookupCache naviVertexLookupCache)
        {
            _navi = navi;
            TriangleCount = 0;
            Bounds = Aabb.Zero;
            TriangleList = new(1);
            _vertexLookupCache = naviVertexLookupCache;
            _sectors = Array.Empty<NaviTriangle>();
        }

        public void Create(Aabb bounds)
        {
            Release();
            Bounds = bounds;
            _extentsSize = Bounds.Extents.MaxElem();
            
            Vector3 boundsSize = Bounds.SizeVec;
            int sectorsX = (int)(boundsSize.X / NaviSectorSize) + 1;
            int sectorsY = (int)(boundsSize.Y / NaviSectorSize) + 1;

            _sectorSize = sectorsX;
            _sectors = new NaviTriangle[sectorsX * sectorsY];
            if (_navi.Log) Logger.Trace($"Navi Fast Triangle Lookup: {sectorsX}x{sectorsY} = {_sectors.Length} sectors");
        }

        public void Release()
        {
            foreach (var triangle in TriangleList.Iterate())
                RemoveTriangle(triangle);

            _lastTriangle = null;
            _serial = 0;
            _sectors = null;
            _sectorSize = 0;
        }

        internal void AddTriangle(NaviTriangle triangle)
        {
            _lastTriangle ??= triangle;

            TriangleList.AddBack(triangle);
            _triangleCount++;

            AddTriangleFastLookupRef(triangle);
        }

        private void AddTriangleFastLookupRef(NaviTriangle triangle)
        {
            int sectorIndex = PointToSectorIndex(triangle.Centroid());
            if (sectorIndex != -1) _sectors[sectorIndex] = triangle;
        }

        public void RemoveTriangle(NaviTriangle triangle)
        {
            RemoveTriangleFastLookupRef(triangle);
            triangle.Detach();

            TriangleList.Remove(triangle);
            _triangleCount--;

            if (_lastTriangle == triangle)
                _lastTriangle = TriangleList.Head();
        }

        private void RemoveTriangleFastLookupRef(NaviTriangle triangle)
        {
            int sectorIndex = PointToSectorIndex(triangle.Centroid());
            if (sectorIndex != -1 && triangle == _sectors[sectorIndex])
            {
                _sectors[sectorIndex] = null;
                for (int edgeIndex = 0; edgeIndex < 3; ++edgeIndex)
                {
                    NaviTriangle opposed = triangle.Edge(edgeIndex).OpposedTriangle(triangle);
                    if (opposed != null && PointToSectorIndex(opposed.Centroid()) == sectorIndex)
                    {
                        _sectors[sectorIndex] = opposed;
                        break;
                    }
                }
            }
        }

        private int PointToSectorIndex(Vector3 point)
        {
            Vector3 pos = point - Bounds.Min;
            int sectorIndex = (int)(pos.X / NaviSectorSize) + (int)(pos.Y / NaviSectorSize) * _sectorSize;
            if (sectorIndex >= 0 && sectorIndex < _sectors.Length)
                return sectorIndex;
            else
                return -1;
        }

        internal void RemoveCollinearEdges()
        {
            throw new NotImplementedException();
        }

        internal void AddEdge(NaviEdge e)
        {
            throw new NotImplementedException();
        }

        internal NaviPoint AddPointProjZ(Vector3 pos1)
        {
            throw new NotImplementedException();
        }

        internal NaviPoint AddPoint(Vector3 pos1)
        {
            throw new NotImplementedException();
        }

        internal void RemoveEdge(NaviEdge edge)
        {
            throw new NotImplementedException();
        }
    }
}