using System.Reflection;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData.Calligraphy
{
    /// <summary>
    /// An implementation of <see cref="GameDataSerializer"/> for Calligraphy prototypes.
    /// </summary>
    public partial class CalligraphySerializer : GameDataSerializer
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        /// <summary>
        /// Deserializes a Calligraphy prototype from stream.
        /// </summary>
        public override void Deserialize(Prototype prototype, PrototypeId dataRef, Stream stream)
        {
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

                //Logger.Debug("Done!");
            }
        }

        /// <summary>
        /// Deserializes data for a Calligraphy prototype.
        /// </summary>
        private static void DoDeserialize(Prototype prototype, PrototypeDataHeader header, PrototypeId prototypeDataRef, string prototypeName, BinaryReader reader)
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

        /// <summary>
        /// Deserializes a field group of a Calligraphy prototype.
        /// </summary>
        private static bool DeserializeFieldGroup(Prototype prototype, Blueprint blueprint, byte blueprintCopyNum, string prototypeName, Type classType, BinaryReader reader, string groupTag)
        {
            var classManager = GameDatabase.PrototypeClassManager;

            short numFields = reader.ReadInt16();
            for (int i = 0; i < numFields; i++)
            {
                var fieldId = (StringId)reader.ReadUInt64();
                var fieldBaseType = (CalligraphyBaseType)reader.ReadByte();

                // Get blueprint member info for this field
                if (blueprint.TryGetBlueprintMemberInfo(fieldId, out var blueprintMemberInfo) == false)
                    return Logger.ErrorReturn(false, $"Failed to find member id {fieldId} in blueprint {GameDatabase.GetBlueprintName(blueprint.Id)}");

                // Check to make sure the type matches (do we need this?)
                if (blueprintMemberInfo.Member.BaseType != fieldBaseType)
                    return Logger.ErrorReturn(false, $"Type mismatch between blueprint and prototype");

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
                    // - ProductPrototype in ItemPrototype (simple mixin)
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
                        //Logger.Debug($"Found field info for mixin {mixinType.Name}, field name {blueprintMemberInfo.Member.FieldName}");
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
                            //Logger.Debug($"Found field info for list mixin {mixinType.Name}, field name {blueprintMemberInfo.Member.FieldName}");
                        }
                        else
                        {
                            // Nowhere to put this field, something went very wrong, time to reevaluate life choices
                            return Logger.ErrorReturn(false, $"Failed to find field info for mixin {mixinType.Name}, field name {blueprintMemberInfo.Member.FieldName}");
                        }
                    }
                }

                // Parse
                var parser = GetParser(fieldInfo.PropertyType);
                FieldParserParams @params = new(reader, fieldInfo, fieldOwnerPrototype, fieldOwnerBlueprint, prototypeName, blueprintMemberInfo);
                
                if (parser(@params) == false)
                {
                    Logger.ErrorReturn(false, string.Format("Failed to parse field {0} of field group {1} in {2}",
                        blueprintMemberInfo.Member.FieldName,
                        GameDatabase.GetBlueprintName(blueprint.Id),
                        prototypeName));
                };
            }

            return true;
        }

        /// <summary>
        /// Deserializes a property mixin field group of a Calligraphy prototype.
        /// </summary>
        private static bool DeserializePropertyMixin(Prototype prototype, Blueprint blueprint, Blueprint groupBlueprint, byte blueprintCopyNum,
            PrototypeId prototypeDataRef, string prototypeName, Type classType, BinaryReader reader)
        {
            // TODO: do actual deserialization in DeserializeFieldGroupIntoProperty()
            short numSimpleFields = reader.ReadInt16();
            for (int i = 0; i < numSimpleFields; i++)
            {
                var fieldId = (StringId)reader.ReadUInt64();
                var type = (CalligraphyBaseType)reader.ReadByte();

                // Property mixins don't have any RHStructs, so we can always read the value as uint64
                // (also no types or localized string refs)
                var value = reader.ReadUInt64();

                // hack: write data to a temporary PrototypePropertyCollection implementation
                if (classType == typeof(PropertyPrototype)) continue;
                var propertyCollectionFieldInfo = classType.GetProperty("Properties");
                var propertyCollection = (PrototypePropertyCollection)propertyCollectionFieldInfo.GetValue(prototype);

                if (propertyCollection == null)
                {
                    propertyCollection = new();
                    propertyCollectionFieldInfo.SetValue(prototype, propertyCollection);
                }

                // Get blueprint member info for this field
                if (blueprint.TryGetBlueprintMemberInfo(fieldId, out var blueprintMemberInfo) == false)
                    return Logger.ErrorReturn(false, $"Failed to find member id {fieldId} in blueprint {GameDatabase.GetBlueprintName(blueprint.Id)}");

                propertyCollection.AddPropertyFieldValue(groupBlueprint.Id, blueprintCopyNum, blueprintMemberInfo.Member.FieldName, value);
            }

            // Property field groups do not have any list fields, so numListFields should always be 0
            short numListFields = reader.ReadInt16();
            return true;
        }

        /// <summary>
        /// Copies field values from a prototype with the specified data ref.
        /// </summary>
        private static bool CopyPrototypeDataRefFields(Prototype destPrototype, PrototypeId sourceDataRef)
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
        private static bool CopyPrototypeFields(Prototype destPrototype, Prototype sourcePrototype)
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
        public static List<PrototypeMixinListItem> AcquireOwnedMixinList(Prototype prototype, System.Reflection.PropertyInfo mixinFieldInfo, bool copyItemsFromParent)
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
        private static Prototype AcquireOwnedUniqueMixinListElement(Prototype owner, List<PrototypeMixinListItem> list, Type elementClassType,
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
        private static Prototype AddMixinListItemCopy(Prototype owner, List<PrototypeMixinListItem> list, PrototypeMixinListItem item)
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
        private static Prototype AllocateDynamicPrototype(Type classType, PrototypeId defaults, Prototype instanceToCopy)
        {
            // Create a new prototype of the specified type
            var prototype = GameDatabase.PrototypeClassManager.AllocatePrototype(classType);

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
