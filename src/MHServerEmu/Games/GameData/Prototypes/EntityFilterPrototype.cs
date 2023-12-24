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
        public EntityFilterPrototype[] Filters { get; private set; }
    }

    public class EntityFilterAndPrototype : EntityFilterFilterListPrototype
    {
    }

    public class EntityFilterHasAlliancePrototype : EntityFilterPrototype
    {
        public ulong Alliance { get; private set; }
    }

    public class EntityFilterScriptKeyPrototype : EntityFilterPrototype
    {
        public ScriptRoleKeyEnum ScriptKey { get; private set; }
    }

    public class EntityFilterHasKeywordPrototype : EntityFilterPrototype
    {
        public ulong Keyword { get; private set; }
    }

    public class EntityFilterHasNegStatusEffectPrototype : EntityFilterPrototype
    {
    }

    public class EntityFilterHasPrototypePrototype : EntityFilterPrototype
    {
        public ulong EntityPrototype { get; private set; }
        public bool IncludeChildPrototypes { get; private set; }
    }

    public class EntityFilterInAreaPrototype : EntityFilterPrototype
    {
        public ulong InArea { get; private set; }
    }

    public class EntityFilterInCellPrototype : EntityFilterPrototype
    {
        public ulong[] InCells { get; private set; }
    }

    public class EntityFilterInLocationWithKeywordPrototype : EntityFilterPrototype
    {
        public ulong Keyword { get; private set; }
    }

    public class EntityFilterInRegionPrototype : EntityFilterPrototype
    {
        public ulong InRegion { get; private set; }
    }

    public class EntityFilterMissionStatePrototype : EntityFilterPrototype
    {
        public ulong Mission { get; private set; }
        public MissionState State { get; private set; }
    }

    public class EntityFilterIsHostileToPlayersPrototype : EntityFilterPrototype
    {
    }

    public class EntityFilterIsMemberOfSuperteamPrototype : EntityFilterPrototype
    {
        public ulong Superteam { get; private set; }
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
        public EntityFilterPrototype EntityFilter { get; private set; }
    }

    public class EntityFilterOrPrototype : EntityFilterFilterListPrototype
    {
    }

    public class EntityFilterSpawnedByEncounterPrototype : EntityFilterPrototype
    {
        public ulong EncounterResource { get; private set; }
    }

    public class EntityFilterSpawnedByMissionPrototype : EntityFilterPrototype
    {
        public ulong MissionPrototype { get; private set; }
    }

    public class EntityFilterSpawnedBySpawnerPrototype : EntityFilterPrototype
    {
        public ulong SpawnerPrototype { get; private set; }
    }

    public class EntityFilterHasPrestigeLevelPrototype : EntityFilterPrototype
    {
        public ulong PrestigeLevel { get; private set; }
    }

    public class EntityFilterHasRankPrototype : EntityFilterPrototype
    {
        public ulong RankPrototype { get; private set; }
    }

    public class EntityFilterItemRarityPrototype : EntityFilterPrototype
    {
        public ulong Rarity { get; private set; }
    }

    public class ScoreTableSchemaEntryPrototype : Prototype
    {
        public ScoreTableValueType Type { get; private set; }
        public ulong Name { get; private set; }
        public EvalPrototype EvalOnPlayerAdd { get; private set; }
        public EvalPrototype EvalAuto { get; private set; }
        public EntityFilterPrototype OnEntityDeathFilter { get; private set; }
        public ScoreTableValueEvent Event { get; private set; }
    }

    public class ScoreTableSchemaPrototype : Prototype
    {
        public ScoreTableSchemaEntryPrototype[] Schema { get; private set; }
    }
}
