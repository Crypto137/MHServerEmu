using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class AvatarPrototype : AgentPrototype
    {
        public LocaleStringId BioText { get; protected set; }
        public AbilityAssignmentPrototype[] HiddenPassivePowers { get; protected set; }
        public AssetId PortraitPath { get; protected set; }
#if GAME_VERSION_1_48
        public PrototypeId[] Skills { get; protected set; }
        public AbilityAssignmentPrototype[] StartingEquippedAbilities { get; protected set; }
#endif
        public PrototypeId StartingLootTable { get; protected set; }
        public AssetId UnlockDialogImage { get; protected set; }
        public AssetId HUDTheme { get; protected set; }
        public AvatarPrimaryStatPrototype[] PrimaryStats { get; protected set; }
        public PowerProgressionTablePrototype[] PowerProgressionTables { get; protected set; }
        public ItemAssignmentPrototype StartingCostume { get; protected set; }
        public PrototypeId ResurrectOtherEntityPower { get; protected set; }
        public AvatarEquipInventoryAssignmentPrototype[] EquipmentInventories { get; protected set; }
        public PrototypeId PartyBonusPower { get; protected set; }
        public LocaleStringId UnlockDialogText { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public SecondaryResourceManaBehaviorPrototype SecondaryResourceBehavior { get; protected set; }
        public PrototypeId[] LoadingScreens { get; protected set; }
        public int PowerProgressionVersion { get; protected set; }
        public PrototypeId OnLevelUpEval { get; protected set; }
        public EvalPrototype OnPartySizeChange { get; protected set; }
        public PrototypeId StatsPower { get; protected set; }
        public AssetId SocialIconPath { get; protected set; }
        public AssetId CharacterSelectIconPath { get; protected set; }
        public PrototypeId[] StatProgressionTable { get; protected set; }
        public TransformModeEntryPrototype[] TransformModes { get; protected set; }
        public AvatarSynergyEntryPrototype[] SynergyTable { get; protected set; }
        public PrototypeId[] SuperteamMemberships { get; protected set; }
        public PrototypeId[] CharacterSelectPowers { get; protected set; }
        [PrototypeField(PrototypeFieldType.VectorPrototypeRefPtr)]
        public PrimaryResourceManaBehaviorPrototype[] PrimaryResourceBehaviors { get; protected set; }
        [PrototypeField(PrototypeFieldType.VectorPrototypeRefPtr)]
        public StealablePowerInfoPrototype[] StealablePowersAllowed { get; protected set; }
        public bool ShowInRosterIfLocked { get; protected set; }
        public LocaleStringId CharacterVideoUrl { get; protected set; }
        public AssetId CharacterSelectIconPortraitSmall { get; protected set; }
        public AssetId CharacterSelectIconPortraitFull { get; protected set; }
        public LocaleStringId PrimaryResourceBehaviorNames { get; protected set; }
        public bool IsStarterAvatar { get; protected set; }
        public int CharacterSelectDisplayOrder { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public CostumeCorePrototype CostumeCore { get; protected set; }
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        public TalentGroupPrototype[] TalentGroups { get; protected set; }
        public PrototypeId TravelPower { get; protected set; }
        public AbilityAutoAssignmentSlotPrototype[] AbilityAutoAssignmentSlot { get; protected set; }
        public PrototypeId[] LoadingScreensConsole { get; protected set; }
        public ItemAssignmentPrototype StartingCostumePS4 { get; protected set; }
        public ItemAssignmentPrototype StartingCostumeXboxOne { get; protected set; }
#endif
#if GAME_VERSION_1_53
        public LocaleStringId PresenceStatusKeyXboxOne { get; protected set; }
        public SlotUnlockPrototype[] AbilitySlotUnlockProgression { get; protected set; }
        public bool OmegaPrestigeEnabled { get; protected set; }
        public AssetId SocialIconPathConsole { get; protected set; }
        public AssetId SynergyIconPath { get; protected set; }
        public AssetId SynergyIconPathConsole { get; protected set; }
        public AvatarPowerGroupUIPrototype[] PowerGroupUIs { get; protected set; }
#endif

        //---

        [DoNotCopy]
        public PrototypeId UltimatePowerRef { get; private set; } = PrototypeId.Invalid;

        [DoNotCopy]
        public int AvatarPrototypeEnumValue { get; private set; }
        [DoNotCopy]
        public override int LiveTuneEternitySplinterCost { get => (int)LiveTuningManager.GetLiveAvatarTuningVar(this, AvatarEntityTuningVar.eAETV_EternitySplinterPrice); }

        [DoNotCopy]
        public bool HasPowerProgressionTables { get => PowerProgressionTables.HasValue(); }

        [DoNotCopy]
        public int SynergyUnlockLevel { get; private set; }

        public override bool ApprovedForUse()
        {
            if (base.ApprovedForUse() == false) return false;

            // Avatars also need their starting costume to be approved to be considered approved themselves.
            // This is done in a separate AvatarPrototype.CostumeApprovedForUse() method rather than
            // CostumePrototype.ApprovedForUse() because the latter calls AvatarPrototype.ApprovedForUse().

            // Add settings for PS4 and Xbox One here if we end up supporting console clients
            PrototypeId startingCostumeId = GetStartingCostumeForPlatform(Platforms.PC);
            return CostumeApprovedForUse(startingCostumeId);
        }

        public override void PostProcess()
        {
            base.PostProcess();

            UIGlobalsPrototype uiGlobals = GameDatabase.UIGlobalsPrototype;

            if (PowerProgressionTables != null)
            {
                for (int i = 0; i < PowerProgressionTables.Length; i++)
                {
                    PowerProgressionTablePrototype powerProgTableProto = PowerProgressionTables[i];

                    // Assign tab references to power progression tables
                    Verify.IsTrue(i < 3, $"The following Avatar prototype has more than 3 PowerProgressionTable pages, which requires updates to the PowerBoost bonuses for power progression page code!\n[{this}]");

                    switch (i)
                    {
                        case 0: powerProgTableProto.PowerProgTableTabRef = uiGlobals.PowerProgTableTabRefTab1; break;
                        case 1: powerProgTableProto.PowerProgTableTabRef = uiGlobals.PowerProgTableTabRefTab2; break;
                        case 2: powerProgTableProto.PowerProgTableTabRef = uiGlobals.PowerProgTableTabRefTab3; break;
                        default: powerProgTableProto.PowerProgTableTabRef = PrototypeId.Invalid; break;
                    }

                    // Find the ultimate power
                    foreach (PowerProgressionEntryPrototype entryProto in powerProgTableProto.PowerProgressionEntries)
                    {
                        if (!Verify.IsNotNull(entryProto.PowerAssignment))
                            continue;

                        PowerPrototype powerProto = entryProto.PowerAssignment.Ability.As<PowerPrototype>();
                        if (!Verify.IsNotNull(powerProto, $"Avatar has invalid power assigned in Power Progression Table!\nAvatar: {this}"))
                            continue;

                        if (Power.IsUltimatePower(powerProto))
                        {
                            Verify.IsTrue(UltimatePowerRef == PrototypeId.Invalid,
                                $"The PowerProgressionTable for the following avatar has more than one entry flagged as the avatar's 'ultimate' power, which is not allowed!\n[%s]");
                            
                            UltimatePowerRef = entryProto.PowerAssignment.Ability;
                        }
                    }
                }
            }

            SynergyUnlockLevel = int.MaxValue;
            if (SynergyTable.HasValue())
            {
                Array.Sort(SynergyTable, static (a, b) => a.Level.CompareTo(b.Level));
                SynergyUnlockLevel = SynergyTable[0].Level;                
            }

            // TODO: StealablePowersAllowed (Is this used only for tooltips? In that case we probably don't need it.)

            AvatarPrototypeEnumValue = GetEnumValueFromBlueprint(LiveTuningData.GetAvatarBlueprintDataRef());
        }

        /// <summary>
        /// Returns the <see cref="PrototypeId"/> of the starting costume for the specified platform.
        /// </summary>
        public PrototypeId GetStartingCostumeForPlatform(Platforms platform)
        {
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            if (platform == Platforms.PS4 && StartingCostumePS4 != null)
                return StartingCostumePS4.Item;
            else if (platform == Platforms.XboxOne && StartingCostumeXboxOne != null)
                return StartingCostumeXboxOne.Item;
#endif

            if (!Verify.IsNotNull(StartingCostume)) return PrototypeId.Invalid;
            return StartingCostume.Item;
        }

        public AssetId GetStartingCostumeAssetRef(Platforms platform)
        {
            PrototypeId costumeProtoRef = GetStartingCostumeForPlatform(platform);
            Verify.IsTrue(costumeProtoRef != PrototypeId.Invalid);

            CostumePrototype startingCostumeProto = costumeProtoRef.As<CostumePrototype>();
            if (!Verify.IsNotNull(startingCostumeProto)) return AssetId.Invalid;

            return startingCostumeProto.CostumeUnrealClass;
        }

        /// <summary>
        /// Retrieves <see cref="PowerProgressionEntryPrototype"/> instances for powers that would be unlocked at the specified level or level range.
        /// </summary>
        public bool GetPowersUnlockedAtLevel(List<PowerProgressionEntryPrototype> powerProgEntryList, int level = -1, bool retrieveForLevelRange = false, int startingLevel = -1)
        {
            if (PowerProgressionTables.IsNullOrEmpty())
                return false;

            foreach (PowerProgressionTablePrototype table in PowerProgressionTables)
            {
                if (table.PowerProgressionEntries.IsNullOrEmpty())
                    continue;

                foreach (PowerProgressionEntryPrototype powerProgEntry in table.PowerProgressionEntries)
                {
                    AbilityAssignmentPrototype abilityAssignmentProto = powerProgEntry.PowerAssignment;
                    if (!Verify.IsNotNull(abilityAssignmentProto))
                        continue;

                    // If the specified level is set to -1 it means we need to include all levels.

                    // retrieveForLevelRange means to retrieve all abilities that would be unlocked
                    // if you got from startingLevel to level. Otherwise retrieve just the abilities
                    // for the specified level.

                    if (abilityAssignmentProto.Ability != PrototypeId.Invalid &&
                        (level < 0 || level >= powerProgEntry.Level) &&
                        ((retrieveForLevelRange && powerProgEntry.Level > startingLevel) || powerProgEntry.Level == level))
                    {
                        powerProgEntryList.Add(powerProgEntry);
                    }
                }
            }

            return powerProgEntryList.Count > 0;
        }

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        /// <summary>
        /// Returns the <see cref="AbilityAutoAssignmentSlotPrototype"/> for the specified power <see cref="PrototypeId"/> if there is one.
        /// Otherwise, returns <see langword="null"/>.
        /// </summary>
        public AbilityAutoAssignmentSlotPrototype GetPowerInAbilityAutoAssignmentSlot(PrototypeId powerProtoId)
        {
            if (AbilityAutoAssignmentSlot.IsNullOrEmpty())
                return null;

            foreach (AbilityAutoAssignmentSlotPrototype abilityAutoAssignmentSlot in AbilityAutoAssignmentSlot)
            {
                if (abilityAutoAssignmentSlot.Ability == powerProtoId)
                    return abilityAutoAssignmentSlot;
            }

            return null;
        }
#endif

        public PowerProgressionTablePrototype GetPowerProgressionTableAtIndex(int index)
        {
            if (PowerProgressionTables.IsNullOrEmpty())
                return null;

            if (!Verify.IsTrue(index >= 0)) return null;
            if (!Verify.IsTrue(index < PowerProgressionTables.Length)) return null;

            return PowerProgressionTables[index];
        }

        public int GetPowerProgressionTableIndexForPower(PrototypeId powerProtoRef)
        {
            if (PowerProgressionTables.IsNullOrEmpty())
                return -1;

            int index = 0;

            foreach (PowerProgressionTablePrototype powerProgTableProto in PowerProgressionTables)
            {
                if (powerProgTableProto.PowerProgressionEntries.IsNullOrEmpty())
                    continue;

                foreach (PowerProgressionEntryPrototype powerProgEntry in powerProgTableProto.PowerProgressionEntries)
                {
                    AbilityAssignmentPrototype abilityAssignmentProto = powerProgEntry.PowerAssignment;
                    if (!Verify.IsNotNull(abilityAssignmentProto))
                        continue;

                    if (abilityAssignmentProto.Ability == powerProtoRef)
                        return index;
                }

                index++;
            }

            return -1;
        }

        public PrototypeId GetPowerProgressionTableTabRefForPower(PrototypeId powerProtoRef)
        {
            int tableIndex = GetPowerProgressionTableIndexForPower(powerProtoRef);
            if (tableIndex < 0)
                return PrototypeId.Invalid;

            PowerProgressionTablePrototype powerProgTableProto = GetPowerProgressionTableAtIndex(tableIndex);
            if (!Verify.IsNotNull(powerProgTableProto)) return PrototypeId.Invalid;

            return powerProgTableProto.PowerProgTableTabRef;
        }

        public PowerProgressionEntryPrototype GetPowerProgressionEntryForPower(PrototypeId powerProtoRef)
        {
            if (PowerProgressionTables.IsNullOrEmpty())
                return null;

            foreach (PowerProgressionTablePrototype powerProgTableProto in PowerProgressionTables)
            {
                if (powerProgTableProto.PowerProgressionEntries.IsNullOrEmpty())
                    continue;

                foreach (PowerProgressionEntryPrototype powerProgEntry in powerProgTableProto.PowerProgressionEntries)
                {
                    AbilityAssignmentPrototype abilityAssignmentProto = powerProgEntry.PowerAssignment;
                    if (!Verify.IsNotNull(abilityAssignmentProto))
                        continue;

                    if (abilityAssignmentProto.Ability == powerProtoRef)
                        return powerProgEntry;
                }
            }

            return null;
        }

        public bool HasPowerInPowerProgression(PrototypeId powerProtoRef)
        {
            return GetPowerProgressionEntryForPower(powerProtoRef) != null;
        }

        public TransformModePrototype FindTransformModeThatAssignsPower(PrototypeId powerProtoRef)
        {
            if (!Verify.IsTrue(powerProtoRef != PrototypeId.Invalid)) return null;

            if (TransformModes.IsNullOrEmpty())
                return null;

            foreach (TransformModeEntryPrototype entryProto in TransformModes)
            {
                if (entryProto.TransformMode == PrototypeId.Invalid)
                    continue;

                TransformModePrototype transformModeProto = entryProto.TransformMode.As<TransformModePrototype>();
                if (!Verify.IsNotNull(transformModeProto))
                    continue;

                if (transformModeProto.DefaultEquippedAbilities.HasValue())
                {
                    foreach (AbilityAssignmentPrototype abilityAssignment in transformModeProto.DefaultEquippedAbilities)
                    {
                        if (abilityAssignment.Ability == powerProtoRef)
                            return transformModeProto;
                    }
                }

                if (transformModeProto.HiddenPassivePowers.HasValue())
                {
                    foreach (PrototypeId hiddenPassivePowerProtoRef in transformModeProto.HiddenPassivePowers)
                    {
                        if (hiddenPassivePowerProtoRef == powerProtoRef)
                            return transformModeProto;
                    }
                }
            }

            return null;
        }

        public PrototypeId[] GetAllowedPowersForTransformMode(PrototypeId transformModeRef)
        {
            if (TransformModes.IsNullOrEmpty())
                return null;

            foreach (TransformModeEntryPrototype entryProto in TransformModes)
            {
                if (entryProto.TransformMode == transformModeRef)
                    return entryProto.AllowedPowers;
            }

            return null;
        }

        /// <summary>
        /// Returns <see langword="true"/> if the provided costume is approved for use.
        /// </summary>
        private bool CostumeApprovedForUse(PrototypeId costumeRef)
        {
            // See AvatarPrototype.ApprovedForUse() for why this method exists.
            CostumePrototype costumeProto = costumeRef.As<CostumePrototype>();
            return costumeProto != null && GameDatabase.DesignStateOk(costumeProto.DesignState);
        }

        public bool IsMemberOfSuperteam(PrototypeId superteamProtoRef)
        {
            if (!Verify.IsTrue(superteamProtoRef != PrototypeId.Invalid)) return false;
            return SuperteamMemberships.HasValue() && SuperteamMemberships.Contains(superteamProtoRef);
        }
    }

    public class ItemAssignmentPrototype : Prototype
    {
        public PrototypeId Item { get; protected set; }
        public PrototypeId Rarity { get; protected set; }
    }

    public class AvatarPrimaryStatPrototype : Prototype
    {
        public AvatarStat Stat { get; protected set; }
        public LocaleStringId Tooltip { get; protected set; }
    }

    public class IngredientLookupEntryPrototype : Prototype
    {
        public long LookupSlot { get; protected set; }
        public PrototypeId Ingredient { get; protected set; }
    }

    public class AvatarSynergyEntryPrototype : Prototype
    {
        public int Level { get; protected set; }
        public LocaleStringId TooltipTextForIcon { get; protected set; }
        public PrototypeId UIData { get; protected set; }
    }

    public class AvatarSynergyEvalEntryPrototype : AvatarSynergyEntryPrototype
    {
        public EvalPrototype SynergyEval { get; protected set; }
    }

    public class VanityTitlePrototype : Prototype
    {
        public LocaleStringId Text { get; protected set; }
    }

    public class PowerSpecPrototype : Prototype
    {
        public int Index { get; protected set; }
    }

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
    public class TalentEntryPrototype : Prototype
    {
        public PrototypeId Talent { get; protected set; }
        public int UnlockLevel { get; protected set; }
    }

    public class TalentGroupPrototype : Prototype
    {
        public TalentEntryPrototype[] Talents { get; protected set; }
        public float UIPositionPctX { get; protected set; }
        public float UIPositionPctY { get; protected set; }
    }
#endif

    public class AvatarModePrototype : Prototype
    {
        public AvatarMode AvatarModeEnum { get; protected set; }
        public InventoryConvenienceLabel Inventory { get; protected set; }
    }

    public class StatProgressionEntryPrototype : Prototype
    {
        public int Level { get; protected set; }
        public int DurabilityValue { get; protected set; }
        public int EnergyProjectionValue { get; protected set; }
        public int FightingSkillsValue { get; protected set; }
        public int IntelligenceValue { get; protected set; }
        public int SpeedValue { get; protected set; }
        public int StrengthValue { get; protected set; }

        //---

        public bool TryUpdateStats(PropertyCollection properties)
        {
            bool TryUpdateStatHelper(PropertyEnum statProperty, int statValue)
            {
                if (statValue > 0 && properties[statProperty] != statValue)
                {
                    properties[statProperty] = statValue;
                    return true;
                }

                return false;
            }

            bool statsChanged = false;
            statsChanged |= TryUpdateStatHelper(PropertyEnum.StatDurability, DurabilityValue);
            statsChanged |= TryUpdateStatHelper(PropertyEnum.StatStrength, StrengthValue);
            statsChanged |= TryUpdateStatHelper(PropertyEnum.StatFightingSkills, FightingSkillsValue);
            statsChanged |= TryUpdateStatHelper(PropertyEnum.StatSpeed, SpeedValue);
            statsChanged |= TryUpdateStatHelper(PropertyEnum.StatEnergyProjection, EnergyProjectionValue);
            statsChanged |= TryUpdateStatHelper(PropertyEnum.StatIntelligence, IntelligenceValue);
            return statsChanged;
        }
    }

    public class PowerProgressionEntryPrototype : ProgressionEntryPrototype
    {
        public int Level { get; protected set; }
        public AbilityAssignmentPrototype PowerAssignment { get; protected set; }
        public CurveId MaxRankForPowerAtCharacterLevel { get; protected set; }
        public PrototypeId[] Prerequisites { get; protected set; }
        public float UIPositionPctX { get; protected set; }
        public float UIPositionPctY { get; protected set; }
        public int UIFanSortNumber { get; protected set; }
        public int UIFanTier { get; protected set; }
        public PrototypeId[] Antirequisites { get; protected set; }
#if GAME_VERSION_1_52
        public bool IsTrait { get; protected set; }
#endif
#if GAME_VERSION_1_53
        public PrototypeId CostumeRequired { get; protected set; }
        public TraitCategory TraitCategory { get; protected set; }
        public bool TraitRequiresOmegaPrestige { get; protected set; }
#endif

        //---

        public override int GetRequiredLevel() => Level;
        public override int GetStartingRank() => PowerAssignment != null ? PowerAssignment.Rank : 0;

        public override CurveId GetMaxRankForPowerAtCharacterLevel() => MaxRankForPowerAtCharacterLevel;
        public override PrototypeId[] GetPrerequisites() => Prerequisites;
        public override PrototypeId[] GetAntirequisites() => Antirequisites;
    }

    public class PowerProgressionTablePrototype : Prototype
    {
        public LocaleStringId DisplayName { get; protected set; }
        public PowerProgressionEntryPrototype[] PowerProgressionEntries { get; protected set; }

        //---

        [DoNotCopy]
        public PrototypeId PowerProgTableTabRef { get; set; } = PrototypeId.Invalid;
    }

    public class PowerProgTableTabRefPrototype : Prototype
    {
        public int PowerProgTableTabIndex { get; protected set; }
    }

#if GAME_VERSION_1_53
    public class SlotUnlockPrototype : Prototype
    {
        public int UnlockLevel { get; protected set; }
        public PrototypeId Slot { get; protected set; }
    }
#endif

#if GAME_VERSION_1_53
    public class AvatarPowerGroupUIPrototype : Prototype
    {
        public AssetId IconPath { get; protected set; }
        public PowerKeywordPrototype PowerKeyword { get; protected set; }
    }
#endif
}
