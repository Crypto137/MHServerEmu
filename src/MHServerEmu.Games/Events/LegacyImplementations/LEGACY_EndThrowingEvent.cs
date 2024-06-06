using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Events.LegacyImplementations
{
    public class LEGACY_EndThrowingEvent : ScheduledEvent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public PlayerConnection PlayerConnection { get; set; }
        public PrototypeId PowerId { get; set; }

        public override void OnTriggered()
        {
            Logger.Trace("Event EndThrowing");

            Avatar avatar = PlayerConnection.Player.CurrentAvatar;

            // Remove throwable properties
            avatar.Properties.RemoveProperty(PropertyEnum.ThrowableOriginatorEntity);
            avatar.Properties.RemoveProperty(PropertyEnum.ThrowableOriginatorAssetRef);

            // Unassign throwable and throwable cancel powers
            Power throwablePower = avatar.GetThrowablePower();
            Power throwableCancelPower = avatar.GetThrowableCancelPower();

            avatar.UnassignPower(throwablePower.PrototypeDataRef);
            avatar.UnassignPower(throwableCancelPower.PrototypeDataRef);

            if (GameDatabase.GetPrototypeName(PowerId).Contains("CancelPower"))
            {
                if (PlayerConnection.ThrowableEntity != null)
                    PlayerConnection.SendMessage(PlayerConnection.ThrowableEntity.ToNetMessageEntityCreate());
                Logger.Trace("Event RestoreThrowable");
            }
            else
            {
                PlayerConnection.ThrowableEntity?.Kill();
            }

            PlayerConnection.ThrowableEntity = null;

            // Notify the client
            PlayerConnection.SendMessage(Property.ToNetMessageRemoveProperty(avatar.Properties.ReplicationId, PropertyEnum.ThrowableOriginatorEntity));
            PlayerConnection.SendMessage(Property.ToNetMessageRemoveProperty(avatar.Properties.ReplicationId, PropertyEnum.ThrowableOriginatorAssetRef));

            PlayerConnection.SendMessage(NetMessagePowerCollectionUnassignPower.CreateBuilder()
                .SetEntityId(avatar.Id)
                .SetPowerProtoId((ulong)throwablePower.PrototypeDataRef)
                .Build());

            PlayerConnection.SendMessage(NetMessagePowerCollectionUnassignPower.CreateBuilder()
                .SetEntityId(avatar.Id)
                .SetPowerProtoId((ulong)throwableCancelPower.PrototypeDataRef)
                .Build());
        }

        public override void OnCancelled() { }
    }
}
