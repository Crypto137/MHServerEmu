using System.Text;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
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
    public class Item : WorldEntity
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private ItemSpec _itemSpec = new();

        public ItemPrototype ItemPrototype { get => Prototype as ItemPrototype; }

        public ItemSpec ItemSpec { get => _itemSpec; }
        public PrototypeId OnUsePower { get; set; }
        public bool IsBoundToAccount { get; set; }

        public bool WouldBeDestroyedOnDrop { get => IsBoundToAccount || GameDatabase.DebugGlobalsPrototype.TrashedItemsDropInWorld == false; }

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

        private bool ApplyItemSpec(ItemSpec itemSpec)
        {
            if (itemSpec.IsValid == false) return Logger.WarnReturn(false, $"ApplyItemSpec(): Invalid ItemSpec on Item {this}!");

            _itemSpec.Set(itemSpec);

            ItemPrototype itemProto = ItemPrototype;
            if (itemProto == null) return Logger.WarnReturn(false, "ApplyItemSpec(): itemProto == null");

            if (ApplyItemSpecProperties() == false)
                return Logger.WarnReturn(false, "ApplyItemSpec(): Failed to apply ItemSpec properties");

            itemProto.OnApplyItemSpec(this, _itemSpec);

            GRandom random = new(_itemSpec.Seed);

            if (itemProto.PropertiesBuiltIn.HasValue())
            {
                foreach (PropertyEntryPrototype propertyEntryProto in itemProto.PropertiesBuiltIn)
                {
                    float randomMult = random.NextFloat();

                    /* Uncomment to enable built-in stats
                    if (propertyEntryProto is PropertyPickInRangeEntryPrototype pickInRangeProto)
                        OnBuiltInPropertyRoll(randomMult, pickInRangeProto);
                    else if (propertyEntryProto is PropertySetEntryPrototype setProto)
                        OnBuiltInPropertySet(setProto);
                    else
                        Logger.Warn($"ApplyItemSpec(): Invalid property entry prototype {propertyEntryProto}");
                    */
                }
            }

            // TODO: Apply affixes

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

        private bool OnBuiltInPropertyRoll(float randomMult, PropertyPickInRangeEntryPrototype pickInRangeProto)
        {
            PropertyInfo propertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(pickInRangeProto.Prop.Enum);
            PropertyDataType propDataType = propertyInfo.DataType;

            if (propDataType != PropertyDataType.Boolean && propDataType != PropertyDataType.Real && propDataType != PropertyDataType.Integer)
            {
                return Logger.WarnReturn(false, "OnBuiltInPropertyRoll(): The following Item has a built-in pick-in-range PropertyEntry with a property " +
                    $"that is not an int/float/bool prop, which doesn't work!\nItem: [{this}]\nProperty: [{propertyInfo.PropertyName}]");
            }

            EvalContextData contextData = new();
            contextData.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, Properties);

            float min = 0f;
            if (pickInRangeProto.ValueMin != null)
                min = Eval.RunFloat(pickInRangeProto.ValueMin, contextData);

            float max = 0f;
            if (pickInRangeProto.ValueMax != null)
                max = Eval.RunFloat(pickInRangeProto.ValueMax, contextData);

            if (propDataType == PropertyDataType.Real)
            {
                float value = pickInRangeProto.RollAsInteger
                    ? GenerateTruncatedFloatWithinRange(randomMult, min, max)
                    : GenerateFloatWithinRange(randomMult, min, max);

                Properties[pickInRangeProto.Prop] = value;
            }
            else if (propDataType == PropertyDataType.Integer)
            {
                Properties[pickInRangeProto.Prop] = GenerateIntWithinRange(randomMult, min, max);
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

            EvalContextData contextData = new();
            contextData.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, Properties);

            switch (propDataType)
            {
                case PropertyDataType.Real:
                    Properties[setProto.Prop] = setProto.Value != null ? Eval.RunFloat(setProto.Value, contextData) : 0f;
                    break;

                case PropertyDataType.Integer:
                    Properties[setProto.Prop] = setProto.Value != null ? Eval.RunInt(setProto.Value, contextData) : 0;
                    break;

                case PropertyDataType.Asset:
                    Properties[setProto.Prop] = setProto.Value != null ? Eval.RunAssetId(setProto.Value, contextData) : AssetId.Invalid;
                    break;
            }

            return true;
        }

        private float GenerateTruncatedFloatWithinRange(float randomMult, float min, float max)
        {
            float result = ((max - min + 1f) * randomMult) + min;
            result = Math.Clamp(result, min, max);
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
    }
}
