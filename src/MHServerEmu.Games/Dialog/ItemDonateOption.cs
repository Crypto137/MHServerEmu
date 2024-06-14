using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Dialog
{
    public class ItemDonateOption : InteractionOption
    {
        public ItemDonateOption()
        {
            MethodEnum = InteractionMethod.Donate;
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
                WorldEntity dialogTarget = player.GetDialogTarget();

                if (inventoryProto != null && player != null)
                    if (inventoryProto.IsPlayerGeneralInventory || inventoryProto.IsPlayerStashInventory)
                    {
                        if (dialogTarget != null && dialogTarget.IsGlobalEventVendor)
                            isAvailable = true;
                        else
                            isAvailable = item.GetVendorBaseXPGain(player) > 0;
                    }
            }
            return isAvailable;
        }
    }
}
