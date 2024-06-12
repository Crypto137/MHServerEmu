using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Properties.Eval
{
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

    public class EvalContextData
    {
        public Game Game;
        public EvalContextVar[] Vars = new EvalContextVar[(int)EvalContext.MaxVars];

        public PropertyCollection CallerStackProperties { get; internal set; }
        public PropertyCollection LocalStackProperties { get; internal set; }

        public EvalContextData(Game game)
        {
            Game = game ?? Game.Current;
            for (int i = 0; i < (int)EvalContext.MaxVars; i++)
                Vars[i] = new EvalContextVar();
        }

        public void SetVar_Bool(EvalContext context, bool value)
        {
            if (context >= EvalContext.MaxVars) return;
            Vars[(int)context].ReadOnly = false;
            Vars[(int)context].Var.SetBool(value);
        }

        public void SetVar_Int(EvalContext context, long value)
        {
            if (context >= EvalContext.MaxVars) return;
            Vars[(int)context].ReadOnly = false;
            Vars[(int)context].Var.SetInt(value);
        }

        public void SetVar_Float(EvalContext context, float value)
        {
            if (context >= EvalContext.MaxVars) return;
            Vars[(int)context].ReadOnly = false;
            Vars[(int)context].Var.SetFloat(value);
        }

        public virtual void SetVar_AssetRef(EvalContext context, AssetId value)
        {
            if (context >= EvalContext.MaxVars) return;
            Vars[(int)context].ReadOnly = false;
            Vars[(int)context].Var.SetAssetRef(value);
        }

        public virtual void SetReadOnlyVar_AssetRef(EvalContext context, AssetId value)
        {
            if (context >= EvalContext.MaxVars) return;
            Vars[(int)context].ReadOnly = true;
            Vars[(int)context].Var.SetAssetRef(value);
        }

        public virtual void SetVar_ProtoRef(EvalContext context, PrototypeId value)
        {
            if (context >= EvalContext.MaxVars) return;
            Vars[(int)context].ReadOnly = false;
            Vars[(int)context].Var.SetProtoRef(value);
        }

        public virtual void SetReadOnlyVar_ProtoRef(EvalContext context, PrototypeId value)
        {
            if (context >= EvalContext.MaxVars) return;
            Vars[(int)context].ReadOnly = true;
            Vars[(int)context].Var.SetProtoRef(value);
        }

        public virtual void SetVar_PropertyId(EvalContext context, PropertyId value)
        {
            if (context >= EvalContext.MaxVars) return;
            Vars[(int)context].ReadOnly = false;
            Vars[(int)context].Var.SetPropertyId(value);
        }

        public virtual void SetReadOnlyVar_PropertyId(EvalContext context, PropertyId value)
        {
            if (context >= EvalContext.MaxVars) return;
            Vars[(int)context].ReadOnly = true;
            Vars[(int)context].Var.SetPropertyId(value);
        }

        public virtual void SetVar_EntityId(EvalContext context, ulong value)
        {
            if (context >= EvalContext.MaxVars) return;
            Vars[(int)context].ReadOnly = false;
            Vars[(int)context].Var.SetEntityId(value);
        }

        public virtual void SetReadOnlyVar_EntityId(EvalContext context, ulong value)
        {
            if (context >= EvalContext.MaxVars) return;
            Vars[(int)context].ReadOnly = true;
            Vars[(int)context].Var.SetEntityId(value);
        }

        public virtual void SetVar_PropertyCollectionPtr(EvalContext context, PropertyCollection value)
        {
            if (context >= EvalContext.MaxVars) return;
            Vars[(int)context].ReadOnly = false;
            Vars[(int)context].Var.SetPropertyCollectionPtr(value);
        }

        public virtual void SetReadOnlyVar_PropertyCollectionPtr(EvalContext context, PropertyCollection value)
        {
            if (context >= EvalContext.MaxVars) return;
            Vars[(int)context].ReadOnly = true;
            Vars[(int)context].Var.SetPropertyCollectionPtr(value);
        }

        public virtual void SetVar_ProtoRefListPtr(EvalContext context, List<PrototypeId> value)
        {
            if (context >= EvalContext.MaxVars) return;
            Vars[(int)context].ReadOnly = false;
            Vars[(int)context].Var.SetProtoRefListPtr(value);
        }

        public virtual void SetReadOnlyVar_ProtoRefListPtr(EvalContext context, List<PrototypeId> value)
        {
            if (context >= EvalContext.MaxVars) return;
            Vars[(int)context].ReadOnly = true;
            Vars[(int)context].Var.SetProtoRefListPtr(value);
        }

        public virtual void SetVar_ProtoRefVectorPtr(EvalContext context, PrototypeId[] value)
        {
            if (context >= EvalContext.MaxVars) return;
            Vars[(int)context].ReadOnly = false;
            Vars[(int)context].Var.SetProtoRefVectorPtr(value);
        }

        public virtual void SetReadOnlyVar_ProtoRefVectorPtr(EvalContext context, PrototypeId[] value)
        {
            if (context >= EvalContext.MaxVars) return;
            Vars[(int)context].ReadOnly = true;
            Vars[(int)context].Var.SetProtoRefVectorPtr(value);
        }

        public virtual void SetVar_ConditionCollectionPtr(EvalContext context, ConditionCollection value)
        {
            if (context >= EvalContext.MaxVars) return;
            Vars[(int)context].ReadOnly = false;
            Vars[(int)context].Var.SetConditionCollectionPtr(value);
        }

        public virtual void SetReadOnlyVar_ConditionCollectionPtr(EvalContext context, ConditionCollection value)
        {
            if (context >= EvalContext.MaxVars) return;
            Vars[(int)context].ReadOnly = true;
            Vars[(int)context].Var.SetConditionCollectionPtr(value);
        }

        public virtual void SetVar_EntityPtr(EvalContext context, Entity value)
        {
            if (context >= EvalContext.MaxVars) return;
            Vars[(int)context].ReadOnly = false;
            Vars[(int)context].Var.SetEntityPtr(value);
        }

        public virtual void SetReadOnlyVar_EntityPtr(EvalContext context, Entity value)
        {
            if (context >= EvalContext.MaxVars) return;
            Vars[(int)context].ReadOnly = true;
            Vars[(int)context].Var.SetEntityPtr(value);
        }

        public virtual void SetVar_EntityGuid(EvalContext context, ulong value)
        {
            if (context >= EvalContext.MaxVars) return;
            Vars[(int)context].ReadOnly = false;
            Vars[(int)context].Var.SetEntityGuid(value);
        }

        public virtual void SetReadOnlyVar_EntityGuid(EvalContext context, ulong value)
        {
            if (context >= EvalContext.MaxVars) return;
            Vars[(int)context].ReadOnly = true;
            Vars[(int)context].Var.SetEntityGuid(value);
        }
    }
}
