using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Entities.Items
{
    public class AffixSpec
    {
        public ulong AffixProto { get; set; }
        public ulong ScopeProto { get; set; }
        public int Seed { get; set; }

        public AffixSpec(CodedInputStream stream)
        {            
            AffixProto = stream.ReadPrototypeEnum(PrototypeEnumType.All);
            ScopeProto = stream.ReadPrototypeEnum(PrototypeEnumType.All);
            Seed = stream.ReadRawInt32();
        }

        public AffixSpec(ulong affixProto, ulong scopeProto, int seed)
        {
            AffixProto = affixProto;
            ScopeProto = scopeProto;
            Seed = seed;
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WritePrototypeEnum(AffixProto, PrototypeEnumType.All);
            stream.WritePrototypeEnum(ScopeProto, PrototypeEnumType.All);
            stream.WriteRawInt32(Seed);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"AffixProto: {GameDatabase.GetPrototypeName(AffixProto)}");
            sb.AppendLine($"ScopeProto: {GameDatabase.GetPrototypeName(ScopeProto)}");
            sb.AppendLine($"Seed: {Seed}");
            return sb.ToString();
        }
    }
}
