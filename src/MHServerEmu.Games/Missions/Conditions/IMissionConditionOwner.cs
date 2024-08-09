
namespace MHServerEmu.Games.Missions.Conditions
{
    public interface IMissionConditionOwner
    {
        void OnUpdateCondition(MissionCondition condition);
        bool OnConditionCompleted();
    }
}
