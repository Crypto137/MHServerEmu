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

            var propertyBlueprintId = GameDatabase.DataDirectory.PropertyBlueprint;
            var propertyInfoBlueprintId = GameDatabase.DataDirectory.PropertyInfoBlueprint;
            var propertyInfoDefaultPrototypeId = GameDatabase.DataDirectory.GetBlueprintDefaultPrototype(propertyInfoBlueprintId);

            foreach (Prototype propertyInfo in GameDatabase.DataDirectory.IteratePrototypesInHierarchy(propertyInfoBlueprintId))
            {
                if (propertyInfo.DataRef == propertyInfoDefaultPrototypeId) continue;
                var propertyEnum = Enum.Parse<PropertyEnum>(Path.GetFileNameWithoutExtension(GameDatabase.GetPrototypeName(propertyInfo.DataRef)));
                _propertyInfoDict.Add(propertyEnum, (PropertyInfoPrototype)propertyInfo);
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
