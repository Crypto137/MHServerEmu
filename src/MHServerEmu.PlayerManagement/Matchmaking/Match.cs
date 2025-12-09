using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.PlayerManagement.Regions;

namespace MHServerEmu.PlayerManagement.Matchmaking
{
    public class Match
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly List<MatchTeam> _teams = new();

        public ulong Id { get; }
        public RegionRequestQueue Queue { get; }
        public RegionRequestQueueParams QueueParams { get; }

        public bool IsBypass { get => QueueParams.IsBypass; }

        public RegionHandle Region { get; private set; }

        public Match(ulong id, RegionRequestQueue queue, in RegionRequestQueueParams queueParams)
        {
            Id = id;
            Queue = queue;
            QueueParams = queueParams;

            int[] teamLimits = Queue.Prototype.TeamLimits;
            if (teamLimits.HasValue())
            {
                for (int i = 0; i < teamLimits.Length; i++)
                {
                    MatchTeam team = new(i, teamLimits[i]);
                    _teams.Add(team);
                }
            }
            else
            {
                MatchTeam team = new(-1, Queue.Prototype.PlayerLimit);
                _teams.Add(team);
            }
        }

        public override string ToString()
        {
            return $"{Queue.PrototypeDataRef.GetNameFormatted()}[{Id}][{QueueParams}]";
        }

        public bool IsFull()
        {
            foreach (MatchTeam team in _teams)
            {
                if (team.IsFull() == false)
                    return false;
            }

            return true;
        }

        public bool IsEmpty()
        {
            foreach (MatchTeam team in _teams)
            {
                if (team.Groups.Count > 0)
                    return false;
            }

            return true;
        }

        public int GetAvailableCount()
        {
            int count = 0;

            foreach (MatchTeam team in _teams)
                count += team.GetAvailableCount();

            return count;
        }

        public bool IsLookingForMore()
        {
            if (IsBypass)
                return false;

            if (IsFull())
                return false;

            // NOTE: This prevents adding players mid-match from the queue.
            if (Region != null && Region.IsAccessible(null, false) == false)
                return false;

            if (IsEmpty() && (Queue.Prototype.QueueDoNotWaitToFull == false || Region == null))
                return false;

            return true;
        }

        public bool IsReady()
        {
            if (IsFull() == false && IsBypass == false && Queue.Prototype.QueueDoNotWaitToFull == false)
                return false;

            foreach (MatchTeam team in _teams)
            {
                foreach ((RegionRequestGroup group, _) in team.Groups)
                {
                    if (group.IsReady == false)
                        return false;
                }
            }

            return true;
        }

        public bool HasGroup(RegionRequestGroup group)
        {
            foreach (MatchTeam team in _teams)
            {
                if (team.HasGroup(group))
                    return true;
            }

            return false;
        }

        public MatchTeam? GetTeamForGroup(RegionRequestGroup group)
        {
            foreach (MatchTeam team in _teams)
            {
                foreach ((RegionRequestGroup itGroup, _) in team.Groups)
                {
                    if (itGroup == group)
                        return team;
                }
            }

            return null;
        }

        public bool AddGroupsFromQueue(bool clearTeamsIfNotReady)
        {
            bool hasGroups = true;

            while (IsFull() == false && hasGroups)
                hasGroups = AddNextGroupFromQueue();

            // Always ready if we the match has already started (i.e. it has a region).
            bool isReady = Region != null;

            // If the match hasn't started yet, wait for the group to get full if needed.
            if (isReady == false)
            {
                isReady = Queue.Prototype.QueueDoNotWaitToFull || IsFull();

                if (isReady == false && clearTeamsIfNotReady)
                {
                    ClearTeams();
                    return false;
                }
            }

            if (isReady)
            {
                foreach (MatchTeam team in _teams)
                {
                    for (int i = 0; i < team.Groups.Count; i++)
                    {
                        (RegionRequestGroup group, bool groupIsAdded) = team.Groups[i];
                        if (groupIsAdded)
                            continue;

                        team.Groups[i] = (group, true);
                        group.SetMatch(this);
                    }
                }
            }

            return isReady;
        }

