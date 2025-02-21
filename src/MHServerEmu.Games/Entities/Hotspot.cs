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
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Entities.PowerCollections;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Helpers;

namespace MHServerEmu.Games.Entities
{
    public class Hotspot : WorldEntity
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public static bool Debug = false;

        private EventPointer<ApplyEffectsDelayEvent> _applyEffectsDelayEvent = new();
        private EventPointer<IntervalPowersEvent> _intervalPowersEvent = new();
        private EventPointer<ActivePowersEvent> _activePowersEvent = new();
        public bool IsMissionHotspot { get => Properties.HasProperty(PropertyEnum.MissionHotspot); }
        public HotspotPrototype HotspotPrototype { get => Prototype as HotspotPrototype; }
        public bool HasApplyEffectsDelay { get; private set; }

        private Dictionary<MissionConditionContext, int> _missionConditionEntityCounter;
        private HashSet<ulong> _missionAvatars;
        private HashSet<ulong> _notifiedPlayers;
        private bool _skipCollide;
        private PropertyCollection _directApplyToMissileProperties;
        private ulong _hotspotTicker;
        private int _overlapEventsTargetCount;

        private Dictionary<ulong, PowerTargetMap> _overlapPowerTargets;
        private ulong _powerUserId;
        private ulong _powerPlayerId;
        private bool _checkLOS;
        private int _targetInvervalMS;
        private int _lifeTimeTargets;
        private int _activePowerTargetCount;
        private bool _killSelf;

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

        private void OnApplyEffectsDelay()
        {
            HasApplyEffectsDelay = false;

            var manager = Game.EntityManager;
            if (manager == null) return;

            List<ulong> overlappingEntities = ListPool<ulong>.Instance.Get();
            if (Physics.GetOverlappingEntities(overlappingEntities))
            {
                var overlapPosition = RegionLocation.Position;
                foreach (ulong entityId in overlappingEntities)
                {
                    var target = manager.GetEntity<WorldEntity>(entityId);
                    if (target == null) continue;
                    OnOverlapBegin(target, overlapPosition, target.RegionLocation.Position);
                }
            }
            ListPool<ulong>.Instance.Return(overlappingEntities);
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
                return;
            }

