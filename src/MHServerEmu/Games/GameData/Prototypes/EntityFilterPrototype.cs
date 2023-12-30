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
        public EntityFilterPrototype[] Filters { get; protected set; }
    }

    public class EntityFilterAndPrototype : EntityFilterFilterListPrototype
    {
    }

    public class EntityFilterHasAlliancePrototype : EntityFilterPrototype
    {
        public ulong Alliance { get; protected set; }
    }

    public class EntityFilterScriptKeyPrototype : EntityFilterPrototype
    {
        public ScriptRoleKeyEnum ScriptKey { get; protected set; }
    }

    public class EntityFilterHasKeywordPrototype : EntityFilterPrototype
    {
        public ulong Keyword { get; protected set; }
    }

    public class EntityFilterHasNegStatusEffectPrototype : EntityFilterPrototype
    {
    }

    public class EntityFilterHasPrototypePrototype : EntityFilterPrototype
    {
        public ulong EntityPrototype { get; protected set; }
        public bool IncludeChildPrototypes { get; protected set; }
    }

    public class EntityFilterInAreaPrototype : EntityFilterPrototype
    {
        public ulong InArea { get; protected set; }
    }

    public class EntityFilterInCellPrototype : EntityFilterPrototype
    {
        public ulong[] InCells { get; protected set; }
    }

    public class EntityFilterInLocationWithKeywordPrototype : EntityFilterPrototype
    {
        public ulong Keyword { get; protected set; }
    }

    public class EntityFilterInRegionPrototype : EntityFilterPrototype
    {
        public ulong InRegion { get; protected set; }
    }

    public class EntityFilterMissionStatePrototype : EntityFilterPrototype
    {
        public ulong Mission { get; protected set; }
        public MissionState State { get; protected set; }
    }

    public class EntityFilterIsHostileToPlayersPrototype : EntityFilterPrototype
    {
    }

    public class EntityFilterIsMemberOfSuperteamPrototype : EntityFilterPrototype
    {
        public ulong Superteam { get; protected set; }
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
        public EntityFilterPrototype EntityFilter { get; protected set; }
    }

    public class EntityFilterOrPrototype : EntityFilterFilterListPrototype
    {
    }

    public class EntityFilterSpawnedByEncounterPrototype : EntityFilterPrototype
    {
        public ulong EncounterResource { get; protected set; }
    }

    public class EntityFilterSpawnedByMissionPrototype : EntityFilterPrototype
    {
        public ulong MissionPrototype { get; protected set; }
    }

    public class EntityFilterSpawnedBySpawnerPrototype : EntityFilterPrototype
    {
        public ulong SpawnerPrototype { get; protected set; }
    }

    public class EntityFilterHasPrestigeLevelPrototype : EntityFilterPrototype
    {
        public ulong PrestigeLevel { get; protected set; }
    }

    public class EntityFilterHasRankPrototype : EntityFilterPrototype
    {
        public ulong RankPrototype { get; protected set; }
    }

    public class EntityFilterItemRarityPrototype : EntityFilterPrototype
    {
        public ulong Rarity { get; protected set; }
    }

    public class ScoreTableSchemaEntryPrototype : Prototype
    {
        public ScoreTableValueType Type { get; protected set; }
        public ulong Name { get; protected set; }
        public EvalPrototype EvalOnPlayerAdd { get; protected set; }
        public EvalPrototype EvalAuto { get; protected set; }
        public EntityFilterPrototype OnEntityDeathFilter { get; protected set; }
        public ScoreTableValueEvent Event { get; protected set; }
    }

    public class ScoreTableSchemaPrototype : Prototype
    {
        public ScoreTableSchemaEntryPrototype[] Schema { get; protected set; }
    }
}
