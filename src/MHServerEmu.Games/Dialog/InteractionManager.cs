using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;
using System.Diagnostics;

namespace MHServerEmu.Games.Dialog
{
    public class InteractionManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private Dictionary<PrototypeId, InteractionData> _interaсtionMap;
        private Dictionary<PrototypeId, ExtraMissionData> _missionMap;
        private List<InteractionOption> _options;

        public InteractionManager()
        {
            _interaсtionMap = new();
            _missionMap = new();
            _options = new();
        }

        public void Initialize()
        {
            foreach (var missionRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<MissionPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                MissionPrototype missionProto = GameDatabase.GetPrototype<MissionPrototype>(missionRef);
                if (missionProto == null) continue;
                GetInteractionDataFromMissionPrototype(missionProto);
            }

            HashSet<PrototypeId> contexts = HashSetPool<PrototypeId>.Instance.Get();
            foreach (var kvp in _missionMap)
            {
                var missionData = kvp.Value;
                if (missionData == null) continue;
                if (missionData.CompleteOptions.Count > 0)
                {
                    contexts.Set(missionData.Contexts);
                    if (contexts.Count > 0)
                    {
                        foreach (var completeOption in missionData.CompleteOptions)
                        {
                            if (contexts.Count == 0)
                            {
                                Logger.Warn($"Unable to link option to mission. MISSION={GameDatabase.GetFormattedPrototypeName(missionData.MissionRef)} OPTION={completeOption}");
                                continue;
                            }
                            BindOptionToMap(completeOption, contexts);
                            missionData.PlayerHUDShowObjs |= (completeOption.MissionProto.PlayerHUDShowObjs == true);
                        }
                    }
                }
            }
            HashSetPool<PrototypeId>.Instance.Return(contexts);

            foreach (var uiWidgetRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<MetaGameDataPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                if (uiWidgetRef == PrototypeId.Invalid) continue;
                GetInteractionDataFromUIWidgetPrototype(uiWidgetRef);
            }

            foreach (var metaStateRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<MetaStatePrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                if (metaStateRef == PrototypeId.Invalid) continue;
                GetInteractionDataFromMetaStatePrototype(metaStateRef);
            }

            foreach (var kvp in _interaсtionMap)
                kvp.Value?.Sort();
        }

        private void BindOptionToMap(InteractionOption option, HashSet<PrototypeId> contexts)
        {
            if (option == null) return;
            if (contexts.Count == 0)
            {
                Logger.Warn($"Interaction Manager: Empty contexts OPTION={option}");
                return;
            }
            //Logger.Debug($"{option.GetType().Name} {_bindInd++}");
            int optionCount = 0;
            foreach (PrototypeId contextRef in contexts)
            {
                if (contextRef == PrototypeId.Invalid) continue;
                if (_interaсtionMap.TryGetValue(contextRef, out InteractionData dataInMap) == false)
                {
                    dataInMap = new InteractionData();
                    _interaсtionMap[contextRef] = dataInMap;
                }

                dataInMap.AddOption(option);
                optionCount++;
            }

            if (option is BaseMissionOption missionOption)
            {
                if (missionOption.MissionProto == null) return;
                PrototypeId missionRef = missionOption.MissionProto.DataRef;

                ExtraMissionData missionData = GetMissionData(missionRef);
                if (missionData == null) return;

                missionData.Options.Add(missionOption);

                foreach (PrototypeId contextRef in contexts)
                    missionData.Contexts.Add(contextRef);

                if (missionOption is MissionConditionMissionCompleteOption completeOption)
                {
                    foreach (PrototypeId completeMissionRef in completeOption.CompleteMissionRefs)
                    {
                        if (completeMissionRef == PrototypeId.Invalid) continue;
                        ExtraMissionData completeMissionData = GetMissionData(completeMissionRef);
                        if (completeMissionData == null) continue;

                        completeMissionData.CompleteOptions.Add(completeOption);
                    }
                }
            }

            if (optionCount == 0)
            {
                Logger.Warn($"Interaction Manager: Failed to add option to any  OPTION={option}");
                return;
            }
        }

        public ExtraMissionData GetMissionData(PrototypeId missionRef)
        {
            ExtraMissionData missionData = null;
            if (missionRef != PrototypeId.Invalid && _missionMap.TryGetValue(missionRef, out missionData) == false)
            {
                missionData = new (missionRef);
                _missionMap[missionRef] = missionData;
            }
            return missionData;
        }

        private void GetInteractionDataFromMetaStatePrototype(PrototypeId metaStateRef)
        {
            MetaStatePrototype metaStateProto = GameDatabase.GetPrototype<MetaStatePrototype>(metaStateRef);
            if (metaStateProto == null) return;

            if (metaStateProto is MetaStateTimedBonusPrototype timedBonusProto && timedBonusProto.Entries.HasValue())
            {
                foreach (var entryProto in timedBonusProto.Entries)
                {
                    if (entryProto == null || entryProto.MissionsToWatch.IsNullOrEmpty()) continue;

                    foreach (var missionRef in entryProto.MissionsToWatch)
                    {
                        var missionProto = GameDatabase.GetPrototype<MissionPrototype>(missionRef);
                        if (missionProto == null) continue;

                        if (entryProto.ActionsOnSuccess.HasValue())
                            RegisterActionInfoFromList(missionProto, entryProto.ActionsOnSuccess, MissionStateFlags.Completed, -1, 0, MissionOptionTypeFlags.None);

                        if (entryProto.ActionsOnFail.HasValue())
                            RegisterActionInfoFromList(missionProto, entryProto.ActionsOnFail, MissionStateFlags.Failed, -1, 0, MissionOptionTypeFlags.None);
                    }
                }
            }
        }

        private void GetInteractionDataFromUIWidgetPrototype(PrototypeId uiWidgetRef)
        {
            MetaGameDataPrototype metaGameDataProto = GameDatabase.GetPrototype<MetaGameDataPrototype>(uiWidgetRef);
            if (metaGameDataProto == null) return;
            
            if (metaGameDataProto is UIWidgetEntityIconsPrototype uiWidgetEntityIconsProto && uiWidgetEntityIconsProto.Entities.HasValue())
            {
                HashSet<PrototypeId> contextRefs = HashSetPool<PrototypeId>.Instance.Get();
                foreach (var entryP in uiWidgetEntityIconsProto.Entities)
                {
                    if (entryP == null) continue;
                    entryP.Filter?.GetEntityDataRefs(contextRefs);
                }

                if (contextRefs.Count > 0)
                {
                    var option = CreateOption<UIWidgetOption>();
                    if (option == null)
                    {
                        Logger.Warn($"Failed to create UIWidgetOption for prototype! METAGAMEDATA={metaGameDataProto}"); 
                        HashSetPool<PrototypeId>.Instance.Return(contextRefs);
                        return;
                    }
                    option.UIWidgetRef = uiWidgetRef;
                    option.Proto = uiWidgetEntityIconsProto;                        
                    BindOptionToMap(option, contextRefs);                    
                }
                HashSetPool<PrototypeId>.Instance.Return(contextRefs);
            }
        }

