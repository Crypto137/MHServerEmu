using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.MetaGames;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Network
{
    public static class PacketHelper
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private static readonly string PacketDirectory = Path.Combine(FileHelper.DataDirectory, "Packets");

        public static bool ParseServerMessagesFromPacketFile(string fileName)
        {
            string path = Path.Combine(PacketDirectory, fileName);

            if (File.Exists(path) == false)
                return Logger.WarnReturn(false, $"ParseServerMessagesFromPacketFile(): {path} not found");

            MuxPacket packet;
            using (MemoryStream ms = new(File.ReadAllBytes(path)))
                packet = new(ms);

            if (packet.IsDataPacket)
                ParseServerMessagesFromPacket(packet, Path.Combine(PacketDirectory, $"{Path.GetFileNameWithoutExtension(path)}_parsed.txt"));

            return true;
        }

        public static void ParseServerMessagesFromAllPacketFiles()
        {
            string[] files = Directory.GetFiles(PacketDirectory);
            int packetCount = 0;

            foreach (string file in files)
            {
                if (Path.GetExtension(file) != ".bin") continue;     // ignore previous parses and other files

                //Logger.Info($"Parsing {file}...");
                ParseServerMessagesFromPacketFile(Path.GetFileName(file));
                packetCount++;
            }

            Logger.Info($"Finished parsing {packetCount} packet files");
        }

        public static bool ParseServerMessagesFromPacketDump(string fileName)
        {
            string path = Path.Combine(PacketDirectory, fileName);

            if (File.Exists(path) == false)
                return Logger.WarnReturn(false, $"ParseServerMessagesFromPacketDump(): {path} not found");

            using (MemoryStream ms = new(File.ReadAllBytes(path)))
            {
                int packetCount = 0;

                while (ms.Position < ms.Length)
                {
                    MuxPacket packet = new(ms);
                    if (packet.IsDataPacket == false) continue;

                    ParseServerMessagesFromPacket(packet, Path.Combine(PacketDirectory, $"{Path.GetFileNameWithoutExtension(path)}_packet{packetCount}_parsed.txt"));
                    packetCount++;
                }
            }

            return true;
        }

        public static bool ExtractMessagePacketsFromDump(string fileName)
        {
            string path = Path.Combine(PacketDirectory, fileName);

            if (File.Exists(path) == false)
                return Logger.WarnReturn(false, $"ExtractMessagePacketsFromDump(): {path} not found");

            using (MemoryStream ms = new(File.ReadAllBytes(path)))
            {
                int packetCount = 0;

                while (ms.Position < ms.Length)
                {
                    MuxPacket packet = new(ms);
                    if (packet.IsDataPacket == false) continue;

                    byte[] rawPacket = packet.Serialize();
                    File.WriteAllBytes(Path.Combine(PacketDirectory, $"{Path.GetFileNameWithoutExtension(path)}_packet{packetCount}_raw.bin"), rawPacket);
                    packetCount++;
                }
            }

            return true;
        }

        public static IEnumerable<MessagePackage> LoadMessagesFromPacketFile(string fileName)
        {
            string path = Path.Combine(PacketDirectory, fileName);

            if (File.Exists(path) == false)
                return Logger.WarnReturn(Array.Empty<MessagePackage>(), $"LoadMessagesFromPacketFile(): {fileName} not found");

            using (MemoryStream ms = new(File.ReadAllBytes(path)))
            {
                MuxPacket packet = new(ms);
                Logger.Info($"Loaded {packet.Messages.Count()} messages from {fileName}");
                return packet.Messages;
            }
        }

        private static void ParseServerMessagesFromPacket(MuxPacket packet, string outputPath)
        {
            using (StreamWriter writer = new(outputPath))
            {
                int packetCount = 0;

                foreach (MessagePackage message in packet.Messages)
                {
                    writer.Write($"[{packetCount++}] ");

                    string messageName = packet.MuxId == 1
                        ? ((GameServerToClientMessage)message.Id).ToString()
                        : ((GroupingManagerMessage)message.Id).ToString();
                    //Logger.Trace($"Deserializing {messageName}...");
                    writer.WriteLine(messageName);

                    try
                    {
                        message.Protocol = packet.MuxId == 1
                                ? typeof(GameServerToClientMessage)
                                : typeof(GroupingManagerMessage);

                        IMessage protobufMessage = message.Deserialize();

                        switch (protobufMessage)
                        {
                            default:
                                writer.WriteLine(protobufMessage);
                                break;

                            case NetMessageEntityCreate entityCreate:
                                // Parse base data
                                EntityBaseData baseData = new(entityCreate.BaseData);
                                writer.WriteLine($"BaseData: {baseData}");

                                // Get blueprint for this entity
                                Blueprint blueprint = GameDatabase.DataDirectory.GetPrototypeBlueprint(baseData.PrototypeId);
                                writer.WriteLine($"Blueprint: {GameDatabase.GetBlueprintName(blueprint.Id)} (bound to {blueprint.RuntimeBindingClassType.Name})");

                                // Parse entity depending on its blueprint class
                                switch (blueprint.RuntimeBindingClassType.Name)
                                {
                                    case "EntityPrototype":
                                        writer.WriteLine($"ArchiveData: {new Entity(baseData, entityCreate.ArchiveData)}");
                                        break;

                                    case "WorldEntityPrototype":
                                    case "PropPrototype":
                                    case "DestructiblePropPrototype":
                                    case "SpawnerPrototype":
                                        writer.WriteLine($"ArchiveData: {new WorldEntity(baseData, entityCreate.ArchiveData)}");
                                        break;

                                    case "HotspotPrototype":
                                        writer.WriteLine($"ArchiveData: {new Hotspot(baseData, entityCreate.ArchiveData)}");
                                        break;

                                    case "AgentPrototype":
                                    case "AgentTeamUpPrototype":
                                    case "OrbPrototype":
                                    case "SmartPropPrototype":
                                        writer.WriteLine($"ArchiveData: {new Agent(baseData, entityCreate.ArchiveData)}");
                                        break;

                                    case "MissilePrototype":
                                        writer.WriteLine($"ArchiveData: {new Missile(baseData, entityCreate.ArchiveData)}");
                                        break;

                                    case "AvatarPrototype":
                                        writer.WriteLine($"ArchiveData: {new Avatar(baseData, entityCreate.ArchiveData)}");
                                        break;

                                    case "KismetSequenceEntityPrototype":
                                        writer.WriteLine($"ArchiveData: {new KismetSequenceEntity(baseData, entityCreate.ArchiveData)}");
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
                                        writer.WriteLine($"ArchiveData: {new Item(baseData, entityCreate.ArchiveData)}");
                                        break;

                                    case "PlayerPrototype":
                                        writer.WriteLine($"ArchiveData: {new Player(baseData, entityCreate.ArchiveData)}");
                                        break;

                                    case "TransitionPrototype":
                                        writer.WriteLine($"ArchiveData: {new Transition(baseData, entityCreate.ArchiveData)}");
                                        break;

                                    case "MetaGamePrototype":
                                    case "MatchMetaGamePrototype":
                                        writer.WriteLine($"ArchiveData: {new MetaGame(baseData, entityCreate.ArchiveData)}");
                                        break;

                                    case "PvPPrototype":
                                        writer.WriteLine($"ArchiveData: {new PvP(baseData, entityCreate.ArchiveData)}");
                                        break;

                                    case "MissionMetaGamePrototype":
                                        writer.WriteLine($"ArchiveData: {new MissionMetaGame(baseData, entityCreate.ArchiveData)}");
                                        break;

                                    default:
                                        writer.WriteLine($"ArchiveData: unsupported entity ({blueprint.RuntimeBindingClassType.Name})");
                                        break;
                                }

                                break;

                            case NetMessageRegionChange regionChange:
                                writer.WriteLine(protobufMessage);
                                if (regionChange.ArchiveData.Length > 0)
                                {
                                    using (Archive archive = new(ArchiveSerializeType.Replication, regionChange.ArchiveData.ToByteArray()))
                                    {
                                        RegionArchive regionArchive = new();
                                        regionArchive.Serialize(archive);
                                        writer.WriteLine($"ArchiveData: {regionArchive}");
                                    }
                                }
                                    
                                break;

                            case NetMessageEntityEnterGameWorld entityEnterGameWorld:
                                writer.WriteLine($"ArchiveData: {new EnterGameWorldArchive(entityEnterGameWorld.ArchiveData)}");
                                break;

                            case NetMessageLocomotionStateUpdate locomotionStateUpdate:
                                writer.WriteLine($"ArchiveData: {new LocomotionStateUpdateArchive(locomotionStateUpdate.ArchiveData)}");
                                break;

                            case NetMessageActivatePower activatePower:
                                using (Archive archive = new(ArchiveSerializeType.Replication, activatePower.ArchiveData.ToByteArray()))
                                {
                                    ActivatePowerArchive activatePowerArchive = new();
                                    activatePowerArchive.ReplicationPolicy = archive.GetReplicationPolicyEnum();
                                    activatePowerArchive.Serialize(archive);
                                    writer.WriteLine($"ArchiveData: {activatePowerArchive}");
                                }
                                break;

                            case NetMessagePowerResult powerResult:
                                writer.WriteLine($"ArchiveData: {new PowerResultArchive(powerResult.ArchiveData)}");
                                break;

                            case NetMessageAddCondition addCondition:
                                writer.WriteLine($"ArchiveData: {new AddConditionArchive(addCondition.ArchiveData)}");
                                break;

                            case NetMessageSetProperty setProperty:
                                PropertyId propertyId = new(setProperty.PropertyId.ReverseBits());
                                PropertyInfo propertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyId.Enum);
                                PropertyValue propertyValue = PropertyCollection.ConvertBitsToValue(setProperty.ValueBits, propertyInfo.DataType);
                                writer.WriteLine($"({setProperty.ReplicationId}) {propertyId}: {propertyValue.Print(propertyInfo.DataType)}");
                                break;

                            case NetMessageRemoveProperty removeProperty:
                                writer.WriteLine($"({removeProperty.ReplicationId}) {new PropertyId(removeProperty.PropertyId.ReverseBits())}");
                                break;

                            case NetMessageUpdateMiniMap updateMiniMap:
                                writer.WriteLine($"ArchiveData: {new MiniMapArchive(updateMiniMap.ArchiveData)}");
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Warn($"Failed to deserialize {messageName} ({Path.GetFileName(outputPath)})");
                        Logger.Trace(e.ToString());
                        writer.WriteLine("Failed to deserialize");
                    }

                    writer.WriteLine();
                    writer.WriteLine();
                }
            }
        }
    }
}
