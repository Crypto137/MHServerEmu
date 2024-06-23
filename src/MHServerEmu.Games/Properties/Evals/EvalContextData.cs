using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Properties.Evals
{
    public class EvalContextData
    {
        public Game Game { get; }
        public EvalContextVar[] ContextVars { get; } = new EvalContextVar[(int)EvalContext.MaxVars];

        public PropertyCollection CallerStackProperties { get; internal set; }
        public PropertyCollection LocalStackProperties { get; internal set; }

        public EvalContextData(Game game)
        {
            Game = game ?? Game.Current;
            for (int i = 0; i < (int)EvalContext.MaxVars; i++)
                ContextVars[i] = new EvalContextVar();
        }

        public void SetVar_Bool(EvalContext context, bool value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = false;
            ContextVars[(int)context].Var.SetBool(value);
        }

        public void SetVar_Int(EvalContext context, long value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = false;
            ContextVars[(int)context].Var.SetInt(value);
        }

        public void SetVar_Float(EvalContext context, float value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = false;
            ContextVars[(int)context].Var.SetFloat(value);
        }

        public virtual void SetVar_AssetRef(EvalContext context, AssetId value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = false;
            ContextVars[(int)context].Var.SetAssetRef(value);
        }

        public virtual void SetReadOnlyVar_AssetRef(EvalContext context, AssetId value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = true;
            ContextVars[(int)context].Var.SetAssetRef(value);
        }

        public virtual void SetVar_ProtoRef(EvalContext context, PrototypeId value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = false;
            ContextVars[(int)context].Var.SetProtoRef(value);
        }

        public virtual void SetReadOnlyVar_ProtoRef(EvalContext context, PrototypeId value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = true;
            ContextVars[(int)context].Var.SetProtoRef(value);
        }

        public virtual void SetVar_PropertyId(EvalContext context, PropertyId value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = false;
            ContextVars[(int)context].Var.SetPropertyId(value);
        }

        public virtual void SetReadOnlyVar_PropertyId(EvalContext context, PropertyId value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = true;
            ContextVars[(int)context].Var.SetPropertyId(value);
        }

        public virtual void SetVar_EntityId(EvalContext context, ulong value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = false;
            ContextVars[(int)context].Var.SetEntityId(value);
        }

        public virtual void SetReadOnlyVar_EntityId(EvalContext context, ulong value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = true;
            ContextVars[(int)context].Var.SetEntityId(value);
        }

        public virtual void SetVar_PropertyCollectionPtr(EvalContext context, PropertyCollection value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = false;
            ContextVars[(int)context].Var.SetPropertyCollectionPtr(value);
        }

        public virtual void SetReadOnlyVar_PropertyCollectionPtr(EvalContext context, PropertyCollection value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = true;
            ContextVars[(int)context].Var.SetPropertyCollectionPtr(value);
        }

        public virtual void SetVar_ProtoRefListPtr(EvalContext context, List<PrototypeId> value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = false;
            ContextVars[(int)context].Var.SetProtoRefListPtr(value);
        }

        public virtual void SetReadOnlyVar_ProtoRefListPtr(EvalContext context, List<PrototypeId> value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = true;
            ContextVars[(int)context].Var.SetProtoRefListPtr(value);
        }

        public virtual void SetVar_ProtoRefVectorPtr(EvalContext context, PrototypeId[] value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = false;
            ContextVars[(int)context].Var.SetProtoRefVectorPtr(value);
        }

        public virtual void SetReadOnlyVar_ProtoRefVectorPtr(EvalContext context, PrototypeId[] value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = true;
            ContextVars[(int)context].Var.SetProtoRefVectorPtr(value);
        }

        public virtual void SetVar_ConditionCollectionPtr(EvalContext context, ConditionCollection value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = false;
            ContextVars[(int)context].Var.SetConditionCollectionPtr(value);
        }

        public virtual void SetReadOnlyVar_ConditionCollectionPtr(EvalContext context, ConditionCollection value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = true;
            ContextVars[(int)context].Var.SetConditionCollectionPtr(value);
        }

        public virtual void SetVar_EntityPtr(EvalContext context, Entity value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = false;
            ContextVars[(int)context].Var.SetEntityPtr(value);
        }

        public virtual void SetReadOnlyVar_EntityPtr(EvalContext context, Entity value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = true;
            ContextVars[(int)context].Var.SetEntityPtr(value);
        }

        public virtual void SetVar_EntityGuid(EvalContext context, ulong value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = false;
            ContextVars[(int)context].Var.SetEntityGuid(value);
        }

        public virtual void SetReadOnlyVar_EntityGuid(EvalContext context, ulong value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = true;
            ContextVars[(int)context].Var.SetEntityGuid(value);
        }
    }
}
