using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
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

        private readonly Dictionary<RegionRequestQueueParams, Dictionary<int, List<RegionRequestGroup>>> _bucketsByParams = new();
        private readonly SortedSet<Match> _matches = new(MatchLoadComparer.Instance);
        private readonly Dictionary<RegionRequestQueueParams, Match> _pendingMatches = new();

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

        public void UpdateQueue(in RegionRequestQueueParams queueParams)
        {
            if (queueParams.IsBypass)
                return;

            bool hasGroups = true;

            // Try to fill existing matches.
            List<Match> lfmMatches = ListPool<Match>.Instance.Get();

            foreach (Match match in _matches)
            {
                if (match.IsLookingForMore())
                    lfmMatches.Add(match);
            }

            foreach (Match match in lfmMatches)
            {
                match.AddGroupsFromQueue(false);
                UpdateMatchSortOrder(match);

                hasGroups = HasBucketedGroupsForParams(queueParams);
                if (hasGroups == false)
                    break;
            }

            ListPool<Match>.Instance.Return(lfmMatches);

            // Set up new matches.
            if (hasGroups)
            {
                if (_pendingMatches.TryGetValue(queueParams, out Match match) == false)
                {
                    match = CreateMatch(queueParams);
                    _pendingMatches.Add(queueParams, match);
                }

                if (match.AddGroupsFromQueue(true))
                    _pendingMatches.Remove(queueParams);
            }
        }

        public bool UpdateGroupBucket(RegionRequestGroup group)
        {
            if (group == null) return Logger.WarnReturn(false, "UpdateGroupBucket(): group == null");

            // Remove from the current bucket.
            List<RegionRequestGroup> oldBucket = group.Bucket;
            if (oldBucket != null)
            {
                if (oldBucket.Remove(group))
                    group.Bucket = null;
                else
                    return Logger.WarnReturn(false, $"UpdateGroupBucket(): Group {group} is not in the bucket assigned to it.");
            }

            // Do not rebucket if the group is empty now.
            int memberCount = group.GetCountNotInWaitlist();
            if (memberCount == 0)
                return false;

            RegionRequestQueueParams queueParams = group.QueueParams;

            List<RegionRequestGroup> newBucket = GetBucket(queueParams, memberCount);
            if (newBucket == null)
                return Logger.WarnReturn(false, $"UpdateGroupBucket(): No bucket found for region=[{Prototype}], params=[{queueParams}], memberCount=[{memberCount}]");

            newBucket.Add(group);
            group.Bucket = newBucket;

            if (oldBucket != newBucket)
                UpdateQueue(queueParams);

            return true;
        }

        public void UpdateMatchSortOrder(Match match)
        {
            if (_matches.Remove(match) == false)
                return;

            // QueueDoNotWaitToFull regions can stay in the queue even if they don't have any players in them.
            if (match.IsEmpty() == false || (Prototype.QueueDoNotWaitToFull && match.Region != null))
                _matches.Add(match);
            else
                Logger.Info($"Removed match {match}");
        }

        public List<RegionRequestGroup> GetBucket(in RegionRequestQueueParams queueParams, int memberCount)
        {
            Dictionary<int, List<RegionRequestGroup>> buckets = GetBucketsForParams(queueParams);
            if (buckets == null)
                return null;

            if (buckets.TryGetValue(memberCount, out List<RegionRequestGroup> bucket) == false)
                return null;

            return bucket;
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

        private Dictionary<int, List<RegionRequestGroup>> GetBucketsForParams(in RegionRequestQueueParams queueParams)
        {
            if (_bucketsByParams.TryGetValue(queueParams, out Dictionary<int, List<RegionRequestGroup>> buckets) == false)
            {
                buckets = new();

                int playerLimit = Prototype.QueueGroupLimit;
                for (int i = 1; i <= playerLimit; i++)
                    buckets.Add(i, new());

                _bucketsByParams.Add(queueParams, buckets);
            }

            return buckets;
        }

        private bool HasBucketedGroupsForParams(in RegionRequestQueueParams queueParams)
        {
            Dictionary<int, List<RegionRequestGroup>> buckets = GetBucketsForParams(queueParams);
            
            if (buckets != null)
            {
                foreach (var kvp in buckets)
                {
                    if (kvp.Value.Count > 0)
                        return true;
                }
            }

            return false;
        }

        private Match CreateMatch(in RegionRequestQueueParams queueParams)
        {
            ulong matchNumber = ++_currentMatchNumber;
            Match match = new(matchNumber, this, queueParams);
            _matches.Add(match);

            Logger.Info($"Created match {match}");

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
