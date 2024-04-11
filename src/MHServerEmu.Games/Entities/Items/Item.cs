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

        // new
        public Item(Game game) : base(game) { }

        // old
        public Item(EntityBaseData baseData, ByteString archiveData) : base(baseData, archiveData) { }

        public Item(EntityBaseData baseData, ulong replicationId, PrototypeId rank, int itemLevel, PrototypeId itemRarity, float itemVariation, ItemSpec itemSpec) : base(baseData)
        {
            Properties = new(replicationId);
            Properties[PropertyEnum.Requirement, (PrototypeId)4312898931213406054] = itemLevel * 1.0f;    // Property/Info/CharacterLevel.defaults
            Properties[PropertyEnum.ItemRarity] = itemRarity;
            Properties[PropertyEnum.ItemVariation] = itemVariation;
            
            TrackingContextMap = new();
            ConditionCollection = new();    
            PowerCollection = new();
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
