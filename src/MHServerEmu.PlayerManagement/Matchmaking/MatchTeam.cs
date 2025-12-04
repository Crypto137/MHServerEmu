namespace MHServerEmu.PlayerManagement.Matchmaking
{
    public readonly struct MatchTeam
    {
        public int Index { get; }
        public int PlayerLimit { get; }
        public List<(RegionRequestGroup, bool)> Groups { get; }

        public MatchTeam(int index, int playerLimit)
        {
            Index = index;
            PlayerLimit = playerLimit;
            Groups = new();
        }

        public override string ToString()
        {
            return $"[{Index}] {GetCount()} / {PlayerLimit}";
        }

        public int GetCount()
        {
            // TODO
            return 0;
        }
    }
}
