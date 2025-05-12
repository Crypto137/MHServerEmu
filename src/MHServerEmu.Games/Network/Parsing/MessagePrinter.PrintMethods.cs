using System.Text;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions.Maps;
using MHServerEmu.Games.Powers;
using MHServerEmu.Core.Memory;

// This was previously used for our packet parsing functionality, which we no longer need. I am leaving this here just for reference.

#if false
namespace MHServerEmu.Games.Network.Parsing
{
    public static partial class MessagePrinter
    {
        [PrintMethod(typeof(NetMessageEntityCreate))]
        private static string PrintNetMessageEntityCreate(IMessage message)
        {
            var entityCreate = (NetMessageEntityCreate)message;

            StringBuilder sb = new();

            ulong entityId;
            PrototypeId entityPrototypeRef;

            using (Archive archive = new(ArchiveSerializeType.Replication, entityCreate.BaseData))
                ArchiveParser.ParseEntityCreateBaseData(archive, sb, out entityId, out entityPrototypeRef);

            // Get blueprint for this entity
            Blueprint blueprint = GameDatabase.DataDirectory.GetPrototypeBlueprint(entityPrototypeRef);
            sb.AppendLine($"blueprint: {GameDatabase.GetBlueprintName(blueprint.Id)} (bound to {blueprint.RuntimeBindingClassType.Name})");

            // Deserialize archive data
            using (Archive archive = new(ArchiveSerializeType.Replication, entityCreate.ArchiveData))
            {
                Entity entity = DummyGame.AllocateEntity(entityPrototypeRef);

                using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
                settings.EntityRef = entityPrototypeRef;
                settings.Id = entityId;

                entity.Initialize(settings);

                entity.Serialize(archive);

                sb.AppendLine().AppendLine("archiveData");
                sb.AppendLine(entity.ToStringVerbose());

                DummyGame.MessageHandlerDict.Clear();   // Clear handlers to avoid collision errors
            }

            return sb.ToString();
        }

        [PrintMethod(typeof(NetMessageInventoryMove))]
        private static string PrintNetMessageInventoryMove(IMessage message)
        {
            var inventoryMove = (NetMessageInventoryMove)message;

            StringBuilder sb = new();
            sb.AppendLine($"entityId: {inventoryMove.EntityId}");
            sb.AppendLine($"invLocContainerEntityId: {inventoryMove.InvLocContainerEntityId}");
            sb.AppendLine($"invLocInventoryPrototypeId: {GameDatabase.GetPrototypeName((PrototypeId)inventoryMove.InvLocInventoryPrototypeId)}");
            sb.AppendLine($"invLocSlot: {inventoryMove.InvLocSlot}");

            if (inventoryMove.HasRequiredNoOwnerOnClient)
                sb.AppendLine($"requiredNoOwnerOnClient: {inventoryMove.RequiredNoOwnerOnClient}");

            if (inventoryMove.HasEntityDataId)
                sb.AppendLine($"entityDataId: {GameDatabase.GetPrototypeName((PrototypeId)inventoryMove.EntityDataId)}");

            if (inventoryMove.HasDestOwnerDataId)
                sb.AppendLine($"destOwnerDataId: {GameDatabase.GetPrototypeName((PrototypeId)inventoryMove.DestOwnerDataId)}");

            return sb.ToString();
        }

        [PrintMethod(typeof(NetMessageQueueLoadingScreen))]
        private static string PrintNetMessageQueueLoadingScreen(IMessage message)
        {
            var queueLoadingScreen = (NetMessageQueueLoadingScreen)message;

            StringBuilder sb = new();
            if (queueLoadingScreen.HasRegionPrototypeId)
                sb.AppendLine($"regionPrototypeId: {GameDatabase.GetPrototypeName((PrototypeId)queueLoadingScreen.RegionPrototypeId)}");
            if (queueLoadingScreen.HasLoadingScreenPrototypeId)
                sb.AppendLine($"loadingScreenPrototypeId: {GameDatabase.GetPrototypeName((PrototypeId)queueLoadingScreen.LoadingScreenPrototypeId)}");
            return sb.ToString();
        }

