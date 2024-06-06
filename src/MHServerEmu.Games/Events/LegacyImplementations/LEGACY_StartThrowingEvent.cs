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
    public class LEGACY_StartThrowingEvent : ScheduledEvent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public PlayerConnection PlayerConnection { get; set; }
        public ulong TargetId { get; set; }

        public override void OnTriggered()
        {
            Logger.Trace($"Event StartThrowing");

            Avatar avatar = PlayerConnection.Player.CurrentAvatar;

            PlayerConnection.ThrowableEntity = PlayerConnection.Game.EntityManager.GetEntity<Entity>(TargetId);
            if (PlayerConnection.ThrowableEntity == null) return;

            Logger.Trace($"{GameDatabase.GetPrototypeName(PlayerConnection.ThrowableEntity.PrototypeDataRef)}");

            var throwableProto = PlayerConnection.ThrowableEntity.Prototype as WorldEntityPrototype;
            if (throwableProto == null) return;

            // Set throwable properties on the avatar
            avatar.Properties[PropertyEnum.ThrowableOriginatorEntity] = TargetId;
            avatar.Properties[PropertyEnum.ThrowableOriginatorAssetRef] = throwableProto.UnrealClass;

            // Assign throwable and throwable can powers to the avatar's power collection
            PrototypeId throwablePowerRef = throwableProto.Properties[PropertyEnum.ThrowablePower];
            PrototypeId throwableCancelPowerRef = throwableProto.Properties[PropertyEnum.ThrowableRestorePower];

            PowerIndexProperties indexProps = new(0, avatar.CharacterLevel, avatar.CombatLevel);
            avatar.AssignPower(throwablePowerRef, indexProps);
            avatar.AssignPower(throwableCancelPowerRef, indexProps);

            // Notify the client
            PlayerConnection.SendMessage(Property.ToNetMessageSetProperty(avatar.Properties.ReplicationId, PropertyEnum.ThrowableOriginatorEntity, TargetId));

            PlayerConnection.SendMessage(Property.ToNetMessageSetProperty(avatar.Properties.ReplicationId, PropertyEnum.ThrowableOriginatorAssetRef, throwableProto.UnrealClass));

            PlayerConnection.SendMessage(NetMessagePowerCollectionAssignPower.CreateBuilder()
                .SetEntityId(avatar.Id)
                .SetPowerProtoId((ulong)throwableCancelPowerRef)
                .SetPowerRank(indexProps.PowerRank)
                .SetCharacterLevel(indexProps.CharacterLevel)
                .SetCombatLevel(indexProps.CombatLevel)
                .SetItemLevel(indexProps.ItemLevel)
                .SetItemVariation(indexProps.ItemVariation)
                .Build());

            PlayerConnection.SendMessage(NetMessagePowerCollectionAssignPower.CreateBuilder()
                .SetEntityId(avatar.Id)
                .SetPowerProtoId((ulong)throwablePowerRef)
                .SetPowerRank(indexProps.PowerRank)
                .SetCharacterLevel(indexProps.CharacterLevel)
                .SetCombatLevel(indexProps.CombatLevel)
                .SetItemLevel(indexProps.ItemLevel)
                .SetItemVariation(indexProps.ItemVariation)
                .Build());

            PlayerConnection.SendMessage(NetMessageEntityDestroy.CreateBuilder()
                .SetIdEntity(TargetId)
                .Build());
        }

        public override void OnCancelled() { }
    }
}
