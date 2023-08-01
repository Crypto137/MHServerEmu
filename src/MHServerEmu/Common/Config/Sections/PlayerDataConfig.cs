using System;
using MHServerEmu.GameServer.Entities;
using MHServerEmu.GameServer.Regions;

namespace MHServerEmu.Common.Config.Sections
{
    public class PlayerDataConfig
    {
        private const string Section = "PlayerData";
        private static readonly Logger Logger = LogManager.CreateLogger();

        public RegionPrototype StartingRegion { get; }
        public HardcodedAvatarEntity StartingAvatar { get; }

        public PlayerDataConfig(IniFile configFile)
        {
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

            if (StartingRegion != RegionPrototype.NPEAvengersTowerHUBRegion &&
                StartingRegion != RegionPrototype.DangerRoomHubRegion &&
                StartingRegion != RegionPrototype.XManhattanRegion60Cosmic)
            {
                Logger.Error($"Region {StartingRegion} has no data, falling back to NPEAvengersTowerHUBRegion");
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
        }
    }
}
