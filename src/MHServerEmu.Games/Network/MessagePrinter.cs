using System.Reflection;
using System.Text;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Regions.Maps;

namespace MHServerEmu.Games.Network
{
    /// <summary>
    /// Prints <see cref="IMessage"/> instances using custom methods.
    /// </summary>
    public static class MessagePrinter
    {
        private static readonly Dictionary<Type, Func<IMessage, string>> PrintMethodDict = new();
        private static readonly Game DummyGame = new(0);

        static MessagePrinter()
        {
            foreach (var method in typeof(MessagePrinter).GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
            {
                if (method.IsDefined(typeof(PrintMethodAttribute)) == false) continue;
                Type messageType = method.GetCustomAttribute<PrintMethodAttribute>().MessageType;
                PrintMethodDict.Add(messageType, method.CreateDelegate<Func<IMessage, string>>());
            }
        }

        /// <summary>
        /// Prints the provided <see cref="IMessage"/> to <see cref="string"/>.
        /// Uses a custom printing method if there is one defined for the <see cref="Type"/> of the provided message.
        /// </summary>
        public static string Print(IMessage message)
        {
            Type messageType = message.GetType();

            if (PrintMethodDict.TryGetValue(messageType, out var print) == false)
                return message.ToString();  // No custom print method is defined

            // Print using our custom method
            return print(message);
        }

        // Add custom message print methods below
        #region Custom Print Methods

        [PrintMethod(typeof(NetMessageEntityCreate))]
        private static string PrintNetMessageEntityCreate(IMessage message)
        {
            var entityCreate = (NetMessageEntityCreate)message;

            StringBuilder sb = new();

            // Parse base data (see GameConnection::handleEntityCreateMessage() for reference)
            ulong entityId = 0;
            PrototypeId entityPrototypeRef = PrototypeId.Invalid;

            using (Archive archive = new(ArchiveSerializeType.Replication, entityCreate.BaseData))
            {
                Serializer.Transfer(archive, ref entityId);
                sb.AppendLine($"{nameof(entityId)}: {entityId}");

                Serializer.TransferPrototypeEnum<EntityPrototype>(archive, ref entityPrototypeRef);
                sb.AppendLine($"{nameof(entityPrototypeRef)}: {GameDatabase.GetPrototypeName(entityPrototypeRef)}");

                uint fieldFlagsRaw = 0;
                Serializer.Transfer(archive, ref fieldFlagsRaw);
                var fieldFlags = (EntityCreateMessageFlags)fieldFlagsRaw;
                sb.AppendLine($"{nameof(fieldFlags)}: {fieldFlags}");

                uint locoFieldFlagsRaw = 0;
                Serializer.Transfer(archive, ref locoFieldFlagsRaw);
                var locoFieldFlags = (LocomotionMessageFlags)locoFieldFlagsRaw;
                sb.AppendLine($"{nameof(locoFieldFlags)}: {locoFieldFlags}");

                AOINetworkPolicyValues interestPolicies = AOINetworkPolicyValues.AOIChannelProximity;
                if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasNonProximityInterest))
                {
                    uint interestPoliciesRaw = 0;
                    Serializer.Transfer(archive, ref interestPoliciesRaw);
                    interestPolicies = (AOINetworkPolicyValues)interestPoliciesRaw;
                }
                sb.AppendLine($"{nameof(interestPolicies)}: {interestPolicies}");

                if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasAvatarWorldInstanceId))
                {
                    uint avatarWorldInstanceId = 0;
                    Serializer.Transfer(archive, ref avatarWorldInstanceId);
                    sb.AppendLine($"{nameof(avatarWorldInstanceId)}: {avatarWorldInstanceId}");
                }

