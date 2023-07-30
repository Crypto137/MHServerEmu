using System.Text;
using MHServerEmu.Common;

namespace MHServerEmu.GameServer.Data
{
    public static class Database
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static bool IsInitialized { get; private set; }
        public static Dictionary<ulong, Prototype> PrototypeDataDict { get; private set; }
        public static ulong[] GlobalEnumReferenceTable { get; private set; }
        public static ulong[] ResourceEnumReferenceTable { get; private set; }

        static Database()
        {
            Logger.Info("Loading prototypes...");
            PrototypeDataDict = LoadPrototypeData($"{Directory.GetCurrentDirectory()}\\Assets\\PrototypeDataTable.bin");
            GlobalEnumReferenceTable = LoadPrototypeEnumReferenceTable($"{Directory.GetCurrentDirectory()}\\Assets\\GlobalEnumReferenceTable.bin");
            ResourceEnumReferenceTable = LoadPrototypeEnumReferenceTable($"{Directory.GetCurrentDirectory()}\\Assets\\ResourceEnumReferenceTable.bin");

            if (PrototypeDataDict.Count > 0 && GlobalEnumReferenceTable.Length > 0 && ResourceEnumReferenceTable.Length > 0)
            {
                // -1 is here because the first entry is 0 to offset values and align with the data we get from the game
                Logger.Info($"Loaded {PrototypeDataDict.Count} prototypes, {GlobalEnumReferenceTable.Length - 1} global enum references, and {ResourceEnumReferenceTable.Length - 1} resource enum references");
                IsInitialized = true;
            }
            else
            {
                Logger.Fatal("Failed to initialize database");
                IsInitialized = false;
            }
        }

        private static Dictionary<ulong, Prototype> LoadPrototypeData(string path)
        {
            Dictionary<ulong, Prototype> prototypeDict = new();

            if (File.Exists(path))
            {
                using (MemoryStream memoryStream = new(File.ReadAllBytes(path)))
                using (BinaryReader binaryReader = new(memoryStream))
                {
                    while (memoryStream.Position < memoryStream.Length)
                    {
                        ulong id = binaryReader.ReadUInt64();
                        ulong field1 = binaryReader.ReadUInt64();
                        ulong parentId = binaryReader.ReadUInt64();
                        byte flag = binaryReader.ReadByte();
                        byte size = binaryReader.ReadByte();
                        binaryReader.ReadByte();                // always 0x00
                        string stringValue = Encoding.UTF8.GetString(binaryReader.ReadBytes(size));

                        prototypeDict.Add(id, new(id, field1, parentId, flag, stringValue));
                    }
                }
            }
            else
            {
                Logger.Error($"Failed to locate {Path.GetFileName(path)}");
            }

            /*
            using (StreamWriter streamWriter = new($"{Directory.GetCurrentDirectory()}\\parsed.tsv"))
            {
                foreach (KeyValuePair<ulong, Prototype> kvp in prototypeDict)
                {
                    streamWriter.WriteLine($"{kvp.Value.Id}\t{kvp.Value.Field1}\t{kvp.Value.ParentId}\t{kvp.Value.Flag}\t{kvp.Value.StringValue}");
                }

                streamWriter.Flush();
            }
            */

            return prototypeDict;
        }

        private static ulong[] LoadPrototypeEnumReferenceTable(string path)
        {
            if (File.Exists(path))
            {
                using (MemoryStream memoryStream = new(File.ReadAllBytes(path)))
                using (BinaryReader binaryReader = new(memoryStream))
                {
                    ulong[] prototypes = new ulong[memoryStream.Length / 8];
                    for (int i = 0; i < prototypes.Length; i++) prototypes[i] = binaryReader.ReadUInt64();
                    return prototypes;
                }
            }
            else
            {
                Logger.Error($"Failed to locate {Path.GetFileName(path)}");
                return Array.Empty<ulong>();
            }
        }
    }
}
