using MHServerEmu.Common;

namespace MHServerEmu.GameServer.GameData
{
    public enum PrototypeEnumType
    {
        Entity,
        Inventory,
        Power,
        Property
    }

    public class PrototypeEnumManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private Dictionary<PrototypeEnumType, ulong[]> _prototypeEnumDict;                  // For enum -> prototypeId conversion
        private Dictionary<PrototypeEnumType, Dictionary<ulong, ulong>> _enumLookupDict;    // For prototypeId -> enum conversion

        public PrototypeEnumManager(string propertyEnumTableDirectoryPath)
        {
            // Load prototype enums from external files (TODO: derive enums from prototypeIds instead)
            _prototypeEnumDict = new()
            {
                { PrototypeEnumType.Entity, LoadPrototypeEnumTable($"{propertyEnumTableDirectoryPath}\\Entity.bin") },         // formerly ResourceEnumRefTable
                { PrototypeEnumType.Inventory, LoadPrototypeEnumTable($"{propertyEnumTableDirectoryPath}\\Inventory.bin") },
                { PrototypeEnumType.Power, LoadPrototypeEnumTable($"{propertyEnumTableDirectoryPath}\\Power.bin") },           // formerly PropertyIdPowerRefTable
                { PrototypeEnumType.Property, LoadPrototypeEnumTable($"{propertyEnumTableDirectoryPath}\\Property.bin") },     // formerly GlobalEnumRefTable
            };

            // Create a dictionary to quickly look up enums from prototypeIds
            _enumLookupDict = new();
            foreach (var kvp in _prototypeEnumDict)
            {
                _enumLookupDict.Add(kvp.Key, new());

                for (int i = 0; i < kvp.Value.Length; i++)
                    _enumLookupDict[kvp.Key].Add(kvp.Value[i], (ulong)i);
            }
        }

        public ulong GetPrototypeId(ulong enumValue, PrototypeEnumType type) => _prototypeEnumDict[type][enumValue];
        public ulong GetEnumValue(ulong prototypeId, PrototypeEnumType type) => _enumLookupDict[type][prototypeId];
        public int GetMaxEnumValue() => _enumLookupDict[PrototypeEnumType.Property].Count - 1;

        public bool Verify()
        {
            return _prototypeEnumDict[PrototypeEnumType.Entity].Length > 0
                && _prototypeEnumDict[PrototypeEnumType.Inventory].Length > 0
                && _prototypeEnumDict[PrototypeEnumType.Power].Length > 0
                && _prototypeEnumDict[PrototypeEnumType.Property].Length > 0;
        }

        public List<ulong> GetPowerPropertyIdList(string filter)
        {
            ulong[] powerTable = _prototypeEnumDict[PrototypeEnumType.Power];
            List<ulong> propertyIdList = new();

            for (int i = 1; i < powerTable.Length; i++)
                if (GameDatabase.GetPrototypePath(powerTable[i]).Contains(filter))
                    propertyIdList.Add(DataHelper.ReconstructPowerPropertyIdFromHash((ulong)i));

            return propertyIdList;
        }

        private ulong[] LoadPrototypeEnumTable(string path)
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
