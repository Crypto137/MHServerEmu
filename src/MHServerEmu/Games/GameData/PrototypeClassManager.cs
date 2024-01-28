using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.GameData
{
    // We use C# types and reflection instead of class ids / class info and GRTTI

    public class PrototypeClassManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<string, Type> _prototypeNameToClassTypeDict = new();
        private readonly Dictionary<Type, Func<Prototype>> _prototypeConstructorDict;
        private readonly Dictionary<System.Reflection.PropertyInfo, PrototypeFieldType> _prototypeFieldTypeDict = new();

        private static readonly Dictionary<Type, PrototypeFieldType> TypeToPrototypeFieldTypeEnumDict = new()
        {
            { typeof(bool),                         PrototypeFieldType.Bool },
            { typeof(sbyte),                        PrototypeFieldType.Int8 },
            { typeof(short),                        PrototypeFieldType.Int16 },
            { typeof(int),                          PrototypeFieldType.Int32 },
            { typeof(long),                         PrototypeFieldType.Int64 },
            { typeof(float),                        PrototypeFieldType.Float32 },
            { typeof(double),                       PrototypeFieldType.Float64 },
            { typeof(Enum),                         PrototypeFieldType.Enum },
            { typeof(AssetId),                      PrototypeFieldType.AssetRef },
            { typeof(AssetTypeId),                  PrototypeFieldType.AssetTypeRef },
            { typeof(CurveId),                      PrototypeFieldType.CurveRef },
            { typeof(PrototypeId),                  PrototypeFieldType.PrototypeDataRef },
            { typeof(LocaleStringId),               PrototypeFieldType.LocaleStringId },
            { typeof(Prototype),                    PrototypeFieldType.PrototypePtr },
            { typeof(PropertyId),                   PrototypeFieldType.PropertyId },
            { typeof(bool[]),                       PrototypeFieldType.ListBool },
            { typeof(sbyte[]),                      PrototypeFieldType.ListInt8 },
            { typeof(short[]),                      PrototypeFieldType.ListInt16 },
            { typeof(int[]),                        PrototypeFieldType.ListInt32 },
            { typeof(long[]),                       PrototypeFieldType.ListInt64 },
            { typeof(float[]),                      PrototypeFieldType.ListFloat32 },
            { typeof(double[]),                     PrototypeFieldType.ListFloat64 },
            { typeof(Enum[]),                       PrototypeFieldType.ListEnum },
            { typeof(AssetId[]),                    PrototypeFieldType.ListAssetRef },
            { typeof(AssetTypeId[]),                PrototypeFieldType.ListAssetTypeRef },
            { typeof(PrototypeId[]),                PrototypeFieldType.ListPrototypeDataRef },
            { typeof(Prototype[]),                  PrototypeFieldType.ListPrototypePtr },
            { typeof(List<PrototypeMixinListItem>), PrototypeFieldType.ListMixin },
            { typeof(PrototypePropertyCollection),  PrototypeFieldType.PropertyCollection }   // FIXME: Separate PropertyCollection from PropertyList somehow?
        };

        public int ClassCount { get => _prototypeNameToClassTypeDict.Count; }

        public PrototypeClassManager()
        {
            var stopwatch = Stopwatch.StartNew();

            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (PrototypeClassIsA(type, typeof(Prototype)) == false) continue;  // Skip non-prototype classes
                _prototypeNameToClassTypeDict.Add(type.Name, type);
            }

            _prototypeConstructorDict = new(ClassCount);

            stopwatch.Stop();
            Logger.Info($"Initialized {ClassCount} prototype classes in {stopwatch.ElapsedMilliseconds} ms");
        }

        /// <summary>
        /// Creates a new <see cref="Prototype"/> instance of the specified <see cref="Type"/> using a cached constructor delegate if possible.
        /// </summary>
        public Prototype AllocatePrototype(Type type)
        {
            // Check if we already have a cached constructor delegate
            if (_prototypeConstructorDict.TryGetValue(type, out var constructorDelegate) == false)
            {
                // Cache constructor delegate for future use
                var constructor = type.GetConstructor(Type.EmptyTypes);
                var newExpression = Expression.New(constructor);
                var lambdaExpression = Expression.Lambda<Func<Prototype>>(newExpression);
                constructorDelegate = lambdaExpression.Compile();
                _prototypeConstructorDict.Add(type, constructorDelegate);
            }

            return constructorDelegate();
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
                    if (mixinAttribute == typeof(MixinAttribute))
                    {
                        // For simple mixins we just return the property if it matches our field type and has the correct attribute
                        if (property.PropertyType != fieldClassType) continue;
                        if (property.IsDefined(mixinAttribute)) return property;
                    }
                    else if (mixinAttribute == typeof(ListMixinAttribute))
                    {
                        // For list mixins we look for a list that is compatible with our requested field type
                        if (property.PropertyType != typeof(List<PrototypeMixinListItem>)) continue;

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

        /// <summary>
        /// Returns a matching <see cref="PrototypeFieldType"/> enum value for a <see cref="System.Reflection.PropertyInfo"/>.
        /// </summary>
        public PrototypeFieldType GetPrototypeFieldTypeEnumValue(System.Reflection.PropertyInfo fieldInfo)
        {
            // Retrieve an already matched enum value if we have one for this property
            if (_prototypeFieldTypeDict.TryGetValue(fieldInfo, out var prototypeFieldTypeEnumValue) == false)
            {
                if (fieldInfo.IsDefined(typeof(DoNotCopyAttribute)))
                {
                    _prototypeFieldTypeDict.Add(fieldInfo, PrototypeFieldType.Invalid);
                    return PrototypeFieldType.Invalid;
                }

                var fieldType = fieldInfo.PropertyType;

                // Adjust non-primitive types
                if (fieldType.IsPrimitive == false)
                {
                    if (fieldType.IsArray == false)
                    {
                        // Check if this is a mixin or a list mixin
                        if (PrototypeIsMixin(fieldType))
                            return PrototypeFieldType.Mixin;
                        else if (fieldType == typeof(List<PrototypeMixinListItem>))
                            return PrototypeFieldType.ListMixin;

                        // Check the type itself if it's a simple field
                        if (fieldType.IsSubclassOf(typeof(Prototype)))
                            fieldType = typeof(Prototype);
                        else if (fieldType.IsEnum && fieldType.IsDefined(typeof(AssetEnumAttribute)))
                            fieldType = typeof(Enum);
                    }
                    else
                    {
                        // Check element type instead if it's a collection
                        var elementType = fieldType.GetElementType();

                        if (elementType.IsSubclassOf(typeof(Prototype)))
                            fieldType = typeof(Prototype[]);
                        else if (elementType.IsEnum && elementType.IsDefined(typeof(AssetEnumAttribute)))
                            fieldType = typeof(Enum[]);
                    }
                }

                // Try to match a C# type to a prototype field type enum value using a lookup dict
                if (TypeToPrototypeFieldTypeEnumDict.TryGetValue(fieldType, out prototypeFieldTypeEnumValue) == false)
                    prototypeFieldTypeEnumValue = PrototypeFieldType.Invalid;

                // There is an issue with using PropertyInfo as a key: PropertyInfos for inherited properties are different on each
                // level of inheritance, which causes this code to be called more often than necessary.
                _prototypeFieldTypeDict.Add(fieldInfo, prototypeFieldTypeEnumValue);
            }

            return prototypeFieldTypeEnumValue;
        }

        public void PostProcessContainedPrototypes(Prototype prototype)
        {
            foreach (var property in prototype.GetType().GetProperties())
            {
                if (property.DeclaringType == typeof(Prototype)) continue;

                switch (GetPrototypeFieldTypeEnumValue(property))
                {
                    case PrototypeFieldType.PrototypePtr:
                    case PrototypeFieldType.Mixin:
                        // Simple embedded prototypes
                        var embeddedPrototype = (Prototype)property.GetValue(prototype);
                        embeddedPrototype?.PostProcess();
                        break;


                    case PrototypeFieldType.ListPrototypePtr:
                        // List / vector collections of embedded prototypes (that we implemented as arrays)
                        var prototypeCollection = (IEnumerable<Prototype>)property.GetValue(prototype);
                        if (prototypeCollection == null) continue;
                        foreach (var element in prototypeCollection)
                            element.PostProcess();
                        break;

                    case PrototypeFieldType.ListMixin:
                        var mixinList = (List<PrototypeMixinListItem>)property.GetValue(prototype);
                        if (mixinList == null) continue;
                        foreach (var mixin in mixinList)
                            mixin.Prototype.PostProcess();
                        break;
                }
            }
        }

        private bool PrototypeIsMixin(Type type)
        {
            // Speed hack: instead of calling IsDefined() we just check if the type is one of mixin prototype types
            return type == typeof(LocomotorPrototype) || type == typeof(PopulationInfoPrototype) || type == typeof(ProductPrototype);
        }
    }
}