        [PrintMethod(typeof(NetMessageRegionChange))]
        private static string PrintNetMessageRegionChange(IMessage message)
        {
            var regionChange = (NetMessageRegionChange)message;

            StringBuilder sb = new();

            sb.AppendLine($"regionId: 0x{regionChange.RegionId:X}");
            sb.AppendLine($"serverGameId: 0x{regionChange.ServerGameId:X}");
            sb.AppendLine($"clearingAllInterest: {regionChange.ClearingAllInterest}");

            for (int i = 0; i < regionChange.EntitiestodestroyCount; i++)
                sb.AppendLine($"entitiestodestroy[{i}]: {regionChange.EntitiestodestroyList[i]}");

            if (regionChange.HasRegionPrototypeId)
                sb.AppendLine($"regionPrototypeId: {GameDatabase.GetPrototypeName((PrototypeId)regionChange.RegionPrototypeId)}");

            if (regionChange.HasRegionRandomSeed)
                sb.AppendLine($"regionRandomSeed: {regionChange.RegionRandomSeed}");

            if (regionChange.HasArchiveData)
            {
                using (Archive archive = new(ArchiveSerializeType.Replication, regionChange.ArchiveData))
                    ArchiveParser.ParseRegionChangeArchiveData(archive, sb);
            }

            if (regionChange.HasRegionMax)
                sb.AppendLine($"regionMin: {new Vector3(regionChange.RegionMin)}");

            if (regionChange.HasRegionMax)
                sb.AppendLine($"regionMax: {new Vector3(regionChange.RegionMax)}");

            if (regionChange.HasCreateRegionParams)
                sb.AppendLine($"createRegionParams: {regionChange.CreateRegionParams}");

            return sb.ToString();
        }

        [PrintMethod(typeof(NetMessageLocomotionStateUpdate))]
        private static string PrintNetMessageLocomotionStateUpdate(IMessage message)
        {
            var locomotionStateUpdate = (NetMessageLocomotionStateUpdate)message;

            StringBuilder sb = new();

            using (Archive archive = new(ArchiveSerializeType.Replication, locomotionStateUpdate.ArchiveData))
                ArchiveParser.ParseLocomotionStateUpdate(archive, sb);

            return sb.ToString();
        }

        [PrintMethod(typeof(NetMessageInterestPolicies))]
        private static string PrintNetMessageInterestPolicies(IMessage message)
        {
            var interestPolicies = (NetMessageInterestPolicies)message;
            return string.Format("idEntity: {0}\nnewPolicies: {1}\nprevPolicies: {2}",
                interestPolicies.IdEntity,
                (AOINetworkPolicyValues)interestPolicies.NewPolicies,
                (AOINetworkPolicyValues)interestPolicies.PrevPolicies);
        }

        [PrintMethod(typeof(NetMessageAddArea))]
        private static string PrintNetMessageAddArea(IMessage message)
        {
            var addArea = (NetMessageAddArea)message;

            StringBuilder sb = new();
            sb.AppendLine($"areaId: {addArea.AreaId}");
            sb.AppendLine($"areaPrototypeId: {GameDatabase.GetPrototypeName((PrototypeId)addArea.AreaPrototypeId)}");
            sb.AppendLine($"areaOrigin: {new Vector3(addArea.AreaOrigin)}");

            if (addArea.HasIsStartArea)
                sb.AppendLine($"isStartArea: {addArea.IsStartArea}");

            return sb.ToString();
        }

