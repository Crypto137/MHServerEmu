using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;

namespace MHServerEmu.Games.Properties
{
    public class PropertyCollection
    {
        // TODO: Remove direct access to PropertyList once we no longer need it and interact with it through methods
        public List<Property> List { get; } = new();

        public PropertyCollection() { }

        public PropertyCollection(List<Property> propertyList = null)
        {
            if (propertyList != null)
                List.AddRange(propertyList);
        }

        public Property GetPropertyByEnum(PropertyEnum propertyEnum)
        {
            return List.Find(property => property.Id.Enum == propertyEnum);
        }

        public virtual void Encode(CodedOutputStream stream)
        {
            stream.WriteRawUInt32((uint)List.Count);
            foreach (Property property in List)
                property.Encode(stream);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            for (int i = 0; i < List.Count; i++)
                sb.AppendLine($"Property{i}: {List[i]}");
            return sb.ToString();
        }
    }
}
