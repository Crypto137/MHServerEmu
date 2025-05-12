using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionItemEquip : MissionPlayerCondition
    {
        private MissionConditionItemEquipPrototype _proto;
        private Event<PlayerEquippedItemGameEvent>.Action _playerEquippedItemAction;

        public MissionConditionItemEquip(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // DevelopmentOnly testTutorial
            _proto = prototype as MissionConditionItemEquipPrototype;
            _playerEquippedItemAction = OnPlayerEquippedItem;
        }

        private void OnPlayerEquippedItem(in PlayerEquippedItemGameEvent evt)
        {
            var player = evt.Player;
            var item = evt.Item;

            if (player == null || item == null || IsMissionPlayer(player) == false) return;
            if (item.IsAPrototype(_proto.ItemPrototype) == false) return;

            UpdatePlayerContribution(player);
            SetCompleted();
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.PlayerEquippedItemEvent.AddActionBack(_playerEquippedItemAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.PlayerEquippedItemEvent.RemoveAction(_playerEquippedItemAction);
        }
    }
}
