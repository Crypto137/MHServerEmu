using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Dialog;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionHealthRange : MissionPlayerCondition
    {
        private MissionConditionHealthRangePrototype _proto;
        private Action<AdjustHealthGameEvent> _adjustHealthAction;

        public MissionConditionHealthRange(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // FirstHeal
            _proto = prototype as MissionConditionHealthRangePrototype;
            _adjustHealthAction = OnAdjustHealth;
        }

        public override bool OnReset()
        {
            bool healthChanged = false;
            if (_proto.EntityFilter != null)
            {
                var region = Region;
                if (region != null)
                {
                    var entityTracker = region.EntityTracker;
                    if (entityTracker == null) return false;
                    foreach(var entity in entityTracker.Iterate(Mission.PrototypeDataRef, EntityTrackingFlag.MissionCondition)) 
                        if (entity != null && entity.IsDead == false && EvaluateEntity(entity))
                        {
                            healthChanged = true;
                            break;
                        }
                }
            }
            else
            {
                List<Player> participants = ListPool<Player>.Instance.Get();
                if (Mission.GetParticipants(participants))
                {
                    foreach (var player in participants)
                    {
                        var avatar = player.CurrentAvatar;
                        if (avatar != null)
                        {
                            if (EvaluateEntity(avatar))
                            {
                                healthChanged = true;
                                break;
                            }
                        }
                    }
                }
                ListPool<Player>.Instance.Return(participants);
            }               

            SetCompletion(healthChanged);
            return true;
        }

        private bool EvaluateEntity(WorldEntity entity, long damage = 0)
        {
            if (entity == null) return false;

            if (_proto.EntityFilter != null)
            {
                if (EvaluateEntityFilter(_proto.EntityFilter, entity) == false) return false;
            }
            else
            {
                if (entity is not Avatar avatar) return false;
                var player = avatar.GetOwnerOfType<Player>();
                if (player == null || IsMissionPlayer(player) == false) return false;
            }

            long healthMax = entity.Properties[PropertyEnum.HealthMax];
            if (healthMax <= 0) return false;

            long health = entity.Properties[PropertyEnum.Health] + damage;
            float healthPct = MathHelper.Ratio(health, healthMax);
            if (healthPct < _proto.HealthMinPct || healthPct > _proto.HealthMaxPct) return false;

            return true;
        }

        private void OnAdjustHealth(AdjustHealthGameEvent evt)
        {
            if (evt.Dodged) return;
            var player = evt.Player;
            var entity = evt.Entity;
            long damage = evt.Damage;

            if (EvaluateEntity(entity, damage))
            {
                if (player != null)
                    UpdatePlayerContribution(player);
                SetCompleted();
            }
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.AdjustHealthEvent.AddActionBack(_adjustHealthAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.AdjustHealthEvent.RemoveAction(_adjustHealthAction);
        }
    }
}
