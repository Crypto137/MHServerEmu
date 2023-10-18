using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.GameServer.Common;

namespace MHServerEmu.GameServer.Entities
{
    public class PvP : MetaGame
    {
        public ReplicatedInt32 Team1 { get; set; }
        public ReplicatedInt32 Team2 { get; set; }

        public PvP(EntityBaseData baseData, byte[] archiveData) : base(baseData, archiveData) {
            CodedInputStream stream = CodedInputStream.CreateInstance(archiveData);

            DecodeEntityFields(stream);

            Name = new(stream);
            Team1 = new(stream);
            Team2 = new(stream);
        }

        public PvP(EntityBaseData baseData) : base(baseData) { }
        public override byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                // Encode
                EncodeEntityFields(cos);

                cos.WriteRawBytes(Name.Encode());
                cos.WriteRawBytes(Team1.Encode());
                cos.WriteRawBytes(Team2.Encode());

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            WriteEntityString(sb);

            sb.AppendLine($"Name: {Name}");
            sb.AppendLine($"Team1: {Team1}");
            sb.AppendLine($"Team2: {Team2}");

            return sb.ToString();
        }
    }
}
