using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Resources;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    public enum PanelScaleMode
    {
        None,
        XStretch,
        YOnly,
        XOnly,
        Both,
        ScreenSize
    }

    [AssetEnum]
    public enum UIWidgetEntityState
    {
        Unknown = 0,
        Alive = 1,
        Dead = 2,
    }

    [AssetEnum]
    public enum ButtonStyle
    {
        Default,
        Primary,
        SecondaryPositive,
        SecondaryNegative,
        Terciary,
    }

    [AssetEnum]
    public enum TransitionUIType
    {
        Environment,
        HeroOwned,
    }

    [AssetEnum]
    public enum TipTypeEnum
    {
        GenericGameplay,
        SpecificGameplay,
    }

    [AssetEnum]
    public enum MissionTrackerFilterTypeEnum
    {
        None = -1,
        Standard = 0,
        PvE = 1,
        PvP = 2,
        Daily = 3,
        Challenge = 4,
    }

    [AssetEnum]
    public enum BannerMessageStyle
    {
        Standard = 0,
        Error = 1,
        FlyIn = 2,
        TimeBonusBronze = 3,
        TimeBonusSilver = 4,
        TimeBonusGold = 5,
    }

    [AssetEnum]
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

    [AssetEnum]
    public enum VOEventType
    {
        Spawned = 1,
        Aggro = 2,
    }

    [AssetEnum]
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

    [AssetEnum]
    public enum MovieType
    {
        None = 0,
        Loading = 1,
        TeleportFar = 2,
        Cinematic = 3,
    }

    #endregion

    #region Resource UI prototypes

    public class UIPrototype : Prototype, IBinaryResource
    {
        public UIPanelPrototype[] UIPanels { get; private set; }

        public void Deserialize(BinaryReader reader)
        {
            UIPanels = new UIPanelPrototype[reader.ReadUInt32()];
            for (int i = 0; i < UIPanels.Length; i++)
                UIPanels[i] = UIPanelPrototype.ReadFromBinaryReader(reader);
        }
    }

    public class UIPanelPrototype : Prototype
    {
        public string PanelName { get; private set; }
        public string TargetName { get; private set; }
        public PanelScaleMode ScaleMode { get; private set; }
        public UIPanelPrototype Children { get; private set; }
        public string WidgetClass { get; private set; }
        public string SwfName { get; private set; }
        public byte OpenOnStart { get; private set; }
        public byte VisibilityToggleable { get; private set; }
        public byte CanClickThrough { get; private set; }
        public byte StaticPosition { get; private set; }
        public byte EntityInteractPanel { get; private set; }
        public byte UseNewPlacementSystem { get; private set; }
        public byte KeepLoaded { get; private set; }

        public static UIPanelPrototype ReadFromBinaryReader(BinaryReader reader)
        {
            var hash = (ResourcePrototypeHash)reader.ReadUInt32();

            switch (hash)
            {
                case ResourcePrototypeHash.StretchedPanelPrototype:
                    return new StretchedPanelPrototype(reader);
                case ResourcePrototypeHash.AnchoredPanelPrototype:
                    return new AnchoredPanelPrototype(reader);
                case ResourcePrototypeHash.None:
                    return null;
                default:    // Throw an exception if there's a hash for a type we didn't expect
                    throw new NotImplementedException($"Unknown ResourcePrototypeHash {(uint)hash}.");
            }
        }

        protected void ReadCommonPanelFields(BinaryReader reader)
        {
            PanelName = reader.ReadFixedString32();
            TargetName = reader.ReadFixedString32();
            ScaleMode = (PanelScaleMode)reader.ReadUInt32();
            Children = ReadFromBinaryReader(reader);
            WidgetClass = reader.ReadFixedString32();
            SwfName = reader.ReadFixedString32();
            OpenOnStart = reader.ReadByte();
            VisibilityToggleable = reader.ReadByte();
            CanClickThrough = reader.ReadByte();
            StaticPosition = reader.ReadByte();
            EntityInteractPanel = reader.ReadByte();
            UseNewPlacementSystem = reader.ReadByte();
            KeepLoaded = reader.ReadByte();
        }
    }

    public class StretchedPanelPrototype : UIPanelPrototype
    {
        public Vector2 TopLeftPin { get; }
        public string TL_X_TargetName { get; }
        public string TL_Y_TargetName { get; }
        public Vector2 BottomRightPin { get; }
        public string BR_X_TargetName { get; }
        public string BR_Y_TargetName { get; }

        public StretchedPanelPrototype(BinaryReader reader)
        {
            TopLeftPin = reader.ReadVector2();
            TL_X_TargetName = reader.ReadFixedString32();
            TL_Y_TargetName = reader.ReadFixedString32();
            BottomRightPin = reader.ReadVector2();
            BR_X_TargetName = reader.ReadFixedString32();
            BR_Y_TargetName = reader.ReadFixedString32();

            ReadCommonPanelFields(reader);
        }
    }

    public class AnchoredPanelPrototype : UIPanelPrototype
    {
        public Vector2 SourceAttachmentPin { get; }
        public Vector2 TargetAttachmentPin { get; }
        public Vector2 VirtualPixelOffset { get; }
        public string PreferredLane { get; }
        public Vector2 OuterEdgePin { get; }
        public Vector2 NewSourceAttachmentPin { get; }

        public AnchoredPanelPrototype(BinaryReader reader)
        {
            SourceAttachmentPin = reader.ReadVector2();
            TargetAttachmentPin = reader.ReadVector2();
            VirtualPixelOffset = reader.ReadVector2();
            PreferredLane = reader.ReadFixedString32();
            OuterEdgePin = reader.ReadVector2();
            NewSourceAttachmentPin = reader.ReadVector2();

            ReadCommonPanelFields(reader);
        }
    }

    #endregion

    public class UILocalizedInfoPrototype : Prototype
    {
        public ulong DisplayText { get; protected set; }
        public ulong TooltipText { get; protected set; }
        public ulong TooltipStyle { get; protected set; }
        public ulong TooltipFont { get; protected set; }
    }

    public class UILocalizedStatInfoPrototype : UILocalizedInfoPrototype
    {
        public ulong Stat { get; protected set; }
        public int StatValue { get; protected set; }
        public ulong LevelUnlockTooltipStyle { get; protected set; }
        public TooltipSectionPrototype[] TooltipSectionList { get; protected set; }
    }

    public class UICraftingTabLabelPrototype : UILocalizedInfoPrototype
    {
        public int SortOrder { get; protected set; }
    }

    public class InventoryUIDataPrototype : Prototype
    {
        public ulong EmptySlotTooltip { get; protected set; }
        public ulong SlotBackgroundIcon { get; protected set; }
        public ulong InventoryItemDisplayName { get; protected set; }
        public bool HintSlots { get; protected set; }
        public ulong SlotBackgroundIconHiRes { get; protected set; }
    }

    public class OfferingInventoryUIDataPrototype : Prototype
    {
        public ulong NotificationIcon { get; protected set; }
        public ulong NotificationTooltip { get; protected set; }
        public ulong OfferingDescription { get; protected set; }
        public ulong OfferingTitle { get; protected set; }
    }

    public class TipEntryPrototype : Prototype
    {
        public ulong Entry { get; protected set; }
        public int Weight { get; protected set; }
        public bool SkipIfOnPC { get; protected set; }
        public bool SkipIfOnPS4 { get; protected set; }
        public bool SkipIfOnXBox { get; protected set; }
    }

    public class TipEntryCollectionPrototype : Prototype
    {
        public TipEntryPrototype[] TipEntries { get; protected set; }
    }

    public class GenericTipEntryCollectionPrototype : TipEntryCollectionPrototype
    {
    }

    public class RegionTipEntryCollectionPrototype : TipEntryCollectionPrototype
    {
        public ulong RegionBindings { get; protected set; }
    }

    public class AvatarTipEntryCollectionPrototype : TipEntryCollectionPrototype
    {
        public ulong AvatarBindings { get; protected set; }
    }

    public class WeightedTipCategoryPrototype : Prototype
    {
        public TipTypeEnum TipType { get; protected set; }
        public int Weight { get; protected set; }
    }

    public class TransitionUIPrototype : Prototype
    {
        public WeightedTipCategoryPrototype[] TipCategories { get; protected set; }
        public TransitionUIType TransitionType { get; protected set; }
        public int Weight { get; protected set; }
    }

    public class AvatarSynergyUIDataPrototype : Prototype
    {
        public ulong DisplayName { get; protected set; }
        public ulong IconPath { get; protected set; }
        public ulong SynergyActiveValue { get; protected set; }
        public ulong SynergyInactiveValue { get; protected set; }
        public ulong TooltipTextForList { get; protected set; }
        public ulong IconPathHiRes { get; protected set; }
    }

    public class MetaGameDataPrototype : Prototype
    {
        public ulong Descriptor { get; protected set; }
        public bool DisplayMissionName { get; protected set; }
        public int SortPriority { get; protected set; }
        public ulong IconHeader { get; protected set; }
        public int Justification { get; protected set; }
        public ulong WidgetMovieClipOverride { get; protected set; }
        public ulong IconHeaderHiRes { get; protected set; }
    }

    public class UIWidgetGenericFractionPrototype : MetaGameDataPrototype
    {
        public ulong IconComplete { get; protected set; }
        public ulong IconIncomplete { get; protected set; }
        public int IconSpacing { get; protected set; }
        public ulong IconCompleteHiRes { get; protected set; }
        public ulong IconIncompleteHiRes { get; protected set; }
    }

    public class UIWidgetEntityIconsEntryPrototype : Prototype
    {
        public EntityFilterPrototype Filter { get; protected set; }
        public int Count { get; protected set; }
        public UIWidgetEntityState TreatUnknownAs { get; protected set; }
        public ulong Icon { get; protected set; }
        public ulong Descriptor { get; protected set; }
        public ulong IconDead { get; protected set; }
        public int IconSpacing { get; protected set; }
        public ulong IconHiRes { get; protected set; }
        public ulong IconDeadHiRes { get; protected set; }
    }

    public class UIWidgetEnrageEntryPrototype : UIWidgetEntityIconsEntryPrototype
    {
    }

    public class WidgetPropertyEntryPrototype : Prototype
    {
        public ulong Color { get; protected set; }
        public ulong Descriptor { get; protected set; }
        public ulong Icon { get; protected set; }
        public EvalPrototype PropertyEval { get; protected set; }
        public ulong IconHiRes { get; protected set; }
    }

    public class UIWidgetEntityPropertyEntryPrototype : UIWidgetEntityIconsEntryPrototype
    {
        public WidgetPropertyEntryPrototype[] PropertyEntryTable { get; protected set; }
        public EvalPrototype PropertyEval { get; protected set; }
    }

    public class HealthPercentIconPrototype : Prototype
    {
        public ulong Color { get; protected set; }
        public int HealthPercent { get; protected set; }
        public ulong Icon { get; protected set; }
        public ulong Descriptor { get; protected set; }
        public ulong IconHiRes { get; protected set; }
    }

    public class UIWidgetHealthPercentEntryPrototype : UIWidgetEntityIconsEntryPrototype
    {
        public bool ColorBasedOnHealth { get; protected set; }
        public HealthPercentIconPrototype[] HealthDisplayTable { get; protected set; }
    }

    public class UIWidgetEntityIconsPrototype : MetaGameDataPrototype
    {
        public UIWidgetEntityIconsEntryPrototype[] Entities { get; protected set; }
    }

    public class UIWidgetMissionTextPrototype : MetaGameDataPrototype
    {
    }

    public class UIWidgetButtonPrototype : MetaGameDataPrototype
    {
    }

    public class UIWidgetReadyCheckPrototype : MetaGameDataPrototype
    {
    }

    public class UIWidgetPanelPrototype : Prototype
    {
        public ulong[] Widgets { get; protected set; }
    }

    public class UIWidgetTopPanelPrototype : UIWidgetPanelPrototype
    {
    }

    public class LogoffPanelEntryPrototype : Prototype
    {
        public ulong Description { get; protected set; }
        public ulong GameModeType { get; protected set; }
        public new ulong Header { get; protected set; }
        public ulong Image { get; protected set; }
        public int Priority { get; protected set; }
        public ulong Title { get; protected set; }
    }

    public class StoreCategoryPrototype : Prototype
    {
        public ulong Icon { get; protected set; }
        public ulong Identifier { get; protected set; }
        public ulong Label { get; protected set; }
    }

    public class ReputationLevelDisplayInfoPrototype : Prototype
    {
        public ulong DisplayName { get; protected set; }
        public ulong IconPath { get; protected set; }
        public int ReputationLevel { get; protected set; }
    }

    public class ReputationDisplayInfoPrototype : Prototype
    {
        public ulong DisplayName { get; protected set; }
        public ReputationLevelDisplayInfoPrototype ReputationLevels { get; protected set; }
    }

    public class UISystemLockPrototype : Prototype
    {
        public ulong GameNotification { get; protected set; }
        public ulong UISystem { get; protected set; }
        public int UnlockLevel { get; protected set; }
        public bool IsNewPlayerExperienceLocked { get; protected set; }
    }

    public class IconPackagePrototype : Prototype
    {
        public ulong Package { get; protected set; }
        public bool AlwaysLoaded { get; protected set; }
        public bool EnableStreaming { get; protected set; }
        public bool HighPriorityStreaming { get; protected set; }
        public bool RemoveUnreferencedContent { get; protected set; }
    }

    public class RadialMenuEntryPrototype : Prototype
    {
        public ulong ImageNormal { get; protected set; }
        public ulong ImageSelected { get; protected set; }
        public ulong LocalizedName { get; protected set; }
        public ulong Panel { get; protected set; }
    }

    public class InputBindingPrototype : Prototype
    {
        public ulong DisplayText { get; protected set; }
        public ulong BindingName { get; protected set; }
        public ulong TutorialImage { get; protected set; }
        public ulong TutorialImageOverlayText { get; protected set; }
        public ulong ControlScheme { get; protected set; }
    }

    public class PanelLoaderTabPrototype : Prototype
    {
        public ulong Context { get; protected set; }
        public ulong DisplayName { get; protected set; }
        public ulong Panel { get; protected set; }
        public bool ShowAvatarInfo { get; protected set; }
        public ulong SubTabs { get; protected set; }
        public ulong Icon { get; protected set; }
        public bool ShowLocalPlayerName { get; protected set; }
    }

    public class PanelLoaderTabListPrototype : Prototype
    {
        public ulong[] Tabs { get; protected set; }
        public bool IsSubTabList { get; protected set; }
    }

    public class ConsoleRadialMenuEntryPrototype : Prototype
    {
        public ulong DisplayName { get; protected set; }
        public ulong ImageNormal { get; protected set; }
        public ulong ImageSelected { get; protected set; }
        public ulong TabList { get; protected set; }
    }

    public class DialogPrototype : Prototype
    {
        public ulong Text { get; protected set; }
        public ulong Button1 { get; protected set; }
        public ulong Button2 { get; protected set; }
        public ButtonStyle Button1Style { get; protected set; }
        public ButtonStyle Button2Style { get; protected set; }
    }

    public class MissionTrackerFilterPrototype : Prototype
    {
        public MissionTrackerFilterTypeEnum FilterType { get; protected set; }
        public ulong Label { get; protected set; }
        public bool DisplayByDefault { get; protected set; }
        public int DisplayOrder { get; protected set; }
    }

    public class LocalizedTextAndImagePrototype : Prototype
    {
        public ulong Image { get; protected set; }
        public ulong Text { get; protected set; }
    }

    public class TextStylePrototype : Prototype
    {
        public bool Bold { get; protected set; }
        public ulong Color { get; protected set; }
        public ulong Tag { get; protected set; }
        public bool Underline { get; protected set; }
        public int FontSize { get; protected set; }
        public ulong Alignment { get; protected set; }
        public bool Hidden { get; protected set; }
        public int FontSizeConsole { get; protected set; }
    }

    public class UINotificationPrototype : Prototype
    {
    }

    public class BannerMessagePrototype : UINotificationPrototype
    {
        public ulong BannerText { get; protected set; }
        public int TimeToLiveMS { get; protected set; }
        public BannerMessageStyle MessageStyle { get; protected set; }
        public bool DoNotQueue { get; protected set; }
        public ulong TextStyle { get; protected set; }
        public bool ShowImmediately { get; protected set; }
    }

    public class GameNotificationPrototype : UINotificationPrototype
    {
        public ulong BannerText { get; protected set; }
        public GameNotificationType GameNotificationType { get; protected set; }
        public ulong IconPath { get; protected set; }
        public ulong TooltipText { get; protected set; }
        public bool PlayAudio { get; protected set; }
        public BannerMessageType BannerType { get; protected set; }
        public bool FlashContinuously { get; protected set; }
        public bool StackNotifications { get; protected set; }
        public bool ShowTimer { get; protected set; }
        public bool ShowScore { get; protected set; }
        public ulong TooltipStyle { get; protected set; }
        public ulong TooltipFont { get; protected set; }
        public ulong DisplayText { get; protected set; }
        public int MinimizeTimeDelayMS { get; protected set; }
        public int DurationMS { get; protected set; }
        public bool ShowAnimatedCircle { get; protected set; }
        public bool Unique { get; protected set; }
        public ulong[] OnCreateRemoveNotifications { get; protected set; }
        public bool RemoveOnRegionChange { get; protected set; }
        public bool ShowOnSystemLock { get; protected set; }
    }

    public class StoryNotificationPrototype : UINotificationPrototype
    {
        public ulong DisplayText { get; protected set; }
        public int TimeToLiveMS { get; protected set; }
        public ulong SpeakingEntity { get; protected set; }
        public ulong VOTrigger { get; protected set; }
    }

    public class VOStoryNotificationPrototype : Prototype
    {
        public VOEventType VOEventType { get; protected set; }
        public StoryNotificationPrototype StoryNotification { get; protected set; }
    }

    public class ConsoleHUDNotificationPrototype : Prototype
    {
        public ulong DisplayName { get; protected set; }
        public int DurationMS { get; protected set; }
        public ConsoleHUDNotificationType NotificationType { get; protected set; }
        public ulong OpensPanel { get; protected set; }
        public ulong PanelContext { get; protected set; }
    }

    public class HUDTutorialPrototype : UINotificationPrototype
    {
        public ulong Description { get; protected set; }
        public int DisplayDurationMS { get; protected set; }
        public ulong Image { get; protected set; }
        public ulong ImageOverlayText { get; protected set; }
        public ulong Title { get; protected set; }
        public ulong[] HighlightAvatars { get; protected set; }
        public ulong[] HighlightPowers { get; protected set; }
        public bool AllowMovement { get; protected set; }
        public bool AllowPowerUsage { get; protected set; }
        public bool AllowTakingDamage { get; protected set; }
        public bool CanDismiss { get; protected set; }
        public bool HighlightFirstEmptyPowerSlot { get; protected set; }
        public bool HighlightUpgradeablePowers { get; protected set; }
        public bool HighlightUnusedPowers { get; protected set; }
        public bool HighlightUnequippedItem { get; protected set; }
        public bool CloseOnRegionLeave { get; protected set; }
        public ulong ImageFromCommand { get; protected set; }
        public ulong DescriptionGamepad { get; protected set; }
        public ulong DescriptionNoBindings { get; protected set; }
        public ulong ImageFromCommandGamepad { get; protected set; }
        public ulong[] HighlightTeamUps { get; protected set; }
        public bool SkipIfOnConsole { get; protected set; }
        public bool SkipIfOnPC { get; protected set; }
        public bool SkipIfUsingGamepad { get; protected set; }
        public bool SkipIfUsingKeyboardMouse { get; protected set; }
        public float ScreenPositionConsoleX { get; protected set; }
        public float ScreenPositionConsoleY { get; protected set; }
        public float ScreenPositionX { get; protected set; }
        public float ScreenPositionY { get; protected set; }
        public int FlashDelayMS { get; protected set; }
        public bool ShowOncePerAccount { get; protected set; }
    }

    public class SessionImagePrototype : Prototype
    {
        public ulong SessionImageAsset { get; protected set; }
    }

    public class CurrencyDisplayPrototype : Prototype
    {
        public ulong DisplayName { get; protected set; }
        public ulong DisplayColor { get; protected set; }
        public ulong IconPath { get; protected set; }
        public ulong PropertyValueToDisplay { get; protected set; }
        public ulong TooltipText { get; protected set; }
        public bool UseGsBalance { get; protected set; }
        public ulong CurrencyToDisplay { get; protected set; }
        public ulong IconPathHiRes { get; protected set; }
        public sbyte CategoryIndex { get; protected set; }
        public ulong CategoryName { get; protected set; }
        public bool HideIfOnConsole { get; protected set; }
        public bool HideIfOnPC { get; protected set; }
    }

    public class FullscreenMoviePrototype : Prototype
    {
        public ulong MovieName { get; protected set; }
        public bool Skippable { get; protected set; }
        public MovieType MovieType { get; protected set; }
        public bool ExitGameAfterPlay { get; protected set; }
        public ulong MovieTitle { get; protected set; }
        public ulong Banter { get; protected set; }
        public ulong YouTubeVideoID { get; protected set; }
        public bool YouTubeControlsEnabled { get; protected set; }
        public ulong StreamingMovieNameHQ { get; protected set; }
        public ulong StreamingMovieNameLQ { get; protected set; }
        public ulong StreamingMovieNameMQ { get; protected set; }
    }

    public class LoadingScreenPrototype : Prototype
    {
        public ulong LoadingScreenAsset { get; protected set; }
        public ulong Title { get; protected set; }
    }

    public class UICinematicsListPrototype : Prototype
    {
        public ulong CinematicsListToPopulate { get; protected set; }
    }
}
