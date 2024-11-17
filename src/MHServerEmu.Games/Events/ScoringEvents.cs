using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

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
        MetaGameModeComplete,
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
        MetaGameStateCompleteDifficulty,
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
        public Prototype Proto0 { get; }
        public Prototype Proto1 { get; }
        public Prototype Proto2 { get; }
        public bool Proto0IncludeChildren { get; }
        public bool Proto1IncludeChildren { get; }
        public bool Proto2IncludeChildren { get; }
        public int Count { get; }

        public ScoringEvent()
        {
            Type = ScoringEventType.Invalid;
            Proto0 = null;
            Proto1 = null;
            Proto2 = null;
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

        public ScoringEvent(ScoringEventType eventType, Prototype prototype) : this()
        {
            Type = eventType;
            Proto0 = prototype;
        }

        public ScoringEvent(ScoringEventType eventType, Prototype prototype, int count) : this()
        {
            Type = eventType;
            Proto0 = prototype;
            Count = count;
        }

        public ScoringEvent(ScoringEventType eventType, Prototype prototype, Prototype prototype1, int count) : this()
        {
            Type = eventType;
            Proto0 = prototype;
            Proto1 = prototype1;
            Count = count;
        }

        public ScoringEvent(ScoringEventType eventType, Prototype prototype, Prototype prototype1) : this()
        {
            Type = eventType;
            Proto0 = prototype;
            Proto1 = prototype1;
        }

        public ScoringEvent(ScoringEventType eventType, Prototype prototype, Prototype prototype1, Prototype prototype2) : this()
        {
            Type = eventType;
            Proto0 = prototype;
            Proto1 = prototype1;
            Proto2 = prototype2;
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

        public static bool FilterPrototype(Prototype prototype, Prototype eventPrototype, bool includeChildren)
        {
            if (eventPrototype == null) return false;
            if (prototype == null || prototype == eventPrototype) return true;

            if (prototype is KeywordPrototype keywordPrototype)
            {
                return eventPrototype switch
                {
                    MissionPrototype missionPrototype => missionPrototype.HasKeyword(keywordPrototype),
                    PowerPrototype powerPrototype => powerPrototype.HasKeyword(keywordPrototype),
                    RankPrototype rankPrototype => rankPrototype.HasKeyword(keywordPrototype),
                    RegionPrototype regionPrototype => regionPrototype.HasKeyword(keywordPrototype),
                    WorldEntityPrototype worldEntityPrototype => worldEntityPrototype.HasKeyword(keywordPrototype),
                    _ => false,
                };
            }

            if (includeChildren == false) return false;

            return GameDatabase.DataDirectory.PrototypeIsAPrototype(eventPrototype.DataRef, prototype.DataRef);
        }
    }
}
