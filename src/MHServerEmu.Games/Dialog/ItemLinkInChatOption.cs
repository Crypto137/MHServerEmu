using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Dialog
{
    public class ItemLinkInChatOption : InteractionOption
    {
        public ItemLinkInChatOption()
        {
            Priority = 10;
            MethodEnum = InteractionMethod.LinkItemInChat;
        }

        public override bool IsCurrentlyAvailable(EntityDesc interacteeDesc, WorldEntity localInteractee, WorldEntity interactor, InteractionFlags interactionFlags)
        {
            bool isAvailable = false;
            if (base.IsCurrentlyAvailable(interacteeDesc, localInteractee, interactor, interactionFlags) 
                && localInteractee != null)
            {
                InventoryPrototype inventoryProto = localInteractee.InventoryLocation.InventoryPrototype;
                isAvailable = inventoryProto != null;
            }

            return isAvailable;
        }
    }
}
