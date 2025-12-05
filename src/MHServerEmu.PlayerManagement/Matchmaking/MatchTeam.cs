namespace MHServerEmu.PlayerManagement.Matchmaking
{
    public readonly struct MatchTeam : IComparable<MatchTeam>
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
            return $"[{Index}] {GetUsedCount()} / {PlayerLimit}";
        }

        public int GetUsedCount()
        {
            int count = 0;

            foreach ((RegionRequestGroup group, bool isReady) in Groups)
                count += group.GetCountNotInWaitlist();

            return count;
        }

        public int GetAvailableCount()
        {
            int availableCount = PlayerLimit - GetUsedCount();
            return Math.Clamp(availableCount, 0, PlayerLimit);
        }

        public bool IsFull()
        {
            return GetAvailableCount() <= 0;
        }

        public bool HasGroup(RegionRequestGroup group)
        {
            foreach ((RegionRequestGroup itGroup, _) in Groups)
            {
                if (itGroup == group)
                    return true;
            }

            return false;
        }

        private float GetAvailableRatio()
        {
            return GetAvailableCount() / (float)PlayerLimit;
        }

        public int CompareTo(MatchTeam other)
        {
            // Sort in descending order (i.e. teams with the highest available counts go first)
            float thisRatio = GetAvailableRatio();
            float otherRatio = other.GetAvailableRatio();
            return -thisRatio.CompareTo(otherRatio);
        }
    }
}
