using MHServerEmu.Core.Config;

namespace MHServerEmu.Games
{
    public class CustomGameOptionsConfig : ConfigContainer
    {
        public bool AutoUnlockAvatars { get; private set; } = true;
        public bool AutoUnlockTeamUps { get; private set; } = true;
        public int RegionCleanupIntervalMS { get; private set; } = 1000 * 60 * 5;       // 5 minutes
        public int RegionUnvisitedThresholdMS { get; private set; } = 1000 * 60 * 5;    // 5 minutes
        public bool DisableMovementPowerChargeCost { get; private set; } = false;
        public bool AllowSameGroupTalents { get; private set; } = false;
        public bool DisableInstancedLoot { get; private set; } = false;
        public float LootSpawnGridCellRadius { get; private set; } = 20f;
        public float TrashedItemExpirationTimeMultiplier { get; private set; } = 1f;
        public bool DisableAccountBinding { get; private set; } = false;
        public bool DisableCharacterBinding { get; private set; } = true;
        public bool UsePrestigeLootTable { get; private set; } = false;

        [ConfigIgnore]
        public TimeSpan RegionCleanupInterval { get => TimeSpan.FromMilliseconds(RegionCleanupIntervalMS); }
        [ConfigIgnore]
        public TimeSpan RegionUnvisitedThreshold { get => TimeSpan.FromMilliseconds(RegionUnvisitedThresholdMS); }
    }
}
