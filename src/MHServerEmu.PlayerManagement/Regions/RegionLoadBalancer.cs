using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.PlayerManagement.Regions
{
    public class RegionLoadBalancer
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly SortedSet<RegionHandle> _regions = new(RegionLoadComparer.Instance);
        private TimeSpan _nextCleanupTime = Clock.UnixTime;

        public PrototypeId RegionProtoRef { get; }

        public RegionLoadBalancer(PrototypeId regionProtoRef)
        {
            RegionProtoRef = regionProtoRef;
        }

        public RegionHandle GetAvailableRegion(PrototypeId difficultyProtoRef)
        {
            TryCleanUpRegions();

            foreach (RegionHandle region in _regions)
            {
                // We have full regions sorted to the back of the set, so if we reach a full region it means there isn't an available one for sure.
                if (region.IsFull)
                    break;

                if (region.State == RegionHandleState.Shutdown)
                    continue;

                if (region.DifficultyTierProtoRef != difficultyProtoRef)
                    continue;

                return region;
            }

            return null;
        }

        public bool AddRegion(RegionHandle region)
        {
            if (region == null) return Logger.WarnReturn(false, "AddRegion(): region == null");

            if (region.State == RegionHandleState.Shutdown)
                return Logger.WarnReturn(false, $"AddRegion(): Attempting to add region {region} that has already been shut down");

            return _regions.Add(region);
        }

        public bool RemoveRegion(RegionHandle region)
        {
            if (region == null) return Logger.WarnReturn(false, "RemoveRegion(): region == null");
            return _regions.Remove(region);
        }

        private void TryCleanUpRegions()
        {
            TimeSpan now = Clock.UnixTime;
            if (now < _nextCleanupTime)
                return;

            // TODO

            _nextCleanupTime = now + TimeSpan.FromMinutes(5);
        }

        private class RegionLoadComparer : IComparer<RegionHandle>
        {
            public static RegionLoadComparer Instance { get; } = new();

            private RegionLoadComparer() { }

            public int Compare(RegionHandle x, RegionHandle y)
            {
                int xCount = GetLoadValue(x);
                int yCount = GetLoadValue(y);

                int compare = xCount.CompareTo(yCount);
                if (compare != 0)
                    return compare;

                return x.Id.CompareTo(y.Id);
            }

            private static int GetLoadValue(RegionHandle region)
            {
                // 1. Non-empty regions from lowest to highest player count.
                // 2. Empty regions (we don't want to put players into a completely empty region unless we have to).
                // 3. Full regions (reaching this means we need a new empty region).

                int playerCount = region.PlayerCount;
                if (playerCount == 0)
                    return int.MaxValue - 1;

                if (playerCount >= region.PlayerLimit)
                    return int.MaxValue;

                return playerCount;
            }
        }
    }
}
