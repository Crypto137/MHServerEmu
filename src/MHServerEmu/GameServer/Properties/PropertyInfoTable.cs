using MHServerEmu.Common;
using MHServerEmu.GameServer.GameData;
using MHServerEmu.GameServer.GameData.Prototypes;

namespace MHServerEmu.GameServer.Properties
{
    public static class PropertyInfoTable
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private static Dictionary<PropertyEnum, PropertyInfoPrototype> _propertyInfoDict = new();

        public static bool IsInitialized { get; private set; }

        static PropertyInfoTable()
        {
            // Loop through the main property info directory to get most info
            foreach (var kvp in GameDatabase.Calligraphy.DefaultsDict)
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
                    new(GameDatabase.Calligraphy.PrototypeDict["Calligraphy/Property/Info/DisplayNameOverride.prototype"]));

                _propertyInfoDict.Add(PropertyEnum.MissileAlwaysCollides,
                    new(GameDatabase.Calligraphy.DefaultsDict["Calligraphy/Property/Mixin/BewareOfTiger/MissileAlwaysCollides.defaults"]));

                _propertyInfoDict.Add(PropertyEnum.StolenPowerAvailable,
                    new(GameDatabase.Calligraphy.DefaultsDict["Calligraphy/Property/Mixin/BewareOfTiger/StolenPowerAvailable.defaults"]));
            }
            catch
            {
                Logger.Warn("Failed to manually add additional property info");
            }

            // Finish initialization
            if (_propertyInfoDict.Count > 0)
            {
                Logger.Info($"Loaded info for {_propertyInfoDict.Count} properties");
                IsInitialized = true;
            }
            else
            {
                Logger.Fatal("Failed to initialize PropertyInfoTable");
                IsInitialized = false;
            }
        }

        public static PropertyInfoPrototype GetPropertyInfo(PropertyEnum property) => _propertyInfoDict[property];
    }
}
