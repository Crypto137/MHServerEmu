using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Properties.Evals
{
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
