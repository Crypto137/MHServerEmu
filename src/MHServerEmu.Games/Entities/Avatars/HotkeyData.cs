using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Entities.Avatars
{
    public struct HotkeyData : ISerialize
    {
        public PrototypeId AbilityProtoRef;
        public AbilitySlot AbilitySlot;

        public HotkeyData() { }

        public HotkeyData(PrototypeId abilityProtoRef, AbilitySlot abilitySlot)
        {
            AbilityProtoRef = abilityProtoRef;
            AbilitySlot = abilitySlot;
        }

        public override string ToString()
        {
            return $"abilityProtoRef={AbilityProtoRef.GetName()}, abilitySlot={AbilitySlot}";
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            success &= Serializer.Transfer(archive, ref AbilityProtoRef);

            int abilitySlot = (int)AbilitySlot;
            success &= Serializer.Transfer(archive, ref abilitySlot);
            AbilitySlot = (AbilitySlot)abilitySlot;

            return success;
        }
    }
}