        private void GetInteractionDataFromMissionPrototype(MissionPrototype missionProto)
        {
            if (missionProto == null || missionProto.ApprovedForUse() == false) return;

            const sbyte InvalidIndex = -1;
            InteractionOptimizationFlags missionFlag = InteractionOptimizationFlags.None;
            if (missionProto.PlayerHUDShowObjs && (missionProto.PlayerHUDShowObjsOnMap || missionProto.PlayerHUDShowObjsOnScreenEdge))
                missionFlag |= InteractionOptimizationFlags.Hint;

            if (missionProto.OnAvailableActions.HasValue())
                RegisterActionInfoFromList(missionProto, missionProto.OnAvailableActions, MissionStateFlags.Available, InvalidIndex, 0, MissionOptionTypeFlags.None);

            if (missionProto.OnStartActions.HasValue())
                RegisterActionInfoFromList(missionProto, missionProto.OnStartActions, MissionStateFlags.Active | MissionStateFlags.OnStart, InvalidIndex, 0, MissionOptionTypeFlags.None);

            if (missionProto.OnFailActions.HasValue())
                RegisterActionInfoFromList(missionProto, missionProto.OnFailActions, MissionStateFlags.Failed, InvalidIndex, 0, MissionOptionTypeFlags.None);

            if (missionProto.OnSuccessActions.HasValue())
                RegisterActionInfoFromList(missionProto, missionProto.OnSuccessActions, MissionStateFlags.Completed, InvalidIndex, 0, MissionOptionTypeFlags.None);

            if (missionProto.PrereqConditions != null)
                RegisterConditionInfoFromList(missionProto, missionProto.PrereqConditions, MissionStateFlags.Inactive, InvalidIndex, 0, MissionOptionTypeFlags.SkipComplete, missionFlag);

            if (missionProto.ActivateNowConditions != null)
            {
                MissionStateFlags state = MissionStateFlags.Inactive | MissionStateFlags.Available;
                if (missionProto.Repeatable)
                    state |= MissionStateFlags.Completed | MissionStateFlags.Failed;
                RegisterConditionInfoFromList(missionProto, missionProto.ActivateNowConditions, state, InvalidIndex, 0, MissionOptionTypeFlags.Skip, missionFlag);
            }

            if (missionProto.ActivateConditions != null)
            {
                MissionStateFlags state = MissionStateFlags.Available;
                if (missionProto.Repeatable)
                    state |= MissionStateFlags.Completed | MissionStateFlags.Failed;
                RegisterConditionInfoFromList(missionProto, missionProto.ActivateConditions, state, InvalidIndex, 0, MissionOptionTypeFlags.ActivateCondition | MissionOptionTypeFlags.SkipComplete, missionFlag);
            }

            if (missionProto.FailureConditions != null)
                RegisterConditionInfoFromList(missionProto, missionProto.FailureConditions, MissionStateFlags.Active, InvalidIndex, 0, MissionOptionTypeFlags.Skip, missionFlag);

            if (missionProto.DialogText.HasValue())
                RegisterDialogTextFromList(missionProto, missionProto.DialogText, MissionStateFlags.Active, InvalidIndex, 0);

            if (missionProto.DialogTextWhenCompleted.HasValue())
                RegisterDialogTextFromList(missionProto, missionProto.DialogTextWhenCompleted, MissionStateFlags.Completed, InvalidIndex, 0);

            if (missionProto.DialogTextWhenFailed.HasValue())
                RegisterDialogTextFromList(missionProto, missionProto.DialogTextWhenFailed, MissionStateFlags.Failed, InvalidIndex, 0);

            if (missionProto.Objectives.HasValue())
            {
                for (sbyte objectiveIndex = 0; objectiveIndex < missionProto.Objectives.Length; ++objectiveIndex)
                {
                    var objectivePrototype = missionProto.Objectives[objectiveIndex];
                    if (objectivePrototype == null) continue;

                    InteractionOptimizationFlags objectiveFlag = 0;
                    if (missionFlag.HasFlag(InteractionOptimizationFlags.Hint) && (objectivePrototype.PlayerHUDShowObjsOnMap || objectivePrototype.PlayerHUDShowObjsOnScreenEdge))
                        objectiveFlag |= InteractionOptimizationFlags.Hint;

                    if (objectivePrototype.ObjectiveHints.HasValue())
                    {
                        HashSet<PrototypeId> contextRefs = HashSetPool<PrototypeId>.Instance.Get();                        
                        foreach (var hintProto in objectivePrototype.ObjectiveHints)
                        {
                            if (hintProto == null) continue;
                            contextRefs.Clear();
                            hintProto.GetPrototypeContextRefs(contextRefs);
                            if (contextRefs.Count > 0)
                            {
                                var option = CreateOption<MissionHintOption>();
                                if (option == null)
                                {
                                    Logger.Error($"Failed to create MissionObjectiveHintOption! MISSION={missionProto}");
                                    HashSetPool<PrototypeId>.Instance.Return(contextRefs);
                                    return;
                                }
                                option.Proto = hintProto;
                                option.InitializeForMission(missionProto, MissionStateFlags.Active, objectiveIndex, MissionObjectiveStateFlags.Active, MissionOptionTypeFlags.None);
                                BindOptionToMap(option, contextRefs);
                            }
                        }
                        HashSetPool<PrototypeId>.Instance.Return(contextRefs);
                    }

                    if (objectivePrototype.DialogText.HasValue())
                        RegisterDialogTextFromList(missionProto, objectivePrototype.DialogText, MissionStateFlags.Active, objectiveIndex, MissionObjectiveStateFlags.Active);

                    if (objectivePrototype.DialogTextWhenCompleted.HasValue())
                        RegisterDialogTextFromList(missionProto, objectivePrototype.DialogTextWhenCompleted, MissionStateFlags.Active, objectiveIndex, MissionObjectiveStateFlags.Completed);

                    if (objectivePrototype.DialogTextWhenFailed.HasValue())
                        RegisterDialogTextFromList(missionProto, objectivePrototype.DialogTextWhenFailed, MissionStateFlags.Active, objectiveIndex, MissionObjectiveStateFlags.Failed);

                    if (objectivePrototype.OnAvailableActions.HasValue())
                        RegisterActionInfoFromList(missionProto, objectivePrototype.OnAvailableActions, MissionStateFlags.Active, objectiveIndex, MissionObjectiveStateFlags.Available, MissionOptionTypeFlags.None);

                    if (objectivePrototype.OnStartActions.HasValue())
                        RegisterActionInfoFromList(missionProto, objectivePrototype.OnStartActions, MissionStateFlags.Active | MissionStateFlags.OnStart, objectiveIndex, MissionObjectiveStateFlags.Active, MissionOptionTypeFlags.None);

                    if (objectivePrototype.OnFailActions.HasValue())
                        RegisterActionInfoFromList(missionProto, objectivePrototype.OnFailActions, MissionStateFlags.Active, objectiveIndex, MissionObjectiveStateFlags.Failed, MissionOptionTypeFlags.None);

                    if (objectivePrototype.OnSuccessActions.HasValue())
                        RegisterActionInfoFromList(missionProto, objectivePrototype.OnSuccessActions, MissionStateFlags.Active, objectiveIndex, MissionObjectiveStateFlags.Completed, MissionOptionTypeFlags.None);

                    if (objectivePrototype.ActivateConditions != null)
                        RegisterConditionInfoFromList(missionProto, objectivePrototype.ActivateConditions, MissionStateFlags.Active, objectiveIndex, MissionObjectiveStateFlags.Available, MissionOptionTypeFlags.Skip, objectiveFlag);

                    if (objectivePrototype.SuccessConditions != null)
                        RegisterConditionInfoFromList(missionProto, objectivePrototype.SuccessConditions, MissionStateFlags.Active, objectiveIndex, MissionObjectiveStateFlags.Active, MissionOptionTypeFlags.None, objectiveFlag);

                    if (objectivePrototype.FailureConditions != null)
                        RegisterConditionInfoFromList(missionProto, objectivePrototype.FailureConditions, MissionStateFlags.Active, objectiveIndex, MissionObjectiveStateFlags.Active, MissionOptionTypeFlags.Skip, objectiveFlag);

                    if (objectivePrototype.InteractionsWhenActive.HasValue())
                        RegisterInteractionsFromList(missionProto, objectivePrototype.InteractionsWhenActive, MissionStateFlags.Active, objectiveIndex, MissionObjectiveStateFlags.Active);

                    if (objectivePrototype.InteractionsWhenComplete.HasValue())
                        RegisterInteractionsFromList(missionProto, objectivePrototype.InteractionsWhenComplete, MissionStateFlags.Active, objectiveIndex, MissionObjectiveStateFlags.Completed);

                    if (objectivePrototype.InteractionsWhenFailed.HasValue())
                        RegisterInteractionsFromList(missionProto, objectivePrototype.InteractionsWhenFailed, MissionStateFlags.Active, objectiveIndex, MissionObjectiveStateFlags.Failed);
                }

                if (missionProto.InteractionsWhenActive.HasValue())
                    RegisterInteractionsFromList(missionProto, missionProto.InteractionsWhenActive, MissionStateFlags.Active, InvalidIndex, 0);

                if (missionProto.InteractionsWhenComplete.HasValue())
                    RegisterInteractionsFromList(missionProto, missionProto.InteractionsWhenComplete, MissionStateFlags.Completed, InvalidIndex, 0);

                if (missionProto.InteractionsWhenFailed.HasValue())
                    RegisterInteractionsFromList(missionProto, missionProto.InteractionsWhenFailed, MissionStateFlags.Failed, InvalidIndex, 0);
            }
        }

