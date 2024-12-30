using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Properties.Evals
{
    public sealed class EvalContextData : IPoolable, IDisposable
    {
        public Game Game { get; set; }
        public EvalContextVar[] ContextVars { get; } = new EvalContextVar[(int)EvalContext.MaxVars];

        public PropertyCollection CallerStackProperties { get; set; }
        public PropertyCollection LocalStackProperties { get; set; }

        public bool IsInPool { get; set; }

        public EvalContextData()    // Use pooling instead of calling this directly
        {
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

        public void SetVar_AssetRef(EvalContext context, AssetId value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = false;
            ContextVars[(int)context].Var.SetAssetRef(value);
        }

        public void SetReadOnlyVar_AssetRef(EvalContext context, AssetId value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = true;
            ContextVars[(int)context].Var.SetAssetRef(value);
        }

        public void SetVar_ProtoRef(EvalContext context, PrototypeId value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = false;
            ContextVars[(int)context].Var.SetProtoRef(value);
        }

        public void SetReadOnlyVar_ProtoRef(EvalContext context, PrototypeId value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = true;
            ContextVars[(int)context].Var.SetProtoRef(value);
        }

        public void SetVar_PropertyId(EvalContext context, PropertyId value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = false;
            ContextVars[(int)context].Var.SetPropertyId(value);
        }

        public void SetReadOnlyVar_PropertyId(EvalContext context, PropertyId value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = true;
            ContextVars[(int)context].Var.SetPropertyId(value);
        }

        public void SetVar_EntityId(EvalContext context, ulong value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = false;
            ContextVars[(int)context].Var.SetEntityId(value);
        }

        public void SetReadOnlyVar_EntityId(EvalContext context, ulong value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = true;
            ContextVars[(int)context].Var.SetEntityId(value);
        }

        public void SetVar_PropertyCollectionPtr(EvalContext context, PropertyCollection value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = false;
            ContextVars[(int)context].Var.SetPropertyCollectionPtr(value);
        }

        public void SetReadOnlyVar_PropertyCollectionPtr(EvalContext context, PropertyCollection value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = true;
            ContextVars[(int)context].Var.SetPropertyCollectionPtr(value);
        }

        public void SetVar_ProtoRefListPtr(EvalContext context, List<PrototypeId> value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = false;
            ContextVars[(int)context].Var.SetProtoRefListPtr(value);
        }

        public void SetReadOnlyVar_ProtoRefListPtr(EvalContext context, List<PrototypeId> value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = true;
            ContextVars[(int)context].Var.SetProtoRefListPtr(value);
        }

        public void SetVar_ProtoRefVectorPtr(EvalContext context, PrototypeId[] value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = false;
            ContextVars[(int)context].Var.SetProtoRefVectorPtr(value);
        }

        public void SetReadOnlyVar_ProtoRefVectorPtr(EvalContext context, PrototypeId[] value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = true;
            ContextVars[(int)context].Var.SetProtoRefVectorPtr(value);
        }

        public void SetVar_ConditionCollectionPtr(EvalContext context, ConditionCollection value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = false;
            ContextVars[(int)context].Var.SetConditionCollectionPtr(value);
        }

        public void SetReadOnlyVar_ConditionCollectionPtr(EvalContext context, ConditionCollection value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = true;
            ContextVars[(int)context].Var.SetConditionCollectionPtr(value);
        }

        public void SetVar_EntityPtr(EvalContext context, Entity value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = false;
            ContextVars[(int)context].Var.SetEntityPtr(value);
        }

        public void SetReadOnlyVar_EntityPtr(EvalContext context, Entity value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = true;
            ContextVars[(int)context].Var.SetEntityPtr(value);
        }

        public void SetVar_EntityGuid(EvalContext context, ulong value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = false;
            ContextVars[(int)context].Var.SetEntityGuid(value);
        }

        public void SetReadOnlyVar_EntityGuid(EvalContext context, ulong value)
        {
            if (context >= EvalContext.MaxVars) return;
            ContextVars[(int)context].ReadOnly = true;
            ContextVars[(int)context].Var.SetEntityGuid(value);
        }

        public void ResetForPool()
        {
            Game = null;

            for (int i = 0; i < (int)EvalContext.MaxVars; i++)
                ContextVars[i] = new EvalContextVar();

            CallerStackProperties = null;
            LocalStackProperties = null;
        }

        public void Dispose()
        {
            ObjectPoolManager.Instance.Return(this);
        }
    }
}
