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

        private Picker<ulong> _targetPicker;    // Reusable picker for AppliesIntervalPowers hotspots, remove this if we implement picker pooling.

        public Hotspot(Game game) : base(game) 
        { 
            SetFlag(EntityFlags.IsHotspot, true); 
        }

        public override bool Initialize(EntitySettings settings)
        {
            if (!Verify.IsTrue(base.Initialize(settings))) return false;

            HotspotPrototype hotspotProto = HotspotPrototype;
            if (!Verify.IsNotNull(hotspotProto)) return false;

            if (!Verify.IsNotNull(GetPowerCollectionAllocateIfNull())) return false;

            _skipCollide = settings.HotspotSkipCollide;
            HasApplyEffectsDelay = hotspotProto.ApplyEffectsDelayMS > 0;

            if (hotspotProto.DirectApplyToMissilesData?.EvalPropertiesToApply != null || hotspotProto.Negatable)
                SetFlag(EntityFlags.IsCollidableHotspot, true);

            if (hotspotProto.IntervalPowersRandomTarget)
                _targetPicker = new(Game.Random);

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

            EntityManager entityManager = Game.EntityManager;
            if (!Verify.IsNotNull(entityManager)) return;

            using var overlappingEntitiesHandle = ListPool<ulong>.Instance.Get(out List<ulong> overlappingEntities);
            if (Physics.GetOverlappingEntities(overlappingEntities))
            {
                Vector3 overlapPosition = RegionLocation.Position;
                foreach (ulong entityId in overlappingEntities)
                {
                    WorldEntity target = entityManager.GetEntity<WorldEntity>(entityId);
                    if (!Verify.IsNotNull(target))
                        continue;

                    OnOverlapBegin(target, overlapPosition, target.RegionLocation.Position);
                }
            }
        }

        public override void OnEnteredWorld(EntitySettings settings)
        {
            base.OnEnteredWorld(settings);

            HotspotPrototype hotspotProto = HotspotPrototype;
            if (!Verify.IsNotNull(hotspotProto)) return;

            if (hotspotProto.ApplyEffectsDelayMS > 0)
            {
                if (!Verify.IsNotNull(Game.GameEventScheduler)) return;
                ScheduleEntityEvent(_applyEffectsDelayEvent, TimeSpan.FromMilliseconds(hotspotProto.ApplyEffectsDelayMS));
            }

            HotspotDirectApplyToMissilesDataPrototype missilesData = hotspotProto.DirectApplyToMissilesData;
            if (missilesData != null)
            {
                _directApplyToMissileProperties = new();

                if (missilesData.EvalPropertiesToApply != null)
                {
                    using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                    evalContext.Game = Game;
                    evalContext.SetVar_PropertyCollectionPtr(EvalContext.Default, _directApplyToMissileProperties);
                    evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, Properties);

                    if (!Verify.IsTrue(Eval.RunBool(missilesData.EvalPropertiesToApply, evalContext))) return;
                }
            }

            if (hotspotProto.UINotificationOnEnter != null)
                _notifiedPlayers = new();

            if (IsMissionHotspot)
            {
                _missionConditionEntityCounter = new();
                _missionAvatars = new();
                InitializeMissionEntityTracker();
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

            EventScheduler scheduler = Game?.GameEventScheduler;
            if (!Verify.IsNotNull(scheduler)) return;

            scheduler.CancelEvent(_applyEffectsDelayEvent);
            CancelPowerEvents();
        }

        public override void OnOverlapBegin(WorldEntity overlappedWith, Vector3 thisPosition, Vector3 otherPosition)
        {
            base.OnOverlapBegin(overlappedWith, thisPosition, otherPosition);

            if (HasApplyEffectsDelay)
                return;

            if (!Verify.IsNotNull(overlappedWith)) return;

            if (overlappedWith is Hotspot)
                return;

            if (overlappedWith is Missile missile)
            {
                HandleOverlapBegin_Missile(missile, otherPosition);
                return;
            }

            if (overlappedWith is Avatar avatar)
                HandleOverlapBegin_Player(avatar);

            if (IsMissionHotspot)
            {
                HandleOverlapBegin_Missions(overlappedWith);
            }
            else
            {
                HotspotPrototype hotspotProto = HotspotPrototype;
                if (!Verify.IsNotNull(hotspotProto)) return;

                if (hotspotProto.AppliesPowers.HasValue() || hotspotProto.AppliesIntervalPowers.HasValue())
                    HandleOverlapBegin_Powers(overlappedWith);

                HandleOverlapBegin_PowerEvent(overlappedWith);
            }
        }

        public override void OnOverlapEnd(WorldEntity overlappedWith)
        {
            base.OnOverlapEnd(overlappedWith);

            if (HasApplyEffectsDelay)
                return;

            if (!Verify.IsNotNull(overlappedWith)) return;

            if (overlappedWith is Hotspot)
                return;

            if (overlappedWith is Missile missile)
            {
                HandleOverlapEnd_Missile(missile);
                return;
            }

            if (overlappedWith is Avatar avatar)
                HandleOverlapEnd_Player(avatar);

            if (IsMissionHotspot)
            {
                HandleOverlapEnd_Missions(overlappedWith);
            }
            else
            {
                HandleOverlapEnd_PowerEvent(overlappedWith);

                HotspotPrototype hotspotProto = HotspotPrototype;
                if (!Verify.IsNotNull(hotspotProto)) return;

                if (hotspotProto.AppliesPowers.HasValue() || hotspotProto.AppliesIntervalPowers.HasValue())
                    HandleOverlapEnd_Powers(overlappedWith);
            }
        }

        public override void OnSkillshotReflected(Missile missile)
        {
            HotspotPrototype hotspotProto = HotspotPrototype;
            if (!Verify.IsNotNull(hotspotProto)) return;

            HotspotDirectApplyToMissilesDataPrototype missilesData = hotspotProto.DirectApplyToMissilesData;
            if (missilesData != null && missilesData.AffectsReflectedMissilesOnly)
            {
                if (missile.IsMovedIndependentlyOnClient)
                {
                    if (missilesData.IsPermanent)
                        missile.Properties.FlattenCopyFrom(_directApplyToMissileProperties, false);
                    else
                        missile.Properties.AddChildCollection(_directApplyToMissileProperties);
                }
                else
                {
                    missile.Properties.AddChildCollection(_directApplyToMissileProperties);
                }
            }

            base.OnSkillshotReflected(missile);
        }

        public override SimulateResult SetSimulated(bool simulated)
        {
            SimulateResult result = base.SetSimulated(simulated);

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
            HotspotPrototype hotspotProto = HotspotPrototype;
            if (!Verify.IsNotNull(hotspotProto)) return;

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
            HotspotPrototype hotspotProto = HotspotPrototype;
            if (!Verify.IsNotNull(hotspotProto)) return;

            HotspotDirectApplyToMissilesDataPrototype missilesData = hotspotProto.DirectApplyToMissilesData;
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
            HotspotPrototype hotspotProto = HotspotPrototype;
            if (!Verify.IsNotNull(hotspotProto)) return;

            if (hotspotProto.DirectApplyToMissilesData != null && hotspotProto.DirectApplyToMissilesData.AffectsReflectedMissilesOnly)
                return;

            if (_directApplyToMissileProperties != null && _directApplyToMissileProperties.IsChildOf(missile.Properties))
                _directApplyToMissileProperties.RemoveFromParent(missile.Properties);
        }

        private void HandleOverlapBegin_Missions(WorldEntity target)
        {
            bool targetIsAvatar = target is Avatar;
            if (targetIsAvatar)
            {
                if (!Verify.IsNotNull(_missionAvatars)) return;
                _missionAvatars.Add(target.Id);
            }

            bool hasMissionEvent = false;
            if (_missionConditionEntityCounter != null)
            {
                foreach (MissionConditionContext context in _missionConditionEntityCounter.Keys)
                {
                    PrototypeId missionRef = context.MissionRef;
                    MissionConditionPrototype conditionProto = context.ConditionProto;
                    if (EvaluateTargetCondition(target, missionRef, conditionProto))
                    {
                        _missionConditionEntityCounter[context]++;
                        hasMissionEvent = true;
                    }
                }
            }

            // entered hotspot mission event
            if (hasMissionEvent || targetIsAvatar)
            {
                Region region = Region;
                if (Verify.IsNotNull(region))
                    region.EntityEnteredMissionHotspotEvent.Invoke(new(target, this));
            }                
        }

        private void HandleOverlapEnd_Missions(WorldEntity target)
        {
            bool targetIsAvatar = target is Avatar;
            if (targetIsAvatar)
            {
                if (!Verify.IsNotNull(_missionAvatars)) return;
                _missionAvatars.Remove(target.Id);
            }

            bool hasMissionEvent = false;
            if (_missionConditionEntityCounter != null)
            {
                foreach (MissionConditionContext context in _missionConditionEntityCounter.Keys)
                {
                    PrototypeId missionRef = context.MissionRef;
                    MissionConditionPrototype conditionProto = context.ConditionProto;
                    if (EvaluateTargetCondition(target, missionRef, conditionProto))
                    {
                        _missionConditionEntityCounter[context]--;
                        hasMissionEvent = true;
                    }
                }
            }

            // left hotspot mission event
            if (hasMissionEvent || targetIsAvatar)
            {
                Region region = Region;
                if (Verify.IsNotNull(region))
                    region.EntityLeftMissionHotspotEvent.Invoke(new(target, this));
            }
        }

        public bool ContainsAvatar(Avatar avatar)
        {
            return _missionAvatars != null && _missionAvatars.Contains(avatar.Id);
        }

        public int GetMissionConditionCount(PrototypeId missionRef, MissionConditionPrototype conditionProto)
        {
            if (_missionConditionEntityCounter != null)
            {
                MissionConditionContext key = new(missionRef, conditionProto);
                if (_missionConditionEntityCounter.TryGetValue(key, out int count))
                    return count;
            }

            return 0;
        }

        private void HandleOverlapBegin_Player(Avatar avatar)
        {
            Player player = avatar.GetOwnerOfType<Player>();
            if (!Verify.IsNotNull(player)) return;

            player.OnScoringEvent(new(ScoringEventType.HotspotEnter, Prototype));

            PrototypeId waypointRef = Properties[PropertyEnum.WaypointHotspotUnlock];
            if (waypointRef != PrototypeId.Invalid)
                player.UnlockWaypoint(waypointRef);

            EntityManager entityManager = Game.EntityManager;
            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.HotspotTriggerEntity))
            {
                Property.FromParam(kvp.Key, 0, out int triggerEnum);
                ulong spawnerId = kvp.Value;
                if (spawnerId != InvalidId)
                {
                    Spawner spawner = entityManager.GetEntity<Spawner>(spawnerId);
                    if (Verify.IsNotNull(spawner))
                    {
                        spawner.Trigger((EntityTriggerEnum)triggerEnum);
                        ScheduleDestroyEvent(TimeSpan.Zero);
                    }
                }
            }

            PrototypeId targetRespawnRef = Properties[PropertyEnum.RespawnHotspotOverride];
            if (targetRespawnRef != PrototypeId.Invalid)
                player.Properties[PropertyEnum.RespawnHotspotOverrideInst, targetRespawnRef] = Id;

            HotspotPrototype hotspotProto = HotspotPrototype;
            if (!Verify.IsNotNull(hotspotProto)) return;

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
            Player player = avatar.GetOwnerOfType<Player>();
            if (!Verify.IsNotNull(player)) return;

            PrototypeId targetRespawnRef = Properties[PropertyEnum.RespawnHotspotOverride];
            if (targetRespawnRef != PrototypeId.Invalid && player.Properties[PropertyEnum.RespawnHotspotOverrideInst, targetRespawnRef] == Id)
                player.Properties.RemoveProperty(new(PropertyEnum.RespawnHotspotOverrideInst, targetRespawnRef));
        }

        private void HandleOverlapBegin_PowerEvent(WorldEntity target)
        {
            //Logger.Trace($"HandleOverlapBegin_PowerEvent {this} {target}");

            if (CanTriggerOverlapEvents(target) == false)
                return;
            
            _overlapEventsTargetCount++;

            HotspotPrototype hotspotProto = HotspotPrototype;
            if (!Verify.IsNotNull(hotspotProto)) return;

            if (hotspotProto.OverlapEventsMaxTargets != 0 && _overlapEventsTargetCount > hotspotProto.OverlapEventsMaxTargets)
                return;
            
            WorldEntity owner = Game.EntityManager.GetEntity<WorldEntity>(PowerUserOverrideId);
            if (owner == null || owner.IsInWorld == false)
                return;

            Power power = owner.GetPower(owner.Properties[PropertyEnum.CreatorPowerPrototype]);
            power?.HandleTriggerPowerEventOnHotspotOverlapBegin(target.Id);
        }

        private void HandleOverlapEnd_PowerEvent(WorldEntity target)
        {
            //Logger.Trace($"HandleOverlapEnd_PowerEvent {this} {target}");

            if (CanTriggerOverlapEvents(target) == false)
                return;

            _overlapEventsTargetCount--;

            HotspotPrototype hotspotProto = HotspotPrototype;
            if (!Verify.IsNotNull(hotspotProto)) return;

            if (hotspotProto.OverlapEventsMaxTargets != 0 && _overlapEventsTargetCount >= hotspotProto.OverlapEventsMaxTargets)
                return;

            WorldEntity owner = Game.EntityManager.GetEntity<WorldEntity>(PowerUserOverrideId);
            if (owner == null || owner.IsInWorld == false)
                return;

            Power power = owner.GetPower(owner.Properties[PropertyEnum.CreatorPowerPrototype]);
            power?.HandleTriggerPowerEventOnHotspotOverlapEnd(target.Id);
        }

        private bool CanTriggerOverlapEvents(WorldEntity target)
        {
            HotspotPrototype hotspotProto = HotspotPrototype;
            if (!Verify.IsNotNull(hotspotProto)) return false;

            if (hotspotProto.OverlapEventsTriggerOn == HotspotOverlapEventTriggerType.None)
                return false;

            if (target.IsDestructible || target.IsTargetable(this) == false)
                return false;

            switch (hotspotProto.OverlapEventsTriggerOn)
            {
                case HotspotOverlapEventTriggerType.All:
                    return true;

                case HotspotOverlapEventTriggerType.Allies:
                    return IsFriendlyTo(target.Alliance);

                case HotspotOverlapEventTriggerType.Enemies:
                    return IsHostileTo(target.Alliance);

                default:
                    Verify.IsTrue(false);
                    return false;
            }
        }

        private void HandleOverlapBegin_Powers(WorldEntity target)
        {
            if (target.IsAffectedByPowers() == false)
                return;

            if (!Verify.IsNotNull(_overlapPowerTargets)) return;
            
            HotspotPrototype hotspotProto = HotspotPrototype;
            if (!Verify.IsNotNull(hotspotProto)) return;
            
            PowerTargetMap powerTarget = new();

            if (hotspotProto.AppliesPowers.HasValue())
                ApplyActivePowers(target, ref powerTarget);
            if (Debug) Logger.Debug($"OverlapBegin Add {target.PrototypeName}[{target.Id}]");
            _overlapPowerTargets[target.Id] = powerTarget;

            ScheduleActivePowersEvent();
            ScheduleIntervalPowersEvent();
        }

        private void HandleOverlapEnd_Powers(WorldEntity target)
        {
            if (!Verify.IsNotNull(_overlapPowerTargets)) return;

            ulong targetId = target.Id;
            EntityManager entityManager = Game.EntityManager;

            if (_overlapPowerTargets.TryGetValue(targetId, out PowerTargetMap powerTarget) == false)
                return;

            if (Debug) Logger.Debug($"OverlapEnd {target.PrototypeName}[{target.Id}]");
            if (powerTarget.ActivePowers.Any)
            {
                EndPowerForActivePowers(target, ref powerTarget);

                if (_activePowerTargetCount == 0 && HotspotPrototype.KillCreatorWhenHotspotIsEmpty)
                {
                    WorldEntity creator = entityManager.GetEntity<WorldEntity>(OwnerId);
                    if (creator != null && creator.IsDead == false)
                        creator.Kill(this);
                }
            }

            _overlapPowerTargets.Remove(targetId);
            if (_overlapPowerTargets.Count == 0)
            {
                CancelPowerEvents();
                Verify.IsTrue(_activePowerTargetCount == 0);
            }
        }

        public override void OnPowerEnded(Power power, EndPowerFlags flags)
        {
            PrototypeId powerRef = power.PrototypeDataRef;
            if (!Verify.IsTrue(powerRef != PrototypeId.Invalid)) return;
            
            HotspotPrototype hotspotProto = HotspotPrototype;
            if (!Verify.IsNotNull(hotspotProto)) return;

            if (Debug) Logger.Debug($"OnPowerEnded {power.PrototypeDataRef.GetNameFormatted()}[{flags}]");

            if (flags.HasFlag(EndPowerFlags.ExitWorld) && hotspotProto.AppliesPowers.HasValue() && _overlapPowerTargets != null)
            {
                // Powers not included in AppliesPowers can get here, ignore them.
                int index = Array.IndexOf(hotspotProto.AppliesPowers, powerRef);
                if (index == -1)
                    return;

                if (!Verify.IsTrue(index < HotspotPowerMask.Size))
                    return;

                // TODO: iterate _overlapPowerTargets keys and get refs using GetValueOrDefault() to remove this pooled dictionary?
                using var changedHandle = ListPool<(ulong, PowerTargetMap)>.Instance.Get(out List<(ulong, PowerTargetMap)> changed);

                foreach (var kvp in _overlapPowerTargets)
                {
                    ulong key = kvp.Key;
                    PowerTargetMap powerTarget = kvp.Value;
                    if (powerTarget.ActivePowers[index])
                    {
                        ClearActiveTargetPowers(ref powerTarget, index);
                        changed.Add((key, powerTarget));
                    }
                }

                foreach (var kv in changed)
                    _overlapPowerTargets[kv.Item1] = kv.Item2;
            }
        }

        private void EndPowerForActivePowers(WorldEntity target, ref PowerTargetMap powerTarget)
        {
            if (Debug) Logger.Debug($"EndPowerForActivePowers for {target.PrototypeName}");

            HotspotPrototype hotspotProto = HotspotPrototype;
            if (!Verify.IsNotNull(hotspotProto)) return;
            if (!Verify.IsTrue(hotspotProto.AppliesPowers.HasValue())) return;

            for (int i = 0; i < hotspotProto.AppliesPowers.Length; i++)
            {
                if (powerTarget.ActivePowers[i])
                {
                    PowerPrototype powerProto = hotspotProto.AppliesPowers[i].As<PowerPrototype>();
                    if (!Verify.IsNotNull(powerProto)) return;
                    
                    EndPowerForActiveTarget(powerProto.DataRef, target.Id, ref powerTarget, i);
                }
            }
        }

        private void EndPowerForActiveTarget(PrototypeId powerRef, ulong targetId, ref PowerTargetMap powerTarget, int index)
        {
            ClearActiveTargetPowers(ref powerTarget, index);

            Power power = GetPower(powerRef);
            if (!Verify.IsNotNull(power)) return;

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
                if (powerTarget.ActivePowers.Empty && Verify.IsTrue(_activePowerTargetCount > 0))
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
                Region region = Region;
                if (!Verify.IsNotNull(region)) return ChangePositionResult.NotChanged;

                Vector3 forward = Forward;
                if (isOrientation)
                {
                    if (flags.HasFlag(ChangePositionFlags.EnterWorld) == false)
                    {
                        SummonEntityContextPrototype summonProto = GetSummonEntityContext();
                        if (!Verify.IsNotNull(summonProto)) return ChangePositionResult.NotChanged;

                        if (Segment.IsNearZero(summonProto.SummonOffsetAngle) == false)
                        {
                            float angle = MathHelper.ToRadians(summonProto.SummonOffsetAngle);
                            Orientation newOrientation = orientation.Value;
                            newOrientation.Yaw += angle;
                            orientation = newOrientation;
                        }
                    }

                    Transform3 transform = Transform3.BuildTransform(Vector3.Zero, orientation.Value);
                    forward = transform.Col0;
                }

                Vector3 offsetPosition = position.Value + forward * centerOffset;                
                if (region.GetCellAtPosition(offsetPosition) != null)
                    position = offsetPosition;

                //if (Debug) Logger.Debug($"ChangeRegionPosition {PrototypeName} at {position} {orientation}");
            }

            return base.ChangeRegionPosition(position, orientation, flags);
        }

        private void InitializeMissionEntityTracker()
        {
            EntityTrackingContextMap involvementMap = new();
            if (GameDatabase.InteractionManager.GetEntityContextInvolvement(this, involvementMap) == false)
                return;

            foreach (var involvement in involvementMap)
            {
                if (involvement.Value.HasFlag(EntityTrackingFlag.Hotspot) == false)
                    continue;
                
                PrototypeId missionRef = involvement.Key;
                MissionPrototype missionProto = missionRef.As<MissionPrototype>();
                if (missionProto == null)
                    continue;
                
                List<MissionConditionPrototype> conditionList = missionProto.HotspotConditionList;
                if (conditionList == null)
                    continue;

                foreach (MissionConditionPrototype conditionProto in conditionList)
                {
                    if (EvaluateHotspotCondition(missionRef, conditionProto))
                    {
                        MissionConditionContext key = new(missionRef, conditionProto);
                        _missionConditionEntityCounter[key] = 0;
                    }
                }
            }
        }

        private bool EvaluateHotspotCondition(PrototypeId missionRef, MissionConditionPrototype conditionProto)
        {
            if (!Verify.IsNotNull(conditionProto)) return false;

            if (conditionProto is MissionConditionHotspotContainsPrototype hotspotContainsProto)
                return hotspotContainsProto.EntityFilter != null && hotspotContainsProto.EntityFilter.Evaluate(this, new(missionRef));
            if (conditionProto is MissionConditionHotspotEnterPrototype hotspotEnterProto)
                return hotspotEnterProto.EntityFilter != null && hotspotEnterProto.EntityFilter.Evaluate(this, new(missionRef));
            if (conditionProto is MissionConditionHotspotLeavePrototype hotspotLeaveProto)
                return hotspotLeaveProto.EntityFilter != null && hotspotLeaveProto.EntityFilter.Evaluate(this, new(missionRef));

            Verify.IsTrue(false);
            return false;
        }

        private bool EvaluateTargetCondition(WorldEntity target, PrototypeId missionRef, MissionConditionPrototype conditionProto)
        {
            if (!Verify.IsNotNull(conditionProto)) return false;

            if (conditionProto is MissionConditionHotspotContainsPrototype hotspotContainsProto)
                return hotspotContainsProto.TargetFilter != null && hotspotContainsProto.TargetFilter.Evaluate(target, new(missionRef));
            if (conditionProto is MissionConditionHotspotEnterPrototype hotspotEnterProto)
                return hotspotEnterProto.TargetFilter != null && hotspotEnterProto.TargetFilter.Evaluate(target, new(missionRef));
            if (conditionProto is MissionConditionHotspotLeavePrototype hotspotLeaveProto)
                return hotspotLeaveProto.TargetFilter != null && hotspotLeaveProto.TargetFilter.Evaluate(target, new(missionRef));

            Verify.IsTrue(false);
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
            HotspotPrototype hotspotProto = HotspotPrototype;
            if (!Verify.IsNotNull(hotspotProto)) return;
            if (!Verify.IsTrue(hotspotProto.AppliesIntervalPowers.HasValue())) return;
            if (!Verify.IsNotNull(_overlapPowerTargets)) return;

            if (hotspotProto.IntervalPowersRandomTarget)
            {
                Picker<ulong> picker = _targetPicker;
                bool? hasLOS = null;
                ulong prevTargetId = InvalidId;

                foreach (PrototypeId powerRef in hotspotProto.AppliesIntervalPowers)
                {
                    PowerPrototype powerProto = powerRef.As<PowerPrototype>();
                    if (!Verify.IsNotNull(powerProto))
                        continue;

                    picker.Clear();
                    foreach (var targetPower in _overlapPowerTargets)
                        picker.Add(targetPower.Key);

                    int numTargets = hotspotProto.IntervalPowersNumRandomTargets;
                    ulong targetId = InvalidId;
                    while (numTargets > 0 && picker.PickRemove(out targetId))
                    {
                        if (targetId != prevTargetId)
                            hasLOS = null;
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
                    bool? hasLOS = null;
                    bool activated = false;

                    foreach (PrototypeId powerRef in hotspotProto.AppliesIntervalPowers)
                    {
                        PowerPrototype powerProto = powerRef.As<PowerPrototype>();
                        if (!Verify.IsNotNull(powerProto))
                            continue;

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

        private bool ActivateIntervalPowerForTarget(PrototypeId powerRef, ulong targetId, ref bool? hasLOS)
        {
            EntityManager entityManager = Game?.EntityManager;
            if (!Verify.IsNotNull(entityManager)) return false;

            WorldEntity target = entityManager.GetEntity<WorldEntity>(targetId);
            if (!Verify.IsNotNull(target)) return false;

            Power power = GetPower(powerRef);
            if (!Verify.IsNotNull(power)) return false;

            if (power.IsValidTarget(target) == false)
                return false;

            if (_checkLOS && power.RequiresLineOfSight())
            {
                hasLOS ??= target.LineOfSightTo(GetHotspotCenter());
                if (hasLOS == false)
                    return false;
            }

            PowerActivationSettings settings = new(target.Id, target.RegionLocation.Position, RegionLocation.Position);
            settings.Flags |= PowerActivationSettingsFlags.NotifyOwner;
            if (_skipCollide) settings.Flags |= PowerActivationSettingsFlags.SkipRangeCheck;

            PowerUseResult result = power.Activate(ref settings);
            if (Debug) Logger.Debug($"ActivateIntervalPower {power.PrototypeDataRef.GetNameFormatted()} from {PrototypeName} to {target.PrototypeName}");
            return result == PowerUseResult.Success;
        }

        private void ScheduleIntervalPowersEvent()
        {
            HotspotPrototype hotspotProto = HotspotPrototype;
            if (!Verify.IsNotNull(hotspotProto)) return;

            if (hotspotProto.AppliesIntervalPowers.IsNullOrEmpty())
                return;

            int intervalMS = hotspotProto.IntervalPowersTimeDelayMS;
            if (intervalMS <= 0)
                return;

            if (_intervalPowersEvent.IsValid)
                return;

            if (CanSchedulePowersEvent() == false)
                return;

            ScheduleEntityEvent(_intervalPowersEvent, TimeSpan.FromMilliseconds(intervalMS));
        }

        private void OnApplyActivePowers()
        {
            if (!Verify.IsNotNull(_overlapPowerTargets)) return;

            var manager = Game.EntityManager;

            // TODO: same ref based optimization as in OnPowerEnded()
            using var changedHandle = ListPool<(ulong, PowerTargetMap)>.Instance.Get(out List<(ulong, PowerTargetMap)> changed);

            foreach (var entry in _overlapPowerTargets)
            {
                ulong targetId = entry.Key;
                PowerTargetMap powerTarget = entry.Value;
                
                WorldEntity target = manager.GetEntity<WorldEntity>(targetId);
                if (!Verify.IsNotNull(target))
                    continue;
                
                ApplyActivePowers(target, ref powerTarget);
                changed.Add((targetId, powerTarget));
            }

            foreach (var kv in changed)
                _overlapPowerTargets[kv.Item1] = kv.Item2;

            ScheduleActivePowersEvent();
        }

        private void ApplyActivePowers(WorldEntity target, ref PowerTargetMap powerTarget)
        {
            HotspotPrototype hotspotProto = HotspotPrototype;
            if (!Verify.IsNotNull(hotspotProto)) return;
            if (!Verify.IsTrue(hotspotProto.AppliesPowers.HasValue())) return;

            if (_killSelf)
                return;

            if (hotspotProto.MaxSimultaneousTargets > 0 && _activePowerTargetCount >= hotspotProto.MaxSimultaneousTargets && powerTarget.ActivePowers.Empty)
                return;

            bool hasLOS = false;
            bool checkedLOS = false;
            bool activated = false;

            for (int i = 0; i < hotspotProto.AppliesPowers.Length; i++)
            {
                if (powerTarget.IgnorePowers[i])
                    continue;

                PowerPrototype powerProto = hotspotProto.AppliesPowers[i].As<PowerPrototype>();
                if (!Verify.IsNotNull(powerProto))
                    continue;

                // check conditions
                if (powerProto.AppliesConditions != null || powerProto.ConditionsByRef.HasValue())
                {
                    if (HasConditionsForTarget(powerProto, target, out bool hasOthers) == false)
                    {
                        ClearActiveTargetPowers(ref powerTarget, i);
                        if (Debug) Logger.Debug($"hasOthers[{hasOthers}] {powerProto.DataRef.GetNameFormatted()} {target.PrototypeName} {target.Id}");
                        if (hasOthers == false)
                            continue;
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
                    if (Debug) Logger.Debug($"ActivatePowerForTarget {powerProto.DataRef.GetNameFormatted()} {target.PrototypeName} {target.Id}");
                    activated |= ActivatePowerForTarget(powerProto.DataRef, target, ref powerTarget, i);
                }
                else if (powerTarget.ActivePowers[i] && isValidTarget == false)
                { 
                    EndPowerForActiveTarget(powerProto.DataRef, target.Id, ref powerTarget, i);
                }                
            }

            if (activated)
            {
                if (hotspotProto.MaxLifetimeTargets > 0)
                    _lifeTimeTargets++;
                OnPowerActivated();
            }
        }

        private void OnPowerActivated()
        {
            HotspotPrototype hotspotProto = HotspotPrototype;
            if (!Verify.IsNotNull(hotspotProto)) return;
            
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
            Power power = GetPower(powerRef);
            if (!Verify.IsNotNull(power)) return false;

            PowerActivationSettings settings = new(target.Id, target.RegionLocation.Position, RegionLocation.Position);
            settings.Flags |= PowerActivationSettingsFlags.NotifyOwner;
            if (_skipCollide) settings.Flags |= PowerActivationSettingsFlags.SkipRangeCheck;

            if (power.Activate(ref settings) != PowerUseResult.Success)
                return false;

            if (powerTarget.ActivePowers.Empty)
                _activePowerTargetCount++;
            powerTarget.ActivePowers.Set(index);

            return true;
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

            ConditionCollection conditionCollection = target.ConditionCollection;
            if (!Verify.IsNotNull(conditionCollection)) return false;

            if (powerProto.AppliesConditions != null)
            {
                foreach (var mixinPrototype in powerProto.AppliesConditions)
                {
                    ConditionPrototype conditionProto = mixinPrototype.Prototype as ConditionPrototype;
                    if (!Verify.IsNotNull(conditionProto))
                        continue;

                    if (CheckStackingBehavior(conditionProto, powerProto, conditionCollection, ref hasOthers, ref hasThis))
                    {
                        if (Debug) Logger.Warn($"AppliesConditions [{hasThis}] [{hasOthers}] for [{target.PrototypeName}] in {powerProto.DataRef.GetNameFormatted()}");
                        return hasThis;
                    }
                }
            }

            if (powerProto.ConditionsByRef.HasValue())
            {
                foreach (PrototypeId conditionRef in powerProto.ConditionsByRef)
                {
                    ConditionPrototype conditionProto = conditionRef.As<ConditionPrototype>();
                    if (!Verify.IsNotNull(conditionProto))
                        continue;

                    if (CheckStackingBehavior(conditionProto, powerProto, conditionCollection, ref hasOthers, ref hasThis))
                    {
                        if (Debug) Logger.Warn($"ConditionsByRef [{hasThis}] [{hasOthers}] for [{target.PrototypeName}] in {powerProto.DataRef.GetNameFormatted()}");
                        return hasThis;
                    }
                }
            }

            return hasThis;
        }

        private bool CheckStackingBehavior(ConditionPrototype conditionProto, PowerPrototype powerProto, 
            ConditionCollection conditionCollection, ref bool hasOthers, ref bool hasThis)
        {
            ConditionCollection.StackId stackId = ConditionCollection.MakeConditionStackId(powerProto, conditionProto, _powerUserId, _powerPlayerId, out StackingBehaviorPrototype stackingProto);
            if (!Verify.IsTrue(stackId.PrototypeRef != PrototypeId.Invalid)) return false;
            if (!Verify.IsNotNull(stackingProto)) return false;

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
            HotspotPrototype hotspotProto = HotspotPrototype;
            if (!Verify.IsNotNull(hotspotProto)) return;

            if (hotspotProto.AppliesPowers.IsNullOrEmpty())
                return;

            if (hotspotProto.MaxLifetimeTargets > 0 && _lifeTimeTargets >= hotspotProto.MaxLifetimeTargets)
                return;

            if (_activePowersEvent.IsValid)
                return;

            if (CanSchedulePowersEvent() == false)
                return;
                
            ScheduleEntityEvent(_activePowersEvent, TimeSpan.FromMilliseconds(_targetInvervalMS));
        }

        private void CancelPowerEvents()
        {
            EventScheduler scheduler = Game?.GameEventScheduler;
            if (!Verify.IsNotNull(scheduler)) return;

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

    public class MissionConditionContext    // TODO: change to struct
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
        public const int Size = 32;

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
