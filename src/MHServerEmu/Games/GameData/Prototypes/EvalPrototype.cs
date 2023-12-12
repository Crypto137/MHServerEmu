namespace MHServerEmu.Games.GameData.Prototypes
{
    public class EvalPrototype : Prototype
    {
        public EvalPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EvalPrototype), proto); }
    }

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

    public enum DamageType
    {
        Physical = 0,
        Energy = 1,
        Mental = 2,
        Any = 4,
    }

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

    public class ExportErrorPrototype : EvalPrototype
    {
        public ExportErrorPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ExportErrorPrototype), proto); }
    }

    public class AssignPropPrototype : EvalPrototype
    {
        public EvalContext Context;
        public ulong Prop;
        public EvalPrototype Eval;
        public AssignPropPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AssignPropPrototype), proto); }
    }

    public class SwapPropPrototype : EvalPrototype
    {
        public EvalContext LeftContext;
        public ulong Prop;
        public EvalContext RightContext;
        public SwapPropPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SwapPropPrototype), proto); }
    }

    public class AssignPropEvalParamsPrototype : EvalPrototype
    {
        public EvalContext Context;
        public EvalPrototype Param0;
        public EvalPrototype Param1;
        public EvalPrototype Param2;
        public EvalPrototype Param3;
        public ulong Prop;
        public EvalPrototype Eval;
        public AssignPropEvalParamsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AssignPropEvalParamsPrototype), proto); }
    }

    public class HasPropPrototype : EvalPrototype
    {
        public EvalContext Context;
        public ulong Prop;
        public HasPropPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(HasPropPrototype), proto); }
    }

    public class LoadPropPrototype : EvalPrototype
    {
        public EvalContext Context;
        public ulong Prop;
        public LoadPropPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LoadPropPrototype), proto); }
    }

    public class LoadPropContextParamsPrototype : EvalPrototype
    {
        public EvalContext PropertyCollectionContext;
        public ulong Prop;
        public EvalContext PropertyIdContext;
        public LoadPropContextParamsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LoadPropContextParamsPrototype), proto); }
    }

    public class LoadPropEvalParamsPrototype : EvalPrototype
    {
        public EvalContext Context;
        public EvalPrototype Param0;
        public EvalPrototype Param1;
        public EvalPrototype Param2;
        public EvalPrototype Param3;
        public ulong Prop;
        public LoadPropEvalParamsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LoadPropEvalParamsPrototype), proto); }
    }

    public class LoadBoolPrototype : EvalPrototype
    {
        public bool Value;
        public LoadBoolPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LoadBoolPrototype), proto); }
    }

    public class LoadIntPrototype : EvalPrototype
    {
        public int Value;
        public LoadIntPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LoadIntPrototype), proto); }
    }

    public class LoadFloatPrototype : EvalPrototype
    {
        public float Value;
        public LoadFloatPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LoadFloatPrototype), proto); }
    }

    public class LoadCurvePrototype : EvalPrototype
    {
        public ulong Curve;
        public EvalPrototype Index;
        public LoadCurvePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LoadCurvePrototype), proto); }
    }

    public class LoadAssetRefPrototype : EvalPrototype
    {
        public ulong Value;
        public LoadAssetRefPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LoadAssetRefPrototype), proto); }
    }

    public class LoadProtoRefPrototype : EvalPrototype
    {
        public ulong Value;
        public LoadProtoRefPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LoadProtoRefPrototype), proto); }
    }

    public class LoadContextIntPrototype : EvalPrototype
    {
        public EvalContext Context;
        public LoadContextIntPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LoadContextIntPrototype), proto); }
    }

    public class LoadContextProtoRefPrototype : EvalPrototype
    {
        public EvalContext Context;
        public LoadContextProtoRefPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LoadContextProtoRefPrototype), proto); }
    }

    public class AddPrototype : EvalPrototype
    {
        public EvalPrototype Arg1;
        public EvalPrototype Arg2;
        public AddPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AddPrototype), proto); }
    }

    public class SubPrototype : EvalPrototype
    {
        public EvalPrototype Arg1;
        public EvalPrototype Arg2;
        public SubPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SubPrototype), proto); }
    }

    public class MultPrototype : EvalPrototype
    {
        public EvalPrototype Arg1;
        public EvalPrototype Arg2;
        public MultPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MultPrototype), proto); }
    }

    public class DivPrototype : EvalPrototype
    {
        public EvalPrototype Arg1;
        public EvalPrototype Arg2;
        public DivPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(DivPrototype), proto); }
    }

    public class ExponentPrototype : EvalPrototype
    {
        public EvalPrototype BaseArg;
        public EvalPrototype ExpArg;
        public ExponentPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ExponentPrototype), proto); }
    }

    public class ScopePrototype : EvalPrototype
    {
        public EvalPrototype[] Scope;
        public ScopePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScopePrototype), proto); }
    }

    public class GreaterThanPrototype : EvalPrototype
    {
        public EvalPrototype Arg1;
        public EvalPrototype Arg2;
        public GreaterThanPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(GreaterThanPrototype), proto); }
    }

    public class LessThanPrototype : EvalPrototype
    {
        public EvalPrototype Arg1;
        public EvalPrototype Arg2;
        public LessThanPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LessThanPrototype), proto); }
    }

    public class EqualsPrototype : EvalPrototype
    {
        public EvalPrototype Arg1;
        public EvalPrototype Arg2;
        public float Epsilon;
        public EqualsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EqualsPrototype), proto); }
    }

    public class AndPrototype : EvalPrototype
    {
        public EvalPrototype Arg1;
        public EvalPrototype Arg2;
        public AndPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AndPrototype), proto); }
    }

    public class OrPrototype : EvalPrototype
    {
        public EvalPrototype Arg1;
        public EvalPrototype Arg2;
        public OrPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(OrPrototype), proto); }
    }

    public class NotPrototype : EvalPrototype
    {
        public EvalPrototype Arg;
        public NotPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(NotPrototype), proto); }
    }

    public class IsContextDataNullPrototype : EvalPrototype
    {
        public EvalContext Context;
        public IsContextDataNullPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(IsContextDataNullPrototype), proto); }
    }

    public class IfElsePrototype : EvalPrototype
    {
        public EvalPrototype Conditional;
        public EvalPrototype EvalIf;
        public EvalPrototype EvalElse;
        public IfElsePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(IfElsePrototype), proto); }
    }

    public class DifficultyTierRangePrototype : EvalPrototype
    {
        public EvalContext Context;
        public ulong Min;
        public ulong Max;
        public DifficultyTierRangePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(DifficultyTierRangePrototype), proto); }
    }

    public class MissionIsActivePrototype : EvalPrototype
    {
        public EvalContext Context;
        public ulong Mission;
        public MissionIsActivePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionIsActivePrototype), proto); }
    }

    public class GetCombatLevelPrototype : EvalPrototype
    {
        public EvalContext Context;
        public GetCombatLevelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(GetCombatLevelPrototype), proto); }
    }

    public class GetPowerRankPrototype : EvalPrototype
    {
        public EvalContext Context;
        public ulong Power;
        public GetPowerRankPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(GetPowerRankPrototype), proto); }
    }

    public class CalcPowerRankPrototype : EvalPrototype
    {
        public EvalContext Context;
        public ulong Power;
        public CalcPowerRankPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(CalcPowerRankPrototype), proto); }
    }

    public class GetDamageReductionPctPrototype : EvalPrototype
    {
        public EvalContext Context;
        public DamageType VsDamageType;
        public GetDamageReductionPctPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(GetDamageReductionPctPrototype), proto); }
    }

    public class GetDistanceToEntityPrototype : EvalPrototype
    {
        public EvalContext SourceEntity;
        public EvalContext TargetEntity;
        public bool EdgeToEdge;
        public GetDistanceToEntityPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(GetDistanceToEntityPrototype), proto); }
    }

    public class HasEntityInInventoryPrototype : EvalPrototype
    {
        public EvalContext Context;
        public ulong Entity;
        public ConvenienceLabel Inventory;
        public HasEntityInInventoryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(HasEntityInInventoryPrototype), proto); }
    }

    public class IsInPartyPrototype : EvalPrototype
    {
        public EvalContext Context;
        public IsInPartyPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(IsInPartyPrototype), proto); }
    }

    public class IsDynamicCombatLevelEnabledPrototype : EvalPrototype
    {

        public IsDynamicCombatLevelEnabledPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(IsDynamicCombatLevelEnabledPrototype), proto); }
    }

    public class MissionIsCompletePrototype : EvalPrototype
    {
        public EvalContext Context;
        public ulong Mission;
        public MissionIsCompletePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionIsCompletePrototype), proto); }
    }

    public class MaxPrototype : EvalPrototype
    {
        public EvalPrototype Arg1;
        public EvalPrototype Arg2;
        public MaxPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MaxPrototype), proto); }
    }

    public class MinPrototype : EvalPrototype
    {
        public EvalPrototype Arg1;
        public EvalPrototype Arg2;
        public MinPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MinPrototype), proto); }
    }

    public class ModulusPrototype : EvalPrototype
    {
        public EvalPrototype Arg1;
        public EvalPrototype Arg2;
        public ModulusPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ModulusPrototype), proto); }
    }

    public class RandomFloatPrototype : EvalPrototype
    {
        public float Max;
        public float Min;
        public RandomFloatPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RandomFloatPrototype), proto); }
    }

    public class RandomIntPrototype : EvalPrototype
    {
        public int Max;
        public int Min;
        public RandomIntPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RandomIntPrototype), proto); }
    }

    public class ForPrototype : EvalPrototype
    {
        public EvalPrototype LoopAdvance;
        public EvalPrototype LoopCondition;
        public EvalPrototype LoopVarInit;
        public EvalPrototype PostLoop;
        public EvalPrototype PreLoop;
        public EvalPrototype[] ScopeLoopBody;
        public ForPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ForPrototype), proto); }
    }

    public class ForEachConditionInContextPrototype : EvalPrototype
    {
        public EvalPrototype PostLoop;
        public EvalPrototype PreLoop;
        public EvalPrototype[] ScopeLoopBody;
        public EvalPrototype LoopConditionPreScope;
        public EvalPrototype LoopConditionPostScope;
        public EvalContext ConditionCollectionContext;
        public ForEachConditionInContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ForEachConditionInContextPrototype), proto); }
    }

    public class ForEachProtoRefInContextRefListPrototype : EvalPrototype
    {
        public EvalPrototype PostLoop;
        public EvalPrototype PreLoop;
        public EvalPrototype[] ScopeLoopBody;
        public EvalPrototype LoopCondition;
        public EvalContext ProtoRefListContext;
        public ForEachProtoRefInContextRefListPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ForEachProtoRefInContextRefListPrototype), proto); }
    }

    public class LoadEntityToContextVarPrototype : EvalPrototype
    {
        public EvalContext Context;
        public EvalPrototype EntityId;
        public LoadEntityToContextVarPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LoadEntityToContextVarPrototype), proto); }
    }

    public class LoadConditionCollectionToContextPrototype : EvalPrototype
    {
        public EvalContext Context;
        public EvalPrototype EntityId;
        public LoadConditionCollectionToContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LoadConditionCollectionToContextPrototype), proto); }
    }

    public class EntityHasKeywordPrototype : EvalPrototype
    {
        public EvalContext Context;
        public KeywordPrototype Keyword;
        public bool ConditionKeywordOnly;
        public EntityHasKeywordPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityHasKeywordPrototype), proto); }
    }

    public class EntityHasTalentPrototype : EvalPrototype
    {
        public EvalContext Context;
        public ulong Talent;
        public EntityHasTalentPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityHasTalentPrototype), proto); }
    }


}
