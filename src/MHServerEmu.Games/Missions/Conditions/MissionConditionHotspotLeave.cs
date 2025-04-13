using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionHotspotLeave : MissionPlayerCondition
    {
        private MissionConditionHotspotLeavePrototype _proto;
        private Event<EntityLeftMissionHotspotGameEvent>.Action _entityLeftMissionHotspotAction;

        public MissionConditionHotspotLeave(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // RaftNPEQuinjetKismetController
            _proto = prototype as MissionConditionHotspotLeavePrototype;
            _entityLeftMissionHotspotAction = OnEntityLeftMissionHotspot;
        }

        public override bool OnReset()
        {
            var missionRef = Mission.PrototypeDataRef;
            bool leave = true;
            if (_proto.TargetFilter != null)
            {
                List<Hotspot> hotspots = ListPool<Hotspot>.Instance.Get();
                if (Mission.GetMissionHotspots(hotspots))
                {
                    foreach (var hotspot in hotspots)
                        if (EvaluateEntityFilter(_proto.EntityFilter, hotspot)
                            && hotspot.GetMissionConditionCount(missionRef, _proto) > 0)
                        {
                            leave = false;
                            break;
                        }
                }
                ListPool<Hotspot>.Instance.Return(hotspots);
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
                            if (Mission.FilterHotspots(avatar, PrototypeId.Invalid, _proto.EntityFilter))
                            {
                                leave = false;
                                break;
                            }
                        }
                    }
                }
                ListPool<Player>.Instance.Return(participants);
            }

            SetCompletion(leave);
            return true;
        }

        private bool EvaluateEntity(WorldEntity entity, Hotspot hotspot)
        {
            if (hotspot == null) return false;
            if (EvaluateEntityFilter(_proto.EntityFilter, hotspot) == false) return false;

            if (_proto.TargetFilter != null)
            {
                if (entity == null) return false;
                if (EvaluateEntityFilter(_proto.TargetFilter, entity) == false) return false;
            }
            else
            {
                if (entity is not Avatar avatar) return false;
                var player = avatar.GetOwnerOfType<Player>();
                if (player == null || IsMissionPlayer(player) == false) return false;
            }

            return true;
        }

        private void OnEntityLeftMissionHotspot(in EntityLeftMissionHotspotGameEvent evt)
        {
            var entity = evt.Target;
            var hotspot = evt.Hotspot;

            if (EvaluateEntity(entity, hotspot) == false) return;

            if (entity is Avatar avatar)
            {
                var player = avatar.GetOwnerOfType<Player>();
                if (player != null)
                    UpdatePlayerContribution(player);
            }
            SetCompleted();
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.EntityLeftMissionHotspotEvent.AddActionBack(_entityLeftMissionHotspotAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.EntityLeftMissionHotspotEvent.RemoveAction(_entityLeftMissionHotspotAction);
        }
    }
}