        private T CreateOption<T>() where T: InteractionOption, new ()
        {
            T option = new();
            _options.Add(option); // CreateOptionInList(_options)
            return option;
        }

        private static T CreateOptionInList<T>(List<InteractionOption> optionsList) where T : InteractionOption, new()
        {
            T option = new(); // InteractionOptions.AllocateOption<T>()
            if (option != null) optionsList.Add(option);
            return option;
        }

        private void RegisterInteractionsFromList(MissionPrototype missionProto, InteractionSpecPrototype[] interactionSpec, 
            MissionStateFlags state, sbyte objectiveIndex, MissionObjectiveStateFlags objectiveState)
        {
            if (interactionSpec.IsNullOrEmpty()) return;

            HashSet<PrototypeId> contextRefs = HashSetPool<PrototypeId>.Instance.Get();
            foreach (var specProto in interactionSpec)
            {
                if (specProto == null) continue;
                contextRefs.Clear();
                specProto.GetPrototypeContextRefs(contextRefs);

                if (contextRefs.Count > 0)
                {
                    BaseMissionOption option = null;
                    if (specProto is EntityBaseSpecPrototype entitySpecProto)
                    {
                        if (entitySpecProto is EntityAppearanceSpecPrototype entityAppearanceProto)
                        {
                            var appearance = CreateOption<MissionAppearanceOption>();
                            if (appearance == null) continue;
                            appearance.Proto = entityAppearanceProto;
                            option = appearance;
                        }
                        else if (entitySpecProto is EntityVisibilitySpecPrototype entityVisibilitySpecProto)
                        {
                            var visibility = CreateOption<MissionVisibilityOption>();
                            if (visibility == null) continue;                           
                            visibility.Proto = entityVisibilitySpecProto;
                            option = visibility;                            
                        }
                        if (option == null) continue;
                        option.EntityFilterWrapper.AddEntityFilter(entitySpecProto.EntityFilter);
                    }
                    else if (specProto is ConnectionTargetEnableSpecPrototype connectionTargetEnableSpecProto)
                    {
                        var connectionTargetEnable = CreateOption<MissionConnectionTargetEnableOption>();
                        if (connectionTargetEnable == null) continue;                        
                        connectionTargetEnable.Proto = connectionTargetEnableSpecProto;
                        option = connectionTargetEnable;                        
                    }

                    if (option == null) continue;                    
                    option.InitializeForMission(missionProto, state, objectiveIndex, objectiveState, MissionOptionTypeFlags.None);
                    BindOptionToMap(option, contextRefs);                    
                }
            }
            HashSetPool<PrototypeId>.Instance.Return(contextRefs);
        }

        private void RegisterDialogTextFromList(MissionPrototype missionProto, MissionDialogTextPrototype[] dialogTexts, 
            MissionStateFlags state, sbyte objectiveIndex, MissionObjectiveStateFlags objectiveState)
        {
            if (dialogTexts.IsNullOrEmpty()) return;

            HashSet<PrototypeId> contextRefs = HashSetPool<PrototypeId>.Instance.Get();
            foreach (var missionDialogTextProto in dialogTexts)
            {
                if (missionDialogTextProto == null) continue;
                contextRefs.Clear();
                missionDialogTextProto.GetPrototypeContextRefs(contextRefs);

                if (contextRefs.Count > 0)
                {
                    var option = CreateOption<MissionDialogOption>();
                    if (option == null) break;
                    option.EntityFilterWrapper.AddEntityFilter(missionDialogTextProto.EntityFilter);
                    option.InitializeForMission(missionProto, state, objectiveIndex, objectiveState, MissionOptionTypeFlags.None);
                    option.Proto = missionDialogTextProto;
                    BindOptionToMap(option, contextRefs);                    
                }
            }
            HashSetPool<PrototypeId>.Instance.Return(contextRefs);
        }

        private void RegisterConditionInfoFromList(MissionPrototype missionProto, MissionConditionListPrototype conditionList, 
            MissionStateFlags state, sbyte objectiveIndex, MissionObjectiveStateFlags objectiveState, 
            MissionOptionTypeFlags optionType, InteractionOptimizationFlags optimizationFlag)
        {
            if (conditionList == null) return;
            HashSet<PrototypeId> contextRefs = HashSetPool<PrototypeId>.Instance.Get();
            foreach (MissionConditionPrototype prototype in conditionList.IteratePrototypes())
            {
                if (prototype == null) continue;
                if (optionType.HasFlag(MissionOptionTypeFlags.SkipComplete) && prototype is MissionConditionMissionCompletePrototype)
                    continue;

                contextRefs.Clear();
                prototype.GetPrototypeContextRefs(contextRefs);
                if (contextRefs.Count > 0)
                {
                    BaseMissionConditionOption option;
                    if (prototype is MissionConditionHotspotContainsPrototype || prototype is MissionConditionHotspotEnterPrototype || prototype is MissionConditionHotspotLeavePrototype)
                        option = CreateOption<MissionConditionHotspotOption>();
                    else if (prototype is MissionConditionCellEnterPrototype || prototype is MissionConditionAreaEnterPrototype || prototype is MissionConditionRegionEnterPrototype || prototype is MissionConditionRegionBeginTravelToPrototype)
                        option = CreateOption<MissionConditionRegionOption>();
                    else if (prototype is MissionConditionEntityInteractPrototype)
                        option = CreateOption<MissionConditionEntityInteractOption>();
                    else if (prototype is MissionConditionMissionCompletePrototype)
                        option = CreateOption<MissionConditionMissionCompleteOption>();
                    else
                        option = CreateOption<BaseMissionConditionOption>();
                    option.Proto = prototype;

                    if (missionProto is OpenMissionPrototype openMissionProto)
                        option.EntityFilterWrapper.AddRegionPtrs(openMissionProto.ActiveInRegions);

                    prototype.BuildEntityFilter(option.EntityFilterWrapper, missionProto.DataRef);
                    prototype.SetInterestLocations(option.InterestRegions, option.InterestAreas, option.InterestCells);

                    option.OptimizationFlags |= optimizationFlag;
                    option.InitializeForMission(missionProto, state, objectiveIndex, objectiveState, optionType);
                    BindOptionToMap(option, contextRefs);
                }
            }
            HashSetPool<PrototypeId>.Instance.Return(contextRefs);
        }

        private void RegisterActionInfoFromList(MissionPrototype missionProto, MissionActionPrototype[] actionList, MissionStateFlags state, 
            sbyte objectiveIndex, MissionObjectiveStateFlags objectiveState, MissionOptionTypeFlags optionType)
        {
            if (actionList.IsNullOrEmpty()) return;

            HashSet<PrototypeId> contextRefs = HashSetPool<PrototypeId>.Instance.Get();
            foreach (var prototype in actionList)
            {
                if (prototype == null) continue;
                if (prototype is MissionActionEntityTargetPrototype actionEntityTargetProto)
                {
                    contextRefs.Clear();
                    actionEntityTargetProto.GetPrototypeContextRefs(contextRefs);
                    if (contextRefs.Count > 0)
                    {
                        var option = CreateOption<MissionActionEntityTargetOption>();
                        if (option == null) break;
                        option.InitializeForMission(missionProto, state, objectiveIndex, objectiveState, optionType);
                        option.Proto = actionEntityTargetProto;
                        BindOptionToMap(option, contextRefs);
                    }
                }
                else if (prototype is MissionActionTimedActionPrototype timedActionProto && timedActionProto.ActionsToPerform.HasValue())
                {
                    RegisterActionInfoFromList(missionProto, timedActionProto.ActionsToPerform, state, objectiveIndex, objectiveState, optionType);
                }
            }
            HashSetPool<PrototypeId>.Instance.Return(contextRefs);
        }

