using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Properties.Eval
{
    public class Eval
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static T Run<T>(EvalPrototype evalP, EvalContextData data)
        {
            Run(evalP, data, out T retVal);
            return retVal;
        }

        public static bool Run<T>(EvalPrototype evalProto, EvalContextData data, out T resultVal)
        {
            EvalVar var = Run(evalProto, data);
            if (FromValue(var, out resultVal) == false)
            {
                Logger.Warn($"Invalid return type [{var.Type}]");
                if (evalProto != null)
                    Logger.Warn($"for operator [{evalProto.Op}] EvalPrototype=[{evalProto.GetType().Name}] ExpressionString=[{evalProto.ExpressionString()}] Path=[{evalProto}]");
                return false;
            }

            return true;
        }

        public static bool FromValue(EvalVar var, out int resultVal)
        {
            switch (var.Type)
            {
                case EvalReturnType.Int:
                    resultVal = (int)var.Value.Int;
                    return true;
                case EvalReturnType.Float:
                    resultVal = (int)var.Value.Float;
                    return true;
            }
            resultVal = 0;
            return false;
        }

        public static bool FromValue(EvalVar var, out long resultVal)
        {
            switch (var.Type)
            {
                case EvalReturnType.Int:
                    resultVal = var.Value.Int;
                    return true;
                case EvalReturnType.Float:
                    resultVal = (long)var.Value.Float;
                    return true;
            }
            resultVal = 0;
            return false;
        }

        public static bool FromValue(EvalVar var, out float resultVal)
        {
            switch (var.Type)
            {
                case EvalReturnType.Int:
                    resultVal = var.Value.Int;
                    return true;
                case EvalReturnType.Float:
                    resultVal = var.Value.Float;
                    return true;
            }
            resultVal = 0.0f;
            return false;
        }

        public static bool FromValue(EvalVar var, out bool resultVal)
        {
            switch (var.Type)
            {
                case EvalReturnType.Int:
                    resultVal = var.Value.Int > 0;
                    return true;
                case EvalReturnType.Float:
                    resultVal = var.Value.Float > 0.0f;
                    return true;
                case EvalReturnType.Bool:
                case EvalReturnType.Undefined:
                    resultVal = var.Value.Bool;
                    return true;
            }
            resultVal = false;
            return false;
        }

        public static bool FromValue(EvalVar var, out ulong resultVal)
        {
            if (var.Type == EvalReturnType.EntityId)
            {
                resultVal = var.Value.EntityId;
                return true;
            }
            resultVal = Entity.InvalidId;
            return false;
        }

        public static bool FromValue(EvalVar var, out PrototypeId resultVal)
        {
            if (var.Type == EvalReturnType.ProtoRef)
            {
                resultVal = var.Value.Proto; // GetPrototypeDataRef ?
                return true;
            }
            else if (var.Type == EvalReturnType.AssetRef)
            {
                resultVal = GameDatabase.GetDataRefByAsset(var.Value.AssetId);
                return true;
            }
            resultVal = PrototypeId.Invalid;
            return false;
        }

        public static bool FromValue(EvalVar var, out PropertyId resultVal)
        {
            resultVal = new();
            if (var.Type == EvalReturnType.PropertyId)
            {
                resultVal.Raw = var.Value.PropId;
                return true;
            }
            resultVal = PropertyId.Invalid;
            return false;
        }

        public static bool FromValue(EvalVar var, out Entity resultVal)
        {
            if (var.Type == EvalReturnType.EntityPtr)
            {
                resultVal = var.Value.Entity;
                return true;
            }
            resultVal = null;
            return false;
        }

        public static bool FromValue(EvalVar var, out PropertyCollection resultVal, Game game)
        {
            switch (var.Type)
            {
                case EvalReturnType.PropertyCollectionPtr:
                    resultVal = var.Value.Props;
                    return true;
                case EvalReturnType.EntityPtr:
                    resultVal = var.Value.Entity.Properties;
                    return true;
                case EvalReturnType.EntityGuid:
                    resultVal = null;
                    if (var.Value.EntityGuid != 0 && game != null)
                        resultVal = game.EntityManager.GetEntityByDbGuid<Entity>(var.Value.EntityGuid).Properties;
                    return resultVal != null;
            }
            resultVal = null;
            return false;
        }

        public static bool FromValue(EvalVar var, out ConditionCollection resultVal)
        {
            if (var.Type == EvalReturnType.ConditionCollectionPtr)
            {
                resultVal = var.Value.Conditions;
                return true;
            }
            resultVal = null;
            return false;
        }

        public static bool FromValue(EvalVar var, out List<PrototypeId> resultVal)
        {
            if (var.Type == EvalReturnType.ProtoRefListPtr)
            {
                resultVal = var.Value.ProtoRefList;
                return true;
            }
            resultVal = null;
            return false;
        }

        public static bool FromValue(EvalVar var, out PrototypeId[] resultVal)
        {
            if (var.Type == EvalReturnType.ProtoRefVectorPtr)
            {
                resultVal = var.Value.ProtoRefVector;
                return true;
            }
            resultVal = null;
            return false;
        }

        public static bool FromValue<T>(EvalVar var, out T resultVal)
        {
            resultVal = default;
            bool result = false;
            Type targetType = typeof(T);
            if (targetType == typeof(int))
            {
                result = FromValue(var, out int resultFrom);
                resultVal = (T)(object)resultFrom;
                return result;
            }
            else if (targetType == typeof(long))
            {
                result = FromValue(var, out long resultFrom);
                resultVal = (T)(object)resultFrom;
                return result;
            }
            else if (targetType == typeof(float))
            {
                result = FromValue(var, out float resultFrom);
                resultVal = (T)(object)resultFrom;
                return result;
            }
            else if (targetType == typeof(bool))
            {
                result = FromValue(var, out bool resultFrom);
                resultVal = (T)(object)resultFrom;
                return result;
            }
            else if (targetType == typeof(PrototypeId))
            {
                result = FromValue(var, out PrototypeId resultFrom);
                resultVal = (T)(object)resultFrom;
                return result;
            }            
            return result;
        }

        private static EvalVar Run(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar var = new ();
            var.SetError();
            if (evalProto == null) return var;

            switch (evalProto.Op)
            {
                case EvalOpEnums.And:
                    return RunAnd(evalProto, data);
                case EvalOpEnums.Equals:
                    return RunEquals(evalProto, data);
                case EvalOpEnums.GreaterThan:
                    return RunGreaterThan(evalProto, data);
                case EvalOpEnums.IsContextDataNull:
                    return RunIsContextDataNull(evalProto, data);
                case EvalOpEnums.LessThan:
                    return RunLessThan(evalProto, data);
                case EvalOpEnums.DifficultyTierRange:
                    return RunDifficultyTierRange(evalProto, data);
                case EvalOpEnums.MissionIsActive:
                    return RunMissionIsActive(evalProto, data);
                case EvalOpEnums.MissionIsComplete:
                    return RunMissionIsComplete(evalProto, data);
                case EvalOpEnums.Not:
                    return RunNot(evalProto, data);
                case EvalOpEnums.Or:
                    return RunOr(evalProto, data);
                case EvalOpEnums.HasEntityInInventory:
                    return RunHasEntityInInventory(evalProto, data);
                case EvalOpEnums.LoadAssetRef:
                    return RunLoadAssetRef(evalProto, data);
                case EvalOpEnums.LoadBool:
                    return RunLoadBool(evalProto, data);
                case EvalOpEnums.LoadFloat:
                    return RunLoadFloat(evalProto, data);
                case EvalOpEnums.LoadInt:
                    return RunLoadInt(evalProto, data);
                case EvalOpEnums.LoadProtoRef:
                    return RunLoadProtoRef(evalProto, data);
                case EvalOpEnums.LoadContextInt:
                    return RunLoadContextInt(evalProto, data);
                case EvalOpEnums.LoadContextProtoRef:
                    return RunLoadContextProtoRef(evalProto, data);
                case EvalOpEnums.For:
                    return RunFor(evalProto, data);
                case EvalOpEnums.ForEachConditionInContext:
                    return RunForEachConditionInContext(evalProto, data);
                case EvalOpEnums.ForEachProtoRefInContextRefList:
                    return RunForEachProtoRefInContextRefList(evalProto, data);
                case EvalOpEnums.IfElse:
                    return RunIfElse(evalProto, data);
                case EvalOpEnums.Scope:
                    return RunScope(evalProto, data);
                case EvalOpEnums.LoadCurve:
                    return RunLoadCurve(evalProto, data);
                case EvalOpEnums.Add:
                    return RunAdd(evalProto, data);
                case EvalOpEnums.Div:
                    return RunDiv(evalProto, data);
                case EvalOpEnums.Exponent:
                    return RunExponent(evalProto, data);
                case EvalOpEnums.Max:
                    return RunMax(evalProto, data);
                case EvalOpEnums.Min:
                    return RunMin(evalProto, data);
                case EvalOpEnums.Modulus:
                    return RunModulus(evalProto, data);
                case EvalOpEnums.Mult:
                    return RunMult(evalProto, data);
                case EvalOpEnums.Sub:
                    return RunSub(evalProto, data);
                case EvalOpEnums.AssignProp:
                    return RunAssignProp(evalProto, data);
                case EvalOpEnums.AssignPropEvalParams:
                    return RunAssignPropEvalParams(evalProto, data);
                case EvalOpEnums.HasProp:
                    return RunHasProp(evalProto, data);
                case EvalOpEnums.LoadProp:
                    return RunLoadProp(evalProto, data);
                case EvalOpEnums.LoadPropContextParams:
                    return RunLoadPropContextParams(evalProto, data);
                case EvalOpEnums.LoadPropEvalParams:
                    return RunLoadPropEvalParams(evalProto, data);
                case EvalOpEnums.SwapProp:
                    return RunSwapProp(evalProto, data);
                case EvalOpEnums.RandomFloat:
                    return RunRandomFloat(evalProto, data);
                case EvalOpEnums.RandomInt:
                    return RunRandomInt(evalProto, data);
                case EvalOpEnums.LoadEntityToContextVar:
                    return RunLoadEntityToContextVar(evalProto, data);
                case EvalOpEnums.LoadConditionCollectionToContext:
                    return RunLoadConditionCollectionToContext(evalProto, data);
                case EvalOpEnums.EntityHasKeyword:
                    return RunEntityHasKeyword(evalProto, data);
                case EvalOpEnums.EntityHasTalent:
                    return RunEntityHasTalent(evalProto, data);
                case EvalOpEnums.GetCombatLevel:
                    return RunGetCombatLevel(evalProto, data);
                case EvalOpEnums.GetPowerRank:
                    return RunGetPowerRank(evalProto, data);
                case EvalOpEnums.CalcPowerRank:
                    return RunCalcPowerRank(evalProto, data);
                case EvalOpEnums.IsInParty:
                    return RunIsInParty(evalProto, data);
                case EvalOpEnums.GetDamageReductionPct:
                    return RunGetDamageReductionPct(evalProto, data);
                case EvalOpEnums.GetDistanceToEntity:
                    return RunGetDistanceToEntity(evalProto, data);
                case EvalOpEnums.IsDynamicCombatLevelEnabled:
                    return RunIsDynamicCombatLevelEnabled(evalProto, data);
                default:
                    Logger.Warn($"Invalid Operation");
                    break;
            }
            return var;
        }

    }
}
