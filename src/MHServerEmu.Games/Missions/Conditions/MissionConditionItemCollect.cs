using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionItemCollect : MissionPlayerCondition
    {
        private MissionConditionItemCollectPrototype _proto;
        private Action<PlayerPreItemPickupGameEvent> _playerPreItemPickupAction;
        private Action<PlayerCollectedItemGameEvent> _playerCollectedItemAction;
        private Action<PlayerLostItemGameEvent> _playerLostItemAction;

        protected override long RequiredCount => _proto.Count;

        public MissionConditionItemCollect(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // RaftNPETutorialPurpleOrbController
            _proto = prototype as MissionConditionItemCollectPrototype;
            _playerPreItemPickupAction = OnPlayerPreItemPickup;
            _playerCollectedItemAction = OnPlayerCollectedItem;
            _playerLostItemAction = OnPlayerLostItem;
        }

        public override bool OnReset()
        {
            long count = 0;
            if (_proto.CountItemsOnMissionStart)
            {
                var manager = Game.EntityManager;

                List<Player> participants = ListPool<Player>.Instance.Get();
                if (Mission.GetParticipants(participants))
                {
                    foreach (var player in participants)
                    {
                        foreach (Inventory inventory in new InventoryIterator(player))
                        {
                            if (inventory == null) continue;
                            var inventoryProto = inventory.Prototype;
                            if (inventoryProto.IsPlayerGeneralInventory || inventoryProto.IsEquipmentInventory)
                            {
                                foreach (var entry in inventory)
                                {
                                    var item = manager.GetEntity<Item>(entry.Id);
                                    if (EvaluateItem(player, item))
                                        count += item.CurrentStackSize;
                                }
                            }
                        }
                    }
                }
                ListPool<Player>.Instance.Return(participants);
            }

            SetCount(count);
            return true;
        }

        private bool EvaluateItem(Player player, Item item)
        {
            if (player == null || item == null || IsMissionPlayer(player) == false) return false;
            if (EvaluateEntityFilter(_proto.EntityFilter, item) == false) return false;
            if (_proto.MustBeEquippableByAvatar)
            {
                var avatar = player.CurrentAvatar;
                if (avatar == null) return false;
                PropertyEnum prop = 0;
                if (avatar.CanEquip(item, ref prop) != InventoryResult.Success) return false;
            }
            return true;
        }

        private void CollectItem(Player player, int count)
        {
            if (count > 0 || (count < 0 && Count > 0))
            {                
                Count += count;
                UpdatePlayerContribution(player, count);
            }
        }

        private void OnPlayerPreItemPickup(PlayerPreItemPickupGameEvent evt)
        {
            var player = evt.Player;
            var item = evt.Item;
            if (EvaluateItem(player, item) == false) return;
            int count = item.CurrentStackSize;
            if (count > 0)
            {
                CollectItem(player, count);
                item.Properties[PropertyEnum.PickupDestroyPending] = true;
            }
        }

        private void OnPlayerCollectedItem(PlayerCollectedItemGameEvent evt)
        {
            var player = evt.Player;
            var item = evt.Item;
            if (EvaluateItem(player, item) == false) return;
            int count = evt.Count;
            CollectItem(player, count);
        }

        private void OnPlayerLostItem(PlayerLostItemGameEvent evt)
        {
            var player = evt.Player;
            var item = evt.Item;
            if (EvaluateItem(player, item) == false) return;
            int count = evt.Count;
            CollectItem(player, count);
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            if (_proto.DestroyOnPickup)
                region.PlayerPreItemPickupEvent.AddActionBack(_playerPreItemPickupAction);
            else
            {
                region.PlayerCollectedItemEvent.AddActionBack(_playerCollectedItemAction);
                region.PlayerLostItemEvent.AddActionBack(_playerLostItemAction);
            }
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            if (_proto.DestroyOnPickup)
                region.PlayerPreItemPickupEvent.RemoveAction(_playerPreItemPickupAction);
            else
            {
                region.PlayerCollectedItemEvent.RemoveAction(_playerCollectedItemAction);
                region.PlayerLostItemEvent.RemoveAction(_playerLostItemAction);
            }
        }
    }
}
