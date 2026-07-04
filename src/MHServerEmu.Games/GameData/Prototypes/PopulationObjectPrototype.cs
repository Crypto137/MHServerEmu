using System.Text;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.GameData.Calligraphy;
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

        //---

        [DoNotCopy]
        public int PopulationObjectPrototypeEnumValue { get; private set; }

        public override void PostProcess()
        {
            base.PostProcess();

            PopulationObjectPrototypeEnumValue = GetEnumValueFromBlueprint(LiveTuningData.GetPopulationObjectBlueprintDataRef());
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

        public virtual void GetContainedEntities(HashSet<PrototypeId> entities, bool unwrapEntitySelectors = false)
        {
            if (Riders.HasValue())
            {
                foreach (PopulationRiderPrototype rider in Riders)
                {
                    if (rider is PopulationRiderEntityPrototype riderEntityProto && riderEntityProto.Entity != PrototypeId.Invalid)
                        entities.Add(riderEntityProto.Entity);
                }
            }
        }

        public FormationTypePrototype GetFormation()
        {
            if (Formation != null)
                return Formation;
            else
                return GameDatabase.GetPrototype<FormationTypePrototype>(FormationTemplate);
        }

        public virtual void BuildCluster(ClusterGroup group, ClusterObjectFlag flags)
        {
            if (Riders.HasValue() && flags.HasFlag(ClusterObjectFlag.Henchmen) == false)
            {
                foreach (PopulationRiderPrototype rider in Riders)
                {
                    if (rider is not PopulationRiderEntityPrototype riderEntityProto)
                        continue;

                    ClusterEntity clusterEntity = group.CreateClusterEntity(riderEntityProto.Entity);
                    if (!Verify.IsNotNull(clusterEntity)) return;

                    clusterEntity.Flags |= flags;
                    clusterEntity.Flags |= ClusterObjectFlag.SkipFormation;
                }
            }
        }

        public virtual float GetAverageSize()
        {
            return 0.0f;
        }

        protected static int UnwrapEntitySelector(PrototypeId selectorRef, HashSet<PrototypeId> entities)
        {
            int count = 0;

            EntitySelectorPrototype selectorProto = selectorRef.As<EntitySelectorPrototype>();
            if (selectorProto != null && selectorProto.Entities.HasValue())
            {
                foreach (PrototypeId entity in selectorProto.Entities)
                {
                    entities.Add(entity);
                    count++;
                }
            }

            return count;
        }
    }

    public class PopulationEntityPrototype : PopulationObjectPrototype
    {
        public PrototypeId Entity { get; protected set; }

        //---

        public override void GetContainedEntities(HashSet<PrototypeId> entities, bool unwrapEntitySelectors = false)
        {
            base.GetContainedEntities(entities, unwrapEntitySelectors);

            if (Entity != PrototypeId.Invalid)
            {
                if (unwrapEntitySelectors == false || UnwrapEntitySelector(Entity, entities) == 0)
                    entities.Add(Entity);
            }
        }

        public override void BuildCluster(ClusterGroup group, ClusterObjectFlag flags)
        {
            ClusterEntity clusterEntity = group.CreateClusterEntity(Entity);
            if (!Verify.IsNotNull(clusterEntity)) return;

            clusterEntity.Flags |= flags;

            base.BuildCluster(group, flags);
        }

        public override float GetAverageSize()
        {
            return 1f;
        }
    }

    public class PopulationClusterFixedPrototype : PopulationObjectPrototype
    {
        public PrototypeId[] Entities { get; protected set; }
        public EntityCountEntryPrototype[] EntityEntries { get; protected set; }

        //---

        public override void GetContainedEntities(HashSet<PrototypeId> entities, bool unwrapEntitySelectors = false)
        {
            base.GetContainedEntities(entities, unwrapEntitySelectors);
            InternalGetContainedEntities(entities, unwrapEntitySelectors);
        }

        public override void BuildCluster(ClusterGroup group, ClusterObjectFlag flags)
        {
            if (Entities.HasValue())
            {
                foreach (PrototypeId entityRef in Entities)
                {
                    ClusterEntity clusterEntity = group.CreateClusterEntity(entityRef);
                    if (!Verify.IsNotNull(clusterEntity))
                        continue;

                    clusterEntity.Flags |= flags;
                }
            }

            if (EntityEntries.HasValue())
            {
                foreach (EntityCountEntryPrototype entryProto in EntityEntries)
                {
                    if (!Verify.IsNotNull(entryProto))
                        continue;

                    for (int i = 0; i < entryProto.Count; i++)
                    {
                        ClusterEntity clusterEntity = group.CreateClusterEntity(entryProto.Entity);
                        if (!Verify.IsNotNull(clusterEntity))
                            continue;

                        clusterEntity.Flags |= flags;
                    }
                }
            }

            base.BuildCluster(group, flags);
        }

        public override float GetAverageSize() 
        {
            float count = 0.0f;

            if (Entities.HasValue())
            {
                count += Entities.Length;
            }
            else if (EntityEntries.HasValue())
            {
                foreach (EntityCountEntryPrototype entryProto in EntityEntries)
                    count += entryProto.Count;
            }

            return count;
        }

        private void InternalGetContainedEntities(HashSet<PrototypeId> entities, bool unwrapEntitySelectors)
        {
            if (Entities.HasValue())
            {
                foreach (PrototypeId entityRef in Entities)
                {
                    if (entityRef == PrototypeId.Invalid)
                        continue;

                    if (unwrapEntitySelectors == false || UnwrapEntitySelector(entityRef, entities) == 0)
                        entities.Add(entityRef);
                }
            }

            if (EntityEntries.HasValue())
            {
                foreach (EntityCountEntryPrototype entryProto in EntityEntries)
                {
                    if (!Verify.IsNotNull(entryProto))
                        continue;

                    PrototypeId entityRef = entryProto.Entity;
                    if (!Verify.IsTrue(entityRef != PrototypeId.Invalid))
                        continue;

                    if (unwrapEntitySelectors == false || UnwrapEntitySelector(entityRef, entities) == 0)
                        entities.Add(entityRef);
                }
            }
        }
    }

    public class PopulationClusterPrototype : PopulationObjectPrototype
    {
        public short Max { get; protected set; }
        public short Min { get; protected set; }
        public float RandomOffset { get; protected set; }
        public PrototypeId Entity { get; protected set; }

        //---

        public override void GetContainedEntities(HashSet<PrototypeId> entities, bool unwrapEntitySelectors = false)
        {
            base.GetContainedEntities(entities, unwrapEntitySelectors);

            if (Entity != PrototypeId.Invalid)
            {
                if (unwrapEntitySelectors == false || UnwrapEntitySelector(Entity, entities) == 0)
                    entities.Add(Entity);
            }
        }

        public override void BuildCluster(ClusterGroup group, ClusterObjectFlag flags)
        {
            if (Entity != PrototypeId.Invalid)
            {
                int numEntities = group.Random.Next(Min, Max + 1);
                if (!Verify.IsTrue(numEntities > 0)) return;

                for (int i = 0; i < numEntities; i++)
                {
                    ClusterEntity clusterEntity = group.CreateClusterEntity(Entity);
                    if (!Verify.IsNotNull(clusterEntity))
                        continue;

                    clusterEntity.Flags |= flags;
                }
            }

            base.BuildCluster(group, flags);
        }

        public override float GetAverageSize()
        {
            return (Min + Max) / 2.0f;
        }
    }

    public class PopulationClusterMixedPrototype : PopulationObjectPrototype
    {
        public short Max { get; protected set; }
        public short Min { get; protected set; }
        public float RandomOffset { get; protected set; }
        public PopulationObjectPrototype[] Choices { get; protected set; }

        //---

        public override void GetContainedEntities(HashSet<PrototypeId> entities, bool unwrapEntitySelectors = false)
        {
            base.GetContainedEntities(entities, unwrapEntitySelectors);
            InternalGetContainedEntities(entities, unwrapEntitySelectors);
        }

        public override void BuildCluster(ClusterGroup group, ClusterObjectFlag flags)
        {
            // NOTE: The client-side implementation for this in 1.52 and below appears to be incomplete, so this is based on 1.53.
            if (!Verify.IsTrue(Choices.HasValue())) return;

            int numEntities = group.Random.Next(Min, Max + 1);

            // V53_TODO: Unique flag support (use PickRemove instead of Pick and validate available choice count to be <= rolled number).
            Picker<PopulationObjectPrototype> picker = new(group.Random);
            foreach (PopulationObjectPrototype objectProto in Choices)
                picker.Add(objectProto);

            if (!Verify.IsTrue(picker.Empty() == false)) return;

            for (int i = 0; i < numEntities; i++)
            {
                if (picker.Pick(out PopulationObjectPrototype objectProto))
                {
                    PopulationEntityPrototype choiceEntity = objectProto as PopulationEntityPrototype;
                    if (!Verify.IsNotNull(choiceEntity))    // If this verify fires, we probably need to add PopulationGroupPrototype support here.
                        continue;

                    ClusterEntity clusterEntity = group.CreateClusterEntity(choiceEntity.Entity);
                    if (!Verify.IsNotNull(clusterEntity))
                        continue;
                    
                    clusterEntity.Flags |= flags;
                }
            }

            base.BuildCluster(group, flags);
        }

        public override float GetAverageSize()
        {
            return (Min + Max) / 2.0f;
        }

        private void InternalGetContainedEntities(HashSet<PrototypeId> entities, bool unwrapEntitySelectors)
        {
            if (Choices.HasValue())
            {
                foreach (PopulationObjectPrototype objectProto in Choices)
                {
                    if (!Verify.IsNotNull(objectProto))
                        continue;

                    objectProto.GetContainedEntities(entities, unwrapEntitySelectors);
                }
            }
        }
    }

    public class PopulationLeaderPrototype : PopulationObjectPrototype
    {
        public PrototypeId Leader { get; protected set; }
        public PopulationObjectPrototype[] Henchmen { get; protected set; }

        //---

        public override void GetContainedEntities(HashSet<PrototypeId> entities, bool unwrapEntitySelectors = false)
        {
            base.GetContainedEntities(entities, unwrapEntitySelectors);
            InternalGetContainedEntities(entities, unwrapEntitySelectors);
        }

        public override void BuildCluster(ClusterGroup group, ClusterObjectFlag flags)
        {
            if (Leader != PrototypeId.Invalid)
            {
                ClusterEntity clusterEntity = group.CreateClusterEntity(Leader);
                if (Verify.IsNotNull(clusterEntity))
                {
                    clusterEntity.Flags |= flags;
                    clusterEntity.Flags |= ClusterObjectFlag.Leader;
                }
            }

            if (Henchmen.HasValue())
            {
                Picker<PopulationObjectPrototype> picker = new(group.Random);
                foreach (PopulationObjectPrototype objectProto in Henchmen)
                    picker.Add(objectProto);

                if (Verify.IsTrue(picker.Pick(out PopulationObjectPrototype henchmenEntry))) 
                    henchmenEntry.BuildCluster(group, ClusterObjectFlag.Henchmen);
            }

            base.BuildCluster(group, flags);
        }

        public override float GetAverageSize()
        {
            float count = 0.0f;

            if (Henchmen.HasValue())
            {
                foreach (PopulationObjectPrototype objectProto in Henchmen)
                {
                    if (objectProto != null)
                        count += objectProto.GetAverageSize();
                }

                count /= Henchmen.Length;
            }

            return count + 1.0f;
        }

        private void InternalGetContainedEntities(HashSet<PrototypeId> entities, bool unwrapEntitySelectors)
        {
            if (Leader != PrototypeId.Invalid)
            {
                if (unwrapEntitySelectors == false || UnwrapEntitySelector(Leader, entities) == 0)
                    entities.Add(Leader);
            }

            if (Henchmen.HasValue())
            {
                foreach (PopulationObjectPrototype objectProto in Henchmen)
                {
                    if (Verify.IsNotNull(objectProto))
                        continue;

                    objectProto.GetContainedEntities(entities, unwrapEntitySelectors);
                }
            }
        }
    }

    public class PopulationEncounterPrototype : PopulationObjectPrototype
    {
        public AssetId EncounterResource { get; protected set; }

        //---

        public override void GetContainedEntities(HashSet<PrototypeId> entities, bool unwrapEntitySelectors = false)
        {
            base.GetContainedEntities(entities, unwrapEntitySelectors);
            InternalGetContainedEntities(entities, unwrapEntitySelectors);
        }

        public override void BuildCluster(ClusterGroup group, ClusterObjectFlag flags)
        {
            PrototypeId encounterResourceRef = GetEncounterRef();
            if (!Verify.IsTrue(encounterResourceRef != PrototypeId.Invalid)) return;
            
            EncounterResourcePrototype encounterResourceProto = GetEncounterResource();
            if (!Verify.IsNotNull(encounterResourceProto)) return;
                
            MarkerSetPrototype markerSetProto = encounterResourceProto.MarkerSet;
            if (!Verify.IsNotNull(markerSetProto)) return;
                    
            group.Flags |= ClusterObjectFlag.SkipFormation;
            group.Properties[PropertyEnum.EncounterResource] = encounterResourceRef;

            foreach (MarkerPrototype abstractMarker in markerSetProto.Markers)
            {
                EntityMarkerPrototype markerProto = abstractMarker as EntityMarkerPrototype;
                if (!Verify.IsNotNull(markerProto))
                    continue;

                if (!Verify.IsTrue(markerProto.EntityGuid != PrototypeGuid.Invalid, $"Marker at in Cell:\n  {this}\nand position:\n  {markerProto.Position}\nhas invalid GUID"))
                    continue;

                PrototypeId markerRef = GameDatabase.GetDataRefByPrototypeGuid(markerProto.EntityGuid);
                if (!Verify.IsTrue(markerRef != PrototypeId.Invalid, $"Marker at in Cell:\n  {this}\nand position:\n  {markerProto.Position}\nhas invalid Ref, GUID was valid, so likely prototype ref was deleted from calligraphy:\n  {markerProto.LastKnownEntityName}"))
                    continue;
                
                Prototype proto = GameDatabase.GetPrototype<Prototype>(markerRef);

                if (proto is WorldEntityPrototype)
                {
                    ClusterEntity clusterEntity = group.CreateClusterEntity(markerRef);
                    if (Verify.IsNotNull(clusterEntity))
                    {
                        clusterEntity.Flags |= flags;
                        clusterEntity.SetParentRelativePosition(markerProto.Position);
                        clusterEntity.SetParentRelativeOrientation(markerProto.Rotation);
                        clusterEntity.SnapToFloor = SpawnSpec.SnapToFloorConvert(markerProto.OverrideSnapToFloor, markerProto.OverrideSnapToFloorValue);
                        clusterEntity.EncounterSpawnPhase = markerProto.EncounterSpawnPhase;
                        clusterEntity.Flags |= ClusterObjectFlag.SkipFormation;
                    }
                }

                if (proto is BlackOutZonePrototype)
                {
                    Verify.IsTrue(group.BlackOutZone.Key == PrototypeId.Invalid);
                    group.BlackOutZone = new(markerRef, markerProto.Position);
                }
            }

            base.BuildCluster(group, flags);            
        }

        public override float GetAverageSize()
        {
            return 1.0f;
        }

        public PrototypeId GetEncounterRef()
        {
            if (!Verify.IsTrue(EncounterResource != AssetId.Invalid, $"PopulationEncounter {this} has no value in its EncounterResource field."))
                return PrototypeId.Invalid;

            PrototypeId encounterProtoRef = GameDatabase.GetDataRefByAsset(EncounterResource);
            if (!Verify.IsTrue(encounterProtoRef != PrototypeId.Invalid, $"PopulationEncounter {this} was unable to find resource for asset {EncounterResource.GetName()}, check file path and verify file exists."))
                return PrototypeId.Invalid;

            return encounterProtoRef;
        }

        private EncounterResourcePrototype GetEncounterResource()
        {
            PrototypeId encounterProtoRef = GetEncounterRef();
            if (!Verify.IsTrue(encounterProtoRef != PrototypeId.Invalid)) return null;

            EncounterResourcePrototype encounter = encounterProtoRef.As<EncounterResourcePrototype>();
            if (!Verify.IsNotNull(encounter)) return null;

            return encounter;
        }

        public bool HasClientData()
        {
            EncounterResourcePrototype encounter = GetEncounterResource();
            return encounter != null && (encounter.HasEdges || encounter.ClientMap != null);
        }

        private void InternalGetContainedEntities(HashSet<PrototypeId> entities, bool unwrapEntitySelectors)
        {
            EncounterResourcePrototype resourceProto = GetEncounterResource();
            resourceProto?.MarkerSet?.GetContainedEntities(entities);
        }
    }

    public class PopulationFormationPrototype : PopulationObjectPrototype
    {
        public PopulationRequiredObjectPrototype[] Objects { get; protected set; }

        //---

        public override void BuildCluster(ClusterGroup group, ClusterObjectFlag flags)
        {
            if (Objects.HasValue())
            {
                foreach (PopulationRequiredObjectPrototype requiredObjectProto in Objects)
                {
                    if (!Verify.IsNotNull(requiredObjectProto))
                        continue;

                    Verify.IsTrue(requiredObjectProto.EvalSpawnProperties == null, "Unsupported");

                    for (int i = 0; i < requiredObjectProto.Count; i++)
                    {
                        PopulationObjectPrototype objectProto = requiredObjectProto.GetPopObject();
                        if (!Verify.IsNotNull(objectProto))
                            continue;

                        ClusterGroup newGroup = group.CreateClusterGroup(objectProto);
                        if (!Verify.IsNotNull(newGroup)) return;

                        newGroup.Flags |= flags;
                    }
                }
            }

            base.BuildCluster(group, flags);
        }
    }

    public class PopulationGroupPrototype : PopulationObjectPrototype
    {
        public PopulationObjectPrototype[] EntitiesAndGroups { get; protected set; }

        //---

        public override void GetContainedEntities(HashSet<PrototypeId> entities, bool unwrapEntitySelectors = false)
        {
            if (EntitiesAndGroups.HasValue())
            {
                foreach (PopulationObjectPrototype objectProto in EntitiesAndGroups)
                {
                    if (!Verify.IsNotNull(objectProto))
                        continue;

                    objectProto.GetContainedEntities(entities, unwrapEntitySelectors);
                }
            }
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

        //---

        public PopulationObjectPrototype GetPopObject()
        {
            if (Object != null)
                return Object;
            else
                return GameDatabase.GetPrototype<PopulationObjectPrototype>(ObjectTemplate);
        }

        public virtual void GetContainedEntities(HashSet<PrototypeId> refs) 
        {
            PopulationObjectPrototype objectProto = GetPopObject();
            if (!Verify.IsNotNull(objectProto))
                return;

            objectProto.GetContainedEntities(refs);
        }

        public void EvaluateSpawnProperties(PropertyCollection properties, Region region, MetaGame metaGame)
        {
            if (properties == null)
                return;

            if (RankOverride != PrototypeId.Invalid)
            {
                PrototypeId rankRef = RankPrototype.DoOverride(properties[PropertyEnum.Rank], RankOverride);
                if (rankRef != PrototypeId.Invalid)
                    properties[PropertyEnum.Rank] = rankRef;
            }

            if (EvalSpawnProperties != null)
            {
                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                evalContext.Game = region?.Game;
                evalContext.SetVar_PropertyCollectionPtr(EvalContext.Default, properties);
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, region.Properties);
                evalContext.SetReadOnlyVar_EntityPtr(EvalContext.Entity, metaGame);
                Eval.RunBool(EvalSpawnProperties, evalContext);
            }
        }

        public bool AllowedInDifficulty(PrototypeId difficultyRef)
        {
            return DifficultyTierPrototype.InRange(difficultyRef, RestrictToDifficultyMin, RestrictToDifficultyMax);
        }
    }

    public class PopulationRequiredObjectListPrototype : Prototype
    {
        public PopulationRequiredObjectPrototype[] RequiredObjects { get; protected set; }

        //---

        public virtual void GetContainedEntities(HashSet<PrototypeId> entities)
        {
            if (RequiredObjects.HasValue())
            {
                foreach (PopulationRequiredObjectPrototype objectProto in RequiredObjects)
                {
                    if (!Verify.IsNotNull(objectProto))
                        continue;

                    objectProto.GetContainedEntities(entities);
                }
            }
        }
    }

    public class FormationTypePrototype : Prototype
    {
        public FormationFacing Facing { get; protected set; }
        public float Spacing { get; protected set; }
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

        //---

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