                if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasDbId))
                {
                    ulong dbId = 0;
                    Serializer.Transfer(archive, ref dbId);
                    sb.AppendLine($"{nameof(dbId)}: 0x{dbId:X}");
                }

                if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasPositionAndOrientation))
                {
                    Vector3 position = Vector3.Zero;
                    Serializer.Transfer(archive, ref position);
                    sb.AppendLine($"{nameof(position)}: {position}");

                    bool yawOnly = locoFieldFlags.HasFlag(LocomotionMessageFlags.HasFullOrientation) == false;
                    Orientation orientation = Orientation.Zero;
                    Serializer.TransferOrientationFixed(archive, ref orientation, yawOnly, 6);
                    sb.AppendLine($"{nameof(orientation)}: {orientation}");
                }

                if (locoFieldFlags.HasFlag(LocomotionMessageFlags.NoLocomotionState) == false)
                {
                    LocomotionState locomotionState = new();
                    LocomotionState.SerializeFrom(archive, locomotionState, locoFieldFlags);
                    sb.AppendLine($"{nameof(locomotionState)}: {locomotionState}");
                }

                if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasBoundsScaleOverride))
                {
                    float boundsScaleOverride = 0f;
                    Serializer.TransferFloatFixed(archive, ref boundsScaleOverride, 8);
                    sb.AppendLine($"{nameof(boundsScaleOverride)}: {boundsScaleOverride}");
                }

                if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasSourceEntityId))
                {
                    ulong sourceEntityId = 0;
                    Serializer.Transfer(archive, ref sourceEntityId);
                    sb.AppendLine($"{nameof(sourceEntityId)}: {sourceEntityId}");
                }

                if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasSourcePosition))
                {
                    Vector3 sourcePosition = Vector3.Zero;
                    Serializer.Transfer(archive, ref sourcePosition);
                    sb.AppendLine($"{nameof(sourcePosition)}: {sourcePosition}");
                }

                if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasActivePowerPrototypeRef))
                {
                    PrototypeId activePowerPrototypeRef = PrototypeId.Invalid;
                    Serializer.TransferPrototypeEnum<PowerPrototype>(archive, ref activePowerPrototypeRef);
                    sb.AppendLine($"{nameof(activePowerPrototypeRef)}: {GameDatabase.GetPrototypeName(activePowerPrototypeRef)}");
                }

                if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasInvLoc))
                {
                    InventoryLocation invLoc = new();
                    InventoryLocation.SerializeFrom(archive, invLoc);
                    sb.AppendLine($"{nameof(invLoc)}: {invLoc}");
                }

                if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasInvLocPrev))
                {
                    InventoryLocation invLocPrev = new();
                    InventoryLocation.SerializeFrom(archive, invLocPrev);
                    sb.AppendLine($"{nameof(invLocPrev)}: {invLocPrev}");
                }

                if (fieldFlags.HasFlag(EntityCreateMessageFlags.HasAttachedEntities))
                {
                    List<ulong> attachedEntityList = new();
                    Serializer.Transfer(archive, ref attachedEntityList);
                    for (int i = 0; i < attachedEntityList.Count; i++)
                        sb.AppendLine($"{nameof(attachedEntityList)}[{i}]: {attachedEntityList[i]}");
                }
            }

            // Get blueprint for this entity
            Blueprint blueprint = GameDatabase.DataDirectory.GetPrototypeBlueprint(entityPrototypeRef);
            sb.AppendLine($"Blueprint: {GameDatabase.GetBlueprintName(blueprint.Id)} (bound to {blueprint.RuntimeBindingClassType.Name})");

            // Deserialize archive data
            using (Archive archive = new(ArchiveSerializeType.Replication, entityCreate.ArchiveData))
            {
                Entity entity = DummyGame.AllocateEntity(entityPrototypeRef);
                if (entity is Player)   // Player entity needs to be initialized to have a community to deserialize into
                    entity.Initialize(new() { EntityRef = entityPrototypeRef, Id = entityId });
                else
                    entity.TEMP_ReplacePrototype(entityPrototypeRef);

                entity.Serialize(archive);
                entity.InterestPolicies = archive.GetReplicationPolicyEnum();
                sb.Append($"ArchiveData: {entity.ToStringVerbose()}");
            }

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

        [PrintMethod(typeof(NetMessageEntityEnterGameWorld))]
        private static string PrintNetMessageEntityEnterGameWorld(IMessage message)
        {
            var entityEnterGameWorld = (NetMessageEntityEnterGameWorld)message;

            using (Archive archive = new(ArchiveSerializeType.Replication, entityEnterGameWorld.ArchiveData))
            {
                EnterGameWorldArchive enterGameWorldArchive = new();
                enterGameWorldArchive.ReplicationPolicy = archive.GetReplicationPolicyEnum();
                enterGameWorldArchive.Serialize(archive);
                return $"ArchiveData: {enterGameWorldArchive}";
            }
        }

        [PrintMethod(typeof(NetMessageLocomotionStateUpdate))]
        private static string PrintNetMessageLocomotionStateUpdate(IMessage message)
        {
            var locomotionStateUpdate = (NetMessageLocomotionStateUpdate)message;

            using (Archive archive = new(ArchiveSerializeType.Replication, locomotionStateUpdate.ArchiveData))
            {
                LocomotionStateUpdateArchive update = new();
                update.ReplicationPolicy = archive.GetReplicationPolicyEnum();
                update.Serialize(archive);
                return $"ArchiveData: {update}";
            }
        }

        [PrintMethod(typeof(NetMessageActivatePower))]
        private static string PrintNetMessageActivatePower(IMessage message)
        {
            var activatePower = (NetMessageActivatePower)message;

            using (Archive archive = new(ArchiveSerializeType.Replication, activatePower.ArchiveData))
            {
                ActivatePowerArchive activatePowerArchive = new();
                activatePowerArchive.ReplicationPolicy = archive.GetReplicationPolicyEnum();
                activatePowerArchive.Serialize(archive);
                return $"ArchiveData: {activatePowerArchive}";
            }
        }

        [PrintMethod(typeof(NetMessagePowerResult))]
        private static string PrintNetMessagePowerResult(IMessage message)
        {
            var powerResult = (NetMessagePowerResult)message;

            using (Archive archive = new(ArchiveSerializeType.Replication, powerResult.ArchiveData))
            {
                PowerResults powerResults = new();
                powerResults.ReplicationPolicy = archive.GetReplicationPolicyEnum();
                powerResults.Serialize(archive);
                return $"ArchiveData: {powerResults}";
            }
        }

        [PrintMethod(typeof(NetMessageAddCondition))]
        private static string PrintNetMessageAddCondition(IMessage message)
        {
            var addCondition = (NetMessageAddCondition)message;

            StringBuilder sb = new();

            using (Archive archive = new(ArchiveSerializeType.Replication, addCondition.ArchiveData))
            {
                sb.AppendLine($"ReplicationPolicy: {archive.GetReplicationPolicyEnum()}");

                ulong entityId = 0;
                Serializer.Transfer(archive, ref entityId);
                sb.AppendLine($"entityId: {entityId}");

                Condition condition = new();
                condition.Serialize(archive, null);
                sb.AppendLine($"condition: {condition}");
            }

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

        [PrintMethod(typeof(NetMessageUpdateMiniMap))]
        private static string PrintNetMessageUpdateMiniMap(IMessage message)
        {
            var updateMiniMap = (NetMessageUpdateMiniMap)message;

            using (Archive archive = new(ArchiveSerializeType.Replication, updateMiniMap.ArchiveData))
            {
                MiniMapArchive miniMapArchive = new();
                miniMapArchive.ReplicationPolicy = archive.GetReplicationPolicyEnum();
                miniMapArchive.Serialize(archive);
                return $"ArchiveData: {miniMapArchive}";
            }
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

        #endregion

        [AttributeUsage(AttributeTargets.Method)]
        private class PrintMethodAttribute : Attribute
        {
            public Type MessageType { get; }

            public PrintMethodAttribute(Type messageType)
            {
                MessageType = messageType;
            }
        }
    }
}
