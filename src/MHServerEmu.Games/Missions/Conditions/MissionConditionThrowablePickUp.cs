using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionThrowablePickUp : MissionPlayerCondition
    {
        private MissionConditionThrowablePickUpPrototype _proto;
        private Event<ThrowablePickedUpGameEvent>.Action _throwablePickedUpAction;

        public MissionConditionThrowablePickUp(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // TRBrownstonesSubwayMaggia1
            _proto = prototype as MissionConditionThrowablePickUpPrototype;
            _throwablePickedUpAction = OnThrowablePickedUp;
        }

        private void OnThrowablePickedUp(in ThrowablePickedUpGameEvent evt)
        {
            var player = evt.Player;
            var throwable = evt.Throwable;

            if (player == null || throwable == null || IsMissionPlayer(player) == false) return;
            if (EvaluateEntityFilter(_proto.EntityFilter, throwable) == false) return;

            UpdatePlayerContribution(player);
            SetCompleted();
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.ThrowablePickedUpEvent.AddActionBack(_throwablePickedUpAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.ThrowablePickedUpEvent.RemoveAction(_throwablePickedUpAction);
        }
    }
}
