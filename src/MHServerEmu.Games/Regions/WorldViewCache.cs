using System.Collections;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Regions
{
    /// <summary>
    /// Game-local cache for a player's WorldView. PlayerManager holds the authoritative copy of this data.
    /// </summary>
    /// <remarks>
    /// WorldView represents a collection of region instances that are bound to a specific player.
    /// </remarks>
    public class WorldViewCache : IEnumerable<(PrototypeId, ulong)>
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly HashSet<ulong> _regionIds = new();
        private readonly SortedVector<(PrototypeId, ulong)> _regionIdsByProto = new();

        public PlayerConnection Owner { get; }

        public WorldViewCache(PlayerConnection owner)
        {
            Owner = owner;
        }

        public List<(PrototypeId, ulong)>.Enumerator GetEnumerator()
        {
            return _regionIdsByProto.GetEnumerator();
        }

        IEnumerator<(PrototypeId, ulong)> IEnumerable<(PrototypeId, ulong)>.GetEnumerator()
        {
            return _regionIdsByProto.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _regionIdsByProto.GetEnumerator();
        }

        /// <summary>
        /// Adds a region to this <see cref="WorldViewCache"/>. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool AddRegion(ulong regionId, PrototypeId regionProtoRef)
        {
            if (regionId == 0) return Logger.WarnReturn(false, "AddRegion(): regionId == 0");
            if (regionProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "AddRegion(): regionProtoRef == PrototypeId.Invalid");

            if (_regionIds.Contains(regionId))
                return Logger.WarnReturn(false, $"AddRegion(): World view for {Owner} already contains region 0x{regionId:X} ({regionProtoRef.GetName()})");

            _regionIds.Add(regionId);
            _regionIdsByProto.SortedInsert((regionProtoRef, regionId));
            return true;
        }

        /// <summary>
        /// Removes the specified region from this <see cref="WorldViewCache"/>. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool RemoveRegion(ulong regionId)
        {
            if (_regionIds.Remove(regionId) == false)
                return Logger.WarnReturn(false, $"RemoveRegion(): 0x{regionId:X} not found");

            for (int i = 0; i < _regionIdsByProto.Count; i++)
            {
                (_, ulong itRegionId) = _regionIdsByProto[i];
                if (itRegionId == regionId)
                {
                    _regionIdsByProto.RemoveAt(i);
                    break;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="WorldViewCache"/> contains the specified region.
        /// </summary>
        public bool ContainsRegionInstanceId(ulong regionId)
        {
            return _regionIds.Contains(regionId);
        }

        /// <summary>
        /// Returns the instance ids of regions with the specified prototype in this <see cref="WorldViewCache"/>.
        /// </summary>
        public bool GetRegionInstanceIds(PrototypeId regionProtoRef, List<ulong> regionIds)
        {
            for (int i = 0; i < _regionIdsByProto.Count; i++)
            {
                (PrototypeId itRegionProtoRef, ulong itRegionId) = _regionIdsByProto[i];
                if (itRegionProtoRef == regionProtoRef)
                    regionIds.Add(itRegionId);

                if (itRegionProtoRef > regionProtoRef)
                    break;
            }

            return regionIds.Count > 0;
        }

        /// <summary>
        /// Returns the first instance id with the specified prototype in this <see cref="WorldViewCache"/>. Returns 0 if not found.
        /// </summary>
        public ulong GetRegionInstanceId(PrototypeId regionProtoRef)
        {
            for (int i = 0; i < _regionIdsByProto.Count; i++)
            {
                (PrototypeId itRegionProtoRef, ulong itRegionId) = _regionIdsByProto[i];
                if (itRegionProtoRef == regionProtoRef)
                    return itRegionId;

                if (itRegionProtoRef > regionProtoRef)
                    break;
            }

            return 0;
        }

        /// <summary>
        /// Removes all regions from this <see cref="WorldViewCache"/>.
        /// </summary>
        public void Clear()
        {
            _regionIds.Clear();
            _regionIdsByProto.Clear();
        }
    }
}
