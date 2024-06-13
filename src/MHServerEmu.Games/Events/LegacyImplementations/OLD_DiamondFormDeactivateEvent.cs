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

            // Remove the diamond form condition
            avatar.ConditionCollection.RemoveCondition(111);

            return true;
        }
    }
}
