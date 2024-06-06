using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;

namespace MHServerEmu.Games.Events.LegacyImplementations
{
    public class OLD_EndMagikUltimateEvent : ScheduledEvent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public PlayerConnection PlayerConnection { get; set; }

        public override bool OnTriggered()
        {
            Player player = PlayerConnection.Player;
            Avatar avatar = player.CurrentAvatar;

            if (avatar.PrototypeDataRef != (PrototypeId)AvatarPrototypeId.Magik)
            {
                // Make sure we still get Magik in case the player switched to another avatar
                Inventory avatarLibrary = player.GetInventory(InventoryConvenienceLabel.AvatarLibrary);
                avatar = avatarLibrary.GetMatchingEntity((PrototypeId)AvatarPrototypeId.Magik) as Avatar;
                if (avatar == null) return Logger.WarnReturn(false, "OnEndMagikUltimate(): avatar == null");
            }

            Condition magikUltimateCondition = avatar.ConditionCollection.GetCondition(777);
            if (magikUltimateCondition == null) return Logger.WarnReturn(false, "OnEndMagikUltimate(): magikUltimateCondition == null");

            Logger.Trace($"EventEnd Magik Ultimate");

            // Remove the ultimate condition
            avatar.ConditionCollection.RemoveCondition(777);

            /*
            // TODO: Removed the hotspot effect power from the arena's power collection

            // Destroy the arena entity
            ulong arenaEntityId = playerConnection.MagikUltimateEntityId;
            var entity = _game.EntityManager.GetEntityById(arenaEntityId);
            entity?.Destroy();
            */

            // Notify the client
            PlayerConnection.SendMessage(NetMessageDeleteCondition.CreateBuilder()
                .SetIdEntity(avatar.Id)
                .SetKey(777)
                .Build());

            /*
            playerConnection.SendMessage(NetMessagePowerCollectionUnassignPower.CreateBuilder()
                .SetEntityId(arenaEntityId)
                .SetPowerProtoId((ulong)PowerPrototypes.Magik.UltimateHotspotEffect)
                .Build());

            playerConnection.SendMessage(NetMessageEntityDestroy.CreateBuilder()
                .SetIdEntity(arenaEntityId)
                .Build());
            */

            return true;
        }
    }
}
