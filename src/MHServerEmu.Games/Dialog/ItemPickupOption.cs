using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;

namespace MHServerEmu.Games.Dialog
{
    public class ItemPickupOption : InteractionOption
    {
        public ItemPickupOption()
        {
            MethodEnum = InteractionMethod.PickUp;
        }

        public override bool IsCurrentlyAvailable(EntityDesc interacteeDesc, WorldEntity localInteractee, WorldEntity interactor, InteractionFlags interactionFlags)
        {
            return base.IsCurrentlyAvailable(interacteeDesc, localInteractee, interactor, interactionFlags)
                && PlayerCanPickUp(localInteractee, interactor);
        }

        private static bool PlayerCanPickUp(WorldEntity localInteractee, WorldEntity interactor)
        {
            bool canPickup = false;
            if (localInteractee != null && localInteractee.IsInWorld && interactor.IsDead == false)
            {
                Player interactingPlayer = interactor.GetOwnerOfType<Player>();
                if (interactingPlayer != null)
                {
                    if (interactingPlayer.CanAcquireCurrencyItem(localInteractee))
                        canPickup = true;
                    else
                    {
                        if (localInteractee is Item item && item.IsBoundToAccount == false)
                        {
                            Inventory generalInventory = interactingPlayer.GetInventory(InventoryConvenienceLabel.General);
                            if (generalInventory != null && item.CanChangeInventoryLocation(generalInventory) == InventoryResult.Success)
                                canPickup = true;
                        }
                    }
                }
            }
            return canPickup;
        }
    }
}
