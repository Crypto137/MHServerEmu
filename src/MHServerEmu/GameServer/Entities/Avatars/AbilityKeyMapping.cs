using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoding;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.GameData;

namespace MHServerEmu.GameServer.Entities.Avatars
{
    public class AbilityKeyMapping
    {
        public int PowerSpecIndex { get; set; }
        public bool ShouldPersist { get; set; }
        public ulong AssociatedTransformMode { get; set; }  // EnumProperty
        public ulong Slot0 { get; set; }    // EnumProperty
        public ulong Slot1 { get; set; }    // EnumProperty
        public ulong[] PowerSlots { get; set; } // Gazillion::Serializer::Transfer_PrototypeDataRef_6ul

        public AbilityKeyMapping(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            PowerSpecIndex = stream.ReadRawInt32();

            if (boolDecoder.IsEmpty) boolDecoder.SetBits(stream.ReadRawByte());
            ShouldPersist = boolDecoder.ReadBool();

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

        public byte[] Encode(BoolEncoder boolEncoder)
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawInt32(PowerSpecIndex);

                byte bitBuffer = boolEncoder.GetBitBuffer();             // ShouldPersist
                if (bitBuffer != 0) stream.WriteRawByte(bitBuffer);

                stream.WritePrototypeId(AssociatedTransformMode, PrototypeEnumType.All);
                stream.WritePrototypeId(Slot0, PrototypeEnumType.All);
                stream.WritePrototypeId(Slot1, PrototypeEnumType.All);

                stream.WriteRawVarint64((ulong)PowerSlots.Length);
                foreach (ulong powerSlot in PowerSlots)
                    stream.WritePrototypeId(powerSlot, PrototypeEnumType.All);

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public override string ToString()
        {
            using (MemoryStream memoryStream = new())
            using (StreamWriter streamWriter = new(memoryStream))
            {
                streamWriter.WriteLine($"PowerSpecIndex: 0x{PowerSpecIndex.ToString("X")}");
                streamWriter.WriteLine($"ShouldPersist: {ShouldPersist}");
                streamWriter.WriteLine($"AssociatedTransformMode: {GameDatabase.GetPrototypePath(AssociatedTransformMode)}");
                streamWriter.WriteLine($"Slot0: {GameDatabase.GetPrototypePath(Slot0)}");
                streamWriter.WriteLine($"Slot1: {GameDatabase.GetPrototypePath(Slot1)}");
                for (int i = 0; i < PowerSlots.Length; i++) streamWriter.WriteLine($"PowerSlot{i}: {GameDatabase.GetPrototypePath(PowerSlots[i])}");

                streamWriter.Flush();
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
