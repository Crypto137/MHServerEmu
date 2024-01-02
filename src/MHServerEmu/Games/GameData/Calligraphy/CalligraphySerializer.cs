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
                    DeserializePropertyMixin(prototype, blueprint, groupBlueprint, fieldGroupCopyNum, prototypeDataRef, prototypeName, reader);
                }
                else
                {
                    // Simple fields
                    DeserializeFieldGroup(prototype, blueprint, fieldGroupCopyNum, prototypeName, reader, "Simple Fields");

                    // List fields
                    DeserializeFieldGroup(prototype, blueprint, fieldGroupCopyNum, prototypeName, reader, "List Fields");
                }
            }
        }

        private void DeserializeFieldGroup(Prototype prototype, Blueprint blueprint, byte fieldGroupCopyNum, string prototypeName, BinaryReader reader, string groupTag)
        {
            // Placeholder implementation for testing

            short numFields = reader.ReadInt16();
            for (int i = 0; i < numFields; i++)
            {
                var id = (StringId)reader.ReadUInt64();
                var type = (CalligraphyBaseType)reader.ReadByte();

                if (groupTag == "Simple Fields")
                {
                    object value = type switch
                    {
                        CalligraphyBaseType.Boolean => Convert.ToBoolean(reader.ReadUInt64()),
                        CalligraphyBaseType.Double => reader.ReadDouble(),
                        CalligraphyBaseType.Long => reader.ReadInt64(),
                        CalligraphyBaseType.RHStruct => new Prototype(reader),
                        _ => reader.ReadUInt64(),
                    };
                }
                else if (groupTag == "List Fields")
                {
                    var values = new object[reader.ReadInt16()];
                    for (int j = 0; j < values.Length; j++)
                    {
                        values[j] = type switch
                        {
                            CalligraphyBaseType.Boolean => Convert.ToBoolean(reader.ReadUInt64()),
                            CalligraphyBaseType.Double => reader.ReadDouble(),
                            CalligraphyBaseType.Long => reader.ReadInt64(),
                            CalligraphyBaseType.RHStruct => new Prototype(reader),
                            _ => reader.ReadUInt64(),
                        };
                    }
                }
            }
        }

        private void DeserializePropertyMixin(Prototype prototype, Blueprint blueprint, Blueprint groupBlueprint, byte fieldGroupCopyNum,
            PrototypeId prototypeDataRef, string prototypeName, BinaryReader reader)
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
    }
}
