using Gazillion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;

namespace MHServerEmu.Games.Events.LegacyImplementations
{
    public class OLD_EmoteDanceEvent : ScheduledEvent
    {
        public PlayerConnection PlayerConnection { get; set; }

        public override bool OnTriggered()
        {
            ulong avatarEntityId = PlayerConnection.Player.CurrentAvatar.Id;
            ActivatePowerArchive activatePower = new()
            {
                ReplicationPolicy = AOINetworkPolicyValues.AOIChannelProximity,
                Flags = ActivatePowerMessageFlags.HasTriggeringPowerPrototypeRef | ActivatePowerMessageFlags.TargetPositionIsUserPosition | ActivatePowerMessageFlags.HasPowerRandomSeed | ActivatePowerMessageFlags.HasFXRandomSeed,
                UserEntityId = avatarEntityId,
                TargetEntityId = avatarEntityId,
                PowerPrototypeRef = (PrototypeId)PowerPrototypes.Emotes.EmoteDance,
                UserPosition = PlayerConnection.LastPosition,
                PowerRandomSeed = 1111,
                FXRandomSeed = 1111
            };

            PlayerConnection.SendMessage(NetMessageActivatePower.CreateBuilder()
                .SetArchiveData(activatePower.ToByteString())
                .Build());

            return true;
        }
    }
}
