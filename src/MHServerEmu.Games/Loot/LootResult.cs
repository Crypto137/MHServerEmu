using System.Runtime.InteropServices;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot.Specs;

namespace MHServerEmu.Games.Loot
{
    /// <summary>
    /// A container for various types of loot.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct LootResult
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        [FieldOffset(0)]
        private readonly LootType _type = default;

        [FieldOffset(4)]
        private readonly int _amount = default;

        // Reference types (need to be at offset 8 for memory alignment reasons)
        [FieldOffset(8)]
        private readonly ItemSpec _itemSpec = default;
        [FieldOffset(8)]
        private readonly LootDropRealMoneyPrototype _realMoneyProto = default;
        [FieldOffset(8)]
        private readonly LootNodePrototype _callbackNodeProto = default;
        [FieldOffset(8)]
        private readonly LootMutationPrototype _lootMutationProto = default;

        // Vaporization flag
        [FieldOffset(16)]
        private readonly bool _isVaporized = default;

        // Value types
        [FieldOffset(20)]
        private readonly AgentSpec _agentSpec = default;
        [FieldOffset(20)]
        private readonly CurveId _xpCurveRef = default;
        [FieldOffset(20)]
        private readonly PrototypeId _vanityTitleProtoRef = default;
        [FieldOffset(20)]
        private readonly VendorXPSummary _vendorXPSummary = default;
        [FieldOffset(20)]
        private readonly CurrencySpec _currencySpec = default;

        public LootType Type { get => _type; }
        public int Amount { get => _amount; }
        public bool IsVaporized { get => _isVaporized; }
        
        public ItemSpec ItemSpec { get => _type == LootType.Item ? _itemSpec : null; }
        public LootDropRealMoneyPrototype RealMoneyProto { get => _type == LootType.RealMoney ? _realMoneyProto : null; }
        public LootNodePrototype CallbackNodeProto { get => _type == LootType.CallbackNode ? _callbackNodeProto : null; }
        public LootMutationPrototype LootMutationProto { get => _type == LootType.LootMutation ? _lootMutationProto : null; }

        public AgentSpec AgentSpec { get => _type == LootType.Agent ? _agentSpec : default; }
        public PrototypeId VanityTitleProtoRef { get => _type == LootType.VanityTitle ? _vanityTitleProtoRef : PrototypeId.Invalid; }
        public CurveId XPCurveRef { get => _type == LootType.Experience ? _xpCurveRef : CurveId.Invalid; }
        public VendorXPSummary VendorXPSummary { get => _type == LootType.VendorXP ? _vendorXPSummary : default; }
        public CurrencySpec CurrencySpec { get => _type == LootType.Currency ? _currencySpec : default; }

        public LootResult(ItemSpec itemSpec, bool isVaporized = false)
        {
            _type = LootType.Item;
            _itemSpec = itemSpec;
            _isVaporized = isVaporized;
        }

        public LootResult(in AgentSpec agentSpec)
        {
            _type = LootType.Agent;
            _agentSpec = agentSpec;
        }

        public LootResult(CurveId xpCurveRef, int amount)
        {
            _type = LootType.Experience;
            _amount = amount;
            _xpCurveRef = xpCurveRef;
        }

        public LootResult(LootType type, int amount, bool isVaporized = false)
        {
            switch (type)
            {
                case LootType.Credits:
                case LootType.PowerPoints:
                case LootType.HealthBonus:
                case LootType.EnduranceBonus:
                    break;

                default:
                    Logger.Warn($"LootResult(): Unsupported LootType {type} for the amount-based constructor");
                    return;
            }

            if (amount < 0)
            {
                Logger.Warn($"LootResult(): Invalid amount {amount} for LootType {type}");
                return;
            }

            _type = type;
            _amount = amount;
            _isVaporized = isVaporized;
        }

        public LootResult(LootDropRealMoneyPrototype lootDropRealMoneyProto)
        {
            _type = LootType.RealMoney;
            _realMoneyProto = lootDropRealMoneyProto;
        }

        public LootResult(LootNodePrototype callbackNodeProto)
        {
            _type = LootType.CallbackNode;
            _callbackNodeProto = callbackNodeProto;
        }

        public LootResult(LootMutationPrototype lootMutationProto)
        {
            _type = LootType.LootMutation;
            _lootMutationProto = lootMutationProto;
        }

        public LootResult(PrototypeId vanityTitleProtoRef)
        {
            _type = LootType.VanityTitle;
            _vanityTitleProtoRef = vanityTitleProtoRef;
        }

        public LootResult(in VendorXPSummary vendorXPSummary)
        {
            _type = LootType.VendorXP;
            _vendorXPSummary = vendorXPSummary;
        }

        public LootResult(in CurrencySpec currencySpec)
        {
            _type = LootType.Currency;
            _currencySpec = currencySpec;
        }
    }
}