        [PrintMethod(typeof(NetMessageCellCreate))]
        private static string PrintNetMessageCellCreate(IMessage message)
        {
            var cellCreate = (NetMessageCellCreate)message;

            StringBuilder sb = new();
            sb.AppendLine($"areaId: {cellCreate.AreaId}");
            sb.AppendLine($"cellId: {cellCreate.CellId}");
            sb.AppendLine($"cellPrototypeId: {GameDatabase.GetPrototypeName((PrototypeId)cellCreate.CellPrototypeId)}");
            sb.AppendLine($"positionInArea: {new Vector3(cellCreate.PositionInArea)}");
            sb.AppendLine($"cellRandomSeed: {cellCreate.CellRandomSeed}");

            for (int i = 0; i < cellCreate.EncountersCount; i++)
            {
                NetStructReservedSpawn encounter = cellCreate.EncountersList[i];
                sb.AppendLine($"encounters[{i}]: asset={GameDatabase.GetAssetName((AssetId)encounter.Asset)}, id={encounter.Id}, useMarkerOrientation={encounter.UseMarkerOrientation}");
            }

            sb.AppendLine($"bufferWidth: {cellCreate.Bufferwidth}");
            sb.AppendLine($"overrideLocationName: {cellCreate.OverrideLocationName}");

            return sb.ToString();
        }

        [PrintMethod(typeof(NetMessageChangeAOIPolicies))]
        private static string PrintNetMessageChangeAOIPolicies(IMessage message)
        {
            var changeAoiPolicies = (NetMessageChangeAOIPolicies)message;

            StringBuilder sb = new();
            sb.AppendLine($"idEntity: {changeAoiPolicies.IdEntity}");
            sb.AppendLine($"currentPolicies: {(AOINetworkPolicyValues)changeAoiPolicies.Currentpolicies}");
            if (changeAoiPolicies.HasExitGameWorld)
                sb.AppendLine($"exitGameWorld: {changeAoiPolicies.ExitGameWorld}");

            return sb.ToString();
        }

        [PrintMethod(typeof(NetMessageCancelPower))]
        private static string PrintNetMessageCancelPower(IMessage message)
        {
            var cancelPower = (NetMessageCancelPower)message;
            return string.Format("idAgent: {0}\npowerPrototypeId: {1}\nendPowerFlags: {2}",
                cancelPower.IdAgent,
                GameDatabase.GetPrototypeName(GameDatabase.DataDirectory.GetPrototypeFromEnumValue<PowerPrototype>((int)cancelPower.PowerPrototypeId)),
                (EndPowerFlags)cancelPower.EndPowerFlags);
        }

        [PrintMethod(typeof(NetMessageActivatePower))]
        private static string PrintNetMessageActivatePower(IMessage message)
        {
            var activatePower = (NetMessageActivatePower)message;

            StringBuilder sb = new();

            using (Archive archive = new(ArchiveSerializeType.Replication, activatePower.ArchiveData))
                ArchiveParser.ParseActivatePower(archive, sb);

            return sb.ToString();
        }

        [PrintMethod(typeof(NetMessagePowerResult))]
        private static string PrintNetMessagePowerResult(IMessage message)
        {
            var powerResult = (NetMessagePowerResult)message;

            StringBuilder sb = new();

            using (Archive archive = new(ArchiveSerializeType.Replication, powerResult.ArchiveData))
                ArchiveParser.ParsePowerResult(archive, sb);

            return sb.ToString();
        }

        [PrintMethod(typeof(NetMessageEntityEnterGameWorld))]
        private static string PrintNetMessageEntityEnterGameWorld(IMessage message)
        {
            var entityEnterGameWorld = (NetMessageEntityEnterGameWorld)message;

            StringBuilder sb = new();

            using (Archive archive = new(ArchiveSerializeType.Replication, entityEnterGameWorld.ArchiveData))
                ArchiveParser.ParseEntityEnterGameWorld(archive, sb);

            return sb.ToString();
        }

