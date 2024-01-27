using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;

namespace MHServerEmu.Games.Properties
{
    public class PropertyCollection
    {
        // TODO: Remove direct access to PropertyList once we no longer need it and interact with it through methods
        public List<Property> PropertyList { get; } = new();

        public PropertyCollection(CodedInputStream stream)
        {
            uint propertyCount = stream.ReadRawUInt32();
            for (int i = 0; i < propertyCount; i++)
                PropertyList.Add(new(stream));
        }

        public PropertyCollection(List<Property> propertyList = null)
        {
            if (propertyList != null)
                PropertyList.AddRange(propertyList);
        }

        public Property GetPropertyByEnum(PropertyEnum propertyEnum)
        {
            return PropertyList.Find(property => property.Id.Enum == propertyEnum);
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawUInt32((uint)PropertyList.Count);
            foreach (Property property in PropertyList)
                property.Encode(stream);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            for (int i = 0; i < PropertyList.Count; i++)
                sb.AppendLine($"Property{i}: {PropertyList[i]}");
            return sb.ToString();
        }
    }
}
