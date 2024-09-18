using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Common
{
    public static class ArchiveExtensions
    {
        /// <summary>
        /// Returns the replication policy for this <see cref="Archive"/> as an <see cref="AOINetworkPolicyValues"/> enum.
        /// </summary>
        public static AOINetworkPolicyValues GetReplicationPolicyEnum(this Archive archive)
        {
            return (AOINetworkPolicyValues)archive.ReplicationPolicy;
        }

        public static bool HasReplicationPolicy(this Archive archive, AOINetworkPolicyValues replicationPolicy)
        {
            return ((AOINetworkPolicyValues)archive.ReplicationPolicy).HasFlag(replicationPolicy);
        }
    }
}
