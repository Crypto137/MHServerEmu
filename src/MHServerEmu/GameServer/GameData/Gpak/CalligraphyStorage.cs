using MHServerEmu.Common;
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

        public Dictionary<Prototype, Blueprint> PrototypeBlueprintDict { get; }     // .defaults prototype -> blueprint
        public Dictionary<ulong, string> AssetDict { get; } = new();                // asset id -> name
        public Dictionary<ulong, string> AssetTypeDict { get; } = new();            // asset id -> type
        public Dictionary<ulong, string> PrototypeFieldDict { get; } = new();       // blueprint entry key -> field name

        public CalligraphyStorage(GpakFile gpakFile)
        {
            // Convert GPAK file to a dictionary for easy access to all of its entries
            var gpakDict = gpakFile.ToDictionary();

            // Initialize directories
            GTypeDirectory = new(gpakDict["Calligraphy/Type.directory"]);
            CurveDirectory = new(gpakDict["Calligraphy/Curve.directory"]);
            BlueprintDirectory = new(gpakDict["Calligraphy/Blueprint.directory"]);
            PrototypeDirectory = new(gpakDict["Calligraphy/Prototype.directory"]);
            ReplacementDirectory = new(gpakDict["Calligraphy/Replacement.directory"]);

            // Populate directories with data from GPAK
            // GType
            foreach (DataDirectoryGTypeEntry entry in GTypeDirectory.Entries)
                entry.GType = new(gpakDict[$"Calligraphy/{entry.FilePath}"]);
            Logger.Info($"Parsed {GTypeDirectory.Entries.Length} types");

            // Curve
            foreach (DataDirectoryCurveEntry entry in CurveDirectory.Entries)
                entry.Curve = new(gpakDict[$"Calligraphy/{entry.FilePath}"]);
            Logger.Info($"Parsed {CurveDirectory.Entries.Length} curves");

            // Blueprint
            foreach (DataDirectoryBlueprintEntry entry in BlueprintDirectory.Entries)
                entry.Blueprint = new(gpakDict[$"Calligraphy/{entry.FilePath}"]);
            Logger.Info($"Parsed {BlueprintDirectory.Entries.Length} blueprints");

            // Prototype
            foreach (DataDirectoryPrototypeEntry entry in PrototypeDirectory.Entries)
                entry.Prototype = new(gpakDict[$"Calligraphy/{entry.FilePath}"]);
            Logger.Info($"Parsed {PrototypeDirectory.Entries.Length} prototypes");

            // Initialize supplementary dictionaries
            PrototypeBlueprintDict = new(BlueprintDirectory.Entries.Length);
            foreach (DataDirectoryBlueprintEntry entry in BlueprintDirectory.Entries)
                PrototypeBlueprintDict.Add(GetPrototype(entry.Blueprint.PrototypeId), entry.Blueprint);

            // Assets
            AssetDict.Add(0, "0");  // add 0 manually
            AssetTypeDict.Add(0, "0");

            foreach (DataDirectoryGTypeEntry dirEntry in GTypeDirectory.Entries)
            {
                foreach (GTypeEntry entry in dirEntry.GType.Entries)
                {
                    AssetDict.Add(entry.Id, entry.Name);
                    AssetTypeDict.Add(entry.Id, Path.GetFileNameWithoutExtension(dirEntry.FilePath));
                }
            }

            Logger.Info($"Loaded {AssetDict.Count} asset references");

            // Prototype fields
            foreach (DataDirectoryBlueprintEntry dirEntry in BlueprintDirectory.Entries)
                foreach (var kvp in dirEntry.Blueprint.FieldDict)
                    PrototypeFieldDict.Add(kvp.Key, kvp.Value.Name);
        }

        // Accessors for various data files
        public GType GetGType(ulong id) => ((DataDirectoryGTypeEntry)GTypeDirectory.IdDict[id]).GType;
        public GType GetGType(string path) => ((DataDirectoryGTypeEntry)GTypeDirectory.FilePathDict[path]).GType;
        public Curve GetCurve(ulong id) => ((DataDirectoryCurveEntry)CurveDirectory.IdDict[id]).Curve;
        public Curve GetCurve(string path) => ((DataDirectoryCurveEntry)CurveDirectory.FilePathDict[path]).Curve;
        public Blueprint GetBlueprint(ulong id) => ((DataDirectoryBlueprintEntry)BlueprintDirectory.IdDict[id]).Blueprint;
        public Blueprint GetBlueprint(string path) => ((DataDirectoryBlueprintEntry)BlueprintDirectory.FilePathDict[path]).Blueprint;
        public Prototype GetPrototype(ulong id) => ((DataDirectoryPrototypeEntry)PrototypeDirectory.IdDict[id]).Prototype;
        public Prototype GetPrototype(string path) => ((DataDirectoryPrototypeEntry)PrototypeDirectory.FilePathDict[path]).Prototype;

        public Prototype GetBlueprintPrototype(Blueprint blueprint) => GetPrototype(blueprint.PrototypeId);
        public Prototype GetBlueprintPrototype(ulong blueprintId) => GetBlueprintPrototype(GetBlueprint(blueprintId));
        public Prototype GetBlueprintPrototype(string blueprintPath) => GetBlueprintPrototype(GetBlueprint(blueprintPath));

        public Blueprint GetPrototypeBlueprint(Prototype prototype)
        {
            if (prototype.Data.ParentId == 0) return PrototypeBlueprintDict[prototype];     // use this prototype as a key if it's a .defaults prototype
            return PrototypeBlueprintDict[GetPrototype(prototype.Data.ParentId)];           // get .defaults to use as a key if it's a child prototype
        }

        public Blueprint GetPrototypeBlueprint(ulong prototypeId) => GetPrototypeBlueprint(GetPrototype(prototypeId));
        public Blueprint GetPrototypeBlueprint(string prototypePath) => GetPrototypeBlueprint(GetPrototype(prototypePath));

        public override bool Verify()
        {
            return GTypeDirectory.Entries.Length > 0
                && CurveDirectory.Entries.Length > 0
                && BlueprintDirectory.Entries.Length > 0
                && PrototypeDirectory.Entries.Length > 0
                && ReplacementDirectory.Entries.Length > 0;
        }

        public override void Export()
        {
            // Set up json serializer
            _jsonSerializerOptions.Converters.Add(new BlueprintConverter(PrototypeDirectory, CurveDirectory, GTypeDirectory));
            _jsonSerializerOptions.Converters.Add(new PrototypeConverter(PrototypeDirectory, CurveDirectory, GTypeDirectory, PrototypeFieldDict, AssetDict, AssetTypeDict));
            _jsonSerializerOptions.MaxDepth = 128;  // 64 is not enough for prototypes

            // Build dictionaries out of directories for compatibility with the old JSON export
            // Exporting isn't performance / memory critical, so it should be fine
            Dictionary<string, GType> gtypeDict = new(GTypeDirectory.Entries.Length);
            foreach (DataDirectoryGTypeEntry entry in GTypeDirectory.Entries)
                gtypeDict.Add($"Calligraphy/{entry.FilePath}", entry.GType);

            Dictionary<string, Blueprint> blueprintDict = new(BlueprintDirectory.Entries.Length);
            foreach (DataDirectoryBlueprintEntry entry in BlueprintDirectory.Entries)
                blueprintDict.Add($"Calligraphy/{entry.FilePath}", entry.Blueprint);

            Dictionary<string, Prototype> prototypeDict = new(PrototypeDirectory.Entries.Length);
            foreach (DataDirectoryPrototypeEntry entry in PrototypeDirectory.Entries)
                prototypeDict.Add($"Calligraphy/{entry.FilePath}", entry.Prototype);

            // Serialize and save
            ExportDataDirectories();
            SerializeDictAsJson(gtypeDict);
            ExportCurveDict();
            SerializeDictAsJson(blueprintDict);
            SerializeDictAsJson(prototypeDict);
        }

        private void ExportDataDirectories()
        {
            string dir = $"{Directory.GetCurrentDirectory()}\\Assets\\GPAK\\Export\\Calligraphy\\";
            if (Directory.Exists(dir) == false) Directory.CreateDirectory(dir);

            using (StreamWriter writer = new($"{dir}\\Type.directory.tsv"))
            {
                foreach (DataDirectoryGTypeEntry entry in GTypeDirectory.Entries)
                    writer.WriteLine($"{entry.Id1}\t{entry.Id2}\t{entry.Field2}\t{entry.FilePath}");
            }

            using (StreamWriter writer = new($"{dir}\\Curve.directory.tsv"))
            {
                foreach (DataDirectoryCurveEntry entry in CurveDirectory.Entries)
                    writer.WriteLine($"{entry.Id1}\t{entry.Id2}\t{entry.Field2}\t{entry.FilePath}");
            }

            using (StreamWriter writer = new($"{dir}\\Blueprint.directory.tsv"))
            {
                foreach (DataDirectoryBlueprintEntry entry in BlueprintDirectory.Entries)
                    writer.WriteLine($"{entry.Id1}\t{entry.Id2}\t{entry.Field2}\t{entry.FilePath}");
            }

            using (StreamWriter writer = new($"{dir}\\Prototype.directory.tsv"))
            {
                foreach (DataDirectoryPrototypeEntry entry in PrototypeDirectory.Entries)
                    writer.WriteLine($"{entry.Id1}\t{entry.Id2}\t{entry.ParentId}\t{entry.Field3}\t{entry.FilePath}");
            }

            using (StreamWriter writer = new($"{dir}\\Replacement.directory.tsv"))
            {
                foreach (DataDirectoryEntry entry in ReplacementDirectory.Entries)
                    writer.WriteLine($"{entry.Id1}\t{entry.Id2}\t{entry.FilePath}");
            }
        }

        private void ExportCurveDict()
        {
            foreach (DataDirectoryCurveEntry dirEntry in CurveDirectory.Entries)  // use TSV for curves
            {
                string path = $"{Directory.GetCurrentDirectory()}\\Assets\\GPAK\\Export\\Calligraphy\\{dirEntry.FilePath}.tsv";
                string dir = Path.GetDirectoryName(path);
                if (Directory.Exists(dir) == false) Directory.CreateDirectory(dir);

                using (StreamWriter sw = new(path))
                {
                    foreach (double value in dirEntry.Curve.Entries)
                        sw.WriteLine(value);
                }
            }
        }
    }
}
