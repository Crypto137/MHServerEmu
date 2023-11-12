using MHServerEmu.Games.GameData.Prototypes;


namespace MHServerEmu.Games.Generators.Prototypes
{
    public class PopulationObjectPrototype : Prototype
    {
        public ulong AllianceOverride;
        public bool AllowCrossMissionHostility;
        public ulong EntityActionTimelineScript;
        public EntityFilterSettingsPrototype[] EntityFilterSettings;
        public ulong[] EntityFilterSettingTemplates;
        public EvalPrototype EvalSpawnProperties;
        public FormationTypePrototype Formation;
        public ulong FormationTemplate;
        public int GameModeScoreValue;
        public bool IgnoreBlackout;
        public bool IgnoreNaviCheck;
        public float LeashDistance;
        public ulong OnDefeatLootTable;
        public SpawnOrientationTweak OrientationTweak;
        public PopulationRiderPrototype[] Riders;
        public bool UseMarkerOrientation;
        public ulong UsePopulationMarker;
        public ulong CleanUpPolicy;

        public PopulationObjectPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationObjectPrototype), proto); }
    }

    public class EntityFilterSettingsPrototype : Prototype
    {
        public EntityFilterPrototype EntityFilter;
        public ScriptRoleKey ScriptRoleKey;
        public TranslationPrototype[] NameList;
        public HUDEntitySettingsPrototype HUDEntitySettingOverride;

        public EntityFilterSettingsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterSettingsPrototype), proto); }

    }

    public enum ScriptRoleKey {
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

    public class PopulationRiderPrototype : Prototype
    {
        public PopulationRiderPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationRiderPrototype), proto); }
    }

    public class PopulationRiderEntityPrototype : PopulationRiderPrototype
    {
        public ulong Entity;
        public PopulationRiderEntityPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationRiderEntityPrototype), proto); }

    }

    public class PopulationRiderBlackOutPrototype : PopulationRiderPrototype
    {
        public ulong BlackOutZone;
        public PopulationRiderBlackOutPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationRiderBlackOutPrototype), proto); }

    }

    public class FormationTypePrototype : Prototype
    {
        public FormationFacing Facing;
        public float Spacing;
        public FormationTypePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(FormationTypePrototype), proto); }
    }

    public enum FormationFacing {
	    None = 0,
	    FaceParent = 0,
	    FaceParentInverse = 1,
	    FaceOrigin = 2,
	    FaceOriginInverse = 3,
    }

    public enum SpawnOrientationTweak {
	    Default,
	    Offset15,
	    Random,
    }

    public class PopulationRequiredObjectPrototype : Prototype
    {
        public PopulationObjectPrototype Object;
        public ulong ObjectTemplate;
        public short Count;
        public EvalPrototype EvalSpawnProperties;
        public ulong RankOverride;
        public bool Critical;
        public float Density;
        public ulong[] RestrictToCells;
        public ulong[] RestrictToAreas;
        public ulong RestrictToDifficultyMin;
        public ulong RestrictToDifficultyMax;

        public PopulationRequiredObjectPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationRequiredObjectPrototype), proto); }
    }
}
