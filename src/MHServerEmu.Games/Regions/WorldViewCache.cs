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

        public void Sync(List<(ulong, ulong)> syncData)
        {
            Clear();

            if (syncData != null)
            {
                foreach ((ulong regionId, ulong regionProtoRef) in syncData)
                    AddRegion(regionId, (PrototypeId)regionProtoRef);
            }
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

            Owner.Player?.ScheduleWorldViewUpdate();

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

            Owner.Player?.ScheduleWorldViewUpdate();

            return true;
        }

        /// <summary>
        /// Removes all regions from this <see cref="WorldViewCache"/>.
        /// </summary>
        public void Clear()
        {
            _regionIds.Clear();
            _regionIdsByProto.Clear();

            Owner.Player?.ScheduleWorldViewUpdate();
        }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="WorldViewCache"/> contains the specified region.
        /// </summary>
        public bool ContainsRegion(ulong regionId)
        {
            return _regionIds.Contains(regionId);
        }
    }
}
