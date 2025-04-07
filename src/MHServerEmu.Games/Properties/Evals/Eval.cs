using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Properties.Evals
{        
    public class Eval
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static bool ValidateEvalContextsForField(EvalPrototype[] evals, HashSet<EvalContext> validContexts, string contextName)
        {
            HashSet<EvalContext> contexts = new ();
            validContexts.Add(EvalContext.Globals);

            foreach (var evalProto in evals)
                GetEvalContexts(evalProto, contexts, validContexts);

            bool validate = true;
            foreach (var context in contexts)
            {
                if (validContexts.Contains(context)) continue;
                validate = false;
                Logger.Warn($"Unsupported context {context} used in Eval {contextName}!"); // DataValidateFailFormatMessage
            }
            return validate;
        }

        public static bool ValidateEvalContextsForField(EvalPrototype evalProto, HashSet<EvalContext> validContexts, string contextName)
        {
            HashSet<EvalContext> contexts = new ();
            validContexts.Add(EvalContext.Globals);

            GetEvalContexts(evalProto, contexts, validContexts);

            bool validate = true;
            foreach (var context in contexts)
            {
                if (validContexts.Contains(context)) continue;
                validate = false;
                Logger.Warn($"Unsupported context {context} used in Eval {contextName}!"); // DataValidateFailFormatMessage
            }
            return validate;
        }

        public static void InitTeamUpEvalContext(EvalContextData data, WorldEntity owner)
        {
            Entity teamUpEntity = null;
            if (owner != null)
            {
                if (owner.IsTeamUpAgent)
                    teamUpEntity = owner;
                else
                {
                    Avatar responsibleAvatar = owner.GetMostResponsiblePowerUser<Avatar>();
                    if (responsibleAvatar != null)
                        teamUpEntity = responsibleAvatar.CurrentTeamUpAgent;
                }
            }

            data.SetReadOnlyVar_EntityPtr(EvalContext.TeamUp, teamUpEntity);
        }

        public static void GetEvalPropertyInputs(PropertyInfo evalInfo, List<PropertyId> resultInputs)
        {
            if (evalInfo.IsEvalProperty == false) return;
            string debugString = evalInfo.PropertyName;
            GetEvalPropertyIds(evalInfo.Eval, resultInputs, GetEvalPropertyIdEnum.PropertyInfoEvalInput, debugString);
        }

        public static void GetEvalPropertyIds(EvalPrototype startEvalProto, List<PropertyId> resultIds, GetEvalPropertyIdEnum type, string debugString)
        {
            if (startEvalProto == null) return;

            Stack<EvalPrototype> evalStack = new ();
            evalStack.Push(startEvalProto);

            while (evalStack.Count > 0)
            {
                EvalPrototype evalProto = evalStack.Pop();
                if (evalProto == null) continue;

                switch (evalProto.Op)
                {
                    case EvalOp.AssignProp:
                        {
                            var typedProto = (AssignPropPrototype)evalProto;
                            if (type == GetEvalPropertyIdEnum.PropertyInfoEvalInput)
                            {
                                if (typedProto.Context != EvalContext.LocalStack)
                                {
                                    Logger.Warn($"Assign property eval operators to context other than local stack not allowed in get-property-eval");
                                    continue;
                                }
                            }
                            else if (type == GetEvalPropertyIdEnum.Output)
                                if (resultIds.Contains(typedProto.Prop) == false) resultIds.Add(typedProto.Prop);
                            evalStack.Push(typedProto.Eval);
                        }
                        break;

                    case EvalOp.AssignPropEvalParams:
                        {
                            var typedProto = (AssignPropEvalParamsPrototype)evalProto;
                            if (type == GetEvalPropertyIdEnum.PropertyInfoEvalInput)
                                if (typedProto.Context != EvalContext.LocalStack)
                                {
                                    Logger.Warn($"Assign property eval operators to context other than local stack not allowed in get-property-eval");
                                    continue;
                                }

                            evalStack.Push(typedProto.Eval);
                            if (typedProto.Param0 != null)
                                evalStack.Push(typedProto.Param0);
                            if (typedProto.Param1 != null)
                                evalStack.Push(typedProto.Param1);
                            if (typedProto.Param2 != null)
                                evalStack.Push(typedProto.Param2);
                            if (typedProto.Param3 != null)
                                evalStack.Push(typedProto.Param3);
                        }
                        break;

                    case EvalOp.LoadEntityToContextVar:
                    case EvalOp.LoadConditionCollectionToContext:
                    case EvalOp.EntityHasKeyword:
                    case EvalOp.EntityHasTalent:
                    case EvalOp.GetCombatLevel:
                    case EvalOp.GetPowerRank:
                    case EvalOp.CalcPowerRank:
                    case EvalOp.GetDamageReductionPct:
                    case EvalOp.GetDistanceToEntity:
                    case EvalOp.IsInParty:

                        if (type == GetEvalPropertyIdEnum.PropertyInfoEvalInput)
                            Logger.Warn($"{evalProto.Op} eval operator not allowed in get-property-eval");

                        break;

                    case EvalOp.HasProp:
                        {
                            var typedProto = (HasPropPrototype)evalProto;
                            if (type == GetEvalPropertyIdEnum.PropertyInfoEvalInput)
                            {
                                if (typedProto.Context != EvalContext.Globals && typedProto.Context != EvalContext.Default && typedProto.Context != EvalContext.LocalStack)
                                {
                                    Logger.Warn($"Eval operator found in a get-property-eval references unsupported Context type. [{debugString}]");
                                    continue;
                                }
                                if (typedProto.Context == EvalContext.Default)
                                    if (resultIds.Contains(typedProto.Prop) == false) resultIds.Add(typedProto.Prop);
                            }
                            else if (type == GetEvalPropertyIdEnum.Input)
                                if (resultIds.Contains(typedProto.Prop) == false) resultIds.Add(typedProto.Prop);
                        }
                        break;

                    case EvalOp.LoadProp:
                        {
                            var typedProto = (LoadPropPrototype)evalProto;
                            if (type == GetEvalPropertyIdEnum.PropertyInfoEvalInput)
                            {
                                if (typedProto.Context != EvalContext.Globals && typedProto.Context != EvalContext.Default && typedProto.Context != EvalContext.LocalStack)
                                {
                                    Logger.Warn($"Eval operator found in a get-property-eval references unsupported Context type. [{debugString}]");
                                    continue;
                                }
                                if (typedProto.Context == EvalContext.Default)
                                    if (resultIds.Contains(typedProto.Prop) == false) resultIds.Add(typedProto.Prop);
                            }
                            else if (type == GetEvalPropertyIdEnum.Input)
                                if (resultIds.Contains(typedProto.Prop) == false) resultIds.Add(typedProto.Prop);
                        }
                        break;

                    case EvalOp.LoadCurve:
                        {
                            var typedProto = (LoadCurvePrototype)evalProto;
                            evalStack.Push(typedProto.Index);
                        }
                        break;

                    case EvalOp.LoadContextInt:
                    case EvalOp.LoadContextProtoRef:

                        if (type == GetEvalPropertyIdEnum.PropertyInfoEvalInput)
                            Logger.Warn($"{evalProto.Op} eval operators not allowed in get-property-eval (but there is no reason they couldn't be added in)");
                        
                        break;

                    case EvalOp.Add:
                        {
                            var typedProto = (AddPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Exponent:
                        {
                            var typedProto = (ExponentPrototype)evalProto;
                            evalStack.Push(typedProto.BaseArg);
                            evalStack.Push(typedProto.ExpArg);
                        }
                        break;

                    case EvalOp.Max:
                        {
                            var typedProto = (MaxPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Min:
                        {
                            var typedProto = (MinPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Modulus:
                        {
                            var typedProto = (ModulusPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Sub:
                        {
                            var typedProto = (SubPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Mult:
                        {
                            var typedProto = (MultPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Div:
                        {
                            var typedProto = (DivPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Scope:
                        {
                            var typedProto = (ScopePrototype)evalProto;
                            if (typedProto.Scope.HasValue())
                                foreach (var each in typedProto.Scope)
                                    evalStack.Push(each);
                        }
                        break;

                    case EvalOp.For:
                        {
                            var typedProto = (ForPrototype)evalProto;
                            if (typedProto.ScopeLoopBody.HasValue())
                            {
                                if (typedProto.PreLoop != null)
                                    evalStack.Push(typedProto.PreLoop);

                                if (typedProto.LoopVarInit != null)
                                    evalStack.Push(typedProto.LoopVarInit);

                                if (typedProto.LoopCondition != null)
                                    evalStack.Push(typedProto.LoopCondition);

                                if (typedProto.LoopAdvance != null)
                                    evalStack.Push(typedProto.LoopAdvance);

                                if (typedProto.PostLoop != null)
                                    evalStack.Push(typedProto.PostLoop);

                                foreach (var each in typedProto.ScopeLoopBody)
                                    evalStack.Push(each);
                            }
                        }
                        break;

                    case EvalOp.ForEachConditionInContext:
                        {
                            var typedProto = (ForEachConditionInContextPrototype)evalProto;
                            if (typedProto.ScopeLoopBody.HasValue())
                            {
                                if (typedProto.PreLoop != null)
                                    evalStack.Push(typedProto.PreLoop);

                                if (typedProto.LoopConditionPreScope != null)
                                    evalStack.Push(typedProto.LoopConditionPreScope);

                                if (typedProto.LoopConditionPostScope != null)
                                    evalStack.Push(typedProto.LoopConditionPostScope);

                                if (typedProto.PostLoop != null)
                                    evalStack.Push(typedProto.PostLoop);

                                foreach (var each in typedProto.ScopeLoopBody)
                                    evalStack.Push(each);
                            }
                        }
                        break;

                    case EvalOp.ForEachProtoRefInContextRefList:
                        {
                            var typedProto = (ForEachProtoRefInContextRefListPrototype)evalProto;
                            if (typedProto.ScopeLoopBody.HasValue())
                            {
                                if (typedProto.PreLoop != null)
                                    evalStack.Push(typedProto.PreLoop);

                                if (typedProto.LoopCondition != null)
                                    evalStack.Push(typedProto.LoopCondition);

                                if (typedProto.PostLoop != null)
                                    evalStack.Push(typedProto.PostLoop);

                                foreach (var each in typedProto.ScopeLoopBody)
                                    evalStack.Push(each);
                            }
                        }
                        break;

                    case EvalOp.GreaterThan:
                        {
                            var typedProto = (GreaterThanPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.LessThan:
                        {
                            var typedProto = (LessThanPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Equals:
                        {
                            var typedProto = (EqualsPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.And:
                        {
                            var typedProto = (AndPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Or:
                        {
                            var typedProto = (OrPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Not:
                        {
                            var typedProto = (NotPrototype)evalProto;
                            evalStack.Push(typedProto.Arg);
                        }
                        break;

                    case EvalOp.IfElse:
                        {
                            var typedProto = (IfElsePrototype)evalProto;
                            evalStack.Push(typedProto.Conditional);
                            evalStack.Push(typedProto.EvalIf);
                            if (typedProto.EvalElse != null)
                                evalStack.Push(typedProto.EvalElse);
                        }
                        break;

                    case EvalOp.LoadAssetRef:
                    case EvalOp.LoadProtoRef:
                    case EvalOp.LoadFloat:
                    case EvalOp.LoadInt:
                    case EvalOp.LoadBool:
                    case EvalOp.DifficultyTierRange:
                    case EvalOp.MissionIsActive:
                    case EvalOp.MissionIsComplete:
                    case EvalOp.RandomFloat:
                    case EvalOp.RandomInt:
                    case EvalOp.ExportError:
                    case EvalOp.HasEntityInInventory:
                    case EvalOp.IsContextDataNull:
                    case EvalOp.IsDynamicCombatLevelEnabled:
                        break;

                    case EvalOp.LoadPropContextParams:
                        {
                            var typedProto = (LoadPropContextParamsPrototype)evalProto;
                            if (type == GetEvalPropertyIdEnum.PropertyInfoEvalInput)
                                Logger.Warn("GetEvalPropertyInputs() is being called for a LoadPropContextParams, which means the PropertyInfo doesn't have the 'always re-compute eval' flag set! " +
                                    $"Prop: [{GameDatabase.GetPrototypeName(typedProto.Prop)}]");
                        }
                        break;

                    case EvalOp.LoadPropEvalParams:
                        {
                            var typedProto = (LoadPropEvalParamsPrototype)evalProto;
                            if (type == GetEvalPropertyIdEnum.PropertyInfoEvalInput)
                                Logger.Warn("GetEvalPropertyInputs() is being called for a LoadPropEvalParams, which means the PropertyInfo doesn't have the 'always re-compute eval' flag set! " +
                                    $"Prop: [{GameDatabase.GetPrototypeName(typedProto.Prop)}]");
                        }
                        break;

                    default:
                        Logger.Warn("Invalid Operation");
                        break;
                }
            }
        }

        private static void GetEvalContexts(EvalPrototype startEvalProto, HashSet<EvalContext> resultContexts, HashSet<EvalContext> validContexts)
        {
            if (startEvalProto == null) return;

            Stack<EvalPrototype> evalStack = new();
            evalStack.Push(startEvalProto);

            while (evalStack.Count > 0)
            {
                EvalPrototype evalProto = evalStack.Pop();
                if (evalProto == null) continue;

                switch (evalProto.Op)
                {
                    case EvalOp.AssignProp:
                        {
                            var typedProto = (AssignPropPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                            evalStack.Push(typedProto.Eval);
                        }
                        break;

                    case EvalOp.AssignPropEvalParams:
                        {
                            var typedProto = (AssignPropEvalParamsPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                            evalStack.Push(typedProto.Eval);
                            if (typedProto.Param0 != null)
                                evalStack.Push(typedProto.Param0);
                            if (typedProto.Param1 != null)
                                evalStack.Push(typedProto.Param1);
                            if (typedProto.Param2 != null)
                                evalStack.Push(typedProto.Param2);
                            if (typedProto.Param3 != null)
                                evalStack.Push(typedProto.Param3);
                        }
                        break;

                    case EvalOp.LoadEntityToContextVar:
                        {
                            var typedProto = (LoadEntityToContextVarPrototype)evalProto;
                            validContexts?.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.LoadConditionCollectionToContext:
                        {
                            var typedProto = (LoadConditionCollectionToContextPrototype)evalProto;
                            validContexts?.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.EntityHasKeyword:
                        {
                            var typedProto = (EntityHasKeywordPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.EntityHasTalent:
                        {
                            var typedProto = (EntityHasTalentPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.GetCombatLevel:
                        {
                            var typedProto = (GetCombatLevelPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.GetPowerRank:
                        {
                            var typedProto = (GetPowerRankPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.CalcPowerRank:
                        {
                            var typedProto = (CalcPowerRankPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.GetDamageReductionPct:
                        {
                            var typedProto = (GetDamageReductionPctPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.GetDistanceToEntity:
                        {
                            var typedProto = (GetDistanceToEntityPrototype)evalProto;
                            resultContexts.Add(typedProto.SourceEntity);
                            resultContexts.Add(typedProto.TargetEntity);
                        }
                        break;

                    case EvalOp.IsInParty:
                        {
                            var typedProto = (IsInPartyPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.HasProp:
                        {
                            var typedProto = (HasPropPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.LoadProp:
                        {
                            var typedProto = (LoadPropPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.LoadCurve:
                        {
                            var typedProto = (LoadCurvePrototype)evalProto;
                            evalStack.Push(typedProto.Index);
                        }
                        break;

                    case EvalOp.LoadContextInt:
                        {
                            var typedProto = (LoadContextIntPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.LoadContextProtoRef:
                        {
                            var typedProto = (LoadContextProtoRefPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.Add:
                        {
                            var typedProto = (AddPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Exponent:
                        {
                            var typedProto = (ExponentPrototype)evalProto;
                            evalStack.Push(typedProto.BaseArg);
                            evalStack.Push(typedProto.ExpArg);
                        }
                        break;

                    case EvalOp.Max:
                        {
                            var typedProto = (MaxPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Min:
                        {
                            var typedProto = (MinPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Modulus:
                        {
                            var typedProto = (ModulusPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Sub:
                        {
                            var typedProto = (SubPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Mult:
                        {
                            var typedProto = (MultPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Div:
                        {
                            var typedProto = (DivPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Scope:
                        {
                            var typedProto = (ScopePrototype)evalProto;
                            if (typedProto.Scope.HasValue())
                            {
                                foreach (var each in typedProto.Scope)
                                    evalStack.Push(each);

                                if (validContexts != null)
                                {
                                    validContexts.Add(EvalContext.LocalStack);
                                    validContexts.Add(EvalContext.CallerStack);
                                }
                            }
                        }
                        break;

                    case EvalOp.For:
                        {
                            var typedProto = (ForPrototype)evalProto;
                            if (typedProto.ScopeLoopBody.HasValue())
                            {
                                if (typedProto.PreLoop != null)
                                    evalStack.Push(typedProto.PreLoop);

                                if (typedProto.LoopVarInit != null)
                                    evalStack.Push(typedProto.LoopVarInit);

                                if (typedProto.LoopCondition != null)
                                    evalStack.Push(typedProto.LoopCondition);

                                if (typedProto.LoopAdvance != null)
                                    evalStack.Push(typedProto.LoopAdvance);

                                if (typedProto.PostLoop != null)
                                    evalStack.Push(typedProto.PostLoop);

                                foreach (var each in typedProto.ScopeLoopBody)
                                    evalStack.Push(each);

                                if (validContexts != null)
                                {
                                    validContexts.Add(EvalContext.LocalStack);
                                    validContexts.Add(EvalContext.CallerStack);
                                }
                            }
                        }
                        break;

                    case EvalOp.ForEachConditionInContext:
                        {
                            var typedProto = (ForEachConditionInContextPrototype)evalProto;
                            if (typedProto.ScopeLoopBody.HasValue())
                            {
                                if (typedProto.PreLoop != null)
                                    evalStack.Push(typedProto.PreLoop);

                                if (typedProto.LoopConditionPreScope != null)
                                    evalStack.Push(typedProto.LoopConditionPreScope);

                                if (typedProto.LoopConditionPostScope != null)
                                    evalStack.Push(typedProto.LoopConditionPostScope);

                                if (typedProto.PostLoop != null)
                                    evalStack.Push(typedProto.PostLoop);

                                foreach (var each in typedProto.ScopeLoopBody)
                                    evalStack.Push(each);

                                if (validContexts != null)
                                {
                                    validContexts.Add(EvalContext.LocalStack);
                                    validContexts.Add(EvalContext.CallerStack);
                                    validContexts.Add(EvalContext.Condition);
                                    validContexts.Add(EvalContext.ConditionKeywords);
                                }
                            }
                        }
                        break;

                    case EvalOp.ForEachProtoRefInContextRefList:
                        {
                            var typedProto = (ForEachProtoRefInContextRefListPrototype)evalProto;
                            if (typedProto.ScopeLoopBody.HasValue())
                            {
                                if (typedProto.PreLoop != null)
                                    evalStack.Push(typedProto.PreLoop);

                                if (typedProto.LoopCondition != null)
                                    evalStack.Push(typedProto.LoopCondition);

                                if (typedProto.PostLoop != null)
                                    evalStack.Push(typedProto.PostLoop);

                                foreach (var each in typedProto.ScopeLoopBody)
                                    evalStack.Push(each);

                                if (validContexts != null)
                                {
                                    validContexts.Add(EvalContext.LocalStack);
                                    validContexts.Add(EvalContext.CallerStack);
                                }
                            }
                        }
                        break;

                    case EvalOp.GreaterThan:
                        {
                            var typedProto = (GreaterThanPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.LessThan:
                        {
                            var typedProto = (LessThanPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Equals:
                        {
                            var typedProto = (EqualsPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.And:
                        {
                            var typedProto = (AndPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Or:
                        {
                            var typedProto = (OrPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Not:
                        {
                            var typedProto = (NotPrototype)evalProto;
                            evalStack.Push(typedProto.Arg);
                        }
                        break;

                    case EvalOp.IfElse:
                        {
                            var typedProto = (IfElsePrototype)evalProto;
                            evalStack.Push(typedProto.Conditional);
                            evalStack.Push(typedProto.EvalIf);
                            if (typedProto.EvalElse != null)
                                evalStack.Push(typedProto.EvalElse);
                        }
                        break;

                    case EvalOp.LoadAssetRef:
                    case EvalOp.LoadProtoRef:
                    case EvalOp.LoadFloat:
                    case EvalOp.LoadInt:
                    case EvalOp.LoadBool:
                    case EvalOp.RandomFloat:
                    case EvalOp.RandomInt:
                    case EvalOp.ExportError:
                        break;

                    case EvalOp.DifficultyTierRange:
                        {
                            var typedProto = (DifficultyTierRangePrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.MissionIsActive:
                        {
                            var typedProto = (MissionIsActivePrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.MissionIsComplete:
                        {
                            var typedProto = (MissionIsCompletePrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.HasEntityInInventory:
                        {
                            var typedProto = (HasEntityInInventoryPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.IsContextDataNull:
                        {
                            var typedProto = (IsContextDataNullPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.LoadPropContextParams:
                        {
                            var typedProto = (LoadPropContextParamsPrototype)evalProto;
                            resultContexts.Add(typedProto.PropertyCollectionContext);
                            resultContexts.Add(typedProto.PropertyIdContext);
                        }
                        break;

                    case EvalOp.LoadPropEvalParams:
                        {
                            var typedProto = (LoadPropEvalParamsPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                            if (typedProto.Param0 != null)
                                evalStack.Push(typedProto.Param0);
                            if (typedProto.Param1 != null)
                                evalStack.Push(typedProto.Param1);
                            if (typedProto.Param2 != null)
                                evalStack.Push(typedProto.Param2);
                            if (typedProto.Param3 != null)
                                evalStack.Push(typedProto.Param3);
                        }
                        break;
                    case EvalOp.SwapProp:
                        {
                            var typedProto = (SwapPropPrototype)evalProto;
                            resultContexts.Add(typedProto.LeftContext);
                            resultContexts.Add(typedProto.RightContext);
                        }
                        break;

                    default:
                        Logger.Warn("Invalid Operation");
                        break;
                }
            }
        }

        public static int RunInt(EvalPrototype evalProto, EvalContextData data)
        {
            Run(evalProto, data, out int retVal);
            return retVal;
        }

        public static bool Run(EvalPrototype evalProto, EvalContextData data, out int resultVal)
        {
            EvalVar evalVar = Run(evalProto, data);
            if (FromValue(evalVar, out resultVal) == false)
            {
                Logger.Warn($"Invalid return type [{evalVar.Type}]");
                if (evalProto != null)
                    Logger.Warn($"for operator [{evalProto.Op}] EvalPrototype=[{evalProto.GetType().Name}] ExpressionString=[{evalProto.ExpressionString()}] Path=[{evalProto}]");
                return false;
            }

            return true;
        }

        public static long RunLong(EvalPrototype evalProto, EvalContextData data)
        {
            Run(evalProto, data, out long retVal);
            return retVal;
        }

        public static bool Run(EvalPrototype evalProto, EvalContextData data, out long resultVal)
        {
            EvalVar evalVar = Run(evalProto, data);
            if (FromValue(evalVar, out resultVal) == false)
            {
                Logger.Warn($"Invalid return type [{evalVar.Type}]");
                if (evalProto != null)
                    Logger.Warn($"for operator [{evalProto.Op}] EvalPrototype=[{evalProto.GetType().Name}] ExpressionString=[{evalProto.ExpressionString()}] Path=[{evalProto}]");
                return false;
            }

            return true;
        }

        public static float RunFloat(EvalPrototype evalProto, EvalContextData data)
        {
            Run(evalProto, data, out float retVal);
            return retVal;
        }

        public static bool Run(EvalPrototype evalProto, EvalContextData data, out float resultVal)
        {
            EvalVar evalVar = Run(evalProto, data);
            if (FromValue(evalVar, out resultVal) == false)
            {
                Logger.Warn($"Invalid return type [{evalVar.Type}]");
                if (evalProto != null)
                    Logger.Warn($"for operator [{evalProto.Op}] EvalPrototype=[{evalProto.GetType().Name}] ExpressionString=[{evalProto.ExpressionString()}] Path=[{evalProto}]");
                return false;
            }

            return true;
        }

        public static bool RunBool(EvalPrototype evalProto, EvalContextData data)
        {
            Run(evalProto, data, out bool retVal);
            return retVal;
        }

        public static bool Run(EvalPrototype evalProto, EvalContextData data, out bool resultVal)
        {
            EvalVar evalVar = Run(evalProto, data);
            if (FromValue(evalVar, out resultVal) == false)
            {
                Logger.Warn($"Invalid return type [{evalVar.Type}]");
                if (evalProto != null)
                    Logger.Warn($"for operator [{evalProto.Op}] EvalPrototype=[{evalProto.GetType().Name}] ExpressionString=[{evalProto.ExpressionString()}] Path=[{evalProto}]");
                return false;
            }

            return true;
        }

        public static PrototypeId RunPrototypeId(EvalPrototype evalProto, EvalContextData data)
        {
            Run(evalProto, data, out PrototypeId retVal);
            return retVal;
        }

        public static bool Run(EvalPrototype evalProto, EvalContextData data, out PrototypeId resultVal)
        {
            EvalVar evalVar = Run(evalProto, data);
            if (FromValue(evalVar, out resultVal) == false)
            {
                Logger.Warn($"Invalid return type [{evalVar.Type}]");
                if (evalProto != null)
                    Logger.Warn($"for operator [{evalProto.Op}] EvalPrototype=[{evalProto.GetType().Name}] ExpressionString=[{evalProto.ExpressionString()}] Path=[{evalProto}]");
                return false;
            }

            return true;
        }

        public static AssetId RunAssetId(EvalPrototype evalProto, EvalContextData data)
        {
            Run(evalProto, data, out AssetId retVal);
            return retVal;
        }

        public static bool Run(EvalPrototype evalProto, EvalContextData data, out AssetId resultVal)
        {
            EvalVar evalVar = Run(evalProto, data);
            if (FromValue(evalVar, out resultVal) == false)
            {
                Logger.Warn($"Invalid return type [{evalVar.Type}]");
                if (evalProto != null)
                    Logger.Warn($"for operator [{evalProto.Op}] EvalPrototype=[{evalProto.GetType().Name}] ExpressionString=[{evalProto.ExpressionString()}] Path=[{evalProto}]");
                return false;
            }

            return true;
        }

        public static bool FromValue(EvalVar evalVar, out int resultVal)
        {
            switch (evalVar.Type)
            {
                case EvalReturnType.Int:
                    resultVal = (int)evalVar.Value.Int;
                    return true;
                case EvalReturnType.Float:
                    resultVal = (int)evalVar.Value.Float;
                    return true;
            }
            resultVal = 0;
            return false;
        }

        public static bool FromValue(EvalVar evalVar, out long resultVal)
        {
            switch (evalVar.Type)
            {
                case EvalReturnType.Int:
                    resultVal = evalVar.Value.Int;
                    return true;
                case EvalReturnType.Float:
                    resultVal = (long)evalVar.Value.Float;
                    return true;
            }
            resultVal = 0;
            return false;
        }

        public static bool FromValue(EvalVar evalVar, out float resultVal)
        {
            switch (evalVar.Type)
            {
                case EvalReturnType.Int:
                    resultVal = evalVar.Value.Int;
                    return true;
                case EvalReturnType.Float:
                    resultVal = evalVar.Value.Float;
                    return true;
            }
            resultVal = 0.0f;
            return false;
        }

        public static bool FromValue(EvalVar evalVar, out bool resultVal)
        {
            switch (evalVar.Type)
            {
                case EvalReturnType.Int:
                    resultVal = evalVar.Value.Int > 0;
                    return true;
                case EvalReturnType.Float:
                    resultVal = evalVar.Value.Float > 0.0f;
                    return true;
                case EvalReturnType.Bool:
                case EvalReturnType.Undefined:
                    resultVal = evalVar.Value.Bool;
                    return true;
            }
            resultVal = false;
            return false;
        }

        public static bool FromValue(EvalVar evalVar, out ulong resultVal)
        {
            if (evalVar.Type == EvalReturnType.EntityId)
            {
                resultVal = evalVar.Value.EntityId;
                return true;
            }
            resultVal = Entity.InvalidId;
            return false;
        }

        public static bool FromValue(EvalVar evalVar, out PrototypeId resultVal)
        {
            if (evalVar.Type == EvalReturnType.ProtoRef)
            {
                resultVal = evalVar.Value.Proto;
                return true;
            }
            resultVal = PrototypeId.Invalid;
            return false;
        }

        public static bool FromValue(EvalVar evalVar, out AssetId resultVal)
        {
            if (evalVar.Type == EvalReturnType.AssetRef)
            {
                resultVal = evalVar.Value.AssetId;
                return true;
            }
            resultVal = AssetId.Invalid;
            return false;
        }

        public static bool FromValue(EvalVar evalVar, out PropertyId resultVal)
        {
            resultVal = new();
            if (evalVar.Type == EvalReturnType.PropertyId)
            {
                resultVal.Raw = evalVar.Value.PropId;
                return true;
            }
            resultVal = PropertyId.Invalid;
            return false;
        }

        public static bool FromValue(EvalVar evalVar, out Entity resultVal)
        {
            if (evalVar.Type == EvalReturnType.EntityPtr)
            {
                resultVal = evalVar.Value.Entity;
                return true;
            }
            resultVal = null;
            return false;
        }

        public static bool FromValue(EvalVar evalVar, out PropertyCollection resultVal, Game game)
        {
            switch (evalVar.Type)
            {
                case EvalReturnType.PropertyCollectionPtr:
                    resultVal = evalVar.Value.Props;
                    return true;
                case EvalReturnType.EntityPtr:
                    resultVal = evalVar.Value.Entity?.Properties;
                    return true;
                case EvalReturnType.EntityGuid:
                    resultVal = null;
                    if (evalVar.Value.EntityGuid != 0 && game != null)
                    {
                        var entity = game.EntityManager.GetEntityByDbGuid<Entity>(evalVar.Value.EntityGuid);
                        if (entity != null) resultVal = entity.Properties;
                    }
                    return resultVal != null;
            }
            resultVal = null;
            return false;
        }

        public static bool FromValue(EvalVar evalVar, out ConditionCollection resultVal)
        {
            if (evalVar.Type == EvalReturnType.ConditionCollectionPtr)
            {
                resultVal = evalVar.Value.Conditions;
                return true;
            }
            resultVal = null;
            return false;
        }

        public static bool FromValue(EvalVar evalVar, out List<PrototypeId> resultVal)
        {
            if (evalVar.Type == EvalReturnType.ProtoRefListPtr)
            {
                resultVal = evalVar.Value.ProtoRefList;
                return true;
            }
            resultVal = null;
            return false;
        }

        public static bool FromValue(EvalVar evalVar, out PrototypeId[] resultVal)
        {
            if (evalVar.Type == EvalReturnType.ProtoRefVectorPtr)
            {
                resultVal = evalVar.Value.ProtoRefVector;
                return true;
            }
            resultVal = null;
            return false;
        }

        private static EvalVar Run(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();
            if (evalProto == null) return evalVar;

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
                EvalOp.ExportError => RunExportError(evalProto, data),
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
                _ => Logger.WarnReturn(evalVar, "Invalid Operation"),
            };
        }

        private static EvalVar GetEvalVarFromContext(EvalContext context, EvalContextData data, bool writable, bool checkNull = true)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();
            bool readOnly;

            if (context < EvalContext.MaxVars)
            {
                evalVar = data.ContextVars[(int)context].Var;
                readOnly = data.ContextVars[(int)context].ReadOnly;
            }
            else if (context == EvalContext.CallerStack)
            {
                evalVar.SetPropertyCollectionPtr(data.CallerStackProperties);
                readOnly = false;
            }
            else if (context == EvalContext.LocalStack)
            {
                evalVar.SetPropertyCollectionPtr(data.LocalStackProperties);
                readOnly = false;
            }
            else if (context == EvalContext.Globals)
            {
                GlobalsPrototype globals = GameDatabase.GlobalsPrototype;
                GlobalPropertiesPrototype globalProperties = globals?.Properties;
                if (globalProperties == null || checkNull && globalProperties.Properties == null)
                    return Logger.WarnReturn(evalVar, "Failed to get globals prototype for eval with Globals context type.");
                evalVar.SetPropertyCollectionPtr(globalProperties.Properties);
                readOnly = true;
            }
            else
                return Logger.WarnReturn(evalVar, "Invalid Context");

            if (writable && readOnly)
            {
                evalVar.SetError();
                return Logger.WarnReturn(evalVar, $"Attempting to get a writable '{context}' from a context that has it set as read-only");
            }

            if (checkNull && evalVar.Type == EvalReturnType.PropertyCollectionPtr && evalVar.Value.Props == null)
            {
                evalVar.SetError();
                return Logger.WarnReturn(evalVar, $"Attempting to get '{context}' from a context that doesn't have it set");
            }

            return evalVar;
        }

        private static EvalVar RunAnd(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();
            if (evalProto is not AndPrototype andProto) return evalVar;
            if (andProto.Arg1 == null || andProto.Arg2 == null) return evalVar;

            EvalVar arg1 = Run(andProto.Arg1, data);
            if (arg1.Type != EvalReturnType.Bool)
                return Logger.WarnReturn(evalVar, "And: Non-Bool/Error field Arg1");

            if (arg1.Value.Bool)
            {
                EvalVar arg2 = Run(andProto.Arg2, data);
                if (arg2.Type != EvalReturnType.Bool)
                    return Logger.WarnReturn(evalVar, "Equals: Non-Bool/Error field Arg2");
                evalVar.SetBool(arg2.Value.Bool);
            }
            else
                evalVar.SetBool(false);

            return evalVar;
        }

        private static EvalVar RunEquals(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();

            if (evalProto is not EqualsPrototype equalsProto) return evalVar;

            EvalVar arg1 = Run(equalsProto.Arg1, data);
            if (arg1.Type == EvalReturnType.Error)
                return Logger.WarnReturn(evalVar, "Equals: Error field Arg1");

            EvalVar arg2 = Run(equalsProto.Arg2, data);
            if (arg2.Type == EvalReturnType.Error)
                return Logger.WarnReturn(evalVar, "Equals: Error field Arg2");

            if (arg1.Type == EvalReturnType.Int && arg2.Type == EvalReturnType.Int)
                evalVar.SetBool(arg1.Value.Int == arg2.Value.Int);
            else if (arg1.Type == EvalReturnType.Int && arg2.Type == EvalReturnType.Float)
                evalVar.SetBool(Segment.EpsilonTest(arg1.Value.Int, arg2.Value.Float, equalsProto.Epsilon));
            else if (arg1.Type == EvalReturnType.Float && arg2.Type == EvalReturnType.Int)
                evalVar.SetBool(Segment.EpsilonTest(arg1.Value.Float, arg2.Value.Int, equalsProto.Epsilon));
            else if (arg1.Type == EvalReturnType.Float && arg2.Type == EvalReturnType.Float)
                evalVar.SetBool(Segment.EpsilonTest(arg1.Value.Float, arg2.Value.Float, equalsProto.Epsilon));
            else if (arg1.Type == EvalReturnType.ProtoRef && arg2.Type == EvalReturnType.ProtoRef)
                evalVar.SetBool(arg1.Value.Proto == arg2.Value.Proto);
            else if (arg1.Type == EvalReturnType.AssetRef && arg2.Type == EvalReturnType.AssetRef)
                evalVar.SetBool(arg1.Value.AssetId == arg2.Value.AssetId);
            else if (arg1.Type == EvalReturnType.Bool && arg2.Type == EvalReturnType.Bool)
                evalVar.SetBool(arg1.Value.Bool == arg2.Value.Bool);
            else
                Logger.Warn("Error with arg types!");

            return evalVar;
        }

        private static EvalVar RunGreaterThan(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();
            if (evalProto is not GreaterThanPrototype greaterThanProto) return evalVar;

            EvalVar arg1 = Run(greaterThanProto.Arg1, data);
            if (arg1.IsNumeric() == false) 
                return Logger.WarnReturn(evalVar, "GreaterThan: Non-Numeric/Error field Arg1");

            EvalVar arg2 = Run(greaterThanProto.Arg2, data);
            if (arg2.IsNumeric() == false) 
                return Logger.WarnReturn(evalVar, "GreaterThan: Non-Numeric/Error field Arg2");

            if (arg1.Type == EvalReturnType.Int && arg2.Type == EvalReturnType.Int)
                evalVar.SetBool(arg1.Value.Int > arg2.Value.Int);
            else if (arg1.Type == EvalReturnType.Int && arg2.Type == EvalReturnType.Float)
                evalVar.SetBool(arg1.Value.Int > arg2.Value.Float);
            else if (arg1.Type == EvalReturnType.Float && arg2.Type == EvalReturnType.Int)
                evalVar.SetBool(arg1.Value.Float > arg2.Value.Int);
            else if (arg1.Type == EvalReturnType.Float && arg2.Type == EvalReturnType.Float)
                evalVar.SetBool(arg1.Value.Float > arg2.Value.Float);
            else
                Logger.Warn("Error with arg types!");

            return evalVar;
        }

        private static EvalVar RunIsContextDataNull(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();

            if (evalProto is not IsContextDataNullPrototype isContextDataNullProto) return evalVar;

            EvalVar contextVar = GetEvalVarFromContext(isContextDataNullProto.Context, data, false, false);
            switch (contextVar.Type)
            {
                case EvalReturnType.PropertyCollectionPtr:
                    if (FromValue(contextVar, out PropertyCollection collection, data.Game) == false) return evalVar;
                    evalVar.SetBool(collection == null);
                    break;

                case EvalReturnType.ProtoRefListPtr:
                    if (FromValue(contextVar, out List<PrototypeId> protoRefList) == false) return evalVar;
                    evalVar.SetBool(protoRefList == null);
                    break;

                case EvalReturnType.ProtoRefVectorPtr:
                    if (FromValue(contextVar, out PrototypeId[] protoRefVector) == false) return evalVar;
                    evalVar.SetBool(protoRefVector == null);
                    break;

                case EvalReturnType.EntityPtr:
                    if (FromValue(contextVar, out Entity entity) == false) return evalVar;
                    evalVar.SetBool(entity == null);
                    break;

                case EvalReturnType.Error:
                    if (isContextDataNullProto.Context >= EvalContext.Var1 && isContextDataNullProto.Context < EvalContext.MaxVars)
                    {
                        evalVar.SetBool(true);
                        break;
                    }
                    return Logger.WarnReturn(evalVar, "IsContextDataNull Eval being checked on a context evalVar that is not a pointer!");

                default:
                    return Logger.WarnReturn(evalVar, "IsContextDataNull Eval being checked on a context evalVar that is not a pointer!");
            }

            return evalVar;
        }

        private static EvalVar RunLessThan(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();
            if (evalProto is not LessThanPrototype lessThanProto) return evalVar;

            EvalVar arg1 = Run(lessThanProto.Arg1, data);
            if (arg1.IsNumeric() == false) return Logger.WarnReturn(evalVar, "LessThan: Non-Numeric/Error field Arg1");
            EvalVar arg2 = Run(lessThanProto.Arg2, data);
            if (arg2.IsNumeric() == false) return Logger.WarnReturn(evalVar, "LessThan: Non-Numeric/Error field Arg2");

            if (arg1.Type == EvalReturnType.Int && arg2.Type == EvalReturnType.Int)
                evalVar.SetBool(arg1.Value.Int < arg2.Value.Int);
            else if (arg1.Type == EvalReturnType.Int && arg2.Type == EvalReturnType.Float)
                evalVar.SetBool(arg1.Value.Int < arg2.Value.Float);
            else if (arg1.Type == EvalReturnType.Float && arg2.Type == EvalReturnType.Int)
                evalVar.SetBool(arg1.Value.Float < arg2.Value.Int);
            else if (arg1.Type == EvalReturnType.Float && arg2.Type == EvalReturnType.Float)
                evalVar.SetBool(arg1.Value.Float < arg2.Value.Float);
            else return Logger.WarnReturn(evalVar, "Error with arg types!");

            return evalVar;
        }

        private static EvalVar RunDifficultyTierRange(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();
            if (evalProto is not DifficultyTierRangePrototype DifficultyTierRangeProto) return evalVar;

            PrototypeId tierRef = PrototypeId.Invalid;
            EvalVar contextVar = GetEvalVarFromContext(DifficultyTierRangeProto.Context, data, false);
            if (FromValue(contextVar, out Entity entity))
            {
                WorldEntity worldEntity = entity as WorldEntity;
                Region region = worldEntity?.Region;
                if (region == null && entity is Player player)
                    region = player.GetRegion();
                if (region != null)
                    tierRef = region.DifficultyTierRef;
            }
            else if (FromValue(contextVar, out PropertyCollection collection, data.Game))
                tierRef = collection.GetProperty(PropertyEnum.DifficultyTier);

            if (tierRef == PrototypeId.Invalid)
            {
                evalVar.SetBool(true);
                return evalVar;
            }
            else
            {
                evalVar.SetBool(DifficultyTierPrototype.InRange(tierRef, DifficultyTierRangeProto.Min, DifficultyTierRangeProto.Max));
                return evalVar;
            }
        }

        private static EvalVar RunMissionIsActive(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();
            if (evalProto is not MissionIsActivePrototype missionIsActiveProto) return evalVar;
            if (FromValue(GetEvalVarFromContext(missionIsActiveProto.Context, data, false), out Entity entity) == false) return evalVar;

            Player player = entity as Player;
            if (player == null && entity is Avatar avatar)
                player = avatar.GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(evalVar, "Context is not a player.");

            MissionPrototype missionProto = GameDatabase.GetPrototype<MissionPrototype>(missionIsActiveProto.Mission);
            if (missionProto == null) return Logger.WarnReturn(evalVar, "Missing Mission field.");

            if (missionProto.ApprovedForUse() == false || missionProto.IsLiveTuningEnabled() == false)
            {
                evalVar.SetBool(false);
                return evalVar;
            }

            Mission mission = MissionManager.FindMissionForPlayer(player, missionIsActiveProto.Mission);
            evalVar.SetBool(mission != null && mission.State == MissionState.Active);
            return evalVar;
        }

        private static EvalVar RunMissionIsComplete(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();

            if (evalProto is not MissionIsCompletePrototype missionIsCompleteProto) return evalVar;

            if (FromValue(GetEvalVarFromContext(missionIsCompleteProto.Context, data, false), out Entity entity) == false) return evalVar;

            Player player = entity?.GetSelfOrOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(evalVar, "Context is not a player.");

            MissionPrototype missionProto = GameDatabase.GetPrototype<MissionPrototype>(missionIsCompleteProto.Mission);
            if (missionProto == null) return evalVar;

            if (missionProto.ApprovedForUse() == false || missionProto.IsLiveTuningEnabled() == false)
            {
                evalVar.SetBool(false);
                return evalVar;
            }

            bool avatarMissionState = false;
            Avatar avatar = null;
            if (missionProto.SaveStatePerAvatar)
            {
                avatar = entity as Avatar;
                if (avatar == null) return Logger.WarnReturn(evalVar, "Mission state is per-avatar but Context is not an avatar.");
                if (player.PrimaryAvatar != avatar)
                    avatarMissionState = true;
            }

            if (avatarMissionState)
                evalVar.SetBool((int)avatar.Properties[PropertyEnum.AvatarMissionState, missionProto.DataRef] == (int)MissionState.Completed);
            else
            {
                Mission mission = MissionManager.FindMissionForPlayer(player, missionProto.DataRef);
                evalVar.SetBool(mission != null && mission.State == MissionState.Completed);
            }

            return evalVar;
        }

        private static EvalVar RunNot(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();
            if (evalProto is not NotPrototype NotProto) return evalVar;
            if (NotProto.Arg == null) return evalVar;

            EvalVar arg1 = Run(NotProto.Arg, data);
            if (arg1.Type != EvalReturnType.Bool)
                return Logger.WarnReturn(evalVar, "Not: Non-Bool/Error field Arg");

            evalVar.SetBool(!arg1.Value.Bool);
            return evalVar;
        }

        private static EvalVar RunOr(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();
            if (evalProto is not OrPrototype OrProto) return evalVar;
            if (OrProto.Arg1 == null || OrProto.Arg2 == null) return evalVar;

            EvalVar arg1 = Run(OrProto.Arg1, data);
            if (arg1.Type != EvalReturnType.Bool)
                return Logger.WarnReturn(evalVar, "Or: Non-Bool/Error field Arg1");

            if (arg1.Value.Bool)
                evalVar.SetBool(true); 
            else
            {
                EvalVar arg2 = Run(OrProto.Arg2, data);
                if (arg2.Type != EvalReturnType.Bool)
                    return Logger.WarnReturn(evalVar, "Or: Non-Bool/Error field Arg2");
                evalVar.SetBool(arg2.Value.Bool);
            }

            return evalVar;
        }

        private static EvalVar RunHasEntityInInventory(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();

            if (evalProto is not HasEntityInInventoryPrototype hasEntityInInventoryProto) return evalVar;
            if (FromValue(GetEvalVarFromContext(hasEntityInInventoryProto.Context, data, false), out Entity inventoryOwner) == false)
                return Logger.WarnReturn(evalVar, "Inventory owner is not valid. Make sure you're using the var1 (or EntityPointer) context.");

            PrototypeId entityRef = hasEntityInInventoryProto.Entity;
            InventoryConvenienceLabel inventoryLabel = hasEntityInInventoryProto.Inventory;
            if (inventoryLabel == InventoryConvenienceLabel.None)
                return Logger.WarnReturn(evalVar, "The EntityInventory field in the HasEntityInInventoryPrototype is not valid.");

            BlueprintId parent = BlueprintId.Invalid;
            var dataDir = GameDatabase.DataDirectory;
            if (entityRef != PrototypeId.Invalid && dataDir.PrototypeIsADefaultPrototype(entityRef))
                parent = dataDir.GetPrototypeBlueprintDataRef(entityRef);

            Inventory inventory = inventoryOwner.GetInventory(inventoryLabel);
            bool inInventory = false;
            if (inventory != null)
            {
                EntityManager entityManager = inventoryOwner.Game.EntityManager;

                foreach (var entry in inventory)
                {
                    Entity inventoryEntity = entityManager.GetEntity<WorldEntity>(entry.Id);
                    if (inventoryEntity == null) continue;
                    PrototypeId inventoryEntityRef = inventoryEntity.PrototypeDataRef;
                    if (entityRef == PrototypeId.Invalid
                        || inventoryEntityRef == entityRef
                        || (parent != BlueprintId.Invalid && dataDir.PrototypeIsChildOfBlueprint(inventoryEntityRef, parent)))
                    {
                        inInventory = true;
                        break;
                    }
                }
            }


            evalVar.SetBool(inInventory);
            return evalVar;
        }

        private static EvalVar RunLoadAssetRef(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new();           
            if (evalProto is not LoadAssetRefPrototype loadAssetRefProto)
            {
                evalVar.SetError();
                return evalVar;
            }
            evalVar.SetAssetRef(loadAssetRefProto.Value);
            return evalVar;
        }

        private static EvalVar RunLoadBool(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new();            
            if (evalProto is not LoadBoolPrototype loadBoolProto)
            {
                evalVar.SetError();
                return evalVar;
            }
            evalVar.SetBool(loadBoolProto.Value);
            return evalVar;
        }

        private static EvalVar RunLoadFloat(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            if (evalProto is not LoadFloatPrototype loadFloatProto)
            {
                evalVar.SetError();
                return evalVar;
            }
            evalVar.SetFloat(loadFloatProto.Value);
            return evalVar;
        }

        private static EvalVar RunLoadInt(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            if (evalProto is not LoadIntPrototype loadIntProto)
            {
                evalVar.SetError();
                return evalVar;
            }
            evalVar.SetInt(loadIntProto.Value);
            return evalVar;
        }

        private static EvalVar RunLoadProtoRef(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            if (evalProto is not LoadProtoRefPrototype loadProtoRefProto)
            {
                evalVar.SetError();
                return evalVar;
            }
            evalVar.SetProtoRef(loadProtoRefProto.Value);
            return evalVar;
        }

        private static EvalVar RunLoadContextInt(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();
            if (evalProto is not LoadContextIntPrototype loadContextIntProto) return evalVar;

            EvalContext context = loadContextIntProto.Context;
            if (context < 0 || context >= EvalContext.MaxVars)
                return Logger.WarnReturn(evalVar, $"LoadContextInt: Context ({context}) is out of the bounds of possible context vars ({EvalContext.MaxVars})");

            if (data.ContextVars[(int)context].Var.IsNumeric() == false)
                return Logger.WarnReturn(evalVar, $"LoadContextInt: Non-Numeric value in Context Var {context}");

            FromValue(data.ContextVars[(int)context].Var, out long resultInt);
            evalVar.SetInt(resultInt);

            return evalVar;
        }

        private static EvalVar RunLoadContextProtoRef(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();
            if (evalProto is not LoadContextProtoRefPrototype loadContextProtoRefProto) return evalVar;

            EvalContext context = loadContextProtoRefProto.Context;
            if (context < 0 || context >= EvalContext.MaxVars)
                return Logger.WarnReturn(evalVar, $"LoadContextProtoRef: Context ({context}) is out of the bounds of possible context vars ({EvalContext.MaxVars})");

            FromValue(data.ContextVars[(int)context].Var, out PrototypeId resultProtoRef);
            evalVar.SetProtoRef(resultProtoRef);

            return evalVar;
        }

        private static EvalVar RunFor(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();

            if (evalProto is not ForPrototype forProto) return evalVar;

            if (forProto.LoopVarInit == null)
                return Logger.WarnReturn(evalVar, "No eval in For eval LoopVarInit");

            if (forProto.LoopAdvance == null)
                return Logger.WarnReturn(evalVar, "No eval in For eval LoopAdvance");

            if (forProto.LoopCondition == null)
                return Logger.WarnReturn(evalVar, "No eval in For eval LoopCondition");

            if (forProto.ScopeLoopBody.IsNullOrEmpty())
                return Logger.WarnReturn(evalVar, "No evals in For eval ScopeLoopBody");

            var dataCallerStackProps = data.CallerStackProperties;
            var dataLocalStackProps = data.LocalStackProperties;
            data.CallerStackProperties = dataLocalStackProps;
            using var localStackProps = ObjectPoolManager.Instance.Get<PropertyCollection>();
            data.LocalStackProperties = localStackProps;

            if (forProto.PreLoop != null)
            {
                evalVar = Run(forProto.PreLoop, data);
                if (evalVar.Type == EvalReturnType.Error) return Return();
            }

            evalVar = Run(forProto.LoopVarInit, data);
            if (evalVar.Type == EvalReturnType.Error) return Return();

            evalVar = Run(forProto.LoopCondition, data);
            if (evalVar.Type != EvalReturnType.Bool) return Return();

            while (evalVar.Value.Bool)
            {
                foreach (var eachProto in forProto.ScopeLoopBody)
                {
                    if (eachProto == null) continue;

                    data.CallerStackProperties = dataLocalStackProps;
                    data.LocalStackProperties = localStackProps;

                    evalVar = Run(eachProto, data);
                    if (evalVar.Type == EvalReturnType.Error) return Return();
                }

                data.CallerStackProperties = dataLocalStackProps;
                data.LocalStackProperties = localStackProps;

                evalVar = Run(forProto.LoopAdvance, data);
                if (evalVar.Type == EvalReturnType.Error) return Return();

                evalVar = Run(forProto.LoopCondition, data);
                if (evalVar.Type != EvalReturnType.Bool) return Return();
            }

            data.CallerStackProperties = dataCallerStackProps;
            data.LocalStackProperties = localStackProps;

            if (forProto.PostLoop != null)
            {
                evalVar = Run(forProto.PostLoop, data);
                if (evalVar.Type == EvalReturnType.Error) return Return();
            }

            return Return();

            EvalVar Return()
            {
                data.CallerStackProperties = dataCallerStackProps;
                data.LocalStackProperties = dataLocalStackProps;
                return evalVar;
            }
        }

        private static EvalVar RunForEachConditionInContext(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();

            if (evalProto is not ForEachConditionInContextPrototype forEachProto) return evalVar;

            if (forEachProto.ScopeLoopBody.IsNullOrEmpty())
                return Logger.WarnReturn(evalVar, "No evals in ForEachProtoRefInContextRefList eval ScopeLoopBody");

            var dataCallerStackProps = data.CallerStackProperties;
            var dataLocalStackProps = data.LocalStackProperties;
            data.CallerStackProperties = dataLocalStackProps;
            using var localStackProps = ObjectPoolManager.Instance.Get<PropertyCollection>();
            data.LocalStackProperties = localStackProps;

            if (forEachProto.PreLoop != null)
            {
                evalVar = Run(forEachProto.PreLoop, data);
                if (evalVar.Type == EvalReturnType.Error) return Return();
            }

            if (FromValue(GetEvalVarFromContext(forEachProto.ConditionCollectionContext, data, false), out ConditionCollection conditionCollection) == false)
                return evalVar;

            if (conditionCollection != null)
                foreach (var condition in conditionCollection)
                {
                    data.SetVar_PropertyCollectionPtr(EvalContext.Condition, condition.Properties);
                    data.SetReadOnlyVar_ProtoRefVectorPtr(EvalContext.ConditionKeywords, condition.GetKeywords());

                    if (forEachProto.LoopConditionPreScope != null)
                    {
                        evalVar = Run(forEachProto.LoopConditionPreScope, data);
                        if (evalVar.Type != EvalReturnType.Bool) return Return();
                        if (evalVar.Value.Bool == false) break;
                    }

                    foreach (EvalPrototype eachProto in forEachProto.ScopeLoopBody)
                    {
                        if (eachProto == null) continue;

                        data.CallerStackProperties = dataLocalStackProps;
                        data.LocalStackProperties = localStackProps;

                        evalVar = Run(eachProto, data);
                        if (evalVar.Type == EvalReturnType.Error) return Return();
                    }

                    if (forEachProto.LoopConditionPostScope != null)
                    {
                        evalVar = Run(forEachProto.LoopConditionPostScope, data);
                        if (evalVar.Type != EvalReturnType.Bool) return Return();
                        if (evalVar.Value.Bool == false) break;
                    }
                }

            data.SetVar_ConditionCollectionPtr(EvalContext.Condition, null);
            data.SetVar_ProtoRefVectorPtr(EvalContext.ConditionKeywords, null);

            data.CallerStackProperties = dataLocalStackProps;
            data.LocalStackProperties = localStackProps;

            if (forEachProto.PostLoop != null)
            {
                evalVar = Run(forEachProto.PostLoop, data);
                if (evalVar.Type == EvalReturnType.Error) return Return();
            }

            return Return();

            EvalVar Return()
            {
                data.CallerStackProperties = dataCallerStackProps;
                data.LocalStackProperties = dataLocalStackProps;
                return evalVar;
            }
        }

        private static EvalVar RunForEachProtoRefInContextRefList(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();

            if (evalProto is not ForEachProtoRefInContextRefListPrototype forEachProto) return evalVar;

            if (forEachProto.ScopeLoopBody.IsNullOrEmpty())
                return Logger.WarnReturn(evalVar, "No evals in ForEachProtoRefInContextRefList eval ScopeLoopBody");

            var dataCallerStackProps = data.CallerStackProperties;
            var dataLocalStackProps = data.LocalStackProperties;
            data.CallerStackProperties = dataLocalStackProps;
            using var localStackProps = ObjectPoolManager.Instance.Get<PropertyCollection>();
            data.LocalStackProperties = localStackProps;

            if (forEachProto.PreLoop != null)
            {
                evalVar = Run(forEachProto.PreLoop, data);
                if (evalVar.Type == EvalReturnType.Error) return Return();
            }

            if (RunForEachProtoRefInContextRefListType(forEachProto, data, ref evalVar, dataLocalStackProps, localStackProps) == false)
            {
                Logger.Warn("A ForEachProtoRefInContextRefList prototype specified a ProtoRefListContext that is not a valid PrototypeDataRefList or PrototypeDataRefVector.");
                return Return();
            }

            data.CallerStackProperties = dataLocalStackProps;
            data.LocalStackProperties = localStackProps;

            if (forEachProto.PostLoop != null)
            {
                evalVar = Run(forEachProto.PostLoop, data);
                if (evalVar.Type == EvalReturnType.Error)
                    return Return();
            }

            return Return();

            EvalVar Return()
            {
                data.CallerStackProperties = dataCallerStackProps;
                data.LocalStackProperties = dataLocalStackProps;
                return evalVar;
            }
        }

        private static bool RunForEachProtoRefInContextRefListType(ForEachProtoRefInContextRefListPrototype forEachProto, EvalContextData data, ref EvalVar evalVar,
            PropertyCollection originalLocalStackProps, PropertyCollection localStackProps)
        {
            if (forEachProto == null || originalLocalStackProps == null) return false;

            EvalVar varList = GetEvalVarFromContext(forEachProto.ProtoRefListContext, data, false);
            if (FromValue(varList, out List<PrototypeId> protoRefList) == false)
            {
                if (FromValue(varList, out PrototypeId[] protoRefVector)) // part from Eval::runForEachProtoRefInContextRefList
                    protoRefList = new (protoRefVector);
                else
                    return false;
            }

            if (protoRefList != null)
                foreach (var protoRef in protoRefList)
                {
                    if (forEachProto.LoopCondition != null)
                    {
                        evalVar = Run(forEachProto.LoopCondition, data);
                        if (evalVar.Type != EvalReturnType.Bool) return false;
                        if (evalVar.Value.Bool == false) break;
                    }

                    localStackProps[PropertyEnum.EvalLoopVarProtoRef, 0] = protoRef;

                    foreach (var evalProto in forEachProto.ScopeLoopBody)
                    {
                        if (evalProto == null) continue;

                        data.CallerStackProperties = originalLocalStackProps;
                        data.LocalStackProperties = localStackProps;

                        evalVar = Run(evalProto, data);
                        if (evalVar.Type == EvalReturnType.Error) return false;
                    }
                }

            return true;
        }

        private static EvalVar RunIfElse(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();
            if (evalProto is not IfElsePrototype ifElseProto) return evalVar;

            if (ifElseProto.Conditional == null)
                return Logger.WarnReturn(evalVar, "IfElse Eval with a NULL Conditional field!");

            if (ifElseProto.EvalIf == null)
                return Logger.WarnReturn(evalVar, "IfElse Eval with a NULL EvalIf field!");

            EvalVar conditionalVar = Run(ifElseProto.Conditional, data);
            bool conditionalValue;
            switch (conditionalVar.Type)
            {
                case EvalReturnType.Bool:
                case EvalReturnType.Float:
                case EvalReturnType.Int:
                    FromValue(conditionalVar, out conditionalValue);
                    break;
                default:
                    return Logger.WarnReturn(evalVar, "Non-Value/Bool Conditional.");
            }

            if (conditionalValue)
                evalVar = Run(ifElseProto.EvalIf, data);
            else
                if (ifElseProto.EvalElse != null)
                    evalVar = Run(ifElseProto.EvalElse, data);
                else
                    evalVar.SetUndefined();

            return evalVar;
        }

        private static EvalVar RunScope(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();
            if (evalProto is not ScopePrototype scopeProto) return evalVar;

            if (scopeProto.Scope.IsNullOrEmpty())
                return Logger.WarnReturn(evalVar, "No eval entries in Scope eval");

            var dataCallerStackProps = data.CallerStackProperties;
            var dataLocalStackProps = data.LocalStackProperties;
            using var localStackProps = ObjectPoolManager.Instance.Get<PropertyCollection>();

            bool errors = false;
            foreach (var evalEach in scopeProto.Scope)
            {
                if (evalEach == null) continue;

                data.CallerStackProperties = dataLocalStackProps;
                data.LocalStackProperties = localStackProps;

                evalVar = Run(evalEach, data);
                if (evalVar.Type == EvalReturnType.Error) errors = true;
            }

            data.CallerStackProperties = dataCallerStackProps;
            data.LocalStackProperties = dataLocalStackProps;

            if (errors)
                evalVar.SetError();

            return evalVar;
        }

        private static EvalVar RunExportError(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();
            Logger.Warn("Eval failed to export correctly from Calligraphy");
            return evalVar;
        }

        private static EvalVar RunLoadCurve(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();
            if (evalProto is not LoadCurvePrototype loadCurveProto) return evalVar;

            if (loadCurveProto.Curve == CurveId.Invalid)
                return Logger.WarnReturn(evalVar, "LoadCurvePrototype contains Invalid \"Curve\" Field");

            if (loadCurveProto.Index == null)
                return Logger.WarnReturn(evalVar, "LoadCurvePrototype contains NULL \"Index\" Field");

            Curve curve = GameDatabase.GetCurve(loadCurveProto.Curve);
            if (curve == null) return evalVar;

            EvalVar indexVar = Run(loadCurveProto.Index, data);
            int index;
            switch (indexVar.Type)
            {
                case EvalReturnType.Int:
                    index = (int)indexVar.Value.Int;
                    break;
                case EvalReturnType.Float:
                    index = (int)indexVar.Value.Float;
                    break;
                default:
                    return Logger.WarnReturn(evalVar, $"LoadCurvePrototype contains an invalid var type for its \"Index\" Field! (Index var type=[{indexVar.Type}])");
            }

            if (curve.IndexInRange(index) == false)
            {
                Logger.Warn($"LoadCurvePrototype index ({index}) is out of range of the curve {GameDatabase.GetCurveName(loadCurveProto.Curve)}, clamping to bounds and still running");
                index = Math.Clamp(index, curve.MinPosition, curve.MaxPosition);
            }

            evalVar.SetFloat(curve.GetAt(index));
            return evalVar;
        }

        private static EvalVar RunAdd(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();
            if (evalProto is not AddPrototype addProto) return evalVar;

            EvalVar arg1 = Run(addProto.Arg1, data);
            if (arg1.IsNumeric() == false)
                return Logger.WarnReturn(evalVar, "Add: Non-Numeric/Error field Arg1");

            EvalVar arg2 = Run(addProto.Arg2, data);
            if (arg2.IsNumeric() == false)
                return Logger.WarnReturn(evalVar, "Add: Non-Numeric/Error field Arg2");

            if (arg1.Type == EvalReturnType.Int && arg2.Type == EvalReturnType.Int)
                evalVar.SetInt(arg1.Value.Int + arg2.Value.Int);
            else if (arg1.Type == EvalReturnType.Int && arg2.Type == EvalReturnType.Float)
                evalVar.SetFloat(arg1.Value.Int + arg2.Value.Float);
            else if (arg1.Type == EvalReturnType.Float && arg2.Type == EvalReturnType.Int)
                evalVar.SetFloat(arg1.Value.Float + arg2.Value.Int);
            else if (arg1.Type == EvalReturnType.Float && arg2.Type == EvalReturnType.Float)
                evalVar.SetFloat(arg1.Value.Float + arg2.Value.Float);
            else
                Logger.Warn("Error with arg types!");

            return evalVar;
        }

        private static EvalVar RunDiv(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();
            if (evalProto is not DivPrototype divProto) return evalVar;

            EvalVar arg1 = Run(divProto.Arg1, data);
            if (arg1.IsNumeric() == false)
                return Logger.WarnReturn(evalVar, "Div: Non-Numeric/Error field Arg1");

            EvalVar arg2 = Run(divProto.Arg2, data);
            if (arg2.IsNumeric() == false)
                return Logger.WarnReturn(evalVar, "Div: Non-Numeric/Error field Arg2");

            if (arg2.Type == EvalReturnType.Int && arg2.Value.Int == 0)
                return Logger.WarnReturn(evalVar, "Div: Arg2=0 DIVZERO!");
            else if (arg2.Type == EvalReturnType.Float && arg2.Value.Float == 0)
                return Logger.WarnReturn(evalVar, "Div: Arg2=0.0f DIVZERO!");

            if (arg1.Type == EvalReturnType.Int && arg2.Type == EvalReturnType.Int)
                evalVar.SetFloat(arg1.Value.Int / (float)arg2.Value.Int);
            else if (arg1.Type == EvalReturnType.Int && arg2.Type == EvalReturnType.Float)
                evalVar.SetFloat(arg1.Value.Int / arg2.Value.Float);
            else if (arg1.Type == EvalReturnType.Float && arg2.Type == EvalReturnType.Int)
                evalVar.SetFloat(arg1.Value.Float / arg2.Value.Int);
            else if (arg1.Type == EvalReturnType.Float && arg2.Type == EvalReturnType.Float)
                evalVar.SetFloat(arg1.Value.Float / arg2.Value.Float);
            else
                Logger.Warn("Error with arg types!");

            return evalVar;
        }

        private static EvalVar RunExponent(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();
            if (evalProto is not ExponentPrototype exponentProto) return evalVar;

            EvalVar baseVar = Run(exponentProto.BaseArg, data);
            if (baseVar.IsNumeric() == false)
                return Logger.WarnReturn(evalVar, "Exponent: Non-Numeric/Error field Base");

            EvalVar exponentVar = Run(exponentProto.ExpArg, data);
            if (exponentVar.IsNumeric() == false)
                return Logger.WarnReturn(evalVar, "Exponent: Non-Numeric/Error field Exponent");

            if (FromValue(baseVar, out float baseFloat) == false)
                return Logger.WarnReturn(evalVar, "Exponent: Error Extracting Base from evalVar");

            if (FromValue(exponentVar, out float expFloat) == false)
                return Logger.WarnReturn(evalVar, "Exponent: Error Extracting Exponent from evalVar");

            evalVar.SetFloat(MathF.Pow(baseFloat, expFloat));
            return evalVar;
        }

        private static EvalVar RunMax(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();
            if (evalProto is not MaxPrototype maxProto) return evalVar;

            EvalVar arg1 = Run(maxProto.Arg1, data);
            if (arg1.IsNumeric() == false)
                return Logger.WarnReturn(evalVar, "Max: Non-Numeric/Error field Arg1");

            EvalVar arg2 = Run(maxProto.Arg2, data);
            if (arg2.IsNumeric() == false)
                return Logger.WarnReturn(evalVar, "Max: Non-Numeric/Error field Arg2");

            if (arg1.Type == EvalReturnType.Int && arg2.Type == EvalReturnType.Int)
                evalVar.SetInt(Math.Max(arg1.Value.Int, arg2.Value.Int));
            else if (arg1.Type == EvalReturnType.Int && arg2.Type == EvalReturnType.Float)
                evalVar.SetFloat(Math.Max(arg1.Value.Int, arg2.Value.Float));
            else if (arg1.Type == EvalReturnType.Float && arg2.Type == EvalReturnType.Int)
                evalVar.SetFloat(Math.Max(arg1.Value.Float, arg2.Value.Int));
            else if (arg1.Type == EvalReturnType.Float && arg2.Type == EvalReturnType.Float)
                evalVar.SetFloat(Math.Max(arg1.Value.Float, arg2.Value.Float));
            else
                Logger.Warn("Error with arg types!");

            return evalVar;
        }

        private static EvalVar RunMin(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();
            if (evalProto is not MinPrototype minProto) return evalVar;

            EvalVar arg1 = Run(minProto.Arg1, data);
            if (arg1.IsNumeric() == false)
                return Logger.WarnReturn(evalVar, "Min: Non-Numeric/Error field Arg1");

            EvalVar arg2 = Run(minProto.Arg2, data);
            if (arg2.IsNumeric() == false)
                return Logger.WarnReturn(evalVar, "Min: Non-Numeric/Error field Arg2");

            if (arg1.Type == EvalReturnType.Int && arg2.Type == EvalReturnType.Int)
                evalVar.SetInt(Math.Min(arg1.Value.Int, arg2.Value.Int));
            else if (arg1.Type == EvalReturnType.Int && arg2.Type == EvalReturnType.Float)
                evalVar.SetFloat(Math.Min(arg1.Value.Int, arg2.Value.Float));
            else if (arg1.Type == EvalReturnType.Float && arg2.Type == EvalReturnType.Int)
                evalVar.SetFloat(Math.Min(arg1.Value.Float, arg2.Value.Int));
            else if (arg1.Type == EvalReturnType.Float && arg2.Type == EvalReturnType.Float)
                evalVar.SetFloat(Math.Min(arg1.Value.Float, arg2.Value.Float));
            else
                Logger.Warn("Error with arg types!");

            return evalVar;
        }

        private static EvalVar RunModulus(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();
            if (evalProto is not ModulusPrototype modulusProto) return evalVar;

            EvalVar arg1 = Run(modulusProto.Arg1, data);
            if (arg1.IsNumeric() == false)
                return Logger.WarnReturn(evalVar, "Modulus: Non-Numeric/Error field Arg1");

            EvalVar arg2 = Run(modulusProto.Arg2, data);
            if (arg2.IsNumeric() == false)
                return Logger.WarnReturn(evalVar, "Modulus: Non-Numeric/Error field Arg2");
            
            if (arg1.Type == EvalReturnType.Int && arg2.Type == EvalReturnType.Int)
                evalVar.SetInt(MathHelper.Modulus(arg1.Value.Int, arg2.Value.Int));
            else if (arg1.Type == EvalReturnType.Int && arg2.Type == EvalReturnType.Float)
                evalVar.SetFloat(MathHelper.FloatModulus(arg1.Value.Int, arg2.Value.Float));
            else if (arg1.Type == EvalReturnType.Float && arg2.Type == EvalReturnType.Int)
                evalVar.SetFloat(MathHelper.FloatModulus(arg1.Value.Float, arg2.Value.Int));
            else if (arg1.Type == EvalReturnType.Float && arg2.Type == EvalReturnType.Float)
                evalVar.SetFloat(MathHelper.FloatModulus(arg1.Value.Float, arg2.Value.Float));
            else
                Logger.Warn("Error with arg types!");

            return evalVar;
        }

        private static EvalVar RunMult(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();
            if (evalProto is not MultPrototype multProto) return evalVar;

            EvalVar arg1 = Run(multProto.Arg1, data);
            if (arg1.IsNumeric() == false)
                return Logger.WarnReturn(evalVar, "Multiply: Non-Numeric/Error field Arg1");

            EvalVar arg2 = Run(multProto.Arg2, data);
            if (arg2.IsNumeric() == false)
                return Logger.WarnReturn(evalVar, "Multiply: Non-Numeric/Error field Arg2");

            if (arg1.Type == EvalReturnType.Int && arg2.Type == EvalReturnType.Int)
                evalVar.SetInt(arg1.Value.Int * arg2.Value.Int);
            else if (arg1.Type == EvalReturnType.Int && arg2.Type == EvalReturnType.Float)
                evalVar.SetFloat(arg1.Value.Int * arg2.Value.Float);
            else if (arg1.Type == EvalReturnType.Float && arg2.Type == EvalReturnType.Int)
                evalVar.SetFloat(arg1.Value.Float * arg2.Value.Int);
            else if (arg1.Type == EvalReturnType.Float && arg2.Type == EvalReturnType.Float)
                evalVar.SetFloat(arg1.Value.Float * arg2.Value.Float);
            else
                Logger.Warn("Error with arg types!");

            return evalVar;
        }

        private static EvalVar RunSub(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();
            if (evalProto is not SubPrototype subProto) return evalVar;

            EvalVar arg1 = Run(subProto.Arg1, data);
            if (arg1.IsNumeric() == false)
                return Logger.WarnReturn(evalVar, "Subtract: Non-Numeric/Error field Arg1");

            EvalVar arg2 = Run(subProto.Arg2, data);
            if (arg2.IsNumeric() == false)
                return Logger.WarnReturn(evalVar, "Subtract: Non-Numeric/Error field Arg2");

            if (arg1.Type == EvalReturnType.Int && arg2.Type == EvalReturnType.Int)
                evalVar.SetInt(arg1.Value.Int - arg2.Value.Int);
            else if (arg1.Type == EvalReturnType.Int && arg2.Type == EvalReturnType.Float)
                evalVar.SetFloat(arg1.Value.Int - arg2.Value.Float);
            else if (arg1.Type == EvalReturnType.Float && arg2.Type == EvalReturnType.Int)
                evalVar.SetFloat(arg1.Value.Float - arg2.Value.Int);
            else if (arg1.Type == EvalReturnType.Float && arg2.Type == EvalReturnType.Float)
                evalVar.SetFloat(arg1.Value.Float - arg2.Value.Float);
            else
                Logger.Warn("Error with arg types!");

            return evalVar;
        }

        private static EvalVar RunAssignProp(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();

            if (evalProto is not AssignPropPrototype assignPropProto) return evalVar;

            if (assignPropProto.Eval == null)
                return Logger.WarnReturn(evalVar, "AssignPropPrototype contains NULL \"Eval\" Field");

            if (assignPropProto.Prop == PropertyId.Invalid)
                return Logger.WarnReturn(evalVar, "AssignPropPrototype contains Invalid \"Prop\" Field");

            EvalVar assignVar = Run(assignPropProto.Eval, data);
            if (assignVar.Type == EvalReturnType.Error || assignVar.Type == EvalReturnType.Undefined)
                return Logger.WarnReturn(evalVar, "AssignPrototype has Eval that returns Error or Undefined Value");

            if (FromValue(GetEvalVarFromContext(assignPropProto.Context, data, true), out PropertyCollection collection, data.Game) == false)
                return evalVar;

            if (collection == null)
                return Logger.WarnReturn(evalVar, "Invalid Context");

            PropertyId propId = assignPropProto.Prop;
            PropertyEnum propEnum = propId.Enum;
            PropertyInfo propInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propEnum);
            PropertyDataType propertyType = propInfo.DataType;

            switch (propertyType)
            {
                case PropertyDataType.Integer:
                    if (FromValue(assignVar, out long intValue))
                        collection[propId] = intValue;
                    else
                        return Logger.WarnReturn(evalVar, $"Unable to convert TYPE to Int, Property: [{propInfo.PropertyName}]");
                    break;

                case PropertyDataType.Real:
                    if (FromValue(assignVar, out float floatValue))
                        collection[propId] = floatValue;
                    else
                        return Logger.WarnReturn(evalVar, $"Unable to convert TYPE to Float, Property: [{propInfo.PropertyName}]");
                    break;

                case PropertyDataType.Boolean:
                    if (FromValue(assignVar, out bool boolValue))
                        collection[propId] = boolValue;
                    else
                        return Logger.WarnReturn(evalVar, $"Unable to convert TYPE to Bool, Property: [{propInfo.PropertyName}]");
                    break;

                case PropertyDataType.EntityId:
                    if (FromValue(assignVar, out ulong entityIdValue))
                        collection[propId] = entityIdValue;
                    else
                        return Logger.WarnReturn(evalVar, $"Unable to convert TYPE to EntityId, Property: [{propInfo.PropertyName}]");
                    break;

                case PropertyDataType.RegionId:
                    if (FromValue(assignVar, out ulong regionIdValue))
                        collection[propId] = regionIdValue;
                    else
                        return Logger.WarnReturn(evalVar, $"Unable to convert TYPE to RegionId, Property: [{propInfo.PropertyName}]");
                    break;

                case PropertyDataType.Prototype:
                    if (FromValue(assignVar, out PrototypeId protoRefValue))
                        collection[propId] = protoRefValue;
                    else
                        return Logger.WarnReturn(evalVar, $"Unable to convert TYPE to Prototype, Property: [{propInfo.PropertyName}]");
                    break;

                case PropertyDataType.Asset:
                    if (FromValue(assignVar, out AssetId assetValue))
                        collection[propId] = assetValue;
                    else
                        return Logger.WarnReturn(evalVar, $"Unable to convert TYPE to Asset, Property: [{propInfo.PropertyName}]");
                    break;

                case PropertyDataType.Time:
                    if (FromValue(assignVar, out long timeSpanValue))
                        collection[propId] = TimeSpan.FromMilliseconds(timeSpanValue);
                    else
                        return Logger.WarnReturn(evalVar, $"Unable to convert TYPE to Int, Property: [{propInfo.PropertyName}]");
                    break;

                default:
                    return Logger.WarnReturn(evalVar, $"Assignment into invalid property (property type is not int/float/bool)! Property: [{propInfo.PropertyName}]");
            }

            evalVar.SetUndefined();
            return evalVar;
        }

        private static EvalVar RunAssignPropEvalParams(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();

            if (evalProto is not AssignPropEvalParamsPrototype assignPropEvalParamsProto)
                return evalVar;

            if (assignPropEvalParamsProto.Eval == null)
                return Logger.WarnReturn(evalVar, "AssignPropEvalParamsPrototype contains NULL \"Eval\" Field");

            if (assignPropEvalParamsProto.Prop == PrototypeId.Invalid)
                return Logger.WarnReturn(evalVar, "AssignPropEvalParamsPrototype contains Invalid \"Prop\" Field");

            EvalVar assignVar = Run(assignPropEvalParamsProto.Eval, data);
            if (assignVar.Type == EvalReturnType.Error || assignVar.Type == EvalReturnType.Undefined)
                return Logger.WarnReturn(evalVar, "AssignPropEvalParamsPrototype has Eval that returns Error or Undefined Value");

            if (FromValue(GetEvalVarFromContext(assignPropEvalParamsProto.Context, data, true), out PropertyCollection collection, data.Game) == false)
                return evalVar;

            if (collection == null)
                return Logger.WarnReturn(evalVar, "Invalid Context");

            PropertyInfoTable propInfoTable = GameDatabase.PropertyInfoTable;
            PropertyEnum propEnum = propInfoTable.GetPropertyEnumFromPrototype(assignPropEvalParamsProto.Prop);
            PropertyInfo propInfo = propInfoTable.LookupPropertyInfo(propEnum);

            Span<PropertyParam> paramValues = stackalloc PropertyParam[Property.MaxParamCount];
            propInfo.DefaultParamValues.CopyTo(paramValues);

            for (int i = 0; i < propInfo.ParamCount; i++)
            {
                if (i >= 4) break;
                EvalPrototype paramEval = i switch
                {
                    0 => assignPropEvalParamsProto.Param0,
                    1 => assignPropEvalParamsProto.Param1,
                    2 => assignPropEvalParamsProto.Param2,
                    3 => assignPropEvalParamsProto.Param3,
                    _ => null
                };

                if (paramEval == null) continue;

                switch (propInfo.GetParamType(i))
                {
                    case PropertyParamType.Asset:
                        if (FromValue(Run(paramEval, data), out AssetId assetParam))
                            paramValues[i] = Property.ToParam(assetParam);
                        break;

                    case PropertyParamType.Prototype:
                        if (FromValue(Run(paramEval, data), out PrototypeId protoRefParam))
                            paramValues[i] = Property.ToParam(propEnum, i, protoRefParam);
                        break;

                    case PropertyParamType.Integer:
                        if (FromValue(Run(paramEval, data), out int intParam))
                            paramValues[i] = (PropertyParam)intParam;
                        break;

                    default:
                        return Logger.WarnReturn(evalVar, "Encountered an unknown prop param type in an AssignPropEvalParams Eval!");
                }
            }

            PropertyId propId = new(propEnum, paramValues);

            switch (propInfo.DataType)
            {
                case PropertyDataType.Integer:
                    if (FromValue(assignVar, out long intValue))
                        collection[propId] = intValue;
                    else
                        return Logger.WarnReturn(evalVar, $"Unable to convert TYPE to Int, Property: {propInfo.PropertyName}");
                    break;

                case PropertyDataType.Real:
                    if (FromValue(assignVar, out float floatValue))
                        collection[propId] = floatValue;
                    else
                        return Logger.WarnReturn(evalVar, $"Unable to convert TYPE to Float, Property: {propInfo.PropertyName}");
                    break;

                case PropertyDataType.Boolean:
                    if (FromValue(assignVar, out bool boolValue))
                        collection[propId] = boolValue;
                    else
                        return Logger.WarnReturn(evalVar, $"Unable to convert TYPE to Bool, Property: {propInfo.PropertyName}");
                    break;

                case PropertyDataType.EntityId:
                    if (FromValue(assignVar, out ulong entityIdValue))
                        collection[propId] = entityIdValue;
                    else
                        return Logger.WarnReturn(evalVar, $"Unable to convert TYPE to EntityId, Property: {propInfo.PropertyName}");
                    break;

                case PropertyDataType.RegionId:
                    if (FromValue(assignVar, out ulong regionIdValue))
                        collection[propId] = regionIdValue;
                    else
                        return Logger.WarnReturn(evalVar, $"Unable to convert TYPE to RegionId, Property: {propInfo.PropertyName}");
                    break;

                case PropertyDataType.Prototype:
                    if (FromValue(assignVar, out PrototypeId protoRefValue))
                        collection[propId] = protoRefValue;
                    else
                        return Logger.WarnReturn(evalVar, $"Unable to convert TYPE to Prototype, Property: {propInfo.PropertyName}");
                    break;

                case PropertyDataType.Asset:
                    if (FromValue(assignVar, out AssetId assetValue))
                        collection[propId] = assetValue;
                    else
                        return Logger.WarnReturn(evalVar, $"Unable to convert TYPE to Asset, Property: {propInfo.PropertyName}");
                    break;

                default:
                    return Logger.WarnReturn(evalVar, $"Assignment into invalid property (property type is not int/float/bool)! Property: {propInfo.PropertyName}");
            }

            evalVar.SetUndefined();
            return evalVar;
        }

        private static EvalVar RunHasProp(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();
            if (evalProto is not HasPropPrototype hasPropProto) return evalVar;

            if (hasPropProto.Prop == PropertyId.Invalid)
                return Logger.WarnReturn(evalVar, "HasPropPrototype contains Invalid \"Prop\" Field");

            if (FromValue(GetEvalVarFromContext(hasPropProto.Context, data, false), out PropertyCollection collection, data.Game) == false)
                return evalVar;

            if (collection == null)
                return Logger.WarnReturn(evalVar, "Invalid Context");

            evalVar.SetBool(collection.HasProperty(hasPropProto.Prop));
            return evalVar;
        }

        private static EvalVar RunLoadProp(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();
            if (evalProto is not LoadPropPrototype loadPropProto) return evalVar;

            if (loadPropProto.Prop == PropertyId.Invalid)
                return Logger.WarnReturn(evalVar, "LoadPropPrototype contains Invalid \"Prop\" Field");

            PropertyId propId = loadPropProto.Prop;
            PropertyEnum propEnum = propId.Enum;
            PropertyInfo propInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propEnum);
            PropertyDataType propertyType = propInfo.DataType;

            if (FromValue(GetEvalVarFromContext(loadPropProto.Context, data, false), out PropertyCollection collection, data.Game) == false)
                return evalVar;

            if (collection == null)
                return Logger.WarnReturn(evalVar, $"Invalid Context ({loadPropProto.Context}) when trying to load prop.\nProp: {propInfo.PropertyName}");

            switch (propertyType)
            {
                case PropertyDataType.Integer:
                    evalVar.SetInt(collection[propId]);
                    break;

                case PropertyDataType.Real:
                case PropertyDataType.Curve:
                    evalVar.SetFloat(collection[propId]);
                    break;

                case PropertyDataType.EntityId:
                    evalVar.SetEntityId(collection[propId]);
                    break;

                case PropertyDataType.RegionId:
                    evalVar.SetRegionId(collection[propId]);
                    break;

                case PropertyDataType.Boolean:
                    evalVar.SetBool(collection[propId]);
                    break;

                case PropertyDataType.Prototype:
                    evalVar.SetProtoRef(collection[propId]);
                    break;

                case PropertyDataType.Asset:
                    evalVar.SetAssetRef(collection[propId]);
                    break;

                default:
                    return Logger.WarnReturn(evalVar, "Assignment into invalid property!");
            }

            return evalVar;
        }

        private static EvalVar RunLoadPropContextParams(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();
            if (evalProto is not LoadPropContextParamsPrototype loadPropContextParamsProto) return evalVar;

            if (loadPropContextParamsProto.Prop == PrototypeId.Invalid)
                return Logger.WarnReturn(evalVar, "LoadPropContextParamsPrototype contains invalid \"Prop\" field");

            if (FromValue(GetEvalVarFromContext(loadPropContextParamsProto.PropertyIdContext, data, false), out PropertyId propIdParams) == false)
                return evalVar;

            if (propIdParams == PropertyId.Invalid)
                return Logger.WarnReturn(evalVar, "LoadPropContextParams eval being run with a context that has an invalid propertyId");

            if (FromValue(GetEvalVarFromContext(loadPropContextParamsProto.PropertyCollectionContext, data, false), out PropertyCollection collection, data.Game) == false)
                return evalVar;

            if (collection == null)
                return Logger.WarnReturn(evalVar, "Invalid Context");

            PropertyInfoTable propInfoTable = GameDatabase.PropertyInfoTable;
            PropertyEnum propEnum = propInfoTable.GetPropertyEnumFromPrototype(loadPropContextParamsProto.Prop);
            PropertyInfo propInfoValue = propInfoTable.LookupPropertyInfo(propEnum);
            PropertyInfo propInfoParams = propInfoTable.LookupPropertyInfo(propIdParams.Enum);

            if (propInfoParams.ParamCount != propInfoValue.ParamCount)
                return evalVar;

            Span<PropertyParam> paramValues = stackalloc PropertyParam[propInfoParams.ParamCount];
            for (int i = 0; i < propInfoParams.ParamCount; ++i)
            {
                if (propInfoParams.GetParamType(i) != propInfoValue.GetParamType(i)) return evalVar;

                switch (propInfoParams.GetParamType(i))
                {
                    case PropertyParamType.Asset:
                        Property.FromParam(propIdParams.Enum, i, propIdParams.GetParam(i), out AssetId assetRefParam);
                        paramValues[i] = Property.ToParam(assetRefParam);
                        break;
                    case PropertyParamType.Prototype:
                        Property.FromParam(propIdParams.Enum, i, propIdParams.GetParam(i), out PrototypeId protoRefParam);
                        paramValues[i] = Property.ToParam(propEnum, i, protoRefParam);
                        break;
                    case PropertyParamType.Integer:
                        int intParam = (int)propIdParams.GetParam(i);
                        paramValues[i] = (PropertyParam)intParam;
                        break;
                    default:
                        return Logger.WarnReturn(evalVar, "Encountered an unknown prop param type in a LoadPropContextParams Eval!");
                }
            }

            PropertyId propIdValue = new(propEnum, paramValues);

            switch (propInfoValue.DataType)
            {
                case PropertyDataType.Integer:
                    evalVar.SetInt(collection[propIdValue]);
                    break;
                case PropertyDataType.Curve:
                case PropertyDataType.Real:
                    evalVar.SetFloat(collection[propIdValue]);
                    break;
                case PropertyDataType.Boolean:
                    evalVar.SetBool(collection[propIdValue]);
                    break;
                case PropertyDataType.EntityId:
                    evalVar.SetEntityId(collection[propIdValue]);
                    break;
                case PropertyDataType.RegionId:
                    evalVar.SetRegionId(collection[propIdValue]);
                    break;
                case PropertyDataType.Prototype:
                    evalVar.SetProtoRef(collection[propIdValue]);
                    break;
                case PropertyDataType.Asset:
                    evalVar.SetAssetRef(collection[propIdValue]);
                    break;
                default:
                    return Logger.WarnReturn(evalVar, "Assignment into invalid property!");
            }

            return evalVar;
        }

        private static EvalVar RunLoadPropEvalParams(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();
            if (evalProto is not LoadPropEvalParamsPrototype loadPropEvalParamsProto) return evalVar;

            if (loadPropEvalParamsProto.Prop == PrototypeId.Invalid)
                return Logger.WarnReturn(evalVar, "LoadPropEvalParamsPrototype contains invalid \"Prop\" field");

            if (FromValue(GetEvalVarFromContext(loadPropEvalParamsProto.Context, data, false), out PropertyCollection collection, data.Game) == false)
                return evalVar;

            if (collection == null)
                return Logger.WarnReturn(evalVar, "Invalid Context");

            PropertyInfoTable propInfoTable = GameDatabase.PropertyInfoTable;
            PropertyEnum propEnum = propInfoTable.GetPropertyEnumFromPrototype(loadPropEvalParamsProto.Prop);
            PropertyInfo propInfo = propInfoTable.LookupPropertyInfo(propEnum);

            Span<PropertyParam> paramValues = stackalloc PropertyParam[Property.MaxParamCount];
            propInfo.DefaultParamValues.CopyTo(paramValues);

            for (int i = 0; i < propInfo.ParamCount; ++i)
            {
                if (i >= 4) break;
                EvalPrototype paramEval = i switch
                {
                    0 => loadPropEvalParamsProto.Param0,
                    1 => loadPropEvalParamsProto.Param1,
                    2 => loadPropEvalParamsProto.Param2,
                    3 => loadPropEvalParamsProto.Param3,
                    _ => null
                };

                if (paramEval == null) continue;

                switch (propInfo.GetParamType(i))
                {
                    case PropertyParamType.Asset:
                        if (FromValue(Run(paramEval, data), out AssetId assetRefParam))
                            paramValues[i] = Property.ToParam(assetRefParam);
                        break;

                    case PropertyParamType.Prototype:
                        if (FromValue(Run(paramEval, data), out PrototypeId protoRefParam))
                            paramValues[i] = Property.ToParam(propEnum, i, protoRefParam);
                        break;

                    case PropertyParamType.Integer:
                        if (FromValue(Run(paramEval, data), out int intParam))
                            paramValues[i] = (PropertyParam)intParam;
                        break;

                    default:
                        return Logger.WarnReturn(evalVar, "Encountered an unknown prop param type in a LoadPropEvalParams Eval!");
                }
            }

            PropertyId propId = new(propEnum, paramValues);

            switch (propInfo.DataType)
            {
                case PropertyDataType.Integer:
                    evalVar.SetInt(collection[propId]);
                    break;
                case PropertyDataType.Real:
                case PropertyDataType.Curve:
                    evalVar.SetFloat(collection[propId]);
                    break;
                case PropertyDataType.Boolean:
                    evalVar.SetBool(collection[propId]);
                    break;
                case PropertyDataType.EntityId:
                    evalVar.SetEntityId(collection[propId]);
                    break;
                case PropertyDataType.RegionId:
                    evalVar.SetRegionId(collection[propId]);
                    break;
                case PropertyDataType.Prototype:
                    evalVar.SetProtoRef(collection[propId]);
                    break;
                case PropertyDataType.Asset:
                    evalVar.SetAssetRef(collection[propId]);
                    break;
                default:
                    return Logger.WarnReturn(evalVar, "Assignment into invalid property!");
            }

            return evalVar;
        }

        private static EvalVar RunSwapProp(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();
            if (evalProto is not SwapPropPrototype swapPropProto) return evalVar;

            if (swapPropProto.Prop == PropertyId.Invalid)
                return Logger.WarnReturn(evalVar, "SwapPropPrototype contains Invalid \"Prop\" Field");

            PropertyInfoTable propInfoTable = GameDatabase.PropertyInfoTable;
            PropertyInfo propInfo = propInfoTable.LookupPropertyInfo(swapPropProto.Prop.Enum);
            PropertyInfoPrototype propInfoProto = propInfo.Prototype;
            if (propInfoProto == null)
                return Logger.WarnReturn(evalVar, "No PropertyInfoPrototype");

            if (propInfoProto.AggMethod != AggregationMethod.None)
                return Logger.WarnReturn(evalVar, $"SwapPropPrototype cannot swap with a property with an AggMethod other than None. Property: {propInfoProto}");

            if (FromValue(GetEvalVarFromContext(swapPropProto.LeftContext, data, true), out PropertyCollection collectionLeft, data.Game) == false)
                return evalVar;

            if (collectionLeft == null)
                return Logger.WarnReturn(evalVar, "Invalid Left Context");

            if (FromValue(GetEvalVarFromContext(swapPropProto.RightContext, data, true), out PropertyCollection collectionRight, data.Game) == false)
                return evalVar;

            if (collectionRight == null)
                return Logger.WarnReturn(evalVar, "Invalid Right Context");

            PropertyId propId = swapPropProto.Prop;
            (collectionRight[propId], collectionLeft[propId]) = (collectionLeft[propId], collectionRight[propId]);
            evalVar.SetUndefined();
            return evalVar;
        }

        private static EvalVar RunRandomFloat(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();
            if (evalProto is not RandomFloatPrototype randomFloatProto) return evalVar;

            if (data.Game == null)
                return Logger.WarnReturn(evalVar, "The context given to a RandomFloat Eval doesn't have a valid Game to use for the random generator!");

            float randomValue = data.Game.Random.NextFloat(randomFloatProto.Min, randomFloatProto.Max);
            evalVar.SetFloat(randomValue);

            return evalVar;
        }

        private static EvalVar RunRandomInt(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();
            if (evalProto is not RandomIntPrototype randomIntProto) return evalVar;

            if (data.Game == null)
                return Logger.WarnReturn(evalVar, "The context given to a RandomInt Eval doesn't have a valid Game to use for the random generator!");

            int randomValue = data.Game.Random.Next(randomIntProto.Min, randomIntProto.Max + 1);
            evalVar.SetInt(randomValue);

            return evalVar;
        }

        private static EvalVar RunLoadEntityToContextVar(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();
            if (evalProto is not LoadEntityToContextVarPrototype loadEntityToContextVarProto) return evalVar;

            if (loadEntityToContextVarProto.EntityId == null) return evalVar;

            if (data.Game == null)
                return Logger.WarnReturn(evalVar, "The context given to a LoadEntityToContextVar Eval doesn't have a valid Game to use for the entity lookup!");

            EvalVar entityIdEvalResult = Run(loadEntityToContextVarProto.EntityId, data);
            if (entityIdEvalResult.Type != EvalReturnType.EntityId)
                return Logger.WarnReturn(evalVar, $"A LoadEntityToContextVar eval has an EntityId field Eval that did not return an EntityId (Return type=[{entityIdEvalResult.Type}])");

            Entity entity = data.Game.EntityManager.GetEntity<Entity>(entityIdEvalResult.Value.EntityId);
            data.SetVar_PropertyCollectionPtr(loadEntityToContextVarProto.Context, entity.Properties);

            evalVar.SetUndefined();
            return evalVar;
        }

        private static EvalVar RunLoadConditionCollectionToContext(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();
            if (evalProto is not LoadConditionCollectionToContextPrototype loadConditionCollectionProto) return evalVar;

            if (loadConditionCollectionProto.EntityId == null) return evalVar;

            if (data.Game == null)
                return Logger.WarnReturn(evalVar, "The context given to a LoadConditionCollectionToContext Eval doesn't have a valid Game to use for the entity lookup!");

            if (data.ContextVars[(int)loadConditionCollectionProto.Context].Var.Type != EvalReturnType.Undefined)
                Logger.Warn("Attempting to assign to a ContextVar that is currently in use! Operation will be performed but this is usually a bad idea!");

            EvalVar entityIdEvalResult = Run(loadConditionCollectionProto.EntityId, data);
            if (entityIdEvalResult.Type != EvalReturnType.EntityId)
                return Logger.WarnReturn(evalVar,
                    $"A LoadConditionCollectionToContext eval has an EntityId field Eval that did not return an EntityId (Return type=[{entityIdEvalResult.Type}])");

            WorldEntity entity = data.Game.EntityManager.GetEntity<WorldEntity>(entityIdEvalResult.Value.EntityId);
            if (entity != null)
                data.SetReadOnlyVar_ConditionCollectionPtr(loadConditionCollectionProto.Context, entity.ConditionCollection);

            evalVar.SetUndefined();
            return evalVar;
        }

        private static EvalVar RunEntityHasKeyword(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();
            if (evalProto is not EntityHasKeywordPrototype entityHasKeywordProto) return evalVar;

            if (entityHasKeywordProto.Keyword == PrototypeId.Invalid)
                return Logger.WarnReturn(evalVar, "EntityHasKeyword Eval doesn't have a valid Keyword to check!");

            EvalVar contextVar = data.ContextVars[(int)entityHasKeywordProto.Context].Var;
            if (contextVar.Type != EvalReturnType.EntityPtr)
                return Logger.WarnReturn(evalVar, "EntityHasKeyword was given a context variable that is not an EntityPtr.");

            evalVar.SetBool(false);

            if (FromValue(contextVar, out Entity entity))
                if (entity is WorldEntity worldEntity)
                {
                    if (entityHasKeywordProto.ConditionKeywordOnly)
                        evalVar.SetBool(worldEntity.HasConditionWithKeyword(entityHasKeywordProto.Keyword));
                    else
                        evalVar.SetBool(worldEntity.HasKeyword(entityHasKeywordProto.Keyword) || worldEntity.HasConditionWithKeyword(entityHasKeywordProto.Keyword));
                }

            return evalVar;
        }

        private static EvalVar RunEntityHasTalent(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();
            if (evalProto is not EntityHasTalentPrototype entityHasTalentProto) return evalVar;

            if (entityHasTalentProto.Talent == PrototypeId.Invalid)
                return Logger.WarnReturn(evalVar, "EntityHasTalent Eval doesn't have a valid Talent to check!");

            EvalVar contextVar = data.ContextVars[(int)entityHasTalentProto.Context].Var;
            if (contextVar.Type != EvalReturnType.EntityPtr)
                return Logger.WarnReturn(evalVar, "EntityHasTalent was given a context variable that is not an EntityPtr.");

            evalVar.SetBool(false);

            if (FromValue(contextVar, out Entity entity))
                if (entity is WorldEntity worldEntity)
                    evalVar.SetBool(worldEntity.HasPowerInPowerCollection(entityHasTalentProto.Talent));

            return evalVar;
        }

        private static EvalVar RunGetCombatLevel(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();
            if (evalProto is not GetCombatLevelPrototype getCombatLevelProto) return evalVar;

            EvalVar contextVar = data.ContextVars[(int)getCombatLevelProto.Context].Var;
            if (contextVar.Type != EvalReturnType.EntityPtr)
                return Logger.WarnReturn(evalVar, "GetCombatLevel was given a context variable that is not an EntityPtr.");

            evalVar.SetInt(0);

            if (FromValue(contextVar, out Entity entity))
                if (entity is Agent agent)
                    evalVar.SetInt(agent.CombatLevel);

            return evalVar;
        }

        private static EvalVar RunGetPowerRank(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();
            if (evalProto is not GetPowerRankPrototype getPowerRankProto) return evalVar;

            if (getPowerRankProto.Power == PrototypeId.Invalid)
                return Logger.WarnReturn(evalVar, "GetPowerRank Eval doesn't have a valid Power to check!");

            EvalVar contextVar = data.ContextVars[(int)getPowerRankProto.Context].Var;
            if (contextVar.Type != EvalReturnType.EntityPtr)
                return Logger.WarnReturn(evalVar, "GetPowerRank was given a context variable that is not an EntityPtr.");

            evalVar.SetInt(0);

            if (FromValue(contextVar, out Entity entity))
                if (entity is Agent agent)
                    evalVar.SetInt(agent.GetPowerRank(getPowerRankProto.Power));

            return evalVar;
        }

        private static EvalVar RunCalcPowerRank(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();
            if (evalProto is not CalcPowerRankPrototype calcPowerRankProto) return evalVar;
            
            if (calcPowerRankProto.Power == PrototypeId.Invalid)
                return Logger.WarnReturn(evalVar, "CalcPowerRank Eval doesn't have a valid Power to check!");

            EvalVar contextVar = data.ContextVars[(int)calcPowerRankProto.Context].Var;
            if (contextVar.Type != EvalReturnType.EntityPtr)
                return Logger.WarnReturn(evalVar, "CalcPowerRank was given a context variable that is not an EntityPtr.");

            evalVar.SetInt(0);

            if (FromValue(contextVar, out Entity entity))
                if (entity is Agent agent)
                {
                    EvalVar defaultContextVar = GetEvalVarFromContext(EvalContext.Default, data, false, false);
                    PropertyCollection defaultContextProps = null;
                    if (defaultContextVar.Type == EvalReturnType.PropertyCollectionPtr)
                        FromValue(defaultContextVar, out defaultContextProps, data.Game);

                    EvalVar var1ContextVar = GetEvalVarFromContext(EvalContext.Var1, data, false, false);
                    PropertyCollection var1ContextProps = null;
                    if (var1ContextVar.Type == EvalReturnType.PropertyCollectionPtr)
                        FromValue(var1ContextVar, out var1ContextProps, data.Game);

                    bool showNextRank = 
                        (defaultContextProps != null && defaultContextProps[PropertyEnum.PowerRankShowNextRank]) 
                        || (var1ContextProps != null && var1ContextProps[PropertyEnum.PowerRankShowNextRank]);

                    if (agent.GetPowerProgressionInfo(calcPowerRankProto.Power, out PowerProgressionInfo powerInfo) == false)
                        return evalVar;

                    int powerRank = agent.ComputePowerRank(ref powerInfo, agent.GetPowerSpecIndexActive(), out _);
                    if (showNextRank) powerRank++;

                    evalVar.SetInt(powerRank);
                }

            return evalVar;
        }

        private static EvalVar RunIsInParty(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();
            if (evalProto is not IsInPartyPrototype isInPartyProto) return evalVar;

            EvalVar contextVar = data.ContextVars[(int)isInPartyProto.Context].Var;
            if (contextVar.Type != EvalReturnType.EntityPtr)
                return Logger.WarnReturn(evalVar, "GetPartySize was given a context variable that is not an EntityPtr.");

            evalVar.SetBool(false);

            if (FromValue(contextVar, out Entity entity))
            {
                Player player = entity.GetSelfOrOwnerOfType<Player>();
                if (player != null)
                    evalVar.SetBool(player.IsInParty);
            }

            return evalVar;
        }

        private static EvalVar RunGetDamageReductionPct(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();
            if (evalProto is not GetDamageReductionPctPrototype getDamageReductionPctProto) return evalVar;

            EvalVar contextVar = data.ContextVars[(int)getDamageReductionPctProto.Context].Var;
            if (contextVar.Type != EvalReturnType.EntityPtr)
                return Logger.WarnReturn(evalVar, "GetDamageReductionPct was given a context variable that is not an EntityPtr.");

            evalVar.SetFloat(0f);

            if (FromValue(contextVar, out Entity entity))
                if (entity is WorldEntity worldEntity)
                {
                    float defenseRating = worldEntity.GetDefenseRating(getDamageReductionPctProto.VsDamageType);
                    evalVar.SetFloat(worldEntity.GetDamageReductionPct(defenseRating, worldEntity.Properties, null));
                }

            return evalVar;
        }

        private static EvalVar RunGetDistanceToEntity(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();
            if (evalProto is not GetDistanceToEntityPrototype getDistanceToEntityProto) return evalVar;

            EvalVar sourceVar = data.ContextVars[(int)getDistanceToEntityProto.SourceEntity].Var;
            if (sourceVar.Type != EvalReturnType.EntityPtr)
                return Logger.WarnReturn(evalVar, "GetDistanceToEntity was given a source context variable that is not an EntityPtr.");

            EvalVar targetVar = data.ContextVars[(int)getDistanceToEntityProto.TargetEntity].Var;
            if (targetVar.Type != EvalReturnType.EntityPtr)
                return Logger.WarnReturn(evalVar, "GetDistanceToEntity was given a target context variable that is not an EntityPtr.");

            evalVar.SetFloat(0f);

            if (FromValue(sourceVar, out Entity sourceEntity) && FromValue(targetVar, out Entity targetEntity))
                if (sourceEntity is WorldEntity sourceWorldEntity && targetEntity is WorldEntity targetWorldEntity)
                    evalVar.SetFloat(sourceWorldEntity.GetDistanceTo(targetWorldEntity, getDistanceToEntityProto.EdgeToEdge));

            return evalVar;
        }

        private static EvalVar RunIsDynamicCombatLevelEnabled(EvalPrototype evalProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();
            evalVar.SetBool(true);
            return evalVar;
        }

    }
}
