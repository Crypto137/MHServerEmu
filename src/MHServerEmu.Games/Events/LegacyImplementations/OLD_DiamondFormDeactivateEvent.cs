using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Events.LegacyImplementations
{
    public class OLD_DiamondFormDeactivateEvent : ScheduledEvent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public PlayerConnection PlayerConnection { get; set; }

        public override bool OnTriggered()
        {
            Avatar avatar = PlayerConnection.Player.CurrentAvatar;

            // TODO: get DiamondFormCondition Condition Key
            if (avatar.ConditionCollection.GetCondition(111) == null) return false;

            Logger.Trace($"EventEnd EmmaDiamondForm");

            // Remove the condition server-side
            avatar.ConditionCollection.RemoveCondition(111);

            // Notify the client
            PlayerConnection.SendMessage(NetMessageDeleteCondition.CreateBuilder()
              .SetKey(111)
              .SetIdEntity(avatar.Id)
              .Build());

            return true;
        }
    }
}
