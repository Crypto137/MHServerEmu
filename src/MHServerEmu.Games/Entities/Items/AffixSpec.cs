using Gazillion;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Entities.Items
{
    public class AffixSpec : ISerialize
    {
        private PrototypeId _affixProtoRef;     // Replace with object ref?
        private PrototypeId _scopeProtoRef;
        private int _seed;

        public PrototypeId AffixProtoRef { get => _affixProtoRef; set => _affixProtoRef = value; }
        public PrototypeId ScopeProtoRef { get => _scopeProtoRef; set => _scopeProtoRef = value; }
        public int Seed { get => _seed; set => _seed = value; }

        public AffixSpec() { }

        public AffixSpec(PrototypeId affixProtoRef, PrototypeId scopeProtoRef, int seed)
        {
            _affixProtoRef = affixProtoRef;
            _scopeProtoRef = scopeProtoRef;
            _seed = seed;
        }

        public AffixSpec(AffixSpec other)
        {
            _affixProtoRef = other._affixProtoRef;
            _scopeProtoRef = other._scopeProtoRef;
            _seed = other._seed;
        }

        public AffixSpec(NetStructAffixSpec protobuf)
        {
            _affixProtoRef = (PrototypeId)protobuf.AffixProtoRef;
            _scopeProtoRef = (PrototypeId)protobuf.ScopeProtoRef;
            _seed = protobuf.Seed;
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;
            success &= Serializer.Transfer(archive, ref _affixProtoRef);
            success &= Serializer.Transfer(archive, ref _scopeProtoRef);
            success &= Serializer.Transfer(archive, ref _seed);
            return success;
        }

        public NetStructAffixSpec ToProtobuf()
        {
            return NetStructAffixSpec.CreateBuilder()
                .SetAffixProtoRef((ulong)_affixProtoRef)
                .SetScopeProtoRef((ulong)_scopeProtoRef)
                .SetSeed(_seed)
                .Build();
        }

        public override string ToString()
        {
            return string.Format("{0}={1}, {2}={3}, {4}=0x{5}",
                nameof(_affixProtoRef), GameDatabase.GetPrototypeName(_affixProtoRef),
                nameof(_scopeProtoRef), GameDatabase.GetPrototypeName(_scopeProtoRef),
                nameof(_seed), _seed.ToString("X"));
        }
    }
}
