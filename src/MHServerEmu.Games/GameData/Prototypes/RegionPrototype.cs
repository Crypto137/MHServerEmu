using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Regions.ObjectiveGraphs;
using MHServerEmu.Games.Social;
using MHServerEmu.Games.Social.Communities;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)None)]
    public enum MissionTrackerFilterType
    {
        None = -1,
        Standard = 0,
        PvE = 1,
        PvP = 2,
        Daily = 3,
        Challenge = 4,
    }

    [AssetEnum((int)Invalid)]
    public enum RegionBehavior     // Regions/RegionBehavior.type
    {
        Invalid = -1,
        Town = 0,
        PublicCombatZone = 1,
        PrivateStory = 2,
        PrivateNonStory = 3,
        PrivateRaid = 5,
        MatchPlay = 4,
    }

    [AssetEnum((int)None)]
    public enum RegionMusicBehavior
    {
        None,
        Default,
        Mission,
    }

    [AssetEnum((int)None)]
    public enum FootstepTraceBehavior
    {
        None,
        Enable,
        Disable,
    }

    [AssetEnum((int)None)]
    public enum RegionQueueMethod
    {
        None = 0,
        PvPQueue = 1,
        DailyQueue = 5,
    }

    [AssetEnum((int)BiDirectional)]
    public enum RegionTransitionDirectionality  // Regions/RegionConnectionType.type
    {
        BiDirectional = 0,
        OneWay = 1,
        Disabled = 2,
    }

    #endregion

    public class RegionPrototype : Prototype
    {
        public AssetId ClientMap { get; protected set; }
        public PrototypeId BodySliderTarget { get; protected set; }
        public PrototypeId StartTarget { get; protected set; }
        public AssetId[] Music { get; protected set; }
        public RegionGeneratorPrototype RegionGenerator { get; protected set; }
        public RegionBehavior Behavior { get; protected set; }
        public LocaleStringId RegionName { get; protected set; }
        public PrototypeId[] MetaGames { get; protected set; }
        public bool ForceSimulation { get; protected set; }
        public PrototypeId[] LoadingScreens { get; protected set; }
        public bool AlwaysRevealFullMap { get; protected set; }
        public PrototypeId Chapter { get; protected set; }
        public int PlayerLimit { get; protected set; }
        public float LifetimeInMinutes { get; protected set; }
        public PrototypeId WaypointAutoUnlock { get; protected set; }
        public bool PartyFormationAllowed { get; protected set; }
        public TransitionUIPrototype[] TransitionUITypes { get; protected set; }
        public AssetId AmbientSfx { get; protected set; }
        public PrototypeId[] PowerKeywordBlacklist { get; protected set; }
        public bool CloseWhenReservationsReachesZero { get; protected set; }
        public float UIMapWallThickness { get; protected set; }
        public PrototypeId[] PopulationOverrides { get; protected set; }
        public int Level { get; protected set; }
        public MissionTrackerFilterType[] MissionTrackerFilterList { get; protected set; }
        public bool AllowAutoPartyOnEnter { get; protected set; }
        public float AutoPartyWindowSecs { get; protected set; }
        public bool DailyCheckpointStartTarget { get; protected set; }
        public int LowPopulationPlayerLimit { get; protected set; }
        public PrototypeId RespawnOverride { get; protected set; }
        public PrototypeId PlayerCameraSettings { get; protected set; }
        public RegionQueueMethod RegionQueueMethod { get; protected set; }
        public EvalPrototype EvalAccessRestriction { get; protected set; }
        public PrototypeId[] WaypointAutoUnlockList { get; protected set; }
        public bool AlwaysShutdownWhenVacant { get; protected set; }
        public bool SynergyEditAllowed { get; protected set; }
        public PrototypeId[] Keywords { get; protected set; }
        public PrototypeId UITopPanel { get; protected set; }
        public PrototypeId[] AltRegions { get; protected set; }
        public RegionAccessCheckPrototype[] AccessChecks { get; protected set; }
        public LocaleStringId UIDescription { get; protected set; }
        public LocaleStringId UILocation { get; protected set; }
        public bool PausesBoostConditions { get; protected set; }
        public bool ShowTransitionIndicators { get; protected set; }
        public RegionQueueStateEntryPrototype[] RegionQueueStates { get; protected set; }
        public PrototypeId MarkerFilter { get; protected set; }
        public bool LevelBandedRegionUsesPlayerLevel { get; protected set; }
        public FootstepTraceBehavior FootstepTraceOverride { get; protected set; }
        public bool QueueDoNotWaitToFull { get; protected set; }
        public bool DisplayCommunityNews { get; protected set; }
        public AssetId UnrealClass { get; protected set; }
        public bool RespawnDestructibles { get; protected set; }
        public PrototypeId PropertyGameModeSetOnEntry { get; protected set; }
        public bool UsePrevRegionPlayerDeathCount { get; protected set; }
        public LootTableAssignmentPrototype[] LootTables { get; protected set; }
        public PrototypeId AffixTable { get; protected set; }
        public ObjectiveGraphSettingsPrototype ObjectiveGraph { get; protected set; }
        public DividedStartLocationPrototype[] DividedStartLocations { get; protected set; }
        public RegionMusicBehavior MusicBehavior { get; protected set; }
        public PrototypeId AvatarObjectiveInfoOverride { get; protected set; }
        public RegionDifficultySettingsPrototype DifficultySettings { get; protected set; }
        public bool LevelOverridesCharacterLevel { get; protected set; }
        public bool LevelUseAreaOffset { get; protected set; }
        public LocaleStringId EvalAccessRestrictionMessage { get; protected set; }
        public bool BodySliderOneWay { get; protected set; }
        public bool EnableAvatarSwap { get; protected set; }
        public PrototypeId[] RestrictedRoster { get; protected set; }
        public PrototypeId[] AvatarPowers { get; protected set; }
        public bool IsNPE { get; protected set; }
        public LocaleStringId PresenceStatusText { get; protected set; }
        public PrototypeId[] AccessDifficulties { get; protected set; }
        public PrototypeId Tuning { get; protected set; }
        public int BonusItemFindMultiplier { get; protected set; }
        public PrototypeId PlayerCameraSettingsOrbis { get; protected set; }
        public PrototypeId[] LoadingScreensConsole { get; protected set; }
        public bool AllowLocalCoopMode { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        [DoNotCopy]
        public KeywordsMask KeywordsMask { get; private set; }
        [DoNotCopy]
        public bool HasKeywords { get => Keywords.HasValue(); }
        [DoNotCopy]
        public DifficultyTierMask DifficultyTierMask { get; private set; }
        [DoNotCopy]
        public bool HasPvPMetaGame { get; private set; }
        [DoNotCopy]
        public bool HasScoreSchema { get; private set; }
        [DoNotCopy]
        public int RegionPrototypeEnumValue { get; private set; }
        [DoNotCopy]
        public HashSet<PrototypeId> AreasInGenerator { get; private set; }
        [DoNotCopy]
        public bool IsPublic { get => Behavior == RegionBehavior.Town || Behavior == RegionBehavior.PublicCombatZone || Behavior == RegionBehavior.MatchPlay; }
        [DoNotCopy]
        public bool IsPrivate { get => IsPublic == false; }

        private Dictionary<AssetId, List<LootTableAssignmentPrototype>> _lootTableMap = new();

        public static bool Equivalent(RegionPrototype regionA, RegionPrototype regionB)
        {
            if (regionA == null || regionB == null) return false;
            if (regionA == regionB) return true;
            return regionA.HasAltRegion(regionB.DataRef);
        }

        private bool HasAltRegion(PrototypeId dataRef)
        {
            if (AltRegions != null) return AltRegions.Contains(dataRef);
            return false;
        }

        public PrototypeId GetDefaultAreaRef(Region region)
        {
            PrototypeId defaultArea = PrototypeId.Invalid;

            if (StartTarget != PrototypeId.Invalid)
            {
                var target = GameDatabase.GetPrototype<RegionConnectionTargetPrototype>(StartTarget);
                if (target != null)
                    defaultArea = target.Area;
            }

            if (RegionGenerator != null && defaultArea == PrototypeId.Invalid)
                return RegionGenerator.GetStartAreaRef(region); // TODO check return

            return defaultArea;
        }

        public RegionDifficultySettingsPrototype GetDifficultySettings()
        {
            if (DifficultySettings != null) return DifficultySettings;

            var difficultyGlobals = GameDatabase.DifficultyGlobalsPrototype;
            if (difficultyGlobals == null) return null;

            if (Behavior == RegionBehavior.PublicCombatZone && difficultyGlobals.RegionSettingsDefaultPCZ != null)
                return difficultyGlobals.RegionSettingsDefaultPCZ;

            return difficultyGlobals.RegionSettingsDefault;
        }

        public override void PostProcess()
        {
            base.PostProcess();

            DifficultyTierMask = DifficultyTierMask.None;

            if (AccessDifficulties.HasValue())
            {
                foreach (var difficultyTierRef in AccessDifficulties)
                {
                    var difficultyTierProto = GameDatabase.GetPrototype<DifficultyTierPrototype>(difficultyTierRef);
                    if (difficultyTierProto == null) continue;
                    DifficultyTierMask |= (DifficultyTierMask)(1 << (int)difficultyTierProto.Tier);
                }
            }
            else
                DifficultyTierMask = DifficultyTierMask.Green | DifficultyTierMask.Red | DifficultyTierMask.Cosmic;

            if (RegionQueueStates.HasValue())
            {
                int index = 0;
                foreach (var entryProto in RegionQueueStates)
                {
                    if (entryProto != null) entryProto.Index = index;
                    index++;
                }
            }

            HasPvPMetaGame = false;
            HasScoreSchema = false;

            if (MetaGames.HasValue())
                foreach (var metaGameRef in MetaGames)
                {
                    if (metaGameRef == PrototypeId.Invalid) continue;
                    var metaPvP = GameDatabase.GetPrototype<PvPPrototype>(metaGameRef);
                    if (metaPvP != null)
                    {
                        if (metaPvP.IsPvP) HasPvPMetaGame = true;
                        if (metaPvP.ScoreSchemaPlayer != PrototypeId.Invalid || metaPvP.ScoreSchemaRegion != PrototypeId.Invalid)
                            HasScoreSchema = true;
                        break;
                    }
                }

            KeywordsMask = KeywordPrototype.GetBitMaskForKeywordList(Keywords);

            // GetLevelAccessRestrictionMinMax client only?

            if (LootTables.HasValue())
                foreach (var lootTable in LootTables)
                {
                    if (lootTable.Name == AssetId.Invalid) continue;
                    if (_lootTableMap.TryGetValue(lootTable.Name, out var table) == false)
                    {
                        table = new();
                        _lootTableMap[lootTable.Name] = table;
                    }
                    table?.Add(lootTable);
                }

            RegionPrototypeEnumValue = GetEnumValueFromBlueprint(LiveTuningData.GetRegionBlueprintDataRef());

            if (AreasInGenerator == null)
            {
                AreasInGenerator = new();
                HashSet<PrototypeId> regions = new();
                GetAreasInGenerator(this, AreasInGenerator, regions);
            }

            // ClientMapOverrides client only
        }

        public static PrototypeId ConstrainDifficulty(PrototypeId regionProtoRef, PrototypeId difficultyTierProtoRef)
        {
            return ConstrainDifficulty(regionProtoRef.As<RegionPrototype>(), difficultyTierProtoRef.As<DifficultyTierPrototype>());
        }

        public static PrototypeId ConstrainDifficulty(RegionPrototype regionProto, DifficultyTierPrototype difficultyTierProto)
        {
            if (regionProto == null || difficultyTierProto == null)
                return PrototypeId.Invalid;

            DifficultyTierMask mask = regionProto.DifficultyTierMask;
            DifficultyTier tier = difficultyTierProto.Tier;

            // First try to downgrade
            for (DifficultyTier i = tier; i >= 0; i--)
            {
                if (mask.HasFlag((DifficultyTierMask)(1 << (int)i)))
                {
                    DifficultyTierPrototype constrainedDifficultyProto = GameDatabase.GlobalsPrototype.GetDifficultyTierByEnum(i);
                    return constrainedDifficultyProto.DataRef;
                }
            }

            // Now upgrade
            for (DifficultyTier i = tier + 1; i < DifficultyTier.NumTiers; i++)
            {
                if (mask.HasFlag((DifficultyTierMask)(1 << (int)i)))
                {
                    DifficultyTierPrototype constrainedDifficultyProto = GameDatabase.GlobalsPrototype.GetDifficultyTierByEnum(i);
                    return constrainedDifficultyProto.DataRef;
                }
            }

            // No available difficulty
            return PrototypeId.Invalid;
        }

        public bool HasKeyword(KeywordPrototype keywordProto)
        {
            return keywordProto != null && KeywordPrototype.TestKeywordBit(KeywordsMask, keywordProto);
        }

        public bool HasKeyword(PrototypeId keywordRef)
        {
            var keywordProto = GameDatabase.GetPrototype<KeywordPrototype>(keywordRef);
            return HasKeyword(keywordProto);
        }

        public bool AllowRaids()
        {
            var globalsProto = GameDatabase.GlobalsPrototype;
            if (globalsProto == null) return false;
            switch (Behavior)
            {
                case RegionBehavior.Town:
                case RegionBehavior.PublicCombatZone:
                case RegionBehavior.PrivateRaid:
                    return true;
                case RegionBehavior.PrivateStory:
                case RegionBehavior.PrivateNonStory:
                    return false;
                case RegionBehavior.MatchPlay:
                    int largestTeamSize = GetLargestTeamSize();
                    if (largestTeamSize > 0)
                        return largestTeamSize >= globalsProto.PlayerRaidMaxSize;
                    else if (PlayerLimit > 0)
                        return PlayerLimit >= globalsProto.PlayerRaidMaxSize;
                    break;
                default:
                    return false;
            }
            return false;
        }

        private int GetLargestTeamSize()
        {
            int largestTeamSize = 0;
            if(MetaGames.HasValue())
                foreach (var metaGameRef in MetaGames)
                {
                    var metaGameProto = GameDatabase.GetPrototype<MetaGamePrototype>(metaGameRef);
                    if (metaGameProto != null && metaGameProto.Teams.HasValue())
                        foreach (var teamRef in metaGameProto.Teams)
                        {
                            var teamProto = GameDatabase.GetPrototype<MetaGameTeamPrototype>(teamRef);
                            if (teamProto != null)
                                largestTeamSize = Math.Max(largestTeamSize, teamProto.MaxPlayers);
                        }
                }
            return largestTeamSize;
        }

        public static void BuildRegionsFromFilters(HashSet<PrototypeId> regions, PrototypeId[] includeRegions, bool includeChildren, PrototypeId[] excludeRegions)
        {
            if (includeRegions.HasValue())
                foreach (var regionRef in includeRegions)
                    if (regionRef != PrototypeId.Invalid) regions.Add(regionRef);

            if (includeChildren)
            {
                List<PrototypeId> parentRegions = ListPool<PrototypeId>.Instance.Get(regions);
                foreach (var childRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<RegionPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
                    foreach (var parentRef in parentRegions)
                        if (GameDatabase.DataDirectory.PrototypeIsAPrototype(childRef, parentRef))
                        {
                            regions.Add(childRef);
                            break;
                        }
                ListPool<PrototypeId>.Instance.Return(parentRegions);
            }

            if (excludeRegions.HasValue())
                foreach (var regionRef in excludeRegions)
                    regions.Remove(regionRef);

            List<PrototypeId> altRegions = ListPool<PrototypeId>.Instance.Get(regions);
            foreach (var regionRef in altRegions)
            {
                var regionProto = GameDatabase.GetPrototype<RegionPrototype>(regionRef);
                if (regionProto != null && regionProto.AltRegions.HasValue())
                    foreach (var altRegionRef in regionProto.AltRegions)
                        regions.Add(altRegionRef);
            }
            ListPool<PrototypeId>.Instance.Return(altRegions);
        }

        public static void GetAreasInGenerator(PrototypeId regionRef, HashSet<PrototypeId> areas)
        {
            if (regionRef == PrototypeId.Invalid) return;
            var regionProto = GameDatabase.GetPrototype<RegionPrototype>(regionRef);
            if (regionProto == null) return;

            if (regionProto.AreasInGenerator == null)
            {
                regionProto.AreasInGenerator = [];
                HashSet<PrototypeId> regions = HashSetPool<PrototypeId>.Instance.Get();
                GetAreasInGenerator(regionProto, regionProto.AreasInGenerator, regions);
                HashSetPool<PrototypeId>.Instance.Return(regions);
            }

            if (regionProto.AreasInGenerator != null)
                areas.Insert(regionProto.AreasInGenerator);
        }

        private static void GetAreasInGenerator(RegionPrototype regionProto, HashSet<PrototypeId> areas, HashSet<PrototypeId> regions)
        {
            if (regionProto == null) return;
            if (regions.Contains(regionProto.DataRef)) return;
            regions.Add(regionProto.DataRef);

            if (regionProto.AltRegions.HasValue())
                foreach (var altRegionRef in regionProto.AltRegions)
                {
                    var altRegionProto = GameDatabase.GetPrototype<RegionPrototype>(altRegionRef);
                    if (altRegionProto != null) GetAreasInGenerator(altRegionProto, areas, regions);
                }

            regionProto.RegionGenerator?.GetAreasInGenerator(areas);
        }

        public bool FilterRegion(PrototypeId filterRef, bool includeChildren, PrototypeId[] regionsExclude)
        {
            if (regionsExclude.HasValue())
                if (regionsExclude.Contains(DataRef)) return false;

            if (filterRef == PrototypeId.Invalid) return false;
            var filterProto = GameDatabase.GetPrototype<RegionPrototype>(filterRef);
            if (filterProto != null)
            {
                if (filterProto == this) return true;
                if (includeChildren && GameDatabase.DataDirectory.PrototypeIsAPrototype(DataRef, filterRef)) return true;
                if (filterProto.HasAltRegion(DataRef)) return true;
            }
            return false;
        }

        public RegionQueueStateEntryPrototype GetRegionQueueStateEntry(PrototypeId gameStateRef)
        {
            if (RegionQueueStates.HasValue())
                foreach (var entryProto in RegionQueueStates)
                {
                    if (entryProto == null) continue;
                    if (entryProto.State == gameStateRef)
                        return entryProto;
                }
            return null;
        }

        public PrototypeId GetMetagame()
        {
            if (MetaGames.HasValue()) return MetaGames[0];
            return PrototypeId.Invalid;
        }

        public bool HasEndless()
        {
            return RegionGenerator is SequenceRegionGeneratorPrototype sequenceRegion && sequenceRegion.EndlessThemes.HasValue();
        }

        public PrototypeId GetLootTableOverride(WorldEntity worldEntity, AssetId source, LootDropEventType lootEvent)
        {
            if (_lootTableMap.TryGetValue(source, out List<LootTableAssignmentPrototype> tables))
            {
                foreach (LootTableAssignmentPrototype tableAssignment in tables)
                {
                    LootDropEventType tableAssignmentEvent = tableAssignment.Event;

                    if (tableAssignmentEvent == lootEvent)
                        return tableAssignment.Table;

                    if (tableAssignmentEvent == LootDropEventType.None && (lootEvent == LootDropEventType.OnKilled || lootEvent == LootDropEventType.OnInteractedWith))
                        return tableAssignment.Table;
                }
            }

            return Logger.WarnReturn(PrototypeId.Invalid, $"GetLootTableOverride(): Region [{this}] has no overrides for source=[{source}], lootEvent=[{lootEvent}] requested by entity [{worldEntity}]");
        }

        public bool RunEvalAccessRestriction(Player player, Avatar avatar, PrototypeId difficultyProtoRef)
        {
            // Default to true if no valid avatar
            if (avatar == null) return Logger.WarnReturn(true, "RunEvalAccessRestriction(): avatar == null");

            bool success = true;

            if (AccessChecks.HasValue())
            {
                foreach (RegionAccessCheckPrototype checkProto in AccessChecks)
                {
                    if (checkProto == null)
                    {
                        Logger.Warn("RunEvalAccessRestriction(): checkProto == null");
                        continue;
                    }

                    if (checkProto.NoAccessOnFail == false)
                        continue;

                    success &= checkProto.Check(player, avatar);
                }
            }

            DifficultyTierPrototype difficultyProto = difficultyProtoRef.As<DifficultyTierPrototype>();
            if (success && difficultyProto != null)
                success &= avatar.CharacterLevel >= difficultyProto.UnlockLevel;

            if (success && EvalAccessRestriction != null)
            {
                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                evalContext.SetReadOnlyVar_ProtoRef(EvalContext.Var1, difficultyProtoRef);
                evalContext.SetReadOnlyVar_EntityPtr(EvalContext.Default, avatar);
                evalContext.SetReadOnlyVar_EntityPtr(EvalContext.Other, player);
                success = Eval.RunBool(EvalAccessRestriction, evalContext);
            }

            return success;
        }
    }

    public class RegionConnectionTargetPrototype : Prototype
    {
        public PrototypeId Region { get; protected set; }
        public PrototypeId Area { get; protected set; }
        public AssetId Cell { get; protected set; }
        public PrototypeId Entity { get; protected set; }
        public PrototypeId IntroKismetSeq { get; protected set; }
        public LocaleStringId Name { get; protected set; }
        public bool EnabledByDefault { get; protected set; }
        public int UISortOrder { get; protected set; }
    }

    public class ObjectiveGraphSettingsPrototype : Prototype
    {
        public ObjectiveGraphMode Mode { get; protected set; }
    }

    public class FactionLimitPrototype : Prototype
    {
        public PrototypeId Faction { get; protected set; }
        public int PlayerLimit { get; protected set; }
    }

    public class RegionAccessCheckPrototype : Prototype
    {
        public bool NoAccessOnFail { get; protected set; }
        public bool NoDisplayOnFail { get; protected set; }

        //---

        public virtual bool Check(Player player, Avatar avatar)
        {
            return false;
        }
    }

    public class LevelAccessCheckPrototype : RegionAccessCheckPrototype
    {
        public LocaleStringId UIResponseMessage { get; protected set; }
        public LocaleStringId UILevelRangeFormat { get; protected set; }
        public LocaleStringId UIMapDescriptionTag { get; protected set; }
        public LocaleStringId UIWaypointNameTag { get; protected set; }
        public int LevelMin { get; protected set; }
        public int LevelMax { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override bool Check(Player player, Avatar avatar)
        {
            if (player == null) return Logger.WarnReturn(false, "Check(): player == null");
            if (avatar == null) return Logger.WarnReturn(false, "Check(): avatar == null");

            int avatarLevel = avatar.CharacterLevel;

            // Check party
            if (NoAccessOnFail == false)
            {
                Party party = player.Party;
                if (party != null)
                {
                    CommunityMember communityMember = party.GetCommunityMemberForLeader(player);
                    if (communityMember != null)
                    {
                        AvatarSlotInfo avatarSlotInfo = communityMember.GetAvatarSlotInfo();
                        avatarLevel = avatarSlotInfo != null ? avatarSlotInfo.Level : 0;
                    }
                }
            }

            // Not doing LocaleString things because they are not needed server-side
            return avatarLevel >= LevelMin && avatarLevel <= LevelMax;
        }
    }

    public class RegionQueueStateEntryPrototype : Prototype
    {
        public PrototypeId StateParent { get; protected set; }
        public PrototypeId State { get; protected set; }
        public LocaleStringId QueueText { get; protected set; }

        //---

        [DoNotCopy]
        public int Index { get; set; }
    }

    public class DividedStartLocationPrototype : Prototype
    {
        public PrototypeId Target { get; protected set; }
        public int Players { get; protected set; }
    }

    public class RegionPortalControlEntryPrototype : Prototype
    {
        public PrototypeId Region { get; protected set; }
        public int UnlockDurationMinutes { get; protected set; }
        public int UnlockPeriodMinutes { get; protected set; }
    }

    public class RegionConnectionNodePrototype : Prototype
    {
        public PrototypeId Origin { get; protected set; }
        public PrototypeId Target { get; protected set; }
        public RegionTransitionDirectionality Type { get; protected set; }
    }

    public class ZoneLevelPrototype : Prototype
    {
    }

    public class ZoneLevelFixedPrototype : ZoneLevelPrototype
    {
        public short level { get; protected set; }
    }

    public class ZoneLevelRelativePrototype : ZoneLevelPrototype
    {
        public short modmax { get; protected set; }
        public short modmin { get; protected set; }
    }

    public class BlackOutZonePrototype : Prototype
    {
        public float BlackOutRadius { get; protected set; }
    }
}
