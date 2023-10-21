using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Social
{
    public class AvatarSlotInfo
    {
        public ulong AvatarRef { get; set; }
        public ulong CostumeRef { get; set; }
        public int AvatarLevel { get; set; }
        public int PrestigeLevel { get; set; }

        public AvatarSlotInfo(CodedInputStream stream)
        {
            AvatarRef = stream.ReadPrototypeEnum(PrototypeEnumType.All);
            CostumeRef = stream.ReadPrototypeEnum(PrototypeEnumType.All);
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

        public void Encode(CodedOutputStream stream)
        {
            stream.WritePrototypeEnum(AvatarRef, PrototypeEnumType.All);
            stream.WritePrototypeEnum(CostumeRef, PrototypeEnumType.All);
            stream.WriteRawInt32(AvatarLevel);
            stream.WriteRawInt32(PrestigeLevel);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"AvatarRef: {GameDatabase.GetPrototypeName(AvatarRef)}");
            sb.AppendLine($"CostumeRef: {GameDatabase.GetPrototypeName(CostumeRef)}");
            sb.AppendLine($"AvatarLevel: {AvatarLevel}");
            sb.AppendLine($"PrestigeLevel: {PrestigeLevel}");
            return sb.ToString();
        }
    }
}
