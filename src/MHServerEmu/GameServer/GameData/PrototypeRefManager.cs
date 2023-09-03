using MHServerEmu.Common;
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

        private HashMap _prototypeHashMap;
        private Dictionary<PrototypeEnumType, ulong[]> _prototypeEnumDict;                  // For enum -> prototypeId conversion
        private Dictionary<PrototypeEnumType, Dictionary<ulong, ulong>> _enumLookupDict;    // For prototypeId -> enum conversion

        public PrototypeRefManager(CalligraphyStorage calligraphy, ResourceStorage resource)
        {
            // Generate a hash map for all prototypes (Calligraphy + Resource)
            _prototypeHashMap = InitializePrototypeHashMap(calligraphy, resource);

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

                    if (EntityClasses.Contains(blueprint.ClassName))
                        entityList.Add(allEnumValues[i]);
                    else if (InventoryClasses.Contains(blueprint.ClassName))
                        inventoryList.Add(allEnumValues[i]);
                    else if (PowerClasses.Contains(blueprint.ClassName))
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

        public string GetPrototypePath(ulong id) => _prototypeHashMap.GetForward(id);
        public ulong GetPrototypeId(string path) => _prototypeHashMap.GetReverse(path);

        public ulong GetPrototypeId(ulong enumValue, PrototypeEnumType type) => _prototypeEnumDict[type][enumValue];
        public ulong GetEnumValue(ulong prototypeId, PrototypeEnumType type) => _enumLookupDict[type][prototypeId];
        public int GetMaxEnumValue() => _enumLookupDict[PrototypeEnumType.All].Count - 1;

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

        private static HashMap InitializePrototypeHashMap(CalligraphyStorage calligraphy, ResourceStorage resource)
        {
            HashMap hashMap;

            if (calligraphy.PrototypeDirectory != null && resource.DirectoryDict.Count > 0)
            {
                hashMap = new(calligraphy.PrototypeDirectory.Entries.Length + resource.DirectoryDict.Count);
                hashMap.Add(0, "");

                foreach (DataDirectoryPrototypeEntry entry in calligraphy.PrototypeDirectory.Entries)
                    hashMap.Add(entry.Id1, entry.FilePath);

                foreach (var kvp in resource.DirectoryDict)
                    hashMap.Add(kvp.Key, kvp.Value);
            }
            else
            {
                hashMap = new();
            }

            return hashMap;
        }
    }
}
