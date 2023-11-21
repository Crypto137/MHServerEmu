using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class UIPrototype : Prototype
    {
        public UIPanelPrototype[] UIPanels;
        public UIPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(UIPrototype), proto); }
    }

    public class UIPanelPrototype : Prototype
    {
        public ulong PanelName;
        public ulong TargetName;
        public PanelScaleMode ScaleMode;
        public UIPanelPrototype[] Children;
        public ulong WidgetClass;
        public ulong SWFName;
        public bool OpenOnStart;
        public bool VisibilityToggleable;
        public bool CanClickThrough;
        public bool StaticPosition;
        public bool EntityInteractPanel;
        public bool UseNewPlacementSystem;
        public bool KeepLoaded;
        public UIPanelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(UIPanelPrototype), proto); }
    }

    public class AnchoredPanelPrototype : UIPanelPrototype
    {
        public Vector2 SourceAttachmentPin;
        public Vector2 TargetAttachmentPin;
        public Vector2 VirtualPixelOffset;
        public ulong PreferredLane;
        public Vector2 OuterEdgePin;
        public Vector2 NewSourceAttachmentPin;
        public AnchoredPanelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AnchoredPanelPrototype), proto); }
    }

    public class StretchedPanelPrototype : UIPanelPrototype
    {
        public Vector2 TopLeftPin;
        public ulong TL_X_TargetName;
        public ulong TL_Y_TargetName;
        public Vector2 BottomRightPin;
        public ulong BR_X_TargetName;
        public ulong BR_Y_TargetName;
        public StretchedPanelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(StretchedPanelPrototype), proto); }
    }

    public class UILocalizedInfoPrototype : Prototype
    {
        public ulong DisplayText;
        public ulong TooltipText;
        public ulong TooltipStyle;
        public ulong TooltipFont;
        public UILocalizedInfoPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(UILocalizedInfoPrototype), proto); }
    }

    public class UILocalizedStatInfoPrototype : UILocalizedInfoPrototype
    {
        public ulong Stat;
        public int StatValue;
        public ulong LevelUnlockTooltipStyle;
        public TooltipSectionPrototype[] TooltipSectionList;
        public UILocalizedStatInfoPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(UILocalizedStatInfoPrototype), proto); }
    }

    public class UICraftingTabLabelPrototype : UILocalizedInfoPrototype
    {
        public int SortOrder;
        public UICraftingTabLabelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(UICraftingTabLabelPrototype), proto); }
    }

    public class InventoryUIDataPrototype : Prototype
    {
        public ulong EmptySlotTooltip;
        public ulong SlotBackgroundIcon;
        public ulong InventoryItemDisplayName;
        public bool HintSlots;
        public ulong SlotBackgroundIconHiRes;
        public InventoryUIDataPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(InventoryUIDataPrototype), proto); }
    }

    public class OfferingInventoryUIDataPrototype : Prototype
    {
        public ulong NotificationIcon;
        public ulong NotificationTooltip;
        public ulong OfferingDescription;
        public ulong OfferingTitle;
        public OfferingInventoryUIDataPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(OfferingInventoryUIDataPrototype), proto); }
    }

    public class TipEntryPrototype : Prototype
    {
        public ulong Entry;
        public int Weight;
        public bool SkipIfOnPC;
        public bool SkipIfOnPS4;
        public bool SkipIfOnXBox;
        public TipEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TipEntryPrototype), proto); }
    }

    public class TipEntryCollectionPrototype : Prototype
    {
        public TipEntryPrototype[] TipEntries;
        public TipEntryCollectionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TipEntryCollectionPrototype), proto); }
    }

    public class GenericTipEntryCollectionPrototype : TipEntryCollectionPrototype
    {
        public GenericTipEntryCollectionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(GenericTipEntryCollectionPrototype), proto); }
    }

    public class RegionTipEntryCollectionPrototype : TipEntryCollectionPrototype
    {
        public ulong RegionBindings;
        public RegionTipEntryCollectionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RegionTipEntryCollectionPrototype), proto); }
    }

    public class AvatarTipEntryCollectionPrototype : TipEntryCollectionPrototype
    {
        public ulong AvatarBindings;
        public AvatarTipEntryCollectionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AvatarTipEntryCollectionPrototype), proto); }
    }

    public class WeightedTipCategoryPrototype : Prototype
    {
        public TipTypeEnum TipType;
        public int Weight;
        public WeightedTipCategoryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(WeightedTipCategoryPrototype), proto); }
    }

    public class TransitionUIPrototype : Prototype
    {
        public WeightedTipCategoryPrototype[] TipCategories;
        public TransitionUIType TransitionType;
        public int Weight;
        public TransitionUIPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TransitionUIPrototype), proto); }
    }

    public class AvatarSynergyUIDataPrototype : Prototype
    {
        public ulong DisplayName;
        public ulong IconPath;
        public ulong SynergyActiveValue;
        public ulong SynergyInactiveValue;
        public ulong TooltipTextForList;
        public ulong IconPathHiRes;
        public AvatarSynergyUIDataPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AvatarSynergyUIDataPrototype), proto); }
    }

    public class MetaGameDataPrototype : Prototype
    {
        public ulong Descriptor;
        public bool DisplayMissionName;
        public int SortPriority;
        public ulong IconHeader;
        public int Justification;
        public ulong WidgetMovieClipOverride;
        public ulong IconHeaderHiRes;
        public MetaGameDataPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaGameDataPrototype), proto); }
    }

    public class UIWidgetGenericFractionPrototype : MetaGameDataPrototype
    {
        public ulong IconComplete;
        public ulong IconIncomplete;
        public int IconSpacing;
        public ulong IconCompleteHiRes;
        public ulong IconIncompleteHiRes;
        public UIWidgetGenericFractionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(UIWidgetGenericFractionPrototype), proto); }
    }

    public class UIWidgetEntityIconsEntryPrototype : Prototype
    {
        public EntityFilterPrototype Filter;
        public int Count;
        public UIWidgetEntityState TreatUnknownAs;
        public ulong Icon;
        public ulong Descriptor;
        public ulong IconDead;
        public int IconSpacing;
        public ulong IconHiRes;
        public ulong IconDeadHiRes;
        public UIWidgetEntityIconsEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(UIWidgetEntityIconsEntryPrototype), proto); }
    }
    public enum UIWidgetEntityState
    {
        Unknown = 0,
        Alive = 1,
        Dead = 2,
    }
    public class UIWidgetEnrageEntryPrototype : UIWidgetEntityIconsEntryPrototype
    {
        public UIWidgetEnrageEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(UIWidgetEnrageEntryPrototype), proto); }
    }

    public class WidgetPropertyEntryPrototype : Prototype
    {
        public ulong Color;
        public ulong Descriptor;
        public ulong Icon;
        public EvalPrototype PropertyEval;
        public ulong IconHiRes;
        public WidgetPropertyEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(WidgetPropertyEntryPrototype), proto); }
    }

    public class UIWidgetEntityPropertyEntryPrototype : UIWidgetEntityIconsEntryPrototype
    {
        public WidgetPropertyEntryPrototype[] PropertyEntryTable;
        public EvalPrototype PropertyEval;
        public UIWidgetEntityPropertyEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(UIWidgetEntityPropertyEntryPrototype), proto); }
    }

    public class HealthPercentIconPrototype : Prototype
    {
        public ulong Color;
        public int HealthPercent;
        public ulong Icon;
        public ulong Descriptor;
        public ulong IconHiRes;
        public HealthPercentIconPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(HealthPercentIconPrototype), proto); }
    }

    public class UIWidgetHealthPercentEntryPrototype : UIWidgetEntityIconsEntryPrototype
    {
        public bool ColorBasedOnHealth;
        public HealthPercentIconPrototype[] HealthDisplayTable;
        public UIWidgetHealthPercentEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(UIWidgetHealthPercentEntryPrototype), proto); }
    }

    public class UIWidgetEntityIconsPrototype : MetaGameDataPrototype
    {
        public UIWidgetEntityIconsEntryPrototype[] Entities;
        public UIWidgetEntityIconsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(UIWidgetEntityIconsPrototype), proto); }
    }

    public class UIWidgetMissionTextPrototype : MetaGameDataPrototype
    {
        public UIWidgetMissionTextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(UIWidgetMissionTextPrototype), proto); }
    }

    public class UIWidgetButtonPrototype : MetaGameDataPrototype
    {
        public UIWidgetButtonPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(UIWidgetButtonPrototype), proto); }
    }

    public class UIWidgetReadyCheckPrototype : MetaGameDataPrototype
    {
        public UIWidgetReadyCheckPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(UIWidgetReadyCheckPrototype), proto); }
    }

    public class UIWidgetPanelPrototype : Prototype
    {
        public ulong[] Widgets;
        public UIWidgetPanelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(UIWidgetPanelPrototype), proto); }
    }

    public class UIWidgetTopPanelPrototype : UIWidgetPanelPrototype
    {
        public UIWidgetTopPanelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(UIWidgetTopPanelPrototype), proto); }
    }

    public class LogoffPanelEntryPrototype : Prototype
    {
        public ulong Description;
        public ulong GameModeType;
        public ulong Header;
        public ulong Image;
        public int Priority;
        public ulong Title;
        public LogoffPanelEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LogoffPanelEntryPrototype), proto); }
    }

    public class StoreCategoryPrototype : Prototype
    {
        public ulong Icon;
        public ulong Identifier;
        public ulong Label;
        public StoreCategoryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(StoreCategoryPrototype), proto); }
    }

    public class ReputationLevelDisplayInfoPrototype : Prototype
    {
        public ulong DisplayName;
        public ulong IconPath;
        public int ReputationLevel;
        public ReputationLevelDisplayInfoPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ReputationLevelDisplayInfoPrototype), proto); }
    }

    public class ReputationDisplayInfoPrototype : Prototype
    {
        public ulong DisplayName;
        public ReputationLevelDisplayInfoPrototype ReputationLevels;
        public ReputationDisplayInfoPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ReputationDisplayInfoPrototype), proto); }
    }

    public class UISystemLockPrototype : Prototype
    {
        public ulong GameNotification;
        public ulong UISystem;
        public int UnlockLevel;
        public bool IsNewPlayerExperienceLocked;
        public UISystemLockPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(UISystemLockPrototype), proto); }
    }

    public class IconPackagePrototype : Prototype
    {
        public ulong Package;
        public bool AlwaysLoaded;
        public bool EnableStreaming;
        public bool HighPriorityStreaming;
        public bool RemoveUnreferencedContent;
        public IconPackagePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(IconPackagePrototype), proto); }
    }

    public class RadialMenuEntryPrototype : Prototype
    {
        public ulong ImageNormal;
        public ulong ImageSelected;
        public ulong LocalizedName;
        public ulong Panel;
        public RadialMenuEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RadialMenuEntryPrototype), proto); }
    }

    public class InputBindingPrototype : Prototype
    {
        public ulong DisplayText;
        public ulong BindingName;
        public ulong TutorialImage;
        public ulong TutorialImageOverlayText;
        public ulong ControlScheme;
        public InputBindingPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(InputBindingPrototype), proto); }
    }

    public class PanelLoaderTabPrototype : Prototype
    {
        public ulong Context;
        public ulong DisplayName;
        public ulong Panel;
        public bool ShowAvatarInfo;
        public ulong SubTabs;
        public ulong Icon;
        public bool ShowLocalPlayerName;
        public PanelLoaderTabPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PanelLoaderTabPrototype), proto); }
    }

    public class PanelLoaderTabListPrototype : Prototype
    {
        public ulong[] Tabs;
        public bool IsSubTabList;
        public PanelLoaderTabListPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PanelLoaderTabListPrototype), proto); }
    }

    public class ConsoleRadialMenuEntryPrototype : Prototype
    {
        public ulong DisplayName;
        public ulong ImageNormal;
        public ulong ImageSelected;
        public ulong TabList;
        public ConsoleRadialMenuEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ConsoleRadialMenuEntryPrototype), proto); }
    }

    public enum ButtonStyle {
	    Default,
	    Primary,
	    SecondaryPositive,
	    SecondaryNegative,
	    Terciary,
    }

    public class DialogPrototype : Prototype
    {
        public ulong Text;
        public ulong Button1;
        public ulong Button2;
        public ButtonStyle Button1Style;
        public ButtonStyle Button2Style;
        public DialogPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(DialogPrototype), proto); }
    }

    public enum TransitionUIType
    {
        Environment,
        HeroOwned,
    }

    public enum TipTypeEnum
    {
        GenericGameplay,
        SpecificGameplay,
    }

    public class MissionTrackerFilterPrototype : Prototype
    {
        public MissionTrackerFilterTypeEnum FilterType;
        public ulong Label;
        public bool DisplayByDefault;
        public int DisplayOrder;
        public MissionTrackerFilterPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionTrackerFilterPrototype), proto); }
    }
    public enum MissionTrackerFilterTypeEnum
    {
        None = -1,
        Standard = 0,
        PvE = 1,
        PvP = 2,
        Daily = 3,
        Challenge = 4,
    }
    public class LocalizedTextAndImagePrototype : Prototype
    {
        public ulong Image;
        public ulong Text;
        public LocalizedTextAndImagePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LocalizedTextAndImagePrototype), proto); }
    }

    public class TextStylePrototype : Prototype
    {
        public bool Bold;
        public ulong Color;
        public ulong Tag;
        public bool Underline;
        public int FontSize;
        public ulong Alignment;
        public bool Hidden;
        public int FontSizeConsole;
        public TextStylePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TextStylePrototype), proto); }
    }

    public class UINotificationPrototype : Prototype
    {
        public UINotificationPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(UINotificationPrototype), proto); }
    }


    public class BannerMessagePrototype : UINotificationPrototype
    {
        public ulong BannerText;
        public int TimeToLiveMS;
        public BannerMessageStyle MessageStyle;
        public bool DoNotQueue;
        public ulong TextStyle;
        public bool ShowImmediately;
        public BannerMessagePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(BannerMessagePrototype), proto); }
    }
    public enum BannerMessageStyle
    {
        Standard = 0,
        Error = 1,
        FlyIn = 2,
        TimeBonusBronze = 3,
        TimeBonusSilver = 4,
        TimeBonusGold = 5,
    }
    public class GameNotificationPrototype : UINotificationPrototype
    {
        public ulong BannerText;
        public GameNotificationType GameNotificationType;
        public ulong IconPath;
        public ulong TooltipText;
        public bool PlayAudio;
        public BannerMessageType BannerType;
        public bool FlashContinuously;
        public bool StackNotifications;
        public bool ShowTimer;
        public bool ShowScore;
        public ulong TooltipStyle;
        public ulong TooltipFont;
        public ulong DisplayText;
        public int MinimizeTimeDelayMS;
        public int DurationMS;
        public bool ShowAnimatedCircle;
        public bool Unique;
        public ulong[] OnCreateRemoveNotifications;
        public bool RemoveOnRegionChange;
        public bool ShowOnSystemLock;
        public GameNotificationPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(GameNotificationPrototype), proto); }
    }
    public enum BannerMessageType
    {
        Default = 0,
        DiscoveryCompleted = 1,
        LevelUp = 2,
        ItemError = 4,
        PartyInvite = 5,
        PartyError = 6,
        GuildInvite = 7,
        PowerError = 8,
        PowerErrorDoNotQueue = 9,
        PvPDisabledPortalFail = 10,
        PvPFactionPortalFail = 11,
        PvPPartyPortalFail = 12,
        RegionChange = 13,
        MissionAccepted = 14,
        MissionCompleted = 15,
        MissionFailed = 16,
        WaypointUnlocked = 17,
        PowerUnlocked = 18,
        PowerPointsAwarded = 3,
        StatProgression = 19,
        AvatarSwitchError = 20,
        OmegaPointsAwarded = 21,
        UISystemUnlocked = 22,
        LeaderboardRewarded = 23,
        InfinityPointsAwarded = 26,
        WaypointError = 27,
    }

    public class StoryNotificationPrototype : UINotificationPrototype
    {
        public ulong DisplayText;
        public int TimeToLiveMS;
        public ulong SpeakingEntity;
        public ulong VOTrigger;
        public StoryNotificationPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(StoryNotificationPrototype), proto); }
    }

    public class VOStoryNotificationPrototype : Prototype
    {
        public VOEventType VOEventType;
        public StoryNotificationPrototype StoryNotification;
        public VOStoryNotificationPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(VOStoryNotificationPrototype), proto); }
    }
    public enum VOEventType
    {
        Spawned = 1,
        Aggro = 2,
    }
    public class ConsoleHUDNotificationPrototype : Prototype
    {
        public ulong DisplayName;
        public int DurationMS;
        public ConsoleHUDNotificationType NotificationType;
        public ulong OpensPanel;
        public ulong PanelContext;
        public ConsoleHUDNotificationPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ConsoleHUDNotificationPrototype), proto); }
    }
    public enum ConsoleHUDNotificationType
    {
        NewPower = 1,
        NewItem = 2,
        QueueReady = 3,
        QueueGracePeriodAboutToExpire = 4,
        QueueEntered = 5,
        NewSynergy = 6,
        NewInfinityGemUpgrade = 7,
        InfinityUnlocked = 8,
        NewTalent = 9,
        NewDeliveryItem = 10,
        LegendaryQuestsUnlocked = 11,
        OmegaWeekAvailable = 12,
        GlobalEventAvailable = 13,
    }
    public class HUDTutorialPrototype : UINotificationPrototype
    {
        public ulong Description;
        public int DisplayDurationMS;
        public ulong Image;
        public ulong ImageOverlayText;
        public ulong Title;
        public ulong[] HighlightAvatars;
        public ulong[] HighlightPowers;
        public bool AllowMovement;
        public bool AllowPowerUsage;
        public bool AllowTakingDamage;
        public bool CanDismiss;
        public bool HighlightFirstEmptyPowerSlot;
        public bool HighlightUpgradeablePowers;
        public bool HighlightUnusedPowers;
        public bool HighlightUnequippedItem;
        public bool CloseOnRegionLeave;
        public ulong ImageFromCommand;
        public ulong DescriptionGamepad;
        public ulong DescriptionNoBindings;
        public ulong ImageFromCommandGamepad;
        public ulong[] HighlightTeamUps;
        public bool SkipIfOnConsole;
        public bool SkipIfOnPC;
        public bool SkipIfUsingGamepad;
        public bool SkipIfUsingKeyboardMouse;
        public float ScreenPositionConsoleX;
        public float ScreenPositionConsoleY;
        public float ScreenPositionX;
        public float ScreenPositionY;
        public int FlashDelayMS;
        public bool ShowOncePerAccount;
        public HUDTutorialPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(HUDTutorialPrototype), proto); }
    }

    public class SessionImagePrototype : Prototype
    {
        public ulong SessionImageAsset;
        public SessionImagePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SessionImagePrototype), proto); }
    }

    public class CurrencyDisplayPrototype : Prototype
    {
        public ulong DisplayName;
        public ulong DisplayColor;
        public ulong IconPath;
        public ulong PropertyValueToDisplay;
        public ulong TooltipText;
        public bool UseGsBalance;
        public ulong CurrencyToDisplay;
        public ulong IconPathHiRes;
        public sbyte CategoryIndex;
        public ulong CategoryName;
        public bool HideIfOnConsole;
        public bool HideIfOnPC;
        public CurrencyDisplayPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(CurrencyDisplayPrototype), proto); }
    }

    public class FullscreenMoviePrototype : Prototype
    {
        public ulong MovieName;
        public bool Skippable;
        public MovieType MovieType;
        public bool ExitGameAfterPlay;
        public ulong MovieTitle;
        public ulong Banter;
        public ulong YouTubeVideoID;
        public bool YouTubeControlsEnabled;
        public ulong StreamingMovieNameHQ;
        public ulong StreamingMovieNameLQ;
        public ulong StreamingMovieNameMQ;
        public FullscreenMoviePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(FullscreenMoviePrototype), proto); }
    }
    public enum MovieType
    {
        None = 0,
        Loading = 1,
        TeleportFar = 2,
        Cinematic = 3,
    }

    public class LoadingScreenPrototype : Prototype
    {
        public ulong LoadingScreenAsset;
        public ulong Title;
        public LoadingScreenPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LoadingScreenPrototype), proto); }
    }
    public class UICinematicsListPrototype : Prototype
    {
        public ulong CinematicsListToPopulate;
        public UICinematicsListPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(UICinematicsListPrototype), proto); }
    }
}
