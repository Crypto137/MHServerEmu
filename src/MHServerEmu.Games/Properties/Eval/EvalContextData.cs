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
        public const int MaxVars = (int)EvalContext.MaxVars;
        public Game Game;
        public EvalContextVar[] Vars = new EvalContextVar[MaxVars];

        public PropertyCollection CallerStackProperties { get; internal set; }
        public PropertyCollection LocalStackProperties { get; internal set; }

        public EvalContextData(Game game)
        {
            Game = game ?? Game.Current;
            for (int i = 0; i < MaxVars; i++)
                Vars[i] = new EvalContextVar();
        }

        public void SetVar_Bool(int index, bool value)
        {
            if (index >=  MaxVars) return;
            Vars[index].ReadOnly = false;
            Vars[index].Var.SetBool(value);
        }

        public void SetVar_Int(int index, long value)
        {
            if (index >=  MaxVars) return;
            Vars[index].ReadOnly = false;
            Vars[index].Var.SetInt(value);
        }

        public void SetVar_Float(int index, float value)
        {
            if (index >=  MaxVars) return;
            Vars[index].ReadOnly = false;
            Vars[index].Var.SetFloat(value);
        }

        public virtual void SetVar_AssetRef(int index, AssetId value)
        {
            if (index >=  MaxVars) return;
            Vars[index].ReadOnly = false;
            Vars[index].Var.SetAssetRef(value);
        }

        public virtual void SetReadOnlyVar_AssetRef(int index, AssetId value)
        {
            if (index >=  MaxVars) return;
            Vars[index].ReadOnly = true;
            Vars[index].Var.SetAssetRef(value);
        }

        public virtual void SetVar_ProtoRef(int index, PrototypeId value)
        {
            if (index >=  MaxVars) return;
            Vars[index].ReadOnly = false;
            Vars[index].Var.SetProtoRef(value);
        }

        public virtual void SetReadOnlyVar_ProtoRef(int index, PrototypeId value)
        {
            if (index >=  MaxVars) return;
            Vars[index].ReadOnly = true;
            Vars[index].Var.SetProtoRef(value);
        }

        public virtual void SetVar_PropertyId(int index, PropertyId value)
        {
            if (index >=  MaxVars) return;
            Vars[index].ReadOnly = false;
            Vars[index].Var.SetPropertyId(value);
        }

        public virtual void SetReadOnlyVar_PropertyId(int index, PropertyId value)
        {
            if (index >=  MaxVars) return;
            Vars[index].ReadOnly = true;
            Vars[index].Var.SetPropertyId(value);
        }

        public virtual void SetVar_EntityId(int index, ulong value)
        {
            if (index >=  MaxVars) return;
            Vars[index].ReadOnly = false;
            Vars[index].Var.SetEntityId(value);
        }

        public virtual void SetReadOnlyVar_EntityId(int index, ulong value)
        {
            if (index >=  MaxVars) return;
            Vars[index].ReadOnly = true;
            Vars[index].Var.SetEntityId(value);
        }

        public virtual void SetVar_PropertyCollectionPtr(int index, PropertyCollection value)
        {
            if (index >=  MaxVars) return;
            Vars[index].ReadOnly = false;
            Vars[index].Var.SetPropertyCollectionPtr(value);
        }

        public virtual void SetReadOnlyVar_PropertyCollectionPtr(int index, PropertyCollection value)
        {
            if (index >=  MaxVars) return;
            Vars[index].ReadOnly = true;
            Vars[index].Var.SetPropertyCollectionPtr(value);
        }

        public virtual void SetVar_ProtoRefListPtr(int index, List<PrototypeId> value)
        {
            if (index >=  MaxVars) return;
            Vars[index].ReadOnly = false;
            Vars[index].Var.SetProtoRefListPtr(value);
        }

        public virtual void SetReadOnlyVar_ProtoRefListPtr(int index, List<PrototypeId> value)
        {
            if (index >=  MaxVars) return;
            Vars[index].ReadOnly = true;
            Vars[index].Var.SetProtoRefListPtr(value);
        }

        public virtual void SetVar_ProtoRefVectorPtr(int index, PrototypeId[] value)
        {
            if (index >=  MaxVars) return;
            Vars[index].ReadOnly = false;
            Vars[index].Var.SetProtoRefVectorPtr(value);
        }

        public virtual void SetReadOnlyVar_ProtoRefVectorPtr(int index, PrototypeId[] value)
        {
            if (index >=  MaxVars) return;
            Vars[index].ReadOnly = true;
            Vars[index].Var.SetProtoRefVectorPtr(value);
        }

        public virtual void SetVar_ConditionCollectionPtr(int index, ConditionCollection value)
        {
            if (index >=  MaxVars) return;
            Vars[index].ReadOnly = false;
            Vars[index].Var.SetConditionCollectionPtr(value);
        }

        public virtual void SetReadOnlyVar_ConditionCollectionPtr(int index, ConditionCollection value)
        {
            if (index >=  MaxVars) return;
            Vars[index].ReadOnly = true;
            Vars[index].Var.SetConditionCollectionPtr(value);
        }

        public virtual void SetVar_EntityPtr(int index, Entity value)
        {
            if (index >=  MaxVars) return;
            Vars[index].ReadOnly = false;
            Vars[index].Var.SetEntityPtr(value);
        }

        public virtual void SetReadOnlyVar_EntityPtr(int index, Entity value)
        {
            if (index >=  MaxVars) return;
            Vars[index].ReadOnly = true;
            Vars[index].Var.SetEntityPtr(value);
        }

        public virtual void SetVar_EntityGuid(int index, ulong value)
        {
            if (index >=  MaxVars) return;
            Vars[index].ReadOnly = false;
            Vars[index].Var.SetEntityGuid(value);
        }

        public virtual void SetReadOnlyVar_EntityGuid(int index, ulong value)
        {
            if (index >=  MaxVars) return;
            Vars[index].ReadOnly = true;
            Vars[index].Var.SetEntityGuid(value);
        }
    }
}
