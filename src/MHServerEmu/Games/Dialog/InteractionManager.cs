using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Dialog
{
    public class InteractionManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private Dictionary<PrototypeId, InteractionData> _interationMap;
        private Dictionary<PrototypeId, ExtraMissionData> _missionMap;
        private List<InteractionOption> _options;

        public InteractionManager()
        {
            _interationMap = new();
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
                                Logger.Warn($"Unable to link option to mission. MISSION={GameDatabase.GetFormattedPrototypeName(missionData.MissionRef)} OPTION={completeOption.ToString()}");
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

            foreach (var kvp in _interationMap)
                kvp.Value?.Sort();
        }

        private void BindOptionToMap(InteractionOption option, HashSet<PrototypeId> contexts)
        {
            throw new NotImplementedException();
        }

        private void GetInteractionDataFromMetaStatePrototype(PrototypeId metaStateRef)
        {
            throw new NotImplementedException();
        }

        private void GetInteractionDataFromUIWidgetPrototype(PrototypeId uiWidgetRef)
        {
            throw new NotImplementedException();
        }



        private void GetInteractionDataFromMissionPrototype(MissionPrototype missionProto)
        {
            if (missionProto == null || missionProto.ApprovedForUse() == false) return;

            const sbyte InvalidIndex = -1;
            InteractionOptimizationFlags missionFlag = 0;
            if (missionProto.PlayerHUDShowObjs && (missionProto.PlayerHUDShowObjsOnMap || missionProto.PlayerHUDShowObjsOnScreenEdge))
                missionFlag |= InteractionOptimizationFlags.Hint;

            if (missionProto.OnAvailableActions.IsNullOrEmpty() == false)
                RegisterActionInfoFromList(missionProto, missionProto.OnAvailableActions, MissionStateFlags.Available, InvalidIndex, 0, MissionOptionTypeFlags.None);

            if (missionProto.OnStartActions.IsNullOrEmpty() == false)
                RegisterActionInfoFromList(missionProto, missionProto.OnStartActions, MissionStateFlags.Active, InvalidIndex, 0, MissionOptionTypeFlags.None);

            if (missionProto.OnFailActions.IsNullOrEmpty() == false)
                RegisterActionInfoFromList(missionProto, missionProto.OnFailActions, MissionStateFlags.Failed, InvalidIndex, 0, MissionOptionTypeFlags.None);

            if (missionProto.OnSuccessActions.IsNullOrEmpty() == false)
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

            if (missionProto.DialogText.IsNullOrEmpty() == false)
                RegisterDialogTextFromList(missionProto, missionProto.DialogText, MissionStateFlags.Active, InvalidIndex, 0);

            if (missionProto.DialogTextWhenCompleted.IsNullOrEmpty() == false)
                RegisterDialogTextFromList(missionProto, missionProto.DialogTextWhenCompleted, MissionStateFlags.Completed, InvalidIndex, 0);

            if (missionProto.DialogTextWhenFailed.IsNullOrEmpty() == false)
                RegisterDialogTextFromList(missionProto, missionProto.DialogTextWhenFailed, MissionStateFlags.Failed, InvalidIndex, 0);

            if (missionProto.Objectives.IsNullOrEmpty() == false)
            {
                for (sbyte objectiveIndex = 0; objectiveIndex < missionProto.Objectives.Length; ++objectiveIndex)
                {
                    var objectivePrototype = missionProto.Objectives[objectiveIndex];
                    if (objectivePrototype == null) continue;

                    InteractionOptimizationFlags objectiveFlag = 0;
                    if (missionFlag.HasFlag(InteractionOptimizationFlags.Hint) && (objectivePrototype.PlayerHUDShowObjsOnMap || objectivePrototype.PlayerHUDShowObjsOnScreenEdge))
                        objectiveFlag |= InteractionOptimizationFlags.Hint;

                    if (objectivePrototype.ObjectiveHints.IsNullOrEmpty() == false)
                    {
                        foreach (var hintProto in objectivePrototype.ObjectiveHints)
                        {
                            if (hintProto == null) continue;
                            HashSet<PrototypeId> contextRefs = new();
                            hintProto.GetPrototypeContextRefs(contextRefs);
                            if (contextRefs.Count > 0)
                            {
                                MissionHintOption option = CreateOption<MissionHintOption>();
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

                    if (objectivePrototype.DialogText.IsNullOrEmpty() == false)
                        RegisterDialogTextFromList(missionProto, objectivePrototype.DialogText, MissionStateFlags.Active, objectiveIndex, MissionObjectiveStateFlags.Active);

                    if (objectivePrototype.DialogTextWhenCompleted.IsNullOrEmpty() == false)
                        RegisterDialogTextFromList(missionProto, objectivePrototype.DialogTextWhenCompleted, MissionStateFlags.Active, objectiveIndex, MissionObjectiveStateFlags.Completed);

                    if (objectivePrototype.DialogTextWhenFailed.IsNullOrEmpty() == false)
                        RegisterDialogTextFromList(missionProto, objectivePrototype.DialogTextWhenFailed, MissionStateFlags.Active, objectiveIndex, MissionObjectiveStateFlags.Failed);

                    if (objectivePrototype.OnAvailableActions.IsNullOrEmpty() == false)
                        RegisterActionInfoFromList(missionProto, objectivePrototype.OnAvailableActions, MissionStateFlags.Active, objectiveIndex, MissionObjectiveStateFlags.Available, MissionOptionTypeFlags.None);

                    if (objectivePrototype.OnStartActions.IsNullOrEmpty() == false)
                        RegisterActionInfoFromList(missionProto, objectivePrototype.OnStartActions, MissionStateFlags.Active, objectiveIndex, MissionObjectiveStateFlags.Active, MissionOptionTypeFlags.None);

                    if (objectivePrototype.OnFailActions.IsNullOrEmpty() == false)
                        RegisterActionInfoFromList(missionProto, objectivePrototype.OnFailActions, MissionStateFlags.Active, objectiveIndex, MissionObjectiveStateFlags.Failed, MissionOptionTypeFlags.None);

                    if (objectivePrototype.OnSuccessActions.IsNullOrEmpty() == false)
                        RegisterActionInfoFromList(missionProto, objectivePrototype.OnSuccessActions, MissionStateFlags.Active, objectiveIndex, MissionObjectiveStateFlags.Completed, MissionOptionTypeFlags.None);

                    if (objectivePrototype.ActivateConditions != null)
                        RegisterConditionInfoFromList(missionProto, objectivePrototype.ActivateConditions, MissionStateFlags.Active, objectiveIndex, MissionObjectiveStateFlags.Available, MissionOptionTypeFlags.Skip, objectiveFlag);

                    if (objectivePrototype.SuccessConditions != null)
                        RegisterConditionInfoFromList(missionProto, objectivePrototype.SuccessConditions, MissionStateFlags.Active, objectiveIndex, MissionObjectiveStateFlags.Active, MissionOptionTypeFlags.None, objectiveFlag);

                    if (objectivePrototype.FailureConditions != null)
                        RegisterConditionInfoFromList(missionProto, objectivePrototype.FailureConditions, MissionStateFlags.Active, objectiveIndex, MissionObjectiveStateFlags.Active, MissionOptionTypeFlags.Skip, objectiveFlag);

                    if (objectivePrototype.InteractionsWhenActive.IsNullOrEmpty() == false)
                        RegisterInteractionsFromList(missionProto, objectivePrototype.InteractionsWhenActive, MissionStateFlags.Active, objectiveIndex, MissionObjectiveStateFlags.Active);

                    if (objectivePrototype.InteractionsWhenComplete.IsNullOrEmpty() == false)
                        RegisterInteractionsFromList(missionProto, objectivePrototype.InteractionsWhenComplete, MissionStateFlags.Active, objectiveIndex, MissionObjectiveStateFlags.Completed);

                    if (objectivePrototype.InteractionsWhenFailed.IsNullOrEmpty() == false)
                        RegisterInteractionsFromList(missionProto, objectivePrototype.InteractionsWhenFailed, MissionStateFlags.Active, objectiveIndex, MissionObjectiveStateFlags.Failed);
                }

                if (missionProto.InteractionsWhenActive.IsNullOrEmpty() == false)
                    RegisterInteractionsFromList(missionProto, missionProto.InteractionsWhenActive, MissionStateFlags.Active, InvalidIndex, 0);

                if (missionProto.InteractionsWhenComplete.IsNullOrEmpty() == false)
                    RegisterInteractionsFromList(missionProto, missionProto.InteractionsWhenComplete, MissionStateFlags.Completed, InvalidIndex, 0);

                if (missionProto.InteractionsWhenFailed != null)
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
            throw new NotImplementedException();
        }

        private void RegisterDialogTextFromList(MissionPrototype missionProto, MissionDialogTextPrototype[] dialogText, 
            MissionStateFlags state, sbyte objectiveIndex, MissionObjectiveStateFlags objectiveState)
        {
            throw new NotImplementedException();
        }

        private void RegisterConditionInfoFromList(MissionPrototype missionProto, MissionConditionListPrototype condition, 
            MissionStateFlags state, sbyte objectiveIndex, MissionObjectiveStateFlags objectiveState, 
            MissionOptionTypeFlags optionType, InteractionOptimizationFlags missionFlag)
        {
            throw new NotImplementedException();
        }

        private void RegisterActionInfoFromList(MissionPrototype missionProto, MissionActionPrototype[] actions, MissionStateFlags state, 
            sbyte objectiveIndex, MissionObjectiveStateFlags objectiveState, MissionOptionTypeFlags optionType)
        {
            throw new NotImplementedException();
        }
    }

    public class InteractionData
    {
        private List<InteractionOption> _options;
        public int OptionFlags { get; set; }

        public void Sort()
        {
            _options.Sort((a, b) => a.CompareTo(b));
        }

        public InteractionData()
        {
            _options = new();
            OptionFlags = 0;
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
