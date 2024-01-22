using System.Globalization;
using System.Reflection;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.GameData.Calligraphy
{
    public partial class CalligraphySerializer
    {
        private static readonly Dictionary<Type, Func<FieldParserParams, bool>> ParserDict = new()
        {
            { typeof(bool),             ParseBool },
            { typeof(sbyte),            ParseInt8 },
            { typeof(short),            ParseInt16 },
            { typeof(int),              ParseInt32 },
            { typeof(long),             ParseInt64 },
            { typeof(float),            ParseFloat32 },
            { typeof(double),           ParseFloat64 },
            { typeof(Enum),             ParseEnum },
            { typeof(ulong),            ParseDataRef },         // ulong fields are data refs we haven't defined exact types for yet
            { typeof(StringId),         ParseDataRef },
            { typeof(AssetTypeId),      ParseDataRef },
            { typeof(CurveId),          ParseDataRef },
            { typeof(PrototypeId),      ParseDataRef },
            { typeof(LocaleStringId),   ParseDataRef },
            { typeof(Prototype),        ParsePrototypePtr },
            { typeof(PropertyId),       ParsePropertyId },
            { typeof(bool[]),           ParseListBool },
            { typeof(sbyte[]),          ParseListInt8 },
            { typeof(short[]),          ParseListInt16 },
            { typeof(int[]),            ParseListInt32 },
            { typeof(long[]),           ParseListInt64 },
            { typeof(float[]),          ParseListFloat32 },
            { typeof(double[]),         ParseListFloat64 },
            { typeof(Enum[]),           ParseListEnum },
            { typeof(ulong[]),          ParseListDataRef },     // ulong fields are data refs we haven't defined exact types for yet
            { typeof(StringId[]),       ParseListDataRef },
            { typeof(AssetTypeId[]),    ParseListDataRef },
            { typeof(CurveId[]),        ParseListDataRef },
            { typeof(PrototypeId[]),    ParseListDataRef },
            { typeof(LocaleStringId[]), ParseListDataRef },
            { typeof(Prototype[]),      ParseListPrototypePtr },
            { typeof(PrototypePropertyCollection), ParsePropertyList }      // should this be a property collection or some other type? used only in ModPrototype
        };

        /// <summary>
        /// Returns a parser for the specified prototype field type.
        /// </summary>
        private static Func<FieldParserParams, bool> GetParser(Type prototypeFieldType)
        {
            // Adjust type for enums and prototype pointers
            if (prototypeFieldType.IsPrimitive == false)
            {
                if (prototypeFieldType.IsArray == false)
                {
                    // Check the type itself if it's a simple field
                    if (prototypeFieldType.IsSubclassOf(typeof(Prototype)))
                        prototypeFieldType = typeof(Prototype);
                    else if (prototypeFieldType.IsDefined(typeof(AssetEnumAttribute)))
                        prototypeFieldType = typeof(Enum);
                }
                else
                {
                    // Check element type instead if it's a list field
                    var elementType = prototypeFieldType.GetElementType();

                    if (elementType.IsSubclassOf(typeof(Prototype)))
                        prototypeFieldType = typeof(Prototype[]);
                    else if (elementType.IsDefined(typeof(AssetEnumAttribute)))
                        prototypeFieldType = typeof(Enum[]);
                }
            }

            // Try to get a defined parser from our dict
            if (ParserDict.TryGetValue(prototypeFieldType, out var parser))
                return parser;

            Logger.Warn($"Failed to get parser for unsupported prototype field type {prototypeFieldType.Name}");
            return null;
        }

        /// <summary>
        /// Parses an integer or float value and assigns it to a prototype field.
        /// </summary>
        private static bool ParseValue<T>(FieldParserParams @params, bool parseAsFloat = false) where T: IConvertible
        {
            // Boolean and numeric values are stored in Calligraphy as 64-bit values.
            // We read the value as either Int64 or Float64 and then cast it to the appropriate type for our field.
            var rawValue = parseAsFloat ? @params.Reader.ReadDouble() : @params.Reader.ReadInt64();

            try
            {
                var value = Convert.ChangeType(rawValue, typeof(T), CultureInfo.InvariantCulture);
                @params.FieldInfo.SetValue(@params.OwnerPrototype, value);
            }
            catch (OverflowException)
            {
                // Some prototypes (e.g. ProceduralProfileDrDoomPhase1.defaults) use very high values for int fields that cause overflows.
                // The client handles this by taking the first 4 bytes of the value and throwing away everything else.
                // We handle this by setting those fields to int.MaxValue, since the intention is apparently to have the value be as high
                // as possible. This doesn't seem to happen with other types, but we'll leave this check here just in case.
                if (typeof(T) != typeof(int))
                    throw new($"Unexpected overflow for type {typeof(T).Name}.");

                @params.FieldInfo.SetValue(@params.OwnerPrototype, int.MaxValue);
                Logger.Warn($"ParseValue overflow for field {@params.BlueprintMemberInfo.Member.FieldName} in {@params.FileName}, raw value {rawValue}");
            }
            
            return true;
        }

        /// <summary>
        /// Parses an asset enum and assigns it to a prototype field.
        /// </summary>
        private static bool ParseEnum(FieldParserParams @params)
        {
            // Enums are represented in Calligraphy by assets.
            // We get asset name from the serialized asset id, and then parse the actual enum value from it.
            var assetId = (StringId)@params.Reader.ReadUInt64();
            var assetName = GameDatabase.GetAssetName(assetId);

            // Fix asset names that start with a digit (C# doesn't allow enum members to start with a digit)
            if (assetName.Length > 0 && char.IsDigit(assetName[0]))
                assetName = $"_{assetName}";

            // Try to parse enum value from its name
            if (Enum.TryParse(@params.FieldInfo.PropertyType, assetName, true, out var value) == false)
            {
                if (assetName != string.Empty)
                    Logger.Warn(string.Format("Missing enum member {0} in {1}, field {2}, file name {3}",
                        assetName,
                        @params.FieldInfo.PropertyType.Name,
                        @params.BlueprintMemberInfo.Member.RuntimeClassFieldInfo.Name,
                        @params.FileName));

                // Set value to default for enums we can't parse
                var attribute = @params.FieldInfo.PropertyType.GetCustomAttribute<AssetEnumAttribute>();
                @params.FieldInfo.SetValue(@params.OwnerPrototype, attribute.DefaultValue);
                return true;
            }

            // Set value to what we parsed if everything is okay
            @params.FieldInfo.SetValue(@params.OwnerPrototype, value);
            return true;
        }

        /// <summary>
        /// Parses a data reference and assigns it to a prototype field.
        /// </summary>
        private static bool ParseDataRef(FieldParserParams @params)
        {
            // Data refs can be StringId, AssetTypeId, CurveId, PrototypeId, LocaleStringId or ulong.
            // Eventually we will assign appropriate data ref types to all ulong fields.
            // C# enums are not picky when assigning values with reflection, so we can reuse the same code for all of them.
            var value = @params.Reader.ReadUInt64();
            @params.FieldInfo.SetValue(@params.OwnerPrototype, value);
            return true;
        }

        /// <summary>
        /// Deserializes an embedded prototype and assigns it to a prototype field.
        /// </summary>
        private static bool ParsePrototypePtr(FieldParserParams @params)
        {
            // The client nests multiple methods for deserializing embedded prototypes:
            // ParsePrototypePtr -> deserializePrototypePtr -> deserializePrototypePtrNoTemplate
            // We combine deserializePrototypePtr and deserializePrototypePtrNoTemplate in a single method.
            DeserializePrototypePtr(@params, false, out var prototype);
            @params.FieldInfo.SetValue(@params.OwnerPrototype, prototype);
            return true;
        }

        /// <summary>
        /// Deserializes an embedded prototype WITHOUT assigning it to a field.
        /// </summary>
        private static bool DeserializePrototypePtr(FieldParserParams @params, bool polymorphicSetAllowed, out Prototype prototype)
        {
            prototype = null;
            var reader = @params.Reader;

            // Parse header
            PrototypeDataHeader header = new(reader);
            if (header.ReferenceExists == false) return true;   // Early return if this is an empty prototype
            if (header.PolymorphicData && (polymorphicSetAllowed == false))
                return Logger.WarnReturn(false, $"Polymorphic prototype data encountered but not expected");
            
            // If this prototype has no data of its own, but it references a parent, we interpret it as its parent
            if (header.DataExists == false)
            {
                prototype = GameDatabase.GetPrototype<Prototype>(header.ReferenceType);
                return true;
            }

            // Deserialize
            Type classType = GameDatabase.DataDirectory.GetPrototypeClassType(header.ReferenceType);
            prototype = (Prototype)Activator.CreateInstance(classType);

            DoDeserialize(prototype, header, PrototypeId.Invalid, @params.FileName, reader);
            return true;
        }

        /// <summary>
        /// Parses a property id and assigns it to a prototype field.
        /// </summary>
        private static bool ParsePropertyId(FieldParserParams @params)
        {
            // todo: proper property id deserialization and assignment
            DeserializePrototypePtr(@params, false, out var prototype);
            return true;
        }
        
        /// <summary>
        /// Parses a collection of integer or float values and assigns it to a prototype field.
        /// </summary>
        private static bool ParseCollection<T>(FieldParserParams @params, bool parseAsFloat = false) where T : IConvertible
        {
            var reader = @params.Reader;

            var values = new T[reader.ReadInt16()];
            for (int i = 0; i < values.Length; i++)
            {
                var rawValue = parseAsFloat ? @params.Reader.ReadDouble() : @params.Reader.ReadInt64();
                values[i] = (T)Convert.ChangeType(rawValue, typeof(T), CultureInfo.InvariantCulture);
            }

            @params.FieldInfo.SetValue(@params.OwnerPrototype, values);
            return true;
        }

        /// <summary>
        /// Parses a collection of asset enum values and assigns it to a prototype field.
        /// </summary>
        private static bool ParseListEnum(FieldParserParams @params)
        {
            var reader = @params.Reader;

            // We have only type info for our enum, so we have to use Array.CreateInstance() to create our enum array
            var values = Array.CreateInstance(@params.FieldInfo.PropertyType.GetElementType(), reader.ReadInt16());
            for (int i = 0; i < values.Length; i++)
            {
                // Enums are represented in Calligraphy by assets.
                // We get asset name from the serialized asset id, and then parse the actual enum value from it.
                var assetId = (StringId)@params.Reader.ReadUInt64();
                var assetName = GameDatabase.GetAssetName(assetId);

                // Looks like there are no numeric or invalid enum values in list enums, so we can speed this up
                // by just parsing whatever asset name we have as is.
                var value = Enum.Parse(@params.FieldInfo.PropertyType.GetElementType(), assetName, true);
                values.SetValue(value, i);
            }

            @params.FieldInfo.SetValue(@params.OwnerPrototype, values);
            return true;
        }

        /// <summary>
        /// Parses a collection of data references and assigns it to a prototype field.
        /// </summary>
        private static bool ParseListDataRef(FieldParserParams @params)
        {
            var reader = @params.Reader;

            // Same as with enums, we use Array.CreateInstance()
            var values = Array.CreateInstance(@params.FieldInfo.PropertyType.GetElementType(), reader.ReadInt16());
            for (int i = 0; i < values.Length; i++)
            {
                var value = reader.ReadUInt64();
                values.SetValue(value, i);
            }

            @params.FieldInfo.SetValue(@params.OwnerPrototype, values);
            return true;
        }

        /// <summary>
        /// Parses a collection of embedded prototypes and assigns it to a prototype field.
        /// </summary>
        private static bool ParseListPrototypePtr(FieldParserParams @params)
        {
            var reader = @params.Reader;

            var values = Array.CreateInstance(@params.FieldInfo.PropertyType.GetElementType(), reader.ReadInt16());
            for (int i = 0; i < values.Length; i++)
            {
                DeserializePrototypePtr(@params, true, out var prototype);
                values.SetValue(prototype, i);
            }

            @params.FieldInfo.SetValue(@params.OwnerPrototype, values);
            return true;
        }

        /// <summary>
        /// Parses a property collection and assigns it to a prototype field.
        /// </summary>
        /// <param name="params"></param>
        /// <returns></returns>
        private static bool ParsePropertyList(FieldParserParams @params)
        {
            // todo: proper property list deserialization and assignment
            var reader = @params.Reader;

            short numValues = reader.ReadInt16();
            for (int i = 0; i < numValues; i++)
                DeserializePrototypePtr(@params, true, out var prototype);

            return true;
        }

        private static bool ParseBool(FieldParserParams @params) => ParseValue<bool>(@params);
        private static bool ParseInt8(FieldParserParams @params) => ParseValue<sbyte>(@params);
        private static bool ParseInt16(FieldParserParams @params) => ParseValue<short>(@params);
        private static bool ParseInt32(FieldParserParams @params) => ParseValue<int>(@params);
        private static bool ParseInt64(FieldParserParams @params) => ParseValue<long>(@params);
        private static bool ParseFloat32(FieldParserParams @params) => ParseValue<float>(@params, true);
        private static bool ParseFloat64(FieldParserParams @params) => ParseValue<double>(@params, true);

        private static bool ParseListBool(FieldParserParams @params) => ParseCollection<bool>(@params);
        private static bool ParseListInt8(FieldParserParams @params) => ParseCollection<sbyte>(@params);
        private static bool ParseListInt16(FieldParserParams @params) => ParseCollection<short>(@params);
        private static bool ParseListInt32(FieldParserParams @params) => ParseCollection<int>(@params);
        private static bool ParseListInt64(FieldParserParams @params) => ParseCollection<long>(@params);
        private static bool ParseListFloat32(FieldParserParams @params) => ParseCollection<float>(@params, true);
        private static bool ParseListFloat64(FieldParserParams @params) => ParseCollection<double>(@params, true);

        /// <summary>
        /// Contains parameters for field parsing methods.
        /// </summary>
        private readonly struct FieldParserParams
        {
            public BinaryReader Reader { get; }
            public System.Reflection.PropertyInfo FieldInfo { get; }
            public Prototype OwnerPrototype { get; }
            public Blueprint OwnerBlueprint { get; }
            public string FileName { get; }
            public BlueprintMemberInfo BlueprintMemberInfo { get; }

            public FieldParserParams(BinaryReader reader, System.Reflection.PropertyInfo fieldInfo, Prototype ownerPrototype,
                Blueprint ownerBlueprint, string fileName, BlueprintMemberInfo blueprintMemberInfo)
            {
                Reader = reader;
                FieldInfo = fieldInfo;
                OwnerPrototype = ownerPrototype;
                OwnerBlueprint = ownerBlueprint;
                FileName = fileName;
                BlueprintMemberInfo = blueprintMemberInfo;
            }
        }
    }
}
