using System.Text;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Social.Communities
{
    /// <summary>
    /// Contains avatar information of a <see cref="CommunityMember"/>.
    /// </summary>
    public class AvatarSlotInfo : ISerialize
    {
        private PrototypeId _avatarRef;
        private PrototypeId _costumeRef;
        private int _level;
        private int _prestigeLevel;

        public PrototypeId AvatarRef { get => _avatarRef; set => _avatarRef = value; }
        public PrototypeId CostumeRef { get => _costumeRef; set => _costumeRef = value; }
        public int Level { get => _level; set => _level = value; }
        public int PrestigeLevel { get => _prestigeLevel; set => _prestigeLevel = value; }

        /// <summary>
        /// Constructs a new <see cref="AvatarSlotInfo"/> instance with default data.
        /// </summary>
        public AvatarSlotInfo() { }

        /// <summary>
        /// Constructs a new <see cref="AvatarSlotInfo"/> instance with the provided data.
        /// </summary>
        public AvatarSlotInfo(PrototypeId avatarRef, PrototypeId costumeRef, int level, int prestigeLevel)
        {
            AvatarRef = avatarRef;
            CostumeRef = costumeRef;
            Level = level;
            PrestigeLevel = prestigeLevel;
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            success &= Serializer.Transfer(archive, ref _avatarRef);
            success &= Serializer.Transfer(archive, ref _costumeRef);
            success &= Serializer.Transfer(archive, ref _level);
            success &= Serializer.Transfer(archive, ref _prestigeLevel);

            return success;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(_avatarRef)}: {GameDatabase.GetPrototypeName(_avatarRef)}");
            sb.AppendLine($"{nameof(_costumeRef)}: {GameDatabase.GetPrototypeName(_costumeRef)}");
            sb.AppendLine($"{nameof(_level)}: {_level}");
            sb.AppendLine($"{nameof(_prestigeLevel)}: {_prestigeLevel}");
            return sb.ToString();
        }
    }
}
