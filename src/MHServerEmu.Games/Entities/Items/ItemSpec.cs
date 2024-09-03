using System.Text;
using Gazillion;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Entities.Items
{
    public class ItemSpec : ISerialize
    {
        private PrototypeId _itemProtoRef;
        private PrototypeId _rarityProtoRef;
        private int _itemLevel;
        private int _creditsAmount;
        private List<AffixSpec> _affixSpecList = new();
        private int _seed;
        private PrototypeId _equippableBy;
        private int _count = 1;

        public PrototypeId ItemProtoRef { get => _itemProtoRef; }
        public PrototypeId RarityProtoRef { get => _rarityProtoRef; }
        public int ItemLevel { get => _itemLevel; }
        public int CreditsAmount { get => _creditsAmount; }
        public IEnumerable<AffixSpec> AffixSpecs { get => _affixSpecList; }
        public int Seed { get => _seed; }
        public PrototypeId EquippableBy { get => _equippableBy; }

        public bool IsValid { get => _itemProtoRef != PrototypeId.Invalid && _rarityProtoRef != PrototypeId.Invalid; }

        public ItemSpec() { }

        public ItemSpec(PrototypeId itemProtoRef, PrototypeId rarityProtoRef, int itemLevel, int creditsAmount, IEnumerable<AffixSpec> affixSpecs, int seed, PrototypeId equippableBy)
        {
            _itemProtoRef = itemProtoRef;
            _rarityProtoRef = rarityProtoRef;
            _itemLevel = itemLevel;
            _creditsAmount = creditsAmount;
            _affixSpecList.AddRange(affixSpecs);
            _seed = seed;
            _equippableBy = equippableBy;
        }

        public ItemSpec(NetStructItemSpec protobuf)
        {
            _itemProtoRef = (PrototypeId)protobuf.ItemProtoRef;
            _rarityProtoRef = (PrototypeId)protobuf.RarityProtoRef;
            _itemLevel = (int)protobuf.ItemLevel;

            if (protobuf.HasCreditsAmount)
                _creditsAmount = (int)protobuf.CreditsAmount;

            _affixSpecList.AddRange(protobuf.AffixSpecsList.Select(affixSpecProtobuf => new AffixSpec(affixSpecProtobuf)));
            _seed = (int)protobuf.Seed;

            if (protobuf.HasEquippableBy)
                _equippableBy = (PrototypeId)protobuf.EquippableBy;
        }

        public NetStructItemSpecStack ToStackProtobuf()
        {
            return NetStructItemSpecStack.CreateBuilder()
                .SetSpec(ToProtobuf())
                .SetCount((uint)_count)
                .Build();
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;
            success &= Serializer.Transfer(archive, ref _itemProtoRef);
            success &= Serializer.Transfer(archive, ref _rarityProtoRef);
            success &= Serializer.Transfer(archive, ref _itemLevel);
            success &= Serializer.Transfer(archive, ref _creditsAmount);
            success &= Serializer.Transfer(archive, ref _affixSpecList);
            success &= Serializer.Transfer(archive, ref _seed);
            success &= Serializer.Transfer(archive, ref _equippableBy);
            return success;
        }

        public NetStructItemSpec ToProtobuf()
        {
            return NetStructItemSpec.CreateBuilder()
                .SetItemProtoRef((ulong)_itemProtoRef)
                .SetRarityProtoRef((ulong)_rarityProtoRef)
                .SetItemLevel((uint)_itemLevel)
                .SetCreditsAmount((uint)_creditsAmount)
                .AddRangeAffixSpecs(_affixSpecList.Select(affixSpec => affixSpec.ToProtobuf()))
                .SetSeed((uint)_seed)
                .SetEquippableBy((ulong)_equippableBy)
                .Build();
        }

        public void Set(ItemSpec other)
        {
            if (other == null) throw new ArgumentException("other == null");
            if (ReferenceEquals(this, other)) return;

            _itemProtoRef = other._itemProtoRef;
            _rarityProtoRef = other._rarityProtoRef;
            _itemLevel = other._itemLevel;
            _creditsAmount = other._creditsAmount;
            _seed = other._seed;
            _equippableBy = other._equippableBy;

            _affixSpecList.Clear();
            foreach (AffixSpec otherAffixSpec in other._affixSpecList)
                _affixSpecList.Add(new(otherAffixSpec));
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(_itemProtoRef)}: {GameDatabase.GetPrototypeName(_itemProtoRef)}");
            sb.AppendLine($"{nameof(_rarityProtoRef)}: {GameDatabase.GetPrototypeName(_rarityProtoRef)}");
            sb.AppendLine($"{nameof(_itemLevel)}: {_itemLevel}");
            sb.AppendLine($"{nameof(_creditsAmount)}: {_creditsAmount}");

            for (int i = 0; i < _affixSpecList.Count; i++)
                sb.AppendLine($"{nameof(_affixSpecList)}[{i}]: {_affixSpecList[i]}");
            
            sb.AppendLine($"{nameof(_seed)}: 0x{_seed:X}");
            sb.AppendLine($"{nameof(_equippableBy)}: {GameDatabase.GetPrototypeName(_equippableBy)}");
            return sb.ToString();
        }
    }
}
