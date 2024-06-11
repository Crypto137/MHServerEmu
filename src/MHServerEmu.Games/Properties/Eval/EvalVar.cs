using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Properties.Eval
{
    public struct EvalContextVar
    {
        public EvalVar Var;
        public bool ReadOnly;
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

    public struct EvalVarValue
    {
        public bool Bool;
        public float Float;
        public long Int;
        public ulong EntityId;
        public AssetId AssetId;
        public PrototypeId Proto;
        public ulong PropId;
        public PropertyCollection Props;
        public List<PrototypeId> ProtoRefList;
        public PrototypeId[] ProtoRefVector;
        public ConditionCollection Conditions;
        public Entity Entity;
        public ulong EntityGuid;
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
