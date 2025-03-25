using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionItemBuy : MissionPlayerCondition
    {
        private MissionConditionItemBuyPrototype _proto;
        private Event<PlayerBoughtItemGameEvent>.Action _playerBoughtItemAction;
        protected override long RequiredCount => _proto.Count;

        public MissionConditionItemBuy(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // NotInGame TutorialEternitySplinters
            _proto = prototype as MissionConditionItemBuyPrototype;
            _playerBoughtItemAction = OnPlayerBoughtItem;
        }

        private void OnPlayerBoughtItem(in PlayerBoughtItemGameEvent evt)
        {
            var player = evt.Player;
            var item = evt.Item;
            int count = evt.Count;
            if (player == null || item == null || count <= 0 || IsMissionPlayer(player) == false) return;
            if (EvaluateEntityFilter(_proto.EntityFilter, item) == false) return;
            UpdatePlayerContribution(player, count);
            Count += count;
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.PlayerBoughtItemEvent.AddActionBack(_playerBoughtItemAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.PlayerBoughtItemEvent.RemoveAction(_playerBoughtItemAction);
        }
    }
}
