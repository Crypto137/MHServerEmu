using MHServerEmu.GameServer.Entities;
using MHServerEmu.GameServer.Entities.Avatars;
using MHServerEmu.GameServer.Regions;

namespace MHServerEmu.Common.Config.Sections
{
    public class PlayerDataConfig
    {
        private const string Section = "PlayerData";

        public string PlayerName { get; }
        public RegionPrototype StartingRegion { get; }
        public AvatarPrototype StartingAvatar { get; }

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

            if (Enum.TryParse(typeof(AvatarPrototype), startingAvatar, out object avatarEntityEnum))
                StartingAvatar = (AvatarPrototype)avatarEntityEnum;
            else
                StartingAvatar = AvatarPrototype.BlackCat;
        }
    }
}
