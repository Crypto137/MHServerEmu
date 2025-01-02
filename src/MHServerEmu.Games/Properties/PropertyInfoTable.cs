using System.Diagnostics;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties.Evals;

namespace MHServerEmu.Games.Properties
{
    public class PropertyInfoTable
    {
        // Number of properties for allocating property info list, 1030 is the number for version 1.52
        // This is for optimization only and should not affect functionality.
        private const int ExpectedNumberOfProperties = 1030;

        private static readonly Logger Logger = LogManager.CreateLogger();

        public static readonly (string, Type)[] AssetEnumBindings = new(string, Type)[]     // s_PropertyParamEnumLookups
        {
            ("ProcTriggerType",                 typeof(ProcTriggerType)),
            ("DamageType",                      typeof(DamageType)),
            ("TargetRestrictionType",           typeof(TargetRestrictionType)),
            ("PowerEventType",                  typeof(PowerEventType)),
            ("LootDropEventType",               typeof(LootDropEventType)),
            ("LootDropActionType",              typeof(LootActionType)),
            ("PowerConditionType",              typeof(ConditionType)),
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

        private readonly List<PropertyInfo> _propertyInfoList = new(ExpectedNumberOfProperties);
        private readonly Dictionary<PrototypeId, PropertyEnum> _prototypeIdToPropertyEnumDict = new();

        public void Initialize()
        {
            var stopwatch = Stopwatch.StartNew();

            var dataDirectory = GameDatabase.DataDirectory;

            var propertyBlueprintId = dataDirectory.PropertyBlueprint;
            var propertyInfoBlueprintId = dataDirectory.PropertyInfoBlueprint;
            var propertyInfoDefaultPrototypeId = dataDirectory.GetBlueprintDefaultPrototype(propertyInfoBlueprintId);

            // Initialize property info list
            foreach (PropertyEnum propertyEnum in Enum.GetValues<PropertyEnum>())
            {
                if (propertyEnum == PropertyEnum.Invalid) continue;
                _propertyInfoList.Add(null);
            }

            // Create property infos
            foreach (PrototypeId propertyInfoPrototypeRef in dataDirectory.IteratePrototypesInHierarchy(propertyInfoBlueprintId))
            {
                if (propertyInfoPrototypeRef == propertyInfoDefaultPrototypeId) continue;

                string prototypeName = GameDatabase.GetPrototypeName(propertyInfoPrototypeRef);
                string propertyName = Path.GetFileNameWithoutExtension(prototypeName);

                // Note: in the client there are enums that are not pre-defined in the property enum. The game handles this
                // by adding them to the property info table here, but we just have them in the enum.
                // See PropertyEnum.cs for more details.
                var propertyEnum = Enum.Parse<PropertyEnum>(propertyName);

                // Add data ref -> property enum lookup
                _prototypeIdToPropertyEnumDict.Add(propertyInfoPrototypeRef, propertyEnum);

                // Create property info instance
                _propertyInfoList[(int)propertyEnum] = new(propertyEnum, propertyName, propertyInfoPrototypeRef);
            }

            // Match property infos with mixin prototypes where possible
            foreach (var blueprint in GameDatabase.DataDirectory.IterateBlueprints())
            {
                // Skip irrelevant blueprints
                if (blueprint.Id == propertyBlueprintId) continue;
                if (blueprint.RuntimeBindingClassType != typeof(PropertyPrototype)) continue;

                // Get property name from blueprint file path
                string propertyBlueprintName = GameDatabase.GetBlueprintName(blueprint.Id);
                string propertyName = Path.GetFileNameWithoutExtension(propertyBlueprintName);

                // Try to find a matching property info for this property mixin
                bool infoFound = false;
                foreach (var propertyInfo in _propertyInfoList)
                {
                    // Property mixin blueprints are inconsistently named: most have the Prop suffix, but some do not
                    if (propertyInfo.PropertyName == propertyName || propertyInfo.PropertyInfoName == propertyName)
                    {
                        blueprint.SetPropertyPrototypeDataRef(propertyInfo.PrototypeDataRef);
                        propertyInfo.PropertyMixinBlueprintRef = blueprint.Id;
                        infoFound = true;
                        break;
                    }
                }

                // All mixins should have a matching info. If this goes off, something went wrong
                if (infoFound == false)
                    Logger.Warn($"Failed to find matching property info for property mixin {propertyName}");
            }

            // Preload infos
            foreach (var propertyInfo in _propertyInfoList)
                LoadPropertyInfo(propertyInfo);                

            // Preload property default prototypes
            foreach (var propertyPrototypeRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy< PropertyPrototype>())
                GameDatabase.GetPrototype<Prototype>(propertyPrototypeRef);

            // Initialize eval dependencies
            foreach (PropertyInfo evalInfo in _propertyInfoList)
            {
                if (evalInfo.IsEvalProperty == false || evalInfo.IsEvalAlwaysCalculated) continue;

                Eval.GetEvalPropertyInputs(evalInfo, evalInfo.EvalDependencies);
                foreach (PropertyId propertyId in evalInfo.EvalDependencies)
                {
                    PropertyInfo dependencyInfo = _propertyInfoList[(int)propertyId.Enum];
                    dependencyInfo.DependentEvals.Add(evalInfo.Id);
                }
            }

            // Check for cyclic dependencies
            Stack<PropertyId> checkStack = new();
            foreach (PropertyInfo info in _propertyInfoList)
            {
                checkStack.Push(info.Id);

                while (checkStack.Count > 0)
                {
                    PropertyInfo checkInfo = _propertyInfoList[(int)checkStack.Pop().Enum];
                    foreach (PropertyId evalId in checkInfo.DependentEvals)
                    {
                        if (evalId == info.Id)
                        {
                            Logger.Warn($"Initialize(): Cyclic property eval dependency found in property {info.PropertyInfoName}");
                            continue;
                        }

                        checkStack.Push(evalId);
                    }
                }
            }

            // Calculate default values for enum properties
            List<bool> evalDoneList = new(_propertyInfoList.Count);
            for (int i = 0; i < _propertyInfoList.Count; i++)
                evalDoneList.Add(false);

            Queue<PropertyEnum> evalQueue = new();
            foreach (PropertyInfo info in _propertyInfoList)
            {
                if (info.IsEvalProperty == false) continue;
                evalQueue.Enqueue(info.Id.Enum);
            }

            using PropertyCollection dummyCollection = ObjectPoolManager.Instance.Get<PropertyCollection>();
            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, dummyCollection);
            evalContext.SetReadOnlyVar_PropertyId(EvalContext.Var1, PropertyId.Invalid);

            while (evalQueue.Count > 0)
            {
                PropertyEnum evalPropertyEnum = evalQueue.Dequeue();
                PropertyInfo info = _propertyInfoList[(int)evalPropertyEnum];

                // Check all dependencies to make sure input has been calculated
                bool hasInput = true;
                foreach (PropertyId dependencyId in info.EvalDependencies)
                {
                    int dependencyIndex = (int)dependencyId.Enum;
                    PropertyInfo dependencyInfo = _propertyInfoList[dependencyIndex];
                    if (dependencyInfo.IsEvalProperty && evalDoneList[dependencyIndex] == false)
                    {
                        hasInput = false;
                        break;
                    }
                }

                // Add the property back to the queue if we are not ready to run its eval yet
                if (hasInput == false)
                {
                    evalQueue.Enqueue(evalPropertyEnum);
                    continue;
                }

                if (info.IsEvalAlwaysCalculated == false)
                    info.SetEvalDefaultValue(PropertyCollection.EvalProperty(info.Id, evalContext));

                evalDoneList[(int)evalPropertyEnum] = true;
            }

            // Finish initialization
            Logger.Info($"Initialized info for {_propertyInfoList.Count} properties in {stopwatch.ElapsedMilliseconds} ms");
        }

