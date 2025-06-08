using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.Properties;

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

        [DoNotCopy]
        public virtual ItemPrototype AutoPopulatedIngredientPrototype { get => null; }

        public virtual CraftingResult AllowItem(ItemSpec itemSpec, Avatar avatar)
        {
            // TODO;
            return CraftingResult.Success;
        }
    }

    public class AutoPopulatedInputPrototype : CraftingInputPrototype
    {
        public PrototypeId Ingredient { get; protected set; }
        public int Quantity { get; protected set; }

        //---

        [DoNotCopy]
        public override ItemPrototype AutoPopulatedIngredientPrototype { get => Ingredient.As<ItemPrototype>(); }

        public override CraftingResult AllowItem(ItemSpec itemSpec, Avatar avatar)
        {
            // TODO;
            return CraftingResult.Success;
        }
    }

    public class RestrictionSetInputPrototype : CraftingInputPrototype
    {
        public DropRestrictionPrototype[] Restrictions { get; protected set; }

        //---

        public override CraftingResult AllowItem(ItemSpec itemSpec, Avatar avatar)
        {
            // TODO;
            return CraftingResult.Success;
        }
    }

    public class AllowedItemListInputPrototype : CraftingInputPrototype
    {
        public PrototypeId[] AllowedItems { get; protected set; }

        //---

        public override CraftingResult AllowItem(ItemSpec itemSpec, Avatar avatar)
        {
            // TODO;
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
                    return CraftingResult.InputFirstIngredientMismatch;

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
                return CraftingResult.InputFirstIngredientMismatch;

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
            // TODO
            creditsCost = 0;
            legendaryMarksCost = 0;
            return true;
        }
    }
}
