using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.PlayerManagement.Matchmaking
{
    /// <summary>
    /// Represents a match queue for a particular <see cref="RegionPrototype"/>.
    /// </summary>
    public class RegionRequestQueue
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly SortedSet<Match> _matches = new(MatchLoadComparer.Instance);

        private ulong _currentMatchNumber = 0;

        public RegionPrototype Prototype { get; }
        public PrototypeId PrototypeDataRef { get => Prototype.DataRef; }

        public RegionRequestQueue(RegionPrototype regionProto)
        {
            Prototype = regionProto;
        }

        public void AddBypassGroup(RegionRequestGroup group)
        {
            Match match = CreateMatch(group.QueueParams);
            match.AddBypassGroup(group);
        }

        public void UpdateGroupBucket(RegionRequestGroup group)
        {

        }

        public void UpdateMatchSortOrder(Match match)
        {
            if (_matches.Remove(match) == false)
                return;

            // QueueDoNotWaitToFull regions can stay in the queue even if they don't have any players in them.
            if (match.IsEmpty() == false || (Prototype.QueueDoNotWaitToFull && match.Region != null))
                _matches.Add(match);
            else
                Logger.Debug($"Match {match.Id} removed from queue for {Prototype}");
        }

        public Match GetMatch(ulong matchNumber)
        {
            foreach (Match match in _matches)
            {
                if (match.Id == matchNumber)
                    return match;
            }

            return null;
        }

        private Match CreateMatch(in RegionRequestQueueParams queueParams)
        {
            ulong matchNumber = ++_currentMatchNumber;
            Match match = new(matchNumber, this, queueParams);
            _matches.Add(match);
            return match;
        }

        private class MatchLoadComparer : IComparer<Match>
        {
            public static MatchLoadComparer Instance { get; } = new();

            private MatchLoadComparer() { }

            public int Compare(Match x, Match y)
            {
                int xCount = GetLoadValue(x);
                int yCount = GetLoadValue(y);

                return xCount.CompareTo(yCount);
            }

            private static int GetLoadValue(Match match)
            {
                // Prioritize matches that are most close to being full, put locked matches at the end.
                if (match.Region != null)
                {
                    RegionPlayerAccessVar access = match.Region.PlayerAccess;
                    if (access != RegionPlayerAccessVar.eRPA_Open && access != RegionPlayerAccessVar.eRPA_InviteOnly)
                        return int.MaxValue;
                }

                return match.GetAvailableCount();
            }
        }
    }
}
