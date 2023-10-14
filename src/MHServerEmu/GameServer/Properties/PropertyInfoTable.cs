using MHServerEmu.Common.Logging;
using MHServerEmu.GameServer.GameData;
using MHServerEmu.GameServer.GameData.Gpak;
using MHServerEmu.GameServer.GameData.Gpak.FileFormats;
using MHServerEmu.GameServer.GameData.Prototypes;

namespace MHServerEmu.GameServer.Properties
{
    public class PropertyInfoTable
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private Dictionary<PropertyEnum, PropertyInfoPrototype> _propertyInfoDict = new();

        public PropertyInfoTable(CalligraphyStorage calligraphy)
        {
            Dictionary<PropertyEnum, PropertyPrototype> mixinDict = new();

            // Loop through the main property info directory to get most info

            // hacky reimplementation for compatibility
            ulong[] blueprintIds = GameDatabase.BlueprintRefManager.Enumerate();

            foreach (ulong blueprintId in blueprintIds)
            {
                string filePath = GameDatabase.GetBlueprintName(blueprintId);

                if (filePath.Contains("Property/Info"))
                {
                    PropertyEnum property = (PropertyEnum)Enum.Parse(typeof(PropertyEnum), Path.GetFileNameWithoutExtension(filePath));
                    PropertyInfoPrototype prototype = new(calligraphy.GetBlueprintDefaultPrototype(filePath));

                    _propertyInfoDict.Add(property, prototype);
                }
                else if (filePath.Contains("Property/Mixin") && filePath.Contains("Prop.blueprint"))   // param mixin information is stored in PropertyPrototypes
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    PropertyEnum property = (PropertyEnum)Enum.Parse(typeof(PropertyEnum), fileName.Substring(0, fileName.Length - 4)); // -4 to remove Prop at the end
                    PropertyPrototype mixin = new(calligraphy.GetBlueprintDefaultPrototype(filePath));
                    mixinDict.Add(property, mixin);
                }
            }

            // Manually add property info missed by the loop
            try
            {
                _propertyInfoDict.Add(PropertyEnum.DisplayNameOverride,
                    new(calligraphy.GetPrototype("Property/Info/DisplayNameOverride.prototype")));

                _propertyInfoDict.Add(PropertyEnum.MissileAlwaysCollides,
                    new(calligraphy.GetBlueprintDefaultPrototype("Property/Mixin/BewareOfTiger/MissileAlwaysCollides.blueprint")));

                _propertyInfoDict.Add(PropertyEnum.StolenPowerAvailable,
                    new(calligraphy.GetBlueprintDefaultPrototype("Property/Mixin/BewareOfTiger/StolenPowerAvailable.blueprint")));
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
                Logger.Info($"Loaded info for {_propertyInfoDict.Count} properties");
            else
                Logger.Error("Failed to initialize PropertyInfoTable");
        }

        public bool Verify() => _propertyInfoDict.Count > 0;
        public PropertyInfoPrototype GetInfo(PropertyEnum property) => _propertyInfoDict[property];
    }
}
