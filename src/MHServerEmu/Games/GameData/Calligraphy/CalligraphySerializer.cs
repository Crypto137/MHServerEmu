using MHServerEmu.Common.Logging;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData.Calligraphy
{
    /// <summary>
    /// An implementation of <see cref="GameDataSerializer"/> for Calligraphy prototypes.
    /// </summary>
    public class CalligraphySerializer : GameDataSerializer
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public override void Deserialize(Prototype prototype, PrototypeId dataRef, Stream stream)
        {
            // Temp implementation

            // Set this prototype's id data ref
            prototype.DataRef = dataRef;

            // Deserialize
            using (BinaryReader reader = new(stream))
            {
                // Read Calligraphy prototype file header
                CalligraphyHeader header = new(reader);

                // Temp deserialization
                prototype.DeserializeCalligraphy(reader);

                // Temp hack for property info
                if (prototype is PropertyInfoPrototype propertyInfo)
                    propertyInfo.FillPropertyInfoFields();
            }
        }

        public void DeserializeWip(Prototype prototype, PrototypeId dataRef, Stream stream)
        {
            // WIP proper deserialization
            string prototypeName = GameDatabase.GetPrototypeName(dataRef);

            using (BinaryReader reader = new(stream))
            {
                // Read Calligraphy header
                CalligraphyHeader calligraphyHeader = new(reader);

                // Read prototype header and check it
                PrototypeDataHeader prototypeHeader = new(reader);
                if (prototypeHeader.ReferenceExists == false) return;
                if (prototypeHeader.PolymorphicData) return;

                // Begin deserialization
                DoDeserialize(prototype, prototypeHeader, dataRef, prototypeName, reader);
            }
        }

        /// <summary>
        /// Deserializes data for a Calligraphy prototype.
        /// </summary>
        private void DoDeserialize(Prototype prototype, PrototypeDataHeader header, PrototypeId prototypeDataRef, string prototypeName, BinaryReader reader)
        {
            DataDirectory dataDirectory = GameDatabase.DataDirectory;

            // Set prototype data ref
            prototype.DataRef = prototypeDataRef;

            // Get blueprint
            Blueprint blueprint = dataDirectory.GetPrototypeBlueprint(prototypeDataRef != PrototypeId.Invalid ? prototypeDataRef : header.ReferenceType);

            // Make sure there is data to deserialize
            if (header.ReferenceExists == false) return;

            // Get class type (we get it from the blueprint's binding instead of calling GetRuntimeClassId())
            Type classType = blueprint.RuntimeBindingClassType;

            // Copy parent data if there is any
            if (header.ReferenceType != PrototypeId.Invalid)
            {
                CopyPrototypeDataRefFields(prototype, header.ReferenceType);
                prototype.ParentDataRef = header.ReferenceType;
            }

            // Deserialize this prototype's data if there is any
            if (header.DataExists == false) return;

            short numFieldGroups = reader.ReadInt16();
            for (int i = 0; i < numFieldGroups; i++)
            {
                // Read blueprint information and get the specified blueprint
                BlueprintId groupBlueprintDataRef = (BlueprintId)reader.ReadUInt64();
                byte fieldGroupCopyNum = reader.ReadByte();
                Blueprint groupBlueprint = GameDatabase.GetBlueprint(groupBlueprintDataRef);

                if (groupBlueprint.IsProperty())
                {
                    DeserializePropertyMixin(prototype, blueprint, groupBlueprint, fieldGroupCopyNum, prototypeDataRef, prototypeName, classType, reader);
                }
                else
                {
                    // Simple fields
                    DeserializeFieldGroup(prototype, blueprint, fieldGroupCopyNum, prototypeName, classType, reader, "Simple Fields");

                    // List fields
                    DeserializeFieldGroup(prototype, blueprint, fieldGroupCopyNum, prototypeName, classType, reader, "List Fields");
                }
            }
        }

        private bool DeserializeFieldGroup(Prototype prototype, Blueprint blueprint, byte fieldGroupCopyNum, string prototypeName, Type classType, BinaryReader reader, string groupTag)
        {
            var classManager = GameDatabase.PrototypeClassManager;

            short numFields = reader.ReadInt16();
            for (int i = 0; i < numFields; i++)
            {
                var fieldId = (StringId)reader.ReadUInt64();
                var fieldBaseType = (CalligraphyBaseType)reader.ReadByte();

                // Get blueprint member info for this field
                if (blueprint.TryGetBlueprintMemberInfo(fieldId, out var blueprintMemberInfo) == false)
                    return Logger.WarnReturn(false, $"Failed to find member {GameDatabase.GetBlueprintFieldName(fieldId)} in blueprint {GameDatabase.GetBlueprintName(blueprint.Id)}");

                // Check to make sure the type matches (do we need this?)
                if (blueprintMemberInfo.Member.BaseType != fieldBaseType)
                    return Logger.WarnReturn(false, $"Type mismatch between blueprint and prototype");

                // Determine where this field belongs
                Prototype fieldOwnerPrototype = prototype;
                Blueprint fieldOwnerBlueprint = blueprint;

                System.Reflection.PropertyInfo fieldInfo;
                if (blueprint.IsRuntimeChildOf(blueprintMemberInfo.Blueprint))
                {
                    // For regular fields we just get field info straight away
                    fieldInfo = classManager.GetFieldInfo(blueprint.RuntimeBindingClassType, blueprintMemberInfo, false);
                }
                else
                {
                    // The blueprint for this field is not a runtime child of our main blueprint, meaning it belongs to one of the mixins
                    Type mixinType = blueprintMemberInfo.Blueprint.RuntimeBindingClassType;

                    // First we look for a mixin field
                    var mixinFieldInfo = classManager.GetMixinFieldInfo(classType, mixinType);
                    if (mixinFieldInfo != null)
                    {
                        // Create a mixin instance if there isn't one
                        if (mixinFieldInfo.GetValue(prototype) == null)
                            mixinFieldInfo.SetValue(prototype, Activator.CreateInstance(mixinType));

                        // Get the field info from our mixin
                        fieldInfo = classManager.GetFieldInfo(mixinType, blueprintMemberInfo, false);
                    }
                    else
                    {
                        // TODO: list mixins
                        Logger.Warn($"Failed to get field info for {blueprintMemberInfo.Member.FieldName}: list mixins are not implemented");
                    }
                }

                // Test parsing
                var parser = GetParser(fieldBaseType, blueprintMemberInfo.Member.StructureType);
                var value = parser(reader);
            }

            return true;
        }

        private void DeserializePropertyMixin(Prototype prototype, Blueprint blueprint, Blueprint groupBlueprint, byte fieldGroupCopyNum,
            PrototypeId prototypeDataRef, string prototypeName, Type classType, BinaryReader reader)
        {
            // Skip property fields groups for now
            // todo: do actual deserialization in DeserializeFieldGroupIntoProperty()
            short numSimpleFields = reader.ReadInt16();
            for (int i = 0; i < numSimpleFields; i++)
            {
                var id = (StringId)reader.ReadUInt64();
                var type = (CalligraphyBaseType)reader.ReadByte();

                // Property mixins don't have any RHStructs, so we can always read the value as uint64
                // (also no types or localized string refs)
                var value = reader.ReadUInt64();
            }

            // Property field groups do not have any list fields, so numListFields should always be 0
            short numListFields = reader.ReadInt16();
        }

        /// <summary>
        /// Copies field values from a prototype with the specified data ref.
        /// </summary>
        private bool CopyPrototypeDataRefFields(Prototype destPrototype, PrototypeId sourceDataRef)
        {
            // Check to make sure our reference is valid
            if (sourceDataRef == PrototypeId.Invalid)
                return Logger.WarnReturn(false, "Failed to copy prototype data ref fields: invalid source ref");

            // Get source prototype and copy fields from it
            Prototype sourcePrototype = GameDatabase.GetPrototype<Prototype>(sourceDataRef);
            return CopyPrototypeFields(destPrototype, sourcePrototype);
        }

        /// <summary>
        /// Copies field values from one prototype to another.
        /// </summary>
        private bool CopyPrototypeFields(Prototype destPrototype, Prototype sourcePrototype)
        {
            // Get type information for both prototypes and make sure they are the same
            Type destType = destPrototype.GetType();
            Type sourceType = sourcePrototype.GetType();

            if (sourceType != destType)
                return Logger.WarnReturn(false, $"Failed to copy prototype fields: source type ({sourceType.Name}) does not match destination type ({destType.Name})");

            foreach (var property in destType.GetProperties())
            {
                if (property.DeclaringType == typeof(Prototype)) continue;      // Skip base prototype properties

                //Logger.Debug(property.Name);

                // Set value if property is a value type
                if (property.PropertyType.IsValueType)
                    property.SetValue(destPrototype, property.GetValue(sourcePrototype));

                // todo: reference type copy, deep copy
                //Logger.Trace($"Reference type field copying not implemented, skipping...");
            }

            return true;
        }

        #region Parsers

        // Rough early experiments implementing field parsers

        private static Func<BinaryReader, object> GetParser(CalligraphyBaseType baseType, CalligraphyStructureType structureType)
        {
            // We're currently using Calligraphy types here as a temporary solution
            // Probably need some kind of lookup dictionary to avoid unnecessary branching here
            switch (baseType)
            {
                case CalligraphyBaseType.Boolean:   return structureType == CalligraphyStructureType.Simple ? ParseBool     : ParseListBool;
                case CalligraphyBaseType.Double:    return structureType == CalligraphyStructureType.Simple ? ParseDouble   : ParseListDouble;
                case CalligraphyBaseType.Long:      return structureType == CalligraphyStructureType.Simple ? ParseInt64    : ParseListInt64;
                case CalligraphyBaseType.RHStruct:  return structureType == CalligraphyStructureType.Simple ? ParseRHStruct : ParseListRHStruct;
                default:                            return structureType == CalligraphyStructureType.Simple ? ParseUInt64   : ParseListUInt64;
            }
        }

        // TODO: FieldParserParams
        // TODO: parseValue()
        // TODO: Maybe move this to a helper static class?

        private static object ParseBool(BinaryReader reader) => Convert.ToBoolean(reader.ReadUInt64());
        private static object ParseInt64(BinaryReader reader) => reader.ReadInt64();
        private static object ParseUInt64(BinaryReader reader) => reader.ReadUInt64();
        private static object ParseDouble(BinaryReader reader) => reader.ReadDouble();
        private static object ParseRHStruct(BinaryReader reader) => new Prototype(reader);

        private static object ParseListBool(BinaryReader reader) => ParseCollection(reader, ParseBool);
        private static object ParseListInt64(BinaryReader reader) => ParseCollection(reader, ParseInt64);
        private static object ParseListUInt64(BinaryReader reader) => ParseCollection(reader, ParseUInt64);
        private static object ParseListDouble(BinaryReader reader) => ParseCollection(reader, ParseDouble);
        private static object ParseListRHStruct(BinaryReader reader) => ParseCollection(reader, ParseRHStruct);

        private static object ParseCollection(BinaryReader reader, Func<BinaryReader, object> itemParser)
        {
            var values = new object[reader.ReadInt16()];
            for (int i = 0; i < values.Length; i++)
                values[i] = itemParser(reader);
            return values;
        }

        #endregion
    }
}
