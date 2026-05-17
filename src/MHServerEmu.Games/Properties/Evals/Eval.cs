using MHServerEmu.Core.Collections;
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
            using var contextsHandle = HashSetPool<EvalContext>.Instance.Get(out HashSet<EvalContext> contexts);
            validContexts.Add(EvalContext.Globals);

            foreach (EvalPrototype evalProto in evals)
                GetEvalContexts(evalProto, contexts, validContexts);

            bool isValid = true;

            foreach (EvalContext context in contexts)
                isValid &= Verify.IsTrue(validContexts.Contains(context), $"Unsupported context {context} used in Eval {contextName}!"); // DataValidateFailFormatMessage
            
            return isValid;
        }

        public static bool ValidateEvalContextsForField(EvalPrototype evalProto, HashSet<EvalContext> validContexts, string contextName)
        {
            using var contextsHandle = HashSetPool<EvalContext>.Instance.Get(out HashSet<EvalContext> contexts);
            validContexts.Add(EvalContext.Globals);

            GetEvalContexts(evalProto, contexts, validContexts);

            bool isValid = true;

            foreach (EvalContext context in contexts)
                isValid &= Verify.IsTrue(validContexts.Contains(context), $"Unsupported context {context} used in Eval {contextName}!"); // DataValidateFailFormatMessage

            return isValid;
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
            if (!Verify.IsTrue(evalInfo.IsEvalProperty)) return;
            string debugString = evalInfo.PropertyName;
            GetEvalPropertyIds(evalInfo.Eval, resultInputs, GetEvalPropertyIdEnum.PropertyInfoEvalInput, debugString);
        }

        public static void GetEvalPropertyIds(EvalPrototype startEvalProto, List<PropertyId> resultIds, GetEvalPropertyIdEnum type, string debugString)
        {
            if (!Verify.IsNotNull(startEvalProto)) return;

            using var evalStackHandle = StackPool<EvalPrototype>.Instance.Get(out PoolableStack<EvalPrototype> evalStack);
            evalStack.Push(startEvalProto);

            while (evalStack.Count > 0)
            {
                EvalPrototype evalProto = evalStack.Pop();
                if (!Verify.IsNotNull(evalProto))
                    continue;

                switch (evalProto.Op)
                {
                    case EvalOp.AssignProp:
                        {
                            AssignPropPrototype typedProto = (AssignPropPrototype)evalProto;
                            if (type == GetEvalPropertyIdEnum.PropertyInfoEvalInput)
                            {
                                if (!Verify.IsTrue(typedProto.Context == EvalContext.LocalStack, "Assign property eval operators to context other than local stack not allowed in get-property-eval"))
                                    continue;
                            }
                            else if (type == GetEvalPropertyIdEnum.Output)
                            {
                                if (resultIds.Contains(typedProto.Prop) == false)
                                    resultIds.Add(typedProto.Prop);
                            }

                            evalStack.Push(typedProto.Eval);
                        }
                        break;

                    case EvalOp.AssignPropEvalParams:
                        {
                            AssignPropEvalParamsPrototype typedProto = (AssignPropEvalParamsPrototype)evalProto;
                            if (type == GetEvalPropertyIdEnum.PropertyInfoEvalInput)
                            {
                                if (!Verify.IsTrue(typedProto.Context == EvalContext.LocalStack, "Assign property eval operators to context other than local stack not allowed in get-property-eval"))
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
                        Verify.IsTrue(type != GetEvalPropertyIdEnum.PropertyInfoEvalInput, $"{evalProto.Op} eval operator not allowed in get-property-eval");
                        break;

                    case EvalOp.HasProp:
                        {
                            HasPropPrototype typedProto = (HasPropPrototype)evalProto;
                            if (type == GetEvalPropertyIdEnum.PropertyInfoEvalInput)
                            {
                                if (!Verify.IsTrue(typedProto.Context == EvalContext.Globals || typedProto.Context == EvalContext.Default || typedProto.Context == EvalContext.LocalStack,
                                    $"Eval operator found in a get-property-eval references unsupported Context type. [{debugString}]"))
                                    continue;

                                if (typedProto.Context == EvalContext.Default)
                                {
                                    if (resultIds.Contains(typedProto.Prop) == false)
                                        resultIds.Add(typedProto.Prop);
                                }
                            }
                            else if (type == GetEvalPropertyIdEnum.Input)
                            {
                                if (resultIds.Contains(typedProto.Prop) == false)
                                    resultIds.Add(typedProto.Prop);
                            }
                        }
                        break;

                    case EvalOp.LoadProp:
                        {
                            LoadPropPrototype typedProto = (LoadPropPrototype)evalProto;
                            if (type == GetEvalPropertyIdEnum.PropertyInfoEvalInput)
                            {
                                if (!Verify.IsTrue(typedProto.Context == EvalContext.Globals || typedProto.Context == EvalContext.Default || typedProto.Context == EvalContext.LocalStack,
                                    $"Eval operator found in a get-property-eval references unsupported Context type. [{debugString}]"))
                                    continue;

                                if (typedProto.Context == EvalContext.Default)
                                {
                                    if (resultIds.Contains(typedProto.Prop) == false)
                                        resultIds.Add(typedProto.Prop);
                                }
                            }
                            else if (type == GetEvalPropertyIdEnum.Input)
                            {
                                if (resultIds.Contains(typedProto.Prop) == false)
                                    resultIds.Add(typedProto.Prop);
                            }
                        }
                        break;

                    case EvalOp.LoadCurve:
                        {
                            LoadCurvePrototype typedProto = (LoadCurvePrototype)evalProto;
                            evalStack.Push(typedProto.Index);
                        }
                        break;

                    case EvalOp.LoadContextInt:
                    case EvalOp.LoadContextProtoRef:
                        Verify.IsTrue(type != GetEvalPropertyIdEnum.PropertyInfoEvalInput, $"{evalProto.Op} eval operators not allowed in get-property-eval ( but there is no reason they couldn't be added in (:  ) ");
                        break;

                    case EvalOp.Add:
                        {
                            AddPrototype typedProto = (AddPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Exponent:
                        {
                            ExponentPrototype typedProto = (ExponentPrototype)evalProto;
                            evalStack.Push(typedProto.BaseArg);
                            evalStack.Push(typedProto.ExpArg);
                        }
                        break;

                    case EvalOp.Max:
                        {
                            MaxPrototype typedProto = (MaxPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Min:
                        {
                            MinPrototype typedProto = (MinPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Modulus:
                        {
                            ModulusPrototype typedProto = (ModulusPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Sub:
                        {
                            SubPrototype typedProto = (SubPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Mult:
                        {
                            MultPrototype typedProto = (MultPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Div:
                        {
                            DivPrototype typedProto = (DivPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Scope:
                        {
                            ScopePrototype typedProto = (ScopePrototype)evalProto;
                            if (typedProto.Scope.HasValue())
                            {
                                foreach (EvalPrototype eval in typedProto.Scope)
                                    evalStack.Push(eval);
                            }
                        }
                        break;

                    case EvalOp.For:
                        {
                            ForPrototype typedProto = (ForPrototype)evalProto;
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

                                foreach (EvalPrototype eval in typedProto.ScopeLoopBody)
                                    evalStack.Push(eval);
                            }
                        }
                        break;

                    case EvalOp.ForEachConditionInContext:
                        {
                            ForEachConditionInContextPrototype typedProto = (ForEachConditionInContextPrototype)evalProto;
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

                                foreach (EvalPrototype eval in typedProto.ScopeLoopBody)
                                    evalStack.Push(eval);
                            }
                        }
                        break;

                    case EvalOp.ForEachProtoRefInContextRefList:
                        {
                            ForEachProtoRefInContextRefListPrototype typedProto = (ForEachProtoRefInContextRefListPrototype)evalProto;
                            if (typedProto.ScopeLoopBody.HasValue())
                            {
                                if (typedProto.PreLoop != null)
                                    evalStack.Push(typedProto.PreLoop);

                                if (typedProto.LoopCondition != null)
                                    evalStack.Push(typedProto.LoopCondition);

                                if (typedProto.PostLoop != null)
                                    evalStack.Push(typedProto.PostLoop);

                                foreach (EvalPrototype eval in typedProto.ScopeLoopBody)
                                    evalStack.Push(eval);
                            }
                        }
                        break;

                    case EvalOp.GreaterThan:
                        {
                            GreaterThanPrototype typedProto = (GreaterThanPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.LessThan:
                        {
                            LessThanPrototype typedProto = (LessThanPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Equals:
                        {
                            EqualsPrototype typedProto = (EqualsPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.And:
                        {
                            AndPrototype typedProto = (AndPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Or:
                        {
                            OrPrototype typedProto = (OrPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Not:
                        {
                            NotPrototype typedProto = (NotPrototype)evalProto;
                            evalStack.Push(typedProto.Arg);
                        }
                        break;

                    case EvalOp.IfElse:
                        {
                            IfElsePrototype typedProto = (IfElsePrototype)evalProto;
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
                            LoadPropContextParamsPrototype typedProto = (LoadPropContextParamsPrototype)evalProto;
                            Verify.IsTrue(type != GetEvalPropertyIdEnum.PropertyInfoEvalInput, $"GetEvalPropertyInputs() is being called for a LoadPropContextParams, which means the PropertyInfo doesn't have the 'always re-compute eval' flag set!\nProp: [{typedProto.Prop.GetName()}]");
                        }
                        break;

                    case EvalOp.LoadPropEvalParams:
                        {
                            LoadPropEvalParamsPrototype typedProto = (LoadPropEvalParamsPrototype)evalProto;
                            Verify.IsTrue(type != GetEvalPropertyIdEnum.Input, $"GetEvalPropertyInputs() is being called for a LoadPropEvalParams, which means the PropertyInfo doesn't have the 'always re-compute eval' flag set!\nProp: [{typedProto.Prop.GetName()}]");
                        }
                        break;

                    default:
                        Verify.IsTrue(false, "Invalid Operation");
                        break;
                }
            }
        }

        private static void GetEvalContexts(EvalPrototype startEvalProto, HashSet<EvalContext> resultContexts, HashSet<EvalContext> validContexts)
        {
            if (!Verify.IsNotNull(startEvalProto)) return;

            using var evalStackHandle = StackPool<EvalPrototype>.Instance.Get(out PoolableStack<EvalPrototype> evalStack);
            evalStack.Push(startEvalProto);

            while (evalStack.Count > 0)
            {
                EvalPrototype evalProto = evalStack.Pop();
                if (!Verify.IsNotNull(evalProto))
                    continue;

                switch (evalProto.Op)
                {
                    case EvalOp.AssignProp:
                        {
                            AssignPropPrototype typedProto = (AssignPropPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                            evalStack.Push(typedProto.Eval);
                        }
                        break;

                    case EvalOp.AssignPropEvalParams:
                        {
                            AssignPropEvalParamsPrototype typedProto = (AssignPropEvalParamsPrototype)evalProto;
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
                            LoadEntityToContextVarPrototype typedProto = (LoadEntityToContextVarPrototype)evalProto;
                            validContexts?.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.LoadConditionCollectionToContext:
                        {
                            LoadConditionCollectionToContextPrototype typedProto = (LoadConditionCollectionToContextPrototype)evalProto;
                            validContexts?.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.EntityHasKeyword:
                        {
                            EntityHasKeywordPrototype typedProto = (EntityHasKeywordPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.EntityHasTalent:
                        {
                            EntityHasTalentPrototype typedProto = (EntityHasTalentPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.GetCombatLevel:
                        {
                            GetCombatLevelPrototype typedProto = (GetCombatLevelPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.GetPowerRank:
                        {
                            GetPowerRankPrototype typedProto = (GetPowerRankPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.CalcPowerRank:
                        {
                            CalcPowerRankPrototype typedProto = (CalcPowerRankPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.GetDamageReductionPct:
                        {
                            GetDamageReductionPctPrototype typedProto = (GetDamageReductionPctPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.GetDistanceToEntity:
                        {
                            GetDistanceToEntityPrototype typedProto = (GetDistanceToEntityPrototype)evalProto;
                            resultContexts.Add(typedProto.SourceEntity);
                            resultContexts.Add(typedProto.TargetEntity);
                        }
                        break;

                    case EvalOp.IsInParty:
                        {
                            IsInPartyPrototype typedProto = (IsInPartyPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.HasProp:
                        {
                            HasPropPrototype typedProto = (HasPropPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.LoadProp:
                        {
                            LoadPropPrototype typedProto = (LoadPropPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.LoadCurve:
                        {
                            LoadCurvePrototype typedProto = (LoadCurvePrototype)evalProto;
                            evalStack.Push(typedProto.Index);
                        }
                        break;

                    case EvalOp.LoadContextInt:
                        {
                            LoadContextIntPrototype typedProto = (LoadContextIntPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.LoadContextProtoRef:
                        {
                            LoadContextProtoRefPrototype typedProto = (LoadContextProtoRefPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.Add:
                        {
                            AddPrototype typedProto = (AddPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Exponent:
                        {
                            ExponentPrototype typedProto = (ExponentPrototype)evalProto;
                            evalStack.Push(typedProto.BaseArg);
                            evalStack.Push(typedProto.ExpArg);
                        }
                        break;

                    case EvalOp.Max:
                        {
                            MaxPrototype typedProto = (MaxPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Min:
                        {
                            MinPrototype typedProto = (MinPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Modulus:
                        {
                            ModulusPrototype typedProto = (ModulusPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Sub:
                        {
                            SubPrototype typedProto = (SubPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Mult:
                        {
                            MultPrototype typedProto = (MultPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Div:
                        {
                            DivPrototype typedProto = (DivPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Scope:
                        {
                            ScopePrototype typedProto = (ScopePrototype)evalProto;
                            if (typedProto.Scope.HasValue())
                            {
                                foreach (EvalPrototype eval in typedProto.Scope)
                                    evalStack.Push(eval);

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
                            ForPrototype typedProto = (ForPrototype)evalProto;
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

                                foreach (EvalPrototype eval in typedProto.ScopeLoopBody)
                                    evalStack.Push(eval);

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
                            ForEachConditionInContextPrototype typedProto = (ForEachConditionInContextPrototype)evalProto;
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

                                foreach (EvalPrototype eval in typedProto.ScopeLoopBody)
                                    evalStack.Push(eval);

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
                            ForEachProtoRefInContextRefListPrototype typedProto = (ForEachProtoRefInContextRefListPrototype)evalProto;
                            if (typedProto.ScopeLoopBody.HasValue())
                            {
                                if (typedProto.PreLoop != null)
                                    evalStack.Push(typedProto.PreLoop);

                                if (typedProto.LoopCondition != null)
                                    evalStack.Push(typedProto.LoopCondition);

                                if (typedProto.PostLoop != null)
                                    evalStack.Push(typedProto.PostLoop);

                                foreach (EvalPrototype eval in typedProto.ScopeLoopBody)
                                    evalStack.Push(eval);

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
                            GreaterThanPrototype typedProto = (GreaterThanPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.LessThan:
                        {
                            LessThanPrototype typedProto = (LessThanPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Equals:
                        {
                            EqualsPrototype typedProto = (EqualsPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.And:
                        {
                            AndPrototype typedProto = (AndPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Or:
                        {
                            OrPrototype typedProto = (OrPrototype)evalProto;
                            evalStack.Push(typedProto.Arg1);
                            evalStack.Push(typedProto.Arg2);
                        }
                        break;

                    case EvalOp.Not:
                        {
                            NotPrototype typedProto = (NotPrototype)evalProto;
                            evalStack.Push(typedProto.Arg);
                        }
                        break;

                    case EvalOp.IfElse:
                        {
                            IfElsePrototype typedProto = (IfElsePrototype)evalProto;
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
                            DifficultyTierRangePrototype typedProto = (DifficultyTierRangePrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.MissionIsActive:
                        {
                            MissionIsActivePrototype typedProto = (MissionIsActivePrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.MissionIsComplete:
                        {
                            MissionIsCompletePrototype typedProto = (MissionIsCompletePrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.HasEntityInInventory:
                        {
                            HasEntityInInventoryPrototype typedProto = (HasEntityInInventoryPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.IsContextDataNull:
                        {
                            IsContextDataNullPrototype typedProto = (IsContextDataNullPrototype)evalProto;
                            resultContexts.Add(typedProto.Context);
                        }
                        break;

                    case EvalOp.LoadPropContextParams:
                        {
                            LoadPropContextParamsPrototype typedProto = (LoadPropContextParamsPrototype)evalProto;
                            resultContexts.Add(typedProto.PropertyCollectionContext);
                            resultContexts.Add(typedProto.PropertyIdContext);
                        }
                        break;

                    case EvalOp.LoadPropEvalParams:
                        {
                            LoadPropEvalParamsPrototype typedProto = (LoadPropEvalParamsPrototype)evalProto;
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
                            SwapPropPrototype typedProto = (SwapPropPrototype)evalProto;
                            resultContexts.Add(typedProto.LeftContext);
                            resultContexts.Add(typedProto.RightContext);
                        }
                        break;

                    default:
                        Verify.IsTrue(false, "Invalid Operation");
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
            return Verify.IsTrue(FromValue(evalVar, out resultVal), $"Invalid return type [{evalVar.Type}] for operator [{evalProto?.Op}]. EvalPrototype=[{evalProto?.GetType().Name}] ExpressionString=[{evalProto?.ExpressionString()}] Path=[{evalProto}]");
        }

        public static long RunLong(EvalPrototype evalProto, EvalContextData data)
        {
            Run(evalProto, data, out long retVal);
            return retVal;
        }

        public static bool Run(EvalPrototype evalProto, EvalContextData data, out long resultVal)
        {
            EvalVar evalVar = Run(evalProto, data);
            return Verify.IsTrue(FromValue(evalVar, out resultVal), $"Invalid return type [{evalVar.Type}] for operator [{evalProto?.Op}]. EvalPrototype=[{evalProto?.GetType().Name}] ExpressionString=[{evalProto?.ExpressionString()}] Path=[{evalProto}]");
        }

        public static float RunFloat(EvalPrototype evalProto, EvalContextData data)
        {
            Run(evalProto, data, out float retVal);
            return retVal;
        }

        public static bool Run(EvalPrototype evalProto, EvalContextData data, out float resultVal)
        {
            EvalVar evalVar = Run(evalProto, data);
            return Verify.IsTrue(FromValue(evalVar, out resultVal), $"Invalid return type [{evalVar.Type}] for operator [{evalProto?.Op}]. EvalPrototype=[{evalProto?.GetType().Name}] ExpressionString=[{evalProto?.ExpressionString()}] Path=[{evalProto}]");
        }

        public static bool RunBool(EvalPrototype evalProto, EvalContextData data)
        {
            Run(evalProto, data, out bool retVal);
            return retVal;
        }

        public static bool Run(EvalPrototype evalProto, EvalContextData data, out bool resultVal)
        {
            EvalVar evalVar = Run(evalProto, data);
            return Verify.IsTrue(FromValue(evalVar, out resultVal), $"Invalid return type [{evalVar.Type}] for operator [{evalProto?.Op}]. EvalPrototype=[{evalProto?.GetType().Name}] ExpressionString=[{evalProto?.ExpressionString()}] Path=[{evalProto}]");
        }

        public static PrototypeId RunPrototypeId(EvalPrototype evalProto, EvalContextData data)
        {
            Run(evalProto, data, out PrototypeId retVal);
            return retVal;
        }

        public static bool Run(EvalPrototype evalProto, EvalContextData data, out PrototypeId resultVal)
        {
            EvalVar evalVar = Run(evalProto, data);
            return Verify.IsTrue(FromValue(evalVar, out resultVal), $"Invalid return type [{evalVar.Type}] for operator [{evalProto?.Op}]. EvalPrototype=[{evalProto?.GetType().Name}] ExpressionString=[{evalProto?.ExpressionString()}] Path=[{evalProto}]");
        }

        public static AssetId RunAssetId(EvalPrototype evalProto, EvalContextData data)
        {
            Run(evalProto, data, out AssetId retVal);
            return retVal;
        }

        public static bool Run(EvalPrototype evalProto, EvalContextData data, out AssetId resultVal)
        {
            EvalVar evalVar = Run(evalProto, data);
            return Verify.IsTrue(FromValue(evalVar, out resultVal), $"Invalid return type [{evalVar.Type}] for operator [{evalProto?.Op}]. EvalPrototype=[{evalProto?.GetType().Name}] ExpressionString=[{evalProto?.ExpressionString()}] Path=[{evalProto}]");
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
                        Entity entity = game.EntityManager.GetEntityByDbGuid<Entity>(evalVar.Value.EntityGuid);
                        resultVal = entity?.Properties;
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
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsNotNull(evalProto)) return evalVar;

            switch (evalProto.Op)
            {
                case EvalOp.And: return RunAnd((AndPrototype)evalProto, data);
                case EvalOp.Equals: return RunEquals((EqualsPrototype)evalProto, data);
                case EvalOp.GreaterThan: return RunGreaterThan((GreaterThanPrototype)evalProto, data);
                case EvalOp.IsContextDataNull: return RunIsContextDataNull((IsContextDataNullPrototype)evalProto, data);
                case EvalOp.LessThan: return RunLessThan((LessThanPrototype)evalProto, data);
                case EvalOp.DifficultyTierRange: return RunDifficultyTierRange((DifficultyTierRangePrototype)evalProto, data);
                case EvalOp.MissionIsActive: return RunMissionIsActive((MissionIsActivePrototype)evalProto, data);
                case EvalOp.MissionIsComplete: return RunMissionIsComplete((MissionIsCompletePrototype)evalProto, data);
                case EvalOp.Not: return RunNot((NotPrototype)evalProto, data);
                case EvalOp.Or: return RunOr((OrPrototype)evalProto, data);
                case EvalOp.HasEntityInInventory: return RunHasEntityInInventory((HasEntityInInventoryPrototype)evalProto, data);
                case EvalOp.LoadAssetRef: return RunLoadAssetRef((LoadAssetRefPrototype)evalProto, data);
                case EvalOp.LoadBool: return RunLoadBool((LoadBoolPrototype)evalProto, data);
                case EvalOp.LoadFloat: return RunLoadFloat((LoadFloatPrototype)evalProto, data);
                case EvalOp.LoadInt: return RunLoadInt((LoadIntPrototype)evalProto, data);
                case EvalOp.LoadProtoRef: return RunLoadProtoRef((LoadProtoRefPrototype)evalProto, data);
                case EvalOp.LoadContextInt: return RunLoadContextInt((LoadContextIntPrototype)evalProto, data);
                case EvalOp.LoadContextProtoRef: return RunLoadContextProtoRef((LoadContextProtoRefPrototype)evalProto, data);
                case EvalOp.For: return RunFor((ForPrototype)evalProto, data);
                case EvalOp.ForEachConditionInContext: return RunForEachConditionInContext(evalProto, data);
                case EvalOp.ForEachProtoRefInContextRefList: return RunForEachProtoRefInContextRefList(evalProto, data);
                case EvalOp.IfElse: return RunIfElse(evalProto, data);
                case EvalOp.Scope: return RunScope(evalProto, data);
                case EvalOp.ExportError: return RunExportError(evalProto, data);
                case EvalOp.LoadCurve: return RunLoadCurve(evalProto, data);
                case EvalOp.Add: return RunAdd(evalProto, data);
                case EvalOp.Div: return RunDiv(evalProto, data);
                case EvalOp.Exponent: return RunExponent(evalProto, data);
                case EvalOp.Max: return RunMax(evalProto, data);
                case EvalOp.Min: return RunMin(evalProto, data);
                case EvalOp.Modulus: return RunModulus(evalProto, data);
                case EvalOp.Mult: return RunMult(evalProto, data);
                case EvalOp.Sub: return RunSub(evalProto, data);
                case EvalOp.AssignProp: return RunAssignProp(evalProto, data);
                case EvalOp.AssignPropEvalParams: return RunAssignPropEvalParams(evalProto, data);
                case EvalOp.HasProp: return RunHasProp(evalProto, data);
                case EvalOp.LoadProp: return RunLoadProp(evalProto, data);
                case EvalOp.LoadPropContextParams: return RunLoadPropContextParams(evalProto, data);
                case EvalOp.LoadPropEvalParams: return RunLoadPropEvalParams(evalProto, data);
                case EvalOp.SwapProp: return RunSwapProp(evalProto, data);
                case EvalOp.RandomFloat: return RunRandomFloat(evalProto, data);
                case EvalOp.RandomInt: return RunRandomInt(evalProto, data);
                case EvalOp.LoadEntityToContextVar: return RunLoadEntityToContextVar(evalProto, data);
                case EvalOp.LoadConditionCollectionToContext: return RunLoadConditionCollectionToContext(evalProto, data);
                case EvalOp.EntityHasKeyword: return RunEntityHasKeyword(evalProto, data);
                case EvalOp.EntityHasTalent: return RunEntityHasTalent(evalProto, data);
                case EvalOp.GetCombatLevel: return RunGetCombatLevel(evalProto, data);
                case EvalOp.GetPowerRank: return RunGetPowerRank(evalProto, data);
                case EvalOp.CalcPowerRank: return RunCalcPowerRank(evalProto, data);
                case EvalOp.IsInParty: return RunIsInParty(evalProto, data);
                case EvalOp.GetDamageReductionPct: return RunGetDamageReductionPct(evalProto, data);
                case EvalOp.GetDistanceToEntity: return RunGetDistanceToEntity(evalProto, data);
                case EvalOp.IsDynamicCombatLevelEnabled: return RunIsDynamicCombatLevelEnabled(evalProto, data);
                default:
                    Verify.IsTrue(false, "Invalid Operation");
                    return evalVar;
            }
        }

        private static EvalVar GetEvalVarFromContext(EvalContext context, EvalContextData data, bool writeable, bool checkNull = true)
        {
            EvalVar evalVar = new();
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
                const string GlobalErrorMessage = "Failed to get globals prototype for eval with Globals context type.";

                GlobalPropertiesPrototype globalProperties = GameDatabase.GlobalsPrototype?.Properties;
                if (!Verify.IsNotNull(globalProperties, GlobalErrorMessage))
                    return evalVar;

                if (!Verify.IsTrue(checkNull == false || globalProperties.Properties != null, GlobalErrorMessage))
                    return evalVar;

                evalVar.SetPropertyCollectionPtr(globalProperties.Properties);
                readOnly = true;
            }
            else
            {
                Verify.IsTrue(false, "Invalid Context");
                return evalVar;
            }

            if (writeable && readOnly)
            {
                evalVar.SetError();
                Verify.IsTrue(false, $"Attempting to get a writeable '{context}' from a context that has it set as read-only");
                return evalVar;
            }

            if (checkNull && evalVar.Type == EvalReturnType.PropertyCollectionPtr && evalVar.Value.Props == null)
            {
                evalVar.SetError();
                Verify.IsTrue(false, $"Attempting to get '{context}' from a context that doesn't have it set");
                return evalVar;
            }

            return evalVar;
        }

        private static EvalVar RunAnd(AndPrototype andProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsNotNull(andProto)) return evalVar;
            if (!Verify.IsTrue(andProto.Arg1 != null && andProto.Arg2 != null)) return evalVar;

            EvalVar lhs = Run(andProto.Arg1, data);
            if (!Verify.IsTrue(lhs.Type == EvalReturnType.Bool, "And: Non-Bool/Error field Arg1"))
                return evalVar;

            if (lhs.Value.Bool)
            {
                EvalVar rhs = Run(andProto.Arg2, data);
                if (!Verify.IsTrue(rhs.Type == EvalReturnType.Bool, "Equals: Non-Bool/Error field Arg2"))
                    return evalVar;

                evalVar.SetBool(rhs.Value.Bool);
            }
            else
            {
                evalVar.SetBool(false);
            }

            return evalVar;
        }

        private static EvalVar RunEquals(EqualsPrototype equalsProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            EvalVar lhs = Run(equalsProto.Arg1, data);
            if (!Verify.IsTrue(lhs.Type != EvalReturnType.Error, "Equals: Error field Arg1"))
                return evalVar;

            EvalVar rhs = Run(equalsProto.Arg2, data);
            if (!Verify.IsTrue(rhs.Type != EvalReturnType.Error, "Equals: Error field Arg2"))
                return evalVar;

            if (lhs.Type == EvalReturnType.Int && rhs.Type == EvalReturnType.Int)
                evalVar.SetBool(lhs.Value.Int == rhs.Value.Int);
            else if (lhs.Type == EvalReturnType.Int && rhs.Type == EvalReturnType.Float)
                evalVar.SetBool(Segment.EpsilonTest(lhs.Value.Int, rhs.Value.Float, equalsProto.Epsilon));
            else if (lhs.Type == EvalReturnType.Float && rhs.Type == EvalReturnType.Int)
                evalVar.SetBool(Segment.EpsilonTest(lhs.Value.Float, rhs.Value.Int, equalsProto.Epsilon));
            else if (lhs.Type == EvalReturnType.Float && rhs.Type == EvalReturnType.Float)
                evalVar.SetBool(Segment.EpsilonTest(lhs.Value.Float, rhs.Value.Float, equalsProto.Epsilon));
            else if (lhs.Type == EvalReturnType.ProtoRef && rhs.Type == EvalReturnType.ProtoRef)
                evalVar.SetBool(lhs.Value.Proto == rhs.Value.Proto);
            else if (lhs.Type == EvalReturnType.AssetRef && rhs.Type == EvalReturnType.AssetRef)
                evalVar.SetBool(lhs.Value.AssetId == rhs.Value.AssetId);
            else if (lhs.Type == EvalReturnType.Bool && rhs.Type == EvalReturnType.Bool)
                evalVar.SetBool(lhs.Value.Bool == rhs.Value.Bool);
            else
                Verify.IsTrue(false, "Error with arg types!");

            return evalVar;
        }

        private static EvalVar RunGreaterThan(GreaterThanPrototype greaterThanProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            EvalVar lhs = Run(greaterThanProto.Arg1, data);
            if (!Verify.IsTrue(lhs.IsNumeric(), "GreaterThan: Non-Numeric/Error field Arg1"))
                return evalVar;

            EvalVar rhs = Run(greaterThanProto.Arg2, data);
            if (!Verify.IsTrue(rhs.IsNumeric(), "GreaterThan: Non-Numeric/Error field Arg2"))
                return evalVar;

            if (lhs.Type == EvalReturnType.Int && rhs.Type == EvalReturnType.Int)
                evalVar.SetBool(lhs.Value.Int > rhs.Value.Int);
            else if (lhs.Type == EvalReturnType.Int && rhs.Type == EvalReturnType.Float)
                evalVar.SetBool(lhs.Value.Int > rhs.Value.Float);
            else if (lhs.Type == EvalReturnType.Float && rhs.Type == EvalReturnType.Int)
                evalVar.SetBool(lhs.Value.Float > rhs.Value.Int);
            else if (lhs.Type == EvalReturnType.Float && rhs.Type == EvalReturnType.Float)
                evalVar.SetBool(lhs.Value.Float > rhs.Value.Float);
            else
                Verify.IsTrue(false, "Error with arg types!");

            return evalVar;
        }

        private static EvalVar RunIsContextDataNull(IsContextDataNullPrototype isContextDataNullProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            EvalVar contextVar = GetEvalVarFromContext(isContextDataNullProto.Context, data, false, false);
            switch (contextVar.Type)
            {
                case EvalReturnType.PropertyCollectionPtr:
                    if (!Verify.IsTrue(FromValue(contextVar, out PropertyCollection collection, data.Game))) return evalVar;
                    evalVar.SetBool(collection == null);
                    break;

                case EvalReturnType.ProtoRefListPtr:
                    if (!Verify.IsTrue(FromValue(contextVar, out List<PrototypeId> protoRefList))) return evalVar;
                    evalVar.SetBool(protoRefList == null);
                    break;

                case EvalReturnType.ProtoRefVectorPtr:
                    if (!Verify.IsTrue(FromValue(contextVar, out PrototypeId[] protoRefVector))) return evalVar;
                    evalVar.SetBool(protoRefVector == null);
                    break;

                case EvalReturnType.EntityPtr:
                    if (!Verify.IsTrue(FromValue(contextVar, out Entity entity))) return evalVar;
                    evalVar.SetBool(entity == null);
                    break;

                case EvalReturnType.Error:
                    if (isContextDataNullProto.Context >= EvalContext.Var1 && isContextDataNullProto.Context < EvalContext.MaxVars)
                    {
                        evalVar.SetBool(true);
                        break;
                    }
                    goto default;

                default:
                    Verify.IsTrue(false, "IsContextDataNull Eval being checked on a context evalVar that is not a pointer!");
                    break;
            }

            return evalVar;
        }

        private static EvalVar RunLessThan(LessThanPrototype lessThanProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            EvalVar lhs = Run(lessThanProto.Arg1, data);
            if (!Verify.IsTrue(lhs.IsNumeric(), "LessThan: Non-Numeric/Error field Arg1"))
                return evalVar;

            EvalVar rhs = Run(lessThanProto.Arg2, data);
            if (!Verify.IsTrue(rhs.IsNumeric(), "LessThan: Non-Numeric/Error field Arg2"))
                return evalVar;

            if (lhs.Type == EvalReturnType.Int && rhs.Type == EvalReturnType.Int)
                evalVar.SetBool(lhs.Value.Int < rhs.Value.Int);
            else if (lhs.Type == EvalReturnType.Int && rhs.Type == EvalReturnType.Float)
                evalVar.SetBool(lhs.Value.Int < rhs.Value.Float);
            else if (lhs.Type == EvalReturnType.Float && rhs.Type == EvalReturnType.Int)
                evalVar.SetBool(lhs.Value.Float < rhs.Value.Int);
            else if (lhs.Type == EvalReturnType.Float && rhs.Type == EvalReturnType.Float)
                evalVar.SetBool(lhs.Value.Float < rhs.Value.Float);
            else
                Verify.IsTrue(false, "Error with arg types!");

            return evalVar;
        }

        private static EvalVar RunDifficultyTierRange(DifficultyTierRangePrototype difficultyTierRangeProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsNotNull(difficultyTierRangeProto)) return evalVar;

            PrototypeId tierRef = PrototypeId.Invalid;
            EvalVar contextVar = GetEvalVarFromContext(difficultyTierRangeProto.Context, data, false);

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
            {
                tierRef = collection.GetProperty(PropertyEnum.DifficultyTier);
            }

            bool isInRange = tierRef == PrototypeId.Invalid || DifficultyTierPrototype.InRange(tierRef, difficultyTierRangeProto.Min, difficultyTierRangeProto.Max);
            evalVar.SetBool(isInRange);
            return evalVar;
        }

        private static EvalVar RunMissionIsActive(MissionIsActivePrototype missionIsActiveProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsNotNull(missionIsActiveProto)) return evalVar;

            if (!Verify.IsTrue(FromValue(GetEvalVarFromContext(missionIsActiveProto.Context, data, false), out Entity entity))) return evalVar;

            Player player = entity as Player;
            if (player == null && entity is Avatar avatar)
                player = avatar.GetOwnerOfType<Player>();

            if (!Verify.IsNotNull(player, "Context is not a player."))
                return evalVar;

            MissionPrototype missionProto = GameDatabase.GetPrototype<MissionPrototype>(missionIsActiveProto.Mission);
            if (!Verify.IsNotNull(missionProto, "Missing Mission field."))
                return evalVar;

            if (missionProto.ApprovedForUse() == false || missionProto.IsLiveTuningEnabled() == false)
            {
                evalVar.SetBool(false);
                return evalVar;
            }

            Mission mission = MissionManager.FindMissionForPlayer(player, missionIsActiveProto.Mission);
            evalVar.SetBool(mission != null && mission.State == MissionState.Active);
            return evalVar;
        }

        private static EvalVar RunMissionIsComplete(MissionIsCompletePrototype missionIsCompleteProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsNotNull(missionIsCompleteProto)) return evalVar;

            if (!Verify.IsTrue(FromValue(GetEvalVarFromContext(missionIsCompleteProto.Context, data, false), out Entity entity))) return evalVar;

            Player player = entity?.GetSelfOrOwnerOfType<Player>();
            if (!Verify.IsNotNull(player, "Context is not a player."))
                return evalVar;

            MissionPrototype missionProto = missionIsCompleteProto.Mission.As<MissionPrototype>();
            if (!Verify.IsNotNull(missionProto)) return evalVar;

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
                if (!Verify.IsNotNull(avatar, "Mission state is per-avatar but Context is not an avatar."))
                    return evalVar;

                if (player.PrimaryAvatar != avatar)
                {
                    // client-only verify return: Mission state is per-avatar and cannot be tested for the non-current avatar on the client!
                    avatarMissionState = true;
                }
            }

            if (avatarMissionState)
            {
                evalVar.SetBool((int)avatar.Properties[PropertyEnum.AvatarMissionState, missionProto.DataRef] == (int)MissionState.Completed);
            }
            else
            {
                Mission mission = MissionManager.FindMissionForPlayer(player, missionProto.DataRef);
                evalVar.SetBool(mission != null && mission.State == MissionState.Completed);
            }

            return evalVar;
        }

        private static EvalVar RunNot(NotPrototype notProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsNotNull(notProto)) return evalVar;
            if (!Verify.IsNotNull(notProto.Arg)) return evalVar;

            EvalVar argResult = Run(notProto.Arg, data);
            if (!Verify.IsTrue(argResult.Type == EvalReturnType.Bool, "Not: Non-Bool/Error field Arg"))
                return evalVar;

            evalVar.SetBool(!argResult.Value.Bool);
            return evalVar;
        }

        private static EvalVar RunOr(OrPrototype orProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsNotNull(orProto)) return evalVar;
            if (!Verify.IsTrue(orProto.Arg1 != null && orProto.Arg2 != null)) return evalVar;

            EvalVar lhs = Run(orProto.Arg1, data);
            if (!Verify.IsTrue(lhs.Type == EvalReturnType.Bool, "Or: Non-Bool/Error field Arg1"))
                return evalVar;

            if (lhs.Value.Bool)
            {
                evalVar.SetBool(true);
            }
            else
            {
                EvalVar rhs = Run(orProto.Arg2, data);
                if (!Verify.IsTrue(rhs.Type == EvalReturnType.Bool, "Or: Non-Bool/Error field Arg2"))
                    return evalVar;

                evalVar.SetBool(rhs.Value.Bool);
            }

            return evalVar;
        }

        private static EvalVar RunHasEntityInInventory(HasEntityInInventoryPrototype hasEntityInInventoryProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsNotNull(hasEntityInInventoryProto)) return evalVar;

            if (!Verify.IsTrue(FromValue(GetEvalVarFromContext(hasEntityInInventoryProto.Context, data, false), out Entity inventoryOwner),
                "Inventory owner is not valid. Make sure you're using the var1 (or EntityPointer) context."))
                return evalVar;

            PrototypeId entityRef = hasEntityInInventoryProto.Entity;
            InventoryConvenienceLabel inventoryLabel = hasEntityInInventoryProto.Inventory;
            if (!Verify.IsTrue(inventoryLabel != InventoryConvenienceLabel.None, "The EntityInventory field in the HasEntityInInventoryPrototype is not valid."))
                return evalVar;

            BlueprintId parent = BlueprintId.Invalid;
            DataDirectory dataDir = GameDatabase.DataDirectory;
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
                    if (inventoryEntity == null)
                        continue;

                    PrototypeId inventoryEntityRef = inventoryEntity.PrototypeDataRef;
                    if (entityRef == PrototypeId.Invalid ||
                        inventoryEntityRef == entityRef ||
                        (parent != BlueprintId.Invalid && dataDir.PrototypeIsChildOfBlueprint(inventoryEntityRef, parent)))
                    {
                        inInventory = true;
                        break;
                    }
                }
            }

            evalVar.SetBool(inInventory);
            return evalVar;
        }

        private static EvalVar RunLoadAssetRef(LoadAssetRefPrototype loadAssetRefProto, EvalContextData data)
        {
            EvalVar evalVar = new();           
            evalVar.SetAssetRef(loadAssetRefProto.Value);
            return evalVar;
        }

        private static EvalVar RunLoadBool(LoadBoolPrototype loadBoolProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetBool(loadBoolProto.Value);
            return evalVar;
        }

        private static EvalVar RunLoadFloat(LoadFloatPrototype loadFloatProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetFloat(loadFloatProto.Value);
            return evalVar;
        }

        private static EvalVar RunLoadInt(LoadIntPrototype loadIntProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetInt(loadIntProto.Value);
            return evalVar;
        }

        private static EvalVar RunLoadProtoRef(LoadProtoRefPrototype loadProtoRefProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetProtoRef(loadProtoRefProto.Value);
            return evalVar;
        }

        private static EvalVar RunLoadContextInt(LoadContextIntPrototype loadContextIntProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            EvalContext context = loadContextIntProto.Context;
            if (!Verify.IsTrue(context >= 0 && context < EvalContext.MaxVars, $"LoadContextInt: Context ({context}) is out of the bounds of possible context vars ({EvalContext.MaxVars})"))
                return evalVar;

            if (!Verify.IsTrue(data.ContextVars[(int)context].Var.IsNumeric(), $"LoadContextInt: Non-Numeric value in Context Var {context}"))
                return evalVar;

            FromValue(data.ContextVars[(int)context].Var, out long resultInt);
            evalVar.SetInt(resultInt);

            return evalVar;
        }

        private static EvalVar RunLoadContextProtoRef(LoadContextProtoRefPrototype loadContextProtoRefProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            EvalContext context = loadContextProtoRefProto.Context;
            if (!Verify.IsTrue(context >= 0 && context < EvalContext.MaxVars, $"LoadContextProtoRef: Context ({context}) is out of the bounds of possible context vars ({EvalContext.MaxVars})"))
                return evalVar;

            FromValue(data.ContextVars[(int)context].Var, out PrototypeId resultProtoRef);
            evalVar.SetProtoRef(resultProtoRef);

            return evalVar;
        }

        private static EvalVar RunFor(ForPrototype forProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();

            if (!Verify.IsNotNull(forProto)) return evalVar;

            if (!Verify.IsNotNull(forProto.LoopVarInit, "No eval in For eval LoopVarInit"))
                return evalVar;

            if (!Verify.IsNotNull(forProto.LoopAdvance, "No eval in For eval LoopAdvance"))
                return evalVar;

            if (!Verify.IsNotNull(forProto.LoopCondition, "No eval in For eval LoopCondition"))
                return evalVar;

            if (!Verify.IsTrue(forProto.ScopeLoopBody.HasValue(), "No evals in For eval ScopeLoopBody"))
                return evalVar;

            PropertyCollection dataCallerStackProps = data.CallerStackProperties;
            PropertyCollection dataLocalStackProps = data.LocalStackProperties;
            data.CallerStackProperties = dataLocalStackProps;

            using PropertyCollection localStackProps = ObjectPoolManager.Instance.Get<PropertyCollection>();
            data.LocalStackProperties = localStackProps;

            if (forProto.PreLoop != null)
            {
                evalVar = Run(forProto.PreLoop, data);
                if (!Verify.IsTrue(evalVar.Type != EvalReturnType.Error)) goto Return;
            }

            evalVar = Run(forProto.LoopVarInit, data);
            if (!Verify.IsTrue(evalVar.Type != EvalReturnType.Error)) goto Return;

            evalVar = Run(forProto.LoopCondition, data);
            if (!Verify.IsTrue(evalVar.Type == EvalReturnType.Bool)) goto Return;

            while (evalVar.Value.Bool)
            {
                foreach (EvalPrototype evalProto in forProto.ScopeLoopBody)
                {
                    if (!Verify.IsNotNull(evalProto))
                        continue;

                    data.CallerStackProperties = dataLocalStackProps;
                    data.LocalStackProperties = localStackProps;

                    evalVar = Run(evalProto, data);
                    if (!Verify.IsTrue(evalVar.Type != EvalReturnType.Error)) goto Return;
                }

                data.CallerStackProperties = dataLocalStackProps;
                data.LocalStackProperties = localStackProps;

                evalVar = Run(forProto.LoopAdvance, data);
                if (!Verify.IsTrue(evalVar.Type != EvalReturnType.Error)) goto Return;

                evalVar = Run(forProto.LoopCondition, data);
                if (!Verify.IsTrue(evalVar.Type == EvalReturnType.Bool)) goto Return;
            }

            data.CallerStackProperties = dataCallerStackProps;
            data.LocalStackProperties = localStackProps;

            if (forProto.PostLoop != null)
            {
                evalVar = Run(forProto.PostLoop, data);
                if (!Verify.IsTrue(evalVar.Type != EvalReturnType.Error)) goto Return;
            }

        Return:
            data.CallerStackProperties = dataCallerStackProps;
            data.LocalStackProperties = dataLocalStackProps;
            return evalVar;
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
                    {
                        // HACK: Fix for Health = Health * 0.999f evals
                        // (not sure if this is actually happening, but leaving it here for now just in case)
                        if (intValue == 0 && propId.Enum == PropertyEnum.Health)
                        {
                            Logger.Warn("RunAssignProp(): Eval is trying to set Health to 0");
                            intValue = 1;
                        }

                        collection[propId] = intValue;
                    }
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
