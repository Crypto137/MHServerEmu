using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Achievements
{
    public class AchievementManager
    {
        public Player Owner { get; }
        public AchievementState AchievementState { get => Owner.AchievementState; }

        public AchievementManager(Player owner)
        {
            Owner = owner;
        }

        public void UpdateScore()
        {
            uint score = AchievementState.GetTotalStats().Score;
            Owner.Properties[PropertyEnum.AchievementScore] = score;

            var avatar = Owner.CurrentAvatar;
            if (avatar == null) return;
            avatar.Properties[PropertyEnum.AchievementScore] = score;
        }

        public void OnScoringEvent(in ScoringEvent scoringEvent)
        {
            // TODO update
        }
    }
}