        [PrintMethod(typeof(NetMessageMissionUpdate))]
        private static string PrintNetMessageMissionUpdate(IMessage message)
        {
            var missionUpdate = (NetMessageMissionUpdate)message;

            StringBuilder sb = new();
            sb.AppendLine($"missionPrototypeId: {GameDatabase.GetPrototypeName((PrototypeId)missionUpdate.MissionPrototypeId)}");

            if (missionUpdate.HasMissionState)
                sb.AppendLine($"missionState: {(MissionState)missionUpdate.MissionState}");

            if (missionUpdate.HasMissionStateExpireTime)
                sb.AppendLine($"missionStateExpireTime: {missionUpdate.MissionStateExpireTime}");

            if (missionUpdate.HasRewards)
                sb.AppendLine($"rewards: {missionUpdate.Rewards}");

            if (missionUpdate.ParticipantsCount > 0)
                sb.AppendLine($"participants: {string.Join(' ', missionUpdate.ParticipantsList)}");

            if (missionUpdate.HasSuppressNotification)
                sb.AppendLine($"suppressNotification: {missionUpdate.SuppressNotification}");

            if (missionUpdate.HasSuspendedState)
                sb.AppendLine($"suspendedState: {missionUpdate.SuspendedState}");

            return sb.ToString();
        }

        [PrintMethod(typeof(NetMessageMissionObjectiveUpdate))]
        private static string PrintNetMessageMissionObjectiveUpdate(IMessage message)
        {
            var missionObjectiveUpdate = (NetMessageMissionObjectiveUpdate)message;

            StringBuilder sb = new();
            sb.AppendLine($"missionPrototypeId: {GameDatabase.GetPrototypeName((PrototypeId)missionObjectiveUpdate.MissionPrototypeId)}");
            sb.AppendLine($"objectiveIndex: {missionObjectiveUpdate.ObjectiveIndex}");

            if (missionObjectiveUpdate.HasObjectiveState)
                sb.AppendLine($"objectiveState: {(MissionObjectiveState)missionObjectiveUpdate.ObjectiveState}");

            if (missionObjectiveUpdate.HasObjectiveStateExpireTime)
                sb.AppendLine($"objectiveStateExpireTime: {missionObjectiveUpdate.ObjectiveStateExpireTime}");

            if (missionObjectiveUpdate.HasCurrentCount || missionObjectiveUpdate.HasRequiredCount)
                sb.AppendLine($"count: {missionObjectiveUpdate.CurrentCount}/{missionObjectiveUpdate.RequiredCount}");

            if (missionObjectiveUpdate.HasFailCurrentCount || missionObjectiveUpdate.HasFailRequiredCount)
                sb.AppendLine($"failCount: {missionObjectiveUpdate.FailCurrentCount}/{missionObjectiveUpdate.FailRequiredCount}");

            for (int i = 0; i < missionObjectiveUpdate.InteractedEntitiesCount; i++)
            {
                var interactedEntity = missionObjectiveUpdate.InteractedEntitiesList[i];
                sb.AppendLine($"interactedEntities[{i}]: entityId={interactedEntity.EntityId}, regionId={interactedEntity.RegionId}");
            }

            if (missionObjectiveUpdate.HasSuppressNotification)
                sb.AppendLine($"suppressNotification: {missionObjectiveUpdate.SuppressNotification}");

            if (missionObjectiveUpdate.HasSuspendedState)
                sb.AppendLine($"suspendedState: {missionObjectiveUpdate.SuspendedState}");

            return sb.ToString();
        }

        [PrintMethod(typeof(NetMessagePrefetchAssets))]
        private static string PrintNetMessagePrefetchAssets(IMessage message)
        {
            var prefetchAssets = (NetMessagePrefetchAssets)message;

            StringBuilder sb = new();
            sb.AppendLine($"priority: {prefetchAssets.Priority}");

            for (int i = 0; i < prefetchAssets.AssetsCount; i++)
                sb.AppendLine($"assets[{i}]: {GameDatabase.GetAssetName((AssetId)prefetchAssets.AssetsList[i])}");

            for (int i = 0; i < prefetchAssets.PrototypesCount; i++)
                sb.AppendLine($"prototypes[{i}]: {GameDatabase.GetPrototypeName((PrototypeId)prefetchAssets.PrototypesList[i])}");

            for (int i = 0; i < prefetchAssets.CellsCount; i++)
            {
                NetStructPrefetchCell prefetchCell = prefetchAssets.CellsList[i];
                sb.AppendLine($"cells[{i}]: cellId={prefetchCell.CellId}, cellPrototypeId={GameDatabase.GetPrototypeName((PrototypeId)prefetchCell.CellPrototypeId)}");
            }

            if (prefetchAssets.HasRegionId)
                sb.AppendLine($"regionId: {prefetchAssets.RegionId}");

            return sb.ToString();
        }

