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

            region.OnAddedToWorldView(this);
            return true;
        }

        public bool RemoveRegion(RegionHandle region)
        {
            if (region == null) return Logger.WarnReturn(false, "RemoveRegion(): region == null");

            if (_regions.Remove(region.Id) == false)
                return false;

            region.OnRemovedFromWorldView(this);

            // TODO: Send WorldView to the game to update cache

            return true;
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
    }
}
