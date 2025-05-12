using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionRemoteNotification : MissionPlayerCondition
    {
        private Event<NotificationInteractGameEvent>.Action _notificationInteractAction;

        public MissionConditionRemoteNotification(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // AxisRaidBreadcrumb
            _notificationInteractAction = OnNotificationInteract;
        }

        private void OnNotificationInteract(in NotificationInteractGameEvent evt)
        {
            var player = evt.Player;
            var missionRef = evt.MissionRef;

            if (missionRef != PrototypeId.Invalid && missionRef != Mission.PrototypeDataRef) return;
            if (player == null || IsMissionPlayer(player) == false) return;

            // Check avatarLevel for Notification
            if (Mission.State != MissionState.Active)
            {
                int avatarCharacterLevel = player.CurrentAvatarCharacterLevel;
                var missionProto = Mission.Prototype;
                if (missionProto.Level - avatarCharacterLevel >= MissionManager.MissionLevelUpperBoundsOffset()) return;
            }

            UpdatePlayerContribution(player);
            Count++;
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.NotificationInteractEvent.AddActionBack(_notificationInteractAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.NotificationInteractEvent.RemoveAction(_notificationInteractAction);
        }
    }
}
