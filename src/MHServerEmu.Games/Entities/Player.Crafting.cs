using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Loot.Specs;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Regions;

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

            Avatar avatar = CurrentAvatar;
            if (avatar == null) return Logger.WarnReturn(CraftingResult.CraftingFailed, "Craft(): avatar == null");

            // Validate craftability
            CraftingResult canCraftRecipeResult = recipeItem.CanCraftRecipe(this, ingredientIds, vendor, isRecraft);
            if (canCraftRecipeResult != CraftingResult.Success)
                return Logger.WarnReturn(canCraftRecipeResult, $"Craft(): CanCraftRecipe() failed for player=[{this}], recipeItem=[{recipeItem}], result=[{canCraftRecipeResult}]");

            // Get crafting costs (already validated in CanCraftRecipe() above)
            using PropertyCollection currencyCost = ObjectPoolManager.Instance.Get<PropertyCollection>();
            recipeProto.GetCraftingCost(this, ingredientIds, out uint creditsCost, out uint legendaryMarksCost, currencyCost);

            // Recraft uses current results as input for another crafting attempt.
            if (isRecraft)
            {
                // Move the input from slot 0 so that the newly created output can occupy it
                ulong recraftItemId = resultsInv.GetEntityInSlot(0);
                if (recipeItemId == InvalidId) return Logger.WarnReturn(CraftingResult.CraftingFailed, "Craft(): recraftItemId == InvalidId");

                Item recraftItem = Game.EntityManager.GetEntity<Item>(recraftItemId);
                if (recraftItem == null) return Logger.WarnReturn(CraftingResult.CraftingFailed, "Craft(): recraftItem == null");

                uint recraftFreeSlot = resultsInv.GetFreeSlot(recraftItem, false, false);
                if (recraftFreeSlot == Inventory.InvalidSlot) return Logger.WarnReturn(CraftingResult.CraftingFailed, "Craft(): recraftFreeSlot = Inventory.InvalidSlot");

                ulong? stackEntityId = null;    // do not allow stacking here
                InventoryResult recraftItemMoveResult = recraftItem.ChangeInventoryLocation(resultsInv, recraftFreeSlot, ref stackEntityId, false);
                if (recraftItemMoveResult != InventoryResult.Success) return Logger.WarnReturn(CraftingResult.CraftingFailed, "Craft(): recraftItemMoveResult != recraftItemMoveResult != InventoryResult.Success");
            }

            // Crafting is done through generating new items via the loot system
            using ItemResolver resolver = ObjectPoolManager.Instance.Get<ItemResolver>();
            resolver.Initialize(Game.Random);
            resolver.SetContext(LootContext.Crafting, this);

            // Log this crafting attempt in case there is a memory leak or something
            //Logger.Debug($"Craft(): recipeItem=[{recipeItem}], ingredientIds=[{string.Join(' ', ingredientIds)}], isRecraft={isRecraft} player=[{this}]");

            // Prepare crafting ingredients
            List<Item> ingredients = ListPool<Item>.Instance.Get();
            Dictionary<Item, int> autoPopulatedIngredients = DictionaryPool<Item, int>.Instance.Get();
            List<Item> outputItems = ListPool<Item>.Instance.Get();

            try
            {
                if (CraftPrepareIngredients(recipeProto, ingredientIds, resolver, ingredients, autoPopulatedIngredients) == false)
                    return CraftingResult.InsufficientIngredients;

                // Roll the crafting output
                using LootRollSettings settings = ObjectPoolManager.Instance.Get<LootRollSettings>();
                settings.Player = this;
                settings.UsableAvatar = avatar.AvatarPrototype;
                settings.Level = avatar.CharacterLevel;

                // In version 1.52 some heroes don't have any valid costumes for the 3 to 1 recipe, so try the roll multiple times
                const int MaxRollAttempts = 10;

                LootRollResult rollResult = LootRollResult.NoRoll;
                for (int i = 0; i < MaxRollAttempts; i++)
                {
                    rollResult = recipeProto.RecipeOutput.RollLootTable(settings, resolver);
                    if (rollResult == LootRollResult.Success)
                        break;

                    Logger.Warn($"Craft(): Loot roll failed, attempt=[{i + 1}/{MaxRollAttempts}], recipeitem=[{recipeItem}], player=[{this}]");
                    resolver.SetContext(LootContext.Crafting, this);    // reset partial results to prevent potential abuse
                }

                if (rollResult != LootRollResult.Success)
                    return CraftingResult.LootRollFailed;

                using LootResultSummary summary = ObjectPoolManager.Instance.Get<LootResultSummary>();
                resolver.FillLootResultSummary(summary);

                const LootType LootTypeFilter = LootType.Item | LootType.LootMutation | LootType.VendorXP | LootType.CallbackNode;
                if ((summary.Types & ~LootTypeFilter) != LootType.None)
                    return Logger.WarnReturn(CraftingResult.LootRollFailed, $"Craft(): Unsupported loot types rolled! lootTypes=[{summary.Types}], recipeItem=[{recipeItem}], ingredientIds=[{string.Join(' ', ingredientIds)}], isRecraft={isRecraft}, player=[{this}]");

                // Do the crafting: create output, consume ingredients, and pay costs
                if (CraftCreateOutputItemsFromSummary(recipeProto, ingredients, summary, resultsInv, outputItems) == false)
                {
                    // Bail out if output creation failed for whatever reason.
                    Logger.Error($"Craft(): Failed to create output items! recipeItem=[{recipeItem}], ingredientIds=[{string.Join(' ', ingredientIds)}], isRecraft={isRecraft}, player=[{this}]");

                    // Since we haven't consumed any ingredients or paid any cost yet, we just need to clean up partially created items.
                    foreach (Item item in outputItems)
                        item?.Destroy();
                    
                    return CraftingResult.CraftingFailed;
                }

                // Point of no return from here
                CraftConsumeIngredients(ingredients, autoPopulatedIngredients);

                CraftPayCost(creditsCost, legendaryMarksCost, currencyCost);

                CraftProcessVendorLoot(summary, vendor);

                return CraftingResult.Success;
            }
            finally
            {
                ListPool<Item>.Instance.Return(ingredients);
                DictionaryPool<Item, int>.Instance.Return(autoPopulatedIngredients);
                ListPool<Item>.Instance.Return(outputItems);
            }
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

        public bool HasLearnedCraftingRecipe(PrototypeId recipeProtoRef)
        {
            if (recipeProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "HasLearnedCraftingReipe(): recipeProtoRef == PrototypeId.Invalid");

            Inventory learnedRecipeInv = GetInventory(InventoryConvenienceLabel.CraftingRecipesLearned);
            if (learnedRecipeInv == null) return Logger.WarnReturn(false, "HasLearnedCraftingReipe(): learnedRecipeInv == null");

            EntityManager entityManager = Game.EntityManager;
            foreach (var entry in learnedRecipeInv)
            {
                ulong recipeId = entry.Id;
                if (recipeId == InvalidId)
                {
                    Logger.Warn("HasLearnedCraftingRecipe(): recipeId == InvalidId");
                    continue;
                }

                Item recipe = entityManager.GetEntity<Item>(recipeId);
                if (recipe == null)
                {
                    Logger.Warn("HasLearnedCraftingRecipe(): recipe == null");
                    continue;
                }

                if (recipe.PrototypeDataRef == recipeProtoRef)
                    return true;
            }

            return false;
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

            foreach (Inventory inventory in new InventoryIterator(this, InventoryIterationFlags.CraftingIngredients))
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
        }

        private bool CraftPrepareIngredients(CraftingRecipePrototype recipeProto, List<ulong> ingredientIds, ItemResolver resolver,
            List<Item> ingredients, Dictionary<Item, int> autoPopulatedIngredients)
        {
            CraftingInputPrototype[] recipeInputs = recipeProto?.RecipeInputs;
            if (recipeInputs.IsNullOrEmpty()) return Logger.WarnReturn(false, "CraftPrepareIngredients(): recipeInputs.IsNullOrEmpty()");
            if (recipeInputs.Length != ingredientIds.Count) return Logger.WarnReturn(false, "CraftPrepareIngredients(): recipeInputs.Length != ingredientIds.Count");

            EntityManager entityManager = Game.EntityManager;

            Dictionary<PrototypeId, int> autoPopulatedIngredientCounts = DictionaryPool<PrototypeId, int>.Instance.Get();
            
            try
            {
                for (int i = 0; i < ingredientIds.Count; i++)
                {
                    ulong ingredientId = ingredientIds[i];

                    if (ingredientId != InvalidId)  // Explicitly specified ingredients
                    {
                        Item ingredient = entityManager.GetEntity<Item>(ingredientId);
                        if (ingredient == null) return Logger.WarnReturn(false, "CraftPrepareIngredients(): ingredient == null");

                        // Add the item reference to the list to consume it after crafting output is created
                        ingredients.Add(ingredient);

                        // Set clone source reference to allow mutations to reference this ingredient
                        ItemSpec cloneSource = new(ingredient.ItemSpec);
                        cloneSource.StackCount = ingredient.IsRelic ? ingredient.CurrentStackSize : 1;
                        resolver.SetCloneSource(i, cloneSource);
                    }
                    else                            // AutoPopulated ingredients
                    {
                        AutoPopulatedInputPrototype inputProto = recipeInputs[i] as AutoPopulatedInputPrototype;
                        if (inputProto == null) return Logger.WarnReturn(false, "CraftPrepareIngredients(): inputProto == null");

                        ItemPrototype autoPopulatedIngredientProto = inputProto.AutoPopulatedIngredientPrototype;
                        if (autoPopulatedIngredientProto == null) return Logger.WarnReturn(false, "CraftPrepareIngredients(): autoPopulatedIngredientProto == null");

                        if (autoPopulatedIngredientCounts.ContainsKey(autoPopulatedIngredientProto.DataRef))
                            return Logger.WarnReturn(false, $"CraftPrepareIngredients(): AutoPopulated ingredient {autoPopulatedIngredientProto} specified multiple times for recipe {recipeProto}");

                        autoPopulatedIngredientCounts.Add(autoPopulatedIngredientProto.DataRef, inputProto.Quantity);

                        // Add a null to the ingredient list to be able to look up explicit ingredients by index
                        ingredients.Add(null);
                    }
                }

                // Find auto-populated items if any are required for this recipe
                if (autoPopulatedIngredientCounts.Count == 0)
                    return true;

                foreach (Inventory inventory in new InventoryIterator(this, InventoryIterationFlags.CraftingIngredients))
                {
                    foreach (var entry in inventory)
                    {
                        PrototypeId itemProtoRef = entry.ProtoRef;

                        if (autoPopulatedIngredientCounts.TryGetValue(itemProtoRef, out int quantity) == false)
                            continue;

                        Item ingredient = entityManager.GetEntity<Item>(entry.Id);
                        if (ingredient == null)
                        {
                            Logger.Warn("CraftPrepareIngredients(): ingredient == null");
                            continue;
                        }

                        if (ingredient.IsScheduledToDestroy)
                        {
                            Logger.Warn("CraftPrepareIngredients(): ingredient.IsScheduledToDestroy");
                            continue;
                        }

                        // Add the item reference to the dictionary to consume it after crafting output is created
                        int quantityToConsume = Math.Min(quantity, ingredient.CurrentStackSize);
                        autoPopulatedIngredients[ingredient] = quantityToConsume;
                            
                        quantity -= quantityToConsume;
                        if (quantity > 0)
                            autoPopulatedIngredientCounts[itemProtoRef] = quantity;
                        else
                            autoPopulatedIngredientCounts.Remove(itemProtoRef);

                        if (autoPopulatedIngredientCounts.Count == 0)
                            return true;
                    }
                }

                return Logger.WarnReturn(false, $"CraftPrepareIngredients(): Failed to find auto-populated ingredients for recipe [{recipeProto}] crafted by player [{this}]");
            }
            finally
            {
                DictionaryPool<PrototypeId, int>.Instance.Return(autoPopulatedIngredientCounts);
            }
        }

        private bool CraftCreateOutputItemsFromSummary(CraftingRecipePrototype recipeProto, List<Item> ingredients, LootResultSummary summary, Inventory resultsInv, List<Item> outputItems)
        {
            // Create items
            EntityManager entityManager = Game.EntityManager;

            // Crafting input needs to be created in the results inventory rather than moved there because it can be bound, which would prevent movement.
            InventoryLocation invLoc = new(Id, resultsInv.PrototypeDataRef);

            foreach (ItemSpec itemSpec in summary.ItemSpecs)
            {
                using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
                settings.EntityRef = itemSpec.ItemProtoRef;
                settings.ItemSpec = itemSpec;
                settings.InventoryLocation = invLoc;
                settings.OptionFlags |= EntitySettingsOptionFlags.DoNotAllowStackingOnCreate;

                if (IsInGame == false)
                    settings.OptionFlags &= ~EntitySettingsOptionFlags.EnterGame;

                using PropertyCollection properties = ObjectPoolManager.Instance.Get<PropertyCollection>();
                settings.Properties = properties;
                properties[PropertyEnum.InventoryStackCount] = itemSpec.StackCount;

                Entity entity = entityManager.CreateEntity(settings);
                if (entity == null) return Logger.WarnReturn(false, "CraftCreateOutputItemsFromSummary(): entity == null");

                if (entity is not Item item)
                {
                    entity.Destroy();
                    return Logger.WarnReturn(false, "CraftCreateOutputItemsFromSummary(): entity is not Item");
                }

                outputItems.Add(item);
            }

            // Post-process created items
            HashSet<Item> clonedItems = HashSetPool<Item>.Instance.Get();

            try
            {
                // Clone post-processing
                LootNodePrototype[] lootNodes = recipeProto.RecipeOutput.Choices;

                bool isCloneRecipe = false;

                foreach (LootNodePrototype lootNodeProto in lootNodes)
                {
                    if (lootNodeProto is LootDropClonePrototype)
                    {
                        isCloneRecipe = true;
                        break;
                    }
                }

                if (isCloneRecipe)
                {
                    if (lootNodes.Length != outputItems.Count)
                        return Logger.WarnReturn(false, "CraftCreateOutputItemsFromSummary(): lootNodes.Length != outputItems.Count");

                    for (int i = 0; i < outputItems.Count; i++)
                    {
                        if (lootNodes[i] is not LootDropClonePrototype cloneProto)
                            continue;

                        Item outputItem = outputItems[i];
                        if (outputItem == null)
                        {
                            Logger.Warn("CraftCreateOutputItemsFromSummary(): outputItem == null");
                            continue;
                        }

                        clonedItems.Add(outputItem);

                        // Copy additional properties from the source item
                        int index = cloneProto.SourceIndex;
                        if (index < 0 || index >= ingredients.Count) return Logger.WarnReturn(false, "CraftCreateOutputItemsFromSummary(): index < 0 || index >= ingredients.Count");
                        
                        Item sourceItem = ingredients[index];
                        if (sourceItem == null) return Logger.WarnReturn(false, "CraftCreateOutputItemsFromSummary(): sourceItem == null");

                        outputItem.Properties.CopyProperty(sourceItem.Properties, PropertyEnum.ItemLimitedEdition);

                        if (sourceItem.IsPetItem)
                            outputItem.Properties.CopyPropertyRange(sourceItem.Properties, PropertyEnum.PetItemDonationCount);

                        // Do mutation callbacks
                        foreach (LootMutationPrototype lootMutationProto in summary.LootMutations)
                            lootMutationProto.OnItemCreated(outputItem);
                    }
                }

                // Run OnRecipeComplete eval
                if (recipeProto.OnRecipeComplete != null)
                {
                    using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                    evalContext.Game = Game;

                    for (int i = 0; i < outputItems.Count; i++)
                    {
                        EvalContext contextEnum = EvalContext.Var1 + i;
                        evalContext.SetVar_PropertyCollectionPtr(contextEnum, outputItems[i]?.Properties);

                        if (contextEnum == EvalContext.Var5)
                            break;
                    }

                    Eval.RunBool(recipeProto.OnRecipeComplete, evalContext);
                }

                // Trigger events
                if (outputItems.Count > 0)
                {
                    Region region = GetRegion();

                    foreach (Item outputItem in outputItems)
                    {
                        if (outputItem == null)
                            continue;

                        RarityPrototype rarityProto = ((PrototypeId)outputItem.Properties[PropertyEnum.ItemRarity]).As<RarityPrototype>();
                        int count = outputItem.CurrentStackSize;

                        region?.PlayerCraftedItemEvent.Invoke(new(this, outputItem, recipeProto.DataRef, count));

                        OnScoringEvent(new(ScoringEventType.ItemCrafted, recipeProto, rarityProto, count));

                        // Only newly created items count for the ItemCollected event
                        if (clonedItems.Contains(outputItem) == false)
                            OnScoringEvent(new(ScoringEventType.ItemCollected, outputItem.ItemPrototype, rarityProto, count));
                    }
                }
                else
                {
                    OnScoringEvent(new(ScoringEventType.ItemCrafted, recipeProto, 1));
                }

                return true;
            }
            finally
            {
                HashSetPool<Item>.Instance.Return(clonedItems);
            }
        }

        private void CraftConsumeIngredients(List<Item> ingredients, Dictionary<Item, int> autoPopulatedIngredients)
        {
            foreach (Item ingredient in ingredients)
            {
                // AutoPopulated ingredients will be null and are consumed separately
                if (ingredient == null)
                    continue;

                // Relic stacks are treated as individual items
                int quantity = ingredient.IsRelic ? ingredient.CurrentStackSize : 1;

                ingredient.DecrementStack(quantity);
            }

            foreach (var kvp in autoPopulatedIngredients)
                kvp.Key.DecrementStack(kvp.Value);
        }

        private void CraftPayCost(uint creditsCost, uint legendaryMarksCost, PropertyCollection currencyCosts)
        {
            CurrencyGlobalsPrototype currencyGlobalsProto = GameDatabase.CurrencyGlobalsPrototype;

            if (creditsCost > 0)
            {
                int delta = -(int)creditsCost;
                Properties.AdjustProperty(delta, new(PropertyEnum.Currency, currencyGlobalsProto.Credits));
            }

            if (legendaryMarksCost > 0)
            {
                int delta = -(int)legendaryMarksCost;
                Properties.AdjustProperty(delta, new(PropertyEnum.Currency, currencyGlobalsProto.LegendaryMarks));
            }

            foreach (var kvp in currencyCosts.IteratePropertyRange(PropertyEnum.Currency))
            {
                int delta = -(int)kvp.Value;
                Properties.AdjustProperty(delta, kvp.Key);
            }
        }

        private void CraftProcessVendorLoot(LootResultSummary summary, WorldEntity vendor)
        {
            // Award vendor XP
            if (summary.Types.HasFlag(LootType.VendorXP))
            {
                foreach (VendorXPSummary vendorXPSummary in summary.VendorXP)
                {
                    ulong xpVendorId = vendor.Properties[PropertyEnum.VendorType] == vendorXPSummary.VendorProtoRef ? vendor.Id : InvalidId;
                    AwardVendorXP(vendorXPSummary.XPAmount, vendorXPSummary.VendorProtoRef, xpVendorId);
                }
            }

            // Trigger callbacks
            if (summary.Types.HasFlag(LootType.CallbackNode))
            {
                foreach (LootNodePrototype callbackNode in summary.CallbackNodes)
                    callbackNode.OnResultsEvaluation(this, vendor);
            }
        }
    }
}
