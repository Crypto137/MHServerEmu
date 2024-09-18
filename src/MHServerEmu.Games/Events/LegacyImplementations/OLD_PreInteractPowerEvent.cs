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
            var preInteractPowerRef = world.PreInteractPower;
            if (preInteractPowerRef == PrototypeId.Invalid) return false;
            Logger.Trace($"OnPreInteractPower {preInteractPowerRef.GetName()}");

            PowerIndexProperties indexProps = new(0, avatar.CharacterLevel, avatar.CombatLevel);
            Power preInteractPower = avatar.AssignPower(preInteractPowerRef, indexProps);

            PowerActivationSettings settings = new(avatar.Id, avatar.RegionLocation.Position, avatar.RegionLocation.Position);
            settings.Flags = PowerActivationSettingsFlags.NotifyOwner;
            avatar.ActivatePower(preInteractPowerRef, ref settings);

            return true;
        }
    }
}
