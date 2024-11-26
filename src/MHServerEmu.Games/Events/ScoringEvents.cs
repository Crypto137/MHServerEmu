using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
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
        public Prototype Proto0 { get; set; }
        public Prototype Proto1 { get; set; }
        public Prototype Proto2 { get; set; }
        public bool Proto0IncludeChildren { get; set; }
        public bool Proto1IncludeChildren { get; set; }
        public bool Proto2IncludeChildren { get; set; }
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

    public struct ScoringEventContext
    {
        public Prototype Avatar { get; set; }
        public Prototype Item { get; set; }
        public Prototype Party { get; set; }
        public Prototype Pet { get; set; }
        public Prototype Region { get; set; }
        public bool RegionIncludeChildren { get; set; }
        public DifficultyTierPrototype DifficultyTier { get; set; }
        public DifficultyTierPrototype DifficultyTierMin { get; set; }
        public DifficultyTierPrototype DifficultyTierMax { get; set; }
        public Prototype TeamUp { get; set; }
        public Prototype PublicEventTeam { get; set; }

        public ScoringEventContext(ScoringEventContextPrototype prototype)
        {
            if (prototype.ContextRegion != PrototypeId.Invalid)
                Region = prototype.ContextRegion.As<Prototype>();
            else if (prototype.ContextRegionKeyword != PrototypeId.Invalid)
                Region = prototype.ContextRegionKeyword.As<Prototype>();

            RegionIncludeChildren = prototype.ContextRegionIncludeChildren;

            Avatar = prototype.ContextAvatar != PrototypeId.Invalid ? prototype.ContextAvatar.As<Prototype>() : null;
            Item = prototype.ContextItemEquipped != PrototypeId.Invalid ? prototype.ContextItemEquipped.As<Prototype>() : null;
            Party = prototype.ContextParty != PrototypeId.Invalid ? prototype.ContextParty.As<Prototype>() : null;
            Pet = prototype.ContextPet != PrototypeId.Invalid ? prototype.ContextPet.As<Prototype>() : null;
            DifficultyTierMin = prototype.ContextDifficultyTierMin != PrototypeId.Invalid ? prototype.ContextDifficultyTierMin.As<DifficultyTierPrototype>() : null;
            DifficultyTierMax = prototype.ContextDifficultyTierMax != PrototypeId.Invalid ? prototype.ContextDifficultyTierMax.As<DifficultyTierPrototype>() : null;
            TeamUp = prototype.ContextTeamUp != PrototypeId.Invalid ? prototype.ContextTeamUp.As<Prototype>() : null;
            PublicEventTeam = prototype.ContextPublicEventTeam != PrototypeId.Invalid ? prototype.ContextPublicEventTeam.As<Prototype>() : null;
        }

        public void FromPlayer(Player player)
        {
            ClearForPlayer();

            var avatar = player.CurrentAvatar;
            if (avatar != null)
            {
                Avatar = avatar.Prototype;

                var teamUp = avatar.CurrentTeamUpAgent;
                if (teamUp != null && teamUp.IsInWorld && teamUp.IsDead == false && teamUp.TestStatus(EntityStatus.ExitingWorld) == false)
                    TeamUp = teamUp.Prototype;

                var pet = avatar.CurrentVanityPet;
                if (pet != null && pet.IsInWorld && pet.IsDead == false && pet.TestStatus(EntityStatus.ExitingWorld) == false)
                    Pet = pet.Prototype;
            }

            var region = player.GetRegion();
            if (region != null)
            {
                Region = region.Prototype;
                DifficultyTier = region.DifficultyTierRef.As<DifficultyTierPrototype>();
            }

            // TODO: Party
            // TODO: PublicEventTeam
        }

        private void ClearForPlayer()
        {
            Avatar = null;
            TeamUp = null;
            Pet = null;
            Region = null;
            DifficultyTier = null;
        }

        public bool FilterOwnerContext(Player owner, in ScoringEventContext ownerContext)
        {
            return ScoringEvents.FilterPrototype(Avatar, ownerContext.Avatar, false)
                && ScoringEvents.FilterPrototype(Region, ownerContext.Region, RegionIncludeChildren)
                && ScoringEvents.FilterPrototype(Pet, ownerContext.Pet, false)
                && ScoringEvents.FilterPrototype(TeamUp, ownerContext.TeamUp, false)
                && FilterOwnerItem(owner)
                && FilterDifficultyTier(ownerContext.DifficultyTier)
                && FilterParty(owner);

            // TODO: PublicEventTeam test
        }

        private bool FilterParty(Player owner)
        {
            if (Party == null) return true;

            // TODO: Party

            return false;
        }

        private bool FilterDifficultyTier(DifficultyTierPrototype difficultyTier)
        {
            if (DifficultyTierMin == null && DifficultyTierMax == null) return true;
            return DifficultyTierPrototype.InRange(difficultyTier, DifficultyTierMin, DifficultyTierMax);
        }

        private bool FilterOwnerItem(Player owner)
        {
            if (Item == null) return true;

            var avatar = owner.CurrentAvatar;
            if (avatar == null) return false;

            var manager = owner.Game?.EntityManager;
            if (manager == null) return false;

            var itemPrototype = Item as ItemPrototype;
            var keywordPrototype = Item as KeywordPrototype;

            foreach (Inventory inventory in new InventoryIterator(avatar, InventoryIterationFlags.Equipment))
                foreach (var entry in inventory)
                {
                    var item = manager.GetEntity<Item>(entry.Id);
                    if (item == null) continue;
                    if (keywordPrototype != null && item.HasKeyword(keywordPrototype)) return true;
                    else if (itemPrototype != null && item.Prototype == itemPrototype) return true;
                }

            return false;
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
