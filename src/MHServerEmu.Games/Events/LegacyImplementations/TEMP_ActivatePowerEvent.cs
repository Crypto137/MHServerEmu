using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;

namespace MHServerEmu.Games.Events.LegacyImplementations
{
    public class TEMP_ActivatePowerEvent : ScheduledEvent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private PlayerConnection _playerConnection;
        private PrototypeId _powerProtoRef;

        public void Initialize(PlayerConnection playerConnection, PrototypeId powerProtoRef)
        {
            _playerConnection = playerConnection;
            _powerProtoRef = powerProtoRef;
        }

        public override bool OnTriggered()
        {
            Avatar avatar = _playerConnection.Player.CurrentAvatar;

            Logger.Trace($"Activating {GameDatabase.GetPrototypeName(_powerProtoRef)} for {avatar}");

            ActivatePowerArchive activatePower = new()
            {
                Flags = ActivatePowerMessageFlags.TargetIsUser | ActivatePowerMessageFlags.HasTargetPosition |
                ActivatePowerMessageFlags.TargetPositionIsUserPosition | ActivatePowerMessageFlags.HasFXRandomSeed |
                ActivatePowerMessageFlags.HasPowerRandomSeed,

                PowerPrototypeRef = _powerProtoRef,
                UserEntityId = avatar.Id,
                TargetPosition = _playerConnection.LastPosition,
                FXRandomSeed = (uint)_playerConnection.Game.Random.Next(),
                PowerRandomSeed = (uint)_playerConnection.Game.Random.Next()
            };

            _playerConnection.SendMessage(NetMessageActivatePower.CreateBuilder()
                .SetArchiveData(activatePower.ToByteString())
                .Build());

            return true;
        }
    }
}
