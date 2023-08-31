using MHServerEmu.Common;
using MHServerEmu.GameServer.GameData.Gpak;
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
            foreach (var kvp in calligraphy.DefaultsDict)
            {
                if (kvp.Key.Contains("Calligraphy/Property/Info"))
                {
                    PropertyEnum property = (PropertyEnum)Enum.Parse(typeof(PropertyEnum), Path.GetFileNameWithoutExtension(kvp.Key));
                    PropertyInfoPrototype prototype = new(kvp.Value);

                    _propertyInfoDict.Add(property, prototype);
                }
                else if (kvp.Key.Contains("Calligraphy/Property/Mixin") && kvp.Key.Contains("Prop.defaults"))   // param mixin information is stored in PropertyPrototypes
                {
                    string fileName = Path.GetFileNameWithoutExtension(kvp.Key);
                    PropertyEnum property = (PropertyEnum)Enum.Parse(typeof(PropertyEnum), fileName.Substring(0, fileName.Length - 4));
                    PropertyPrototype mixin = new(kvp.Value);
                    mixinDict.Add(property, mixin);
                }
            }

            // Manually add property info missed by the loop
            try
            {
                _propertyInfoDict.Add(PropertyEnum.DisplayNameOverride,
                    new(calligraphy.PrototypeDict["Calligraphy/Property/Info/DisplayNameOverride.prototype"]));

                _propertyInfoDict.Add(PropertyEnum.MissileAlwaysCollides,
                    new(calligraphy.DefaultsDict["Calligraphy/Property/Mixin/BewareOfTiger/MissileAlwaysCollides.defaults"]));

                _propertyInfoDict.Add(PropertyEnum.StolenPowerAvailable,
                    new(calligraphy.DefaultsDict["Calligraphy/Property/Mixin/BewareOfTiger/StolenPowerAvailable.defaults"]));
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
