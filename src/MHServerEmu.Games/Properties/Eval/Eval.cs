using MHServerEmu.Core.Collisions;
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

            return evalProto.Op switch
            {
                EvalOp.And => RunAnd(evalProto, data),
                EvalOp.Equals => RunEquals(evalProto, data),
                EvalOp.GreaterThan => RunGreaterThan(evalProto, data),
                EvalOp.IsContextDataNull => RunIsContextDataNull(evalProto, data),
                EvalOp.LessThan => RunLessThan(evalProto, data),
                EvalOp.DifficultyTierRange => RunDifficultyTierRange(evalProto, data),
                EvalOp.MissionIsActive => RunMissionIsActive(evalProto, data),
                EvalOp.MissionIsComplete => RunMissionIsComplete(evalProto, data),
                EvalOp.Not => RunNot(evalProto, data),
                EvalOp.Or => RunOr(evalProto, data),
                EvalOp.HasEntityInInventory => RunHasEntityInInventory(evalProto, data),
                EvalOp.LoadAssetRef => RunLoadAssetRef(evalProto, data),
                EvalOp.LoadBool => RunLoadBool(evalProto, data),
                EvalOp.LoadFloat => RunLoadFloat(evalProto, data),
                EvalOp.LoadInt => RunLoadInt(evalProto, data),
                EvalOp.LoadProtoRef => RunLoadProtoRef(evalProto, data),
                EvalOp.LoadContextInt => RunLoadContextInt(evalProto, data),
                EvalOp.LoadContextProtoRef => RunLoadContextProtoRef(evalProto, data),
                EvalOp.For => RunFor(evalProto, data),
                EvalOp.ForEachConditionInContext => RunForEachConditionInContext(evalProto, data),
                EvalOp.ForEachProtoRefInContextRefList => RunForEachProtoRefInContextRefList(evalProto, data),
                EvalOp.IfElse => RunIfElse(evalProto, data),
                EvalOp.Scope => RunScope(evalProto, data),
                EvalOp.LoadCurve => RunLoadCurve(evalProto, data),
                EvalOp.Add => RunAdd(evalProto, data),
                EvalOp.Div => RunDiv(evalProto, data),
                EvalOp.Exponent => RunExponent(evalProto, data),
                EvalOp.Max => RunMax(evalProto, data),
                EvalOp.Min => RunMin(evalProto, data),
                EvalOp.Modulus => RunModulus(evalProto, data),
                EvalOp.Mult => RunMult(evalProto, data),
                EvalOp.Sub => RunSub(evalProto, data),
                EvalOp.AssignProp => RunAssignProp(evalProto, data),
                EvalOp.AssignPropEvalParams => RunAssignPropEvalParams(evalProto, data),
                EvalOp.HasProp => RunHasProp(evalProto, data),
                EvalOp.LoadProp => RunLoadProp(evalProto, data),
                EvalOp.LoadPropContextParams => RunLoadPropContextParams(evalProto, data),
                EvalOp.LoadPropEvalParams => RunLoadPropEvalParams(evalProto, data),
                EvalOp.SwapProp => RunSwapProp(evalProto, data),
                EvalOp.RandomFloat => RunRandomFloat(evalProto, data),
                EvalOp.RandomInt => RunRandomInt(evalProto, data),
                EvalOp.LoadEntityToContextVar => RunLoadEntityToContextVar(evalProto, data),
                EvalOp.LoadConditionCollectionToContext => RunLoadConditionCollectionToContext(evalProto, data),
                EvalOp.EntityHasKeyword => RunEntityHasKeyword(evalProto, data),
                EvalOp.EntityHasTalent => RunEntityHasTalent(evalProto, data),
                EvalOp.GetCombatLevel => RunGetCombatLevel(evalProto, data),
                EvalOp.GetPowerRank => RunGetPowerRank(evalProto, data),
                EvalOp.CalcPowerRank => RunCalcPowerRank(evalProto, data),
                EvalOp.IsInParty => RunIsInParty(evalProto, data),
                EvalOp.GetDamageReductionPct => RunGetDamageReductionPct(evalProto, data),
                EvalOp.GetDistanceToEntity => RunGetDistanceToEntity(evalProto, data),
                EvalOp.IsDynamicCombatLevelEnabled => RunIsDynamicCombatLevelEnabled(evalProto, data),
                _ => Logger.WarnReturn(var, "Invalid Operation"),
            };
        }

        private static EvalVar GetEvalVarFromContext(EvalContext context, EvalContextData data, bool writable, bool checkNull)
        {
            EvalVar var = new ();
            var.SetError();
            bool readOnly;

            if (context < EvalContext.MaxVars)
            {
                var = data.Vars[(int)context].Var;
                readOnly = data.Vars[(int)context].ReadOnly;
            }
            else if (context == EvalContext.CallerStack)
            {
                var.SetPropertyCollectionPtr(data.CallerStackProperties);
                readOnly = false;
            }
            else if (context == EvalContext.LocalStack)
            {
                var.SetPropertyCollectionPtr(data.LocalStackProperties);
                readOnly = false;
            }
            else if (context == EvalContext.Globals)
            {
                GlobalsPrototype globals = GameDatabase.GlobalsPrototype;
                GlobalPropertiesPrototype globalProperties = globals?.Properties;
                if (globalProperties == null || checkNull && globalProperties.Properties == null)
                    return Logger.WarnReturn(var, "Failed to get globals prototype for eval with Globals context type.");
                var.SetPropertyCollectionPtr(globalProperties.Properties);
                readOnly = true;
            }
            else
                return Logger.WarnReturn(var, "Invalid Context");

            if (writable && readOnly)
            {
                var.SetError();
                return Logger.WarnReturn(var, $"Attempting to get a writable '{context}' from a context that has it set as read-only");
            }

            if (checkNull && var.Type == EvalReturnType.PropertyCollectionPtr && var.Value.Props == null)
            {
                var.SetError();
                return Logger.WarnReturn(var, $"Attempting to get '{context}' from a context that doesn't have it set");
            }

            return var;
        }

        private static EvalVar RunAnd(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar var = new();
            var.SetError();
            if (evalProto is not AndPrototype andProto) return var;
            if (andProto.Arg1 == null || andProto.Arg2 == null) return var;

            EvalVar arg1 = Run(andProto.Arg1, data);
            if (arg1.Type != EvalReturnType.Bool)
                return Logger.WarnReturn(var, "And: Non-Bool/Error field Arg1");

            if (arg1.Value.Bool)
            {
                EvalVar arg2 = Run(andProto.Arg2, data);
                if (arg2.Type != EvalReturnType.Bool)
                    return Logger.WarnReturn(var, "Equals: Non-Bool/Error field Arg2");
                var.SetBool(arg2.Value.Bool);
            }
            else
                var.SetBool(false);

            return var;
        }

        private static EvalVar RunEquals(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar var = new ();
            var.SetError();

            if (evalProto is not EqualsPrototype equalsProto) return var;

            EvalVar arg1 = Run(equalsProto.Arg1, data);
            if (arg1.Type == EvalReturnType.Error)
                return Logger.WarnReturn(var, "Equals: Error field Arg1");

            EvalVar arg2 = Run(equalsProto.Arg2, data);
            if (arg2.Type == EvalReturnType.Error)
                return Logger.WarnReturn(var, "Equals: Error field Arg2");

            if (arg1.Type == EvalReturnType.Int && arg2.Type == EvalReturnType.Int)
                var.SetBool(arg1.Value.Int == arg2.Value.Int);
            else if (arg1.Type == EvalReturnType.Int && arg2.Type == EvalReturnType.Float)
                var.SetBool(Segment.EpsilonTest(arg1.Value.Int, arg2.Value.Float, equalsProto.Epsilon));
            else if (arg1.Type == EvalReturnType.Float && arg2.Type == EvalReturnType.Int)
                var.SetBool(Segment.EpsilonTest(arg1.Value.Float, arg2.Value.Int, equalsProto.Epsilon));
            else if (arg1.Type == EvalReturnType.Float && arg2.Type == EvalReturnType.Float)
                var.SetBool(Segment.EpsilonTest(arg1.Value.Float, arg2.Value.Float, equalsProto.Epsilon));
            else if (arg1.Type == EvalReturnType.ProtoRef && arg2.Type == EvalReturnType.ProtoRef)
                var.SetBool(arg1.Value.Proto == arg2.Value.Proto);
            else if (arg1.Type == EvalReturnType.AssetRef && arg2.Type == EvalReturnType.AssetRef)
                var.SetBool(arg1.Value.AssetId == arg2.Value.AssetId);
            else if (arg1.Type == EvalReturnType.Bool && arg2.Type == EvalReturnType.Bool)
                var.SetBool(arg1.Value.Bool == arg2.Value.Bool);
            else
                Logger.Warn("Error with arg types!");

            return var;
        }

        private static EvalVar RunGreaterThan(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar var = new ();
            var.SetError();
            if (evalProto is not GreaterThanPrototype greaterThanProto) return var;

            EvalVar arg1 = Run(greaterThanProto.Arg1, data);
            if (arg1.IsNumeric() == false) 
                return Logger.WarnReturn(var, "GreaterThan: Non-Numeric/Error field Arg1");

            EvalVar arg2 = Run(greaterThanProto.Arg2, data);
            if (arg2.IsNumeric() == false) 
                return Logger.WarnReturn(var, "GreaterThan: Non-Numeric/Error field Arg2");

            if (arg1.Type == EvalReturnType.Int && arg2.Type == EvalReturnType.Int)
                var.SetBool(arg1.Value.Int > arg2.Value.Int);
            else if (arg1.Type == EvalReturnType.Int && arg2.Type == EvalReturnType.Float)
                var.SetBool(arg1.Value.Int > arg2.Value.Float);
            else if (arg1.Type == EvalReturnType.Float && arg2.Type == EvalReturnType.Int)
                var.SetBool(arg1.Value.Float > arg2.Value.Int);
            else if (arg1.Type == EvalReturnType.Float && arg2.Type == EvalReturnType.Float)
                var.SetBool(arg1.Value.Float > arg2.Value.Float);
            else
                Logger.Warn("Error with arg types!");

            return var;
        }

        private static EvalVar RunIsContextDataNull(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar var = new ();
            var.SetError();

            if (evalProto is not IsContextDataNullPrototype isContextDataNullProto) return var;

            EvalVar contextVar = GetEvalVarFromContext(isContextDataNullProto.Context, data, false, false);
            switch (contextVar.Type)
            {
                case EvalReturnType.PropertyCollectionPtr:
                    if (FromValue(contextVar, out PropertyCollection collection) == false) return var;
                    var.SetBool(collection == null);
                    break;

                case EvalReturnType.ProtoRefListPtr:
                    if (FromValue(contextVar, out List<PrototypeId> protoRefList) == false) return var;
                    var.SetBool(protoRefList == null);
                    break;

                case EvalReturnType.ProtoRefVectorPtr:
                    if (FromValue(contextVar, out PrototypeId[] protoRefVector) == false) return var;
                    var.SetBool(protoRefVector == null);
                    break;

                case EvalReturnType.EntityPtr:
                    if (FromValue(contextVar, out Entity entity) == false) return var;
                    var.SetBool(entity == null);
                    break;

                case EvalReturnType.Error:
                    if (isContextDataNullProto.Context >= EvalContext.Var1 && isContextDataNullProto.Context < EvalContext.MaxVars)
                    {
                        var.SetBool(true);
                        break;
                    }
                    return Logger.WarnReturn(var, "IsContextDataNull Eval being checked on a context var that is not a pointer!");

                default:
                    return Logger.WarnReturn(var, "IsContextDataNull Eval being checked on a context var that is not a pointer!");
            }

            return var;
        }

        private static EvalVar RunLessThan(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunDifficultyTierRange(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunMissionIsActive(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunMissionIsComplete(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunNot(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar var = new();
            var.SetError();
            if (evalProto is not NotPrototype NotProto) return var;
            if (NotProto.Arg == null) return var;

            EvalVar arg1 = Run(NotProto.Arg, data);
            if (arg1.Type != EvalReturnType.Bool)
                return Logger.WarnReturn(var, "Not: Non-Bool/Error field Arg");

            var.SetBool(!arg1.Value.Bool);
            return var;
        }

        private static EvalVar RunOr(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar var = new();
            var.SetError();
            if (evalProto is not OrPrototype OrProto) return var;
            if (OrProto.Arg1 == null || OrProto.Arg2 == null) return var;

            EvalVar arg1 = Run(OrProto.Arg1, data);
            if (arg1.Type != EvalReturnType.Bool)
                return Logger.WarnReturn(var, "Or: Non-Bool/Error field Arg1");

            if (arg1.Value.Bool)
                var.SetBool(false); 
            else
            {
                EvalVar arg2 = Run(OrProto.Arg2, data);
                if (arg2.Type != EvalReturnType.Bool)
                    return Logger.WarnReturn(var, "Or: Non-Bool/Error field Arg2");
                var.SetBool(arg2.Value.Bool);
            }

            return var;
        }

        private static EvalVar RunHasEntityInInventory(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunLoadAssetRef(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunLoadBool(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunLoadFloat(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunLoadInt(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunLoadProtoRef(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunLoadContextInt(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunLoadContextProtoRef(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunFor(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunForEachConditionInContext(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunForEachProtoRefInContextRefList(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunIfElse(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunScope(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunLoadCurve(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunAdd(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunDiv(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunExponent(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunMax(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunMin(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunModulus(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunMult(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunSub(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunAssignProp(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunAssignPropEvalParams(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunHasProp(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunLoadProp(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunLoadPropContextParams(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunLoadPropEvalParams(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunSwapProp(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunRandomFloat(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunRandomInt(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunLoadEntityToContextVar(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunLoadConditionCollectionToContext(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunEntityHasKeyword(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunEntityHasTalent(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunGetCombatLevel(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunGetPowerRank(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunCalcPowerRank(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunIsInParty(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunGetDamageReductionPct(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunGetDistanceToEntity(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

        private static EvalVar RunIsDynamicCombatLevelEnabled(EvalPrototype evalProto, EvalContextData data)
        {
            throw new NotImplementedException();
        }

    }
}
