using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.GameData.Resources;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Entities;

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

    [AssetEnum((int)Unknown)]
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

    // There are two MissionTrackerFilterType (asset and symbolic), the symbolic one is the one that fits our data files,
    // and it seems to be UI-related
    //[AssetEnum((int)None)]
    public enum MissionTrackerFilterTypeEnum
    {
        None = -1,
        Standard = 0,
        PvE = 1,
        PvP = 2,
        Daily = 3,
        Challenge = 4,
    }

    [AssetEnum((int)StoryMissions)]
    public enum UIMissionTrackerFilterTypeEnum
    {
        AccountMissions = 0,
        Achievements = 1,
        GlobalEventMissions = 2,
        InfluenceMissions = 3,
        LegendaryQuests = 4,
        LoreMissions = 5,
        RegionEventMissions = 6,
        SharedQuests = 7,
        StoryMissions = 8,
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
        PowerPointsAwarded = 3,
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
        StatProgression = 19,
        AvatarSwitchError = 20,
        OmegaPointsAwarded = 21,
        UISystemUnlocked = 22,
        LeaderboardRewarded = 23,
        InfinityPointsAwarded = 26,
        WaypointError = 27,
        // Not found in client
        MatchQueue = 0
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
        public LocaleStringId DisplayText { get; protected set; }
        public LocaleStringId TooltipText { get; protected set; }
        public PrototypeId TooltipStyle { get; protected set; }
        public AssetId TooltipFont { get; protected set; }
    }

    public class UILocalizedStatInfoPrototype : UILocalizedInfoPrototype
    {
        public PrototypeId Stat { get; protected set; }
        public int StatValue { get; protected set; }
        public PrototypeId LevelUnlockTooltipStyle { get; protected set; }
        public TooltipSectionPrototype[] TooltipSectionList { get; protected set; }
    }

    public class UICraftingTabLabelPrototype : UILocalizedInfoPrototype
    {
        public int SortOrder { get; protected set; }
    }

    public class InventoryUIDataPrototype : Prototype
    {
        public PrototypeId EmptySlotTooltip { get; protected set; }
        public AssetId SlotBackgroundIcon { get; protected set; }
        public LocaleStringId InventoryItemDisplayName { get; protected set; }
        public bool HintSlots { get; protected set; }
        public AssetId SlotBackgroundIconHiRes { get; protected set; }
    }

    public class OfferingInventoryUIDataPrototype : Prototype
    {
        public AssetId NotificationIcon { get; protected set; }
        public LocaleStringId NotificationTooltip { get; protected set; }
        public LocaleStringId OfferingDescription { get; protected set; }
        public LocaleStringId OfferingTitle { get; protected set; }
    }

    public class TipEntryPrototype : Prototype
    {
        public LocaleStringId Entry { get; protected set; }
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
        public PrototypeId[] RegionBindings { get; protected set; }
    }

    public class AvatarTipEntryCollectionPrototype : TipEntryCollectionPrototype
    {
        public PrototypeId[] AvatarBindings { get; protected set; }
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
        public LocaleStringId DisplayName { get; protected set; }
        public AssetId IconPath { get; protected set; }
        public PrototypeId SynergyActiveValue { get; protected set; }
        public PrototypeId SynergyInactiveValue { get; protected set; }
        public LocaleStringId TooltipTextForList { get; protected set; }
        public AssetId IconPathHiRes { get; protected set; }
    }

    public class MetaGameDataPrototype : Prototype
    {
        public LocaleStringId Descriptor { get; protected set; }
        public bool DisplayMissionName { get; protected set; }
        public int SortPriority { get; protected set; }
        public AssetId IconHeader { get; protected set; }
        public int Justification { get; protected set; }
        public AssetId WidgetMovieClipOverride { get; protected set; }
        public AssetId IconHeaderHiRes { get; protected set; }
    }

    public class UIWidgetGenericFractionPrototype : MetaGameDataPrototype
    {
        public AssetId IconComplete { get; protected set; }
        public AssetId IconIncomplete { get; protected set; }
        public int IconSpacing { get; protected set; }
        public AssetId IconCompleteHiRes { get; protected set; }
        public AssetId IconIncompleteHiRes { get; protected set; }
    }

    public class UIWidgetEntityIconsEntryPrototype : Prototype
    {
        public EntityFilterPrototype Filter { get; protected set; }
        public int Count { get; protected set; }
        public UIWidgetEntityState TreatUnknownAs { get; protected set; }
        public AssetId Icon { get; protected set; }
        public PrototypeId Descriptor { get; protected set; }
        public AssetId IconDead { get; protected set; }
        public int IconSpacing { get; protected set; }
        public AssetId IconHiRes { get; protected set; }
        public AssetId IconDeadHiRes { get; protected set; }
    }

    public class UIWidgetEnrageEntryPrototype : UIWidgetEntityIconsEntryPrototype
    {
    }

    public class WidgetPropertyEntryPrototype : Prototype
    {
        public AssetId Color { get; protected set; }
        public LocaleStringId Descriptor { get; protected set; }
        public AssetId Icon { get; protected set; }
        public EvalPrototype PropertyEval { get; protected set; }
        public AssetId IconHiRes { get; protected set; }
    }

    public class UIWidgetEntityPropertyEntryPrototype : UIWidgetEntityIconsEntryPrototype
    {
        public WidgetPropertyEntryPrototype[] PropertyEntryTable { get; protected set; }
        public EvalPrototype PropertyEval { get; protected set; }

        [DoNotCopy]
        public List<PropertyId> PropertyIds { get; private set; } = new();

        public override void PostProcess()
        {
            base.PostProcess();

            PropertyIds.Clear();
            if (PropertyEntryTable.HasValue())
                foreach (var entryProto in PropertyEntryTable)
                    if (entryProto != null && entryProto.PropertyEval != null)
                        Eval.GetEvalPropertyIds(entryProto.PropertyEval, PropertyIds, GetEvalPropertyIdEnum.Input, null);
        }
    }

    public class HealthPercentIconPrototype : Prototype
    {
        public AssetId Color { get; protected set; }
        public int HealthPercent { get; protected set; }
        public AssetId Icon { get; protected set; }
        public PrototypeId Descriptor { get; protected set; }
        public AssetId IconHiRes { get; protected set; }
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
        public PrototypeId[] Widgets { get; protected set; }
    }

    public class UIWidgetTopPanelPrototype : UIWidgetPanelPrototype
    {
    }

    public class LogoffPanelEntryPrototype : Prototype
    {
        public LocaleStringId Description { get; protected set; }
        public PrototypeId GameModeType { get; protected set; }
        public LocaleStringId Header { get; protected set; }
        public AssetId Image { get; protected set; }
        public int Priority { get; protected set; }
        public LocaleStringId Title { get; protected set; }
    }

    public class StoreCategoryPrototype : Prototype
    {
        public AssetId Icon { get; protected set; }
        public AssetId Identifier { get; protected set; }
        public PrototypeId Label { get; protected set; }
    }

    public class ReputationLevelDisplayInfoPrototype : Prototype
    {
        public LocaleStringId DisplayName { get; protected set; }
        public AssetId IconPath { get; protected set; }
        public int ReputationLevel { get; protected set; }
    }

    public class ReputationDisplayInfoPrototype : Prototype
    {
        public LocaleStringId DisplayName { get; protected set; }
        public PrototypeId[] ReputationLevels { get; protected set; }     // VectorPrototypeRefPtr ReputationLevelDisplayInfoPrototype
    }

    public class UISystemLockPrototype : Prototype
    {
        public PrototypeId GameNotification { get; protected set; }
        public AssetId UISystem { get; protected set; }
        public int UnlockLevel { get; protected set; }
        public bool IsNewPlayerExperienceLocked { get; protected set; }
    }

    public class IconPackagePrototype : Prototype
    {
        public AssetId Package { get; protected set; }
        public bool AlwaysLoaded { get; protected set; }
        public bool EnableStreaming { get; protected set; }
        public bool HighPriorityStreaming { get; protected set; }
        public bool RemoveUnreferencedContent { get; protected set; }
    }

    public class RadialMenuEntryPrototype : Prototype
    {
        public AssetId ImageNormal { get; protected set; }
        public AssetId ImageSelected { get; protected set; }
        public PrototypeId LocalizedName { get; protected set; }
        public AssetId Panel { get; protected set; }
    }

    public class InputBindingPrototype : Prototype
    {
        public PrototypeId DisplayText { get; protected set; }
        public LocaleStringId BindingName { get; protected set; }
        public AssetId TutorialImage { get; protected set; }
        public LocaleStringId TutorialImageOverlayText { get; protected set; }
        public AssetId ControlScheme { get; protected set; }
    }

    public class PanelLoaderTabPrototype : Prototype
    {
        public LocaleStringId Context { get; protected set; }
        public PrototypeId DisplayName { get; protected set; }
        public AssetId Panel { get; protected set; }
        public bool ShowAvatarInfo { get; protected set; }
        public PrototypeId SubTabs { get; protected set; }
        public AssetId Icon { get; protected set; }
        public bool ShowLocalPlayerName { get; protected set; }
    }

    public class PanelLoaderTabListPrototype : Prototype
    {
        public PrototypeId[] Tabs { get; protected set; }
        public bool IsSubTabList { get; protected set; }
    }

    public class ConsoleRadialMenuEntryPrototype : Prototype
    {
        public PrototypeId DisplayName { get; protected set; }
        public AssetId ImageNormal { get; protected set; }
        public AssetId ImageSelected { get; protected set; }
        public PrototypeId TabList { get; protected set; }
    }

    public class DialogPrototype : Prototype
    {
        public LocaleStringId Text { get; protected set; }
        public LocaleStringId Button1 { get; protected set; }
        public LocaleStringId Button2 { get; protected set; }
        public ButtonStyle Button1Style { get; protected set; }
        public ButtonStyle Button2Style { get; protected set; }
    }

    public class MissionTrackerFilterPrototype : Prototype
    {
        public UIMissionTrackerFilterTypeEnum FilterType { get; protected set; }
        public LocaleStringId Label { get; protected set; }
        public bool DisplayByDefault { get; protected set; }
        public int DisplayOrder { get; protected set; }
    }

    public class LocalizedTextAndImagePrototype : Prototype
    {
        public AssetId Image { get; protected set; }
        public LocaleStringId Text { get; protected set; }
    }

    public class TextStylePrototype : Prototype
    {
        public bool Bold { get; protected set; }
        public AssetId Color { get; protected set; }
        public LocaleStringId Tag { get; protected set; }
        public bool Underline { get; protected set; }
        public int FontSize { get; protected set; }
        public AssetId Alignment { get; protected set; }
        public bool Hidden { get; protected set; }
        public int FontSizeConsole { get; protected set; }
    }

    public class UINotificationPrototype : Prototype
    {
    }

    public class BannerMessagePrototype : UINotificationPrototype
    {
        public LocaleStringId BannerText { get; protected set; }
        public int TimeToLiveMS { get; protected set; }
        public BannerMessageStyle MessageStyle { get; protected set; }
        public bool DoNotQueue { get; protected set; }
        public PrototypeId TextStyle { get; protected set; }
        public bool ShowImmediately { get; protected set; }
    }

    public class GameNotificationPrototype : UINotificationPrototype
    {
        public LocaleStringId BannerText { get; protected set; }
        public GameNotificationType GameNotificationType { get; protected set; }
        public AssetId IconPath { get; protected set; }
        public LocaleStringId TooltipText { get; protected set; }
        public bool PlayAudio { get; protected set; }
        public BannerMessageType BannerType { get; protected set; }
        public bool FlashContinuously { get; protected set; }
        public bool StackNotifications { get; protected set; }
        public bool ShowTimer { get; protected set; }
        public bool ShowScore { get; protected set; }
        public PrototypeId TooltipStyle { get; protected set; }
        public AssetId TooltipFont { get; protected set; }
        public LocaleStringId DisplayText { get; protected set; }
        public int MinimizeTimeDelayMS { get; protected set; }
        public int DurationMS { get; protected set; }
        public bool ShowAnimatedCircle { get; protected set; }
        public bool Unique { get; protected set; }
        public PrototypeId[] OnCreateRemoveNotifications { get; protected set; }
        public bool RemoveOnRegionChange { get; protected set; }
        public bool ShowOnSystemLock { get; protected set; }
    }

    public class StoryNotificationPrototype : UINotificationPrototype
    {
        public LocaleStringId DisplayText { get; protected set; }
        public int TimeToLiveMS { get; protected set; }
        public PrototypeId SpeakingEntity { get; protected set; }
        public AssetId VOTrigger { get; protected set; }
    }

    public class VOStoryNotificationPrototype : Prototype
    {
        public VOEventType VOEventType { get; protected set; }
        public StoryNotificationPrototype StoryNotification { get; protected set; }
    }

    public class ConsoleHUDNotificationPrototype : Prototype
    {
        public PrototypeId DisplayName { get; protected set; }
        public int DurationMS { get; protected set; }
        public ConsoleHUDNotificationType NotificationType { get; protected set; }
        public AssetId OpensPanel { get; protected set; }
        public LocaleStringId PanelContext { get; protected set; }
    }

    public class HUDTutorialPrototype : UINotificationPrototype
    {
        public LocaleStringId Description { get; protected set; }
        public int DisplayDurationMS { get; protected set; }
        public AssetId Image { get; protected set; }
        public LocaleStringId ImageOverlayText { get; protected set; }
        public LocaleStringId Title { get; protected set; }
        public PrototypeId[] HighlightAvatars { get; protected set; }
        public PrototypeId[] HighlightPowers { get; protected set; }
        public bool AllowMovement { get; protected set; }
        public bool AllowPowerUsage { get; protected set; }
        public bool AllowTakingDamage { get; protected set; }
        public bool CanDismiss { get; protected set; }
        public bool HighlightFirstEmptyPowerSlot { get; protected set; }
        public bool HighlightUpgradeablePowers { get; protected set; }
        public bool HighlightUnusedPowers { get; protected set; }
        public bool HighlightUnequippedItem { get; protected set; }
        public bool CloseOnRegionLeave { get; protected set; }
        public LocaleStringId ImageFromCommand { get; protected set; }
        public LocaleStringId DescriptionGamepad { get; protected set; }
        public LocaleStringId DescriptionNoBindings { get; protected set; }
        public LocaleStringId ImageFromCommandGamepad { get; protected set; }
        public PrototypeId[] HighlightTeamUps { get; protected set; }
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

        public bool ShouldShowTip(Player player)
        {
            if (ShowOncePerAccount == false) return true;
            else return player.Properties[PropertyEnum.TutorialHasSeenTip, DataRef] == false;
        }
    }

    public class SessionImagePrototype : Prototype
    {
        public AssetId SessionImageAsset { get; protected set; }
    }

    public class CurrencyDisplayPrototype : Prototype
    {
        public LocaleStringId DisplayName { get; protected set; }
        public AssetId DisplayColor { get; protected set; }
        public AssetId IconPath { get; protected set; }
        public PrototypeId PropertyValueToDisplay { get; protected set; }
        public LocaleStringId TooltipText { get; protected set; }
        public bool UseGsBalance { get; protected set; }
        public PrototypeId CurrencyToDisplay { get; protected set; }
        public AssetId IconPathHiRes { get; protected set; }
        public sbyte CategoryIndex { get; protected set; }
        public LocaleStringId CategoryName { get; protected set; }
        public bool HideIfOnConsole { get; protected set; }
        public bool HideIfOnPC { get; protected set; }
    }

    public class UICinematicsListPrototype : Prototype
    {
        public PrototypeId[] CinematicsListToPopulate { get; protected set; }
    }
}
