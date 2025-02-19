using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionInventoryRemoveItem : MissionAction
    {
        private MissionActionInventoryRemoveItemPrototype _proto;
        private MissionActionList _onRemoveActions;

        public MissionActionInventoryRemoveItem(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // CH03Main2TheTabletChase
            _proto = prototype as MissionActionInventoryRemoveItemPrototype;
        }

        public override void Destroy()
        {
            _onRemoveActions?.Destroy();
        }

        public override void Run()
        {
            var itemRef = _proto.ItemPrototype;
            if (itemRef == PrototypeId.Invalid) return;

            long itemCount = _proto.Count;
            if (itemCount <= 0) itemCount = 10000;
            var flags = InventoryIterationFlags.PlayerGeneral 
                | InventoryIterationFlags.PlayerGeneralExtra 
                | InventoryIterationFlags.PlayerStashGeneral 
                | InventoryIterationFlags.SortByPrototypeRef;

            List<Player> participants = ListPool<Player>.Instance.Get();
            if (Mission.GetParticipants(participants))
            {
                foreach (Player player in participants)
                    RemoveItemsFromInventory(new InventoryIterator(player, flags), itemRef, itemCount);
            }
            ListPool<Player>.Instance.Return(participants);
        }

        private void RemoveItemsFromInventory(InventoryIterator inventoryIterator, PrototypeId itemRef, long count)
        {           
            var manager = Game?.EntityManager;
            if (manager == null) return;

            foreach (var inventory in inventoryIterator)
                foreach (var entry in inventory)
                    if (entry.ProtoRef == itemRef)
                    {
                        var item = manager.GetEntity<Item>(entry.Id);
                        if (item == null || item.IsScheduledToDestroy) continue;
                        item.DecrementStack();
                        count--;

                        MissionActionList.CreateActionList(ref _onRemoveActions, _proto.OnRemoveActions, Owner);
                        if (count == 0) return;
                    }
        }
    }
}
