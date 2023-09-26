using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.Common;

namespace MHServerEmu.GameServer.Missions
{
    public class ObjectiveGraphNode
    {
        public ulong Id { get; set; }
        public Vector3 Position { get; set; }
        public ulong[] Areas { get; set; }
        public ulong[] Cells { get; set; }
        public int Type { get; set; }

        // Put this here for now until we know what to do with it
        public int Index { get; set; }

        public ObjectiveGraphNode(CodedInputStream stream)
        {
            Id = stream.ReadRawVarint64();
            Position = new(stream);

            Areas = new ulong[stream.ReadRawVarint64()];
            for (int i = 0; i < Areas.Length; i++)
                Areas[i] = stream.ReadRawVarint64();

            Cells = new ulong[stream.ReadRawVarint64()];
            for (int i = 0; i < Cells.Length; i++)
                Cells[i] = stream.ReadRawVarint64();

            Type = stream.ReadRawInt32();

            Index = stream.ReadRawInt32();
        }

        public ObjectiveGraphNode() { }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint64(Id);
                cos.WriteRawBytes(Position.Encode());

                cos.WriteRawVarint64((ulong)Areas.Length);
                for (int i = 0; i < Areas.Length; i++)
                    cos.WriteRawVarint64(Areas[i]);

                cos.WriteRawVarint64((ulong)Cells.Length);
                for (int i = 0; i < Cells.Length; i++)
                    cos.WriteRawVarint64(Cells[i]);

                cos.WriteRawInt32(Type);

                cos.WriteRawInt32(Index);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Id: {Id}");
            sb.AppendLine($"Position: {Position}");
            for (int i = 0; i < Areas.Length; i++) sb.AppendLine($"Area{i}: {Areas[i]}");
            for (int i = 0; i < Cells.Length; i++) sb.AppendLine($"Cell{i}: {Cells[i]}");
            sb.AppendLine($"Type: {Type}");
            sb.AppendLine($"Index: {Index}");
            return sb.ToString();
        }
    }
}
