using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Dialog
{
    public class ItemUseOption : InteractionOption
    {
        public ItemUseOption()
        {
            Priority = 11;
            MethodEnum = InteractionMethod.Use;
        }

        public override bool IsCurrentlyAvailable(EntityDesc interacteeDesc, WorldEntity localInteractee, WorldEntity interactor, InteractionFlags interactionFlags)
        {
            bool isAvailable = false;
            if (base.IsCurrentlyAvailable(interacteeDesc, localInteractee, interactor, interactionFlags)
                && localInteractee != null)
            {                
                Player player = interactor.GetOwnerOfType<Player>();
                if (player == null) return false;

                bool isUsable;
                if (player.CanAcquireCurrencyItem(localInteractee))
                    isUsable = true;
                else
                {
                    if (localInteractee.Prototype is not ItemPrototype itemProto) return false;
                    isUsable = itemProto.IsUsable;
                }

                isAvailable = isUsable && player.Owns(localInteractee);
                if (isAvailable)
                {
                    var inventoryLocation = localInteractee.InventoryLocation;
                    var inventoryCategory = inventoryLocation.InventoryCategory;
                    if (localInteractee.Prototype is not ItemPrototype itemProto) return false;
                    if (inventoryCategory != InventoryCategory.PlayerGeneral 
                        && inventoryCategory != InventoryCategory.PlayerGeneralExtra 
                        && inventoryLocation.InventoryConvenienceLabel != InventoryConvenienceLabel.PvP)
                    {
                        if (inventoryCategory == InventoryCategory.PlayerStashGeneral 
                            || inventoryCategory == InventoryCategory.PlayerStashAvatarSpecific)
                        {
                            WorldEntity dialogTarget = player.GetDialogTarget(true); 
                            isAvailable = dialogTarget != null && dialogTarget.Properties[PropertyEnum.OpenPlayerStash];
                        }
                        else if (inventoryLocation.InventoryConvenienceLabel == InventoryConvenienceLabel.DeliveryBox)
                            isAvailable = itemProto.IsContainer;
                        else if (inventoryCategory == InventoryCategory.AvatarEquipment)
                            isAvailable = true;
                        else
                            isAvailable = false;
                    }
                    else if (itemProto.AbilitySettings != null)
                        isAvailable = itemProto.AbilitySettings.OnlySlottableWhileEquipped == false;
                }
            }
            return isAvailable;
        }
    }
}
