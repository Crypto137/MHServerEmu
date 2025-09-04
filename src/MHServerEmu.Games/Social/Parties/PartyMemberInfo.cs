namespace MHServerEmu.Games.Social.Parties
{
    public class PartyMemberInfo
    {
        public ulong PlayerDbId { get; private set; }
        public string PlayerName { get; private set; }
        // repeated uint64 boosts
        // optional uint64 consoleAccountId
        // optional uint64 secondaryConsoleAccountId
        // optional string secondaryPlayerName
    }
}
