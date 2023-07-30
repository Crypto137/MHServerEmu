using System;
using MHServerEmu.GameServer.Data.Enums;

namespace MHServerEmu.Common.Config.Sections
{
    public class PlayerDataConfig
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public RegionPrototype StartingRegion { get; }
        public HardcodedAvatarEntity StartingAvatar { get; }

        public PlayerDataConfig(string startingRegion, string startingAvatar)
        {
            // StartingRegion
            try
            {
                StartingRegion = (RegionPrototype)Enum.Parse(typeof(RegionPrototype), startingRegion);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                Logger.Error($"Failed to parse StartingRegion {startingRegion}, falling back to AvengersTower");
                StartingRegion = RegionPrototype.AvengersTower;
            }

            if (StartingRegion != RegionPrototype.AvengersTower &&
                StartingRegion != RegionPrototype.DangerRoom &&
                StartingRegion != RegionPrototype.MidtownPatrolCosmic)
            {
                Logger.Error($"Region {StartingRegion} has no data, falling back to AvengersTower");
                StartingRegion = RegionPrototype.AvengersTower;
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
