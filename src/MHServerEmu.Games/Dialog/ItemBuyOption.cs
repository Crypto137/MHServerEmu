using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Dialog
{
    public class ItemBuyOption : InteractionOption
    {
        public ItemBuyOption()
        {
            MethodEnum = InteractionMethod.Buy;
        }

        public override bool IsCurrentlyAvailable(EntityDesc interacteeDesc, WorldEntity localInteractee, WorldEntity interactor, InteractionFlags interactionFlags)
        {
            bool isAvailable = false;
            if (base.IsCurrentlyAvailable(interacteeDesc, localInteractee, interactor, interactionFlags) 
                && localInteractee != null)
            {
                if (localInteractee is not Item item) return false;
                InventoryPrototype inventoryProto = item.InventoryLocation.InventoryPrototype;
                isAvailable = inventoryProto != null && inventoryProto.VendorInvContentsCanBeBought;
            }
            return isAvailable;
        }
    }
}
