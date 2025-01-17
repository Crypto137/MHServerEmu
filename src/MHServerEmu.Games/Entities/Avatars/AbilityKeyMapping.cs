using System.Text;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Entities.Avatars
{
    public enum AbilitySlot     // See UnrealEngine3\MarvelGame\Config\DefaultInput.ini for reference
    {
        Invalid = -1,
        PrimaryAction = 0,          // Left Click
        SecondaryAction = 1,        // Right Click
        ActionKey0 = 2,             // A
        ActionKey1 = 3,             // S
        ActionKey2 = 4,             // D
        ActionKey3 = 5,             // F
        ActionKey4 = 6,             // G
        ActionKey5 = 7,             // H
        NumActions = 8,             
        DedicatedHealSlot = 9,      // M
        DedicatedUltimateSlot = 10, // U
        DedicatedTeamUpSlot = 11,   // K
        DedicatedPetTechSlot = 12,  // J
        TravelPower = 13,           // R
        NumSlotsTotal = 14
    }

    /// <summary>
    /// Binds abilities to slots.
    /// </summary>
    public class AbilityKeyMapping : ISerialize
    {
        private const int NumActionKeySlots = 6;   // non-mouse slots

        private static readonly Logger Logger = LogManager.CreateLogger();

        private int _powerSpecIndex;
        private bool _shouldPersist;
        private PrototypeId _associatedTransformMode;

        // Assignable slots
        private PrototypeId _primaryAction = PrototypeId.Invalid;
        private PrototypeId _secondaryAction = PrototypeId.Invalid;
        private PrototypeId[] _actionKeys = new PrototypeId[NumActionKeySlots];

        public int PowerSpecIndex { get => _powerSpecIndex; set => _powerSpecIndex = value; }
        public bool ShouldPersist { get => _shouldPersist; set => _shouldPersist = value; }
        public PrototypeId AssociatedTransformMode { get => _associatedTransformMode; set => _associatedTransformMode = value; }

        public AbilityKeyMapping() { }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            success &= Serializer.Transfer(archive, ref _powerSpecIndex);
            success &= Serializer.Transfer(archive, ref _shouldPersist);
            success &= Serializer.Transfer(archive, ref _associatedTransformMode);
            success &= Serializer.Transfer(archive, ref _primaryAction);
            success &= Serializer.Transfer(archive, ref _secondaryAction);
            success &= Serializer.Transfer(archive, ref _actionKeys);

            return success;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(_powerSpecIndex)}: {_powerSpecIndex}");
            sb.AppendLine($"{nameof(_shouldPersist)}: {_shouldPersist}");
            sb.AppendLine($"{nameof(_associatedTransformMode)}: {GameDatabase.GetPrototypeName(_associatedTransformMode)}");
            sb.AppendLine($"{nameof(_primaryAction)}: {GameDatabase.GetPrototypeName(_primaryAction)}");
            sb.AppendLine($"{nameof(_secondaryAction)}: {GameDatabase.GetPrototypeName(_secondaryAction)}");
            for (int i = 0; i < _actionKeys.Length; i++)
                sb.AppendLine($"{nameof(_actionKeys)}[{i}]: {GameDatabase.GetPrototypeName(_actionKeys[i])}");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the <see cref="PrototypeId"/> of the ability slotted in the specified <see cref="AbilitySlot"/>.
        /// </summary>
        public PrototypeId GetAbilityInAbilitySlot(AbilitySlot abilitySlot)
        {
            if (IsActiveAbilitySlot(abilitySlot) == false)
                return Logger.WarnReturn(PrototypeId.Invalid, $"GetAbilityInAbilitySlot(): Invalid ability slot {abilitySlot}");

            switch (abilitySlot)
            {
                case AbilitySlot.PrimaryAction:         return _primaryAction;
                case AbilitySlot.SecondaryAction:       return _secondaryAction;

                case AbilitySlot.DedicatedHealSlot:
                case AbilitySlot.DedicatedPetTechSlot:
                case AbilitySlot.DedicatedTeamUpSlot:
                case AbilitySlot.DedicatedUltimateSlot: return PrototypeId.Invalid;     // TODO (if we need this)
            }

            if (IsActionKeyAbilitySlot(abilitySlot) == false)
                return Logger.WarnReturn(PrototypeId.Invalid, $"GetAbilityInAbilitySlot(): Unhandled ability slot {abilitySlot}");

            // Action keys
            int index = ConvertSlotToArrayIndex(abilitySlot);
            return _actionKeys[index];
        }

        public void GetActiveAbilitySlotsContainingProtoRef(PrototypeId powerProtoRef, List<AbilitySlot> abilitySlotList)
        {
            if (_primaryAction == powerProtoRef)
                abilitySlotList.Add(AbilitySlot.PrimaryAction);

            if (_secondaryAction == powerProtoRef)
                abilitySlotList.Add(AbilitySlot.SecondaryAction);

            // TODO: DedicatedHealSlot, DedicatedPetTechSlot, DedicatedTeamUpSlot, DedicatedUltimateSlot

            for (int i = 0; i < _actionKeys.Length; i++)
            {
                if (_actionKeys[i] == powerProtoRef)
                    abilitySlotList.Add(AbilitySlot.ActionKey0 + i);
            }
        }

        /// <summary>
        /// Sets the ability <see cref="PrototypeId"/> to the specified <see cref="AbilitySlot"/>.
        /// </summary>
        public bool SetAbilityInAbilitySlot(PrototypeId abilityPrototypeId, AbilitySlot abilitySlot)
        {
            if (IsActiveAbilitySlot(abilitySlot) == false)
                return Logger.WarnReturn(false, $"SetAbilityInAbilitySlot(): Invalid ability slot {abilitySlot}");

            switch (abilitySlot)
            {
                case AbilitySlot.PrimaryAction:
                    _primaryAction = abilityPrototypeId;
                    ShouldPersist = true;
                    break;
                case AbilitySlot.SecondaryAction:
                    _secondaryAction = abilityPrototypeId;
                    ShouldPersist = true;
                    break;

                case AbilitySlot.DedicatedHealSlot:
                case AbilitySlot.DedicatedPetTechSlot:
                case AbilitySlot.DedicatedTeamUpSlot:
                case AbilitySlot.DedicatedUltimateSlot:
                case AbilitySlot.TravelPower:
                    // TODO (if we need this)
                    break;

                default:    // action key slots
                    if (IsActionKeyAbilitySlot(abilitySlot) == false)
                        return Logger.WarnReturn(false, $"SetAbilityInAbilitySlot(): Unhandled ability slot {abilitySlot}");

                    int index = ConvertSlotToArrayIndex(abilitySlot);
                    _actionKeys[index] = abilityPrototypeId;
                    break;
            }

            return true;
        }

        /// <summary>
        /// Slots default abilities into all slots.
        /// </summary>
        public void SlotDefaultAbilities(Avatar avatar)
        {
            AvatarPrototype avatarProto = avatar.AvatarPrototype;

            List<HotkeyData> hotkeyDataList = ListPool<HotkeyData>.Instance.Get();

            if (GetDefaultAbilities(hotkeyDataList, avatar))
            {
                foreach (HotkeyData hotkeyData in hotkeyDataList)
                    SetAbilityInAbilitySlot(hotkeyData.AbilityProtoRef, hotkeyData.AbilitySlot);
            }

            ListPool<HotkeyData>.Instance.Return(hotkeyDataList);
        }

        public bool GetDefaultAbilities(List<HotkeyData> hotkeyDataList, Avatar avatar, int startingLevel = -1)
        {
            AvatarPrototype avatarProto = avatar.AvatarPrototype;

            List<PowerProgressionEntryPrototype> powerProgEntryList = ListPool<PowerProgressionEntryPrototype>.Instance.Get();
            if (avatarProto.GetPowersUnlockedAtLevel(powerProgEntryList, avatar.CharacterLevel, true, startingLevel))
            {
                foreach (PowerProgressionEntryPrototype powerProgEntry in powerProgEntryList)
                {
                    // Skip traits
                    if (powerProgEntry.IsTrait)
                        continue;

                    // Skip powers that don't have auto-assignment defined
                    AbilityAutoAssignmentSlotPrototype autoAssignmentSlot = avatarProto.GetPowerInAbilityAutoAssignmentSlot(powerProgEntry.PowerAssignment.Ability);
                    if (autoAssignmentSlot == null)
                        continue;

                    // Get ability slot 
                    GamepadSlotBindingPrototype gamepadSlotBinding = GameDatabase.GetPrototype<GamepadSlotBindingPrototype>(autoAssignmentSlot.Slot);
                    var abilitySlot = (AbilitySlot)gamepadSlotBinding.PCSlotNumber;  // Avatar::ChooseGamePadSlot()

                    // Override only empty slots
                    if (GetAbilityInAbilitySlot(abilitySlot) != PrototypeId.Invalid)
                        continue;

                    // TODO: Avatar::GetMappedPowerFromOriginalPower()
                    // TODO: Avatar::CheckAbilitySlotRestrictions()

                    hotkeyDataList.Add(new HotkeyData(autoAssignmentSlot.Ability, abilitySlot));
                }
            }

            ListPool<PowerProgressionEntryPrototype>.Instance.Return(powerProgEntryList);
            return hotkeyDataList.Count > 0;
        }

        // In the client these ability slot checks are in Avatar, but they make more sense here

        /// <summary>
        /// Checks if an <see cref="AbilitySlot"/> is valid.
        /// </summary>
        private static bool IsActiveAbilitySlot(AbilitySlot slot)
        {
            return slot > AbilitySlot.Invalid && slot < AbilitySlot.NumSlotsTotal;
        }

        /// <summary>
        /// Checks if an <see cref="AbilitySlot"/> is an action key slot (non-mouse bindable slot).
        /// </summary>
        private static bool IsActionKeyAbilitySlot(AbilitySlot slot)
        {
            return slot >= AbilitySlot.ActionKey0 && slot <= AbilitySlot.ActionKey5;
        }

        /// <summary>
        /// Checks if an <see cref="AbilitySlot"/> is a dedicated ability slot (ultimate, travel, etc.).
        /// </summary>
        private static bool IsDedicatedAbilitySlot(AbilitySlot slot)
        {
            return slot > AbilitySlot.NumActions && slot < AbilitySlot.NumSlotsTotal;
        }

        /// <summary>
        /// Converts an <see cref="AbilitySlot"/> to an array index.
        /// </summary>
        private static int ConvertSlotToArrayIndex(AbilitySlot slot)
        {
            if (slot < AbilitySlot.ActionKey0) return (int)slot;
            if (slot < AbilitySlot.NumActions) return (int)slot - 2;
            return Logger.WarnReturn((int)slot, $"ConvertSlotToArrayIndex(): Enum argument is not within an array-stored ability slot range");
        }
    }
}
