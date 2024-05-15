using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)Default)]
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

    [AssetEnum((int)Physical)]
    public enum DamageType
    {
        Physical = 0,
        Energy = 1,
        Mental = 2,
        Any = 4,
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
        public EvalContext Context { get; protected set; }
        public PropertyId Prop { get; protected set; }
        public EvalPrototype Eval { get; protected set; }
    }

    public class SwapPropPrototype : EvalPrototype
    {
        public EvalContext LeftContext { get; protected set; }
        public PropertyId Prop { get; protected set; }
        public EvalContext RightContext { get; protected set; }
    }

    public class AssignPropEvalParamsPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public EvalPrototype Param0 { get; protected set; }
        public EvalPrototype Param1 { get; protected set; }
        public EvalPrototype Param2 { get; protected set; }
        public EvalPrototype Param3 { get; protected set; }
        public PrototypeId Prop { get; protected set; }
        public EvalPrototype Eval { get; protected set; }
    }

    public class HasPropPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public PropertyId Prop { get; protected set; }
    }

    public class LoadPropPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public PropertyId Prop { get; protected set; }
    }

    public class LoadPropContextParamsPrototype : EvalPrototype
    {
        public EvalContext PropertyCollectionContext { get; protected set; }
        public PrototypeId Prop { get; protected set; }
        public EvalContext PropertyIdContext { get; protected set; }
    }

    public class LoadPropEvalParamsPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public EvalPrototype Param0 { get; protected set; }
        public EvalPrototype Param1 { get; protected set; }
        public EvalPrototype Param2 { get; protected set; }
        public EvalPrototype Param3 { get; protected set; }
        public PrototypeId Prop { get; protected set; }
    }

    public class LoadBoolPrototype : EvalPrototype
    {
        public bool Value { get; protected set; }
    }

    public class LoadIntPrototype : EvalPrototype
    {
        public int Value { get; protected set; }
    }

    public class LoadFloatPrototype : EvalPrototype
    {
        public float Value { get; protected set; }
    }

    public class LoadCurvePrototype : EvalPrototype
    {
        public CurveId Curve { get; protected set; }
        public EvalPrototype Index { get; protected set; }
    }

    public class LoadAssetRefPrototype : EvalPrototype
    {
        public AssetId Value { get; protected set; }
    }

    public class LoadProtoRefPrototype : EvalPrototype
    {
        public PrototypeId Value { get; protected set; }
    }

    public class LoadContextIntPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
    }

    public class LoadContextProtoRefPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
    }

    public class AddPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; protected set; }
        public EvalPrototype Arg2 { get; protected set; }
    }

    public class SubPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; protected set; }
        public EvalPrototype Arg2 { get; protected set; }
    }

    public class MultPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; protected set; }
        public EvalPrototype Arg2 { get; protected set; }
    }

    public class DivPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; protected set; }
        public EvalPrototype Arg2 { get; protected set; }
    }

    public class ExponentPrototype : EvalPrototype
    {
        public EvalPrototype BaseArg { get; protected set; }
        public EvalPrototype ExpArg { get; protected set; }
    }

    public class ScopePrototype : EvalPrototype
    {
        public EvalPrototype[] Scope { get; protected set; }
    }

    public class GreaterThanPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; protected set; }
        public EvalPrototype Arg2 { get; protected set; }
    }

    public class LessThanPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; protected set; }
        public EvalPrototype Arg2 { get; protected set; }
    }

    public class EqualsPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; protected set; }
        public EvalPrototype Arg2 { get; protected set; }
        public float Epsilon { get; protected set; }
    }

    public class AndPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; protected set; }
        public EvalPrototype Arg2 { get; protected set; }
    }

    public class OrPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; protected set; }
        public EvalPrototype Arg2 { get; protected set; }
    }

    public class NotPrototype : EvalPrototype
    {
        public EvalPrototype Arg { get; protected set; }
    }

    public class IsContextDataNullPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
    }

    public class IfElsePrototype : EvalPrototype
    {
        public EvalPrototype Conditional { get; protected set; }
        public EvalPrototype EvalIf { get; protected set; }
        public EvalPrototype EvalElse { get; protected set; }
    }

    public class DifficultyTierRangePrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public PrototypeId Min { get; protected set; }
        public PrototypeId Max { get; protected set; }
    }

    public class MissionIsActivePrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public PrototypeId Mission { get; protected set; }
    }

    public class GetCombatLevelPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
    }

    public class GetPowerRankPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public PrototypeId Power { get; protected set; }
    }

    public class CalcPowerRankPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public PrototypeId Power { get; protected set; }
    }

    public class GetDamageReductionPctPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public DamageType VsDamageType { get; protected set; }
    }

    public class GetDistanceToEntityPrototype : EvalPrototype
    {
        public EvalContext SourceEntity { get; protected set; }
        public EvalContext TargetEntity { get; protected set; }
        public bool EdgeToEdge { get; protected set; }
    }

    public class HasEntityInInventoryPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public PrototypeId Entity { get; protected set; }
        public InventoryConvenienceLabel Inventory { get; protected set; }
    }

    public class IsInPartyPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
    }

    public class IsDynamicCombatLevelEnabledPrototype : EvalPrototype
    {
    }

    public class MissionIsCompletePrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public PrototypeId Mission { get; protected set; }
    }

    public class MaxPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; protected set; }
        public EvalPrototype Arg2 { get; protected set; }
    }

    public class MinPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; protected set; }
        public EvalPrototype Arg2 { get; protected set; }
    }

    public class ModulusPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; protected set; }
        public EvalPrototype Arg2 { get; protected set; }
    }

    public class RandomFloatPrototype : EvalPrototype
    {
        public float Max { get; protected set; }
        public float Min { get; protected set; }
    }

    public class RandomIntPrototype : EvalPrototype
    {
        public int Max { get; protected set; }
        public int Min { get; protected set; }
    }

    public class ForPrototype : EvalPrototype
    {
        public EvalPrototype LoopAdvance { get; protected set; }
        public EvalPrototype LoopCondition { get; protected set; }
        public EvalPrototype LoopVarInit { get; protected set; }
        public EvalPrototype PostLoop { get; protected set; }
        public EvalPrototype PreLoop { get; protected set; }
        public EvalPrototype[] ScopeLoopBody { get; protected set; }
    }

    public class ForEachConditionInContextPrototype : EvalPrototype
    {
        public EvalPrototype PostLoop { get; protected set; }
        public EvalPrototype PreLoop { get; protected set; }
        public EvalPrototype[] ScopeLoopBody { get; protected set; }
        public EvalPrototype LoopConditionPreScope { get; protected set; }
        public EvalPrototype LoopConditionPostScope { get; protected set; }
        public EvalContext ConditionCollectionContext { get; protected set; }
    }

    public class ForEachProtoRefInContextRefListPrototype : EvalPrototype
    {
        public EvalPrototype PostLoop { get; protected set; }
        public EvalPrototype PreLoop { get; protected set; }
        public EvalPrototype[] ScopeLoopBody { get; protected set; }
        public EvalPrototype LoopCondition { get; protected set; }
        public EvalContext ProtoRefListContext { get; protected set; }
    }

    public class LoadEntityToContextVarPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public EvalPrototype EntityId { get; protected set; }
    }

    public class LoadConditionCollectionToContextPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public EvalPrototype EntityId { get; protected set; }
    }

    public class EntityHasKeywordPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public PrototypeId Keyword { get; protected set; }
        public bool ConditionKeywordOnly { get; protected set; }
    }

    public class EntityHasTalentPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public PrototypeId Talent { get; protected set; }
    }
}
