using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Eval;

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
        MaxVars = 12,
        LocalStack = 13,
        CallerStack = 14,
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

    public class EvalNodeVisitor
    {
        public virtual void Visit(EvalPrototype visitor) { }
    }

    public class EvalPrototype : Prototype
    {
        [DoNotCopy]
        public EvalOp Op { get; protected set; }

        public virtual void Visit(EvalNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public virtual string ExpressionString()
        {
            return "";
        }
    }

    public class ExportErrorPrototype : EvalPrototype
    {
        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.ExportError;
        }

        public override string ExpressionString()
        {
            return "ExportError";
        }
    }

    public class AssignPropPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public PropertyId Prop { get; protected set; }
        public EvalPrototype Eval { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.AssignProp;
        }

        public override void Visit(EvalNodeVisitor visitor)
        {
            base.Visit(visitor);
            Eval?.Visit(visitor);
        }
    }

    public class SwapPropPrototype : EvalPrototype
    {
        public EvalContext LeftContext { get; protected set; }
        public PropertyId Prop { get; protected set; }
        public EvalContext RightContext { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.SwapProp;
        }
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

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.AssignPropEvalParams;
        }

        public override void Visit(EvalNodeVisitor visitor)
        {
            base.Visit(visitor);
            Param0?.Visit(visitor);
            Param1?.Visit(visitor);
            Param2?.Visit(visitor);
            Param3?.Visit(visitor);
            Eval?.Visit(visitor);
        }
    }

    public class HasPropPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public PropertyId Prop { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.HasProp;
        }
    }

    public class LoadPropPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public PropertyId Prop { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.LoadProp;
        }
    }

    public class LoadPropContextParamsPrototype : EvalPrototype
    {
        public EvalContext PropertyCollectionContext { get; protected set; }
        public PrototypeId Prop { get; protected set; }
        public EvalContext PropertyIdContext { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.LoadPropContextParams;
        }
    }

    public class LoadPropEvalParamsPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public EvalPrototype Param0 { get; protected set; }
        public EvalPrototype Param1 { get; protected set; }
        public EvalPrototype Param2 { get; protected set; }
        public EvalPrototype Param3 { get; protected set; }
        public PrototypeId Prop { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.LoadPropEvalParams;
        }

        public override void Visit(EvalNodeVisitor visitor)
        {
            base.Visit(visitor);
            Param0?.Visit(visitor);
            Param1?.Visit(visitor);
            Param2?.Visit(visitor);
            Param3?.Visit(visitor);
        }
    }

    public class LoadBoolPrototype : EvalPrototype
    {
        public bool Value { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.LoadBool;
        }
    }

    public class LoadIntPrototype : EvalPrototype
    {
        public int Value { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.LoadInt;
        }
    }

    public class LoadFloatPrototype : EvalPrototype
    {
        public float Value { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.LoadFloat;
        }
    }

    public class LoadCurvePrototype : EvalPrototype
    {
        public CurveId Curve { get; protected set; }
        public EvalPrototype Index { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.LoadCurve;
        }

        public override void Visit(EvalNodeVisitor visitor)
        {
            base.Visit(visitor);
            Index?.Visit(visitor);
        }
    }

    public class LoadAssetRefPrototype : EvalPrototype
    {
        public AssetId Value { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.LoadAssetRef;
        }
    }

    public class LoadProtoRefPrototype : EvalPrototype
    {
        public PrototypeId Value { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.LoadProtoRef;
        }
    }

    public class LoadContextIntPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.LoadContextInt;
        }
    }

    public class LoadContextProtoRefPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.LoadContextProtoRef;
        }
    }

    public class AddPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; protected set; }
        public EvalPrototype Arg2 { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.Add;
        }

        public override void Visit(EvalNodeVisitor visitor)
        {
            base.Visit(visitor);
            Arg1?.Visit(visitor);
            Arg2?.Visit(visitor);
        }
    }

    public class SubPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; protected set; }
        public EvalPrototype Arg2 { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.Sub;
        }

        public override void Visit(EvalNodeVisitor visitor)
        {
            base.Visit(visitor);
            Arg1?.Visit(visitor);
            Arg2?.Visit(visitor);
        }
    }

    public class MultPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; protected set; }
        public EvalPrototype Arg2 { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.Mult;
        }

        public override void Visit(EvalNodeVisitor visitor)
        {
            base.Visit(visitor);
            Arg1?.Visit(visitor);
            Arg2?.Visit(visitor);
        }
    }

    public class DivPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; protected set; }
        public EvalPrototype Arg2 { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.Div;
        }

        public override void Visit(EvalNodeVisitor visitor)
        {
            base.Visit(visitor);
            Arg1?.Visit(visitor);
            Arg2?.Visit(visitor);
        }
    }

    public class ExponentPrototype : EvalPrototype
    {
        public EvalPrototype BaseArg { get; protected set; }
        public EvalPrototype ExpArg { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.Exponent;
        }

        public override void Visit(EvalNodeVisitor visitor)
        {
            base.Visit(visitor);
            BaseArg?.Visit(visitor);
            ExpArg?.Visit(visitor);
        }
    }

    public class ScopePrototype : EvalPrototype
    {
        public EvalPrototype[] Scope { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.Scope;
        }

        public override void Visit(EvalNodeVisitor visitor)
        {
            base.Visit(visitor);
            if (Scope.HasValue())
                foreach (var node in Scope)
                    node?.Visit(visitor);
        }
    }

    public class GreaterThanPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; protected set; }
        public EvalPrototype Arg2 { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.GreaterThan;
        }

        public override void Visit(EvalNodeVisitor visitor)
        {
            base.Visit(visitor);
            Arg1?.Visit(visitor);
            Arg2?.Visit(visitor);
        }
    }

    public class LessThanPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; protected set; }
        public EvalPrototype Arg2 { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.LessThan;
        }

        public override void Visit(EvalNodeVisitor visitor)
        {
            base.Visit(visitor);
            Arg1?.Visit(visitor);
            Arg2?.Visit(visitor);
        }
    }

    public class EqualsPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; protected set; }
        public EvalPrototype Arg2 { get; protected set; }
        public float Epsilon { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.Equals;
        }

        public override void Visit(EvalNodeVisitor visitor)
        {
            base.Visit(visitor);
            Arg1?.Visit(visitor);
            Arg2?.Visit(visitor);
        }
    }

    public class AndPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; protected set; }
        public EvalPrototype Arg2 { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.And;
        }

        public override void Visit(EvalNodeVisitor visitor)
        {
            base.Visit(visitor);
            Arg1?.Visit(visitor);
            Arg2?.Visit(visitor);
        }
    }

    public class OrPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; protected set; }
        public EvalPrototype Arg2 { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.Or;
        }

        public override void Visit(EvalNodeVisitor visitor)
        {
            base.Visit(visitor);
            Arg1?.Visit(visitor);
            Arg2?.Visit(visitor);
        }
    }

    public class NotPrototype : EvalPrototype
    {
        public EvalPrototype Arg { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.Not;
        }

        public override void Visit(EvalNodeVisitor visitor)
        {
            base.Visit(visitor);
            Arg?.Visit(visitor);
        }
    }

    public class IsContextDataNullPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.IsContextDataNull;
        }
    }

    public class IfElsePrototype : EvalPrototype
    {
        public EvalPrototype Conditional { get; protected set; }
        public EvalPrototype EvalIf { get; protected set; }
        public EvalPrototype EvalElse { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.IfElse;
        }

        public override void Visit(EvalNodeVisitor visitor)
        {
            base.Visit(visitor);
            Conditional?.Visit(visitor);
            EvalIf?.Visit(visitor);
            EvalElse?.Visit(visitor);
        }
    }

    public class DifficultyTierRangePrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public PrototypeId Min { get; protected set; }
        public PrototypeId Max { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.DifficultyTierRange;
        }
    }

    public class MissionIsActivePrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public PrototypeId Mission { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.MissionIsActive;
        }
    }

    public class GetCombatLevelPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.GetCombatLevel;
        }
    }

    public class GetPowerRankPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public PrototypeId Power { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.GetPowerRank;
        }
    }

    public class CalcPowerRankPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public PrototypeId Power { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.CalcPowerRank;
        }
    }

    public class GetDamageReductionPctPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public DamageType VsDamageType { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.GetDamageReductionPct;
        }
    }

    public class GetDistanceToEntityPrototype : EvalPrototype
    {
        public EvalContext SourceEntity { get; protected set; }
        public EvalContext TargetEntity { get; protected set; }
        public bool EdgeToEdge { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.GetDistanceToEntity;
        }
    }

    public class HasEntityInInventoryPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public PrototypeId Entity { get; protected set; }
        public InventoryConvenienceLabel Inventory { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.HasEntityInInventory;
        }
    }

    public class IsInPartyPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.IsInParty;
        }
    }

    public class IsDynamicCombatLevelEnabledPrototype : EvalPrototype
    {
        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.IsDynamicCombatLevelEnabled;
        }
    }

    public class MissionIsCompletePrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public PrototypeId Mission { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.MissionIsComplete;
        }
    }

    public class MaxPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; protected set; }
        public EvalPrototype Arg2 { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.Max;
        }

        public override void Visit(EvalNodeVisitor visitor)
        {
            base.Visit(visitor);
            Arg1?.Visit(visitor);
            Arg2?.Visit(visitor);
        }
    }

    public class MinPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; protected set; }
        public EvalPrototype Arg2 { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.Min;
        }

        public override void Visit(EvalNodeVisitor visitor)
        {
            base.Visit(visitor);
            Arg1?.Visit(visitor);
            Arg2?.Visit(visitor);
        }
    }

    public class ModulusPrototype : EvalPrototype
    {
        public EvalPrototype Arg1 { get; protected set; }
        public EvalPrototype Arg2 { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.Modulus;
        }

        public override void Visit(EvalNodeVisitor visitor)
        {
            base.Visit(visitor);
            Arg1?.Visit(visitor);
            Arg2?.Visit(visitor);
        }
    }

    public class RandomFloatPrototype : EvalPrototype
    {
        public float Max { get; protected set; }
        public float Min { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.RandomFloat;
        }
    }

    public class RandomIntPrototype : EvalPrototype
    {
        public int Max { get; protected set; }
        public int Min { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.RandomInt;
        }
    }

    public class ForPrototype : EvalPrototype
    {
        public EvalPrototype LoopAdvance { get; protected set; }
        public EvalPrototype LoopCondition { get; protected set; }
        public EvalPrototype LoopVarInit { get; protected set; }
        public EvalPrototype PostLoop { get; protected set; }
        public EvalPrototype PreLoop { get; protected set; }
        public EvalPrototype[] ScopeLoopBody { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.For;
        }

        public override void Visit(EvalNodeVisitor visitor)
        {
            base.Visit(visitor);
            LoopAdvance?.Visit(visitor);
            LoopCondition?.Visit(visitor);
            LoopVarInit?.Visit(visitor);
            PostLoop?.Visit(visitor);
            PreLoop?.Visit(visitor);
            if (ScopeLoopBody.HasValue())
                foreach (var node in ScopeLoopBody)
                    node?.Visit(visitor);
        }
    }

    public class ForEachConditionInContextPrototype : EvalPrototype
    {
        public EvalPrototype PostLoop { get; protected set; }
        public EvalPrototype PreLoop { get; protected set; }
        public EvalPrototype[] ScopeLoopBody { get; protected set; }
        public EvalPrototype LoopConditionPreScope { get; protected set; }
        public EvalPrototype LoopConditionPostScope { get; protected set; }
        public EvalContext ConditionCollectionContext { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.ForEachConditionInContext;
        }

        public override void Visit(EvalNodeVisitor visitor)
        {
            base.Visit(visitor);
            LoopConditionPreScope?.Visit(visitor);
            LoopConditionPostScope?.Visit(visitor);
            PostLoop?.Visit(visitor);
            PreLoop?.Visit(visitor);
            if (ScopeLoopBody.HasValue())
                foreach (var node in ScopeLoopBody)
                    node?.Visit(visitor);
        }
    }

    public class ForEachProtoRefInContextRefListPrototype : EvalPrototype
    {
        public EvalPrototype PostLoop { get; protected set; }
        public EvalPrototype PreLoop { get; protected set; }
        public EvalPrototype[] ScopeLoopBody { get; protected set; }
        public EvalPrototype LoopCondition { get; protected set; }
        public EvalContext ProtoRefListContext { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.ForEachProtoRefInContextRefList;
        }

        public override void Visit(EvalNodeVisitor visitor)
        {
            base.Visit(visitor);
            LoopCondition?.Visit(visitor);
            PostLoop?.Visit(visitor);
            PreLoop?.Visit(visitor);
            if (ScopeLoopBody.HasValue())
                foreach (var node in ScopeLoopBody)
                    node?.Visit(visitor);
        }
    }

    public class LoadEntityToContextVarPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public EvalPrototype EntityId { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.LoadEntityToContextVar;
        }

        public override void Visit(EvalNodeVisitor visitor)
        {
            base.Visit(visitor);
            EntityId?.Visit(visitor);
        }
    }

    public class LoadConditionCollectionToContextPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public EvalPrototype EntityId { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.LoadConditionCollectionToContext;
        }

        public override void Visit(EvalNodeVisitor visitor)
        {
            base.Visit(visitor);
            EntityId?.Visit(visitor);
        }
    }

    public class EntityHasKeywordPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public PrototypeId Keyword { get; protected set; }
        public bool ConditionKeywordOnly { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.EntityHasKeyword;
        }
    }

    public class EntityHasTalentPrototype : EvalPrototype
    {
        public EvalContext Context { get; protected set; }
        public PrototypeId Talent { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Op = EvalOp.EntityHasTalent;
        }
    }
}
