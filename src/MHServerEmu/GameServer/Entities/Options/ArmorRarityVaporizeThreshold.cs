using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.GameData;
using MHServerEmu.GameServer.Loot;

namespace MHServerEmu.GameServer.Entities.Options
{
    public class ArmorRarityVaporizeThreshold
    {
        public EquipmentInvUISlot Slot { get; set; }
        public ulong RarityPrototypeId { get; set; }

        public ArmorRarityVaporizeThreshold(CodedInputStream stream)
        {
            Slot = (EquipmentInvUISlot)stream.ReadRawVarint64();
            RarityPrototypeId = stream.ReadPrototypeEnum(PrototypeEnumType.All);
        }

        public ArmorRarityVaporizeThreshold(EquipmentInvUISlot slot, ulong rarityPrototypeId)
        {
            Slot = slot;
            RarityPrototypeId = rarityPrototypeId;
        }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint64((ulong)Slot);
                cos.WritePrototypeEnum(RarityPrototypeId, PrototypeEnumType.All);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Slot: {Slot}");
            sb.AppendLine($"RarityPrototypeId: {GameDatabase.GetPrototypeName(RarityPrototypeId)}");
            return sb.ToString();
        }
    }
}
