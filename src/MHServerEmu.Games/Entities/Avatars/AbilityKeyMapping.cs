using System.Text;
using MHServerEmu.Core.Extensions;
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
    /// Binds abilities to slots. Abilities refers to both powers and items that can be slotted on the action bar.
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

        /// <summary>
        /// Returns the <see cref="PrototypeId"/> of the ability slotted in the specified <see cref="AbilitySlot"/>.
        /// </summary>
        public PrototypeId GetAbilityInAbilitySlot(AbilitySlot abilitySlot)
        {
            if (Avatar.IsActiveAbilitySlot(abilitySlot) == false)
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

            if (Avatar.IsActionKeyAbilitySlot(abilitySlot) == false)
                return Logger.WarnReturn(PrototypeId.Invalid, $"GetAbilityInAbilitySlot(): Unhandled ability slot {abilitySlot}");

            // Action keys
            int index = ConvertSlotToArrayIndex(abilitySlot);
            return _actionKeys[index];
        }

        public void GetActiveAbilitySlotsContainingProtoRef(PrototypeId abilityProtoRef, List<AbilitySlot> abilitySlotList)
        {
            if (_primaryAction == abilityProtoRef)
                abilitySlotList.Add(AbilitySlot.PrimaryAction);

            if (_secondaryAction == abilityProtoRef)
                abilitySlotList.Add(AbilitySlot.SecondaryAction);

            // TODO: DedicatedHealSlot, DedicatedPetTechSlot, DedicatedTeamUpSlot, DedicatedUltimateSlot

            for (int i = 0; i < _actionKeys.Length; i++)
            {
                if (_actionKeys[i] == abilityProtoRef)
                    abilitySlotList.Add(AbilitySlot.ActionKey0 + i);
            }
        }

        public bool ContainsAbilityInActiveSlot(PrototypeId abilityProtoRef)
        {
            // TODO: Check dedicated slots (if needed)
            if (_primaryAction == abilityProtoRef || _secondaryAction == abilityProtoRef)
                return true;

            foreach (PrototypeId actionKey in _actionKeys)
            {
                if (actionKey == abilityProtoRef)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Sets the ability <see cref="PrototypeId"/> to the specified <see cref="AbilitySlot"/>.
        /// </summary>
        public bool SetAbilityInAbilitySlot(PrototypeId abilityPrototypeId, AbilitySlot abilitySlot)
        {
            if (Avatar.IsActiveAbilitySlot(abilitySlot) == false)
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
                    if (Avatar.IsActionKeyAbilitySlot(abilitySlot) == false)
                        return Logger.WarnReturn(false, $"SetAbilityInAbilitySlot(): Unhandled ability slot {abilitySlot}");

                    int index = ConvertSlotToArrayIndex(abilitySlot);
                    _actionKeys[index] = abilityPrototypeId;
                    break;
            }

            return true;
        }

        public void InitDedicatedAbilitySlots(Avatar avatar)
        {
            // TODO
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

        public void SlotDefaultAbilitiesForTransformMode(TransformModePrototype transformModeProto)
        {
            if (transformModeProto.DefaultEquippedAbilities.IsNullOrEmpty())
                return;

            AbilitySlot itSlot = 0;
            for (int i = 0; i < transformModeProto.DefaultEquippedAbilities.Length; i++, itSlot++)
            {
                AbilityAssignmentPrototype abilityAssignment = transformModeProto.DefaultEquippedAbilities[i];
                PrototypeId abilityProtoRef = abilityAssignment.Ability;

                // Skip empty slots
                if (abilityProtoRef == PrototypeId.Invalid)
                    continue;

                // Not implementing prototype validation here since we don't really need it

                // Validate slot
                AbilitySlot abilitySlot = itSlot < AbilitySlot.NumActions ? itSlot : AbilitySlot.Invalid;
                if (abilitySlot == AbilitySlot.Invalid)
                {
                    Logger.Warn($"SlotDefaultAbilitiesForTransformMode(): Failed an ability assignment because there are too many default abilities for TransformMode: [{transformModeProto}]");
                    continue;
                }

                AbilitySlotOpValidateResult slotResult = Avatar.CheckAbilitySlotRestrictions(abilityProtoRef, abilitySlot);
                if (slotResult != AbilitySlotOpValidateResult.Valid)
                {
                    Logger.Warn($"SlotDefaultAbilitiesForTransformMode(): Failed to slot ability because of slot restriction. SlotResult: [{slotResult}]\nTransformMode: [{transformModeProto}]\nAbility: [{abilityProtoRef.GetName()}]");
                    continue;
                }

                // Do the slotting
                SetAbilityInAbilitySlot(abilityProtoRef, abilitySlot);
            }
        }

        public bool GetDefaultAbilities(List<HotkeyData> hotkeyDataList, Avatar avatar, int startingLevel = -1)
        {
            AvatarPrototype avatarProto = avatar.AvatarPrototype;
            if (avatarProto == null) return Logger.WarnReturn(false, "GetDefaultAbilities(): avatarProto == null");

            List<PowerProgressionEntryPrototype> powerProgEntryList = ListPool<PowerProgressionEntryPrototype>.Instance.Get();
            if (avatarProto.GetPowersUnlockedAtLevel(powerProgEntryList, avatar.CharacterLevel, true, startingLevel))
            {
                foreach (PowerProgressionEntryPrototype powerProgEntry in powerProgEntryList)
                {
                    // Skip traits
                    if (powerProgEntry.IsTrait)
                        continue;

                    // Skip powers that don't have auto-assignment defined
                    AbilityAutoAssignmentSlotPrototype powerAssignmentProto = avatarProto.GetPowerInAbilityAutoAssignmentSlot(powerProgEntry.PowerAssignment.Ability);
                    if (powerAssignmentProto == null)
                        continue;

                    PrototypeId abilityToBeSlotted = powerAssignmentProto.Ability;

                    // Get ability slot 
                    GamepadSlotBindingPrototype gamepadSlotProto = GameDatabase.GetPrototype<GamepadSlotBindingPrototype>(powerAssignmentProto.Slot);
                    if (gamepadSlotProto == null)
                    {
                        Logger.Warn($"GetDefaultAbilities(): Failed to get the gamepad slot using data ref in the Ability Auto Assignment Slot:\nAvatar: [{avatar}]");
                        continue;
                    }

                    // Avatar::ChooseGamePadSlot() - we don't need this on PC
                    AbilitySlot slot = (AbilitySlot)gamepadSlotProto.PCSlotNumber;

                    // Override only empty slots
                    if (GetAbilityInAbilitySlot(slot) != PrototypeId.Invalid)
                        continue;

                    // Check if the ability to be slotted is mapped to something else (e.g. Jean Grey)
                    PrototypeId mappedPowerRef = avatar.GetMappedPowerFromOriginalPower(abilityToBeSlotted);
                    if (mappedPowerRef != PrototypeId.Invalid)
                        abilityToBeSlotted = mappedPowerRef;

                    // Check slot restrictions
                    AbilitySlotOpValidateResult slotResult = Avatar.CheckAbilitySlotRestrictions(abilityToBeSlotted, slot);
                    if (slotResult != AbilitySlotOpValidateResult.Valid)
                    {
                        Logger.Warn($"GetDefaultAbilities(): Failed to slot ability because of slot restriction. SlotResult: [{slotResult}]\nAvatar: [{avatar}]\nAbility: [{abilityToBeSlotted.GetName()}]");
                        continue;
                    }

                    hotkeyDataList.Add(new HotkeyData(abilityToBeSlotted, slot));
                }
            }

            ListPool<PowerProgressionEntryPrototype>.Instance.Return(powerProgEntryList);
            return hotkeyDataList.Count > 0;
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
