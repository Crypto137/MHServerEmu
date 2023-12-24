namespace MHServerEmu.Games.GameData.Prototypes
{
    public class ScoringEventContextPrototype : Prototype
    {
        public ulong ContextAvatar { get; private set; }
        public ulong ContextItemEquipped { get; private set; }
        public ulong ContextParty { get; private set; }
        public ulong ContextPet { get; private set; }
        public ulong ContextRegion { get; private set; }
        public bool ContextRegionIncludeChildren { get; private set; }
        public ulong ContextRegionKeyword { get; private set; }
        public ulong ContextDifficultyTierMin { get; private set; }
        public ulong ContextDifficultyTierMax { get; private set; }
        public ulong ContextTeamUp { get; private set; }
        public ulong ContextPublicEventTeam { get; private set; }
    }

    public class ScoringEventTimerPrototype : Prototype
    {
        public ulong UIWidget { get; private set; }
    }

    public class ScoringEventPrototype : Prototype
    {
        public ScoringEventContextPrototype Context { get; private set; }
    }

    public class ScoringEventAchievementScorePrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventAreaEnterPrototype : ScoringEventPrototype
    {
        public ulong Area { get; private set; }
    }

    public class ScoringEventAvatarDeathPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventAvatarKillPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventAvatarKillAssistPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventAvatarLevelPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventAvatarLevelTotalPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventAvatarLevelTotalAllAvatarsPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventAvatarPrestigeLevelPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventAvatarsAtLevelCapPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventAvatarsAtPrstgLvlPrototype : ScoringEventPrototype
    {
        public ulong PrestigeLevel { get; private set; }
    }

    public class ScoringEventAvatarsAtPrstgLvlCapPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventAvatarsUnlockedPrototype : ScoringEventPrototype
    {
        public ulong Avatar { get; private set; }
    }

    public class ScoringEventAvatarUsedPowerPrototype : ScoringEventPrototype
    {
        public ulong Power { get; private set; }
        public ulong PowerKeyword { get; private set; }
        public ulong TargetKeyword { get; private set; }
        public ulong TargetPrototype { get; private set; }
        public bool TargetPrototypeIncludeChildren { get; private set; }
    }

    public class ScoringEventCompleteMissionPrototype : ScoringEventPrototype
    {
        public ulong Mission { get; private set; }
        public ulong MissionKeyword { get; private set; }
    }

    public class ScoringEventCompletionTimePrototype : ScoringEventPrototype
    {
        public ulong Timer { get; private set; }
    }

    public class ScoringEventCurrencyCollectedPrototype : ScoringEventPrototype
    {
        public ulong Currency { get; private set; }
    }

    public class ScoringEventCurrencySpentPrototype : ScoringEventPrototype
    {
        public ulong Currency { get; private set; }
    }

    public class ScoringEventEntityDeathPrototype : ScoringEventPrototype
    {
        public ulong EntityKeyword { get; private set; }
        public ulong EntityPrototype { get; private set; }
        public bool EntityPrototypeIncludeChildren { get; private set; }
        public ulong Rank { get; private set; }
        public ulong RankKeyword { get; private set; }
    }

    public class ScoringEventEntityDeathViaPowerPrototype : ScoringEventPrototype
    {
        public ulong EntityKeyword { get; private set; }
        public ulong EntityPrototype { get; private set; }
        public bool EntityPrototypeIncludeChildren { get; private set; }
        public ulong Power { get; private set; }
        public ulong PowerKeyword { get; private set; }
        public ulong Rank { get; private set; }
        public ulong RankKeyword { get; private set; }
    }

    public class ScoringEventEntityInteractPrototype : ScoringEventPrototype
    {
        public ulong EntityKeyword { get; private set; }
        public ulong EntityPrototype { get; private set; }
        public bool EntityPrototypeIncludeChildren { get; private set; }
    }

    public class ScoringEventFullyUpgradedLgndrysPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventFullyUpgradedPetTechPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventHotspotEnterPrototype : ScoringEventPrototype
    {
        public ulong HotspotEntity { get; private set; }
        public bool HotspotEntityIncludeChildren { get; private set; }
        public ulong HotspotKeyword { get; private set; }
    }

    public class ScoringEventHoursPlayedPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventHoursPlayedByAvatarPrototype : ScoringEventPrototype
    {
        public ulong Avatar { get; private set; }
    }

    public class ScoringEventItemBoughtPrototype : ScoringEventPrototype
    {
        public ulong ItemKeyword { get; private set; }
        public ulong ItemPrototype { get; private set; }
        public bool ItemPrototypeIncludeChildren { get; private set; }
        public ulong Rarity { get; private set; }
    }

    public class ScoringEventItemCollectedPrototype : ScoringEventPrototype
    {
        public ulong ItemKeyword { get; private set; }
        public ulong ItemPrototype { get; private set; }
        public bool ItemPrototypeIncludeChildren { get; private set; }
        public ulong Rarity { get; private set; }
    }

    public class ScoringEventItemCraftedPrototype : ScoringEventPrototype
    {
        public ulong Rarity { get; private set; }
        public ulong RecipeKeyword { get; private set; }
        public ulong RecipePrototype { get; private set; }
        public bool RecipePrototypeIncludeChildren { get; private set; }
    }

    public class ScoringEventItemDonatedPrototype : ScoringEventPrototype
    {
        public ulong ItemKeyword { get; private set; }
        public ulong ItemPrototype { get; private set; }
        public bool ItemPrototypeIncludeChildren { get; private set; }
        public ulong Rarity { get; private set; }
    }

    public class ScoringEventItemSpentPrototype : ScoringEventPrototype
    {
        public ulong ItemKeyword { get; private set; }
        public ulong ItemPrototype { get; private set; }
        public bool ItemPrototypeIncludeChildren { get; private set; }
    }

    public class ScoringEventMetaGameModeCompletePrototype : ScoringEventPrototype
    {
        public ulong MetaGameMode { get; private set; }
    }

    public class ScoringEventMetaGameStateCompltePrototype : ScoringEventPrototype
    {
        public ulong MetaGameState { get; private set; }
        public ulong ItemRarity { get; private set; }
    }

    public class ScoringEventMetaGameStateCompDifPrototype : ScoringEventPrototype
    {
        public ulong ItemRarity { get; private set; }
        public ulong MetaGameState { get; private set; }
    }

    public class ScoringEventMetaGameStateCompAfxPrototype : ScoringEventPrototype
    {
        public ulong ItemRarity { get; private set; }
        public ulong MetaGameState { get; private set; }
        public ulong RegionAffix { get; private set; }
    }

    public class ScoringEventMetaGameWaveCompletePrototype : ScoringEventPrototype
    {
        public ulong MetaGameMode { get; private set; }
    }

    public class ScoringEventMinGearLevelPrototype : ScoringEventPrototype
    {
        public ulong Avatar { get; private set; }
    }

    public class ScoringEventOrbsCollectedPrototype : ScoringEventPrototype
    {
        public ulong OrbKeyword { get; private set; }
        public ulong OrbPrototype { get; private set; }
        public bool OrbPrototypeIncludeChildren { get; private set; }
    }

    public class ScoringEventPowerRankPrototype : ScoringEventPrototype
    {
        public ulong Power { get; private set; }
        public ulong PowerKeyword { get; private set; }
    }

    public class ScoringEventPowerRankUltimatePrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventPvPMatchLostPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventPvPMatchWonPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventRegionEnterPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventVendorLevelPrototype : ScoringEventPrototype
    {
        public ulong VendorType { get; private set; }
    }

    public class ScoringEventWaypointUnlockedPrototype : ScoringEventPrototype
    {
        public ulong Waypoint { get; private set; }
    }
}
