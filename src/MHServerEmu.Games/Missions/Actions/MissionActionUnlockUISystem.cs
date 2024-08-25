using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionUnlockUISystem : MissionAction
    {
        private MissionActionUnlockUISystemPrototype _proto;
        public MissionActionUnlockUISystem(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // TimesBehaviorController
            _proto = prototype as MissionActionUnlockUISystemPrototype;
        }

        public override void Run()
        {
            var uiGlobalsProto = GameDatabase.UIGlobalsPrototype;
            if (uiGlobalsProto == null || uiGlobalsProto.UISystemLockList.IsNullOrEmpty()) return;

            foreach (var uiSystemLockRef in uiGlobalsProto.UISystemLockList)
            {
                var uiSystemLockProto = GameDatabase.GetPrototype<UISystemLockPrototype>(uiSystemLockRef);
                if (uiSystemLockProto != null && uiSystemLockProto.UISystem == _proto.UISystem)
                {
                    foreach (var player in Mission.GetParticipants())
                        player.Properties[PropertyEnum.UISystemLock, uiSystemLockRef] = 1;

                    return;
                }
            }
        }
    }
}
