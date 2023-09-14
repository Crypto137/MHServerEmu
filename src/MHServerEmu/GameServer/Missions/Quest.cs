using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.GameServer.GameData;

namespace MHServerEmu.GameServer.Missions
{
    public class Quest
    {
        public ulong PrototypeGuid { get; set; }
        public ulong[] SubPrototypeGuids { get; set; }

        public Quest(CodedInputStream stream)
        {
            PrototypeGuid = stream.ReadRawVarint64();

            SubPrototypeGuids = new ulong[stream.ReadRawVarint64()];
            for (int i = 0; i < SubPrototypeGuids.Length; i++)
            {
                SubPrototypeGuids[i] = stream.ReadRawVarint64();
            }
        }

        public Quest(ulong prototypeGuid, ulong[] subPrototypeGuids)
        {
            PrototypeGuid = prototypeGuid;
            SubPrototypeGuids = subPrototypeGuids;
        }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint64(PrototypeGuid);
                cos.WriteRawVarint64((ulong)SubPrototypeGuids.Length);
                foreach (ulong subPrototypeGuid in SubPrototypeGuids) cos.WriteRawVarint64(subPrototypeGuid);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"PrototypeGuid: {GameDatabase.GetPrototypePath(GameDatabase.GetPrototypeId(PrototypeGuid))}");
            for (int i = 0; i < SubPrototypeGuids.Length; i++) sb.AppendLine($"SubPrototypeGuid{i}: {GameDatabase.GetPrototypePath(GameDatabase.GetPrototypeId(SubPrototypeGuids[i]))}");
            return sb.ToString();
        }
    }
}
