using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionStoryNotification : MissionAction
    {
        private MissionActionStoryNotificationPrototype _proto;
        public MissionActionStoryNotification(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // KaZarController
            _proto = prototype as MissionActionStoryNotificationPrototype;
        }

        public override void Run()
        {
            foreach (Player player in GetDistributors(_proto.SendTo))
                player.SendStoryNotification(_proto.StoryNotification, MissionRef);
        }
    }
}
