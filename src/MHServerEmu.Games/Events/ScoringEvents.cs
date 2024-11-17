using MHServerEmu.Games.Entities;
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

    public struct ScoringEventContext
    {
        public Prototype Avatar { get; set; }
        public Prototype ItemEquipped { get; set; }
        public Prototype Party { get; set; }
        public Prototype Pet { get; set; }
        public Prototype Region { get; set; }
        public bool RegionIncludeChildren { get; set; }
        public Prototype RegionKeyword { get; set; }
        public Prototype DifficultyTierMin { get; set; }
        public Prototype DifficultyTierMax { get; set; }
        public Prototype TeamUp { get; set; }
        public Prototype PublicEventTeam { get; set; }

        public ScoringEventContext(ScoringEventContextPrototype prototype)
        {
            RegionIncludeChildren = prototype.ContextRegionIncludeChildren;
            Avatar = prototype.ContextAvatar != PrototypeId.Invalid ? prototype.ContextAvatar.As<Prototype>() : null;
            ItemEquipped = prototype.ContextItemEquipped != PrototypeId.Invalid ? prototype.ContextItemEquipped.As<Prototype>() : null;
            Party = prototype.ContextParty != PrototypeId.Invalid ? prototype.ContextParty.As<Prototype>() : null;
            Pet = prototype.ContextPet != PrototypeId.Invalid ? prototype.ContextPet.As<Prototype>() : null;
            Region = prototype.ContextRegion != PrototypeId.Invalid ? prototype.ContextRegion.As<Prototype>() : null;
            RegionKeyword = prototype.ContextRegionKeyword != PrototypeId.Invalid ? prototype.ContextRegionKeyword.As<Prototype>() : null;
            DifficultyTierMin = prototype.ContextDifficultyTierMin != PrototypeId.Invalid ? prototype.ContextDifficultyTierMin.As<Prototype>() : null;
            DifficultyTierMax = prototype.ContextDifficultyTierMax != PrototypeId.Invalid ? prototype.ContextDifficultyTierMax.As<Prototype>() : null;
            TeamUp = prototype.ContextTeamUp != PrototypeId.Invalid ? prototype.ContextTeamUp.As<Prototype>() : null;
            PublicEventTeam = prototype.ContextPublicEventTeam != PrototypeId.Invalid ? prototype.ContextPublicEventTeam.As<Prototype>() : null;

        }

        public void FromPlayer(Player player)
        {
            Avatar = player.CurrentAvatar?.Prototype;
            // TODO: ItemEquipped
            // TODO: Party
            // TODO: Pet
            Region = player.GetRegion()?.Prototype;
            // TODO: RegionKeyword
            // TODO: DifficultyTierMin
            // TODO: DifficultyTierMax
            Agent teamUp = player.CurrentAvatar?.CurrentTeamUpAgent;
            bool isTeamUpValid = teamUp != null && teamUp.IsInWorld && !teamUp.TestStatus(EntityStatus.ExitingWorld) && !teamUp.IsDead;
            TeamUp = isTeamUpValid ? teamUp.Prototype : null;
            // TODO: PublicEventTeam
        }

        public bool FilterContext(ScoringEventContext other)
        {
            bool avaterTest = ScoringEvents.FilterPrototype(Avatar, other.Avatar, false);
            bool regionTest = ScoringEvents.FilterPrototype(Region, other.Region, RegionIncludeChildren);
            bool petTest = ScoringEvents.FilterPrototype(Pet, other.Pet, false);
            bool teamUpTest = ScoringEvents.FilterPrototype(TeamUp, other.TeamUp, false);
            // TODO: Item test
            // TODO: Party test
            // TODO: DifficultyTier test
            // TODO: PublicEventTeam test
            return regionTest && avaterTest && petTest && teamUpTest;
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
