using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.PlayerManagement.Regions
{
    /// <summary>
    /// Represents a collection of region instances (both public and private) bound to a player.
    /// </summary>
    /// <remarks>
    /// This is what allows a player to access their private instances, as well as consistently return to the same public instances. 
    /// When in party, everyone should use the world view of the leader.
    /// </remarks>
    public class WorldView
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<ulong, RegionHandle> _regions = new();

        public PlayerHandle Owner { get; }

        public WorldView(PlayerHandle owner)
        {
            Owner = owner;
        }

        public bool AddRegion(RegionHandle region)
        {
            if (region == null) return Logger.WarnReturn(false, "AddRegion(): region == null");

            if (_regions.TryAdd(region.Id, region) == false)
                return false;

            region.Reserve(RegionReservationType.WorldView);
            return true;
        }

        public bool RemoveRegion(RegionHandle region)
        {
            if (region == null) return Logger.WarnReturn(false, "RemoveRegion(): region == null");

            if (_regions.Remove(region.Id) == false)
                return false;

            region.Unreserve(RegionReservationType.WorldView);

            Owner.SyncWorldView();

            return true;
        }

        public void Clear()
        {
            foreach (RegionHandle region in _regions.Values)
                RemoveRegion(region);

            if (_regions.Count != 0)
                Logger.Warn("Clear(): _regions.Count != 0");
        }

        public void ClearPrivateStoryRegions()
        {
            foreach (RegionHandle region in _regions.Values)
            {
                if (region.IsPrivateStory == false)
                    continue;

                RemoveRegion(region);
            }
        }

        public RegionHandle GetMatchingRegion(PrototypeId regionProtoRef, NetStructCreateRegionParams createRegionParams)
        {
            foreach (RegionHandle region in _regions.Values)
            {
                if (region.State == RegionHandleState.Shutdown)
                {
                    _regions.Remove(region.Id);
                    continue;
                }

                if (region.RegionProtoRef != regionProtoRef)
                    continue;

                if (createRegionParams != null && region.MatchesCreateParams(createRegionParams) == false)
                    continue;

                return region;
            }

            return null;
        }

        public List<(ulong, ulong)> BuildWorldViewCache()
        {
            // TODO: Consider pooling this if it causes too many List allocations.
            List<(ulong, ulong)> list = new(_regions.Count);

            foreach (RegionHandle region in _regions.Values)
                list.Add((region.Id, (ulong)region.RegionProtoRef));

            return list;
        }
    }
}
