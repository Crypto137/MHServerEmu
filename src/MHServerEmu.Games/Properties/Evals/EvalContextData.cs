using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Properties.Evals
{
    public struct EvalContextVar
    {
        public EvalVar Var;
        public bool ReadOnly;
    }

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

        public void SetVar_Bool(EvalContext index, bool value)
        {
            if (!Verify.IsTrue(index < EvalContext.MaxVars)) return;
            ContextVars[(int)index].ReadOnly = false;
            ContextVars[(int)index].Var.SetBool(value);
        }

        public void SetVar_Int(EvalContext index, long value)
        {
            if (!Verify.IsTrue(index < EvalContext.MaxVars)) return;
            ContextVars[(int)index].ReadOnly = false;
            ContextVars[(int)index].Var.SetInt(value);
        }

        public void SetVar_Float(EvalContext index, float value)
        {
            if (!Verify.IsTrue(index < EvalContext.MaxVars)) return;
            ContextVars[(int)index].ReadOnly = false;
            ContextVars[(int)index].Var.SetFloat(value);
        }

        public void SetVar_AssetRef(EvalContext index, AssetId value)
        {
            if (!Verify.IsTrue(index < EvalContext.MaxVars)) return;
            ContextVars[(int)index].ReadOnly = false;
            ContextVars[(int)index].Var.SetAssetRef(value);
        }

        public void SetReadOnlyVar_AssetRef(EvalContext index, AssetId value)
        {
            if (!Verify.IsTrue(index < EvalContext.MaxVars)) return;
            ContextVars[(int)index].ReadOnly = true;
            ContextVars[(int)index].Var.SetAssetRef(value);
        }

        public void SetVar_ProtoRef(EvalContext index, PrototypeId value)
        {
            if (!Verify.IsTrue(index < EvalContext.MaxVars)) return;
            ContextVars[(int)index].ReadOnly = false;
            ContextVars[(int)index].Var.SetProtoRef(value);
        }

        public void SetReadOnlyVar_ProtoRef(EvalContext index, PrototypeId value)
        {
            if (!Verify.IsTrue(index < EvalContext.MaxVars)) return;
            ContextVars[(int)index].ReadOnly = true;
            ContextVars[(int)index].Var.SetProtoRef(value);
        }

        public void SetVar_PropertyId(EvalContext index, PropertyId value)
        {
            if (!Verify.IsTrue(index < EvalContext.MaxVars)) return;
            ContextVars[(int)index].ReadOnly = false;
            ContextVars[(int)index].Var.SetPropertyId(value);
        }

        public void SetReadOnlyVar_PropertyId(EvalContext index, PropertyId value)
        {
            if (!Verify.IsTrue(index < EvalContext.MaxVars)) return;
            ContextVars[(int)index].ReadOnly = true;
            ContextVars[(int)index].Var.SetPropertyId(value);
        }

        public void SetVar_EntityId(EvalContext index, ulong value)
        {
            if (!Verify.IsTrue(index < EvalContext.MaxVars)) return;
            ContextVars[(int)index].ReadOnly = false;
            ContextVars[(int)index].Var.SetEntityId(value);
        }

        public void SetReadOnlyVar_EntityId(EvalContext index, ulong value)
        {
            if (!Verify.IsTrue(index < EvalContext.MaxVars)) return;
            ContextVars[(int)index].ReadOnly = true;
            ContextVars[(int)index].Var.SetEntityId(value);
        }

        public void SetVar_PropertyCollectionPtr(EvalContext index, PropertyCollection value)
        {
            if (!Verify.IsTrue(index < EvalContext.MaxVars)) return;
            ContextVars[(int)index].ReadOnly = false;
            ContextVars[(int)index].Var.SetPropertyCollectionPtr(value);
        }

        public void SetReadOnlyVar_PropertyCollectionPtr(EvalContext index, PropertyCollection value)
        {
            if (!Verify.IsTrue(index < EvalContext.MaxVars)) return;
            ContextVars[(int)index].ReadOnly = true;
            ContextVars[(int)index].Var.SetPropertyCollectionPtr(value);
        }

        public void SetVar_ProtoRefListPtr(EvalContext index, List<PrototypeId> value)
        {
            if (!Verify.IsTrue(index < EvalContext.MaxVars)) return;
            ContextVars[(int)index].ReadOnly = false;
            ContextVars[(int)index].Var.SetProtoRefListPtr(value);
        }

        public void SetReadOnlyVar_ProtoRefListPtr(EvalContext index, List<PrototypeId> value)
        {
            if (!Verify.IsTrue(index < EvalContext.MaxVars)) return;
            ContextVars[(int)index].ReadOnly = true;
            ContextVars[(int)index].Var.SetProtoRefListPtr(value);
        }

        public void SetVar_ProtoRefVectorPtr(EvalContext index, PrototypeId[] value)
        {
            if (!Verify.IsTrue(index < EvalContext.MaxVars)) return;
            ContextVars[(int)index].ReadOnly = false;
            ContextVars[(int)index].Var.SetProtoRefVectorPtr(value);
        }

        public void SetReadOnlyVar_ProtoRefVectorPtr(EvalContext index, PrototypeId[] value)
        {
            if (!Verify.IsTrue(index < EvalContext.MaxVars)) return;
            ContextVars[(int)index].ReadOnly = true;
            ContextVars[(int)index].Var.SetProtoRefVectorPtr(value);
        }

        public void SetVar_ConditionCollectionPtr(EvalContext index, ConditionCollection value)
        {
            if (!Verify.IsTrue(index < EvalContext.MaxVars)) return;
            ContextVars[(int)index].ReadOnly = false;
            ContextVars[(int)index].Var.SetConditionCollectionPtr(value);
        }

        public void SetReadOnlyVar_ConditionCollectionPtr(EvalContext index, ConditionCollection value)
        {
            if (!Verify.IsTrue(index < EvalContext.MaxVars)) return;
            ContextVars[(int)index].ReadOnly = true;
            ContextVars[(int)index].Var.SetConditionCollectionPtr(value);
        }

        public void SetVar_EntityPtr(EvalContext index, Entity value)
        {
            if (!Verify.IsTrue(index < EvalContext.MaxVars)) return;
            ContextVars[(int)index].ReadOnly = false;
            ContextVars[(int)index].Var.SetEntityPtr(value);
        }

        public void SetReadOnlyVar_EntityPtr(EvalContext index, Entity value)
        {
            if (!Verify.IsTrue(index < EvalContext.MaxVars)) return;
            ContextVars[(int)index].ReadOnly = true;
            ContextVars[(int)index].Var.SetEntityPtr(value);
        }

        public void SetVar_EntityGuid(EvalContext index, ulong value)
        {
            if (!Verify.IsTrue(index < EvalContext.MaxVars)) return;
            ContextVars[(int)index].ReadOnly = false;
            ContextVars[(int)index].Var.SetEntityGuid(value);
        }

        public void SetReadOnlyVar_EntityGuid(EvalContext index, ulong value)
        {
            if (!Verify.IsTrue(index < EvalContext.MaxVars)) return;
            ContextVars[(int)index].ReadOnly = true;
            ContextVars[(int)index].Var.SetEntityGuid(value);
        }

        public void ResetForPool()
        {
            Game = null;
            ContextVars.AsSpan().Clear();
            CallerStackProperties = null;
            LocalStackProperties = null;
        }

        public void Dispose()
        {
            ObjectPoolManager.Instance.Return(this);
        }
    }
}
