using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.PlayerManagement.Players;

namespace MHServerEmu.PlayerManagement.Regions
{
    /// <summary>
    /// Represents a collection of region instances (both public and private) bound to a player or a party.
    /// </summary>
    /// <remarks>
    /// This is what allows a player to access their private instances, as well as consistently return to the same public instances.
    /// </remarks>
    public class WorldView
    {
        private readonly Dictionary<ulong, RegionHandle> _regions = new();
        
        // Party world views start as copies of the leader's world view, and their ownership is shared by all members.
        private readonly HashSet<PlayerHandle> _owners = new();

        public WorldView(PlayerHandle owner = null)
        {
            if (owner != null)
                AddOwner(owner);
        }

        public Dictionary<ulong, RegionHandle>.ValueCollection.Enumerator GetEnumerator()
        {
            return _regions.Values.GetEnumerator();
        }

        public void AddOwner(PlayerHandle player)
        {
            if (!Verify.IsNotNull(player)) return;

            if (_owners.Add(player) == false)
                return;

            player.SyncWorldView();
        }

        public void RemoveOwner(PlayerHandle player)
        {
            if (!Verify.IsNotNull(player)) return;

            if (_owners.Remove(player) == false)
                return;

            player.SyncWorldView();
        }

        public void AddRegion(RegionHandle region)
        {
            if (!Verify.IsNotNull(region)) return;

            if (_regions.TryAdd(region.Id, region) == false)
                return;

            region.Reserve(RegionReservationType.WorldView);
        }

        public void RemoveRegion(RegionHandle region)
        {
            if (!Verify.IsNotNull(region)) return;

            if (_regions.Remove(region.Id) == false)
                return;

            region.Unreserve(RegionReservationType.WorldView);

            foreach (PlayerHandle owner in _owners)
                owner.SyncWorldView();
        }

        public void AddRegionsFrom(WorldView other)
        {
            if (!Verify.IsNotNull(other)) return;

            foreach (RegionHandle region in other)
                AddRegion(region);
        }

        public void Clear()
        {
            foreach (RegionHandle region in _regions.Values)
                RemoveRegion(region);

            Verify.IsTrue(_regions.Count == 0);
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

        public bool ContainsRegion(ulong regionId)
        {
            return _regions.ContainsKey(regionId);
        }

        public void BuildWorldViewCache(List<(ulong, ulong)> worldViewCache)
        {
            foreach (RegionHandle region in this)
                worldViewCache.Add((region.Id, (ulong)region.RegionProtoRef));
        }
    }
}
