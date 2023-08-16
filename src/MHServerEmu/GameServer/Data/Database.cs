using System.Text;
using MHServerEmu.Common;
using MHServerEmu.GameServer.Data.Gpak;

namespace MHServerEmu.GameServer.Data
{
    public enum HashMapType
    {
        Blueprint,
        Curve,
        Prototype,
        Type
    }

    public static class Database
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly string AssetDirectory = $"{Directory.GetCurrentDirectory()}\\Assets";

        public static bool IsInitialized { get; private set; }
        public static GpakFile CalligraphyFile { get; private set; }
        public static GpakFile ResourceFile { get; private set; }

        public static Dictionary<HashMapType, HashMap> HashMapDict { get; private set; }

        public static PropertyInfo[] PropertyInfos { get; private set; }

        public static ulong[] GlobalEnumRefTable { get; private set; }
        public static ulong[] ResourceEnumRefTable { get; private set; }
        public static ulong[] PropertyIdPowerRefTable { get; private set; }

        static Database()
        {
            long startTime = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();

            CalligraphyFile = new("Calligraphy.sip");
            ResourceFile = new("mu_cdata.sip");
            Calligraphy.Initialize(CalligraphyFile);
            Resource.Initialize(ResourceFile);

            HashMapDict = LoadHashMapDict();

            PropertyInfos = LoadPropertyInfos($"{AssetDirectory}\\PropertyInfoTable.tsv");

            GlobalEnumRefTable = LoadPrototypeEnumRefTable($"{AssetDirectory}\\GlobalEnumRefTable.bin");
            ResourceEnumRefTable = LoadPrototypeEnumRefTable($"{AssetDirectory}\\ResourceEnumRefTable.bin");
            PropertyIdPowerRefTable = LoadPrototypeEnumRefTable($"{AssetDirectory}\\PropertyIdPowerRefTable.bin");

            if (VerifyData())
            {
                long loadTime = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds() - startTime;
                Logger.Info($"Finished loading in {loadTime} ms");
                IsInitialized = true;
            }
            else
            {
                Logger.Fatal("Failed to initialize database");
                IsInitialized = false;
            }
        }

        public static void ExportGpakEntries()
        {
            Logger.Info("Exporting Calligraphy entries...");
            CalligraphyFile.ExportEntries("Calligraphy.tsv");
            Logger.Info("Exporting Resource entries...");
            ResourceFile.ExportEntries("mu_cdata.tsv");
            Logger.Info("Finished exporting GPAK entries");
        }

        public static void ExportGpakData()
        {
            Logger.Info("Exporting Calligraphy data...");
            CalligraphyFile.ExportData();
            Logger.Info("Exporting Resource data...");
            ResourceFile.ExportData();
            Logger.Info("Finished exporting GPAK data");
        }

        public static string GetPrototypePath(ulong id) => HashMapDict[HashMapType.Prototype].ForwardDict[id];
        public static ulong GetPrototypeId(string path) => HashMapDict[HashMapType.Prototype].ReverseDict[path];

        private static Dictionary<HashMapType, HashMap> LoadHashMapDict()
        {
            Dictionary<HashMapType, HashMap> hashMapDict = new();

            foreach (string name in Enum.GetNames(typeof(HashMapType)))
            {
                string path = $"{AssetDirectory}\\Hashmaps\\{name}s.tsv";

                if (File.Exists(path))
                {
                    HashMap hashMap = new();

                    using (StreamReader streamReader = new(path))
                    {
                        string line = streamReader.ReadLine();

                        while (line != null)
                        {
                            if (line != "")
                            {
                                string[] values = line.Split("\t");
                                hashMap.Add(ulong.Parse(values[0]), values[1]);
                            }

                            line = streamReader.ReadLine();
                        }
                    }

                    hashMapDict.Add((HashMapType)Enum.Parse(typeof(HashMapType), name), hashMap);
                }
                else
                {
                    Logger.Warn($"Failed to locate {Path.GetFileName(path)}");
                }
            }

            return hashMapDict;
        }

        private static PropertyInfo[] LoadPropertyInfos(string path)
        {
            List<PropertyInfo> propertyInfoList = new();

            if (File.Exists(path))
            {
                using (StreamReader streamReader = new(path))
                {
                    string line = streamReader.ReadLine();

                    while (line != null)
                    {
                        if (line != "")
                        {
                            string[] values = line.Split("\t");
                            propertyInfoList.Add(new(values[0], (PropertyValueType)Enum.Parse(typeof(PropertyValueType), values[1])));
                        }

                        line = streamReader.ReadLine();
                    }
                }
            }
            else
            {
                Logger.Error($"Failed to locate {Path.GetFileName(path)}");
            }

            return propertyInfoList.ToArray();
        }

        private static ulong[] LoadPrototypeEnumRefTable(string path)
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

        private static bool VerifyData()
        {
            return CalligraphyFile.Entries.Length > 0
                && ResourceFile.Entries.Length > 0
                && HashMapDict.Count > 0
                && PropertyInfos.Length > 0
                && GlobalEnumRefTable.Length > 0
                && ResourceEnumRefTable.Length > 0
                && PropertyIdPowerRefTable.Length > 0;
        }
    }
}
