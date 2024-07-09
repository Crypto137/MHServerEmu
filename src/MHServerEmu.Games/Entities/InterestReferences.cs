using System.Text;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Entities
{
    /// <summary>
    /// Tracks <see cref="AOINetworkPolicyValues"/> for all players who are interested in an <see cref="Entity"/>.
    /// </summary>
    public class InterestReferences
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly HashSet<ulong> _interestedPlayerIds = new();
        private readonly int[] _accumulatedPolicyCounts = new int[8];

        public int PlayerCount { get => _interestedPlayerIds.Count; }
        public bool IsEmpty { get => PlayerCount == 0 && GetInterestedPoliciesUnion() == AOINetworkPolicyValues.AOIChannelNone; }

        public bool Track(Entity entity, ulong playerId, InterestTrackOperation operation, AOINetworkPolicyValues gainedPolicies, AOINetworkPolicyValues lostPolicies)
        {
            if (entity == null) return Logger.WarnReturn(false, "Track(): entity == null");
            if (playerId == Entity.InvalidId) return Logger.WarnReturn(false, "Track(): playerId == Entity.InvalidId");

            if (operation == InterestTrackOperation.Add)
            {
                if (_interestedPlayerIds.Contains(playerId))
                    return Logger.WarnReturn(false, $"Track(): Player id {playerId} is already found in interest references for entity {entity} during add operation");

                if (lostPolicies != AOINetworkPolicyValues.AOIChannelNone)
                    return Logger.WarnReturn(false, $"Track(): Expected no lost policies ({lostPolicies}) when scoring entity {entity} for add interest for player id {playerId}");

                // Add the player and increment policy counts
                _interestedPlayerIds.Add(playerId);
                AccumulatePolicyCounts(entity, playerId, gainedPolicies, false);
            }
            else if (operation == InterestTrackOperation.Remove)
            {
                if (_interestedPlayerIds.Contains(playerId) == false)
                    return Logger.WarnReturn(false, $"Track(): Player id {playerId} is not found in the interest scorecord for entity {entity} during remove operation");

                if (gainedPolicies != AOINetworkPolicyValues.AOIChannelNone)
                    return Logger.WarnReturn(false, $"Track(): Expected no gained policies ({gainedPolicies}) when scoring entity {entity} for remove interest for player id {playerId}");

                // Remove the player and decremenet policy counts
                _interestedPlayerIds.Remove(playerId);
                AccumulatePolicyCounts(entity, playerId, lostPolicies, true);
            }
            else
            {
                if (operation != InterestTrackOperation.Modify)
                    return Logger.WarnReturn(false, $"Track(): Expected modify operation for entity {entity} interest tracking with player id {playerId}");

                if (_interestedPlayerIds.Contains(playerId) == false)
                    return Logger.WarnReturn(false, $"Track(): Player id {playerId} not found in interest references for modify operation for entity {entity}");

                // Increment and decrement policy counts for the previously added player
                AccumulatePolicyCounts(entity, playerId, gainedPolicies, false);
                AccumulatePolicyCounts(entity, playerId, lostPolicies, true);
            }

            return true;
        }

        public AOINetworkPolicyValues GetInterestedPoliciesUnion()
        {
            AOINetworkPolicyValues interestPolicies = AOINetworkPolicyValues.AOIChannelNone;

            for (int i = 0; i < _accumulatedPolicyCounts.Length; i++)
            {
                if (_accumulatedPolicyCounts[i] > 0)
                    interestPolicies |= (AOINetworkPolicyValues)(1 << i);
            }

            return interestPolicies;
        }

        public bool IsAnyPlayerInterested(AOINetworkPolicyValues channel)
        {
            int index = ((ulong)channel).HighestBitSet();
            return _accumulatedPolicyCounts[index] > 0;
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            sb.Append("Player IDs: [");

            foreach (ulong playerId in _interestedPlayerIds)
                sb.Append(playerId).Append(',');

            if (_interestedPlayerIds.Count > 0)
                sb.Length--;

            sb.Append("] ");

            AOINetworkPolicyValues policyUnion = GetInterestedPoliciesUnion();
            sb.Append($"Policy Union: [{policyUnion}]");

            return sb.ToString();
        }

        private bool AccumulatePolicyCounts(Entity entity, ulong playerId, AOINetworkPolicyValues interestPolicies, bool remove)
        {
            if (entity == null) return Logger.WarnReturn(false, "AccumulatePolicyCounts(): entity == null");
            if (playerId == Entity.InvalidId) return Logger.WarnReturn(false, "AccumulatePolicyCounts(): playerId == Entity.InvalidId");

            int delta = remove ? -1 : 1;
            int policyBits = (int)interestPolicies;

            // Shift until we apply all policy bits
            for (int i = 0; policyBits != 0; i++)
            {
                if ((policyBits & 0x1) != 0)
                    _accumulatedPolicyCounts[i] += delta;

                policyBits >>= 1;
            }

            return true;
        }
    }
}
