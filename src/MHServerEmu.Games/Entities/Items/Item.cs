using System.Text;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Entities.Items
{
    public class Item : WorldEntity
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private ItemSpec _itemSpec = new();

        public ItemSpec ItemSpec { get => _itemSpec; }
        public PrototypeId OnUsePower { get; internal set; }
        public bool IsBoundToAccount { get; internal set; }

        public Item(Game game) : base(game) { }

        public override bool Initialize(EntitySettings settings)
        {
            base.Initialize(settings);

            // Apply ItemSpec if one was provided with entity settings
            if (settings.ItemSpec != null)
                ApplyItemSpec(settings.ItemSpec);

            return true;
        }

        public override bool Serialize(Archive archive)
        {
            bool success = base.Serialize(archive);
            success &= Serializer.Transfer(archive, ref _itemSpec);
            return success;
        }

        public override bool IsAutoStackedWhenAddedToInventory()
        {
            var itemProto = Prototype as ItemPrototype;
            if (itemProto == null) return Logger.WarnReturn(false, "IsAutoStackedWhenAddedToInventory(): itemProto == null");
            if (itemProto.StackSettings == null) return false;
            return itemProto.StackSettings.AutoStackWhenAddedToInventory;
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);
            sb.AppendLine($"{nameof(_itemSpec)}: {_itemSpec}");
        }

        private void ApplyItemSpec(ItemSpec itemSpec)
        {
            _itemSpec = itemSpec;
            // TODO: everything else
        }

        internal bool CanUse(Agent agent, bool powerUse)
        {
            throw new NotImplementedException();
        }

        internal int GetVendorBaseXPGain(Player player)
        {
            throw new NotImplementedException();
        }

        internal uint GetSellPrice(Player player)
        {
            throw new NotImplementedException();
        }
    }
}
