using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PropertyPrototype : Prototype
    {
    }

    public class PropertyEntryPrototype : Prototype
    {
    }

    public class PropertyPickInRangeEntryPrototype : PropertyEntryPrototype
    {
        public PropertyId Prop { get; protected set; }
        public EvalPrototype ValueMax { get; protected set; }
        public EvalPrototype ValueMin { get; protected set; }
        public bool RollAsInteger { get; protected set; }
        public LocaleStringId TooltipOverrideText { get; protected set; }
    }

    public class PropertySetEntryPrototype : PropertyEntryPrototype
    {
        public PropertyId Prop { get; protected set; }
        public LocaleStringId TooltipOverrideText { get; protected set; }
        public EvalPrototype Value { get; protected set; }

        //---

        public PropertyValue GetValue(Entity entity)
        {
            // NOTE: This was moved from Item::onBuiltInPropertySet() in 1.53 to reuse this code for equipment sets.
            // We keep this here for all versions because it's arguably cleaner and more consistent.
            PropertyInfo propertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(Prop.Enum);
            PropertyDataType propDataType = propertyInfo.DataType;

            if (!Verify.IsTrue(propDataType == PropertyDataType.Real || propDataType == PropertyDataType.Integer || propDataType == PropertyDataType.Asset,
                $"The following Item has a built-in set PropertyEntry with a property that is not an int/float/asset prop, which doesn't work!\nItem: [{entity}]\nProperty: [{propertyInfo.PropertyName}]"))
                return default;

            using var evalContextHandle = EvalContextDataPool.Get(out EvalContextData evalContext);
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, entity?.Properties);

            return propDataType switch
            {
                PropertyDataType.Real    => Value != null ? Eval.RunFloat(Value, evalContext) : 0f,
                PropertyDataType.Integer => Value != null ? Eval.RunInt(Value, evalContext) : 0,
                PropertyDataType.Asset   => Value != null ? Eval.RunAssetId(Value, evalContext) : AssetId.Invalid,
                _                        => default,
            };
        }
    }
}
