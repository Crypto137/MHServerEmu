using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Regions
{
    public class WorldView
    {
        // NOTE: This is based on the PlayerMgrToGameServer protocol extracted from 1.53 builds.

        // WorldView is a class that represents a collection of region instances (both public and private)
        // bound to a player. This is what allows a player to access their private instances, as well as
        // consistently return to the same public instances. When in party, everyone should use the world view
        // of the leader.

        // TODO: Implement some method of short-term persistence between sessions (e.g. so your world view doesn't reset when you relog).
        // TODO: PlayerManager should keep track of this as well.

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<PrototypeId, ulong> _regionInstanceDict = new();

        public PlayerConnection Owner { get; }

        public WorldView(PlayerConnection owner)
        {
            Owner = owner;
        }

        public bool AddRegion(PrototypeId regionProtoRef, ulong regionId)
        {
            if (regionProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "AddRegion(): regionProtoRef == PrototypeId.Invalid");
            if (regionId == 0) return Logger.WarnReturn(false, "AddRegion(): regionId == 0");

            _regionInstanceDict[regionProtoRef] = regionId;
            return true;
        }

        public bool RemoveRegion(PrototypeId regionProtoRef)
        {
            return _regionInstanceDict.Remove(regionProtoRef);
        }

        public ulong GetRegionInstanceId(PrototypeId regionProtoRef)
        {
            if (_regionInstanceDict.TryGetValue(regionProtoRef, out ulong regionId) == false)
                return 0;

            return regionId;
        }

        public void Clear()
        {
            _regionInstanceDict.Clear();
        }
    }
}
