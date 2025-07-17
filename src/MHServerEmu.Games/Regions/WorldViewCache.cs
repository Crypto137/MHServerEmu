using System.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Regions
{
    public class WorldViewCache : IEnumerable<KeyValuePair<PrototypeId, ulong>>
    {
        // TODO: PlayerManager should have the authoritative copy of this data. This is just a cache for local lookups.

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly HashSet<ulong> _regionIds = new();
        private readonly Dictionary<PrototypeId, ulong> _regionInstanceDict = new();

        public PlayerConnection Owner { get; }

        public WorldViewCache(PlayerConnection owner)
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
