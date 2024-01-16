using System.Reflection;
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
                byte blueprintCopyNum = reader.ReadByte();
                Blueprint groupBlueprint = GameDatabase.GetBlueprint(groupBlueprintDataRef);

                if (groupBlueprint.IsProperty())
                {
                    DeserializePropertyMixin(prototype, blueprint, groupBlueprint, blueprintCopyNum, prototypeDataRef, prototypeName, classType, reader);
                }
                else
                {
                    // Simple fields
                    DeserializeFieldGroup(prototype, blueprint, blueprintCopyNum, prototypeName, classType, reader, "Simple Fields");

                    // List fields
                    DeserializeFieldGroup(prototype, blueprint, blueprintCopyNum, prototypeName, classType, reader, "List Fields");
                }
            }
        }

        private bool DeserializeFieldGroup(Prototype prototype, Blueprint blueprint, byte blueprintCopyNum, string prototypeName, Type classType, BinaryReader reader, string groupTag)
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
                    fieldOwnerBlueprint = blueprintMemberInfo.Blueprint;
                    Type mixinType = blueprintMemberInfo.Blueprint.RuntimeBindingClassType;

                    // Currently known cases for non-property mixins:
                    // - LocomotorPrototype and PopulationInfoPrototype in AgentPrototype (simple mixins, PopulationInfoPrototype seems to be unused)
                    // - ConditionPrototype and ConditionEffectPrototype in PowerPrototype (list mixins)
                    // We use MixinAttribute and ListMixinAttribute to differentiate them from RHStructs.

                    // First we look for a non-list mixin field
                    var mixinFieldInfo = classManager.GetMixinFieldInfo(classType, mixinType, typeof(MixinAttribute));
                    if (mixinFieldInfo != null)
                    {
                        // Set owner prototype to the existing mixin instance or create a new instance if there isn't one
                        fieldOwnerPrototype = (Prototype)mixinFieldInfo.GetValue(prototype);
                        if (fieldOwnerPrototype == null)
                        {
                            fieldOwnerPrototype = (Prototype)Activator.CreateInstance(mixinType);
                            mixinFieldInfo.SetValue(prototype, fieldOwnerPrototype);
                        }

                        // Get the field info from our mixin
                        fieldInfo = classManager.GetFieldInfo(mixinType, blueprintMemberInfo, false);
                        Logger.Debug($"Found field info for mixin {mixinType.Name}, field name {blueprintMemberInfo.Member.FieldName}");
                    }
                    else
                    {
                        // Look for a list mixin
                        mixinFieldInfo = classManager.GetMixinFieldInfo(classType, mixinType, typeof(ListMixinAttribute));
                        if (mixinFieldInfo != null)
                        {
                            List<PrototypeMixinListItem> list = AcquireOwnedMixinList(prototype, mixinFieldInfo, false);

                            // Get a matching list element
                            Prototype element = AcquireOwnedUniqueMixinListElement(prototype, list, mixinType, fieldOwnerBlueprint, blueprintCopyNum);
                            if (element == null)
                                Logger.WarnReturn(false, $"Failed to acquire element of a list mixin to deserialize field into");

                            fieldOwnerPrototype = element;
                            fieldInfo = classManager.GetFieldInfo(mixinType, blueprintMemberInfo, false);
                            Logger.Debug($"Found field info for list mixin {mixinType.Name}, field name {blueprintMemberInfo.Member.FieldName}");
                        }
                        else
                        {
                            // Nowhere to put this field, something went very wrong, time to reevaluate life choices
                            Logger.WarnReturn(false, $"Failed to find field info for mixin {mixinType.Name}, field name {blueprintMemberInfo.Member.FieldName}");
                        }
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

        #region List Mixin Management

        /// <summary>
        /// Creates if needed and returns a list mixin from the specified field of the provided <see cref="Prototype"/> instance that belongs to it.
        /// </summary>
        public List<PrototypeMixinListItem> AcquireOwnedMixinList(Prototype prototype, System.Reflection.PropertyInfo mixinFieldInfo, bool copyItemsFromParent)
        {
            // Make sure the field info we have is for a list mixin
            if (mixinFieldInfo.IsDefined(typeof(ListMixinAttribute)) == false)
                Logger.WarnReturn<List<PrototypeMixinListItem>>(null, $"Tried to acquire owned mixin list for a field that is not a list mixin");

            // Create a new list if there isn't one or it belongs to another prototype
            var list = (List<PrototypeMixinListItem>)mixinFieldInfo.GetValue(prototype);
            if (list == null || prototype.IsDynamicFieldOwnedBy(list) == false)
            {
                List<PrototypeMixinListItem> newList = new();

                // Fill the new list
                if (list != null)
                {
                    if (copyItemsFromParent)
                    {
                        // Create copies of all parent items and take ownership of those copies
                        foreach (var item in list)
                            AddMixinListItemCopy(prototype, newList, item);
                    }
                    else
                    {
                        // Do a shallow copy of the parent list and do not take ownership of any of its items
                        // In this case copies are created when each list element is acquired with AcquireOwnedUniqueMixinListElement()
                        newList.AddRange(list);
                    }
                }

                // Assign the new list to the field and take ownership of it
                prototype.SetDynamicFieldOwner(newList);

                list = newList;
            }

            return list;
        }

        /// <summary>
        /// Creates if needed and returns an element from a list mixin.
        /// </summary>
        private Prototype AcquireOwnedUniqueMixinListElement(Prototype owner, List<PrototypeMixinListItem> list, Type elementClassType,
            Blueprint elementBlueprint, byte blueprintCopyNum)
        {
            // Look for a unique list element
            // Instead of calling a separate findUniqueMixinListElement() method like the client does, we'll just look for it here
            PrototypeMixinListItem uniqueListElement = null;
            foreach (var element in list)
            {
                if (element.Prototype.GetType() == elementClassType && element.BlueprintId == elementBlueprint.Id && element.BlueprintCopyNum == blueprintCopyNum)
                {
                    uniqueListElement = element;
                    break;
                }
            }

            if (uniqueListElement == null)
            {
                // Create the element we're looking for if it's not in our list
                Prototype prototype = AllocateDynamicPrototype(elementClassType, elementBlueprint.DefaultPrototypeId, null);
                prototype.ParentDataRef = elementBlueprint.DefaultPrototypeId;

                // Assign ownership of the new mixin
                owner.SetDynamicFieldOwner(prototype);

                // Add the new mixin to the list
                PrototypeMixinListItem newListItem = new()
                {
                    Prototype = prototype,
                    BlueprintId = elementBlueprint.Id,
                    BlueprintCopyNum = blueprintCopyNum
                };

                list.Add(newListItem);

                // Return the new mixin
                return prototype;
            }
            else
            {
                // Return the item we found

                // Return the prototype as is if it belongs to our owner
                if (owner.IsDynamicFieldOwnedBy(uniqueListElement.Prototype))
                    return uniqueListElement.Prototype;

                // If there is a matching item but it doesn't belong to the owner, then we need to replace it with a copy
                list.Remove(uniqueListElement);
                return AddMixinListItemCopy(owner, list, uniqueListElement);
            }
        }

        /// <summary>
        /// Creates a copy of an element from a parent list mixin and assigns it to the child.
        /// </summary>
        private Prototype AddMixinListItemCopy(Prototype owner, List<PrototypeMixinListItem> list, PrototypeMixinListItem item)
        {
            // Copy the prototype from the provided list item
            Prototype element = AllocateDynamicPrototype(item.Prototype.GetType(), PrototypeId.Invalid, item.Prototype);

            // Update parent
            element.ParentDataRef = item.Prototype.DataRef;

            // Update ownership
            owner.SetDynamicFieldOwner(element);

            // Add the copied item to the list
            item.Prototype = element;
            list.Add(item);

            return element;
        }

        /// <summary>
        /// Creates a new prototype of the specified type and fills it with data from the specified source (either a default prototype or a prototype instance).
        /// </summary>
        private Prototype AllocateDynamicPrototype(Type classType, PrototypeId defaults, Prototype instanceToCopy)
        {
            // Create a new prototype of the specified type
            var prototype = (Prototype)Activator.CreateInstance(classType);

            // Copy fields either from the specified defaults prototype or the provided prototype
            if (defaults != PrototypeId.Invalid && instanceToCopy == null)
            {
                var defaultsProto = GameDatabase.GetPrototype<Prototype>(defaults);
                CopyPrototypeFields(prototype, defaultsProto);
            }
            else if (instanceToCopy != null)
            {
                CopyPrototypeFields(prototype, instanceToCopy);
            }

            return prototype;
        }

        #endregion

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

    /// <summary>
    /// Contains an item and its blueprint information for a list of mixin prototypes.
    /// </summary>
    public class PrototypeMixinListItem
    {
        // TODO: Maybe move this somewhere else?
        public Prototype Prototype { get; set; }
        public BlueprintId BlueprintId { get; set; }
        public byte BlueprintCopyNum { get; set; }
    }
}
