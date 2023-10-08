using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.Common;
using MHServerEmu.GameServer.GameData.Prototypes;

namespace MHServerEmu.GameServer.GameData.Gpak.JsonOutput
{
    public class NaviPatchPrototypeJson
    {
        public Vector3[] Points { get; }
        public NaviPatchEdgePrototypeJson[] Edges { get; }

        public NaviPatchPrototypeJson(NaviPatchPrototype prototype)
        {
            Points = prototype.Points;
            Edges = prototype.Edges.Select(edge => new NaviPatchEdgePrototypeJson(edge)).ToArray();
        }
    }

    public class NaviPatchEdgePrototypeJson
    {
        public uint ProtoNameHash { get; }
        public uint Index0 { get; }
        public uint Index1 { get; }
        public string Flags0 { get; }
        public string Flags1 { get; }

        public NaviPatchEdgePrototypeJson(NaviPatchEdgePrototype prototype)
        {
            ProtoNameHash = prototype.ProtoNameHash;
            Index0 = prototype.Index0;
            Index1 = prototype.Index1;
            Flags0 = prototype.Flags0.ToHexString();
            Flags1 = prototype.Flags1.ToHexString();
        }
    }
}
