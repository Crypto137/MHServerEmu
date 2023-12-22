using MHServerEmu.Common.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using System.Diagnostics;

namespace MHServerEmu.Games.Properties
{
    public class PropertyInfoTable
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<PropertyEnum, PropertyInfoPrototype> _propertyInfoDict = new();

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

            foreach (PrototypeId propertyInfoId in dataDirectory.IteratePrototypesInHierarchy(propertyInfoBlueprintId))
            {
                if (propertyInfoId == propertyInfoDefaultPrototypeId) continue;
                PropertyInfoPrototype propertyInfo = GameDatabase.GetPrototype<PropertyInfoPrototype>(propertyInfoId);
                var propertyEnum = Enum.Parse<PropertyEnum>(Path.GetFileNameWithoutExtension(GameDatabase.GetPrototypeName(propertyInfo.DataRef)));
                _propertyInfoDict.Add(propertyEnum, propertyInfo);
            }

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