        private bool AddNextGroupFromQueue()
        {
            MatchTeam? nullableTeam = GetAvailableTeam();
            if (nullableTeam == null)
                return false;

            MatchTeam team = nullableTeam.Value;
            int count = team.GetAvailableCount();

            if (count > 0)
            {
                // Search buckets highest to lowest until we find a valid group.
                for (int i = count; i > 0; i--)
                {
                    List<RegionRequestGroup> bucket = Queue.GetBucket(QueueParams, i);
                    if (bucket == null || bucket.Count == 0)
                        continue;

                    foreach (RegionRequestGroup group in bucket)
                    {
                        if (HasGroup(group))
                            continue;

                        team.Groups.Add((group, false));
                        return true;
                    }
                }
            }

            // No groups added in this iteration.
            return false;
        }

        public void AddBypassGroup(RegionRequestGroup group)
        {
            MatchTeam? team = GetAvailableTeam();
            if (team == null)
                return;

            if (HasGroup(group))
                return;

            team.Value.Groups.Add((group, true));
            group.SetMatch(this);
        }

        public void OnGroupUpdate(RegionRequestGroup group, bool memberCountChanged)
        {
            MatchTeam? nullableTeam = GetTeamForGroup(group);
            if (nullableTeam == null)
            {
                Logger.Warn("OnGroupUpdate(): nullableTeam == null");
                return;
            }

            MatchTeam team = nullableTeam.Value;

            // Remove groups that have become empty
            if (memberCountChanged && group.Count == 0)
            {
                var groups = team.Groups;
                for (int i = 0; i < groups.Count; i++)
                {
                    (RegionRequestGroup itGroup, _) = groups[i];
                    if (itGroup == group)
                    {
                        groups.RemoveAt(i);
                        break;
                    }
                }
            }

            // Try filling the team with waitlisted players from existing groups.
            if (team.IsFull() == false)
            {
                foreach ((RegionRequestGroup itGroup, _) in team.Groups)
                {
                    if (team.GetAvailableCount() <= 0)
                        break;

                    itGroup.OnMatchRegionAccessChange(Region);
                }
            }

            if (Region == null)
            {
                if (IsFull() == false && IsBypass == false)
                    AddGroupsFromQueue(Queue.Prototype.QueueDoNotWaitToFull == false);

                if (IsReady())
                {
                    CreateRegion();

                    foreach (MatchTeam itTeam in _teams)
                    {
                        foreach ((RegionRequestGroup itGroup, _) in itTeam.Groups)
                            itGroup.SetState(RegionRequestGroup.InMatchState.Instance);
                    }
                }
            }
            else if (IsBypass == false)
            {
                foreach (MatchTeam itTeam in _teams)
                {
                    foreach ((RegionRequestGroup itGroup, _) in itTeam.Groups)
                    {
                        if (itGroup.IsReady)
                            itGroup.SetState(RegionRequestGroup.InMatchState.Instance);
                    }
                }
            }

            Queue.UpdateMatchSortOrder(this);

            if (IsFull() == false)
                Queue.UpdateQueue(group.QueueParams);
        }

        public void OnRegionAccessChanged(RegionHandle region)
        {
            if (region != Region)
            {
                Logger.Warn("OnRegionAccessChanged(): region != Region");
                return;
            }

            foreach (MatchTeam team in _teams)
            {
                foreach ((RegionRequestGroup group, _) in team.Groups)
                    group?.OnMatchRegionAccessChange(region);
            }
        }

        public void CreateRegion()
        {
            Logger.Info($"Creating region for match {this}");

            PrototypeId regionProtoRef = Queue.PrototypeDataRef;

            NetStructCreateRegionParams createRegionParams = NetStructCreateRegionParams.CreateBuilder()
                .SetLevel(0)
                .SetDifficultyTierProtoId((ulong)QueueParams.DifficultyTierRef)
                .SetGameStateId((ulong)QueueParams.MetaStateRef)
                .SetMatchNumber(Id)
                .Build();

            Region = PlayerManagerService.Instance.WorldManager.CreateMatchRegion(regionProtoRef, createRegionParams);
        }

        private MatchTeam? GetAvailableTeam()
        {
            if (_teams.Count > 0)
            {
                _teams.Sort();
                MatchTeam team = _teams[0];

                if (team.IsFull() == false)
                    return team;
            }

            return null;
        }

        private void ClearTeams()
        {
            foreach (MatchTeam team in _teams)
            {
                foreach ((RegionRequestGroup group, _) in team.Groups)
                {
                    if (IsBypass == false)
                        group.SetState(RegionRequestGroup.WaitingInQueueState.Instance);

                    group.ClearMatch();
                }

                team.Groups.Clear();
            }
        }
    }
}
