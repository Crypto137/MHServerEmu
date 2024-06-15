using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Entities.Inventories;

namespace MHServerEmu.Games.Dialog
{
    public class ItemMoveToTeamUpOption : InteractionOption
    {
        public ItemMoveToTeamUpOption()
        {
            Priority = 9;
            MethodEnum = InteractionMethod.MoveToTeamUp;
        }

        public override bool IsCurrentlyAvailable(EntityDesc interacteeDesc, WorldEntity localInteractee, WorldEntity interactor, InteractionFlags interactionFlags)
        {
            bool isAvailable = false;
            if (base.IsCurrentlyAvailable(interacteeDesc, localInteractee, interactor, interactionFlags)
                && localInteractee != null)
            {
                if (localInteractee is not Item item) return false;
                InventoryPrototype inventoryProto = item.InventoryLocation.InventoryPrototype;
                if (inventoryProto != null)
                    isAvailable = inventoryProto.ConvenienceLabel == InventoryConvenienceLabel.General
                        || inventoryProto is PlayerStashInventoryPrototype
                        || inventoryProto.IsEquipmentInventory;
            }
            return isAvailable;
        }
    }
}
