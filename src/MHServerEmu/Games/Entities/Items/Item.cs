using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Entities.Items
{
    public class Item : WorldEntity
    {
        public ItemSpec ItemSpec { get; set; }

        public Item(EntityBaseData baseData, ByteString archiveData) : base(baseData, archiveData) { }

        public Item(EntityBaseData baseData, ulong replicationId, ulong rank, int itemLevel, PrototypeId itemRarity, float itemVariation, ItemSpec itemSpec) : base(baseData)
        {
            Property requirement = new(PropertyEnum.Requirement, itemLevel * 1.0f);
            requirement.Id = 0x66A3940000000000;
            PropertyCollection = new(replicationId, new()
            {
                new(PropertyEnum.Rank, rank),
                new(PropertyEnum.ItemRarity, itemRarity),
                new(PropertyEnum.ItemVariation, itemVariation),
                requirement
            });
            TrackingContextMap = Array.Empty<EntityTrackingContextMap>();
            ConditionCollection = Array.Empty<Condition>();
            PowerCollection = Array.Empty<PowerCollectionRecord>();
            UnkEvent = 0;
            ItemSpec = itemSpec;
        }

        protected override void Decode(CodedInputStream stream)
        {
            base.Decode(stream);

            ItemSpec = new(stream);
        }

        public override void Encode(CodedOutputStream stream)
        {
            base.Encode(stream);

            ItemSpec.Encode(stream);
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            sb.AppendLine($"ItemSpec: {ItemSpec}");
        }
    }
}
