using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Games.Navi
{
    public class NaviPathGenerator
    {
        private NaviMesh _naviMesh;

        public NaviPathGenerator(NaviMesh naviMesh)
        {
            _naviMesh = naviMesh;
        }

        internal static void GenerateDirectMove(Vector3 position, Vector3 goalPosition, List<NaviPathNode> pathNodes)
        {
            throw new NotImplementedException();
        }

        internal NaviPathResult GeneratePath(Vector3 position, Vector3 goalPosition, float radius, PathFlags pathFlags, List<NaviPathNode> pathNodes, bool v, PathGenerationFlags pathGenerationFlags, float incompleteDistance)
        {
            throw new NotImplementedException();
        }
    }

    public enum PathGenerationFlags
    {
        Default = 0,
        IncompletedPath = 1 << 1,
    }
}
