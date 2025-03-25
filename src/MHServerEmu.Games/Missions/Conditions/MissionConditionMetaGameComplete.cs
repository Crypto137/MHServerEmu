using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionMetaGameComplete : MissionPlayerCondition
    {
        private MissionConditionMetaGameCompletePrototype _proto;
        private Event<PlayerMetaGameCompleteGameEvent>.Action _playerMetaGameCompleteAction;
        protected override long RequiredCount => _proto.Count;

        public MissionConditionMetaGameComplete(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // PvPDefenderDefeatTier1
            _proto = prototype as MissionConditionMetaGameCompletePrototype;
            _playerMetaGameCompleteAction = OnPlayerMetaGameComplete;
        }

        private void OnPlayerMetaGameComplete(in PlayerMetaGameCompleteGameEvent evt)
        {
            var player = evt.Player;
            var metaGameRef = evt.MetaGameRef;
            var completeType = evt.CompleteType;

            if (player == null || IsMissionPlayer(player) == false) return;
            if (_proto.MetaGame != metaGameRef) return;
            if (_proto.CompleteType != completeType) return;

            UpdatePlayerContribution(player);
            Count++;
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.PlayerMetaGameCompleteEvent.AddActionBack(_playerMetaGameCompleteAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.PlayerMetaGameCompleteEvent.RemoveAction(_playerMetaGameCompleteAction);
        }
    }
}
