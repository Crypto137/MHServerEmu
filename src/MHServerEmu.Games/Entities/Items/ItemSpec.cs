using System.Text;
using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;

namespace MHServerEmu.Games.Entities.Items
{
    public class ItemSpec : ISerialize
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private PrototypeId _itemProtoRef;
        private PrototypeId _rarityProtoRef;
        private int _itemLevel;
        private int _creditsAmount;
        private List<AffixSpec> _affixSpecList = new();
        private int _seed;
        private PrototypeId _equippableBy;

        public PrototypeId ItemProtoRef { get => _itemProtoRef; }
        public PrototypeId RarityProtoRef { get => _rarityProtoRef; set => _rarityProtoRef = value; }
        public int ItemLevel { get => _itemLevel; set => _itemLevel = value; }
        public int CreditsAmount { get => _creditsAmount; }
        public IReadOnlyList<AffixSpec> AffixSpecs { get => _affixSpecList; }
        public int Seed { get => _seed; }
        public PrototypeId EquippableBy { get => _equippableBy; }

        public int StackCount { get; set; } = 1;

        public bool IsValid { get => _itemProtoRef != PrototypeId.Invalid && _rarityProtoRef != PrototypeId.Invalid; }

        public ItemSpec() { }

        public ItemSpec(PrototypeId itemProtoRef, PrototypeId rarityProtoRef, int itemLevel,
            int creditsAmount = 0, IEnumerable<AffixSpec> affixSpecs = null, int seed = 0, PrototypeId equippableBy = PrototypeId.Invalid)
        {
            _itemProtoRef = itemProtoRef;
            _rarityProtoRef = rarityProtoRef;
            _itemLevel = itemLevel;
            _creditsAmount = creditsAmount;

            if (affixSpecs != null)
                _affixSpecList.AddRange(affixSpecs);
            
            _seed = seed;
            _equippableBy = equippableBy;
        }

        public ItemSpec(NetStructItemSpec protobuf)
        {
            _itemProtoRef = (PrototypeId)protobuf.ItemProtoRef;
            _rarityProtoRef = (PrototypeId)protobuf.RarityProtoRef;
            _itemLevel = (int)protobuf.ItemLevel;

            if (protobuf.HasCreditsAmount)
                _creditsAmount = (int)protobuf.CreditsAmount;

            _affixSpecList.AddRange(protobuf.AffixSpecsList.Select(affixSpecProtobuf => new AffixSpec(affixSpecProtobuf)));
            _seed = (int)protobuf.Seed;

            if (protobuf.HasEquippableBy)
                _equippableBy = (PrototypeId)protobuf.EquippableBy;
        }

        public ItemSpec(ItemSpec other)
        {
            _itemProtoRef = other._itemProtoRef;
            _rarityProtoRef = other._rarityProtoRef;
            _itemLevel = other._itemLevel;
            _creditsAmount = other._creditsAmount;

            foreach (AffixSpec affixSpec in other._affixSpecList)
                _affixSpecList.Add(new(affixSpec));

            _seed = other._seed;
            _equippableBy = other._equippableBy;

            StackCount = other.StackCount;
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;
            success &= Serializer.Transfer(archive, ref _itemProtoRef);
            success &= Serializer.Transfer(archive, ref _rarityProtoRef);
            success &= Serializer.Transfer(archive, ref _itemLevel);
            success &= Serializer.Transfer(archive, ref _creditsAmount);
            success &= Serializer.Transfer(archive, ref _affixSpecList);
            success &= Serializer.Transfer(archive, ref _seed);
            success &= Serializer.Transfer(archive, ref _equippableBy);
            // StackCount is serialized as a property
            return success;
        }

        public NetStructItemSpec ToProtobuf()
        {
            return NetStructItemSpec.CreateBuilder()
                .SetItemProtoRef((ulong)_itemProtoRef)
                .SetRarityProtoRef((ulong)_rarityProtoRef)
                .SetItemLevel((uint)_itemLevel)
                .SetCreditsAmount((uint)_creditsAmount)
                .AddRangeAffixSpecs(_affixSpecList.Select(affixSpec => affixSpec.ToProtobuf()))
                .SetSeed((uint)_seed)
                .SetEquippableBy((ulong)_equippableBy)
                .Build();
        }

