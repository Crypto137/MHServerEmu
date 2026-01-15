using System.Diagnostics;
using MHServerEmu.Core.Extensions;
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
            ("Ranks",                           typeof(Rank)),

            // Extra bindings not present in the client here, but scattered across various asset enum lookup instances
            ("RegionBehavior",                  typeof(RegionBehavior)),
        };

        private readonly Dictionary<PrototypeId, PropertyEnum> _prototypeIdToPropertyEnumDict = new();

        private PropertyInfo[] _propertyInfos;

        public void Initialize()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            DataDirectory dataDirectory = GameDatabase.DataDirectory;

            BlueprintId propertyBlueprintId = dataDirectory.PropertyBlueprint;
            BlueprintId propertyInfoBlueprintId = dataDirectory.PropertyInfoBlueprint;
            PrototypeId propertyInfoDefaultPrototypeId = dataDirectory.GetBlueprintDefaultPrototype(propertyInfoBlueprintId);

            // Find properties
            List<PrototypeId> propertyInfoProtoRefs = new((int)Property.EnumMax);
            foreach (PrototypeId propertyInfoProtoRef in dataDirectory.IteratePrototypesInHierarchy(propertyInfoBlueprintId))
            {
                if (propertyInfoProtoRef == propertyInfoDefaultPrototypeId)
                    continue;

                propertyInfoProtoRefs.Add(propertyInfoProtoRef);
            }

            // The client explicitly sorts propertyInfoProtoRefs here, but it's unnecessary because IteratePrototypesInHierarchy() outputs in sorted order.

            // _propertyInfos is also a vector in the client, but it's not really dynamic, so we can just use a fixed array.
            _propertyInfos = new PropertyInfo[propertyInfoProtoRefs.Count];

            // Sanity check in case somebody in the future goes crazy with adding data properties via mods.
            if (_propertyInfos.Length >= (int)Property.EnumMax)
                throw new($"Property count overflow! ");

            // Create property infos
            int numDataProperties = 0;
            foreach (PrototypeId propertyInfoProtoRef in propertyInfoProtoRefs)
            {
                string prototypeName = GameDatabase.GetPrototypeName(propertyInfoProtoRef);
                string propertyName = Path.GetFileNameWithoutExtension(prototypeName);

                // Data-only properties do not have an enum value, in which case they are appended at the end.
                if (Enum.TryParse(propertyName, out PropertyEnum propertyEnum) == false)
                    propertyEnum = PropertyEnum.NumCodeProperties + numDataProperties++;

                // Add data ref -> property enum lookup
                _prototypeIdToPropertyEnumDict.Add(propertyInfoProtoRef, propertyEnum);

                // Create property info instance
                _propertyInfos[(int)propertyEnum] = new(propertyEnum, propertyName, propertyInfoProtoRef);
            }

            // Match property infos with mixin prototypes where possible
            foreach (Blueprint blueprint in dataDirectory.IterateBlueprints())
            {
                // Skip irrelevant blueprints
                if (blueprint.Id == propertyBlueprintId)
                    continue;

                if (blueprint.RuntimeBindingClassType != typeof(PropertyPrototype))
                    continue;

                // Get property name from blueprint file path
                string propertyBlueprintName = GameDatabase.GetBlueprintName(blueprint.Id);
                string propertyName = Path.GetFileNameWithoutExtension(propertyBlueprintName);

                // Try to find a matching property info for this property mixin
                bool infoFound = false;
                foreach (PropertyInfo propertyInfo in _propertyInfos)
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
            foreach (PropertyInfo propertyInfo in _propertyInfos)
                LoadPropertyInfo(propertyInfo);                

            // Preload property default prototypes
            foreach (PrototypeId propertyPrototypeRef in dataDirectory.IteratePrototypesInHierarchy<PropertyPrototype>())
                GameDatabase.GetPrototype<Prototype>(propertyPrototypeRef);

            // Initialize eval dependencies
            foreach (PropertyInfo evalInfo in _propertyInfos)
            {
                if (evalInfo.IsEvalProperty == false || evalInfo.IsEvalAlwaysCalculated)
                    continue;

                Eval.GetEvalPropertyInputs(evalInfo, evalInfo.EvalDependencies);
                foreach (PropertyId propertyId in evalInfo.EvalDependencies)
                {
                    PropertyInfo dependencyInfo = _propertyInfos[(int)propertyId.Enum];
                    dependencyInfo.DependentEvals.Add(evalInfo.Id);
                }
            }

            // Check for cyclic dependencies
            Stack<PropertyId> checkStack = new();
            foreach (PropertyInfo info in _propertyInfos)
            {
                checkStack.Push(info.Id);

                while (checkStack.Count > 0)
                {
                    PropertyInfo checkInfo = _propertyInfos[(int)checkStack.Pop().Enum];
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
            List<bool> evalDoneList = new(_propertyInfos.Length);
            evalDoneList.Fill(false, _propertyInfos.Length);

            Queue<PropertyEnum> evalQueue = new();
            foreach (PropertyInfo info in _propertyInfos)
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
                PropertyInfo info = _propertyInfos[(int)evalPropertyEnum];

                // Check all dependencies to make sure input has been calculated
                bool hasInput = true;
                foreach (PropertyId dependencyId in info.EvalDependencies)
                {
                    int dependencyIndex = (int)dependencyId.Enum;
                    PropertyInfo dependencyInfo = _propertyInfos[dependencyIndex];
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
            stopwatch.Stop();
            Logger.Info($"Initialized info for {_propertyInfos.Length} properties in {(long)stopwatch.Elapsed.TotalMilliseconds} ms");
        }

        public PropertyInfo LookupPropertyInfo(PropertyEnum propertyEnum)
        {
            if (propertyEnum == PropertyEnum.Invalid)
                return Logger.WarnReturn<PropertyInfo>(null, "LookupPropertyInfo(): propertyEnum == PropertyEnum.Invalid");

            return _propertyInfos[(int)propertyEnum];
        }

        public PropertyEnum GetPropertyEnumFromPrototype(PrototypeId propertyDataRef)
        {
            if (_prototypeIdToPropertyEnumDict.TryGetValue(propertyDataRef, out var propertyEnum) == false)
                return PropertyEnum.Invalid;

            return propertyEnum;
        }

        private static bool LoadPropertyInfo(PropertyInfo propertyInfo)
        {
            if (propertyInfo.IsFullyLoaded)
                return true;

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
