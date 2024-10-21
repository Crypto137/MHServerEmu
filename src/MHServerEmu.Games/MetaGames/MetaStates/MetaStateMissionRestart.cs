using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateMissionRestart : MetaState
    {
	    private MetaStateMissionRestartPrototype _proto;
		
        public MetaStateMissionRestart(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateMissionRestartPrototype;
        }

        public override void OnApply()
        {
            if (_proto.MissionsToRestart.IsNullOrEmpty()) return;

            var manager = Region?.MissionManager;
            if (manager == null) return;

            foreach (var missionRef in _proto.MissionsToRestart)
            {
                var mission = manager.MissionByDataRef(missionRef);
                mission?.RestartMission();
            }
        }
    }
}
