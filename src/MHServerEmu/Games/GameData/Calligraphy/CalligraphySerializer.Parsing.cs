using System.Globalization;
using System.Reflection;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData.Calligraphy
{
    public partial class CalligraphySerializer
    {
        /// <summary>
        /// Returns a parser for the specified <see cref="PrototypeFieldType"/> enum value.
        /// </summary>
        private static Func<FieldParserParams, bool> GetParser(PrototypeFieldType prototypeFieldType)
        {
            switch (prototypeFieldType)
            {
                case PrototypeFieldType.Bool:                   return ParseBool;
                case PrototypeFieldType.Int8:                   return ParseInt8;
                case PrototypeFieldType.Int16:                  return ParseInt16;
                case PrototypeFieldType.Int32:                  return ParseInt32;
                case PrototypeFieldType.Int64:                  return ParseInt64;
                case PrototypeFieldType.Float32:                return ParseFloat32;
                case PrototypeFieldType.Float64:                return ParseFloat64;
                case PrototypeFieldType.Enum:                   return ParseEnum;
                case PrototypeFieldType.AssetRef:
                case PrototypeFieldType.AssetTypeRef:
                case PrototypeFieldType.CurveRef:
                case PrototypeFieldType.PrototypeDataRef:
                case PrototypeFieldType.LocaleStringId:         return ParseDataRef;
                case PrototypeFieldType.PrototypePtr:           return ParsePrototypePtr;
                case PrototypeFieldType.PropertyId:             return ParsePropertyId;
                case PrototypeFieldType.ListBool:               return ParseListBool;
                case PrototypeFieldType.ListInt8:               return ParseListInt8;
                case PrototypeFieldType.ListInt16:              return ParseListInt16;
                case PrototypeFieldType.ListInt32:              return ParseListInt32;
                case PrototypeFieldType.ListInt64:              return ParseListInt64;
                case PrototypeFieldType.ListFloat32:            return ParseListFloat32;
                case PrototypeFieldType.ListFloat64:            return ParseListFloat64;
                case PrototypeFieldType.ListEnum:               return ParseListEnum;
                case PrototypeFieldType.ListAssetRef:
                case PrototypeFieldType.ListAssetTypeRef:
                case PrototypeFieldType.ListPrototypeDataRef:   return ParseListDataRef;
                case PrototypeFieldType.ListPrototypePtr:       return ParseListPrototypePtr;
                case PrototypeFieldType.PropertyCollection:     return ParsePropertyList;

                default: return Logger.WarnReturn<Func<FieldParserParams,bool>>(null, $"Failed to get parser for unsupported prototype field type {prototypeFieldType}");
            }
        }

        /// <summary>
        /// Parses an integer or float value and assigns it to a prototype field.
        /// </summary>
        private static bool ParseValue<T>(FieldParserParams @params, bool parseAsFloat = false) where T: IConvertible
        {
            // Boolean and numeric values are stored in Calligraphy as 64-bit values.
            // We read the value as either Int64 or Float64 and then cast it to the appropriate type for our field.
            var rawValue = parseAsFloat ? @params.Reader.ReadDouble() : @params.Reader.ReadInt64();

            // Some prototypes (e.g. ProceduralProfileDrDoomPhase1.defaults) use very high values for int fields that cause overflows.
            // The client handles this by taking the first 4 bytes of the value and throwing away everything else.
            // We handle this by setting those fields to int.MaxValue, since the intention is apparently to have the value be as high
            // as possible. This doesn't seem to happen with other types.
            if (typeof(T) == typeof(int) && rawValue > int.MaxValue)
            {
                Logger.Trace($"ParseValue overflow for Int32 field {@params.BlueprintMemberInfo.Member.FieldName}, raw value {rawValue}, file name {@params.FileName}");
                rawValue = int.MaxValue;
            }

            var value = Convert.ChangeType(rawValue, typeof(T), CultureInfo.InvariantCulture);
            @params.FieldInfo.SetValue(@params.OwnerPrototype, value);
            return true;
        }

        /// <summary>
        /// Parses an asset enum and assigns it to a prototype field.
        /// </summary>
        private static bool ParseEnum(FieldParserParams @params)
        {
            // Enums are represented in Calligraphy by assets.
            // We get asset name from the serialized asset id, and then parse the actual enum value from it.
            var assetId = (AssetId)@params.Reader.ReadUInt64();
            var assetName = GameDatabase.GetAssetName(assetId);

            // Fix asset names that start with a digit (C# doesn't allow enum members to start with a digit)
            if (assetName.Length > 0 && char.IsDigit(assetName[0]))
                assetName = $"_{assetName}";

            // Try to parse enum value from its name
            if (Enum.TryParse(@params.FieldInfo.PropertyType, assetName, true, out var value) == false)
            {
                if (assetName != string.Empty)
                    Logger.Trace(string.Format("Missing enum member {0} in {1}, field {2}, file name {3}",
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
            // Data refs can be StringId, AssetTypeId, CurveId, PrototypeId, or LocaleStringId.
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
            prototype = GameDatabase.PrototypeClassManager.AllocatePrototype(classType);

            DoDeserialize(prototype, header, PrototypeId.Invalid, @params.FileName, reader);
            return true;
        }

        /// <summary>
        /// Parses a PropertyId and assigns it to a prototype field.
        /// </summary>
        private static bool ParsePropertyId(FieldParserParams @params)
        {
            var reader = @params.Reader;
            PrototypeDataHeader header = new(reader);

            if (header.DataExists)
            {
                short numFieldGroups = reader.ReadInt16();
                for (int i = 0; i < numFieldGroups; i++)
                {
                    var groupBlueprintId = (BlueprintId)reader.ReadUInt64();
                    byte blueprintCopyNum = reader.ReadByte();

                    // Get the field group blueprint and make sure it's a property one
                    var groupBlueprint = GameDatabase.GetBlueprint(groupBlueprintId);
                    if (groupBlueprint.IsProperty() == false)
                        Logger.WarnReturn(false, "Failed to parse PropertyId field: the specified group blueprint is not a property");

                    // TODO: deserializeFieldGroupIntoPropertyId
                    // For now skip by reading and throwing away this data
                    short numSimpleFields = reader.ReadInt16();
                    for (int j = 0; j < numSimpleFields; j++)
                    {
                        var fieldId = (StringId)reader.ReadUInt64();
                        var type = (CalligraphyBaseType)reader.ReadByte();
                        var value = reader.ReadUInt64();
                    }

                    // Same as in DeserializePropertyMixin, there should be no list fields
                    short numListFields = reader.ReadInt16();
                    if (numListFields != 0) Logger.Warn($"Property field group numListFields != 0");
                }
            }
            else if (header.ReferenceExists)
            {
                // TODO: Get parent PropertyId from reference
            }
            else
            {
                //Logger.Trace($"Empty PropertyId field, file name {@params.FileName}");
            }

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
                var assetId = (AssetId)@params.Reader.ReadUInt64();
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

            // Same as with enums, we use Array.CreateInstance() because we know the type only during runtime
            var elementType = @params.FieldInfo.PropertyType.GetElementType();
            var values = Array.CreateInstance(elementType, reader.ReadInt16());
            for (int i = 0; i < values.Length; i++)
            {
                // All data refs are ulong values strongly typed using enums
                var value = reader.ReadUInt64();
                values.SetValue(Enum.ToObject(elementType, value), i);
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
