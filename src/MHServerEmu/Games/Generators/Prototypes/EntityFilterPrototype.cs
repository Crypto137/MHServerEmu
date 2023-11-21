using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class EntityFilterPrototype : Prototype
    {
        public EntityFilterPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterPrototype), proto); }

    }

    public class EntityFilterFilterListPrototype : EntityFilterPrototype
    {
        public EntityFilterPrototype[] Filters;
        public EntityFilterFilterListPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterFilterListPrototype), proto); }
    }

    public class EntityFilterAndPrototype : EntityFilterFilterListPrototype
    {
        public EntityFilterAndPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterAndPrototype), proto); }
    }

    public class EntityFilterHasAlliancePrototype : EntityFilterPrototype
    {
        public ulong Alliance;
        public EntityFilterHasAlliancePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterHasAlliancePrototype), proto); }
    }

    public class EntityFilterScriptKeyPrototype : EntityFilterPrototype
    {
        public ScriptRoleKey ScriptKey;
        public EntityFilterScriptKeyPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterScriptKeyPrototype), proto); }
    }

    public class EntityFilterHasKeywordPrototype : EntityFilterPrototype
    {
        public ulong Keyword;
        public EntityFilterHasKeywordPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterHasKeywordPrototype), proto); }
    }

    public class EntityFilterHasNegStatusEffectPrototype : EntityFilterPrototype
    {
        public EntityFilterHasNegStatusEffectPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterHasNegStatusEffectPrototype), proto); }
    }

    public class EntityFilterHasPrototypePrototype : EntityFilterPrototype
    {
        public ulong EntityPrototype;
        public bool IncludeChildPrototypes;
        public EntityFilterHasPrototypePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterHasPrototypePrototype), proto); }
    }

    public class EntityFilterInAreaPrototype : EntityFilterPrototype
    {
        public ulong InArea;
        public EntityFilterInAreaPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterInAreaPrototype), proto); }
    }

    public class EntityFilterInCellPrototype : EntityFilterPrototype
    {
        public ulong[] InCells;
        public EntityFilterInCellPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterInCellPrototype), proto); }
    }

    public class EntityFilterInLocationWithKeywordPrototype : EntityFilterPrototype
    {
        public ulong Keyword;
        public EntityFilterInLocationWithKeywordPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterInLocationWithKeywordPrototype), proto); }
    }

    public class EntityFilterInRegionPrototype : EntityFilterPrototype
    {
        public ulong InRegion;
        public EntityFilterInRegionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterInRegionPrototype), proto); }
    }

    public enum MissionState {
	    Invalid,
	    Inactive,
	    Available,
	    Active,
	    Completed,
	    Failed,
    }

    public class EntityFilterMissionStatePrototype : EntityFilterPrototype
    {
        public ulong Mission;
        public MissionState State;
        public EntityFilterMissionStatePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterMissionStatePrototype), proto); }
    }

    public class EntityFilterIsHostileToPlayersPrototype : EntityFilterPrototype
    {
        public EntityFilterIsHostileToPlayersPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterIsHostileToPlayersPrototype), proto); }
    }

    public class EntityFilterIsMemberOfSuperteamPrototype : EntityFilterPrototype
    {
        public ulong Superteam;
        public EntityFilterIsMemberOfSuperteamPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterIsMemberOfSuperteamPrototype), proto); }
    }

    public class EntityFilterIsMissionContributorPrototype : EntityFilterPrototype
    {
        public EntityFilterIsMissionContributorPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterIsMissionContributorPrototype), proto); }
    }

    public class EntityFilterIsMissionParticipantPrototype : EntityFilterPrototype
    {
        public EntityFilterIsMissionParticipantPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterIsMissionParticipantPrototype), proto); }
    }

    public class EntityFilterIsPartyMemberPrototype : EntityFilterPrototype
    {
        public EntityFilterIsPartyMemberPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterIsPartyMemberPrototype), proto); }
    }

    public class EntityFilterIsPlayerAvatarPrototype : EntityFilterPrototype
    {
        public EntityFilterIsPlayerAvatarPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterIsPlayerAvatarPrototype), proto); }
    }

    public class EntityFilterIsPowerOwnerPrototype : EntityFilterPrototype
    {
        public EntityFilterIsPowerOwnerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterIsPowerOwnerPrototype), proto); }
    }

    public class EntityFilterNotPrototype : EntityFilterPrototype
    {
        public EntityFilterPrototype EntityFilter;
        public EntityFilterNotPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterNotPrototype), proto); }
    }

    public class EntityFilterOrPrototype : EntityFilterFilterListPrototype
    {
        public EntityFilterOrPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterOrPrototype), proto); }
    }

    public class EntityFilterSpawnedByEncounterPrototype : EntityFilterPrototype
    {
        public ulong EncounterResource;
        public EntityFilterSpawnedByEncounterPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterSpawnedByEncounterPrototype), proto); }
    }

    public class EntityFilterSpawnedByMissionPrototype : EntityFilterPrototype
    {
        public ulong MissionPrototype;
        public EntityFilterSpawnedByMissionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterSpawnedByMissionPrototype), proto); }
    }

    public class EntityFilterSpawnedBySpawnerPrototype : EntityFilterPrototype
    {
        public ulong SpawnerPrototype;
        public EntityFilterSpawnedBySpawnerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterSpawnedBySpawnerPrototype), proto); }
    }

    public class EntityFilterHasPrestigeLevelPrototype : EntityFilterPrototype
    {
        public ulong PrestigeLevel;
        public EntityFilterHasPrestigeLevelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterHasPrestigeLevelPrototype), proto); }
    }

    public class EntityFilterHasRankPrototype : EntityFilterPrototype
    {
        public ulong RankPrototype;
        public EntityFilterHasRankPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterHasRankPrototype), proto); }
    }

    public class EntityFilterItemRarityPrototype : EntityFilterPrototype
    {
        public ulong Rarity;
        public EntityFilterItemRarityPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterItemRarityPrototype), proto); }
    }

    public class ScoreTableSchemaEntryPrototype : Prototype
    {
        public ScoreTableValueType Type;
        public ulong Name;
        public EvalPrototype EvalOnPlayerAdd;
        public EvalPrototype EvalAuto;
        public EntityFilterPrototype OnEntityDeathFilter;
        public ScoreTableValueEventToEnum Event;
        public ScoreTableSchemaEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoreTableSchemaEntryPrototype), proto); }
    }

    public enum ScoreTableValueType
    {
        Invalid = 0,
        Int = 1,
        Float = 2,
    }

    public enum ScoreTableValueEventToEnum
    {
        Invalid = 0,
        DamageTaken = 1,
        DamageDealt = 2,
        Deaths = 3,
        PlayerAssists = 4,
        PlayerDamageDealt = 5,
        PlayerKills = 6,
    }

    public class ScoreTableSchemaPrototype : Prototype
    {
        public ScoreTableSchemaEntryPrototype[] Schema;
        public ScoreTableSchemaPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoreTableSchemaPrototype), proto); }
    }

}
