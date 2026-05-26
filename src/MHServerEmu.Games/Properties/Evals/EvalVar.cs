using System.Runtime.InteropServices;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Properties.Evals
{
    [StructLayout(LayoutKind.Explicit)]
    public struct EvalVarValue
    {
        // Value and reference types cannot overlap in safe C#, ensure this by using manual field offsets.
        [FieldOffset(0)]
        public bool Bool = false;
        [FieldOffset(0)]
        public float Float = 0.0f;
        [FieldOffset(0)]
        public long Int = 0;
        [FieldOffset(0)]
        public ulong EntityId = 0;
        [FieldOffset(0)]
        public AssetId AssetId = 0;
        [FieldOffset(0)]
        public PrototypeId Proto = 0;
        [FieldOffset(0)]
        public ulong PropId = 0;
        [FieldOffset(8)]
        public PropertyCollection Props = null;
        [FieldOffset(8)]
        public List<PrototypeId> ProtoRefList = null;
        [FieldOffset(8)]
        public PrototypeId[] ProtoRefVector = null;
        [FieldOffset(8)]
        public ConditionCollection Conditions = null;
        [FieldOffset(8)]
        public Entity Entity = null;
        [FieldOffset(0)]
        public ulong EntityGuid = 0;

        public EvalVarValue() { }
    }

    public struct EvalVar
    {
        public EvalReturnType Type;
        public EvalVarValue Value;

        public void SetBool(bool b)
        {
            Type = EvalReturnType.Bool;
            Value.Bool = b;
        }

        public void SetFloat(float f)
        {
            Type = EvalReturnType.Float;
            Value.Float = f;
        }

        public void SetInt(long i)
        {
            Type = EvalReturnType.Int;
            Value.Int = i;
        }

        public void SetEntityId(ulong entityId)
        {
            Type = EvalReturnType.EntityId;
            Value.EntityId = entityId;
        }

        public void SetEntityPtr(Entity entity)
        {
            Type = EvalReturnType.EntityPtr;
            Value.Entity = entity;
        }

        public void SetRegionId(ulong entityId)
        {
            Type = EvalReturnType.RegionId;
            Value.EntityId = entityId;
        }

        public void SetAssetRef(AssetId assetRef)
        {
            Type = EvalReturnType.AssetRef;
            Value.AssetId = assetRef;
        }

        public void SetProtoRef(PrototypeId protoRef)
        {
            Type = EvalReturnType.ProtoRef;
            Value.Proto = protoRef;
        }

        public void SetPropertyId(PropertyId propId)
        {
            Type = EvalReturnType.PropertyId;
            Value.PropId = propId.Raw;
        }

        public void SetPropertyCollectionPtr(PropertyCollection props)
        {
            Type = EvalReturnType.PropertyCollectionPtr;
            Value.Props = props;
        }

        public void SetProtoRefListPtr(List<PrototypeId> protoRefs)
        {
            Type = EvalReturnType.ProtoRefListPtr;
            Value.ProtoRefList = protoRefs;
        }

        public void SetProtoRefVectorPtr(PrototypeId[] protoRefs)
        {
            Type = EvalReturnType.ProtoRefVectorPtr;
            Value.ProtoRefVector = protoRefs;
        }

        public void SetConditionCollectionPtr(ConditionCollection conditions)
        {
            Type = EvalReturnType.ConditionCollectionPtr;
            Value.Conditions = conditions;
        }

        public void SetEntityGuid(ulong dbGuid)
        {
            Type = EvalReturnType.EntityGuid;
            Value.EntityGuid = dbGuid;
        }

        public void SetUndefined()
        {
            Type = EvalReturnType.Undefined;
            Value.Bool = true;
        }

        public void SetError()
        {
            Type = EvalReturnType.Error;
            Value.Bool = false;
        }

        public bool IsNumeric()
        {
            switch (Type)
            {
                case EvalReturnType.Float:
                case EvalReturnType.Int:
                    return true;
                default:
                    return false;
            }
        }
    }
}
