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
using MHServerEmu.Games.Powers.Conditions;
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
                case EvalOp.ForEachConditionInContext: return RunForEachConditionInContext((ForEachConditionInContextPrototype)evalProto, data);
                case EvalOp.ForEachProtoRefInContextRefList: return RunForEachProtoRefInContextRefList((ForEachProtoRefInContextRefListPrototype)evalProto, data);
                case EvalOp.IfElse: return RunIfElse((IfElsePrototype)evalProto, data);
                case EvalOp.Scope: return RunScope((ScopePrototype)evalProto, data);
                case EvalOp.ExportError: return RunExportError((ExportErrorPrototype)evalProto, data);
                case EvalOp.LoadCurve: return RunLoadCurve((LoadCurvePrototype)evalProto, data);
                case EvalOp.Add: return RunAdd((AddPrototype)evalProto, data);
                case EvalOp.Div: return RunDiv((DivPrototype)evalProto, data);
                case EvalOp.Exponent: return RunExponent((ExponentPrototype)evalProto, data);
                case EvalOp.Max: return RunMax((MaxPrototype)evalProto, data);
                case EvalOp.Min: return RunMin((MinPrototype)evalProto, data);
                case EvalOp.Modulus: return RunModulus((ModulusPrototype)evalProto, data);
                case EvalOp.Mult: return RunMult((MultPrototype)evalProto, data);
                case EvalOp.Sub: return RunSub((SubPrototype)evalProto, data);
                case EvalOp.AssignProp: return RunAssignProp((AssignPropPrototype)evalProto, data);
                case EvalOp.AssignPropEvalParams: return RunAssignPropEvalParams((AssignPropEvalParamsPrototype)evalProto, data);
                case EvalOp.HasProp: return RunHasProp((HasPropPrototype)evalProto, data);
                case EvalOp.LoadProp: return RunLoadProp((LoadPropPrototype)evalProto, data);
                case EvalOp.LoadPropContextParams: return RunLoadPropContextParams((LoadPropContextParamsPrototype)evalProto, data);
                case EvalOp.LoadPropEvalParams: return RunLoadPropEvalParams((LoadPropEvalParamsPrototype)evalProto, data);
                case EvalOp.SwapProp: return RunSwapProp((SwapPropPrototype)evalProto, data);
                case EvalOp.RandomFloat: return RunRandomFloat((RandomFloatPrototype)evalProto, data);
                case EvalOp.RandomInt: return RunRandomInt((RandomIntPrototype)evalProto, data);
                case EvalOp.LoadEntityToContextVar: return RunLoadEntityToContextVar((LoadEntityToContextVarPrototype)evalProto, data);
                case EvalOp.LoadConditionCollectionToContext: return RunLoadConditionCollectionToContext((LoadConditionCollectionToContextPrototype)evalProto, data);
                case EvalOp.EntityHasKeyword: return RunEntityHasKeyword((EntityHasKeywordPrototype)evalProto, data);
                case EvalOp.EntityHasTalent: return RunEntityHasTalent((EntityHasTalentPrototype)evalProto, data);
                case EvalOp.GetCombatLevel: return RunGetCombatLevel((GetCombatLevelPrototype)evalProto, data);
                case EvalOp.GetPowerRank: return RunGetPowerRank((GetPowerRankPrototype)evalProto, data);
                case EvalOp.CalcPowerRank: return RunCalcPowerRank((CalcPowerRankPrototype)evalProto, data);
                case EvalOp.IsInParty: return RunIsInParty((IsInPartyPrototype)evalProto, data);
                case EvalOp.GetDamageReductionPct: return RunGetDamageReductionPct((GetDamageReductionPctPrototype)evalProto, data);
                case EvalOp.GetDistanceToEntity: return RunGetDistanceToEntity((GetDistanceToEntityPrototype)evalProto, data);
                case EvalOp.IsDynamicCombatLevelEnabled: return RunIsDynamicCombatLevelEnabled((IsDynamicCombatLevelEnabledPrototype)evalProto, data);
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

        private static EvalVar RunForEachConditionInContext(ForEachConditionInContextPrototype forEachProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsNotNull(forEachProto)) return evalVar;

            if (!Verify.IsTrue(forEachProto.ScopeLoopBody.HasValue(), "No evals in ForEachProtoRefInContextRefList eval ScopeLoopBody"))
                return evalVar;

            PropertyCollection dataCallerStackProps = data.CallerStackProperties;
            PropertyCollection dataLocalStackProps = data.LocalStackProperties;
            data.CallerStackProperties = dataLocalStackProps;

            using PropertyCollection localStackProps = ObjectPoolManager.Instance.Get<PropertyCollection>();
            data.LocalStackProperties = localStackProps;

            if (forEachProto.PreLoop != null)
            {
                evalVar = Run(forEachProto.PreLoop, data);
                if (!Verify.IsTrue(evalVar.Type != EvalReturnType.Error)) goto Return;
            }

            if (!Verify.IsTrue(FromValue(GetEvalVarFromContext(forEachProto.ConditionCollectionContext, data, false), out ConditionCollection conditionCollection)))
                goto Return; // NOTE: The client code just returns here without restoring stack properties, which is going to mess things up.

            if (conditionCollection != null)
            {
                foreach (Condition condition in conditionCollection)
                {
                    data.SetVar_PropertyCollectionPtr(EvalContext.Condition, condition.Properties);
                    data.SetReadOnlyVar_ProtoRefVectorPtr(EvalContext.ConditionKeywords, condition.GetKeywords());

                    if (forEachProto.LoopConditionPreScope != null)
                    {
                        evalVar = Run(forEachProto.LoopConditionPreScope, data);
                        if (!Verify.IsTrue(evalVar.Type == EvalReturnType.Bool)) goto Return;

                        if (evalVar.Value.Bool == false)
                            break;
                    }

                    foreach (EvalPrototype evalProto in forEachProto.ScopeLoopBody)
                    {
                        if (!Verify.IsNotNull(evalProto))
                            continue;

                        data.CallerStackProperties = dataLocalStackProps;
                        data.LocalStackProperties = localStackProps;

                        evalVar = Run(evalProto, data);
                        if (!Verify.IsTrue(evalVar.Type != EvalReturnType.Error)) goto Return;
                    }

                    if (forEachProto.LoopConditionPostScope != null)
                    {
                        evalVar = Run(forEachProto.LoopConditionPostScope, data);
                        if (!Verify.IsTrue(evalVar.Type == EvalReturnType.Bool)) goto Return;

                        if (evalVar.Value.Bool == false)
                            break;
                    }
                }
            }

            data.SetVar_ConditionCollectionPtr(EvalContext.Condition, null);
            data.SetVar_ProtoRefVectorPtr(EvalContext.ConditionKeywords, null);

            data.CallerStackProperties = dataLocalStackProps;
            data.LocalStackProperties = localStackProps;

            if (forEachProto.PostLoop != null)
            {
                evalVar = Run(forEachProto.PostLoop, data);
                if (!Verify.IsTrue(evalVar.Type != EvalReturnType.Error)) goto Return;
            }

        Return:
            data.CallerStackProperties = dataCallerStackProps;
            data.LocalStackProperties = dataLocalStackProps;
            return evalVar;
        }

        private static EvalVar RunForEachProtoRefInContextRefList(ForEachProtoRefInContextRefListPrototype forEachProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsNotNull(forEachProto)) return evalVar;

            if (!Verify.IsTrue(forEachProto.ScopeLoopBody.HasValue(), "No evals in ForEachProtoRefInContextRefList eval ScopeLoopBody"))
                return evalVar;

            PropertyCollection dataCallerStackProps = data.CallerStackProperties;
            PropertyCollection dataLocalStackProps = data.LocalStackProperties;
            data.CallerStackProperties = dataLocalStackProps;

            using PropertyCollection localStackProps = ObjectPoolManager.Instance.Get<PropertyCollection>();
            data.LocalStackProperties = localStackProps;

            if (forEachProto.PreLoop != null)
            {
                evalVar = Run(forEachProto.PreLoop, data);
                if (!Verify.IsTrue(evalVar.Type != EvalReturnType.Error)) goto Return;
            }

            // This can use one of two possible proto ref collection types. The client has two templated versions of
            // RunForEachProtoRefInContextRefListType that it calls in succession, while we handle it all inside one.
            if (RunForEachProtoRefInContextRefListType(forEachProto, data, ref evalVar, dataLocalStackProps, localStackProps) == false)
            {
                Verify.IsTrue(false, "A ForEachProtoRefInContextRefList prototype specified a ProtoRefListContext that is not a valid PrototypeDataRefList or PrototypeDataRefVector.");
                goto Return;
            }

            data.CallerStackProperties = dataLocalStackProps;
            data.LocalStackProperties = localStackProps;

            if (forEachProto.PostLoop != null)
            {
                evalVar = Run(forEachProto.PostLoop, data);
                if (!Verify.IsTrue(evalVar.Type != EvalReturnType.Error)) goto Return;
            }

        Return:
            data.CallerStackProperties = dataCallerStackProps;
            data.LocalStackProperties = dataLocalStackProps;
            return evalVar;
        }

        private static bool RunForEachProtoRefInContextRefListType(ForEachProtoRefInContextRefListPrototype forEachProto, EvalContextData data, ref EvalVar evalVar,
            PropertyCollection originalLocalStackProps, PropertyCollection localStackProps)
        {
            if (!Verify.IsNotNull(forEachProto)) return false;
            if (!Verify.IsNotNull(originalLocalStackProps)) return false;

            EvalVar varList = GetEvalVarFromContext(forEachProto.ProtoRefListContext, data, false);
            IReadOnlyList<PrototypeId> protoRefList = null;

            if (FromValue(varList, out List<PrototypeId> mutableProtoRefList))
            {
                protoRefList = mutableProtoRefList;
            }
            else
            {
                // This handles the vector template specialization case from the client.
                if (FromValue(varList, out PrototypeId[] protoRefVector))
                    protoRefList = protoRefVector;
                else
                    return false;
            }

            if (protoRefList != null)
            {
                for (int i = 0; i < protoRefList.Count; i++)
                {
                    PrototypeId protoRef = protoRefList[i];

                    if (forEachProto.LoopCondition != null)
                    {
                        evalVar = Run(forEachProto.LoopCondition, data);
                        if (!Verify.IsTrue(evalVar.Type == EvalReturnType.Bool)) return false;

                        if (evalVar.Value.Bool == false)
                            break;
                    }

                    localStackProps[PropertyEnum.EvalLoopVarProtoRef, 0] = protoRef;

                    foreach (EvalPrototype evalProto in forEachProto.ScopeLoopBody)
                    {
                        if (!Verify.IsNotNull(evalProto))
                            continue;

                        data.CallerStackProperties = originalLocalStackProps;
                        data.LocalStackProperties = localStackProps;

                        evalVar = Run(evalProto, data);
                        if (!Verify.IsTrue(evalVar.Type != EvalReturnType.Error)) return false;
                    }
                }
            }

            return true;
        }

        private static EvalVar RunIfElse(IfElsePrototype ifElseProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsNotNull(ifElseProto)) return evalVar;

            if (!Verify.IsNotNull(ifElseProto.Conditional, "IfElse Eval with a NULL Conditional field!"))
                return evalVar;

            if (!Verify.IsNotNull(ifElseProto.EvalIf, "IfElse Eval with a NULL EvalIf field!"))
                return evalVar;

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
                    Verify.IsTrue(false, "Non-Value/Bool Conditional.");
                    return evalVar;
            }

            if (conditionalValue)
            {
                evalVar = Run(ifElseProto.EvalIf, data);
            }
            else
            {
                if (ifElseProto.EvalElse != null)
                    evalVar = Run(ifElseProto.EvalElse, data);
                else
                    evalVar.SetUndefined();
            }

            return evalVar;
        }

        private static EvalVar RunScope(ScopePrototype scopeProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsNotNull(scopeProto)) return evalVar;

            if (!Verify.IsTrue(scopeProto.Scope.HasValue(), "No eval entries in Scope eval"))
                return evalVar;

            PropertyCollection dataCallerStackProps = data.CallerStackProperties;
            PropertyCollection dataLocalStackProps = data.LocalStackProperties;

            using PropertyCollection localStackProps = ObjectPoolManager.Instance.Get<PropertyCollection>();

            bool errors = false;
            foreach (EvalPrototype evalProto in scopeProto.Scope)
            {
                if (!Verify.IsNotNull(evalProto))
                    continue;

                data.CallerStackProperties = dataLocalStackProps;
                data.LocalStackProperties = localStackProps;

                evalVar = Run(evalProto, data);
                errors |= evalVar.Type == EvalReturnType.Error;
            }

            data.CallerStackProperties = dataCallerStackProps;
            data.LocalStackProperties = dataLocalStackProps;

            if (errors)
                evalVar.SetError();

            return evalVar;
        }

        private static EvalVar RunExportError(ExportErrorPrototype exportErrorProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();
            Verify.IsTrue(false, "Eval failed to export correctly from Calligraphy");
            return evalVar;
        }

        private static EvalVar RunLoadCurve(LoadCurvePrototype loadCurveProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsTrue(loadCurveProto.Curve != CurveId.Invalid, "LoadCurvePrototype contains Invalid \"Curve\" Field"))
                return evalVar;

            if (!Verify.IsNotNull(loadCurveProto.Index, "LoadCurvePrototype contains NULL \"Index\" Field"))
                return evalVar;

            Curve curve = GameDatabase.GetCurve(loadCurveProto.Curve);
            if (!Verify.IsNotNull(curve)) return evalVar;

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
                    Verify.IsTrue(false, $"LoadCurvePrototype contains an invalid var type for its \"Index\" Field! (Index var type=[{indexVar.Type}])");
                    return evalVar;
            }

            if (!Verify.IsTrue(curve.IndexInRange(index), $"LoadCurvePrototype index ({index}) is out of range of the curve {loadCurveProto.Curve.GetName()}, clamping to bounds and still running"))
                index = Math.Clamp(index, curve.MinPosition, curve.MaxPosition);

            evalVar.SetFloat(curve.GetAt(index));
            return evalVar;
        }

        private static EvalVar RunAdd(AddPrototype addProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            EvalVar lhs = Run(addProto.Arg1, data);
            if (!Verify.IsTrue(lhs.IsNumeric(), "Add: Non-Numeric/Error field Arg1"))
                return evalVar;

            EvalVar rhs = Run(addProto.Arg2, data);
            if (!Verify.IsTrue(rhs.IsNumeric(), "Add: Non-Numeric/Error field Arg2"))
                return evalVar;

            if (lhs.Type == EvalReturnType.Int && rhs.Type == EvalReturnType.Int)
                evalVar.SetInt(lhs.Value.Int + rhs.Value.Int);
            else if (lhs.Type == EvalReturnType.Int && rhs.Type == EvalReturnType.Float)
                evalVar.SetFloat(lhs.Value.Int + rhs.Value.Float);
            else if (lhs.Type == EvalReturnType.Float && rhs.Type == EvalReturnType.Int)
                evalVar.SetFloat(lhs.Value.Float + rhs.Value.Int);
            else if (lhs.Type == EvalReturnType.Float && rhs.Type == EvalReturnType.Float)
                evalVar.SetFloat(lhs.Value.Float + rhs.Value.Float);
            else
                Verify.IsTrue(false, "Error with arg types!");

            return evalVar;
        }

        private static EvalVar RunDiv(DivPrototype divProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            EvalVar lhs = Run(divProto.Arg1, data);
            if (!Verify.IsTrue(lhs.IsNumeric(), "Div: Non-Numeric/Error field Arg1"))
                return evalVar;

            EvalVar rhs = Run(divProto.Arg2, data);
            if (!Verify.IsTrue(rhs.IsNumeric(), "Div: Non-Numeric/Error field Arg2"))
                return evalVar;

            if (rhs.Type == EvalReturnType.Int)
            {
                if (!Verify.IsTrue(rhs.Value.Int != 0, "Div: Arg2=0 DIVZERO!"))
                    return evalVar;
            }
            else if (rhs.Type == EvalReturnType.Float)
            {
                if (!Verify.IsTrue(rhs.Value.Float != 0f, "Div: Arg2=0.0f DIVZERO!"))
                    return evalVar;
            }

            if (lhs.Type == EvalReturnType.Int && rhs.Type == EvalReturnType.Int)
                evalVar.SetFloat(lhs.Value.Int / (float)rhs.Value.Int);
            else if (lhs.Type == EvalReturnType.Int && rhs.Type == EvalReturnType.Float)
                evalVar.SetFloat(lhs.Value.Int / rhs.Value.Float);
            else if (lhs.Type == EvalReturnType.Float && rhs.Type == EvalReturnType.Int)
                evalVar.SetFloat(lhs.Value.Float / rhs.Value.Int);
            else if (lhs.Type == EvalReturnType.Float && rhs.Type == EvalReturnType.Float)
                evalVar.SetFloat(lhs.Value.Float / rhs.Value.Float);
            else
                Verify.IsTrue(false, "Error with arg types!");

            return evalVar;
        }

        private static EvalVar RunExponent(ExponentPrototype exponentProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            EvalVar baseVar = Run(exponentProto.BaseArg, data);
            if (!Verify.IsTrue(baseVar.IsNumeric(), "Exponent: Non-Numeric/Error field Base"))
                return evalVar;

            EvalVar exponentVar = Run(exponentProto.ExpArg, data);
            if (!Verify.IsTrue(exponentVar.IsNumeric(), "Exponent: Non-Numeric/Error field Exponent"))
                return evalVar;

            if (!Verify.IsTrue(FromValue(baseVar, out float baseFloat), "Exponent: Error Extracting Base from evalVar"))
                return evalVar;

            if (!Verify.IsTrue(FromValue(exponentVar, out float expFloat), "Exponent: Error Extracting Exponent from evalVar"))
                return evalVar;

            evalVar.SetFloat(MathF.Pow(baseFloat, expFloat));
            return evalVar;
        }

        private static EvalVar RunMax(MaxPrototype maxProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            EvalVar lhs = Run(maxProto.Arg1, data);
            if (!Verify.IsTrue(lhs.IsNumeric(), "Max: Non-Numeric/Error field Arg1"))
                return evalVar;

            EvalVar rhs = Run(maxProto.Arg2, data);
            if (!Verify.IsTrue(rhs.IsNumeric(), "Max: Non-Numeric/Error field Arg2"))
                return evalVar;

            if (lhs.Type == EvalReturnType.Int && rhs.Type == EvalReturnType.Int)
                evalVar.SetInt(Math.Max(lhs.Value.Int, rhs.Value.Int));
            else if (lhs.Type == EvalReturnType.Int && rhs.Type == EvalReturnType.Float)
                evalVar.SetFloat(Math.Max(lhs.Value.Int, rhs.Value.Float));
            else if (lhs.Type == EvalReturnType.Float && rhs.Type == EvalReturnType.Int)
                evalVar.SetFloat(Math.Max(lhs.Value.Float, rhs.Value.Int));
            else if (lhs.Type == EvalReturnType.Float && rhs.Type == EvalReturnType.Float)
                evalVar.SetFloat(Math.Max(lhs.Value.Float, rhs.Value.Float));
            else
                Verify.IsTrue(false, "Error with arg types!");

            return evalVar;
        }

        private static EvalVar RunMin(MinPrototype minProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            EvalVar lhs = Run(minProto.Arg1, data);
            if (!Verify.IsTrue(lhs.IsNumeric(), "Min: Non-Numeric/Error field Arg1"))
                return evalVar;

            EvalVar rhs = Run(minProto.Arg2, data);
            if (!Verify.IsTrue(rhs.IsNumeric(), "Min: Non-Numeric/Error field Arg2"))
                return evalVar;

            if (lhs.Type == EvalReturnType.Int && rhs.Type == EvalReturnType.Int)
                evalVar.SetInt(Math.Min(lhs.Value.Int, rhs.Value.Int));
            else if (lhs.Type == EvalReturnType.Int && rhs.Type == EvalReturnType.Float)
                evalVar.SetFloat(Math.Min(lhs.Value.Int, rhs.Value.Float));
            else if (lhs.Type == EvalReturnType.Float && rhs.Type == EvalReturnType.Int)
                evalVar.SetFloat(Math.Min(lhs.Value.Float, rhs.Value.Int));
            else if (lhs.Type == EvalReturnType.Float && rhs.Type == EvalReturnType.Float)
                evalVar.SetFloat(Math.Min(lhs.Value.Float, rhs.Value.Float));
            else
                Verify.IsTrue(false, "Error with arg types!");

            return evalVar;
        }

        private static EvalVar RunModulus(ModulusPrototype modulusProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            EvalVar lhs = Run(modulusProto.Arg1, data);
            if (!Verify.IsTrue(lhs.IsNumeric(), "Modulus: Non-Numeric/Error field Arg1"))
                return evalVar;

            EvalVar rhs = Run(modulusProto.Arg2, data);
            if (!Verify.IsTrue(rhs.IsNumeric(), "Modulus: Non-Numeric/Error field Arg2"))
                return evalVar;

            if (lhs.Type == EvalReturnType.Int && rhs.Type == EvalReturnType.Int)
                evalVar.SetInt(MathHelper.Modulus(lhs.Value.Int, rhs.Value.Int));
            else if (lhs.Type == EvalReturnType.Int && rhs.Type == EvalReturnType.Float)
                evalVar.SetFloat(MathHelper.FloatModulus(lhs.Value.Int, rhs.Value.Float));
            else if (lhs.Type == EvalReturnType.Float && rhs.Type == EvalReturnType.Int)
                evalVar.SetFloat(MathHelper.FloatModulus(lhs.Value.Float, rhs.Value.Int));
            else if (lhs.Type == EvalReturnType.Float && rhs.Type == EvalReturnType.Float)
                evalVar.SetFloat(MathHelper.FloatModulus(lhs.Value.Float, rhs.Value.Float));
            else
                Verify.IsTrue(false, "Error with arg types!");

            return evalVar;
        }

        private static EvalVar RunMult(MultPrototype multProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            EvalVar lhs = Run(multProto.Arg1, data);
            if (!Verify.IsTrue(lhs.IsNumeric(), "Multiply: Non-Numeric/Error field Arg1"))
                return evalVar;

            EvalVar rhs = Run(multProto.Arg2, data);
            if (!Verify.IsTrue(rhs.IsNumeric(), "Multiply: Non-Numeric/Error field Arg2"))
                return evalVar;

            if (lhs.Type == EvalReturnType.Int && rhs.Type == EvalReturnType.Int)
                evalVar.SetInt(lhs.Value.Int * rhs.Value.Int);
            else if (lhs.Type == EvalReturnType.Int && rhs.Type == EvalReturnType.Float)
                evalVar.SetFloat(lhs.Value.Int * rhs.Value.Float);
            else if (lhs.Type == EvalReturnType.Float && rhs.Type == EvalReturnType.Int)
                evalVar.SetFloat(lhs.Value.Float * rhs.Value.Int);
            else if (lhs.Type == EvalReturnType.Float && rhs.Type == EvalReturnType.Float)
                evalVar.SetFloat(lhs.Value.Float * rhs.Value.Float);
            else
                Verify.IsTrue(false, "Error with arg types!");

            return evalVar;
        }

        private static EvalVar RunSub(SubPrototype subProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            EvalVar lhs = Run(subProto.Arg1, data);
            if (!Verify.IsTrue(lhs.IsNumeric(), "Subtract: Non-Numeric/Error field Arg1"))
                return evalVar;

            EvalVar rhs = Run(subProto.Arg2, data);
            if (!Verify.IsTrue(rhs.IsNumeric(), "Subtract: Non-Numeric/Error field Arg2"))
                return evalVar;

            if (lhs.Type == EvalReturnType.Int && rhs.Type == EvalReturnType.Int)
                evalVar.SetInt(lhs.Value.Int - rhs.Value.Int);
            else if (lhs.Type == EvalReturnType.Int && rhs.Type == EvalReturnType.Float)
                evalVar.SetFloat(lhs.Value.Int - rhs.Value.Float);
            else if (lhs.Type == EvalReturnType.Float && rhs.Type == EvalReturnType.Int)
                evalVar.SetFloat(lhs.Value.Float - rhs.Value.Int);
            else if (lhs.Type == EvalReturnType.Float && rhs.Type == EvalReturnType.Float)
                evalVar.SetFloat(lhs.Value.Float - rhs.Value.Float);
            else
                Verify.IsTrue(false, "Error with arg types!");

            return evalVar;
        }

        private static EvalVar RunAssignProp(AssignPropPrototype assignPropProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsNotNull(assignPropProto)) return evalVar;

            if (!Verify.IsNotNull(assignPropProto.Eval, "AssignPropPrototype contains NULL \"Eval\" Field"))
                return evalVar;

            if (!Verify.IsTrue(assignPropProto.Prop != PropertyId.Invalid, "AssignPropPrototype contains Invalid \"Prop\" Field"))
                return evalVar;

            EvalVar assignVar = Run(assignPropProto.Eval, data);
            if (!Verify.IsTrue(assignVar.Type != EvalReturnType.Error && assignVar.Type != EvalReturnType.Undefined, "AssignPrototype has Eval that returns Error or Undefined Value"))
                return evalVar;

            if (!Verify.IsTrue(FromValue(GetEvalVarFromContext(assignPropProto.Context, data, true), out PropertyCollection collection, data.Game)))
                return evalVar;

            if (!Verify.IsNotNull(collection, "Invalid Context"))
                return evalVar;

            PropertyId propId = assignPropProto.Prop;
            PropertyEnum propEnum = propId.Enum;
            PropertyInfo propInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propEnum);
            PropertyDataType propertyType = propInfo.DataType;

            switch (propertyType)
            {
                case PropertyDataType.Integer:
                    if (Verify.IsTrue(FromValue(assignVar, out long intValue), $"Unable to convert TYPE to Int, Property: [{propInfo.PropertyName}]"))
                    {
                        // HACK: Fix for Health = Health * 0.999f evals potentially resulting in 0 assignment without going through the death codepath.
                        if (!Verify.IsTrue(propEnum != PropertyEnum.Health || intValue > 0))
                            intValue = 1;

                        collection[propId] = intValue;
                    }
                    else
                    {
                        return evalVar;
                    }
                    break;

                case PropertyDataType.Real:
                    if (Verify.IsTrue(FromValue(assignVar, out float floatValue), $"Unable to convert TYPE to Float, Property: [{propInfo.PropertyName}]"))
                        collection[propId] = floatValue;
                    else
                        return evalVar;
                    break;

                case PropertyDataType.Boolean:
                    if (Verify.IsTrue(FromValue(assignVar, out bool boolValue), $"Unable to convert TYPE to Bool, Property: [{propInfo.PropertyName}]"))
                        collection[propId] = boolValue;
                    else
                        return evalVar;
                    break;

                case PropertyDataType.EntityId:
                    if (Verify.IsTrue(FromValue(assignVar, out ulong entityIdValue), $"Unable to convert TYPE to EntityId, Property: [{propInfo.PropertyName}]"))
                        collection[propId] = entityIdValue;
                    else
                        return evalVar;
                    break;

                case PropertyDataType.RegionId:
                    if (Verify.IsTrue(FromValue(assignVar, out ulong regionIdValue), $"Unable to convert TYPE to RegionId, Property: [{propInfo.PropertyName}]"))
                        collection[propId] = regionIdValue;
                    else
                        return evalVar;
                    break;

                case PropertyDataType.Prototype:
                    if (Verify.IsTrue(FromValue(assignVar, out PrototypeId protoRefValue), $"Unable to convert TYPE to Prototype, Property: [{propInfo.PropertyName}]"))
                        collection[propId] = protoRefValue;
                    else
                        return evalVar;
                    break;

                case PropertyDataType.Asset:
                    if (Verify.IsTrue(FromValue(assignVar, out AssetId assetValue), $"Unable to convert TYPE to Asset, Property: [{propInfo.PropertyName}]"))
                        collection[propId] = assetValue;
                    else
                        return evalVar;
                    break;

                case PropertyDataType.Time:
                    if (Verify.IsTrue(FromValue(assignVar, out long timeSpanValue), $"Unable to convert TYPE to Int, Property: [{propInfo.PropertyName}]"))
                        collection[propId] = TimeSpan.FromMilliseconds(timeSpanValue);
                    else
                        return evalVar;
                    break;

                default:
                    Verify.IsTrue(false, $"Assignment into invalid property (property type is not int/float/bool)! Property: [{propInfo.PropertyName}]");
                    return evalVar;
            }

            evalVar.SetUndefined();
            return evalVar;
        }

        private static EvalVar RunAssignPropEvalParams(AssignPropEvalParamsPrototype assignPropEvalParamsProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsNotNull(assignPropEvalParamsProto)) return evalVar;

            if (!Verify.IsNotNull(assignPropEvalParamsProto.Eval, "AssignPropEvalParamsPrototype contains NULL \"Eval\" Field"))
                return evalVar;

            if (!Verify.IsTrue(assignPropEvalParamsProto.Prop != PrototypeId.Invalid, "AssignPropEvalParamsPrototype contains Invalid \"Prop\" Field"))
                return evalVar;

            EvalVar assignVar = Run(assignPropEvalParamsProto.Eval, data);
            if (!Verify.IsTrue(assignVar.Type != EvalReturnType.Error && assignVar.Type != EvalReturnType.Undefined, "AssignPropEvalParamsPrototype has Eval that returns Error or Undefined Value"))
                return evalVar;

            if (!Verify.IsTrue(FromValue(GetEvalVarFromContext(assignPropEvalParamsProto.Context, data, true), out PropertyCollection collection, data.Game)))
                return evalVar;

            if (!Verify.IsNotNull(collection, "Invalid Context"))
                return evalVar;

            PropertyInfoTable propInfoTable = GameDatabase.PropertyInfoTable;
            PropertyEnum propEnum = propInfoTable.GetPropertyEnumFromPrototype(assignPropEvalParamsProto.Prop);
            PropertyInfo propInfo = propInfoTable.LookupPropertyInfo(propEnum);

            Span<PropertyParam> paramValues = stackalloc PropertyParam[Property.MaxParamCount];
            propInfo.DefaultParamValues.CopyTo(paramValues);

            for (int i = 0; i < propInfo.ParamCount; i++)
            {
                if (!Verify.IsTrue(i < 4))
                    break;

                EvalPrototype paramEval = i switch
                {
                    0 => assignPropEvalParamsProto.Param0,
                    1 => assignPropEvalParamsProto.Param1,
                    2 => assignPropEvalParamsProto.Param2,
                    3 => assignPropEvalParamsProto.Param3,
                    _ => null
                };

                if (paramEval == null)
                    continue;

                // NOTE: We don't check the return value of FromValue() here, same as the client, so these can potentially be invalid / zero.
                switch (propInfo.GetParamType(i))
                {
                    case PropertyParamType.Asset:
                        FromValue(Run(paramEval, data), out AssetId assetParam);
                        paramValues[i] = Property.ToParam(assetParam);
                        break;

                    case PropertyParamType.Prototype:
                        FromValue(Run(paramEval, data), out PrototypeId protoRefParam);
                        paramValues[i] = Property.ToParam(propEnum, i, protoRefParam);
                        break;

                    case PropertyParamType.Integer:
                        FromValue(Run(paramEval, data), out int intParam);
                        paramValues[i] = (PropertyParam)intParam;
                        break;

                    default:
                        Verify.IsTrue(false, "Encountered an unknown prop param type in an AssignPropEvalParams Eval!");
                        return evalVar;
                }
            }

            PropertyId propId = new(propEnum, paramValues);

            switch (propInfo.DataType)
            {
                case PropertyDataType.Integer:
                    if (Verify.IsTrue(FromValue(assignVar, out long intValue), $"Unable to convert TYPE to Int, Property: {propInfo.PropertyName}"))
                        collection[propId] = intValue;
                    else
                        return evalVar;
                    break;

                case PropertyDataType.Real:
                    if (Verify.IsTrue(FromValue(assignVar, out float floatValue), $"Unable to convert TYPE to Float, Property: {propInfo.PropertyName}"))
                        collection[propId] = floatValue;
                    else
                        return evalVar;
                    break;

                case PropertyDataType.Boolean:
                    if (Verify.IsTrue(FromValue(assignVar, out bool boolValue), $"Unable to convert TYPE to Bool, Property: {propInfo.PropertyName}"))
                        collection[propId] = boolValue;
                    else
                        return evalVar;
                    break;

                case PropertyDataType.EntityId:
                    if (Verify.IsTrue(FromValue(assignVar, out ulong entityIdValue), $"Unable to convert TYPE to EntityId, Property: {propInfo.PropertyName}"))
                        collection[propId] = entityIdValue;
                    else
                        return evalVar;
                    break;

                case PropertyDataType.RegionId:
                    if (Verify.IsTrue(FromValue(assignVar, out ulong regionIdValue), $"Unable to convert TYPE to RegionId, Property: {propInfo.PropertyName}"))
                        collection[propId] = regionIdValue;
                    else
                        return evalVar;
                    break;

                case PropertyDataType.Prototype:
                    if (Verify.IsTrue(FromValue(assignVar, out PrototypeId protoRefValue), $"Unable to convert TYPE to Prototype, Property: {propInfo.PropertyName}"))
                        collection[propId] = protoRefValue;
                    else
                        return evalVar;
                    break;

                case PropertyDataType.Asset:
                    if (Verify.IsTrue(FromValue(assignVar, out AssetId assetValue), $"Unable to convert TYPE to Asset, Property: {propInfo.PropertyName}"))
                        collection[propId] = assetValue;
                    else
                        return evalVar;
                    break;

                default:
                    Verify.IsTrue(false, $"Assignment into invalid property (property type is not int/float/bool)! Property: {propInfo.PropertyName}");
                    return evalVar;
            }

            evalVar.SetUndefined();
            return evalVar;
        }

        private static EvalVar RunHasProp(HasPropPrototype hasPropProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsNotNull(hasPropProto)) return evalVar;

            if (!Verify.IsTrue(hasPropProto.Prop != PropertyId.Invalid, "HasPropPrototype contains Invalid \"Prop\" Field"))
                return evalVar;

            if (!Verify.IsTrue(FromValue(GetEvalVarFromContext(hasPropProto.Context, data, false), out PropertyCollection collection, data.Game)))
                return evalVar;

            if (!Verify.IsNotNull(collection, "Invalid Context"))
                return evalVar;

            evalVar.SetBool(collection.HasProperty(hasPropProto.Prop));
            return evalVar;
        }

        private static EvalVar RunLoadProp(LoadPropPrototype loadPropProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsNotNull(loadPropProto)) return evalVar;

            if (!Verify.IsTrue(loadPropProto.Prop != PropertyId.Invalid, "LoadPropPrototype contains Invalid \"Prop\" Field"))
                return evalVar;

            PropertyId propId = loadPropProto.Prop;
            PropertyEnum propEnum = propId.Enum;
            PropertyInfo propInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propEnum);
            PropertyDataType propertyType = propInfo.DataType;

            if (!Verify.IsTrue(FromValue(GetEvalVarFromContext(loadPropProto.Context, data, false), out PropertyCollection collection, data.Game)))
                return evalVar;

            if (!Verify.IsNotNull(collection, $"Invalid Context ({loadPropProto.Context}) when trying to load prop.\nProp: {propInfo.PropertyName}"))
                return evalVar;

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
                    Verify.IsTrue(false, "Assignment into invalid property!");
                    return evalVar;
            }

            return evalVar;
        }

        private static EvalVar RunLoadPropContextParams(LoadPropContextParamsPrototype loadPropContextParamsProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsNotNull(loadPropContextParamsProto)) return evalVar;

            if (!Verify.IsTrue(loadPropContextParamsProto.Prop != PrototypeId.Invalid, "LoadPropContextParamsPrototype contains invalid \"Prop\" field"))
                return evalVar;

            if (!Verify.IsTrue(FromValue(GetEvalVarFromContext(loadPropContextParamsProto.PropertyIdContext, data, false), out PropertyId propIdToGetParamsFrom)))
                return evalVar;

            if (!Verify.IsTrue(propIdToGetParamsFrom != PropertyId.Invalid, "LoadPropContextParams eval being run with a context that has an invalid propertyId"))
                return evalVar;

            if (!Verify.IsTrue(FromValue(GetEvalVarFromContext(loadPropContextParamsProto.PropertyCollectionContext, data, false), out PropertyCollection collection, data.Game)))
                return evalVar;

            if (!Verify.IsNotNull(collection, "Invalid Context"))
                return evalVar;

            PropertyInfoTable propInfoTable = GameDatabase.PropertyInfoTable;
            PropertyEnum propEnum = propInfoTable.GetPropertyEnumFromPrototype(loadPropContextParamsProto.Prop);
            PropertyInfo infoForPropToGetValueOf = propInfoTable.LookupPropertyInfo(propEnum);
            PropertyInfo infoForPropToGetParamsFrom = propInfoTable.LookupPropertyInfo(propIdToGetParamsFrom.Enum);

            if (!Verify.IsTrue(infoForPropToGetParamsFrom.ParamCount == infoForPropToGetValueOf.ParamCount)) return evalVar;

            Span<PropertyParam> paramValues = stackalloc PropertyParam[infoForPropToGetParamsFrom.ParamCount];
            for (int i = 0; i < infoForPropToGetParamsFrom.ParamCount; ++i)
            {
                if (!Verify.IsTrue(infoForPropToGetParamsFrom.GetParamType(i) == infoForPropToGetValueOf.GetParamType(i)))
                    return evalVar;

                switch (infoForPropToGetParamsFrom.GetParamType(i))
                {
                    case PropertyParamType.Asset:
                        Property.FromParam(propIdToGetParamsFrom.Enum, i, propIdToGetParamsFrom.GetParam(i), out AssetId assetRefParam);
                        paramValues[i] = Property.ToParam(assetRefParam);
                        break;
                    case PropertyParamType.Prototype:
                        Property.FromParam(propIdToGetParamsFrom.Enum, i, propIdToGetParamsFrom.GetParam(i), out PrototypeId protoRefParam);
                        paramValues[i] = Property.ToParam(propEnum, i, protoRefParam);
                        break;
                    case PropertyParamType.Integer:
                        int intParam = (int)propIdToGetParamsFrom.GetParam(i);
                        paramValues[i] = (PropertyParam)intParam;
                        break;
                    default:
                        Verify.IsTrue(false, "Encountered an unknown prop param type in a LoadPropContextParams Eval!");
                        return evalVar;
                }
            }

            PropertyId propIdToGetValueOf = new(propEnum, paramValues);

            switch (infoForPropToGetValueOf.DataType)
            {
                case PropertyDataType.Integer:
                    evalVar.SetInt(collection[propIdToGetValueOf]);
                    break;
                case PropertyDataType.Curve:
                case PropertyDataType.Real:
                    evalVar.SetFloat(collection[propIdToGetValueOf]);
                    break;
                case PropertyDataType.Boolean:
                    evalVar.SetBool(collection[propIdToGetValueOf]);
                    break;
                case PropertyDataType.EntityId:
                    evalVar.SetEntityId(collection[propIdToGetValueOf]);
                    break;
                case PropertyDataType.RegionId:
                    evalVar.SetRegionId(collection[propIdToGetValueOf]);
                    break;
                case PropertyDataType.Prototype:
                    evalVar.SetProtoRef(collection[propIdToGetValueOf]);
                    break;
                case PropertyDataType.Asset:
                    evalVar.SetAssetRef(collection[propIdToGetValueOf]);
                    break;
                default:
                    Verify.IsTrue(false, "Assignment into invalid property!");
                    return evalVar;
            }

            return evalVar;
        }

        private static EvalVar RunLoadPropEvalParams(LoadPropEvalParamsPrototype loadPropEvalParamsProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsNotNull(loadPropEvalParamsProto)) return evalVar;

            if (!Verify.IsTrue(loadPropEvalParamsProto.Prop != PrototypeId.Invalid, "LoadPropEvalParamsPrototype contains invalid \"Prop\" field"))
                return evalVar;

            if (!Verify.IsTrue(FromValue(GetEvalVarFromContext(loadPropEvalParamsProto.Context, data, false), out PropertyCollection collection, data.Game)))
                return evalVar;

            if (!Verify.IsNotNull(collection, "Invalid Context"))
                return evalVar;

            PropertyInfoTable propInfoTable = GameDatabase.PropertyInfoTable;
            PropertyEnum propEnum = propInfoTable.GetPropertyEnumFromPrototype(loadPropEvalParamsProto.Prop);
            PropertyInfo propInfo = propInfoTable.LookupPropertyInfo(propEnum);

            Span<PropertyParam> paramValues = stackalloc PropertyParam[Property.MaxParamCount];
            propInfo.DefaultParamValues.CopyTo(paramValues);

            for (int i = 0; i < propInfo.ParamCount; ++i)
            {
                if (!Verify.IsTrue(i < 4))
                    break;

                EvalPrototype paramEval = i switch
                {
                    0 => loadPropEvalParamsProto.Param0,
                    1 => loadPropEvalParamsProto.Param1,
                    2 => loadPropEvalParamsProto.Param2,
                    3 => loadPropEvalParamsProto.Param3,
                    _ => null
                };

                if (paramEval == null)
                    continue;

                // NOTE: We don't check the return value of FromValue() here, same as the client, so these can potentially be invalid / zero.
                switch (propInfo.GetParamType(i))
                {
                    case PropertyParamType.Asset:
                        FromValue(Run(paramEval, data), out AssetId assetRefParam);
                        paramValues[i] = Property.ToParam(assetRefParam);
                        break;

                    case PropertyParamType.Prototype:
                        FromValue(Run(paramEval, data), out PrototypeId protoRefParam);
                        paramValues[i] = Property.ToParam(propEnum, i, protoRefParam);
                        break;

                    case PropertyParamType.Integer:
                        FromValue(Run(paramEval, data), out int intParam);
                        paramValues[i] = (PropertyParam)intParam;
                        break;

                    default:
                        Verify.IsTrue(false, "Encountered an unknown prop param type in a LoadPropEvalParams Eval!");
                        return evalVar;
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
                    Verify.IsTrue(false, "Assignment into invalid property!");
                    return evalVar;
            }

            return evalVar;
        }

        private static EvalVar RunSwapProp(SwapPropPrototype swapPropProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsNotNull(swapPropProto)) return evalVar;

            if (!Verify.IsTrue(swapPropProto.Prop != PropertyId.Invalid, "SwapPropPrototype contains Invalid \"Prop\" Field"))
                return evalVar;

            PropertyInfoTable propInfoTable = GameDatabase.PropertyInfoTable;
            PropertyInfo propInfo = propInfoTable.LookupPropertyInfo(swapPropProto.Prop.Enum);
            PropertyInfoPrototype propInfoProto = propInfo.Prototype;

            if (!Verify.IsNotNull(propInfoProto, "No PropertyInfoPrototype"))
                return evalVar;

            if (!Verify.IsTrue(propInfoProto.AggMethod == AggregationMethod.None, $"SwapPropPrototype cannot swap with a property with an AggMethod other than None. Property: {propInfoProto}"))
                return evalVar;

            if (!Verify.IsTrue(FromValue(GetEvalVarFromContext(swapPropProto.LeftContext, data, true), out PropertyCollection collectionLeft, data.Game)))
                return evalVar;

            if (!Verify.IsNotNull(collectionLeft, "Invalid Context"))
                return evalVar;

            if (!Verify.IsTrue(FromValue(GetEvalVarFromContext(swapPropProto.RightContext, data, true), out PropertyCollection collectionRight, data.Game)))
                return evalVar;

            if (!Verify.IsNotNull(collectionRight, "Invalid Context"))
                return evalVar;

            PropertyId propId = swapPropProto.Prop;
            (collectionRight[propId], collectionLeft[propId]) = (collectionLeft[propId], collectionRight[propId]);

            evalVar.SetUndefined();
            return evalVar;
        }

        private static EvalVar RunRandomFloat(RandomFloatPrototype randomFloatProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsNotNull(randomFloatProto)) return evalVar;

            if (!Verify.IsNotNull(data.Game, "The context given to a RandomFloat Eval doesn't have a valid Game to use for the random generator!"))
                return evalVar;

            float randomValue = data.Game.Random.NextFloat(randomFloatProto.Min, randomFloatProto.Max);
            evalVar.SetFloat(randomValue);
            return evalVar;
        }

        private static EvalVar RunRandomInt(RandomIntPrototype randomIntProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsNotNull(randomIntProto)) return evalVar;

            if (!Verify.IsNotNull(data.Game, "The context given to a RandomInt Eval doesn't have a valid Game to use for the random generator!"))
                return evalVar;

            int randomValue = data.Game.Random.Next(randomIntProto.Min, randomIntProto.Max + 1);
            evalVar.SetInt(randomValue);
            return evalVar;
        }

        private static EvalVar RunLoadEntityToContextVar(LoadEntityToContextVarPrototype loadEntityToContextVarProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsNotNull(loadEntityToContextVarProto)) return evalVar;
            if (!Verify.IsNotNull(loadEntityToContextVarProto.EntityId)) return evalVar;

            if (!Verify.IsNotNull(data.Game, "The context given to a LoadEntityToContextVar Eval doesn't have a valid Game to use for the entity lookup!"))
                return evalVar;

            EvalVar entityIdEvalResult = Run(loadEntityToContextVarProto.EntityId, data);
            if (!Verify.IsTrue(entityIdEvalResult.Type == EvalReturnType.EntityId, $"A LoadEntityToContextVar eval has an EntityId field Eval that did not return an EntityId (Return type=[{entityIdEvalResult.Type}])"))
                return evalVar;

            Entity entity = data.Game.EntityManager.GetEntity<Entity>(entityIdEvalResult.Value.EntityId);
            data.SetVar_PropertyCollectionPtr(loadEntityToContextVarProto.Context, entity?.Properties);

            evalVar.SetUndefined();
            return evalVar;
        }

        private static EvalVar RunLoadConditionCollectionToContext(LoadConditionCollectionToContextPrototype loadConditionCollectionProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsNotNull(loadConditionCollectionProto)) return evalVar;
            if (!Verify.IsNotNull(loadConditionCollectionProto.EntityId)) return evalVar;

            if (!Verify.IsNotNull(data.Game, "The context given to a LoadConditionCollectionToContext Eval doesn't have a valid Game to use for the entity lookup!"))
                return evalVar;

            Verify.IsTrue(data.ContextVars[(int)loadConditionCollectionProto.Context].Var.Type == EvalReturnType.Undefined,
                "Attempting to assign to a ContextVar that is currently in use! Operation will be performed but this is usually a bad idea!");

            EvalVar entityIdEvalResult = Run(loadConditionCollectionProto.EntityId, data);
            if (!Verify.IsTrue(entityIdEvalResult.Type == EvalReturnType.EntityId,
                $"A LoadConditionCollectionToContext eval has an EntityId field Eval that did not return an EntityId (Return type=[{entityIdEvalResult.Type}])"))
                return evalVar;

            WorldEntity entity = data.Game.EntityManager.GetEntity<WorldEntity>(entityIdEvalResult.Value.EntityId);
            if (entity != null)
                data.SetReadOnlyVar_ConditionCollectionPtr(loadConditionCollectionProto.Context, entity.ConditionCollection);

            evalVar.SetUndefined();
            return evalVar;
        }

        private static EvalVar RunEntityHasKeyword(EntityHasKeywordPrototype entityHasKeywordProto, EvalContextData data)
        {
            // NOTE: Pre-BUE specialization powers use this instead of EntityHasTalent, which didn't exist back then.
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsNotNull(entityHasKeywordProto)) return evalVar;

            if (!Verify.IsTrue(entityHasKeywordProto.Keyword != PrototypeId.Invalid, "EntityHasKeyword Eval doesn't have a valid Keyword to check!"))
                return evalVar;

            EvalVar contextVar = data.ContextVars[(int)entityHasKeywordProto.Context].Var;
            if (!Verify.IsTrue(contextVar.Type == EvalReturnType.EntityPtr,
                "EntityHasKeyword was given a context variable that is not an EntityPtr."))
                return evalVar;

            evalVar.SetBool(false);

            if (FromValue(contextVar, out Entity entity) && entity is WorldEntity worldEntity)
            {
                if (entityHasKeywordProto.ConditionKeywordOnly)
                    evalVar.SetBool(worldEntity.HasConditionWithKeyword(entityHasKeywordProto.Keyword));
                else
                    evalVar.SetBool(worldEntity.HasKeyword(entityHasKeywordProto.Keyword) || worldEntity.HasConditionWithKeyword(entityHasKeywordProto.Keyword));
            }

            return evalVar;
        }

        private static EvalVar RunEntityHasTalent(EntityHasTalentPrototype entityHasTalentProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsNotNull(entityHasTalentProto)) return evalVar;

            if (!Verify.IsTrue(entityHasTalentProto.Talent != PrototypeId.Invalid, "EntityHasTalent Eval doesn't have a valid Talent to check!"))
                return evalVar;

            EvalVar contextVar = data.ContextVars[(int)entityHasTalentProto.Context].Var;
            if (!Verify.IsTrue(contextVar.Type == EvalReturnType.EntityPtr, "EntityHasTalent was given a context variable that is not an EntityPtr."))
                return evalVar;

            evalVar.SetBool(false);

            if (FromValue(contextVar, out Entity entity) && entity is WorldEntity worldEntity)
                evalVar.SetBool(worldEntity.HasPowerInPowerCollection(entityHasTalentProto.Talent));

            return evalVar;
        }

        private static EvalVar RunGetCombatLevel(GetCombatLevelPrototype getCombatLevelProto, EvalContextData data)
        {
            EvalVar evalVar = new ();
            evalVar.SetError();

            if (!Verify.IsNotNull(getCombatLevelProto)) return evalVar;

            EvalVar contextVar = data.ContextVars[(int)getCombatLevelProto.Context].Var;
            if (!Verify.IsTrue(contextVar.Type == EvalReturnType.EntityPtr, "GetCombatLevel was given a context variable that is not an EntityPtr."))
                return evalVar;

            evalVar.SetInt(0);

            if (FromValue(contextVar, out Entity entity) && entity is Agent agent)
                evalVar.SetInt(agent.CombatLevel);

            return evalVar;
        }

        private static EvalVar RunGetPowerRank(GetPowerRankPrototype getPowerRankProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsNotNull(getPowerRankProto)) return evalVar;

            if (!Verify.IsTrue(getPowerRankProto.Power != PrototypeId.Invalid, "GetPowerRank Eval doesn't have a valid Power to check!"))
                return evalVar;

            EvalVar contextVar = data.ContextVars[(int)getPowerRankProto.Context].Var;
            if (!Verify.IsTrue(contextVar.Type == EvalReturnType.EntityPtr, "GetPowerRank was given a context variable that is not an EntityPtr."))
                return evalVar;

            evalVar.SetInt(0);

            if (FromValue(contextVar, out Entity entity) && entity is Agent agent)
                evalVar.SetInt(agent.GetPowerRank(getPowerRankProto.Power));

            return evalVar;
        }

        private static EvalVar RunCalcPowerRank(CalcPowerRankPrototype calcPowerRankProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsNotNull(calcPowerRankProto)) return evalVar;

            if (!Verify.IsTrue(calcPowerRankProto.Power != PrototypeId.Invalid, "CalcPowerRank Eval doesn't have a valid Power to check!"))
                return evalVar;

            EvalVar contextVar = data.ContextVars[(int)calcPowerRankProto.Context].Var;
            if (!Verify.IsTrue(contextVar.Type == EvalReturnType.EntityPtr, "CalcPowerRank was given a context variable that is not an EntityPtr."))
                return evalVar;

            evalVar.SetInt(0);

            if (FromValue(contextVar, out Entity entity) && entity is Agent agent)
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

                if (!Verify.IsTrue(agent.GetPowerProgressionInfo(calcPowerRankProto.Power, out PowerProgressionInfo powerInfo))) return evalVar;

                int powerRank = agent.ComputePowerRank(ref powerInfo, agent.GetPowerSpecIndexActive(), out _);
                if (showNextRank)
                    powerRank++;

                evalVar.SetInt(powerRank);
            }

            return evalVar;
        }

        private static EvalVar RunIsInParty(IsInPartyPrototype isInPartyProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsNotNull(isInPartyProto)) return evalVar;

            EvalVar contextVar = data.ContextVars[(int)isInPartyProto.Context].Var;
            if (!Verify.IsTrue(contextVar.Type == EvalReturnType.EntityPtr, "GetPartySize was given a context variable that is not an EntityPtr."))
                return evalVar;

            evalVar.SetBool(false);

            if (FromValue(contextVar, out Entity entity))
            {
                Player player = entity.GetSelfOrOwnerOfType<Player>();
                if (player != null)
                    evalVar.SetBool(player.IsInParty);
            }

            return evalVar;
        }

        private static EvalVar RunGetDamageReductionPct(GetDamageReductionPctPrototype getDamageReductionPctProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            if (!Verify.IsNotNull(getDamageReductionPctProto)) return evalVar;

            EvalVar contextVar = data.ContextVars[(int)getDamageReductionPctProto.Context].Var;
            if (!Verify.IsTrue(contextVar.Type == EvalReturnType.EntityPtr, "GetDamageReductionPct was given a context variable that is not an EntityPtr."))
                return evalVar;

            evalVar.SetFloat(0f);

            if (FromValue(contextVar, out Entity entity) && entity is WorldEntity worldEntity)
            {
                float defenseRating = worldEntity.GetDefenseRating(getDamageReductionPctProto.VsDamageType);
                evalVar.SetFloat(worldEntity.GetDamageReductionPct(defenseRating, worldEntity.Properties, null));
            }

            return evalVar;
        }

        private static EvalVar RunGetDistanceToEntity(GetDistanceToEntityPrototype getDistanceToEntityProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            EvalVar sourceVar = data.ContextVars[(int)getDistanceToEntityProto.SourceEntity].Var;
            if (!Verify.IsTrue(sourceVar.Type == EvalReturnType.EntityPtr, "GetDistanceToEntity was given a context variable that is not an EntityPtr."))
                return evalVar;

            EvalVar targetVar = data.ContextVars[(int)getDistanceToEntityProto.TargetEntity].Var;
            if (!Verify.IsTrue(targetVar.Type == EvalReturnType.EntityPtr, "GetDistanceToEntity was given a context variable that is not an EntityPtr."))
                return evalVar;

            evalVar.SetFloat(0f);

            FromValue(sourceVar, out Entity sourceEntity);
            FromValue(targetVar, out Entity targetEntity);

            if (sourceEntity is WorldEntity sourceWorldEntity && targetEntity is WorldEntity targetWorldEntity)
                evalVar.SetFloat(sourceWorldEntity.GetDistanceTo(targetWorldEntity, getDistanceToEntityProto.EdgeToEdge));                    

            return evalVar;
        }

        private static EvalVar RunIsDynamicCombatLevelEnabled(IsDynamicCombatLevelEnabledPrototype isDynamicCombatLevelEnabledProto, EvalContextData data)
        {
            EvalVar evalVar = new();
            evalVar.SetError();

            // NOTE: Get Game from context data and check the DCL flag in AdminCommandManager for pre-BUE versions of the game.
            evalVar.SetBool(true);

            return evalVar;
        }
    }
}
