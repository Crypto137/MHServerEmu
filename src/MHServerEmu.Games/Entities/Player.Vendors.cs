using Gazillion;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Dialog;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Entities
{
    public enum VendorResult    // Names from CPlayer::BuyItemFromVendor(), CPlayer::SellItemToVendor(), CPlayer::DonateItemToVendor()
    {
        BuySuccess,
        BuyResult1,
        BuyOutOfRange2,
        BuyInsufficientCredits,
        BuyInsufficientPrestige,
        BuyCannotAffordItem,
        BuyInventoryFull,
        BuyResult7,
        BuyOutOfRange8,
        BuyAvatarUltimateAlreadyMaxedOut,
        BuyAvatarUltimateUpgradeCurrentOnly,
        BuyCharacterAlreadyUnlocked,
        BuyPlayerAlreadyHasCraftingRecipe,
        BuyItemDisabledByLiveTuning,

        SellSuccess,
        SellNotAllowed,

        DonateSuccess,
        DonateResult17,
        DonateNotAcceptingDonations,
        DonateNotAcceptingItem,

        RefreshSuccess,
        RefreshResult21,
        RefreshResult22,
        RefreshResult23,

        UnkResult24,
        UnkResult25,

        OpSuccess,
        OpResult27,
    }

    public partial class Player
    {
        private const int VendorMinLevel = 1;
        private const int VendorInvalidXP = -1;

        private readonly HashSet<PrototypeId> _initializedVendorProtoRefs = new();

        public void InitializeVendorInventory(PrototypeId inventoryProtoRef)
        {
            foreach (PrototypeId vendorTypeProtoRef in DataDirectory.Instance.IteratePrototypesInHierarchy<VendorTypePrototype>(PrototypeIterateFlags.NoAbstract))
            {
                VendorTypePrototype vendorTypeProto = vendorTypeProtoRef.As<VendorTypePrototype>();
                if (vendorTypeProto.ContainsInventory(inventoryProtoRef))
                {
                    RollVendorInventory(vendorTypeProto, true);
                    return;
                }
            }
        }

        public bool AwardVendorXP(int amount, PrototypeId vendorProtoRef)
        {
            // TODO: Implement this
            // NOTE: Do weekly rollover checks and reset ePID_VendorXPCapCounter when rolling LootDropVendorXP
            Logger.Debug($"AwardVendorXP(): amount=[{amount}], vendorProtoRef=[{vendorProtoRef}], player=[{this}]");
            return true;
        }

        private void InitializeVendors()
        {
            foreach (PrototypeId vendorTypeProtoRef in DataDirectory.Instance.IteratePrototypesInHierarchy<VendorTypePrototype>(PrototypeIterateFlags.NoAbstract))
            {
                // Vendor level is not persisted, so it can be used to check if a vendor has been initialized already
                if (Properties[PropertyEnum.VendorLevel, vendorTypeProtoRef] != 0)
                    continue;

                VendorTypePrototype vendorTypeProto = vendorTypeProtoRef.As<VendorTypePrototype>();
                TryLevelUpVendor(vendorTypeProto, InvalidId, true);

                if (Properties[PropertyEnum.VendorLevel, vendorTypeProtoRef] == 0)
                {
                    Logger.Warn($"InitializeVendors(): Failed to initialize vendor level for vendor type {vendorTypeProto}");
                    continue;
                }

                if (Properties[PropertyEnum.VendorRollSeed, vendorTypeProtoRef] == 0)
                {
                    UpdateVendorLootProperties(vendorTypeProto);
                    SetVendorEnergyPct(vendorTypeProtoRef, 1f);
                }
            }
        }

        private bool TryLevelUpVendor(VendorTypePrototype vendorTypeProto, ulong vendorId, bool isInitializing)
        {
            if (vendorTypeProto == null) return Logger.WarnReturn(false, "TryLevelUpVendor(): vendorTypeProto == null");
            PrototypeId vendorTypeProtoRef = vendorTypeProto.DataRef;

            if (CalculateVendorLevel(vendorTypeProto, out int newLevel) == false)
                return true;

            int oldLevel = Properties[PropertyEnum.VendorLevel, vendorTypeProtoRef];
            Properties[PropertyEnum.VendorLevel, vendorTypeProtoRef] = newLevel;

            if (isInitializing || oldLevel == newLevel)
                return true;

            return OnVendorLevelUp(vendorTypeProto, vendorId, newLevel);
        }

        private bool CalculateVendorLevel(VendorTypePrototype vendorTypeProto, out int newLevel)
        {
            newLevel = 0;

            if (vendorTypeProto == null) return Logger.WarnReturn(false, "CalculateVendorLevel(): vendorTypeProto == null");
            PrototypeId vendorTypeProtoRef = vendorTypeProto.DataRef;

            Curve levelingCurve = GetVendorLevelingCurve(vendorTypeProto);
            if (levelingCurve == null) return Logger.WarnReturn(false, "CalculateVendorLevel(): levelingCurve == null");

            int maxLevel = GetVendorMaxLevel(vendorTypeProto);
            int oldLevel = Properties[PropertyEnum.VendorLevel, vendorTypeProtoRef];
            int nextLevel = levelingCurve.MinPosition + 1;

            int xp = Properties[PropertyEnum.VendorXP, vendorTypeProtoRef];
            int prevXPRequirement = 0;
            int nextXPRequirement = GetVendorXPRequirement(vendorTypeProto, nextLevel);

            // Each next xp requirement has to big larger than the previous one, or things are going to break
            while (nextXPRequirement <= xp && nextXPRequirement > prevXPRequirement && nextLevel <= maxLevel)
            {
                prevXPRequirement = nextXPRequirement;

                if (++nextLevel > maxLevel)
                    break;

                nextXPRequirement = GetVendorXPRequirement(vendorTypeProto, nextLevel);
            }

            newLevel = nextLevel - 1;

            return newLevel != oldLevel;
        }

        private Curve GetVendorLevelingCurve(VendorTypePrototype vendorTypeProto)
        {
            if (vendorTypeProto == null) return Logger.WarnReturn<Curve>(null, "GetVendorLevelingCurve(): vendorTypeProto == null");

            CurveId vendorLevelingCurveId = vendorTypeProto.VendorLevelingCurve;
            if (vendorLevelingCurveId == CurveId.Invalid) return Logger.WarnReturn<Curve>(null, "GetVendorLevelingCurve(): vendorLevelingCurveId == CurveId.Invalid");

            return CurveDirectory.Instance.GetCurve(vendorLevelingCurveId);
        }

        private int GetVendorMaxLevel(VendorTypePrototype vendorTypeProto)
        {
            if (vendorTypeProto == null) return Logger.WarnReturn(0, "GetVendorMaxLevel(): vendorTypeProto == null");
            PrototypeId vendorTypeProtoRef = vendorTypeProto.DataRef;

            int currentLevel = Properties[PropertyEnum.VendorLevel, vendorTypeProtoRef];

            Curve levelingCurve = GetVendorLevelingCurve(vendorTypeProto);
            if (levelingCurve == null) return Logger.WarnReturn(0, "GetVendorMaxLevel(): levelingCurve == null");

            // No more valid data in the curve = we are at max level
            int nextLevel = currentLevel + 1;
            if (nextLevel <= levelingCurve.MaxPosition && levelingCurve.GetIntAt(nextLevel) < 0)
                return currentLevel;

            return levelingCurve.MaxPosition;
        }

        private int GetVendorXPRequirement(VendorTypePrototype vendorTypeProto, int level)
        {
            if (vendorTypeProto == null) return Logger.WarnReturn(VendorInvalidXP, "GetVendorXPRequirement(): vendorTypeProto == null");
            if (level < VendorMinLevel) return Logger.WarnReturn(VendorInvalidXP, "GetVendorXPRequirement(): level < VendorMinLevel");

            Curve levelingCurve = GetVendorLevelingCurve(vendorTypeProto);
            if (levelingCurve == null) return Logger.WarnReturn(VendorInvalidXP, "GetVendorXPRequirement(): levelingCurve == null");

            if (level < levelingCurve.MinPosition || level > levelingCurve.MaxPosition)
                return Logger.WarnReturn(VendorInvalidXP, "GetVendorXPRequirement(): level < levelingCurve.MinPosition || level > levelingCurve.MaxPosition");

            return levelingCurve.GetIntAt(level);
        }

        private bool OnVendorLevelUp(VendorTypePrototype vendorTypeProto, ulong vendorId, int newLevel)
        {
            if (vendorTypeProto == null) return Logger.WarnReturn(false, "OnVendorLevelUp(): vendorTypeProto == null");
            PrototypeId vendorTypeProtoRef = vendorTypeProto.DataRef;

            SetVendorEnergyPct(vendorTypeProtoRef, 1f);

            if (vendorTypeProto.IsCrafter)
            {
                UpdateVendorLootProperties(vendorTypeProto);
                RollVendorInventory(vendorTypeProto, false);
            }

            if (vendorId != InvalidId)
                SendMessage(NetMessageVendorLevelUp.CreateBuilder().SetVendorTypeProtoId((ulong)vendorTypeProtoRef).SetVendorID(vendorId).Build());

            return true;
        }

        private bool UpdateVendorLootProperties(VendorTypePrototype vendorTypeProto)
        {
            if (vendorTypeProto == null) return Logger.WarnReturn(false, "UpdateVendorLootProperties(): vendorTypeProto == null");
            PrototypeId vendorTypeProtoRef = vendorTypeProto.DataRef;

            // VendorRollAvatar / VendorRollLevel
            Avatar avatar = CurrentAvatar;

            if (avatar != null)
            {
                Properties[PropertyEnum.VendorRollAvatar, vendorTypeProto.DataRef] = avatar.PrototypeDataRef;
                Properties[PropertyEnum.VendorRollLevel, vendorTypeProto.DataRef] = avatar.CharacterLevel;
            }
            else
            {
                Properties[PropertyEnum.VendorRollAvatar, vendorTypeProto.DataRef] = PrototypeId.Invalid;
                Properties[PropertyEnum.VendorRollLevel, vendorTypeProto.DataRef] = 1;
            }

            // VendorRollSeed
            int oldSeed = Properties[PropertyEnum.VendorRollSeed, vendorTypeProtoRef];
            int newSeed = oldSeed;

            while (newSeed == oldSeed || newSeed == 0)
                newSeed = Game.Random.Next();

            Properties[PropertyEnum.VendorRollSeed, vendorTypeProtoRef] = newSeed;

            // VendorRollTableLevel
            int tableLevel;
            if (vendorTypeProto.IsCrafter == false)
            {
                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, Properties);
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, avatar?.Properties);
                evalContext.SetReadOnlyVar_ProtoRef(EvalContext.Var1, vendorTypeProtoRef);
                tableLevel = Eval.RunInt(GameDatabase.AdvancementGlobalsPrototype.VendorRollTableLevelEval, evalContext);
            }
            else
            {
                // Table level is equal to vendor level for crafting vendors
                tableLevel = Properties[PropertyEnum.VendorLevel, vendorTypeProtoRef];
            }

            Properties[PropertyEnum.VendorRollTableLevel, vendorTypeProtoRef] = tableLevel;

            return true;
        }

        private bool SetVendorEnergyPct(PrototypeId vendorTypeProtoRef, float energyPct)
        {
            if (vendorTypeProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "SetVendorEnergyPct(): vendorTypeProtoRef == PrototypeId.Invalid");

            Properties[PropertyEnum.VendorLastRefreshPctEngAfter, vendorTypeProtoRef] = Math.Clamp(energyPct, 0f, 1f);
            Properties[PropertyEnum.VendorLastRefreshTime, vendorTypeProtoRef] = Game.CurrentTime;

            return true;
        }

        private bool RollVendorInventory(VendorTypePrototype vendorTypeProto, bool isInitializing)
        {
            Logger.Debug($"RollVendorInventory(): {vendorTypeProto}");

            if (vendorTypeProto == null) return Logger.WarnReturn(false, "RollVendorInventory(): vendorTypeProto == null");
            PrototypeId vendorTypeProtoRef = vendorTypeProto.DataRef;

            if (isInitializing && _initializedVendorProtoRefs.Add(vendorTypeProtoRef) == false)
                return true;

            // TODO: roll inventory contents here

            return true;
        }

        private VendorResult CanBuyItemFromVendor(int avatarIndex, ulong itemId, ulong vendorId)
        {
            // TODO
            return VendorResult.BuySuccess;
        }

        private VendorResult CanSellItemToVendor(int avatarIndex, ulong itemId, ulong vendorId)
        {
            // TODO
            return VendorResult.SellSuccess;
        }

        private VendorResult CanDonateItemToVendor(int avatarIndex, ulong itemId, ulong vendorId)
        {
            // TODO
            return VendorResult.DonateSuccess;
        }

        private VendorResult CanRefreshVendor(ulong arg0)
        {
            // TODO
            return VendorResult.RefreshSuccess;
        }

        private VendorResult CanPerformVendorOpAtVendor(int avatarIndex, ulong itemId, ulong vendorId, InteractionMethod interactionMethod)
        {
            // TODO
            return VendorResult.OpSuccess;
        }
    }
}
