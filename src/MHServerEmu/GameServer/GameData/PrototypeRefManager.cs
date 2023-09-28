using MHServerEmu.Common.Logging;
using MHServerEmu.GameServer.GameData.Gpak;
using MHServerEmu.GameServer.GameData.Gpak.FileFormats;

namespace MHServerEmu.GameServer.GameData
{
    public enum PrototypeEnumType
    {
        All,
        Entity,
        Inventory,
        Power
    }

    public class PrototypeRefManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        #region Enum Filters

        private static readonly string[] EntityClasses = new string[]
        {
            "EntityPrototype",
            "AgentPrototype",
            "AgentTeamUpPrototype",
            "OrbPrototype",
            "AvatarPrototype",
            "CharacterTokenPrototype",
            "HotspotPrototype",
            "MissilePrototype",
            "ItemPrototype",
            "BagItemPrototype",
            "CostumePrototype",
            "CraftingIngredientPrototype",
            "CostumeCorePrototype",
            "CraftingRecipePrototype",
            "ArmorPrototype",
            "ArtifactPrototype",
            "LegendaryPrototype",
            "MedalPrototype",
            "RelicPrototype",
            "TeamUpGearPrototype",
            "PlayerPrototype",
            "TransitionPrototype",
            "PropPrototype",
            "SmartPropPrototype",
            "WorldEntityPrototype",
            "DestructiblePropPrototype",
            "PvPPrototype",
            "MatchMetaGamePrototype",
            "MissionMetaGamePrototype",
            "MetaGamePrototype",
            "SpawnerPrototype",
            "KismetSequenceEntityPrototype",
            "InventoryStashTokenPrototype",
            "EmoteTokenPrototype",
            "DestructibleSmartPropPrototype"
        };

        private static readonly string[] PowerClasses = new string[]
        {
            "PowerPrototype",
            "MissilePowerPrototype",
            "SummonPowerPrototype",
            "SituationalPowerPrototype",
            "MovementPowerPrototype",
            "SpecializationPowerPrototype"
        };

        private static readonly string[] InventoryClasses = new string[]
        {
            "InventoryPrototype",
            "PlayerStashInventoryPrototype"
        };

        #endregion

        private HashMap _prototypeHashMap;                                                  // PrototypeId <-> FilePath
        private Dictionary<ulong, ulong> _prototypeGuidDict;                                // PrototypeGuid -> PrototypeId
        private Dictionary<PrototypeEnumType, ulong[]> _prototypeEnumDict;                  // EnumValue -> PrototypeId
        private Dictionary<PrototypeEnumType, Dictionary<ulong, ulong>> _enumLookupDict;    // PrototypeId -> EnumValue

        public int MaxEnumValue { get => _enumLookupDict[PrototypeEnumType.All].Count - 1; }

        public PrototypeRefManager(CalligraphyStorage calligraphy, ResourceStorage resource)
        {
            // Generate a hash map for all prototypes (Calligraphy + Resource) and fill _prototypeGuidDict
            _prototypeHashMap = new(calligraphy.PrototypeDirectory.Records.Length + resource.DirectoryDict.Count);
            _prototypeHashMap.Add(0, "");
            _prototypeGuidDict = new(calligraphy.PrototypeDirectory.Records.Length);

            foreach (DataDirectoryPrototypeRecord record in calligraphy.PrototypeDirectory.Records)
            {
                _prototypeHashMap.Add(record.Id, record.FilePath);
                _prototypeGuidDict.Add(record.Guid, record.Id);
            }

            foreach (var kvp in resource.DirectoryDict)
                _prototypeHashMap.Add(kvp.Key, kvp.Value);

            // Enumerate prototypes
            _prototypeEnumDict = new();

            ulong[] allEnumValues = _prototypeHashMap.Enumerate();          // Prototype enum is an array of sorted prototype hashes where id's index in the array is its enum value
            _prototypeEnumDict.Add(PrototypeEnumType.All, allEnumValues);

            // Enumerated hashmap is already sorted, so we just need to filter prototypes according to their blueprint classes
            List<ulong> entityList = new() { 0 };
            List<ulong> inventoryList = new() { 0 };
            List<ulong> powerList = new() { 0 };

            for (int i = 0; i < allEnumValues.Length; i++)
            {
                if (calligraphy.IsCalligraphyPrototype(allEnumValues[i]))   // skip resource prototype ids
                {
                    Blueprint blueprint = calligraphy.GetPrototypeBlueprint(allEnumValues[i]);

                    if (EntityClasses.Contains(blueprint.RuntimeBinding))
                        entityList.Add(allEnumValues[i]);
                    else if (InventoryClasses.Contains(blueprint.RuntimeBinding))
                        inventoryList.Add(allEnumValues[i]);
                    else if (PowerClasses.Contains(blueprint.RuntimeBinding))
                        powerList.Add(allEnumValues[i]);
                }
            }

            _prototypeEnumDict.Add(PrototypeEnumType.Entity, entityList.ToArray());
            _prototypeEnumDict.Add(PrototypeEnumType.Inventory, inventoryList.ToArray());
            _prototypeEnumDict.Add(PrototypeEnumType.Power, powerList.ToArray());

            // Create a dictionary to quickly look up enums from prototypeIds
            _enumLookupDict = new();
            foreach (var kvp in _prototypeEnumDict)
            {
                _enumLookupDict.Add(kvp.Key, new());

                for (int i = 0; i < kvp.Value.Length; i++)
                    _enumLookupDict[kvp.Key].Add(kvp.Value[i], (ulong)i);
            }
        }

        // Direct get methods for internal server use (we trust this input to be valid)
        public string GetPrototypePath(ulong id) => _prototypeHashMap.GetForward(id);
        public ulong GetPrototypeId(string path) => _prototypeHashMap.GetReverse(path);
        public ulong GetPrototypeId(ulong guid) => _prototypeGuidDict[guid];
        public ulong GetPrototypeId(ulong enumValue, PrototypeEnumType type) => _prototypeEnumDict[type][enumValue];
        public ulong GetEnumValue(ulong prototypeId, PrototypeEnumType type) => _enumLookupDict[type][prototypeId];

        // TryGet methods for handling client input (we don't trust clients)
        public bool TryGetPrototypePath(ulong id, out string path) => _prototypeHashMap.TryGetForward(id, out path);
        public bool TryGetPrototypeId(string path, out ulong id) => _prototypeHashMap.TryGetReverse(path, out id);
        public bool TryGetPrototypeId(ulong guid, out ulong id) => _prototypeGuidDict.TryGetValue(guid, out id);
        public bool TryGetPrototypeId(ulong enumValue, PrototypeEnumType type, out ulong id)
        {
            if ((int)enumValue < _prototypeEnumDict[type].Length)
            {
                id = _prototypeEnumDict[type][enumValue];
                return true;
            }
            else
            {
                id = 0;
                return false;
            }
        }
        public bool TryGetEnumValue(ulong prototypeId, PrototypeEnumType type, out ulong enumValue) => _enumLookupDict[type].TryGetValue(prototypeId, out enumValue);


        public bool Verify()
        {
            return _prototypeHashMap.Count > 0
                && _prototypeEnumDict[PrototypeEnumType.All].Length > 0
                && _prototypeEnumDict[PrototypeEnumType.Entity].Length > 0
                && _prototypeEnumDict[PrototypeEnumType.Inventory].Length > 0
                && _prototypeEnumDict[PrototypeEnumType.Power].Length > 0;
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
    }
}