        public bool GetEntityContextInvolvement(WorldEntity entity, EntityTrackingContextMap map)
        {
            if (entity == null) return false;

            map.Clear();

            var worldEntityProto = entity.WorldEntityPrototype;
            HashSet<InteractionOption> checkList = HashSetPool<InteractionOption>.Instance.Get();

            if (entity is Transition transition)
            {
                for (int i = 0; i < transition.Destinations.Count; i++)
                {
                    TransitionDestination destination = transition.Destinations[i];

                    var regionRef = destination.RegionRef;
                    if (regionRef != PrototypeId.Invalid)
                    {
                        map.Insert(regionRef, EntityTrackingFlag.TransitionRegion);
                        if (_interaсtionMap.TryGetValue(regionRef, out var data))
                        {
                            if (data == null) continue;
                            foreach (var currentOption in data.Options)
                            {
                                if (currentOption == null) continue;
                                currentOption.InterestedInEntity(map, entity, checkList);
                                checkList.Clear();
                            }
                        }
                    }
                }
            }

            if (entity is KismetSequenceEntity)
            {
                var globalsProto = GameDatabase.GlobalsPrototype;
                if (globalsProto == null) return false;
                if (globalsProto.KismetSequenceEntityPrototype != PrototypeId.Invalid)
                    map.Insert(globalsProto.KismetSequenceEntityPrototype, EntityTrackingFlag.KismetSequenceTracking);
            }

            var missionRef = entity.MissionPrototype;
            if (missionRef != PrototypeId.Invalid)
                map.Insert(missionRef, EntityTrackingFlag.SpawnedByMission);

            InteractionData interactionData = worldEntityProto.GetInteractionData();
            if (interactionData != null && interactionData.HasAnyOptionFlags())
            {
                foreach (var option in interactionData.Options)
                {
                    option.InterestedInEntity(map, entity, checkList);
                    checkList.Clear();
                }
            }

            List<InteractionData> keywordsInteractionData = worldEntityProto.GetKeywordsInteractionData();
            foreach (var interKeyData in keywordsInteractionData)
            {
                if (interKeyData != null && interKeyData.HasAnyOptionFlags())
                {
                    foreach (var option in interKeyData.Options)
                    {
                        option.InterestedInEntity(map, entity, checkList);
                        checkList.Clear();
                    }
                }
            }

            HashSetPool<InteractionOption>.Instance.Return(checkList);
            return map.Count > 0;
        }

        public void BuildEntityPrototypeCachedData(WorldEntityPrototype entityProto)
        {
            if (entityProto == null) return;

            if (_interaсtionMap.ContainsKey(entityProto.DataRef) && _interaсtionMap[entityProto.DataRef] != null)
                entityProto.InteractionData = _interaсtionMap[entityProto.DataRef];

            if (entityProto.Keywords.HasValue())
                foreach (var keywordRef in entityProto.Keywords)
                    if (_interaсtionMap.ContainsKey(keywordRef) && _interaсtionMap[keywordRef] != null)
                        entityProto.KeywordsInteractionData.Add(_interaсtionMap[keywordRef]);
        }

        public static InteractionMethod CallGetInteractionStatus(EntityDesc interacteeDesc, WorldEntity interactor, 
            InteractionOptimizationFlags optimizationFlags, InteractionFlags flags, ref InteractData interactData)
        {
            if (interactor == null) return InteractionMethod.None;
            var manager = GameDatabase.InteractionManager;
            if (manager == null) return InteractionMethod.None;
            interactData ??= new InteractData();
            return manager.GetInteractionStatus(interacteeDesc, interactor, optimizationFlags, flags, ref interactData);
        }

        private InteractionMethod GetInteractionStatus(EntityDesc interacteeDesc, WorldEntity interactor, 
            InteractionOptimizationFlags optimizationFlags, InteractionFlags flags, ref InteractData interactData)
        {
            var interactee = interacteeDesc.GetEntity<WorldEntity>(interactor.Game);
            if (interactee != null)
                return GetInteractionsForLocalEntity(interactee, interactor, optimizationFlags, flags, ref interactData);
            return InteractionMethod.None;
        }

        private InteractionMethod GetInteractionsForLocalEntity(WorldEntity interactee, WorldEntity interactor, 
            InteractionOptimizationFlags optimizationFlags, InteractionFlags interactionFlags, ref InteractData interactData)
        {
            var interactionsResult = InteractionMethod.None;
            if (CheckEntityPrerequisites(interactee, interactor, interactionFlags))
            {
                interactionsResult = EvaluateInteractionOptions(interactee, interactor, optimizationFlags, interactionFlags, ref interactData);
                interactionsResult = CheckAndApplyLegacyInteractableProperties(interactionsResult, interactee);
                interactionsResult = CheckAndApplyInteractData(interactionsResult, interactData);

                Player player = interactor.GetOwnerOfType<Player>();
                if (player != null)
                    interactData.Visible = GetVisibilityStatus(interactee, interactData.VisibleOverride);

                if (interactee is Transition transition)
                {
                    TransitionPrototype transitionProto = transition.TransitionPrototype;
                    if (transitionProto == null)
                        return interactionsResult;

                    PrototypeId? none = null;
                    if ((transitionProto.Type == RegionTransitionType.Transition || transitionProto.Type == RegionTransitionType.TransitionDirectReturn) 
                        && transitionProto.ShowIndicator 
                        && (interactData.Interactable == TriBool.True || interactee.Properties[PropertyEnum.Interactable] == (int)TriBool.True))
                        TrySetIndicatorTypeAndMapOverrideWithPriority(interactee, ref interactData.IndicatorType, ref none, HUDEntityOverheadIcon.Transporter);
                }

                if (interactData.PlayerHUDFlags.HasFlag(PlayerHUDEnum.HasObjectives | PlayerHUDEnum.ShowObjs | PlayerHUDEnum.ShowObjsOnMap))
                {
                    if (interactData.MapIconOverrideRef == PrototypeId.Invalid)
                    {
                        UIGlobalsPrototype uiGlobals = GameDatabase.UIGlobalsPrototype;
                        if (uiGlobals == null)
                            return InteractionMethod.None;

                        if (interactee.IsHostileTo(interactor))
                            interactData.MapIconOverrideRef = uiGlobals.MapInfoMissionObjectiveMob;
                        else
                            interactData.MapIconOverrideRef = uiGlobals.MapInfoMissionObjectiveUse;
                    }

                    TriBool interactable = (TriBool)(int)interactee.Properties[PropertyEnum.Interactable];
                    if (interactable == TriBool.False)
                    {
                        interactData.PlayerHUDFlags = PlayerHUDEnum.None;
                        interactData.MissionObjectives?.Clear();
                    }
                }
            }

            return interactionsResult;
        }

        public static void TrySetIndicatorTypeAndMapOverrideWithPriority(WorldEntity target, ref HUDEntityOverheadIcon? setIndicatorType, 
            ref PrototypeId? mapOverrideRef, HUDEntityOverheadIcon indicatorType)
        {
            if (setIndicatorType.HasValue && setIndicatorType < indicatorType)
                setIndicatorType = indicatorType;

            if (mapOverrideRef.HasValue)
            {
                UIGlobalsPrototype uiGlobalsProto = GameDatabase.UIGlobalsPrototype;
                if (indicatorType == HUDEntityOverheadIcon.MissionBestower)
                    mapOverrideRef = uiGlobalsProto.MapInfoMissionGiver;
                else if (indicatorType == HUDEntityOverheadIcon.MissionAdvancer && mapOverrideRef != uiGlobalsProto.MapInfoMissionGiver)
                {
                    if (target is Agent)
                        mapOverrideRef = uiGlobalsProto.MapInfoMissionObjectiveTalk;
                    else
                        mapOverrideRef = uiGlobalsProto.MapInfoMissionObjectiveUse;
                }
            }
        }