        [PrintMethod(typeof(NetMessageQueryIsRegionAvailable))]
        private static string PrintNetMessageQueryIsRegionAvailable(IMessage message)
        {
            var queryIsRegionAvailable = (NetMessageQueryIsRegionAvailable)message;
            return $"regionPrototype: {GameDatabase.GetPrototypeName((PrototypeId)queryIsRegionAvailable.RegionPrototype)}";
        }

        [PrintMethod(typeof(NetMessageStoryNotification))]
        private static string PrintNetMessageStoryNotification(IMessage message)
        {
            var storyNotification = (NetMessageStoryNotification)message;

            StringBuilder sb = new();
            sb.AppendLine($"displayTextStringId: {storyNotification.DisplayTextStringId}");

            if (storyNotification.HasSpeakingEntityPrototypeId)
                sb.AppendLine($"speakingEntityPrototypeId: {GameDatabase.GetPrototypeName((PrototypeId)storyNotification.SpeakingEntityPrototypeId)}");

            sb.AppendLine($"timeToLiveMS: {storyNotification.TimeToLiveMS}");
            sb.AppendLine($"voTriggerAssetId: {GameDatabase.GetAssetName((AssetId)storyNotification.VoTriggerAssetId)}");

            if (storyNotification.HasMissionPrototypeId)
                sb.AppendLine($"missionPrototypeId: {GameDatabase.GetPrototypeName((PrototypeId)storyNotification.MissionPrototypeId)}");

            return sb.ToString();
        }

        [PrintMethod(typeof(NetMessageSetProperty))]
        private static string PrintNetMessageSetProperty(IMessage message)
        {
            var setProperty = (NetMessageSetProperty)message;
            PropertyId propertyId = new(setProperty.PropertyId.ReverseBits());
            Properties.PropertyInfo propertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyId.Enum);
            PropertyValue propertyValue = PropertyCollection.ConvertBitsToValue(setProperty.ValueBits, propertyInfo.DataType);
            return $"({setProperty.ReplicationId}) {propertyId}: {propertyValue.Print(propertyInfo.DataType)}";
        }

        [PrintMethod(typeof(NetMessageRemoveProperty))]
        private static string PrintNetMessageRemoveProperty(IMessage message)
        {
            var removeProperty = (NetMessageRemoveProperty)message;
            return $"({removeProperty.ReplicationId}) {new PropertyId(removeProperty.PropertyId.ReverseBits())}";
        }

        [PrintMethod(typeof(NetMessageAddCondition))]
        private static string PrintNetMessageAddCondition(IMessage message)
        {
            var addCondition = (NetMessageAddCondition)message;

            StringBuilder sb = new();

            using (Archive archive = new(ArchiveSerializeType.Replication, addCondition.ArchiveData))
                ArchiveParser.ParseAddCondition(archive, sb);

            return sb.ToString();
        }

        [PrintMethod(typeof(NetMessagePowerCollectionAssignPower))]
        private static string PrintNetMessagePowerCollectionAssignPower(IMessage message)
        {
            var assignPower = (NetMessagePowerCollectionAssignPower)message;

            return string.Format("({0}) {1} (powerRank={2}, characterLevel={3}, combatLevel={4}, itemLevel={5}, itemVariation={6}f)",
                assignPower.EntityId, GameDatabase.GetPrototypeName((PrototypeId)assignPower.PowerProtoId), assignPower.PowerRank, assignPower.CharacterLevel,
                assignPower.CombatLevel, assignPower.ItemLevel, assignPower.ItemVariation);
        }

        [PrintMethod(typeof(NetMessageAssignPowerCollection))]
        private static string PrintNetMessageAssignPowerCollection(IMessage message)
        {
            var assignPowerCollection = (NetMessageAssignPowerCollection)message;

            StringBuilder sb = new();

            foreach (NetMessagePowerCollectionAssignPower assignPower in assignPowerCollection.PowerList)
                sb.AppendLine(PrintNetMessagePowerCollectionAssignPower(assignPower));

            return sb.ToString();
        }

