﻿using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Entities.Items
{
    public class AffixSpec
    {
        public PrototypeId AffixProto { get; set; }
        public PrototypeId ScopeProto { get; set; }
        public int Seed { get; set; }

        public AffixSpec(CodedInputStream stream)
        {            
            AffixProto = stream.ReadPrototypeRef<Prototype>();
            ScopeProto = stream.ReadPrototypeRef<Prototype>();
            Seed = stream.ReadRawInt32();
        }

        public AffixSpec(PrototypeId affixProto, PrototypeId scopeProto, int seed)
        {
            AffixProto = affixProto;
            ScopeProto = scopeProto;
            Seed = seed;
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WritePrototypeRef<Prototype>(AffixProto);
            stream.WritePrototypeRef<Prototype>(ScopeProto);
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
