using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Dialog
{
    public class BaseMissionOption : InteractionOption
    {
        public MissionPrototype MissionProto { get; private set; }
        public MissionStateFlags MissionState { get; private set; }
        public sbyte ObjectiveIndex { get; private set; }
        public MissionObjectiveStateFlags ObjectiveState { get; private set; }
        public SortedSet<PrototypeId> InterestRegions { get; private set; }
        public SortedSet<PrototypeId> InterestAreas { get; private set; }
        public SortedSet<PrototypeId> InterestCells { get; private set; }

        public BaseMissionOption()
        {
            MissionState = 0;
            InterestRegions = new();
            InterestAreas = new();
            InterestCells = new();
            ObjectiveIndex = -1;
            ObjectiveState = 0;
            Priority = 10;
        }

        public virtual void InitializeForMission(MissionPrototype missionProto, MissionStateFlags state, sbyte objectiveIndex, MissionObjectiveStateFlags objectiveState, MissionOptionTypeFlags optionType)
        {
            MissionProto = missionProto;
            MissionState = state;
            ObjectiveIndex = objectiveIndex;
            ObjectiveState = objectiveState;
            OptionType = optionType;

            EntityFilterWrapper.FilterContextMissionRef = missionProto.DataRef;
        }
    }

    public class MissionHintOption : BaseMissionOption
    {
        public MissionObjectiveHintPrototype Proto { get; internal set; }  

        public MissionHintOption()
        {
            OptimizationFlags |= InteractionOptimizationFlags.Hint;
            EntityTrackingFlags |= EntityTrackingFlag.HUD;
        }
    }

    public class BaseMissionConditionOption : BaseMissionOption
    {
        public MissionConditionPrototype Proto { get; internal set; }

        public BaseMissionConditionOption()
        {
            EntityTrackingFlags |= EntityTrackingFlag.MissionCondition;
        }
    }

    public class MissionConditionMissionCompleteOption : BaseMissionConditionOption
    {
        private readonly SortedSet<PrototypeId> _missionRefs = new();

        public override void InitializeForMission(MissionPrototype missionProto, MissionStateFlags state, sbyte objectiveIndex, MissionObjectiveStateFlags objectiveState, MissionOptionTypeFlags optionType)
        {
            base.InitializeForMission(missionProto, state, objectiveIndex, objectiveState, optionType);
            if (Proto is not MissionConditionMissionCompletePrototype missionComplete) return;

            PrototypeId missionCompleteRef = missionComplete.MissionPrototype;
            if (missionCompleteRef != PrototypeId.Invalid)
                _missionRefs.Add(missionCompleteRef);

            if (missionComplete.MissionKeyword != PrototypeId.Invalid)
            {
                foreach (var missionRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy(typeof(MissionPrototype),
                    PrototypeIterateFlags.NoAbstract | PrototypeIterateFlags.ApprovedOnly))
                {
                    MissionPrototype mission = missionRef.As<MissionPrototype>();
                    if (mission != null && mission.Keywords.IsNullOrEmpty() == false && mission.Keywords.Contains(missionComplete.MissionKeyword))
                        _missionRefs.Add(missionRef);
                }
            }
        }

        public SortedSet<PrototypeId> GetCompleteMissionRefs()
        {
            return _missionRefs;
        }
    }

    public class MissionConditionRegionOption : BaseMissionConditionOption
    {
        public MissionConditionRegionOption()
        {
            EntityTrackingFlags |= EntityTrackingFlag.TransitionRegion;
        }
    }

    public class MissionConditionHotspotOption : BaseMissionConditionOption
    {
        public MissionConditionHotspotOption()
        {
            OptimizationFlags |= InteractionOptimizationFlags.ConditionHotspot;
            EntityTrackingFlags |= EntityTrackingFlag.Hotspot;
        }
    }
    public class MissionConditionEntityInteractOption : BaseMissionConditionOption
    {

    }

    public class MissionVisibilityOption : BaseMissionOption
    {
        public EntityVisibilitySpecPrototype Proto { get; internal set; }

        public MissionVisibilityOption()
        {
            OptimizationFlags |= InteractionOptimizationFlags.Visibility;
        }
    }

    public class MissionDialogOption : BaseMissionOption
    {
        public MissionDialogTextPrototype Proto { get; internal set; }
    }

    public class MissionConnectionTargetEnableOption : BaseMissionOption
    {
        public ConnectionTargetEnableSpecPrototype Proto { get; internal set; }

        public MissionConnectionTargetEnableOption()
        {
            OptimizationFlags |= InteractionOptimizationFlags.ConnectionTargetEnable;
        }
    }

    public class MissionAppearanceOption : BaseMissionOption
    {
        public EntityAppearanceSpecPrototype Proto { get; internal set; }

        public MissionAppearanceOption()
        {
            OptimizationFlags |= InteractionOptimizationFlags.Appearance;
        }
    }

    public class MissionActionEntityTargetOption: BaseMissionOption
    {
        public MissionActionEntityTargetPrototype Proto { get; internal set; }

        public MissionActionEntityTargetOption()
        {
            OptimizationFlags |= InteractionOptimizationFlags.ActionEntityTarget;
        }
    }
}
