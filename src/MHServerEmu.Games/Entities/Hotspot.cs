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
using MHServerEmu.Games.Behavior;
using static MHServerEmu.Games.Missions.MissionManager;

namespace MHServerEmu.Games.Entities
{
    public class Hotspot : WorldEntity
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private EventPointer<ApplyEffectsDelayEvent> _applyEffectsDelayEvent = new();
        public bool IsMissionHotspot { get => Properties.HasProperty(PropertyEnum.MissionHotspot); }
        public HotspotPrototype HotspotPrototype { get => Prototype as HotspotPrototype; }
        public bool HasApplyEffectsDelay { get; private set; }

        private Dictionary<MissionConditionContext, int> ConditionEntityCounter;
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
                    EvalContextData evalContext = new(Game);
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
            {
                // TODO UINotification
            }

            if (IsMissionHotspot)
            {
                ConditionEntityCounter = new();
                MissionEntityTracker();
            }

            // TODO hotspotProto.AppliesPowers
        }

        public override void OnExitedWorld()
        {
            base.OnExitedWorld();

            var sheduler = Game?.GameEventScheduler;
            if (sheduler == null) return;
            sheduler.CancelEvent(_applyEffectsDelayEvent);

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
            Logger.Trace($"HandleOverlapBegin_Missile {this} {missile} {missilePosition}");
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
            Logger.Trace($"HandleOverlapEnd_Missile {this} {missile}");
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
            bool missionEvent = false;
            if (ConditionEntityCounter.Count > 0)
                foreach(var context in ConditionEntityCounter)
                {
                    var missionRef = context.Key.MissionRef;
                    var conditionProto = context.Key.ConditionProto;
                    if (EvaluateTargetCondition(target, missionRef, conditionProto))
                    {
                        ConditionEntityCounter[context.Key]++;
                        missionEvent = true;
                    }
                    // Hardcoded part
                    if (targetAvatar) OLD_AvatarEnter(target as Avatar, missionRef, conditionProto);
                }

            if (Region == null) return;
            // entered hotspot mision event
            if (missionEvent || targetAvatar) 
            {
                EntityEnteredMissionHotspotGameEvent hotspotEvent = new(target, this);
                Region.EntityEnteredMissionHotspotEvent.Invoke(hotspotEvent);
            }
        }

        private void HandleOverlapEnd_Missions(WorldEntity target)
        {
            // Logger.Trace($"HandleOverlapEnd_Missions {this} {target}");
            bool targetAvatar = target is Avatar;
            bool missionEvent = false;
            if (ConditionEntityCounter.Count > 0)
                foreach (var context in ConditionEntityCounter)
                {
                    var missionRef = context.Key.MissionRef;
                    var conditionProto = context.Key.ConditionProto;
                    if (EvaluateTargetCondition(target, missionRef, conditionProto))
                    {
                        ConditionEntityCounter[context.Key]--;
                        missionEvent = true;
                    }
                    // Hardcoded part
                    if (targetAvatar) OLD_AvatarLeave(target as Avatar, missionRef, conditionProto);
                }

            if (Region == null) return;
            // left hotspot mision event
            if (missionEvent || targetAvatar)
            {
                EntityLeftMissionHotspotGameEvent hotspotEvent = new(target, this);
                Region.EntityLeftMissionHotspotEvent.Invoke(hotspotEvent);
            }
        }

        #region hardcoded

        public static MissionPrototypeId[] HotspotEnterKismetControllers = new MissionPrototypeId[]
        {
            MissionPrototypeId.RaftNPEVenomKismetController,
            MissionPrototypeId.RaftNPEElectroKismetController,
            MissionPrototypeId.RaftNPEGreenGoblinKismetController,
            MissionPrototypeId.RaftNPEJuggernautKismetController,
            MissionPrototypeId.OpVultureKismetController, 
            MissionPrototypeId.CH06BlobKismetController,
            MissionPrototypeId.CH07MrSinisterKismetController,
            MissionPrototypeId.CH07SabretoothKismetController,
            //MissionPrototypeId.CH08MODOKSpawnKismetController, // Client blocked
        };

        public static MissionPrototypeId[] HotspotLeaveKismetControllers = new MissionPrototypeId[]
        {
          //  MissionPrototypeId.RaftNPEQuinjetKismetController,
        };

        private void OLD_AvatarEnter(Avatar avatar, PrototypeId missionRef, MissionConditionPrototype conditionProto)
        {
            // TODO move this as EntityEnteredMissionHotspotEvent in MissionManager
            if (conditionProto is MissionConditionHotspotEnterPrototype)
            {
                var mission = GameDatabase.GetPrototype<MissionPrototype>(missionRef);                
                if (HotspotEnterKismetControllers.Contains((MissionPrototypeId)missionRef))
                {
                    PropertyId missionReset = new(PropertyEnum.AvatarMissionResetsWithRegionId, missionRef);
                    if (avatar.Properties.HasProperty(missionReset)) return;
                    if (avatar.GetOwner() is not Player player) return;
                    if (mission.Objectives.IsNullOrEmpty()) return;
                    var objective = mission.Objectives[0];
                    if (MissionPrototypeId.CH08MODOKSpawnKismetController == (MissionPrototypeId)missionRef)
                        objective = mission.Objectives[3];
                    MissionActionPlayKismetSeqPrototype missionKismetSeq = null;
                    if (objective != null && objective.OnSuccessActions.HasValue()) 
                        missionKismetSeq = objective.OnSuccessActions[0] as MissionActionPlayKismetSeqPrototype;
                    else if(mission.OnSuccessActions.HasValue()) 
                        missionKismetSeq = mission.OnSuccessActions[0] as MissionActionPlayKismetSeqPrototype;
                    if (missionKismetSeq == null) return;
                    var kismetSeq = missionKismetSeq.KismetSeqPrototype;
                    avatar.Properties[missionReset] = Region.Id;
                    if (objective.InteractionsWhenActive.HasValue() && objective.InteractionsWhenActive[0] is EntityVisibilitySpecPrototype visibilitySpec)
                        OLD_HideEntity(visibilitySpec);
                    player.PlayKismetSeq(kismetSeq);
                }
                // Logger.Warn($"AvatarEnter {avatar} {mission}");
            }
        }

        private void OLD_HideEntity(EntityVisibilitySpecPrototype visibilitySpec) 
        {
            HashSet<PrototypeId> refs = new();
            visibilitySpec.EntityFilter.GetEntityDataRefs(refs);
            if (visibilitySpec.Visible == false && refs.Count > 0)
                foreach(var entity in RegionLocation.Cell.Entities)
                    if (refs.Contains(entity.PrototypeDataRef))
                        entity.Properties[PropertyEnum.Visible] = false;
        }

        private void OLD_AvatarLeave(Avatar avatar, PrototypeId missionRef, MissionConditionPrototype conditionProto)
        {
            // TODO move this as EntityLeftMissionHotspotEvent in MissionManager
            if (conditionProto is MissionConditionHotspotLeavePrototype)
            {
                var mission = GameDatabase.GetPrototype<MissionPrototype>(missionRef);
                if (HotspotLeaveKismetControllers.Contains((MissionPrototypeId)missionRef))
                {
                    PropertyId missionReset = new(PropertyEnum.AvatarMissionObjectiveRegionId, missionRef);
                    if (avatar.Properties.HasProperty(missionReset)) return;
                    if (avatar.GetOwner() is not Player player) return;
                    if (mission.Objectives.IsNullOrEmpty()) return;
                    var objective = mission.Objectives[0];
                    if (objective == null || objective.OnSuccessActions.IsNullOrEmpty()) return;
                    var missionKismetSeq = objective.OnSuccessActions[0] as MissionActionPlayKismetSeqPrototype;
                    var kismetSeq = missionKismetSeq.KismetSeqPrototype;
                    avatar.Properties[missionReset] = Region.Id;
                    player.PlayKismetSeq(kismetSeq);
                }
                // Logger.Warn($"AvatarLeave {avatar} {mission}");
            }
        }

        #endregion

        private void HandleOverlapBegin_Player(Avatar avatar)
        {
            // Logger.Trace($"HandleOverlapBegin_Player {this} {avatar}");

            // TODO Unlock Waypoint Properties[PropertyEnum.WaypointHotspotUnlock]
        }

        private void HandleOverlapEnd_Player(Avatar avatar)
        {
            // Logger.Trace($"HandleOverlapEnd_Player {this} {avatar}");
        }

        private void HandleOverlapBegin_PowerEvent(WorldEntity whom)
        {
            Logger.Trace($"HandleOverlapBegin_PowerEvent {this} {whom}");
        }

        private void HandleOverlapEnd_PowerEvent(WorldEntity whom)
        {
            Logger.Trace($"HandleOverlapEnd_PowerEvent {this} {whom}");
        }

        private void HandleOverlapBegin_Powers(WorldEntity whom)
        {
            Logger.Trace($"HandleOverlapBegin_Powers {this} {whom}");
        }

        private void HandleOverlapEnd_Powers(WorldEntity whom)
        {
            Logger.Trace($"HandleOverlapEnd_Powers {this} {whom}");
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
                        ConditionEntityCounter[key] = 0;
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