        [PrintMethod(typeof(NetMessagePowerCollectionUnassignPower))]
        private static string PrintNetMessagePowerCollectionUnassignPower(IMessage message)
        {
            var unassignPower = (NetMessagePowerCollectionUnassignPower)message;
            return $"({unassignPower.EntityId}) {GameDatabase.GetPrototypeName((PrototypeId)unassignPower.PowerProtoId)}";
        }

        [PrintMethod(typeof(NetMessageUpdatePowerIndexProps))]
        private static string PrintNetMessageUpdatePowerIndexProps(IMessage message)
        {
            var updatePowerIndexProps = (NetMessageUpdatePowerIndexProps)message;
            return string.Format("({0}) {1} (powerRank={2}, characterLevel={3}, combatLevel={4}, itemLevel={5}, itemVariation={6}f)",
                updatePowerIndexProps.EntityId,
                GameDatabase.GetPrototypeName((PrototypeId)updatePowerIndexProps.PowerProtoId),
                updatePowerIndexProps.PowerRank,
                updatePowerIndexProps.CharacterLevel,
                updatePowerIndexProps.CombatLevel,
                updatePowerIndexProps.ItemLevel,
                updatePowerIndexProps.ItemVariation);
        }

        [PrintMethod(typeof(NetMessageUpdateMiniMap))]
        private static string PrintNetMessageUpdateMiniMap(IMessage message)
        {
            var updateMiniMap = (NetMessageUpdateMiniMap)message;

            StringBuilder sb = new();

            using (Archive archive = new(ArchiveSerializeType.Replication, updateMiniMap.ArchiveData))
            {
                sb.AppendLine($"ReplicationPolicy: {archive.GetReplicationPolicyEnum()}");

                LowResMap lowResMap = new();
                Serializer.Transfer(archive, ref lowResMap);
                sb.AppendLine($"lowResMap: {lowResMap}");
            }

            return sb.ToString();
        }

        [PrintMethod(typeof(NetMessagePlayStoryBanter))]
        private static string PrintNetMessagePlayStoryBanter(IMessage message)
        {
            var playStoryBanter = (NetMessagePlayStoryBanter)message;
            return $"banterAssetId: {GameDatabase.GetAssetName((AssetId)playStoryBanter.BanterAssetId)}";
        }

        [PrintMethod(typeof(NetMessagePlayKismetSeq))]
        private static string PrintNetMessagePlayKismetSeq(IMessage message)
        {
            var playKismetSeq = (NetMessagePlayKismetSeq)message;
            return $"kismetSeqPrototypeId: {GameDatabase.GetPrototypeName((PrototypeId)playKismetSeq.KismetSeqPrototypeId)}";
        }

        [PrintMethod(typeof(NetMessageInventoryLoaded))]
        private static string PrintNetMessageInventoryLoaded(IMessage message)
        {
            var inventoryLoaded = (NetMessageInventoryLoaded)message;

            StringBuilder sb = new();

            sb.AppendLine($"inventoryProtoId={GameDatabase.GetPrototypeName((PrototypeId)inventoryLoaded.InventoryProtoId)}");
            sb.AppendLine($"loadState={inventoryLoaded.LoadState}");
            for (int i = 0; i < inventoryLoaded.ArchivedEntitiesCount; i++)
                sb.AppendLine($"archivedEntities[{i}]: {inventoryLoaded.ArchivedEntitiesList[i]}");

            return sb.ToString();
        }

        #region Client Messages

