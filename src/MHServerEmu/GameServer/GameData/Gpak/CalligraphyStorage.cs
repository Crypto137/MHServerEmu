using MHServerEmu.Common.Logging;
using MHServerEmu.GameServer.GameData.Gpak.FileFormats;

namespace MHServerEmu.GameServer.GameData.Gpak
{
    public class CalligraphyStorage : GpakStorage
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public DataDirectory CurveDirectory { get; }
        public DataDirectory AssetTypeDirectory { get; }
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
            CurveDirectory = new(gpakDict["Calligraphy/Curve.directory"]);
            AssetTypeDirectory = new(gpakDict["Calligraphy/Type.directory"]);
            BlueprintDirectory = new(gpakDict["Calligraphy/Blueprint.directory"]);
            PrototypeDirectory = new(gpakDict["Calligraphy/Prototype.directory"]);
            ReplacementDirectory = new(gpakDict["Calligraphy/Replacement.directory"]);

            // Populate directories with data from GPAK
            // Curve
            foreach (DataDirectoryCurveRecord record in CurveDirectory.Records)
                record.Curve = new(gpakDict[$"Calligraphy/{record.FilePath}"]);
            Logger.Info($"Parsed {CurveDirectory.Records.Length} curves");

            // AssetType
            foreach (DataDirectoryAssetTypeRecord record in AssetTypeDirectory.Records)
                record.AssetType = new(gpakDict[$"Calligraphy/{record.FilePath}"]);
            Logger.Info($"Parsed {AssetTypeDirectory.Records.Length} asset types");

            // Blueprint
            foreach (DataDirectoryBlueprintRecord record in BlueprintDirectory.Records)
                record.Blueprint = new(gpakDict[$"Calligraphy/{record.FilePath}"]);
            Logger.Info($"Parsed {BlueprintDirectory.Records.Length} blueprints");

            // Prototype
            foreach (DataDirectoryPrototypeRecord record in PrototypeDirectory.Records)
                record.Prototype = new(gpakDict[$"Calligraphy/{record.FilePath}"]);
            Logger.Info($"Parsed {PrototypeDirectory.Records.Length} prototypes");

            // Initialize supplementary dictionaries
            PrototypeBlueprintDict = new(BlueprintDirectory.Records.Length);
            foreach (DataDirectoryBlueprintRecord record in BlueprintDirectory.Records)
                PrototypeBlueprintDict.Add(GetPrototype(record.Blueprint.DefaultPrototypeId), record.Blueprint);

            // Assets
            AssetDict.Add(0, "0");  // add 0 manually
            AssetTypeDict.Add(0, "0");

            foreach (DataDirectoryAssetTypeRecord record in AssetTypeDirectory.Records)
            {
                foreach (AssetTypeEntry entry in record.AssetType.Entries)
                {
                    AssetDict.Add(entry.Id1, entry.Name);
                    AssetTypeDict.Add(entry.Id1, Path.GetFileNameWithoutExtension(record.FilePath));
                }
            }

            Logger.Info($"Loaded {AssetDict.Count} asset references");

