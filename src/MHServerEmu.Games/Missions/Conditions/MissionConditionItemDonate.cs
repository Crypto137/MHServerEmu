using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionItemDonate : MissionPlayerCondition
    {
        private MissionConditionItemDonatePrototype _proto;
        private Action<PlayerDonatedItemGameEvent> _playerDonatedItemAction;
        protected override long RequiredCount => _proto.Count;

        public MissionConditionItemDonate(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            _proto = prototype as MissionConditionItemDonatePrototype;
            _playerDonatedItemAction = OnPlayerDonatedItem;
        }

        private void OnPlayerDonatedItem(PlayerDonatedItemGameEvent evt)
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
            region.PlayerDonatedItemEvent.AddActionBack(_playerDonatedItemAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.PlayerDonatedItemEvent.RemoveAction(_playerDonatedItemAction);
        }
    }
}
