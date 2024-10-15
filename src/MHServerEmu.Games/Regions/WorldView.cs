using System.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Regions
{
    public class WorldView : IEnumerable<KeyValuePair<PrototypeId, ulong>>
    {
        // NOTE: This is based on the PlayerMgrToGameServer protocol extracted from 1.53 builds.

        // WorldView is a class that represents a collection of region instances (both public and private)
        // bound to a player. This is what allows a player to access their private instances, as well as
        // consistently return to the same public instances. When in party, everyone should use the world view
        // of the leader.

        // TODO: Implement some method of short-term persistence between sessions (e.g. so your world view doesn't reset when you relog).
        // TODO: PlayerManager should keep track of this as well.

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly HashSet<ulong> _regionIds = new();
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

            if (_regionIds.Contains(regionId))
                return Logger.WarnReturn(false, $"AddRegion(): World view for {Owner} already contains region 0x{regionId:X} ({regionProtoRef.GetName()})");

            _regionIds.Add(regionId);
            _regionInstanceDict[regionProtoRef] = regionId;
            return true;
        }

        public bool RemoveRegion(PrototypeId regionProtoRef)
        {
            if (_regionInstanceDict.TryGetValue(regionProtoRef, out ulong regionId) == false)
                return false;

            if (_regionIds.Remove(regionId) == false)
                Logger.Warn($"RemoveRegion(): 0x{regionId:X} not found");

            _regionInstanceDict.Remove(regionProtoRef);
            return true;
        }

        public bool ContainsRegionInstanceId(ulong regionId)
        {
            return _regionIds.Contains(regionId);
        }

        public ulong GetRegionInstanceId(PrototypeId regionProtoRef)
        {
            if (_regionInstanceDict.TryGetValue(regionProtoRef, out ulong regionId) == false)
                return 0;

            return regionId;
        }

        public void Clear()
        {
            _regionIds.Clear();
            _regionInstanceDict.Clear();
        }

        public Dictionary<PrototypeId, ulong>.Enumerator GetEnumerator()
        {
            return _regionInstanceDict.GetEnumerator();
        }

        IEnumerator<KeyValuePair<PrototypeId, ulong>> IEnumerable<KeyValuePair<PrototypeId, ulong>>.GetEnumerator()
        {
            return _regionInstanceDict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _regionInstanceDict.GetEnumerator();
        }
    }
}
