namespace MHServerEmu.Games.GameData.Prototypes
{
    public class HUDEntitySettingsPrototype : Prototype
    {
        public HUDEntityFloorEffect FloorEffect;
        public HUDEntityOverheadIcon OverheadIcon;
        public ulong MapIcon;
        public ulong EdgeIcon;

        public HUDEntitySettingsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(HUDEntitySettingsPrototype), proto); }
    }

    public enum HUDEntityFloorEffect
    {
        None = 0,
        Generic = 1,
        Target = 2,
        Rescue = 3,
    }

    public enum HUDEntityOverheadIcon
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

    public class UIMapInfoIconBehaviorPrototype : Prototype
    {
        public ulong IconPath;
        public ulong IconPathHiRes;
        public UIMapInfoIconBehaviorPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(UIMapInfoIconBehaviorPrototype), proto); }
    }

    public class UIMapInfoIconAppearancePrototype : Prototype
    {
        public ulong IconOnScreen;
        public ulong IconOffScreen;
        public UIMapInfoIconAppearancePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(UIMapInfoIconAppearancePrototype), proto); }
    }

    public class ObjectiveInfoPrototype : Prototype
    {
        public ulong EdgeColor;
        public bool EdgeEnabled;
        public bool EdgeOnlyInArea;
        public int EdgeRange;
        public bool FloorRingAnimation;
        public bool MapEnabled;
        public int MapRange;
        public bool ShowToSummonerOnly;
        public bool TrackAfterDiscovery;
        public ObjectiveVisibility Visibility;

        public ObjectiveInfoPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ObjectiveInfoPrototype), proto); }
    }

    public enum ObjectiveVisibility
    {
        VisibleOnlyByMission,
        VisibleWhenFound,
        VisibleAlways,
        VisibleToParty,
    }

    public class EntityFilterSettingsPrototype : Prototype
    {
        public EntityFilterPrototype EntityFilter;
        public ScriptRoleKey ScriptRoleKey;
        public TranslationPrototype[] NameList;
        public HUDEntitySettingsPrototype HUDEntitySettingOverride;

        public EntityFilterSettingsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterSettingsPrototype), proto); }

    }

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

    public class FormationTypePrototype : Prototype
    {
        public FormationFacing Facing;
        public float Spacing;
        public FormationTypePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(FormationTypePrototype), proto); }
    }

    public enum FormationFacing
    {
        None = 0,
        FaceParent = 0,
        FaceParentInverse = 1,
        FaceOrigin = 2,
        FaceOriginInverse = 3,
    }
}
