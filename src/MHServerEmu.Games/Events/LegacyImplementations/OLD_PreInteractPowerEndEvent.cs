using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Entities.Avatars;

namespace MHServerEmu.Games.Events.LegacyImplementations
{
    public class OLD_PreInteractPowerEndEvent : ScheduledEvent
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
            PrototypeId preInteractPower = world.PreInteractPower;
            if (preInteractPower == 0) return false;
            Logger.Trace($"OnPreInteractPowerEnd");
            avatar.UnassignPower(preInteractPower);

            _playerConnection.SendMessage(NetMessageOnPreInteractPowerEnd.CreateBuilder()
                .SetIdTargetEntity(_interactObject.Id)
                .SetAvatarIndex(0)
                .Build());

            return true;
        }
    }
}
