using Gazillion;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Entities.Items
{
    public class AffixSpec : ISerialize
    {
        private PrototypeId _scopeProtoRef;
        private int _seed;

        public AffixPrototype AffixProto { get; set; }
        public PrototypeId ScopeProtoRef { get => _scopeProtoRef; set => _scopeProtoRef = value; }
        public int Seed { get => _seed; set => _seed = value; }

        public bool IsValid { get => AffixProto != null && _seed != 0; }

        public AffixSpec() { }

        public AffixSpec(AffixPrototype affixProto, PrototypeId scopeProtoRef, int seed)
        {
            AffixProto = affixProto;
            _scopeProtoRef = scopeProtoRef;
            _seed = seed;
        }

        public AffixSpec(AffixSpec other)
        {
            AffixProto = other.AffixProto;
            _scopeProtoRef = other._scopeProtoRef;
            _seed = other._seed;
        }

        public AffixSpec(NetStructAffixSpec protobuf)
        {
            AffixProto = GameDatabase.GetPrototype<AffixPrototype>((PrototypeId)protobuf.AffixProtoRef);
            _scopeProtoRef = (PrototypeId)protobuf.ScopeProtoRef;
            _seed = protobuf.Seed;
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            PrototypeId affixProtoRef = AffixProto != null ? AffixProto.DataRef : PrototypeId.Invalid;
            success &= Serializer.Transfer(archive, ref affixProtoRef);

            success &= Serializer.Transfer(archive, ref _scopeProtoRef);
            success &= Serializer.Transfer(archive, ref _seed);
            return success;
        }

        public NetStructAffixSpec ToProtobuf()
        {
            return NetStructAffixSpec.CreateBuilder()
                .SetAffixProtoRef((ulong)AffixProto.DataRef)
                .SetScopeProtoRef((ulong)_scopeProtoRef)
                .SetSeed(_seed)
                .Build();
        }

        public override string ToString()
        {
            return $"AffixSpec [{AffixProto}] scope=[{_scopeProtoRef.GetName()}] seed=[{_seed}]";
        }
    }
}
