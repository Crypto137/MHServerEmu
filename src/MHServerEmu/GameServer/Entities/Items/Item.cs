using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.GameServer.Common;
using MHServerEmu.GameServer.Powers;

namespace MHServerEmu.GameServer.Entities.Items
{
    public class Item : WorldEntity
    {
        public ItemSpec ItemSpec { get; set; }

        public Item(EntityBaseData baseData, byte[] archiveData) : base(baseData)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(archiveData);
            DecodeEntityFields(stream);
            DecodeWorldEntityFields(stream);
            ItemSpec = new(stream);
        }

        public Item(EntityBaseData baseData, EntityTrackingContextMap[] trackingContextMap, Condition[] conditionCollection,
            PowerCollectionRecord[] powerCollection, int unkEvent, ItemSpec itemSpec) : base(baseData)
        {
            TrackingContextMap = trackingContextMap;
            ConditionCollection = conditionCollection;
            PowerCollection = powerCollection;
            UnkEvent = unkEvent;
            ItemSpec = itemSpec;
        }

        public override byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                // Encode
                EncodeEntityFields(cos);
                EncodeWorldEntityFields(cos);

                cos.WriteRawBytes(ItemSpec.Encode());

                cos.Flush();
                return ms.ToArray();
            }
        }
        public override string ToString()
        {
            StringBuilder sb = new();
            WriteEntityString(sb);
            WriteWorldEntityString(sb);
            sb.AppendLine($"ItemSpec: {ItemSpec}");
            return sb.ToString();
        }
    }
}
