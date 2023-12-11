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
        public HUDEntityFloorEffect FloorEffect { get; set; }
        public HUDEntityOverheadIcon OverheadIcon { get; set; }
        public ulong MapIcon { get; set; }
        public ulong EdgeIcon { get; set; }
    }

    public class UIMapInfoIconBehaviorPrototype : Prototype
    {
        public ulong IconPath { get; set; }
        public ulong IconPathHiRes { get; set; }
    }

    public class UIMapInfoIconAppearancePrototype : Prototype
    {
        public ulong IconOnScreen { get; set; }
        public ulong IconOffScreen { get; set; }
    }

    public class ObjectiveInfoPrototype : Prototype
    {
        public ulong EdgeColor { get; set; }
        public bool EdgeEnabled { get; set; }
        public bool EdgeOnlyInArea { get; set; }
        public int EdgeRange { get; set; }
        public bool FloorRingAnimation { get; set; }
        public bool MapEnabled { get; set; }
        public int MapRange { get; set; }
        public bool ShowToSummonerOnly { get; set; }
        public bool TrackAfterDiscovery { get; set; }
        public ObjectiveVisibility Visibility { get; set; }
    }

    public class EntityFilterSettingsPrototype : Prototype
    {
        public EntityFilterPrototype EntityFilter { get; set; }
        public ScriptRoleKeyEnum ScriptRoleKey { get; set; }
        public TranslationPrototype[] NameList { get; set; }
        public HUDEntitySettingsPrototype HUDEntitySettingOverride { get; set; }
    }

    public class FormationTypePrototype : Prototype
    {
        public FormationFacing Facing { get; set; }
        public float Spacing { get; set; }
    }
}