        public bool GetVisibilityStatus(Player player, WorldEntity interactee)
        {
            TriBool visibilityOverride = EvaluateVisibilityOptions(player, interactee);
            return GetVisibilityStatus(interactee, visibilityOverride);
        }

        private TriBool EvaluateVisibilityOptions(Player interactingPlayer, WorldEntity interactee)
        {
            TriBool result = TriBool.Undefined;
            if (_interaсtionMap.TryGetValue(interactee.PrototypeDataRef, out InteractionData interactionData))
            {
                if (interactionData == null || interactingPlayer.GetRegion() == null) return result;
                if (interactionData.HasOptionFlags(InteractionOptimizationFlags.Visibility) == false) return result;
                foreach (var option in interactionData.Options)
                    if (option is MissionVisibilityOption)
                    {
                        TriBool optionResult = EvaluateVisibilityOption(option, interactingPlayer, interactee);
                        result = TriBoolTrueBias(result, optionResult);
                        if (result == TriBool.True) break;
                    }
            }
            return result;
        }

        private static bool GetVisibilityStatus(WorldEntity interactee, TriBool visibilityOverride)
        {
            bool visibility = false;
            switch (visibilityOverride)
            {
                case TriBool.Undefined:
                    visibility = interactee.DefaultRuntimeVisibility;
                    break;
                case TriBool.True:
                    visibility = true;
                    break;
                case TriBool.False:
                    visibility = false;
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }
            return visibility;
        }

        private static InteractionMethod CheckAndApplyInteractData(InteractionMethod interactions, InteractData interactData)
        {
            var interactionsResult = interactions;
            TriBool dataInteractable = interactData.Interactable;
            if (dataInteractable == TriBool.True)
                interactionsResult |= InteractionMethod.Use;
            else if (dataInteractable == TriBool.False)
                interactionsResult = InteractionMethod.None;

            return interactionsResult;
        }

        private static InteractionMethod CheckAndApplyLegacyInteractableProperties(InteractionMethod interactions, WorldEntity interactee)
        {
            var interactionsResult = interactions;
            TriBool legacyInteractable = (TriBool)(int)interactee.Properties[PropertyEnum.Interactable];
            if (legacyInteractable == TriBool.True)
                if (interactee.Properties[PropertyEnum.InteractableUsesLeft] == 0)
                    legacyInteractable = TriBool.False;

            if (legacyInteractable == TriBool.True)
                interactionsResult |= InteractionMethod.Use;
            else if (legacyInteractable == TriBool.False)
                interactionsResult = InteractionMethod.None;

            return interactionsResult;
        }

        private InteractionMethod EvaluateInteractionOptions(WorldEntity interactee, WorldEntity interactor, InteractionOptimizationFlags optimizationFlags, InteractionFlags interactionFlags, ref InteractData outInteractData)
        {
            var interactionsResult = InteractionMethod.None;
            const int startingPriority = int.MaxValue;
            int lastAvailableOptionPriority = startingPriority;

            List<InteractionOption> optionsList = ListPool<InteractionOption>.Instance.Get();
            if (optimizationFlags == InteractionOptimizationFlags.None)
            {
                GetInteractionDataFromWorldEntityPrototype(optionsList, interactee.PrototypeDataRef);
                if (interactee.Properties[PropertyEnum.EntSelActHasInteractOption])
                    CreateOptionInList<EntitySelectorActionOption>(optionsList);
            }

            var worldEntityProto = interactee.WorldEntityPrototype;
            if (worldEntityProto == null)
                return InteractionMethod.None;

            var interactionData = worldEntityProto.GetInteractionData();
            bool hasInteractionData = interactionData != null;
            bool hasKeywords = worldEntityProto.Keywords.HasValue();

            if (optionsList.Count > 0 || hasInteractionData || hasKeywords)
            {
                HashSet<InteractionOption> interactionOptions = HashSetPool<InteractionOption>.Instance.Get();
                if (hasInteractionData)
                    if (optimizationFlags == InteractionOptimizationFlags.None || interactionData.HasOptionFlags(optimizationFlags))
                        foreach (var option in interactionData.Options)
                            interactionOptions.Add(option);

                if (hasKeywords)
                    foreach (var keyword in worldEntityProto.Keywords)
                        if (_interaсtionMap.TryGetValue(keyword, out var keywordInteractionData))
                            if (optimizationFlags == InteractionOptimizationFlags.None || keywordInteractionData.HasOptionFlags(optimizationFlags))
                                foreach (var option in keywordInteractionData.Options)
                                    interactionOptions.Add(option);

                optionsList.AddRange(interactionOptions);
                HashSetPool<InteractionOption>.Instance.Return(interactionOptions);
                optionsList.Sort((a, b) => a.SortPriority(b));

                bool before = false;
                bool after = false;

                foreach (var currentOption in optionsList)
                    if (CheckOptionFilters(interactee, interactor, currentOption))
                    {
                        int currentOptionPriority = currentOption.Priority;
                        if (!(lastAvailableOptionPriority == startingPriority || currentOptionPriority >= lastAvailableOptionPriority))
                        {
                            Logger.Warn($"InteractionManager's options for '{interactee.PrototypeName}' must be sorted in ascending order of priority, but the following option isn't!\n{currentOption}");
                            ListPool<InteractionOption>.Instance.Return(optionsList);
                            return InteractionMethod.None;
                        }

                        if (interactionFlags.HasFlag(InteractionFlags.EvaluateInteraction) || currentOptionPriority <= lastAvailableOptionPriority)
                        {
                            if (EvaluateInteractionOption(interactee, interactor, currentOption, interactionFlags, ref interactionsResult, ref outInteractData))
                            {
                                lastAvailableOptionPriority = currentOptionPriority;
                                if (currentOption.MethodEnum < InteractionMethod.Neutral)
                                    before = true;
                                else if (currentOption.MethodEnum > InteractionMethod.Neutral)
                                    after = true;
                            }
                        }
                        else
                            break;
                    }

                if (before && after)
                    interactionsResult |= InteractionMethod.Neutral;
            }
            ListPool<InteractionOption>.Instance.Return(optionsList);
            return interactionsResult;
        }

        private bool EvaluateInteractionOption(WorldEntity interactee, WorldEntity interactor, InteractionOption option, InteractionFlags interactionFlags,
            ref InteractionMethod outInteractions, ref InteractData outInteractData)
        {
            bool result;
            List<BaseMissionOption> checkList = ListPool<BaseMissionOption>.Instance.Get();

            if (option is BaseMissionOption baseMissionOption)
            {
                checkList.Clear();
                var missionResult = ParseBaseMissionOption(interactee, interactor, baseMissionOption, ref outInteractData, interactionFlags, null, checkList);
                result = missionResult != InteractionMethod.None;
                if (result)
                    outInteractions |= missionResult;
            }
            else 
            {
                result = option.Evaluate(new EntityDesc(interactee), interactor, interactionFlags, ref outInteractions, ref outInteractData);
            }

            ListPool<BaseMissionOption>.Instance.Return(checkList);
            return result;
        }

