using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum MissionState
    {
        Invalid,
        Inactive,
        Available,
        Active,
        Completed,
        Failed,
    }

    [AssetEnum]
    public enum ScoreTableValueType
    {
        Invalid = 0,
        Int = 1,
        Float = 2,
    }

    [AssetEnum]
    public enum ScoreTableValueEvent
    {
        Invalid = 0,
        DamageTaken = 1,
        DamageDealt = 2,
        Deaths = 3,
        PlayerAssists = 4,
        PlayerDamageDealt = 5,
        PlayerKills = 6,
    }

    #endregion

    public class EntityFilterPrototype : Prototype
    {
    }

    public class EntityFilterFilterListPrototype : EntityFilterPrototype
    {
        public EntityFilterPrototype[] Filters { get; set; }
    }

    public class EntityFilterAndPrototype : EntityFilterFilterListPrototype
    {
    }

    public class EntityFilterHasAlliancePrototype : EntityFilterPrototype
    {
        public ulong Alliance { get; set; }
    }

    public class EntityFilterScriptKeyPrototype : EntityFilterPrototype
    {
        public ScriptRoleKeyEnum ScriptKey { get; set; }
    }

    public class EntityFilterHasKeywordPrototype : EntityFilterPrototype
    {
        public ulong Keyword { get; set; }
    }

    public class EntityFilterHasNegStatusEffectPrototype : EntityFilterPrototype
    {
    }

    public class EntityFilterHasPrototypePrototype : EntityFilterPrototype
    {
        public ulong EntityPrototype { get; set; }
        public bool IncludeChildPrototypes { get; set; }
    }

    public class EntityFilterInAreaPrototype : EntityFilterPrototype
    {
        public ulong InArea { get; set; }
    }

    public class EntityFilterInCellPrototype : EntityFilterPrototype
    {
        public ulong[] InCells { get; set; }
    }

    public class EntityFilterInLocationWithKeywordPrototype : EntityFilterPrototype
    {
        public ulong Keyword { get; set; }
    }

    public class EntityFilterInRegionPrototype : EntityFilterPrototype
    {
        public ulong InRegion { get; set; }
    }

    public class EntityFilterMissionStatePrototype : EntityFilterPrototype
    {
        public ulong Mission { get; set; }
        public MissionState State { get; set; }
    }

    public class EntityFilterIsHostileToPlayersPrototype : EntityFilterPrototype
    {
    }

    public class EntityFilterIsMemberOfSuperteamPrototype : EntityFilterPrototype
    {
        public ulong Superteam { get; set; }
    }

    public class EntityFilterIsMissionContributorPrototype : EntityFilterPrototype
    {
    }

    public class EntityFilterIsMissionParticipantPrototype : EntityFilterPrototype
    {
    }

    public class EntityFilterIsPartyMemberPrototype : EntityFilterPrototype
    {
    }

    public class EntityFilterIsPlayerAvatarPrototype : EntityFilterPrototype
    {
    }

    public class EntityFilterIsPowerOwnerPrototype : EntityFilterPrototype
    {
    }

    public class EntityFilterNotPrototype : EntityFilterPrototype
    {
        public EntityFilterPrototype EntityFilter { get; set; }
    }

    public class EntityFilterOrPrototype : EntityFilterFilterListPrototype
    {
    }

    public class EntityFilterSpawnedByEncounterPrototype : EntityFilterPrototype
    {
        public ulong EncounterResource { get; set; }
    }

    public class EntityFilterSpawnedByMissionPrototype : EntityFilterPrototype
    {
        public ulong MissionPrototype { get; set; }
    }

    public class EntityFilterSpawnedBySpawnerPrototype : EntityFilterPrototype
    {
        public ulong SpawnerPrototype { get; set; }
    }

    public class EntityFilterHasPrestigeLevelPrototype : EntityFilterPrototype
    {
        public ulong PrestigeLevel { get; set; }
    }

    public class EntityFilterHasRankPrototype : EntityFilterPrototype
    {
        public ulong RankPrototype { get; set; }
    }

    public class EntityFilterItemRarityPrototype : EntityFilterPrototype
    {
        public ulong Rarity { get; set; }
    }

    public class ScoreTableSchemaEntryPrototype : Prototype
    {
        public ScoreTableValueType Type { get; set; }
        public ulong Name { get; set; }
        public EvalPrototype EvalOnPlayerAdd { get; set; }
        public EvalPrototype EvalAuto { get; set; }
        public EntityFilterPrototype OnEntityDeathFilter { get; set; }
        public ScoreTableValueEvent Event { get; set; }
    }

    public class ScoreTableSchemaPrototype : Prototype
    {
        public ScoreTableSchemaEntryPrototype[] Schema { get; set; }
    }
}
