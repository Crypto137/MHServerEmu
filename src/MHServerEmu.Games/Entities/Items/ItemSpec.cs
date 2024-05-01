using System.Text;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

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

        public void Decode(CodedInputStream stream)
        {
            _itemProtoRef = stream.ReadPrototypeRef<Prototype>();
            _rarityProtoRef = stream.ReadPrototypeRef<Prototype>();
            _itemLevel = stream.ReadRawInt32();
            _creditsAmount = stream.ReadRawInt32();

            uint numAffixSpecs = stream.ReadRawVarint32();
            for (uint i = 0; i < numAffixSpecs; i++)
            {
                AffixSpec affixSpec = new();
                affixSpec.Decode(stream);
                _affixSpecList.Add(affixSpec);
            }

            _seed = stream.ReadRawInt32();
            _equippableBy = stream.ReadPrototypeRef<Prototype>();
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WritePrototypeRef<Prototype>(_itemProtoRef);
            stream.WritePrototypeRef<Prototype>(_rarityProtoRef);
            stream.WriteRawInt32(_itemLevel);
            stream.WriteRawInt32(_creditsAmount);

            stream.WriteRawVarint32((uint)_affixSpecList.Count);
            foreach (AffixSpec affixSpec in _affixSpecList)
                affixSpec.Encode(stream);

            stream.WriteRawInt32(_seed);
            stream.WritePrototypeRef<Prototype>(_equippableBy);
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
