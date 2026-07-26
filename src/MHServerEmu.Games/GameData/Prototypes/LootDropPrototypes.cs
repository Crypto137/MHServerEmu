using Gazillion;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.PowerCollections;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Social;
using MHServerEmu.Games.Social.Communities;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class LootDropAgentPrototype : LootDropPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public WorldEntityPrototype Agent { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();

            if (Agent != null && GameDatabase.DataDirectory.PrototypeIsAbstract(Agent.DataRef))
                Agent = null;
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

            // Never roll XP for capped starter avatars
            Player player = settings.Player;
            Avatar avatar = player?.CurrentAvatar;
            if (avatar != null && player.HasAvatarAsCappedStarter(avatar) && agentProto.HasKeyword(GameDatabase.KeywordGlobalsPrototype.OrbExperienceEntityKeyword))
                return result;

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
            if (Agent == null)
                return LootRollResult.NoRoll;

            int numAgents = NumMin == NumMax ? NumMin : resolver.Random.Next(NumMin, NumMax + 1);

            return RollAgent(Agent, numAgents, settings, resolver);
        }
    }

    public class LootDropCharacterTokenPrototype : LootNodePrototype
    {
        public CharacterTokenType AllowedTokenType { get; protected set; }
        public CharacterFilterType FilterType { get; protected set; }
        public LootNodePrototype OnTokenUnavailable { get; protected set; }

        //---

        public override void Visit<T>(ref T visitor)
        {
            base.Visit(ref visitor);

            OnTokenUnavailable?.Visit(ref visitor);
        }

        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            if (FilterType == CharacterFilterType.DropUnownedAvatarOnly)
            {
                if (!Verify.IsNotNull(settings.Player)) return LootRollResult.Failure;
            }

            ItemPrototype itemProto = null;

            // Build picker
            Picker<Prototype> picker = new(resolver.Random);

            foreach (PrototypeId charTokenProtoRef in DataDirectory.Instance.IteratePrototypesInHierarchy<CharacterTokenPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                CharacterTokenPrototype charTokenProto = charTokenProtoRef.As<CharacterTokenPrototype>();
                if (!Verify.IsNotNull(charTokenProto))
                    continue;

                if (charTokenProto.TokenType != AllowedTokenType)
                    continue;

                // Skip tokens that don't have an ES cost
                ItemCostPrototype itemCostProto = charTokenProto.Cost;
                if (itemCostProto == null || itemCostProto.HasEternitySplintersComponent() == false)
                    continue;

                bool addToPicker = true;

                switch (FilterType)
                {
                    case CharacterFilterType.DropCurrentAvatarOnly:
                        addToPicker = charTokenProto.Character == settings.UsableAvatar.DataRef;
                        if (addToPicker)
                            itemProto = charTokenProto;
                        break;

                    case CharacterFilterType.DropUnownedAvatarOnly:
                        addToPicker = charTokenProto.HasUnlockedCharacter(settings.Player) == false;
                        break;

                    // Add by default if no filter is specified
                }

                if (addToPicker)
                    picker.Add(charTokenProto);
            }

            // Fallback if failed to find anything to pick
            if (picker.Empty())
            {
                if (OnTokenUnavailable == null)
                {
                    resolver.ClearPending();
                    return LootRollResult.Failure;
                }

                return OnTokenUnavailable.Select(settings, resolver);
            }

            // Pick and push to the resolver
            LootRollResult result = LootRollResult.NoRoll;
            AvatarPrototype usableAvatarProto = settings.UsableAvatar;

            int level = resolver.ResolveLevel(settings.Level, settings.UseLevelVerbatim);
            AvatarPrototype resolvedAvatarProto = resolver.ResolveAvatarPrototype(usableAvatarProto, settings.ForceUsable, settings.UsablePercent);
            PrototypeId rollFor = resolvedAvatarProto != null ? resolvedAvatarProto.DataRef : PrototypeId.Invalid;

            PrototypeId? rarityProtoRef = resolver.ResolveRarity(settings.Rarities, level, null);
            if (rarityProtoRef == PrototypeId.Invalid)
            {
                resolver.ClearPending();
                return LootRollResult.Failure;
            }

            using DropFilterArguments filterArgs = ObjectPoolManager.Instance.Get<DropFilterArguments>();
            DropFilterArguments.Initialize(filterArgs, itemProto, rollFor, level, rarityProtoRef.Value, 0, EquipmentInvUISlot.Invalid, resolver.LootContext);
            filterArgs.DropDistanceSq = settings.DropDistanceSq;

            if (LootUtilities.PickValidItem(resolver, picker, null, filterArgs, ref itemProto, RestrictionTestFlags.All, ref rarityProtoRef) == false)
            {
                resolver.ClearPending();
                return LootRollResult.Failure;
            }

            filterArgs.Rarity = rarityProtoRef.Value;
            filterArgs.ItemProto = itemProto;

            RestrictionTestFlags restrictionFlags = RestrictionTestFlags.All;
            if (settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.IgnoreCooldown) ||
                settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.PreviewOnly))
            {
                restrictionFlags &= ~RestrictionTestFlags.Cooldown;
            }

            result |= resolver.PushItem(filterArgs, restrictionFlags, 1, null);

            if (result.HasFlag(LootRollResult.Failure))
            {
                resolver.ClearPending();
                return LootRollResult.Failure;
            }

            return resolver.ProcessPending(settings) ? result : LootRollResult.Failure;
        }
    }

    public class LootDropClonePrototype : LootNodePrototype
    {
        public LootMutationPrototype[] Mutations { get; protected set; }
        public short SourceIndex { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();

            SourceIndex = (short)Math.Max((int)SourceIndex, 0);
        }

        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            using LootCloneRecord lootCloneRecord = ObjectPoolManager.Instance.Get<LootCloneRecord>();
            if (resolver.InitializeCloneRecordFromSource(SourceIndex, lootCloneRecord) == false)
                return LootRollResult.Failure;

            // Set up flags
            if (lootCloneRecord.RollFor == PrototypeId.Invalid)
                lootCloneRecord.RestrictionFlags &= ~RestrictionTestFlags.UsableBy;

            lootCloneRecord.RestrictionFlags &= ~RestrictionTestFlags.Rarity;

            // Apply mutations
            if (Mutations.HasValue())
            {
                MutationResults mutationResult = MutationResults.None;
                foreach (LootMutationPrototype lootMutationProto in Mutations)
                {
                    mutationResult |= lootMutationProto.Mutate(settings, resolver, lootCloneRecord);
                    if (mutationResult.HasFlag(MutationResults.Error))
                    {
                        resolver.ClearPending();
                        return LootRollResult.Failure;
                    }
                }
            }

            // Push to the resolver
            LootRollResult result = resolver.PushClone(lootCloneRecord);
            if (result.HasFlag(LootRollResult.Failure))
            {
                resolver.ClearPending();
                return LootRollResult.Failure;
            }

            return resolver.ProcessPending(settings) ? result : LootRollResult.Failure;
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
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public ItemPrototype Item { get; protected set; }
        public LootMutationPrototype[] Mutations { get; protected set; }

        //---

        public override void OnResultsEvaluation(Player player, WorldEntity source)
        {
            if (!Verify.IsTrue(Item != null && Item is CostumePrototype, "LootDropItemPrototype::OnResultsEvaluation() is only supported for Costumes!"))
                return;

            // Unlock costume for costume closet (consoles / 1.53)
            // player.UnlockCostume(Item);
        }

        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            if (Item == null)
                return LootRollResult.NoRoll;

            int numItems = NumMin == NumMax ? NumMin : resolver.Random.Next(NumMin, NumMax + 1);

            return RollItem(Item, numItems, settings, resolver, Mutations);
        }
    }

    public class LootDropItemFilterPrototype : LootDropPrototype
    {
        public short ItemRank { get; protected set; }
        public EquipmentInvUISlot UISlot { get; protected set; }

        //---

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
#if GAME_VERSION_1_48
        public float Scalar { get; protected set; }
#endif

        //---

        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            LootRollResult result = LootRollResult.NoRoll;

            // Never roll XP for capped starter avatars
            Player player = settings.Player;
            Avatar avatar = player?.CurrentAvatar;
            if (avatar != null && player.HasAvatarAsCappedStarter(avatar))
                return result;

            if (XPCurve == CurveId.Invalid)
                return result;

            Curve xpCurve = CurveDirectory.Instance.GetCurve(XPCurve);
            if (!Verify.IsNotNull(xpCurve)) return result;

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
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public BannerMessagePrototype BannerMessage { get; protected set; }

        //---

        public override void OnResultsEvaluation(Player player, WorldEntity dropper)
        {
            player.SendBannerMessage(BannerMessage);
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

        public override void OnResultsEvaluation(Player player, WorldEntity dropper)
        {
            if (!Verify.IsNotNull(dropper)) return;

            Avatar avatar = player.CurrentAvatar;
            if (avatar == null) return;

            Power power = dropper.GetPower(Power);
            if (power == null)
            {
                PowerIndexProperties props = new();
                Verify.IsNotNull(dropper.AssignPower(Power, props), $"LootDropUsePowerPrototype: failed to assign power on dropper!\nPower: {Power.GetName()}\nDropper {dropper}:\nNode: {this}");
            }

            PowerActivationSettings settings = new(avatar.Id, avatar.RegionLocation.Position, dropper.RegionLocation.Position);
            settings.Flags |= PowerActivationSettingsFlags.SkipRangeCheck;

            PowerUseResult result = dropper.ActivatePower(Power, ref settings);
            Verify.IsTrue(result == PowerUseResult.Success, $"LootDropUsePowerPrototype: failed to activate power!\nPowerUseResult: {result}\nPower: {Power.GetName()}\nDropper: {dropper}\nNode: {this}");
        }

        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            return PushLootNodeCallback(settings, resolver);
        }
    }

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
    public class LootDropPlayVisualEffectPrototype : LootNodePrototype
    {
        public AssetId RecipientVisualEffect { get; protected set; }
        public AssetId DropperVisualEffect { get; protected set; }

        //---

        public override void OnResultsEvaluation(Player player, WorldEntity dropper)
        {
            Game game = player?.Game;
            if (game == null)
                return;

            Avatar avatar = player?.CurrentAvatar;
            if (avatar == null)
                return;

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
#endif

    public class LootDropChatMessagePrototype : LootNodePrototype
    {
        public LocaleStringId ChatMessage { get; protected set; }
        public PlayerScope MessageScope { get; protected set; }

        //---

        public override void OnResultsEvaluation(Player player, WorldEntity dropper)
        {
            ChatManager chatManager = player.Game.ChatManager;
            CircleId circleId = CircleId.__None;

            switch (MessageScope)
            {
                case PlayerScope.CurrentRecipientOnly:
                    chatManager.SendChatFromGameSystem(ChatMessage, player);
                    return;

                case PlayerScope.Party:
                    circleId = CircleId.__Party;
                    break;

                case PlayerScope.Nearby:
                    circleId = CircleId.__Nearby;
                    break;

                case PlayerScope.Friends:
                    circleId = CircleId.__Friends;
                    break;

                case PlayerScope.Guild:
                    circleId = CircleId.__Guild;
                    break;

                default:
                    Verify.IsTrue(false, $"Unknown message scope {MessageScope}");
                    return;
            }

            chatManager.SendChatFromGameSystem(ChatMessage, player, circleId);
        }

        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            return PushLootNodeCallback(settings, resolver);
        }
    }

    public class LootDropVanityTitlePrototype : LootNodePrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public VanityTitlePrototype VanityTitle { get; protected set; }


        //---

        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            LootRollResult result = LootRollResult.NoRoll;

            if (VanityTitle == null)
                return result;

            result = resolver.PushVanityTitle(VanityTitle.DataRef);
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
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public VendorTypePrototype Vendor { get; protected set; }
        public int XP { get; protected set; }

        //---

        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            LootRollResult result = LootRollResult.NoRoll;

            // Validate this drop
            if (XP <= 0 || Vendor == null)
                return result;

            // Make sure this drop is not on cooldown
            if (settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.PreviewOnly) == false &&
                settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.IgnoreCooldown) == false)
            {
                if (resolver.CheckDropCooldown(Vendor.DataRef, XP))
                    return result;
            }

            // Get XP can info prototype for this drop's vendor
            VendorXPCapInfoPrototype vendorXPCapInfoProto = null;
            foreach (VendorXPCapInfoPrototype currentInfoProto in GameDatabase.LootGlobalsPrototype.VendorXPCapInfo)
            {
                if (currentInfoProto.Vendor == Vendor.DataRef)
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
                if (!Verify.IsNotNull(player, "Unable to get player when rewarded VendorXP!"))
                    return LootRollResult.NoRoll;

                int vendorXPCapCounter = player.Properties[PropertyEnum.VendorXPCapCounter, Vendor.DataRef];
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
                    player.Properties.AdjustProperty(xpAmount, new(PropertyEnum.VendorXPCapCounter, Vendor.DataRef));
            }

            if (xpAmount <= 0)
                return result;

            result = resolver.PushVendorXP(Vendor.DataRef, xpAmount);
            if (result.HasFlag(LootRollResult.Failure))
            {
                resolver.ClearPending();
                return LootRollResult.Failure;
            }

            return resolver.ProcessPending(settings) ? result : LootRollResult.Failure;
        }
    }

#if GAME_VERSION_1_53
    public class LootDropOmegaPrestigePrototype : LootNodePrototype
    {
        //---

        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            // V53_TODO
            Verify.IsTrue(false);
            return LootRollResult.NoRoll;
        }
    }
#endif
}
