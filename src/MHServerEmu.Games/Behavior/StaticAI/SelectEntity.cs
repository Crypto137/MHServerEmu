using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class SelectEntity
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static WorldEntity DoSelectEntity(in SelectEntityContext selectionContext, CombatTargetFlags flags = CombatTargetFlags.None)
        {
            if (selectionContext.OwnerController == null) return null;
            var ownerAgent = selectionContext.OwnerController.Owner;
            if (ownerAgent == null) return null;
            var ownersGame = ownerAgent.Game;
            if (ownersGame == null) return null;
            var manager = ownersGame.EntityManager;
            var blackboard = selectionContext.OwnerController.Blackboard;

            var taunterId = ownerAgent.Properties[PropertyEnum.TauntersID];
            if (taunterId != Entity.InvalidId)
            {
                var taunter = manager.GetEntity<WorldEntity>(taunterId);
                if (taunter != null && taunter.IsDead == false && taunter.IsInWorld)
                {
                    selectionContext.OwnerController.SetTargetEntity(taunter);
                    return taunter;
                }
            }

            var pendingTargetId = blackboard.PropertyCollection[PropertyEnum.AIPendingTargetId];
            if (pendingTargetId != Entity.InvalidId)
            {
                var pendingTarget = manager.GetEntity<WorldEntity>(pendingTargetId);
                if (pendingTarget != null && pendingTarget.IsDead == false && pendingTarget.IsInWorld)
                {
                    selectionContext.OwnerController.SetTargetEntity(pendingTarget);
                    return pendingTarget;
                }
            }

            if (selectionContext.SelectionType == SelectEntityType.SelectTargetByAssistedEntitiesLastTarget)
            {
                var assistedEntity = selectionContext.OwnerController.AssistedEntity;
                if (assistedEntity != null && assistedEntity.IsDead == false && assistedEntity.IsInWorld)
                {
                    var lastHostile = manager.GetEntity<WorldEntity>(assistedEntity.Properties[PropertyEnum.LastHostileTargetID]);
                    if (lastHostile != null && lastHostile.IsDead == false && lastHostile.IsInWorld)
                    {
                        selectionContext.OwnerController.SetTargetEntity(lastHostile);
                        return lastHostile;
                    }
                    return null;
                }
                else
                    return null;
            }

            if (selectionContext.SelectionMethod == SelectEntityMethodType.Self)
                return ownerAgent;

            WorldEntity bestTargetSoFar = null;
            float bestValue = -1f;
            switch (selectionContext.PoolType)
            {
                case SelectEntityPoolType.AllEntitiesInRegionOfAgent:
                    
                    var entityRegion = ownerAgent.Region;
                    if (entityRegion == null) return null;
                    var regionBound = entityRegion.Bound;
                    if (Segment.IsNearZero(selectionContext.CellOrRegionAABBScale) == false)
                    {
                        regionBound = new Aabb(regionBound.Center,
                            regionBound.Width * selectionContext.CellOrRegionAABBScale,
                            regionBound.Length * selectionContext.CellOrRegionAABBScale,
                            regionBound.Height);
                    }
                    if (LoopHelper(selectionContext, regionBound, ref bestTargetSoFar, ref bestValue) == false) 
                        return null;

                    break;                    

                case SelectEntityPoolType.AllEntitiesInCellOfAgent:
                    
                    var entityCell = ownerAgent.Cell;
                    if (entityCell == null) return null;
                    var cellBound = entityCell.RegionBounds;
                    if (Segment.IsNearZero(selectionContext.CellOrRegionAABBScale) == false)
                    {
                        cellBound = new Aabb(cellBound.Center,
                            cellBound.Width * selectionContext.CellOrRegionAABBScale,
                            cellBound.Length * selectionContext.CellOrRegionAABBScale,
                            cellBound.Height);
                    }
                    if (LoopHelper(selectionContext, cellBound, ref bestTargetSoFar, ref bestValue) == false) 
                        return null;

                    break;                    

                case SelectEntityPoolType.PotentialAlliesOfAgent:
                case SelectEntityPoolType.PotentialEnemiesOfAgent:
                    
                    if (LoopHelper(selectionContext, ref bestTargetSoFar, ref bestValue, flags) == false) 
                        return null;

                    break;                    

                default:                    
                    Logger.Warn($"Invalid Pool Type in ActionSelectEntity, for {ownerAgent}");
                    return null;                    
            }

            return bestTargetSoFar;
        }

        private static bool LoopHelper(in SelectEntityContext selectionContext, ref WorldEntity bestTargetSoFar, ref float bestValue, CombatTargetFlags flags)
        {
            if (selectionContext.OwnerController == null) return false;
            Game ownersGame = selectionContext.OwnerController.Game;
            if (ownersGame == null) return false;
            Agent ownerAgent = selectionContext.OwnerController.Owner;
            if (ownerAgent == null) return false;
            AIController ownerController = ownerAgent.AIController;
            if (ownerController == null) return false;
            BehaviorSensorySystem senses = ownerController.Senses;
            var manager = ownersGame.EntityManager;

            List<ulong> targets = null;
            CombatTargetType targetType = CombatTargetType.Hostile;

            if (senses.ShouldSenseEntitiesOfPoolType(selectionContext.PoolType))
            {
                switch (selectionContext.PoolType)
                {
                    case SelectEntityPoolType.PotentialEnemiesOfAgent:
                        senses.SensePotentialHostileTargets(selectionContext, ref bestTargetSoFar, ref bestValue, flags);
                        targets = senses.PotentialHostileTargetIds;
                        targetType = CombatTargetType.Hostile;
                        break;
                    case SelectEntityPoolType.PotentialAlliesOfAgent:
                        senses.SensePotentialAllyTargets(selectionContext, ref bestTargetSoFar, ref bestValue, flags);
                        targets = senses.PotentialAllyTargetIds;
                        targetType = CombatTargetType.Ally;
                        break;
                }
            }
            else
            {
                switch (selectionContext.PoolType)
                {
                    case SelectEntityPoolType.PotentialEnemiesOfAgent:
                        targets = senses.PotentialHostileTargetIds;
                        targetType = CombatTargetType.Hostile;
                        break;
                    case SelectEntityPoolType.PotentialAlliesOfAgent:
                        targets = senses.PotentialAllyTargetIds;
                        targetType = CombatTargetType.Ally;
                        break;
                }

                if (targets == null) return false;
                if (selectionContext.SelectionMethod != SelectEntityMethodType.RandomEntity || targets.Count == 1)
                    foreach (var entityId in targets)
                    {
                        WorldEntity itEntity = manager.GetEntity<WorldEntity>(entityId);
                        if (itEntity == null) continue;
                        if (LoopHelperCommon(ownersGame, ownerAgent, selectionContext, itEntity, ref bestTargetSoFar, ref bestValue, targetType, flags))
                            break;
                    }
            }

            if (selectionContext.SelectionMethod == SelectEntityMethodType.RandomEntity)
            {
                if (targets != null && targets.Count > 1)
                {
                    bestTargetSoFar = null;
                    Picker<WorldEntity> picker = new (ownersGame.Random);
                    foreach (var entityId in targets)
                    {
                        WorldEntity itEntity = manager.GetEntity<WorldEntity>(entityId);
                        if (itEntity != null)
                            picker.Add(itEntity);
                    }
                    while (picker.Empty() == false)
                    {
                        picker.PickRemove(out WorldEntity randomEntity);
                        LoopHelperCommon(ownersGame, ownerAgent, selectionContext, randomEntity, ref bestTargetSoFar, ref bestValue, targetType, flags);
                        if (bestTargetSoFar != null) break;
                    }
                }
            }

            return true;
        }

        private static bool LoopHelperCommon(Game ownersGame, Agent ownerAgent, in SelectEntityContext selectionContext, WorldEntity entity, 
            ref WorldEntity bestTargetSoFar, ref float bestValue, CombatTargetType targetType, CombatTargetFlags flags)
        {
            if (Combat.ValidTarget(ownersGame, ownerAgent, entity, targetType, false, flags))
                if (EntityMatchesSelectionCriteria(selectionContext, entity, ref bestTargetSoFar, ref bestValue))
                    return true;

            return false;
        }

        private static bool LoopHelper(in SelectEntityContext selectionContext, in Aabb bounds, ref WorldEntity bestTargetSoFar, ref float bestValue)
        {
            if (selectionContext.OwnerController == null) return false;
            var ownersGame = selectionContext.OwnerController.Game;
            if (ownersGame == null) return false;
            var ownerAgent = selectionContext.OwnerController.Owner;
            if (ownerAgent == null) return false;
            var region = ownerAgent.Region;
            if (region == null) return false;

            var spFlags = EntityRegionSPContextFlags.ActivePartition;
            if (selectionContext.StaticEntities)
                spFlags |= EntityRegionSPContextFlags.StaticPartition;
            var spatialPartitionContext = new EntityRegionSPContext(spFlags);

            if (selectionContext.SelectionMethod == SelectEntityMethodType.RandomEntity)
            {
                Picker<WorldEntity> picker = new (ownersGame.Random);
                foreach (var itEntity in region.IterateEntitiesInVolume(bounds, spatialPartitionContext))
                {
                    if (itEntity == null) continue;
                    picker.Add(itEntity);
                }

                while (picker.Empty() == false)
                {
                    picker.PickRemove(out var randomEntity);
                    if (randomEntity == null) return false;
                    if (EntityMatchesSelectionCriteria(in selectionContext, randomEntity, ref bestTargetSoFar, ref bestValue))
                        return true;
                }
            }
            else
            {
                foreach (var itEntity in region.IterateEntitiesInVolume(bounds, spatialPartitionContext))
                {
                    if (itEntity == null) continue;
                    if (EntityMatchesSelectionCriteria(in selectionContext, itEntity, ref bestTargetSoFar, ref bestValue))
                        break;
                }
            }

            return true;
        }

        public static bool EntityMatchesSelectionCriteria(in SelectEntityContext selectionContext, WorldEntity entity, ref WorldEntity bestTargetSoFar, ref float bestValue)
        {
            if (CheckAttributes(in selectionContext, entity) 
                && HandleTargetingMethod(in selectionContext, ref bestTargetSoFar, ref bestValue, entity))
                return true;

            return false;
        }

        private static bool HandleTargetingMethod(in SelectEntityContext selectionContext, ref WorldEntity bestTargetSoFar, ref float bestValue, WorldEntity entity)
        {
            if (ShouldDoAlliancePriorityCheck(selectionContext, bestTargetSoFar)
                && HasAlliancePriority(selectionContext, bestTargetSoFar) 
                && HasAlliancePriority(selectionContext, entity) == false)
                return false;

            switch (selectionContext.SelectionMethod)
            {
                case SelectEntityMethodType.FirstFound:
                    return SelectEntityFirstFoundOrRandomEntity(selectionContext, ref bestTargetSoFar, entity);

                case SelectEntityMethodType.RandomEntity:
                    SelectEntityFirstFoundOrRandomEntity(selectionContext, ref bestTargetSoFar, entity);
                    return false;

                case SelectEntityMethodType.MostDamageInTimeInterval:
                    return SelectEntityByMostDamageInTimeInterval(selectionContext, ref bestTargetSoFar, ref bestValue, entity);

                case SelectEntityMethodType.ClosestEntity:
                case SelectEntityMethodType.FarthestEntity:
                    SelectEntityByDistance(selectionContext, ref bestTargetSoFar, ref bestValue, entity);
                    return false;

                case SelectEntityMethodType.HighestValueOfProperty:
                case SelectEntityMethodType.LowestValueOfProperty:
                    SelectEntityByProperty(selectionContext, ref bestTargetSoFar, ref bestValue, entity);
                    return false;

                default:
                    if (selectionContext.OwnerController == null) return false;
                    var ownerAsAgent = selectionContext.OwnerController.Owner;
                    if (ownerAsAgent == null) return false;
                    Logger.Warn($"Invalid Targeting Selection Method in HandleTargetingMethod for Agent {ownerAsAgent}");
                    return false;
            }
        }

        private static void SelectEntityByProperty(in SelectEntityContext selectionContext, ref WorldEntity bestTargetSoFar, ref float bestValue, WorldEntity entity)
        {
            if (selectionContext.OwnerController == null) return;         
            if (PotentialTargetWithinDistanceThresholds(selectionContext, entity) == false) return;
            Agent ownerAsAgent = selectionContext.OwnerController.Owner;
            if (ownerAsAgent == null) return;

            float value;
            PropertyId comparisonProp = new (selectionContext.ComparisonEnum);

            switch (selectionContext.ComparisonDataType)
            {
                case PropertyDataType.Integer:
                    value = entity.Properties[comparisonProp];
                    break;

                case PropertyDataType.Curve:
                case PropertyDataType.Real:
                    value = entity.Properties[comparisonProp];
                    break;

                default:
                    Logger.Warn($"ActionSelectEntity.SelectEntityByProperty(): found a property match but there is no valid value that is acceptable for this function {ownerAsAgent}");
                    return;
            }

            if (bestTargetSoFar == null)
            {
                bestTargetSoFar = entity;
                bestValue = value;
                return;
            }

            bool foundBestValue = false;
            switch (selectionContext.SelectionMethod)
            {
                case SelectEntityMethodType.HighestValueOfProperty:
                    if (value > bestValue)
                        foundBestValue = true;
                    break;

                case SelectEntityMethodType.LowestValueOfProperty:
                    if (value < bestValue)
                        foundBestValue = true;
                    break;

                default:
                    Logger.Warn("Cannot call this function with a non-health related selection method!");
                    return;
            }

            bool targetSwitch = false;
            if (ShouldDoAlliancePriorityCheck(selectionContext, bestTargetSoFar))
                targetSwitch = ShouldDoAlliancePriorityTargetSwitch(selectionContext, entity, bestTargetSoFar);

            if (foundBestValue || targetSwitch)
            {
                bestTargetSoFar = entity;
                bestValue = value;
            }
        }

        private static bool ShouldDoAlliancePriorityTargetSwitch(in SelectEntityContext selectionContext, WorldEntity entity, WorldEntity bestTargetSoFar)
        {
            if (HasAlliancePriority(selectionContext, entity) && HasAlliancePriority(selectionContext, bestTargetSoFar) == false)
                return true;
            return false;
        }

        private static bool PotentialTargetWithinDistanceThresholds(in SelectEntityContext selectionContext, WorldEntity entity, float distanceTo = -1.0f)
        {
            if (entity.IsInWorld == false) return false;

            if (selectionContext.MaxDistanceThreshold > 0f)
            {
                if (selectionContext.OwnerController == null) return false;
                Agent owningAgent = selectionContext.OwnerController.Owner;
                if (owningAgent == null) return false;

                float distance = distanceTo;
                if (distance < 0f)
                    distance = owningAgent.GetDistanceTo(entity, true);

                return distance >= selectionContext.MinDistanceThreshold && distance <= selectionContext.MaxDistanceThreshold;
            }

            return true;
        }

        private static void SelectEntityByDistance(in SelectEntityContext selectionContext, ref WorldEntity bestTargetSoFar, ref float bestValue, WorldEntity entity)
        {
            if (selectionContext.OwnerController == null) return;
            Agent ownerAsAgent = selectionContext.OwnerController.Owner;
            if (ownerAsAgent == null) return;

            float distanceTo = ownerAsAgent.GetDistanceTo(entity, true);
            if (PotentialTargetWithinDistanceThresholds(selectionContext, entity, distanceTo) == false) return;

            bool foundBestValue = false;
            if (bestValue < 0)
                foundBestValue = true;
            else if (selectionContext.SelectionMethod == SelectEntityMethodType.FarthestEntity)
            {
                if (distanceTo > bestValue)
                    foundBestValue = true;
            }
            else if (selectionContext.SelectionMethod == SelectEntityMethodType.ClosestEntity)
            {
                if (distanceTo < bestValue)
                    foundBestValue = true;
            }

            bool targetSwitch = false;
            if (ShouldDoAlliancePriorityCheck(selectionContext, bestTargetSoFar))
                targetSwitch = ShouldDoAlliancePriorityTargetSwitch(selectionContext, entity, bestTargetSoFar);

            if (foundBestValue || targetSwitch)
            {
                bestValue = distanceTo;
                bestTargetSoFar = entity;
            }
        }

        private static bool SelectEntityByMostDamageInTimeInterval(in SelectEntityContext selectionContext, ref WorldEntity bestTargetSoFar, ref float bestValue, WorldEntity entity)
        {
            if (selectionContext.OwnerController == null) return false;
            if (PotentialTargetWithinDistanceThresholds(selectionContext, entity) == false) return false;

            Agent ownerAsAgent = selectionContext.OwnerController.Owner;
            BehaviorBlackboard blackboard = selectionContext.OwnerController.Blackboard;

            if (blackboard.PropertyCollection.HasProperty(PropertyEnum.AITrackIncomingDamage) == false)
            {
                Logger.Warn($"Trying to use SelectEntityByMostDamage when this AI doesn't have the property to track it: {ownerAsAgent}");
                return false;
            }

            if (ShouldDoAlliancePriorityCheck(selectionContext, bestTargetSoFar) 
                && ShouldDoAlliancePriorityTargetSwitch(selectionContext, entity, bestTargetSoFar) == false)
                return false;

            var damageMap = blackboard.DamageMap;
            if (damageMap == null)
            {
                bestTargetSoFar = entity;
                return true;
            }

            float damage = damageMap.ContainsKey(entity.Id) ? (float)damageMap[entity.Id] : 0f;

            if (bestValue < 0)
            {
                bestValue = damage;
                bestTargetSoFar = entity;
            }
            else if (damage > bestValue)
            {
                bestValue = damage;
                bestTargetSoFar = entity;
            }

            return false;
        }

        private static bool SelectEntityFirstFoundOrRandomEntity(in SelectEntityContext selectionContext, ref WorldEntity bestTargetSoFar, WorldEntity entity)
        {
            if (PotentialTargetWithinDistanceThresholds(selectionContext, entity) == false)
                return false;

            if (ShouldDoAlliancePriorityCheck(selectionContext, bestTargetSoFar) 
                && ShouldDoAlliancePriorityTargetSwitch(selectionContext, entity, bestTargetSoFar) == false)
                return false;

            bestTargetSoFar = entity;
            return true;
        }

        private static bool HasAlliancePriority(in SelectEntityContext selectionContext, WorldEntity entity)
        {
            return entity.Alliance.DataRef == selectionContext.AlliancePriority;
        }

        private static bool ShouldDoAlliancePriorityCheck(in SelectEntityContext selectionContext, WorldEntity bestTargetSoFar)
        {
            return bestTargetSoFar != null && selectionContext.AlliancePriority != PrototypeId.Invalid;
        }

        private static bool CheckAttributes(in SelectEntityContext selectionContext, WorldEntity entity)
        {
            if (selectionContext.OwnerController == null) return false;
            Agent actingAgent = selectionContext.OwnerController.Owner;
            if (actingAgent == null || entity.IsInWorld == false) return false;

            bool success = true;
            if (selectionContext.AttributeList.HasValue())
                foreach (var attrib in selectionContext.AttributeList)
                {
                    if (attrib == null) return false;
                    success &= attrib.Check(actingAgent, entity);
                    if (success == false) break;
                }

            return success;
        }

        public static bool RegisterSelectedEntity(AIController ownerController, WorldEntity selectedEntity, SelectEntityType selectionType)
        {
            var owner = ownerController.Owner;
            if (owner == null) return false;
            if (selectedEntity == null)
            {
                Logger.Debug($"Agent is trying to select an entity that is invalid! {owner}");
                return false;
            }
            if (selectedEntity.IsInWorld == false)
            {
                Logger.Debug($"Agent is trying to select an entity not in the world! {owner}");
                return false;
            }

            switch (selectionType)
            {
                case SelectEntityType.SelectTarget:
                    ownerController.SetTargetEntity(selectedEntity);
                    break;

                case SelectEntityType.SelectAssistedEntity:
                    ownerController.Blackboard.PropertyCollection[PropertyEnum.AIAssistedEntityID] = selectedEntity.Id;
                    break;

                case SelectEntityType.SelectInteractedEntity:
                    ownerController.Blackboard.PropertyCollection[PropertyEnum.AIInteractEntityId] = selectedEntity.Id;
                    break;

                default:
                    Logger.Debug($"ActionSelectEntity.RegisterSelectedEntity() - Invalid select entity type for {owner}");
                    return false;
            }

            return true;
        }

        public struct SelectEntityContext
        {
            public AIController OwnerController;
            public AIEntityAttributePrototype[] AttributeList;
            public float MaxDistanceThreshold;
            public float MinDistanceThreshold;
            public SelectEntityPoolType PoolType;
            public SelectEntityMethodType SelectionMethod;
            public SelectEntityType SelectionType;
            public bool LockEntityOnceSelected;
            public float CellOrRegionAABBScale;
            public PrototypeId AlliancePriority;
            public PropertyEnum ComparisonEnum;
            public bool StaticEntities;
            public PropertyDataType ComparisonDataType;

            public SelectEntityContext(AIController ownerController, SelectEntityContextPrototype proto)
            {
                OwnerController = ownerController;
                SelectionMethod = proto.SelectionMethod;
                PoolType = proto.PoolType;
                AttributeList = proto.AttributeList;
                MinDistanceThreshold = proto.MinDistanceThreshold;                
                MaxDistanceThreshold = proto.MaxDistanceThreshold;
                LockEntityOnceSelected = proto.LockEntityOnceSelected;
                SelectionType = proto.SelectEntityType;
                CellOrRegionAABBScale = proto.CellOrRegionAABBScale;
                AlliancePriority = proto.AlliancePriority;
                ComparisonEnum = 0;
                StaticEntities = false;
                ComparisonDataType = PropertyDataType.Invalid;
                if (FindPropertyInfoForPropertyComparison(ref ComparisonEnum, ref ComparisonDataType, proto.EntitiesPropertyForComparison) == false)
                    Logger.Warn("SelectEntityInfo()::Could not find property info for targets property for comparison");
            }
        }

        public static bool FindPropertyInfoForPropertyComparison(ref PropertyEnum property, ref PropertyDataType dataType, PrototypeId propertyForComparison)
        {
            if (propertyForComparison != PrototypeId.Invalid)
            {
                PropertyInfoTable infoTable = GameDatabase.PropertyInfoTable;
                property = infoTable.GetPropertyEnumFromPrototype(propertyForComparison);
                if (property == PropertyEnum.Invalid) return false;

                PropertyInfo propertyInfo = infoTable.LookupPropertyInfo(property);
                dataType = propertyInfo.DataType;
                if (propertyInfo.ParamCount != 0)
                {
                    Logger.Warn("Found an ActionSelectEntity that has the EntitiesPropertyForComparison option referencing a property with parameters, which isn't supported!");
                    return false;
                }
            }
            return true;
        }
    }
}
