using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

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
            foreach (var missionRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy(typeof(MissionPrototype), 
                PrototypeIterateFlags.NoAbstract | PrototypeIterateFlags.ApprovedOnly))
            {
                MissionPrototype missionProto = GameDatabase.GetPrototype<MissionPrototype>(missionRef);
                if (missionProto == null) continue;
                GetInteractionDataFromMissionPrototype(missionProto);
            }

            foreach (var kvp in _missionMap)
            {
                var missionData = kvp.Value;
                if (missionData == null) continue;
                if (missionData.CompleteOptions.Any())
                {
                    HashSet<PrototypeId> contexts = new (missionData.Contexts);
                    if (contexts.Any())
                    {
                        foreach (var completeOption in missionData.CompleteOptions)
                        {
                            if (!contexts.Any())
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

            foreach (var uiWidgetRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy(typeof(MetaGameDataPrototype), 
                PrototypeIterateFlags.NoAbstract | PrototypeIterateFlags.ApprovedOnly))
            {
                if (uiWidgetRef == PrototypeId.Invalid) continue;
                GetInteractionDataFromUIWidgetPrototype(uiWidgetRef);
            }

            foreach (var metaStateRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy(typeof(MetaStatePrototype), 
                PrototypeIterateFlags.NoAbstract | PrototypeIterateFlags.ApprovedOnly))
            {
                if (metaStateRef == PrototypeId.Invalid) continue;
                GetInteractionDataFromMetaStatePrototype(metaStateRef);
            }

            foreach (var kvp in _interaсtionMap)
            {
               // if (kvp.Key == (PrototypeId)3318119032142371986)
               //    Logger.Warn($"{GameDatabase.GetFormattedPrototypeName(kvp.Key)}");
                kvp.Value?.Sort();
            }
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
                    SortedSet<PrototypeId> completeMissions = completeOption.GetCompleteMissionRefs();
                    foreach (PrototypeId completeMissionRef in completeMissions)
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
                HashSet<PrototypeId> contextRefs = new ();
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
                        return;
                    }
                    option.UIWidgetRef = uiWidgetRef;
                    option.Proto = uiWidgetEntityIconsProto;                        
                    BindOptionToMap(option, contextRefs);                    
                }
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
                        foreach (var hintProto in objectivePrototype.ObjectiveHints)
                        {
                            if (hintProto == null) continue;
                            HashSet<PrototypeId> contextRefs = new();
                            hintProto.GetPrototypeContextRefs(contextRefs);
                            if (contextRefs.Count > 0)
                            {
                                var option = CreateOption<MissionHintOption>();
                                if (option == null)
                                {
                                    Logger.Error($"Failed to create MissionObjectiveHintOption! MISSION={missionProto}");
                                    return;
                                }
                                option.Proto = hintProto;
                                option.InitializeForMission(missionProto, MissionStateFlags.Active, objectiveIndex, MissionObjectiveStateFlags.Active, MissionOptionTypeFlags.None);
                                BindOptionToMap(option, contextRefs);
                            }
                        }
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

        private T CreateOption<T>() where T: InteractionOption
        {
            T option = Activator.CreateInstance<T>();
            _options.Add(option);
            return option;
        }

        private void RegisterInteractionsFromList(MissionPrototype missionProto, InteractionSpecPrototype[] interactionSpec, 
            MissionStateFlags state, sbyte objectiveIndex, MissionObjectiveStateFlags objectiveState)
        {
            if (interactionSpec.IsNullOrEmpty()) return;

            foreach (var specProto in interactionSpec)
            {
                if (specProto == null) continue;
                HashSet<PrototypeId> contextRefs = new ();
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

                    if (option == null) return;                    
                    option.InitializeForMission(missionProto, state, objectiveIndex, objectiveState, MissionOptionTypeFlags.None);
                    BindOptionToMap(option, contextRefs);                    
                }
            }
        }

        private void RegisterDialogTextFromList(MissionPrototype missionProto, MissionDialogTextPrototype[] dialogTexts, 
            MissionStateFlags state, sbyte objectiveIndex, MissionObjectiveStateFlags objectiveState)
        {
            if (dialogTexts.IsNullOrEmpty()) return;

            foreach (var missionDialogTextProto in dialogTexts)
            {
                if (missionDialogTextProto == null) continue;
                HashSet<PrototypeId> contextRefs = new ();
                missionDialogTextProto.GetPrototypeContextRefs(contextRefs);

                if (contextRefs.Count > 0)
                {
                    var option = CreateOption<MissionDialogOption>();
                    if (option == null) return;                    
                    option.EntityFilterWrapper.AddEntityFilter(missionDialogTextProto.EntityFilter);
                    option.InitializeForMission(missionProto, state, objectiveIndex, objectiveState, MissionOptionTypeFlags.None);
                    option.Proto = missionDialogTextProto;
                    BindOptionToMap(option, contextRefs);                    
                }
            }
        }

        private void RegisterConditionInfoFromList(MissionPrototype missionProto, MissionConditionListPrototype conditionList, 
            MissionStateFlags state, sbyte objectiveIndex, MissionObjectiveStateFlags objectiveState, 
            MissionOptionTypeFlags optionType, InteractionOptimizationFlags optimizationFlag)
        {
            if (conditionList == null) return;
            foreach (MissionConditionPrototype prototype in conditionList.IteratePrototypes())
            {
                if (prototype == null) continue;
                if (optionType.HasFlag(MissionOptionTypeFlags.SkipComplete) && prototype is MissionConditionMissionCompletePrototype)
                    continue;

                HashSet<PrototypeId> contextRefs = new ();
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

                    if (missionProto is OpenMissionPrototype openMissionProto)
                        option.EntityFilterWrapper.AddRegionPtrs(openMissionProto.ActiveInRegions);

                    prototype.BuildEntityFilter(option.EntityFilterWrapper, missionProto.DataRef);
                    prototype.SetInterestLocations(option.InterestRegions, option.InterestAreas, option.InterestCells);

                    option.OptimizationFlags |= optimizationFlag;
                    option.InitializeForMission(missionProto, state, objectiveIndex, objectiveState, optionType);
                    BindOptionToMap(option, contextRefs);
                }
            }
        }

        private void RegisterActionInfoFromList(MissionPrototype missionProto, MissionActionPrototype[] actionList, MissionStateFlags state, 
            sbyte objectiveIndex, MissionObjectiveStateFlags objectiveState, MissionOptionTypeFlags optionType)
        {
            if (actionList.IsNullOrEmpty()) return;
            foreach (var prototype in actionList)
            {
                if (prototype == null) continue;
                if (prototype is MissionActionEntityTargetPrototype actionEntityTargetProto)
                {
                    HashSet<PrototypeId> contextRefs = new ();
                    actionEntityTargetProto.GetPrototypeContextRefs(contextRefs);
                    if (contextRefs.Count > 0)
                    {
                        var option = CreateOption<MissionActionEntityTargetOption>();
                        if (option == null) return;
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
        }

        // Test function for get StartAction
        public bool GetStartAction(PrototypeId entityRef, PrototypeId targetRef, out MissionActionEntityPerformPowerPrototype action)
        {
            action = null;
            var proto = GameDatabase.GetPrototype<Prototype>(targetRef);
            if (proto is MissionPrototype missionProto && missionProto.OnStartActions.HasValue())
            {
                if (_interaсtionMap.TryGetValue(entityRef, out var data))
                {
                    var actionPower = data.GetStartPower(missionProto);
                    if (actionPower != null)
                    {
                        action = actionPower;
                        return true;
                    }
                }
            }
            else
            {
                if (_interaсtionMap.TryGetValue(entityRef, out var entityData))
                {
                    if (_interaсtionMap.TryGetValue(targetRef, out var targetData))
                    {
                        var actionPower = entityData.GetStartPowerIntersect(targetData);
                        if (actionPower != null)
                        {
                            action = actionPower;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool GetEntityContextInvolvement(WorldEntity entity, EntityTrackingContextMap2 map)
        {
            if (entity == null) return false;

            map.Clear();

            var worldEntityProto = entity.WorldEntityPrototype;

            if (entity is Transition transition)
                foreach (var destination in transition.Destinations)
                {
                    var regionRef = destination.Region;
                    if (regionRef != PrototypeId.Invalid)
                    {
                        map.Insert(regionRef, EntityTrackingFlag.TransitionRegion);
                        if (_interaсtionMap.TryGetValue(regionRef, out var data))
                        {
                            if (data == null) continue;
                            foreach (var currentOption in data.Options)
                            {
                                if (currentOption == null) continue;
                                currentOption.InterestedInEntity(map, entity, new());
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
                foreach (var option in interactionData.Options)
                    option.InterestedInEntity(map, entity, new());

            List<InteractionData> keywordsInteractionData = worldEntityProto.GetKeywordsInteractionData();
            foreach (var interKeyData in keywordsInteractionData)
                if (interKeyData != null && interKeyData.HasAnyOptionFlags())
                    foreach (var option in interKeyData.Options)
                        option.InterestedInEntity(map, entity, new());

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
            Options.Sort((a, b) => a.CompareTo(b));
        }

        public void AddOption(InteractionOption option)
        {
            if (option == null) return;
            Options.Add(option);
            _optionFlags |= option.OptimizationFlags;
        }

        public MissionActionEntityPerformPowerPrototype GetStartPower(MissionPrototype missionProto)
        {            
            foreach(var option in Options)
            {
                if (option is not MissionActionEntityTargetOption targetOption) continue;
                if (targetOption.MissionProto != missionProto) continue;
                if (targetOption.MissionState.HasFlag(MissionStateFlags.OnStart) == false) continue;
                if (targetOption.Proto is MissionActionEntityPerformPowerPrototype performPower) return performPower;
            }
            return null;
        }

        public MissionActionEntityPerformPowerPrototype GetStartPowerIntersect(InteractionData targetData)
        {
            var intersectingOptions = Options.Intersect(targetData.Options);
            foreach (var option in intersectingOptions)
            {
                if (option is not MissionActionEntityTargetOption targetOption) continue;
                if (targetOption.MissionState.HasFlag(MissionStateFlags.OnStart) == false) continue;
                if (targetOption.Proto is MissionActionEntityPerformPowerPrototype performPower) return performPower;
            }
            return null;
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
        public SortedSet<BaseMissionOption> Options { get; set; }
        public SortedSet<PrototypeId> Contexts { get; set; }
        public SortedSet<BaseMissionOption> CompleteOptions { get; set; }
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
