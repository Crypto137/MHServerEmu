using MHServerEmu.Games.GameData;

namespace MHServerEmu.Leaderboards
{
    public class LeaderboardEntry
    {
        public uint GameId { get; set; }
        public string Name { get; set; }
        public LocaleStringId NameId { get; set; }
        public uint Score { get; set; }

        public Gazillion.LeaderboardEntry ToProtobuf()
        {
            var entryBuilder = Gazillion.LeaderboardEntry.CreateBuilder()
                .SetGameId(GameId)
                .SetName(Name)
                .SetScore(Score);

            if (NameId != LocaleStringId.Blank)
                entryBuilder.SetNameId((ulong)NameId);

            return entryBuilder.Build();
        }
    }
}
