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

    public class UIPrototype : Prototype
    {
        public UIPanelPrototype[] UIPanels { get; }

        public UIPrototype(Stream stream)
        {
            using (BinaryReader reader = new(stream))
            {
                ResourceHeader header = new(reader);

                UIPanels = new UIPanelPrototype[reader.ReadUInt32()];
                for (int i = 0; i < UIPanels.Length; i++)
                    UIPanels[i] = UIPanelPrototype.ReadFromBinaryReader(reader);
            }
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
        public ulong DisplayText { get; private set; }
        public ulong TooltipText { get; private set; }
        public ulong TooltipStyle { get; private set; }
        public ulong TooltipFont { get; private set; }
    }

    public class UILocalizedStatInfoPrototype : UILocalizedInfoPrototype
    {
        public ulong Stat { get; private set; }
        public int StatValue { get; private set; }
        public ulong LevelUnlockTooltipStyle { get; private set; }
        public TooltipSectionPrototype[] TooltipSectionList { get; private set; }
    }

    public class UICraftingTabLabelPrototype : UILocalizedInfoPrototype
    {
        public int SortOrder { get; private set; }
    }

    public class InventoryUIDataPrototype : Prototype
    {
        public ulong EmptySlotTooltip { get; private set; }
        public ulong SlotBackgroundIcon { get; private set; }
        public ulong InventoryItemDisplayName { get; private set; }
        public bool HintSlots { get; private set; }
        public ulong SlotBackgroundIconHiRes { get; private set; }
    }

    public class OfferingInventoryUIDataPrototype : Prototype
    {
        public ulong NotificationIcon { get; private set; }
        public ulong NotificationTooltip { get; private set; }
        public ulong OfferingDescription { get; private set; }
        public ulong OfferingTitle { get; private set; }
    }

    public class TipEntryPrototype : Prototype
    {
        public ulong Entry { get; private set; }
        public int Weight { get; private set; }
        public bool SkipIfOnPC { get; private set; }
        public bool SkipIfOnPS4 { get; private set; }
        public bool SkipIfOnXBox { get; private set; }
    }

    public class TipEntryCollectionPrototype : Prototype
    {
        public TipEntryPrototype[] TipEntries { get; private set; }
    }

    public class GenericTipEntryCollectionPrototype : TipEntryCollectionPrototype
    {
    }

    public class RegionTipEntryCollectionPrototype : TipEntryCollectionPrototype
    {
        public ulong RegionBindings { get; private set; }
    }

    public class AvatarTipEntryCollectionPrototype : TipEntryCollectionPrototype
    {
        public ulong AvatarBindings { get; private set; }
    }

    public class WeightedTipCategoryPrototype : Prototype
    {
        public TipTypeEnum TipType { get; private set; }
        public int Weight { get; private set; }
    }

    public class TransitionUIPrototype : Prototype
    {
        public WeightedTipCategoryPrototype[] TipCategories { get; private set; }
        public TransitionUIType TransitionType { get; private set; }
        public int Weight { get; private set; }
    }

    public class AvatarSynergyUIDataPrototype : Prototype
    {
        public ulong DisplayName { get; private set; }
        public ulong IconPath { get; private set; }
        public ulong SynergyActiveValue { get; private set; }
        public ulong SynergyInactiveValue { get; private set; }
        public ulong TooltipTextForList { get; private set; }
        public ulong IconPathHiRes { get; private set; }
    }

    public class MetaGameDataPrototype : Prototype
    {
        public ulong Descriptor { get; private set; }
        public bool DisplayMissionName { get; private set; }
        public int SortPriority { get; private set; }
        public ulong IconHeader { get; private set; }
        public int Justification { get; private set; }
        public ulong WidgetMovieClipOverride { get; private set; }
        public ulong IconHeaderHiRes { get; private set; }
    }

    public class UIWidgetGenericFractionPrototype : MetaGameDataPrototype
    {
        public ulong IconComplete { get; private set; }
        public ulong IconIncomplete { get; private set; }
        public int IconSpacing { get; private set; }
        public ulong IconCompleteHiRes { get; private set; }
        public ulong IconIncompleteHiRes { get; private set; }
    }

    public class UIWidgetEntityIconsEntryPrototype : Prototype
    {
        public EntityFilterPrototype Filter { get; private set; }
        public int Count { get; private set; }
        public UIWidgetEntityState TreatUnknownAs { get; private set; }
        public ulong Icon { get; private set; }
        public ulong Descriptor { get; private set; }
        public ulong IconDead { get; private set; }
        public int IconSpacing { get; private set; }
        public ulong IconHiRes { get; private set; }
        public ulong IconDeadHiRes { get; private set; }
    }

    public class UIWidgetEnrageEntryPrototype : UIWidgetEntityIconsEntryPrototype
    {
    }

    public class WidgetPropertyEntryPrototype : Prototype
    {
        public ulong Color { get; private set; }
        public ulong Descriptor { get; private set; }
        public ulong Icon { get; private set; }
        public EvalPrototype PropertyEval { get; private set; }
        public ulong IconHiRes { get; private set; }
    }

    public class UIWidgetEntityPropertyEntryPrototype : UIWidgetEntityIconsEntryPrototype
    {
        public WidgetPropertyEntryPrototype[] PropertyEntryTable { get; private set; }
        public EvalPrototype PropertyEval { get; private set; }
    }

    public class HealthPercentIconPrototype : Prototype
    {
        public ulong Color { get; private set; }
        public int HealthPercent { get; private set; }
        public ulong Icon { get; private set; }
        public ulong Descriptor { get; private set; }
        public ulong IconHiRes { get; private set; }
    }

    public class UIWidgetHealthPercentEntryPrototype : UIWidgetEntityIconsEntryPrototype
    {
        public bool ColorBasedOnHealth { get; private set; }
        public HealthPercentIconPrototype[] HealthDisplayTable { get; private set; }
    }

    public class UIWidgetEntityIconsPrototype : MetaGameDataPrototype
    {
        public UIWidgetEntityIconsEntryPrototype[] Entities { get; private set; }
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
        public ulong[] Widgets { get; private set; }
    }

    public class UIWidgetTopPanelPrototype : UIWidgetPanelPrototype
    {
    }

    public class LogoffPanelEntryPrototype : Prototype
    {
        public ulong Description { get; private set; }
        public ulong GameModeType { get; private set; }
        public new ulong Header { get; private set; }
        public ulong Image { get; private set; }
        public int Priority { get; private set; }
        public ulong Title { get; private set; }
    }

    public class StoreCategoryPrototype : Prototype
    {
        public ulong Icon { get; private set; }
        public ulong Identifier { get; private set; }
        public ulong Label { get; private set; }
    }

    public class ReputationLevelDisplayInfoPrototype : Prototype
    {
        public ulong DisplayName { get; private set; }
        public ulong IconPath { get; private set; }
        public int ReputationLevel { get; private set; }
    }

    public class ReputationDisplayInfoPrototype : Prototype
    {
        public ulong DisplayName { get; private set; }
        public ReputationLevelDisplayInfoPrototype ReputationLevels { get; private set; }
    }

    public class UISystemLockPrototype : Prototype
    {
        public ulong GameNotification { get; private set; }
        public ulong UISystem { get; private set; }
        public int UnlockLevel { get; private set; }
        public bool IsNewPlayerExperienceLocked { get; private set; }
    }

    public class IconPackagePrototype : Prototype
    {
        public ulong Package { get; private set; }
        public bool AlwaysLoaded { get; private set; }
        public bool EnableStreaming { get; private set; }
        public bool HighPriorityStreaming { get; private set; }
        public bool RemoveUnreferencedContent { get; private set; }
    }

    public class RadialMenuEntryPrototype : Prototype
    {
        public ulong ImageNormal { get; private set; }
        public ulong ImageSelected { get; private set; }
        public ulong LocalizedName { get; private set; }
        public ulong Panel { get; private set; }
    }

    public class InputBindingPrototype : Prototype
    {
        public ulong DisplayText { get; private set; }
        public ulong BindingName { get; private set; }
        public ulong TutorialImage { get; private set; }
        public ulong TutorialImageOverlayText { get; private set; }
        public ulong ControlScheme { get; private set; }
    }

    public class PanelLoaderTabPrototype : Prototype
    {
        public ulong Context { get; private set; }
        public ulong DisplayName { get; private set; }
        public ulong Panel { get; private set; }
        public bool ShowAvatarInfo { get; private set; }
        public ulong SubTabs { get; private set; }
        public ulong Icon { get; private set; }
        public bool ShowLocalPlayerName { get; private set; }
    }

    public class PanelLoaderTabListPrototype : Prototype
    {
        public ulong[] Tabs { get; private set; }
        public bool IsSubTabList { get; private set; }
    }

    public class ConsoleRadialMenuEntryPrototype : Prototype
    {
        public ulong DisplayName { get; private set; }
        public ulong ImageNormal { get; private set; }
        public ulong ImageSelected { get; private set; }
        public ulong TabList { get; private set; }
    }

    public class DialogPrototype : Prototype
    {
        public ulong Text { get; private set; }
        public ulong Button1 { get; private set; }
        public ulong Button2 { get; private set; }
        public ButtonStyle Button1Style { get; private set; }
        public ButtonStyle Button2Style { get; private set; }
    }

    public class MissionTrackerFilterPrototype : Prototype
    {
        public MissionTrackerFilterTypeEnum FilterType { get; private set; }
        public ulong Label { get; private set; }
        public bool DisplayByDefault { get; private set; }
        public int DisplayOrder { get; private set; }
    }

    public class LocalizedTextAndImagePrototype : Prototype
    {
        public ulong Image { get; private set; }
        public ulong Text { get; private set; }
    }

    public class TextStylePrototype : Prototype
    {
        public bool Bold { get; private set; }
        public ulong Color { get; private set; }
        public ulong Tag { get; private set; }
        public bool Underline { get; private set; }
        public int FontSize { get; private set; }
        public ulong Alignment { get; private set; }
        public bool Hidden { get; private set; }
        public int FontSizeConsole { get; private set; }
    }

    public class UINotificationPrototype : Prototype
    {
    }

    public class BannerMessagePrototype : UINotificationPrototype
    {
        public ulong BannerText { get; private set; }
        public int TimeToLiveMS { get; private set; }
        public BannerMessageStyle MessageStyle { get; private set; }
        public bool DoNotQueue { get; private set; }
        public ulong TextStyle { get; private set; }
        public bool ShowImmediately { get; private set; }
    }

    public class GameNotificationPrototype : UINotificationPrototype
    {
        public ulong BannerText { get; private set; }
        public GameNotificationType GameNotificationType { get; private set; }
        public ulong IconPath { get; private set; }
        public ulong TooltipText { get; private set; }
        public bool PlayAudio { get; private set; }
        public BannerMessageType BannerType { get; private set; }
        public bool FlashContinuously { get; private set; }
        public bool StackNotifications { get; private set; }
        public bool ShowTimer { get; private set; }
        public bool ShowScore { get; private set; }
        public ulong TooltipStyle { get; private set; }
        public ulong TooltipFont { get; private set; }
        public ulong DisplayText { get; private set; }
        public int MinimizeTimeDelayMS { get; private set; }
        public int DurationMS { get; private set; }
        public bool ShowAnimatedCircle { get; private set; }
        public bool Unique { get; private set; }
        public ulong[] OnCreateRemoveNotifications { get; private set; }
        public bool RemoveOnRegionChange { get; private set; }
        public bool ShowOnSystemLock { get; private set; }
    }

    public class StoryNotificationPrototype : UINotificationPrototype
    {
        public ulong DisplayText { get; private set; }
        public int TimeToLiveMS { get; private set; }
        public ulong SpeakingEntity { get; private set; }
        public ulong VOTrigger { get; private set; }
    }

    public class VOStoryNotificationPrototype : Prototype
    {
        public VOEventType VOEventType { get; private set; }
        public StoryNotificationPrototype StoryNotification { get; private set; }
    }

    public class ConsoleHUDNotificationPrototype : Prototype
    {
        public ulong DisplayName { get; private set; }
        public int DurationMS { get; private set; }
        public ConsoleHUDNotificationType NotificationType { get; private set; }
        public ulong OpensPanel { get; private set; }
        public ulong PanelContext { get; private set; }
    }

    public class HUDTutorialPrototype : UINotificationPrototype
    {
        public ulong Description { get; private set; }
        public int DisplayDurationMS { get; private set; }
        public ulong Image { get; private set; }
        public ulong ImageOverlayText { get; private set; }
        public ulong Title { get; private set; }
        public ulong[] HighlightAvatars { get; private set; }
        public ulong[] HighlightPowers { get; private set; }
        public bool AllowMovement { get; private set; }
        public bool AllowPowerUsage { get; private set; }
        public bool AllowTakingDamage { get; private set; }
        public bool CanDismiss { get; private set; }
        public bool HighlightFirstEmptyPowerSlot { get; private set; }
        public bool HighlightUpgradeablePowers { get; private set; }
        public bool HighlightUnusedPowers { get; private set; }
        public bool HighlightUnequippedItem { get; private set; }
        public bool CloseOnRegionLeave { get; private set; }
        public ulong ImageFromCommand { get; private set; }
        public ulong DescriptionGamepad { get; private set; }
        public ulong DescriptionNoBindings { get; private set; }
        public ulong ImageFromCommandGamepad { get; private set; }
        public ulong[] HighlightTeamUps { get; private set; }
        public bool SkipIfOnConsole { get; private set; }
        public bool SkipIfOnPC { get; private set; }
        public bool SkipIfUsingGamepad { get; private set; }
        public bool SkipIfUsingKeyboardMouse { get; private set; }
        public float ScreenPositionConsoleX { get; private set; }
        public float ScreenPositionConsoleY { get; private set; }
        public float ScreenPositionX { get; private set; }
        public float ScreenPositionY { get; private set; }
        public int FlashDelayMS { get; private set; }
        public bool ShowOncePerAccount { get; private set; }
    }

    public class SessionImagePrototype : Prototype
    {
        public ulong SessionImageAsset { get; private set; }
    }

    public class CurrencyDisplayPrototype : Prototype
    {
        public ulong DisplayName { get; private set; }
        public ulong DisplayColor { get; private set; }
        public ulong IconPath { get; private set; }
        public ulong PropertyValueToDisplay { get; private set; }
        public ulong TooltipText { get; private set; }
        public bool UseGsBalance { get; private set; }
        public ulong CurrencyToDisplay { get; private set; }
        public ulong IconPathHiRes { get; private set; }
        public sbyte CategoryIndex { get; private set; }
        public ulong CategoryName { get; private set; }
        public bool HideIfOnConsole { get; private set; }
        public bool HideIfOnPC { get; private set; }
    }

    public class FullscreenMoviePrototype : Prototype
    {
        public ulong MovieName { get; private set; }
        public bool Skippable { get; private set; }
        public MovieType MovieType { get; private set; }
        public bool ExitGameAfterPlay { get; private set; }
        public ulong MovieTitle { get; private set; }
        public ulong Banter { get; private set; }
        public ulong YouTubeVideoID { get; private set; }
        public bool YouTubeControlsEnabled { get; private set; }
        public ulong StreamingMovieNameHQ { get; private set; }
        public ulong StreamingMovieNameLQ { get; private set; }
        public ulong StreamingMovieNameMQ { get; private set; }
    }

    public class LoadingScreenPrototype : Prototype
    {
        public ulong LoadingScreenAsset { get; private set; }
        public ulong Title { get; private set; }
    }

    public class UICinematicsListPrototype : Prototype
    {
        public ulong CinematicsListToPopulate { get; private set; }
    }
}
