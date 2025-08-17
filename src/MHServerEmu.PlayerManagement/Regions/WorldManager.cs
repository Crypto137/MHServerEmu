using Gazillion;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System;
using MHServerEmu.Games.GameData;

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
        private readonly Dictionary<PrototypeId, RegionLoadBalancer> _publicRegions = new();

        private readonly DoubleBufferQueue<IGameServiceMessage> _messageQueue = new();

        private readonly PlayerManagerService _playerManager;

        public ulong NextRegionId { get => _idGenerator.Generate(); }

        public WorldManager(PlayerManagerService playerManager)
        {
            _playerManager = playerManager;
        }

        public Dictionary<ulong, RegionHandle>.ValueCollection.Enumerator GetEnumerator()
        {
            return _allRegions.Values.GetEnumerator();
        }

        public void Update()
        {
            ProcessMessageQueue();
        }

        public void ReceiveMessage<T>(in T message) where T : struct, IGameServiceMessage
        {
            _messageQueue.Enqueue(message);
        }

        public RegionHandle GetOrCreatePublicRegion(PrototypeId regionProtoRef, NetStructCreateRegionParams createRegionParams)
        {
            if (_publicRegions.TryGetValue(regionProtoRef, out RegionLoadBalancer regionLoadBalancer) == false)
            {
                regionLoadBalancer = new(regionProtoRef);
                _publicRegions.Add(regionProtoRef, regionLoadBalancer);
            }

            RegionHandle region = regionLoadBalancer.GetAvailableRegion((PrototypeId)createRegionParams.DifficultyTierProtoId);
            if (region == null)
            {
                GameHandle game = _playerManager.GameHandleManager.CreateGame();
                region = CreateRegionInGame(game, regionProtoRef, createRegionParams, RegionFlags.None);
            }

            return region;
        }

        public RegionHandle CreatePrivateRegion(PlayerHandle owner, PrototypeId regionProtoRef, NetStructCreateRegionParams createRegionParams)
        {
            GameHandle privateGame = owner.PrivateGame;

            // The owner may not have a private game yet OR it may have crashed, in which case we need to create a new one.
            if (privateGame == null || privateGame.State == GameHandleState.PendingShutdown || privateGame.State == GameHandleState.Shutdown)
            {
                privateGame = _playerManager.GameHandleManager.CreateGame();
                owner.SetPrivateGame(privateGame);
            }

            return CreateRegionInGame(privateGame, regionProtoRef, createRegionParams, RegionFlags.CloseWhenReservationsReachesZero);
        }

        public RegionHandle GetRegion(ulong regionId)
        {
            if (_allRegions.TryGetValue(regionId, out RegionHandle region) == false)
                return null;

            return region;
        }

        public bool AddRegion(RegionHandle region)
        {
            // Lock here because another thread may be reading all regions to generate a report.
            lock (_allRegions)
            {
                if (_allRegions.TryAdd(region.Id, region) == false)
                    return false;
            }

            if (region.IsPublic)
                RegisterPublicRegion(region);

            return true;
        }

        public bool RemoveRegion(RegionHandle region)
        {
            lock (_allRegions)
            {
                if (_allRegions.Remove(region.Id) == false)
                    return false;
            }

            if (region.IsPublic)
                UnregisterPublicRegion(region);

            return true;
        }

        public bool RegisterPublicRegion(RegionHandle region)
        {
            if (_publicRegions.TryGetValue(region.RegionProtoRef, out RegionLoadBalancer loadBalancer) == false)
                return false;

            return loadBalancer.AddRegion(region);
        }

        public bool UnregisterPublicRegion(RegionHandle region)
        {
            if (_publicRegions.TryGetValue(region.RegionProtoRef, out RegionLoadBalancer loadBalancer) == false)
                return false;

            return loadBalancer.RemoveRegion(region);
        }

        public void GetRegionReportData(RegionReport report)
        {
            // TODO: Track regions in metrics instead of locking the original collection.
            lock (_allRegions)
                report.Initialize(this);
        }

        private RegionHandle CreateRegionInGame(GameHandle game, PrototypeId regionProtoRef, NetStructCreateRegionParams createRegionParams, RegionFlags flags)
        {
            if (game.CreateRegion(_idGenerator.Generate(), regionProtoRef, createRegionParams, flags, out RegionHandle region) == false)
                return null;

            return region;
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
                    case ServiceMessage.CreateRegionResult createRegionResponse:
                        OnCreateRegionResponse(createRegionResponse);
                        break;

                    case ServiceMessage.RequestRegionShutdown requestRegionShutdown:
                        OnRequestRegionShutdown(requestRegionShutdown);
                        break;

                    default:
                        Logger.Warn($"ProcessMessageQueue(): Unhandled service message type {serviceMessage.GetType().Name}");
                        break;
                }
            }
        }

        #endregion

        #region Message Handling

        private bool OnCreateRegionResponse(in ServiceMessage.CreateRegionResult createRegionResponse)
        {
            RegionHandle region = GetRegion(createRegionResponse.RegionId);
            if (region == null)
                return Logger.WarnReturn(false, $"OnCreateRegionResponse(): Region 0x{createRegionResponse.RegionId:X} not found");

            region.OnInstanceCreateResponse(createRegionResponse.Success);
            return true;
        }

        private bool OnRequestRegionShutdown(in ServiceMessage.RequestRegionShutdown requestRegionShutdown)
        {
            RegionHandle region = GetRegion(requestRegionShutdown.RegionId);
            if (region == null)
                return Logger.WarnReturn(false, $"OnRequestRegionShutdown(): Region 0x{requestRegionShutdown.RegionId:X} not found");

            region.RequestShutdown();
            return true;
        }

        #endregion
    }
}
