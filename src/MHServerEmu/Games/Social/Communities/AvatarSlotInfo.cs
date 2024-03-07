using System.Text;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Social.Communities
{
    public class AvatarSlotInfo
    {
        public PrototypeId AvatarRef { get; set; }
        public PrototypeId CostumeRef { get; set; }
        public int Level { get; set; }
        public int PrestigeLevel { get; set; }

        public AvatarSlotInfo() { }

        public AvatarSlotInfo(PrototypeId avatarRef, PrototypeId costumeRef, int level, int prestigeLevel)
        {
            AvatarRef = avatarRef;
            CostumeRef = costumeRef;
            Level = level;
            PrestigeLevel = prestigeLevel;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(AvatarRef)}: {GameDatabase.GetPrototypeName(AvatarRef)}");
            sb.AppendLine($"{nameof(CostumeRef)}: {GameDatabase.GetPrototypeName(CostumeRef)}");
            sb.AppendLine($"{nameof(Level)}: {Level}");
            sb.AppendLine($"{nameof(PrestigeLevel)}: {PrestigeLevel}");
            return sb.ToString();
        }
    }
}
