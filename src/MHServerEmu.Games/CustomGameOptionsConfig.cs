﻿using MHServerEmu.Core.Config;

namespace MHServerEmu.Games
{
    public class CustomGameOptionsConfig : ConfigContainer
    {
        public int RegionCleanupIntervalMS { get; private set; } = 1000 * 60 * 5;       // 5 minutes
        public int RegionUnvisitedThresholdMS { get; private set; } = 1000 * 60 * 5;    // 5 minutes
        public bool DisableMovementPowerChargeCost { get; private set; } = true;
        public bool DisableInstancedLoot { get; private set; } = false;
        public int WorldEntityRespawnTimeMS { get; private set; } = 1000 * 30;          // 30 seconds

        [ConfigIgnore]
        public TimeSpan RegionCleanupInterval { get => TimeSpan.FromMilliseconds(RegionCleanupIntervalMS); }
        [ConfigIgnore]
        public TimeSpan RegionUnvisitedThreshold { get => TimeSpan.FromMilliseconds(RegionUnvisitedThresholdMS); }
        [ConfigIgnore]
        public TimeSpan WorldEntityRespawnTime { get => TimeSpan.FromMilliseconds(WorldEntityRespawnTimeMS); }
    }
}
