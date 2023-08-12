using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common;

namespace MHServerEmu.GameServer.Common
{
    public class NaviVector
    {
        public Vector3 Vector { get; set; }
        public ulong VectorParam { get; set; }  // zigzag int

        public NaviVector(CodedInputStream stream)
        {
            Vector = new(stream.ReadRawFloat(3), stream.ReadRawFloat(3), stream.ReadRawFloat(3));
            VectorParam = stream.ReadRawVarint64();
        }

        public NaviVector(Vector3 vector, ulong vectorParam)
        {
            Vector = vector;
            VectorParam = vectorParam;
        }

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawBytes(Vector.Encode());
                stream.WriteRawVarint64(VectorParam);

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public override string ToString()
        {
            using (MemoryStream memoryStream = new())
            using (StreamWriter streamWriter = new(memoryStream))
            {
                streamWriter.WriteLine($"Vector: {Vector}");
                streamWriter.WriteLine($"VectorParam: 0x{VectorParam.ToString("X")}");

                streamWriter.Flush();
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
