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
        Dependent, // Legendary
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

    public enum ScoringMethod
    {
        Update,
        Add,
        Min,
        Max
    }

    #endregion

    public struct ScoringEventData
    {
        public Prototype Proto0 { get; }
        public Prototype Proto1 { get; }
        public Prototype Proto2 { get; }
        public bool Proto0IncludeChildren { get; }
        public bool Proto1IncludeChildren { get; }
        public bool Proto2IncludeChildren { get; }
    }

    public readonly struct ScoringEvent
    {
        public ScoringEventType Type { get; }
        public Prototype Proto0 { get; }
        public Prototype Proto1 { get; }
        public Prototype Proto2 { get; }
        public int Count { get; }

        public ScoringEvent()
        {
            Type = ScoringEventType.Invalid;
            Proto0 = null;
            Proto1 = null;
            Proto2 = null;
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
            if (prototype == null || prototype == eventPrototype) return true;
            if (eventPrototype == null) return false;

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

        public static ScoringMethod GetMethod(ScoringEventType eventType)
        {
            switch (eventType)
            {
                case ScoringEventType.AreaEnter:
                case ScoringEventType.AvatarsUnlocked:
                case ScoringEventType.AvatarUsedPower:
                case ScoringEventType.CompleteMission:
                case ScoringEventType.CurrencySpent:
                case ScoringEventType.CurrencyCollected:
                case ScoringEventType.EntityDeath:
                case ScoringEventType.EntityInteract:
                case ScoringEventType.HotspotEnter:
                case ScoringEventType.ItemBought:
                case ScoringEventType.ItemCollected:
                case ScoringEventType.ItemCrafted:
                case ScoringEventType.ItemDonated:
                case ScoringEventType.RegionEnter:
                case ScoringEventType.MetaGameModeComplete:
                case ScoringEventType.MetaGameStateComplete:
                case ScoringEventType.ItemSpent:
                case ScoringEventType.IsComplete:
                case ScoringEventType.EntityDeathViaPower:
                case ScoringEventType.PvPMatchWon:
                case ScoringEventType.PvPMatchLost:
                case ScoringEventType.FullyUpgradedLgndrys:
                case ScoringEventType.OrbsCollected:
                case ScoringEventType.MetaGameStateCompleteAffix:
                case ScoringEventType.AvatarDeath:
                case ScoringEventType.AvatarKill:
                    return ScoringMethod.Add;

                case ScoringEventType.AvatarPrestigeLevel:
                case ScoringEventType.VendorLevel:
                case ScoringEventType.MetaGameWaveComplete:
                case ScoringEventType.AvatarsAtPrestigeLevel:
                case ScoringEventType.AvatarsAtPrestigeLevelCap:
                case ScoringEventType.AvatarsAtLevelCap:
                case ScoringEventType.HoursPlayed:
                case ScoringEventType.HoursPlayedByAvatar:
                case ScoringEventType.MetaGameStateCompleteDifficulty:
                case ScoringEventType.AvatarLevelTotal:
                case ScoringEventType.AvatarLevelTotalAllAvatars:
                    return ScoringMethod.Max;

                case ScoringEventType.AvatarLevel:
                case ScoringEventType.DifficultyUnlocked:
                case ScoringEventType.WaypointUnlocked:
                case ScoringEventType.ChildrenComplete:
                case ScoringEventType.AchievementScore:
                case ScoringEventType.FullyUpgradedPetTech:
                case ScoringEventType.MinGearLevel:
                case ScoringEventType.PowerRank:
                case ScoringEventType.PowerRankUltimate:
                case ScoringEventType.Dependent:
                    return ScoringMethod.Update;

                case ScoringEventType.CompletionTime:
                    return ScoringMethod.Min;
            }

            return ScoringMethod.Update;
        }
    }
}
