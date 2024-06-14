using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Dialog
{
    public class ItemDonatePetTechOption : InteractionOption
    {
        public ItemDonatePetTechOption()
        {
            MethodEnum = InteractionMethod.DonatePetTech;
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
                    isAvailable = inventoryProto.IsPlayerGeneralInventory || inventoryProto.IsPlayerStashInventory;
            }
            return isAvailable;
        }
    }
}
