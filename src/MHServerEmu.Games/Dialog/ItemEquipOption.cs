using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Dialog
{
    public class ItemEquipOption : InteractionOption
    {
        public ItemEquipOption()
        {
            Priority = 12;
            MethodEnum = InteractionMethod.Equip;
        }

        public override bool IsCurrentlyAvailable(EntityDesc interacteeDesc, WorldEntity localInteractee, WorldEntity interactor, InteractionFlags interactionFlags)
        {
            bool isAvailable = false;
            if (base.IsCurrentlyAvailable(interacteeDesc, localInteractee, interactor, interactionFlags)
                && localInteractee != null)
            {
                InventoryPrototype inventoryProto = localInteractee.InventoryLocation.InventoryPrototype;
                if (inventoryProto != null)
                    isAvailable = inventoryProto.IsPlayerGeneralInventory || inventoryProto.IsPlayerGeneralExtraInventory;
            }
            return isAvailable;
        }
    }
}
