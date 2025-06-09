using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class CraftingInputPrototype : Prototype
    {
        public PrototypeId SlotDisplayInfo { get; protected set; }
        public bool OnlyDroppableForThisAvatar { get; protected set; }
        public bool OnlyNotDroppableForThisAvatar { get; protected set; }
        public bool OnlyEquippableAtThisAvatarLevel { get; protected set; }
        public bool MatchFirstInput { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        [DoNotCopy]
        public virtual ItemPrototype AutoPopulatedIngredientPrototype { get => null; }

        public virtual CraftingResult AllowItem(ItemSpec itemSpec, Avatar avatar)
        {
            if (itemSpec.StackCount < 1)
                return CraftingResult.InsufficientIngredients;

            if (OnlyDroppableForThisAvatar || OnlyNotDroppableForThisAvatar)
            {
                ItemPrototype itemProto = itemSpec.ItemProtoRef.As<ItemPrototype>();
                if (itemProto == null) return Logger.WarnReturn(CraftingResult.CraftingFailed, "AllowItem(): itemProto == null");

                bool isDroppable = itemProto.IsDroppableForAgent(avatar.AgentPrototype);

                if (OnlyDroppableForThisAvatar && isDroppable == false)
                    return CraftingResult.IngredientNotDroppableForAvatar;

                if (OnlyNotDroppableForThisAvatar && isDroppable)
                    return CraftingResult.IngredientDroppableForAvatar;
            }

            if (OnlyEquippableAtThisAvatarLevel)
            {
                if (avatar.CharacterLevel < Item.GetEquippableAtLevelForItemLevel(itemSpec.ItemLevel))
                    return CraftingResult.IngredientLevelRestricted;
            }

            return CraftingResult.Success;
        }
    }

    public class AutoPopulatedInputPrototype : CraftingInputPrototype
    {
        public PrototypeId Ingredient { get; protected set; }
        public int Quantity { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        [DoNotCopy]
        public override ItemPrototype AutoPopulatedIngredientPrototype { get => Ingredient.As<ItemPrototype>(); }

        public override CraftingResult AllowItem(ItemSpec itemSpec, Avatar avatar)
        {
            // Auto populated ingredients use fake item specs, so they don't have rarity specified and will fail the IsValid check.
            if (itemSpec.ItemProtoRef == PrototypeId.Invalid)
                return Logger.WarnReturn(CraftingResult.CraftingFailed, "AllowItem(): itemSpec.ItemProtoRef == PrototypeId.Invalid");

            if (Ingredient == PrototypeId.Invalid)
                return Logger.WarnReturn(CraftingResult.CraftingFailed, "AllowItem(): Ingredient == PrototypeId.Invalid");

            if (itemSpec.ItemProtoRef != Ingredient)
                return CraftingResult.IngredientAutoPopulatedMismatch;

            if (itemSpec.StackCount < Quantity)
                return CraftingResult.InsufficientIngredients;

            // This prototype validation seems overkill, but it's here to match the client.
            ItemPrototype ingredient = Ingredient.As<ItemPrototype>();
            if (ingredient.StackSettings == null || ingredient.StackSettings.MaxStacks <= 1)
                return Logger.WarnReturn(CraftingResult.CraftingFailed, "AllowItem(): ingredient.StackSettings == null || ingredient.StackSettings.MaxStacks <= 1");

            if (ingredient.BindingSettings?.DefaultSettings?.BindsToCharacterOnEquip == true)
                return Logger.WarnReturn(CraftingResult.CraftingFailed, "AllowItem(): ingredient.BindingSettings?.DefaultSettings?.BindsToCharacterOnEquip == true");

            return CraftingResult.Success;
        }
    }

    public class RestrictionSetInputPrototype : CraftingInputPrototype
    {
        public DropRestrictionPrototype[] Restrictions { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override CraftingResult AllowItem(ItemSpec itemSpec, Avatar avatar)
        {
            if (itemSpec.IsValid == false)
                return Logger.WarnReturn(CraftingResult.CraftingFailed, "AllowItem(): itemSpec.IsValid == false");

            CraftingResult baseResult = base.AllowItem(itemSpec, avatar);
            if (baseResult != CraftingResult.Success)
                return baseResult;

            using LootCloneRecord lootCloneRecord = ObjectPoolManager.Instance.Get<LootCloneRecord>();
            LootCloneRecord.Initialize(lootCloneRecord, LootContext.Crafting, itemSpec, avatar.PrototypeDataRef);

            foreach (DropRestrictionPrototype dropRestrictionProto in Restrictions)
            {
                if (dropRestrictionProto.AllowAsCraftingInput(lootCloneRecord) == false)
                    return CraftingResult.IngredientDropRestricted;
            }

            return CraftingResult.Success;
        }
    }

    public class AllowedItemListInputPrototype : CraftingInputPrototype
    {
        public PrototypeId[] AllowedItems { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override CraftingResult AllowItem(ItemSpec itemSpec, Avatar avatar)
        {
            if (itemSpec.IsValid == false)
                return Logger.WarnReturn(CraftingResult.CraftingFailed, "AllowItem(): itemSpec.IsValid == false");

            if (AllowedItems.IsNullOrEmpty())
                return Logger.WarnReturn(CraftingResult.CraftingFailed, "AllowItem(): AllowedItems.IsNullOrEmpty()");

            CraftingResult baseResult = base.AllowItem(itemSpec, avatar);
            if (baseResult != CraftingResult.Success)
                return baseResult;

            bool contains = false;
            foreach (PrototypeId allowedItemProtoRef in AllowedItems)
            {
                if (itemSpec.ItemProtoRef == allowedItemProtoRef)
                {
                    contains = true;
                    break;
                }
            }

            if (contains == false)
                return CraftingResult.IngredientNotInAllowedItemList;

            return CraftingResult.Success;
        }
    }

    public class CraftingCostPrototype : Prototype
    {
        public EvalPrototype CostEvalCredits { get; protected set; }
        public bool DependsOnInput1 { get; protected set; }
        public bool DependsOnInput2 { get; protected set; }
        public bool DependsOnInput3 { get; protected set; }
        public bool DependsOnInput4 { get; protected set; }
        public bool DependsOnInput5 { get; protected set; }
        public EvalPrototype CostEvalLegendaryMarks { get; protected set; }
        public EvalPrototype CostEvalCurrencies { get; protected set; }

        //---

        [DoNotCopy]
        public bool DependsOnAnyInput { get => DependsOnInput1 || DependsOnInput2 || DependsOnInput3 || DependsOnInput4 || DependsOnInput5; }

        [DoNotCopy]
        public bool HasAnyCostEval { get => CostEvalCredits != null || CostEvalLegendaryMarks != null || CostEvalCurrencies != null; }
    }

    public class CraftingIngredientPrototype : ItemPrototype
    {
    }

    public class CostumeCorePrototype : CraftingIngredientPrototype
    {
        public override bool IsDroppableForAgent(AgentPrototype agentProto)
        {
            if (agentProto is not AvatarPrototype avatarProto)
                return false;

            return this == avatarProto.CostumeCore.As<CostumeCorePrototype>();
        }

        public override bool IsUsableByAgent(AgentPrototype agentProto)
        {
            if (base.IsUsableByAgent(agentProto) == false)
                return false;

            if (agentProto is not AvatarPrototype avatarProto)
                return false;

            return this == avatarProto.CostumeCore.As<CostumeCorePrototype>();
        }
    }

    public class CraftingRecipePrototype : ItemPrototype
    {
        public CraftingInputPrototype[] RecipeInputs { get; protected set; }
        public LootTablePrototype RecipeOutput { get; protected set; }
        public LocaleStringId RecipeDescription { get; protected set; }
        public AssetId RecipeIconPath { get; protected set; }
        public int SortOrder { get; protected set; }
        public LocaleStringId RecipeTooltip { get; protected set; }
        public CraftingCostPrototype CraftingCost { get; protected set; }
        public int UnlockAtCrafterRank { get; protected set; }
        public EvalPrototype OnRecipeComplete { get; protected set; }
        public PrototypeId RecipeCategory { get; protected set; }
        public AssetId RecipeIconPathHiRes { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public CraftingResult ValidateIngredients(Player player, List<ulong> ingredientIds)
        {
            if (RecipeInputs.IsNullOrEmpty()) return Logger.WarnReturn(CraftingResult.CraftingFailed, "ValidateIngredients(): RecipeInputs.IsNullOrEmpty()");

            Dictionary<ulong, int> usedStackCounts = DictionaryPool<ulong, int>.Instance.Get();
            try
            {
                for (int slot = 0; slot < RecipeInputs.Length; slot++)
                {
                    CraftingResult slotResult = ValidateIngredient(player, ingredientIds, slot, usedStackCounts);
                    if (slotResult != CraftingResult.Success)
                        return slotResult;
                }

                return CraftingResult.Success;
            }
            finally
            {
                DictionaryPool<ulong, int>.Instance.Return(usedStackCounts);
            }
        }

        public CraftingResult ValidateIngredient(Player player, List<ulong> ingredientIds, int slot, Dictionary<ulong, int> usedStackCounts)
        {
            if (RecipeInputs.IsNullOrEmpty()) return Logger.WarnReturn(CraftingResult.CraftingFailed, "ValidateIngredient(): RecipeInputs.IsNullOrEmpty()");

            if (RecipeInputs.Length != ingredientIds.Count) return Logger.WarnReturn(CraftingResult.CraftingFailed, "ValidateIngredient(): RecipeInputs.Length != ingredientIds.Count");

            if (slot < 0 || slot >= RecipeInputs.Length) return Logger.WarnReturn(CraftingResult.CraftingFailed, "ValidateIngredient(): slot < 0 || slot >= RecipeInputs.Length");

            Avatar avatar = player.CurrentAvatar;
            if (avatar == null) return Logger.WarnReturn(CraftingResult.CraftingFailed, "ValidateIngredient(): avatar == null");

            CraftingInputPrototype firstInputProto = RecipeInputs[0];
            if (firstInputProto == null) return Logger.WarnReturn(CraftingResult.CraftingFailed, "ValidateIngredient(): firstInputProto == null");

            CraftingInputPrototype targetInputProto = RecipeInputs[slot];
            if (targetInputProto == null) return Logger.WarnReturn(CraftingResult.CraftingFailed, "ValidateIngredient(): targetInputProto == null");

            CraftingInputPrototype inputProtoToUse = null;
            ItemPrototype ingredientProtoToMatch = null;

            if (targetInputProto.MatchFirstInput)
            {
                // MatchFirstInput handles combining items of the same type (e.g. 3 -> 1 of higher tier), in which case they all need to be the same item
                if (targetInputProto == firstInputProto)
                    return Logger.WarnReturn(CraftingResult.CraftingFailed, $"ValidateIngredient(): The first input CANNOT itself have MatchFirstInput set recipe=[{this}]");

                inputProtoToUse = firstInputProto;
                
                Item firstIngredient = player.Game.EntityManager.GetEntity<Item>(ingredientIds[0]);
                if (firstIngredient == null)
                    return CraftingResult.IngredientFirstMismatch;

                ingredientProtoToMatch = firstIngredient.ItemPrototype;
            }
            else
            {
                inputProtoToUse = targetInputProto;
            }

            ulong ingredientId = ingredientIds[slot];

            ItemPrototype ingredientProto;
            ItemSpec ingredientSpec;

            ItemPrototype autoPopulatedIngredientProto = inputProtoToUse.AutoPopulatedIngredientPrototype;

            if (autoPopulatedIngredientProto != null)
            {
                if (ingredientId != Entity.InvalidId)
                    return Logger.WarnReturn(CraftingResult.IngredientInvalid, $"ValidateIngredient(): ingredient entity id is unexpectedly set for an AutoPopulated input. entityId=[{ingredientId}] slot=[{slot}] recipe=[{this}]");

                ingredientProto = autoPopulatedIngredientProto;

                // Create a new item spec for auto populated ingredients
                ingredientSpec = new(ingredientProto.DataRef);
                ingredientSpec.StackCount = player.GetCraftingIngredientAvailableStackCount(autoPopulatedIngredientProto.DataRef);
            }
            else
            {
                Item ingredientItem = player.Game.EntityManager.GetEntity<Item>(ingredientId);
                if (ingredientItem == null) return Logger.WarnReturn(CraftingResult.IngredientInvalid, $"ValidateIngredient(): ingredientItem == null");

                if (ingredientItem.IsScheduledToDestroy)
                    return CraftingResult.IngredientInvalid;

                ingredientProto = ingredientItem.ItemPrototype;

                // Use the specified item's spec for manual ingredients (TODO: see if we can get away with passing by reference here)
                ingredientSpec = new(ingredientItem.ItemSpec);
                ingredientSpec.StackCount = ingredientItem.CurrentStackSize;
            }

            if (ingredientProto == null) return Logger.WarnReturn(CraftingResult.CraftingFailed, "ValidateIngredient(): ingredientProto == null");

            if (ingredientProto.ApprovedForUse() == false)
                return CraftingResult.IngredientNotApproved;

            if (ingredientProto.IsLiveTuningEnabled() == false)
                return CraftingResult.IngredientDisabledByLiveTuning;

            if (ingredientProtoToMatch != null && ingredientProto != ingredientProtoToMatch)
                return CraftingResult.IngredientFirstMismatch;

            CraftingResult result = inputProtoToUse.AllowItem(ingredientSpec, avatar);
            if (result == CraftingResult.Success && autoPopulatedIngredientProto == null && usedStackCounts != null)
            {
                usedStackCounts.TryGetValue(ingredientId, out int count);
                usedStackCounts[ingredientId] = ++count;
                if (count > ingredientSpec.StackCount)
                    return CraftingResult.InsufficientIngredients;
            }

            return CraftingResult.Success;
        }

        public bool GetCraftingCost(Player player, List<ulong> ingredientIds, out uint creditsCost, out uint legendaryMarksCost, PropertyCollection currencyCost)
        {
            creditsCost = 0;
            legendaryMarksCost = 0;

            // Early out if there is no cost
            if (CraftingCost == null)
                return true;

            if (ingredientIds == null && CraftingCost.DependsOnAnyInput)
                return false;

            if (CraftingCost.HasAnyCostEval == false)
                return true;

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, player.Properties);

            // Add ingredients to eval context
            Span<bool> dependsOnInput = stackalloc bool[]
{
                CraftingCost.DependsOnInput1,
                CraftingCost.DependsOnInput2,
                CraftingCost.DependsOnInput3,
                CraftingCost.DependsOnInput4,
                CraftingCost.DependsOnInput5
            };

            if (ingredientIds != null)
            {
                EntityManager entityManager = player.Game.EntityManager;

                for (int i = 0; i < 5; i++)
                {
                    if (dependsOnInput[i] == false)
                        continue;

                    // Early out if the required ingredient was not specified
                    if (i >= ingredientIds.Count || ingredientIds[i] == Entity.InvalidId)
                        return false;

                    Entity ingredient = entityManager.GetEntity<Entity>(ingredientIds[i]);
                    if (ingredient == null) return Logger.WarnReturn(false, "GetCraftingCost(): ingredient == null");

                    evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Var1 + i, ingredient.Properties);
                }
            }

            // Run evals
            if (CraftingCost.CostEvalCredits != null)
            {
                creditsCost += (uint)Eval.RunInt(CraftingCost.CostEvalCredits, evalContext);

                // Apply CraftingCostCreditsModPct if we have one
                Avatar avatar = player.CurrentAvatar;
                if (avatar != null)
                {
                    float craftingCostCreditsModPct = avatar.Properties[PropertyEnum.CraftingCostCreditsModPct];
                    if (MathHelper.IsNearZero(craftingCostCreditsModPct) == false)
                    {
                        float mult = Math.Max(1f + craftingCostCreditsModPct, 0f);
                        creditsCost = (uint)MathHelper.RoundDownToInt(creditsCost * mult);
                    }
                }
            }

            if (CraftingCost.CostEvalLegendaryMarks != null)
                legendaryMarksCost += (uint)Eval.RunInt(CraftingCost.CostEvalLegendaryMarks, evalContext);

            if (CraftingCost.CostEvalCurrencies != null)
            {
                evalContext.SetVar_PropertyCollectionPtr(EvalContext.Default, currencyCost);
                if (Eval.RunBool(CraftingCost.CostEvalCurrencies, evalContext) == false)
                    return Logger.WarnReturn(false, "GetCraftingCost(): Eval.RunBool(CraftingCost.CostEvalCurrencies, evalContext) == false");
            }

            return true;
        }
    }
}
