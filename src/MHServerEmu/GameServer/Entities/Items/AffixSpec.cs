using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.GameData;
using System.Text;

namespace MHServerEmu.GameServer.Entities.Items
{
    public class AffixSpec
    {
        public ulong AffixProto { get; set; }
        public ulong ScopeProto { get; set; }
        public int Seed { get; set; }

        public AffixSpec(CodedInputStream stream)
        {            
            AffixProto = stream.ReadPrototypeId(PrototypeEnumType.All);
            ScopeProto = stream.ReadPrototypeId(PrototypeEnumType.All);
            Seed = stream.ReadRawInt32();
        }

        public AffixSpec(ulong affixProto, ulong scopeProto, int seed)
        {
            AffixProto = affixProto;
            ScopeProto = scopeProto;
            Seed = seed;
        }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WritePrototypeId(AffixProto, PrototypeEnumType.All);
                cos.WritePrototypeId(ScopeProto, PrototypeEnumType.All);
                cos.WriteRawInt32(Seed);

                cos.Flush();
                return ms.ToArray();
            }
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
