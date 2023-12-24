namespace MHServerEmu.Games.GameData.Prototypes
{
    public class NaviFragmentPrototype : Prototype
    {
        public NaviFragmentPolyPrototype[] FragmentPolys { get; private set; }
        public NaviFragmentPolyPrototype[] PropFragmentPolys { get; private set; }
    }

    public class NaviFragmentPolyPrototype : Prototype
    {
        public NaviContentTags ContentTag { get; private set; }
        public ulong Points { get; private set; }
    }
}
