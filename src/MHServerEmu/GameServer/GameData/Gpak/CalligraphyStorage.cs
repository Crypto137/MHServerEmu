using MHServerEmu.Common;
using MHServerEmu.GameServer.GameData.Gpak.FileFormats;

namespace MHServerEmu.GameServer.GameData.Gpak
{
    public class CalligraphyStorage : GpakStorage
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public Dictionary<string, DataDirectory> DataDirectoryDict { get; } = new();
        public Dictionary<string, GType> GTypeDict { get; } = new();
        public Dictionary<string, Curve> CurveDict { get; } = new();
        public Dictionary<string, Blueprint> BlueprintDict { get; } = new();
        public Dictionary<string, Prototype> DefaultsDict { get; } = new();     // defaults are parent prototypes
        public Dictionary<string, Prototype> PrototypeDict { get; } = new();

        public Dictionary<ulong, string> AssetDict { get; } = new();
        public Dictionary<ulong, string> AssetTypeDict { get; } = new();
        public Dictionary<ulong, string> PrototypeFieldDict { get; } = new();

        public CalligraphyStorage(GpakFile gpakFile)
        {
            // Sort GpakEntries by type
            List<GpakEntry> directoryList = new();
            List<GpakEntry> typeList = new();
            List<GpakEntry> curveList = new();
            List<GpakEntry> blueprintList = new();
            List<GpakEntry> defaultsList = new();
            List<GpakEntry> prototypeList = new();

            foreach (GpakEntry entry in gpakFile.Entries)
            {
                switch (Path.GetExtension(entry.FilePath))
                {
                    case ".directory":
                        directoryList.Add(entry);
                        break;
                    case ".type":
                        typeList.Add(entry);
                        break;
                    case ".curve":
                        curveList.Add(entry);
                        break;
                    case ".blueprint":
                        blueprintList.Add(entry);
                        break;
                    case ".defaults":
                        defaultsList.Add(entry);
                        break;
                    case ".prototype":
                        prototypeList.Add(entry);
                        break;
                }
            }

            // Parse all entries in order by type
            foreach (GpakEntry entry in directoryList)
                DataDirectoryDict.Add(entry.FilePath, new(entry.Data));

            Logger.Info($"Parsed {DataDirectoryDict.Count} directories");

            foreach (GpakEntry entry in typeList)
                GTypeDict.Add(entry.FilePath, new(entry.Data));

            Logger.Info($"Parsed {GTypeDict.Count} types");

            foreach (GpakEntry entry in curveList)
                CurveDict.Add(entry.FilePath, new(entry.Data));

            Logger.Info($"Parsed {CurveDict.Count} curves");

            foreach (GpakEntry entry in blueprintList)
                BlueprintDict.Add(entry.FilePath, new(entry.Data));

            Logger.Info($"Parsed {BlueprintDict.Count} blueprints");

            foreach (GpakEntry entry in defaultsList)
                DefaultsDict.Add(entry.FilePath, new(entry.Data));

            Logger.Info($"Parsed {DefaultsDict.Count} defaults");

            foreach (GpakEntry entry in prototypeList)
                PrototypeDict.Add(entry.FilePath, new(entry.Data));

            Logger.Info($"Parsed {PrototypeDict.Count} prototypes");

            // Asset dictionary
            AssetDict.Add(0, "0");  // add 0 manually
            AssetTypeDict.Add(0, "0");

            foreach (var kvp in GTypeDict)
            {
                foreach (GTypeEntry entry in kvp.Value.Entries)
                {
                    AssetDict.Add(entry.Id, entry.Name);
                    AssetTypeDict.Add(entry.Id, Path.GetFileNameWithoutExtension(kvp.Key));
                }
            }

            Logger.Info($"Loaded {AssetDict.Count} asset references");

            // Prototype fields
            foreach (var kvp in BlueprintDict)
                foreach (BlueprintField entry in kvp.Value.Fields)
                    PrototypeFieldDict.Add(entry.Id, entry.Name);
        }

        public override bool Verify()
        {
            return DataDirectoryDict.Count > 0
                && GTypeDict.Count > 0
                && CurveDict.Count > 0
                && BlueprintDict.Count > 0
                && DefaultsDict.Count > 0
                && PrototypeDict.Count > 0;
        }

        public override void Export()
        {
            // Prepare dictionaries
            Dictionary<ulong, string> prototypeDict = new() { { 0, "0" } }; // add 0 manually
            foreach (IDataDirectoryEntry entry in DataDirectoryDict["Calligraphy/Prototype.directory"].Entries)
                prototypeDict.Add(entry.Id1, entry.Name);

            Dictionary<ulong, string> curveDict = new();
            foreach (IDataDirectoryEntry entry in DataDirectoryDict["Calligraphy/Curve.directory"].Entries)
                curveDict.Add(entry.Id1, entry.Name);

            Dictionary<ulong, string> typeDict = new();
            foreach (IDataDirectoryEntry entry in DataDirectoryDict["Calligraphy/Type.directory"].Entries)
                typeDict.Add(entry.Id1, entry.Name);

            // Set up json serializer
            _jsonSerializerOptions.Converters.Add(new DataDirectoryEntryConverter());
            _jsonSerializerOptions.Converters.Add(new BlueprintConverter(prototypeDict, curveDict, typeDict));
            _jsonSerializerOptions.Converters.Add(new PrototypeConverter(prototypeDict, PrototypeFieldDict, curveDict, AssetDict, AssetTypeDict, typeDict));
            _jsonSerializerOptions.MaxDepth = 128;  // 64 is not enough for prototypes

            // Serialize and save
            SerializeDictAsJson(DataDirectoryDict);
            SerializeDictAsJson(GTypeDict);

            foreach (var kvp in CurveDict)  // use TSV for curves
            {
                string path = $"{Directory.GetCurrentDirectory()}\\Assets\\GPAK\\Export\\{kvp.Key}.tsv";
                string dir = Path.GetDirectoryName(path);
                if (Directory.Exists(dir) == false) Directory.CreateDirectory(dir);

                using (StreamWriter sw = new(path))
                {
                    foreach (double value in kvp.Value.Entries)
                        sw.WriteLine(value);
                }
            }

            SerializeDictAsJson(BlueprintDict);
            SerializeDictAsJson(DefaultsDict);
            SerializeDictAsJson(PrototypeDict);
        }
    }
}
