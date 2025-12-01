using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.PlayerManagement.Matchmaking
{
    /// <summary>
    /// Manages <see cref="RegionRequestQueue"/> instances.
    /// </summary>
    public class RegionRequestQueueManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<PrototypeId, RegionRequestQueue> _queues = new();

        private readonly PlayerManagerService _playerManager;

        public RegionRequestQueueManager(PlayerManagerService playerManager)
        {
            _playerManager = playerManager;
        }

        /// <summary>
        /// Initializes <see cref="RegionRequestQueue"/> instances for all match regions.
        /// </summary>
        public void Initialize()
        {
            _queues.Clear();

            foreach (PrototypeId regionRef in DataDirectory.Instance.IteratePrototypesInHierarchy<RegionPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                RegionPrototype regionProto = regionRef.As<RegionPrototype>();
                if (regionProto == null)
                {
                    Logger.Warn("Initialize(): regionProto == null");
                    continue;
                }

                if (regionProto.IsQueueRegion == false)
                    continue;

                RegionRequestQueue queue = new(regionProto);
                _queues.Add(regionRef, queue);

                Logger.Trace($"Initialized region request queue for {regionRef.GetName()}");
            }
        }

        /// <summary>
        /// Returns the <see cref="RegionRequestQueue"/> instance for the specified region.
        /// </summary>
        public RegionRequestQueue GetRegionRequestQueue(PrototypeId regionRef)
        {
            if (regionRef == PrototypeId.Invalid)
                return null;

            if (_queues.TryGetValue(regionRef, out RegionRequestQueue queue) == false)
                return null;

            return queue;
        }
    }
}
