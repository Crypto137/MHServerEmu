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

            ItemPrototype itemPrototype = GameDatabase.GetPrototype<ItemPrototype>(proto);

            if (itemPrototype?.ActionsTriggeredOnItemEvent?.Choices == null)
                return true;

            if (itemPrototype.ActionsTriggeredOnItemEvent.PickMethod == PickMethod.PickAll) // TODO : other pick method
            {
                foreach (var choice in itemPrototype.ActionsTriggeredOnItemEvent.Choices)
                {
                    ItemActionPrototype itemActionPrototype = choice as ItemActionPrototype;
                    if (itemActionPrototype == null)
                        continue;

                    ExecuteAction(itemActionPrototype);
                }
            }

            return true;
        }

        private void ExecuteAction(ItemActionPrototype itemActionPrototype)
        {
            if (itemActionPrototype.TriggeringEvent != ItemEventType.OnUse)
                return;

            ItemActionUsePowerPrototype itemActionUsePowerPrototype = itemActionPrototype as ItemActionUsePowerPrototype;
            if (itemActionUsePowerPrototype != null)
                UsePower(_player.CurrentAvatar, itemActionUsePowerPrototype.Power);
        }

        private void UsePower(Avatar avatar, PrototypeId powerPrototypeId)
        {
            if (avatar.HasPowerInPowerCollection(powerPrototypeId) == false)
                avatar.AssignPower(powerPrototypeId, new(0, avatar.CharacterLevel, avatar.CombatLevel));

            Power power = avatar.GetPower(powerPrototypeId);
            if (power == null)
                return;

            SummonPowerPrototype summonPowerPrototype = power.Prototype as SummonPowerPrototype;

            if(summonPowerPrototype != null)
            {
                if (avatar.Properties[PropertyEnum.PowerToggleOn, powerPrototypeId])
                    EntityHelper.DestroySummonFromPower(avatar, powerPrototypeId);
                else
                    EntityHelper.SummonEntityFromPower(avatar, powerPrototypeId);
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
