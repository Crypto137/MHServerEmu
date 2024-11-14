using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Events
{
    #region Enum

    public enum ScoringEventType
    {
        Invalid = -1,
        AreaEnter,
        AvatarLevel,
        AvatarPrestigeLevel,
        AvatarsUnlocked,
        AvatarUsedPower,
        CompleteMission,
        CurrencySpent,
        CurrencyCollected,
        DifficultyUnlocked, // Be The Hero // Removed in 1.52
        EntityDeath,
        EntityInteract,
        HotspotEnter,
        ItemBought,
        ItemCollected,
        ItemCrafted,
        ItemDonated,
        RegionEnter,
        VendorLevel,
        WaypointUnlocked,
        ChildrenComplete,
        GameModeComplete,
        MetaGameStateComplete,
        MetaGameWaveComplete,
        ItemSpent,
        IsComplete, // Cow Tags
        EntityDeathViaPower,
        PvPMatchWon,
        PvPMatchLost,
        AvatarsAtPrestigeLevel,
        AvatarsAtPrestigeLevelCap,
        AvatarsAtLevelCap,
        AchievementScore,
        FullyUpgradedLgndrys,
        FullyUpgradedPetTech,
        HoursPlayed,
        HoursPlayedByAvatar,
        MinGearLevel,
        OrbsCollected,
        PowerRank,
        PowerRankUltimate,
        Count, // Legendary
        MetaGameStateCompleteDif,
        MetaGameStateCompleteAffix,
        AvatarDeath,
        AvatarKill,
        AvatarKillAssist,
        CompletionTime,
        AvatarLevelTotal,
        AvatarLevelTotalAllAvatars,
        Max
    }

    #endregion

    public readonly struct ScoringEvent
    {
        public ScoringEventType Type { get; }
        public PrototypeId Proto0 { get; }
        public PrototypeId Proto1 { get; }
        public PrototypeId Proto2 { get; }
        public bool Proto0IncludeChildren { get; }
        public bool Proto1IncludeChildren { get; }
        public bool Proto2IncludeChildren { get; }
        public int Count { get; }

        public ScoringEvent()
        {
            Type = ScoringEventType.Invalid;
            Proto0 = PrototypeId.Invalid;
            Proto1 = PrototypeId.Invalid;
            Proto2 = PrototypeId.Invalid;
            Proto0IncludeChildren = false;
            Proto1IncludeChildren = false;
            Proto2IncludeChildren = false;
            Count = 1;
        }

        public ScoringEvent(ScoringEventType eventType) : this()
        {
            Type = eventType;
        }

        public ScoringEvent(ScoringEventType eventType, int count) : this()
        {
            Type = eventType;
            Count = count;
        }

        public ScoringEvent(ScoringEventType eventType, PrototypeId prototypeDataRef) : this()
        {
            Type = eventType;
            Proto0 = prototypeDataRef;
        }
    }

    public class ScoringEvents
    {

        public static ScoringEventType GetScoringEventTypeFromInt(uint eventType)
        {
            return eventType < (uint)ScoringEventType.Max 
                ? (ScoringEventType)eventType 
                : ScoringEventType.Invalid;
        }
    }
}
