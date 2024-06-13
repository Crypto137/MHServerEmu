using System.Text;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Core;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Regions.Maps;

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
                entity.Initialize(new() { EntityRef = entityPrototypeRef, Id = entityId });
                entity.Serialize(archive);

                sb.AppendLine().AppendLine("archiveData");
                sb.AppendLine(entity.ToStringVerbose());
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

            sb.Append(regionChange.ToString());
            if (regionChange.ArchiveData.Length > 0)
            {
                using (Archive archive = new(ArchiveSerializeType.Replication, regionChange.ArchiveData))
                {
                    RegionArchive regionArchive = new();
                    regionArchive.Serialize(archive);
                    sb.Append($"ArchiveData: {regionArchive}");
                }
            }

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
                (UInt32Flags)cancelPower.EndPowerFlags);
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

        [PrintMethod(typeof(NetMessageQueryIsRegionAvailable))]
        private static string PrintNetMessageQueryIsRegionAvailable(IMessage message)
        {
            var queryIsRegionAvailable = (NetMessageQueryIsRegionAvailable)message;
            return $"regionPrototype: {GameDatabase.GetPrototypeName((PrototypeId)queryIsRegionAvailable.RegionPrototype)}";
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
    }
}
