using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
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

            // TODO: Validate

            // Create region request group
            MasterParty party = command == RegionRequestQueueCommandVar.eRRQC_AddToQueueParty ? _player.CurrentParty : null;
            RegionRequestQueueParams queueParams = new(difficultyTierRef, metaStateRef, command == RegionRequestQueueCommandVar.eRRQC_AddToQueueBypass);
            RegionRequestGroup group = RegionRequestGroup.Create(queue, queueParams, _player, party);

            if (group == null)
                return Logger.WarnReturn(false, $"OnAddToQueue(): Failed to create region request group for player [{_player}]");

            return true;
        }

        private void OnGroupInviteResponse()
        {

        }

        private void OnRemoveFromQueue()
        {
            RegionRequestGroup group = _player.RegionRequestGroup;
            if (group == null)
                return;

            group.RemovePlayer(_player);
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
    }
}
