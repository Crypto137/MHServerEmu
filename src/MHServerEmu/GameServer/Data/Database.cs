using System.Text;
using MHServerEmu.Common;

namespace MHServerEmu.GameServer.Data
{
    public static class Database
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static bool IsInitialized { get; private set; }
        public static Prototype[] PrototypeDataTable { get; private set; }
        public static ulong[] PrototypeEnumReferenceTable { get; private set; }

        static Database()
        {
            Logger.Info("Loading prototypes...");
            PrototypeDataTable = LoadPrototypeDataTable($"{Directory.GetCurrentDirectory()}\\Assets\\PrototypeDataTable.bin");
            PrototypeEnumReferenceTable = LoadPrototypeEnumReferenceTable($"{Directory.GetCurrentDirectory()}\\Assets\\PrototypeEnumReferenceTable.bin");

            if (PrototypeEnumReferenceTable.Length > 0 && PrototypeDataTable.Length > 0)
            {
                // -1 is here because the first entry is 0 to offset values and align with the data we get from the game
                Logger.Info($"Loaded {PrototypeDataTable.Length} prototypes and {PrototypeEnumReferenceTable.Length - 1} enum references");
                IsInitialized = true;
            }
            else
            {
                Logger.Error("Failed to initialize");
                IsInitialized = false;
            }
        }

        private static Prototype[] LoadPrototypeDataTable(string path)
        {
            List<Prototype> prototypeList = new();

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

                        prototypeList.Add(new(id, field1, parentId, flag, stringValue));
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
                foreach (Prototype prototype in prototypeList)
                {
                    streamWriter.WriteLine($"{prototype.Id}\t{prototype.Field1}\t{prototype.Parent}\t{prototype.Flag}\t{prototype.StringValue}");
                }

                streamWriter.Flush();
            }
            */

            return prototypeList.ToArray();
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
