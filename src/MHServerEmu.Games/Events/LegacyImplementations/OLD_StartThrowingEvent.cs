using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.PowerCollections;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Events.LegacyImplementations
{
    public class OLD_StartThrowingEvent : ScheduledEvent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private PlayerConnection _playerConnection;
        private ulong _targetId;
        
        public void Initialize(PlayerConnection playerConnection, ulong targetId)
        {
            _playerConnection = playerConnection;
            _targetId = targetId;
        }

        public override bool OnTriggered()
        {
            Logger.Trace($"Event StartThrowing");

            Avatar avatar = _playerConnection.Player.CurrentAvatar;

            _playerConnection.ThrowableEntity = _playerConnection.Game.EntityManager.GetEntity<Entity>(_targetId);
            if (_playerConnection.ThrowableEntity == null) return false;

            Logger.Trace($"{GameDatabase.GetPrototypeName(_playerConnection.ThrowableEntity.PrototypeDataRef)}");

            var throwableProto = _playerConnection.ThrowableEntity.Prototype as WorldEntityPrototype;
            if (throwableProto == null) return false;

            // Set throwable properties on the avatar
            avatar.Properties[PropertyEnum.ThrowableOriginatorEntity] = _targetId;
            avatar.Properties[PropertyEnum.ThrowableOriginatorAssetRef] = throwableProto.UnrealClass;

            // Assign throwable and throwable can powers to the avatar's power collection
            PrototypeId throwablePowerRef = throwableProto.Properties[PropertyEnum.ThrowablePower];
            PrototypeId throwableCancelPowerRef = throwableProto.Properties[PropertyEnum.ThrowableRestorePower];

            PowerIndexProperties indexProps = new(0, avatar.CharacterLevel, avatar.CombatLevel);
            avatar.AssignPower(throwablePowerRef, indexProps);
            avatar.AssignPower(throwableCancelPowerRef, indexProps);

            // Notify the client

            _playerConnection.SendMessage(NetMessageEntityDestroy.CreateBuilder()
                .SetIdEntity(_targetId)
                .Build());

            return true;
        }
    }
}
