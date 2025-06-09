using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Entities
{
    // NOTE: Many of the different ingredient error codes are displayed in the client as InsufficientIngredients
    public enum CraftingResult  // CPlayer::DisplayCraftingFailureMessage()
    {
        Success,
        CraftingFailed,
        RecipeNotInRecipeLibrary,
        RecipeDisabledByLiveTuning,
        InsufficientIngredients,
        InsufficientCredits,
        InsufficientLegendaryMarks,
        IngredientInvalid,
        IngredientNotApproved,              // InsufficientIngredients
        IngredientDisabledByLiveTuning,     // InsufficientIngredients
        IngredientLevelRestricted,
        IngredientNotDroppableForAvatar,    // InsufficientIngredients
        IngredientDroppableForAvatar,       // InsufficientIngredients
        IngredientFirstMismatch,            // InsufficientIngredients
        IngredientDropRestricted,           // InsufficientIngredients
        IngredientNotInAllowedItemList,     // InsufficientIngredients
        IngredientAutoPopulatedMismatch,    // InsufficientIngredients
        Result17,                           // InsufficientIngredients
        LootRollFailed,
    }

    public partial class Player
    {
        public CraftingResult Craft(ulong recipeItemId, ulong vendorId, List<ulong> ingredientIds, bool isRecraft)
        {
            // Validate arguments
            Item recipeItem = Game.EntityManager.GetEntity<Item>(recipeItemId);
            if (recipeItem == null) return Logger.WarnReturn(CraftingResult.CraftingFailed, "Craft(): recipeItem == null");

            CraftingRecipePrototype recipeProto = recipeItem.Prototype as CraftingRecipePrototype;
            if (recipeProto == null) return Logger.WarnReturn(CraftingResult.CraftingFailed, "Craft(): recipeProto == null");

            Inventory resultsInv = GetInventory(InventoryConvenienceLabel.CraftingResults);
            if (resultsInv == null) return Logger.WarnReturn(CraftingResult.CraftingFailed, "Craft(): resultsInv == null");

            WorldEntity vendor = Game.EntityManager.GetEntity<WorldEntity>(vendorId);
            if (vendor == null) return Logger.WarnReturn(CraftingResult.CraftingFailed, "Craft(): vendor == null");

            // Validate craftability
            CraftingResult canCraftRecipeResult = recipeItem.CanCraftRecipe(this, ingredientIds, vendor, isRecraft);
            if (canCraftRecipeResult != CraftingResult.Success)
                return Logger.WarnReturn(canCraftRecipeResult, $"Craft(): CanCraftRecipe() failed for player=[{this}], recipeItem=[{recipeItem}], result=[{canCraftRecipeResult}]");

            // TODO: the actual crafting

            return Logger.DebugReturn(CraftingResult.CraftingFailed, $"Craft(): recipeItemId={recipeItemId}, ingredientIds=[{string.Join(' ', ingredientIds)}], isRecraft={isRecraft}");
        }

        public CraftingResult CanCraftRecipeWithVendor(int avatarIndex, Item recipeItem, WorldEntity vendor)
        {
            PrototypeId vendorTypeProtoRef = vendor.Properties[PropertyEnum.VendorType];
            if (vendorTypeProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(CraftingResult.CraftingFailed, "CanCraftRecipeWithVendor(): vendorTypeProtoRef == PrototypeId.Invalid");

            VendorTypePrototype vendorTypeProto = vendorTypeProtoRef.As<VendorTypePrototype>();
            if (vendorTypeProto == null) return Logger.WarnReturn(CraftingResult.CraftingFailed, "CanCraftRecipeWithVendor(): vendorTypeProto == null");

            if (vendorTypeProto.IsCrafter == false)
                return CraftingResult.CraftingFailed;

            CraftingRecipePrototype recipeProto = recipeItem.ItemPrototype as CraftingRecipePrototype;
            if (recipeProto == null) return Logger.WarnReturn(CraftingResult.CraftingFailed, "CanCraftRecipeWithVendor(): recipeProto == null");

            PrototypeId invProtoRef = recipeItem.InventoryLocation.InventoryRef;
            if (invProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(CraftingResult.CraftingFailed, "CanCraftRecipeWithVendor(): invProtoRef == PrototypeId.Invalid");

            if (vendorTypeProto.ContainsInventory(invProtoRef) == false)
                return CraftingResult.CraftingFailed;

            Avatar avatar = GetActiveAvatarByIndex(avatarIndex);
            if (avatar == null) return Logger.WarnReturn(CraftingResult.CraftingFailed, "CanCraftRecipeWithVendor(): avatar == null");

            int vendorLevel = Properties[PropertyEnum.VendorLevel, vendorTypeProtoRef];
            if (recipeProto.UnlockAtCrafterRank > vendorLevel)
                return CraftingResult.CraftingFailed;

            return CraftingResult.Success;
        }

        public int GetCraftingIngredientAvailableStackCount(PrototypeId craftingIngredientProtoRef)
        {
            int count = 0;

            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.CraftingIngredientAvailable, craftingIngredientProtoRef))
                count += kvp.Value;

            return count;
        }

        public void AdjustCraftingIngredientAvailable(PrototypeId itemProtoRef, int delta, InventoryCategory inventoryCategory)
        {
            if (itemProtoRef == PrototypeId.Invalid)
                return;

            if (delta == 0)
                return;

            if (inventoryCategory != InventoryCategory.PlayerGeneral && inventoryCategory != InventoryCategory.PlayerGeneralExtra &&
                inventoryCategory != InventoryCategory.PlayerStashAvatarSpecific && inventoryCategory != InventoryCategory.PlayerStashGeneral)
            {
                return;
            }

            PropertyId propId = new(PropertyEnum.CraftingIngredientAvailable, itemProtoRef);
            if (Properties.HasProperty(propId) == false)
                return;

            // REMOVEME: debug
            //int currentValue = Properties[propId];
            //Logger.Debug($"AdjustCraftingIngredientAvailable(): {propId} = {currentValue} => {currentValue + delta}");

            Properties.AdjustProperty(delta, propId);
        }

        private void InitializeCraftingIngredientAvailable(CraftingRecipePrototype recipeProto, HashSet<PrototypeId> ingredientSet)
        {
            if (recipeProto.RecipeInputs.IsNullOrEmpty())
                return;

            foreach (CraftingInputPrototype inputProto in recipeProto.RecipeInputs)
            {
                ItemPrototype ingredientProto = inputProto.AutoPopulatedIngredientPrototype;
                if (ingredientProto == null)
                    continue;

                PropertyId propId = new(PropertyEnum.CraftingIngredientAvailable, ingredientProto.DataRef);

                if (Properties.HasProperty(propId))
                    continue;

                Properties[propId] = 0;
                ingredientSet.Add(ingredientProto.DataRef);
            }
        }

        private void UpdateCraftingIngredientAvailableStackCounts(HashSet<PrototypeId> ingredientSet)
        {
            if (ingredientSet.Count == 0)
                return;

            EntityManager entityManager = Game.EntityManager;

            const InventoryIterationFlags flags = InventoryIterationFlags.PlayerGeneral | InventoryIterationFlags.PlayerGeneralExtra |
                InventoryIterationFlags.PlayerStashAvatarSpecific | InventoryIterationFlags.PlayerStashGeneral;

            foreach (Inventory inventory in new InventoryIterator(this, flags))
            {
                foreach (var entry in inventory)
                {
                    if (ingredientSet.Contains(entry.ProtoRef) == false)
                        continue;

                    Item item = entityManager.GetEntity<Item>(entry.Id);
                    if (item == null)
                    {
                        Logger.Warn("UpdateCraftingIngredientAvailableStackCounts(): item == null");
                        continue;
                    }

                    Properties.AdjustProperty(item.CurrentStackSize, new(PropertyEnum.CraftingIngredientAvailable, entry.ProtoRef));
                }
            }

            // REMOVEME: debug
            //foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.CraftingIngredientAvailable))
            //    Logger.Debug($"UpdateCraftingIngredientAvailableStackCounts(): {kvp.Key} = {(int)kvp.Value}");
        }
    }
}
