using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Missions.Actions;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionEntityInteract : MissionPlayerCondition
    {
        private MissionConditionEntityInteractPrototype _proto;
        private Action<PlayerInteractGameEvent> _playerInteractAction;
        private Action<PlayerRequestMissionRewardsGameEvent> _playerRequestMissionRewardsAction;
        private Action<CinematicFinishedGameEvent> _cinematicFinishedAction;
        private ulong _cinematicEntityId;
        private bool _cinematicEventRegistered;

        public MissionObjective MissionObjective { get; }

        protected override long RequiredCount => _proto.Count;

        public MissionConditionEntityInteract(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype)
            : base(mission, owner, prototype)
        {
            // CH00NPEEternitySplinter
            _proto = prototype as MissionConditionEntityInteractPrototype;
            _playerInteractAction = OnPlayerInteract;
            _playerRequestMissionRewardsAction = OnPlayerRequestMissionRewards;
            _cinematicFinishedAction = OnCinematicFinished;

            if (Owner is MissionObjective objective)
                MissionObjective = objective;
            else if (Owner is MissionConditionList list && list.Owner is MissionObjective listObjective) 
                MissionObjective = listObjective;
        }

        private void OnCinematicFinished(CinematicFinishedGameEvent evt)
        {
            var player = evt.Player;
            var movieRef = evt.MovieRef;

            if (player == null || IsMissionPlayer(player) == false) return;
            if (_proto.Cinematic == PrototypeId.Invalid || _proto.Cinematic != movieRef) return;

            var entity = Game.EntityManager.GetEntity<WorldEntity>(_cinematicEntityId);
            if (entity == null) return;

            EntityInteract(player, entity);
            Mission.MissionManager.SchedulePlayerInteract(player, entity);
        }

        private void OnPlayerRequestMissionRewards(PlayerRequestMissionRewardsGameEvent evt)
        {
            var player = evt.Player;
            if (player == null || player != Player) return;
            var missionRef = evt.MissionRef;
            if (missionRef != Mission.PrototypeDataRef) return;
            uint conditionIndex = evt.ConditionIndex;
            if (conditionIndex != _proto.Index) return;
            ulong entityId = evt.EntityId;

            var message = NetMessageMissionRewardsResponse.CreateBuilder();
            message.SetMissionPrototypeId((ulong)missionRef);
            message.SetConditionIndex(conditionIndex);

            if (entityId != Entity.InvalidId)
                message.SetEntityId(entityId);

            using LootResultSummary lootSummary = ObjectPoolManager.Instance.Get<LootResultSummary>();

            if (GetShowItems(player, lootSummary))
            {
                message.SetShowItems(lootSummary.ToProtobuf());
                lootSummary.ResetForPool();
            }

            if (GetGiveItems(player, lootSummary, true))
                message.SetGiveItems(lootSummary.ToProtobuf());

            player.SendMessage(message.Build());
        }

        private void RollMissionRewards(Player player, LootResultSummary lootSummary, bool previewOnly)
        {
            Avatar avatar = player.CurrentAvatar;
            if (avatar != null && Mission.HasRewards(player, avatar))
            {
                LootTablePrototype[] rewards = Mission.Prototype.Rewards;
                if (rewards.HasValue())
                    Mission.RollLootSummary(lootSummary, player, rewards, Mission.LootSeed, previewOnly);
            }
        }

        private bool GetGiveItems(Player player, LootResultSummary lootSummary, bool previewOnly)
        {
            if (_proto.GiveItems.HasValue())
                Mission.RollLootSummary(lootSummary, player, _proto.GiveItems, Mission.LootSeed + _proto.Index + 1, previewOnly);

            if (_proto.IsTurnInNPC)
                RollMissionRewards(player, lootSummary, previewOnly);

            return lootSummary.HasAnyResult;
        }

        private bool GetShowItems(Player player, LootResultSummary lootSummary)
        {
            if (Mission.State != MissionState.Active || _proto.ShowRewards)
                RollMissionRewards(player, lootSummary, true);

            return lootSummary.HasAnyResult;
        }

        private void OnPlayerInteract(PlayerInteractGameEvent evt)
        {
            var player = evt.Player;
            var entity = evt.InteractableObject;
            var missionRef = evt.MissionRef;

            if (player == null || entity == null || entity.IsDead || IsMissionPlayer(player) == false) return;

            if (_proto.DialogText != LocaleStringId.Blank || _proto.DialogTextList.HasValue())
                if (missionRef != PrototypeId.Invalid && missionRef != Mission.PrototypeDataRef) return;

            if (EvaluateEntityFilter(_proto.EntityFilter, entity) == false) return;

            if (Mission.State != MissionState.Active)
            {
                var missionProto = Mission.Prototype;
                int avatarCharacterLevel = player.CurrentAvatarCharacterLevel;
                if (missionProto.Level - avatarCharacterLevel >= MissionManager.MissionLevelUpperBoundsOffset()) return;
            }

            if (_proto.RequiredItems.HasValue())
                if (MissionManager.MatchItemsToRemove(player, _proto.RequiredItems) == false) return;

            if (entity.IsInWorld)
            {
                var objective = MissionObjective;
                if (objective != null && objective.HasInteractedWithEntity(entity)) return;
            }

            if (_proto.WithinHotspot != PrototypeId.Invalid)
            {
                var avatar = player.CurrentAvatar;
                if (avatar == null || Mission.FilterHotspots(avatar, _proto.WithinHotspot) == false) return;
            }

            if (_proto.Cinematic != PrototypeId.Invalid)
            {
                if (_cinematicEventRegistered == false)
                {
                    var region = player.GetRegion();
                    if (region != null)
                    {
                        region.CinematicFinishedEvent.AddActionBack(_cinematicFinishedAction);
                        _cinematicEntityId = entity.Id;
                        _cinematicEventRegistered = true;
                    }
                } else return; // don't play already registred cinematic

                player.QueueFullscreenMovie(_proto.Cinematic);
                return;
            }

            EntityInteract(player, entity);
        }

        private void EntityInteract(Player player, WorldEntity entity)
        {
            if (_proto.RequiredItems.HasValue())
            {
                List<Entity> itemsOut = new();
                List<int> itemCounts = new();
                if (MissionManager.MatchItemsToRemove(player, _proto.RequiredItems, itemsOut, itemCounts) == false) return;
                if (itemsOut.Count != itemCounts.Count) return;
                for (int i = 0; i < itemsOut.Count; i++)
                {
                    if (itemsOut[i] is not Item item) return;
                    int count = itemCounts[i];
                    for (int j = 0; j < count; j++)
                        item.DecrementStack();
                }
            }

            GiveRewards(player, entity);

            if (_proto.OnInteractBehavior != PrototypeId.Invalid)
                InteractBehavior(entity, _proto.OnInteractBehavior, player);

            if (_proto.OnInteractEntityActions.HasValue())
                InteractEntityActions(entity, _proto.OnInteractEntityActions);

            switch (_proto.OnInteract)
            {
                case OnInteractAction.Despawn:
                    entity.Destroy();
                    break;
                case OnInteractAction.Disable:
                    entity.Properties[PropertyEnum.Interactable] = (int)TriBool.False;
                    break;
            }

            if (entity.IsInWorld)
                MissionObjective?.AddInteractedEntity(entity);

            UpdatePlayerContribution(player);
            Count++;
        }

        private void InteractEntityActions(WorldEntity entity, MissionActionPrototype[] missionActions)
        {
            MissionActionList actionList = new(Mission);
            foreach (var actionProto in missionActions)
                if (actionProto is MissionActionEntityTargetPrototype targetProto)
                {
                    var action = MissionAction.CreateAction(actionList, targetProto);
                    if (action is MissionActionEntityTarget actionTaget && action.Initialize())
                        actionTaget.EvaluateAndRunEntity(entity);

                    action.Destroy();
                }
            actionList.Destroy();
        }

        private static void InteractBehavior(WorldEntity entity, PrototypeId brainOverride, Player player)
        {
            if (entity is not Agent agent) return;
            var controller = agent.AIController;
            if (controller == null)
            {
                var brain = GameDatabase.GetPrototype<BrainPrototype>(brainOverride);
                if (brain is not ProceduralAIProfilePrototype profile) return;
                using PropertyCollection collection = ObjectPoolManager.Instance.Get<PropertyCollection>();
                collection[PropertyEnum.AIAssistedEntityID] = player.Id;
                agent.InitAIOverride(profile, collection);
            }
            else
            {
                var collection = controller.Blackboard.PropertyCollection;
                collection[PropertyEnum.AIFullOverride] = brainOverride;
                collection[PropertyEnum.AIAssistedEntityID] = player.Id;
            }
        }

        private void GiveRewards(Player player, WorldEntity entity)
        {
            Avatar avatar = player.CurrentAvatar;
            if (avatar == null) return;

            using LootResultSummary lootSummary = ObjectPoolManager.Instance.Get<LootResultSummary>();

            if (GetGiveItems(player, lootSummary, false))
            {
                WorldEntity lootDropper = _proto.DropLootOnGround ? entity : null;
                if (Mission.AwardLootToPlayerFromSummary(lootSummary, player, lootDropper) == false)
                    return;

                if (_proto.IsTurnInNPC)
                {
                    Mission.OnGiveRewards(avatar);
                    if (Mission.IsOpenMission == false)
                        Mission.LootSeed = 0;
                }
            }
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.PlayerInteractEvent.AddActionBack(_playerInteractAction);
            region.PlayerRequestMissionRewardsEvent.AddActionBack(_playerRequestMissionRewardsAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.PlayerInteractEvent.RemoveAction(_playerInteractAction);
            region.PlayerRequestMissionRewardsEvent.RemoveAction(_playerRequestMissionRewardsAction);

            if (_cinematicEventRegistered)
            {
                region.CinematicFinishedEvent.RemoveAction(_cinematicFinishedAction);
                _cinematicEventRegistered = false;
            }
        }
    }
}
