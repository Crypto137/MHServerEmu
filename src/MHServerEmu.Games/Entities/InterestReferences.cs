using MHServerEmu.Core.Collections;
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
        private const int NumInterestPolicies = 8;

        private readonly HashSet<ulong> _interestedPlayerIds = new();
        private InlineArray8<int> _accumulatedPolicyCounts;

        public int PlayerCount { get => _interestedPlayerIds.Count; }
        public bool IsEmpty { get => PlayerCount == 0 && GetInterestedPoliciesUnion() == AOINetworkPolicyValues.AOIChannelNone; }

        public InterestReferences() { }

        public override string ToString()
        {
            return $"Player IDs: [{string.Join(',', _interestedPlayerIds)}] Policy Union: [{GetInterestedPoliciesUnion()}]";
        }

        public HashSet<ulong>.Enumerator GetEnumerator()
        {
            return _interestedPlayerIds.GetEnumerator();
        }

        public bool Track(Entity entity, ulong playerId, InterestTrackOperation operation, AOINetworkPolicyValues gainedPolicies, AOINetworkPolicyValues lostPolicies)
        {
            if (!Verify.IsNotNull(entity)) return false;
            if (!Verify.IsTrue(playerId != Entity.InvalidId)) return false;

            if (operation == InterestTrackOperation.Add)
            {
                if (!Verify.IsTrue(_interestedPlayerIds.Contains(playerId) == false, $"Player id {playerId} is already found in interest references for entity {entity} during add operation"))
                    return false;

                if (!Verify.IsTrue(lostPolicies == AOINetworkPolicyValues.AOIChannelNone, $"Expected no lost policies ({lostPolicies}) when scoring entity {entity} for add interest for player id {playerId}"))
                    return false;

                // Add the player and increment policy counts
                _interestedPlayerIds.Add(playerId);
                AccumulatePolicyCounts(entity, playerId, gainedPolicies, false);
            }
            else if (operation == InterestTrackOperation.Remove)
            {
                if (!Verify.IsTrue(_interestedPlayerIds.Contains(playerId), $"Player id {playerId} is not found in the interest scorecord for entity {entity} during remove operation"))
                    return false;

                if (!Verify.IsTrue(gainedPolicies == AOINetworkPolicyValues.AOIChannelNone, $"Expected no gained policies ({gainedPolicies}) when scoring entity {entity} for remove interest for player id {playerId}"))
                    return false;

                // Remove the player and decremenet policy counts
                _interestedPlayerIds.Remove(playerId);
                AccumulatePolicyCounts(entity, playerId, lostPolicies, true);
            }
            else
            {
                if (!Verify.IsTrue(operation == InterestTrackOperation.Modify, $"Expected modify operation for entity {entity} interest tracking with player id {playerId}"))
                    return false;

                if (!Verify.IsTrue(_interestedPlayerIds.Contains(playerId), $"Player id {playerId} not found in interest references for modify operation for entity {entity}"))
                    return false;

                // Increment and decrement policy counts for the previously added player
                AccumulatePolicyCounts(entity, playerId, gainedPolicies, false);
                AccumulatePolicyCounts(entity, playerId, lostPolicies, true);
            }

            return true;
        }

        public AOINetworkPolicyValues GetInterestedPoliciesUnion()
        {
            AOINetworkPolicyValues interestPolicies = AOINetworkPolicyValues.AOIChannelNone;

            for (int i = 0; i < NumInterestPolicies; i++)
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

        public bool IsPlayerInterested(Player player)
        {
            return _interestedPlayerIds.Contains(player.Id);
        }

        private bool AccumulatePolicyCounts(Entity entity, ulong playerId, AOINetworkPolicyValues interestPolicies, bool remove)
        {
            if (!Verify.IsNotNull(entity)) return false;
            if (!Verify.IsTrue(playerId != Entity.InvalidId)) return false;

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
