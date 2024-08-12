using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionItemCraft : MissionPlayerCondition
    {
        private MissionConditionItemCraftPrototype _proto;
        private Action<PlayerCraftedItemGameEvent> _playerCraftedItemAction;
        protected override long RequiredCount => _proto.Count;

        public MissionConditionItemCraft(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            _proto = prototype as MissionConditionItemCraftPrototype;
            _playerCraftedItemAction = OnPlayerCraftedItem;
        }

        private void OnPlayerCraftedItem(PlayerCraftedItemGameEvent evt)
        {
            var player = evt.Player;
            var item = evt.Item;
            var recipeRef = evt.RecipeRef;
            int count = evt.Count;

            if (player == null || item == null || count <= 0 || IsMissionPlayer(player) == false) return;
            if (EvaluateEntityFilter(_proto.EntityFilter, item) == false) return;
            if (_proto.UsingRecipe != PrototypeId.Invalid && _proto.UsingRecipe != recipeRef) return;

            UpdatePlayerContribution(player, count);
            Count += count;
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.PlayerCraftedItemEvent.AddActionBack(_playerCraftedItemAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.PlayerCraftedItemEvent.RemoveAction(_playerCraftedItemAction);
        }
    }
}
