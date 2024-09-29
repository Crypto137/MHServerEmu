using System.Reflection;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.GameData.Calligraphy
{
    public partial class CalligraphySerializer
    {
        private delegate bool ParseDelegate(in FieldParserParams @params);

        private static readonly Dictionary<PrototypeFieldType, ParseDelegate> ParseDelegateDict = new()
        {
            { PrototypeFieldType.Bool,                  ParseBool },
            { PrototypeFieldType.Int8,                  ParseInt8 },
            { PrototypeFieldType.Int16,                 ParseInt16 },
            { PrototypeFieldType.Int32,                 ParseInt32 },
            { PrototypeFieldType.Int64,                 ParseInt64 },
            { PrototypeFieldType.Float32,               ParseFloat32 },
            { PrototypeFieldType.Float64,               ParseFloat64 },
            { PrototypeFieldType.Enum,                  ParseEnum },
            { PrototypeFieldType.AssetRef,              ParseDataRef },
            { PrototypeFieldType.AssetTypeRef,          ParseDataRef },
            { PrototypeFieldType.CurveRef,              ParseDataRef },
            { PrototypeFieldType.PrototypeDataRef,      ParseDataRef },
            { PrototypeFieldType.LocaleStringId,        ParseDataRef },
            { PrototypeFieldType.PrototypePtr,          ParsePrototypePtr },
            { PrototypeFieldType.PropertyId,            ParsePropertyId },
            { PrototypeFieldType.ListBool,              ParseListBool },
            { PrototypeFieldType.ListInt8,              ParseListInt8 },
            { PrototypeFieldType.ListInt16,             ParseListInt16 },
            { PrototypeFieldType.ListInt32,             ParseListInt32 },
            { PrototypeFieldType.ListInt64,             ParseListInt64 },
            { PrototypeFieldType.ListFloat32,           ParseListFloat32 },
            { PrototypeFieldType.ListFloat64,           ParseListFloat64 },
            { PrototypeFieldType.ListEnum,              ParseListEnum },
            { PrototypeFieldType.ListAssetRef,          ParseListAssetRef },
            { PrototypeFieldType.ListAssetTypeRef,      ParseListAssetTypeRef },
            { PrototypeFieldType.ListPrototypeDataRef,  ParseListPrototypeDataRef },
            { PrototypeFieldType.ListPrototypePtr,      ParseListPrototypePtr },
            { PrototypeFieldType.PropertyCollection,    ParsePropertyList },
        };

        /// <summary>
        /// Returns a <see cref="ParseDelegate"/> for the specified <see cref="PrototypeFieldType"/> enum value.
        /// </summary>
        private static ParseDelegate GetParser(PrototypeFieldType prototypeFieldType)
        {
            if (ParseDelegateDict.TryGetValue(prototypeFieldType, out ParseDelegate parser) == false)
                return Logger.ErrorReturn<ParseDelegate>(null, $"GetParser(): Unsupported prototype field type {prototypeFieldType}");

            return parser;
        }

        /// <summary>
        /// Parses a <see cref="bool"/> value and assigns it to a prototype field.
        /// </summary>
        private static bool ParseBool(in FieldParserParams @params)
        {
            long rawValue = @params.Reader.ReadInt64();
            @params.FieldInfo.SetValue(@params.OwnerPrototype, rawValue != 0);
            return true;
        }

        /// <summary>
        /// Parses an <see cref="sbyte"/> value and assigns it to a prototype field.
        /// </summary>
        private static bool ParseInt8(in FieldParserParams @params)
        {
            long rawValue = @params.Reader.ReadInt64();
            @params.FieldInfo.SetValue(@params.OwnerPrototype, (sbyte)rawValue);
            return true;
        }

        /// <summary>
        /// Parses a <see cref="short"/> value and assigns it to a prototype field.
        /// </summary>
        private static bool ParseInt16(in FieldParserParams @params)
        {
            long rawValue = @params.Reader.ReadInt64();
            @params.FieldInfo.SetValue(@params.OwnerPrototype, (short)rawValue);
            return true;
        }

        /// <summary>
        /// Parses an <see cref="int"/> value and assigns it to a prototype field.
        /// </summary>
        private static bool ParseInt32(in FieldParserParams @params)
        {
            long rawValue = @params.Reader.ReadInt64();

            // Some prototypes (e.g. ProceduralProfileDrDoomPhase1.defaults) use very high values for int fields that cause overflows.
            // The client handles this by taking the first 4 bytes of the value and throwing away everything else.
            // We handle this by setting those fields to int.MaxValue, since the intention is apparently to have the value be as high
            // as possible. This doesn't seem to happen with other types.
            if (rawValue > int.MaxValue)
            {
                Logger.Trace($"ParseInt32(): Overflow for Int32 field {@params.BlueprintMemberInfo.Member.FieldName}, raw value {rawValue}, file name {@params.FileName}");
                rawValue = int.MaxValue;
            }

            @params.FieldInfo.SetValue(@params.OwnerPrototype, (int)rawValue);
            return true;
        }

        /// <summary>
        /// Parses a <see cref="long"/> value and assigns it to a prototype field.
        /// </summary>
        private static bool ParseInt64(in FieldParserParams @params)
        {
            long rawValue = @params.Reader.ReadInt64();
            @params.FieldInfo.SetValue(@params.OwnerPrototype, rawValue);
            return true;
        }

        /// <summary>
        /// Parses a <see cref="float"/> value and assigns it to a prototype field.
        /// </summary>
        private static bool ParseFloat32(in FieldParserParams @params)
        {
            double rawValue = @params.Reader.ReadDouble();
            @params.FieldInfo.SetValue(@params.OwnerPrototype, (float)rawValue);
            return true;
        }

        /// <summary>
        /// Parses a <see cref="double"/> value and assigns it to a prototype field.
        /// </summary>
        private static bool ParseFloat64(in FieldParserParams @params)
        {
            double rawValue = @params.Reader.ReadDouble();
            @params.FieldInfo.SetValue(@params.OwnerPrototype, rawValue);
            return true;
        }

        /// <summary>
        /// Parses an asset enum and assigns it to a prototype field.
        /// </summary>
        private static bool ParseEnum(in FieldParserParams @params)
        {
            // Enums are represented in Calligraphy by assets.
            // We get asset name from the serialized asset id, and then parse the actual enum value from it.
            AssetId assetId = (AssetId)@params.Reader.ReadUInt64();
            string assetName = GameDatabase.GetAssetName(assetId);

            // Fix asset names that start with a digit (C# doesn't allow enum members to start with a digit)
            if (assetName.Length > 0 && char.IsDigit(assetName[0]))
                assetName = $"_{assetName}";

            // Try to parse enum value from its name
            if (Enum.TryParse(@params.FieldInfo.PropertyType, assetName, true, out object value) == false)
            {
                if (assetName != string.Empty)
                    Logger.Trace(string.Format("ParseEnum(): Missing enum member {0} in {1}, field {2}, file name {3}",
                        assetName,
                        @params.FieldInfo.PropertyType.Name,
                        @params.BlueprintMemberInfo.Member.RuntimeClassFieldInfo.Name,
                        @params.FileName));

                // Set value to default for enums we can't parse
                AssetEnumAttribute attribute = @params.FieldInfo.PropertyType.GetCustomAttribute<AssetEnumAttribute>();
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
        private static bool ParseDataRef(in FieldParserParams @params)
        {
            // Data refs can be StringId, AssetTypeId, CurveId, PrototypeId, or LocaleStringId.
            // C# enums are not picky when assigning values with reflection, so we can reuse the same code for all of them.
            ulong value = @params.Reader.ReadUInt64();
            @params.FieldInfo.SetValue(@params.OwnerPrototype, value);
            return true;
        }

        /// <summary>
        /// Deserializes an embedded prototype and assigns it to a prototype field.
        /// </summary>
        private static bool ParsePrototypePtr(in FieldParserParams @params)
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
        private static bool DeserializePrototypePtr(in FieldParserParams @params, bool polymorphicSetAllowed, out Prototype prototype)
        {
            prototype = null;
            BinaryReader reader = @params.Reader;

            // Parse header
            PrototypeDataHeader header = new(reader);

            if (header.ReferenceExists == false)
                return true;   // Early return if this is an empty prototype

            if (header.PolymorphicData && (polymorphicSetAllowed == false))
                return Logger.ErrorReturn(false, $"DeserializePrototypePtr(): Polymorphic prototype data encountered but not expected");
            
            // If this prototype has no data of its own, but it references a parent, we interpret it as its parent
            if (header.InstanceDataExists == false)
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
        /// Parses a <see cref="PropertyId"/> and assigns it to a prototype field.
        /// </summary>
        private static bool ParsePropertyId(in FieldParserParams @params)
        {
            BinaryReader reader = @params.Reader;
            PrototypeDataHeader header = new(reader);

            if (header.InstanceDataExists)
            {
                short numFieldGroups = reader.ReadInt16();
                for (int i = 0; i < numFieldGroups; i++)
                {
                    BlueprintId groupBlueprintId = (BlueprintId)reader.ReadUInt64();
                    byte blueprintCopyNum = reader.ReadByte();

                    // Get the field group blueprint and make sure it is bound to a property
                    Blueprint groupBlueprint = GameDatabase.GetBlueprint(groupBlueprintId);
                    if (groupBlueprint.IsProperty() == false)
                        return Logger.ErrorReturn(false, "ParsePropertyId(): Group blueprint is not bound to a property");

                    // Deserialize the property id and assign it to the field
                    PropertyId propertyId = PropertyId.Invalid;
                    DeserializeFieldGroupIntoPropertyId(ref propertyId, groupBlueprint, @params.FileName, reader, "Property List");
                    @params.FieldInfo.SetValue(@params.OwnerPrototype, propertyId);

                    // Same as in DeserializePropertyMixin(), there should be no list fields
                    short numListFields = reader.ReadInt16();
                    if (numListFields != 0)
                        return Logger.ErrorReturn(false, $"ParsePropertyId(): Property field group numListFields != 0");
                }
            }
            else if (header.ReferenceExists)
            {
                // If there is no data but a reference to a parent exists, get default property id from parent blueprint
                Blueprint parentBlueprint = GameDatabase.DataDirectory.GetPrototypeBlueprint(header.ReferenceType);

                if (parentBlueprint.IsProperty() == false)
                    return Logger.ErrorReturn(false, "ParsePropertyId(): Parent blueprint is not bound to a property");

                PrototypeId propertyDataRef = parentBlueprint.PropertyPrototypeRef;
                PropertyEnum propertyEnum = GameDatabase.PropertyInfoTable.GetPropertyEnumFromPrototype(propertyDataRef);

                if (propertyEnum == PropertyEnum.Invalid)
                    return Logger.ErrorReturn(false, "ParsePropertyId(): Parent property enum value is invalid");

                Properties.PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEnum);
                PropertyId defaultId = new(propertyEnum, info.DefaultParamValues);
                @params.FieldInfo.SetValue(@params.OwnerPrototype, defaultId);
            }

            return true;
        }

        /// <summary>
        /// Parses a collection of <see cref="bool"/> values and assigns it to a prototype field.
        /// </summary>
        private static bool ParseListBool(in FieldParserParams @params)
        {
            BinaryReader reader = @params.Reader;

            bool[] values = new bool[reader.ReadInt16()];
            for (int i = 0; i < values.Length; i++)
            {
                long rawValue = reader.ReadInt64();
                values[i] = rawValue != 0;
            }

            @params.FieldInfo.SetValue(@params.OwnerPrototype, values);
            return true;
        }

        /// <summary>
        /// Parses a collection of <see cref="sbyte"/> values and assigns it to a prototype field.
        /// </summary>
        private static bool ParseListInt8(in FieldParserParams @params)
        {
            BinaryReader reader = @params.Reader;

            sbyte[] values = new sbyte[reader.ReadInt16()];
            for (int i = 0; i < values.Length; i++)
            {
                long rawValue = reader.ReadInt64();
                values[i] = (sbyte)rawValue;
            }

            @params.FieldInfo.SetValue(@params.OwnerPrototype, values);
            return true;
        }

        /// <summary>
        /// Parses a collection of <see cref="short"/> values and assigns it to a prototype field.
        /// </summary>
        private static bool ParseListInt16(in FieldParserParams @params)
        {
            BinaryReader reader = @params.Reader;

            short[] values = new short[reader.ReadInt16()];
            for (int i = 0; i < values.Length; i++)
            {
                long rawValue = reader.ReadInt64();
                values[i] = (short)rawValue;
            }

            @params.FieldInfo.SetValue(@params.OwnerPrototype, values);
            return true;
        }

        /// <summary>
        /// Parses a collection of <see cref="int"/> values and assigns it to a prototype field.
        /// </summary>
        private static bool ParseListInt32(in FieldParserParams @params)
        {
            BinaryReader reader = @params.Reader;

            int[] values = new int[reader.ReadInt16()];
            for (int i = 0; i < values.Length; i++)
            {
                long rawValue = reader.ReadInt64();
                values[i] = (int)rawValue;
            }

            @params.FieldInfo.SetValue(@params.OwnerPrototype, values);
            return true;
        }

        /// <summary>
        /// Parses a collection of <see cref="long"/> values and assigns it to a prototype field.
        /// </summary>
        private static bool ParseListInt64(in FieldParserParams @params)
        {
            BinaryReader reader = @params.Reader;

            long[] values = new long[reader.ReadInt16()];
            for (int i = 0; i < values.Length; i++)
            {
                long rawValue = reader.ReadInt64();
                values[i] = rawValue;
            }

            @params.FieldInfo.SetValue(@params.OwnerPrototype, values);
            return true;
        }

        /// <summary>
        /// Parses a collection of <see cref="float"/> values and assigns it to a prototype field.
        /// </summary>
        private static bool ParseListFloat32(in FieldParserParams @params)
        {
            BinaryReader reader = @params.Reader;

            float[] values = new float[reader.ReadInt16()];
            for (int i = 0; i < values.Length; i++)
            {
                double rawValue = reader.ReadDouble();
                values[i] = (float)rawValue;
            }

            @params.FieldInfo.SetValue(@params.OwnerPrototype, values);
            return true;
        }

        /// <summary>
        /// Parses a collection of <see cref="double"/> values and assigns it to a prototype field.
        /// </summary>
        private static bool ParseListFloat64(in FieldParserParams @params)
        {
            BinaryReader reader = @params.Reader;

            double[] values = new double[reader.ReadInt16()];
            for (int i = 0; i < values.Length; i++)
            {
                double rawValue = reader.ReadDouble();
                values[i] = rawValue;
            }

            @params.FieldInfo.SetValue(@params.OwnerPrototype, values);
            return true;
        }

        /// <summary>
        /// Parses a collection of asset enum values and assigns it to a prototype field.
        /// </summary>
        private static bool ParseListEnum(in FieldParserParams @params)
        {
            BinaryReader reader = @params.Reader;

            // We have only type info for our enum, so we have to use Array.CreateInstance() to create our enum array
            Array values = Array.CreateInstance(@params.FieldInfo.PropertyType.GetElementType(), reader.ReadInt16());
            for (int i = 0; i < values.Length; i++)
            {
                // Enums are represented in Calligraphy by assets.
                // We get asset name from the serialized asset id, and then parse the actual enum value from it.
                AssetId assetId = (AssetId)reader.ReadUInt64();
                string assetName = GameDatabase.GetAssetName(assetId);

                // Looks like there are no numeric or invalid enum values in list enums, so we can speed this up
                // by just parsing whatever asset name we have as is.
                object value = Enum.Parse(@params.FieldInfo.PropertyType.GetElementType(), assetName, true);
                values.SetValue(value, i);
            }

            @params.FieldInfo.SetValue(@params.OwnerPrototype, values);
            return true;
        }

        /// <summary>
        /// Parses a collection of <see cref="AssetId"/> values and assigns it to a prototype field.
        /// </summary>
        private static bool ParseListAssetRef(in FieldParserParams @params)
        {
            BinaryReader reader = @params.Reader;

            AssetId[] values = new AssetId[reader.ReadInt16()];
            for (int i = 0; i < values.Length; i++)
            {
                ulong rawValue = reader.ReadUInt64();
                values[i] = (AssetId)rawValue;
            }

            @params.FieldInfo.SetValue(@params.OwnerPrototype, values);
            return true;
        }

        /// <summary>
        /// Parses a collection of <see cref="AssetTypeId"/> values and assigns it to a prototype field.
        /// </summary>
        private static bool ParseListAssetTypeRef(in FieldParserParams @params)
        {
            BinaryReader reader = @params.Reader;

            AssetTypeId[] values = new AssetTypeId[reader.ReadInt16()];
            for (int i = 0; i < values.Length; i++)
            {
                ulong rawValue = reader.ReadUInt64();
                values[i] = (AssetTypeId)rawValue;
            }

            @params.FieldInfo.SetValue(@params.OwnerPrototype, values);
            return true;
        }

        /// <summary>
        /// Parses a collection of <see cref="PrototypeId"/> values and assigns it to a prototype field.
        /// </summary>
        private static bool ParseListPrototypeDataRef(in FieldParserParams @params)
        {
            BinaryReader reader = @params.Reader;

            PrototypeId[] values = new PrototypeId[reader.ReadInt16()];
            for (int i = 0; i < values.Length; i++)
            {
                ulong rawValue = reader.ReadUInt64();
                values[i] = (PrototypeId)rawValue;
            }

            @params.FieldInfo.SetValue(@params.OwnerPrototype, values);
            return true;
        }

        /// <summary>
        /// Parses a collection of embedded prototypes and assigns it to a prototype field.
        /// </summary>
        private static bool ParseListPrototypePtr(in FieldParserParams @params)
        {
            BinaryReader reader = @params.Reader;

            Array values = Array.CreateInstance(@params.FieldInfo.PropertyType.GetElementType(), reader.ReadInt16());
            for (int i = 0; i < values.Length; i++)
            {
                DeserializePrototypePtr(@params, true, out Prototype prototype);
                values.SetValue(prototype, i);
            }

            @params.FieldInfo.SetValue(@params.OwnerPrototype, values);
            return true;
        }

        /// <summary>
        /// Parses a <see cref="PrototypePropertyCollection"/> from a serialized collection of embedded prototypes and assigns it to a prototype field.
        /// </summary>
        private static bool ParsePropertyList(in FieldParserParams @params)
        {
            // PropertyList seems to be used only in ModPrototype
            PrototypePropertyCollection propertyCollection = GetPropertyCollectionField(@params.OwnerPrototype);
            if (propertyCollection == null)
                return Logger.ErrorReturn(false, $"ParsePropertyList(): Failed to get a property collection, file name {@params.FileName}");

            BinaryReader reader = @params.Reader;

            short numItems = reader.ReadInt16();
            for (int i = 0; i < numItems; i++)
            {
                PrototypeDataHeader header = new(reader);
                if (header.InstanceDataExists == false) continue;

                short numFieldGroups = reader.ReadInt16();
                for (int j = 0; j < numFieldGroups; j++)
                {
                    BlueprintId groupBlueprintId = (BlueprintId)reader.ReadUInt64();
                    byte blueprintCopyNum = reader.ReadByte();

                    // Get the field group blueprint and make sure it is bound to a property
                    Blueprint groupBlueprint = GameDatabase.GetBlueprint(groupBlueprintId);
                    if (groupBlueprint.IsProperty() == false)
                        return Logger.ErrorReturn(false, "ParsePropertyList(): Group blueprint is not bound to a property");

                    DeserializeFieldGroupIntoProperty(propertyCollection, groupBlueprint, blueprintCopyNum, @params.FileName, reader, "PropertyList");

                    short numListFields = reader.ReadInt16();
                    if (numListFields != 0)
                        return Logger.ErrorReturn(false, $"ParsePropertyList(): Property field group numListFields != 0");
                }
            }

            return true;
        }

        /// <summary>
        /// Contains parameters for field parsing methods.
        /// </summary>
        private readonly struct FieldParserParams
        {
            public readonly BinaryReader Reader;
            public readonly System.Reflection.PropertyInfo FieldInfo;
            public readonly Prototype OwnerPrototype;
            public readonly Blueprint OwnerBlueprint;
            public readonly string FileName;
            public readonly BlueprintMemberInfo BlueprintMemberInfo;

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