        public NetStructItemSpecStack ToStackProtobuf()
        {
            return NetStructItemSpecStack.CreateBuilder()
                .SetSpec(ToProtobuf())
                .SetCount((uint)StackCount)
                .Build();
        }

        public void Set(ItemSpec other)
        {
            if (other == null) throw new ArgumentException("other == null");
            if (ReferenceEquals(this, other)) return;

            _itemProtoRef = other._itemProtoRef;
            _rarityProtoRef = other._rarityProtoRef;
            _itemLevel = other._itemLevel;
            _creditsAmount = other._creditsAmount;
            _seed = other._seed;
            _equippableBy = other._equippableBy;

            _affixSpecList.Clear();
            foreach (AffixSpec otherAffixSpec in other._affixSpecList)
                _affixSpecList.Add(new(otherAffixSpec));
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(_itemProtoRef)}: {GameDatabase.GetPrototypeName(_itemProtoRef)}");
            sb.AppendLine($"{nameof(_rarityProtoRef)}: {GameDatabase.GetPrototypeName(_rarityProtoRef)}");
            sb.AppendLine($"{nameof(_itemLevel)}: {_itemLevel}");
            sb.AppendLine($"{nameof(_creditsAmount)}: {_creditsAmount}");

            for (int i = 0; i < _affixSpecList.Count; i++)
                sb.AppendLine($"{nameof(_affixSpecList)}[{i}]: {_affixSpecList[i]}");
            
