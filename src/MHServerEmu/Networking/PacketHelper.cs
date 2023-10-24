using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.MetaGame;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Networking
{
    public static class PacketHelper
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private static readonly string PacketDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Packets");

        public static void ParseServerMessagesFromPacketFile(string fileName)
        {
            string path = Path.Combine(PacketDirectory, fileName);

            if (File.Exists(path) == false)
            {
                Logger.Warn($"{path} not found");
                return;
            }

            CodedInputStream stream = CodedInputStream.CreateInstance(File.ReadAllBytes(path));
            PacketIn packet = new(stream);

            if (packet.Command == MuxCommand.Data)
                ParseServerMessagesFromPacket(packet, Path.Combine(PacketDirectory, $"{Path.GetFileNameWithoutExtension(path)}_parsed.txt"));
        }

        public static void ParseServerMessagesFromAllPacketFiles()
        {
            string[] files = Directory.GetFiles(PacketDirectory);
            int packetCount = 0;

            foreach (string file in files)
            {
                if (Path.GetExtension(file) == ".bin")     // ignore previous parses and other files
                {
                    //Logger.Info($"Parsing {file}...");
                    ParseServerMessagesFromPacketFile(Path.GetFileName(file));
                    packetCount++;
                }
            }

            Logger.Info($"Finished parsing {packetCount} packet files");
        }

        public static void ParseServerMessagesFromPacketDump(string fileName)
        {
            string path = Path.Combine(PacketDirectory, fileName);

            if (File.Exists(path) == false)
            {
                Logger.Warn($"{path} not found");
                return;
            }

            CodedInputStream stream = CodedInputStream.CreateInstance(File.ReadAllBytes(path));
            int packetCount = 0;

            while (!stream.IsAtEnd)
            {
                PacketIn packet = new(stream);

                if (packet.Command == MuxCommand.Data)
                {
                    ParseServerMessagesFromPacket(packet, Path.Combine(PacketDirectory, $"{Path.GetFileNameWithoutExtension(path)}_packet{packetCount}_parsed.txt"));
                    packetCount++;
                }
            }
        }

        public static void ExtractMessagePacketsFromDump(string fileName)
        {
            string path = Path.Combine(PacketDirectory, fileName);

            if (File.Exists(path) == false)
            {
                Logger.Warn($"{path} not found");
                return;
            }

            CodedInputStream stream = CodedInputStream.CreateInstance(File.ReadAllBytes(path));
            int packetCount = 0;

            while (!stream.IsAtEnd)
            {
                PacketIn packet = new(stream);

                if (packet.Command == MuxCommand.Data)
                {
                    byte[] rawPacket = packet.ToPacketOut().Data;
                    File.WriteAllBytes(Path.Combine(PacketDirectory, $"{Path.GetFileNameWithoutExtension(path)}_packet{packetCount}_raw.bin"), rawPacket);
                    packetCount++;
                }
            }
        }

        public static GameMessage[] LoadMessagesFromPacketFile(string fileName)
        {
            string path = Path.Combine(PacketDirectory, fileName);

            if (File.Exists(path) == false)
            {
                Logger.Warn($"{fileName} not found");
                return Array.Empty<GameMessage>();
            }

            CodedInputStream stream = CodedInputStream.CreateInstance(File.ReadAllBytes(path));
            PacketIn packet = new(stream);
            Logger.Info($"Loaded {packet.Messages.Length} messages from {fileName}");
            return packet.Messages;
        }

        private static void ParseServerMessagesFromPacket(PacketIn packet, string outputPath)
        {
            using (StreamWriter writer = new(outputPath))
            {
                int packetCount = 0;

                foreach (GameMessage message in packet.Messages)
                {
                    writer.Write($"[{packetCount++}] ");

                    string messageName = (packet.MuxId == 1) 
                        ? ((GameServerToClientMessage)message.Id).ToString()
                        : ((GroupingManagerMessage)message.Id).ToString();
                    //Logger.Trace($"Deserializing {messageName}...");
                    writer.WriteLine(messageName);

                    try
                    {
                        IMessage protobufMessage = (packet.MuxId == 1)
                                ? message.Deserialize(typeof(GameServerToClientMessage))
                                : message.Deserialize(typeof(GroupingManagerMessage));

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
                                writer.WriteLine($"Blueprint: {blueprint.RuntimeBinding}");

                                // Parse entity depending on its blueprint class
                                switch (blueprint.RuntimeBinding)
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
                                        writer.WriteLine($"ArchiveData: unsupported entity ({blueprint.RuntimeBinding})");
                                        break;
                                }
                                
                                break;

                            case NetMessageRegionChange regionChange:
                                writer.WriteLine(protobufMessage);
                                if (regionChange.ArchiveData.Length > 0)
                                    writer.WriteLine($"ArchiveDataHex: {new RegionArchive(regionChange.ArchiveData)}");
                                break;

                            case NetMessageEntityEnterGameWorld entityEnterGameWorld:
                                writer.WriteLine($"ArchiveData: {new EnterGameWorldArchive(entityEnterGameWorld.ArchiveData)}");
                                break;

                            case NetMessageLocomotionStateUpdate locomotionStateUpdate:
                                writer.WriteLine($"ArchiveData: {new LocomotionStateUpdateArchive(locomotionStateUpdate.ArchiveData)}");
                                break;

                            case NetMessageActivatePower activatePower:
                                writer.WriteLine($"ArchiveData: {new ActivatePowerArchive(activatePower.ArchiveData)}");
                                break;

                            case NetMessagePowerResult powerResult:
                                writer.WriteLine($"ArchiveData: {new PowerResultArchive(powerResult.ArchiveData)}");
                                break;

                            case NetMessageAddCondition addCondition:
                                writer.WriteLine($"ArchiveData: {new AddConditionArchive(addCondition.ArchiveData)}");
                                break;

                            case NetMessageSetProperty setProperty:
                                writer.WriteLine($"ReplicationId: {setProperty.ReplicationId}\n{new Property(setProperty)}");
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
