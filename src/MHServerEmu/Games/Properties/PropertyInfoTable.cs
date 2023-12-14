using MHServerEmu.Common.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using System.Diagnostics;

namespace MHServerEmu.Games.Properties
{
    public class PropertyInfoTable
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private Dictionary<PropertyEnum, PropertyInfoPrototype> _propertyInfoDict = new();

        public static readonly (string, Type)[] AssetEnumBindings = new(string, Type)[]     // s_PropertyParamEnumLookups
        {
            ("ProcTriggerType",                 typeof(ProcTriggerType)),
            ("DamageType",                      typeof(DamageType)),
            ("TargetRestrictionType",           typeof(TargetRestrictionType)),
            ("PowerEventType",                  typeof(PowerEventActionType)),
            ("LootDropEventType",               typeof(LootDropEventType)),
            ("LootDropActionType",              typeof(LootActionType)),
            ("PowerConditionType",              typeof(PowerConditionType)),
            ("ItemEffectUnrealClass",           null),
            ("HotspotNegateByAllianceType",     typeof(HotspotNegateByAllianceType)),
            ("DEPRECATEDDifficultyMode",        typeof(DEPRECATEDDifficultyMode)),
            ("EntityGameEventEnum",             typeof(EntityGameEventEnum)),
            ("EntitySelectorActionEventType",   typeof(EntitySelectorActionEventType)),
            ("Weekday",                         typeof(Weekday)),
            ("AffixPositionType",               typeof(AffixPosition)),
            ("ManaType",                        typeof(ManaType)),
            ("Ranks",                           typeof(Rank))
        };

        // Old experimental hacky code below, to be properly re-implemented

        public PropertyInfoTable(DataDirectory dataDirectory)
        {
            var stopwatch = Stopwatch.StartNew();

            Dictionary<PropertyEnum, PropertyPrototype> mixinDict = new();

            // Loop through the main property info directory to get most info

            // hacky reimplementation for compatibility
            BlueprintId[] blueprintIds = GameDatabase.BlueprintRefManager.Enumerate();

            foreach (var blueprintId in blueprintIds)
            {
                string filePath = GameDatabase.GetBlueprintName(blueprintId);

                if (filePath.Contains("Property/Info"))
                {
                    PropertyEnum property = (PropertyEnum)Enum.Parse(typeof(PropertyEnum), Path.GetFileNameWithoutExtension(filePath));
                    PrototypeId defaultPrototypeId = dataDirectory.GetBlueprintDefaultPrototype(blueprintId);
                    PropertyInfoPrototype prototype = new(GameDatabase.GetPrototype<Prototype>(defaultPrototypeId));

                    _propertyInfoDict.Add(property, prototype);
                }
                else if (filePath.Contains("Property/Mixin") && filePath.Contains("Prop.blueprint"))   // param mixin information is stored in PropertyPrototypes
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    PropertyEnum property = (PropertyEnum)Enum.Parse(typeof(PropertyEnum), fileName.Substring(0, fileName.Length - 4)); // -4 to remove Prop at the end
                    PropertyPrototype mixin = new((PrototypeId)blueprintId);
                    mixinDict.Add(property, mixin);
                }
            }

            // Manually add property info missed by the loop
            try
            {
                // Property/Info/DisplayNameOverride.prototype
                _propertyInfoDict.Add(PropertyEnum.DisplayNameOverride,
                    new(dataDirectory.GetPrototype<Prototype>((PrototypeId)14845682279047958969)));

                // Property/Mixin/BewareOfTiger/MissileAlwaysCollides.blueprint
                _propertyInfoDict.Add(PropertyEnum.MissileAlwaysCollides,
                    new(dataDirectory.GetPrototype<Prototype>((PrototypeId)9507546413010851972)));

                // Property/Mixin/BewareOfTiger/StolenPowerAvailable.blueprint
                _propertyInfoDict.Add(PropertyEnum.StolenPowerAvailable,
                    new(dataDirectory.GetPrototype<Prototype>((PrototypeId)11450873518952749073)));
            }
            catch
            {
                Logger.Warn("Failed to manually add additional property info");
            }

            // Add mixin information to PropertyInfo
            foreach (var kvp in mixinDict)
                _propertyInfoDict[kvp.Key].Mixin = kvp.Value;

            // Finish initialization
            if (Verify())
                Logger.Info($"Loaded info for {_propertyInfoDict.Count} properties in {stopwatch.ElapsedMilliseconds} ms");
            else
                Logger.Error("Failed to initialize PropertyInfoTable");
        }

        public bool Verify() => _propertyInfoDict.Count > 0;
        public PropertyInfoPrototype GetInfo(PropertyEnum property) => _propertyInfoDict[property];
    }
}
