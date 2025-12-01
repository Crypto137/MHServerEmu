using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.GameData;
using MHServerEmu.PlayerManagement.Players;

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
            Logger.Debug($"HandleCommand(): {command}");

            // REMOVEME: debug command handling
            ulong gameId = _player.CurrentGame.Id;
            ulong playerDbId = _player.PlayerDbId;

            ServiceMessage.MatchQueueUpdate message = new(_player.CurrentGame.Id, playerDbId, (ulong)regionRef,
                (ulong)difficultyTierRef, 0, regionRequestGroupId, new());

            switch (command)
            {
                case RegionRequestQueueCommandVar.eRRQC_AddToQueueSolo:
                case RegionRequestQueueCommandVar.eRRQC_AddToQueueParty:
                case RegionRequestQueueCommandVar.eRRQC_AddToQueueBypass:
                    message.Data.Add(new(playerDbId, RegionRequestQueueUpdateVar.eRRQ_WaitingInQueue));
                    break;

                case RegionRequestQueueCommandVar.eRRQC_RemoveFromQueue:
                    message.Data.Add(new(playerDbId, RegionRequestQueueUpdateVar.eRRQ_RemovedFromGroup));
                    break;
            }

            if (message.Data.Count > 0)
                ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, message);
        }

    }
}
