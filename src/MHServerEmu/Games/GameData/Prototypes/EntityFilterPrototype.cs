using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)Invalid)]
    public enum MissionState
    {
        Invalid,
        Inactive,
        Available,
        Active,
        Completed,
        Failed,
    }

    [AssetEnum((int)Invalid)]
    public enum ScoreTableValueType
    {
        Invalid = 0,
        Int = 1,
        Float = 2,
    }

    [AssetEnum((int)Invalid)]
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
        public virtual void GetAreaDataRefs(HashSet<PrototypeId> refs) { }
        public virtual void GetEntityDataRefs(HashSet<PrototypeId> refs) { }
        public virtual void GetRegionDataRefs(HashSet<PrototypeId> refs) { }
        public virtual void GetKeywordDataRefs(HashSet<PrototypeId> refs) { }
    }

    public class EntityFilterFilterListPrototype : EntityFilterPrototype
    {
        public EntityFilterPrototype[] Filters { get; protected set; }

        public override void GetAreaDataRefs(HashSet<PrototypeId> refs)
        {
            if (Filters.IsNullOrEmpty()) return;
            foreach (var prototype in Filters)
                prototype?.GetAreaDataRefs(refs);
        }
        public override void GetEntityDataRefs(HashSet<PrototypeId> refs)
        {
            if (Filters.IsNullOrEmpty()) return;
            foreach (var prototype in Filters)
                prototype?.GetEntityDataRefs(refs);
        }
        public override void GetRegionDataRefs(HashSet<PrototypeId> refs)
        {
            if (Filters.IsNullOrEmpty()) return;
            foreach (var prototype in Filters)
                prototype?.GetRegionDataRefs(refs);
        }
    }

    public class EntityFilterAndPrototype : EntityFilterFilterListPrototype
    {
    }

    public class EntityFilterHasAlliancePrototype : EntityFilterPrototype
    {
        public PrototypeId Alliance { get; protected set; }
    }

    public class EntityFilterScriptKeyPrototype : EntityFilterPrototype
    {
        public ScriptRoleKeyEnum ScriptKey { get; protected set; }
    }

    public class EntityFilterHasKeywordPrototype : EntityFilterPrototype
    {
        public PrototypeId Keyword { get; protected set; }

        public override void GetKeywordDataRefs(HashSet<PrototypeId> refs)
        {
            if (Keyword != PrototypeId.Invalid) refs.Add(Keyword);
        }
    }

    public class EntityFilterHasNegStatusEffectPrototype : EntityFilterPrototype
    {
    }

    public class EntityFilterHasPrototypePrototype : EntityFilterPrototype
    {
        public PrototypeId EntityPrototype { get; protected set; }
        public bool IncludeChildPrototypes { get; protected set; }

        public override void GetEntityDataRefs(HashSet<PrototypeId> refs)
        {
            if (EntityPrototype != PrototypeId.Invalid && GameDatabase.DataDirectory.PrototypeIsADefaultPrototype(EntityPrototype) == false)
                refs.Add(EntityPrototype);
        }
    }

    public class EntityFilterInAreaPrototype : EntityFilterPrototype
    {
        public PrototypeId InArea { get; protected set; }

        public override void GetAreaDataRefs(HashSet<PrototypeId> refs)
        {
            if (InArea != PrototypeId.Invalid) refs.Add(InArea);
        }
    }

    public class EntityFilterInCellPrototype : EntityFilterPrototype
    {
        public AssetId[] InCells { get; protected set; }
    }

    public class EntityFilterInLocationWithKeywordPrototype : EntityFilterPrototype
    {
        public PrototypeId Keyword { get; protected set; }
    }

    public class EntityFilterInRegionPrototype : EntityFilterPrototype
    {
        public PrototypeId InRegion { get; protected set; }

        public override void GetRegionDataRefs(HashSet<PrototypeId> refs)
        {
            if (InRegion != PrototypeId.Invalid) refs.Add(InRegion);
        }
    }

    public class EntityFilterMissionStatePrototype : EntityFilterPrototype
    {
        public PrototypeId Mission { get; protected set; }
        public MissionState State { get; protected set; }
    }

    public class EntityFilterIsHostileToPlayersPrototype : EntityFilterPrototype
    {
    }

    public class EntityFilterIsMemberOfSuperteamPrototype : EntityFilterPrototype
    {
        public PrototypeId Superteam { get; protected set; }
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
        public AssetId EncounterResource { get; protected set; }
    }

    public class EntityFilterSpawnedByMissionPrototype : EntityFilterPrototype
    {
        public PrototypeId MissionPrototype { get; protected set; }
    }

    public class EntityFilterSpawnedBySpawnerPrototype : EntityFilterPrototype
    {
        public PrototypeId SpawnerPrototype { get; protected set; }
    }

    public class EntityFilterHasPrestigeLevelPrototype : EntityFilterPrototype
    {
        public PrototypeId PrestigeLevel { get; protected set; }
    }

    public class EntityFilterHasRankPrototype : EntityFilterPrototype
    {
        public PrototypeId RankPrototype { get; protected set; }
    }

    public class EntityFilterItemRarityPrototype : EntityFilterPrototype
    {
        public PrototypeId Rarity { get; protected set; }
    }

    public class ScoreTableSchemaEntryPrototype : Prototype
    {
        public ScoreTableValueType Type { get; protected set; }
        public LocaleStringId Name { get; protected set; }
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
