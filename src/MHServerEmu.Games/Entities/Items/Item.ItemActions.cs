using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
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
        private void TriggerItemActionOnUse(ItemActionPrototype itemActionProto, Player player, Avatar avatar)
        {
            if (itemActionProto.TriggeringEvent != ItemEventType.OnUse)
                return;

            switch (itemActionProto.ActionType)
            {
                case ItemActionType.AssignPower:
                    DoItemActionAssignPower();
                    break;

                case ItemActionType.DestroySelf:
                    DoItemActionDestroySelf();
                    break;

                case ItemActionType.GuildUnlock:
                    DoItemActionGuildUnlock();
                    break;
                case ItemActionType.PrestigeMode:
                    DoItemActionPrestigeMode();
                    break;

                case ItemActionType.ReplaceSelfItem:
                    DoItemActionReplaceSelfItem();
                    break;

                case ItemActionType.ReplaceSelfLootTable:
                    DoItemActionReplaceSelfLootTable();
                    break;

                case ItemActionType.ResetMissions:
                    DoItemActionResetMissions();
                    break;

                case ItemActionType.Respec:
                    DoItemActionRespec();
                    break;

                case ItemActionType.SaveDangerRoomScenario:
                    DoItemActionSaveDangerRoomScenario();
                    break;

                case ItemActionType.UnlockPermaBuff:
                    DoItemActionUnlockPermaBuff();
                    break;

                case ItemActionType.UsePower:
                    ItemActionUsePowerPrototype usePowerProto = (ItemActionUsePowerPrototype)itemActionProto;
                    DoItemActionUsePower(usePowerProto.Power, avatar);
                    break;

                case ItemActionType.AwardTeamUpXP:
                    DoItemActionAwardTeamUpXP();
                    break;

                case ItemActionType.OpenUIPanel:
                    DoItemActionOpenUIPanel();
                    break;
            }
        }

        private void DoItemActionAssignPower()
        {
            Logger.Debug($"DoItemActionAssignPower(): {this}");
        }

        private void DoItemActionDestroySelf()
        {
            Logger.Debug($"DoItemActionDestroySelf(): {this}");
        }

        private void DoItemActionGuildUnlock()
        {
            Logger.Debug($"DoItemActionGuildUnlock(): {this}");
        }

        private void DoItemActionPrestigeMode()
        {
            Logger.Debug($"DoItemActionPrestigeMode(): {this}");
        }

        private void DoItemActionReplaceSelfItem()
        {
            Logger.Debug($"DoItemActionReplaceSelfItem(): {this}");
        }

        private void DoItemActionReplaceSelfLootTable()
        {
            Logger.Debug($"DoItemActionReplaceSelfLootTable(): {this}");
        }

        private void DoItemActionResetMissions()
        {
            Logger.Debug($"DoItemActionResetMissions(): {this}");
        }

        private void DoItemActionRespec()
        {
            Logger.Debug($"DoItemActionRespec(): {this}");
        }

        private void DoItemActionSaveDangerRoomScenario()
        {
            Logger.Debug($"DoItemActionSaveDangerRoomScenario(): {this}");
        }

        private void DoItemActionUnlockPermaBuff()
        {
            Logger.Debug($"DoItemActionUnlockPermaBuff(): {this}");
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

                return true;
            }

            // TODO: normal implementation

            return true;
        }

        private void DoItemActionAwardTeamUpXP()
        {
            Logger.Debug($"DoItemActionAwardTeamUpXP(): {this}");
        }

        private void DoItemActionOpenUIPanel()
        {
            Logger.Debug($"DoItemActionOpenUIPanel(): {this}");
        }

    }
}
