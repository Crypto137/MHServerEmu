using System.Text;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;

namespace MHServerEmu.Games.Entities.Items
{
    public enum ItemActionType
    {
        None,
        AssignPower,
        DestroySelf,
        GuildUnlock,
        PrestigeMode,
        ReplaceSelfItem,
        ReplaceSelfLootTable,
        ResetMissions,
        Respec,
        SaveDangerRoomScenario,
        UnlockPermaBuff,
        UsePower,
        AwardTeamUpXP,
        OpenUIPanel
    }

    public class Item : WorldEntity
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private ItemSpec _itemSpec = new();
        private List<AffixPropertiesCopyEntry> _affixProperties = new();

        public ItemPrototype ItemPrototype { get => Prototype as ItemPrototype; }

        public ItemSpec ItemSpec { get => _itemSpec; }
        public PrototypeId OnUsePower { get; private set; }
        public PrototypeId OnEquipPower { get; private set; }

        public bool IsBoundToAccount { get => _itemSpec.GetBindingState(); }
        public bool WouldBeDestroyedOnDrop { get => IsBoundToAccount || GameDatabase.DebugGlobalsPrototype.TrashedItemsDropInWorld == false; }
        public bool IsPetItem { get => ItemPrototype?.IsPetItem == true; }

        public Item(Game game) : base(game) { }

        public override bool Initialize(EntitySettings settings)
        {
            base.Initialize(settings);

            // Apply ItemSpec if one was provided with entity settings
            if (settings.ItemSpec != null)
                ApplyItemSpec(settings.ItemSpec);

            return true;
        }

        public override bool ApplyInitialReplicationState(ref EntitySettings settings)
        {
            if (base.ApplyInitialReplicationState(ref settings) == false)
                return false;

            // Serialized entities get their ItemSpec from serialized data rather than as a settings field
            if (settings.ArchiveData != null)
                ApplyItemSpec(ItemSpec);

            return true;
        }

        public override bool Serialize(Archive archive)
        {
            bool success = base.Serialize(archive);
            success &= Serializer.Transfer(archive, ref _itemSpec);
            return success;
        }

        public override bool IsAutoStackedWhenAddedToInventory()
        {
            var itemProto = Prototype as ItemPrototype;
            if (itemProto == null) return Logger.WarnReturn(false, "IsAutoStackedWhenAddedToInventory(): itemProto == null");
            if (itemProto.StackSettings == null) return false;
            return itemProto.StackSettings.AutoStackWhenAddedToInventory;
        }

        public bool CanUse(Agent agent, bool powerUse)
        {
            // TODO
            return true;
        }

        public bool PlayerCanDestroy(Player player)
        {
            if (player.Owns(this) == false)
                return false;

            ItemPrototype itemProto = ItemPrototype;
            if (itemProto == null)
                return Logger.WarnReturn(false, "PlayerCanDestroy(): itemProto == null");

            if (itemProto.CanBeDestroyed == false)
                return false;

            // TODO: Avatar::ValidateEquipmentChange

            return true;
        }

        public uint GetVendorBaseXPGain(Player player)
        {
            if (player == null) return Logger.WarnReturn(0u, "GetVendorBaseXPGain(): player == null");
            float xpGain = GetSellPrice(player);
            xpGain *= LiveTuningManager.GetLiveGlobalTuningVar(Gazillion.GlobalTuningVar.eGTV_VendorXPGain);
            return (uint)xpGain;
        }

        public uint GetSellPrice(Player player)
        {
            if (player == null) return Logger.WarnReturn(0u, "GetSellPrice(): player == null");

            ItemPrototype itemProto = Prototype as ItemPrototype;
            if (itemProto == null) return Logger.WarnReturn(0u, "GetSellPrice(): proto == null");

            return itemProto.Cost != null ? (uint)itemProto.Cost.GetSellPriceInCredits(player, this) : 0u;
        }

