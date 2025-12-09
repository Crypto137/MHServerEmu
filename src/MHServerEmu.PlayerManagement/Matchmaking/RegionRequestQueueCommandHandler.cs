using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.GameData;
using MHServerEmu.PlayerManagement.Players;
using MHServerEmu.PlayerManagement.Social;

namespace MHServerEmu.PlayerManagement.Matchmaking
{
    /// <summary>
    /// Handles <see cref="RegionRequestQueueCommandVar"/> commands received from players in game instances.
    /// </summary>
    public class RegionRequestQueueCommandHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly PlayerHandle _player;

        public RegionRequestQueueCommandHandler(PlayerHandle player)
        {
            _player = player;
        }

        public void HandleCommand(PrototypeId regionRef, PrototypeId difficultyTierRef, PrototypeId metaStateRef,
            RegionRequestQueueCommandVar command, ulong regionRequestGroupId, ulong targetPlayerDbId)
        {
            Logger.Trace($"HandleCommand(): command=[{command}], region=[{regionRef.GetNameFormatted()}], player=[{_player}]");

            switch (command)
            {
                case RegionRequestQueueCommandVar.eRRQC_AddToQueueSolo:
                case RegionRequestQueueCommandVar.eRRQC_AddToQueueParty:
                case RegionRequestQueueCommandVar.eRRQC_AddToQueueBypass:
                    OnAddToQueue(regionRef, difficultyTierRef, metaStateRef, command);
                    break;

                case RegionRequestQueueCommandVar.eRRQC_RemoveFromQueue:
                    OnRemoveFromQueue();
                    break;

                case RegionRequestQueueCommandVar.eRRQC_MatchInviteAccept:
                case RegionRequestQueueCommandVar.eRRQC_MatchInviteDecline:
                    OnMatchInviteResponse(command == RegionRequestQueueCommandVar.eRRQC_MatchInviteAccept);
                    break;

                case RegionRequestQueueCommandVar.eRRQC_RequestToJoinGroup:
                    OnRequestToJoinGroup(targetPlayerDbId);
                    break;

                default:
                    Logger.Warn($"HandleCommand(): Unhandled command {command} from player [{_player}]");
                    break;
            }
        }

        private bool OnAddToQueue(PrototypeId regionRef, PrototypeId difficultyTierRef, PrototypeId metaStateRef, RegionRequestQueueCommandVar command)
        {
            RegionRequestQueue queue = PlayerManagerService.Instance.RegionRequestQueueManager.GetRegionRequestQueue(regionRef);
            if (queue == null)
                return Logger.WarnReturn(false, $"OnAddToQueue(): Player [{_player}] attempted to enter queue for non-queue region [{regionRef.GetName()}]");

            if (difficultyTierRef == PrototypeId.Invalid)
            {
                OnRemoveFromQueue();
                return true;
            }

            RegionTransferFailure canEnterRegion = _player.CanEnterRegion(queue.Prototype, true);
            if (canEnterRegion != RegionTransferFailure.eRTF_NoError)
            {
                SendMatchQueueStatusError(regionRef, difficultyTierRef, canEnterRegion);
                return true;
            }

            // Create region request group
            MasterParty party = command == RegionRequestQueueCommandVar.eRRQC_AddToQueueParty ? _player.CurrentParty : null;
            RegionRequestQueueParams queueParams = new(difficultyTierRef, metaStateRef, command == RegionRequestQueueCommandVar.eRRQC_AddToQueueBypass);
            RegionRequestGroup group = RegionRequestGroup.Create(queue, queueParams, _player, party);

            if (group == null)
                return Logger.WarnReturn(false, $"OnAddToQueue(): Failed to create region request group for player [{_player}]");

            return true;
        }

        private void OnRemoveFromQueue()
        {
            RegionRequestGroup group = _player.RegionRequestGroup;
            group?.RemovePlayer(_player);
        }

        private void OnMatchInviteResponse(bool response)
        {
            _player.RegionRequestGroup?.ReceiveMatchInviteResponse(_player, response);
        }

        private void OnRequestToJoinGroup(ulong targetPlayerDbId)
        {
            if (targetPlayerDbId == 0)
                return;

            PlayerHandle targetPlayer = PlayerManagerService.Instance.ClientManager.GetPlayer(targetPlayerDbId);
            targetPlayer?.RegionRequestGroup?.AddPlayer(_player);
        }

        private void SendMatchQueueStatusError(PrototypeId regionRef, PrototypeId difficultyTierRef, RegionTransferFailure failure)
        {
            if (_player.State != PlayerHandleState.InGame)
                return;

            RegionRequestQueueUpdateVar status;

            switch (failure)
            {
                case RegionTransferFailure.eRTF_Full:
                    status = RegionRequestQueueUpdateVar.eRRQ_PartyTooLarge;
                    break;

                case RegionTransferFailure.eRTF_RaidsNotAllowed:
                    status = RegionRequestQueueUpdateVar.eRRQ_RaidNotAllowed;
                    break;

                default:
                    return;
            }

            ulong gameId = _player.CurrentGame.Id;
            ulong playerDbId = _player.PlayerDbId;
            ulong regionProtoId = (ulong)regionRef;
            ulong difficultyTierProtoId = (ulong)difficultyTierRef;
            int playersInQueue = 0;
            ulong regionRequestGroupId = 0;

            ServiceMessage.MatchQueueUpdate message = new(gameId, playerDbId, regionProtoId, difficultyTierProtoId,
                playersInQueue, regionRequestGroupId, new());

            ulong updatePlayerGuid = _player.PlayerDbId;
            string updatePlayerName = null;

            ServiceMessage.MatchQueueUpdateData updatePlayerData = new(updatePlayerGuid, status, updatePlayerName);
            message.Data.Add(updatePlayerData);

            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, message);
        }
    }
}
