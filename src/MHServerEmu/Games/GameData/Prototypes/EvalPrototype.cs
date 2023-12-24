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
        public EvalContext Context { get; private set; }
        public ulong Prop { get; private set; }
        public EvalPrototype Eval { get; private set; }
    }

    public class SwapPropPrototype : EvalPrototype
    {
        public EvalContext LeftContext { get; private set; }
        public ulong Prop { get; private set; }
        public EvalContext RightContext { get; private set; }
    }

    public class AssignPropEvalParamsPrototype : EvalPrototype
    {
        public EvalContext Context { get; private set; }
        public EvalPrototype Param0 { get; private set; }
        public EvalPrototype Param1 { get; private set; }
        public EvalPrototype Param2 { get; private set; }
        public EvalPrototype Param3 { get; private set; }
        public ulong Prop { get; private set; }
        public EvalPrototype Eval { get; private set; }
    }

    public class HasPropPrototype : EvalPrototype
    {
        public EvalContext Context { get; private set; }
        public ulong Prop { get; private set; }
    }

    public class LoadPropPrototype : EvalPrototype
    {
        public EvalContext Context { get; private set; }
        public ulong Prop { get; private set; }
    }

    public class LoadPropContextParamsPrototype : EvalPrototype
    {
        public EvalContext PropertyCollectionContext { get; private set; }
        public ulong Prop { get; private set; }
        public EvalContext PropertyIdContext { get; private set; }
    }

    public class LoadPropEvalParamsPrototype : EvalPrototype
    {
        public EvalContext Context { get; private set; }
        public EvalPrototype Param0 { get; private set; }
        public EvalPrototype Param1 { get; private set; }
        public EvalPrototype Param2 { get; private set; }
        public EvalPrototype Param3 { get; private set; }
        public ulong Prop { get; private set; }
    }

    public class LoadBoolPrototype : EvalPrototype
    {
        public bool Value { get; private set; }
    }

    public class LoadIntPrototype : EvalPrototype
    {
        public int Value { get; private set; }
    }

    public class LoadFloatPrototype : EvalPrototype
    {
        public float Value { get; private set; }
    }

    public class LoadCurvePrototype : EvalPrototype
    {
        public ulong Curve { get; private set; }
        public EvalPrototype Index { get; private set; }
    }

    public class LoadAssetRefPrototype : EvalPrototype
    {
        public ulong Value { get; private set; }
    }

    public class LoadProtoRefPrototype : EvalPrototype
    {
        public ulong Value { get; private set; }
    }

    public class LoadContextIntPrototype : EvalPrototype
    {
        public EvalContext Context { get; private set; }
    }

    public class LoadContextProtoRefPrototype : EvalPrototype
    {
        public EvalContext Context { get; private set; }
    }

    public class AddPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; private set; }
        public EvalPrototype Arg2 { get; private set; }
    }

    public class SubPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; private set; }
        public EvalPrototype Arg2 { get; private set; }
    }

    public class MultPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; private set; }
        public EvalPrototype Arg2 { get; private set; }
    }

    public class DivPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; private set; }
        public EvalPrototype Arg2 { get; private set; }
    }

    public class ExponentPrototype : EvalPrototype
    {
        public EvalPrototype BaseArg { get; private set; }
        public EvalPrototype ExpArg { get; private set; }
    }

    public class ScopePrototype : EvalPrototype
    {
        public EvalPrototype[] Scope { get; private set; }
    }

    public class GreaterThanPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; private set; }
        public EvalPrototype Arg2 { get; private set; }
    }

    public class LessThanPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; private set; }
        public EvalPrototype Arg2 { get; private set; }
    }

    public class EqualsPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; private set; }
        public EvalPrototype Arg2 { get; private set; }
        public float Epsilon { get; private set; }
    }

    public class AndPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; private set; }
        public EvalPrototype Arg2 { get; private set; }
    }

    public class OrPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; private set; }
        public EvalPrototype Arg2 { get; private set; }
    }

    public class NotPrototype : EvalPrototype
    {
        public EvalPrototype Arg { get; private set; }
    }

    public class IsContextDataNullPrototype : EvalPrototype
    {
        public EvalContext Context { get; private set; }
    }

    public class IfElsePrototype : EvalPrototype
    {
        public EvalPrototype Conditional { get; private set; }
        public EvalPrototype EvalIf { get; private set; }
        public EvalPrototype EvalElse { get; private set; }
    }

    public class DifficultyTierRangePrototype : EvalPrototype
    {
        public EvalContext Context { get; private set; }
        public ulong Min { get; private set; }
        public ulong Max { get; private set; }
    }

    public class MissionIsActivePrototype : EvalPrototype
    {
        public EvalContext Context { get; private set; }
        public ulong Mission { get; private set; }
    }

    public class GetCombatLevelPrototype : EvalPrototype
    {
        public EvalContext Context { get; private set; }
    }

    public class GetPowerRankPrototype : EvalPrototype
    {
        public EvalContext Context { get; private set; }
        public ulong Power { get; private set; }
    }

    public class CalcPowerRankPrototype : EvalPrototype
    {
        public EvalContext Context { get; private set; }
        public ulong Power { get; private set; }
    }

    public class GetDamageReductionPctPrototype : EvalPrototype
    {
        public EvalContext Context { get; private set; }
        public DamageType VsDamageType { get; private set; }
    }

    public class GetDistanceToEntityPrototype : EvalPrototype
    {
        public EvalContext SourceEntity { get; private set; }
        public EvalContext TargetEntity { get; private set; }
        public bool EdgeToEdge { get; private set; }
    }

    public class HasEntityInInventoryPrototype : EvalPrototype
    {
        public EvalContext Context { get; private set; }
        public ulong Entity { get; private set; }
        public ConvenienceLabel Inventory { get; private set; }
    }

    public class IsInPartyPrototype : EvalPrototype
    {
        public EvalContext Context { get; private set; }
    }

    public class IsDynamicCombatLevelEnabledPrototype : EvalPrototype
    {
    }

    public class MissionIsCompletePrototype : EvalPrototype
    {
        public EvalContext Context { get; private set; }
        public ulong Mission { get; private set; }
    }

    public class MaxPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; private set; }
        public EvalPrototype Arg2 { get; private set; }
    }

    public class MinPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; private set; }
        public EvalPrototype Arg2 { get; private set; }
    }

    public class ModulusPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; private set; }
        public EvalPrototype Arg2 { get; private set; }
    }

    public class RandomFloatPrototype : EvalPrototype
    {
        public float Max { get; private set; }
        public float Min { get; private set; }
    }

    public class RandomIntPrototype : EvalPrototype
    {
        public int Max { get; private set; }
        public int Min { get; private set; }
    }

    public class ForPrototype : EvalPrototype
    {
        public EvalPrototype LoopAdvance { get; private set; }
        public EvalPrototype LoopCondition { get; private set; }
        public EvalPrototype LoopVarInit { get; private set; }
        public EvalPrototype PostLoop { get; private set; }
        public EvalPrototype PreLoop { get; private set; }
        public EvalPrototype[] ScopeLoopBody { get; private set; }
    }

    public class ForEachConditionInContextPrototype : EvalPrototype
    {
        public EvalPrototype PostLoop { get; private set; }
        public EvalPrototype PreLoop { get; private set; }
        public EvalPrototype[] ScopeLoopBody { get; private set; }
        public EvalPrototype LoopConditionPreScope { get; private set; }
        public EvalPrototype LoopConditionPostScope { get; private set; }
        public EvalContext ConditionCollectionContext { get; private set; }
    }

    public class ForEachProtoRefInContextRefListPrototype : EvalPrototype
    {
        public EvalPrototype PostLoop { get; private set; }
        public EvalPrototype PreLoop { get; private set; }
        public EvalPrototype[] ScopeLoopBody { get; private set; }
        public EvalPrototype LoopCondition { get; private set; }
        public EvalContext ProtoRefListContext { get; private set; }
    }

    public class LoadEntityToContextVarPrototype : EvalPrototype
    {
        public EvalContext Context { get; private set; }
        public EvalPrototype EntityId { get; private set; }
    }

    public class LoadConditionCollectionToContextPrototype : EvalPrototype
    {
        public EvalContext Context { get; private set; }
        public EvalPrototype EntityId { get; private set; }
    }

    public class EntityHasKeywordPrototype : EvalPrototype
    {
        public EvalContext Context { get; private set; }
        public ulong Keyword { get; private set; }
        public bool ConditionKeywordOnly { get; private set; }
    }

    public class EntityHasTalentPrototype : EvalPrototype
    {
        public EvalContext Context { get; private set; }
        public ulong Talent { get; private set; }
    }
}
