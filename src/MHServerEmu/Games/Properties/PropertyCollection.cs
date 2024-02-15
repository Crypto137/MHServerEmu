using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;

namespace MHServerEmu.Games.Properties
{
    public class PropertyCollection
    {
        // TODO: PropertyList data structure or something to replace it
        protected List<Property> _propertyList = new();

        public PropertyCollection() { }

        public PropertyCollection(List<Property> propertyList = null)
        {
            if (propertyList != null)
                _propertyList.AddRange(propertyList);
        }

        public Property GetProperty(PropertyId propertyId)
        {
            return _propertyList.Find(property => property.Id == propertyId);
        }

        public void SetProperty(object value, PropertyId propertyId)
        {
            Property prop = GetProperty(propertyId);
            if (prop == null) prop = new(propertyId);
            prop.Value.Set(value);
        }

        public bool HasProperty(PropertyEnum propertyEnum)
        {
            return _propertyList.Find(property => property.Id.Enum == propertyEnum) != null;
        }

        public bool HasProperty(PropertyId propertyId)
        {
            return GetProperty(propertyId) != null;
        }

        #region Value Accessors

        public object this[PropertyEnum propertyEnum]
        {
            get => GetProperty(new(propertyEnum)).Value.Get();
            set => SetProperty(value, new(propertyEnum));
        }

        public object this[PropertyEnum propertyEnum, int param0]
        {
            get => GetProperty(new(propertyEnum, param0)).Value.Get();
            set => SetProperty(value, new(propertyEnum, param0));
        }

        public object this[PropertyEnum propertyEnum, int param0, int param1]
        {
            get => GetProperty(new(propertyEnum, param0, param1)).Value.Get();
            set => SetProperty(value, new(propertyEnum, param0, param1));
        }

        public object this[PropertyEnum propertyEnum, int param0, int param1, int param2]
        {
            get => GetProperty(new(propertyEnum, param0, param1, param2)).Value.Get();
            set => SetProperty(value, new(propertyEnum, param0, param1, param2));
        }

        public object this[PropertyEnum propertyEnum, int param0, int param1, int param2, int param3]
        {
            get => GetProperty(new(propertyEnum, param0, param1, param2, param3)).Value.Get();
            set => SetProperty(value, new(propertyEnum, param0, param1, param2, param3));
        }

        #endregion

        public IEnumerable<Property> IterateProperties()
        {
            // temp method
            foreach (Property prop in _propertyList)
                yield return prop;
        }

        public virtual void Encode(CodedOutputStream stream)
        {
            stream.WriteRawUInt32((uint)_propertyList.Count);
            foreach (Property property in _propertyList)
                property.Encode(stream);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            for (int i = 0; i < _propertyList.Count; i++)
                sb.AppendLine($"Property{i}: {_propertyList[i]}");
            return sb.ToString();
        }
    }
}
