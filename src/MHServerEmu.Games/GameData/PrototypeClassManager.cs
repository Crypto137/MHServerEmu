using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.GameData.PatchManager;
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

        private readonly Dictionary<Type, CachedPrototypeField[]> _copyableFieldDict = new();
        private readonly Dictionary<Type, CachedPrototypeField[]> _postProcessableFieldDict = new();

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
            { typeof(PrototypeMixinList),           PrototypeFieldType.ListMixin },
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
                DynamicMethod dm = new("ConstructPrototype", typeof(Prototype), null);
                ILGenerator il = dm.GetILGenerator();

                il.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
                il.Emit(OpCodes.Ret);

                constructorDelegate = dm.CreateDelegate<Func<Prototype>>();
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
        public System.Reflection.PropertyInfo GetFieldInfo(Type prototypeClassType, BlueprintMemberInfo? blueprintMemberInfo, bool getPropertyCollection)
        {
            // Return the C# property info the blueprint member is bound to if we are not looking for a property collection
            if (getPropertyCollection == false)
                return blueprintMemberInfo?.Member.RuntimeClassFieldInfo;

            // Look for a property collection field for this prototype
            // Same as in CalligraphySerializer.GetPropertyCollection(), we make use of the fact that
            // all property collection fields in our data are called "Properties".
            // The client here iterates all fields to find the one that is the property collection.
            return prototypeClassType.GetProperty("Properties");
        }

        /// <summary>
        /// Returns a <see cref="System.Reflection.PropertyInfo"/> for a mixin field in a Calligraphy prototype.
        /// </summary>
        public System.Reflection.PropertyInfo GetMixinFieldInfo(Type ownerClassType, Type fieldClassType, PrototypeFieldType fieldType)
        {
            // Make sure we have a valid field type enum value
            if ((fieldType == PrototypeFieldType.Mixin || fieldType == PrototypeFieldType.ListMixin) == false)
                throw new ArgumentException($"{fieldType} is not a mixin field type.");

            // Search the entire class hierarchy for a mixin of the matching type
            while (ownerClassType != typeof(Prototype))
            {
                // We do what PrototypeFieldSet::GetMixinFieldInfo() does right here using reflection
                foreach (var property in ownerClassType.GetProperties())
                {
                    if (fieldType == PrototypeFieldType.Mixin)
                    {
                        // For simple mixins we just return the property if it matches our field type and has the correct attribute
                        if (property.PropertyType != fieldClassType) continue;
                        if (property.IsDefined(typeof(MixinAttribute))) return property;
                    }
                    else if (fieldType == PrototypeFieldType.ListMixin)
                    {
                        // For list mixins we look for a list that is compatible with our requested field type
                        if (property.PropertyType != typeof(PrototypeMixinList)) continue;

                        // NOTE: While we check if the field type defined in the attribute matches our field class type argument exactly,
                        // the client checks if the argument type is derived from the type defined in the field info.
                        // This doesn't seem to cause any issues in 1.52, but may need to be changed if we run into issues with other versions.
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
            // In the client PrototypeFieldType values for all fields are defined in the code. In our implementation
            // we use a combination of C# property types and attributes to determine approximate values.
            // This relies on reflection, which is slow, so we cache the results in a dictionary.

            // Retrieve an already matched enum value if we have one for this property
            if (_prototypeFieldTypeDict.TryGetValue(fieldInfo, out var prototypeFieldTypeEnumValue) == false)
            {
                // There is an issue with using PropertyInfo as a key: PropertyInfos for inherited properties are different on each
                // level of inheritance (because they contain ReflectedType), which causes this code to be called more often than necessary.
                prototypeFieldTypeEnumValue = DeterminePrototypeFieldType(fieldInfo);
                _prototypeFieldTypeDict.Add(fieldInfo, prototypeFieldTypeEnumValue);
            }

            return prototypeFieldTypeEnumValue;
        }

        /// <summary>
        /// Returns copyable fields for a given prototype type.
        /// </summary>
        public CachedPrototypeField[] GetCopyablePrototypeFields(Type type)
        {
            // Cache copyable fields for reuse
            if (_copyableFieldDict.TryGetValue(type, out CachedPrototypeField[] copyableFields) == false)
            {
                List<CachedPrototypeField> copyableFieldList = new();

                // Populate the the new list
                foreach (var fieldInfo in type.GetProperties())
                {
                    // Skip base prototype properties
                    if (fieldInfo.DeclaringType == typeof(Prototype))
                        continue;

                    // Skip uncopyable fields (e.g. DoNotCopy)
                    PrototypeFieldType fieldType = GetPrototypeFieldTypeEnumValue(fieldInfo);
                    if (fieldType == PrototypeFieldType.Invalid)
                        continue;

                    copyableFieldList.Add(new(fieldInfo, fieldType));
                }

                // Convert to array to avoid boxing while iterating
                copyableFields = copyableFieldList.ToArray();
                _copyableFieldDict.Add(type, copyableFields);
            }

            return copyableFields;
        }

        public uint CalculateDataCRC(Prototype prototype)
        {
            // Since we don't have version migration, we can get away with using just the prototype's path crc for now.
            return (uint)((ulong)prototype.DataRef >> 32);
        }

        /// <summary>
        /// Calls PostProcess() on all prototypes embedded in the provided one.
        /// </summary>
        public void PostProcessContainedPrototypes(Prototype prototype)
        {
            bool hasPatch = PrototypePatchManager.Instance.PreCheck(prototype.DataRef);

            foreach (CachedPrototypeField cachedField in GetPostProcessablePrototypeFields(prototype.GetType()))
            {
                System.Reflection.PropertyInfo fieldInfo = cachedField.FieldInfo;

                switch (cachedField.FieldType)
                {
                    case PrototypeFieldType.PrototypePtr:
                    case PrototypeFieldType.Mixin:
                        // Simple embedded prototypes
                        var embeddedPrototype = (Prototype)fieldInfo.GetValue(prototype);
                        if (embeddedPrototype != null)
                        {
                            if (hasPatch) PrototypePatchManager.Instance.SetPath(prototype, embeddedPrototype, fieldInfo.Name);
                            embeddedPrototype.PostProcess();
                        }
                        break;

                    case PrototypeFieldType.ListPrototypePtr:
                        // List / vector collections of embedded prototypes (that we implemented as arrays)
                        var prototypeCollection = (IEnumerable<Prototype>)fieldInfo.GetValue(prototype);
                        if (prototypeCollection == null) continue;

                        int index = 0;
                        foreach (Prototype element in prototypeCollection)
                        {
                            if (hasPatch) PrototypePatchManager.Instance.SetPathIndex(prototype, element, fieldInfo.Name, index++);
                            element.PostProcess();
                        }
                        
                        break;

                    case PrototypeFieldType.ListMixin:
                        var mixinList = (PrototypeMixinList)fieldInfo.GetValue(prototype);
                        if (mixinList == null) continue;

                        foreach (PrototypeMixinListItem mixin in mixinList)
                            mixin.Prototype.PostProcess();
                        
                        break;
                }
            }

            if (hasPatch) PrototypePatchManager.Instance.PostOverride(prototype);
        }

        /// <summary>
        /// PreCheck data of prototype for patch.
        /// </summary>
        public void PreCheck(Prototype prototype)
        {
            bool hasPatch = PrototypePatchManager.Instance.PreCheck(prototype.DataRef);
            if (hasPatch) PrototypePatchManager.Instance.PostOverride(prototype);
        }

        private CachedPrototypeField[] GetPostProcessablePrototypeFields(Type type)
        {
            // Cache post-processable fields for reuse
            if (_postProcessableFieldDict.TryGetValue(type, out CachedPrototypeField[] postProcessableFields) == false)
            {
                List<CachedPrototypeField> postProcessableFieldList = new();

                // Populate the the new list
                foreach (var fieldInfo in type.GetProperties())
                {
                    // Skip base prototype properties
                    if (fieldInfo.DeclaringType == typeof(Prototype))
                        continue;

                    // Add approrite fields
                    PrototypeFieldType fieldType = GetPrototypeFieldTypeEnumValue(fieldInfo);

                    switch (fieldType)
                    {
                        case PrototypeFieldType.PrototypePtr:
                        case PrototypeFieldType.Mixin:
                        case PrototypeFieldType.ListPrototypePtr:
                        case PrototypeFieldType.ListMixin:
                            postProcessableFieldList.Add(new(fieldInfo, fieldType));
                            break;
                    }
                }

                // Convert to array to avoid boxing while iterating
                postProcessableFields = postProcessableFieldList.ToArray();
                _postProcessableFieldDict.Add(type, postProcessableFields);
            }

            return postProcessableFields;
        }

        /// <summary>
        /// Determines a matching <see cref="PrototypeFieldType"/> enum value for a <see cref="System.Reflection.PropertyInfo"/>.
        /// </summary>
        private PrototypeFieldType DeterminePrototypeFieldType(System.Reflection.PropertyInfo fieldInfo)
        {
            // Skip if the field is marked to be ignored
            if (fieldInfo.IsDefined(typeof(DoNotCopyAttribute)))
                return PrototypeFieldType.Invalid;

            var fieldType = fieldInfo.PropertyType;

            // Manually determine some of non-primitive types
            if (fieldType.IsPrimitive == false)
            {
                if (fieldType.IsArray == false)
                {
                    // Check if this is a mixin or a list mixin
                    // Speed hack: instead of calling IsDefined() we just check if the type is one of mixin prototype types
                    if (fieldType == typeof(LocomotorPrototype) || fieldType == typeof(PopulationInfoPrototype) || fieldType == typeof(ProductPrototype))
                        return PrototypeFieldType.Mixin;
                    else if (fieldType == typeof(PrototypeMixinList))
                        return PrototypeFieldType.ListMixin;

                    // Check for prototypes and asset enums
                    // In resource prototypes we consider embedded prototypes as PrototypeFieldType.PrototypePtr (same as Calligraphy),
                    // even though technically they should be just PrototypeFieldType.Prototype. Distinguishing them doesn't seem
                    // to serve any purpose within our implementation of this system as of right now.
                    if (fieldType.IsSubclassOf(typeof(Prototype)))
                        return PrototypeFieldType.PrototypePtr;
                    else if (fieldType.IsEnum && fieldType.IsDefined(typeof(AssetEnumAttribute)))
                        return PrototypeFieldType.Enum;
                }
                else
                {
                    // Check element type instead if it's a collection
                    var elementType = fieldType.GetElementType();

                    if (elementType.IsSubclassOf(typeof(Prototype)))
                        return PrototypeFieldType.ListPrototypePtr;
                    else if (elementType.IsEnum && elementType.IsDefined(typeof(AssetEnumAttribute)))
                        return PrototypeFieldType.ListEnum;
                }
            }

            // Try to match a C# type to a prototype field type enum value using a lookup dict
            if (TypeToPrototypeFieldTypeEnumDict.TryGetValue(fieldType, out var prototypeFieldTypeEnumValue) == false)
                return PrototypeFieldType.Invalid;

            return prototypeFieldTypeEnumValue;
        }

        public readonly struct CachedPrototypeField
        {
            public readonly System.Reflection.PropertyInfo FieldInfo;
            public readonly PrototypeFieldType FieldType;

            public CachedPrototypeField(System.Reflection.PropertyInfo fieldInfo, PrototypeFieldType fieldType)
            {
                FieldInfo = fieldInfo;
                FieldType = fieldType;
            }
        }
    }
}
