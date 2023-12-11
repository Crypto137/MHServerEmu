using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class RegionPrototype : Prototype
    {
        public ulong ClientMap;
        public ulong BodySliderTarget;
        public ulong StartTarget;
        public ulong[] Music;
        public RegionGeneratorPrototype RegionGenerator;
        public RegionBehaviorAsset Behavior;
        public ulong RegionName;
        public ulong MetaGames;
        public bool ForceSimulation;
        public ulong LoadingScreens;
        public bool AlwaysRevealFullMap;
        public ulong Chapter;
        public int PlayerLimit;
        public float LifetimeInMinutes;
        public ulong WaypointAutoUnlock;
        public bool PartyFormationAllowed;
        public TransitionUIPrototype[] TransitionUITypes;
        public ulong AmbientSfx;
        public ulong[] PowerKeywordBlacklist;
        public bool CloseWhenReservationsReachesZero;
        public float UIMapWallThickness;
        public ulong[] PopulationOverrides;
        public int Level;
        public MissionTrackerFilterType[] MissionTrackerFilterList;
        public bool AllowAutoPartyOnEnter;
        public float AutoPartyWindowSecs;
        public bool DailyCheckpointStartTarget;
        public int LowPopulationPlayerLimit;
        public ulong RespawnOverride;
        public ulong PlayerCameraSettings;
        public RegionQueueMethod RegionQueueMethod;
        public EvalPrototype EvalAccessRestriction;
        public ulong WaypointAutoUnlockList;
        public bool AlwaysShutdownWhenVacant;
        public bool SynergyEditAllowed;
        public ulong[] Keywords;
        public ulong UITopPanel;
        public ulong[] AltRegions;
        public RegionAccessCheckPrototype[] AccessChecks;
        public ulong UIDescription;
        public ulong UILocation;
        public bool PausesBoostConditions;
        public bool ShowTransitionIndicators;
        public RegionQueueStateEntryPrototype[] RegionQueueStates;
        public ulong MarkerFilter;
        public bool LevelBandedRegionUsesPlayerLevel;
        public FootstepTraceBehaviorAsset FootstepTraceOverride;
        public bool QueueDoNotWaitToFull;
        public bool DisplayCommunityNews;
        public ulong UnrealClass;
        public bool RespawnDestructibles;
        public ulong PropertyGameModeSetOnEntry;
        public bool UsePrevRegionPlayerDeathCount;
        public LootTableAssignmentPrototype[] LootTables;
        public ulong AffixTable;
        public ObjectiveGraphSettingsPrototype ObjectiveGraph;
        public DividedStartLocationPrototype[] DividedStartLocations;
        public RegionMusicBehaviorAsset MusicBehavior;
        public ulong AvatarObjectiveInfoOverride;
        public RegionDifficultySettingsPrototype DifficultySettings;
        public bool LevelOverridesCharacterLevel;
        public bool LevelUseAreaOffset;
        public ulong EvalAccessRestrictionMessage;
        public bool BodySliderOneWay;
        public bool EnableAvatarSwap;
        public ulong[] RestrictedRoster;
        public ulong[] AvatarPowers;
        public bool IsNPE;
        public ulong PresenceStatusText;
        public ulong[] AccessDifficulties;
        public ulong Tuning;
        public int BonusItemFindMultiplier;
        public ulong PlayerCameraSettingsOrbis;
        public ulong LoadingScreensConsole;
        public bool AllowLocalCoopMode;

        public RegionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RegionPrototype), proto); }

        public static bool Equivalent(RegionPrototype regionA, RegionPrototype regionB)
        {
            if (regionA == null || regionB == null)  return false;
            if (regionA == regionB) return true;
            return regionA.HasAltRegion(regionB.GetDataRef());
        }

        private bool HasAltRegion(ulong dataRef)
        {
            if (AltRegions!=null) return AltRegions.Contains(dataRef);
            return false;
        }

        public ulong GetDefaultArea(Region region)
        {
            ulong defaultArea = 0;

            if (StartTarget != 0)
            {
                RegionConnectionTargetPrototype target = new(StartTarget.GetPrototype());
                defaultArea = target.Area;
            }
            
            if (defaultArea == 0)
            {
                return RegionGenerator.GetStartAreaRef(region); // TODO check return
            }

            return defaultArea;
        }

        public RegionDifficultySettingsPrototype GetDifficultySettings()
        {
            if (DifficultySettings != null) return DifficultySettings;

            DifficultyGlobalsPrototype difficultyGlobals = GameDatabase.GetDifficultyGlobalsPrototype();
            if (difficultyGlobals == null) return null;

            if (Behavior == RegionBehaviorAsset.PublicCombatZone && difficultyGlobals.RegionSettingsDefaultPCZ != null)
                return difficultyGlobals.RegionSettingsDefaultPCZ;

            return difficultyGlobals.RegionSettingsDefault;
        }

    }

    public class RegionConnectionTargetPrototype : Prototype
    {
        public ulong Region;
        public ulong Area;
        public ulong Cell;
        public ulong Entity;
        public bool EnabledByDefault;
        public ulong IntroKismetSeq;
        public ulong Name;
        public int UISortOrder;
        public RegionConnectionTargetPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RegionConnectionTargetPrototype), proto); }
    }

    #region Enum
    public enum MissionTrackerFilterType
    {
	    None = -1,
	    Standard = 0,
	    PvE = 1,
	    PvP = 2,
	    Daily = 3,
	    Challenge = 4,
    }

    public enum RegionBehaviorAsset
    {
	    Invalid = -1,
	    Town = 0,
	    PublicCombatZone = 1,
	    PrivateStory = 2,
	    PrivateNonStory = 3,
	    PrivateRaid = 5,
	    MatchPlay = 4,
    }

    public enum RegionMusicBehaviorAsset
    {
        None,
        Default,
        Mission,
    }

    public enum FootstepTraceBehaviorAsset
    {
        None,
        Enable,
        Disable,
    }
    public enum RegionQueueMethod {
	    None = 0,
	    PvPQueue = 1,
	    DailyQueue = 5,
    }
    #endregion

    public class ObjectiveGraphSettingsPrototype : Prototype
    {
        public ObjectiveGraphModeAsset Mode;
        public ObjectiveGraphSettingsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ObjectiveGraphSettingsPrototype), proto); }
    }

    public enum ObjectiveGraphModeAsset {
	    Off,
	    PathDistance,
	    PathNavi,
    }

    public class FactionLimitPrototype : Prototype
    {
        public ulong Faction;
        public int PlayerLimit;
        public FactionLimitPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(FactionLimitPrototype), proto); }
    }

    public class RegionAccessCheckPrototype : Prototype
    {
        public bool NoAccessOnFail;
        public bool NoDisplayOnFail;

        public RegionAccessCheckPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RegionAccessCheckPrototype), proto); }
    }

    public class LevelAccessCheckPrototype : RegionAccessCheckPrototype
    {
        public ulong UIResponseMessage;
        public ulong UILevelRangeFormat;
        public ulong UIMapDescriptionTag;
        public ulong UIWaypointNameTag;
        public int LevelMin;
        public int LevelMax;

        public LevelAccessCheckPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LevelAccessCheckPrototype), proto); }
    }
    public class RegionQueueStateEntryPrototype : Prototype
    {
        public ulong StateParent;
        public ulong State;
        public ulong QueueText;

        public RegionQueueStateEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RegionQueueStateEntryPrototype), proto); }
    }

    public class DividedStartLocationPrototype : Prototype
    {
        public ulong Target;
        public int Players;
        public DividedStartLocationPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(DividedStartLocationPrototype), proto); }
    }

    public class RegionPortalControlEntryPrototype : Prototype
    {
        public ulong Region;
        public int UnlockDurationMinutes;
        public int UnlockPeriodMinutes;
        public RegionPortalControlEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RegionPortalControlEntryPrototype), proto); }
    }


    public class RegionConnectionNodePrototype : Prototype
    {
        public ulong Origin;
        public ulong Target;
        public RegionTransitionDirectionality Type;
        public RegionConnectionNodePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RegionConnectionNodePrototype), proto); }
    }
    public enum RegionTransitionDirectionality
    {
        BiDirectional = 0,
        OneWay = 1,
        Disabled = 2,
    }
    public class ZoneLevelPrototype : Prototype
    {
        public ZoneLevelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ZoneLevelPrototype), proto); }
    }

    public class ZoneLevelFixedPrototype : ZoneLevelPrototype
    {
        public short level;
        public ZoneLevelFixedPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ZoneLevelFixedPrototype), proto); }
    }

    public class ZoneLevelRelativePrototype : ZoneLevelPrototype
    {
        public short modmax;
        public short modmin;
        public ZoneLevelRelativePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ZoneLevelRelativePrototype), proto); }
    }

    public class BlackOutZonePrototype : Prototype
    {
        public float BlackOutRadius;
        public BlackOutZonePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(BlackOutZonePrototype), proto); }
    }
}
