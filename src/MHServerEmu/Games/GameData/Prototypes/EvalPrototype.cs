using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum EvalContext
    {
        Default = 0,
        Entity = 1,
        EntityBehaviorBlackboard = 2,
        Other = 3,
        Condition = 4,
        ConditionKeywords = 5,
        TeamUp = 6,
        Var1 = 7,
        Var2 = 8,
        Var3 = 9,
        Var4 = 10,
        Var5 = 11,
        CallerStack = 14,
        LocalStack = 13,
        Globals = 15,
    }

    [AssetEnum]
    public enum DamageType
    {
        Physical = 0,
        Energy = 1,
        Mental = 2,
        Any = 4,
    }

    [AssetEnum]
    public enum ConvenienceLabel
    {
        None = 0,
        AvatarArtifact1 = 1,
        AvatarArtifact2 = 2,
        AvatarArtifact3 = 3,
        AvatarArtifact4 = 4,
        AvatarLegendary = 5,
        AvatarInPlay = 6,
        AvatarLibrary = 7,
        AvatarLibraryHardcore = 8,
        AvatarLibraryLadder = 9,
        TeamUpLibrary = 10,
        TeamUpGeneral = 11,
        Costume = 12,
        CraftingRecipesLearned = 13,
        DEPRECATEDCraftingInProgress = 14,
        CraftingResults = 15,
        DangerRoomScenario = 16,
        General = 17,
        DEPRECATEDPlayerStash = 19,
        Summoned = 20,
        Trade = 21,
        UIItems = 22,
        DeliveryBox = 23,
        ErrorRecovery = 24,
        Controlled = 25,
        VendorBuyback = 26,
        PvP = 27,
        PetItem = 18,
        ItemLink = 28,
        CouponAwards = 29,
        UnifiedStash = 30,
    }

    #endregion

    public class EvalPrototype : Prototype
    {
    }

    public class ExportErrorPrototype : EvalPrototype
    {
    }

    public class AssignPropPrototype : EvalPrototype
    {
        public EvalContext Context { get; set; }
        public ulong Prop { get; set; }
        public EvalPrototype Eval { get; set; }
    }

    public class SwapPropPrototype : EvalPrototype
    {
        public EvalContext LeftContext { get; set; }
        public ulong Prop { get; set; }
        public EvalContext RightContext { get; set; }
    }

    public class AssignPropEvalParamsPrototype : EvalPrototype
    {
        public EvalContext Context { get; set; }
        public EvalPrototype Param0 { get; set; }
        public EvalPrototype Param1 { get; set; }
        public EvalPrototype Param2 { get; set; }
        public EvalPrototype Param3 { get; set; }
        public ulong Prop { get; set; }
        public EvalPrototype Eval { get; set; }
    }

    public class HasPropPrototype : EvalPrototype
    {
        public EvalContext Context { get; set; }
        public ulong Prop { get; set; }
    }

    public class LoadPropPrototype : EvalPrototype
    {
        public EvalContext Context { get; set; }
        public ulong Prop { get; set; }
    }

    public class LoadPropContextParamsPrototype : EvalPrototype
    {
        public EvalContext PropertyCollectionContext { get; set; }
        public ulong Prop { get; set; }
        public EvalContext PropertyIdContext { get; set; }
    }

    public class LoadPropEvalParamsPrototype : EvalPrototype
    {
        public EvalContext Context { get; set; }
        public EvalPrototype Param0 { get; set; }
        public EvalPrototype Param1 { get; set; }
        public EvalPrototype Param2 { get; set; }
        public EvalPrototype Param3 { get; set; }
        public ulong Prop { get; set; }
    }

    public class LoadBoolPrototype : EvalPrototype
    {
        public bool Value { get; set; }
    }

    public class LoadIntPrototype : EvalPrototype
    {
        public int Value { get; set; }
    }

    public class LoadFloatPrototype : EvalPrototype
    {
        public float Value { get; set; }
    }

    public class LoadCurvePrototype : EvalPrototype
    {
        public ulong Curve { get; set; }
        public EvalPrototype Index { get; set; }
    }

    public class LoadAssetRefPrototype : EvalPrototype
    {
        public ulong Value { get; set; }
    }

    public class LoadProtoRefPrototype : EvalPrototype
    {
        public ulong Value { get; set; }
    }

    public class LoadContextIntPrototype : EvalPrototype
    {
        public EvalContext Context { get; set; }
    }

    public class LoadContextProtoRefPrototype : EvalPrototype
    {
        public EvalContext Context { get; set; }
    }

    public class AddPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; set; }
        public EvalPrototype Arg2 { get; set; }
    }

    public class SubPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; set; }
        public EvalPrototype Arg2 { get; set; }
    }

    public class MultPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; set; }
        public EvalPrototype Arg2 { get; set; }
    }

    public class DivPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; set; }
        public EvalPrototype Arg2 { get; set; }
    }

    public class ExponentPrototype : EvalPrototype
    {
        public EvalPrototype BaseArg { get; set; }
        public EvalPrototype ExpArg { get; set; }
    }

    public class ScopePrototype : EvalPrototype
    {
        public EvalPrototype[] Scope { get; set; }
    }

    public class GreaterThanPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; set; }
        public EvalPrototype Arg2 { get; set; }
    }

    public class LessThanPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; set; }
        public EvalPrototype Arg2 { get; set; }
    }

    public class EqualsPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; set; }
        public EvalPrototype Arg2 { get; set; }
        public float Epsilon { get; set; }
    }

    public class AndPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; set; }
        public EvalPrototype Arg2 { get; set; }
    }

    public class OrPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; set; }
        public EvalPrototype Arg2 { get; set; }
    }

    public class NotPrototype : EvalPrototype
    {
        public EvalPrototype Arg { get; set; }
    }

    public class IsContextDataNullPrototype : EvalPrototype
    {
        public EvalContext Context { get; set; }
    }

    public class IfElsePrototype : EvalPrototype
    {
        public EvalPrototype Conditional { get; set; }
        public EvalPrototype EvalIf { get; set; }
        public EvalPrototype EvalElse { get; set; }
    }

    public class DifficultyTierRangePrototype : EvalPrototype
    {
        public EvalContext Context { get; set; }
        public ulong Min { get; set; }
        public ulong Max { get; set; }
    }

    public class MissionIsActivePrototype : EvalPrototype
    {
        public EvalContext Context { get; set; }
        public ulong Mission { get; set; }
    }

    public class GetCombatLevelPrototype : EvalPrototype
    {
        public EvalContext Context { get; set; }
    }

    public class GetPowerRankPrototype : EvalPrototype
    {
        public EvalContext Context { get; set; }
        public ulong Power { get; set; }
    }

    public class CalcPowerRankPrototype : EvalPrototype
    {
        public EvalContext Context { get; set; }
        public ulong Power { get; set; }
    }

    public class GetDamageReductionPctPrototype : EvalPrototype
    {
        public EvalContext Context { get; set; }
        public DamageType VsDamageType { get; set; }
    }

    public class GetDistanceToEntityPrototype : EvalPrototype
    {
        public EvalContext SourceEntity { get; set; }
        public EvalContext TargetEntity { get; set; }
        public bool EdgeToEdge { get; set; }
    }

    public class HasEntityInInventoryPrototype : EvalPrototype
    {
        public EvalContext Context { get; set; }
        public ulong Entity { get; set; }
        public ConvenienceLabel Inventory { get; set; }
    }

    public class IsInPartyPrototype : EvalPrototype
    {
        public EvalContext Context { get; set; }
    }

    public class IsDynamicCombatLevelEnabledPrototype : EvalPrototype
    {
    }

    public class MissionIsCompletePrototype : EvalPrototype
    {
        public EvalContext Context { get; set; }
        public ulong Mission { get; set; }
    }

    public class MaxPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; set; }
        public EvalPrototype Arg2 { get; set; }
    }

    public class MinPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; set; }
        public EvalPrototype Arg2 { get; set; }
    }

    public class ModulusPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; set; }
        public EvalPrototype Arg2 { get; set; }
    }

    public class RandomFloatPrototype : EvalPrototype
    {
        public float Max { get; set; }
        public float Min { get; set; }
    }

    public class RandomIntPrototype : EvalPrototype
    {
        public int Max { get; set; }
        public int Min { get; set; }
    }

    public class ForPrototype : EvalPrototype
    {
        public EvalPrototype LoopAdvance { get; set; }
        public EvalPrototype LoopCondition { get; set; }
        public EvalPrototype LoopVarInit { get; set; }
        public EvalPrototype PostLoop { get; set; }
        public EvalPrototype PreLoop { get; set; }
        public EvalPrototype[] ScopeLoopBody { get; set; }
    }

    public class ForEachConditionInContextPrototype : EvalPrototype
    {
        public EvalPrototype PostLoop { get; set; }
        public EvalPrototype PreLoop { get; set; }
        public EvalPrototype[] ScopeLoopBody { get; set; }
        public EvalPrototype LoopConditionPreScope { get; set; }
        public EvalPrototype LoopConditionPostScope { get; set; }
        public EvalContext ConditionCollectionContext { get; set; }
    }

    public class ForEachProtoRefInContextRefListPrototype : EvalPrototype
    {
        public EvalPrototype PostLoop { get; set; }
        public EvalPrototype PreLoop { get; set; }
        public EvalPrototype[] ScopeLoopBody { get; set; }
        public EvalPrototype LoopCondition { get; set; }
        public EvalContext ProtoRefListContext { get; set; }
    }

    public class LoadEntityToContextVarPrototype : EvalPrototype
    {
        public EvalContext Context { get; set; }
        public EvalPrototype EntityId { get; set; }
    }

    public class LoadConditionCollectionToContextPrototype : EvalPrototype
    {
        public EvalContext Context { get; set; }
        public EvalPrototype EntityId { get; set; }
    }

    public class EntityHasKeywordPrototype : EvalPrototype
    {
        public EvalContext Context { get; set; }
        public ulong Keyword { get; set; }
        public bool ConditionKeywordOnly { get; set; }
    }

    public class EntityHasTalentPrototype : EvalPrototype
    {
        public EvalContext Context { get; set; }
        public ulong Talent { get; set; }
    }
}