        private InteractionMethod ParseBaseMissionOption(WorldEntity interactee, WorldEntity interactor, BaseMissionOption baseMissionOption, ref InteractData outInteractData, 
            InteractionFlags interactionFlags, BaseMissionOption completeOption, List<BaseMissionOption> checkList)
        {
            var resultNoneMethod = InteractionMethod.None;
            if (interactee == null || interactor == null || baseMissionOption == null) return resultNoneMethod;

            if (checkList.Contains(baseMissionOption))
                return resultNoneMethod;
            else
                checkList.Add(baseMissionOption);

            // Player part
            Player player = interactor.GetOwnerOfType<Player>();
            if (player == null) return resultNoneMethod;

            if (baseMissionOption is MissionActionEntityTargetOption || baseMissionOption is MissionConditionRegionOption)
                return resultNoneMethod;

            MissionPrototype missionProto = baseMissionOption.MissionProto;
            if (missionProto == null) return resultNoneMethod;

            var missionResult = resultNoneMethod;
            Mission mission = baseMissionOption.GetMission(player);
            if (mission != null && mission.IsSuspended == false)
            {
                if (baseMissionOption is MissionConditionEntityInteractOption interactOption)
                {
                    if (interactOption.IsActiveForMissionAndEntity(mission, interactee))
                    {
                        var indicatorType = HUDEntityOverheadIcon.None;
                        if (interactOption.HasObjective == false)
                        {
                            if (interactee is Agent)
                                indicatorType = mission.ShouldShowInteractIndicators() ? HUDEntityOverheadIcon.MissionBestower : HUDEntityOverheadIcon.DiscoveryBestower;
                        }
                        else
                        {
                            MissionObjective objective = interactOption.GetObjective(mission);
                            if (objective != null && objective.State == MissionObjectiveState.Available)
                            {
                                if (interactOption.Proto is MissionConditionEntityInteractPrototype interactProto && interactProto.IsTurnInNPC)
                                    indicatorType = HUDEntityOverheadIcon.MissionAdvancerDisabled;
                            }
                            else
                            {
                                if (interactee is Agent)
                                    indicatorType = mission.ShouldShowInteractIndicators() ? HUDEntityOverheadIcon.MissionAdvancer : HUDEntityOverheadIcon.DiscoveryAdvancer;                                
                            }
                        }

                        missionResult = ParseMissionConditionEntityInteractPrototype(interactOption, mission, indicatorType, player, interactor, interactee, ref outInteractData, completeOption);
                    }
                }
                else if (baseMissionOption is MissionVisibilityOption visibilityOption)
                {
                    TriBool visibilityResult = EvaluateVisibilityOption(visibilityOption, player, interactee);
                    outInteractData.VisibleOverride = TriBoolTrueBias(outInteractData.VisibleOverride, visibilityResult);
                }
                else if (baseMissionOption is MissionDialogOption dialogOption)
                {
                    if (dialogOption.IsActiveForMissionAndEntity(mission, interactee))
                        missionResult = ParseMissionDialogTextPrototype(mission, interactor, interactee, dialogOption.Proto, -1, ref outInteractData);
                }
                else if (baseMissionOption is MissionAppearanceOption appearanceOption)
                {
                    if (appearanceOption.IsActiveForMissionAndEntity(mission, interactee))
                        missionResult = ParseEntityAppearanceSpecPrototype(mission, true, interactee, appearanceOption.Proto, ref outInteractData);
                }
                else if (baseMissionOption is MissionConditionMissionCompleteOption missionCompleteOption)
                {
                    if (missionCompleteOption.IsActiveForMissionAndEntity(mission, interactee))
                        foreach (PrototypeId completeMissionRef in missionCompleteOption.CompleteMissionRefs)
                        {
                            var missionData = GetMissionData(completeMissionRef);
                            if (missionData == null) continue;

                            foreach (var subOption in missionData.Options)
                            {
                                if (subOption == null) continue;
                                ParseBaseMissionOption(interactee, interactor, subOption, ref outInteractData, interactionFlags, missionCompleteOption, checkList);
                            }
                        }
                }
                else if (baseMissionOption is MissionHintOption hintOption)
                {
                    if (hintOption.IsActiveForMissionAndEntity(mission, interactee))
                    {
                        var hintProto = hintOption.Proto;
                        if (hintProto != null)
                        {
                            bool hintEntity = hintProto.TargetEntity?.Evaluate(interactee, new (missionProto.DataRef)) ?? false;
                            bool hintPlayer = hintProto.PlayerStateFilter?.Evaluate(interactor, new (missionProto.DataRef)) ?? true;
                            if (hintEntity && hintPlayer)
                                hintOption.SetInteractDataObjectiveFlags(player, ref outInteractData, mission, completeOption);
                        }
                    }
                }
                else
                {
                    if (baseMissionOption.ObjectiveFlagsAllowed() && baseMissionOption.IsActiveForMissionAndEntity(mission, interactee))
                        baseMissionOption.SetInteractDataObjectiveFlags(player, ref outInteractData, mission, completeOption);
                }
            }

            return missionResult;
        }

        public static TriBool TriBoolTrueBias(TriBool value, TriBool newValue)
        {
            if (value == TriBool.Undefined)
                return newValue;
            else if (value == TriBool.False && newValue == TriBool.True)
                return newValue;
            return value;
        }

        private static TriBool EvaluateVisibilityOption(InteractionOption option, Player interactingPlayer, WorldEntity interactee)
        {
            if (option == null) return TriBool.Undefined;
            TriBool retVal = TriBool.Undefined;
            if (option is MissionVisibilityOption visibilityOption)
            {
                var visibilityProto = visibilityOption.Proto;
                if (visibilityProto == null) return TriBool.Undefined;
                var MissionProto = visibilityOption.MissionProto;
                if (MissionProto == null) return TriBool.Undefined;

                if (CheckOptionFilters(interactee, interactingPlayer.PrimaryAvatar, visibilityOption))
                {
                    var missionManager = MissionManager.FindMissionManagerForMission(interactingPlayer, interactingPlayer.GetRegion(), MissionProto.DataRef);
                    var mission = missionManager?.FindMissionByDataRef(MissionProto.DataRef);
                    if (mission != null)
                        if (visibilityOption.IsActiveForMissionAndEntity(mission, interactee))
                            retVal = visibilityProto.Visible ? TriBool.True : TriBool.False;
                }
            }
            return retVal;
        }

        private static InteractionMethod ParseEntityAppearanceSpecPrototype(Mission mission, bool state, WorldEntity interactEntity, EntityAppearanceSpecPrototype prototype, ref InteractData outInteractData)
        {
            var missionResult = InteractionMethod.None;
            if (mission == null || interactEntity == null || prototype == null)
                return missionResult;

            var appearanceProto = GameDatabase.GetPrototype<EntityAppearancePrototype>(prototype.Appearance);

            if (state)
            {
                if (outInteractData.AppearanceEnum != null && appearanceProto != null)
                    outInteractData.AppearanceEnum = appearanceProto.AppearanceEnum;

                if (prototype.InteractionEnabled == TriBool.True)
                {
                    if (outInteractData.Interactable == TriBool.False)
                        Logger.Warn($"Trying to set ambiguous interactability state for entity [{interactEntity}]. Overriding with true.");
                    outInteractData.Interactable = TriBool.True;
                    missionResult = InteractionMethod.Use;
                }
                else if (prototype.InteractionEnabled == TriBool.False)
                {
                    if (outInteractData.Interactable == TriBool.True)
                        Logger.Warn($"Trying to set ambiguous interactability state for entity [{interactEntity}].");
                    outInteractData.Interactable = TriBool.False;
                }
            }
            else if (prototype.FailureReasonText != LocaleStringId.Blank)
            {
                if (prototype.InteractionEnabled == TriBool.True)
                    outInteractData.FailureReasonText = prototype.FailureReasonText;
            }

            return missionResult;
        }

        private static InteractionMethod ParseMissionDialogTextPrototype(Mission mission, WorldEntity interactor, WorldEntity interactEntity, MissionDialogTextPrototype prototype, sbyte objectiveIndex, ref InteractData outInteractData)        
        {
            var missionResult = InteractionMethod.None;
            if (mission == null || interactor == null || interactEntity == null || prototype == null)
                return missionResult;

            if (prototype.Text != LocaleStringId.Blank)
            {
                missionResult = InteractionMethod.Converse;
                DialogStyle dialogStyle = prototype.DialogStyle;
                if (dialogStyle == DialogStyle.None)
                    dialogStyle = ((WorldEntityPrototype)interactEntity.Prototype).DialogStyle;

                if (outInteractData.DialogDataCollection != null)
                    mission.MissionManager.AttachDialogDataFromMission(outInteractData.DialogDataCollection, mission, dialogStyle,
                        prototype.Text, VOCategory.MissionInProgress, interactor.Id, PrototypeId.Invalid, interactEntity.Id, 
                        objectiveIndex, -1, false, false, false, LocaleStringId.Blank);

                /* useless code
                if (outInteractData.MapIconOverrideRef != null && outInteractData.MapIconOverrideRef == PrototypeId.Invalid)
                {
                    UIGlobalsPrototype uiGlobals = GameDatabase.UIGlobalsPrototype;
                    if (uiGlobals == null)
                        return missionResult;
                }
                */
            }

            return missionResult;
        }