        [PrintMethod(typeof(NetMessageTryActivatePower))]
        private static string PrintNetMessageTryActivatePower(IMessage message)
        {
            var tryActivatePower = (NetMessageTryActivatePower)message;

            StringBuilder sb = new();
            sb.AppendLine($"idUserEntity: {tryActivatePower.IdUserEntity}");
            sb.AppendLine($"powerPrototypeId: {GameDatabase.GetPrototypeName((PrototypeId)tryActivatePower.PowerPrototypeId)}");

            if (tryActivatePower.HasIdTargetEntity)
                sb.AppendLine($"idTargetEntity: {tryActivatePower.IdTargetEntity}");

            if (tryActivatePower.HasTargetPosition)
                sb.AppendLine($"targetPosition: {new Vector3(tryActivatePower.TargetPosition)}");

            if (tryActivatePower.HasMovementSpeed)
                sb.AppendLine($"movementSpeed: {tryActivatePower.MovementSpeed}f");

            if (tryActivatePower.HasMovementTimeMS)
                sb.AppendLine($"movementTimeMS: {tryActivatePower.MovementTimeMS}");

            if (tryActivatePower.HasPowerRandomSeed)
                sb.AppendLine($"powerRandomSeed: {tryActivatePower.PowerRandomSeed}");

            if (tryActivatePower.HasItemSourceId)
                sb.AppendLine($"itemSourceId: {tryActivatePower.ItemSourceId}");

            sb.AppendLine($"fxRandomSeed: {tryActivatePower.FxRandomSeed}");

            if (tryActivatePower.HasTriggeringPowerPrototypeId)
                sb.AppendLine($"triggeringPowerPrototypeId: {GameDatabase.GetPrototypeName((PrototypeId)tryActivatePower.TriggeringPowerPrototypeId)}");

            return sb.ToString();
        }

        [PrintMethod(typeof(NetMessagePowerRelease))]
        private static string PrintNetMessagePowerRelease(IMessage message)
        {
            var powerRelease = (NetMessagePowerRelease)message;

            StringBuilder sb = new();
            sb.AppendLine($"idUserEntity: {powerRelease.IdUserEntity}");
            sb.AppendLine($"powerPrototypeId: {GameDatabase.GetPrototypeName((PrototypeId)powerRelease.PowerPrototypeId)}");

            if (powerRelease.HasIdTargetEntity)
                sb.AppendLine($"idTargetEntity: {powerRelease.IdUserEntity}");

            if (powerRelease.HasTargetPosition)
                sb.AppendLine($"targetPosition: {new Vector3(powerRelease.TargetPosition)}");

            return sb.ToString();
        }

        [PrintMethod(typeof(NetMessageTryCancelPower))]
        private static string PrintNetMessageTryCancelPower(IMessage message)
        {
            var tryCancelPower = (NetMessageTryCancelPower)message;

            StringBuilder sb = new();
            sb.AppendLine($"idUserEntity: {tryCancelPower.IdUserEntity}");
            sb.AppendLine($"powerPrototypeId: {GameDatabase.GetPrototypeName((PrototypeId)tryCancelPower.PowerPrototypeId)}");
            sb.AppendLine($"endPowerFlags: {(EndPowerFlags)tryCancelPower.EndPowerFlags}");

            return sb.ToString();
        }

        [PrintMethod(typeof(NetMessageContinuousPowerUpdateToServer))]
        private static string PrintNetMessageContinuousPowerUpdateToServer(IMessage message)
        {
            var continuousPowerUpdate = (NetMessageContinuousPowerUpdateToServer)message;

            StringBuilder sb = new();
            sb.AppendLine($"powerPrototypeId: {GameDatabase.GetPrototypeName((PrototypeId)continuousPowerUpdate.PowerPrototypeId)}");
            sb.AppendLine($"avatarIndex: {continuousPowerUpdate.AvatarIndex}");

            if (continuousPowerUpdate.HasIdTargetEntity)
                sb.AppendLine($"idTargetEntity: {continuousPowerUpdate.IdTargetEntity}");

            if (continuousPowerUpdate.HasTargetPosition)
                sb.AppendLine($"targetPosition: {new Vector3(continuousPowerUpdate.TargetPosition)}");

            if (continuousPowerUpdate.HasRandomSeed)
                sb.AppendLine($"randomSeed: {continuousPowerUpdate.RandomSeed}");

            return sb.ToString();
        }

        #endregion
    }
}
#endif
