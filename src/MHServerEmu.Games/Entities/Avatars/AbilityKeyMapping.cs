using System.Text;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Powers;

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
        NumSlotsTotal = 14,

        ActiveFirst = PrimaryAction,
        ActiveLast = ActionKey5,
    }

    /// <summary>
    /// Binds abilities to slots. Abilities refers to both powers and items that can be slotted on the action bar.
    /// </summary>
    public class AbilityKeyMapping : ISerialize
    {
        private const int NumActionKeySlots = 6;   // non-mouse slots
        private const int NumHotkeys = 6;

#if GAME_VERSION_1_48
        private int _keyMappingIndex;
#endif
        private int _powerSpecIndex;
        private bool _shouldPersist;
        private PrototypeId _associatedTransformMode;

        // Assignable slots
        private PrototypeId _primaryAction;
        private PrototypeId _secondaryAction;
        private InlineArray6<PrototypeId> _actionKeys;

        private InlineArray6<HotkeyData> _hotkeys;

#if GAME_VERSION_1_48
        public int KeyMappingIndex { get => _keyMappingIndex; set => _keyMappingIndex = value; }
#endif
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
            for (int i = 0; i < NumActionKeySlots; i++)
                sb.AppendLine($"{nameof(_actionKeys)}[{i}]: {GameDatabase.GetPrototypeName(_actionKeys[i])}");
            return sb.ToString();
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

#if GAME_VERSION_1_48
            success &= Serializer.Transfer(archive, ref _keyMappingIndex);
#endif
            success &= Serializer.Transfer(archive, ref _powerSpecIndex);
            success &= Serializer.Transfer(archive, ref _shouldPersist);
            success &= Serializer.Transfer(archive, ref _associatedTransformMode);
            success &= Serializer.Transfer(archive, ref _primaryAction);
            success &= Serializer.Transfer(archive, ref _secondaryAction);
            success &= Serializer.Transfer(archive, _actionKeys);
#if GAME_VERSION_1_48
            success &= Serializer.Transfer(archive, (Span<HotkeyData>)_hotkeys);
#endif

            return success;
        }

        /// <summary>
        /// Returns the <see cref="PrototypeId"/> of the ability slotted in the specified <see cref="AbilitySlot"/>.
        /// </summary>
        public PrototypeId GetAbilityInAbilitySlot(AbilitySlot abilitySlot)
        {
            if (!Verify.IsTrue(Avatar.IsActiveAbilitySlot(abilitySlot))) return PrototypeId.Invalid;

            switch (abilitySlot)
            {
                case AbilitySlot.PrimaryAction:         return _primaryAction;
                case AbilitySlot.SecondaryAction:       return _secondaryAction;

                case AbilitySlot.DedicatedHealSlot:
                case AbilitySlot.DedicatedPetTechSlot:
                case AbilitySlot.DedicatedTeamUpSlot:
                case AbilitySlot.DedicatedUltimateSlot: return PrototypeId.Invalid;     // TODO (if we need this)
            }

            if (!Verify.IsTrue(Avatar.IsActionKeyAbilitySlot(abilitySlot))) return PrototypeId.Invalid;

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

            for (int i = 0; i < NumActionKeySlots; i++)
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
        public bool SetAbilityInAbilitySlot(PrototypeId abilityProtoRef, AbilitySlot abilitySlot)
        {
            if (!Verify.IsTrue(Avatar.IsActiveAbilitySlot(abilitySlot))) return false;

            switch (abilitySlot)
            {
                case AbilitySlot.PrimaryAction:
                    _primaryAction = abilityProtoRef;
                    ShouldPersist = true;
                    break;
                case AbilitySlot.SecondaryAction:
                    _secondaryAction = abilityProtoRef;
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
                    if (!Verify.IsTrue(Avatar.IsActionKeyAbilitySlot(abilitySlot))) return false;

                    int index = ConvertSlotToArrayIndex(abilitySlot);
                    _actionKeys[index] = abilityProtoRef;
                    ShouldPersist = true;
                    break;
            }

            return true;
        }

        public void InitDedicatedAbilitySlots(Avatar avatar)
        {
            // TODO
        }

        public bool CleanUpAfterRespec(Avatar avatar)
        {
            AvatarPrototype avatarProto = avatar.AvatarPrototype;
            if (!Verify.IsNotNull(avatarProto)) return false;

            for (AbilitySlot slot = AbilitySlot.PrimaryAction; slot < AbilitySlot.NumActions; slot++)
            {
                PrototypeId abilityProtoRef = GetAbilityInAbilitySlot(slot);
                if (abilityProtoRef == PrototypeId.Invalid)
                    continue;

                // Do not remove slotted items
                PowerPrototype powerProto = abilityProtoRef.As<PowerPrototype>();
                if (powerProto == null)
                    continue;

                // Do not remove powers that are still available
                avatar.GetPowerProgressionInfo(abilityProtoRef, out PowerProgressionInfo powerInfo);
                if (avatar.ComputePowerRank(ref powerInfo, PowerSpecIndex, out _) > 0)
                    continue;

                SetAbilityInAbilitySlot(PrototypeId.Invalid, slot);
            }

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            SlotDefaultAbilities(avatar);
#else
            if (_keyMappingIndex == 0)
                SlotDefaultAbilities(avatarProto, true);
#endif

            return true;
        }

        /// <summary>
        /// Slots default abilities into all slots.
        /// </summary>
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        public void SlotDefaultAbilities(Avatar avatar)
        {
            using var hotkeyDataListHandle = ListPool<HotkeyData>.Instance.Get(out List<HotkeyData> hotkeyDataList);

            if (GetDefaultAbilities(hotkeyDataList, avatar))
            {
                foreach (HotkeyData hotkeyData in hotkeyDataList)
                    SetAbilityInAbilitySlot(hotkeyData.AbilityProtoRef, hotkeyData.AbilitySlot);
            }
        }
#else
        public void SlotDefaultAbilities(AvatarPrototype avatarProto, bool isRespecCleanup)
        {
            if (avatarProto.StartingEquippedAbilities.IsNullOrEmpty())
                return;

            AbilitySlot abilitySlot = 0;
            foreach (AbilityAssignmentPrototype abilityAssignment in avatarProto.StartingEquippedAbilities)
            {
                if (!Verify.IsNotNull(abilityAssignment))
                    goto Next;

                PrototypeId abilityProtoRef = abilityAssignment.Ability;

                if (!Verify.IsTrue(abilitySlot >= AbilitySlot.ActiveFirst && abilitySlot <= AbilitySlot.ActiveLast,
                    $"Too many StartingEquippedAbilities to fit on ability bar:\nAvatar: [{avatarProto}]"))
                {
                    return;
                }

                if (abilityProtoRef == PrototypeId.Invalid)
                    goto Next;

                if (GetAbilityInAbilitySlot(abilitySlot) != PrototypeId.Invalid)
                    goto Next;

                Prototype abilityProto = abilityProtoRef.As<Prototype>();
                PowerPrototype powerProto = abilityProto as PowerPrototype;
                ItemPrototype itemProto = abilityProto as ItemPrototype;

                if (!Verify.IsTrue(powerProto == null || powerProto.Activation != PowerActivationType.Passive,
                    $"Encountered a passive power in the following avatar's StartingEquippedAblities which isn't supported anymore. Just give the passive a non-zero Rank in the avatar's PowerProgressionTable.\nAvatar: [{avatarProto}]\nPower: [{abilityProtoRef.GetName()}]"))
                {
                    goto Next;
                }

                if (!Verify.IsTrue(powerProto != null || (itemProto?.AbilitySettings != null),
                    $"Failed to get the prototype using data ref when assigning a starting equipped ability:\nAvatar: [{avatarProto}]\nAbility: [{abilityProtoRef.GetName()}]"))
                {
                    goto Next;
                }

                if (isRespecCleanup && itemProto != null)
                    goto Next;

                AbilitySlotOpValidateResult slotResult = Avatar.CheckAbilitySlotRestrictions(abilityProtoRef, abilitySlot);
                if (!Verify.IsTrue(slotResult == AbilitySlotOpValidateResult.Valid,
                    $"Failed to slot ability because of slot restriction. SlotResult: [{slotResult}]\nAvatar: [{avatarProto}]\nAbility: [{abilityProtoRef.GetName()}]"))
                {
                    goto Next;
                }

                if (!Verify.IsTrue(powerProto == null || avatarProto.HasPowerInPowerProgression(abilityProtoRef),
                    $"Failed to slot starting ability [{abilityProtoRef.GetName()}] to avatar [{avatarProto}], because it is not in the avatar's PowerProgressionTable."))
                {
                    goto Next;
                }

                SetAbilityInAbilitySlot(abilityProtoRef, abilitySlot);

            Next:
                abilitySlot++;
            }
        }
#endif

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
                if (!Verify.IsTrue(abilitySlot != AbilitySlot.Invalid, $"Failed an ability assignment because there are too many default abilities for TransformMode: [{transformModeProto}]"))
                    continue;

                AbilitySlotOpValidateResult slotResult = Avatar.CheckAbilitySlotRestrictions(abilityProtoRef, abilitySlot);
                if (!Verify.IsTrue(slotResult == AbilitySlotOpValidateResult.Valid, $"Failed to slot ability because of slot restriction. SlotResult: [{slotResult}]\nTransformMode: [{transformModeProto}]\nAbility: [{abilityProtoRef.GetName()}]"))
                    continue;

                // Do the slotting
                SetAbilityInAbilitySlot(abilityProtoRef, abilitySlot);
            }
        }

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        public bool GetDefaultAbilities(List<HotkeyData> hotkeyDataList, Avatar avatar, int startingLevel = -1)
        {
            AvatarPrototype avatarProto = avatar.AvatarPrototype;
            if (!Verify.IsNotNull(avatarProto)) return false;

            using var powerProgEntryListHandle = ListPool<PowerProgressionEntryPrototype>.Instance.Get(out List<PowerProgressionEntryPrototype> powerProgEntryList);
            if (avatarProto.GetPowersUnlockedAtLevel(powerProgEntryList, avatar.CharacterLevel, true, startingLevel))
            {
                foreach (PowerProgressionEntryPrototype powerProgEntry in powerProgEntryList)
                {
                    // Skip traits
#if GAME_VERSION_1_52
                    if (powerProgEntry.IsTrait)
                        continue;
#elif GAME_VERSION_1_53
                    if (powerProgEntry.TraitCategory != TraitCategory.None)
                        continue;
#endif

                    // Skip powers that don't have auto-assignment defined
                    AbilityAutoAssignmentSlotPrototype powerAssignmentProto = avatarProto.GetPowerInAbilityAutoAssignmentSlot(powerProgEntry.PowerAssignment.Ability);
                    if (powerAssignmentProto == null)
                        continue;

                    PrototypeId abilityToBeSlotted = powerAssignmentProto.Ability;

                    // Get ability slot 
                    GamepadSlotBindingPrototype gamepadSlotProto = GameDatabase.GetPrototype<GamepadSlotBindingPrototype>(powerAssignmentProto.Slot);
                    if (!Verify.IsNotNull(gamepadSlotProto, $"Failed to get the gamepad slot using data ref in the Ability Auto Assignment Slot:\nAvatar: [{avatar}]"))
                        continue;

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
                    if (!Verify.IsTrue(slotResult == AbilitySlotOpValidateResult.Valid, $"Failed to slot ability because of slot restriction. SlotResult: [{slotResult}]\nAvatar: [{avatar}]\nAbility: [{abilityToBeSlotted.GetName()}]"))
                        continue;

                    hotkeyDataList.Add(new HotkeyData(abilityToBeSlotted, slot));
                }
            }

            return hotkeyDataList.Count > 0;
        }
#endif

        /// <summary>
        /// Converts an <see cref="AbilitySlot"/> to an array index.
        /// </summary>
        private static int ConvertSlotToArrayIndex(AbilitySlot slot)
        {
            if (slot < AbilitySlot.ActionKey0)
                return (int)slot;

            if (slot < AbilitySlot.NumActions)
                return (int)slot - 2;

            Verify.IsTrue(false, "Code is calling ConvertSlotToArrayIndex() with a slot enum argument that is not within an array-stored ability slot range!");
            return (int)slot;
        }
    }
}
