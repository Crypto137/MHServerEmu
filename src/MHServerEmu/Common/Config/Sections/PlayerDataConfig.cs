using System;
using MHServerEmu.Common.Logging;
using MHServerEmu.GameServer.Entities;
using MHServerEmu.GameServer.Regions;

namespace MHServerEmu.Common.Config.Sections
{
    public class PlayerDataConfig
    {
        private const string Section = "PlayerData";
        private static readonly Logger Logger = LogManager.CreateLogger();

        public string PlayerName { get; }
        public RegionPrototype StartingRegion { get; }
        public HardcodedAvatarEntity StartingAvatar { get; }
        public ulong CostumeOverride { get; }

        public PlayerDataConfig(IniFile configFile)
        {
            PlayerName = configFile.ReadString(Section, "PlayerName");

            string startingRegion = configFile.ReadString(Section, "StartingRegion");
            string startingAvatar = configFile.ReadString(Section, "StartingAvatar");

            // StartingRegion
            try
            {
                StartingRegion = (RegionPrototype)Enum.Parse(typeof(RegionPrototype), startingRegion);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                Logger.Error($"Failed to parse StartingRegion {startingRegion}, falling back to NPEAvengersTowerHUBRegion");
                StartingRegion = RegionPrototype.NPEAvengersTowerHUBRegion;
            }

            // StartingHero
            try
            {
                StartingAvatar = (HardcodedAvatarEntity)Enum.Parse(typeof(HardcodedAvatarEntity), startingAvatar);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                Logger.Error($"Failed to parse HardcodedAvatarEntity {startingAvatar}, falling back to BlackCat");
                StartingAvatar = HardcodedAvatarEntity.BlackCat;
            }

            CostumeOverride = Convert.ToUInt64(configFile.ReadString(Section, "CostumeOverride"));
        }
    }
}
