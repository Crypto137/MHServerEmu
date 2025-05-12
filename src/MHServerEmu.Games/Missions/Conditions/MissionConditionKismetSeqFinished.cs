using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionKismetSeqFinished : MissionPlayerCondition
    {

        private MissionConditionKismetSeqFinishedPrototype _proto;
        private Event<KismetSeqFinishedGameEvent>.Action _kismetSeqFinishedAction;

        public MissionConditionKismetSeqFinished(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // RaftNPETutorialPurpleOrbController
            _proto = prototype as MissionConditionKismetSeqFinishedPrototype;
            _kismetSeqFinishedAction = OnKismetSeqFinished;
        }

        private void OnKismetSeqFinished(in KismetSeqFinishedGameEvent evt)
        {
            var player = evt.Player;
            var kismetSeqRef = evt.KismetSeqRef;

            if (player == null || IsMissionPlayer(player) == false) return;
            if (_proto.KismetSeqPrototype != kismetSeqRef) return;

            UpdatePlayerContribution(player);
            SetCompleted();
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.KismetSeqFinishedEvent.AddActionBack(_kismetSeqFinishedAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.KismetSeqFinishedEvent.RemoveAction(_kismetSeqFinishedAction);
        }
    }
}
