using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Games.Missions
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

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64(Id);
            Position.Encode(stream);

            stream.WriteRawVarint64((ulong)Areas.Length);
            for (int i = 0; i < Areas.Length; i++)
                stream.WriteRawVarint64(Areas[i]);

            stream.WriteRawVarint64((ulong)Cells.Length);
            for (int i = 0; i < Cells.Length; i++)
                stream.WriteRawVarint64(Cells[i]);

            stream.WriteRawInt32(Type);

            stream.WriteRawInt32(Index);
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
