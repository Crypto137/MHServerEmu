using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.Powers;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class AvatarPrototype : AgentPrototype
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public LocaleStringId BioText { get; protected set; }
        public AbilityAssignmentPrototype[] HiddenPassivePowers { get; protected set; }
        public AssetId PortraitPath { get; protected set; }
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
        public PrototypeId SecondaryResourceBehavior { get; protected set; }
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
        public PrototypeId[] PrimaryResourceBehaviors { get; protected set; }     // VectorPrototypeRefPtr PrimaryResourceManaBehaviorPrototype
        public PrototypeId[] StealablePowersAllowed { get; protected set; }       // VectorPrototypeRefPtr StealablePowerInfoPrototype
        public bool ShowInRosterIfLocked { get; protected set; }
        public LocaleStringId CharacterVideoUrl { get; protected set; }
        public AssetId CharacterSelectIconPortraitSmall { get; protected set; }
        public AssetId CharacterSelectIconPortraitFull { get; protected set; }
        public LocaleStringId PrimaryResourceBehaviorNames { get; protected set; }
        public bool IsStarterAvatar { get; protected set; }
        public int CharacterSelectDisplayOrder { get; protected set; }
        public PrototypeId CostumeCore { get; protected set; }
        public TalentGroupPrototype[] TalentGroups { get; protected set; }
        public PrototypeId TravelPower { get; protected set; }
        public AbilityAutoAssignmentSlotPrototype[] AbilityAutoAssignmentSlot { get; protected set; }
        public PrototypeId[] LoadingScreensConsole { get; protected set; }
        public ItemAssignmentPrototype StartingCostumePS4 { get; protected set; }
        public ItemAssignmentPrototype StartingCostumeXboxOne { get; protected set; }

        [DoNotCopy]
        public PrototypeId UltimatePowerRef { get; private set; } = PrototypeId.Invalid;

        [DoNotCopy]
        public int AvatarPrototypeEnumValue { get; private set; }

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
                    if (i >= 3) Logger.Warn("PostProcess(): PowerProgressionTables.Length >= 3");
                    switch (i)
                    {
                        case 0: powerProgTableProto.PowerProgTableTabRef = uiGlobals.PowerProgTableTabRefTab1; break;
                        case 1: powerProgTableProto.PowerProgTableTabRef = uiGlobals.PowerProgTableTabRefTab2; break;
                        case 2: powerProgTableProto.PowerProgTableTabRef = uiGlobals.PowerProgTableTabRefTab3; break;
                    }

                    // Find the ultimate power
                    foreach (PowerProgressionEntryPrototype entryProto in powerProgTableProto.PowerProgressionEntries)
                    {
                        PowerPrototype powerProto = entryProto.PowerAssignment.Ability.As<PowerPrototype>();

                        if (Power.IsUltimatePower(powerProto))
                        {
                            if (UltimatePowerRef != PrototypeId.Invalid) Logger.Warn($"PostProcess(): Avatar has more than one ultimate power defined ({this})");
                            UltimatePowerRef = entryProto.PowerAssignment.Ability;
                        }
                    }
                }
            }

            // TODO: SynergyTable
            // TODO: StealablePowersAllowed

            AvatarPrototypeEnumValue = GetEnumValueFromBlueprint(LiveTuningData.GetAvatarBlueprintDataRef());
        }

        /// <summary>
        /// Returns the <see cref="PrototypeId"/> of the starting costume for the specified platform.
        /// </summary>
        public PrototypeId GetStartingCostumeForPlatform(Platforms platform)
        {
            if (platform == Platforms.PS4 && StartingCostumePS4 != null)
                return StartingCostumePS4.Item;
            else if (platform == Platforms.XboxOne && StartingCostumeXboxOne != null)
                return StartingCostumeXboxOne.Item;

            if (StartingCostume == null)
                return Logger.WarnReturn(PrototypeId.Invalid, $"GetStartingCostumeForPlatform(): failed to get starting costume for {platform}");

            return StartingCostume.Item;
        }

        /// <summary>
        /// Retrieves <see cref="PowerProgressionEntryPrototype"/> instances for powers that would be unlocked at the specified level or level range.
        /// </summary>
        public IEnumerable<PowerProgressionEntryPrototype> GetPowersUnlockedAtLevel(int level = -1, bool retrieveForLevelRange = false, int startingLevel = -1)
        {
            if (PowerProgressionTables == null) yield break;

            foreach (PowerProgressionTablePrototype table in PowerProgressionTables)
            {
                if (table.PowerProgressionEntries == null) continue;

                foreach (PowerProgressionEntryPrototype entry in table.PowerProgressionEntries)
                {
                    if (entry.PowerAssignment == null) continue;
                    if (entry.PowerAssignment.Ability == PrototypeId.Invalid) continue;

                    bool match = true;

                    // If the specified level is set to -1 it means we need to include all levels.
                    match &= level < 0 || entry.Level <= level;

                    // retrieveForLevelRange means to retrieve all abilities that would be unlocked
                    // if you got from startingLevel to level. Otherwise retrieve just the abilities
                    // for the specified level.
                    match &= retrieveForLevelRange && entry.Level > startingLevel || entry.Level == level;

                    if (match) yield return entry;
                }
            }
        }

        /// <summary>
        /// Returns the <see cref="AbilityAutoAssignmentSlotPrototype"/> for the specified power <see cref="PrototypeId"/> if there is one.
        /// Otherwise, returns <see langword="null"/>.
        /// </summary>
        public AbilityAutoAssignmentSlotPrototype GetPowerInAbilityAutoAssignmentSlot(PrototypeId powerProtoId)
        {
            if (AbilityAutoAssignmentSlot == null) return null;

            foreach (var abilityAutoAssignmentSlot in AbilityAutoAssignmentSlot)
            {
                if (abilityAutoAssignmentSlot.Ability == powerProtoId)
                    return abilityAutoAssignmentSlot;
            }

            return null;
        }

        public PowerProgressionTablePrototype GetPowerProgressionTableAtIndex(int index)
        {
            if (PowerProgressionTables == null) return null;

            if (index < 0)
                return Logger.WarnReturn<PowerProgressionTablePrototype>(null, "GetPowerProgressionTableAtIndex(): index < 0");

            if (index >= PowerProgressionTables.Length)
                return Logger.WarnReturn<PowerProgressionTablePrototype>(null, "GetPowerProgressionTableAtIndex(): index >= PowerProgressionTables.Length");

            return PowerProgressionTables[index];
        }

        public int GetPowerProgressionTableIndexForPower(PrototypeId powerProtoRef)
        {
            if (PowerProgressionTables == null) return -1;

            int index = 0;

            foreach (PowerProgressionTablePrototype powerProgTableProto in PowerProgressionTables)
            {
                foreach (PowerProgressionEntryPrototype powerProgEntry in powerProgTableProto.PowerProgressionEntries)
                {
                    AbilityAssignmentPrototype abilityAssignmentProto = powerProgEntry.PowerAssignment;
                    if (abilityAssignmentProto?.Ability == powerProtoRef)
                        return index;
                }

                index++;
            }

            return -1;
        }

        public PrototypeId GetPowerProgressionTableTabRefForPower(PrototypeId powerProtoRef)
        {
            int tableIndex = GetPowerProgressionTableIndexForPower(powerProtoRef);
            if (tableIndex < 0) return PrototypeId.Invalid;

            PowerProgressionTablePrototype powerProgTableProto = GetPowerProgressionTableAtIndex(tableIndex);
            if (powerProgTableProto == null)
                return Logger.WarnReturn(PrototypeId.Invalid, "GetPowerProgressionTableTabRefForPower(): powerProgTableProto == null");

            return powerProgTableProto.PowerProgTableTabRef;
        }

        /// <summary>
        /// Returns <see langword="true"/> if the provided costume is approved for use.
        /// </summary>
        private bool CostumeApprovedForUse(PrototypeId costumeId)
        {
            // See AvatarPrototype.ApprovedForUse() for why this method exists.
            var costume = GameDatabase.GetPrototype<CostumePrototype>(costumeId);
            return costume != null && GameDatabase.DesignStateOk(costume.DesignState);
        }

        public bool IsMemberOfSuperteam(PrototypeId superteamProtoRef)
        {
            if (superteamProtoRef == PrototypeId.Invalid) return false;
            if (SuperteamMemberships != null)
                return SuperteamMemberships.Contains(superteamProtoRef);
            return false;
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
        public bool IsTrait { get; protected set; }

        public override int GetRequiredLevel() => Level;
        public override int GetStartingRank() => PowerAssignment != null ? PowerAssignment.StartingRank : 0;

        public override CurveId GetMaxRankForPowerAtCharacterLevel() => MaxRankForPowerAtCharacterLevel;
        public override PrototypeId[] GetPrerequisites() => Prerequisites;
        public override PrototypeId[] GetAntirequisites() => Antirequisites;
    }

    public class PowerProgressionTablePrototype : Prototype
    {
        public LocaleStringId DisplayName { get; protected set; }
        public PowerProgressionEntryPrototype[] PowerProgressionEntries { get; protected set; }

        [DoNotCopy]
        public PrototypeId PowerProgTableTabRef { get; set; } = PrototypeId.Invalid;
    }

    public class PowerProgTableTabRefPrototype : Prototype
    {
        public int PowerProgTableTabIndex { get; protected set; }
    }
}
