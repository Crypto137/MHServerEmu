using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionEntityKill : MissionActionEntityTarget
    {
        public MissionActionEntityKill(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // MGNgaraiInvasion
        }

        public override bool RunOnStart() => false;
    }
}
