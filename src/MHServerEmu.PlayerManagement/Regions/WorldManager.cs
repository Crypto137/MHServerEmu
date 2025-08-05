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
        private readonly Dictionary<(PrototypeId, PrototypeId), RegionHandle> _publicRegions = new();

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

        public RegionHandle GetOrCreatePublicRegion(PrototypeId regionProtoRef, NetStructCreateRegionParams createRegionParams)
        {
            // TODO: Multiple instances of public regions and load balancing
            var key = (regionProtoRef, (PrototypeId)createRegionParams.DifficultyTierProtoId);
            if (_publicRegions.TryGetValue(key, out RegionHandle region) == false)
            {
                GameHandle game = _playerManager.GameHandleManager.CreateGame();
                region = CreateRegionInGame(game, regionProtoRef, createRegionParams);
                _publicRegions[key] = region;
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

            return CreateRegionInGame(privateGame, regionProtoRef, createRegionParams);
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

        private RegionHandle CreateRegionInGame(GameHandle game, PrototypeId regionProtoRef, NetStructCreateRegionParams createRegionParams)
        {
            if (game.CreateRegion(_idGenerator.Generate(), regionProtoRef, createRegionParams, out RegionHandle region) == false)
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
