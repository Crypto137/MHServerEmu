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
            foreach (Player player in Mission.GetParticipants())
                player.SetActiveChapter(chapterRef);
        }
    }
}
