using MHServerEmu.Common;
using MHServerEmu.GameServer.GameData.Gpak;
using MHServerEmu.GameServer.Properties;

namespace MHServerEmu.GameServer.GameData
{
    public static class GameDatabase
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly string AssetDirectory = $"{Directory.GetCurrentDirectory()}\\Assets";

        private static HashMap _prototypeHashMap;

        public static bool IsInitialized { get; private set; }

        public static CalligraphyStorage Calligraphy { get; private set; }
        public static ResourceStorage Resource { get; private set; }

        public static Dictionary<ulong, string> AssetDict { get; private set; }
        public static PropertyInfo[] PropertyInfoTable { get; private set; }

        public static PrototypeEnumManager PrototypeEnumManager { get; private set; }

        static GameDatabase()
        {
            long startTime = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();

            AssetDict = LoadDictionary($"{AssetDirectory}\\AssetDictionary.tsv");   // Load this first because it's needed for initializing Calligraphy

            // Initialize GPAK
            Calligraphy = new(new("Calligraphy.sip"));
            Resource = new(new("mu_cdata.sip"));

            // Load other data
            _prototypeHashMap = LoadHashMap($"{AssetDirectory}\\PrototypeHashMap.tsv");
            PropertyInfoTable = LoadPropertyInfoTable($"{AssetDirectory}\\PropertyInfoTable.tsv");
            PrototypeEnumManager = new($"{AssetDirectory}\\PrototypeEnumTables");

            // Verify and finish game database initialization
            if (VerifyData())
            {
                long loadTime = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds() - startTime;
                Logger.Info($"Finished initializing game database in {loadTime} ms");
                IsInitialized = true;
            }
            else
            {
                Logger.Fatal("Failed to initialize game database");
                IsInitialized = false;
            }
        }

        public static void ExtractGpakEntries()
        {
            Logger.Info("Extracting Calligraphy entries...");
            GpakFile calligraphyFile = new("Calligraphy.sip", true);
            calligraphyFile.ExtractEntries("Calligraphy.tsv");

            Logger.Info("Extracting Resource entries...");
            GpakFile resourceFile = new("mu_cdata.sip", true);
            resourceFile.ExtractEntries("mu_cdata.tsv");
        }

        public static void ExtractGpakData()
        {
            Logger.Info("Extracting Calligraphy data...");
            GpakFile calligraphyFile = new("Calligraphy.sip", true);
            calligraphyFile.ExtractData();

            Logger.Info("Extracting Resource data...");
            GpakFile resourceFile = new("mu_cdata.sip", true);
            resourceFile.ExtractData();
        }

        public static string GetPrototypePath(ulong id) => _prototypeHashMap.GetForward(id);
        public static ulong GetPrototypeId(string path) => _prototypeHashMap.GetReverse(path);

        private static HashMap LoadHashMap(string path)
        {
            HashMap hashMap = new();

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
                            hashMap.Add(ulong.Parse(values[0]), values[1]);
                        }

                        line = streamReader.ReadLine();
                    }
                }
            }
            else
            {
                Logger.Warn($"Failed to locate {Path.GetFileName(path)}");
            }

            return hashMap;
        }

        private static Dictionary<ulong, string> LoadDictionary(string path)
        {
            Dictionary<ulong, string> dict = new();

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
                            dict.Add(ulong.Parse(values[0]), values[1]);
                        }

                        line = streamReader.ReadLine();
                    }
                }
            }
            else
            {
                Logger.Warn($"Failed to locate {Path.GetFileName(path)}");
            }

            return dict;
        }

        private static PropertyInfo[] LoadPropertyInfoTable(string path)
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

        private static bool VerifyData()
        {
            return _prototypeHashMap.Count > 0
                && Calligraphy.Verify()
                && Resource.Verify()
                && AssetDict.Count > 0
                && PropertyInfoTable.Length > 0
                && PrototypeEnumManager.Verify();
        }
    }
}
