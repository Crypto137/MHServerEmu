using Gazillion;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.PowerCollections;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Loot.Visitors;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class LootDropAgentPrototype : LootDropPrototype
    {
        public PrototypeId Agent { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();

            if (Agent != PrototypeId.Invalid && GameDatabase.DataDirectory.PrototypeIsAbstract(Agent))
                Agent = PrototypeId.Invalid;
        }

        public static LootRollResult RollAgent(WorldEntityPrototype agentProto, int numAgents, LootRollSettings settings, IItemResolver resolver)
        {
            LootRollResult result = LootRollResult.NoRoll;

            if (numAgents < 1)
                return result;

            switch (resolver.LootContext)
            {
                case LootContext.AchievementReward:
                case LootContext.LeaderboardReward:
                case LootContext.Drop:
                case LootContext.MissionReward:
                    break;

                default:
                    return LootRollResult.Failure;
            }

            RestrictionTestFlags restrictionFlags = RestrictionTestFlags.All;
            if (settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.PreviewOnly) || settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.IgnoreCooldown))
                restrictionFlags &= ~RestrictionTestFlags.Cooldown;

            if (agentProto.IsCurrency)
            {
                for (int i = 0; i < numAgents; i++)
                {
                    result |= resolver.PushCurrency(agentProto, null, restrictionFlags, settings.DropChanceModifiers, 1);
                    if (result.HasFlag(LootRollResult.Failure))
                    {
                        resolver.ClearPending();
                        return LootRollResult.Failure;
                    }
                }
            }
            else
            {
                for (int i = 0; i < numAgents; i++)
                {
                    int level = resolver.ResolveLevel(settings.Level, true);
                    result |= resolver.PushAgent(agentProto.DataRef, level, restrictionFlags);
                    if (result.HasFlag(LootRollResult.Failure))
                    {
                        resolver.ClearPending();
                        return LootRollResult.Failure;
                    }
                }
            }

            return resolver.ProcessPending(settings) ? result : LootRollResult.Failure;
        }

        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            if (Agent == PrototypeId.Invalid)
                return LootRollResult.NoRoll;

            WorldEntityPrototype agentProto = Agent.As<WorldEntityPrototype>();
            int numAgents = NumMin == NumMax ? NumMin : resolver.Random.Next(NumMin, NumMax + 1);

            return RollAgent(agentProto, numAgents, settings, resolver);
        }
    }

    public class LootDropCharacterTokenPrototype : LootNodePrototype
    {
        public CharacterTokenType AllowedTokenType { get; protected set; }
        public CharacterFilterType FilterType { get; protected set; }
        public LootNodePrototype OnTokenUnavailable { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override void Visit<T>(T visitor)
        {
            base.Visit(visitor);

            OnTokenUnavailable?.Visit(visitor);
        }

        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            Logger.Warn($"Roll(): AllowedTokenType={AllowedTokenType}, FilterType={FilterType}");
            return base.Roll(settings, resolver);
        }
    }

    public class LootDropClonePrototype : LootNodePrototype
    {
        public LootMutationPrototype[] Mutations { get; protected set; }
        public short SourceIndex { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            Logger.Warn($"Roll()");
            return LootRollResult.NoRoll;
        }
    }

    public class LootDropCreditsPrototype : LootNodePrototype
    {
        public CurveId Type { get; protected set; }

        //---

        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            LootRollResult result = LootRollResult.NoRoll;

            if (Type == CurveId.Invalid)
                return result;

            int level = resolver.ResolveLevel(settings.Level, settings.UseLevelVerbatim);
            Curve curve = CurveDirectory.Instance.GetCurve(Type);
            
            int amount = curve.GetIntAt(level);
            amount = resolver.Random.Next(amount, amount * 3 / 2 + 1);

            result = resolver.PushCredits(amount);
            if (result.HasFlag(LootRollResult.Failure))
            {
                resolver.ClearPending();
                return LootRollResult.Failure;
            }

            return resolver.ProcessPending(settings) ? result : LootRollResult.Failure;
        }
    }

    public class LootDropItemPrototype : LootDropPrototype
    {
        public PrototypeId Item { get; protected set; }
        public LootMutationPrototype[] Mutations { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override void OnResultsEvaluation(Player player, WorldEntity source)
        {
            if (Item == PrototypeId.Invalid || DataDirectory.Instance.PrototypeIsA<CostumePrototype>(Item) == false)
            {
                Logger.Warn($"LootDropItemPrototype::OnResultsEvaluation() is only supported for Costumes!");
                return;
            }

            // Unlock costume for costume closet (consoles / 1.53)
            // player.UnlockCostume(Item);
        }

        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            if (Item == PrototypeId.Invalid)
                return LootRollResult.NoRoll;

            int numItems = NumMin == NumMax ? NumMin : resolver.Random.Next(NumMin, NumMax + 1);

            return RollItem(Item.As<ItemPrototype>(), numItems, settings, resolver, Mutations);
        }
    }

    public class LootDropItemFilterPrototype : LootDropPrototype
    {
        public short ItemRank { get; protected set; }
        public EquipmentInvUISlot UISlot { get; protected set; }

        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            LootRollResult result = LootRollResult.NoRoll;

            if (NumMin < 1 || ItemRank < 0 || UISlot == EquipmentInvUISlot.Invalid)
                return result;

            AvatarPrototype usableAvatarProto = settings.UsableAvatar;

            RestrictionTestFlags restrictionFlags = RestrictionTestFlags.All;
            if (settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.IgnoreCooldown) ||
                settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.PreviewOnly))
            {
                restrictionFlags &= ~RestrictionTestFlags.Cooldown;
            }

            int numRolls = NumMin == NumMax ? NumMin : resolver.Random.Next(NumMin, NumMax + 1);

            for (int i = 0; i < numRolls; i++)
            {
                int level = resolver.ResolveLevel(settings.Level, settings.UseLevelVerbatim);
                AvatarPrototype resolvedAvatarProto = resolver.ResolveAvatarPrototype(usableAvatarProto, settings.ForceUsable, settings.UsablePercent);
                PrototypeId rollFor = resolvedAvatarProto != null ? resolvedAvatarProto.DataRef : PrototypeId.Invalid;

                Picker<Prototype> picker = new(resolver.Random);
                LootUtilities.BuildInventoryLootPicker(picker, rollFor, UISlot);

                if (picker.Empty())
                {
                    resolver.ClearPending();
                    return LootRollResult.Failure;
                }

                PrototypeId? rarityProtoRef = resolver.ResolveRarity(settings.Rarities, level, null);
                if (rarityProtoRef == PrototypeId.Invalid)
                {
                    resolver.ClearPending();
                    return LootRollResult.Failure;
                }

                ItemPrototype itemProto = null;

                using DropFilterArguments filterArgs = ObjectPoolManager.Instance.Get<DropFilterArguments>();
                DropFilterArguments.Initialize(filterArgs, itemProto, rollFor, level, rarityProtoRef.Value, ItemRank, UISlot, resolver.LootContext);

                if (LootUtilities.PickValidItem(resolver, picker, null, filterArgs, ref itemProto, RestrictionTestFlags.All, ref rarityProtoRef) == false)
                {
                    resolver.ClearPending();
                    return LootRollResult.Failure;
                }

                filterArgs.Rarity = rarityProtoRef.Value;
                filterArgs.ItemProto = itemProto;

                result |= resolver.PushItem(filterArgs, restrictionFlags, 1, null);

                if (result.HasFlag(LootRollResult.Failure))
                {
                    resolver.ClearPending();
                    return LootRollResult.Failure;
                }
            }

            return resolver.ProcessPending(settings) ? result : LootRollResult.Failure;
        }
    }

    public class LootDropPowerPointsPrototype : LootDropPrototype
    {
        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            LootRollResult result = LootRollResult.NoRoll;

            if (NumMin <= 0)
                return result;

            result = resolver.PushPowerPoints(NumMin);
            if (result.HasFlag(LootRollResult.Failure))
            {
                resolver.ClearPending();
                return LootRollResult.Failure;
            }

            return resolver.ProcessPending(settings) ? result : LootRollResult.Failure;
        }
    }

    public class LootDropHealthBonusPrototype : LootDropPrototype
    {
        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            LootRollResult result = LootRollResult.NoRoll;

            if (NumMin <= 0)
                return result;

            result = resolver.PushHealthBonus(NumMin);
            if (result.HasFlag(LootRollResult.Failure))
            {
                resolver.ClearPending();
                return LootRollResult.Failure;
            }

            return resolver.ProcessPending(settings) ? result : LootRollResult.Failure;
        }
    }

    public class LootDropEnduranceBonusPrototype : LootDropPrototype
    {
        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            LootRollResult result = LootRollResult.NoRoll;

            if (NumMin <= 0)
                return result;

            result = resolver.PushEnduranceBonus(NumMin);
            if (result.HasFlag(LootRollResult.Failure))
            {
                resolver.ClearPending();
                return LootRollResult.Failure;
            }

            return resolver.ProcessPending(settings) ? result : LootRollResult.Failure;
        }
    }

    public class LootDropXPPrototype : LootNodePrototype
    {
        public CurveId XPCurve { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            LootRollResult result = LootRollResult.NoRoll;

            if (XPCurve == CurveId.Invalid)
                return result;

            Curve xpCurve = CurveDirectory.Instance.GetCurve(XPCurve);
            if (xpCurve == null) return Logger.WarnReturn(result, "Roll(): xpCurve == null");

            int amount = (int)MathF.Ceiling(xpCurve.GetAt(settings.Level));

            result = resolver.PushXP(XPCurve, amount);
            if (result.HasFlag(LootRollResult.Failure))
            {
                resolver.ClearPending();
                return LootRollResult.Failure;
            }

            return resolver.ProcessPending(settings) ? result : LootRollResult.Failure;
        }
    }

    public class LootDropRealMoneyPrototype : LootDropPrototype
    {
        public LocaleStringId CouponCode { get; protected set; }
        public PrototypeId TransactionContext { get; protected set; }

        //---

        // NOTE: This loot drop type appears to had been used only for the Vibranium Ticket promotion during the game's second anniversary.
        // See Loot/Tables/Mob/Bosses/GoldenTicketTable.prototype for reference.

        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            LootRollResult result = LootRollResult.NoRoll;

            if (NumMin <= 0 || CouponCode == LocaleStringId.Invalid)
                return result;

            result = resolver.PushRealMoney(this);
            if (result.HasFlag(LootRollResult.Failure))
            {
                resolver.ClearPending();
                return LootRollResult.Failure;
            }

            return resolver.ProcessPending(settings) ? result : LootRollResult.Failure;
        }
    }

    public class LootDropBannerMessagePrototype : LootNodePrototype
    {
        public PrototypeId BannerMessage { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override void OnResultsEvaluation(Player player, WorldEntity dropper)
        {
            Logger.Debug($"OnResultsEvaluation(): BannerMessage={BannerMessage.GetName()}");

            player.SendBannerMessage(BannerMessage.As<BannerMessagePrototype>());
        }

        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            return PushLootNodeCallback(settings, resolver);
        }
    }

    public class LootDropUsePowerPrototype : LootNodePrototype
    {
        public PrototypeId Power { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override void OnResultsEvaluation(Player player, WorldEntity dropper)
        {
            Logger.Debug($"OnResultsEvaluation(): Power={Power.GetName()}");

            if (dropper == null)
            {
                Logger.Warn("OnResultsEvaluation(): dropper == null");
                return;
            }

            Avatar avatar = player.CurrentAvatar;
            if (avatar == null) return;

            Power power = dropper.GetPower(Power);
            if (power == null)
            {
                PowerIndexProperties props = new();
                if (dropper.AssignPower(Power, props) == null)
                    Logger.Warn($"OnResultsEvaluation(): Failed to assign power on dropper!\nPower: {Power.GetName()}\nDropper {dropper}:\nNode: {this}");
            }

            PowerActivationSettings settings = new(avatar.Id, avatar.RegionLocation.Position, dropper.RegionLocation.Position);
            settings.Flags |= PowerActivationSettingsFlags.SkipRangeCheck;

            PowerUseResult result = dropper.ActivatePower(Power, ref settings);
            if (result != PowerUseResult.Success)
                Logger.Warn($"OnResultsEvaluation(): Failed to activate power!\nPowerUseResult: {result}\nPower: {Power.GetName()}\nDropper: {dropper}\nNode: {this}");
        }

        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            return PushLootNodeCallback(settings, resolver);
        }
    }

    public class LootDropPlayVisualEffectPrototype : LootNodePrototype
    {
        public AssetId RecipientVisualEffect { get; protected set; }
        public AssetId DropperVisualEffect { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override void OnResultsEvaluation(Player player, WorldEntity dropper)
        {
            Logger.Debug($"OnResultsEvaluation(): RecipientVisualEffect={RecipientVisualEffect.GetName()}, DropperVisualEffect={DropperVisualEffect.GetName()}");

            Game game = player?.Game;
            if (game == null) return;

            Avatar avatar = player.CurrentAvatar;
            if (avatar == null) return;

            if (RecipientVisualEffect != AssetId.Invalid)
            {
                NetMessagePlayPowerVisuals avatarVisualsMessage = NetMessagePlayPowerVisuals.CreateBuilder()
                    .SetEntityId(avatar.Id)
                    .SetPowerAssetRef((ulong)RecipientVisualEffect)
                    .Build();

                game.NetworkManager.SendMessageToInterested(avatarVisualsMessage, avatar, AOINetworkPolicyValues.AOIChannelProximity);
            }

            if (dropper != null && DropperVisualEffect != AssetId.Invalid)
            {
                NetMessagePlayPowerVisuals dropperVisualsMessage = NetMessagePlayPowerVisuals.CreateBuilder()
                    .SetEntityId(dropper.Id)
                    .SetPowerAssetRef((ulong)DropperVisualEffect)
                    .Build();

                game.NetworkManager.SendMessageToInterested(dropperVisualsMessage, dropper, AOINetworkPolicyValues.AOIChannelProximity);
            }
        }

        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            return PushLootNodeCallback(settings, resolver);
        }
    }

    public class LootDropChatMessagePrototype : LootNodePrototype
    {
        public LocaleStringId ChatMessage { get; protected set; }
        public PlayerScope MessageScope { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override void OnResultsEvaluation(Player player, WorldEntity dropper)
        {
            Logger.Debug($"OnResultsEvaluation(): ChatMessage={ChatMessage}, MessageScope={MessageScope}");

            // TODO: Use MessageScope
            NetMessageChatFromGameSystem chatFromGameSystem = NetMessageChatFromGameSystem.CreateBuilder()
                .SetSourceStringId((ulong)GameDatabase.GlobalsPrototype.SystemLocalized)
                .SetMessageStringId((ulong)ChatMessage)
                .Build();

            player.SendMessage(chatFromGameSystem);
        }

        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            return PushLootNodeCallback(settings, resolver);
        }
    }

    public class LootDropVanityTitlePrototype : LootNodePrototype
    {
        public PrototypeId VanityTitle { get; protected set; }


        //---

        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            LootRollResult result = LootRollResult.NoRoll;

            if (VanityTitle == PrototypeId.Invalid)
                return result;

            result = resolver.PushVanityTitle(VanityTitle);
            if (result.HasFlag(LootRollResult.Failure))
            {
                resolver.ClearPending();
                return LootRollResult.Failure;
            }

            return resolver.ProcessPending(settings) ? result : LootRollResult.Failure;
        }
    }

    public class LootDropVendorXPPrototype : LootNodePrototype
    {
        public PrototypeId Vendor { get; protected set; }
        public int XP { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            LootRollResult result = LootRollResult.NoRoll;

            // Validate this drop
            if (XP <= 0 || Vendor == PrototypeId.Invalid)
                return result;

            // Make sure this drop is not on cooldown
            if (settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.PreviewOnly) == false &&
                settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.IgnoreCooldown) == false)
            {
                if (resolver.CheckDropCooldown(Vendor, XP))
                    return result;
            }

            // Get XP can info prototype for this drop's vendor
            VendorXPCapInfoPrototype vendorXPCapInfoProto = null;
            foreach (VendorXPCapInfoPrototype currentInfoProto in GameDatabase.LootGlobalsPrototype.VendorXPCapInfo)
            {
                if (currentInfoProto.Vendor == Vendor)
                {
                    vendorXPCapInfoProto = currentInfoProto;
                    break;
                }
            }

            // Adjust xp amount to prevent it from going over cap
            int xpAmount = XP;

            if (vendorXPCapInfoProto != null && settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.IgnoreCap) == false)
            {
                // This code handles weekly caps for Entity/Characters/Vendors/VendorTypes/VendorRaidGenosha.prototype.
                Player player = resolver.Player;
                if (player == null)
                    return Logger.WarnReturn(LootRollResult.NoRoll, "Roll(): Unable to get player when rewarding VendorXP");

                int vendorXPCapCounter = player.Properties[PropertyEnum.VendorXPCapCounter, Vendor];
                bool shouldAdjustCounter = settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.PreviewOnly) == false && player.IsInGame;
                if (shouldAdjustCounter)
                {
                    // Reset the counter if a rollover has happened
                    if (player.TryDoVendorXPCapRollover(vendorXPCapInfoProto))
                        vendorXPCapCounter = 0;
                }

                if (vendorXPCapCounter + xpAmount > vendorXPCapInfoProto.Cap)
                    xpAmount = Math.Max(0, vendorXPCapInfoProto.Cap - vendorXPCapCounter);

                if (shouldAdjustCounter)
                    player.Properties.AdjustProperty(xpAmount, new(PropertyEnum.VendorXPCapCounter, Vendor));
            }

            if (xpAmount <= 0)
                return result;

            result = resolver.PushVendorXP(Vendor, xpAmount);
            if (result.HasFlag(LootRollResult.Failure))
            {
                resolver.ClearPending();
                return LootRollResult.Failure;
            }

            return resolver.ProcessPending(settings) ? result : LootRollResult.Failure;
        }
    }
}
