using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.GameData;

namespace MHServerEmu.GameServer.Entities.Avatars
{
    public class AbilityKeyMapping
    {
        public int PowerSpecIndex { get; set; }
        public bool ShouldPersist { get; set; }
        public ulong AssociatedTransformMode { get; set; }  // Prototype
        public ulong Slot0 { get; set; }                    // Prototype
        public ulong Slot1 { get; set; }                    // Prototype
        public ulong[] PowerSlots { get; set; }             // Prototypes

        public AbilityKeyMapping(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            PowerSpecIndex = stream.ReadRawInt32();
            ShouldPersist = boolDecoder.ReadBool(stream);
            AssociatedTransformMode = stream.ReadPrototypeId(PrototypeEnumType.All);
            Slot0 = stream.ReadPrototypeId(PrototypeEnumType.All);
            Slot1 = stream.ReadPrototypeId(PrototypeEnumType.All);

            PowerSlots = new ulong[stream.ReadRawVarint64()];
            for (int i = 0; i < PowerSlots.Length; i++)
                PowerSlots[i] = stream.ReadPrototypeId(PrototypeEnumType.All);
        }

        public AbilityKeyMapping(int powerSpecIndex, bool shouldPersist, ulong associatedTransformMode, ulong slot0, ulong slot1, ulong[] powerSlots)
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

        public byte[] Encode(BoolEncoder boolEncoder)
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawInt32(PowerSpecIndex);
                boolEncoder.WriteBuffer(cos);   // ShouldPersist
                cos.WritePrototypeId(AssociatedTransformMode, PrototypeEnumType.All);
                cos.WritePrototypeId(Slot0, PrototypeEnumType.All);
                cos.WritePrototypeId(Slot1, PrototypeEnumType.All);

                cos.WriteRawVarint64((ulong)PowerSlots.Length);
                foreach (ulong powerSlot in PowerSlots)
                    cos.WritePrototypeId(powerSlot, PrototypeEnumType.All);

                cos.Flush();
                return ms.ToArray();
            }
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
