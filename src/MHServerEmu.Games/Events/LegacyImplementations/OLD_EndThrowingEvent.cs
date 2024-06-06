using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Events.LegacyImplementations
{
    public class OLD_EndThrowingEvent : ScheduledEvent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private PlayerConnection _playerConnection;
        private PrototypeId _powerId;

        public void Initialize(PlayerConnection playerConnection, PrototypeId powerId)
        {
            _playerConnection = playerConnection;
            _powerId = powerId;
        }

        public override bool OnTriggered()
        {
            Logger.Trace("Event EndThrowing");

            Avatar avatar = _playerConnection.Player.CurrentAvatar;

            // Remove throwable properties
            avatar.Properties.RemoveProperty(PropertyEnum.ThrowableOriginatorEntity);
            avatar.Properties.RemoveProperty(PropertyEnum.ThrowableOriginatorAssetRef);

            // Unassign throwable and throwable cancel powers
            Power throwablePower = avatar.GetThrowablePower();
            Power throwableCancelPower = avatar.GetThrowableCancelPower();

            avatar.UnassignPower(throwablePower.PrototypeDataRef);
            avatar.UnassignPower(throwableCancelPower.PrototypeDataRef);

            if (GameDatabase.GetPrototypeName(_powerId).Contains("CancelPower"))
            {
                if (_playerConnection.ThrowableEntity != null)
                    _playerConnection.SendMessage(_playerConnection.ThrowableEntity.ToNetMessageEntityCreate());
                Logger.Trace("Event RestoreThrowable");
            }
            else
            {
                _playerConnection.ThrowableEntity?.Kill();
            }

            _playerConnection.ThrowableEntity = null;

            // Notify the client
            _playerConnection.SendMessage(Property.ToNetMessageRemoveProperty(avatar.Properties.ReplicationId, PropertyEnum.ThrowableOriginatorEntity));
            _playerConnection.SendMessage(Property.ToNetMessageRemoveProperty(avatar.Properties.ReplicationId, PropertyEnum.ThrowableOriginatorAssetRef));

            _playerConnection.SendMessage(NetMessagePowerCollectionUnassignPower.CreateBuilder()
                .SetEntityId(avatar.Id)
                .SetPowerProtoId((ulong)throwablePower.PrototypeDataRef)
                .Build());

            _playerConnection.SendMessage(NetMessagePowerCollectionUnassignPower.CreateBuilder()
                .SetEntityId(avatar.Id)
                .SetPowerProtoId((ulong)throwableCancelPower.PrototypeDataRef)
                .Build());

            return true;
        }
    }
}
