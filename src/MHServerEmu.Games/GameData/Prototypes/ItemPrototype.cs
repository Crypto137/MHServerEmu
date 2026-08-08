using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData.Calligraphy;
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
        [PrototypeField(PrototypeFieldType.Mixin)]
        public ProductPrototype Product { get; protected set; }
        public LocaleStringId ItemCategory { get; protected set; }
        public LocaleStringId ItemSubcategory { get; protected set; }
        public bool IsAvatarRestricted { get; protected set; }
        public DropRestrictionPrototype[] LootDropRestrictions { get; protected set; }
        public ItemBindingSettingsPrototype BindingSettings { get; protected set; }
        public AffixLimitsPrototype[] AffixLimits { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public TextStylePrototype TextStyleOverride { get; protected set; }
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
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        public bool IsContainer { get; protected set; }
#endif
#if GAME_VERSION_1_53
        public bool IsMTXItem { get; protected set; }
        public LocaleStringId ShortDescription { get; protected set; }
#endif

        // ---

        private const int PetTechAffixPositions = 5;

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
                    if (summonContextProto.SummonEntity == null)
                        continue;

                    // Skip summon contexts that do not summon a transition entity
                    TransitionPrototype transitionProto = summonContextProto.SummonEntity as TransitionPrototype;
                    if (transitionProto == null || transitionProto.DirectTarget == PrototypeId.Invalid)
                        continue;

                    // Get region from transition target
                    RegionConnectionTargetPrototype connectionTargetProto = transitionProto.DirectTarget.As<RegionConnectionTargetPrototype>();
                    if (connectionTargetProto == null)
                        continue;

                    if (!Verify.IsTrue(connectionTargetProto.Region != PrototypeId.Invalid))
                        continue;

                    return connectionTargetProto.Region;
                }
            }

            return PrototypeId.Invalid;
        }

        public bool OnApplyItemSpec(Item item, ItemSpec itemSpec)
        {
            ItemPrototype itemProto = item.ItemPrototype;
            if (!Verify.IsNotNull(itemProto)) return false;

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
            using var affixesHandle = DictionaryPool<AffixPosition, AffixSpec>.Get(out Dictionary<AffixPosition, AffixSpec> affixes);
            using var affixPickersHandle = ListPool<Picker<AffixPrototype>>.Get(PetTechAffixPositions, out List<Picker<AffixPrototype>> affixPickers);

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
                    if (!Verify.IsNotNull(affixProto))
                        continue;

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

                    using var affixSetHandle = HashSetPool<ScopedAffixRef>.Get(out HashSet<ScopedAffixRef> affixSet);
                    result |= affixSpec.RollAffix(random, rollFor, itemSpec, affixPickers[i], affixSet);

                    Verify.IsTrue(result.HasFlag(MutationResults.AffixChange));
                }
            }

            // Overwrite affixes
            using var newAffixSpecsHandle = ListPool<AffixSpec>.Get(out List<AffixSpec> newAffixSpecs);
            
            for (int i = 0; i < PetTechAffixPositions; i++)
                newAffixSpecs.Add(affixes[AffixPosition.PetTech1 + i]);

            itemSpec.SetAffixes(newAffixSpecs);

            // Add metadata
            if (affixes.TryGetValue(AffixPosition.Metadata, out AffixSpec metadataAffix))
                itemSpec.AddAffixSpec(metadataAffix);

            return result;
        }

        public static MutationResults CopyPetTechAffixes(ItemSpec sourceItemSpec, ItemSpec destItemSpec, AffixPosition position)
        {
            ItemPrototype destItemProto = destItemSpec.ItemProtoRef.As<ItemPrototype>();
            if (!Verify.IsNotNull(destItemProto)) return MutationResults.Error;
            if (!Verify.IsTrue(destItemProto.IsPetItem)) return MutationResults.Error;
            if (!Verify.IsTrue(position >= AffixPosition.PetTech1 && position <= AffixPosition.PetTech5)) return MutationResults.Error;

            IReadOnlyList<AffixSpec> sourceAffixSpecs = sourceItemSpec.AffixSpecs;
            for (int i = 0; i < sourceAffixSpecs.Count; i++)
            {
                AffixSpec sourceAffixSpec = sourceAffixSpecs[i];
                AffixPrototype sourceAffixProto = sourceAffixSpec.AffixProto;
                if (!Verify.IsNotNull(sourceAffixProto))
                    continue;

                if (sourceAffixProto.Position != position)
                    continue;

                // See if we can replace an existing affix
                bool replaced = false;

                using var destAffixSpecsHandle = ListPool<AffixSpec>.Get(destItemSpec.AffixSpecs, out List<AffixSpec> destAffixSpecs);
                for (int j = 0; j < destAffixSpecs.Count; j++)
                {
                    AffixSpec destAffixSpec = destAffixSpecs[j];
                    AffixPrototype destAffixProto = destAffixSpec.AffixProto;
                    if (!Verify.IsNotNull(destAffixProto))
                        continue;

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

                return MutationResults.AffixChange;
            }

            return MutationResults.None;
        }

        public static bool IsPetTechAffixUnlocked(Item petTechItem, AffixPosition affixPos)
        {
            if (!Verify.IsTrue(petTechItem.IsPetItem)) return false;

            AdvancementGlobalsPrototype advGlobalsProto = GameDatabase.AdvancementGlobalsPrototype;
            if (!Verify.IsNotNull(advGlobalsProto)) return false;

            PetTechAffixInfoPrototype petTechAffixInfoProto = advGlobalsProto.GetPetTechAffixInfoPrototype(affixPos);
            if (!Verify.IsNotNull(petTechAffixInfoProto)) return false;

            int donationCount = petTechItem.Properties[PropertyEnum.PetItemDonationCount, (int)affixPos];
            return donationCount >= petTechAffixInfoProto.ItemsRequiredToUnlock;
        }

        public static bool IsPetTechAffixUnlocked(Item petTechItem, PropertyId propertyId)
        {
            if (!Verify.IsTrue(petTechItem.IsPetItem)) return false;

            AdvancementGlobalsPrototype advGlobalsProto = GameDatabase.AdvancementGlobalsPrototype;

            Property.FromParam(propertyId, 0, out int affixPosValue);
            AffixPosition affixPos = (AffixPosition)affixPosValue;
            if (!Verify.IsTrue(affixPos >= AffixPosition.PetTech1 && affixPos <= AffixPosition.PetTech5)) return false;

            PetTechAffixInfoPrototype petTechAffixInfoProto = advGlobalsProto.GetPetTechAffixInfoPrototype(affixPos);
            if (!Verify.IsNotNull(petTechAffixInfoProto)) return false;

            int donationCount = petTechItem.Properties[PropertyEnum.PetItemDonationCount, (int)affixPos];
            return donationCount >= petTechAffixInfoProto.ItemsRequiredToUnlock;
        }

        public static bool CanDonateItemToPetTech(Item petTechItem, ItemSpec itemSpecToDonate, Item itemToDonate, out AffixPosition availableAffixPosition)
        {
            availableAffixPosition = AffixPosition.None;

            if (!Verify.IsTrue(petTechItem.IsPetItem)) return false;
            if (!Verify.IsNotNull(itemSpecToDonate)) return false;
            if (!Verify.IsTrue(itemSpecToDonate.ItemProtoRef != PrototypeId.Invalid)) return false;

            // Validate the item entity if we have one (i.e. there is no item entity in vaporization)
            if (itemToDonate != null)
            {
                if (itemToDonate.CurrentStackSize > 1)
                    return false;

                if (itemToDonate.IsInWorld)
                {
                    if (!Verify.IsTrue(itemToDonate.GetOwner() == null, "PetTech: An item on the ground cannot be in any inventory!"))
                        return false;
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
            if (!Verify.IsTrue(rarityProtoRef != PrototypeId.Invalid, $"Item without a rarity! \nItem:{itemSpecToDonate}"))
                return false;

            // Item must be flagged with PetTechDonationItem blueprint to be donatable
            DataDirectory dataDirectory = DataDirectory.Instance;
            BlueprintId petTechDonationItemBlueprint = dataDirectory.GetPrototypeBlueprintDataRef(advGlobalsProto.PetTechDonationItemPrototype);
            if (dataDirectory.PrototypeIsChildOfBlueprint(itemSpecToDonate.ItemProtoRef, petTechDonationItemBlueprint) == false)
                return false;

            // Look for available affix position (this is a bit of a mess)
            bool result = false;

            RarityPrototype itemToDonateRarityProto = itemSpecToDonate.RarityProtoRef.As<RarityPrototype>();
            if (!Verify.IsNotNull(itemToDonateRarityProto)) return false;

            for (AffixPosition positionIt = AffixPosition.PetTech5; positionIt >= AffixPosition.PetTech1; positionIt--)
            {
                PetTechAffixInfoPrototype rarityMatchedAffixInfoProto = advGlobalsProto.GetPetTechAffixInfoPrototype(positionIt);
                if (!Verify.IsNotNull(rarityMatchedAffixInfoProto)) return false;

                if (rarityMatchedAffixInfoProto.ItemRarityToConsume != itemToDonateRarityProto)
                    continue;

                if (petTechItem.HasAffixInPosition(positionIt) == false)
                    continue;

                for (AffixPosition subPositionIt = positionIt; subPositionIt >= AffixPosition.PetTech1; subPositionIt--)
                {
                    rarityMatchedAffixInfoProto = advGlobalsProto.GetPetTechAffixInfoPrototype(subPositionIt);
                    if (!Verify.IsNotNull(rarityMatchedAffixInfoProto)) return false;

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

            if (!Verify.IsTrue(petTechItem.IsPetItem)) return false;
            if (!Verify.IsTrue(itemToDonate == null || itemToDonate.ItemSpec == itemSpecToDonate)) return false;

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
            if (EvalExpirationTimeMS == null)
                return TimeSpan.Zero;

            using var evalContextHandle = EvalContextDataPool.Get(out EvalContextData evalContext);
            evalContext.SetReadOnlyVar_ProtoRef(EvalContext.Var1, rarityProtoRef);

            int expirationTimeMS = Eval.RunInt(EvalExpirationTimeMS, evalContext);
            if (!Verify.IsTrue(expirationTimeMS >= 0)) return TimeSpan.Zero;

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

            using var argsHandle = DropFilterArgumentsPool.Get(out DropFilterArguments args);
            DropFilterArguments.Initialize(args, lootContext);

            RestrictionTestFlags flags = RestrictionTestFlags.None;

            foreach (DropRestrictionPrototype dropRestrictionProto in LootDropRestrictions)
                dropRestrictionProto.Adjust(args, ref flags, RestrictionTestFlags.Rank);

            return args.Rank;
        }

        public bool GenerateBuiltInAffixDetails(ItemSpec itemSpec, List<BuiltInAffixDetails> detailsList)
        {
            using var affixEntryListHandle = ListPool<AffixEntryPrototype>.Get(out List<AffixEntryPrototype> affixEntryList);
            if (GetBuiltInAffixEntries(affixEntryList, itemSpec.RarityProtoRef))
            {
                using var affixSeedDictHandle = DictionaryPool<ulong, int>.Get(out Dictionary<ulong, int> affixSeedDict);

                foreach (AffixEntryPrototype affixEntryProto in affixEntryList)
                {
                    if (!Verify.IsTrue(affixEntryProto != null && affixEntryProto.Affix != PrototypeId.Invalid))
                        continue;

                    BuiltInAffixDetails builtInAffixDetails = new(affixEntryProto);

                    if (GeneratePowerModifierRefFromBuiltInAffix(affixEntryProto, itemSpec, ref builtInAffixDetails) == false)
                        continue;

                    builtInAffixDetails.Seed = GenAffixRandomSeed(affixSeedDict, itemSpec.Seed, itemSpec.ItemProtoRef, affixEntryProto.Affix, affixEntryProto.Power);

                    detailsList.Add(builtInAffixDetails);
                }
            }

            return detailsList.Count > 0;
        }

        public bool GetBuiltInAffixEntries(List<AffixEntryPrototype> entryList, PrototypeId rarityProtoRef)
        {
            // This is an Item:: static function in the client, but it makes more sense for it to be here

            if (!Verify.IsTrue(rarityProtoRef != PrototypeId.Invalid)) return false;

            RarityPrototype rarityProto = rarityProtoRef.As<RarityPrototype>();
            if (!Verify.IsNotNull(rarityProto)) return false;

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
            if (!Verify.IsTrue(affixEntryProto.Affix != PrototypeId.Invalid)) return false;

            builtInAffixDetails.LevelRequirement = affixEntryProto.LevelRequirement;
            if (builtInAffixDetails.LevelRequirement < 0)
            {
                Verify.IsTrue(false, $"Could not add a builtin Affix with a level requirement to an Item because of data errors: Item=[{itemSpec.ItemProtoRef.GetName()}], Affix=[{affixEntryProto.Affix.GetName()}]");
                return false;
            }

            AffixPowerModifierPrototype affixPowerModifierProto = affixEntryProto.Affix.As<AffixPowerModifierPrototype>();
            if (affixPowerModifierProto != null)
            {
                builtInAffixDetails.AvatarProtoRef = itemSpec.EquippableBy != PrototypeId.Invalid
                    ? itemSpec.EquippableBy
                    : affixEntryProto.Avatar;

                if (affixEntryProto.Avatar != PrototypeId.Invalid && affixEntryProto.Avatar != itemSpec.EquippableBy)
                {
                    if (!Verify.IsTrue(affixEntryProto.Avatar == itemSpec.EquippableBy,
                        $"An item has an ItemSpec.equippableBy that is different than one of its built-in Affix entry Avatar settings!\nItem: {itemSpec.ItemProtoRef.GetName()}\nAffix: {affixEntryProto.Affix.GetName()}\nEquippableBy: {itemSpec.EquippableBy.GetName()}\nRequired Avatar for Affix: {affixEntryProto.Avatar.GetName()}"))
                        return false;
                }

                if (affixPowerModifierProto.IsForSinglePowerOnly)
                {
                    builtInAffixDetails.ScopeProtoRef = affixEntryProto.Power;
                }
                else if (affixPowerModifierProto.PowerKeywordFilter != null)
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

            ref int count = ref affixSeedDict.GetValueRefOrAddDefault(affixSeed);

            affixSeed = (affixSeed >> count++) & 0xFFFFFFFF;

            return itemSeed ^ (int)affixSeed;
        }

        private static PrototypeId GetTriggeredPower(ItemActionSetPrototype actionSetProto, ItemEventType eventType, ItemActionType actionType)
        {
            if (actionSetProto == null || actionSetProto.Choices.IsNullOrEmpty())
                return PrototypeId.Invalid;

            foreach (ItemActionBasePrototype actionBaseProto in actionSetProto.Choices)
            {
                ItemActionPrototype actionProto = actionBaseProto as ItemActionPrototype;
                if (!Verify.IsNotNull(actionProto))
                    continue;

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
#if GAME_VERSION_1_53
        public int NumOfStacksToConsumeOnUse { get; protected set; }
#endif
    }

    public class ItemActionBasePrototype : Prototype
    {
        public int Weight { get; protected set; }
    }

    public class ItemActionPrototype : ItemActionBasePrototype
    {
        public ItemEventType TriggeringEvent { get; protected set; }

        //---

        [DoNotCopy]
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

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
    public class ItemActionUnlockPermaBuffPrototype : ItemActionPrototype
    {
        public PrototypeId PermaBuff { get; protected set; }

        //---

        public override ItemActionType ActionType { get => ItemActionType.UnlockPermaBuff; }
    }
#endif

#if GAME_VERSION_1_53
    public class ItemActionAwardAvatarXPPrototype : ItemActionPrototype
    {
        public int XP { get; protected set; }

        //---

        public override ItemActionType ActionType { get => ItemActionType.AwardAvatarXP; }
    }
#endif

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

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
    public class CategorizedAffixEntryPrototype : Prototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public AffixCategoryPrototype Category { get; protected set; }
        public short MinAffixes { get; protected set; }
    }
#endif

    public class AffixLimitsPrototype : Prototype
    {
        public LootContext[] AllowedContexts { get; protected set; }
        public PrototypeId ItemRarity { get; protected set; }
#if GAME_VERSION_1_48
        public short MaxPrefixPlusSuffixTotal { get; protected set; }
#endif
        public short MaxPrefixes { get; protected set; }
        public short MaxSuffixes { get; protected set; }
#if GAME_VERSION_1_48
        public short MinPrefixPlusSuffixTotal { get; protected set; }
#endif
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
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        public CategorizedAffixEntryPrototype[] CategorizedAffixes { get; protected set; }
#endif

        // ---

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

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        public short GetMax(AffixCategoryPrototype affixCategoryProto, LootRollSettings settings)
        {
            if (CategorizedAffixes.IsNullOrEmpty())
                return short.MaxValue;

            foreach (CategorizedAffixEntryPrototype entry in CategorizedAffixes)
            {
                if (entry.Category != affixCategoryProto)
                    continue;

                short numAffixes = entry.MinAffixes;

                if (settings != null && settings.AffixLimitByCategoryModifiers.TryGetValue(affixCategoryProto, out short mod))
                    numAffixes += mod;

                return numAffixes;
            }

            return short.MaxValue;
        }
#endif

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
                    Verify.IsTrue(false, $"AffixLimitsPrototype GetMax on unsupported AffixPosition [{affixPosition}]");
                    return 0;
            }

            if (settings != null)
            {
                if (getMax)
                {
                    if (settings.AffixLimitMaxByPositionModifiers.TryGetValue(affixPosition, out short maxMod))
                        limit += maxMod;
                }
                else
                {
                    if (settings.AffixLimitMinByPositionModifiers.TryGetValue(affixPosition, out short minMod))
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
        //---

        public virtual bool IsEquippableByAgent(AgentPrototype agentProto)
        {
            return true;
        }
    }

    public class EquipRestrictionSuperteamPrototype : EquipRestrictionPrototype
    {
        public PrototypeId SuperteamEquippableBy { get; protected set; }

        //---

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
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public AgentPrototype Agent { get; protected set; }

        //---

        public override bool IsEquippableByAgent(AgentPrototype agentProto)
        {
            if (base.IsEquippableByAgent(agentProto) == false)
                return false;

            if (agentProto == null || Agent == null)
                return true;

            return agentProto.DataRef == Agent.DataRef;
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
        //---

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
            if (!Verify.IsNotNull(characterProto)) return false;

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
#if GAME_VERSION_1_52
        public PrototypeId FulfillmentDuplicateItem { get; protected set; }
#elif GAME_VERSION_1_53
        public PrototypeId FulfillmentDuplicateItemConsole { get; protected set; }
        public CostumeSpecialEffectEntryPrototype[] CostumeSpecialEffects { get; protected set; }
        public PrototypeId FulfillmentDuplicateItemPC { get; protected set; }
#endif

        //---

        public override bool ApprovedForUse()
        {
            if (base.ApprovedForUse() == false)
                return false;

            AvatarPrototype avatar = GameDatabase.GetPrototype<AvatarPrototype>(UsableBy);
            if (!Verify.IsNotNull(avatar)) return false;

#if GAME_VERSION_1_53
            // V53_TODO: FulfillmentDuplicateItemConsole
            ItemPrototype itemProto = GameDatabase.GetPrototype<ItemPrototype>(FulfillmentDuplicateItemPC);
            if (!Verify.IsTrue(itemProto != null && itemProto != this)) return false;

            return avatar.ApprovedForUse() && itemProto.ApprovedForUse();
#elif GAME_VERSION_1_52
            ItemPrototype itemProto = GameDatabase.GetPrototype<ItemPrototype>(FulfillmentDuplicateItem);
            if (!Verify.IsTrue(itemProto != null && itemProto != this)) return false;

            return avatar.ApprovedForUse() && itemProto.ApprovedForUse();
#else
            return avatar.ApprovedForUse();
#endif
        }

        public override bool IsUsableByAgent(AgentPrototype agentProto)
        {
            PrototypeId agentProtoRef = agentProto != null ? agentProto.DataRef : PrototypeId.Invalid;
            return UsableBy == agentProtoRef;
        }
    }

    public class LegendaryPrototype : ItemPrototype
    {
#if GAME_VERSION_1_53
        public CurveId ItemAffixLevelingCurve { get; protected set; }
#endif

        //---

        // V53_NOTE: 1.53 added per legendary leveling curves instead of a single global one.
#if GAME_VERSION_1_53
        public int GetItemAffixLevelCap()
        {
            Curve levelingCurve = GetItemAffixLevelingCurve();
            if (!Verify.IsNotNull(levelingCurve)) return 0;

            return levelingCurve.MaxPosition;
        }
#endif

#if GAME_VERSION_1_53
        public long GetItemAffixLevelUpXPRequirement(int level)
        {
            if (level < 0)
                return AdvancementGlobalsPrototype.InvalidXPRequirement;

            Curve levelingCurve = GetItemAffixLevelingCurve();
            if (!Verify.IsNotNull(levelingCurve)) return AdvancementGlobalsPrototype.InvalidXPRequirement;

            return AdvancementGlobalsPrototype.GetLevelUpXPRequirementFromCurve(level, levelingCurve);
        }
#endif

#if GAME_VERSION_1_53
        private Curve GetItemAffixLevelingCurve()
        {
            return CurveDirectory.Instance.GetCurve(ItemAffixLevelingCurve);
        }
#endif
    }

    public class MedalPrototype : ItemPrototype
    {
        //---

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
        //---

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

#if GAME_VERSION_1_53
    public class CostumeSpecialEffectEntryPrototype : Prototype
    {
        public PrototypeId SpecialEffect { get; protected set; }
        public AssetId SpecialEffectUnrealClass { get; protected set; }
    }
#endif

#if GAME_VERSION_1_53
    public class CostumeSpecialEffectPrototype : Prototype
    {
        public DesignWorkflowState DesignState { get; protected set; }
        public DesignWorkflowState DesignStatePS4 { get; protected set; }
        public DesignWorkflowState DesignStateXboxOne { get; protected set; }
        public LocaleStringId DisplayName { get; protected set; }
        public LocaleStringId UnlockRequirementDisplayName { get; protected set; }
    }
#endif

#if GAME_VERSION_1_53
    public class OmegaPrestigeUnlockPrototype : ItemPrototype
    {
        public PrototypeId Avatar { get; protected set; }
    }
#endif
}
