using System.Runtime.CompilerServices;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.GameData.Calligraphy
{
    /// <summary>
    /// An implementation of <see cref="GameDataSerializer"/> for Calligraphy prototypes.
    /// </summary>
    public sealed class CalligraphySerializer : GameDataSerializer
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static CalligraphySerializer Instance { get; } = new();

        private CalligraphySerializer() { }

        /// <summary>
        /// Deserializes a Calligraphy <see cref="Prototype"/> from the provided <see cref="Stream"/>.
        /// </summary>
        public override bool Deserialize(Prototype prototype, PrototypeId dataRef, Stream stream)
        {
            string prototypeName = GameDatabase.GetPrototypeName(dataRef);
            using CalligraphyReader reader = new(stream, prototypeName);

            if (!Verify.IsTrue(reader.ReadHeader("PTP"))) return false;

            if (!Verify.IsTrue(reader.ReadPrototypeHeader(out PrototypeDataHeader header))) return false;

            if (!Verify.IsTrue(header.ReferenceExists)) return false;
            if (!Verify.IsTrue(header.PolymorphicData == false)) return false;

            return DoDeserialize(prototype, header, dataRef, prototypeName, reader);
        }

        /// <summary>
        /// Deserializes data for a Calligraphy prototype.
        /// </summary>
        private static bool DoDeserialize(Prototype prototype, PrototypeDataHeader header, PrototypeId prototypeDataRef, string prototypeName, CalligraphyReader reader)
        {
            DataDirectory dataDirectory = GameDatabase.DataDirectory;

            if (!Verify.IsNotNull(prototype, $"Expected prototype when deserializing {prototypeName}"))
                return false;

            prototype.DataRef = prototypeDataRef;

            Blueprint blueprint = dataDirectory.GetPrototypeBlueprint(prototypeDataRef != PrototypeId.Invalid ? prototypeDataRef : header.ReferenceType);
            if (!Verify.IsNotNull(blueprint, $"Unknown blueprint when deserializing {prototypeName}"))
                return false;

            // Get class type (we get it from the blueprint's binding instead of calling GetRuntimeClassId())
            Type classType = blueprint.RuntimeBindingClassType;

            // Make sure there is data to deserialize
            if (!Verify.IsTrue(header.ReferenceExists, $"Expected reference exists in data for {prototypeName}"))
                return false;

            // Copy parent data if there is any
            if (header.ReferenceType != PrototypeId.Invalid)
            {
                PrototypeId parentPrototypeDataRef = header.ReferenceType;
                if (!Verify.IsTrue(CopyPrototypeDataRefFields(prototype, parentPrototypeDataRef), $"Error copying parent prototype fields for {prototypeName}"))
                    return false;

                prototype.ParentDataRef = parentPrototypeDataRef;
            }

            // Deserialize this prototype's data if there is any
            if (header.InstanceDataExists == false)
                return true;

            if (!Verify.IsTrue(reader.Read(out short numFieldGroups), $"Unable to read number of field groups for {prototypeName}"))
                return false;

            for (int i = 0; i < numFieldGroups; i++)
            {
                // Read blueprint information and get the specified blueprint
                if (!Verify.IsTrue(reader.Read(out BlueprintId groupBlueprintDataRef), $"Error reading {i} group's declaring blueprint id from {prototypeName}"))
                    return false;

                Verify.IsTrue(blueprint.IsA(groupBlueprintDataRef), $"Prototype {prototypeName}'s blueprint parents do not match those loaded by the game database at startup.  (This can be caused by hotloading a prototype whose parents have changed)");

                if (!Verify.IsTrue(reader.Read(out byte fieldGroupCopyNum), $"Error reading {i} group's blueprint copy number from {prototypeName}"))
                    return false;

                Blueprint groupBlueprint = GameDatabase.GetBlueprint(groupBlueprintDataRef);
                if (!Verify.IsNotNull(groupBlueprint, $"Failed to get parent blueprint from id {(ulong)groupBlueprintDataRef} for:\n\tPrototype: {prototypeName}\n\tFieldGroup: {i}"))
                    return false;

                if (groupBlueprint.IsProperty)
                {
                    if (!Verify.IsTrue(DeserializePropertyMixin(prototype, blueprint, groupBlueprint, fieldGroupCopyNum, prototypeDataRef, prototypeName, prototypeName, classType, reader),
                        $"Unable to deserialize property mixin {groupBlueprint} on {prototypeName}"))
                        return false;
                }
                else
                {
                    // Simple fields
                    if (!Verify.IsTrue(DeserializeFieldGroup(prototype, blueprint, fieldGroupCopyNum, prototypeName, classType, reader, "Simple Fields"),
                        $"Unable to deserialize simple fields for {prototypeName}"))
                        return false;

                    // List fields
                    if (!Verify.IsTrue(DeserializeFieldGroup(prototype, blueprint, fieldGroupCopyNum, prototypeName, classType, reader, "List Fields"),
                        $"Unable to deserialize list fields for {prototypeName}"))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Deserializes a field group of a Calligraphy prototype.
        /// </summary>
        private static bool DeserializeFieldGroup(Prototype prototype, Blueprint blueprint, byte blueprintCopyNum, string prototypeName, Type classType, CalligraphyReader record, string groupTag)
        {
            string errorMessage = null;
            BlueprintMemberInfo blueprintMemberInfo = default;
            PrototypeClassManager classManager = GameDatabase.PrototypeClassManager;

            if (!Verify.IsTrue(record.Read(out short numFields), $"Error reading number of fields in {prototypeName} in group {groupTag}"))
                return false;

            for (int i = 0; i < numFields; i++)
            {
                if (record.Read(out StringId fieldId) == false)
                {
                    errorMessage = "Unable to read field id";
                    goto Error;
                }

                if (fieldId == StringId.Invalid)
                {
                    errorMessage = "Invalid blueprint field id encountered";
                    goto Error;
                }

                if (record.Read(out CalligraphyBaseType fieldBaseType) == false)
                {
                    errorMessage = "Field type not found";
                    goto Error;
                }

                // Determine where this field belongs
                Prototype fieldOwnerPrototype = prototype;
                Blueprint fieldOwnerBlueprint = blueprint;

                // Get blueprint member info for this field
                if (blueprint.GetBlueprintMemberInfo(fieldId, out blueprintMemberInfo) == false)
                {
                    errorMessage = "Unable to get blueprint field info for field re";
                    goto Error;
                }

                // Check to make sure the type matches
                if (blueprintMemberInfo.Member.BaseType != fieldBaseType)
                {
                    errorMessage = "Type mismatch between blueprint and prototype";
                    goto Error;
                }

                PrototypeFieldInfo fieldInfo;
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
                    if (mixinType == null)
                    {
                        errorMessage = "Unknown field class";
                        goto Error;
                    }

                    // Currently known cases for non-property mixins:
                    // - LocomotorPrototype and PopulationInfoPrototype in AgentPrototype (simple mixins, PopulationInfoPrototype seems to be unused)
                    // - ProductPrototype in ItemPrototype (simple mixin)
                    // - ConditionPrototype and ConditionEffectPrototype in PowerPrototype (list mixins)
                    // We use MixinAttribute and ListMixinAttribute to differentiate them from RHStructs.

                    // First we look for a non-list mixin field
                    PrototypeFieldInfo mixinFieldInfo = classManager.GetMixinFieldInfo(classType, mixinType, PrototypeFieldType.Mixin);
                    if (mixinFieldInfo != null)
                    {
                        // Set owner prototype to the existing mixin instance or create a new instance if there isn't one
                        mixinFieldInfo.GetValue(prototype, out fieldOwnerPrototype);
                        if (fieldOwnerPrototype == null)
                        {
                            fieldOwnerPrototype = GameDatabase.PrototypeClassManager.AllocatePrototype(mixinType);
                            mixinFieldInfo.SetValue(prototype, fieldOwnerPrototype);
                        }

                        // Get the field info from our mixin
                        fieldInfo = classManager.GetFieldInfo(mixinType, blueprintMemberInfo, false);
                    }
                    else
                    {
                        // Look for a list mixin
                        mixinFieldInfo = classManager.GetMixinFieldInfo(classType, mixinType, PrototypeFieldType.ListMixin);
                        if (mixinFieldInfo != null)
                        {
                            PrototypeMixinList list = AcquireOwnedMixinList(prototype, mixinFieldInfo, false);
                            if (!Verify.IsNotNull(list)) return false;

                            // Get a matching list element
                            Prototype element = AcquireOwnedUniqueMixinListElement(prototype, list, mixinType, fieldOwnerBlueprint, blueprintCopyNum);
                            if (element == null)
                            {
                                errorMessage = "Unable to acquire unique list element of mixin list to deserialize field into";
                                goto Error;
                            }

                            fieldOwnerPrototype = element;
                            fieldInfo = classManager.GetFieldInfo(mixinType, blueprintMemberInfo, false);
                        }
                        else
                        {
                            // Nowhere to put this field, something went very wrong, time to reevaluate life choices
                            errorMessage = "Invalid mixin field info. Please make sure that the runtime bindings are correct, including those of child blueprints of any new ones you might've created.";
                            goto Error;
                        }
                    }
                }

                if (fieldInfo == null)
                {
                    errorMessage = "Unknown field";
                    goto Error;
                }

                PrototypeFieldType fieldType = fieldInfo.Type;

                if (blueprintMemberInfo.Member.IsCompatibleWithType(fieldType) == false)
                {
                    errorMessage = "Type mismatch between code field maps and Calligraphy field type";
                    goto Error;
                }

                // Parse
                FieldParser parser = GetParser(fieldType);
                if (parser == null)
                {
                    errorMessage = "Unable to get field parser for field type";
                    goto Error;
                }

                FieldParserParams @params = new(record, fieldInfo, fieldOwnerPrototype, fieldOwnerBlueprint, prototypeName, blueprintMemberInfo);
                
                if (parser(@params) == false)
                {
                    errorMessage = "Invalid field value(s)";
                    goto Error;
                };
            }

            return true;

        Error:
            Verify.IsTrue(false, $"Error: '{errorMessage}' [{groupTag}]\n Field Name: '{blueprintMemberInfo.Member?.FieldName}'\n Prototype: '{prototypeName}'\n");
            return false;
        }

        #region Properties

        /// <summary>
        /// Deserializes a property mixin field group of a Calligraphy prototype.
        /// </summary>
        private static bool DeserializePropertyMixin(Prototype prototype, Blueprint blueprint, Blueprint groupBlueprint, byte blueprintCopyNum,
            PrototypeId prototypeDataRef, string prototypeName, string prototypeFilePath, Type classType, CalligraphyReader record)
        {
            if (!Verify.IsNotNull(prototype)) return false;
            if (!Verify.IsNotNull(blueprint)) return false;
            if (!Verify.IsNotNull(classType)) return false;
            //if (!Verify.IsTrue(prototype.GetType() == classType)) return false;   // not sure about performance for this
            if (!Verify.IsTrue(groupBlueprint.IsProperty)) return false;

            // This whole mixin system is a huge mess.
            PrototypePropertyCollection collection = null;

            // Property mixins are used both for initializing property infos and filling prototype property collections.
            // If this isn't a default prototype, it means the field group needs to be deserialized into a property collection.
            if (prototypeDataRef != groupBlueprint.DefaultPrototypeRef)
            {
                PrototypeClassManager classManager = GameDatabase.PrototypeClassManager;

                Type propertyHolderClassType = classType;
                Prototype propertyHolderPrototype = prototype;

                // Check if this property belongs in one of mixin property collections.
                // This is basically an edge case for PowerPrototype, since it's the only example of mixins having their own property collections.
                // We save a little bit of time by skipping this for non-power prototypes. Remove this check if something breaks in other versions of the game.
                if (prototype is PowerPrototype)
                {
                    // Iterate through all fields and check if if there are any mixin fields.
                    // We don't need an outer loop like the client because our PrototypeFieldSet implementation includes fields from base classes.
                    foreach (PrototypeFieldInfo fieldInfo in classManager.GetPrototypeFieldSet(classType))
                    {
                        PrototypeFieldType fieldType = fieldInfo.Type;

                        // If this is a mixin check if it has property collections we need to deserialize into
                        // We pass propertyHolderClassType and propertyHolderPrototype as refs so that CheckPropertyMixin can modify them
                        if (fieldType == PrototypeFieldType.Mixin || fieldType == PrototypeFieldType.ListMixin)
                        {
                            bool found = CheckPropertyMixin(fieldInfo, fieldType, blueprint, groupBlueprint, blueprintCopyNum, prototype, ref propertyHolderClassType, ref propertyHolderPrototype);
                            if (found)
                                break;
                        }
                    }
                }

                if (!Verify.IsNotNull(propertyHolderPrototype)) return false;
                
                // Get property collection to deserialize into from the property holder
                PrototypeFieldInfo propertyCollectionFieldInfo = classManager.GetFieldInfo(propertyHolderClassType, default, true);
                if (!Verify.IsNotNull(propertyCollectionFieldInfo, $"Prototype class missing property collection field info. blueprint={blueprint}, groupBlueprint={groupBlueprint}, prototype={prototype}"))
                    return false;

                collection = GetPropertyCollectionField(propertyHolderPrototype, propertyCollectionFieldInfo);
                if (!Verify.IsNotNull(collection, $"Prototype class missing property collection field. blueprint={blueprint}, groupBlueprint={groupBlueprint}, prototype={prototype}"))
                    return false;
            }

            // This handles both cases (initialization and filling property collections)
            if (!Verify.IsTrue(DeserializeFieldGroupIntoProperty(collection, groupBlueprint, blueprintCopyNum, prototypeFilePath, record, "Property Fields"),
                $"Unable to deserialize field group into property. groupBlueprint={groupBlueprint}, copyNum={blueprintCopyNum}, prototype={prototypeName}"))
                return false;

            // Property field groups do not have any list fields, so numListFields should always be 0
            if (!Verify.IsTrue(record.Read(out short numFields), $"Error reading number of fields in {prototypeName} in property mixin")) return false;
            if (!Verify.IsTrue(numFields == 0)) return false;

            return true;
        }

        /// <summary>
        /// Deserializes a property field group.
        /// </summary>
        private static bool DeserializeFieldGroupIntoProperty(PrototypePropertyCollection propertyCollection, Blueprint blueprint, byte blueprintCopyNum,
            string prototypeName, CalligraphyReader record, string groupTag)
        {
            PropertyInfoTable propertyInfoTable = GameDatabase.PropertyInfoTable;

            PrototypeId propertyDataRef = blueprint.PropertyDataRef;
            PropertyEnum propertyEnum = propertyInfoTable.GetPropertyEnumFromPrototype(propertyDataRef);
            if (!Verify.IsTrue(propertyEnum != PropertyEnum.Invalid)) return false;

            bool gatheringPropertyInfo = propertyCollection == null;

            PropertyBuilder propertyBuilder = new(propertyEnum, propertyInfoTable, gatheringPropertyInfo);
            if (!Verify.IsTrue(DeserializeFieldGroupIntoPropertyBuilder(ref propertyBuilder, blueprint, prototypeName, record, gatheringPropertyInfo, groupTag)))
                return false;

            if (gatheringPropertyInfo)
            {
                propertyBuilder.SetPropertyInfo();
                return true;
            }

            // We should get here only after we have already initialized all property infos
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEnum);

            // Build property id
            PropertyId propertyId = propertyBuilder.GetPropertyId();

            // Set a property or override the id of an existing one
            if (info.IsCurveProperty == false)
            {
                if (propertyBuilder.IsValueSet)
                {
                    // Set a property if we have a value
                    propertyCollection.SetPropertyFromMixin(propertyBuilder.PropertyValue, propertyId, blueprintCopyNum, propertyBuilder.ParamsSetMask);
                }
                else
                {
                    // If no value is defined in the field group it means we need to override the id (params) of an existing value
                    propertyCollection.ReplacePropertyIdFromMixin(propertyId, blueprintCopyNum, propertyBuilder.ParamsSetMask);
                }
            }
            else
            {
                if (propertyBuilder.IsValueSet)
                {
                    // Set a curve property if we have a value
                    CurveId curveRef = propertyBuilder.PropertyValue;
                    if (!Verify.IsTrue(curveRef != CurveId.Invalid)) return false;

                    PropertyId indexProperty = propertyBuilder.CurveIndex;
                    propertyCollection.SetCurvePropertyFromMixin(propertyId, curveRef, indexProperty, info, blueprintCopyNum);
                }
                else
                {
                    // Override property id and / or curve index property of an existing property
                    if (propertyBuilder.IsCurveIndexSet)
                    {
                        // Override both the property id and the index property
                        PropertyId indexProperty = propertyBuilder.CurveIndex;
                        if (!Verify.IsTrue(indexProperty.Enum != PropertyEnum.Invalid, $"Curve properties must have an index property"))
                            return false;

                        propertyCollection.ReplaceCurvePropertyIdFromMixin(propertyId, indexProperty, info, blueprintCopyNum, propertyBuilder.ParamsSetMask);
                    }
                    else
                    {
                        // Override just the id of the property itself if no curve index is provided in the field group
                        propertyCollection.ReplaceCurvePropertyIdFromMixin(propertyId, info, blueprintCopyNum, propertyBuilder.ParamsSetMask);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Deserializes a property field group into a <see cref="PropertyBuilder"/> instance.
        /// </summary>
        private static bool DeserializeFieldGroupIntoPropertyBuilder(ref PropertyBuilder builder, Blueprint blueprint, string prototypeName, CalligraphyReader record, bool gatheringPropertyInfo, string groupTag)
        {
            string errorMessage = null;
            BlueprintMemberInfo blueprintMemberInfo = default;

            PrototypeId propertyDataRef = blueprint.PropertyDataRef;
            PropertyEnum propertyEnum = GameDatabase.PropertyInfoTable.GetPropertyEnumFromPrototype(propertyDataRef);
            if (!Verify.IsTrue(propertyEnum != PropertyEnum.Invalid)) return false;

            if (!Verify.IsTrue(record.Read(out short numFields), $"Error reading number of fields in {prototypeName} in group {groupTag}"))
                return false;

            if (!Verify.IsTrue(numFields > 0, $"Read <= 0 for number of fields in {prototypeName} in group {groupTag}"))
                return false;

            for (int i = 0; i < numFields; i++)
            {
                if (record.Read(out StringId fieldId) == false)
                {
                    errorMessage = "Unable to read field id";
                    goto Error;
                }

                if (fieldId == StringId.Invalid)
                {
                    errorMessage = "Invalid blueprint field id encountered";
                    goto Error;
                }

                if (record.Read(out CalligraphyBaseType fieldBaseType) == false)
                {
                    errorMessage = "Field type not found";
                    goto Error;
                }

                if (blueprint.GetBlueprintMemberInfo(fieldId, out blueprintMemberInfo) == false)
                {
                    errorMessage = "Unable to get blueprint field info for field ref";
                    goto Error;
                }

                if (blueprintMemberInfo.Member.BaseType != fieldBaseType)
                {
                    errorMessage = "Type mismatch between blueprint and prototype";
                    goto Error;
                }

                // Fields with the same name can have different field ids in different property prototypes
                // (most likely due to how they are hashed), so we have no choice but to work with strings here.
                string fieldName = blueprintMemberInfo.Member.FieldName;

                if (string.Equals(fieldName, "Value", StringComparison.OrdinalIgnoreCase))
                {
                    if (DeserializePropertyValue(blueprint, prototypeName, record, blueprintMemberInfo, out PropertyValue value) == false ||
                        builder.SetValue(value) == false)
                    {
                        errorMessage = "Invalid property 'Value' field.";
                        goto Error;
                    }
                }
                else if (string.Equals(fieldName, "CurveIndex", StringComparison.OrdinalIgnoreCase))
                {
                    if (DeserializePropertyValue(blueprint, prototypeName, record, blueprintMemberInfo, out PropertyValue curveIndex) == false)
                    {
                        errorMessage = "Invalid property 'CurveIndex' field.";
                        goto Error;
                    }

                    if (builder.SetCurveIndex((PrototypeId)curveIndex) == false)
                    {
                        errorMessage = "Invalid property 'CurveIndex' field (did you leave a CurveIndex field set to <none>?)";
                        goto Error;
                    }
                }
                else if (fieldName.StartsWith("Param", StringComparison.OrdinalIgnoreCase))
                {
                    int paramIndex;
                    if (fieldName.Length >= 6)
                    {
                        paramIndex = int.Parse(fieldName.AsSpan(5, 1));
                    }
                    else
                    {
                        //Logger.Trace($"DeserializeFieldGroupIntoPropertyBuilder(): Param field name '{fieldName}' does not contain param index, defaulting to 0, file name {prototypeName}");
                        paramIndex = 0;     // This probably works out client-side because of the null terminator?
                    }

                    if (paramIndex >= Property.MaxParamCount ||
                        DeserializePropertyParam(blueprintMemberInfo, prototypeName, record, paramIndex, ref builder) == false)
                    {
                        errorMessage = "Invalid property 'Param' field.";
                        goto Error;
                    }
                }
                else
                {
                    // Custom error not present in the client, this probably shouldn't be happening.
                    errorMessage = "Unexpected field name in a property field group";
                    goto Error;
                }
            }

            return true;

        Error:
            Verify.IsTrue(false, $"Error: '{errorMessage}' [{groupTag}]\n Field Name: '{blueprintMemberInfo.Member?.FieldName}'\n Prototype: '{prototypeName}'\n");
            return false;
        }

        /// <summary>
        /// Deserializes a <see cref="PropertyValue"/> from a field group.
        /// </summary>
        private static bool DeserializePropertyValue(Blueprint blueprint, string prototypeName, CalligraphyReader record, BlueprintMemberInfo blueprintMemberInfo, out PropertyValue value)
        {
            value = default;

            if (!Verify.IsTrue(blueprintMemberInfo.Member.StructureType == CalligraphyStructureType.Single)) return false;

            // NOTE: We read values here directly rather than going through FieldParser / FieldParserParams like the client does.
            switch (blueprintMemberInfo.Member.BaseType)
            {
                case CalligraphyBaseType.Asset:
                    if (!Verify.IsTrue(record.Read(out AssetId assetId))) return false;
                    value = assetId;
                    break;

                case CalligraphyBaseType.Boolean:
                    if (!Verify.IsTrue(record.Read(out ulong boolean))) return false;
                    value = boolean != 0;
                    break;

                case CalligraphyBaseType.Curve:
                    if (!Verify.IsTrue(record.Read(out CurveId curveId))) return false;
                    value = curveId;
                    break;

                case CalligraphyBaseType.Double:
                    if (!Verify.IsTrue(record.Read(out double @double))) return false;
                    value = (float)@double;
                    break;

                case CalligraphyBaseType.Long:
                    if (!Verify.IsTrue(record.Read(out long @long))) return false;
                    value = @long;
                    break;

                case CalligraphyBaseType.Prototype:
                    if (!Verify.IsTrue(record.Read(out PrototypeId prototypeId))) return false;
                    value = prototypeId;
                    break;

                default:
                    Verify.IsTrue(false, "Unhandled base type for property value");
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Deserializes a <see cref="PropertyParam"/> from a field group and sets it in the provided <see cref="PropertyBuilder"/> instance.
        /// </summary>
        private static bool DeserializePropertyParam(BlueprintMemberInfo blueprintMemberInfo, string prototypeName, CalligraphyReader record, int paramIndex, ref PropertyBuilder builder)
        {
            if (!Verify.IsTrue(blueprintMemberInfo.Member.StructureType == CalligraphyStructureType.Single, "Unhandled structure type for property value"))
                return false;

            // NOTE: We read values here directly rather than going through FieldParser / FieldParserParams like the client does.
            switch (blueprintMemberInfo.Member.BaseType)
            {
                case CalligraphyBaseType.Long:
                    if (!Verify.IsTrue(record.Read(out long integerParam))) return false;

                    if (!Verify.IsTrue(builder.SetIntegerParam(paramIndex, integerParam))) return false;

                    break;

                case CalligraphyBaseType.Asset:
                    if (!Verify.IsTrue(record.Read(out AssetId assetParam))) return false;

                    if (!Verify.IsTrue(builder.SetAssetParam(paramIndex, assetParam),
                        $"Error: {prototypeName}\n 'Property Parameter of type Asset must contain a valid asset value.'\n Field Name: '{blueprintMemberInfo.Member.FieldName}'"))
                        return false;

                    break;

                case CalligraphyBaseType.Prototype:
                    if (!Verify.IsTrue(record.Read(out PrototypeId prototypeParam))) return false;

                    if (!Verify.IsTrue(builder.SetPrototypeParam(paramIndex, prototypeParam),
                        $"Error: {prototypeName}\n 'Property Parameter of type Prototype must contain a valid value.'\n Field Name: '{blueprintMemberInfo.Member.FieldName}'"))
                        return false;

                    break;

                default:
                    Verify.IsTrue(false, "Unhandled base type for property param");
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Deserializes a standalone <see cref="PropertyId"/> from a field group.
        /// </summary>
        private static bool DeserializeFieldGroupIntoPropertyId(ref PropertyId id, Blueprint blueprint, string prototypeName, CalligraphyReader record, string groupTag)
        {
            PropertyInfoTable propertyInfoTable = GameDatabase.PropertyInfoTable;

            PrototypeId propertyPrototypeRef = blueprint.PropertyDataRef;
            PropertyEnum propertyEnum = propertyInfoTable.GetPropertyEnumFromPrototype(propertyPrototypeRef);
            if (!Verify.IsTrue(propertyEnum != PropertyEnum.Invalid)) return false;

            PropertyBuilder propertyBuilder = new(propertyEnum, propertyInfoTable, false);
            if (!Verify.IsTrue(DeserializeFieldGroupIntoPropertyBuilder(ref propertyBuilder, blueprint, prototypeName, record, false, groupTag)))
                return false;

            id = propertyBuilder.GetPropertyId();
            if (!Verify.IsTrue(id != PropertyId.Invalid)) return false;

            return true;
        }

        /// <summary>
        /// Returns the <see cref="PrototypePropertyCollection"/> belonging to the provided <see cref="Prototype"/> if it has one.
        /// Returns <see langword="null"/> if the prototype has no <see cref="PrototypePropertyCollection"/> fields.
        /// </summary>
        public static PrototypePropertyCollection GetPropertyCollectionField(Prototype prototype)
        {
            // NOTE: This method is public because it is also used by PowerPrototype during post-processing.
            if (!Verify.IsNotNull(prototype)) return null;

            PrototypeFieldSet fieldSet = GameDatabase.PrototypeClassManager.GetPrototypeFieldSet(prototype.GetType());
            if (!Verify.IsNotNull(fieldSet)) return null;

            PrototypeFieldInfo fieldInfo = fieldSet.PropertyCollection;
            if (fieldInfo == null)
                return null;

            return GetPropertyCollectionField(prototype, fieldInfo);
        }

        /// <summary>
        /// Returns the <see cref="PrototypePropertyCollection"/> belonging to the provided <see cref="Prototype"/>.
        /// </summary>
        private static PrototypePropertyCollection GetPropertyCollectionField(Prototype prototype, PrototypeFieldInfo fieldInfo)
        {
            fieldInfo.GetValue(prototype, out PrototypePropertyCollection collection);

            // Initialize a new collection in this field if there isn't one already or it doesn't belong to it
            if (collection == null || prototype.IsDynamicFieldOwnedBy(collection) == false)
            {
                // Copy parent collection if there is one, otherwise start with a blank one
                collection = collection == null ? new() : collection.ShallowCopy();
                if (!Verify.IsNotNull(collection)) return null;

                fieldInfo.SetValue(prototype, collection);
                prototype.SetDynamicFieldOwner(collection);
            }

            return collection;
        }

        /// <summary>
        /// Checks if a mixin field should hold the specified property in its collection.
        /// </summary>
        private static bool CheckPropertyMixin(PrototypeFieldInfo mixinFieldInfo, PrototypeFieldType fieldType, Blueprint prototypeBlueprint,
            Blueprint propertyBlueprint, byte blueprintCopyNum, Prototype parentPrototype, ref Type propertyHolderClassType, ref Prototype propertyHolderPrototype)
        {
            if (!Verify.IsNotNull(mixinFieldInfo)) return false;
            if (!Verify.IsNotNull(parentPrototype)) return false;
            if (!Verify.IsTrue(mixinFieldInfo.Type == PrototypeFieldType.Mixin || mixinFieldInfo.Type == PrototypeFieldType.ListMixin)) return false;

            Type bindingType = fieldType == PrototypeFieldType.ListMixin ? mixinFieldInfo.ListElementType : mixinFieldInfo.ClassType;
            Blueprint mixinBlueprint = prototypeBlueprint.FindRuntimeBindingInBlueprintHierarchy(bindingType, propertyBlueprint);
            if (mixinBlueprint == null)
                return false;

            propertyHolderClassType = mixinBlueprint.RuntimeBindingClassType;
            propertyHolderPrototype = AcquireMixinElement(parentPrototype, propertyHolderClassType, prototypeBlueprint,
                mixinBlueprint, blueprintCopyNum, mixinFieldInfo, fieldType);
            return true;
        }

        #endregion

        #region Field Copying

        /// <summary>
        /// Copies field values from a <see cref="Prototype"/> with the specified data ref.
        /// </summary>
        public static bool CopyPrototypeDataRefFields(Prototype destPrototype, PrototypeId sourceDataRef)
        {
            if (!Verify.IsTrue(sourceDataRef != PrototypeId.Invalid)) return false;

            Prototype sourcePrototype = GameDatabase.GetPrototype<Prototype>(sourceDataRef);
            if (!Verify.IsNotNull(sourcePrototype)) return false;

            return CopyPrototypeFields(destPrototype, sourcePrototype);
        }

        /// <summary>
        /// Copies all appropriate field values from one <see cref="Prototype"/> instance to another.
        /// </summary>
        private static bool CopyPrototypeFields(Prototype destPrototype, Prototype sourcePrototype)
        {
            // In some cases (e.g. PopulationInfoPrototype mixin) destination and/or source may be null
            if (destPrototype == null || sourcePrototype == null)
                return true;

            // Get type information for both prototypes and make sure they are the same
            Type sourceType = sourcePrototype.GetType();
            Type destType = destPrototype.GetType();

            if (!Verify.IsTrue(sourceType == destType)) return false;

            foreach (PrototypeFieldInfo fieldInfo in GameDatabase.PrototypeClassManager.GetPrototypeFieldSet(destType))
            {
                switch (fieldInfo.Type)
                {
                    case PrototypeFieldType.Bool:
                    case PrototypeFieldType.Int8:
                    case PrototypeFieldType.Int16:
                    case PrototypeFieldType.Int32:
                    case PrototypeFieldType.Int64:
                    case PrototypeFieldType.Float32:
                    case PrototypeFieldType.Float64:
                    case PrototypeFieldType.Enum:
                    case PrototypeFieldType.AssetRef:
                    case PrototypeFieldType.AssetTypeRef:
                    case PrototypeFieldType.CurveRef:
                    case PrototypeFieldType.PrototypeDataRef:
                    case PrototypeFieldType.LocaleStringId:
                    case PrototypeFieldType.PrototypePtr:
                    case PrototypeFieldType.PrototypeRefPtr:
                    case PrototypeFieldType.PropertyId:
                        AssignPointedAtValues(destPrototype, sourcePrototype, fieldInfo);
                        break;

                    case PrototypeFieldType.ListBool:
                    case PrototypeFieldType.ListInt8:
                    case PrototypeFieldType.ListInt16:
                    case PrototypeFieldType.ListInt32:
                    case PrototypeFieldType.ListInt64:
                    case PrototypeFieldType.ListFloat32:
                    case PrototypeFieldType.ListFloat64:
                    case PrototypeFieldType.ListEnum:
                    case PrototypeFieldType.ListAssetRef:
                    case PrototypeFieldType.ListAssetTypeRef:
                    case PrototypeFieldType.ListPrototypeDataRef:
                    case PrototypeFieldType.ListPrototypePtr:
                    case PrototypeFieldType.VectorPrototypeRefPtr:
                        ShallowCopyCollection(destPrototype, sourcePrototype, fieldInfo);
                        break;

                    case PrototypeFieldType.Mixin:
                        CopyMixin(destPrototype, sourcePrototype, fieldInfo);
                        break;

                    case PrototypeFieldType.ListMixin:
                        CopyMixinCollection(destPrototype, sourcePrototype, fieldInfo);
                        break;

                    case PrototypeFieldType.PropertyList:
                    case PrototypeFieldType.PropertyCollection:
                        CopyPrototypePropertyCollection(destPrototype, sourcePrototype, fieldInfo);
                        break;

                    default:
                        Verify.IsTrue(false, $"Unhandled prototype field info name: {fieldInfo.Name} type: {fieldInfo.Type} in prototype: {sourcePrototype}");
                        break;
                }
            }

            return true;
        }

        /// <summary>
        /// Copies a field value from one <see cref="Prototype"/> instance to another.
        /// </summary>
        private static void AssignPointedAtValues(Prototype destPrototype, Prototype sourcePrototype, PrototypeFieldInfo fieldInfo)
        {
            fieldInfo.CopyValue(sourcePrototype, destPrototype);
        }

        /// <summary>
        /// Shallow copies a collection field from a source <see cref="Prototype"/>.
        /// </summary>
        private static void ShallowCopyCollection(Prototype destPrototype, Prototype sourcePrototype, PrototypeFieldInfo fieldInfo)
        {
            fieldInfo.CopyArray(sourcePrototype, destPrototype);
        }

        /// <summary>
        /// Copies a mixin field from a source <see cref="Prototype"/>.
        /// </summary>
        private static void CopyMixin(Prototype destPrototype, Prototype sourcePrototype, PrototypeFieldInfo fieldInfo)
        {
            fieldInfo.GetValue(sourcePrototype, out Prototype sourceMixin);
            if (sourceMixin == null)
                return;

            // Create the mixin instance on the destination prototype if there is something to copy and copy data to it
            Prototype destMixin = GameDatabase.PrototypeClassManager.AllocatePrototype(fieldInfo.ClassType);
            fieldInfo.SetValue(destPrototype, destMixin);

            CopyPrototypeFields(destMixin, sourceMixin);
        }

        /// <summary>
        /// Copies a <see cref="PrototypeMixinList"/> from a source <see cref="Prototype"/>.
        /// </summary>
        private static void CopyMixinCollection(Prototype destPrototype, Prototype sourcePrototype, PrototypeFieldInfo fieldInfo)
        {
            fieldInfo.GetValue(sourcePrototype, out PrototypeMixinList sourceList);
            if (sourceList == null)
                return;

            // Create a new list mixin on the destination prototype and take ownership of it
            PrototypeMixinList destList = new();
            fieldInfo.SetValue(destPrototype, destList);
            destPrototype.SetDynamicFieldOwner(destList);

            // Copy all items from the old list
            foreach (PrototypeMixinListItem sourceListItem in sourceList)
            {
                // Create a new item in the destination list
                PrototypeMixinListItem destListItem = new();
                destList.Add(destListItem);

                // Create a copy of the mixin from the source list and take ownership of it
                destListItem.Prototype = AllocateDynamicPrototype(sourceListItem.Prototype.GetType(), PrototypeId.Invalid, sourceListItem.Prototype);
                destListItem.Prototype.ParentDataRef = sourceListItem.Prototype.ParentDataRef;
                destPrototype.SetDynamicFieldOwner(destListItem.Prototype);

                // Copy list item metadata
                destListItem.BlueprintRef = sourceListItem.BlueprintRef;
                destListItem.BlueprintCopyNum = sourceListItem.BlueprintCopyNum;
            }
        }

        /// <summary>
        /// Copies a <see cref="PrototypePropertyCollection"/> from a source <see cref="Prototype"/>.
        /// </summary>
        private static void CopyPrototypePropertyCollection(Prototype destPrototype, Prototype sourcePrototype, PrototypeFieldInfo fieldInfo)
        {
            fieldInfo.GetValue(sourcePrototype, out PrototypePropertyCollection sourcePropertyCollection);
            if (sourcePropertyCollection == null)
                return;

            // Create a copy of the source property collection and take ownership of it
            PrototypePropertyCollection destPropertyCollection = sourcePropertyCollection.ShallowCopy();
            fieldInfo.SetValue(destPrototype, destPropertyCollection);
            destPrototype.SetDynamicFieldOwner(destPropertyCollection);
        }

        #endregion

        #region Mixin Management

        /// <summary>
        /// Acquires either the mixin itself or an element from a list mixin.
        /// </summary>
        private static Prototype AcquireMixinElement(Prototype ownerPrototype, Type elementClassType, Blueprint ownerBlueprint, Blueprint mixinBlueprint,
            byte blueprintCopyNum, PrototypeFieldInfo mixinFieldInfo, PrototypeFieldType fieldType)
        {
            if (!Verify.IsNotNull(ownerPrototype)) return null;
            if (!Verify.IsNotNull(ownerBlueprint)) return null;
            if (!Verify.IsNotNull(mixinBlueprint)) return null;
            if (!Verify.IsNotNull(mixinFieldInfo)) return null;

            if (fieldType == PrototypeFieldType.Mixin)
            {
                // Allocate a simple mixin if needed and return it
                mixinFieldInfo.GetValue(ownerPrototype, out Prototype element);
                if (element == null)
                {
                    element = GameDatabase.PrototypeClassManager.AllocatePrototype(mixinFieldInfo.ClassType);
                    mixinFieldInfo.SetValue(ownerPrototype, element);
                }

                return element;
            }
            else if (fieldType == PrototypeFieldType.ListMixin)
            {
                PrototypeMixinList list = AcquireOwnedMixinList(ownerPrototype, mixinFieldInfo, false);
                if (!Verify.IsNotNull(list)) return null;
                return AcquireOwnedUniqueMixinListElement(ownerPrototype, list, elementClassType, mixinBlueprint, blueprintCopyNum);
            }

            Verify.IsTrue(false, "Field type not a mixin field!");
            return null;
        }

        /// <summary>
        /// Creates if needed and returns a <see cref="PrototypeMixinList"/> from the specified field of the provided <see cref="Prototype"/> instance that belongs to it.
        /// </summary>
        public static PrototypeMixinList AcquireOwnedMixinList(Prototype prototype, PrototypeFieldInfo mixinFieldInfo, bool copyItemsFromParent)
        {
            if (!Verify.IsNotNull(prototype)) return null;
            if (!Verify.IsNotNull(mixinFieldInfo)) return null;
            if (!Verify.IsTrue(mixinFieldInfo.Type == PrototypeFieldType.ListMixin)) return null;

            // Create a new list if there isn't one or it belongs to another prototype
            mixinFieldInfo.GetValue(prototype, out PrototypeMixinList list);
            if (list == null || prototype.IsDynamicFieldOwnedBy(list) == false)
            {
                PrototypeMixinList newList = new();

                // Fill the new list
                if (list != null)
                {
                    if (copyItemsFromParent)
                    {
                        // Create copies of all parent items and take ownership of those copies
                        foreach (PrototypeMixinListItem item in list)
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
                mixinFieldInfo.SetValue(prototype, newList);
                prototype.SetDynamicFieldOwner(newList);

                list = newList;
            }

            return list;
        }

        /// <summary>
        /// Creates if needed and returns a <see cref="Prototype"/> element from a <see cref="PrototypeMixinList"/>.
        /// </summary>
        private static Prototype AcquireOwnedUniqueMixinListElement(Prototype owner, PrototypeMixinList list, Type elementClassType,
            Blueprint elementBlueprint, byte blueprintCopyNum)
        {
            if (!Verify.IsNotNull(owner)) return null;
            if (!Verify.IsNotNull(list)) return null;
            if (!Verify.IsNotNull(elementClassType)) return null;
            if (!Verify.IsNotNull(elementBlueprint)) return null;

            // Look for a unique list element
            PrototypeMixinListItem uniqueListElement = FindUniqueMixinListElement(list, elementClassType, elementBlueprint.BlueprintDataRef, blueprintCopyNum);

            if (uniqueListElement == null)
            {
                // Create the element we're looking for if it's not in our list
                Prototype prototype = AllocateDynamicPrototype(elementClassType, elementBlueprint.DefaultPrototypeRef, null);
                prototype.ParentDataRef = elementBlueprint.DefaultPrototypeRef;

                // Assign ownership of the new mixin
                owner.SetDynamicFieldOwner(prototype);

                // Add the new mixin to the list
                PrototypeMixinListItem newListItem = new()
                {
                    Prototype = prototype,
                    BlueprintRef = elementBlueprint.BlueprintDataRef,
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

        private static PrototypeMixinListItem FindUniqueMixinListElement(PrototypeMixinList list, Type elementClassType, BlueprintId blueprintRef, byte blueprintCopyNum)
        {
            if (!Verify.IsNotNull(elementClassType)) return null;

            foreach (PrototypeMixinListItem element in list)
            {
                // Type check goes last because it's the most expensive one
                if (element.BlueprintRef == blueprintRef &&
                    element.BlueprintCopyNum == blueprintCopyNum &&
                    element.Prototype.GetType() == elementClassType)
                {
                    return element;
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a copy of a <see cref="Prototype"/> element from a parent <see cref="PrototypeMixinList"/> and assigns it to the child.
        /// </summary>
        private static Prototype AddMixinListItemCopy(Prototype owner, PrototypeMixinList list, PrototypeMixinListItem item)
        {
            if (!Verify.IsNotNull(list)) return null;
            if (!Verify.IsNotNull(owner)) return null;

            // Copy the prototype from the provided list item
            Prototype element = AllocateDynamicPrototype(item.Prototype.GetType(), PrototypeId.Invalid, item.Prototype);
            if (!Verify.IsNotNull(element)) return null;

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
        /// Creates a new <see cref="Prototype"/> of the specified <see cref="Type"/> and fills it with data from the specified source (either a default prototype or a prototype instance).
        /// </summary>
        private static Prototype AllocateDynamicPrototype(Type classType, PrototypeId defaults, Prototype instanceToCopy)
        {
            // Create a new prototype of the specified type
            Prototype prototype = GameDatabase.PrototypeClassManager.AllocatePrototype(classType);
            if (!Verify.IsNotNull(prototype)) return null;

            // Copy fields either from the specified defaults prototype or the provided prototype
            if (defaults != PrototypeId.Invalid && instanceToCopy == null)
            {
                Prototype defaultsProto = GameDatabase.GetPrototype<Prototype>(defaults);
                if (!Verify.IsNotNull(defaultsProto)) return null;
                if (!Verify.IsTrue(CopyPrototypeFields(prototype, defaultsProto))) return null;
            }
            else if (instanceToCopy != null)
            {
                if (!Verify.IsTrue(CopyPrototypeFields(prototype, instanceToCopy))) return null;
            }

            return prototype;
        }

        #endregion

        #region Parsing

        private delegate bool FieldParser(in FieldParserParams @params);

        /// <summary>
        /// Returns a <see cref="FieldParser"/> for the specified <see cref="PrototypeFieldType"/> enum value.
        /// </summary>
        private static FieldParser GetParser(PrototypeFieldType fieldType)
        {
            // Delegate caching here should be handled by C# starting with C# 11 / .NET 7.
            // https://devblogs.microsoft.com/dotnet/understanding-the-cost-of-csharp-delegates/#c#-11
            switch (fieldType)
            {
                case PrototypeFieldType.Int8:                   return ParseInt8;
                case PrototypeFieldType.Int16:                  return ParseInt16;
                case PrototypeFieldType.Int32:                  return ParseInt32;
                case PrototypeFieldType.Int64:                  return ParseUnmanaged64<long>;
                case PrototypeFieldType.Bool:                   return ParseBool;
                case PrototypeFieldType.Float32:                return ParseFloat32;
                case PrototypeFieldType.Float64:                return ParseUnmanaged64<double>;
                case PrototypeFieldType.Enum:                   return ParseEnum;
                case PrototypeFieldType.AssetRef:               return ParseUnmanaged64<AssetId>;
                case PrototypeFieldType.AssetTypeRef:           return ParseUnmanaged64<AssetTypeId>;
                case PrototypeFieldType.CurveRef:               return ParseUnmanaged64<CurveId>;
                case PrototypeFieldType.PrototypeDataRef:       return ParseUnmanaged64<PrototypeId>;
                case PrototypeFieldType.LocaleStringId:         return ParseUnmanaged64<LocaleStringId>;
                case PrototypeFieldType.PrototypePtr:           return ParsePrototypePtr;
                case PrototypeFieldType.PrototypeRefPtr:        return ParsePrototypeRefPtr;
                case PrototypeFieldType.PropertyId:             return ParsePropertyId;
                case PrototypeFieldType.ListBool:               return ParseListBool;
                case PrototypeFieldType.ListInt8:               return ParseListInt8;
                case PrototypeFieldType.ListInt16:              return ParseListInt16;
                case PrototypeFieldType.ListInt32:              return ParseListInt32;
                case PrototypeFieldType.ListInt64:              return ParseListUnmanaged64<long>;
                case PrototypeFieldType.ListFloat32:            return ParseListFloat32;
                case PrototypeFieldType.ListFloat64:            return ParseListUnmanaged64<double>;
                case PrototypeFieldType.ListEnum:               return ParseListEnum;
                case PrototypeFieldType.ListAssetRef:           return ParseListUnmanaged64<AssetId>;
                case PrototypeFieldType.ListAssetTypeRef:       return ParseListUnmanaged64<AssetTypeId>;
                case PrototypeFieldType.ListPrototypeDataRef:   return ParseListUnmanaged64<PrototypeId>;
                case PrototypeFieldType.ListPrototypePtr:       return ParseListPrototypePtr;
                case PrototypeFieldType.VectorPrototypeRefPtr:  return ParseVectorPrototypeRefPtr;
                case PrototypeFieldType.PropertyList:           return ParsePropertyList;

                default:
                    Verify.IsTrue(false, $"Failed to find parser for fieldType: {fieldType}");
                    return null;
            }
        }

        /// <summary>
        /// Parses an <see cref="sbyte"/> value and assigns it to a prototype field.
        /// </summary>
        private static bool ParseInt8(in FieldParserParams @params)
        {
            if (!Verify.IsTrue(@params.Reader.Read(out long serializedValue))) return false;
            @params.FieldInfo.SetValue(@params.OwnerPrototype, (sbyte)serializedValue);
            return true;
        }

        /// <summary>
        /// Parses a <see cref="short"/> value and assigns it to a prototype field.
        /// </summary>
        private static bool ParseInt16(in FieldParserParams @params)
        {
            if (!Verify.IsTrue(@params.Reader.Read(out long serializedValue))) return false;
            @params.FieldInfo.SetValue(@params.OwnerPrototype, (short)serializedValue);
            return true;
        }

        /// <summary>
        /// Parses an <see cref="int"/> value and assigns it to a prototype field.
        /// </summary>
        private static bool ParseInt32(in FieldParserParams @params)
        {
            if (!Verify.IsTrue(@params.Reader.Read(out long serializedValue))) return false;

            // Some prototypes (e.g. ProceduralProfileDrDoomPhase1.defaults) use very high values for int fields that cause overflows.
            // The client handles this by taking the first 4 bytes of the value and throwing away everything else.
            // We handle this by setting those fields to int.MaxValue, since the intention is apparently to have the value be as high
            // as possible. This doesn't seem to happen with other types.
            if (serializedValue > int.MaxValue)
            {
                Logger.Trace($"ParseInt32(): Overflow for Int32 field {@params.BlueprintMemberInfo.Member.FieldName}, serialized value {serializedValue}, file name {@params.FileName}");
                serializedValue = int.MaxValue;
            }

            @params.FieldInfo.SetValue(@params.OwnerPrototype, (int)serializedValue);
            return true;
        }

        /// <summary>
        /// Parses an <see langword="unmanaged"/> 64-bit value and assigns it to a prototype field.
        /// </summary>
        /// <remarks>
        /// We use this for any 64-bit values that don't need to be cast after reading.
        /// </remarks>
        private static bool ParseUnmanaged64<T>(in FieldParserParams @params) where T: unmanaged
        {
            if (!Verify.IsTrue(@params.Reader.Read(out T serializedValue))) return false;
            @params.FieldInfo.SetValue(@params.OwnerPrototype, serializedValue);
            return true;
        }

        /// <summary>
        /// Parses a <see cref="bool"/> value and assigns it to a prototype field.
        /// </summary>
        private static bool ParseBool(in FieldParserParams @params)
        {
            if (!Verify.IsTrue(@params.Reader.Read(out long serializedValue))) return false;
            @params.FieldInfo.SetValue(@params.OwnerPrototype, serializedValue != 0);
            return true;
        }

        /// <summary>
        /// Parses a <see cref="float"/> value and assigns it to a prototype field.
        /// </summary>
        private static bool ParseFloat32(in FieldParserParams @params)
        {
            if (!Verify.IsTrue(@params.Reader.Read(out double serializedValue))) return false;
            @params.FieldInfo.SetValue(@params.OwnerPrototype, (float)serializedValue);
            return true;
        }

        /// <summary>
        /// Parses an asset enum and assigns it to a prototype field.
        /// </summary>
        private static bool ParseEnum(in FieldParserParams @params)
        {
            // Enums are represented in Calligraphy by assets.
            // NOTE: Invalid asset refs should be interpreted as zero rather than the default value.
            if (!Verify.IsTrue(@params.Reader.Read(out AssetId assetId))) return false;

            int value = 0;

            if (assetId != AssetId.Invalid)
            {
                string assetName = GameDatabase.GetAssetName(assetId);

                SymbolicLookup<int> symbolicEnum = @params.FieldInfo.SymbolicEnum;
                if (!Verify.IsNotNull(symbolicEnum)) return false;

                value = symbolicEnum.ToLookupValue(assetName, out bool found);

                if (found == false)
                    Logger.Trace($"ParseEnum(): Missing enum member {assetName} in {@params.FieldInfo.ClassType.Name}, field {@params.FieldInfo.Name}, file name {@params.FileName}");
            }

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
            if (DeserializePrototypePtr(@params, false, out Prototype prototype) == false)
                return false;

            @params.FieldInfo.SetValue(@params.OwnerPrototype, prototype);
            return true;
        }

        /// <summary>
        /// Deserializes an embedded prototype WITHOUT assigning it to a field.
        /// </summary>
        private static bool DeserializePrototypePtr(in FieldParserParams @params, bool polymorphicSetAllowed, out Prototype prototype)
        {
            prototype = null;
            CalligraphyReader reader = @params.Reader;

            // Parse header
            if (!Verify.IsTrue(reader.ReadPrototypeHeader(out PrototypeDataHeader header, @params.FileName))) return false;

            if (header.ReferenceExists == false)
                return true;   // Early return if this is an empty prototype

            if (!Verify.IsTrue(header.ReferenceType != PrototypeId.Invalid)) return false;

            if (!Verify.IsTrue(polymorphicSetAllowed || header.PolymorphicData == false, $"Polymorphic prototype data encountered but not expected"))
                return false;
            
            // If this prototype has no data of its own, but it references a parent, we interpret it as its parent
            if (header.InstanceDataExists == false)
            {
                prototype = GameDatabase.GetPrototype<Prototype>(header.ReferenceType);
                return true;
            }

            // Deserialize
            Type classType = GameDatabase.DataDirectory.GetPrototypeClassType(header.ReferenceType);
            prototype = GameDatabase.PrototypeClassManager.AllocatePrototype(classType);

            return DoDeserialize(prototype, header, PrototypeId.Invalid, @params.FileName, reader);
        }

        /// <summary>
        /// Parses a <see cref="PrototypeId"/>, retrieves the <see cref="Prototype"/> instance associated with it, and assigns it to a field.
        /// </summary>
        private static bool ParsePrototypeRefPtr(in FieldParserParams @params)
        {
            // We handle PrototypeRefPtr similarly to RHStructs without instance data
            if (!Verify.IsTrue(@params.Reader.Read(out PrototypeId protoRef))) return false;

            Prototype prototype = GameDatabase.GetPrototype<Prototype>(protoRef);

            @params.FieldInfo.SetValue(@params.OwnerPrototype, prototype);
            return true;
        }

        /// <summary>
        /// Parses a <see cref="PropertyId"/> and assigns it to a prototype field.
        /// </summary>
        private static bool ParsePropertyId(in FieldParserParams @params)
        {
            CalligraphyReader reader = @params.Reader;

            if (!Verify.IsTrue(reader.ReadPrototypeHeader(out PrototypeDataHeader header, @params.FileName))) return false;

            if (header.InstanceDataExists)
            {
                if (!Verify.IsTrue(reader.Read(out short numFieldGroups), $"Unable to read number of field groups for {@params.OwnerPrototype}"))
                    return false;

                for (int i = 0; i < numFieldGroups; i++)
                {
                    if (!Verify.IsTrue(reader.Read(out BlueprintId groupBlueprintDataRef), $"Error reading declaring blueprint id from {@params.OwnerPrototype}"))
                        return false;

                    if (!Verify.IsTrue(groupBlueprintDataRef != BlueprintId.Invalid)) return false;

                    if (!Verify.IsTrue(reader.Read(out byte fieldGroupCopyNum), $"Error reading blueprint copy number from {@params.OwnerPrototype}"))
                        return false;

                    Blueprint groupBlueprint = GameDatabase.GetBlueprint(groupBlueprintDataRef);
                    if (!Verify.IsNotNull(groupBlueprint, $"Failed to get declaring blueprint from id for {@params.OwnerPrototype}"))
                        return false;

                    if (Verify.IsTrue(groupBlueprint.IsProperty))
                    {
                        PropertyId id = PropertyId.Invalid;
                        DeserializeFieldGroupIntoPropertyId(ref id, groupBlueprint, @params.FileName, reader, "Property List");
                        Verify.IsTrue(id != PropertyId.Invalid);

                        @params.FieldInfo.SetValue(@params.OwnerPrototype, id);
                    }

                    // Same as in DeserializePropertyMixin(), there should be no list fields
                    if (!Verify.IsTrue(reader.Read(out short numFields))) return false;
                    if (!Verify.IsTrue(numFields == 0)) return false;
                }
            }
            else if (header.ReferenceExists)
            {
                // If there is no data but a reference to a parent exists, get default property id from parent blueprint
                Blueprint blueprint = GameDatabase.DataDirectory.GetPrototypeBlueprint(header.ReferenceType);
                if (!Verify.IsTrue(blueprint != null && blueprint.IsProperty)) return false;

                PrototypeId propertyDataRef = blueprint.PropertyDataRef;
                PropertyEnum enumVal = GameDatabase.PropertyInfoTable.GetPropertyEnumFromPrototype(propertyDataRef);
                if (!Verify.IsTrue(enumVal != PropertyEnum.Invalid)) return false;
                PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(enumVal);

                PropertyId id = new(enumVal, info.DefaultParamValues);
                Verify.IsTrue(id != PropertyId.Invalid);

                @params.FieldInfo.SetValue(@params.OwnerPrototype, id);
            }

            return true;
        }

        /// <summary>
        /// Parses a collection of <see cref="bool"/> values and assigns it to a prototype field.
        /// </summary>
        private static bool ParseListBool(in FieldParserParams @params)
        {
            if (AllocateCollectionForField(@params, out short numItems, out bool[] values) == false)
                return false;

            CalligraphyReader reader = @params.Reader;

            for (int i = 0; i < numItems; i++)
            {
                if (!Verify.IsTrue(reader.Read(out long serializedValue))) return false;
                values[i] = serializedValue != 0;
            }

            @params.FieldInfo.SetValue(@params.OwnerPrototype, values);
            return true;
        }

        /// <summary>
        /// Parses a collection of <see cref="sbyte"/> values and assigns it to a prototype field.
        /// </summary>
        private static bool ParseListInt8(in FieldParserParams @params)
        {
            if (AllocateCollectionForField(@params, out short numItems, out sbyte[] values) == false)
                return false;

            CalligraphyReader reader = @params.Reader;

            for (int i = 0; i < numItems; i++)
            {
                if (!Verify.IsTrue(reader.Read(out long serializedValue))) return false;
                values[i] = (sbyte)serializedValue;
            }

            @params.FieldInfo.SetValue(@params.OwnerPrototype, values);
            return true;
        }

        /// <summary>
        /// Parses a collection of <see cref="short"/> values and assigns it to a prototype field.
        /// </summary>
        private static bool ParseListInt16(in FieldParserParams @params)
        {
            if (AllocateCollectionForField(@params, out short numItems, out short[] values) == false)
                return false;

            CalligraphyReader reader = @params.Reader;

            for (int i = 0; i < numItems; i++)
            {
                if (!Verify.IsTrue(reader.Read(out long serializedValue))) return false;
                values[i] = (short)serializedValue;
            }

            @params.FieldInfo.SetValue(@params.OwnerPrototype, values);
            return true;
        }

        /// <summary>
        /// Parses a collection of <see cref="int"/> values and assigns it to a prototype field.
        /// </summary>
        private static bool ParseListInt32(in FieldParserParams @params)
        {
            if (AllocateCollectionForField(@params, out short numItems, out int[] values) == false)
                return false;

            CalligraphyReader reader = @params.Reader;

            for (int i = 0; i < numItems; i++)
            {
                if (!Verify.IsTrue(reader.Read(out long serializedValue))) return false;
                values[i] = (int)serializedValue;
            }

            @params.FieldInfo.SetValue(@params.OwnerPrototype, values);
            return true;
        }

        /// <summary>
        /// Parses a collection of <see langword="unmanaged"/> 64-bit values and assigns it to a prototype field.
        /// </summary>
        /// <remarks>
        /// We use this for any 64-bit values that don't need to be cast after reading.
        /// </remarks>
        private static bool ParseListUnmanaged64<T>(in FieldParserParams @params) where T : unmanaged
        {
            if (AllocateCollectionForField(@params, out _, out T[] values) == false)
                return false;

            CalligraphyReader reader = @params.Reader;

            if (!Verify.IsTrue(reader.Read(values))) return false; // read the whole thing in one fell swoop

            @params.FieldInfo.SetValue(@params.OwnerPrototype, values);
            return true;
        }

        /// <summary>
        /// Parses a collection of <see cref="float"/> values and assigns it to a prototype field.
        /// </summary>
        private static bool ParseListFloat32(in FieldParserParams @params)
        {
            if (AllocateCollectionForField(@params, out short numItems, out float[] values) == false)
                return false;

            CalligraphyReader reader = @params.Reader;

            for (int i = 0; i < numItems; i++)
            {
                if (!Verify.IsTrue(reader.Read(out double serializedValue))) return false;
                values[i] = (float)serializedValue;
            }

            @params.FieldInfo.SetValue(@params.OwnerPrototype, values);
            return true;
        }

        /// <summary>
        /// Parses a collection of asset enum values and assigns it to a prototype field.
        /// </summary>
        private static bool ParseListEnum(in FieldParserParams @params)
        {
            if (AllocateCollectionForField(@params, out short numItems, out Array values) == false)
                return false;

            CalligraphyReader reader = @params.Reader;

            SymbolicLookup<int> symbolicEnum = @params.FieldInfo.SymbolicEnum;
            if (!Verify.IsNotNull(symbolicEnum)) return false;

            for (int i = 0; i < numItems; i++)
            {
                if (!Verify.IsTrue(reader.Read(out AssetId assetId))) return false;

                int value = 0;

                if (assetId != AssetId.Invalid)
                {
                    string assetName = GameDatabase.GetAssetName(assetId);
                    value = symbolicEnum.ToLookupValue(assetName, out bool found);

                    if (found == false)
                        Logger.Trace($"ParseListEnum(): Missing enum member {assetName} in {@params.FieldInfo.ClassType.Name}, field {@params.FieldInfo.Name}, file name {@params.FileName}");
                }

                values.SetValueUnsafe(value, i);
            }

            @params.FieldInfo.SetValue(@params.OwnerPrototype, values);
            return true;
        }

        /// <summary>
        /// Parses a collection of embedded prototypes and assigns it to a prototype field.
        /// </summary>
        private static bool ParseListPrototypePtr(in FieldParserParams @params)
        {
            if (AllocateCollectionForField(@params, out short numItems, out Array values) == false)
                return false;

            for (int i = 0; i < numItems; i++)
            {
                if (DeserializePrototypePtr(@params, true, out Prototype prototype) == false)
                    return false;

                values.SetValue(prototype, i);
            }

            @params.FieldInfo.SetValue(@params.OwnerPrototype, values);
            return true;
        }

        /// <summary>
        /// Parses a collection of <see cref="PrototypeId"/>, retrieves the associated <see cref="Prototype"/> instances, and assigns them to a field.
        /// </summary>
        private static bool ParseVectorPrototypeRefPtr(in FieldParserParams @params)
        {
            if (AllocateCollectionForField(@params, out short numItems, out Array values) == false)
                return false;

            CalligraphyReader reader = @params.Reader;

            for (int i = 0; i < numItems; i++)
            {
                if (!Verify.IsTrue(reader.Read(out PrototypeId protoRef))) return false;
                Prototype prototype = GameDatabase.GetPrototype<Prototype>(protoRef);
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
            PrototypePropertyCollection propertyCollection = GetPropertyCollectionField(@params.OwnerPrototype, @params.FieldInfo);
            if (!Verify.IsNotNull(propertyCollection)) return false;

            CalligraphyReader reader = @params.Reader;

            if (!Verify.IsTrue(reader.Read(out short numItems))) return false;

            if (numItems == 0)
                return true;

            for (int i = 0; i < numItems; i++)
            {
                if (!Verify.IsTrue(reader.ReadPrototypeHeader(out PrototypeDataHeader header, @params.FileName))) return false;

                if (header.InstanceDataExists == false)
                    continue;

                if (!Verify.IsTrue(reader.Read(out short numFieldGroups), $"Unable to read number of field groups for {@params.OwnerPrototype}"))
                    return false;

                for (int j = 0; j < numFieldGroups; j++)
                {
                    if (!Verify.IsTrue(reader.Read(out BlueprintId groupBlueprintDataRef), $"Error reading {j} group's declaring blueprint id from {@params.OwnerPrototype}"))
                        return false;

                    if (!Verify.IsTrue(groupBlueprintDataRef != BlueprintId.Invalid)) return false;

                    if (!Verify.IsTrue(reader.Read(out byte fieldGroupCopyNum), $"Error reading {j} group's blueprint copy number from {@params.OwnerPrototype}"))
                        return false;

                    Blueprint groupBlueprint = GameDatabase.GetBlueprint(groupBlueprintDataRef);
                    if (!Verify.IsNotNull(groupBlueprint, $"Failed to get {j} group's declaring blueprint from id for {@params.OwnerPrototype}"))
                        return false;

                    if (Verify.IsTrue(groupBlueprint.IsProperty))
                        DeserializeFieldGroupIntoProperty(propertyCollection, groupBlueprint, fieldGroupCopyNum, @params.FileName, reader, "PropertyList");

                    // Should be no list fields
                    if (!Verify.IsTrue(reader.Read(out short numFields))) return false;
                    if (!Verify.IsTrue(numFields == 0)) return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AllocateCollectionForField<T>(in FieldParserParams @params, out short numItems, out T[] collection)
        {
            if (Verify.IsTrue(@params.Reader.Read(out numItems)))
            {
                collection = numItems > 0 ? new T[numItems] : Array.Empty<T>();
                return true;
            }
            else
            {
                collection = default;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AllocateCollectionForField(in FieldParserParams @params, out short numItems, out Array collection)
        {
            if (Verify.IsTrue(@params.Reader.Read(out numItems)))
            {
                collection = @params.FieldInfo.AllocateCollection(numItems);
                return true;
            }
            else
            {
                collection = default;
                return false;
            }
        }

        /// <summary>
        /// Contains parameters for field parsing methods.
        /// </summary>
        private readonly struct FieldParserParams(CalligraphyReader reader, PrototypeFieldInfo fieldInfo, Prototype ownerPrototype,
            Blueprint ownerBlueprint, string fileName, BlueprintMemberInfo blueprintMemberInfo)
        {
            public readonly CalligraphyReader Reader = reader;
            public readonly PrototypeFieldInfo FieldInfo = fieldInfo;
            public readonly Prototype OwnerPrototype = ownerPrototype;
            public readonly Blueprint OwnerBlueprint = ownerBlueprint;
            public readonly string FileName = fileName;
            public readonly BlueprintMemberInfo BlueprintMemberInfo = blueprintMemberInfo;
        }

        #endregion
    }
}
