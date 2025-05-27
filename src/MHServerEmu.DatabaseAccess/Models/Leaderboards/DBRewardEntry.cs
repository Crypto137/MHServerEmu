using MHServerEmu.Core.System.Time;

namespace MHServerEmu.DatabaseAccess.Models.Leaderboards
{
    public class DBRewardEntry
    {
        public long LeaderboardId { get; set; }
        public long InstanceId { get; set; }
        public long RewardId { get; set; }
        public long ParticipantId { get; set; }
        public int Rank { get; set; }
        public long CreationDate { get; set; }
        public long RewardedDate { get; set; }

        public DBRewardEntry() { }

        public DBRewardEntry(long leaderboardId, long instanceId, long rewardId, long participantId, int rank)
        {
            LeaderboardId = leaderboardId;
            InstanceId = instanceId;
            RewardId = rewardId;
            ParticipantId = participantId;
            Rank = rank;

            CreationDate = Clock.UtcNowTimestamp;
            RewardedDate = 0;
        }

        public void UpdateRewardedDate()
        {
            RewardedDate = Clock.UtcNowTimestamp;
        }
    }
}
