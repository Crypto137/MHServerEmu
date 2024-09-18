using MHServerEmu.Games.Navi;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class NaviFragmentPrototype : Prototype
    {
        public NaviFragmentPolyPrototype[] FragmentPolys { get; protected set; }
        public NaviFragmentPolyPrototype[] PropFragmentPolys { get; protected set; }
    }

    public class NaviFragmentPolyPrototype : Prototype
    {
        public NaviContentTags ContentTag { get; protected set; }
        public ulong Points { get; protected set; }
    }
}
