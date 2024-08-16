using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionAvatarUsedPower : MissionPlayerCondition
    {
        private MissionConditionAvatarUsedPowerPrototype _proto;
        private Action<AvatarUsedPowerGameEvent> _avatarUsedPowerAction;
        protected override long RequiredCount => _proto.Count;

        public MissionConditionAvatarUsedPower(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // TimesBehaviorController
            _proto = prototype as MissionConditionAvatarUsedPowerPrototype;
            _avatarUsedPowerAction = OnAvatarUsedPower;
            // _proto.WithinSeconds always 0
        }

        private void OnAvatarUsedPower(AvatarUsedPowerGameEvent evt)
        {
            var player = evt.Player;
            var avatar = evt.Avatar;
            var powerRef = evt.PowerRef;
            ulong targetId = evt.TargetEntityId;

            if (player == null || avatar == null || IsMissionPlayer(player) == false) return;

            if (_proto.PowerPrototype != PrototypeId.Invalid 
                && GameDatabase.DataDirectory.PrototypeIsAPrototype(powerRef, _proto.PowerPrototype) == false) return;

            if (_proto.AvatarPrototype != PrototypeId.Invalid 
                && avatar.IsAPrototype(_proto.AvatarPrototype) == false) return;

            if (_proto.RegionPrototype != PrototypeId.Invalid)
            {
                var region = avatar.Region;
                if (region == null || region.PrototypeDataRef != _proto.RegionPrototype) return;                
            }

            if (_proto.AreaPrototype != PrototypeId.Invalid)
            {
                var area = avatar.Area;
                if (area == null || area.PrototypeDataRef != _proto.AreaPrototype) return;
            }

            if (_proto.TargetFilter != null)
            {
                var target = Game.EntityManager.GetEntity<WorldEntity>(targetId);
                if (target == null || EvaluateEntityFilter(_proto.TargetFilter, target) == false) return;
            }

            if (_proto.WithinHotspot != PrototypeId.Invalid 
                && Mission.FilterHotspots(avatar, _proto.WithinHotspot, null) == false) return;

            UpdatePlayerContribution(player);
            Count++;
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.AvatarUsedPowerEvent.AddActionBack(_avatarUsedPowerAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.AvatarUsedPowerEvent.RemoveAction(_avatarUsedPowerAction);
        }
    }
}
