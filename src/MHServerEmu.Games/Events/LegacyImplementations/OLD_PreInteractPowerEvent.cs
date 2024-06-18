using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.PowerCollections;

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
            Avatar avatar = _playerConnection.Player.CurrentAvatar;
            var world = _interactObject.Prototype as WorldEntityPrototype;
            if (world == null) return false;
            var preInteractPower = world.PreInteractPower;
            if (preInteractPower == PrototypeId.Invalid) return false;
            Logger.Trace($"OnPreInteractPower {GameDatabase.GetPrototypeName(preInteractPower)}");

            PowerIndexProperties indexProps = new(0, avatar.CharacterLevel, avatar.CombatLevel);
            avatar.AssignPower(world.PreInteractPower, indexProps);

            ActivatePowerArchive activatePower = new()
            {
                ReplicationPolicy = AOINetworkPolicyValues.AOIChannelProximity,
                Flags = ActivatePowerMessageFlags.HasTriggeringPowerPrototypeRef | ActivatePowerMessageFlags.TargetPositionIsUserPosition | ActivatePowerMessageFlags.HasPowerRandomSeed | ActivatePowerMessageFlags.HasFXRandomSeed,
                UserEntityId = avatar.Id,
                TargetEntityId = 0,
                PowerPrototypeRef = preInteractPower,
                UserPosition = avatar.RegionLocation.Position,
                PowerRandomSeed = 2222,
                FXRandomSeed = 2222
            };

            avatar.Game.NetworkManager.SendMessageToInterested(activatePower.ToProtobuf(), avatar, AOINetworkPolicyValues.AOIChannelProximity);
            return true;
        }
    }
}
