using Gazillion;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateCombatQueueLockout : MetaState
    {
	    private MetaStateCombatQueueLockoutPrototype _proto;
        private Event<EntityEnteredCombatGameEvent>.Action _entityEnteredCombat;
        private Event<EntityExitedCombatGameEvent>.Action _entityExitedCombat;
        private EventPointer<CheckAccessEvent> _checkAccessEvent = new();
        private HashSet<ulong> _combatList = [];
        private RegionPlayerAccessVar _access;

        public MetaStateCombatQueueLockout(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateCombatQueueLockoutPrototype;
            _entityEnteredCombat = OnEntityEnteredCombat;
            _entityExitedCombat = OnEntityExitedCombat;
            _access = RegionPlayerAccessVar.eRPA_InviteOnly;
        }

        public override void OnApply()
        {
            var region = Region;
            if (region == null) return;

            region.EntityEnteredCombatEvent.AddActionBack(_entityEnteredCombat);
            region.EntityExitedCombatEvent.AddActionBack(_entityExitedCombat);
        }

        public override void OnRemove()
        {
            var region = Region;
            if (region == null) return;
            
            region.EntityEnteredCombatEvent.RemoveAction(_entityEnteredCombat);
            region.EntityExitedCombatEvent.RemoveAction(_entityExitedCombat);            
            CancelCheckAcessEvent();

            base.OnRemove();
        }

        private void SetAccess(RegionPlayerAccessVar access)
        {
            if (Region == null || access == _access) return;
            ServiceMessage.SetRegionPlayerAccess message = new(Region.Id, access);
            ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, message);
            _access = access;
        }

        private void OnEntityEnteredCombat(in EntityEnteredCombatGameEvent evt)
        {
            var entity = evt.Entity;
            if (entity == null || _proto.EntityFilter == null) return;

            if (_proto.EntityFilter.Evaluate(entity, new()))
            {
                _combatList.Add(entity.Id);
                ScheduleCheckAccess(TimeSpan.Zero);
            }
        }

        private void OnEntityExitedCombat(in EntityExitedCombatGameEvent evt)
        {
            var entity = evt.Entity;
            if (entity == null || _proto.EntityFilter == null) return;

            if (_proto.UnlockOnCombatExit && _proto.EntityFilter.Evaluate(entity, new()))
            {
                _combatList.Remove(entity.Id);
                ScheduleCheckAccess(TimeSpan.Zero);
            }
        }

        private void OnCheckAcess()
        {
            if (_combatList.Count == 0)
            {
                CancelCheckAcessEvent();
                return;
            }

            var manager = Game.EntityManager;
            if (manager == null) return;

            List<ulong> toRemove = ListPool<ulong>.Instance.Get();

            foreach (ulong id in _combatList)
            {
                var entity = manager.GetEntity<WorldEntity>(id);
                if (entity == null)
                    toRemove.Add(id);
            }

            foreach (ulong id in toRemove)
                _combatList.Remove(id);

            ListPool<ulong>.Instance.Return(toRemove);

            if (_combatList.Count > 0)
            {
                SetAccess(RegionPlayerAccessVar.eRPA_Locked);
                ScheduleCheckAccess(TimeSpan.FromSeconds(5));
            } 
            else
                CancelCheckAcessEvent();
        }

        private void ScheduleCheckAccess(TimeSpan timeOffset)
        {
            var scheduler = Game.GameEventScheduler;
            if (scheduler == null) return;

            if (_checkAccessEvent.IsValid)
            {
                scheduler.RescheduleEvent(_checkAccessEvent, timeOffset);
            }
            else
            {
                scheduler.ScheduleEvent(_checkAccessEvent, timeOffset, _pendingEvents);
                _checkAccessEvent.Get().Initialize(this);
            }
        }

        private void CancelCheckAcessEvent()
        {
            SetAccess(RegionPlayerAccessVar.eRPA_InviteOnly);

            var scheduler = Game.GameEventScheduler;
            if (scheduler == null || _checkAccessEvent.IsValid == false) return;
            scheduler.CancelEvent(_checkAccessEvent);
        }

        public class CheckAccessEvent : CallMethodEvent<MetaStateCombatQueueLockout>
        {
            protected override CallbackDelegate GetCallback() => state => state.OnCheckAcess();
        }
    }
}
