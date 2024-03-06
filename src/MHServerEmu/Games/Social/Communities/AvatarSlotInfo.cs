using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Social.Communities
{
    public class AvatarSlotInfo
    {
        public PrototypeId AvatarRef { get; set; }
        public PrototypeId CostumeRef { get; set; }
        public int AvatarLevel { get; set; }
        public int PrestigeLevel { get; set; }

        public AvatarSlotInfo(CodedInputStream stream)
        {
            AvatarRef = stream.ReadPrototypeEnum<Prototype>();
            CostumeRef = stream.ReadPrototypeEnum<Prototype>();
            AvatarLevel = stream.ReadRawInt32();
            PrestigeLevel = stream.ReadRawInt32();
        }

        public AvatarSlotInfo(PrototypeId avatarRef, PrototypeId costumeRef, int avatarLevel, int prestigeLevel)
        {
            AvatarRef = avatarRef;
            CostumeRef = costumeRef;
            AvatarLevel = avatarLevel;
            PrestigeLevel = prestigeLevel;
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WritePrototypeEnum<Prototype>(AvatarRef);
            stream.WritePrototypeEnum<Prototype>(CostumeRef);
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
