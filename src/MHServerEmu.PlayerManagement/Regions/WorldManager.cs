using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System;

namespace MHServerEmu.PlayerManagement.Regions
{
    /// <summary>
    /// Global manager for all regions across all games.
    /// </summary>
    public class WorldManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly IdGenerator _idGenerator = new(IdType.Region, 0);

        private readonly Dictionary<ulong, RegionHandle> _allRegions = new();

        private readonly DoubleBufferQueue<IGameServiceMessage> _messageQueue = new();

        private readonly PlayerManagerService _playerManager;

        public ulong NextRegionId { get => _idGenerator.Generate(); }

        public WorldManager(PlayerManagerService playerManager)
        {
            _playerManager = playerManager;
        }

        public void Update()
        {
            ProcessMessageQueue();
        }

        public void ReceiveMessage<T>(in T message) where T : struct, IGameServiceMessage
        {
            _messageQueue.Enqueue(message);
        }

        public RegionHandle GetRegion(ulong regionId)
        {
            if (_allRegions.TryGetValue(regionId, out RegionHandle region) == false)
                return null;

            return region;
        }

        public bool AddRegion(RegionHandle region)
        {
            _allRegions.Add(region.Id, region);
            return true;
        }

        public bool RemoveRegion(ulong regionId)
        {
            return _allRegions.Remove(regionId);
        }

        #region Ticking

        private void ProcessMessageQueue()
        {
            _messageQueue.Swap();

            while (_messageQueue.CurrentCount > 0)
            {
                IGameServiceMessage serviceMessage = _messageQueue.Dequeue();

                switch (serviceMessage)
                {
                    case ServiceMessage.GameInstanceCreateRegionResponse createRegionResponse:
                        OnCreateRegionResponse(createRegionResponse);
                        break;

                    default:
                        Logger.Warn($"ProcessMessageQueue(): Unhandled service message type {serviceMessage.GetType().Name}");
                        break;
                }
            }
        }

        #endregion

        #region Message Handling

        private bool OnCreateRegionResponse(in ServiceMessage.GameInstanceCreateRegionResponse createRegionResponse)
        {
            RegionHandle region = GetRegion(createRegionResponse.RegionId);
            if (region == null)
                return Logger.WarnReturn(false, $"OnCreateRegionResponse(): Region 0x{createRegionResponse.RegionId:X} not found");

            region.OnInstanceCreateResponse(createRegionResponse.Success);
            return true;
        }

        #endregion
    }
}
