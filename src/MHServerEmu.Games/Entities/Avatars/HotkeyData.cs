using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Entities.Avatars
{
    public readonly struct HotkeyData
    {
        public PrototypeId AbilityProtoRef { get; }
        public AbilitySlot AbilitySlot { get; }

        public HotkeyData(PrototypeId abilityProtoRef, AbilitySlot abilitySlot)
        {
            AbilityProtoRef = abilityProtoRef;
            AbilitySlot = abilitySlot;
        }

        public override string ToString()
        {
            return $"abilityProtoRef={AbilityProtoRef.GetName()}, abilitySlot={AbilitySlot}";
        }
    }
}
