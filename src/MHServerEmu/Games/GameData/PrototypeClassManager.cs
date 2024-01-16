using System.Diagnostics;
using System.Reflection;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.GameData
{
    // We use C# types and reflection instead of class ids / class info and GRTTI

    public class PrototypeClassManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<string, Type> _prototypeNameToClassTypeDict = new();

        public int ClassCount { get => _prototypeNameToClassTypeDict.Count; }

        public PrototypeClassManager()
        {
            var stopwatch = Stopwatch.StartNew();

            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (PrototypeClassIsA(type, typeof(Prototype)) == false) continue;  // Skip non-prototype classes
                _prototypeNameToClassTypeDict.Add(type.Name, type);
            }

            stopwatch.Stop();
            Logger.Info($"Initialized {ClassCount} prototype classes in {stopwatch.ElapsedMilliseconds} ms");
        }

        /// <summary>
        /// Gets prototype class type by its name.
        /// </summary>
        public Type GetPrototypeClassTypeByName(string name)
        {
            if (_prototypeNameToClassTypeDict.TryGetValue(name, out Type type) == false)
            {
                Logger.Warn($"Prototype class {name} not found");
                return null;
            }

            return type;
        }

        /// <summary>
        /// Checks if a prototype class belongs to the specified parent class in the hierarchy.
        /// </summary>
        public bool PrototypeClassIsA(Type classToCheck, Type parent)
        {
            return classToCheck == parent || classToCheck.IsSubclassOf(parent);
        }

        /// <summary>
        /// Returns an IEnumerable of all prototype class types.
        /// </summary>
        public IEnumerable<Type> GetEnumerator()
        {
            return _prototypeNameToClassTypeDict.Values.AsEnumerable();
        }

        /// <summary>
        /// Determines what asset types to bind to what enums and 
        /// </summary>
        public void BindAssetTypesToEnums(AssetDirectory assetDirectory)
        {
            Dictionary<AssetType, Type> assetEnumBindingDict = new();

            // TODO: determine what assets to bind to what enums here

            // Iterate through all fields in all prototype classes and find enums
            /*
            foreach (Type classType in _prototypeNameToClassTypeDict.Values)
            {
                foreach (var property in classType.GetProperties())
                {
                    if (property.PropertyType.IsEnum && property.PropertyType.IsDefined(typeof(AssetEnumAttribute)) == false)
                    {
                        Logger.Debug(property.PropertyType.Name);
                    }
                }
            }
            */

            // Add bindings explicitly defined in PropertyInfoTable
            foreach (var binding in PropertyInfoTable.AssetEnumBindings)
            {
                AssetType assetType = assetDirectory.GetAssetType(binding.Item1);
                assetEnumBindingDict.Add(assetType, binding.Item2);
            }

            assetDirectory.BindAssetTypes(assetEnumBindingDict);
        }

        /// <summary>
        /// Returns a <see cref="System.Reflection.PropertyInfo"/> for a field in a Calligraphy prototype.
        /// </summary>
        public System.Reflection.PropertyInfo GetFieldInfo(Type prototypeClassType, BlueprintMemberInfo blueprintMemberInfo, bool getPropertyCollection)
        {
            if (getPropertyCollection == false)
            {
                // Return the C# property info the blueprint member is bound to if we are not looking for a property collection
                return blueprintMemberInfo.Member.RuntimeClassFieldInfo;
            }

            // TODO: look for a property collection field for this prototype
            return null;
        }

        /// <summary>
        /// Returns a <see cref="System.Reflection.PropertyInfo"/> for a mixin field in a Calligraphy prototype.
        /// </summary>
        public System.Reflection.PropertyInfo GetMixinFieldInfo(Type ownerClassType, Type fieldClassType, Type mixinAttribute)
        {
            // Make sure we have a valid attribute type
            if ((mixinAttribute == typeof(MixinAttribute) || mixinAttribute == typeof(ListMixinAttribute)) == false)
                throw new ArgumentException($"{mixinAttribute.Name} is not a mixin attribute.");

            // Search the entire class hierarchy for a mixin of the matching type
            while (ownerClassType != typeof(Prototype))
            {
                // We do what PrototypeFieldSet::GetMixinFieldInfo() does right here using reflection
                foreach (var property in ownerClassType.GetProperties())
                {
                    if (property.IsDefined(mixinAttribute) == false) continue;  // We look for a property that has our mixin attribute defined

                    if (mixinAttribute == typeof(MixinAttribute))
                    {
                        // For simple mixins we just return the property if it matches our field type
                        if (property.PropertyType == fieldClassType)
                            return property;
                    }
                    else if (mixinAttribute == typeof(ListMixinAttribute))
                    {
                        // For list mixins we have to make sure the list is compatible with our requested field type
                        var attribute = property.GetCustomAttribute<ListMixinAttribute>();
                        if (attribute.FieldType == fieldClassType)
                            return property;
                    }
                }

                // Go up in the hierarchy if not found
                ownerClassType = ownerClassType.BaseType;
            }

            // Mixin not found
            return null;
        }
    }
}
