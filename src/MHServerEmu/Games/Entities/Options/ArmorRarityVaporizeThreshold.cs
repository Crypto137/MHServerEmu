using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Loot;

namespace MHServerEmu.Games.Entities.Options
{
    public class ArmorRarityVaporizeThreshold
    {
        public EquipmentInvUISlot Slot { get; set; }
        public PrototypeId RarityPrototypeId { get; set; }

        public ArmorRarityVaporizeThreshold(CodedInputStream stream)
        {
            Slot = (EquipmentInvUISlot)stream.ReadRawVarint64();
            RarityPrototypeId = stream.ReadPrototypeEnum(PrototypeEnumType.All);
        }

        public ArmorRarityVaporizeThreshold(EquipmentInvUISlot slot, PrototypeId rarityPrototypeId)
        {
            Slot = slot;
            RarityPrototypeId = rarityPrototypeId;
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64((ulong)Slot);
            stream.WritePrototypeEnum(RarityPrototypeId, PrototypeEnumType.All);
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
