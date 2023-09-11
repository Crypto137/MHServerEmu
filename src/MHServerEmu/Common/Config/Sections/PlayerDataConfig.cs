using MHServerEmu.GameServer.Entities;
using MHServerEmu.GameServer.Regions;

namespace MHServerEmu.Common.Config.Sections
{
    public class PlayerDataConfig
    {
        private const string Section = "PlayerData";

        public string PlayerName { get; }
        public RegionPrototype StartingRegion { get; }
        public HardcodedAvatarEntity StartingAvatar { get; }
        public ulong CostumeOverride { get; }

        public PlayerDataConfig(IniFile configFile)
        {
            PlayerName = configFile.ReadString(Section, nameof(PlayerName));

            // StartingRegion
            string startingRegion = configFile.ReadString(Section, nameof(StartingRegion));

            if (Enum.TryParse(typeof(RegionPrototype), startingRegion, out object regionPrototypeEnum))
                StartingRegion = (RegionPrototype)regionPrototypeEnum;
            else
                StartingRegion = RegionPrototype.NPEAvengersTowerHUBRegion;

            // StartingHero
            string startingAvatar = configFile.ReadString(Section, nameof(StartingAvatar));

            if (Enum.TryParse(typeof(HardcodedAvatarEntity), startingAvatar, out object avatarEntityEnum))
                StartingAvatar = (HardcodedAvatarEntity)avatarEntityEnum;
            else
                StartingAvatar = HardcodedAvatarEntity.BlackCat;

            CostumeOverride = Convert.ToUInt64(configFile.ReadString(Section, nameof(CostumeOverride)));
        }
    }
}
