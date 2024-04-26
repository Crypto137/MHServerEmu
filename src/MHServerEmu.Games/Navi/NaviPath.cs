

namespace MHServerEmu.Games.Navi
{
    public class NaviPath
    {
        private List<NaviPathNode> _pathNodes;
        public bool IsValid { get =>  _pathNodes.Count > 0 ; }
    }

    public class NaviPathNode
    {
    }
}
