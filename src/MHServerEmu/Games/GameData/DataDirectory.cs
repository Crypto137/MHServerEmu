using System.Text.Json;
using MHServerEmu.Common;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Gpak;
using MHServerEmu.Games.GameData.JsonOutput;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData
{
    /// <summary>
    /// The class that manages all loaded data.
    /// </summary>
    public class DataDirectory
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<ulong, Blueprint> _blueprintDict = new();

        private readonly Dictionary<ulong, PrototypeDataRefRecord> _prototypeRecordDict = new();
        private readonly Dictionary<ulong, object> _prototypeDict = new();
        private readonly Dictionary<ulong, ulong> _prototypeGuidToDataRefDict = new();

        private readonly Dictionary<Prototype, Blueprint> _prototypeBlueprintDict = new();  // .defaults prototype -> blueprint

        // Temporary helper class for getting prototype enums until we implement prototype class hierarchy properly
        private PrototypeEnumManager _prototypeEnumManager; 

        public AssetDirectory AssetDirectory { get; }
        public CurveDirectory CurveDirectory { get; }
        public ReplacementDirectory ReplacementDirectory { get; }

        public DataDirectory(GpakFile calligraphyGpak, GpakFile resourceGpak)
        {
            // Convert GPAK file to a dictionary for easy access to all of its entries
            var gpakDict = calligraphyGpak.ToDictionary();

            // Initialize asset directory
            AssetDirectory = new();

            using (MemoryStream stream = new(gpakDict["Calligraphy/Type.directory"]))
            using (BinaryReader reader = new(stream))
            {
                CalligraphyHeader header = reader.ReadCalligraphyHeader();      // TDR
                int recordCount = reader.ReadInt32();
                for (int i = 0; i < recordCount; i++)
                    ReadTypeDirectoryEntry(reader, gpakDict);
            }

            Logger.Info($"Parsed {AssetDirectory.AssetCount} assets of {AssetDirectory.AssetTypeCount} types");

            // Initialize curve directory
            CurveDirectory = new();

            using (MemoryStream stream = new(gpakDict["Calligraphy/Curve.directory"]))
            using (BinaryReader reader = new(stream))
            {
                CalligraphyHeader header = reader.ReadCalligraphyHeader();      // CDR
                int recordCount = reader.ReadInt32();
                for (int i = 0; i < recordCount; i++)
                    ReadCurveDirectoryEntry(reader, gpakDict);
            }

            Logger.Info($"Parsed {CurveDirectory.RecordCount} curves");

            // Initialize blueprint directory
            using (MemoryStream stream = new(gpakDict["Calligraphy/Blueprint.directory"]))
            using (BinaryReader reader = new(stream))
            {
                CalligraphyHeader header = reader.ReadCalligraphyHeader();      // BDR
                int recordCount = reader.ReadInt32();
                for (int i = 0; i < recordCount; i++)
                    ReadBlueprintDirectoryEntry(reader, gpakDict);
            }

            Logger.Info($"Parsed {_blueprintDict.Count} blueprints");

            // Initialize prototype directory
            using (MemoryStream stream = new(gpakDict["Calligraphy/Prototype.directory"]))
            using (BinaryReader reader = new(stream))
            {
                CalligraphyHeader header = reader.ReadCalligraphyHeader();      // PDR
                int recordCount = reader.ReadInt32();
                for (int i = 0; i < recordCount; i++)
                    ReadPrototypeDirectoryEntry(reader, gpakDict);
            }

            // Load resource prototypes
            CreatePrototypeDataRefsForDirectory(resourceGpak);

            Logger.Info($"Parsed {_prototypeDict.Count} prototype files");

            // Initialize replacement directory
            ReplacementDirectory = new();

            using (MemoryStream stream = new(gpakDict["Calligraphy/Replacement.directory"]))
            using (BinaryReader reader = new(stream))
            {
                CalligraphyHeader header = reader.ReadCalligraphyHeader();      // RDR
                int recordCount = reader.ReadInt32();
                for (int i = 0; i < recordCount; i++)
                    ReadReplacementDirectoryEntry(reader);
            }

            // old hierarchy init
            InitializeHierarchyCache();
        }

        #region Initialization

        private void ReadTypeDirectoryEntry(BinaryReader reader, Dictionary<string, byte[]> gpakDict)
        {
            ulong dataId = reader.ReadUInt64();
            ulong assetTypeGuid = reader.ReadUInt64();
            byte flags = reader.ReadByte();
            string filePath = reader.ReadFixedString16().Replace('\\', '/');

            GameDatabase.AssetTypeRefManager.AddDataRef(dataId, filePath);
            LoadedAssetTypeRecord record = AssetDirectory.CreateAssetTypeRecord(dataId, flags);
            record.AssetType = new(gpakDict[$"Calligraphy/{filePath}"], AssetDirectory, dataId, assetTypeGuid);

        }

        private void ReadCurveDirectoryEntry(BinaryReader reader, Dictionary<string, byte[]> gpakDict)
        {
            ulong curveId = reader.ReadUInt64();
            ulong guid = reader.ReadUInt64();   // Doesn't seem to be used at all
            byte flags = reader.ReadByte();
            string filePath = reader.ReadFixedString16().Replace('\\', '/');

            GameDatabase.CurveRefManager.AddDataRef(curveId, filePath);
            CurveRecord record = CurveDirectory.CreateCurveRecord(curveId, flags);
            record.Curve = new(gpakDict[$"Calligraphy/{filePath}"]);
        }

        private void ReadBlueprintDirectoryEntry(BinaryReader reader, Dictionary<string, byte[]> gpakDict)
        {
            ulong dataId = reader.ReadUInt64();
            ulong guid = reader.ReadUInt64();
            byte flags = reader.ReadByte();
            string filePath = reader.ReadFixedString16().Replace('\\', '/');

            GameDatabase.BlueprintRefManager.AddDataRef(dataId, filePath);
            LoadBlueprint(dataId, guid, flags, gpakDict);
        }

        public void ReadPrototypeDirectoryEntry(BinaryReader reader, Dictionary<string, byte[]> gpakDict)
        {
            ulong prototypeId = reader.ReadUInt64();
            ulong prototypeGuid = reader.ReadUInt64();
            ulong blueprintId = reader.ReadUInt64();
            byte flags = reader.ReadByte();
            string filePath = reader.ReadFixedString16().Replace('\\', '/');

            AddCalligraphyPrototype(prototypeId, prototypeGuid, blueprintId, flags, filePath, gpakDict);
        }

        private void ReadReplacementDirectoryEntry(BinaryReader reader)
        {
            ulong oldGuid = reader.ReadUInt64();
            ulong newGuid = reader.ReadUInt64();
            string name = reader.ReadFixedString16();

            ReplacementDirectory.AddReplacementRecord(oldGuid, newGuid, name);
        }

        private void LoadBlueprint(ulong id, ulong guid, byte flags, Dictionary<string, byte[]> gpakDict)
        {
            // Blueprint deserialization is not yet properly implemented
            Blueprint blueprint = new(gpakDict[$"Calligraphy/{GameDatabase.GetBlueprintName(id)}"]);
            _blueprintDict.Add(id, blueprint);

            // Add field name refs when loading blueprints
            foreach (BlueprintMember member in blueprint.Members)
                GameDatabase.StringRefManager.AddDataRef(member.FieldId, member.FieldName);
        }

        private void AddCalligraphyPrototype(ulong prototypeId, ulong prototypeGuid, ulong blueprintId, byte flags, string filePath, Dictionary<string, byte[]> gpakDict)
        {
            // Create a dataRef
            GameDatabase.PrototypeRefManager.AddDataRef(prototypeId, filePath);
            _prototypeGuidToDataRefDict.Add(prototypeGuid, prototypeId);

            // Add a new prototype record
            _prototypeRecordDict.Add(prototypeId, new()
            {
                PrototypeId = prototypeId,
                PrototypeGuid = prototypeGuid,
                BlueprintId = blueprintId,
                Flags = flags,
                IsCalligraphyPrototype = true
            });

            // Load the prototype
            PrototypeFile prototypeFile = new(gpakDict[$"Calligraphy/{filePath}"]);
            prototypeFile.Prototype.SetDataRef(prototypeId);
            _prototypeDict.Add(prototypeId, prototypeFile.Prototype);
        }

        private void AddResource(string filePath, byte[] data)
        {
            // Create a dataRef
            ulong prototypeId = HashHelper.HashPath($"&{filePath.ToLower()}");   
            GameDatabase.PrototypeRefManager.AddDataRef(prototypeId, filePath);

            // Add a new prototype record
            _prototypeRecordDict.Add(prototypeId, new()
            {
                PrototypeId = prototypeId,
                PrototypeGuid = 0,
                BlueprintId = 0,
                Flags = 0,
                IsCalligraphyPrototype = false
            });

            // Load the resource
            object resource;
            string extension = Path.GetExtension(filePath);

            switch (extension)
            {
                case ".cell":       resource = new CellPrototype(data);         break;
                case ".district":   resource = new DistrictPrototype(data);     break;
                case ".encounter":  resource = new EncounterResourcePrototype(data);    break;
                case ".propset":    resource = new PropSetPrototype(data);      break;
                case ".prop":       resource = new PropPackagePrototype(data);         break;
                case ".ui":         resource = new UIPrototype(data);           break;
                default:            throw new($"Unsupported resource type ({extension}).");
            }

            _prototypeDict.Add(prototypeId, resource);
        }

        private void InitializeHierarchyCache()
        {
            // not yet properly implemented

            // .defaults prototype -> blueprint
            foreach (var kvp in _blueprintDict)
                _prototypeBlueprintDict.Add(GetPrototype<Prototype>(kvp.Value.DefaultPrototypeId), kvp.Value);

            // enums
            _prototypeEnumManager = new(this);
        }

        private void CreatePrototypeDataRefsForDirectory(GpakFile resourceFile)
        {
            // Not yet properly implemented
            // Todo: after combining both sips into PakfileSystem filter files here by "Resource/" prefix
            foreach (GpakEntry entry in resourceFile.Entries)
                AddResource(entry.FilePath, entry.Data);
        }

        #endregion

        #region Data Access

        public ulong GetPrototypeDataRefByGuid(ulong guid)
        {
            if (_prototypeGuidToDataRefDict.TryGetValue(guid, out ulong id))
                return id;

            return 0;
        }

        public ulong GetPrototypeGuid(ulong id)
        {
            if (_prototypeRecordDict.TryGetValue(id, out PrototypeDataRefRecord record))
                return record.PrototypeGuid;

            return 0;
        }

        public Blueprint GetBlueprint(ulong id)
        {
            if (_blueprintDict.TryGetValue(id, out Blueprint blueprint))
                return blueprint;

            return null;
        }

        public T GetPrototype<T>(ulong id) where T : Prototype
        {
            if (_prototypeDict.TryGetValue(id, out object prototype))
            {
                if (typeof(T) != typeof(Prototype) && prototype.GetType() == typeof(Prototype))
                {
                   var newPrototype = (T)Activator.CreateInstance(typeof(T), new object[] { prototype });
                    ReplacePrototypeDict(id, newPrototype);
                    return newPrototype;
                }
                else
                    return (T)prototype;
            }                

            return default;
        }

        public Prototype GetBlueprintDefaultPrototype(Blueprint blueprint) => GetPrototype<Prototype>(blueprint.DefaultPrototypeId);
        public Prototype GetBlueprintDefaultPrototype(ulong blueprintId) => GetBlueprintDefaultPrototype(GetBlueprint(blueprintId));
        public Prototype GetBlueprintDefaultPrototype(string blueprintPath) => GetBlueprintDefaultPrototype(
            GetBlueprint(GameDatabase.BlueprintRefManager.GetDataRefByName(blueprintPath)));

        public Blueprint GetPrototypeBlueprint(Prototype prototype)
        {
            while (prototype.ParentId != 0)                     // Go up until we get to the parentless prototype (.defaults)
                prototype = GetPrototype<Prototype>(prototype.ParentId);
            if (_prototypeBlueprintDict.TryGetValue(prototype, out Blueprint blueprint))
                return blueprint;          // Use .defaults prototype as a key to get the blueprint for it
            else
                return null;
        }

        public Blueprint GetPrototypeBlueprint(ulong prototypeId) => GetPrototypeBlueprint(GetPrototype<Prototype>(prototypeId));

        public ulong GetPrototypeFromEnumValue(ulong enumValue, PrototypeEnumType type) => _prototypeEnumManager.GetPrototypeFromEnumValue(enumValue, type);
        public ulong GetPrototypeEnumValue(ulong prototypeId, PrototypeEnumType type) => _prototypeEnumManager.GetPrototypeEnumValue(prototypeId, type);

        public List<ulong> GetPowerPropertyIdList(string filter) => _prototypeEnumManager.GetPowerPropertyIdList(filter);   // TO BE REMOVED: temp bruteforcing of power property ids


        // Helper methods
        public bool IsCalligraphyPrototype(ulong prototypeId)
        {
            if (_prototypeRecordDict.TryGetValue(prototypeId, out PrototypeDataRefRecord record))
                return record.IsCalligraphyPrototype;

            return false;
        }

        #endregion

        #region Old Extras

        public bool Verify()
        {
            return AssetDirectory.AssetCount > 0
                && CurveDirectory.RecordCount > 0
                && _blueprintDict.Count > 0
                && _prototypeDict.Count > 0
                && ReplacementDirectory.RecordCount > 0;
        }

        public void Export()
        {
            // Set up json serializer
            JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true, MaxDepth = 128 };
            jsonSerializerOptions.Converters.Add(new BlueprintConverter());
            jsonSerializerOptions.Converters.Add(new PrototypeFileConverter());

            // todo: reimplement export
        }

        private void ReplacePrototypeDict(ulong id, Prototype newPrototype) 
        {
            Prototype oldPrototype = (Prototype)_prototypeDict[id];
            _prototypeDict[id] = newPrototype;
            if (_prototypeBlueprintDict.TryGetValue(oldPrototype, out Blueprint blueprint))
            {
                _prototypeBlueprintDict.Add(newPrototype, blueprint);
                _prototypeBlueprintDict.Remove(oldPrototype);
            }
        }

        public IEnumerable<Prototype> IteratePrototypesInHierarchy(Type prototypeType, int flags)
        {
            // Get list of all prototypes with this type
            foreach (var kvp in _prototypeDict)
            {
                ulong id = kvp.Key;
                object prototype = kvp.Value;

                if (prototype.GetType() == typeof(Prototype))
                {
                    string className = GetPrototypeBlueprint((Prototype)prototype).RuntimeBinding;
                    Type protoType = Type.GetType("MHServerEmu.Games.GameData.Prototypes." + className);

                    if (protoType != null && protoType == prototypeType) {
                        var newPrototype = Activator.CreateInstance(prototypeType, new object[] { prototype });
                        ReplacePrototypeDict(id, (Prototype)newPrototype);                       
                        yield return (Prototype)newPrototype; 
                    }

                } else if (prototype.GetType() == prototypeType)
                {
                    yield return (Prototype)prototype;
                }
            }
        }

        public Prototype GetPrototypeExt(ulong id)
        {
            if (_prototypeDict.TryGetValue(id, out object prototype))
            {
                if (prototype.GetType() == typeof(Prototype))
                {
                    string className = GetPrototypeBlueprint((Prototype)prototype).RuntimeBinding;
                    Type protoType = Type.GetType("MHServerEmu.Games.GameData.Prototypes." + className);
                    if (protoType == null)
                    {
                        Logger.Warn($"PrototypeClass {className} not exist");
                        return null;
                    }
                    var newPrototype = Activator.CreateInstance(protoType, new object[] { prototype });
                    ReplacePrototypeDict(id, (Prototype)newPrototype);
                    return (Prototype)newPrototype;
                }
                else
                    return (Prototype)prototype;
            }

            return default;
        }

        #endregion
    }

    public class PrototypeDataRefRecord
    {
        public ulong PrototypeId { get; set; }
        public ulong PrototypeGuid { get; set; }
        public ulong BlueprintId { get; set; }
        public byte Flags { get; set; }
        public bool IsCalligraphyPrototype { get; set; }
    }
}
