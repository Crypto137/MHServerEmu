using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Entities.Items
{
    public class Item : WorldEntity
    {
        private ItemSpec _itemSpec = new();     // ItemSpec needs to be initialized before the base constructor is called for packet parsing
                                                // TODO: Fix this

        public ItemSpec ItemSpec { get => _itemSpec; }

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
            
            _trackingContextMap = new();
            _conditionCollection = new(this);    
            _powerCollection = new(this);
            _unkEvent = 0;
            _itemSpec = itemSpec;
        }

        public override bool Serialize(Archive archive)
        {
            bool success = base.Serialize(archive);
            success &= Serializer.Transfer(archive, ref _itemSpec);
            return success;
        }

        protected override void Decode(CodedInputStream stream)
        {
            base.Decode(stream);
            _itemSpec.Decode(stream);
        }

        public override void Encode(CodedOutputStream stream)
        {
            base.Encode(stream);
            _itemSpec.Encode(stream);
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);
            sb.AppendLine($"{nameof(_itemSpec)}: {_itemSpec}");
        }
    }
}
