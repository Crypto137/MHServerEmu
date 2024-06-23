using MHServerEmu.Games.GameData.Calligraphy.Attributes;

namespace MHServerEmu.Games.Properties.Evals
{
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

    public enum GetEvalPropertyIdEnum
    {
        PropertyInfoEvalInput,
        Output,
        Input
    }

    public enum EvalOp
    {
        Invalid = 0,
        And = 1,
        Equals = 2,
        GreaterThan = 3,
        IsContextDataNull = 4,
        LessThan = 5,
        DifficultyTierRange = 6,
        MissionIsActive = 7,
        MissionIsComplete = 8,
        Not = 9,
        Or = 10,
        HasEntityInInventory = 11,
        LoadAssetRef = 12,
        LoadBool = 13,
        LoadFloat = 14,
        LoadInt = 15,
        LoadProtoRef = 16,
        LoadContextInt = 17,
        LoadContextProtoRef = 18,
        For = 19,
        ForEachConditionInContext = 20,
        ForEachProtoRefInContextRefList = 21,
        IfElse = 22,
        Scope = 23,
        ExportError = 24,
        LoadCurve = 25,
        Add = 26,
        Div = 27,
        Exponent = 28,
        Max = 29,
        Min = 30,
        Modulus = 31,
        Mult = 32,
        Sub = 33,
        AssignProp = 34,
        AssignPropEvalParams = 35,
        HasProp = 36,
        LoadProp = 37,
        LoadPropContextParams = 38,
        LoadPropEvalParams = 39,
        SwapProp = 40,
        RandomFloat = 41,
        RandomInt = 42,
        LoadEntityToContextVar = 43,
        LoadConditionCollectionToContext = 44,
        EntityHasKeyword = 45,
        EntityHasTalent = 46,
        GetCombatLevel = 47,
        GetPowerRank = 48,
        CalcPowerRank = 49,
        IsInParty = 50,
        GetDamageReductionPct = 51,
        GetDistanceToEntity = 52,
        IsDynamicCombatLevelEnabled = 53,
    }

    public enum EvalReturnType
    {
        Error = 0,
        Undefined = 1,
        Bool = 4,
        Int = 2,
        Float = 3,
        EntityId = 5,
        AssetRef = 8,
        ProtoRef = 7,
        PropertyId = 10,
        PropertyCollectionPtr = 9,
        ProtoRefListPtr = 11,
        ProtoRefVectorPtr = 12,
        RegionId = 6,
        ConditionCollectionPtr = 13,
        EntityPtr = 14,
        EntityGuid = 15,
        AvatarOfPlayerGuid = 16,
    }
}
