
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Dialog
{
    public class BaseMissionOption : InteractionOption
    {
        public MissionPrototype MissionProto { get; internal set; }

        public void InitializeForMission(MissionPrototype missionProto, MissionStateFlags state, sbyte objectiveIndex, MissionObjectiveStateFlags objectiveState, MissionOptionTypeFlags optionType)
        {
            throw new NotImplementedException();
        }
    }

    public class MissionHintOption : BaseMissionOption
    {
        public MissionObjectiveHintPrototype Proto { get; internal set; }


    }

    public class BaseMissionConditionOption : BaseMissionOption
    {

    }

    public class MissionVisibilityOption : BaseMissionOption
    {

    }

    public class MissionDialogOption : BaseMissionOption
    {

    }

    public class MissionConnectionTargetEnableOption : BaseMissionOption
    {

    }

    public class MissionAppearanceOption : BaseMissionOption
    {

    }

    public class MissionActionEntityTargetOption: BaseMissionOption
    {

    }
}
