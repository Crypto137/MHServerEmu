using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.GameData;

namespace MHServerEmu.GameServer.Social
{
    public class AvatarSlotInfo
    {
        public ulong AvatarRef { get; set; }
        public ulong CostumeRef { get; set; }
        public int AvatarLevel { get; set; }
        public int PrestigeLevel { get; set; }

        public AvatarSlotInfo(CodedInputStream stream)
        {
            AvatarRef = stream.ReadPrototypeId(PrototypeEnumType.All);
            CostumeRef = stream.ReadPrototypeId(PrototypeEnumType.All);
            AvatarLevel = stream.ReadRawInt32();
            PrestigeLevel = stream.ReadRawInt32();
        }

        public AvatarSlotInfo(ulong avatarRef, ulong costumeRef, int avatarLevel, int prestigeLevel)
        {
            AvatarRef = avatarRef;
            CostumeRef = costumeRef;
            AvatarLevel = avatarLevel;
            PrestigeLevel = prestigeLevel;
        }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WritePrototypeId(AvatarRef, PrototypeEnumType.All);
                cos.WritePrototypeId(CostumeRef, PrototypeEnumType.All);
                cos.WriteRawInt32(AvatarLevel);
                cos.WriteRawInt32(PrestigeLevel);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"AvatarRef: {GameDatabase.GetPrototypePath(AvatarRef)}");
            sb.AppendLine($"CostumeRef: {GameDatabase.GetPrototypePath(CostumeRef)}");
            sb.AppendLine($"AvatarLevel: {AvatarLevel}");
            sb.AppendLine($"PrestigeLevel: {PrestigeLevel}");
            return sb.ToString();
        }
    }
}
