using MHServerEmu.Core.Collisions;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.Navi
{
    public class NaviCdt // Constrained Delaunay Triangulation
    {
        public int TriangleCount { get; }
        public Aabb Bounds { get; }
        public InvasiveList<NaviTriangle> TriangleList { get; }

        private NaviSystem _naviSystem;
        private List<NaviTriangle> _sectors;
        private NaviVertexLookupCache _vertexLookupCache;

        public NaviCdt(NaviSystem naviSystem, NaviVertexLookupCache naviVertexLookupCache)
        {
            _naviSystem = naviSystem;
            TriangleCount = 0;
            Bounds = Aabb.Zero;
            TriangleList = new(1);
            _vertexLookupCache = naviVertexLookupCache;
            _sectors = new();
        }

        public void Create(Aabb bounds) 
        {

        }

        public void Release()
        {
            throw new NotImplementedException();
        }
    }
}