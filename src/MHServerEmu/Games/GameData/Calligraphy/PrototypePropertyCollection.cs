using MHServerEmu.Common;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.GameData.Calligraphy
{
    public class PrototypePropertyCollection : PropertyCollection
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<ulong, PropertyId> _mixinPropertyLookup;    // See SetKeyToPropertyId() for details on this

        public PrototypePropertyCollection()
        {
            _mixinPropertyLookup = new();
        }

        public PrototypePropertyCollection(Dictionary<ulong, PropertyId> mixinPropertyLookup)
        {
            _mixinPropertyLookup = new(mixinPropertyLookup);    // Copy mixin lookups
        }

        public PrototypePropertyCollection ShallowCopy()
        {
            PrototypePropertyCollection newCollection = new(_mixinPropertyLookup);
            newCollection.FlattenCopyFrom(this, true);
            return newCollection;
        }

        // The ugliness going on here is an attempt to imitate passing struct pointers C++ style.
        // TODO: Clean this mess up.

        public void SetPropertyFromMixin(PropertyValue value, PropertyId propertyId, byte blueprintCopyNum, byte paramsSetMask)
        {
            PropertyValue? existingValueRef = null;
            CurveProperty? curveProperty = null;
            PropertyId? curveIndex = null;

            SetKeyToPropertyId(ref propertyId, blueprintCopyNum, paramsSetMask, ref existingValueRef, ref curveProperty, ref curveIndex);
            SetPropertyValue(propertyId, value);
        }

        public void ReplacePropertyIdFromMixin(PropertyId newPropertyId, byte blueprintCopyNum, byte paramsSetMask)
        {
            PropertyValue? existingValueRef = new();
            CurveProperty? curveProperty = null;
            PropertyId? curveIndex = null;

            if (SetKeyToPropertyId(ref newPropertyId, blueprintCopyNum, paramsSetMask, ref existingValueRef, ref curveProperty, ref curveIndex))
                SetPropertyValue(newPropertyId, (PropertyValue)existingValueRef);     // Set property value only if there is something to replace
        }

        public void SetCurvePropertyFromMixin(PropertyId propertyId, CurveId curveId, PropertyId indexProperty, PropertyInfo info, byte blueprintCopyNum)
        {
            PropertyValue? existingValueRef = null;
            CurveProperty? nullableOldCurve = new();
            PropertyId? nullableIndexProperty = indexProperty;

            bool replaced = SetKeyToPropertyId(ref propertyId, blueprintCopyNum, 0xff, ref existingValueRef, ref nullableOldCurve, ref nullableIndexProperty);

            var oldCurve = (CurveProperty)nullableOldCurve;
            indexProperty = (PropertyId)nullableIndexProperty;

            if (indexProperty == PropertyId.Invalid)
            {
                // If the prototype contained an invalid curve index, fall back to the old curve or default curve index from the property info
                if (replaced)
                {
                    if (oldCurve.IndexPropertyId == PropertyId.Invalid)
                    {
                        Logger.Warn("Prototype property read error: trying to replace a curve property that has an invalid curve index");
                        return;
                    }

                    indexProperty = oldCurve.IndexPropertyId;
                }
                else
                {
                    indexProperty = info.DefaultCurveIndex;
                }
            }

            SetCurveProperty(propertyId, curveId, indexProperty, info, UInt32Flags.None, true);
        }

        public void ReplaceCurvePropertyIdFromMixin(PropertyId propertyId, PropertyId indexProperty, PropertyInfo info, byte blueprintCopyNum, byte paramsSetMask)
        {
            PropertyValue? existingValueRef = null;
            CurveProperty? nullableOldCurve = new();
            PropertyId? nullableIndexProperty = indexProperty;

            if (SetKeyToPropertyId(ref propertyId, blueprintCopyNum, paramsSetMask, ref existingValueRef, ref nullableOldCurve, ref nullableIndexProperty))
                SetCurveProperty(propertyId, ((CurveProperty)nullableOldCurve).CurveId, (PropertyId)nullableIndexProperty, info, UInt32Flags.None, true);
        }

        public void ReplaceCurvePropertyIdFromMixin(PropertyId propertyId, PropertyInfo info, byte blueprintCopyNum, byte paramsSetMask)
        {
            PropertyValue? existingValueRef = null;
            CurveProperty? nullableOldCurve = new();
            PropertyId? nullableIndexProperty = null;

            if (SetKeyToPropertyId(ref propertyId, blueprintCopyNum, paramsSetMask, ref existingValueRef, ref nullableOldCurve, ref nullableIndexProperty))
            {
                CurveProperty oldCurve = (CurveProperty)nullableOldCurve;
                SetCurveProperty(propertyId, oldCurve.CurveId, oldCurve.IndexPropertyId, info, UInt32Flags.None, true);
            }
        }

        private bool SetKeyToPropertyId(ref PropertyId propertyIdRef, byte blueprintCopyNum, byte paramsSetMask,
            ref PropertyValue? existingValueRef, ref CurveProperty? curveProp, ref PropertyId? curveIndex)
        {
            // TODO: Make an overload of this method for handling curve properties
            bool valueIsReplaced = false;

            // Child prototypes may override params of properties of their parents, so PrototypePropertyCollection has to
            // keep track of how contained property ids correspond to blueprints. For that purpose it uses a dictionary with
            // composite values made from blueprint copy number and property enum as keys.
            ulong key = ((ulong)blueprintCopyNum << 32) | (ulong)propertyIdRef.Enum;

            // If the lookup dict already has this key it means we are modifying an existing property rather than adding a new one
            if (_mixinPropertyLookup.TryGetValue(key, out PropertyId existingPropertyId))
            {
                if (HasMatchingParams(propertyIdRef, existingPropertyId, 0xff) == false || curveIndex != null)
                {
                    valueIsReplaced = true;

                    // We need to cast null to one of the supported value types for this check because of all the implicit casting we are doing
                    if (existingValueRef != (bool?)null)
                        existingValueRef = GetPropertyValue(existingPropertyId);

                    if (curveProp != null)
                    {
                        CurveProperty? nullableExistingCurveProp = GetCurveProperty(existingPropertyId);
                        if (nullableExistingCurveProp != null)
                        {
                            var existingCurveProp = (CurveProperty)nullableExistingCurveProp;

                            if (curveIndex != null && propertyIdRef == existingPropertyId && existingCurveProp.IndexPropertyId == curveIndex)
                                return false;

                            curveProp = existingCurveProp;
                        }
                        else
                        {
                            valueIsReplaced = false;
                        }
                    }

                    SetOverridenParams(ref propertyIdRef, existingPropertyId, paramsSetMask);
                    RemoveProperty(existingPropertyId);
                }
            }

            _mixinPropertyLookup[key] = propertyIdRef;
            return valueIsReplaced;
        }

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
