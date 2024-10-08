using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Loot;

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
        private static readonly Logger Logger = LogManager.CreateLogger();

        public PrototypeId Item { get; protected set; }
        public LootMutationPrototype[] Mutations { get; protected set; }

        public override bool OnResultsEvaluation(Player player, WorldEntity source)
        {
            if (Item == PrototypeId.Invalid || DataDirectory.Instance.PrototypeIsA<CostumePrototype>(Item) == false)
                return Logger.WarnReturn(false, $"LootDropItemPrototype::OnResultsEvaluation() is only supported for Costumes!");

            // Unlock costume for costume closet (consoles / 1.53)
            // player.UnlockCostume(Item);

            return true;
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

        public override bool OnResultsEvaluation(Player player, WorldEntity worldEntity)
        {
            return Logger.WarnReturn(false, $"OnResultsEvaluation(): Not yet implemented (BannerMessage={BannerMessage.GetName()})");
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

        public override bool OnResultsEvaluation(Player player, WorldEntity worldEntity)
        {
            return Logger.WarnReturn(false, $"OnResultsEvaluation(): Not yet implemented (Power={Power.GetName()})");
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

        public override bool OnResultsEvaluation(Player player, WorldEntity worldEntity)
        {
            return Logger.WarnReturn(false, $"OnResultsEvaluation(): Not yet implemented (RecipientVisualEffect={RecipientVisualEffect.GetName()}, DropperVisualEffect={DropperVisualEffect.GetName()})");
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

        public override bool OnResultsEvaluation(Player player, WorldEntity worldEntity)
        {
            return Logger.WarnReturn(false, $"OnResultsEvaluation(): Not yet implemented (ChatMessage={ChatMessage}, MessageScope={MessageScope})");
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
            Logger.Warn($"Roll(): {Vendor.GetName()} - {XP}");
            return LootRollResult.NoRoll;
        }
    }
}
