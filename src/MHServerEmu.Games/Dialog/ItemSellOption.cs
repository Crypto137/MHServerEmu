using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Dialog
{
    public class ItemSellOption : InteractionOption
    {
        public ItemSellOption()
        {
            MethodEnum = InteractionMethod.Sell;
        }

        public override bool IsCurrentlyAvailable(EntityDesc interacteeDesc, WorldEntity localInteractee, WorldEntity interactor, InteractionFlags interactionFlags)
        {
            bool isAvailable = false;
            if (base.IsCurrentlyAvailable(interacteeDesc, localInteractee, interactor, interactionFlags)
                && localInteractee != null)
            {
                if (localInteractee is not Item item) return false;

                InventoryPrototype inventoryProto = item.InventoryLocation.InventoryPrototype;
                Player player = interactor.GetOwnerOfType<Player>();

                if (inventoryProto != null && player != null)
                    isAvailable = inventoryProto.IsPlayerGeneralInventory || item.GetSellPrice(player) > 0;
            }
            return isAvailable;
        }
    }
}
