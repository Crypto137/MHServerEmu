using MHServerEmu.Common;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.GameData.Calligraphy
{
    /// <summary>
    /// A <see cref="PropertyCollection"/> that stores properties deserialized from Calligraphy prototype mixin field groups.
    /// </summary>
    public class PrototypePropertyCollection : PropertyCollection
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        // Child prototypes may override params of properties of their parents, so PrototypePropertyCollection has to
        // keep track of how contained property ids correspond to blueprints. For that purpose it uses a dictionary with
        // composite values made from blueprint copy number and property enum as keys.
        private readonly Dictionary<ulong, PropertyId> _mixinPropertyLookup;

        /// <summary>
        /// Constructs a new blank <see cref="PrototypePropertyCollection"/> instance.
        /// </summary>
        public PrototypePropertyCollection()
        {
            _mixinPropertyLookup = new();
        }

        /// <summary>
        /// Constructs a new <see cref="PrototypePropertyCollection"/> instance and copies mixin property lookups from another collection.
        /// </summary>
        public PrototypePropertyCollection(Dictionary<ulong, PropertyId> mixinPropertyLookup)
        {
            _mixinPropertyLookup = new(mixinPropertyLookup);    // Copy mixin lookups
        }

        /// <summary>
        /// Clones this <see cref="PrototypePropertyCollection"/>.
        /// </summary>
        public PrototypePropertyCollection ShallowCopy()
        {
            PrototypePropertyCollection newCollection = new(_mixinPropertyLookup);
            newCollection.FlattenCopyFrom(this, true);
            return newCollection;
        }

        /// <summary>
        /// Sets a <see cref="PropertyValue"/> for a <see cref="PropertyId"/> from a mixin field group.
        /// </summary>
        public void SetPropertyFromMixin(PropertyValue value, PropertyId propertyId, byte blueprintCopyNum, byte paramsSetMask)
        {
            PropertyValue? existingValueRef = null;

            SetKeyToPropertyId(ref propertyId, blueprintCopyNum, paramsSetMask, ref existingValueRef);
            SetPropertyValue(propertyId, value);
        }

        /// <summary>
        /// Updates the <see cref="PropertyId"/> of an existing <see cref="PropertyValue"/> set from a mixin field group.
        /// </summary>
        public void ReplacePropertyIdFromMixin(PropertyId newPropertyId, byte blueprintCopyNum, byte paramsSetMask)
        {
            PropertyValue? existingValueRef = new();

            // If the id got updated we need to reassign the existing value to the new id
            if (SetKeyToPropertyId(ref newPropertyId, blueprintCopyNum, paramsSetMask, ref existingValueRef))
                SetPropertyValue(newPropertyId, (PropertyValue)existingValueRef);
        }

        /// <summary>
        /// Sets a curve property for a <see cref="PropertyId"/> from a mixin field group.
        /// </summary>
        public void SetCurvePropertyFromMixin(PropertyId propertyId, CurveId curveId, PropertyId indexProperty, PropertyInfo info, byte blueprintCopyNum)
        {
            CurveProperty oldCurve = new();

            bool replaced = SetKeyToPropertyId(ref propertyId, blueprintCopyNum, 0xff, ref oldCurve, (PropertyId?)indexProperty);

            // There must be a valid index property
            if (indexProperty == PropertyId.Invalid)
            {
                if (replaced)
                {
                    if (oldCurve.IndexPropertyId == PropertyId.Invalid)
                    {
                        // Nothing to fall back on
                        Logger.Error("Prototype property read error: trying to replace a curve property that has an invalid curve index");
                        return;
                    }

                    // If we are replacing a valid existing curve property, get the index property from it
                    indexProperty = oldCurve.IndexPropertyId;
                }
                else
                {
                    // If we are adding a new property fall back to the default curve defined in property info
                    indexProperty = info.DefaultCurveIndex;
                }
            }

            SetCurveProperty(propertyId, curveId, indexProperty, info, UInt32Flags.None, true);
        }

        /// <summary>
        /// Updates the <see cref="PropertyId"/> and the curve index property for an existing curve property added from a mixin field group.
        /// </summary>
        public void ReplaceCurvePropertyIdFromMixin(PropertyId propertyId, PropertyId indexProperty, PropertyInfo info, byte blueprintCopyNum, byte paramsSetMask)
        {
            CurveProperty oldCurve = new();

            if (SetKeyToPropertyId(ref propertyId, blueprintCopyNum, paramsSetMask, ref oldCurve, (PropertyId?)indexProperty))
                SetCurveProperty(propertyId, oldCurve.CurveId, indexProperty, info, UInt32Flags.None, true);  
        }

        /// <summary>
        /// Updates the <see cref="PropertyId"/> for an existing curve property added from a mixin field group.
        /// </summary>
        public void ReplaceCurvePropertyIdFromMixin(PropertyId propertyId, PropertyInfo info, byte blueprintCopyNum, byte paramsSetMask)
        {
            CurveProperty oldCurve = new();

            if (SetKeyToPropertyId(ref propertyId, blueprintCopyNum, paramsSetMask, ref oldCurve, null))
                SetCurveProperty(propertyId, oldCurve.CurveId, oldCurve.IndexPropertyId, info, UInt32Flags.None, true);
        }

        /// <summary>
        /// Updates the <see cref="PropertyId"/> corresponding to a <see cref="PropertyEnum"/> / blueprint copy number pair. For use with non-curve properties.
        /// </summary>
        private bool SetKeyToPropertyId(ref PropertyId propertyIdRef, byte blueprintCopyNum, byte paramsSetMask, ref PropertyValue? existingValueRef)
        {
            bool valueIsReplaced = false;
            ulong key = ((ulong)blueprintCopyNum << 32) | (ulong)propertyIdRef.Enum;

            // If the lookup dict already has this key it means we are modifying an existing property rather than adding a new one
            if (_mixinPropertyLookup.TryGetValue(key, out PropertyId existingPropertyId))
            {
                if (HasMatchingParams(propertyIdRef, existingPropertyId, 0xff) == false)
                {
                    valueIsReplaced = true;

                    // If existingValueRef is not a null ref it means we are replacing an existing property id,
                    // and we need to output existing PropertyValue to this ref so it can be reassigned to the new id.
                    //
                    // For null comparison we need to cast null to one of the supported PropertyValue types
                    // for this check because of all the implicit casting we are doing.
                    if (existingValueRef != (bool?)null)
                        existingValueRef = GetPropertyValue(existingPropertyId);

                    SetOverridenParams(ref propertyIdRef, existingPropertyId, paramsSetMask);
                    RemoveProperty(existingPropertyId);
                }
            }

            _mixinPropertyLookup[key] = propertyIdRef;
            return valueIsReplaced;
        }

        /// <summary>
        /// Updates the <see cref="PropertyId"/> corresponding to a <see cref="PropertyEnum"/> / blueprint copy number pair. For use with curve properties.
        /// </summary>
        private bool SetKeyToPropertyId(ref PropertyId propertyIdRef, byte blueprintCopyNum, byte paramsSetMask, ref CurveProperty oldCurve, PropertyId? curveIndex)
        {
            bool valueIsReplaced = false;
            ulong key = ((ulong)blueprintCopyNum << 32) | (ulong)propertyIdRef.Enum;

            // If the lookup dict already has this key it means we are modifying an existing property rather than adding a new one
            if (_mixinPropertyLookup.TryGetValue(key, out PropertyId existingPropertyId))
            {
                if (HasMatchingParams(propertyIdRef, existingPropertyId, 0xff) == false || curveIndex != null)
                {
                    // If there is an existing CurveProperty assigned to this id and we are actually making changes,
                    // we need to output this CurveProperty to be reassigned to the new id.
                    CurveProperty? nullableExistingCurveProp = GetCurveProperty(existingPropertyId);
                    if (nullableExistingCurveProp != null)
                    {
                        var existingCurveProp = (CurveProperty)nullableExistingCurveProp;

                        if (curveIndex != null && propertyIdRef == existingPropertyId && existingCurveProp.IndexPropertyId == curveIndex)
                            return false;   // No need to change anything

                        oldCurve = existingCurveProp;
                        valueIsReplaced = true;
                    }

                    SetOverridenParams(ref propertyIdRef, existingPropertyId, paramsSetMask);
                    RemoveProperty(existingPropertyId);
                }
            }

            _mixinPropertyLookup[key] = propertyIdRef;
            return valueIsReplaced;
        }

        /// <summary>
        /// Compares parameters of two <see cref="PropertyId"/> values. The provided mask defines params that need to be compared.
        /// </summary>
        private bool HasMatchingParams(PropertyId left, PropertyId right, byte paramsSetMask)
        {
            if (paramsSetMask == 0xff) return left == right;

            var leftParams = left.GetParams();
            var rightParams = right.GetParams();

            for (int i = 0; i < Property.MaxParamCount; i++)
            {
                if ((paramsSetMask & (1 << i)) != 0)
                {
                    if (leftParams[i] != rightParams[i])
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Overrides <see cref="PropertyParam"/> values of a <see cref="PropertyId"/>. The provided mask defines params that need to be overriden.
        /// </summary>
        private void SetOverridenParams(ref PropertyId destId, PropertyId sourceId, byte paramsSetMask)
        {
            if (paramsSetMask == 0xff) return;

            PropertyParam[] destParams = destId.GetParams();
            PropertyParam[] sourceParams = sourceId.GetParams();

            for (int i = 0; i < Property.MaxParamCount; i++)
            {
                if ((paramsSetMask & (1 << i)) == 0)
                    destParams[i] = sourceParams[i];
            }

            destId = new(destId.Enum, destParams);
        }
    }
}
