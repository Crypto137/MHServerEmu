using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Entities.PowerCollections;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Events.LegacyImplementations
{
    public class OLD_UseInteractableObjectEvent : ScheduledEvent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private PlayerConnection _playerConnection;
        private Entity _interactObject;

        public void Initialize(PlayerConnection playerConnection, Entity interactObject)
        {
            _playerConnection = playerConnection;
            _interactObject = interactObject;
        }

        public override bool OnTriggered()
        {
            var proto = _interactObject.PrototypeDataRef;
            Logger.Trace($"UseInteractableObject {GameDatabase.GetPrototypeName(proto)}");

            Player player = _playerConnection.Player;
            Game game = _playerConnection.Game;

            if (proto != (PrototypeId)16537916167475500124) // BowlingBallReturnDispenser
                return true;

            // bowlingBallItem = proto.LootTablePrototypeProp.Value->Table.Choices.Item.Item
            var bowlingBallItem = (PrototypeId)7835010736274089329; // Entity/Items/Consumables/Prototypes/AchievementRewards/ItemRewards/BowlingBallItem
                                                                    // itemPower = bowlingBallItem.Item.ActionsTriggeredOnItemEvent.ItemActionSet.Choices.ItemActionUsePower.Power
            var itemPower = (PrototypeId)PowerPrototypes.Items.BowlingBallItemPower; // BowlingBallItemPower
                                                                                        // itemRarities = bowlingBallItem.Item.LootDropRestrictions.Rarity.AllowedRarities
            var itemRarity = (PrototypeId)9254498193264414304; // R4Epic

            // Destroy bowling balls that are already present in the player general inventory
            Inventory inventory = player.GetInventory(InventoryConvenienceLabel.General);

            Entity bowlingBall = inventory.GetMatchingEntity(bowlingBallItem) as Item;
            bowlingBall?.Destroy();

            // Create a new ball
            AffixSpec[] affixSpecs = { new AffixSpec((PrototypeId)4906559676663600947, 0, 1) }; // BindingInformation                        
            int seed = game.Random.Next();
            float itemVariation = game.Random.NextFloat();

            ItemSpec itemSpec = new(bowlingBallItem, itemRarity, 1, 0, affixSpecs, seed, 0);

            PropertyCollection properties = new();
            properties[PropertyEnum.Requirement, (PrototypeId)4312898931213406054] = 1.0f;    // Property/Info/CharacterLevel.defaults
            properties[PropertyEnum.ItemRarity] = itemRarity;
            properties[PropertyEnum.ItemVariation] = itemVariation;
            // TODO: applyItemSpecProperties 
            properties[PropertyEnum.InventoryStackSizeMax] = 1000;          // Item.StackSettings
            properties[PropertyEnum.ItemIsTradable] = false;                // DefaultSettings.IsTradable
            properties[PropertyEnum.ItemBindsToCharacterOnEquip] = true;    // DefaultSettings.BindsToAccountOnPickup
            properties[PropertyEnum.ItemBindsToAccountOnPickup] = true;     // DefaultSettings.BindsToCharacterOnEquip 

            EntitySettings ballSettings = new();
            ballSettings.EntityRef = bowlingBallItem;
            ballSettings.InventoryLocation = new(player.Id, inventory.PrototypeDataRef);
            ballSettings.ItemSpec = itemSpec;
            ballSettings.Properties = properties;

            game.EntityManager.CreateEntity(ballSettings);

            //  Unassign bowling ball power if the player already has one
            Avatar avatar = player.CurrentAvatar;
            if (avatar.HasPowerInPowerCollection(itemPower))
            {
                avatar.UnassignPower((PrototypeId)PowerPrototypes.Items.BowlingBallItemPower);
                _playerConnection.SendMessage(NetMessagePowerCollectionUnassignPower.CreateBuilder()
                    .SetEntityId(avatar.Id)
                    .SetPowerProtoId((ulong)PowerPrototypes.Items.BowlingBallItemPower)
                    .Build());
            }

            PowerIndexProperties indexProps = new(0, 60, 60, 1, itemVariation);
            avatar.AssignPower(itemPower, indexProps);

            _playerConnection.SendMessage(NetMessagePowerCollectionAssignPower.CreateBuilder()
                .SetEntityId(avatar.Id)
                .SetPowerProtoId((ulong)itemPower)
                .SetPowerRank(indexProps.PowerRank)
                .SetCharacterLevel(indexProps.CharacterLevel)
                .SetCombatLevel(indexProps.CombatLevel)
                .SetItemLevel(indexProps.ItemLevel)
                .SetItemVariation(itemVariation)
                .Build());

            return true;
        }
    }
}
