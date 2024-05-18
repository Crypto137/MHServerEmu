using System.Reflection;
using System.Text;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
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

            // Parse base data
            EntityBaseData baseData = new();
            using (Archive archive = new(ArchiveSerializeType.Replication, entityCreate.BaseData))
            {
                baseData.ReplicationPolicy = archive.GetReplicationPolicyEnum();
                baseData.Serialize(archive);
            }

            sb.AppendLine($"BaseData: {baseData}");

            // Get blueprint for this entity
            Blueprint blueprint = GameDatabase.DataDirectory.GetPrototypeBlueprint(baseData.EntityPrototypeRef);
            sb.AppendLine($"Blueprint: {GameDatabase.GetBlueprintName(blueprint.Id)} (bound to {blueprint.RuntimeBindingClassType.Name})");

            // Deserialize archive data
            using (Archive archive = new(ArchiveSerializeType.Replication, entityCreate.ArchiveData))
            {
                Entity entity = DummyGame.AllocateEntity(baseData.EntityPrototypeRef);
                entity.BaseData = baseData;
                entity.Serialize(archive);
                entity.ReplicationPolicy = archive.GetReplicationPolicyEnum();
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
            return $"ArchiveData: {new AddConditionArchive(addCondition.ArchiveData)}";
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
