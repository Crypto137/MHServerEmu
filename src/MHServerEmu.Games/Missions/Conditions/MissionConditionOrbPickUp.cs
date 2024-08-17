using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionOrbPickUp : MissionPlayerCondition
    {
        private MissionConditionOrbPickUpPrototype _proto;
        private Action<OrbPickUpEvent> _orbPickUpAction;
        protected override long RequiredCount => _proto.Count;

        public MissionConditionOrbPickUp(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // TimesBehaviorController
            _proto = prototype as MissionConditionOrbPickUpPrototype;
            _orbPickUpAction = OnOrbPickUp;
        }

        private void OnOrbPickUp(OrbPickUpEvent evt)
        {
            var player = evt.Player;
            var orb = evt.Orb;

            if (player == null || orb == null || IsMissionPlayer(player) == false) return;
            if (EvaluateEntityFilter(_proto.EntityFilter, orb) == false) return;

            UpdatePlayerContribution(player);
            SetCompleted();
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.OrbPickUpEvent.AddActionBack(_orbPickUpAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.OrbPickUpEvent.RemoveAction(_orbPickUpAction);
        }
    }
}
