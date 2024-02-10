using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.GameData.Calligraphy
{
    public class PropertyBuilder
    {
        private PropertyEnum _propertyEnum;
        private PropertyInfoTable _propertyInfoTable;
        private bool _isInitializing;

        public PropertyBuilder(PropertyEnum propertyEnum, PropertyInfoTable propertyInfoTable, bool isInitializing)
        {
            _propertyEnum = propertyEnum;
            _propertyInfoTable = propertyInfoTable;
            _isInitializing = isInitializing;
        }

        public void SetPropertyInfo()
        {
            if (_isInitializing == false) return;
            PropertyInfo info = _propertyInfoTable.LookupPropertyInfo(_propertyEnum);
            // do property info initialization here
        }
    }
}
