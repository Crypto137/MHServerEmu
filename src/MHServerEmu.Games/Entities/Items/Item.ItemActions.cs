using Gazillion;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;

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

    public partial class Item
    {
        private void TriggerItemActionOnUse(ItemActionPrototype actionProto, Player player, Avatar avatar, ref bool wasUsed, ref bool isConsumable)
        {
            if (actionProto.TriggeringEvent != ItemEventType.OnUse)
                return;

            switch (actionProto.ActionType)
            {
                case ItemActionType.AssignPower:
                    wasUsed |= DoItemActionAssignPower();
                    break;

                case ItemActionType.DestroySelf:
                    DoItemActionDestroySelf(ref isConsumable);    // This simply flags the item to be destroyed, so we don't need to update wasUsed here
                    break;

                case ItemActionType.GuildUnlock:
                    wasUsed |= DoItemActionGuildUnlock();
                    break;

                case ItemActionType.PrestigeMode:
                    wasUsed |= DoItemActionPrestigeMode();
                    break;

                case ItemActionType.ReplaceSelfItem:
                    wasUsed |= DoItemActionReplaceSelfItem();
                    break;

                case ItemActionType.ReplaceSelfLootTable:
                    if (actionProto is not ItemActionReplaceSelfLootTablePrototype replaceSelfLootTableProto)
                    {
                        Logger.Warn("TriggerItemActionOnUse(): actionProto is not ItemActionReplaceSelfLootTablePrototype replaceSelfLootTableProto");
                        return;
                    }

                    wasUsed |= DoItemActionReplaceSelfLootTable(replaceSelfLootTableProto.LootTable, replaceSelfLootTableProto.UseCurrentAvatarLevelForRoll, player, avatar);
                    break;

                case ItemActionType.ResetMissions:
                    wasUsed |= DoItemActionResetMissions();
                    break;

                case ItemActionType.Respec:
                    wasUsed |= DoItemActionRespec();
                    break;

                case ItemActionType.SaveDangerRoomScenario:
                    wasUsed |= DoItemActionSaveDangerRoomScenario();
                    break;

                case ItemActionType.UnlockPermaBuff:
                    wasUsed |= DoItemActionUnlockPermaBuff();
                    break;

                case ItemActionType.UsePower:
                    if (actionProto is not ItemActionUsePowerPrototype usePowerProto)
                    {
                        Logger.Warn("TriggerItemActionOnUse(): actionProto is not ItemActionUsePowerPrototype usePowerProto");
                        return;
                    }

                    wasUsed |= DoItemActionUsePower(usePowerProto.Power, avatar);
                    break;

                case ItemActionType.AwardTeamUpXP:
                    wasUsed |= DoItemActionAwardTeamUpXP();
                    break;

                case ItemActionType.OpenUIPanel:
                    wasUsed |= DoItemActionOpenUIPanel();
                    break;
            }
        }

        private bool TriggerItemActionOnUsePowerActivated(ItemActionPrototype itemActionProto)
        {
            if (itemActionProto.TriggeringEvent != ItemEventType.OnUsePowerActivated)
                return false;

            switch (itemActionProto.ActionType)
            {
                case ItemActionType.DestroySelf:
                    DecrementStack();
                    return true;

                default:
                    return Logger.WarnReturn(false, $"TriggerItemActionOnUsePowerActivated(): Unhandled action type {itemActionProto.ActionType}");
            }
        }

        private bool DoItemActionAssignPower()
        {
            Logger.Debug($"DoItemActionAssignPower(): {this}");
            return false;
        }

        private void DoItemActionDestroySelf(ref bool isConsumable)
        {
            // This "action" flags this item's effect as consumable (i.e. it needs to be destroyed on use)
            isConsumable = true;
        }

        private bool DoItemActionGuildUnlock()
        {
            Logger.Debug($"DoItemActionGuildUnlock(): {this}");
            return false;
        }

        private bool DoItemActionPrestigeMode()
        {
            Logger.Debug($"DoItemActionPrestigeMode(): {this}");
            return false;
        }

        private bool DoItemActionReplaceSelfItem()
        {
            Logger.Debug($"DoItemActionReplaceSelfItem(): {this}");
            return false;
        }

        private bool DoItemActionReplaceSelfLootTable(LootTablePrototype lootTableProto, bool useAvatarLevel, Player player, Avatar avatar)
        {
            Logger.Debug($"DoItemActionReplaceSelfLootTable(): {this}");

            using LootInputSettings inputSettings = ObjectPoolManager.Instance.Get<LootInputSettings>();
            inputSettings.Initialize(LootContext.MysteryChest, player, null);
            inputSettings.LootRollSettings.Level = useAvatarLevel ? avatar.CharacterLevel : Properties[PropertyEnum.ItemLevel];

            using ItemResolver resolver = ObjectPoolManager.Instance.Get<ItemResolver>();
            resolver.Initialize(Game.Random);
            resolver.SetContext(LootContext.MysteryChest, player);

            LootRollResult result = lootTableProto.Roll(inputSettings.LootRollSettings, resolver);
            if (result != LootRollResult.Success)
            {
                player.SendMessage(NetMessageLootRollFailed.DefaultInstance);
                return Logger.WarnReturn(false, $"DoItemActionReplaceSelfLootTable(): Failed to roll loot table for {this}");
            }

            using LootResultSummary lootResultSummary = ObjectPoolManager.Instance.Get<LootResultSummary>();
            resolver.FillLootResultSummary(lootResultSummary);

            NetMessageLootRewardReport.Builder reportBuilder = NetMessageLootRewardReport.CreateBuilder();

            if (ReplaceSelfInternal(lootResultSummary, player, reportBuilder))
            {
                reportBuilder.SetSource(_itemSpec.ToProtobuf());
                player.SendMessage(reportBuilder.Build());
                return true;
            }
            
            return false;
        }

        private bool DoItemActionResetMissions()
        {
            Logger.Debug($"DoItemActionResetMissions(): {this}");
            return false;
        }

        private bool DoItemActionRespec()
        {
            Logger.Debug($"DoItemActionRespec(): {this}");
            return false;
        }

        private bool DoItemActionSaveDangerRoomScenario()
        {
            Logger.Debug($"DoItemActionSaveDangerRoomScenario(): {this}");
            return false;
        }

        private bool DoItemActionUnlockPermaBuff()
        {
            Logger.Debug($"DoItemActionUnlockPermaBuff(): {this}");
            return false;
        }
        
        private bool DoItemActionUsePower(PrototypeId powerProtoRef, Avatar avatar)
        {
            Logger.Debug($"DoItemActionUsePower(): item=[{this}], powerProtoRef={powerProtoRef.GetName()}");

            Power power = avatar.GetPower(powerProtoRef);
            if (power == null) return Logger.WarnReturn(false, "DoItemActionUsePower(): power == null");

            // HACK/REMOVEME: Remove these hacks when we get summon powers working properly
            if (power.Prototype is SummonPowerPrototype summonPowerProto)
            {
                PropertyId summonedEntityCountProp = new(PropertyEnum.PowerSummonedEntityCount, powerProtoRef);
                if (avatar.Properties[PropertyEnum.PowerToggleOn, powerProtoRef])
                {
                    EntityHelper.DestroySummonerFromPowerPrototype(avatar, summonPowerProto);

                    if (power.IsToggled())  // Check for Danger Room scenarios that are not toggled
                        avatar.Properties[PropertyEnum.PowerToggleOn, powerProtoRef] = false;

                    avatar.Properties.AdjustProperty(-1, summonedEntityCountProp);
                }
                else
                {
                    EntityHelper.SummonEntityFromPowerPrototype(avatar, summonPowerProto, this);

                    if (power.IsToggled())  // Check for Danger Room scenarios that are not toggled
                        avatar.Properties[PropertyEnum.PowerToggleOn, powerProtoRef] = true;

                    avatar.Properties.AdjustProperty(1, summonedEntityCountProp);
                }

                OnUsePowerActivated();
                return true;
            }

            // TODO: normal implementation

            return false;
        }

        private bool DoItemActionAwardTeamUpXP()
        {
            Logger.Debug($"DoItemActionAwardTeamUpXP(): {this}");
            return false;
        }

        private bool DoItemActionOpenUIPanel()
        {
            Logger.Debug($"DoItemActionOpenUIPanel(): {this}");
            return false;
        }

        private bool ReplaceSelfInternal(LootResultSummary lootResultSummary, Player player, NetMessageLootRewardReport.Builder reportBuilder)
        {
            Logger.Debug($"ReplaceSelfInternal(): [{lootResultSummary.Types}]");
            return false;
        }
    }
}