        private static InteractionMethod ParseMissionConditionEntityInteractPrototype(MissionConditionEntityInteractOption option, Mission mission, 
            HUDEntityOverheadIcon indicatorType, Player player, WorldEntity interactor, WorldEntity interactEntity, 
            ref InteractData outInteractData, BaseMissionOption completeOption)
        {
            var missionResult = InteractionMethod.None;
            if (option.Proto is not MissionConditionEntityInteractPrototype interactProto) return missionResult;
            var missionProto = mission.Prototype;
            if (missionProto == null) return missionResult;

            int avatarCharacterLevel = player.CurrentAvatarCharacterLevel;
            if (missionProto.Level - avatarCharacterLevel >= MissionManager.MissionLevelUpperBoundsOffset())
            {
                if (indicatorType == HUDEntityOverheadIcon.MissionBestower)
                    TrySetIndicatorTypeAndMapOverrideWithPriority(interactEntity, ref outInteractData.IndicatorType, 
                        ref outInteractData.MapIconOverrideRef, HUDEntityOverheadIcon.MissionBestowerDisabled);
                return missionResult;
            }

            if (interactProto.RequiredItems.HasValue() && MissionManager.MatchItemsToRemove(player, interactProto.RequiredItems) == false)
                return missionResult;

            if (interactProto.DialogText != LocaleStringId.Blank || interactProto.DialogTextList.HasValue())
                missionResult |= InteractionMethod.Converse;
            else
                missionResult |= InteractionMethod.Use;

            TrySetIndicatorTypeAndMapOverrideWithPriority(interactEntity, ref outInteractData.IndicatorType, ref outInteractData.MapIconOverrideRef, indicatorType);
            option.SetInteractDataObjectiveFlags(player, ref outInteractData, mission, completeOption);

            if (outInteractData.DialogDataCollection != null)
            {
                LocaleStringId textDialog = DialogData.PickDialog(player.Game, interactProto);
                DialogStyle dialogStyle = interactProto.DialogStyle;
                if (dialogStyle == DialogStyle.None)
                    dialogStyle = ((WorldEntityPrototype)interactEntity.Prototype).DialogStyle;

                bool showRewards = interactProto.ShowRewards;
                bool showGiveItems = interactProto.GiveItems.HasValue() || interactProto.IsTurnInNPC;
                var objectiveIndex = option.ObjectiveIndex;
                sbyte conditionIndex = (sbyte)interactProto.Index;

                if (option.HasObjective == false && mission.State != MissionState.Active)
                    if (missionProto.Rewards.HasValue())
                        showRewards = true;

                var voCategory = interactProto.VoiceoverCategory;
                if (voCategory == VOCategory.Invalid) 
                {
                    voCategory = VOCategory.MissionBestow;
                    if (mission.State == MissionState.Active) 
                    {
                        voCategory = VOCategory.MissionInProgress;
                        if (interactProto.IsTurnInNPC)
                            voCategory = VOCategory.MissionCompleted;
                    }                   
                }

                mission.MissionManager.AttachDialogDataFromMission( outInteractData.DialogDataCollection,
                    mission, dialogStyle, textDialog, voCategory, interactor.Id, interactProto.Cinematic,
                    interactEntity.Id, objectiveIndex, conditionIndex, interactProto.IsTurnInNPC,
                    showRewards, showGiveItems, interactProto.DialogTextWhenInventoryFull);
            }

            return missionResult;
        }

        private static bool CheckOptionFilters(WorldEntity interactee, WorldEntity interactor, InteractionOption option)
        {
            if (option.EntityFilterWrapper.EvaluateEntity(interactee) == false) return false;

            // This part never used
            if (option.RegionFilterRef != PrototypeId.Invalid)
            {
                if (interactor == null) return false;
                Region region = interactor.RegionLocation.Region;
                if (region == null || region.PrototypeDataRef != option.RegionFilterRef)
                    return false;
            }

            if (option.AreaFilterRef != PrototypeId.Invalid)
            {
                if (interactor == null) return false;
                Area area = interactor.RegionLocation.Area;
                if (area == null || area.PrototypeDataRef != option.AreaFilterRef)
                    return false;
            }

            if (option.MissionFilterRef != PrototypeId.Invalid)
            {
                PrototypeId missionDataRef = interactee.MissionPrototype;
                if (missionDataRef != option.MissionFilterRef)
                    return false;
            }

            return true;
        }

        private static void GetInteractionDataFromWorldEntityPrototype(List<InteractionOption> optionsList, PrototypeId entityDataRef)
        {
            WorldEntityPrototype entityPrototype = GameDatabase.GetPrototype<WorldEntityPrototype>(entityDataRef);
            if (entityPrototype == null) return;

            if (entityPrototype is not ItemPrototype 
                && entityPrototype is not MissilePrototype 
                && entityPrototype is not OrbPrototype 
                && entityPrototype is not TransitionPrototype)
                CreateOptionInList<AttackOption>(optionsList);

            PropertyCollection properties = entityPrototype.Properties;
            if (properties != null)
            {
                if (properties[PropertyEnum.VendorType] != PrototypeId.Invalid)
                    CreateOptionInList<VendorOption>(optionsList);
                if (properties[PropertyEnum.OpenPlayerStash])
                    CreateOptionInList<StashOption>(optionsList);
                if (properties.HasProperty(PropertyEnum.OpenMTXStore))
                    CreateOptionInList<OpenMTXStoreOption>(optionsList);
                if (properties[PropertyEnum.ThrowablePower] != PrototypeId.Invalid)
                    CreateOptionInList<ThrowOption>(optionsList);
                if (properties[PropertyEnum.Trainer])
                    CreateOptionInList<TrainerOption>(optionsList);
                if (properties[PropertyEnum.HealerNPC])
                    CreateOptionInList<HealOption>(optionsList);
                if (properties[PropertyEnum.OpenStoryWarpPanel])
                    CreateOptionInList<StoryWarpOption>(optionsList);
            }

            if (entityPrototype.DialogText != 0 || entityPrototype.DialogTextList.HasValue())
                CreateOptionInList<DialogOption>(optionsList);

            if (entityPrototype is TransitionPrototype)
                CreateOptionInList<TransitionOption>(optionsList);

            if (entityPrototype is ItemPrototype)
            {
                CreateOptionInList<ItemPickupOption>(optionsList);
                CreateOptionInList<ItemBuyOption>(optionsList);
                CreateOptionInList<ItemSellOption>(optionsList);
                CreateOptionInList<ItemDonateOption>(optionsList);
                CreateOptionInList<ItemDonatePetTechOption>(optionsList);
                CreateOptionInList<ItemMoveToGeneralInventoryOption>(optionsList);
                CreateOptionInList<ItemMoveToStashOption>(optionsList);
                CreateOptionInList<ItemMoveToTeamUpOption>(optionsList);
                CreateOptionInList<ItemMoveToTradeInventoryOption>(optionsList);
                CreateOptionInList<ItemUseOption>(optionsList);
                CreateOptionInList<ItemEquipOption>(optionsList);
                CreateOptionInList<ItemSlotCraftingIngredientOption>(optionsList);
                CreateOptionInList<ItemLinkInChatOption>(optionsList);
            }

            if (entityPrototype.PostInteractState != null)
                CreateOptionInList<PostInteractStateOption>(optionsList);

            if (entityPrototype is AvatarPrototype)
            {
                CreateOptionInList<ResurrectOption>(optionsList);
                CreateOptionInList<PartyBootOption>(optionsList);
                CreateOptionInList<GroupChangeTypeOption>(optionsList);
                CreateOptionInList<PartyInviteOption>(optionsList);
                CreateOptionInList<PartyLeaveOption>(optionsList);
                CreateOptionInList<PartyShareLegendaryQuestOption>(optionsList);
                CreateOptionInList<PlayerMuteOption>(optionsList);
                CreateOptionInList<GuildInviteOption>(optionsList);
                CreateOptionInList<ChatOption>(optionsList);
                CreateOptionInList<TeleportOption>(optionsList);
                CreateOptionInList<ReportOption>(optionsList);
                CreateOptionInList<ReportAsSpamOption>(optionsList);
                CreateOptionInList<FriendOption>(optionsList);
                CreateOptionInList<UnfriendOption>(optionsList);
                CreateOptionInList<IgnoreOption>(optionsList);
                CreateOptionInList<UnignoreOption>(optionsList);
                CreateOptionInList<InspectOption>(optionsList);
                CreateOptionInList<MakeLeaderOption>(optionsList);

                if (Player.IsPlayerTradeEnabled)
                    CreateOptionInList<TradeOption>(optionsList);
            }
        }

