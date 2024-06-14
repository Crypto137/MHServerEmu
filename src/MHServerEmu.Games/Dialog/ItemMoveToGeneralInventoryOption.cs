using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Dialog
{
    public class ItemMoveToGeneralInventoryOption : InteractionOption
    {
        public ItemMoveToGeneralInventoryOption()
        {
            Priority = 10;
            MethodEnum = InteractionMethod.MoveToGeneralInventory;
        }

        public override bool IsCurrentlyAvailable(EntityDesc interacteeDesc, WorldEntity localInteractee, WorldEntity interactor, InteractionFlags interactionFlags)
        {
            bool isAvailable = false;
            if (base.IsCurrentlyAvailable(interacteeDesc, localInteractee, interactor, interactionFlags)
                && localInteractee != null)
            {
                InventoryPrototype inventoryProto = localInteractee.InventoryLocation.InventoryPrototype;
                var playerStashProto = inventoryProto as PlayerStashInventoryPrototype;
                if (inventoryProto != null)
                {
                    isAvailable = playerStashProto != null 
                        || inventoryProto.ConvenienceLabel == InventoryConvenienceLabel.CraftingResults 
                        || inventoryProto.ConvenienceLabel == InventoryConvenienceLabel.DeliveryBox 
                        || inventoryProto.ConvenienceLabel == InventoryConvenienceLabel.Trade;
                }
            }

            return isAvailable;
        }
    }
}
