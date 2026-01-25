using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionSetActiveChapter : MissionAction
    {
        private MissionActionSetActiveChapterPrototype _proto;
        public MissionActionSetActiveChapter(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // CH00RaftTutorial
            _proto = prototype as MissionActionSetActiveChapterPrototype;
        }

        public override void Run()
        {
            var chapterRef = _proto.Chapter;
            if (chapterRef == PrototypeId.Invalid) return;

            using var participantsHandle = ListPool<Player>.Instance.Get(out List<Player> participants);
            if (Mission.GetParticipants(participants))
            {
                foreach (Player player in participants)
                    player.SetActiveChapter(chapterRef);
            }
        }
    }
}
