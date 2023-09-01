using MHServerEmu.Common;
using MHServerEmu.GameServer.Entities;
using MHServerEmu.GameServer.GameData.Gpak.FileFormats;

namespace MHServerEmu.GameServer.GameData.Gpak
{
    public class CalligraphyStorage : GpakStorage
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public DataDirectory GTypeDirectory { get; }
        public DataDirectory CurveDirectory { get; }
        public DataDirectory BlueprintDirectory { get; }
        public DataDirectory PrototypeDirectory { get; }
        public DataDirectory ReplacementDirectory { get; }

        public Dictionary<string, GType> GTypeDict { get; } = new();
        public Dictionary<string, Curve> CurveDict { get; } = new();
        public Dictionary<string, Blueprint> BlueprintDict { get; } = new();
        public Dictionary<string, Prototype> PrototypeDict { get; } = new();

        public Dictionary<ulong, string> AssetDict { get; } = new();
        public Dictionary<ulong, string> AssetTypeDict { get; } = new();
        public Dictionary<ulong, string> PrototypeFieldDict { get; } = new();

        public CalligraphyStorage(GpakFile gpakFile)
        {
            var gpakDict = gpakFile.ToDictionary();

            GTypeDirectory = new(gpakDict["Calligraphy/Type.directory"]);
            CurveDirectory = new(gpakDict["Calligraphy/Curve.directory"]);
            BlueprintDirectory = new(gpakDict["Calligraphy/Blueprint.directory"]);
            PrototypeDirectory = new(gpakDict["Calligraphy/Prototype.directory"]);
            ReplacementDirectory = new(gpakDict["Calligraphy/Replacement.directory"]);

            foreach (DataDirectoryGenericEntry entry in GTypeDirectory.Entries)
                GTypeDict.Add(entry.FilePath, new(gpakDict[$"Calligraphy/{entry.FilePath}"]));

            Logger.Info($"Parsed {GTypeDict.Count} types");

            foreach (DataDirectoryGenericEntry entry in CurveDirectory.Entries)
                CurveDict.Add(entry.FilePath, new(gpakDict[$"Calligraphy/{entry.FilePath}"]));

            Logger.Info($"Parsed {CurveDict.Count} curves");

            foreach (DataDirectoryGenericEntry entry in BlueprintDirectory.Entries)
                BlueprintDict.Add(entry.FilePath, new(gpakDict[$"Calligraphy/{entry.FilePath}"]));

            Logger.Info($"Parsed {BlueprintDict.Count} blueprints");

            foreach (DataDirectoryPrototypeEntry entry in PrototypeDirectory.Entries)
                PrototypeDict.Add(entry.FilePath, new(gpakDict[$"Calligraphy/{entry.FilePath}"]));

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
            return GTypeDirectory.Entries.Length > 0
                && CurveDirectory.Entries.Length > 0
                && BlueprintDirectory.Entries.Length > 0
                && PrototypeDirectory.Entries.Length > 0
                && ReplacementDirectory.Entries.Length > 0
                && GTypeDict.Count > 0
                && CurveDict.Count > 0
                && BlueprintDict.Count > 0
                && PrototypeDict.Count > 0;
        }

        public override void Export()
        {
            // Set up json serializer
            _jsonSerializerOptions.Converters.Add(new BlueprintConverter(PrototypeDirectory, CurveDirectory, GTypeDirectory));
            _jsonSerializerOptions.Converters.Add(new PrototypeConverter(PrototypeDirectory, CurveDirectory, GTypeDirectory, PrototypeFieldDict, AssetDict, AssetTypeDict));
            _jsonSerializerOptions.MaxDepth = 128;  // 64 is not enough for prototypes

            // Serialize and save
            ExportDataDirectories();
            SerializeDictAsJson(GTypeDict);
            ExportCurveDict();
            SerializeDictAsJson(BlueprintDict);
            SerializeDictAsJson(PrototypeDict);
        }

        public Prototype GetBlueprintPrototype(string path)
        {
            if (BlueprintDict.ContainsKey(path))
            {
                string prototypePath = PrototypeDirectory.EntryDict[BlueprintDict[path].PrototypeId].FilePath;
                return PrototypeDict[prototypePath];
            }
            else
            {
                Logger.Warn($"Cannot find blueprint {path}");
                return null;
            }
        }

        private void ExportDataDirectories()
        {
            string dir = $"{Directory.GetCurrentDirectory()}\\Assets\\GPAK\\Export\\Calligraphy\\";
            if (Directory.Exists(dir) == false) Directory.CreateDirectory(dir);

            using (StreamWriter writer = new($"{dir}\\Type.directory.tsv"))
            {
                foreach (DataDirectoryGenericEntry entry in GTypeDirectory.Entries)
                    writer.WriteLine($"{entry.Id1}\t{entry.Id2}\t{entry.Field2}\t{entry.FilePath}");
            }

            using (StreamWriter writer = new($"{dir}\\Curve.directory.tsv"))
            {
                foreach (DataDirectoryGenericEntry entry in CurveDirectory.Entries)
                    writer.WriteLine($"{entry.Id1}\t{entry.Id2}\t{entry.Field2}\t{entry.FilePath}");
            }

            using (StreamWriter writer = new($"{dir}\\Blueprint.directory.tsv"))
            {
                foreach (DataDirectoryGenericEntry entry in BlueprintDirectory.Entries)
                    writer.WriteLine($"{entry.Id1}\t{entry.Id2}\t{entry.Field2}\t{entry.FilePath}");
            }

            using (StreamWriter writer = new($"{dir}\\Prototype.directory.tsv"))
            {
                foreach (DataDirectoryPrototypeEntry entry in PrototypeDirectory.Entries)
                    writer.WriteLine($"{entry.Id1}\t{entry.Id2}\t{entry.ParentId}\t{entry.Field3}\t{entry.FilePath}");
            }

            using (StreamWriter writer = new($"{dir}\\Replacement.directory.tsv"))
            {
                foreach (DataDirectoryReplacementEntry entry in ReplacementDirectory.Entries)
                    writer.WriteLine($"{entry.Id1}\t{entry.Id2}\t{entry.FilePath}");
            }
        }

        private void ExportCurveDict()
        {
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
        }
    }
}
