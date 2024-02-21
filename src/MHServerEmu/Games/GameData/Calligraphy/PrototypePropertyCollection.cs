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

        public void SetPropertyFromMixin(PropertyValue value, PropertyId propertyId, byte blueprintCopyNum, byte paramsSetMask)
        {
            PropertyValue? existingValueRef = null;

            SetKeyToPropertyId(ref propertyId, blueprintCopyNum, paramsSetMask, ref existingValueRef);
            SetPropertyValue(propertyId, value);
        }

        public void ReplacePropertyIdFromMixin(PropertyId newPropertyId, byte blueprintCopyNum, byte paramsSetMask)
        {
            PropertyValue? existingValueRef = new();
            
            if (SetKeyToPropertyId(ref newPropertyId, blueprintCopyNum, paramsSetMask, ref existingValueRef))
                SetPropertyValue(newPropertyId, (PropertyValue)existingValueRef);     // Set property value only if there is something to replace
        }

        public void SetCurvePropertyFromMixin()
        {
            // TODO
        }

        public void ReplaceCurvePropertyIdFromMixin()
        {
            // TODO
        }

        private bool SetKeyToPropertyId(ref PropertyId propertyIdRef, byte blueprintCopyNum, byte paramsSetMask,
            ref PropertyValue? existingValueRef, CurveProperty? curveProperty = null, PropertyId? newCurveIndexProperty = null)
        {
            bool valueIsReplaced = false;

            // Child prototypes may override params of properties of their parents, so PrototypePropertyCollection has to
            // keep track of how contained property ids correspond to blueprints. For that purpose it uses a dictionary with
            // composite values made from blueprint copy number and property enum as keys.
            ulong key = ((ulong)blueprintCopyNum << 32) | (ulong)propertyIdRef.Enum;

            // If the lookup dict already has this key it means we are modifying an existing property rather than adding a new one
            if (_mixinPropertyLookup.TryGetValue(key, out PropertyId existingPropertyId))
            {
                if (HasMatchingParams(propertyIdRef, existingPropertyId, 0xff) == false || newCurveIndexProperty != null)
                {
                    valueIsReplaced = true;

                    // We need to cast null to one of the supported value types for this check because of all the implicit casting we are doing
                    if (existingValueRef != (bool?)null)
                        existingValueRef = GetPropertyValue(existingPropertyId);

                    if (curveProperty != null)
                    {
                        // todo: curve properties
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