            // Prototype fields
            foreach (DataDirectoryBlueprintRecord record in BlueprintDirectory.Records)
                foreach (BlueprintMember member in record.Blueprint.Members)
                    PrototypeFieldDict.Add(member.FieldId, member.FieldName);
        }

        // Accessors for various data files
        public AssetType GetAssetType(ulong id) => ((DataDirectoryAssetTypeRecord)AssetTypeDirectory.IdDict[id]).AssetType;
        public AssetType GetAssetType(string path) => ((DataDirectoryAssetTypeRecord)AssetTypeDirectory.FilePathDict[path]).AssetType;
        public Curve GetCurve(ulong id) => ((DataDirectoryCurveRecord)CurveDirectory.IdDict[id]).Curve;
        public Curve GetCurve(string path) => ((DataDirectoryCurveRecord)CurveDirectory.FilePathDict[path]).Curve;
        public Blueprint GetBlueprint(ulong id) => ((DataDirectoryBlueprintRecord)BlueprintDirectory.IdDict[id]).Blueprint;
        public Blueprint GetBlueprint(string path) => ((DataDirectoryBlueprintRecord)BlueprintDirectory.FilePathDict[path]).Blueprint;
        public Prototype GetPrototype(ulong id) => ((DataDirectoryPrototypeRecord)PrototypeDirectory.IdDict[id]).Prototype;
        public Prototype GetPrototype(string path) => ((DataDirectoryPrototypeRecord)PrototypeDirectory.FilePathDict[path]).Prototype;

        public Prototype GetBlueprintPrototype(Blueprint blueprint) => GetPrototype(blueprint.DefaultPrototypeId);
        public Prototype GetBlueprintPrototype(ulong blueprintId) => GetBlueprintPrototype(GetBlueprint(blueprintId));
        public Prototype GetBlueprintPrototype(string blueprintPath) => GetBlueprintPrototype(GetBlueprint(blueprintPath));

        public Blueprint GetPrototypeBlueprint(Prototype prototype)
        {
            while (prototype.Data.ParentId != 0)                        // Go up until we get to the parentless prototype (.defaults)
                prototype = GetPrototype(prototype.Data.ParentId);
            return PrototypeBlueprintDict[prototype];                   // Use .defaults prototype as a key to get the blueprint for it
        }

        public Blueprint GetPrototypeBlueprint(ulong prototypeId) => GetPrototypeBlueprint(GetPrototype(prototypeId));
        public Blueprint GetPrototypeBlueprint(string prototypePath) => GetPrototypeBlueprint(GetPrototype(prototypePath));

        // Helper methods
        public bool IsCalligraphyPrototype(ulong prototypeId) => PrototypeDirectory.IdDict.TryGetValue(prototypeId, out IDataRecord record);  // TryGetValue is apparently faster than ContainsKey

        public override bool Verify()
        {
            return AssetTypeDirectory.Records.Length > 0
                && CurveDirectory.Records.Length > 0
                && BlueprintDirectory.Records.Length > 0
                && PrototypeDirectory.Records.Length > 0
                && ReplacementDirectory.Records.Length > 0;
        }

        #region Export

        public override void Export()
        {
            // Set up json serializer
            _jsonSerializerOptions.Converters.Add(new BlueprintConverter(PrototypeDirectory, CurveDirectory, AssetTypeDirectory));
            _jsonSerializerOptions.Converters.Add(new PrototypeConverter(PrototypeDirectory, CurveDirectory, AssetTypeDirectory, PrototypeFieldDict, AssetDict, AssetTypeDict));
            _jsonSerializerOptions.MaxDepth = 128;  // 64 is not enough for prototypes

            // Build dictionaries out of directories for compatibility with the old JSON export
            // Exporting isn't performance / memory critical, so it should be fine
            Dictionary<string, AssetType> assetTypeDict = new(AssetTypeDirectory.Records.Length);
            foreach (DataDirectoryAssetTypeRecord record in AssetTypeDirectory.Records)
                assetTypeDict.Add($"Calligraphy/{record.FilePath}", record.AssetType);

            Dictionary<string, Blueprint> blueprintDict = new(BlueprintDirectory.Records.Length);
            foreach (DataDirectoryBlueprintRecord record in BlueprintDirectory.Records)
                blueprintDict.Add($"Calligraphy/{record.FilePath}", record.Blueprint);

            Dictionary<string, Prototype> prototypeDict = new(PrototypeDirectory.Records.Length);
            foreach (DataDirectoryPrototypeRecord record in PrototypeDirectory.Records)
                prototypeDict.Add($"Calligraphy/{record.FilePath}", record.Prototype);

            // Serialize and save
            ExportDataDirectories();
            SerializeDictAsJson(assetTypeDict);
            ExportCurveDict();
            SerializeDictAsJson(blueprintDict);
            SerializeDictAsJson(prototypeDict);
        }

        private void ExportDataDirectories()
        {
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "GPAK", "Export", "Calligraphy");
            if (Directory.Exists(dir) == false) Directory.CreateDirectory(dir);

            using (StreamWriter writer = new(Path.Combine(dir, "Curve.directory.tsv")))
            {
                foreach (DataDirectoryCurveRecord record in CurveDirectory.Records)
                    writer.WriteLine($"{record.Id}\t{record.Guid}\t{record.ByteField}\t{record.FilePath}");
            }

            using (StreamWriter writer = new(Path.Combine(dir, "Type.directory.tsv")))
            {
                foreach (DataDirectoryAssetTypeRecord record in AssetTypeDirectory.Records)
                    writer.WriteLine($"{record.Id}\t{record.Guid}\t{record.ByteField}\t{record.FilePath}");
            }

            using (StreamWriter writer = new(Path.Combine(dir, "Blueprint.directory.tsv")))
            {
                foreach (DataDirectoryBlueprintRecord record in BlueprintDirectory.Records)
                    writer.WriteLine($"{record.Id}\t{record.Guid}\t{record.ByteField}\t{record.FilePath}");
            }

            using (StreamWriter writer = new(Path.Combine(dir, "Prototype.directory.tsv")))
            {
                foreach (DataDirectoryPrototypeRecord record in PrototypeDirectory.Records)
                    writer.WriteLine($"{record.Id}\t{record.Guid}\t{record.ParentId}\t{record.ByteField}\t{record.FilePath}");
            }

            using (StreamWriter writer = new(Path.Combine(dir, "Replacement.directory.tsv")))
            {
                foreach (DataDirectoryReplacementRecord record in ReplacementDirectory.Records)
                    writer.WriteLine($"{record.OldGuid}\t{record.NewGuid}\t{record.Name}");
            }
        }

        private void ExportCurveDict()
        {
            foreach (DataDirectoryCurveRecord record in CurveDirectory.Records)  // use TSV for curves
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "GPAK", "Export", "Calligraphy", $"{record.FilePath}.tsv");
                string dir = Path.GetDirectoryName(path);
                if (Directory.Exists(dir) == false) Directory.CreateDirectory(dir);

                using (StreamWriter sw = new(path))
                {
                    foreach (double value in record.Curve.Entries)
                        sw.WriteLine(value);
                }
            }
        }

        #endregion
    }
}