            sb.AppendLine($"{nameof(_seed)}: 0x{_seed:X}");
            sb.AppendLine($"{nameof(_equippableBy)}: {GameDatabase.GetPrototypeName(_equippableBy)}");
            return sb.ToString();
        }

        public short NumAffixesOfCategory(AffixCategoryPrototype affixCategoryProto)
        {
            short numAffixes = 0;

            foreach (AffixSpec affixSpec in _affixSpecList)
            {
                if (affixSpec.AffixProto == null)
                {
                    Logger.Warn("NumAffixesOfCategory(): affixSpec.AffixProto == null");
                    continue;
                }

                if (affixSpec.AffixProto.HasCategory(affixCategoryProto))
                    numAffixes++;
            }

            return numAffixes;
        }

        public short NumAffixesOfPosition(AffixPosition affixPosition)
        {
            short numAffixes = 0;

            foreach (AffixSpec affixSpec in _affixSpecList)
            {
                if (affixSpec.AffixProto == null)
                {
                    Logger.Warn("NumAffixesOfPosition(): affixSpec.AffixProto == null");
                    continue;
                }

                if (affixSpec.AffixProto.Position == affixPosition)
                    numAffixes++;
            }

            return numAffixes;
        }

        public bool GetBindingState(out PrototypeId agentProtoRef)
        {
            // Binding state is stored as an affix scoped to the bound avatar's prototype
            PrototypeId itemBindingAffixProtoRef = GameDatabase.GlobalsPrototype.ItemBindingAffix;

            foreach (AffixSpec affixSpec in _affixSpecList)
            {
                // Skip non-binding affixes
                if (affixSpec.AffixProto.DataRef != itemBindingAffixProtoRef)
                    continue;

                // Found binding
                agentProtoRef = affixSpec.ScopeProtoRef;
                return true;
            }

            // No binding
            agentProtoRef = PrototypeId.Invalid;
            return false;
        }

        public bool GetBindingState()
        {
            return GetBindingState(out _);
        }

        public bool SetBindingState(bool bound, PrototypeId agentProtoRef = PrototypeId.Invalid, bool? tradeRestricted = null)
        {
            if (agentProtoRef != PrototypeId.Invalid && EquippableBy != PrototypeId.Invalid && agentProtoRef != EquippableBy)
            {
                Logger.Warn("SetBindingState(): Mismatch between equippable avatar and binding request agent detected, defaulting to account-bound");
                return SetBindingState(true);
            }

            if (bound == false && tradeRestricted == true)
                return Logger.WarnReturn(false, "SetBindingState(): Cannot set ItemSpec to both unbound and trade-restricted, not changing binding");

            // Binding state is stored as an affix scoped to the bound avatar's prototype
            PrototypeId itemBindingAffixProtoRef = GameDatabase.GlobalsPrototype.ItemBindingAffix;

            bool stateChanged = false;

            // Use a regular for loop instead of foreach to be able to remove the binding affix from the list
            for (int i = 0; i < _affixSpecList.Count; i++)
            {
                AffixSpec affixSpec = _affixSpecList[i];

                if (affixSpec.AffixProto.DataRef != itemBindingAffixProtoRef)
                    continue;

                if (bound == false)
                {
                    // Remove binding affix
                    _affixSpecList.RemoveAt(i);
                    return true;
                }

                if (affixSpec.ScopeProtoRef != agentProtoRef)
                {
                    // Change bind agent
                    affixSpec.ScopeProtoRef = agentProtoRef;
                    stateChanged = true;
                }

                // Looks like someone at Gazillion had a brilliant idea of storing trade restriction status in the seed field
                if (tradeRestricted == true && affixSpec.Seed != 2)
                {
                    affixSpec.Seed = 2;
                    stateChanged = true;
                }
                
                if (tradeRestricted == false && affixSpec.Seed == 2)
                {
                    affixSpec.Seed = 1;
                    stateChanged = true;
                }

                // This return will happen only if there is an existing binding affix in this ItemSpec
                return stateChanged;
            }

            if (bound == false)
                return false;

            // Add a new binding
            AffixPrototype affixProto = itemBindingAffixProtoRef.As<AffixPrototype>();

            int seed = 1;   // not trade restricted
            if (tradeRestricted == true)
                seed = 2;   // trade restricted

            _affixSpecList.Add(new(affixProto, agentProtoRef, seed));
            return true;
        }

        public bool AddAffixSpec(AffixSpec affixSpec)
        {
            if (affixSpec.IsValid == false)
                return Logger.WarnReturn(false, $"AddAffixSpec(): Trying to add invalid AffixSpec to ItemSpec! ItemSpec: {this}");

            _affixSpecList.Add(affixSpec);
            return true;
        }

        public MutationResults OnAffixesRolled(IItemResolver resolver, PrototypeId rollFor)
        {
            MutationResults result = MutationResults.None;
            PrototypeId equippableByBefore = _equippableBy;

            ItemPrototype itemProto = _itemProtoRef.As<ItemPrototype>();
            if (itemProto == null) return Logger.WarnReturn(MutationResults.Error, "OnAffixesRolled(): itemProto == null");

            // Change EquippableBy if needed
            if (itemProto.IsAvatarRestricted)
            {
                _equippableBy = rollFor;

                // Validate built-in affixes for the new equippableBy value
                if (itemProto.AffixesBuiltIn.HasValue())
                {
                    foreach (AffixEntryPrototype affixEntryProto in itemProto.AffixesBuiltIn)
                    {
                        if (affixEntryProto.Avatar != PrototypeId.Invalid && affixEntryProto.Avatar != _equippableBy)
                        {
                            Logger.Warn(string.Format("The Avatar required for this built-in affix is different than the item's equippableBy!\n" +
                                "Affix: {0}\nAvatar required: {1}\nSpec: {2}\nResolver: {3}",
                                affixEntryProto.Affix.GetName(),
                                affixEntryProto.Avatar.GetName(),
                                this,
                                resolver));
                        }
                    }
                }
            }
            else if (itemProto.IsGem == false) // RIP gems
            {
                _equippableBy = PrototypeId.Invalid;

                // Single power affixes and tab-specific affixes make this item bound to the avatar that power or tab belongs to
                foreach (AffixSpec affixSpec in _affixSpecList)
                {
                    if (affixSpec.ScopeProtoRef == PrototypeId.Invalid)
                        continue;

                    if (affixSpec.AffixProto == null)
                    {
                        Logger.Warn("OnAffixesRolled(): affixSpec.AffixProto == null");
                        continue;
                    }

                    if (affixSpec.AffixProto is not AffixPowerModifierPrototype affixPowerModifierProto)
                        continue;

                    if (affixPowerModifierProto.IsForSinglePowerOnly || affixPowerModifierProto.PowerProgTableTabRef != PrototypeId.Invalid)
                        _equippableBy = rollFor;
                }
            }

            // Finalize EquippableBy change if it happened
            if (EquippableBy != equippableByBefore)
            {
                result |= MutationResults.Changed;

                // Update binding affix
                if (_equippableBy != PrototypeId.Invalid && GetBindingState(out PrototypeId boundAgentProtoRef) && boundAgentProtoRef != _equippableBy)
                    SetBindingState(true, _equippableBy);
            }

            return result;
        }
    }
}
