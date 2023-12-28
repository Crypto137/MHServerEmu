using System.Diagnostics;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Properties
{
    public class PropertyInfoTable
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<PropertyEnum, PropertyInfo> _propertyInfoDict = new();

        public static readonly (string, Type)[] AssetEnumBindings = new(string, Type)[]     // s_PropertyParamEnumLookups
        {
            ("ProcTriggerType",                 typeof(ProcTriggerType)),
            ("DamageType",                      typeof(DamageType)),
            ("TargetRestrictionType",           typeof(TargetRestrictionType)),
            ("PowerEventType",                  typeof(PowerEventType)),
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

        public PropertyInfoTable()
        {
            var stopwatch = Stopwatch.StartNew();

            var dataDirectory = GameDatabase.DataDirectory;

            var propertyBlueprintId = dataDirectory.PropertyBlueprint;
            var propertyInfoBlueprintId = dataDirectory.PropertyInfoBlueprint;
            var propertyInfoDefaultPrototypeId = dataDirectory.GetBlueprintDefaultPrototype(propertyInfoBlueprintId);

            foreach (PrototypeId propertyInfoPrototypeRef in dataDirectory.IteratePrototypesInHierarchy(propertyInfoBlueprintId))
            {
                if (propertyInfoPrototypeRef == propertyInfoDefaultPrototypeId) continue;

                string prototypeName = GameDatabase.GetPrototypeName(propertyInfoPrototypeRef);
                string propertyName = Path.GetFileNameWithoutExtension(prototypeName);

                // Note: in the client there are enums that are not pre-defined in the property enum. The game handles this
                // by adding them to the property info table here, but we just have them in the enum.
                // See PropertyEnum.cs for more details.
                var propertyEnum = Enum.Parse<PropertyEnum>(propertyName);

                PropertyInfo propertyInfo = new(propertyEnum, propertyName, propertyInfoPrototypeRef);
                LoadPropertyInfo(propertyInfo);
                _propertyInfoDict.Add(propertyEnum, propertyInfo);
            }

            // Finish initialization
            if (Verify())
                Logger.Info($"Loaded info for {_propertyInfoDict.Count} properties in {stopwatch.ElapsedMilliseconds} ms");
            else
                Logger.Error("Failed to initialize PropertyInfoTable");
        }

        public PropertyInfo LookupPropertyInfo(PropertyEnum property)
        {
            return _propertyInfoDict[property];
        }

        public bool Verify() => _propertyInfoDict.Count > 0;

        private void LoadPropertyInfo(PropertyInfo propertyInfo)
        {
            propertyInfo.PropertyInfoPrototype = GameDatabase.GetPrototype<PropertyInfoPrototype>(propertyInfo.PropertyInfoPrototypeRef);
        }
    }
}