        public PropertyInfo LookupPropertyInfo(PropertyEnum propertyEnum)
        {
            if (propertyEnum == PropertyEnum.Invalid)
                Logger.WarnReturn<PropertyInfo>(null, "LookupPropertyInfo(): propertyEnum == PropertyEnum.Invalid");

            return _propertyInfoList[(int)propertyEnum];
        }

        public PropertyEnum GetPropertyEnumFromPrototype(PrototypeId propertyDataRef)
        {
            if (_prototypeIdToPropertyEnumDict.TryGetValue(propertyDataRef, out var propertyEnum) == false)
                return PropertyEnum.Invalid;

            return propertyEnum;
        }

        private bool LoadPropertyInfo(PropertyInfo propertyInfo)
        {
            if (propertyInfo.IsFullyLoaded) return true;

            // Load mixin property prototype if there is one
            if (propertyInfo.PropertyMixinBlueprintRef != BlueprintId.Invalid)
            {
                Blueprint blueprint = GameDatabase.GetBlueprint(propertyInfo.PropertyMixinBlueprintRef);
                GameDatabase.GetPrototype<PropertyPrototype>(blueprint.DefaultPrototypeId);
            }

            // Load the property info prototype and assign it to the property info instance
            if (propertyInfo.PrototypeDataRef != PrototypeId.Invalid)
            {
                var propertyInfoPrototype = GameDatabase.GetPrototype<PropertyInfoPrototype>(propertyInfo.PrototypeDataRef);
                propertyInfo.SetPropertyInfoPrototype(propertyInfoPrototype);
            }

            propertyInfo.IsFullyLoaded = true;
            return true;    // propertyInfo.VerifyPropertyInfo()
        }
    }
}
