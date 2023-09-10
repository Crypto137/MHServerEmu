using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.Common;

namespace MHServerEmu.GameServer.Entities.Locomotion
{
    public class LocomotionPathNode
    {
        public Vector3 Vertex { get; set; }
        public int VertexSideRadius { get; set; }  // zigzag int

        public LocomotionPathNode(CodedInputStream stream)
        {
            Vertex = new(stream.ReadRawFloat(3), stream.ReadRawFloat(3), stream.ReadRawFloat(3));
            VertexSideRadius = stream.ReadRawInt32();
        }

        public LocomotionPathNode(Vector3 vertex, int vertexSideRadius)
        {
            Vertex = vertex;
            VertexSideRadius = vertexSideRadius;
        }

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawBytes(Vertex.Encode());
                stream.WriteRawInt32(VertexSideRadius);

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public override string ToString()
        {
            using (MemoryStream memoryStream = new())
            using (StreamWriter streamWriter = new(memoryStream))
            {
                streamWriter.WriteLine($"Vertex: {Vertex}");
                streamWriter.WriteLine($"VertexSideRadius: 0x{VertexSideRadius.ToString("X")}");

                streamWriter.Flush();
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
