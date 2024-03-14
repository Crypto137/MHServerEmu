namespace MHServerEmu.Games.GameData.Prototypes
{
    public class ScoringEventContextPrototype : Prototype
    {
        public PrototypeId ContextAvatar { get; protected set; }
        public PrototypeId ContextItemEquipped { get; protected set; }
        public PrototypeId ContextParty { get; protected set; }
        public PrototypeId ContextPet { get; protected set; }
        public PrototypeId ContextRegion { get; protected set; }
        public bool ContextRegionIncludeChildren { get; protected set; }
        public PrototypeId ContextRegionKeyword { get; protected set; }
        public PrototypeId ContextDifficultyTierMin { get; protected set; }
        public PrototypeId ContextDifficultyTierMax { get; protected set; }
        public PrototypeId ContextTeamUp { get; protected set; }
        public PrototypeId ContextPublicEventTeam { get; protected set; }
    }

    public class ScoringEventTimerPrototype : Prototype
    {
        public PrototypeId UIWidget { get; protected set; }
    }

    public class ScoringEventPrototype : Prototype
    {
        public ScoringEventContextPrototype Context { get; protected set; }
    }

    public class ScoringEventAchievementScorePrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventAreaEnterPrototype : ScoringEventPrototype
    {
        public PrototypeId Area { get; protected set; }
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
        public PrototypeId PrestigeLevel { get; protected set; }
    }

    public class ScoringEventAvatarsAtPrstgLvlCapPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventAvatarsUnlockedPrototype : ScoringEventPrototype
    {
        public PrototypeId Avatar { get; protected set; }
    }

    public class ScoringEventAvatarUsedPowerPrototype : ScoringEventPrototype
    {
        public PrototypeId Power { get; protected set; }
        public PrototypeId PowerKeyword { get; protected set; }
        public PrototypeId TargetKeyword { get; protected set; }
        public PrototypeId TargetPrototype { get; protected set; }
        public bool TargetPrototypeIncludeChildren { get; protected set; }
    }

    public class ScoringEventCompleteMissionPrototype : ScoringEventPrototype
    {
        public PrototypeId Mission { get; protected set; }
        public PrototypeId MissionKeyword { get; protected set; }
    }

    public class ScoringEventCompletionTimePrototype : ScoringEventPrototype
    {
        public PrototypeId Timer { get; protected set; }
    }

    public class ScoringEventCurrencyCollectedPrototype : ScoringEventPrototype
    {
        public PrototypeId Currency { get; protected set; }
    }

    public class ScoringEventCurrencySpentPrototype : ScoringEventPrototype
    {
        public PrototypeId Currency { get; protected set; }
    }

    public class ScoringEventEntityDeathPrototype : ScoringEventPrototype
    {
        public PrototypeId EntityKeyword { get; protected set; }
        public PrototypeId EntityPrototype { get; protected set; }
        public bool EntityPrototypeIncludeChildren { get; protected set; }
        public PrototypeId Rank { get; protected set; }
        public PrototypeId RankKeyword { get; protected set; }
    }

    public class ScoringEventEntityDeathViaPowerPrototype : ScoringEventPrototype
    {
        public PrototypeId EntityKeyword { get; protected set; }
        public PrototypeId EntityPrototype { get; protected set; }
        public bool EntityPrototypeIncludeChildren { get; protected set; }
        public PrototypeId Power { get; protected set; }
        public PrototypeId PowerKeyword { get; protected set; }
        public PrototypeId Rank { get; protected set; }
        public PrototypeId RankKeyword { get; protected set; }
    }

    public class ScoringEventEntityInteractPrototype : ScoringEventPrototype
    {
        public PrototypeId EntityKeyword { get; protected set; }
        public PrototypeId EntityPrototype { get; protected set; }
        public bool EntityPrototypeIncludeChildren { get; protected set; }
    }

    public class ScoringEventFullyUpgradedLgndrysPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventFullyUpgradedPetTechPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventHotspotEnterPrototype : ScoringEventPrototype
    {
        public PrototypeId HotspotEntity { get; protected set; }
        public bool HotspotEntityIncludeChildren { get; protected set; }
        public PrototypeId HotspotKeyword { get; protected set; }
    }

    public class ScoringEventHoursPlayedPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventHoursPlayedByAvatarPrototype : ScoringEventPrototype
    {
        public PrototypeId Avatar { get; protected set; }
    }

    public class ScoringEventItemBoughtPrototype : ScoringEventPrototype
    {
        public PrototypeId ItemKeyword { get; protected set; }
        public PrototypeId ItemPrototype { get; protected set; }
        public bool ItemPrototypeIncludeChildren { get; protected set; }
        public PrototypeId Rarity { get; protected set; }
    }

    public class ScoringEventItemCollectedPrototype : ScoringEventPrototype
    {
        public PrototypeId ItemKeyword { get; protected set; }
        public PrototypeId ItemPrototype { get; protected set; }
        public bool ItemPrototypeIncludeChildren { get; protected set; }
        public PrototypeId Rarity { get; protected set; }
    }

    public class ScoringEventItemCraftedPrototype : ScoringEventPrototype
    {
        public PrototypeId Rarity { get; protected set; }
        public PrototypeId RecipeKeyword { get; protected set; }
        public PrototypeId RecipePrototype { get; protected set; }
        public bool RecipePrototypeIncludeChildren { get; protected set; }
    }

    public class ScoringEventItemDonatedPrototype : ScoringEventPrototype
    {
        public PrototypeId ItemKeyword { get; protected set; }
        public PrototypeId ItemPrototype { get; protected set; }
        public bool ItemPrototypeIncludeChildren { get; protected set; }
        public PrototypeId Rarity { get; protected set; }
    }

    public class ScoringEventItemSpentPrototype : ScoringEventPrototype
    {
        public PrototypeId ItemKeyword { get; protected set; }
        public PrototypeId ItemPrototype { get; protected set; }
        public bool ItemPrototypeIncludeChildren { get; protected set; }
    }

    public class ScoringEventMetaGameModeCompletePrototype : ScoringEventPrototype
    {
        public PrototypeId MetaGameMode { get; protected set; }
    }

    public class ScoringEventMetaGameStateCompltePrototype : ScoringEventPrototype
    {
        public PrototypeId MetaGameState { get; protected set; }
        public PrototypeId ItemRarity { get; protected set; }
    }

    public class ScoringEventMetaGameStateCompDifPrototype : ScoringEventPrototype
    {
        public PrototypeId ItemRarity { get; protected set; }
        public PrototypeId MetaGameState { get; protected set; }
    }

    public class ScoringEventMetaGameStateCompAfxPrototype : ScoringEventPrototype
    {
        public PrototypeId ItemRarity { get; protected set; }
        public PrototypeId MetaGameState { get; protected set; }
        public PrototypeId RegionAffix { get; protected set; }
    }

    public class ScoringEventMetaGameWaveCompletePrototype : ScoringEventPrototype
    {
        public PrototypeId MetaGameMode { get; protected set; }
    }

    public class ScoringEventMinGearLevelPrototype : ScoringEventPrototype
    {
        public PrototypeId Avatar { get; protected set; }
    }

    public class ScoringEventOrbsCollectedPrototype : ScoringEventPrototype
    {
        public PrototypeId OrbKeyword { get; protected set; }
        public PrototypeId OrbPrototype { get; protected set; }
        public bool OrbPrototypeIncludeChildren { get; protected set; }
    }

    public class ScoringEventPowerRankPrototype : ScoringEventPrototype
    {
        public PrototypeId Power { get; protected set; }
        public PrototypeId PowerKeyword { get; protected set; }
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
        public PrototypeId VendorType { get; protected set; }
    }

    public class ScoringEventWaypointUnlockedPrototype : ScoringEventPrototype
    {
        public PrototypeId Waypoint { get; protected set; }
    }
}
