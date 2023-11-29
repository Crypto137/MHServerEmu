namespace MHServerEmu.Games.GameData.Prototypes
{
    public class NaviFragmentPrototype : Prototype
    {
        public NaviFragmentPolyPrototype[] FragmentPolys;
        public NaviFragmentPolyPrototype[] PropFragmentPolys;
    }

    public class NaviFragmentPolyPrototype : Prototype
    {
        public NaviContentTags ContentTag;
        public ulong Points;
    }
}
