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
        public ResourcePrototypeHash ProtoNameHash { get; protected set; }
        public string PanelName { get; protected set; }
        public string TargetName { get; protected set; }
        public PanelScaleMode ScaleMode { get; protected set; }
        public UIPanelPrototype Children { get; protected set; }
        public string WidgetClass { get; protected set; }
        public string SwfName { get; protected set; }
        public byte OpenOnStart { get; protected set; }
        public byte VisibilityToggleable { get; protected set; }
        public byte CanClickThrough { get; protected set; }
        public byte StaticPosition { get; protected set; }
        public byte EntityInteractPanel { get; protected set; }
        public byte UseNewPlacementSystem { get; protected set; }
        public byte KeepLoaded { get; protected set; }

        public static UIPanelPrototype ReadFromBinaryReader(BinaryReader reader)
        {
            ResourcePrototypeHash hash = (ResourcePrototypeHash)reader.ReadUInt32();

            switch (hash)
            {
                case ResourcePrototypeHash.StretchedPanelPrototype:
                    return new StretchedPanelPrototype(reader);
                case ResourcePrototypeHash.AnchoredPanelPrototype:
                    return new AnchoredPanelPrototype(reader);
                case ResourcePrototypeHash.None:
                    return null;
                default:
                    throw new($"Unknown ResourcePrototypeHash {(uint)hash}");   // Throw an exception if there's a hash for a type we didn't expect
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
            ProtoNameHash = ResourcePrototypeHash.StretchedPanelPrototype;

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
            ProtoNameHash = ResourcePrototypeHash.AnchoredPanelPrototype;

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
        public ulong DisplayText { get; set; }
        public ulong TooltipText { get; set; }
        public ulong TooltipStyle { get; set; }
        public ulong TooltipFont { get; set; }
    }

    public class UILocalizedStatInfoPrototype : UILocalizedInfoPrototype
    {
        public ulong Stat { get; set; }
        public int StatValue { get; set; }
        public ulong LevelUnlockTooltipStyle { get; set; }
        public TooltipSectionPrototype[] TooltipSectionList { get; set; }
    }

    public class UICraftingTabLabelPrototype : UILocalizedInfoPrototype
    {
        public int SortOrder { get; set; }
    }

    public class InventoryUIDataPrototype : Prototype
    {
        public ulong EmptySlotTooltip { get; set; }
        public ulong SlotBackgroundIcon { get; set; }
        public ulong InventoryItemDisplayName { get; set; }
        public bool HintSlots { get; set; }
        public ulong SlotBackgroundIconHiRes { get; set; }
    }

    public class OfferingInventoryUIDataPrototype : Prototype
    {
        public ulong NotificationIcon { get; set; }
        public ulong NotificationTooltip { get; set; }
        public ulong OfferingDescription { get; set; }
        public ulong OfferingTitle { get; set; }
    }

    public class TipEntryPrototype : Prototype
    {
        public ulong Entry { get; set; }
        public int Weight { get; set; }
        public bool SkipIfOnPC { get; set; }
        public bool SkipIfOnPS4 { get; set; }
        public bool SkipIfOnXBox { get; set; }
    }

    public class TipEntryCollectionPrototype : Prototype
    {
        public TipEntryPrototype[] TipEntries { get; set; }
    }

    public class GenericTipEntryCollectionPrototype : TipEntryCollectionPrototype
    {
    }

    public class RegionTipEntryCollectionPrototype : TipEntryCollectionPrototype
    {
        public ulong RegionBindings { get; set; }
    }

    public class AvatarTipEntryCollectionPrototype : TipEntryCollectionPrototype
    {
        public ulong AvatarBindings { get; set; }
    }

    public class WeightedTipCategoryPrototype : Prototype
    {
        public TipTypeEnum TipType { get; set; }
        public int Weight { get; set; }
    }

    public class TransitionUIPrototype : Prototype
    {
        public WeightedTipCategoryPrototype[] TipCategories { get; set; }
        public TransitionUIType TransitionType { get; set; }
        public int Weight { get; set; }
    }

    public class AvatarSynergyUIDataPrototype : Prototype
    {
        public ulong DisplayName { get; set; }
        public ulong IconPath { get; set; }
        public ulong SynergyActiveValue { get; set; }
        public ulong SynergyInactiveValue { get; set; }
        public ulong TooltipTextForList { get; set; }
        public ulong IconPathHiRes { get; set; }
    }

    public class MetaGameDataPrototype : Prototype
    {
        public ulong Descriptor { get; set; }
        public bool DisplayMissionName { get; set; }
        public int SortPriority { get; set; }
        public ulong IconHeader { get; set; }
        public int Justification { get; set; }
        public ulong WidgetMovieClipOverride { get; set; }
        public ulong IconHeaderHiRes { get; set; }
    }

    public class UIWidgetGenericFractionPrototype : MetaGameDataPrototype
    {
        public ulong IconComplete { get; set; }
        public ulong IconIncomplete { get; set; }
        public int IconSpacing { get; set; }
        public ulong IconCompleteHiRes { get; set; }
        public ulong IconIncompleteHiRes { get; set; }
    }

    public class UIWidgetEntityIconsEntryPrototype : Prototype
    {
        public EntityFilterPrototype Filter { get; set; }
        public int Count { get; set; }
        public UIWidgetEntityState TreatUnknownAs { get; set; }
        public ulong Icon { get; set; }
        public ulong Descriptor { get; set; }
        public ulong IconDead { get; set; }
        public int IconSpacing { get; set; }
        public ulong IconHiRes { get; set; }
        public ulong IconDeadHiRes { get; set; }
    }

    public class UIWidgetEnrageEntryPrototype : UIWidgetEntityIconsEntryPrototype
    {
    }

    public class WidgetPropertyEntryPrototype : Prototype
    {
        public ulong Color { get; set; }
        public ulong Descriptor { get; set; }
        public ulong Icon { get; set; }
        public EvalPrototype PropertyEval { get; set; }
        public ulong IconHiRes { get; set; }
    }

    public class UIWidgetEntityPropertyEntryPrototype : UIWidgetEntityIconsEntryPrototype
    {
        public WidgetPropertyEntryPrototype[] PropertyEntryTable { get; set; }
        public EvalPrototype PropertyEval { get; set; }
    }

    public class HealthPercentIconPrototype : Prototype
    {
        public ulong Color { get; set; }
        public int HealthPercent { get; set; }
        public ulong Icon { get; set; }
        public ulong Descriptor { get; set; }
        public ulong IconHiRes { get; set; }
    }

    public class UIWidgetHealthPercentEntryPrototype : UIWidgetEntityIconsEntryPrototype
    {
        public bool ColorBasedOnHealth { get; set; }
        public HealthPercentIconPrototype[] HealthDisplayTable { get; set; }
    }

    public class UIWidgetEntityIconsPrototype : MetaGameDataPrototype
    {
        public UIWidgetEntityIconsEntryPrototype[] Entities { get; set; }
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
        public ulong[] Widgets { get; set; }
    }

    public class UIWidgetTopPanelPrototype : UIWidgetPanelPrototype
    {
    }

    public class LogoffPanelEntryPrototype : Prototype
    {
        public ulong Description { get; set; }
        public ulong GameModeType { get; set; }
        public new ulong Header { get; set; }
        public ulong Image { get; set; }
        public int Priority { get; set; }
        public ulong Title { get; set; }
    }

    public class StoreCategoryPrototype : Prototype
    {
        public ulong Icon { get; set; }
        public ulong Identifier { get; set; }
        public ulong Label { get; set; }
    }

    public class ReputationLevelDisplayInfoPrototype : Prototype
    {
        public ulong DisplayName { get; set; }
        public ulong IconPath { get; set; }
        public int ReputationLevel { get; set; }
    }

    public class ReputationDisplayInfoPrototype : Prototype
    {
        public ulong DisplayName { get; set; }
        public ReputationLevelDisplayInfoPrototype ReputationLevels { get; set; }
    }

    public class UISystemLockPrototype : Prototype
    {
        public ulong GameNotification { get; set; }
        public ulong UISystem { get; set; }
        public int UnlockLevel { get; set; }
        public bool IsNewPlayerExperienceLocked { get; set; }
    }

    public class IconPackagePrototype : Prototype
    {
        public ulong Package { get; set; }
        public bool AlwaysLoaded { get; set; }
        public bool EnableStreaming { get; set; }
        public bool HighPriorityStreaming { get; set; }
        public bool RemoveUnreferencedContent { get; set; }
    }

    public class RadialMenuEntryPrototype : Prototype
    {
        public ulong ImageNormal { get; set; }
        public ulong ImageSelected { get; set; }
        public ulong LocalizedName { get; set; }
        public ulong Panel { get; set; }
    }

    public class InputBindingPrototype : Prototype
    {
        public ulong DisplayText { get; set; }
        public ulong BindingName { get; set; }
        public ulong TutorialImage { get; set; }
        public ulong TutorialImageOverlayText { get; set; }
        public ulong ControlScheme { get; set; }
    }

    public class PanelLoaderTabPrototype : Prototype
    {
        public ulong Context { get; set; }
        public ulong DisplayName { get; set; }
        public ulong Panel { get; set; }
        public bool ShowAvatarInfo { get; set; }
        public ulong SubTabs { get; set; }
        public ulong Icon { get; set; }
        public bool ShowLocalPlayerName { get; set; }
    }

    public class PanelLoaderTabListPrototype : Prototype
    {
        public ulong[] Tabs { get; set; }
        public bool IsSubTabList { get; set; }
    }

    public class ConsoleRadialMenuEntryPrototype : Prototype
    {
        public ulong DisplayName { get; set; }
        public ulong ImageNormal { get; set; }
        public ulong ImageSelected { get; set; }
        public ulong TabList { get; set; }
    }

    public class DialogPrototype : Prototype
    {
        public ulong Text { get; set; }
        public ulong Button1 { get; set; }
        public ulong Button2 { get; set; }
        public ButtonStyle Button1Style { get; set; }
        public ButtonStyle Button2Style { get; set; }
    }

    public class MissionTrackerFilterPrototype : Prototype
    {
        public MissionTrackerFilterTypeEnum FilterType { get; set; }
        public ulong Label { get; set; }
        public bool DisplayByDefault { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class LocalizedTextAndImagePrototype : Prototype
    {
        public ulong Image { get; set; }
        public ulong Text { get; set; }
    }

    public class TextStylePrototype : Prototype
    {
        public bool Bold { get; set; }
        public ulong Color { get; set; }
        public ulong Tag { get; set; }
        public bool Underline { get; set; }
        public int FontSize { get; set; }
        public ulong Alignment { get; set; }
        public bool Hidden { get; set; }
        public int FontSizeConsole { get; set; }
    }

    public class UINotificationPrototype : Prototype
    {
    }

    public class BannerMessagePrototype : UINotificationPrototype
    {
        public ulong BannerText { get; set; }
        public int TimeToLiveMS { get; set; }
        public BannerMessageStyle MessageStyle { get; set; }
        public bool DoNotQueue { get; set; }
        public ulong TextStyle { get; set; }
        public bool ShowImmediately { get; set; }
    }

    public class GameNotificationPrototype : UINotificationPrototype
    {
        public ulong BannerText { get; set; }
        public GameNotificationType GameNotificationType { get; set; }
        public ulong IconPath { get; set; }
        public ulong TooltipText { get; set; }
        public bool PlayAudio { get; set; }
        public BannerMessageType BannerType { get; set; }
        public bool FlashContinuously { get; set; }
        public bool StackNotifications { get; set; }
        public bool ShowTimer { get; set; }
        public bool ShowScore { get; set; }
        public ulong TooltipStyle { get; set; }
        public ulong TooltipFont { get; set; }
        public ulong DisplayText { get; set; }
        public int MinimizeTimeDelayMS { get; set; }
        public int DurationMS { get; set; }
        public bool ShowAnimatedCircle { get; set; }
        public bool Unique { get; set; }
        public ulong[] OnCreateRemoveNotifications { get; set; }
        public bool RemoveOnRegionChange { get; set; }
        public bool ShowOnSystemLock { get; set; }
    }

    public class StoryNotificationPrototype : UINotificationPrototype
    {
        public ulong DisplayText { get; set; }
        public int TimeToLiveMS { get; set; }
        public ulong SpeakingEntity { get; set; }
        public ulong VOTrigger { get; set; }
    }

    public class VOStoryNotificationPrototype : Prototype
    {
        public VOEventType VOEventType { get; set; }
        public StoryNotificationPrototype StoryNotification { get; set; }
    }

    public class ConsoleHUDNotificationPrototype : Prototype
    {
        public ulong DisplayName { get; set; }
        public int DurationMS { get; set; }
        public ConsoleHUDNotificationType NotificationType { get; set; }
        public ulong OpensPanel { get; set; }
        public ulong PanelContext { get; set; }
    }

    public class HUDTutorialPrototype : UINotificationPrototype
    {
        public ulong Description { get; set; }
        public int DisplayDurationMS { get; set; }
        public ulong Image { get; set; }
        public ulong ImageOverlayText { get; set; }
        public ulong Title { get; set; }
        public ulong[] HighlightAvatars { get; set; }
        public ulong[] HighlightPowers { get; set; }
        public bool AllowMovement { get; set; }
        public bool AllowPowerUsage { get; set; }
        public bool AllowTakingDamage { get; set; }
        public bool CanDismiss { get; set; }
        public bool HighlightFirstEmptyPowerSlot { get; set; }
        public bool HighlightUpgradeablePowers { get; set; }
        public bool HighlightUnusedPowers { get; set; }
        public bool HighlightUnequippedItem { get; set; }
        public bool CloseOnRegionLeave { get; set; }
        public ulong ImageFromCommand { get; set; }
        public ulong DescriptionGamepad { get; set; }
        public ulong DescriptionNoBindings { get; set; }
        public ulong ImageFromCommandGamepad { get; set; }
        public ulong[] HighlightTeamUps { get; set; }
        public bool SkipIfOnConsole { get; set; }
        public bool SkipIfOnPC { get; set; }
        public bool SkipIfUsingGamepad { get; set; }
        public bool SkipIfUsingKeyboardMouse { get; set; }
        public float ScreenPositionConsoleX { get; set; }
        public float ScreenPositionConsoleY { get; set; }
        public float ScreenPositionX { get; set; }
        public float ScreenPositionY { get; set; }
        public int FlashDelayMS { get; set; }
        public bool ShowOncePerAccount { get; set; }
    }

    public class SessionImagePrototype : Prototype
    {
        public ulong SessionImageAsset { get; set; }
    }

    public class CurrencyDisplayPrototype : Prototype
    {
        public ulong DisplayName { get; set; }
        public ulong DisplayColor { get; set; }
        public ulong IconPath { get; set; }
        public ulong PropertyValueToDisplay { get; set; }
        public ulong TooltipText { get; set; }
        public bool UseGsBalance { get; set; }
        public ulong CurrencyToDisplay { get; set; }
        public ulong IconPathHiRes { get; set; }
        public sbyte CategoryIndex { get; set; }
        public ulong CategoryName { get; set; }
        public bool HideIfOnConsole { get; set; }
        public bool HideIfOnPC { get; set; }
    }

    public class FullscreenMoviePrototype : Prototype
    {
        public ulong MovieName { get; set; }
        public bool Skippable { get; set; }
        public MovieType MovieType { get; set; }
        public bool ExitGameAfterPlay { get; set; }
        public ulong MovieTitle { get; set; }
        public ulong Banter { get; set; }
        public ulong YouTubeVideoID { get; set; }
        public bool YouTubeControlsEnabled { get; set; }
        public ulong StreamingMovieNameHQ { get; set; }
        public ulong StreamingMovieNameLQ { get; set; }
        public ulong StreamingMovieNameMQ { get; set; }
    }

    public class LoadingScreenPrototype : Prototype
    {
        public ulong LoadingScreenAsset { get; set; }
        public ulong Title { get; set; }
    }

    public class UICinematicsListPrototype : Prototype
    {
        public ulong CinematicsListToPopulate { get; set; }
    }
}