        private static bool CheckEntityPrerequisites(WorldEntity interactee, WorldEntity interactor, InteractionFlags interactionFlags)
        {
            if (interactionFlags.HasFlag(InteractionFlags.DeadInteractor) == false)
                if (interactor.IsDead) return false;
            if (interactionFlags.HasFlag(InteractionFlags.DormanInvisibleInteractee) == false)
                if (interactee.Properties[PropertyEnum.Dormant] || interactee.Properties[PropertyEnum.Visible] == false)
                    return false;
            return true;
        }

        public bool GetRegionInterest(Player player, PrototypeId regionRef, PrototypeId areaRef, PrototypeId cellRef, InteractionOptimizationFlags optimizationFlags, ref InteractData outInteractData)
        {
            if (regionRef == PrototypeId.Invalid) return false;

            var interactor = player.PrimaryAvatar;
            if (interactor == null) return false;

            if (DataDirectory.Instance.GetPrototypeClassType(regionRef) != typeof(RegionPrototype))
                return Logger.WarnReturn(false, $"GetRegionInterest called on a non-Region PrototypeId: {regionRef}"); 

            bool interest = false;
            if (_interaсtionMap.TryGetValue(regionRef, out var interactionData))
            {
                if (interactionData == null) return false;

                if (optimizationFlags == 0 || interactionData.HasOptionFlags(optimizationFlags))
                    foreach (var option in interactionData.Options)
                    {
                        if (option == null) continue;
                        interest |= ParseRegionInterests(player, option, ref outInteractData, regionRef, areaRef, cellRef);
                    }
            }
            return interest;
        }

        private static bool ParseRegionInterests(Player player, InteractionOption option, ref InteractData outInteractData, PrototypeId regionRef, PrototypeId areaRef, PrototypeId cellRef)
        {
            bool interest = false;
            MissionPrototype missionProto;

            if (option is BaseMissionOption missionOption)
                missionProto = missionOption.MissionProto;
            else
                return interest;

            if (missionProto == null) return false;

            var region = player.GetRegion();
            if (region == null) return false;

            var missionManager = MissionManager.FindMissionManagerForMission(player, region, missionProto);
            var mission = missionManager?.FindMissionByDataRef(missionProto.DataRef);
            if (mission != null)
            {
                if (mission.ShouldShowMapPingOnPortals == false) return false;

                if (missionOption is MissionHintOption missionHintOption)
                {
                    if (missionHintOption.IsActiveForMissionAndEntity(mission, null))
                    {
                        var data = missionHintOption.Proto;
                        if (data != null)
                        {
                            bool targetRegion = data.TargetRegion == regionRef;
                            bool targetArea = (areaRef == PrototypeId.Invalid) || (data.TargetArea == PrototypeId.Invalid) || areaRef == data.TargetArea;

                            var avatar = player.PrimaryAvatar;
                            bool targetPlayer = (data.PlayerStateFilter == null) 
                                || (avatar != null && data.PlayerStateFilter.Evaluate(avatar, new EntityFilterContext(missionProto.DataRef)));

                            if (targetRegion && targetArea && targetPlayer)
                                missionHintOption.SetInteractDataObjectiveFlags(player, ref outInteractData, mission, null);
                        }
                    }
                }
                else
                {
                    if (missionOption.IsLocationInteresting(player, regionRef, areaRef, cellRef)
                        && missionOption.ObjectiveFlagsAllowed()
                        && missionOption.IsActiveForMissionAndEntity(mission, null))
                        missionOption.SetInteractDataObjectiveFlags(player, ref outInteractData, mission, null);

                    interest |= true;
                }
            }

            return interest;
        }

        public bool IsMissionAssociated(WorldEntityPrototype entityProto)
        {
            if (entityProto is TransitionPrototype) return true;
            else if (entityProto is KismetSequenceEntityPrototype) return true;
            else
            {
                bool isAssociated = false;
                var interactionData = entityProto.GetInteractionData();
                if (interactionData != null)
                    foreach(var option in interactionData.Options)
                        isAssociated |= option is BaseMissionConditionOption || option is MissionActionEntityTargetOption;
                return isAssociated;
            }
        }
    }

    public class InteractData
    {
        public bool Visible;
        public TriBool Interactable;
        public TriBool VisibleOverride;
        public PrototypeId? MapIconOverrideRef;
        public HUDEntityOverheadIcon? IndicatorType;
        public PlayerHUDEnum PlayerHUDFlags;
        public LocaleStringId FailureReasonText;
        public EntityAppearanceEnum? AppearanceEnum;
        public HashSet<EntityObjectiveInfo> MissionObjectives { get; set; } // client only
        public DialogDataCollection DialogDataCollection { get; set; } // client only
        public int PlayerHUDArrowDistanceOverride { get; set; }

        public InteractData()
        {
            Visible = true;
            Interactable = TriBool.Undefined;
            VisibleOverride = TriBool.Undefined;
            PlayerHUDArrowDistanceOverride = -1;
        }

        public void InsertMissionObjective(Mission mission, MissionObjective objective, BaseMissionOption option, PlayerHUDEnum flags)
        {
            if (mission == null) return;
            if (MissionObjectives != null)
            { 
                if (objective != null)
                    MissionObjectives.Add(new EntityObjectiveInfo(mission.PrototypeDataRef, mission.State, objective.State, objective.PrototypeIndex, option, flags));
                else
                    MissionObjectives.Add(new EntityObjectiveInfo(mission.PrototypeDataRef, mission.State, 0, objective.PrototypeIndex, option, flags));
            }
        }
    }

    public class InteractionData
    {
        public List<InteractionOption> Options { get; }
        private InteractionOptimizationFlags _optionFlags;
        public int OptionCount => Options.Count;
        public bool HasOptionFlag(InteractionOptimizationFlags optionFlag) => _optionFlags.HasFlag(optionFlag);
        public bool HasAnyOptionFlags() => _optionFlags != InteractionOptimizationFlags.None;

        public void Sort()
        {
            Options.Sort((a, b) => a.SortPriority(b));
        }

        public void AddOption(InteractionOption option)
        {
            if (option == null) return;
            Options.Add(option);
            _optionFlags |= option.OptimizationFlags;
        }

        public bool HasOptionFlags(InteractionOptimizationFlags optimizationFlags)
        {
            return (_optionFlags & optimizationFlags) != 0;
        }

        public InteractionData()
        {
            Options = new();
            _optionFlags = InteractionOptimizationFlags.None;
        }
    }

    public class ExtraMissionData
    {      
        public PrototypeId MissionRef { get; set; }
        public HashSet<BaseMissionOption> Options { get; set; }
        public HashSet<PrototypeId> Contexts { get; set; }
        public HashSet<BaseMissionOption> CompleteOptions { get; set; }
        public bool PlayerHUDShowObjs { get; set; }

        public ExtraMissionData(PrototypeId missionRef)
        {
            MissionRef = missionRef;
            Options = new();
            Contexts = new();
            CompleteOptions = new();
            PlayerHUDShowObjs = true;
        }
    }

}