        public static int GetEquippableAtLevelForItemLevel(int itemLevel)
        {
            AdvancementGlobalsPrototype advanGlobalsProto = GameDatabase.AdvancementGlobalsPrototype;
            if (advanGlobalsProto == null) return Logger.WarnReturn(0, "GetEquippableAtLevelForItemLevel(): advanGlobalsProto == null");

            Curve itemEquipReqOffsetCurve = CurveDirectory.Instance.GetCurve(advanGlobalsProto.ItemEquipRequirementOffset);
            if (itemEquipReqOffsetCurve == null) return Logger.WarnReturn(0, "GetEquippableAtLevelForItemLevel(): itemEquipReqOffsetCurve == null");

            return Math.Clamp(itemLevel + itemEquipReqOffsetCurve.GetIntAt(itemLevel), 1, advanGlobalsProto.GetAvatarLevelCap());
        }

        public TimeSpan GetExpirationTime()
        {
            PrototypeId rarityProtoRef = Properties[PropertyEnum.ItemRarity];
            return ItemPrototype.GetExpirationTime(rarityProtoRef);
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);
            sb.AppendLine($"{nameof(_itemSpec)}: {_itemSpec}");
        }

        public override void OnSelfRemovedFromOtherInventory(InventoryLocation prevInvLoc)
        {
            base.OnSelfRemovedFromOtherInventory(prevInvLoc);

            // Destroy summoned pet
            if (prevInvLoc.IsValid && prevInvLoc.InventoryConvenienceLabel == InventoryConvenienceLabel.PetItem)
            {
                var itemProto = ItemPrototype;
                if (itemProto?.ActionsTriggeredOnItemEvent?.Choices == null) return;
                var itemActionProto = itemProto.ActionsTriggeredOnItemEvent.Choices[0];
                if (itemActionProto is ItemActionUsePowerPrototype itemActionUsePowerProto){
                    var powerRef = itemActionUsePowerProto.Power;
                    var avatar = Game.EntityManager.GetEntity<Avatar>(prevInvLoc.ContainerId);
                    Power power = avatar?.GetPower(powerRef);
                    if (power == null) return;
                    if (power.Prototype is SummonPowerPrototype summonPowerProto)
                    {
                        PropertyId summonedEntityCountProp = new(PropertyEnum.PowerSummonedEntityCount, powerRef);
                        if (avatar.Properties[PropertyEnum.PowerToggleOn, powerRef])
                        {
                            EntityHelper.DestroySummonerFromPowerPrototype(avatar, summonPowerProto);
                            avatar.Properties[PropertyEnum.PowerToggleOn, powerRef] = false;
                            avatar.Properties.AdjustProperty(-1, summonedEntityCountProp);
                        }
                    }
                }

            }
        }

        public bool DecrementStack(int count = 1)
        {
            if (count < 1) return Logger.WarnReturn(false, "DecrementStack(): count < 1");

            int currentStackSize = CurrentStackSize;
            if (count > currentStackSize) return Logger.WarnReturn(false, "DecrementStack(): count > currentStackSize");

            int newCount = Math.Max(0, currentStackSize - count);

            if (newCount > 0)
                Properties[PropertyEnum.InventoryStackCount] = newCount;
            else
                ScheduleDestroyEvent(TimeSpan.Zero);

            return true;
        }

        public void SetRecentlyAdded(bool value)
        {
            Properties[PropertyEnum.ItemRecentlyAddedGlint] = value;
            Properties[PropertyEnum.ItemRecentlyAddedToInventory] = value;
        }

        private bool ApplyItemSpec(ItemSpec itemSpec)
        {
            if (itemSpec.IsValid == false) return Logger.WarnReturn(false, $"ApplyItemSpec(): Invalid ItemSpec on Item {this}!");

            _itemSpec.Set(itemSpec);

            ItemPrototype itemProto = ItemPrototype;
            if (itemProto == null) return Logger.WarnReturn(false, "ApplyItemSpec(): itemProto == null");

            if (ApplyItemSpecProperties() == false)
                return Logger.WarnReturn(false, "ApplyItemSpec(): Failed to apply ItemSpec properties");

            itemProto.OnApplyItemSpec(this, _itemSpec);     // TODO (needed for PetTech affixes)

            GRandom random = new(_itemSpec.Seed);

            // Apply built-in properties
            if (itemProto.PropertiesBuiltIn.HasValue())
            {
                foreach (PropertyEntryPrototype propertyEntryProto in itemProto.PropertiesBuiltIn)
                {
                    float randomMult = random.NextFloat();

                    if (propertyEntryProto is PropertyPickInRangeEntryPrototype pickInRangeProto)
                        OnBuiltInPropertyRoll(randomMult, pickInRangeProto);
                    else if (propertyEntryProto is PropertySetEntryPrototype setProto)
                        OnBuiltInPropertySet(setProto);
                    else
                        Logger.Warn($"ApplyItemSpec(): Invalid property entry prototype {propertyEntryProto}");
                }
            }

            // NOTE: RNG is reseeded for each affix individually

            // Apply built-in affixes
            foreach (BuiltInAffixDetails builtInAffixDetails in itemProto.GenerateBuiltInAffixDetails(_itemSpec))
            {
                AffixPrototype affixProto = builtInAffixDetails.AffixEntryProto.Affix.As<AffixPrototype>();
                if (affixProto == null)
                {
                    Logger.Warn("ApplyItemSpec(): affixProto == null");
                    continue;
                }

                random.Seed(builtInAffixDetails.Seed);
                OnAffixAdded(random, affixProto, builtInAffixDetails.ScopeProtoRef, builtInAffixDetails.AvatarProtoRef, builtInAffixDetails.LevelRequirement);
            }

            // Apply rolled affixes
            foreach (AffixSpec affixSpec in _itemSpec.AffixSpecs)
            {
                if (affixSpec.Seed == 0) return Logger.WarnReturn(false, "ApplyItemSpec(): affixSpec.Seed == 0");
                random.Seed(affixSpec.Seed);
                
                if (affixSpec.AffixProto == null) return Logger.WarnReturn(false, "ApplyItemSpec(): affixSpec.AffixProto == null");

                OnAffixAdded(random, affixSpec.AffixProto, affixSpec.ScopeProtoRef, _itemSpec.EquippableBy, 0);
            }

            // Pick triggered power
            ItemActionSetPrototype triggeredActions = itemProto.ActionsTriggeredOnItemEvent;
            if (triggeredActions != null && triggeredActions.Choices.HasValue())
            {
                if (triggeredActions.PickMethod == PickMethod.PickWeight)
                {
                    // It seems this is reusing the seed of the last rolled affix?
                    Picker<int> picker = new(random);

                    for (int i = 0; i < triggeredActions.Choices.Length; i++)
                    {
                        ItemActionBasePrototype actionProto = triggeredActions.Choices[i];

                        if (actionProto.Weight <= 0)
                            continue;

                        picker.Add(i, actionProto.Weight);
                    }

                    picker.Pick(out int index);
                    OnItemEventRoll(index);
                }

                OnUsePower = GetTriggeredPower(ItemEventType.OnUse, ItemActionType.UsePower);
                OnEquipPower = GetTriggeredPower(ItemEventType.OnEquip, ItemActionType.AssignPower);
            }

            return true;
        }

        private bool ApplyItemSpecProperties()
        {
            // We can skip some validation here because this is called only from ApplyItemSpec()
            ItemPrototype itemProto = ItemPrototype;

            // Apply rarity
            RarityPrototype rarityProto = GameDatabase.GetPrototype<RarityPrototype>(_itemSpec.RarityProtoRef);
            if (rarityProto == null) return Logger.WarnReturn(false, "ApplyItemSpec(): rarityProto == null");
            Properties[PropertyEnum.ItemRarity] = _itemSpec.RarityProtoRef;

            // Apply level and level requirement
            int itemLevel = Math.Max(1, _itemSpec.ItemLevel);
            Properties[PropertyEnum.ItemLevel] = Math.Max(1, _itemSpec.ItemLevel);
            Properties[PropertyEnum.Requirement, PropertyEnum.CharacterLevel] = (float)GetEquippableAtLevelForItemLevel(itemLevel);

            // Apply binding settings
            if (itemProto.BindingSettings != null)
            {
                // Apply default settings
                ItemBindingSettingsEntryPrototype defaultSettings = itemProto.BindingSettings.DefaultSettings;
                if (defaultSettings != null)
                {
                    Properties[PropertyEnum.ItemBindsToAccountOnPickup] = defaultSettings.BindsToAccountOnPickup;
                    Properties[PropertyEnum.ItemBindsToCharacterOnEquip] = defaultSettings.BindsToCharacterOnEquip;
                    Properties[PropertyEnum.ItemIsTradable] = defaultSettings.IsTradable;
                }

                // Override with rarity settings if there are any
                if (itemProto.BindingSettings.PerRaritySettings != null)
                {
                    foreach (ItemBindingSettingsEntryPrototype perRaritySettingProto in itemProto.BindingSettings.PerRaritySettings)
                    {
                        if (perRaritySettingProto.RarityFilter != _itemSpec.RarityProtoRef) continue;

                        Properties[PropertyEnum.ItemBindsToAccountOnPickup] = defaultSettings.BindsToAccountOnPickup;
                        Properties[PropertyEnum.ItemBindsToCharacterOnEquip] = defaultSettings.BindsToCharacterOnEquip;
                        Properties[PropertyEnum.ItemIsTradable] = defaultSettings.IsTradable;
                    }
                }
            }

            // Apply stack settings
            ItemStackSettingsPrototype stackSettings = itemProto.StackSettings;
            if (stackSettings != null)
            {
                Properties[PropertyEnum.InventoryStackSizeMax] = stackSettings.MaxStacks;
                Properties[PropertyEnum.ItemLevel] = stackSettings.ItemLevelOverride;
                Properties[PropertyEnum.Requirement, PropertyEnum.CharacterLevel] = (float)stackSettings.RequiredCharLevelOverride;
            }

            // Apply rarity bonus to item level
            Properties.AdjustProperty(rarityProto.ItemLevelBonus, PropertyEnum.ItemLevel);

            // Apply random variation using item spec seed
            GRandom random = new(_itemSpec.Seed);
            Properties[PropertyEnum.ItemVariation] = random.NextFloat();

            return true;
        }

        public void InteractWithAvatar(Avatar avatar)
        {
            var player = avatar.GetOwnerOfType<Player>();
            if (player == null) return;

            var itemProto = ItemPrototype;

            if (itemProto.ActionsTriggeredOnItemEvent != null && itemProto.ActionsTriggeredOnItemEvent.Choices.HasValue())
                if (itemProto.ActionsTriggeredOnItemEvent.PickMethod == PickMethod.PickAll) // TODO : other pick method
                {
                    foreach (var choice in itemProto.ActionsTriggeredOnItemEvent.Choices)
                    {
                        if (choice is not ItemActionPrototype itemActionProto) continue;
                        TriggerActionEvent(itemActionProto, player, avatar);
                    }
                }
        }

        private void TriggerActionEvent(ItemActionPrototype itemActionProto, Player player, Avatar avatar)
        {
            if (itemActionProto.TriggeringEvent != ItemEventType.OnUse) return;

            // TODO ItemActionPrototype.ActionType

            if (itemActionProto is ItemActionUsePowerPrototype itemActionUsePowerProto)
                TriggerActionUsePower(avatar, itemActionUsePowerProto.Power);
        }

        private void TriggerActionUsePower(Avatar avatar, PrototypeId powerRef)
        {
            if (avatar.HasPowerInPowerCollection(powerRef) == false)
                avatar.AssignPower(powerRef, new(0, avatar.CharacterLevel, avatar.CombatLevel));

            // TODO move this to powers
            Power power = avatar.GetPower(powerRef);
            if (power == null) return;

            if (power.Prototype is SummonPowerPrototype summonPowerProto)
            {
                PropertyId summonedEntityCountProp = new(PropertyEnum.PowerSummonedEntityCount, powerRef);
                if (avatar.Properties[PropertyEnum.PowerToggleOn, powerRef])
                {
                    EntityHelper.DestroySummonerFromPowerPrototype(avatar, summonPowerProto);
                    avatar.Properties[PropertyEnum.PowerToggleOn, powerRef] = false;
                    avatar.Properties.AdjustProperty(-1, summonedEntityCountProp);
                }
                else
                {
                    EntityHelper.SummonEntityFromPowerPrototype(avatar, summonPowerProto);
                    avatar.Properties[PropertyEnum.PowerToggleOn, powerRef] = true;
                    avatar.Properties.AdjustProperty(1, summonedEntityCountProp);
                }
            }
        }

        private bool OnBuiltInPropertyRoll(float randomMult, PropertyPickInRangeEntryPrototype pickInRangeProto)
        {
            PropertyInfo propertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(pickInRangeProto.Prop.Enum);            
            PropertyDataType propDataType = propertyInfo.DataType;

            if (propDataType != PropertyDataType.Boolean && propDataType != PropertyDataType.Real && propDataType != PropertyDataType.Integer)
            {
                return Logger.WarnReturn(false, "OnBuiltInPropertyRoll(): The following Item has a built-in pick-in-range PropertyEntry with a property " +
                    $"that is not an int/float/bool prop, which doesn't work!\nItem: [{this}]\nProperty: [{propertyInfo.PropertyName}]");
            }

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, Properties);

            float valueMin = 0f;
            if (pickInRangeProto.ValueMin != null)
                valueMin = Eval.RunFloat(pickInRangeProto.ValueMin, evalContext);

            float valueMax = 0f;
            if (pickInRangeProto.ValueMax != null)
                valueMax = Eval.RunFloat(pickInRangeProto.ValueMax, evalContext);

            if (propDataType == PropertyDataType.Real)
            {
                float value = pickInRangeProto.RollAsInteger
                    ? GenerateTruncatedFloatWithinRange(randomMult, valueMin, valueMax)
                    : GenerateFloatWithinRange(randomMult, valueMin, valueMax);

                Properties[pickInRangeProto.Prop] = value;
            }
            else if (propDataType == PropertyDataType.Integer)
            {
                Properties[pickInRangeProto.Prop] = GenerateIntWithinRange(randomMult, valueMin, valueMax);
            }
            else
            {
                // The client doesn't have assignment for bool properties here for some reason
                Logger.Warn($"OnBuiltInPropertyRoll(): Unhandled property data type {propDataType}");
            }

            return true;
        }

        private bool OnBuiltInPropertySet(PropertySetEntryPrototype setProto)
        {
            PropertyInfo propertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(setProto.Prop.Enum);
            PropertyDataType propDataType = propertyInfo.DataType;

            if (propDataType != PropertyDataType.Real && propDataType != PropertyDataType.Integer && propDataType != PropertyDataType.Asset)
            {
                return Logger.WarnReturn(false, "OnBuiltInPropertyRoll(): The following Item has a built-in set PropertyEntry with a property " +
                    $"that is not an int/float/asset prop, which doesn't work!\nItem: [{this}]\nProperty: [{propertyInfo.PropertyName}]");
            }

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, Properties);

            switch (propDataType)
            {
                case PropertyDataType.Real:
                    Properties[setProto.Prop] = setProto.Value != null ? Eval.RunFloat(setProto.Value, evalContext) : 0f;
                    break;

                case PropertyDataType.Integer:
                    Properties[setProto.Prop] = setProto.Value != null ? Eval.RunInt(setProto.Value, evalContext) : 0;
                    break;

                case PropertyDataType.Asset:
                    Properties[setProto.Prop] = setProto.Value != null ? Eval.RunAssetId(setProto.Value, evalContext) : AssetId.Invalid;
                    break;
            }

            return true;
        }

        private bool OnAffixAdded(GRandom random, AffixPrototype affixProto, PrototypeId scopeProtoRef, PrototypeId avatarProtoRef, int levelRequirement)
        {
            if (affixProto.Position == AffixPosition.Metadata)
                return true;

            bool affixHasBonusPropertiesToApply = affixProto.HasBonusPropertiesToApply;

            if (affixHasBonusPropertiesToApply == false && affixProto.DataRef != GameDatabase.GlobalsPrototype.ItemNoVisualsAffix)
                return Logger.WarnReturn(false, "OnAffixAdded(): affixHasBonusPropertiesToApply == false && affixProto.DataRef != GameDatabase.GlobalsPrototype.ItemNoVisualsAffix");

            // Initialized affixes are stored in a struct called AffixPropertiesCopyEntry
            AffixPropertiesCopyEntry affixEntry = new();
            affixEntry.AffixProto = affixProto;
            affixEntry.LevelRequirement = levelRequirement;
            affixEntry.Properties = new();

            if (affixProto.Properties != null)
                affixEntry.Properties.FlattenCopyFrom(affixProto.Properties, true);

            var affixPowerModifierProto = affixProto as AffixPowerModifierPrototype;
            if (affixPowerModifierProto != null)
            {
                int evalLevelVar = 0;

                if (affixPowerModifierProto.IsForSinglePowerOnly)
                {
                    // Verbose validation like in the client

                    if (scopeProtoRef.As<PowerPrototype>() == null)
                    {
                        return Logger.WarnReturn(false, string.Format(
                            $"OnAffixAdded(): AffixPowerModifier IsForSinglePowerOnly but scopeProtoRef is not a power! Affix=[{0}] Scope=[{1}] Item=[{2}]",
                            affixProto.ToString(),
                            scopeProtoRef.GetName(),
                            _itemSpec.ItemProtoRef.GetName()));
                    }

                    if (avatarProtoRef == PrototypeId.Invalid && affixProto.IsGemAffix == false)
                    {
                        return Logger.WarnReturn(false, string.Format(
                            $"OnAffixAdded(): Non-gem AffixPowerModifier IsForSinglePowerOnly, but avatarProtoRef is invalid! Affix=[{0}] Scope=[{1}] Item=[{2}]",
                            affixProto.ToString(),
                            scopeProtoRef.GetName(),
                            _itemSpec.ItemProtoRef.GetName()));
                    }

                    AvatarPrototype avatarProto = avatarProtoRef.As<AvatarPrototype>();
                    if (avatarProto == null)
                    {
                        return Logger.WarnReturn(false, string.Format(
                            $"OnAffixAdded(): Unable to get Avatar=[{0}]. Affix=[{1}] Item=[{2}]",
                            avatarProtoRef.GetName(),
                            affixProto.ToString(),
                            _itemSpec.ItemProtoRef.GetName()));
                    }

                    PowerProgressionEntryPrototype powerProgEntry = avatarProto.GetPowerProgressionEntryForPower(scopeProtoRef);
                    if (powerProgEntry == null)
                    {
                        return Logger.WarnReturn(false, string.Format(
                            $"OnAffixAdded(): Unable to get Power[{0}] in Avatar[{1}] Power Progression Table. Affix=[{2}] Item=[{3}]",
                            scopeProtoRef.GetName(),
                            avatarProtoRef.GetName(),
                            affixProto.ToString(),
                            _itemSpec.ItemProtoRef.GetName()));
                    }

                                                            //
                    evalLevelVar = powerProgEntry.Level;    // <------- THIS IS IMPORTANT: we set an actual value here, and not just validating
                                                            //
                }
                else if (affixPowerModifierProto.PowerProgTableTabRef != PrototypeId.Invalid)
                {
                    if (scopeProtoRef.As<AvatarPrototype>() == null)
                    {
                        return Logger.WarnReturn(false, string.Format(
                            $"OnAffixAdded(): AffixPowerModifier is for PowerProgTableTabRef but scopeProtoRef is not an avatar! Affix=[{0}] Scope=[{1}] Item=[{2}]",
                            affixProto.ToString(),
                            scopeProtoRef.GetName(),
                            _itemSpec.ItemProtoRef.GetName()));
                    }
                }
                else if (affixPowerModifierProto.PowerKeywordFilter != PrototypeId.Invalid)
                {
                    if (scopeProtoRef != PrototypeId.Invalid)
                    {
                        return Logger.WarnReturn(false, string.Format(
                            $"OnAffixAdded(): AffixPowerModifier is for PowerKeywordFilter but scopeProtoRef is NOT invalid as expected! Affix=[{0}] Scope=[{1}] Item=[{2}]",
                            affixProto.ToString(),
                            scopeProtoRef.GetName(),
                            _itemSpec.ItemProtoRef.GetName()));
                    }
                }

                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, Properties);
                evalContext.SetVar_Int(EvalContext.Var1, (int)Properties[PropertyEnum.ItemLevel]);
                evalContext.SetVar_Int(EvalContext.Var2, evalLevelVar);

                // NOTE: PowerBoost and PowerGrantRank values are rolled in parallel on the client and the server,
                // so the order needs to be exact, or we are going to get a desync.

                int powerBoostMax = Eval.RunInt(affixPowerModifierProto.PowerBoostMax, evalContext);
                if (powerBoostMax > 0)
                {
                    int powerBoostMin = Eval.RunInt(affixPowerModifierProto.PowerBoostMin, evalContext);

                    if (affixPowerModifierProto.PowerProgTableTabRef != PrototypeId.Invalid)
                        affixEntry.PowerModifierPropertyId = new(PropertyEnum.PowerBoost, affixPowerModifierProto.PowerProgTableTabRef, scopeProtoRef);
                    else if (affixPowerModifierProto.PowerKeywordFilter != PrototypeId.Invalid)
                        affixEntry.PowerModifierPropertyId = new(PropertyEnum.PowerBoost, affixPowerModifierProto.PowerKeywordFilter, PrototypeId.Invalid);
                    else
                        affixEntry.PowerModifierPropertyId = new(PropertyEnum.PowerBoost, scopeProtoRef);

                    affixEntry.Properties[affixEntry.PowerModifierPropertyId] = GenerateIntWithinRange(random.NextFloat(), powerBoostMin, powerBoostMax);
                }

                int powerGrantMaxRank = Eval.RunInt(affixPowerModifierProto.PowerGrantRankMax, evalContext);
                if (powerGrantMaxRank > 0)
                {
                    int powerGrantMinRank = Eval.RunInt(affixPowerModifierProto.PowerGrantRankMin, evalContext);

                    if (affixPowerModifierProto.PowerProgTableTabRef != PrototypeId.Invalid)
                        affixEntry.PowerModifierPropertyId = new(PropertyEnum.PowerGrantRank, affixPowerModifierProto.PowerProgTableTabRef, scopeProtoRef);
                    else if (affixPowerModifierProto.PowerKeywordFilter != PrototypeId.Invalid)
                        affixEntry.PowerModifierPropertyId = new(PropertyEnum.PowerGrantRank, affixPowerModifierProto.PowerKeywordFilter, PrototypeId.Invalid);
                    else
                        affixEntry.PowerModifierPropertyId = new(PropertyEnum.PowerGrantRank, scopeProtoRef);

                    affixEntry.Properties[affixEntry.PowerModifierPropertyId] = GenerateIntWithinRange(random.NextFloat(), powerGrantMinRank, powerBoostMax);
                }
            }
            else if (affixProto is AffixRegionModifierPrototype affixRegionModifierProto)
            {
                RegionAffixPrototype regionAffixProto = scopeProtoRef.As<RegionAffixPrototype>();
                if (regionAffixProto == null)
                {
                    return Logger.WarnReturn(false,
                        $"OnAffixAdded(): AffixRegionModifier without a scope ref!\n Affix: {affixProto}\nItem: {_itemSpec.ItemProtoRef.GetName()}");
                }

                if (regionAffixProto.Difficulty != 0)
                    affixEntry.Properties[PropertyEnum.RegionAffixDifficulty] = regionAffixProto.Difficulty;

                affixEntry.Properties[PropertyEnum.RegionAffix, scopeProtoRef] = true;
            }

            affixEntry.Properties[PropertyEnum.ItemLevel] = Properties[PropertyEnum.ItemLevel];

            if (affixProto.PropertyEntries != null)
            {
                foreach (PropertyPickInRangeEntryPrototype propertyEntry in affixProto.PropertyEntries)
                {
                    // NOTE: Property entries are rolled in parallel on the client and the server,
                    // so the order needs to be exact, or we are going to get a desync.
                    float randomMult = random.NextFloat();

                    PropertyInfo propertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEntry.Prop.Enum);
                    PropertyDataType propDataType = propertyInfo.DataType;

                    if (propDataType != PropertyDataType.Boolean && propDataType != PropertyDataType.Real && propDataType != PropertyDataType.Integer)
                    {
                        Logger.Warn("OnAffixAdded(): The following Affix has a built-in pick-in-range PropertyEntry with a property " +
                            $"that is not an int/float/bool prop, which doesn't work!\nAffix: [{affixProto}]\nProperty: [{propertyInfo.PropertyName}]");
                        continue;
                    }

                    using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                    evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, Properties);

                    float valueMin = 0f;
                    if (propertyEntry.ValueMin != null)
                        valueMin = Eval.RunFloat(propertyEntry.ValueMin, evalContext);

                    float valueMax = 0f;
                    if (propertyEntry.ValueMax != null)
                        valueMax = Eval.RunFloat(propertyEntry.ValueMax, evalContext);

                    switch (propDataType)
                    {
                        case PropertyDataType.Boolean:
                            if (valueMin < 0 || valueMax > 1)
                            {
                                Logger.Warn("OnAffixAdded(): The following Affix has a built-in pick-in-range PropertyEntry with a boolean property " +
                                    $"and a range that is not in [0, 1]\nAffix: [{affixProto}]\nProperty: [{propertyInfo.PropertyName}]");
                                continue;
                            }

                            affixEntry.Properties[propertyEntry.Prop] = GenerateIntWithinRange(randomMult, valueMin, valueMax);

                            break;

                        case PropertyDataType.Real:
                            float valueFloat = propertyEntry.RollAsInteger
                                ? GenerateTruncatedFloatWithinRange(randomMult, valueMin, valueMax)
                                : GenerateFloatWithinRange(randomMult, valueMin, valueMax);

                            if (affixPowerModifierProto != null && affixPowerModifierProto.PowerProgTableTabRef != PrototypeId.Invalid)
                            {
                                affixEntry.PowerModifierPropertyId = new(propertyEntry.Prop.Enum, affixPowerModifierProto.PowerProgTableTabRef, scopeProtoRef);
                                affixEntry.Properties[affixEntry.PowerModifierPropertyId] = valueFloat;
                            }
                            else
                            {
                                affixEntry.Properties[propertyEntry.Prop] = valueFloat;
                            }

                            break;

                        case PropertyDataType.Integer:
                            int valueInt = GenerateIntWithinRange(randomMult, valueMin, valueMax);

                            if (affixPowerModifierProto != null && affixPowerModifierProto.PowerProgTableTabRef != PrototypeId.Invalid)
                            {
                                affixEntry.PowerModifierPropertyId = new(propertyEntry.Prop.Enum, affixPowerModifierProto.PowerProgTableTabRef, scopeProtoRef);
                                affixEntry.Properties[affixEntry.PowerModifierPropertyId] = valueInt;
                            }
                            else
                            {
                                affixEntry.Properties[propertyEntry.Prop] = valueInt;
                            }

                            break;
                    }
                }
            }

            if (IsPetItem)
            {
                Logger.Warn("OnAffixAdded(): Pet items are not yet not implemented");   // TODO
            }
            else if (affixEntry.LevelRequirement <= Properties[PropertyEnum.ItemAffixLevel])
            {
                if (affixProto is not AffixTeamUpPrototype teamUpAffixProto || teamUpAffixProto.IsAppliedToOwnerAvatar == false)
                {
                    if (Properties.AddChildCollection(affixEntry.Properties) == false)
                        return Logger.WarnReturn(false, "OnAffixAdded(): Properties.AddChildCollection(affixEntry.Properties) == false");
                }
            }

            _affixProperties.Add(affixEntry);
            return true;
        }

        private void OnItemEventRoll(int index)
        {
            Properties[PropertyEnum.ItemEventActionIndex] = index;
        }

        private float GenerateTruncatedFloatWithinRange(float randomMult, float min, float max)
        {
            float result = ((max - min + 1f) * randomMult) + min;
            // NOTE: Using regular Math.Clamp() doesn't work here because it throws when min > max.
            result = MathHelper.ClampNoThrow(result, min, max);
            return MathF.Floor(result);
        }

        private float GenerateFloatWithinRange(float randomMult, float min, float max)
        {
            return ((max - min) * randomMult) + min;
        }

        private int GenerateIntWithinRange(float randomMult, float min, float max)
        {
            return (int)GenerateTruncatedFloatWithinRange(randomMult, min, max);
        }

        private PrototypeId GetTriggeredPower(ItemEventType eventType, ItemActionType actionType)
        {
            //Logger.Warn($"GetTriggeredPower(): Not yet implemented (eventType={eventType}, actionType={actionType})");
            return PrototypeId.Invalid;
        }
    }
}
