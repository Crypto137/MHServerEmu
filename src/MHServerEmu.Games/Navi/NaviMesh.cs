using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.VectorMath;
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

        private readonly NaviSystem _navi;

        public Aabb Bounds { get; private set; }
        public NaviVertexLookupCache NaviVertexLookupCache { get; private set; }
        public NaviCdt NaviCdt { get; private set; }

        private bool _isInit;
        private float _padding;
        private Region _region;
        private List<NaviPoint> _points;
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
            _exteriorSeedEdge = new(navi);
            _points = new();  
            _modifyMeshPatches = new();
            _modifyMeshPatchesProjZ = new();
        }

        public void Initialize(Aabb bounds, float padding, Region region)
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

        private void AddSuperQuad(Aabb bounds, float padding)
        {
            float xMin = bounds.Min.X - padding;
            float xMax = bounds.Max.X + padding;
            float yMin = bounds.Min.Y - padding;
            float yMax = bounds.Max.Y + padding;

            NaviPoint p0 = _navi.CreatePoint(new (xMin, yMin, 0.0f));
            NaviPoint p1 = _navi.CreatePoint(new (xMax, yMin, 0.0f));
            NaviPoint p2 = _navi.CreatePoint(new (xMax, yMax, 0.0f));
            NaviPoint p3 = _navi.CreatePoint(new (xMin, yMax, 0.0f));

            NaviEdge e0 = _navi.CreateEdge(p0, p1, NaviEdgeFlags.Flag0, new());
            NaviEdge e1 = _navi.CreateEdge(p1, p2, NaviEdgeFlags.Flag0, new());
            NaviEdge e2 = _navi.CreateEdge(p2, p3, NaviEdgeFlags.Flag0, new());
            NaviEdge e3 = _navi.CreateEdge(p3, p0, NaviEdgeFlags.Flag0, new());

            NaviEdge e02 = _navi.CreateEdge(p0, p2, NaviEdgeFlags.None, new());

            NaviCdt.AddTriangle(_navi.CreateTriangle(e0, e1, e02));
            NaviCdt.AddTriangle(_navi.CreateTriangle(e2, e3, e02));

            _exteriorSeedEdge = e0;
        }

        public void Release()
        {
            _isInit = false;
            _modifyMeshPatches.Clear();
            _modifyMeshPatchesProjZ.Clear();
            DestroyMeshConnections();
            _edges.Clear();
            _exteriorSeedEdge.SafeRelease();
            _exteriorSeedEdge = null;
            ClearGenerationCache();
            NaviCdt.Release();
        }

        private void ClearGenerationCache()
        {
            NaviVertexLookupCache.Clear();
            _points.Clear();
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
    }
}
