using System.Text;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions.ObjectiveGraphs;
using MHServerEmu.Games.UI;

namespace MHServerEmu.Games.Regions
{
    public class RegionArchive : ISerialize
    {
        public AOINetworkPolicyValues ReplicationPolicy { get; set; }
        public ReplicatedPropertyCollection Properties { get; set; }
        public MissionManager MissionManager { get; set; }
        public UIDataProvider UIDataProvider { get; set; }
        public ObjectiveGraph ObjectiveGraph { get; set; }

        public RegionArchive(ulong replicationId = 0)
        {
            ReplicationPolicy = AOINetworkPolicyValues.DefaultPolicy;
            Properties = new(replicationId);
            MissionManager = new();
            UIDataProvider = new();
            ObjectiveGraph = new(null, null);
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            success &= Properties.Serialize(archive);
            success &= MissionManager.Serialize(archive);
            success &= UIDataProvider.Serialize(archive);
            success &= ObjectiveGraph.Serialize(archive);

            return success;
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            sb.AppendLine($"{nameof(ReplicationPolicy)}: {ReplicationPolicy}");
            sb.AppendLine($"{nameof(Properties)}: {Properties}");
            sb.AppendLine($"{nameof(MissionManager)}: {MissionManager}");
            sb.AppendLine($"{nameof(UIDataProvider)}: {UIDataProvider}");
            sb.AppendLine($"{nameof(ObjectiveGraph)}: {ObjectiveGraph}");

            return sb.ToString();
        }
    }
}
