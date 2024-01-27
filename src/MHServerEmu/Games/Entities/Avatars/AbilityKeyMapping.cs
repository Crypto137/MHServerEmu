using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Entities.Avatars
{
    public class AbilityKeyMapping
    {
        public int PowerSpecIndex { get; set; }
        public bool ShouldPersist { get; set; }
        public PrototypeId AssociatedTransformMode { get; set; }
        public PrototypeId Slot0 { get; set; }
        public PrototypeId Slot1 { get; set; }
        public PrototypeId[] PowerSlots { get; set; }

        public AbilityKeyMapping(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            PowerSpecIndex = stream.ReadRawInt32();
            ShouldPersist = boolDecoder.ReadBool(stream);
            AssociatedTransformMode = stream.ReadPrototypeEnum<Prototype>();
            Slot0 = stream.ReadPrototypeEnum<Prototype>();
            Slot1 = stream.ReadPrototypeEnum<Prototype>();

            PowerSlots = new PrototypeId[stream.ReadRawVarint64()];
            for (int i = 0; i < PowerSlots.Length; i++)
                PowerSlots[i] = stream.ReadPrototypeEnum<Prototype>();
        }

        public AbilityKeyMapping(int powerSpecIndex, bool shouldPersist, PrototypeId associatedTransformMode, PrototypeId slot0, PrototypeId slot1, PrototypeId[] powerSlots)
        {
            PowerSpecIndex = powerSpecIndex;
            ShouldPersist = shouldPersist;
            AssociatedTransformMode = associatedTransformMode;
            Slot0 = slot0;
            Slot1 = slot1;
            PowerSlots = powerSlots;            
        }

        public void EncodeBools(BoolEncoder boolEncoder)
        {
            boolEncoder.EncodeBool(ShouldPersist);
        }

        public void Encode(CodedOutputStream stream, BoolEncoder boolEncoder)
        {
            stream.WriteRawInt32(PowerSpecIndex);
            boolEncoder.WriteBuffer(stream);   // ShouldPersist
            stream.WritePrototypeEnum<Prototype>(AssociatedTransformMode);
            stream.WritePrototypeEnum<Prototype>(Slot0);
            stream.WritePrototypeEnum<Prototype>(Slot1);

            stream.WriteRawVarint64((ulong)PowerSlots.Length);
            foreach (PrototypeId powerSlot in PowerSlots)
                stream.WritePrototypeEnum<Prototype>(powerSlot);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"PowerSpecIndex: 0x{PowerSpecIndex:X}");
            sb.AppendLine($"ShouldPersist: {ShouldPersist}");
            sb.AppendLine($"AssociatedTransformMode: {GameDatabase.GetPrototypeName(AssociatedTransformMode)}");
            sb.AppendLine($"Slot0: {GameDatabase.GetPrototypeName(Slot0)}");
            sb.AppendLine($"Slot1: {GameDatabase.GetPrototypeName(Slot1)}");
            for (int i = 0; i < PowerSlots.Length; i++) sb.AppendLine($"PowerSlot{i}: {GameDatabase.GetPrototypeName(PowerSlots[i])}");
            return sb.ToString();
        }
    }
}
