using System.Buffers;
using Gazillion;
using MHServerEmu.Core.System.Time;

namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// Marker interface for <see cref="IGameService"/> messages.
    /// </summary>
    public interface IGameServiceMessage
    {
    }

    #region Enums

    public enum GameInstanceOpType
    {
        Create,
        CreateResponse,
        Shutdown,
        ShutdownNotice,     // This is called a notice because game instances can shut down due to a crash.
    }

    public enum GameInstanceClientOpType
    {
        Add,
        AddResponse,
        Remove,
        RemoveResponse,
    }

    #endregion

    public static class ServiceMessage
    {
        // NOTE: Although we are currently using readonly structs here, unfortunately it seems
        // using pattern matching to switch on the message type causes boxing. Need to figure
        // out a more performant way to send messages without overcomplicating everything
        // (e.g. using the visitor pattern here would probably work, but it may be too cumbersome).

        public readonly struct AddClient(IFrontendClient client)
            : IGameServiceMessage
        {
            public readonly IFrontendClient Client = client;
        }

        public readonly struct RemoveClient(IFrontendClient client)
            : IGameServiceMessage
        {
            public readonly IFrontendClient Client = client;
        }

        public readonly struct RouteMessageBuffer(IFrontendClient client, MessageBuffer messageBuffer)
            : IGameServiceMessage
        {
            public readonly IFrontendClient Client = client;
            public readonly MessageBuffer MessageBuffer = messageBuffer;
        }

        public readonly struct RouteMessage(IFrontendClient client, Type protocol, MailboxMessage message)
            : IGameServiceMessage
        {
            public readonly IFrontendClient Client = client;
            public readonly Type Protocol = protocol;
            public readonly MailboxMessage Message = message;
        }

        #region Player Manager

        public readonly struct GameInstanceOp(GameInstanceOpType type, ulong gameId)
            : IGameServiceMessage
        {
            public readonly GameInstanceOpType Type = type;
            public readonly ulong GameId = gameId;
        }

        public readonly struct GameInstanceClientOp(GameInstanceClientOpType type, IFrontendClient client, ulong gameId)
            : IGameServiceMessage
        {
            public readonly GameInstanceClientOpType Type = type;
            public readonly IFrontendClient Client = client;
            public readonly ulong GameId = gameId;
        }

        /// <summary>
        /// [PlayerManager -> Game] Instructs the game instance service to create a region with the specified params.
        /// </summary>
        public readonly struct CreateRegion(ulong gameId, ulong regionId, ulong regionProtoRef, NetStructCreateRegionParams createParams = null)
            : IGameServiceMessage
        {
            public readonly ulong GameId = gameId;
            public readonly ulong RegionId = regionId;
            public readonly ulong RegionProtoRef = regionProtoRef;
            public readonly NetStructCreateRegionParams CreateParams = createParams;
        }

        /// <summary>
        /// [Game -> PlayerManager] Notifies the player manager of the result of a region creation request (success or failure).
        /// </summary>
        public readonly struct CreateRegionResult(ulong regionId, bool success)
            : IGameServiceMessage
        {
            public readonly ulong RegionId = regionId;
            public readonly bool Success = success;
        }

        /// <summary>
        /// [Game -> PlayerManager] Requests <see cref="RegionPlayerAccessVar"/> update for a region.
        /// </summary>
        public readonly struct SetRegionPlayerAccess(ulong regionId, RegionPlayerAccessVar playerAccess)
            : IGameServiceMessage
        {
            public readonly ulong RegionId = regionId;
            public readonly RegionPlayerAccessVar PlayerAccess = playerAccess;
        }

        /// <summary>
        /// [Game -> PlayerManager] Requests the player manager to shut down the specified region.
        /// </summary>
        public readonly struct RequestRegionShutdown(ulong regionId)
            : IGameServiceMessage
        {
            public readonly ulong RegionId = regionId;
        }

        /// <summary>
        /// [PlayerManager -> Game] Instructs the game instance service to shut down the specified region.
        /// </summary>
        public readonly struct ShutdownRegion(ulong gameId, ulong regionId)
            : IGameServiceMessage
        {
            public readonly ulong GameId = gameId;
            public readonly ulong RegionId = regionId;
        }

        /// <summary>
        /// [PlayerManager -> Game] Instructs the game instance service to destroy the specified region access portal entity.
        /// </summary>
        public readonly struct DestroyPortal(ulong gameId, NetStructPortalInstance portal)
            : IGameServiceMessage
        {
            public readonly ulong GameId = gameId;
            public readonly NetStructPortalInstance Portal = portal;
        }

        /// <summary>
        /// [Game -> PlayerManager] Requests the player manager to transfer a player to another region.
        /// </summary>
        public readonly struct ChangeRegionRequest
            : IGameServiceMessage
        {
            public readonly ChangeRegionRequestHeader Header;
            public readonly NetStructRegionTarget DestTarget;
            public readonly NetStructRegionLocation DestLocation;
            public readonly ulong DestPlayerDbId;
            public readonly NetStructCreateRegionParams CreateRegionParams;

            public ChangeRegionRequest(ChangeRegionRequestHeader header, NetStructRegionTarget destTarget, NetStructCreateRegionParams createRegionParams)
            {
                Header = header;
                DestTarget = destTarget;
                CreateRegionParams = createRegionParams;
            }

            public ChangeRegionRequest(ChangeRegionRequestHeader header, NetStructRegionLocation destLocation)
            {
                Header = header;
                DestLocation = destLocation;
            }

            public ChangeRegionRequest(ChangeRegionRequestHeader header, ulong destPlayerDbId)
            {
                Header = header;
                DestPlayerDbId = destPlayerDbId;
            }
        }

        /// <summary>
        /// [PlayerManager -> Game] Notification for a failed transfer to the player who requested it.
        /// </summary>
        public readonly struct UnableToChangeRegion(ulong gameId, ulong playerDbId, ChangeRegionFailed changeFailed)
            : IGameServiceMessage
        {
            // Based on PlayerMgrToGameServer.proto from 1.53
            public readonly ulong GameId = gameId;
            public readonly ulong PlayerDbId = playerDbId;
            public readonly ChangeRegionFailed ChangeFailed = changeFailed;
        }

        /// <summary>
        /// [PlayerManager -> Game] Contains <see cref="NetStructTransferParams"/> with information needed to put a player into a region.
        /// </summary>
        public readonly struct GameAndRegionForPlayer(ulong gameId, ulong playerDbId, NetStructTransferParams transferParams, List<(ulong, ulong)> worldViewSyncData)
            : IGameServiceMessage
        {
            // Based on PlayerMgrToGameServer.proto from 1.53
            public readonly ulong GameId = gameId;
            public readonly ulong PlayerDbId = playerDbId;
            public readonly NetStructTransferParams TransferParams = transferParams;
            public readonly List<(ulong, ulong)> WorldViewSyncData = worldViewSyncData;
        }

        /// <summary>
        /// [Game -> PlayerManager] Confirms region transfer completion.
        /// </summary>
        public readonly struct RegionTransferFinished(ulong playerDbId, ulong transferId)
            : IGameServiceMessage
        {
            public readonly ulong PlayerDbId = playerDbId;
            public readonly ulong TransferId = transferId;
        }

        /// <summary>
        /// [PlayerManager -> Game] Synchronizes a WorldViewCache with the state of the authoritative WorldView in the player manager.
        /// </summary>
        public readonly struct WorldViewSync(ulong gameId, ulong playerDbId, List<(ulong, ulong)> syncData)
            : IGameServiceMessage
        {
            public readonly ulong GameId = gameId;
            public readonly ulong PlayerDbId = playerDbId;
            public readonly List<(ulong, ulong)> SyncData = syncData;
        }

        /// <summary>
        /// [Game -> PlayerManager] Requests the player manager to remove all private story regions from the specified player's WorldView.
        /// </summary>
        public readonly struct ClearPrivateStoryRegions(ulong playerDbId)
            : IGameServiceMessage
        {
            public readonly ulong PlayerDbId = playerDbId;
        }

        /// <summary>
        /// [Game -> PlayerManager] Updates the difficulty tier preference of a player on the Player Manager.
        /// </summary>
        public readonly struct SetDifficultyTierPreference(ulong playerDbId, ulong difficultyTierProtoId)
            : IGameServiceMessage
        {
            public readonly ulong PlayerDbId = playerDbId;
            public readonly ulong DifficultyTierProtoId = difficultyTierProtoId;
        }

        /// <summary>
        /// [Game -> PlayerManager] Requests player dbid and properly cased name from the player manager.
        /// </summary>
        public readonly struct PlayerLookupByNameRequest(ulong gameId, ulong playerDbId, ulong remoteJobId, string requestPlayerName)
            : IGameServiceMessage
        {
            public readonly ulong GameId = gameId;
            public readonly ulong PlayerDbId = playerDbId;
            public readonly ulong RemoteJobId = remoteJobId;
            public readonly string RequestPlayerName = requestPlayerName;
        }

        /// <summary>
        /// [PlayerManager -> Game] Response for PlayerLookupByNameRequest.
        /// </summary>
        public readonly struct PlayerLookupByNameResult(ulong gameId, ulong playerDbId, ulong remoteJobId, ulong resultPlayerDbId, string resultPlayerName)
            : IGameServiceMessage
        {
            public readonly ulong GameId = gameId;
            public readonly ulong PlayerDbId = playerDbId;
            public readonly ulong RemoteJobId = remoteJobId;
            public readonly ulong ResultPlayerDbId = resultPlayerDbId;
            public readonly string ResultPlayerName = resultPlayerName;
        }

        public readonly struct PlayerNameChanged(ulong playerDbId, string oldPlayerName, string newPlayerName)
            : IGameServiceMessage
        {
            public readonly ulong PlayerDbId = playerDbId;
            public readonly string OldPlayerName = oldPlayerName;
            public readonly string NewPlayerName = newPlayerName;
        }

        /// <summary>
        /// [Game -> PlayerManager] Updates community status for a player.
        /// </summary>
        public readonly struct CommunityStatusUpdate(CommunityMemberBroadcast broadcast)
            : IGameServiceMessage
        {
            public readonly CommunityMemberBroadcast Broadcast = broadcast;
        }

        /// <summary>
        /// [Game -> PlayerManager] Requests community status for the specified players from the player manager.
        /// </summary>
        public readonly struct CommunityStatusRequest(ulong gameId, ulong playerDbId, List<ulong> members)
            : IGameServiceMessage
        {
            public readonly ulong GameId = gameId;
            public readonly ulong PlayerDbId = playerDbId;
            public readonly List<ulong> Members = members;
        }

        /// <summary>
        /// [PlayerManager -> Game] A batch of <see cref="CommunityMemberBroadcast"/> instances to be delivered to all players on the server.
        /// </summary>
        public readonly struct CommunityBroadcastBatch : IGameServiceMessage
        {
            // This combines both CommunitiesReceiveBroadcastBatch and CommunityBroadcastResults from PlayerMgrToGameServer.proto.

            private readonly CommunityMemberBroadcast Instance;     // Optimization to avoid allocating lists for individual broadcasts
            private readonly List<CommunityMemberBroadcast> List;

            public readonly ulong GameId;
            public readonly ulong PlayerDbId;
            // We don't actually need a broadcast id with out implementation

            public CommunityMemberBroadcast this[int index] { get => Instance != null ? Instance : List[index]; }
            public int Count { get => Instance != null ? 1 : List.Count; }

            public CommunityBroadcastBatch(CommunityMemberBroadcast broadcast, ulong gameId = 0, ulong playerDbId = 0)
            {
                Instance = broadcast;
                List = null;

                GameId = gameId;
                PlayerDbId = playerDbId;
            }

            public CommunityBroadcastBatch(List<CommunityMemberBroadcast> broadcasts, ulong gameId = 0, ulong playerDbId = 0)
            {
                Instance = null;
                List = broadcasts;

                GameId = gameId;
                PlayerDbId = playerDbId;
            }
        }

        /// <summary>
        /// [Game -> PlayerManager] Forwards a party operation request received from a client to the player manager.
        /// </summary>
        public readonly struct PartyOperationRequest(PartyOperationPayload request)
            : IGameServiceMessage
        {
            public readonly PartyOperationPayload Request = request;
        }

        public readonly struct PartyBoostUpdate(ulong playerDbId, List<ulong> boosts)
            : IGameServiceMessage
        {
            public readonly ulong PlayerDbId = playerDbId;
            public readonly List<ulong> Boosts = boosts;
        }

        // NOTE: PlayerManager -> Game party messages are based on 1.53.

        /// <summary>
        /// [PlayerManager -> Game] Contains a response to a forwarded party operation request.
        /// </summary>
        public readonly struct PartyOperationRequestServerResult(ulong gameId, ulong playerDbId, PartyOperationPayload request, GroupingOperationResult result)
            : IGameServiceMessage
        {
            public readonly ulong GameId = gameId;
            public readonly ulong PlayerDbId = playerDbId;
            public readonly PartyOperationPayload Request = request;
            public readonly GroupingOperationResult Result = result;
        }

        /// <summary>
        /// [PlayerManager -> Game] Updates the state of a party in a game instance.
        /// </summary>
        public readonly struct PartyInfoServerUpdate(ulong gameId, ulong playerDbId, ulong groupId, PartyInfo partyInfo)
            : IGameServiceMessage
        {
            public readonly ulong GameId = gameId;
            public readonly ulong PlayerDbId = playerDbId;
            public readonly ulong GroupId = groupId;
            public readonly PartyInfo PartyInfo = partyInfo;
        }

        /// <summary>
        /// [PlayerManager -> Game] Update the state of a party member in a party in a game instance.
        /// </summary>
        public readonly struct PartyMemberInfoServerUpdate(ulong gameId, ulong playerDbId, ulong groupId, ulong memberDbId, PartyMemberEvent memberEvent, PartyMemberInfo memberInfo)
            : IGameServiceMessage
        {
            public readonly ulong GameId = gameId;
            public readonly ulong PlayerDbId = playerDbId;
            public readonly ulong GroupId = groupId;
            public readonly ulong MemberDbId = memberDbId;
            public readonly PartyMemberEvent MemberEvent = memberEvent;
            public readonly PartyMemberInfo MemberInfo = memberInfo;
        }

        /// <summary>
        /// [Game -> PlayerManager] Relays a match region request command from a client.
        /// </summary>
        public readonly struct MatchRegionRequestQueueCommand(ulong playerDbId, ulong regionProtoId, ulong difficultyTierProtoId, ulong metaStateProtoId, RegionRequestQueueCommandVar command, ulong regionRequestGroupId, ulong targetPlayerDbId)
            : IGameServiceMessage
        {
            public readonly ulong PlayerDbId = playerDbId;
            public readonly ulong RegionProtoId = regionProtoId;
            public readonly ulong DifficultyTierProtoId = difficultyTierProtoId;
            public readonly ulong MetaStateProtoId = metaStateProtoId;
            public readonly RegionRequestQueueCommandVar Command = command;
            public readonly ulong RegionRequestGroupId = regionRequestGroupId;
            public readonly ulong TargetPlayerDbId = targetPlayerDbId;
        }

        // MatchQueueUpdate is based on PlayerMgrToGameServer.proto from 1.53
        public readonly struct MatchQueueUpdateData(ulong updatePlayerGuid, RegionRequestQueueUpdateVar status, string updatePlayerName = null)
        {
            public readonly ulong UpdatePlayerGuid = updatePlayerGuid;
            public readonly RegionRequestQueueUpdateVar Status = status;
            public readonly string UpdatePlayerName = updatePlayerName;
        }

        /// <summary>
        /// [PlayerManager -> Game] Updates the state of a MatchQueueStatus instance game-side.
        /// </summary>
        public readonly struct MatchQueueUpdate(ulong gameId, ulong playerDbId, ulong regionProtoId, ulong difficultyTierProtoId, int playersInQueue, ulong regionRequestGroupId, List<MatchQueueUpdateData> data)
            : IGameServiceMessage
        {
            public readonly ulong GameId = gameId;
            public readonly ulong PlayerDbId = playerDbId;
            public readonly ulong RegionProtoId = regionProtoId;
            public readonly ulong DifficultyTierProtoId = difficultyTierProtoId;
            public readonly int PlayersInQueue = playersInQueue;
            public readonly ulong RegionRequestGroupId = regionRequestGroupId;
            public readonly List<MatchQueueUpdateData> Data = data;
        }

        /// <summary>
        /// [PlayerManager -> Game] Clears the state of a MatchQueueStatus instance game-side.
        /// </summary>
        public readonly struct MatchQueueFlush(ulong gameId, ulong playerDbId)
            : IGameServiceMessage
        {
            public readonly ulong GameId = gameId;
            public readonly ulong PlayerDbId = playerDbId;
        }

        #endregion

        #region Grouping Manager

        /// <summary>
        /// [Game -> GroupingManager] Routes a regular chat message from a game instance.
        /// </summary>
        public readonly struct GroupingManagerChat(ulong playerDbId, NetMessageChat chat, int prestigeLevel, List<ulong> playerFilter)
            : IGameServiceMessage
        {
            public readonly ulong PlayerDbId = playerDbId;
            public readonly NetMessageChat Chat = chat;
            public readonly int PrestigeLevel = prestigeLevel;
            public readonly List<ulong> PlayerFilter = playerFilter;
        }

        /// <summary>
        /// [Game -> GroupingManager] Routes a tell chat message from a game instance.
        /// </summary>
        public readonly struct GroupingManagerTell(ulong playerDbId, NetMessageTell tell, int prestigeLevel)
            : IGameServiceMessage
        {
            public readonly ulong PlayerDbId = playerDbId;
            public readonly NetMessageTell Tell = tell;
            public readonly int PrestigeLevel = prestigeLevel;
        }

        /// <summary>
        /// [Any -> GroupingManager] Sends a custom metagame chat message to the specified player.
        /// </summary>
        public readonly struct GroupingManagerMetagameMessage(ulong playerDbId, string text, bool showSender)
            : IGameServiceMessage
        {
            public readonly ulong PlayerDbId = playerDbId;
            public readonly string Text = text;
            public readonly bool ShowSender = showSender;
        }

        /// <summary>
        /// [Command -> GroupingManager] Broadcasts a server notification to all connected clients.
        /// </summary>
        public readonly struct GroupingManagerServerNotification(string notificationText)
            : IGameServiceMessage
        {
            public readonly string NotificationText = notificationText;
        }

        #endregion

        #region Leaderboards

        /// <summary>
        /// [Game -> LeaderboardService] Communicates a change of state of a specific leaderboard rule.
        /// </summary>
        public readonly struct LeaderboardScoreUpdate(ulong leaderboardId, ulong participantId, ulong avatarId, ulong ruleId, ulong count)
            : IGameServiceMessage
        {
            public readonly ulong LeaderboardId = leaderboardId;
            public readonly ulong ParticipantId = participantId;
            public readonly ulong AvatarId = avatarId;
            public readonly ulong RuleId = ruleId;
            public readonly ulong Count = count;
        }

        /// <summary>
        /// [Game -> LeaderboardService] Container for a batch of <see cref="LeaderboardScoreUpdate"/> instances.
        /// </summary>
        public readonly struct LeaderboardScoreUpdateBatch(int count)
            : IGameServiceMessage
        {
            private static readonly ArrayPool<LeaderboardScoreUpdate> Pool = ArrayPool<LeaderboardScoreUpdate>.Create();

            // Use arrays instead of lists to access data by reference instead of copying.
            // ArrayPool can return arrays larger than requested, so we also need to specify count.
            private readonly LeaderboardScoreUpdate[] _updates = Pool.Rent(count);

            public readonly int Count = count;

            public ref LeaderboardScoreUpdate this[int i] { get => ref _updates[i]; }

            /// <summary>
            /// Releases resources used by this <see cref="LeaderboardScoreUpdateBatch"/>. Call this when this instance is no longer needed.
            /// </summary>
            public void Destroy()
            {
                Pool.Return(_updates);
            }
        }

        /// <summary>
        /// [LeaderboardService -> Game] Communicates a change of state of a specific leaderboard.
        /// </summary>
        public readonly struct LeaderboardStateChange(ulong leaderboardId, ulong instanceId, LeaderboardState state, DateTime activationTime, DateTime expirationTime, bool visible)
            : IGameServiceMessage
        {
            public readonly ulong LeaderboardId = leaderboardId;
            public readonly ulong InstanceId = instanceId;
            public readonly LeaderboardState State = state;
            public readonly DateTime ActivationTime = activationTime;
            public readonly DateTime ExpirationTime = expirationTime;
            public readonly bool Visible = visible;

            public NetMessageLeaderboardStateChange ToProtobuf()
            {
                return NetMessageLeaderboardStateChange.CreateBuilder()
                    .SetLeaderboardId(LeaderboardId)
                    .SetInstanceId(InstanceId)
                    .SetNewState(State)
                    .SetActivationTimestamp(Clock.DateTimeToTimestamp(ActivationTime))
                    .SetExpirationTimestamp(Clock.DateTimeToTimestamp(ExpirationTime))
                    .SetVisible(Visible)
                    .Build();
            }
        }

        /// <summary>
        /// [LeaderboardService -> Game] Container for a batch of <see cref="LeaderboardStateChange"/> instances.
        /// </summary>
        public readonly struct LeaderboardStateChangeList(List<LeaderboardStateChange> list)
            : IGameServiceMessage
        {
            // This is currently used only during server initialization, so it's okay not to pool this.
            public readonly IReadOnlyList<LeaderboardStateChange> List = list;

            public List<LeaderboardStateChange>.Enumerator GetEnumerator()
            {
                return ((List<LeaderboardStateChange>)List).GetEnumerator();
            }
        }

        /// <summary>
        /// [Game -> LeaderboardService] Requests for a list of <see cref="LeaderboardRewardEntry"/> instances for the specified participant.
        /// </summary>
        public readonly struct LeaderboardRewardRequest(ulong participantId)
            : IGameServiceMessage
        {
            public readonly ulong ParticipantId = participantId;
        }

        /// <summary>
        /// [LeaderboardService -> Game] Communicates a reward for the specified participant.
        /// </summary>
        public readonly struct LeaderboardRewardEntry(ulong leaderboardId, ulong instanceId, ulong participantId, ulong rewardId, int rank)
            : IGameServiceMessage
        {
            public readonly ulong LeaderboardId = leaderboardId;
            public readonly ulong InstanceId = instanceId;
            public readonly ulong ParticipantId = participantId;
            public readonly ulong RewardId = rewardId;
            public readonly int Rank = rank;
        }

        /// <summary>
        /// [LeaderboardService -> Game] Container for a batch of <see cref="LeaderboardRewardEntry"/> instances.
        /// </summary>
        public readonly struct LeaderboardRewardRequestResponse(ulong participantId, LeaderboardRewardEntry[] entries)
            : IGameServiceMessage
        {
            // This probably doesn't happen frequently enough to pool
            public readonly ulong ParticipantId = participantId;
            public readonly LeaderboardRewardEntry[] Entries = entries;
        }

        /// <summary>
        /// [Game -> LeaderboardService] Communicates that a reward has been distributed to the specified participant.
        /// </summary>
        public readonly struct LeaderboardRewardConfirmation(ulong leaderboardId, ulong instanceId, ulong participantId)
            : IGameServiceMessage
        {
            public readonly ulong LeaderboardId = leaderboardId;
            public readonly ulong InstanceId = instanceId;
            public readonly ulong ParticipantId = participantId;
        }

        #endregion

        #region Auth

        // WebFrontend -> PlayerManager
        public readonly struct AuthRequest(ulong requestId, LoginDataPB loginDataPB)
            : IGameServiceMessage
        {
            public readonly ulong RequestId = requestId;
            public readonly LoginDataPB LoginDataPB = loginDataPB;
        }

        // PlayerManager -> WebFrontend
        public readonly struct AuthResponse(ulong requestId, int statusCode, AuthTicket authTicket)
            : IGameServiceMessage
        {
            public readonly ulong RequestId = requestId;
            public readonly int StatusCode = statusCode;
            public readonly AuthTicket AuthTicket = authTicket;
        }

        // Frontend -> PlayerManager
        public readonly struct SessionVerificationRequest(IFrontendClient client, ClientCredentials clientCredentials)
            : IGameServiceMessage
        {
            public readonly IFrontendClient Client = client;
            public readonly ClientCredentials ClientCredentials = clientCredentials;
        }

        #endregion

        #region MTXStore

        // WebFrontend -> PlayerManager
        public readonly struct MTXStoreESBalanceRequest(ulong requestId, string email, string token)
            : IGameServiceMessage
        {
            public readonly ulong RequestId = requestId;
            public readonly string Email = email;
            public readonly string Token = token;
        }

        // PlayerManager -> WebFrontend
        public readonly struct MTXStoreESBalanceResponse(ulong requestId, int statusCode, int currentBalance = 0, float conversionRatio = 0, int conversionStep = 0)
            : IGameServiceMessage
        {
            public readonly ulong RequestId = requestId;
            public readonly int StatusCode = statusCode;
            public readonly int CurrentBalance = currentBalance;
            public readonly float ConversionRatio = conversionRatio;
            public readonly int ConversionStep = conversionStep;
        }

        // PlayerManager -> Game
        public readonly struct MTXStoreESBalanceGameRequest(ulong requestId, ulong gameId, ulong playerDbId)
            : IGameServiceMessage
        {
            public readonly ulong RequestId = requestId;
            public readonly ulong GameId = gameId;
            public readonly ulong PlayerDbId = playerDbId;
        }

        // Game -> PlayerManager
        public readonly struct MTXStoreESBalanceGameResponse(ulong requestId, int currentBalance, float conversionRatio, int conversionStep)
            : IGameServiceMessage
        {
            public readonly ulong RequestId = requestId;
            public readonly int CurrentBalance = currentBalance;
            public readonly float ConversionRatio = conversionRatio;
            public readonly int ConversionStep = conversionStep;
        }

        // WebFrontend -> PlayerManager
        public readonly struct MTXStoreESConvertRequest(ulong requestId, string email, string token, int amount)
            : IGameServiceMessage
        {
            public readonly ulong RequestId = requestId;
            public readonly string Email = email;
            public readonly string Token = token;
            public readonly int Amount = amount;
        }

        // PlayerManager -> WebFrontend
        public readonly struct MTXStoreESConvertResponse(ulong requestId, int statusCode)
            : IGameServiceMessage
        {
            public readonly ulong RequestId = requestId;
            public readonly int StatusCode = statusCode;
        }

        // PlayerManager -> Game
        public readonly struct MTXStoreESConvertGameRequest(ulong requestId, ulong gameId, ulong playerDbId, int amount)
            : IGameServiceMessage
        {
            public readonly ulong RequestId = requestId;
            public readonly ulong GameId = gameId;
            public readonly ulong PlayerDbId = playerDbId;
            public readonly int Amount = amount;
        }

        // Game -> PlayerManager
        public readonly struct MTXStoreESConvertGameResponse(ulong requestId, bool result)
            : IGameServiceMessage
        {
            public readonly ulong RequestId = requestId;
            public readonly bool Result = result;
        }

        #endregion
    }
}
