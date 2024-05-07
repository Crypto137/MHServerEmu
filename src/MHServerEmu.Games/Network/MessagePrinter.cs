using System.Reflection;
using System.Text;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.MetaGames;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Network
{
    /// <summary>
    /// Prints <see cref="IMessage"/> instances using custom methods.
    /// </summary>
    public static class MessagePrinter
    {
        private static readonly Dictionary<Type, Func<IMessage, string>> PrintMethodDict = new();

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
            EntityBaseData baseData = new(entityCreate.BaseData);
            sb.AppendLine($"BaseData: {baseData}");

            // Get blueprint for this entity
            Blueprint blueprint = GameDatabase.DataDirectory.GetPrototypeBlueprint(baseData.PrototypeId);
            sb.AppendLine($"Blueprint: {GameDatabase.GetBlueprintName(blueprint.Id)} (bound to {blueprint.RuntimeBindingClassType.Name})");

            // Parse entity depending on its blueprint class
            switch (blueprint.RuntimeBindingClassType.Name)
            {
                case "EntityPrototype":
                    sb.Append($"ArchiveData: {new Entity(baseData, entityCreate.ArchiveData)}");
                    break;

                case "WorldEntityPrototype":
                case "PropPrototype":
                case "DestructiblePropPrototype":
                case "SpawnerPrototype":
                    sb.Append($"ArchiveData: {new WorldEntity(baseData, entityCreate.ArchiveData)}");
                    break;

                case "HotspotPrototype":
                    sb.Append($"ArchiveData: {new Hotspot(baseData, entityCreate.ArchiveData)}");
                    break;

                case "AgentPrototype":
                case "AgentTeamUpPrototype":
                case "OrbPrototype":
                case "SmartPropPrototype":
                    sb.Append($"ArchiveData: {new Agent(baseData, entityCreate.ArchiveData)}");
                    break;

                case "MissilePrototype":
                    sb.Append($"ArchiveData: {new Missile(baseData, entityCreate.ArchiveData)}");
                    break;

                case "AvatarPrototype":
                    sb.Append($"ArchiveData: {new Avatar(baseData, entityCreate.ArchiveData)}");
                    break;

                case "KismetSequenceEntityPrototype":
                    sb.Append($"ArchiveData: {new KismetSequenceEntity(baseData, entityCreate.ArchiveData)}");
                    break;

                case "ItemPrototype":
                case "ArmorPrototype":
                case "ArtifactPrototype":
                case "BagItemPrototype":
                case "CharacterTokenPrototype":
                case "CostumeCorePrototype":
                case "CostumePrototype":
                case "CraftingIngredientPrototype":
                case "CraftingRecipePrototype":
                case "EmoteTokenPrototype":
                case "InventoryStashTokenPrototype":
                case "LegendaryPrototype":
                case "MedalPrototype":
                case "RelicPrototype":
                case "TeamUpGearPrototype":
                case "VanityTitleItemPrototype":
                    sb.Append($"ArchiveData: {new Item(baseData, entityCreate.ArchiveData)}");
                    break;

                case "PlayerPrototype":
                    sb.Append($"ArchiveData: {new Player(baseData, entityCreate.ArchiveData)}");
                    break;

                case "TransitionPrototype":
                    sb.Append($"ArchiveData: {new Transition(baseData, entityCreate.ArchiveData)}");
                    break;

                case "MetaGamePrototype":
                case "MatchMetaGamePrototype":
                    sb.Append($"ArchiveData: {new MetaGame(baseData, entityCreate.ArchiveData)}");
                    break;

                case "PvPPrototype":
                    sb.Append($"ArchiveData: {new PvP(baseData, entityCreate.ArchiveData)}");
                    break;

                case "MissionMetaGamePrototype":
                    sb.Append($"ArchiveData: {new MissionMetaGame(baseData, entityCreate.ArchiveData)}");
                    break;

                default:
                    sb.Append($"ArchiveData: unsupported entity ({blueprint.RuntimeBindingClassType.Name})");
                    break;
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
                using (Archive archive = new(ArchiveSerializeType.Replication, regionChange.ArchiveData.ToByteArray()))
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
            return $"ArchiveData: {new EnterGameWorldArchive(entityEnterGameWorld.ArchiveData)}";
        }

        [PrintMethod(typeof(NetMessageLocomotionStateUpdate))]
        private static string PrintNetMessageLocomotionStateUpdate(IMessage message)
        {
            var locomotionStateUpdate = (NetMessageLocomotionStateUpdate)message;

            using (Archive archive = new(ArchiveSerializeType.Replication, locomotionStateUpdate.ArchiveData.ToByteArray()))
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

            using (Archive archive = new(ArchiveSerializeType.Replication, activatePower.ArchiveData.ToByteArray()))
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

            using (Archive archive = new(ArchiveSerializeType.Replication, powerResult.ArchiveData.ToByteArray()))
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
            return $"ArchiveData: {new MiniMapArchive(updateMiniMap.ArchiveData)}";
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
