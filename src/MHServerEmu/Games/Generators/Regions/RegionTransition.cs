namespace MHServerEmu.Games.Generators.Regions
{
    public class RegionTransition
    {
        public RegionTransition() { }

        internal static void GetRequiredTransitionData(ulong RegionRef, ulong AreaRef, List<RegionTransitionSpec> transitions)
        {
            throw new NotImplementedException();
        }
    }

    public class RegionTransitionSpec
    {
        public ulong Cell;
        public ulong Entity;
        public bool Start;

        public RegionTransitionSpec() { }

        public RegionTransitionSpec(ulong cell, ulong entity, bool start)
        {
            Cell = cell;
            Entity = entity;
            Start = start;
        }

        internal ulong GetCellRef()
        {
            throw new NotImplementedException();
        }
    }
}
