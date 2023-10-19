using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.GameServer.Common;
using MHServerEmu.GameServer.Entities;

namespace MHServerEmu.GameServer.MetaGame
{
    public class PvP : MetaGame
    {
        public ReplicatedInt32 Team1 { get; set; }
        public ReplicatedInt32 Team2 { get; set; }

        public PvP(EntityBaseData baseData, ByteString archiveData) : base(baseData, archiveData) { }

        public PvP(EntityBaseData baseData) : base(baseData) { }

        protected override void Decode(CodedInputStream stream)
        {
            base.Decode(stream);

            Team1 = new(stream);
            Team2 = new(stream);
        }

        public override void Encode(CodedOutputStream stream)
        {
            base.Encode(stream);

            Team1.Encode(stream);
            Team2.Encode(stream);
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            sb.AppendLine($"Team1: {Team1}");
            sb.AppendLine($"Team2: {Team2}");
        }
    }
}
