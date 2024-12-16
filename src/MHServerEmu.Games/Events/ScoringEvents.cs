using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Properties;

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
        FullyUpgradedLegendaries,
        FullyUpgradedPetTech,
        HoursPlayed,
        HoursPlayedByAvatar,
        MinGearLevel,
        OrbsCollected,
        PowerRank, // Removed in 1.52
        PowerRankUltimate, // Removed in 1.52
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

    public struct ScoringPlayerContext
    {
        public ScoringEventType EventType { get; set; }
        public Prototype AvatarProto { get; internal set; }
        public int Threshold { get; set; }
        public uint DependentAchievementId { get; set; }
        public ScoringEventData EventData { get; set; }
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

        public ScoringEventContext(Player player)
        {
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

        public bool HasContext()
        {
            return Avatar != null || Region != null || Item != null || Pet != null || TeamUp != null 
                || DifficultyTierMin != null || DifficultyTierMax != null 
                || Party != null || PublicEventTeam != null;
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
                case ScoringEventType.FullyUpgradedLegendaries:
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

        public static bool GetPlayerContextCount(Player player, in ScoringPlayerContext playerContext, ref int count)
        {
            if (player == null) return false;

            return playerContext.EventType switch
            {
                ScoringEventType.AvatarLevel => GetPlayerAvatarLevelCount(player, playerContext.AvatarProto, ref count),
                ScoringEventType.AvatarLevelTotal => GetPlayerAvatarLevelTotalCount(player, playerContext.AvatarProto, ref count),
                ScoringEventType.AvatarLevelTotalAllAvatars => GetPlayerAvatarsTotalLevelsCount(player, ref count),
                ScoringEventType.AvatarsAtLevelCap => GetPlayerAvatarsAtLevelCapCount(player, ref count),
                ScoringEventType.AvatarsAtPrestigeLevel => GetPlayerAvatarsAtPrestigeLevelCount(player, playerContext.EventData, ref count),
                ScoringEventType.AvatarsAtPrestigeLevelCap => GetPlayerAvatarsAtPrestigeLevelCapCount(player, ref count),
                ScoringEventType.AvatarsUnlocked => GetPlayerAvatarsUnlockedCount(player, playerContext.EventData, ref count),
                ScoringEventType.AchievementScore => GetPlayerAchievementScoreCount(player, ref count),
                ScoringEventType.Dependent => GetPlayerDependentAchievementCount(player, playerContext.DependentAchievementId, ref count),
                ScoringEventType.IsComplete => GetPlayerDependentIsCompleteCount(player, playerContext.DependentAchievementId, ref count),
                ScoringEventType.ItemCollected => GetPlayerItemCollectedCount(player, playerContext, ref count),
                ScoringEventType.CompleteMission => GetPlayerCompleteMissionCount(player, playerContext, ref count),
                ScoringEventType.FullyUpgradedLegendaries => GetPlayerFullyUpgradedLegendariesCount(player, ref count),
                ScoringEventType.HoursPlayed => GetPlayerHoursPlayedCount(player, ref count),
                ScoringEventType.HoursPlayedByAvatar => GetPlayerHoursPlayedByAvatarCount(player, playerContext.AvatarProto, ref count),
                ScoringEventType.MinGearLevel => GetPlayerMinGearLevelCount(player, playerContext.AvatarProto, ref count),
                ScoringEventType.VendorLevel => GetPlayerVendorLevelCount(player, playerContext.EventData, ref count),
                ScoringEventType.PvPMatchWon => GetPlayerPvPMatchWonCount(player, playerContext.AvatarProto, ref count),
                ScoringEventType.PvPMatchLost => GetPlayerPvPMatchLostCount(player, playerContext.AvatarProto, ref count),
                ScoringEventType.WaypointUnlocked => GetPlayerWaypointUnlockedCount(player, playerContext.EventData, ref count),
                _ => false
            };
        }

        private static bool GetPlayerAvatarLevelCount(Player player, Prototype avatarProto, ref int count)
        {
            count = 0;
            foreach (var kvp in player.Properties.IteratePropertyRange(PropertyEnum.AvatarLibraryLevel))
            {
                Property.FromParam(kvp.Key, 1, out PrototypeId avatarRef);
                if (avatarRef == PrototypeId.Invalid) continue;
                Property.FromParam(kvp.Key, 0, out int avatarMode);
                if (avatarProto == null || avatarRef == avatarProto.DataRef)
                    count = Math.Max(player.GetMaxCharacterLevelAttainedForAvatar(avatarRef, (AvatarMode)avatarMode), count);
            }
            return true;
        }

        private static bool GetPlayerAvatarLevelTotalCount(Player player, Prototype avatarProto, ref int count)
        {
            count = 0;
            foreach (var kvp in player.Properties.IteratePropertyRange(PropertyEnum.AvatarLibraryLevel))
            {
                Property.FromParam(kvp.Key, 1, out PrototypeId avatarRef);
                if (avatarRef == PrototypeId.Invalid) continue;
                Property.FromParam(kvp.Key, 0, out int avatarMode);
                if (avatarProto == null || avatarRef == avatarProto.DataRef)
                    if ((AvatarMode)avatarMode == AvatarMode.Normal)
                        count = Math.Max(kvp.Value, count);
            }
            return true;
        }

        public static int GetPlayerAvatarsTotalLevels(Player player)
        {
            int totalLevels = 0;
            foreach (var kvp in player.Properties.IteratePropertyRange(PropertyEnum.AvatarLibraryLevel))
            {
                Property.FromParam(kvp.Key, 0, out int avatarMode);
                if ((AvatarMode)avatarMode == AvatarMode.Normal)
                    totalLevels += kvp.Value;
            }
            return totalLevels;
        }

        private static bool GetPlayerAvatarsTotalLevelsCount(Player player, ref int count)
        {
            count = GetPlayerAvatarsTotalLevels(player);
            return true;
        }

        public static int GetPlayerAvatarsAtLevelCap(Player player)
        {
            int levelCap = Avatar.GetAvatarLevelCap();
            HashSet<PrototypeId> avatars = new();
            foreach (var kvp in player.Properties.IteratePropertyRange(PropertyEnum.AvatarLibraryLevel))
            {
                Property.FromParam(kvp.Key, 1, out PrototypeId avatarRef);
                if (avatarRef == PrototypeId.Invalid) continue;
                Property.FromParam(kvp.Key, 0, out int avatarMode);
                if (player.GetMaxCharacterLevelAttainedForAvatar(avatarRef, (AvatarMode)avatarMode) >= levelCap)
                    avatars.Add(avatarRef);
            }
            return avatars.Count;
        }

        private static bool GetPlayerAvatarsAtLevelCapCount(Player player, ref int count)
        {
            count = GetPlayerAvatarsAtLevelCap(player);
            return true;
        }

        public static int GetPlayerAvatarsAtPrestigeLevel(Player player, int prestigeLevel)
        {
            HashSet<PrototypeId> avatars = new();
            foreach (var kvp in player.Properties.IteratePropertyRange(PropertyEnum.AvatarLibraryLevel))
            {
                Property.FromParam(kvp.Key, 1, out PrototypeId avatarRef);
                if (avatarRef == PrototypeId.Invalid) continue;
                Property.FromParam(kvp.Key, 0, out int avatarMode);
                if (player.GetPrestigeLevelForAvatar(avatarRef, (AvatarMode)avatarMode) >= prestigeLevel)
                    avatars.Add(avatarRef);
            }
            return avatars.Count;
        }

        private static bool GetPlayerAvatarsAtPrestigeLevelCount(Player player, in ScoringEventData eventData, ref int count)
        {
            int prestigeLevel = 1;
            if (eventData.Proto0 != null)
            {
                if (eventData.Proto0 is not PrestigeLevelPrototype prestigeProto) return false;
                var advancementProto = GameDatabase.AdvancementGlobalsPrototype;
                if (advancementProto == null) return false;
                prestigeLevel = advancementProto.GetPrestigeLevelIndex(prestigeProto);
                if (prestigeLevel <= 0) return false;
            }
            count = GetPlayerAvatarsAtPrestigeLevel(player, prestigeLevel);
            return true;
        }

        public static int GetPlayerAvatarsAtPrestigeLevelCap(Player player)
        {
            int levelCap = Avatar.GetAvatarLevelCap();
            var advancementProto = GameDatabase.AdvancementGlobalsPrototype;
            if (advancementProto == null) return 0;
            int maxPrestigeLevel = advancementProto.MaxPrestigeLevel;

            HashSet<PrototypeId> avatars = new();
            foreach (var kvp in player.Properties.IteratePropertyRange(PropertyEnum.AvatarLibraryLevel))
            {
                Property.FromParam(kvp.Key, 1, out PrototypeId avatarRef);
                if (avatarRef == PrototypeId.Invalid) continue;
                Property.FromParam(kvp.Key, 0, out int avatarMode);
                if (player.GetCharacterLevelForAvatar(avatarRef, (AvatarMode)avatarMode) >= levelCap
                    && player.GetPrestigeLevelForAvatar(avatarRef, (AvatarMode)avatarMode) >= maxPrestigeLevel)
                    avatars.Add(avatarRef);
            }
            return avatars.Count;
        }

        private static bool GetPlayerAvatarsAtPrestigeLevelCapCount(Player player, ref int count)
        {
            count = GetPlayerAvatarsAtPrestigeLevelCap(player);
            return true;
        }

        private static bool GetPlayerAvatarsUnlockedCount(Player player, in ScoringEventData eventData, ref int count)
        {
            count = 0;
            foreach (var kvp in player.Properties.IteratePropertyRange(PropertyEnum.AvatarUnlock))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId avatarRef);
                if (avatarRef == PrototypeId.Invalid) continue;
                var unlockType = (AvatarUnlockType)(int)kvp.Value;
                if (unlockType != AvatarUnlockType.Starter)
                {
                    var avatarProto = GameDatabase.GetPrototype<AvatarPrototype>(avatarRef);
                    if (avatarProto == null) continue;
                    if (FilterPrototype(eventData.Proto0, avatarProto, eventData.Proto0IncludeChildren))
                        count++;
                }
            }
            return true;
        }

        private static bool GetPlayerAchievementScoreCount(Player player, ref int count)
        {
            count = (int)player.AchievementState.GetTotalStats().Score;
            return true;
        }

        private static bool GetPlayerDependentAchievementCount(Player player, uint dependentAchievementId, ref int count)
        {
            if (dependentAchievementId == 0) return false;
            count = (int)player.AchievementState.GetAchievementProgress(dependentAchievementId).Count;
            return true;
        }

        private static bool GetPlayerDependentIsCompleteCount(Player player, uint dependentAchievementId, ref int count)
        {
            if (dependentAchievementId == 0) return false;
            if (player.AchievementState.GetAchievementProgress(dependentAchievementId).IsComplete)
            {
                count = 1;
                return true;
            }
            return false;
        }

        private static bool GetPlayerItemCollectedCount(Player player, in ScoringPlayerContext playerContext, ref int count)
        {
            var itemProto = playerContext.EventData.Proto0;
            bool itemChilden = playerContext.EventData.Proto0IncludeChildren;
            var rarityProto = playerContext.EventData.Proto1;
            bool rarityChildren = playerContext.EventData.Proto1IncludeChildren;

            if (itemProto == null && rarityProto == null) return false;

            var manager = player.Game?.EntityManager;
            if (manager == null) return false;

            count = 0;
            
            var flags = InventoryIterationFlags.PlayerGeneral
                | InventoryIterationFlags.PlayerGeneralExtra
                | InventoryIterationFlags.PlayerStashGeneral
                | InventoryIterationFlags.PlayerStashAvatarSpecific;

            foreach (var inventory in new InventoryIterator(player, flags))
                foreach (var entry in inventory)
                {
                    var item = manager.GetEntity<Item>(entry.Id);
                    if (item == null) continue;

                    if (itemProto != null && FilterPrototype(itemProto, item.Prototype, itemChilden) == false) continue;
                    if (rarityProto != null && FilterPrototype(rarityProto, item.RarityPrototype, rarityChildren) == false) continue;

                    count += item.CurrentStackSize;
                }

            foreach (var avatar in new AvatarIterator(player))
                if (playerContext.AvatarProto == null || avatar.Prototype == playerContext.AvatarProto)                
                    foreach (var inventory in new InventoryIterator(avatar, InventoryIterationFlags.Equipment))
                        foreach (var entry in inventory)
                        {
                            var item = manager.GetEntity<Item>(entry.Id);
                            if (item == null) continue;

                            if (itemProto != null && FilterPrototype(itemProto, item.Prototype, itemChilden) == false) continue;
                            if (rarityProto != null && FilterPrototype(rarityProto, item.RarityPrototype, rarityChildren) == false) continue;

                            count += item.CurrentStackSize;
                        }

            return true;
        }

        private static bool GetPlayerCompleteMissionCount(Player player, in ScoringPlayerContext playerContext, ref int count)
        {
            if (playerContext.EventData.Proto0 is not MissionPrototype missionProto) return false;
            if (playerContext.Threshold > 1 || missionProto.Repeatable) return false;
            if (missionProto is OpenMissionPrototype 
                || missionProto is LegendaryMissionPrototype 
                || missionProto is DailyMissionPrototype) return false;

            if (missionProto.SaveStatePerAvatar)
            {
                var propId = new PropertyId(PropertyEnum.AvatarMissionState, missionProto.DataRef);
                foreach (var avatar in new AvatarIterator(player))
                    if (avatar.Properties[propId] == (int)MissionState.Completed)
                        if (playerContext.AvatarProto == null || avatar.Prototype == playerContext.AvatarProto)
                        {
                            count = 1;
                            return true;
                        }
            }
            else if (playerContext.AvatarProto == null)
            {
                var manager = player.MissionManager;
                if (manager == null) return false;

                var mission = manager.FindMissionByDataRef(missionProto.DataRef);
                if (mission != null && mission.State == MissionState.Completed)
                {
                    count = 1;
                    return true;
                }
            }

            return false;
        }

        private static bool GetPlayerFullyUpgradedLegendariesCount(Player player, ref int count)
        {
            count = 0;
            var manager = player.Game.EntityManager;

            foreach (var avatar in new AvatarIterator(player))
            {
                var inventory = avatar.GetInventory(InventoryConvenienceLabel.AvatarLegendary);
                if (inventory == null) continue; 
                
                var legendaryId = inventory.GetEntityInSlot(0);
                if (legendaryId == Entity.InvalidId) continue;

                var legendary = manager.GetEntity<Item>(legendaryId);
                if (legendary == null) continue;

                if (legendary.Properties[PropertyEnum.ItemAffixLevel] == legendary.GetAffixLevelCap())
                    count++;
            }

            var flags = InventoryIterationFlags.PlayerGeneral
                | InventoryIterationFlags.PlayerGeneralExtra
                | InventoryIterationFlags.PlayerStashGeneral
                | InventoryIterationFlags.PlayerStashAvatarSpecific;

            foreach (var inventory in new InventoryIterator(player, flags))
                foreach (var entry in inventory)
                { 
                    var itemProto = GameDatabase.GetPrototype<LegendaryPrototype>(entry.ProtoRef);
                    if (itemProto == null) continue;

                    var legendary = manager.GetEntity<Item>(entry.Id);
                    if (legendary == null) continue;

                    if (legendary.Properties[PropertyEnum.ItemAffixLevel] == legendary.GetAffixLevelCap())
                        count++;
                }

            return true;
        }

        private static bool GetPlayerHoursPlayedCount(Player player, ref int count)
        {
            count = (int)Math.Floor(player.TimePlayed().TotalHours);
            return true;
        }

        private static bool GetPlayerHoursPlayedByAvatarCount(Player player, Prototype avatarProto, ref int count)
        {
            count = 0;
            if (avatarProto != null)
            {
                foreach (var avatar in new AvatarIterator(player))
                    if (avatar.Prototype == avatarProto)
                        count += (int)Math.Floor(avatar.TimePlayed().TotalHours);
            }
            else
            {
                foreach (var avatar in new AvatarIterator(player))
                    count = Math.Max((int)Math.Floor(avatar.TimePlayed().TotalHours), count);
            }

            return true;
        }

        public static int GetAvatarMinGearLevel(Avatar avatar)
        {            
            var manager = avatar?.Game?.EntityManager;
            if (manager == null) return 0;

            int minLevel = int.MaxValue;
            int slot = 0;

            foreach (var entry in new InventoryIterator(avatar, InventoryIterationFlags.Equipment))
            {
                var item = manager.GetEntity<Item>(entry.GetEntityInSlot(0));
                if (item != null && item.IsGear(avatar.AvatarPrototype))
                {
                    slot++;
                    int level = item.GetDisplayItemLevel();
                    if (level < minLevel)
                        minLevel = level;
                }
            }

            if (slot != 5 || minLevel == int.MaxValue) return 0;
            return minLevel;
        }

        private static bool GetPlayerMinGearLevelCount(Player player, Prototype avatarProto, ref int count)
        {
            count = 0;
            foreach (var avatar in new AvatarIterator(player))
                if (avatarProto == null || avatar.Prototype == avatarProto)
                    count = Math.Max(GetAvatarMinGearLevel(avatar), count);

            return true;
        }

        private static bool GetPlayerVendorLevelCount(Player player, in ScoringEventData eventData, ref int count)
        {
            count = 0;
            var vendorProto = eventData.Proto0;
            if (vendorProto == null || eventData.Proto0IncludeChildren)
            {
                foreach (var protoRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<VendorTypePrototype>(PrototypeIterateFlags.NoAbstract))
                    if (vendorProto == null || GameDatabase.DataDirectory.PrototypeIsAPrototype(protoRef, vendorProto.DataRef))
                        count = Math.Max(player.Properties[PropertyEnum.VendorLevel, protoRef], count);
            }
            else
                count = player.Properties[PropertyEnum.VendorLevel, vendorProto.DataRef];

            return true;
        }

        private static bool GetPlayerPvPMatchWonCount(Player player, Prototype avatarProto, ref int count)
        {
            count = 0;
            if (avatarProto != null)
            {
                foreach (var avatar in new AvatarIterator(player))
                    if (avatar.Prototype == avatarProto)
                        count = Math.Max(avatar.Properties[PropertyEnum.PvPWins], count);
            }
            else count = player.Properties[PropertyEnum.PvPWins];

            return true;
        }

        private static bool GetPlayerPvPMatchLostCount(Player player, Prototype avatarProto, ref int count)
        {
            count = 0;
            if (avatarProto != null)
            {
                foreach (var avatar in new AvatarIterator(player))
                    if (avatar.Prototype == avatarProto)
                        count = Math.Max(avatar.Properties[PropertyEnum.PvPLosses], count);
            }
            else count = player.Properties[PropertyEnum.PvPLosses];

            return true;
        }

        private static bool GetPlayerWaypointUnlockedCount(Player player, in ScoringEventData eventData, ref int count)
        {
            var waypointProto = eventData.Proto0;
            if (waypointProto == null) return false;

            if (player.WaypointIsUnlocked(waypointProto.DataRef))
            {
                count = 1;
                return true;
            }

            return false;
        }
    }
}
