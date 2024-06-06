using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;

namespace MHServerEmu.Games.Events.LegacyImplementations
{
    public class OLD_PreInteractPowerEvent : ScheduledEvent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private PlayerConnection _playerConnection;
        private Entity _interactObject;

        public void Initialize(PlayerConnection playerConnection, Entity interactObject)
        {
            _playerConnection = playerConnection;
            _interactObject = interactObject;
        }

        public override bool OnTriggered()
        {
            ulong avatarEntityId = _playerConnection.Player.CurrentAvatar.Id;
            var world = _interactObject.Prototype as WorldEntityPrototype;
            if (world == null) return false;
            var preIteractPower = world.PreInteractPower;
            if (preIteractPower == PrototypeId.Invalid) return false;
            Logger.Trace($"OnPreInteractPower {GameDatabase.GetPrototypeName(preIteractPower)}");

            _playerConnection.SendMessage(NetMessagePowerCollectionAssignPower.CreateBuilder()
                .SetEntityId(avatarEntityId)
                .SetPowerProtoId((ulong)preIteractPower)
                .SetPowerRank(0)
                .SetCharacterLevel(60)
                .SetCombatLevel(60)
                .SetItemLevel(1)
                .SetItemVariation(1)
                .Build());

            ActivatePowerArchive activatePower = new()
            {
                ReplicationPolicy = AOINetworkPolicyValues.AOIChannelProximity,
                Flags = ActivatePowerMessageFlags.HasTriggeringPowerPrototypeRef | ActivatePowerMessageFlags.TargetPositionIsUserPosition | ActivatePowerMessageFlags.HasPowerRandomSeed | ActivatePowerMessageFlags.HasFXRandomSeed,
                UserEntityId = avatarEntityId,
                TargetEntityId = 0,
                PowerPrototypeRef = preIteractPower,
                UserPosition = _playerConnection.LastPosition,
                PowerRandomSeed = 2222,
                FXRandomSeed = 2222
            };

            _playerConnection.SendMessage(NetMessageActivatePower.CreateBuilder()
                 .SetArchiveData(activatePower.ToByteString())
                 .Build());

            return true;
        }
    }
}
