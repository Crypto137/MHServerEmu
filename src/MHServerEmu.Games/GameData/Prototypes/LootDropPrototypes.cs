using MHServerEmu.Games.Loot;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class LootDropAgentPrototype : LootDropPrototype
    {
        public PrototypeId Agent { get; protected set; }
    }

    public class LootDropCharacterTokenPrototype : LootNodePrototype
    {
        public CharacterTokenType AllowedTokenType { get; protected set; }
        public CharacterFilterType FilterType { get; protected set; }
        public LootNodePrototype OnTokenUnavailable { get; protected set; }
    }

    public class LootDropClonePrototype : LootNodePrototype
    {
        public LootMutationPrototype[] Mutations { get; protected set; }
        public short SourceIndex { get; protected set; }
    }

    public class LootDropCreditsPrototype : LootNodePrototype
    {
        public CurveId Type { get; protected set; }
    }

    public class LootDropItemPrototype : LootDropPrototype
    {
        public PrototypeId Item { get; protected set; }
        public LootMutationPrototype[] Mutations { get; protected set; }
    }

    public class LootDropItemFilterPrototype : LootDropPrototype
    {
        public short ItemRank { get; protected set; }
        public EquipmentInvUISlot UISlot { get; protected set; }
    }

    public class LootDropPowerPointsPrototype : LootDropPrototype
    {
    }

    public class LootDropHealthBonusPrototype : LootDropPrototype
    {
    }

    public class LootDropEnduranceBonusPrototype : LootDropPrototype
    {
    }

    public class LootDropXPPrototype : LootNodePrototype
    {
        public CurveId XPCurve { get; protected set; }
    }

    public class LootDropRealMoneyPrototype : LootDropPrototype
    {
        public LocaleStringId CouponCode { get; protected set; }
        public PrototypeId TransactionContext { get; protected set; }
    }

    public class LootDropBannerMessagePrototype : LootNodePrototype
    {
        public PrototypeId BannerMessage { get; protected set; }
    }

    public class LootDropUsePowerPrototype : LootNodePrototype
    {
        public PrototypeId Power { get; protected set; }
    }

    public class LootDropPlayVisualEffectPrototype : LootNodePrototype
    {
        public AssetId RecipientVisualEffect { get; protected set; }
        public AssetId DropperVisualEffect { get; protected set; }
    }

    public class LootDropChatMessagePrototype : LootNodePrototype
    {
        public LocaleStringId ChatMessage { get; protected set; }
        public PlayerScope MessageScope { get; protected set; }
    }

    public class LootDropVanityTitlePrototype : LootNodePrototype
    {
        public PrototypeId VanityTitle { get; protected set; }
    }

    public class LootDropVendorXPPrototype : LootNodePrototype
    {
        public PrototypeId Vendor { get; protected set; }
        public int XP { get; protected set; }
    }
}
