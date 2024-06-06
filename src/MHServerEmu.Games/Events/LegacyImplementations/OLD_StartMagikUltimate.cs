using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;

namespace MHServerEmu.Games.Events.LegacyImplementations
{
    public class OLD_StartMagikUltimate : ScheduledEvent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private PlayerConnection _playerConnection;
        private NetStructPoint3 _position;

        public void Initialize(PlayerConnection playerConnection, NetStructPoint3 position)
        {
            _playerConnection = playerConnection;
            _position = position;
        }

        public override bool OnTriggered()
        {
            Avatar avatar = _playerConnection.Player.CurrentAvatar;

            Condition magikUltimateCondition = avatar.ConditionCollection.GetCondition(777);
            if (magikUltimateCondition != null) return Logger.WarnReturn(false, "OnStartMagikUltimate(): magikUltimateCondition != null");

            Logger.Trace($"EventStart Magik Ultimate");

            // Create and add a condition for the ultimate
            magikUltimateCondition = avatar.ConditionCollection.AllocateCondition();
            magikUltimateCondition.InitializeFromPowerMixinPrototype(777, (PrototypeId)PowerPrototypes.Magik.Ultimate, 0, TimeSpan.FromMilliseconds(20000));
            avatar.ConditionCollection.AddCondition(magikUltimateCondition);

            /*
            // Create the arena entity
            WorldEntity arenaEntity = _game.EntityManager.CreateWorldEntityEmpty(
                playerConnection.AOI.Region.Id,
                (PrototypeId)PowerPrototypes.Magik.UltimateArea,
                new(position.X, position.Y, position.Z), new());


            // Save the entity id for the arena entity (we need to store this state in the avatar entity instead)
            playerConnection.MagikUltimateEntityId = arenaEntity.Id;
            */

            // Notify the client
            AddConditionArchive conditionArchive = new()
            {
                ReplicationPolicy = AOINetworkPolicyValues.DefaultPolicy,
                EntityId = avatar.Id,
                Condition = magikUltimateCondition
            };

            _playerConnection.SendMessage(NetMessageAddCondition.CreateBuilder()
                .SetArchiveData(conditionArchive.SerializeToByteString())
                .Build());

            /*
            playerConnection.SendMessage(arenaEntity.ToNetMessageEntityCreate());

            playerConnection.SendMessage(NetMessagePowerCollectionAssignPower.CreateBuilder()
                .SetEntityId(arenaEntity.Id)
                .SetPowerProtoId((ulong)PowerPrototypes.Magik.UltimateHotspotEffect)
                .SetPowerRank(0)
                .SetCharacterLevel(60)
                .SetCombatLevel(60)
                .SetItemLevel(1)
                .SetItemVariation(1)
                .Build());

            playerConnection.SendMessage(Property.ToNetMessageSetProperty(arenaEntity.Properties.ReplicationId, new(PropertyEnum.AttachedToEntityId), avatar.Id));
            */

            return true;
        }
    }
}
