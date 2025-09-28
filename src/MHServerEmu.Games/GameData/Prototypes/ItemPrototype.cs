﻿using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.GameData.Tables;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)None)]
    public enum ItemInstrumentedDropGroup
    {
        None = 0,
        Character = 1,
        Costume = 2,
        RareArtifact = 3,
    }

    [AssetEnum((int)None)]
    public enum ItemEventType
    {
        None = 0,
        OnEquip = 1,
        OnUse = 2,
        OnUsePowerActivated = 3,
    }

    [AssetEnum((int)PickWeight)]
    public enum PickMethod
    {
        PickWeight = 0,
        PickWeightTryAll = 1,
        PickAll = 2,
    }

    [AssetEnum]
    public enum CharacterTokenType  // Entity/Items/CharacterTokens/TokenType.type
    {
        None = 0,
        UnlockCharacterOnly = 1,
        UnlockCharOrUpgradeUlt = 2,
        UpgradeUltimateOnly = 4,
    }

    #endregion

    public class ItemPrototype : WorldEntityPrototype
    {
        public bool IsUsable { get; protected set; }
        public bool CanBeSoldToVendor { get; protected set; }
        public int MaxVisiblePrefixes { get; protected set; }
        public int MaxVisibleSuffixes { get; protected set; }
        public LocaleStringId TooltipDescription { get; protected set; }
        public LocaleStringId TooltipFlavorText { get; protected set; }
        public PrototypeId TooltipTemplate { get; protected set; }
        public ItemStackSettingsPrototype StackSettings { get; protected set; }
        public bool AlwaysDisplayAsUsable { get; protected set; }
        public PrototypeId[] TooltipEquipRestrictions { get; protected set; }
        public AffixEntryPrototype[] AffixesBuiltIn { get; protected set; }
        public PropertyEntryPrototype[] PropertiesBuiltIn { get; protected set; }
        [Mixin]
        public ProductPrototype Product { get; protected set; }
        public LocaleStringId ItemCategory { get; protected set; }
        public LocaleStringId ItemSubcategory { get; protected set; }
        public bool IsAvatarRestricted { get; protected set; }
        public DropRestrictionPrototype[] LootDropRestrictions { get; protected set; }
        public ItemBindingSettingsPrototype BindingSettings { get; protected set; }
        public AffixLimitsPrototype[] AffixLimits { get; protected set; }
        public PrototypeId TextStyleOverride { get; protected set; }
        public ItemAbilitySettingsPrototype AbilitySettings { get; protected set; }
        public AssetId StoreIconPath { get; protected set; }
        public bool ClonedWhenPurchasedFromVendor { get; protected set; }
        public ItemActionSetPrototype ActionsTriggeredOnItemEvent { get; protected set; }
        public bool ConfirmOnDonate { get; protected set; }
        public bool CanBeDestroyed { get; protected set; }
        public bool ConfirmPurchase { get; protected set; }
        public ItemCostPrototype Cost { get; protected set; }
        public int TooltipDepthOverride { get; protected set; }
        public EquipRestrictionPrototype[] EquipRestrictions { get; protected set; }
        public EvalPrototype EvalExpirationTimeMS { get; protected set; }
        public ItemTooltipPropertyBlockSettingsPrototype[] TooltipCustomPropertyBlocks { get; protected set; }
        public float LootDropWeightMultiplier { get; protected set; }
        public InventoryConvenienceLabel DestinationFromVendor { get; protected set; }
        public EvalPrototype EvalDisplayLevel { get; protected set; }
        public bool CanBroadcast { get; protected set; }
        public EquipmentInvUISlot DefaultEquipmentSlot { get; protected set; }
        public EvalPrototype EvalCanUse { get; protected set; }
        public PrototypeId[] CannotEquipWithItemsOfKeyword { get; protected set; }
        public PrototypeId SortCategory { get; protected set; }
        public PrototypeId SortSubCategory { get; protected set; }
        public ItemInstrumentedDropGroup InstrumentedDropGroup { get; protected set; }

        // ---

        private const int PetTechAffixPositions = 5;

        private static readonly Logger Logger = LogManager.CreateLogger();

        [DoNotCopy]
        public bool IsPetItem { get => IsChildBlueprintOf(GameDatabase.GlobalsPrototype.PetItemBlueprint); }

        [DoNotCopy]
        public bool IsGem { get => IsChildBlueprintOf(GameDatabase.LootGlobalsPrototype.GemBlueprint); }

        public override PrototypeId GetPortalTarget()
        {
            if (ActionsTriggeredOnItemEvent == null || ActionsTriggeredOnItemEvent.Choices.IsNullOrEmpty())
                return PrototypeId.Invalid;

            foreach (ItemActionBasePrototype itemActionBaseProto in ActionsTriggeredOnItemEvent.Choices)
            {
                // Skip non-power actions
                if (itemActionBaseProto is not ItemActionUsePowerPrototype usePowerProto || usePowerProto.Power == PrototypeId.Invalid)
                    continue;

                // Skip non-summon powers
                SummonPowerPrototype summonPowerProto = usePowerProto.Power.As<SummonPowerPrototype>();
                if (summonPowerProto == null || summonPowerProto.SummonEntityContexts.IsNullOrEmpty())
                    continue;

                // Search for transitions in summon contexts for this summon power action
                foreach (SummonEntityContextPrototype summonContextProto in summonPowerProto.SummonEntityContexts)
                {
                    if (summonContextProto.SummonEntity == PrototypeId.Invalid)
                        continue;

                    // Skip summon contexts that do not summon a transition entity
                    TransitionPrototype transitionProto = summonContextProto.SummonEntity.As<TransitionPrototype>();
                    if (transitionProto == null || transitionProto.DirectTarget == PrototypeId.Invalid)
                        continue;

                    // Get region from transition target
                    RegionConnectionTargetPrototype connectionTargetProto = transitionProto.DirectTarget.As<RegionConnectionTargetPrototype>();
                    if (connectionTargetProto == null)
                        continue;

                    if (connectionTargetProto.Region == PrototypeId.Invalid)
                    {
                        Logger.Warn("GetPortalTarget(): connectionTargetProto.Region == PrototypeId.Invalid");
                        continue;
                    }

                    return connectionTargetProto.Region;
                }
            }

            return PrototypeId.Invalid;
        }

        public bool OnApplyItemSpec(Item item, ItemSpec itemSpec)
        {
            ItemPrototype itemProto = item.ItemPrototype;
            if (itemProto == null) return Logger.WarnReturn(false, "OnApplyItemSpec(): itemProto == null");

            if (itemProto.IsPetItem == false)
                return true;

            // Roll new pet tech affixes if there aren't enough of them
            int numPetTechAffixes = 0;

            IReadOnlyList<AffixSpec> affixSpecs = itemSpec.AffixSpecs;
            for (int i = 0; i < affixSpecs.Count; i++)
            {
                AffixSpec affixSpec = affixSpecs[i];
                if (affixSpec.AffixProto.IsPetTechAffix)
                    numPetTechAffixes++;
            }

            if (numPetTechAffixes < PetTechAffixPositions)
                UpdatePetTechAffixes(item.Game.Random, item.GetBoundAgentProtoRef(), itemSpec);

            return true;
        }

        #region PetTech

        public static MutationResults UpdatePetTechAffixes(GRandom random, PrototypeId rollFor, ItemSpec itemSpec)
        {
            // *slaps function declaration* this bad boy can fit so much pooling in it
            Dictionary<AffixPosition, AffixSpec> affixes = DictionaryPool<AffixPosition, AffixSpec>.Instance.Get();
            List<Picker<AffixPrototype>> affixPickers = ListPool<Picker<AffixPrototype>>.Instance.Get(PetTechAffixPositions);

            // Build pickers
            for (int i = 0; i < PetTechAffixPositions; i++)
            {
                affixPickers.Add(new(random));

                IReadOnlyList<AffixPrototype> affixPool = GameDataTables.Instance.LootPickingTable.GetAffixesByPosition(AffixPosition.PetTech1 + i);
                for (int j = 0; j < affixPool.Count; j++)
                {
                    AffixPrototype affixProto = affixPool[j];
                    affixPickers[i].Add(affixProto, affixProto.Weight);
                }
            }

            // Roll affixes
            MutationResults result = MutationResults.None;

            for (int i = 0; i < PetTechAffixPositions; i++)
            {
                AffixPosition position = AffixPosition.PetTech1 + i;

                bool hasAffix = false;

                IReadOnlyList<AffixSpec> affixSpecs = itemSpec.AffixSpecs;
                for (int j = 0; j < affixSpecs.Count; j++)
                {
                    AffixSpec affixIt = affixSpecs[j];
                    AffixPrototype affixProto = affixIt.AffixProto;

                    if (affixProto == null)
                    {
                        Logger.Warn("UpdatePetTechAffixes(): affixProto == null");
                        continue;
                    }

                    affixes[affixProto.Position] = affixIt;

                    if (affixProto.Position == position)
                    {
                        hasAffix = true;
                        break;
                    }
                }

                if (hasAffix == false)
                {
                    AffixSpec affixSpec = new();
                    affixes[position] = affixSpec;

                    HashSet<ScopedAffixRef> affixSet = HashSetPool<ScopedAffixRef>.Instance.Get();
                    result |= affixSpec.RollAffix(random, rollFor, itemSpec, affixPickers[i], affixSet);
                    HashSetPool<ScopedAffixRef>.Instance.Return(affixSet);

                    if (result.HasFlag(MutationResults.AffixChange) == false)
                        Logger.Warn("UpdatePetTechAffixes(): result.HasFlag(MutationResults.AffixChange) == false");
                }
            }

            // Overwrite affixes
            List<AffixSpec> newAffixSpecs = ListPool<AffixSpec>.Instance.Get();
            
            for (int i = 0; i < PetTechAffixPositions; i++)
                newAffixSpecs.Add(affixes[AffixPosition.PetTech1 + i]);

            itemSpec.SetAffixes(newAffixSpecs);

            ListPool<AffixSpec>.Instance.Return(newAffixSpecs);

            // Add metadata
            if (affixes.TryGetValue(AffixPosition.Metadata, out AffixSpec metadataAffix))
                itemSpec.AddAffixSpec(metadataAffix);

            DictionaryPool<AffixPosition, AffixSpec>.Instance.Return(affixes);
            ListPool<Picker<AffixPrototype>>.Instance.Return(affixPickers);
            return result;
        }

        public static MutationResults CopyPetTechAffixes(ItemSpec sourceItemSpec, ItemSpec destItemSpec, AffixPosition position)
        {
            ItemPrototype destItemProto = destItemSpec.ItemProtoRef.As<ItemPrototype>();
            if (destItemProto == null) return Logger.WarnReturn(MutationResults.Error, "CopyPetTechAffixes(): destItemProto == null");
            if (destItemProto.IsPetItem == false) return Logger.WarnReturn(MutationResults.Error, "CopyPetTechAffixes(): destItemProto.IsPetItem == false");

            if (position < AffixPosition.PetTech1 || position > AffixPosition.PetTech5) return Logger.WarnReturn(MutationResults.Error, "CopyPetTechAffixes(): position < AffixPosition.PetTech1 || position > AffixPosition.PetTech5");

            IReadOnlyList<AffixSpec> sourceAffixSpecs = sourceItemSpec.AffixSpecs;
            for (int i = 0; i < sourceAffixSpecs.Count; i++)
            {
                AffixSpec sourceAffixSpec = sourceAffixSpecs[i];
                AffixPrototype sourceAffixProto = sourceAffixSpec.AffixProto;
                if (sourceAffixProto == null)
                {
                    Logger.Warn("CopyPetTechAffixes(): sourceAffixProto == null");
                    continue;
                }

                if (sourceAffixProto.Position != position)
                    continue;

                // See if we can replace an existing affix
                bool replaced = false;

                List<AffixSpec> destAffixSpecs = ListPool<AffixSpec>.Instance.Get(destItemSpec.AffixSpecs);
                for (int j = 0; j < destAffixSpecs.Count; j++)
                {
                    AffixSpec destAffixSpec = destAffixSpecs[j];
                    AffixPrototype destAffixProto = destAffixSpec.AffixProto;
                    if (destAffixProto == null)
                    {
                        Logger.Warn("CopyPetTechAffixes(): destAffixProto == null");
                        continue;
                    }

                    if (destAffixProto.Position != position)
                        continue;

                    destAffixSpecs[j] = new(sourceAffixSpec);
                    replaced = true;
                }

                // If there is no existing affix in the specified slot, append it
                if (replaced == false)
                    destAffixSpecs.Add(new(sourceAffixSpec));

                // Update affixes on the destination spec
                destItemSpec.SetAffixes(destAffixSpecs);

                ListPool<AffixSpec>.Instance.Return(destAffixSpecs);
                return MutationResults.AffixChange;
            }

            return MutationResults.None;
        }

        public static bool IsPetTechAffixUnlocked(Item petTechItem, AffixPosition affixPos)
        {
            if (petTechItem.IsPetItem == false) return Logger.WarnReturn(false, "IsPetTechAffixUnlocked(): petTechItem.IsPetItem == false");

            AdvancementGlobalsPrototype advGlobalsProto = GameDatabase.AdvancementGlobalsPrototype;

            PetTechAffixInfoPrototype petTechAffixInfoProto = advGlobalsProto.GetPetTechAffixInfoPrototype(affixPos);
            if (petTechAffixInfoProto == null) return Logger.WarnReturn(false, "IsPetTechAffixUnlocked(): petTechAffixInfoProto == null");

            int donationCount = petTechItem.Properties[PropertyEnum.PetItemDonationCount, (int)affixPos];
            return donationCount >= petTechAffixInfoProto.ItemsRequiredToUnlock;
        }

        public static bool IsPetTechAffixUnlocked(Item petTechItem, PropertyId propertyId)
        {
            if (petTechItem.IsPetItem == false) return Logger.WarnReturn(false, "IsPetTechAffixUnlocked(): petTechItem.IsPetItem == false");

            AdvancementGlobalsPrototype advGlobalsProto = GameDatabase.AdvancementGlobalsPrototype;

            Property.FromParam(propertyId, 0, out int affixPosValue);
            AffixPosition affixPos = (AffixPosition)affixPosValue;
            if (affixPos < AffixPosition.PetTech1 || affixPos > AffixPosition.PetTech5) return Logger.WarnReturn(false, "IsPetTechAffixUnlocked(): affixPos < AffixPosition.PetTech1 || affixPos > AffixPosition.PetTech5");

            PetTechAffixInfoPrototype petTechAffixInfoProto = advGlobalsProto.GetPetTechAffixInfoPrototype(affixPos);
            if (petTechAffixInfoProto == null) return Logger.WarnReturn(false, "IsPetTechAffixUnlocked(): petTechAffixInfoProto == null");

            int donationCount = petTechItem.Properties[PropertyEnum.PetItemDonationCount, (int)affixPos];
            return donationCount >= petTechAffixInfoProto.ItemsRequiredToUnlock;
        }

        public static bool CanDonateItemToPetTech(Item petTechItem, ItemSpec itemSpecToDonate, Item itemToDonate, out AffixPosition availableAffixPosition)
        {
            availableAffixPosition = AffixPosition.None;

            if (petTechItem.IsPetItem == false) return Logger.WarnReturn(false, "CanDonateItemToPetTech(): petTechItem.IsPetItem == false");
            if (itemSpecToDonate == null) return Logger.WarnReturn(false, "CanDonateItemToPetTech(): itemSpecToDonate == null");
            if (itemSpecToDonate.ItemProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "CanDonateItemToPetTech(): itemSpecToDonate.ItemProtoRef == PrototypeId.Invalid");

            // Validate the item entity if we have one (i.e. there is no item entity in vaporization)
            if (itemToDonate != null)
            {
                if (itemToDonate.CurrentStackSize > 1)
                    return false;

                if (itemToDonate.IsInWorld)
                {
                    if (itemToDonate.GetOwner() != null)
                        return Logger.WarnReturn(false, "CanDonateItemToPetTech(): An item on the ground cannot be in any inventory!");
                }
                else
                {
                    switch (itemToDonate.InventoryLocation.InventoryCategory)
                    {
                        // Only items in the player general inventory and stash can be donated
                        case InventoryCategory.PlayerGeneral:
                        case InventoryCategory.PlayerGeneralExtra:
                        case InventoryCategory.PlayerStashAvatarSpecific:
                        case InventoryCategory.PlayerStashGeneral:
                            break;

                        default:
                            return false;
                    }
                }
            }

            AdvancementGlobalsPrototype advGlobalsProto = GameDatabase.AdvancementGlobalsPrototype;

            PrototypeId rarityProtoRef = itemSpecToDonate.RarityProtoRef;

            if (rarityProtoRef == PrototypeId.Invalid)
                return Logger.WarnReturn(false, $"CanDonateItemToPetTech(): Item without a rarity! \nItem:{itemSpecToDonate}");

            // Item must be flagged with PetTechDonationItem blueprint to be donatable
            DataDirectory dataDirectory = DataDirectory.Instance;
            BlueprintId petTechDonationItemBlueprint = dataDirectory.GetPrototypeBlueprintDataRef(advGlobalsProto.PetTechDonationItemPrototype);
            if (dataDirectory.PrototypeIsChildOfBlueprint(itemSpecToDonate.ItemProtoRef, petTechDonationItemBlueprint) == false)
                return false;

            // Look for available affix position (this is a bit of a mess)
            bool result = false;

            for (AffixPosition positionIt = AffixPosition.PetTech5; positionIt >= AffixPosition.PetTech1; positionIt--)
            {
                PetTechAffixInfoPrototype rarityMatchedAffixInfoProto = advGlobalsProto.GetPetTechAffixInfoPrototype(positionIt);
                if (rarityMatchedAffixInfoProto == null) return Logger.WarnReturn(false, "CanDonateItemToPetTech(): rarityMatchedAffixInfoProto == null");

                if (rarityMatchedAffixInfoProto.ItemRarityToConsume != rarityProtoRef)
                    continue;

                if (petTechItem.HasAffixInPosition(positionIt) == false)
                    continue;

                for (AffixPosition subPositionIt = positionIt; subPositionIt >= AffixPosition.PetTech1; subPositionIt--)
                {
                    rarityMatchedAffixInfoProto = advGlobalsProto.GetPetTechAffixInfoPrototype(subPositionIt);
                    if (rarityMatchedAffixInfoProto == null) return Logger.WarnReturn(false, "CanDonateItemToPetTech(): rarityMatchedAffixInfoProto == null");

                    int donationCount = petTechItem.Properties[PropertyEnum.PetItemDonationCount, (int)subPositionIt];
                    if (donationCount < rarityMatchedAffixInfoProto.ItemsRequiredToUnlock && petTechItem.HasAffixInPosition(subPositionIt))
                    {
                        availableAffixPosition = subPositionIt;
                        result = true;
                        break;
                    }
                }

                break;
            }

            return result;
        }

        public static bool DonateItemToPetTech(Player player, Item petTechItem, ItemSpec itemSpecToDonate, Item itemToDonate = null)
        {
            // Donations can come from three sources:
            // - Individual donations in the inventory panel communicated via NetMessageRequestPetTechDonate.
            // - AoE donations of items on the ground via the vacuum power.
            // - Vaporization, which happens automatically before the item is fully generated. In this case we are going to have only an ItemSpec and not a full entity.

            // Validate
            if (petTechItem.IsPetItem == false) return Logger.WarnReturn(false, "DonateItemToPetTech(): petTechItem.IsPetItem == false");
            if (itemToDonate != null && itemToDonate.ItemSpec != itemSpecToDonate) return Logger.WarnReturn(false, "DonateItemToPetTech(): itemToDonate != null && itemToDonate.ItemSpec != itemSpecToDonate");

            if (CanDonateItemToPetTech(petTechItem, itemSpecToDonate, itemToDonate, out AffixPosition availableAffixPosition) == false)
                return false;

            // Do the donation by adjusting the property
            petTechItem.Properties.AdjustProperty(1, new(PropertyEnum.PetItemDonationCount, (PropertyParam)availableAffixPosition));

            // Destroy the donated item (if we have one)
            itemToDonate?.Destroy();

            // Trigger ItemDonated event
            ItemPrototype itemProto = itemSpecToDonate.ItemProtoRef.As<ItemPrototype>();
            RarityPrototype rarityProto = itemSpecToDonate.RarityProtoRef.As<RarityPrototype>();
            player.OnScoringEvent(new(ScoringEventType.ItemDonated, itemProto, rarityProto, itemSpecToDonate.StackCount));

            return true;
        }

        #endregion

        public TimeSpan GetExpirationTime(PrototypeId rarityProtoRef)
        {
            if (EvalExpirationTimeMS == null) return Logger.WarnReturn(TimeSpan.Zero, "GetExpirationTime(): EvalExpirationTimeMS == null");

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_ProtoRef(EvalContext.Var1, rarityProtoRef);

            int expirationTimeMS = Eval.RunInt(EvalExpirationTimeMS, evalContext);
            return TimeSpan.FromMilliseconds(expirationTimeMS);
        }

        public virtual bool IsUsableByAgent(AgentPrototype agentProto)
        {
            if (EquipRestrictions.IsNullOrEmpty())
                return true;

            foreach (EquipRestrictionPrototype equipRestrictionProto in EquipRestrictions)
            {
                if (equipRestrictionProto.IsEquippableByAgent(agentProto) == false)
                    return false;
            }

            return true;
        }

        public virtual bool IsDroppableForAgent(AgentPrototype agentProto)
        {
            return IsUsableByAgent(agentProto);
        }

        /// <summary>
        /// Returns <see langword="true"/> if the provided <see cref="DropFilterArguments"/> passes the specified <see cref="RestrictionTestFlags"/>
        /// for this <see cref="ItemPrototype"/>'s restrictions.
        /// </summary>
        public bool IsDroppableForRestrictions(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags)
        {
            if (LootDropRestrictions.IsNullOrEmpty())
                return true;

            foreach (DropRestrictionPrototype dropRestrictionProto in LootDropRestrictions)
            {
                if (dropRestrictionProto.Allow(filterArgs, restrictionFlags) == false)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Adjusts the provided <see cref="DropFilterArguments"/> to pass the specified <see cref="RestrictionTestFlags"/>
        /// for this <see cref="ItemPrototype"/>'s restrictions.
        /// </summary>
        public bool MakeRestrictionsDroppable(DropFilterArguments filterArgs, RestrictionTestFlags flagsToAdjust, out RestrictionTestFlags adjustResultFlags)
        {
            adjustResultFlags = RestrictionTestFlags.None;

            if (LootDropRestrictions.IsNullOrEmpty())
                return true;

            foreach (DropRestrictionPrototype dropRestrictionProto in LootDropRestrictions)
            {
                if (dropRestrictionProto.Adjust(filterArgs, ref adjustResultFlags, flagsToAdjust) == false)
                {
                    adjustResultFlags = RestrictionTestFlags.None;
                    return false;
                }
            }

            return true;
        }

        public EquipmentInvUISlot GetInventorySlotForAgent(AgentPrototype agentProto)
        {
            if (agentProto is not AvatarPrototype avatarProto)
                return DefaultEquipmentSlot;

            return GameDataTables.Instance.EquipmentSlotTable.EquipmentUISlotForAvatar(this, avatarProto);
        }

        public virtual PrototypeId GetRollForAgent(PrototypeId rollForAvatar, AgentPrototype rollForTeamUp)
        {
            return rollForAvatar;
        }

        public AffixLimitsPrototype GetAffixLimits(PrototypeId rarityProtoRef, LootContext lootContext)
        {
            if (AffixLimits.IsNullOrEmpty())
                return null;

            foreach (AffixLimitsPrototype limits in AffixLimits)
            {
                if (limits.Matches(rarityProtoRef, lootContext))
                    return limits;
            }

            return null;
        }

        public int GetRank(LootContext lootContext)
        {
            if (LootDropRestrictions.IsNullOrEmpty())
                return 0;

            using DropFilterArguments args = ObjectPoolManager.Instance.Get<DropFilterArguments>();
            DropFilterArguments.Initialize(args, lootContext);

            RestrictionTestFlags flags = RestrictionTestFlags.None;

            foreach (DropRestrictionPrototype dropRestrictionProto in LootDropRestrictions)
                dropRestrictionProto.Adjust(args, ref flags, RestrictionTestFlags.Rank);

            return args.Rank;
        }

        public bool GenerateBuiltInAffixDetails(ItemSpec itemSpec, List<BuiltInAffixDetails> detailsList)
        {
            List<AffixEntryPrototype> affixEntryList = ListPool<AffixEntryPrototype>.Instance.Get();
            if (GetBuiltInAffixEntries(affixEntryList, itemSpec.RarityProtoRef))
            {
                Dictionary<ulong, int> affixSeedDict = DictionaryPool<ulong, int>.Instance.Get();

                foreach (AffixEntryPrototype affixEntryProto in affixEntryList)
                {
                    if (affixEntryProto == null || affixEntryProto.Affix == PrototypeId.Invalid)
                    {
                        Logger.Warn("affixEntryProto == null || affixEntryProto.Affix == PrototypeId.Invalid");
                        continue;
                    }

                    BuiltInAffixDetails builtInAffixDetails = new(affixEntryProto);

                    if (GeneratePowerModifierRefFromBuiltInAffix(affixEntryProto, itemSpec, ref builtInAffixDetails) == false)
                        continue;

                    builtInAffixDetails.Seed = GenAffixRandomSeed(affixSeedDict, itemSpec.Seed, itemSpec.ItemProtoRef, affixEntryProto.Affix, affixEntryProto.Power);

                    detailsList.Add(builtInAffixDetails);
                }

                DictionaryPool<ulong, int>.Instance.Return(affixSeedDict);
            }

            ListPool<AffixEntryPrototype>.Instance.Return(affixEntryList);
            return detailsList.Count > 0;
        }

        public bool GetBuiltInAffixEntries(List<AffixEntryPrototype> entryList, PrototypeId rarityProtoRef)
        {
            // This is an Item:: static function in the client, but it makes more sense for it to be here

            if (rarityProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "GetBuiltInAffixEntries(): rarityProtoRef == PrototypeId.Invalid");

            RarityPrototype rarityProto = rarityProtoRef.As<RarityPrototype>();
            if (rarityProto == null) return Logger.WarnReturn(false, "GetBuiltInAffixEntries(): rarityProto == null");

            if (AffixesBuiltIn.HasValue())
            {
                foreach (AffixEntryPrototype affixEntryProto in AffixesBuiltIn)
                    entryList.Add(affixEntryProto);
            }

            if (rarityProto.AffixesBuiltIn.HasValue())
            {
                foreach (AffixEntryPrototype affixEntryProto in rarityProto.AffixesBuiltIn)
                    entryList.Add(affixEntryProto);
            }

            return entryList.Count > 0;
        }

        public PrototypeId GetOnUsePower()
        {
            return GetTriggeredPower(ActionsTriggeredOnItemEvent, ItemEventType.OnUse, ItemActionType.UsePower);
        }

        public static bool AvatarUsesEquipmentType(ItemPrototype itemProto, AgentPrototype agentProto)
        {
            if (agentProto == null)
                return true;

            if (agentProto is not AvatarPrototype avatarProto)
                return false;

            return GameDataTables.Instance.EquipmentSlotTable.EquipmentUISlotForAvatar(itemProto, avatarProto) != EquipmentInvUISlot.Invalid;
        }

        private bool IsChildBlueprintOf(PrototypeId protoRef)
        {
            BlueprintId blueprintRef = DataDirectory.Instance.GetPrototypeBlueprintDataRef(protoRef);
            return DataDirectory.Instance.PrototypeIsChildOfBlueprint(DataRef, blueprintRef);
        }

        private static bool GeneratePowerModifierRefFromBuiltInAffix(AffixEntryPrototype affixEntryProto, ItemSpec itemSpec, ref BuiltInAffixDetails builtInAffixDetails)
        {
            if (affixEntryProto.Affix == PrototypeId.Invalid)
                return Logger.WarnReturn(false, "GeneratePowerModifierRefFromBuiltInAffix(): affixEntryProto.Affix == PrototypeId.Invalid");

            builtInAffixDetails.LevelRequirement = affixEntryProto.LevelRequirement;
            if (builtInAffixDetails.LevelRequirement < 0)
            {
                return Logger.WarnReturn(false, "GeneratePowerModifierRefFromBuiltInAffix(): Could not add a builtin Affix with a level requirement " +
                    $"to an Item because of data errors: Item=[{itemSpec.ItemProtoRef.GetName()}], Affix=[{affixEntryProto.Affix.GetName()}]");
            }

            AffixPowerModifierPrototype affixPowerModifierProto = affixEntryProto.Affix.As<AffixPowerModifierPrototype>();
            if (affixPowerModifierProto != null)
            {
                builtInAffixDetails.AvatarProtoRef = itemSpec.EquippableBy != PrototypeId.Invalid
                    ? itemSpec.EquippableBy
                    : affixEntryProto.Avatar;

                if (affixEntryProto.Avatar != PrototypeId.Invalid && affixEntryProto.Avatar != itemSpec.EquippableBy)
                {
                    return Logger.WarnReturn(false, string.Format("GeneratePowerModifierRefFromBuiltInAffix(): An item has an ItemSpec.equippableBy that is different " +
                        "than one of its built-in Affix entry Avatar settings!\nItem: {0}\nAffix: {1}\nEquippableBy: {2}\nRequired Avatar for Affix: {3}",
                        itemSpec.ItemProtoRef.GetName(),
                        affixEntryProto.Affix.GetName(),
                        itemSpec.EquippableBy.GetName(),
                        affixEntryProto.Avatar.GetName()));
                }

                if (affixPowerModifierProto.IsForSinglePowerOnly)
                {
                    builtInAffixDetails.ScopeProtoRef = affixEntryProto.Power;
                }
                else if (affixPowerModifierProto.PowerKeywordFilter != PrototypeId.Invalid)
                {
                    builtInAffixDetails.ScopeProtoRef = PrototypeId.Invalid;
                }
                else if (affixPowerModifierProto.PowerProgTableTabRef != PrototypeId.Invalid)
                {
                    builtInAffixDetails.ScopeProtoRef = builtInAffixDetails.AvatarProtoRef;
                }
            }

            return true;
        }

        private static int GenAffixRandomSeed(Dictionary<ulong, int> affixSeedDict, int itemSeed, PrototypeId itemProtoRef, PrototypeId affixProtoRef, PrototypeId scopeProtoRef)
        {
            ulong affixSeed = (ulong)GameDatabase.GetPrototypeGuid(itemProtoRef);
            affixSeed ^= (ulong)GameDatabase.GetPrototypeGuid(affixProtoRef);

            if (scopeProtoRef != PrototypeId.Invalid)
                affixSeed ^= (ulong)GameDatabase.GetPrototypeGuid(scopeProtoRef);

            affixSeedDict.TryGetValue(affixSeed, out int count);
            affixSeedDict[affixSeed] = count + 1;

            affixSeed = (affixSeed >> count) & 0xFFFFFFFF;

            return itemSeed ^ (int)affixSeed;
        }

        private static PrototypeId GetTriggeredPower(ItemActionSetPrototype actionSetProto, ItemEventType eventType, ItemActionType actionType)
        {
            if (actionSetProto == null || actionSetProto.Choices.IsNullOrEmpty())
                return PrototypeId.Invalid;

            foreach (ItemActionBasePrototype actionBaseProto in actionSetProto.Choices)
            {
                if (actionBaseProto is not ItemActionPrototype actionProto)
                {
                    Logger.Warn("GetTriggeredPower(): actionBaseProto is not ItemActionPrototype actionProto");
                    continue;
                }

                if (actionProto.TriggeringEvent != eventType)
                    continue;

                if (actionType == ItemActionType.AssignPower && actionProto is ItemActionAssignPowerPrototype assignPowerActionProto)
                    return assignPowerActionProto.Power;
                else if (actionType == ItemActionType.UsePower && actionProto is ItemActionUsePowerPrototype usePowerActionProto)
                    return usePowerActionProto.Power;
            }

            return PrototypeId.Invalid;
        }
    }

    public class ItemAbilitySettingsPrototype : Prototype
    {
        public AbilitySlotRestrictionPrototype AbilitySlotRestriction { get; protected set; }
        public bool OnlySlottableWhileEquipped { get; protected set; }
    }

    public class ItemBindingSettingsEntryPrototype : Prototype
    {
        public bool BindsToAccountOnPickup { get; protected set; }
        public bool BindsToCharacterOnEquip { get; protected set; }
        public bool IsTradable { get; protected set; }
        public PrototypeId RarityFilter { get; protected set; }
    }

    public class ItemBindingSettingsPrototype : Prototype
    {
        public ItemBindingSettingsEntryPrototype DefaultSettings { get; protected set; }
        public ItemBindingSettingsEntryPrototype[] PerRaritySettings { get; protected set; }
    }

    public class ItemStackSettingsPrototype : Prototype
    {
        public int ItemLevelOverride { get; protected set; }
        public int MaxStacks { get; protected set; }
        public int RequiredCharLevelOverride { get; protected set; }
        public bool AutoStackWhenAddedToInventory { get; protected set; }
        public bool StacksCanBeSplit { get; protected set; }
    }

    public class ItemActionBasePrototype : Prototype
    {
        public int Weight { get; protected set; }
    }

    public class ItemActionPrototype : ItemActionBasePrototype
    {
        public ItemEventType TriggeringEvent { get; protected set; }

        //---

        public virtual ItemActionType ActionType { get => ItemActionType.None; }
    }

    public class ItemActionAssignPowerPrototype : ItemActionPrototype
    {
        public PrototypeId Power { get; protected set; }

        //---

        public override ItemActionType ActionType { get => ItemActionType.AssignPower; }
    }

    public class ItemActionDestroySelfPrototype : ItemActionPrototype
    {
        //---

        public override ItemActionType ActionType { get => ItemActionType.DestroySelf; }
    }

    public class ItemActionGuildsUnlockPrototype : ItemActionPrototype
    {
        //---

        public override ItemActionType ActionType { get => ItemActionType.GuildUnlock; }
    }

    public class ItemActionReplaceSelfItemPrototype : ItemActionPrototype
    {
        public PrototypeId Item { get; protected set; }

        //---

        public override ItemActionType ActionType { get => ItemActionType.ReplaceSelfItem; }
    }

    public class ItemActionReplaceSelfLootTablePrototype : ItemActionPrototype
    {
        public LootTablePrototype LootTable { get; protected set; }
        public bool UseCurrentAvatarLevelForRoll { get; protected set; }

        //---

        public override ItemActionType ActionType { get => ItemActionType.ReplaceSelfLootTable; }
    }

    public class ItemActionSaveDangerRoomScenarioPrototype : ItemActionPrototype
    {
        //---

        public override ItemActionType ActionType { get => ItemActionType.SaveDangerRoomScenario; }
    }

    public class ItemActionRespecPrototype : ItemActionPrototype
    {
        //---

        public override ItemActionType ActionType { get => ItemActionType.Respec; }
    }

    public class ItemActionResetMissionsPrototype : ItemActionPrototype
    {
        //---

        public override ItemActionType ActionType { get => ItemActionType.ResetMissions; }
    }

    public class ItemActionPrestigeModePrototype : ItemActionPrototype
    {
        //---

        public override ItemActionType ActionType { get => ItemActionType.PrestigeMode; }
    }

    public class ItemActionUsePowerPrototype : ItemActionPrototype
    {
        public PrototypeId Power { get; protected set; }

        //---

        public override ItemActionType ActionType { get => ItemActionType.UsePower; }
    }

    public class ItemActionAwardTeamUpXPPrototype : ItemActionPrototype
    {
        public int XP { get; protected set; }

        //---

        public override ItemActionType ActionType { get => ItemActionType.AwardTeamUpXP; }
    }

    public class ItemActionSetPrototype : ItemActionBasePrototype
    {
        public ItemActionBasePrototype[] Choices { get; protected set; }
        public PickMethod PickMethod { get; protected set; }
    }

    public class ItemActionOpenUIPanelPrototype : ItemActionPrototype
    {
        public AssetId PanelName { get; protected set; }

        //---

        public override ItemActionType ActionType { get => ItemActionType.OpenUIPanel; }
    }

    public class AffixLimitsPrototype : Prototype
    {
        public LootContext[] AllowedContexts { get; protected set; }
        public PrototypeId ItemRarity { get; protected set; }
        public short MaxPrefixPlusSuffixTotal { get; protected set; }   // V48
        public short MaxPrefixes { get; protected set; }
        public short MaxSuffixes { get; protected set; }
        public short MinPrefixPlusSuffixTotal { get; protected set; }   // V48
        public short MinPrefixes { get; protected set; }
        public short MinSuffixes { get; protected set; }
        public short NumCosmics { get; protected set; }
        public short MaxBlessings { get; protected set; }
        public short NumUltimates { get; protected set; }
        public short MaxRunewords { get; protected set; }
        public short MinTeamUps { get; protected set; }
        public short MaxTeamUps { get; protected set; }
        public short MinUniques { get; protected set; }
        public short MaxUniques { get; protected set; }
        public short RegionAffixMax { get; protected set; }
        public short RegionAffixMin { get; protected set; }
        public short NumSocket1 { get; protected set; }
        public short NumSocket2 { get; protected set; }
        public short NumSocket3 { get; protected set; }
        public int RegionDifficultyIndex { get; protected set; }
        public float DamageRegionMobToPlayer { get; protected set; }
        public float DamageRegionPlayerToMob { get; protected set; }

        // ---

        private static readonly Logger Logger = LogManager.CreateLogger();

        private LootContext _lootContextFlags;

        public override void PostProcess()
        {
            base.PostProcess();

            short zero = 0;     // Use a variable to avoid casting all the time

            MaxPrefixes = Math.Max(MaxPrefixes, zero);
            MinPrefixes = Math.Clamp(MinPrefixes, zero, MaxPrefixes);
            MaxSuffixes = Math.Max(MaxSuffixes, zero);
            MinSuffixes = Math.Clamp(MinSuffixes, zero, MaxSuffixes);
            MaxTeamUps = Math.Max(MaxTeamUps, zero);
            MinTeamUps = Math.Clamp(MinTeamUps, zero, MaxTeamUps);

            if (AllowedContexts.HasValue())
            {
                foreach (LootContext lootContext in AllowedContexts)
                    _lootContextFlags |= lootContext;
            }
        }

        public bool Matches(PrototypeId rarityProtoRef, LootContext lootContext)
        {
            return ItemRarity == rarityProtoRef && _lootContextFlags.HasFlag(lootContext);
        }

        public short GetMin(AffixPosition affixPosition, LootRollSettings settings)
        {
            return GetLimit(affixPosition, false, settings);
        }

        public short GetMax(AffixPosition affixPosition, LootRollSettings settings)
        {
            return GetLimit(affixPosition, true, settings);
        }

        public short GetLimit(AffixPosition affixPosition, bool getMax, LootRollSettings settings)
        {
            short limit = 0;

            switch (affixPosition)
            {
                case AffixPosition.Prefix:      limit = getMax ? MaxPrefixes : MinPrefixes; break;
                case AffixPosition.Suffix:      limit = getMax ? MaxSuffixes : MinSuffixes; break;
                case AffixPosition.Ultimate:    limit = NumUltimates; break;
                case AffixPosition.Cosmic:      limit = NumCosmics; break;
                case AffixPosition.Unique:      limit = getMax ? MaxUniques : MinUniques; break;
                case AffixPosition.Blessing:    if (getMax) limit = MaxBlessings; break;
                case AffixPosition.Runeword:    if (getMax) limit = MaxRunewords; break;
                case AffixPosition.TeamUp:      limit = getMax ? MaxTeamUps : MinTeamUps; break;
                case AffixPosition.RegionAffix: limit = getMax ? RegionAffixMax : RegionAffixMin; break;
                case AffixPosition.Socket1:     limit = NumSocket1; break;
                case AffixPosition.Socket2:     limit = NumSocket2; break;
                case AffixPosition.Socket3:     limit = NumSocket3; break;

                case AffixPosition.None:
                case AffixPosition.Visual:
                case AffixPosition.Metadata:
                case AffixPosition.PetTech1:
                case AffixPosition.PetTech2:
                case AffixPosition.PetTech3:
                case AffixPosition.PetTech4:
                case AffixPosition.PetTech5:    break;  // Keep limit at 0

                default:
                    return Logger.WarnReturn<short>(0, $"GetLimit(): Unsupported AffixPosition [{affixPosition}]");
            }

            if (settings != null)
            {
                if (getMax)
                {
                    if (settings.AffixLimitMaxByPositionModifierDict.TryGetValue(affixPosition, out short maxMod))
                        limit += maxMod;
                }
                else
                {
                    if (settings.AffixLimitMinByPositionModifierDict.TryGetValue(affixPosition, out short minMod))
                        limit += minMod;
                }
            }

            // Do not allow min to be over max
            if (getMax == false)
                limit = Math.Min(limit, GetMax(affixPosition, settings));

            // Do not allow min or max to be negative
            return Math.Max(limit, (short)0);
        }
    }

    public class EquipRestrictionPrototype : Prototype
    {
        public virtual bool IsEquippableByAgent(AgentPrototype agentProto)
        {
            return true;
        }
    }

    public class EquipRestrictionSuperteamPrototype : EquipRestrictionPrototype
    {
        public PrototypeId SuperteamEquippableBy { get; protected set; }

        public override bool IsEquippableByAgent(AgentPrototype agentProto)
        {
            if (base.IsEquippableByAgent(agentProto) == false)
                return false;

            if (agentProto is not AvatarPrototype avatarProto)
                return true;

            return avatarProto.IsMemberOfSuperteam(SuperteamEquippableBy);
        }
    }

    public class EquipRestrictionAgentPrototype : EquipRestrictionPrototype
    {
        public PrototypeId Agent { get; protected set; }

        public override bool IsEquippableByAgent(AgentPrototype agentProto)
        {
            if (base.IsEquippableByAgent(agentProto) == false)
                return false;

            if (agentProto == null || Agent == PrototypeId.Invalid)
                return true;

            return agentProto.DataRef == Agent;
        }
    }

    public class ItemTooltipPropertyBlockSettingsPrototype : Prototype
    {
        public PrototypeId[] IncludeAllButProperties { get; protected set; }
        public PrototypeId[] IncludeOnlyProperties { get; protected set; }
        public bool UseBuiltinPropertyOrdering { get; protected set; }
    }

    public class LimitedEditionPrototype : Prototype
    {
    }

    public class ArmorPrototype : ItemPrototype
    {
        public override bool IsUsableByAgent(AgentPrototype agentProto)
        {
            return AvatarUsesEquipmentType(this, agentProto);
        }
    }

    public class ArtifactPrototype : ItemPrototype
    {
    }

    public class BagItemPrototype : ItemPrototype
    {
        public bool AllowsPlayerAdds { get; protected set; }
    }

    public class CharacterTokenPrototype : ItemPrototype
    {
        public PrototypeId Character { get; protected set; }
        public CharacterTokenType TokenType { get; protected set; }

        //--

        private static readonly Logger Logger = LogManager.CreateLogger();

        [DoNotCopy]
        public bool GrantsCharacterUnlock { get => TokenType == CharacterTokenType.UnlockCharacterOnly || TokenType == CharacterTokenType.UnlockCharOrUpgradeUlt; }
        [DoNotCopy]
        public bool GrantsUltimateUpgrade { get => TokenType == CharacterTokenType.UpgradeUltimateOnly || TokenType == CharacterTokenType.UnlockCharOrUpgradeUlt; }

        [DoNotCopy]
        public bool IsForAvatar { get => Character.As<AvatarPrototype>() != null; }
        [DoNotCopy]
        public bool IsForTeamUp { get => Character.As<AgentTeamUpPrototype>() != null; }

        public override bool ApprovedForUse()
        {
            if (base.ApprovedForUse() == false)
                return false;

            AgentPrototype agentProto = Character.As<AgentPrototype>();
            return agentProto?.ApprovedForUse() == true;
        }

        public override bool IsLiveTuningEnabled()
        {
            if (base.IsLiveTuningEnabled() == false)
                return false;

            return IsLiveTuningEnabled(Character);
        }

        public bool HasUnlockedCharacter(Player player)
        {
            Prototype characterProto = Character.As<Prototype>();
            if (characterProto == null) return Logger.WarnReturn(false, "HasUnlockedCharacter(): characterProto == null");

            if (characterProto is AvatarPrototype)
                return player.HasAvatarFullyUnlocked(Character);

            return player.IsTeamUpAgentUnlocked(Character);
        }

    }

    public class InventoryStashTokenPrototype : ItemPrototype
    {
        public PrototypeId Inventory { get; protected set; }
    }

    public class EmoteTokenPrototype : ItemPrototype
    {
        public PrototypeId Avatar { get; protected set; }
        public PrototypeId EmotePower { get; protected set; }
    }

    public class CostumePrototype : ItemPrototype
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public AssetId CostumeUnrealClass { get; protected set; }
        public AssetId FullBodyIconPath { get; protected set; }
        public PrototypeId UsableBy { get; protected set; }
        public new AssetId StoreIconPath { get; protected set; }
        public AssetId PortraitIconPath { get; protected set; }
        public AssetId FullBodyIconPathDisabled { get; protected set; }
        public AssetId PartyPortraitIconPath { get; protected set; }
        public LocaleStringId MTXStoreInfo { get; protected set; }
        public LocaleStringId AvatarBioText { get; protected set; }
        public LocaleStringId AvatarDisplayName { get; protected set; }
        public LocaleStringId AvatarDisplayNameInformal { get; protected set; }
        public LocaleStringId AvatarDisplayNameShort { get; protected set; }
        public bool EquipTriggersVO { get; protected set; }
        public AssetId PortraitIconPathHiRes { get; protected set; }

        public override bool ApprovedForUse()
        {
            if (base.ApprovedForUse() == false) return false;

            AvatarPrototype avatar = GameDatabase.GetPrototype<AvatarPrototype>(UsableBy);
            if (avatar == null) return Logger.WarnReturn(false, $"ApprovedForUse(): avatar == null");

            return avatar.ApprovedForUse();
        }

        public override bool IsUsableByAgent(AgentPrototype agentProto)
        {
            PrototypeId agentProtoRef = agentProto != null ? agentProto.DataRef : PrototypeId.Invalid;
            return UsableBy == agentProtoRef;
        }
    }

    public class LegendaryPrototype : ItemPrototype
    {
    }

    public class MedalPrototype : ItemPrototype
    {
        public override bool IsUsableByAgent(AgentPrototype agentProto)
        {
            return AvatarUsesEquipmentType(this, agentProto);
        }
    }

    public class RelicPrototype : ItemPrototype
    {
        public EvalPrototype EvalOnStackCountChange { get; protected set; }
    }

    public class SuperteamPrototype : Prototype
    {
        public LocaleStringId DisplayName { get; protected set; }
    }

    public class TeamUpGearPrototype : ItemPrototype
    {
        public override bool IsDroppableForAgent(AgentPrototype agentProto)
        {
            if (agentProto is AvatarPrototype)
                return true;

            return base.IsDroppableForAgent(agentProto);
        }

        public override PrototypeId GetRollForAgent(PrototypeId rollForAvatar, AgentPrototype rollForTeamUp)
        {
            return rollForTeamUp == null ? rollForAvatar : rollForTeamUp.DataRef;
        }
    }

    public class PermaBuffPrototype : Prototype
    {
        public EvalPrototype EvalAvatarProperties { get; protected set; }
    }
}