            if (hotspotProto.AppliesPowers.HasValue() || hotspotProto.AppliesIntervalPowers.HasValue())
            {
                var clusterRef = GameDatabase.GlobalsPrototype.ClusterConfigurationGlobals;
                var clusterProto = GameDatabase.GetPrototype<ClusterConfigurationGlobalsPrototype>(clusterRef);
                var isInTown = IsInTown();
                _checkLOS = isInTown == false || clusterProto.HotspotCheckLOSInTown;
                _targetInvervalMS = isInTown ? clusterProto.HotspotCheckTargetTownIntervalMS : clusterProto.HotspotCheckTargetIntervalMS; 

                _powerUserId = PowerUserOverrideId;
                if (_powerUserId == InvalidId)
                    _powerUserId = Id;
                else
                {
                    var powerUser = Game.EntityManager.GetEntity<WorldEntity>(_powerUserId);
                    if (powerUser != null)
                    {
                        var player = powerUser.GetOwnerOfType<Player>();
                        if (player != null)
                            _powerPlayerId = player.DatabaseUniqueId;
                    }
                }

                if (_overlapPowerTargets == null)
                    _overlapPowerTargets = new();
                else
                    _overlapPowerTargets.Clear();

                if (hotspotProto.AppliesPowers.HasValue())
                    AssignPowers(hotspotProto.AppliesPowers);

                if (hotspotProto.AppliesIntervalPowers.HasValue())
                    AssignPowers(hotspotProto.AppliesIntervalPowers);
            }
        }

        private void AssignPowers(PrototypeId[] powers)
        {
            var powerCollection = PowerCollection;
            var indexProps = new PowerIndexProperties(
                Properties[PropertyEnum.PowerRank], CharacterLevel, CombatLevel,
                Properties[PropertyEnum.ItemLevel], Properties[PropertyEnum.ItemVariation]);

            foreach (var powerRef in powers)
            {
                var powerProto = powerRef.As<PowerPrototype>();
                if (powerProto == null || powerCollection.ContainsPower(powerRef)) continue;
                powerCollection.AssignPower(powerRef, indexProps);
            }
        }

        public override void OnExitedWorld()
        {
            base.OnExitedWorld();

            var scheduler = Game?.GameEventScheduler;
            if (scheduler == null) return;

            scheduler.CancelEvent(_applyEffectsDelayEvent);
            CancelPowerEvents();
        }

        public override void OnOverlapBegin(WorldEntity whom, Vector3 whoPos, Vector3 whomPos)
        {
            base.OnOverlapBegin(whom, whoPos, whomPos);

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

            base.OnSkillshotReflected(missile);
        }

        public override SimulateResult SetSimulated(bool simulated)
        {
            var result = base.SetSimulated(simulated);
            if (result == SimulateResult.Set)
            {
                ScheduleActivePowersEvent();
                ScheduleIntervalPowersEvent();
                _hotspotTicker = StartPropertyTicker(Properties, Id, OwnerId, TimeSpan.FromSeconds(1.0));
            }
            else
            {
                CancelPowerEvents();
                StopPropertyTicker(_hotspotTicker);
            }
            return result;
        }

        // Never activate OnHit / OnKill / OnHotspotNegated procs on the hotspot itself

        public override void TryActivateOnHitProcs(ProcTriggerType triggerType, PowerResults powerResults)
        {
            TryForwardOnHitProcsToOwner(triggerType, powerResults);
        }

        public override void TryActivateOnKillProcs(ProcTriggerType triggerType, PowerResults powerResults)
        {
            TryForwardOnKillProcsToOwner(triggerType, powerResults);
        }

        public override void TryActivateOnHotspotNegatedProcs(WorldEntity other)
        {
            // Forward to owner
            WorldEntity owner = Game.EntityManager.GetEntity<WorldEntity>(PowerUserOverrideId);
            if (owner != null && owner.IsInWorld)
                owner.TryActivateOnHotspotNegatedProcs(other);
        }

        public void OnHotspotNegated(WorldEntity negator, HotspotNegateByAllianceType allianceType, PrototypeId keywordRef, int users)
        {
            var hotspotProto = HotspotPrototype;
            if (hotspotProto.Negatable == false) return;

            var manager = Game.EntityManager;
            if (manager == null) return;

            var entityKeywordRef = GameDatabase.KeywordGlobalsPrototype.EntityKeywordPrototype;
            if (keywordRef != entityKeywordRef && HasKeyword(keywordRef) == false) return;

            if (users != 0)
            {
                var negatorUser = negator.GetMostResponsiblePowerUser<WorldEntity>();
                var negatorUserId = negatorUser != null ? negatorUser.Id : negator.Id;
                if (_powerUserId != negatorUserId) return;
            }

            bool alianceCheck = allianceType switch
            {
                HotspotNegateByAllianceType.All => true,
                HotspotNegateByAllianceType.Allies => negator.IsFriendlyTo(this),
                HotspotNegateByAllianceType.Enemies => negator.IsHostileTo(this),
                _ => false,
            };

            if (alianceCheck == false) return;

            ResetLifespan(TimeSpan.Zero);

            negator.TryActivateOnHotspotNegatedProcs(this);

            var negatorPowerUser = manager.GetEntity<WorldEntity>(negator.PowerUserOverrideId);
            if (negatorPowerUser != null && negatorPowerUser.IsInWorld)
            {
                var power = negatorPowerUser.GetPower(negator.Properties[PropertyEnum.CreatorPowerPrototype]);
                if (power != null)
                {
                    PrototypeId triggeringPowerRef = power.Properties[PropertyEnum.TriggeringPowerRef, power.PrototypeDataRef];
                    if (triggeringPowerRef != PrototypeId.Invalid)
                    {
                        power = negatorPowerUser.GetPower(triggeringPowerRef);
                        if (power == null) return;
                    }
                    power.HandleTriggerPowerEventOnHotspotNegated(this);
                }
            }

            var powerUser = manager.GetEntity<WorldEntity>(_powerUserId);
            if (powerUser != null && powerUser.IsInWorld)
            {
                var power = powerUser.GetPower(Properties[PropertyEnum.CreatorPowerPrototype]);
                if (power != null)
                {
                    PrototypeId triggeringPowerRef = power.Properties[PropertyEnum.TriggeringPowerRef, power.PrototypeDataRef];
                    if (triggeringPowerRef != PrototypeId.Invalid)
                    {
                        power = powerUser.GetPower(triggeringPowerRef);
                        if (power == null) return;
                    }
                    power.HandleTriggerPowerEventOnHotspotNegatedByOther(this);
                }
            }
        }

        public bool IsOverlappingPowerTarget(ulong targetId)
        {
            return _overlapPowerTargets != null && _overlapPowerTargets.ContainsKey(targetId);
        }

        private void HandleOverlapBegin_Missile(Missile missile, Vector3 missilePosition)
        {
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
            var hotspotProto = HotspotPrototype;
            if (hotspotProto == null) return;

            if (hotspotProto.DirectApplyToMissilesData != null 
                && hotspotProto.DirectApplyToMissilesData.AffectsReflectedMissilesOnly) return;

            if (_directApplyToMissileProperties != null && _directApplyToMissileProperties.IsChildOf(missile.Properties))
                _directApplyToMissileProperties.RemoveFromParent(missile.Properties);
        }

        private void HandleOverlapBegin_Missions(WorldEntity target)
        {
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

        private void HandleOverlapBegin_PowerEvent(WorldEntity target)
        {
            //Logger.Trace($"HandleOverlapBegin_PowerEvent {this} {target}");

            if (CanOverlapEvents(target) == false) return;
            _overlapEventsTargetCount++;

            var hotspotProto = HotspotPrototype;
            if (hotspotProto.OverlapEventsMaxTargets != 0 && _overlapEventsTargetCount > hotspotProto.OverlapEventsMaxTargets) return;
            
            var owner = Game.EntityManager.GetEntity<WorldEntity>(PowerUserOverrideId);
            if (owner == null || owner.IsInWorld == false) return;

            var power = owner.GetPower(owner.Properties[PropertyEnum.CreatorPowerPrototype]);
            power?.HandleTriggerPowerEventOnHotspotOverlapBegin(target.Id);
        }

        private void HandleOverlapEnd_PowerEvent(WorldEntity target)
        {
            //Logger.Trace($"HandleOverlapEnd_PowerEvent {this} {target}");

            if (CanOverlapEvents(target) == false) return;
            _overlapEventsTargetCount--;

            var hotspotProto = HotspotPrototype;
            if (hotspotProto.OverlapEventsMaxTargets != 0 && _overlapEventsTargetCount >= hotspotProto.OverlapEventsMaxTargets) return;

            var owner = Game.EntityManager.GetEntity<WorldEntity>(PowerUserOverrideId);
            if (owner == null || owner.IsInWorld == false) return;

            var power = owner.GetPower(owner.Properties[PropertyEnum.CreatorPowerPrototype]);
            power?.HandleTriggerPowerEventOnHotspotOverlapEnd(target.Id);
        }

        private bool CanOverlapEvents(WorldEntity target)
        {
            var hotspotProto = HotspotPrototype;
            if (hotspotProto.OverlapEventsTriggerOn == HotspotOverlapEventTriggerType.None) return false;
            if (target.IsDestructible || target.IsTargetable(this) == false) return false;

            return hotspotProto.OverlapEventsTriggerOn switch
            {
                HotspotOverlapEventTriggerType.All => true,
                HotspotOverlapEventTriggerType.Allies => IsFriendlyTo(target.Alliance),
                HotspotOverlapEventTriggerType.Enemies => IsHostileTo(target.Alliance),
                _ => false,
            };
        }

        private void HandleOverlapBegin_Powers(WorldEntity target)
        {
            if (target.IsAffectedByPowers() == false) return; 
            if (_overlapPowerTargets == null) return;
            
            var hotspotProto = HotspotPrototype;
            var powerTarget = new PowerTargetMap();

            if (hotspotProto.AppliesPowers.HasValue())
                ApplyActivePowers(target, ref powerTarget);
            if (Debug) Logger.Debug($"OverlapBegin Add {target.PrototypeName}[{target.Id}]");
            _overlapPowerTargets[target.Id] = powerTarget;

            ScheduleActivePowersEvent();
            ScheduleIntervalPowersEvent();
        }

        private void HandleOverlapEnd_Powers(WorldEntity target)
        {
            ulong targetId = target.Id;
            var manager = Game.EntityManager;

            if (_overlapPowerTargets == null) return;
            if (_overlapPowerTargets.TryGetValue(targetId, out var powerTarget) == false) return;

            if (Debug) Logger.Debug($"OverlapEnd {target.PrototypeName}[{target.Id}]");
            if (powerTarget.ActivePowers.Any)
            {
                EndPowerForActivePowers(target, ref powerTarget);
                if (_activePowerTargetCount == 0 && HotspotPrototype.KillCreatorWhenHotspotIsEmpty)
                {
                    var creator = manager.GetEntity<WorldEntity>(OwnerId);
                    if (creator != null && creator.IsDead == false)
                        creator.Kill(this);
                }
            }

            _overlapPowerTargets.Remove(targetId);
            if (_overlapPowerTargets.Count == 0)
                CancelPowerEvents();
        }

        public override void OnPowerEnded(Power power, EndPowerFlags flags)
        {
            var powerRef = power.PrototypeDataRef;
            if (powerRef == PrototypeId.Invalid) return;
            var hotspotProto = HotspotPrototype;
            Logger.Debug($"OnPowerEnded {power.PrototypeDataRef.GetNameFormatted()}[{flags}]");
            if (flags.HasFlag(EndPowerFlags.ExitWorld) && hotspotProto.AppliesPowers.HasValue() && _overlapPowerTargets != null)
            {
                int index = Array.IndexOf(hotspotProto.AppliesPowers, powerRef);
                if (index == -1 || index >= 32) return;

                var changed = ListPool <(ulong, PowerTargetMap)>.Instance.Get();

                foreach (var kvp in _overlapPowerTargets)
                {
                    var key = kvp.Key;
                    var powerTarget = kvp.Value;
                    if (powerTarget.ActivePowers[index])
                    {
                        ClearActiveTargetPowers(ref powerTarget, index);
                        changed.Add((key, powerTarget));
                    }
                }

                foreach(var kv in changed)
                    _overlapPowerTargets[kv.Item1] = kv.Item2;

                ListPool<(ulong, PowerTargetMap)>.Instance.Return(changed);
            }
        }

        private void EndPowerForActivePowers(WorldEntity target, ref PowerTargetMap powerTarget)
        {
            Logger.Debug($"EndPowerForActivePowers for {target.PrototypeName}");
            var hotspotProto = HotspotPrototype;
            for (var i = 0; i < hotspotProto.AppliesPowers.Length; i++)
                if (powerTarget.ActivePowers[i])
                {
                    var powerProto = hotspotProto.AppliesPowers[i].As<PowerPrototype>();
                    if (powerProto != null) 
                        EndPowerForActiveTarget(powerProto.DataRef, target.Id, ref powerTarget, i);
                }
        }

        private void EndPowerForActiveTarget(PrototypeId powerRef, ulong targetId, ref PowerTargetMap powerTarget, int index)
        {
            ClearActiveTargetPowers(ref powerTarget, index);
            var power = GetPower(powerRef);
            if (power == null) return;

            if (Debug) Logger.Debug($"EndPowerForActiveTarget for {powerRef.GetNameFormatted()} {targetId}");
            power.CancelScheduledPowerApplicationsForTarget(targetId);
            if (power.Prototype.CancelConditionsOnEnd)
                power.RemoveOrUnpauseTrackedConditionsForTarget(targetId);
        }

        private void ClearActiveTargetPowers(ref PowerTargetMap powerTarget, int index)
        {
            if (powerTarget.ActivePowers[index])
            {
                powerTarget.ActivePowers.Reset(index);
                if (powerTarget.ActivePowers.Empty && _activePowerTargetCount > 0)
                    _activePowerTargetCount--;
            }
        }

        public override ChangePositionResult ChangeRegionPosition(Vector3? position, Orientation? orientation, ChangePositionFlags flags = ChangePositionFlags.None)
        {
            bool isOrientation = orientation.HasValue;
            if (isOrientation == false) orientation = RegionLocation.Orientation;
            if (position.HasValue == false) position = RegionLocation.Position;

            float centerOffset = Bounds.GetCenterOffset();
            if (centerOffset > 0.0f)
            {
                var region = Region;
                if (region == null) return ChangePositionResult.NotChanged;

                var forward = Forward;
                if (isOrientation)
                {
                    if (flags.HasFlag(ChangePositionFlags.EnterWorld) == false)
                    {
                        var summonProto = GetSummonEntityContext();
                        if (summonProto == null) return ChangePositionResult.NotChanged;
                        if (Segment.IsNearZero(summonProto.SummonOffsetAngle) == false)
                        {
                            float angle = MathHelper.ToRadians(summonProto.SummonOffsetAngle);
                            var newOrientation = orientation.Value;
                            newOrientation.Yaw += angle;
                            orientation = newOrientation;
                        }
                    }

                    var transform = Transform3.BuildTransform(Vector3.Zero, orientation.Value);
                    forward = transform.Col0;
                }

                var offsetPosition = position.Value + forward * centerOffset;                
                if (region.GetCellAtPosition(offsetPosition) != null)
                    position = offsetPosition;

                if (Debug) Logger.Debug($"ChangeRegionPosition {PrototypeName} at {position} {orientation}");
            }

            return base.ChangeRegionPosition(position, orientation, flags);
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

        private bool CanSchedulePowersEvent()
        {
            if (_killSelf || IsSimulated == false) return false;
            if (TestStatus(EntityStatus.PendingDestroy) || TestStatus(EntityStatus.Destroyed)) return false;
            return _overlapPowerTargets != null && _overlapPowerTargets.Count > 0;
        }

        private void OnApplyIntervalPowers()
        {
            var hotspotProto = HotspotPrototype;
            if (_overlapPowerTargets == null) return;

            if (hotspotProto.IntervalPowersRandomTarget)
            {
                Picker<ulong> picker = new(Game.Random);
                var hasLOS = TriBool.Undefined;
                ulong prevTargetId = InvalidId;

                foreach (var powerRef in hotspotProto.AppliesIntervalPowers)
                {
                    var powerProto = powerRef.As<PowerPrototype>();
                    if (powerProto == null) continue; 

                    picker.Clear();
                    foreach (var targetPower in _overlapPowerTargets)
                        picker.Add(targetPower.Key);

                    int numTargets = hotspotProto.IntervalPowersNumRandomTargets;
                    ulong targetId = InvalidId;
                    while (numTargets > 0 && picker.PickRemove(out targetId))
                    {
                        if (targetId != prevTargetId) hasLOS = TriBool.Undefined;
                        prevTargetId = targetId;
                        if (ActivateIntervalPowerForTarget(powerRef, targetId, ref hasLOS))
                            numTargets--;
                    }
                }
            }
            else
            {
                int numTargets = 0;
                foreach (var powerTarget in _overlapPowerTargets)
                {
                    var hasLOS = TriBool.Undefined;
                    bool activated = false;

                    foreach (var powerRef in hotspotProto.AppliesIntervalPowers)
                    {
                        var powerProto = powerRef.As<PowerPrototype>();
                        if (powerProto == null) continue;
                        ulong targetId = powerTarget.Key;
                        activated |= ActivateIntervalPowerForTarget(powerRef, targetId, ref hasLOS);
                    }

                    if (activated)
                    {
                        numTargets++;
                        if (hotspotProto.MaxSimultaneousTargets > 0 && numTargets >= hotspotProto.MaxSimultaneousTargets)
                            break;
                    }
                }
            }

            ScheduleIntervalPowersEvent();
        }

        private bool ActivateIntervalPowerForTarget(PrototypeId powerRef, ulong targetId, ref TriBool hasLOS)
        {
            var manager = Game?.EntityManager;
            if (manager == null) return false;

            var target = manager.GetEntity<WorldEntity>(targetId);
            if (target == null) return false;

            var power = GetPower(powerRef);
            if (power == null) return false;

            if (power.IsValidTarget(target) == false) return false;

            if (_checkLOS && power.RequiresLineOfSight())
            {
                if (hasLOS == TriBool.Undefined)
                    hasLOS = target.LineOfSightTo(GetHotspotCenter()) ? TriBool.True : TriBool.False;

                if (hasLOS != TriBool.True)
                    return false;
            }

            var settings = new PowerActivationSettings(target.Id, target.RegionLocation.Position, RegionLocation.Position);
            settings.Flags |= PowerActivationSettingsFlags.NotifyOwner;
            if (_skipCollide) settings.Flags |= PowerActivationSettingsFlags.SkipRangeCheck;

            var result = power.Activate(ref settings);
            if (Debug) Logger.Debug($"ActivateIntervalPower {power.PrototypeDataRef.GetNameFormatted()} from {PrototypeName} to {target.PrototypeName}");
            return result == PowerUseResult.Success;
        }

        private void ScheduleIntervalPowersEvent()
        {
            var hotspotProto = HotspotPrototype;
            if (hotspotProto.AppliesIntervalPowers.IsNullOrEmpty()) return;
            int intervalMS = hotspotProto.IntervalPowersTimeDelayMS;
            if (intervalMS <= 0) return;
            if (_intervalPowersEvent.IsValid) return;
            if (CanSchedulePowersEvent()) 
                ScheduleEntityEvent(_intervalPowersEvent, TimeSpan.FromMilliseconds(intervalMS));
        }

        private void OnApplyActivePowers()
        {
            if (_overlapPowerTargets == null) return;

            var manager = Game.EntityManager;

            var changed = ListPool<(ulong, PowerTargetMap)>.Instance.Get();

            foreach (var entry in _overlapPowerTargets)
            {
                ulong targetId = entry.Key;
                var powerTarget = entry.Value;
                var target = manager.GetEntity<WorldEntity>(targetId);
                if (target == null) continue;
                ApplyActivePowers(target, ref powerTarget);
                changed.Add((targetId, powerTarget));
            }

            foreach (var kv in changed)
                _overlapPowerTargets[kv.Item1] = kv.Item2;

            ListPool<(ulong, PowerTargetMap)>.Instance.Return(changed);

            ScheduleActivePowersEvent();
        }

        private void ApplyActivePowers(WorldEntity target, ref PowerTargetMap powerTarget)
        {
            var hotspotProto = HotspotPrototype;
            if (_killSelf) return;

            if (hotspotProto.MaxSimultaneousTargets > 0 
                && _activePowerTargetCount >= hotspotProto.MaxSimultaneousTargets
                && powerTarget.ActivePowers.Empty) return;

            bool hasLOS = false;
            bool checkedLOS = false;
            bool activated = false;

            for (var i = 0; i < hotspotProto.AppliesPowers.Length; i++)
            {
                if (powerTarget.IgnorePowers[i]) continue;

                var powerProto = hotspotProto.AppliesPowers[i].As<PowerPrototype>();
                if (powerProto == null) continue;

                // check conditions
                if (powerProto.AppliesConditions != null || powerProto.ConditionsByRef.HasValue())
                {
                    if (HasConditionsForTarget(powerProto, target, out bool hasOthers) == false)
                    {
                        ClearActiveTargetPowers(ref powerTarget, i);
                        if (hasOthers == false) continue;
                    }
                }
                else
                {
                    powerTarget.IgnorePowers.Set(i);
                }

                // check valid target and LOS
                bool isValidTarget = Power.IsValidTarget(powerProto, this, Alliance, target);
                if (isValidTarget && _checkLOS && Power.RequiresLineOfSight(powerProto))
                {
                    if (checkedLOS == false)
                    {
                        hasLOS = target.LineOfSightTo(GetHotspotCenter());
                        checkedLOS = true;
                    }
                    isValidTarget = hasLOS;
                }

                // activate powers
                if (powerTarget.ActivePowers[i] == false && isValidTarget)
                {
                    activated |= ActivatePowerForTarget(powerProto.DataRef, target, ref powerTarget, i);
                }
                else if (powerTarget.ActivePowers[i] && isValidTarget == false)
                { 
                    EndPowerForActiveTarget(powerProto.DataRef, target.Id, ref powerTarget, i);
                }                
            }

            if (activated)
            {
                if (hotspotProto.MaxLifetimeTargets > 0) _lifeTimeTargets++;
                OnPowerActivated();
            }
        }

        private void OnPowerActivated()
        {
            var hotspotProto = HotspotPrototype;
            if (hotspotProto.KillSelfWhenPowerApplied || (hotspotProto.MaxLifetimeTargets > 0 && _lifeTimeTargets >= hotspotProto.MaxLifetimeTargets))
                _killSelf = true;

            if (_killSelf)
            {
                if (hotspotProto.RemoveFromWorldTimerMS > 0)
                {
                    SummonPower.KillSummoned(this, null);
                }
                else
                {
                    TryActivateOnDeathProcs(new());
                    ResetLifespan(TimeSpan.Zero);
                }
                CancelPowerEvents();
            }
        }

        private bool ActivatePowerForTarget(PrototypeId powerRef, WorldEntity target, ref PowerTargetMap powerTarget, int index)
        {
            var power = GetPower(powerRef);
            if (power == null) return false;

            var settings = new PowerActivationSettings(target.Id, target.RegionLocation.Position, RegionLocation.Position);
            settings.Flags |= PowerActivationSettingsFlags.NotifyOwner;
            if (_skipCollide) settings.Flags |= PowerActivationSettingsFlags.SkipRangeCheck;

            var result = power.Activate(ref settings);
            if (result == PowerUseResult.Success)
            {
                if (powerTarget.ActivePowers.Empty) _activePowerTargetCount++;
                powerTarget.ActivePowers.Set(index);
                return true;
            }

            return false;
        }

        private Vector3 GetHotspotCenter()
        {
            Vector3 center = RegionLocation.Position;
            float centerOffset = Bounds.GetCenterOffset();
            if (centerOffset > 0.0f)
                return center - Forward * centerOffset;
            else
                return center;
        }

        private bool HasConditionsForTarget(PowerPrototype powerProto, WorldEntity target, out bool hasOthers)
        {
            bool hasThis = false;
            hasOthers = false;

            var conditionCollection = target.ConditionCollection;
            if (conditionCollection == null) return false;

            if (powerProto.AppliesConditions != null)
                foreach (var mixinPrototype in powerProto.AppliesConditions)
                {
                    if (mixinPrototype.Prototype is not ConditionPrototype conditionProto) continue;
                    if (CheckStackingBehavior(conditionProto, powerProto, conditionCollection, ref hasOthers, ref hasThis))
                        return hasThis;
                }

            if (powerProto.ConditionsByRef.HasValue())
                foreach (var conditionRef in powerProto.ConditionsByRef)
                {
                    var conditionProto = conditionRef.As<ConditionPrototype>();
                    if (conditionProto == null) continue;
                    if (CheckStackingBehavior(conditionProto, powerProto, conditionCollection, ref hasOthers, ref hasThis))
                        return hasThis;
                }

            return hasThis;
        }

        private bool CheckStackingBehavior(ConditionPrototype conditionProto, PowerPrototype powerProto, 
            ConditionCollection conditionCollection, ref bool hasOthers, ref bool hasThis)
        {
            var stackId = ConditionCollection.MakeConditionStackId(powerProto, conditionProto, _powerUserId, _powerPlayerId, out var stackingProto);
            if (stackId.PrototypeRef == PrototypeId.Invalid || stackingProto == null) return false;

            if (stackingProto.StacksFromDifferentCreators)
            {
                int othersStacks = conditionCollection.GetStackApplicationData(stackId, stackingProto, Properties[PropertyEnum.PowerRank], Id,
                    out int thisStacks, out _);

                hasOthers |= othersStacks > 0;
                hasThis |= thisStacks > 0;
            }
            else
            {
                int thisStacks = conditionCollection.GetNumberOfStacks(stackId);
                int othersStacks = stackingProto.MaxNumStacks - thisStacks;

                hasOthers |= othersStacks > 0;
                hasThis |= thisStacks > 0;
            }

            return (hasOthers && hasThis) || powerProto.StackingBehaviorLEGACY != null;
        }

        private void ScheduleActivePowersEvent()
        {
            var hotspotProto = HotspotPrototype;
            if (hotspotProto.AppliesPowers.IsNullOrEmpty()) return;
            if (hotspotProto.MaxLifetimeTargets > 0 && _lifeTimeTargets >= hotspotProto.MaxLifetimeTargets) return;
            if (_activePowersEvent.IsValid) return;
            if (CanSchedulePowersEvent())
                ScheduleEntityEvent(_activePowersEvent, TimeSpan.FromMilliseconds(_targetInvervalMS));
        }

        private void CancelPowerEvents()
        {
            var scheduler = Game?.GameEventScheduler;
            if (scheduler == null) return;

            scheduler.CancelEvent(_activePowersEvent);
            scheduler.CancelEvent(_intervalPowersEvent);
        }

        #region Events

        protected class ApplyEffectsDelayEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => (t as Hotspot)?.OnApplyEffectsDelay();
        }

        protected class IntervalPowersEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => (t as Hotspot)?.OnApplyIntervalPowers();
        }

        protected class ActivePowersEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => (t as Hotspot)?.OnApplyActivePowers();
        }

        #endregion
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

    public struct PowerTargetMap
    {
        public HotspotPowerMask ActivePowers;
        public HotspotPowerMask IgnorePowers;
    }

    public struct HotspotPowerMask
    {
        private uint _bits;

        public readonly bool Any => _bits != 0;
        public readonly bool Empty => _bits == 0;

        public bool this[int index]
        {
            get => Get(index);
            set => Set(index, value);
        }

        public bool Get(int index)
        {
            return (_bits & (1u << index)) != 0;
        }

        public void Set(int index, bool value)
        {
            if (value) Set(index);
            else Reset(index);
        }

        public void Set(int index)
        {
            _bits |= (1u << index);
        }

        public void Reset(int index)
        {
            _bits &= ~(1u << index);
        }

        public void Clear()
        {
            _bits = 0;
        }
    }

}
