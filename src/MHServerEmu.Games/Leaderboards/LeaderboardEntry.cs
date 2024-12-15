using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Leaderboards
{
    public class LeaderboardEntry
    {
        public uint GameId;
        public string Name;
        public LocaleStringId NameId;
        public uint Score;

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
