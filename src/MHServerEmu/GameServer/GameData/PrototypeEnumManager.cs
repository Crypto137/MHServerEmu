using MHServerEmu.Common.Logging;
using MHServerEmu.GameServer.GameData.Calligraphy;
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

    public class PrototypeEnumManager
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

        private Dictionary<PrototypeEnumType, ulong[]> _prototypeEnumDict;                  // EnumValue -> PrototypeId
        private Dictionary<PrototypeEnumType, Dictionary<ulong, ulong>> _enumLookupDict;    // PrototypeId -> EnumValue

        public int MaxEnumValue { get => _enumLookupDict[PrototypeEnumType.All].Count - 1; }

        public PrototypeEnumManager()
        {
            // Enumerate prototypes
            _prototypeEnumDict = new();

            // Prototype enum is an array of sorted prototype hashes where id's index in the array is its enum value
            List<ulong> allEnumValueList = new() { 0 };
            allEnumValueList.AddRange(GameDatabase.PrototypeRefManager.Enumerate());
            ulong[] allEnumValues = allEnumValueList.ToArray();

            _prototypeEnumDict.Add(PrototypeEnumType.All, allEnumValues);

            // Enumerated hashmap is already sorted, so we just need to filter prototypes according to their blueprint classes
            List<ulong> entityList = new() { 0 };
            List<ulong> inventoryList = new() { 0 };
            List<ulong> powerList = new() { 0 };

            CalligraphyStorage calligraphy = GameDatabase.Calligraphy;

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

        public ulong GetPrototypeId(ulong enumValue, PrototypeEnumType type) => _prototypeEnumDict[type][enumValue];
        public ulong GetEnumValue(ulong prototypeId, PrototypeEnumType type) => _enumLookupDict[type][prototypeId];

        public bool Verify()
        {
            return _prototypeEnumDict[PrototypeEnumType.All].Length > 0
                && _prototypeEnumDict[PrototypeEnumType.Entity].Length > 0
                && _prototypeEnumDict[PrototypeEnumType.Inventory].Length > 0
                && _prototypeEnumDict[PrototypeEnumType.Power].Length > 0;
        }

        public List<ulong> GetPowerPropertyIdList(string filter)
        {
            ulong[] powerTable = _prototypeEnumDict[PrototypeEnumType.Power];
            List<ulong> propertyIdList = new();

            for (int i = 1; i < powerTable.Length; i++)
                if (GameDatabase.GetPrototypeName(powerTable[i]).Contains(filter))
                    propertyIdList.Add(DataHelper.ReconstructPowerPropertyIdFromHash((ulong)i));

            return propertyIdList;
        }
    }
}
