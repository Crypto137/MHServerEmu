using MHServerEmu.Games.Dialog;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Common;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    public class Hotspot : WorldEntity
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private EventPointer<ApplyEffectsDelayEvent> _applyEffectsDelayEvent = new();
        public bool IsMissionHotspot { get => Properties.HasProperty(PropertyEnum.MissionHotspot); }
        public HotspotPrototype HotspotPrototype { get => Prototype as HotspotPrototype; }
        public bool HasApplyEffectsDelay { get; private set; }

        private Dictionary<MissionConditionContext, int> _missionConditionEntityCounter;
        private HashSet<ulong> _missionAvatars;
        private HashSet<ulong> _notifiedPlayers;
        private bool _skipCollide;
        private PropertyCollection _directApplyToMissileProperties;

        public Hotspot(Game game) : base(game) 
        { 
            SetFlag(EntityFlags.IsHotspot, true); 
        }

        public override bool Initialize(EntitySettings settings)
        {
            base.Initialize(settings);

            // if (GetPowerCollectionAllocateIfNull() == null) return false;
            var hotspotProto = HotspotPrototype;
            _skipCollide = settings.HotspotSkipCollide;
            HasApplyEffectsDelay = hotspotProto.ApplyEffectsDelayMS > 0;

            if (hotspotProto.DirectApplyToMissilesData?.EvalPropertiesToApply != null || hotspotProto.Negatable)
                SetFlag(EntityFlags.IsCollidableHotspot, true);

            return true;
        }

        public override bool CanCollideWith(WorldEntity other)
        {
            if (_skipCollide) return false;
            return base.CanCollideWith(other);
        }

        protected class ApplyEffectsDelayEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => (t as Hotspot)?.OnApplyEffectsDelay();
        }

        private void OnApplyEffectsDelay()
        {
            HasApplyEffectsDelay = false;

            // TODO Apply effect for Physics.OverlappedEntities
        }

        public override void OnEnteredWorld(EntitySettings settings)
        {
            base.OnEnteredWorld(settings);
            var hotspotProto = HotspotPrototype;
            if (hotspotProto.ApplyEffectsDelayMS > 0)
            {
                if (Game.GameEventScheduler == null) return;
                ScheduleEntityEvent(_applyEffectsDelayEvent, TimeSpan.FromMilliseconds(hotspotProto.ApplyEffectsDelayMS));
            }

            var missilesData = hotspotProto.DirectApplyToMissilesData;
            if (missilesData != null)
            {
                _directApplyToMissileProperties = new();
                if (missilesData.EvalPropertiesToApply != null)
                {
                    using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                    evalContext.Game = Game;
                    evalContext.SetVar_PropertyCollectionPtr(EvalContext.Default, _directApplyToMissileProperties);
                    evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, Properties);
                    if (Eval.RunBool(missilesData.EvalPropertiesToApply, evalContext) == false) 
                    {
                        Logger.Warn("Eval.RunBool EvalPropertiesToApply == false");
                        return; 
                    }
                }
            }

            if (hotspotProto.UINotificationOnEnter != null)
                _notifiedPlayers = new();

            if (IsMissionHotspot)
            {
                _missionConditionEntityCounter = new();
                _missionAvatars = new();
                MissionEntityTracker();
            }

            // TODO hotspotProto.AppliesPowers
        }

        public override void OnExitedWorld()
        {
            base.OnExitedWorld();

            var scheduler = Game?.GameEventScheduler;
            if (scheduler == null) return;
            scheduler.CancelEvent(_applyEffectsDelayEvent);

            // TODO cancel other events
        }

        public override void OnOverlapBegin(WorldEntity whom, Vector3 whoPos, Vector3 whomPos)
        {
            if (HasApplyEffectsDelay || whom == null || whom is Hotspot) return;

            if (whom is Missile missile)
            {
                HandleOverlapBegin_Missile(missile, whomPos);
                return;
            }

            if (whom is Avatar avatar)
                HandleOverlapBegin_Player(avatar);

            if (IsMissionHotspot)
                HandleOverlapBegin_Missions(whom);
            else
            {
                var hotspotProto = HotspotPrototype;
                if (hotspotProto != null && (hotspotProto.AppliesPowers.HasValue() || hotspotProto.AppliesIntervalPowers.HasValue()))
                    HandleOverlapBegin_Powers(whom);

                HandleOverlapBegin_PowerEvent(whom);
            }
        }

        public override void OnOverlapEnd(WorldEntity whom)
        {
            if (HasApplyEffectsDelay || whom == null || whom is Hotspot) return;

            if (whom is Missile missile)
            {
                HandleOverlapEnd_Missile(missile);
                return;
            }

            if (whom is Avatar avatar)
                HandleOverlapEnd_Player(avatar);

            if (IsMissionHotspot)
                HandleOverlapEnd_Missions(whom);
            else
            {
                HandleOverlapEnd_PowerEvent(whom);

                var hotspotProto = HotspotPrototype;
                if (hotspotProto != null && (hotspotProto.AppliesPowers.HasValue() || hotspotProto.AppliesIntervalPowers.HasValue()))
                    HandleOverlapEnd_Powers(whom);
            }
        }

        public override void OnSkillshotReflected(Missile missile)
        {
            if (missile.IsMovedIndependentlyOnClient)
            {
                var hotspotProto = HotspotPrototype;
                if (hotspotProto == null) return;

                var missilesData = hotspotProto.DirectApplyToMissilesData;
                if (missilesData != null && missilesData.AffectsReflectedMissilesOnly)
                {
                    if (missilesData.IsPermanent)
                        missile.Properties.FlattenCopyFrom(_directApplyToMissileProperties, false);
                    else
                        missile.Properties.AddChildCollection(_directApplyToMissileProperties);
                }
            }
        }

        private void HandleOverlapBegin_Missile(Missile missile, Vector3 missilePosition)
        {
            //Logger.Trace($"HandleOverlapBegin_Missile {this} {missile} {missilePosition}");
            var hotspotProto = HotspotPrototype;
            if (hotspotProto == null) return;

            var missilesData = hotspotProto.DirectApplyToMissilesData;
            if (missilesData != null)
            {
                if ((missilesData.AffectsAllyMissiles && IsFriendlyTo(missile))
                    || (missilesData.AffectsHostileMissiles && IsHostileTo(missile)))
                {
                    if (missilesData.IsPermanent)
                        missile.Properties.FlattenCopyFrom(_directApplyToMissileProperties, false);
                    else
                        missile.Properties.AddChildCollection(_directApplyToMissileProperties);
                }
            }

            if (Properties[PropertyEnum.MissileBlockingHotspot] && missile.Properties[PropertyEnum.MissileBlockingHotspotImmunity] == false)
                missile.OnCollide(null, missilePosition);
        }

        private void HandleOverlapEnd_Missile(Missile missile)
        {
            //Logger.Trace($"HandleOverlapEnd_Missile {this} {missile}");
            var hotspotProto = HotspotPrototype;
            if (hotspotProto == null) return;

            if (hotspotProto.DirectApplyToMissilesData != null 
                && hotspotProto.DirectApplyToMissilesData.AffectsReflectedMissilesOnly) return;

            if (_directApplyToMissileProperties != null && _directApplyToMissileProperties.IsChildOf(missile.Properties))
                _directApplyToMissileProperties.RemoveFromParent(missile.Properties);
        }

        private void HandleOverlapBegin_Missions(WorldEntity target)
        {
            // Logger.Trace($"HandleOverlapBegin_Missions {this} {target}");
            bool targetAvatar = target is Avatar;
            if (targetAvatar) _missionAvatars?.Add(target.Id);

            bool missionEvent = false;
            if (_missionConditionEntityCounter.Count > 0)
                foreach(var context in _missionConditionEntityCounter)
                {
                    var missionRef = context.Key.MissionRef;
                    var conditionProto = context.Key.ConditionProto;
                    if (EvaluateTargetCondition(target, missionRef, conditionProto))
                    {
                        _missionConditionEntityCounter[context.Key]++;
                        missionEvent = true;
                    }
                }

            if (Region == null) return;
            // entered hotspot mision event
            if (missionEvent || targetAvatar)
                Region.EntityEnteredMissionHotspotEvent.Invoke(new(target, this));
        }

        private void HandleOverlapEnd_Missions(WorldEntity target)
        {
            // Logger.Trace($"HandleOverlapEnd_Missions {this} {target}");
            bool targetAvatar = target is Avatar;
            if (targetAvatar) _missionAvatars?.Remove(target.Id);

            bool missionEvent = false;
            if (_missionConditionEntityCounter.Count > 0)
                foreach (var context in _missionConditionEntityCounter)
                {
                    var missionRef = context.Key.MissionRef;
                    var conditionProto = context.Key.ConditionProto;
                    if (EvaluateTargetCondition(target, missionRef, conditionProto))
                    {
                        _missionConditionEntityCounter[context.Key]--;
                        missionEvent = true;
                    }
                }

            if (Region == null) return;
            // left hotspot mision event
            if (missionEvent || targetAvatar)
                Region.EntityLeftMissionHotspotEvent.Invoke(new(target, this));
        }

        public bool ContainsAvatar(Avatar avatar)
        {
            return _missionAvatars != null && _missionAvatars.Contains(avatar.Id);
        }

        public int GetMissionConditionCount(PrototypeId missionRef, MissionConditionPrototype conditionProto)
        {
            if (_missionConditionEntityCounter != null)
            {
                var key = new MissionConditionContext(missionRef, conditionProto);
                if (_missionConditionEntityCounter.TryGetValue(key, out int count))
                    return count;
            }
            return 0;
        }

        private void HandleOverlapBegin_Player(Avatar avatar)
        {
            var player = avatar.GetOwnerOfType<Player>();
            if (player == null) return;

            player.OnScoringEvent(new(ScoringEventType.HotspotEnter, Prototype));

            PrototypeId waypointRef = Properties[PropertyEnum.WaypointHotspotUnlock];
            if (waypointRef != PrototypeId.Invalid)
                player.UnlockWaypoint(waypointRef);

            var manager = Game.EntityManager;
            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.HotspotTriggerEntity))
            {
                Property.FromParam(kvp.Key, 0, out int triggerEnum);
                ulong spawnerId = kvp.Value;
                if (spawnerId != 0)
                {
                    var spawner = manager.GetEntity<Spawner>(spawnerId);
                    if (spawner != null)
                    {
                        spawner.Trigger((EntityTriggerEnum)triggerEnum);
                        ScheduleDestroyEvent(TimeSpan.Zero);
                    }
                }
            }

            PrototypeId targetRespawnRef = Properties[PropertyEnum.RespawnHotspotOverride];
            if (targetRespawnRef != PrototypeId.Invalid)
                player.Properties[PropertyEnum.RespawnHotspotOverrideInst, targetRespawnRef] = Id;

            var hotspotProto = HotspotPrototype;

            if (hotspotProto.UINotificationOnEnter != null && _notifiedPlayers.Contains(player.Id) == false)
            {
                player.SendUINotification(hotspotProto.UINotificationOnEnter);
                _notifiedPlayers.Add(player.Id);
            }

            if (hotspotProto.TutorialTip != PrototypeId.Invalid)
                player.ShowHUDTutorial(hotspotProto.TutorialTip.As<HUDTutorialPrototype>());

            if (hotspotProto.KismetSeq != PrototypeId.Invalid)
                player.PlayKismetSeq(hotspotProto.KismetSeq);
        }

        private void HandleOverlapEnd_Player(Avatar avatar)
        {
            var player = avatar.GetOwnerOfType<Player>();
            if (player == null) return;

            PrototypeId targetRespawnRef = Properties[PropertyEnum.RespawnHotspotOverride];
            if (targetRespawnRef != PrototypeId.Invalid && player.Properties[PropertyEnum.RespawnHotspotOverrideInst, targetRespawnRef] == Id)
                player.Properties.RemoveProperty(new(PropertyEnum.RespawnHotspotOverrideInst, targetRespawnRef));
        }

        private void HandleOverlapBegin_PowerEvent(WorldEntity whom)
        {
            //Logger.Trace($"HandleOverlapBegin_PowerEvent {this} {whom}");
        }

        private void HandleOverlapEnd_PowerEvent(WorldEntity whom)
        {
            //Logger.Trace($"HandleOverlapEnd_PowerEvent {this} {whom}");
        }

        private void HandleOverlapBegin_Powers(WorldEntity whom)
        {
            //Logger.Trace($"HandleOverlapBegin_Powers {this} {whom}");
        }

        private void HandleOverlapEnd_Powers(WorldEntity whom)
        {
            //Logger.Trace($"HandleOverlapEnd_Powers {this} {whom}");
        }

        private void MissionEntityTracker()
        {
            EntityTrackingContextMap involvementMap = new();
            if (GameDatabase.InteractionManager.GetEntityContextInvolvement(this, involvementMap) == false) return;
            foreach (var involment in involvementMap)
            {
                if (involment.Value.HasFlag(EntityTrackingFlag.Hotspot) == false) continue;
                var missionRef = involment.Key;
                var missionProto = GameDatabase.GetPrototype<MissionPrototype>(involment.Key);
                if (missionProto == null) continue;
                var conditionList = missionProto.HotspotConditionList;
                if (conditionList == null) continue;
                foreach(var conditionProto in conditionList)
                    if (EvaluateHotspotCondition(missionRef, conditionProto))
                    {
                        var key = new MissionConditionContext(missionRef, conditionProto);
                        _missionConditionEntityCounter[key] = 0;
                    }
            }

        }

        private bool EvaluateHotspotCondition(PrototypeId missionRef, MissionConditionPrototype conditionProto)
        {
            if (conditionProto == null) return false;

            if (conditionProto is MissionConditionHotspotContainsPrototype hotspotContainsProto)
                return hotspotContainsProto.EntityFilter != null && hotspotContainsProto.EntityFilter.Evaluate(this, new(missionRef));
            if (conditionProto is MissionConditionHotspotEnterPrototype hotspotEnterProto)
                return hotspotEnterProto.EntityFilter != null && hotspotEnterProto.EntityFilter.Evaluate(this, new(missionRef));
            if (conditionProto is MissionConditionHotspotLeavePrototype hotspotLeaveProto)
                return hotspotLeaveProto.EntityFilter != null && hotspotLeaveProto.EntityFilter.Evaluate(this, new(missionRef));
            return false;
        }

        private bool EvaluateTargetCondition(WorldEntity target, PrototypeId missionRef, MissionConditionPrototype conditionProto)
        {
            if (conditionProto == null) return false;

            if (conditionProto is MissionConditionHotspotContainsPrototype hotspotContainsProto)
                return hotspotContainsProto.TargetFilter != null && hotspotContainsProto.TargetFilter.Evaluate(target, new(missionRef));
            if (conditionProto is MissionConditionHotspotEnterPrototype hotspotEnterProto)
                return hotspotEnterProto.TargetFilter != null && hotspotEnterProto.TargetFilter.Evaluate(target, new(missionRef));
            if (conditionProto is MissionConditionHotspotLeavePrototype hotspotLeaveProto)
                return hotspotLeaveProto.TargetFilter != null && hotspotLeaveProto.TargetFilter.Evaluate(target, new(missionRef));
            return false;
        }
    }

    public class MissionConditionContext
    {
        public PrototypeId MissionRef;
        public MissionConditionPrototype ConditionProto;

        public MissionConditionContext(PrototypeId missionRef, MissionConditionPrototype conditionProto)
        {
            MissionRef = missionRef;
            ConditionProto = conditionProto;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;
            var other = (MissionConditionContext)obj;
            return MissionRef.Equals(other.MissionRef) && ConditionProto.Equals(other.ConditionProto);
        }

        public override int GetHashCode()
        {
            return MissionRef.GetHashCode() ^ ConditionProto.GetHashCode();
        }
    }

}
