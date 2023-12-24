using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    public enum HUDEntityFloorEffect    // What is this? Appears only in UIMapInfoPrototype, and that doesn't have any fields defined
    {
        None = 0,
        Generic = 1,
        Target = 2,
        Rescue = 3,
    }

    [AssetEnum]
    public enum HUDEntityOverheadIcon   // UI/Types/InteractIndicatorType.type
    {
        None = 0,
        DiscoveryBestower = 1,
        DiscoveryAdvancer = 2,
        MissionBestowerDisabled = 3,
        Vendor = 4,
        VendorArmor = 5,
        VendorCrafter = 6,
        VendorWeapon = 7,
        Stash = 8,
        Transporter = 9,
        MissionAdvancerDisabled = 10,
        MissionBestower = 11,
        MissionAdvancer = 12,
        FlavorText = 13,
        Healer = 14,
        StoryWarp = 15,
    }

    [AssetEnum]
    public enum ObjectiveVisibility
    {
        VisibleOnlyByMission,
        VisibleWhenFound,
        VisibleAlways,
        VisibleToParty,
    }

    [AssetEnum]
    public enum ScriptRoleKey
    {
        Invalid,
        FriendlyPassive01,
        FriendlyPassive02,
        FriendlyPassive03,
        FriendlyPassive04,
        FriendlyCombatant01,
        FriendlyCombatant02,
        FriendlyCombatant03,
        FriendlyCombatant04,
        HostileCombatant01,
        HostileCombatant02,
        HostileCombatant03,
        HostileCombatant04,
    }

    #endregion

    public class HUDEntitySettingsPrototype : Prototype
    {
        public HUDEntityFloorEffect FloorEffect { get; private set; }
        public HUDEntityOverheadIcon OverheadIcon { get; private set; }
        public ulong MapIcon { get; private set; }
        public ulong EdgeIcon { get; private set; }
    }

    public class UIMapInfoIconBehaviorPrototype : Prototype
    {
        public ulong IconPath { get; private set; }
        public ulong IconPathHiRes { get; private set; }
    }

    public class UIMapInfoIconAppearancePrototype : Prototype
    {
        public ulong IconOnScreen { get; private set; }
        public ulong IconOffScreen { get; private set; }
    }

    public class ObjectiveInfoPrototype : Prototype
    {
        public ulong EdgeColor { get; private set; }
        public bool EdgeEnabled { get; private set; }
        public bool EdgeOnlyInArea { get; private set; }
        public int EdgeRange { get; private set; }
        public bool FloorRingAnimation { get; private set; }
        public bool MapEnabled { get; private set; }
        public int MapRange { get; private set; }
        public bool ShowToSummonerOnly { get; private set; }
        public bool TrackAfterDiscovery { get; private set; }
        public ObjectiveVisibility Visibility { get; private set; }
    }

    public class EntityFilterSettingsPrototype : Prototype
    {
        public EntityFilterPrototype EntityFilter { get; private set; }
        public ScriptRoleKeyEnum ScriptRoleKey { get; private set; }
        public TranslationPrototype[] NameList { get; private set; }
        public HUDEntitySettingsPrototype HUDEntitySettingOverride { get; private set; }
    }

    public class FormationTypePrototype : Prototype
    {
        public FormationFacing Facing { get; private set; }
        public float Spacing { get; private set; }
    }
}
