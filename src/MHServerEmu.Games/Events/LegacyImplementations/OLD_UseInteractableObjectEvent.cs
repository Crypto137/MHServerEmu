using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Events.LegacyImplementations
{
    public class OLD_UseInteractableObjectEvent : ScheduledEvent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private Player _player;
        private Entity _interactObject;

        public void Initialize(Player player, Entity interactObject)
        {
            _player = player;
            _interactObject = interactObject;
        }

        public override bool OnTriggered()
        {
            var proto = _interactObject.PrototypeDataRef;
            Logger.Trace($"UseInteractableObject {GameDatabase.GetPrototypeName(proto)}");

            if (proto == (PrototypeId)16537916167475500124) // BowlingBallReturnDispenser
                return HandleBowlingBallItem();

            var itemProto = GameDatabase.GetPrototype<ItemPrototype>(proto);

            if (itemProto?.ActionsTriggeredOnItemEvent?.Choices == null)
                return true;

            if (itemProto.ActionsTriggeredOnItemEvent.PickMethod == PickMethod.PickAll) // TODO : other pick method
            {
                foreach (var choice in itemProto.ActionsTriggeredOnItemEvent.Choices)
                {
                    if (choice is not ItemActionPrototype itemActionProto) continue;
                    ExecuteAction(itemActionProto);
                }
            }

            return true;
        }

        private void ExecuteAction(ItemActionPrototype itemActionProto)
        {
            if (itemActionProto.TriggeringEvent != ItemEventType.OnUse) return;

            if (itemActionProto is ItemActionUsePowerPrototype itemActionUsePowerProto)
                UsePower(_player.CurrentAvatar, itemActionUsePowerProto.Power);
        }

        private void UsePower(Avatar avatar, PrototypeId powerRef)
        {
            if (avatar.HasPowerInPowerCollection(powerRef) == false)
                avatar.AssignPower(powerRef, new(0, avatar.CharacterLevel, avatar.CombatLevel));

            Power power = avatar.GetPower(powerRef);
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
                else
                {
                    EntityHelper.SummonEntityFromPowerPrototype(avatar, summonPowerProto);
                    avatar.Properties[PropertyEnum.PowerToggleOn, powerRef] = true;
                    avatar.Properties.AdjustProperty(1, summonedEntityCountProp);
                }
            }
        }

        private bool HandleBowlingBallItem()
        {
            var bowlingBallProtoRef = (PrototypeId)7835010736274089329; // Entity/Items/Consumables/Prototypes/AchievementRewards/ItemRewards/BowlingBallItem
            var itemPower = (PrototypeId)PowerPrototypes.Items.BowlingBallItemPower; // BowlingBallItemPower
                                                                                     // itemPower = bowlingBallItem.Item.ActionsTriggeredOnItemEvent.ItemActionSet.Choices.ItemActionUsePower.Power

            // Destroy bowling balls that are already present in the player general inventory
            Inventory inventory = _player.GetInventory(InventoryConvenienceLabel.General);

            // A player can't have more than ten balls
            if (inventory.GetMatchingEntities(bowlingBallProtoRef) >= 10) return false;

            // Give the player a new bowling ball
            _player.Game.LootGenerator.GiveItem(_player, bowlingBallProtoRef);

            // Assign bowling ball power if the player's avatar doesn't have one
            Avatar avatar = _player.CurrentAvatar;
            if (avatar.HasPowerInPowerCollection(itemPower) == false)
                avatar.AssignPower(itemPower, new(0, avatar.CharacterLevel, avatar.CombatLevel));

            return true;
        }
    }
}
