using MHServerEmu.Common;
using MHServerEmu.GameServer.GameData;
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
            // Loop through the main property info directory to get most info
            foreach (var kvp in calligraphy.DefaultsDict)
            {
                if (kvp.Key.Contains("Calligraphy/Property/Info"))
                {
                    PropertyEnum property = (PropertyEnum)Enum.Parse(typeof(PropertyEnum), Path.GetFileNameWithoutExtension(kvp.Key));
                    PropertyInfoPrototype prototype = new(kvp.Value);

                    _propertyInfoDict.Add(property, prototype);
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
