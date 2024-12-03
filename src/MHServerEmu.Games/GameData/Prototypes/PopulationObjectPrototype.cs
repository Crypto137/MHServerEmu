using System.Text;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.MetaGames;
using MHServerEmu.Games.Populations;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PopulationObjectPrototype : Prototype
    {
        public PrototypeId AllianceOverride { get; protected set; }
        public bool AllowCrossMissionHostility { get; protected set; }
        public PrototypeId EntityActionTimelineScript { get; protected set; }
        public EntityFilterSettingsPrototype[] EntityFilterSettings { get; protected set; }
        public PrototypeId[] EntityFilterSettingTemplates { get; protected set; }
        public EvalPrototype EvalSpawnProperties { get; protected set; }
        public FormationTypePrototype Formation { get; protected set; }
        public PrototypeId FormationTemplate { get; protected set; }
        public int GameModeScoreValue { get; protected set; }
        public bool IgnoreBlackout { get; protected set; }
        public bool IgnoreNaviCheck { get; protected set; }
        public float LeashDistance { get; protected set; }
        public PrototypeId OnDefeatLootTable { get; protected set; }
        public SpawnOrientationTweak OrientationTweak { get; protected set; }
        public PopulationRiderPrototype[] Riders { get; protected set; }
        public bool UseMarkerOrientation { get; protected set; }
        public PrototypeId UsePopulationMarker { get; protected set; }
        public PrototypeId CleanUpPolicy { get; protected set; }

        [DoNotCopy]
        public int PopulationObjectPrototypeEnumValue { get; private set; }

        public override void PostProcess()
        {
            base.PostProcess();

            PopulationObjectPrototypeEnumValue = GetEnumValueFromBlueprint(LiveTuningData.GetPopulationObjectBlueprintDataRef());
        }

        public virtual void BuildCluster(ClusterGroup group, ClusterObjectFlag flags)
        {
            if (Riders.HasValue() && flags.HasFlag(ClusterObjectFlag.Henchmen) == false)
            {
                foreach(var rider in Riders)
                    if (rider is PopulationRiderEntityPrototype riderEntityProto)
                    {
                        if (riderEntityProto == null) continue;
                        ClusterEntity cluster = group.CreateClusterEntity(riderEntityProto.Entity);
                        if (cluster == null) return;
                        cluster.Flags |= flags;
                        cluster.Flags |= ClusterObjectFlag.SkipFormation;
                    }
            }
        }

        public virtual void GetContainedEntities(HashSet<PrototypeId> entities, bool unwrapEntitySelectors = false)
        {
            if (Riders.HasValue())
            {
                foreach (var rider in Riders)
                    if (rider is PopulationRiderEntityPrototype riderEntityProto && riderEntityProto.Entity != PrototypeId.Invalid)
                        entities.Add(riderEntityProto.Entity);
            }
        }

        public static int UnwrapEntitySelector(PrototypeId selectorRef, HashSet<PrototypeId> entities)
        {
            int count = 0;
            var proto = GameDatabase.GetPrototype<Prototype>(selectorRef);
            if (proto is EntitySelectorPrototype selectorProto && selectorProto.Entities.HasValue())
                foreach (PrototypeId entity in selectorProto.Entities)
                {
                    entities.Add(entity);
                    count++;
                }

            return count;
        }

        public FormationTypePrototype GetFormation()
        {
            if (Formation != null)
                return Formation;
            else
                return GameDatabase.GetPrototype<FormationTypePrototype>(FormationTemplate);
        }

        public override string ToString()
        {
            HashSet<PrototypeId> entities = new();
            GetContainedEntities(entities);

            StringBuilder sb = new();
            sb.AppendLine($"[{GetType().Name}]");
            if (entities.Count > 0)
            {
                sb.AppendLine($"Entity: {entities.First().GetNameFormatted()}");
                sb.AppendLine($"Entities: {entities.Count}");
            }
            sb.AppendLine($"Marker: {GameDatabase.GetFormattedPrototypeName(UsePopulationMarker)}");
            sb.AppendLine($"Riders: {Riders.Length}");
            return sb.ToString();
        }

        public virtual float GetAverageSize() => 0.0f;
    }

    public class PopulationEntityPrototype : PopulationObjectPrototype
    {
        public PrototypeId Entity { get; protected set; }

        public override void BuildCluster(ClusterGroup group, ClusterObjectFlag flags)
        {
            ClusterEntity clusterEntity = group.CreateClusterEntity(Entity);
            if (clusterEntity == null) return;            
            clusterEntity.Flags |= flags;

            base.BuildCluster(group, flags);
        }

        public override void GetContainedEntities(HashSet<PrototypeId> entities, bool unwrapEntitySelectors = false)
        {            
            base.GetContainedEntities(entities, unwrapEntitySelectors);

            if (Entity != PrototypeId.Invalid)
                if (unwrapEntitySelectors == false || UnwrapEntitySelector(Entity, entities) == 0)
                    entities.Add(Entity);
        }

        public override float GetAverageSize() => 1.0f;

    }

    public class PopulationClusterFixedPrototype : PopulationObjectPrototype
    {
        public PrototypeId[] Entities { get; protected set; }
        public EntityCountEntryPrototype[] EntityEntries { get; protected set; }

        public override void BuildCluster(ClusterGroup group, ClusterObjectFlag flags)
        {
            if (Entities.HasValue())
                foreach (var entity in Entities)
                {
                    ClusterEntity clusterEntity = group.CreateClusterEntity(entity);
                    if (clusterEntity == null) continue;
                    clusterEntity.Flags |= flags;
                }

            if (EntityEntries.HasValue())
                foreach (var entry in EntityEntries)
                {
                    if (entry == null) continue;
                    for (int i = 0; i < entry.Count; i++)
                    {
                        ClusterEntity clusterEntity = group.CreateClusterEntity(entry.Entity);
                        if (clusterEntity == null) continue;
                        clusterEntity.Flags |= flags;
                    }
                }

            base.BuildCluster(group, flags);
        }

        public override void GetContainedEntities(HashSet<PrototypeId> entities, bool unwrapEntitySelectors = false)
        {
            base.GetContainedEntities(entities, unwrapEntitySelectors);
            InternalGetContainedEntities(entities, unwrapEntitySelectors);
        }

        public override float GetAverageSize() 
        {
            float count = 0.0f;
            if (Entities.HasValue())
                count += Entities.Length;

            if (EntityEntries.HasValue())
                foreach (var entry in EntityEntries)
                    count += entry.Count;
            return count;
        }

        private void InternalGetContainedEntities(HashSet<PrototypeId> entities, bool unwrapEntitySelectors)
        {
            if (Entities.HasValue())
                foreach (var entity in Entities)
                {
                    if (entity != PrototypeId.Invalid)
                        if (unwrapEntitySelectors == false || UnwrapEntitySelector(entity, entities) == 0)
                            entities.Add(entity);
                }

            if (EntityEntries.HasValue())
                foreach (var entry in EntityEntries)
                {
                    if (entry == null) continue;
                    var entity = entry.Entity;
                    if (entity != PrototypeId.Invalid)
                        if (unwrapEntitySelectors == false || UnwrapEntitySelector(entity, entities) == 0)
                            entities.Add(entity);
                }
        }
    }

    public class PopulationClusterPrototype : PopulationObjectPrototype
    {
        public short Max { get; protected set; }
        public short Min { get; protected set; }
        public float RandomOffset { get; protected set; }
        public PrototypeId Entity { get; protected set; }

        public override void BuildCluster(ClusterGroup group, ClusterObjectFlag flags)
        {
            if (Entity != PrototypeId.Invalid)
            {
                int numEntities = group.Random.Next(Min, Max + 1);
                if (numEntities == 0) return;
                for (int i = 0; i < numEntities; i++)
                {
                    ClusterEntity clusterEntity = group.CreateClusterEntity(Entity);
                    if (clusterEntity == null) continue;
                    clusterEntity.Flags |= flags;
                }
            }

            base.BuildCluster(group, flags);
        }

        public override void GetContainedEntities(HashSet<PrototypeId> entities, bool unwrapEntitySelectors = false)
        {
            base.GetContainedEntities(entities, unwrapEntitySelectors);

            if (Entity != PrototypeId.Invalid)
                if (unwrapEntitySelectors == false || UnwrapEntitySelector(Entity, entities) == 0)
                    entities.Add(Entity);
        }

        public override float GetAverageSize() => (Min + Max) / 2.0f;
    }

    public class PopulationClusterMixedPrototype : PopulationObjectPrototype
    {
        public short Max { get; protected set; }
        public short Min { get; protected set; }
        public float RandomOffset { get; protected set; }
        public PopulationObjectPrototype[] Choices { get; protected set; }

        public override void BuildCluster(ClusterGroup group, ClusterObjectFlag flags)
        {
            if (Choices.IsNullOrEmpty()) return;

            int numEntities = group.Random.Next(Min, Max + 1);

            // Add picker for Mix Choices in 1.53
            Picker<PopulationObjectPrototype> picker = new(group.Random);
            foreach (var obj in Choices)
                picker.Add(obj);

            if (picker.Empty()) return;
            
            for (int i = 0; i < numEntities; i++)
            {
                if (picker.Pick(out var obj))  // in 1.52 obj = null
                {
                    // TODO check obj as PopulationGroupPrototype
                    PopulationEntityPrototype choiceEntity = obj as PopulationEntityPrototype;
                    if (choiceEntity == null) continue;
                    ClusterEntity clusterEntity = group.CreateClusterEntity(choiceEntity.Entity);
                    if (clusterEntity == null) continue;
                    clusterEntity.Flags |= flags;
                }
            }
            
            base.BuildCluster(group, flags);            
        }

        public override void GetContainedEntities(HashSet<PrototypeId> entities, bool unwrapEntitySelectors = false)
        {
            base.GetContainedEntities(entities, unwrapEntitySelectors);
            InternalGetContainedEntities(entities, unwrapEntitySelectors);
        }

        public override float GetAverageSize() => (Min + Max) / 2.0f;

        private void InternalGetContainedEntities(HashSet<PrototypeId> entities, bool unwrapEntitySelectors)
        {
            if (Choices.HasValue())
                foreach (var choice in Choices)
                    choice?.GetContainedEntities(entities, unwrapEntitySelectors);
        }

    }

    public class PopulationLeaderPrototype : PopulationObjectPrototype
    {
        public PrototypeId Leader { get; protected set; }
        public PopulationObjectPrototype[] Henchmen { get; protected set; }

        public override void BuildCluster(ClusterGroup group, ClusterObjectFlag flags)
        {
            if (Leader != PrototypeId.Invalid)
            {
                ClusterEntity clusterEntity = group.CreateClusterEntity(Leader);
                if (clusterEntity == null) return;
                clusterEntity.Flags |= flags | ClusterObjectFlag.Leader;
            }

            if (Henchmen.HasValue())
            {
                // Add picker for choice henchmen
                Picker<PopulationObjectPrototype> picker = new(group.Random);
                foreach (var henchmen in Henchmen) picker.Add(henchmen);
                if (picker.Pick(out var henchmenEntry)) 
                    henchmenEntry.BuildCluster(group, ClusterObjectFlag.Henchmen);
            }

            base.BuildCluster(group, flags);
        }

        public override void GetContainedEntities(HashSet<PrototypeId> entities, bool unwrapEntitySelectors = false)
        {
            base.GetContainedEntities(entities, unwrapEntitySelectors);
            InternalGetContainedEntities(entities, unwrapEntitySelectors);
        }

        public override float GetAverageSize()
        {
            float count = 0.0f;
            if (Henchmen.HasValue())
            {
                foreach (var henchmen in Henchmen)
                    if (henchmen != null) count += henchmen.GetAverageSize();
                count /= Henchmen.Length;
            }
            return count + 1.0f;
        }

        private void InternalGetContainedEntities(HashSet<PrototypeId> entities, bool unwrapEntitySelectors)
        {
            if (Leader != PrototypeId.Invalid)
                if (unwrapEntitySelectors == false || UnwrapEntitySelector(Leader, entities) == 0)
                    entities.Add(Leader);

            if (Henchmen.HasValue())
                foreach (var henchmen in Henchmen)
                    henchmen?.GetContainedEntities(entities, unwrapEntitySelectors);
        }
    }

    public class PopulationEncounterPrototype : PopulationObjectPrototype
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public AssetId EncounterResource { get; protected set; }

        public override void BuildCluster(ClusterGroup group, ClusterObjectFlag flags)
        {
            PrototypeId encounterResourceRef = GetEncounterRef();
            if (encounterResourceRef == PrototypeId.Invalid) return;
            
            EncounterResourcePrototype encounterResourceProto = GetEncounterResource();
            if (encounterResourceProto == null) return;
                
            MarkerSetPrototype markerSet = encounterResourceProto.MarkerSet;
            if (markerSet == null) return;
                    
            group.Flags |= ClusterObjectFlag.SkipFormation;
            group.Properties[PropertyEnum.EncounterResource] = encounterResourceRef;

            foreach (var marker in markerSet.Markers)
            {
                if (marker is not EntityMarkerPrototype markerProto) continue;
                if (markerProto.EntityGuid == PrototypeGuid.Invalid)
                {
                    Logger.Warn($"Marker at in Cell:\n  {ToString()}\nand position:\n  {markerProto.Position}\nhas invalid GUID");
                    continue;
                }
                PrototypeId markerRef = GameDatabase.GetDataRefByPrototypeGuid(markerProto.EntityGuid);
                if (markerRef == PrototypeId.Invalid)
                {
                    Logger.Warn($"Marker at in Cell:\n  {ToString()}\nand position:\n  {markerProto.Position}\nhas invalid Ref, GUID was valid, so likely prototype ref was deleted from calligraphy:\n  {markerProto.LastKnownEntityName}");
                    continue;
                }
                
                Prototype proto = GameDatabase.GetPrototype<Prototype>(markerRef);

                if (proto is WorldEntityPrototype)
                {
                    ClusterEntity clusterEntity = group.CreateClusterEntity(markerRef);
                    if (clusterEntity != null)
                    {
                        clusterEntity.Flags |= flags;
                        clusterEntity.SetParentRelativePosition(markerProto.Position);
                        clusterEntity.SetParentRelativeOrientation(markerProto.Rotation);
                        clusterEntity.SnapToFloor = SpawnSpec.SnapToFloorConvert(markerProto.OverrideSnapToFloor, markerProto.OverrideSnapToFloorValue);
                        clusterEntity.EncounterSpawnPhase = markerProto.EncounterSpawnPhase;
                        if (markerProto.EntityGuid == (PrototypeGuid)13841901974787866331) clusterEntity.EncounterSpawnPhase = 0; // Hardfix for SHIELDAgentForgottenPyreEvent
                        clusterEntity.Flags |= ClusterObjectFlag.SkipFormation;
                    }
                }
                if (proto is BlackOutZonePrototype)
                {
                    if (group.BlackOutZone.Key == PrototypeId.Invalid)
                        group.BlackOutZone = new(markerRef, markerProto.Position);
                }
            }

            base.BuildCluster(group, flags);            
        }

        private EncounterResourcePrototype GetEncounterResource()
        {
            PrototypeId encounterProtoRef = GetEncounterRef();
            if (encounterProtoRef == PrototypeId.Invalid) return null;
            return GameDatabase.GetPrototype<EncounterResourcePrototype>(encounterProtoRef);
        }

        public override float GetAverageSize() => 1.0f;

        public PrototypeId GetEncounterRef()
        {
            if (EncounterResource == AssetId.Invalid)
            {
                Logger.Warn($"{ToString()} has no value in its EncounterResource field.");
                return PrototypeId.Invalid;
            }

            PrototypeId encounterProtoRef = GameDatabase.GetDataRefByAsset(EncounterResource);
            if (encounterProtoRef == PrototypeId.Invalid)
            {
                Logger.Warn($"{ToString()} was unable to find resource for asset {GameDatabase.GetAssetName(EncounterResource)}, check file path and verify file exists.");
                return PrototypeId.Invalid;
            }

            return encounterProtoRef;
        }

        public bool HasClientData()
        {
            var encounter = GetEncounterResource();
            if (encounter == null) return false;
            return encounter.HasEdges || encounter.ClientMap != null;
        }

        public override void GetContainedEntities(HashSet<PrototypeId> entities, bool unwrapEntitySelectors = false)
        {
            base.GetContainedEntities(entities, unwrapEntitySelectors);
            InternalGetContainedEntities(entities, unwrapEntitySelectors);
        }

        private void InternalGetContainedEntities(HashSet<PrototypeId> entities, bool unwrapEntitySelectors)
        {
            var resourceProto = GetEncounterResource();
            if (resourceProto != null && resourceProto.MarkerSet != null)
                resourceProto.MarkerSet.GetContainedEntities(entities);
        }
    }

    public class PopulationFormationPrototype : PopulationObjectPrototype
    {
        public PopulationRequiredObjectPrototype[] Objects { get; protected set; }

        public override void BuildCluster(ClusterGroup group, ClusterObjectFlag flags)
        {
            if (Objects.HasValue())
                foreach (var requiredObject in Objects)
                {
                    if (requiredObject == null) continue;
                    // check requiredObject.EvalSpawnProperties
                    for (int i = 0; i < requiredObject.Count; i++)
                    {
                        PopulationObjectPrototype objectProto = requiredObject.GetPopObject();
                        if ( objectProto == null ) continue;
                        ClusterGroup newGroup = group.CreateClusterGroup(objectProto);
                        if (newGroup == null) return;
                        newGroup.Flags |= flags;
                    }
                }

            base.BuildCluster(group, flags);
        }
    }

    public class PopulationGroupPrototype : PopulationObjectPrototype
    {
        public PopulationObjectPrototype[] EntitiesAndGroups { get; protected set; }

        public override void GetContainedEntities(HashSet<PrototypeId> entities, bool unwrapEntitySelectors = false)
        {
            if (EntitiesAndGroups.HasValue())
                foreach (var objectProto in EntitiesAndGroups)
                    objectProto?.GetContainedEntities(entities, unwrapEntitySelectors);
        }

    }

    public class PopulationRiderPrototype : Prototype
    {
    } 

    public class PopulationRiderEntityPrototype : PopulationRiderPrototype
    {
        public PrototypeId Entity { get; protected set; }
    }

    public class PopulationRiderBlackOutPrototype : PopulationRiderPrototype
    {
        public PrototypeId BlackOutZone { get; protected set; }
    }

    public class PopulationRequiredObjectPrototype : Prototype
    {
        public PopulationObjectPrototype Object { get; protected set; }
        public PrototypeId ObjectTemplate { get; protected set; }
        public short Count { get; protected set; }
        public EvalPrototype EvalSpawnProperties { get; protected set; }
        public PrototypeId RankOverride { get; protected set; }
        public bool Critical { get; protected set; }
        public float Density { get; protected set; }
        public AssetId[] RestrictToCells { get; protected set; }
        public PrototypeId[] RestrictToAreas { get; protected set; }
        public PrototypeId RestrictToDifficultyMin { get; protected set; }
        public PrototypeId RestrictToDifficultyMax { get; protected set; }

        public PopulationObjectPrototype GetPopObject()
        {
            if (Object != null)
                return Object;
            else
                return GameDatabase.GetPrototype<PopulationObjectPrototype>(ObjectTemplate);
        }

        public virtual void GetContainedEntities(HashSet<PrototypeId> refs) 
        {
            var objectProto = GetPopObject();
            objectProto?.GetContainedEntities( refs );
        }

        public void EvaluateSpawnProperties(PropertyCollection properties, Region region, MetaGame metaGame)
        {
            if (properties == null) return;

            if (RankOverride != PrototypeId.Invalid)
            {
                PrototypeId rankRef = RankPrototype.DoOverride(properties[PropertyEnum.Rank], RankOverride);
                if (rankRef != PrototypeId.Invalid) properties[PropertyEnum.Rank] = rankRef;
            }

            if (EvalSpawnProperties == null) return;
            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.Game = region.Game;
            evalContext.SetVar_PropertyCollectionPtr(EvalContext.Default, properties);
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, region.Properties);
            evalContext.SetReadOnlyVar_EntityPtr(EvalContext.Entity, metaGame);
            Eval.RunBool(EvalSpawnProperties, evalContext);
        }

        public bool AllowedInDifficulty(PrototypeId difficultyRef)
        {
            return DifficultyTierPrototype.InRange(difficultyRef, RestrictToDifficultyMin, RestrictToDifficultyMax);
        }
    }

    public class PopulationRequiredObjectListPrototype : Prototype
    {
        public PopulationRequiredObjectPrototype[] RequiredObjects { get; protected set; }

        public virtual void GetContainedEntities(HashSet<PrototypeId> entities)
        {
            if (RequiredObjects.HasValue())
                foreach (var requiredObjectProto in RequiredObjects)
                    requiredObjectProto?.GetContainedEntities(entities);
        }

    }

    public class BoxFormationTypePrototype : FormationTypePrototype
    {
    }

    public class LineRowInfoPrototype : Prototype
    {
        public int Num { get; protected set; }
        public FormationFacing Facing { get; protected set; }
    }

    public class LineFormationTypePrototype : FormationTypePrototype
    {
        public LineRowInfoPrototype[] Rows { get; protected set; }
    }

    public class ArcFormationTypePrototype : FormationTypePrototype
    {
        public int ArcDegrees { get; protected set; }

        [DoNotCopy]
        public float ArcRadians { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            ArcRadians = MathHelper.ToRadians(ArcDegrees);
        }
    }

    public class FormationSlotPrototype : FormationTypePrototype
    {
        public float X { get; protected set; }
        public float Y { get; protected set; }
        public float Yaw { get; protected set; }
    }

    public class FixedFormationTypePrototype : FormationTypePrototype
    {
        public FormationSlotPrototype[] Slots { get; protected set; }
    }

    public class CleanUpPolicyPrototype : Prototype
    {
    }

    public class EntityCountEntryPrototype : Prototype
    {
        public PrototypeId Entity { get; protected set; }
        public int Count { get; protected set; }
    }

    public class PopulationListTagObjectPrototype : Prototype
    {
    }

    public class PopulationListTagEncounterPrototype : Prototype
    {
    }

    public class PopulationListTagThemePrototype : Prototype
    {
    }
}
