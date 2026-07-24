using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Behavior;
using MHServerEmu.Games.Behavior.ProceduralAI;
using MHServerEmu.Games.Behavior.StaticAI;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class ProceduralProfileWithTargetPrototype : ProceduralAIProfilePrototype
    {
        public SelectEntityContextPrototype SelectTarget { get; protected set; }
        public PrototypeId NoTargetOverrideProfile { get; protected set; }

        //---

        [Flags]
        protected enum SelectTargetFlags
        {
            None                = 0,
            NoTargetOverride    = 1 << 0,
            NotifyAllies        = 1 << 1,
        }

        public override void Init(Agent agent)
        {
            base.Init(agent);

            AIController ownerController = agent.AIController;
            if (!Verify.IsNotNull(ownerController)) return;

            if (ownerController.Senses.CanLeash)
            {
                AIGlobalsPrototype aiGlobalsPrototype = GameDatabase.AIGlobalsPrototype;
                InitPower(agent, aiGlobalsPrototype.LeashReturnHeal);
                InitPower(agent, aiGlobalsPrototype.LeashReturnImmunity);
            }
        }

        public bool DefaultSensory(ref WorldEntity target, AIController ownerController, ProceduralAI proceduralAI,
            SelectEntityContextPrototype selectTarget, CombatTargetType targetType, CombatTargetFlags flags = CombatTargetFlags.None)
        {
            BehaviorSensorySystem senses = ownerController.Senses;

            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return false;

            if (senses.ShouldSense())
            {
                senses.Sense();

                if (agent.IsDormant == false)
                {
                    if (target == null || target.IsAliveInWorld == false ||
                        (selectTarget.LockEntityOnceSelected == false && ownerController.ActivePowerRef == PrototypeId.Invalid))
                    {
                        SelectTargetEntity(agent, ref target, ownerController, proceduralAI, selectTarget, targetType,
                            SelectTargetFlags.NoTargetOverride | SelectTargetFlags.NotifyAllies, flags);
                    }
                    else
                    {
                        senses.ValidateCurrentTarget(targetType);
                    }
                }
                else
                {
                    return false;
                }
            }

            if (target == null || target.IsInWorld == false || agent.IsDormant)
                return false;

            return true;
        }

        protected bool SelectTargetEntity(Agent agent, ref WorldEntity target, AIController ownerController, ProceduralAI proceduralAI,
            SelectEntityContextPrototype selectTarget, CombatTargetType targetType, SelectTargetFlags targetFlags = SelectTargetFlags.None, 
            CombatTargetFlags flags = CombatTargetFlags.None)
        {
            WorldEntity currentTarget = target;
            SelectEntity.SelectEntityContext selectionContext = new(ownerController, selectTarget);
            WorldEntity selectedEntity = SelectEntity.DoSelectEntity(selectionContext, flags);
            if (selectedEntity == null)
            {
                if (currentTarget != null && Combat.ValidTarget(agent.Game, agent, currentTarget, targetType, true))
                    return true;

                target = null;
                ownerController.SetTargetEntity(null);
                if (targetFlags.HasFlag(SelectTargetFlags.NoTargetOverride))
                {
                    if (NoTargetOverrideProfile != PrototypeId.Invalid && ownerController.Blackboard.PropertyCollection[PropertyEnum.AIIgnoreNoTgtOverrideProfile] == false)
                    {
                        ProceduralAIProfilePrototype profile = NoTargetOverrideProfile.As<ProceduralAIProfilePrototype>();
                        proceduralAI.SetOverride(profile, OverrideType.Full);
                    }
                }

                return false;
            }
            else
            {
                if (selectedEntity == currentTarget)
                    return true;

                SelectEntity.RegisterSelectedEntity(ownerController, selectedEntity, selectionContext.SelectionType);
                target = selectedEntity;

                if (targetFlags.HasFlag(SelectTargetFlags.NotifyAllies))
                    ownerController.Senses.NotifyAlliesOnTargetAquired();
            }

            return false;
        }

        protected void DefaultMeleeMovement(ProceduralAI proceduralAI, AIController ownerController, Locomotor locomotor,
            WorldEntity target, MoveToContextPrototype moveToContextProto, OrbitContextPrototype orbitContextProto)
        {
            if (target == null)
                return;

            if (proceduralAI.GetState(0) != Orbit.Instance)
            {
                HandleMovementContext(proceduralAI, ownerController, locomotor, moveToContextProto, false, out StaticBehaviorReturnType movementResult);
                if (movementResult == StaticBehaviorReturnType.Running || movementResult == StaticBehaviorReturnType.Completed)
                    return;
            }

            HandleMovementContext(proceduralAI, ownerController, locomotor, orbitContextProto, false, out StaticBehaviorReturnType orbitResult);
            if (orbitResult == StaticBehaviorReturnType.Running) return;

            if (orbitResult == StaticBehaviorReturnType.Failed)
            {
                if (NoTargetOverrideProfile == PrototypeId.Invalid ||
                    ownerController.Blackboard.PropertyCollection[PropertyEnum.AIIgnoreNoTgtOverrideProfile] == true)
                {
                    return;
                }

                ProceduralProfileDefaultActiveOverridePrototype profile = NoTargetOverrideProfile.As<ProceduralProfileDefaultActiveOverridePrototype>();
                if (!Verify.IsNotNull(profile, $"default melee movement for [{this}] requires NoTargetOverrideProfile to be a ProceduralProfileDefaultActiveOverridePrototype"))
                    return;

                HandleMovementContext(proceduralAI, ownerController, locomotor, profile.Wander, false, out _);
            }
        }

        protected static void DefaultRangedMovement(ProceduralAI proceduralAI, AIController ownerController, Agent agent, WorldEntity target,
            MoveToContextPrototype moveToContextProto, OrbitContextPrototype orbitContextProto)
        {
            if (!Verify.IsNotNull(moveToContextProto)) return;
            if (!Verify.IsNotNull(orbitContextProto)) return;

            if (target == null)
                return;

            IAIState state = proceduralAI.GetState(0);
            bool toMove = state == Orbit.Instance || state == MoveTo.Instance;
            if (toMove == false)
            {
                toMove = IsPastMaxDistanceOrLostLOS(agent, target, moveToContextProto.RangeMax, moveToContextProto.EnforceLOS,
                    (float)ownerController.Blackboard.PropertyCollection[PropertyEnum.AILOSMaxPowerRadius], moveToContextProto.LOSSweepPadding);
            }

            if (toMove)
            {
                if (proceduralAI.GetState(0) != Orbit.Instance)
                {
                    HandleMovementContext(proceduralAI, ownerController, agent.Locomotor, moveToContextProto, true, out StaticBehaviorReturnType moveToResult);
                    if (moveToResult == StaticBehaviorReturnType.Running || moveToResult == StaticBehaviorReturnType.Completed)
                        return;
                }

                HandleMovementContext(proceduralAI, ownerController, agent.Locomotor, orbitContextProto, true, out StaticBehaviorReturnType orbitResult);
                if (orbitResult == StaticBehaviorReturnType.Running || orbitResult == StaticBehaviorReturnType.Completed)
                    return;
            }

            HandleRotateToTarget(agent, target);
        }

        protected static void DefaultRangedFlankerMovement(ProceduralAI proceduralAI, AIController ownerController, Agent agent, WorldEntity target,
            long currentTime, MoveToContextPrototype moveToContextProto, ProceduralFlankContextPrototype flankContextProto)
        {
            if (target == null)
                return;

            IAIState state = proceduralAI.GetState(0);
            bool toMove = state == Orbit.Instance;
            if (toMove == false && state != Flank.Instance)
            {
                toMove = IsPastMaxDistanceOrLostLOS(agent, target, moveToContextProto.RangeMax, moveToContextProto.EnforceLOS,
                    (float)ownerController.Blackboard.PropertyCollection[PropertyEnum.AILOSMaxPowerRadius], moveToContextProto.LOSSweepPadding);
            }

            if (toMove)
            {
                HandleMovementContext(proceduralAI, ownerController, agent.Locomotor, moveToContextProto, true, out var moveToResult);
                if (moveToResult == StaticBehaviorReturnType.Running)
                    return;
            }

            if (HandleProceduralFlank(proceduralAI, ownerController, agent.Locomotor, currentTime, flankContextProto, true) == StaticBehaviorReturnType.Running)
                return;

            HandleRotateToTarget(agent, target);
        }

        protected static StaticBehaviorReturnType HandleProceduralFlank(ProceduralAI proceduralAI, AIController ownerController, Locomotor locomotor,
            long currentTime, ProceduralFlankContextPrototype proceduralFlankContext, bool checkPower)
        {
            if (!Verify.IsNotNull(proceduralFlankContext, $"AI profile trying to flank without a flank context!\nEntity: {ownerController.Owner}"))
                return StaticBehaviorReturnType.None;

            StaticBehaviorReturnType contextResult = StaticBehaviorReturnType.None;
            long flankTime = ownerController.Blackboard.PropertyCollection[PropertyEnum.AIProceduralNextFlankTime];
            if (proceduralAI.GetState(0) == Flank.Instance || currentTime > flankTime)
                HandleMovementContext(proceduralAI, ownerController, locomotor, proceduralFlankContext.FlankContext, checkPower, out contextResult, proceduralFlankContext);

            return contextResult;
        }

        protected static StaticBehaviorReturnType HandleProceduralFlee(ProceduralAI proceduralAI, AIController ownerController, 
            long currentTime, ProceduralFleeContextPrototype proceduralFleeContext)
        {
            if (!Verify.IsNotNull(proceduralFleeContext)) return StaticBehaviorReturnType.None;

            StaticBehaviorReturnType contextResult = StaticBehaviorReturnType.None;
            long fleeTime = ownerController.Blackboard.PropertyCollection[PropertyEnum.AIProceduralNextFleeTime];
            if (proceduralAI.GetState(0) == Flee.Instance || currentTime > fleeTime)
                contextResult = HandleContext(proceduralAI, ownerController, proceduralFleeContext.FleeContext, proceduralFleeContext);

            return contextResult;
        }

        protected static void HandleRotateToTarget(Agent agent, WorldEntity target)
        {
            if (agent.CanRotate() && target != null && target.IsInWorld)
            {
                Locomotor locomotor = agent.Locomotor;
                if (!Verify.IsNotNull(locomotor, $"Agent [{agent}] does not have a locomotor and should not be calling this function"))
                    return;

                locomotor.LookAt(target.RegionLocation.Position);
            }
        }

        protected static bool IsPastMaxDistanceOrLostLOS(Agent agent, WorldEntity target, float rangeMax, bool enforceLOS, float radius, float padding)
        {
            if (target == null || target.IsInWorld == false)
                return false;

            float boundsRadius = agent.Bounds.Radius + target.Bounds.Radius;
            float distanceSq = Vector3.DistanceSquared2D(agent.RegionLocation.Position, target.RegionLocation.Position);

            if (distanceSq > MathHelper.Square(boundsRadius + rangeMax))
                return true;

            if (enforceLOS && agent.LineOfSightTo(target, radius, padding) == false)
                return true;
            
            return false;
        }

        protected bool CommonSimplifiedSensory(ref WorldEntity target, AIController ownerController, ProceduralAI proceduralAI, 
            SelectEntityContextPrototype selectTargetProto, CombatTargetType targetType)
        {
            BehaviorSensorySystem senses = ownerController.Senses;

            if (senses.ShouldSense())
            {
                if (!Verify.IsNotNull(selectTargetProto)) return false;

                Agent agent = ownerController.Owner;
                if (!Verify.IsNotNull(agent)) return false;

                if (target == null || target.IsAliveInWorld == false || selectTargetProto.LockEntityOnceSelected == false)
                {
                    if (SelectTargetEntity(agent, ref target, ownerController, proceduralAI, selectTargetProto, targetType))
                        return true;
                }
                else
                {
                    senses.ValidateCurrentTarget(targetType);
                }
            }

            if (target == null || target.IsInWorld == false)
                return false;

            return true;
        }

        public virtual void OnInteractEnded(AIController ownerController, ProceduralInteractContextPrototype proceduralInteractContext) { }
    }

    public class ProceduralProfileFearOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public FleeContextPrototype FleeFromTarget { get; protected set; }
        public WanderContextPrototype WanderIfNoTarget { get; protected set; }

        //---

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;

            WorldEntity target = ownerController.TargetEntity;
            if (target == null || target.IsInWorld == false)
            {
                SelectEntity.SelectEntityContext selectionContext = new(ownerController, SelectTarget);
                WorldEntity selectedEntity = SelectEntity.DoSelectEntity(selectionContext);
                if (selectedEntity != null && selectedEntity.IsInWorld)
                {
                    SelectEntity.RegisterSelectedEntity(ownerController, selectedEntity, selectionContext.SelectionType);
                    target = selectedEntity;
                }
            }

            if (target != null && target.IsInWorld)
                HandleContext(proceduralAI, ownerController, FleeFromTarget);
            else
                HandleContext(proceduralAI, ownerController, WanderIfNoTarget);
        }
    }

    public class ProceduralProfileRunToTargetAndDespawnOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public PrototypeId Invulnerability { get; protected set; }
        public int NumberOfWandersBeforeDestroy { get; protected set; }
        public MoveToContextPrototype RunToTarget { get; protected set; }
        public WanderContextPrototype WanderIfMoveFails { get; protected set; }

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);
            
            InitPower(agent, Invulnerability);
        }

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                proceduralAI.PartialOverrideBehavior == null)
            {
                return;
            }

            if (proceduralAI.GetState(0) != MoveTo.Instance && proceduralAI.GetState(0) != Wander.Instance)
            {
                if (!Verify.IsTrue(ownerController.AttemptActivatePower(Invulnerability, agent.Id, agent.RegionLocation.Position)))
                    return;
            }

            StaticBehaviorReturnType contextResult = StaticBehaviorReturnType.None;
            if (proceduralAI.GetState(0) != Wander.Instance)
            {
                contextResult = HandleContext(proceduralAI, ownerController, RunToTarget);
                if (contextResult == StaticBehaviorReturnType.Running) return;
            }

            if (contextResult == StaticBehaviorReturnType.Failed || proceduralAI.GetState(0) == Wander.Instance)
            {
                contextResult = HandleContext(proceduralAI, ownerController, WanderIfMoveFails);
                if (contextResult == StaticBehaviorReturnType.Running) return;
                else if (contextResult == StaticBehaviorReturnType.Completed || contextResult == StaticBehaviorReturnType.Failed)
                {
                    BehaviorBlackboard blackboard = ownerController.Blackboard;
                    int runToExitWanderCount = blackboard.PropertyCollection[PropertyEnum.AIRunToExitWanderCount];
                    if (runToExitWanderCount < NumberOfWandersBeforeDestroy)
                    {
                        blackboard.PropertyCollection[PropertyEnum.AIRunToExitWanderCount] = runToExitWanderCount + 1;
                        return;
                    }
                }
            }

            if (!Verify.IsTrue(ownerController.AttemptActivatePower(Invulnerability, agent.Id, agent.RegionLocation.Position)))
                return;
            
            agent.Destroy();
        }
    }

    public class ProceduralProfileDefaultActiveOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public DelayContextPrototype DelayAfterWander { get; protected set; }
        public WanderContextPrototype Wander { get; protected set; }
        public WanderContextPrototype WanderInPlace { get; protected set; }

        //---

        private enum State
        {
            WanderInPlace,
            Delay,
            Wander,
        }

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            BehaviorSensorySystem senses = ownerController.Senses;

            if (senses.ShouldSense())
            {
                senses.Sense();

                ProceduralAIProfilePrototype baseProfile = proceduralAI.Behavior;
                if (!Verify.IsNotNull(baseProfile)) return;

                ProceduralProfileWithTargetPrototype targetProfile = baseProfile as ProceduralProfileWithTargetPrototype;
                if (!Verify.IsNotNull(targetProfile, $"Agent {ownerController.Owner} has {baseProfile} which contains an invalid select target. Make sure {baseProfile} derives from ProceduralProfileWithTargetPrototype"))
                    return;

                SelectEntity.SelectEntityContext selectionContext = new(ownerController, targetProfile.SelectTarget);
                WorldEntity selectedEntity = SelectEntity.DoSelectEntity(selectionContext);
                if (selectedEntity != null && proceduralAI.GetState(0) != UsePower.Instance)
                {
                    blackboard.PropertyCollection[PropertyEnum.AIDefaultActiveOverrideStateVal] = (int)State.WanderInPlace;
                    SelectEntity.RegisterSelectedEntity(ownerController, selectedEntity, selectionContext.SelectionType);
                    senses.NotifyAlliesOnTargetAquired();
                    proceduralAI.ClearOverrideBehavior(OverrideType.Full);
                    return;
                }
            }

            StaticBehaviorReturnType contextResult;
            int stateVal = blackboard.PropertyCollection[PropertyEnum.AIDefaultActiveOverrideStateVal];
            switch ((State)stateVal)
            {
                case State.WanderInPlace:
                    contextResult = HandleContext(proceduralAI, ownerController, WanderInPlace);
                    if (contextResult == StaticBehaviorReturnType.Completed)
                        blackboard.PropertyCollection[PropertyEnum.AIDefaultActiveOverrideStateVal] = (int)State.Delay;
                    break;

                case State.Delay:
                    contextResult = HandleContext(proceduralAI, ownerController, DelayAfterWander);
                    if (contextResult == StaticBehaviorReturnType.Completed)
                        blackboard.PropertyCollection[PropertyEnum.AIDefaultActiveOverrideStateVal] = (int)State.Wander;
                    break;

                case State.Wander:
                default:
                    contextResult = HandleContext(proceduralAI, ownerController, Wander);
                    if (contextResult == StaticBehaviorReturnType.Completed)
                        blackboard.PropertyCollection[PropertyEnum.AIDefaultActiveOverrideStateVal] = (int)State.Delay;
                    break;
            }
        }

    }

    public class ProceduralProfileFleeOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public FleeContextPrototype FleeFromTarget { get; protected set; }

        //---

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;

            if (HandleContext(proceduralAI, ownerController, FleeFromTarget) == StaticBehaviorReturnType.Running) return;
            proceduralAI.ClearOverrideBehavior(OverrideType.Full);
        }
    }

    public class ProceduralProfileOrbPrototype : ProceduralProfileWithTargetPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public int InitialMoveToDelayMS { get; protected set; }
        public StateChangePrototype InvalidTargetState { get; protected set; }
        public float OrbRadius { get; protected set; }
        public PrototypeId EffectPower { get; protected set; }
        public bool AcceptsAggroRangeBonus { get; protected set; }
        public int ShrinkageDelayMS { get; protected set; }
        public int ShrinkageDurationMS { get; protected set; }
        public float ShrinkageMinScale { get; protected set; }
        public bool DestroyOrbOnUnSimOrTargetLoss { get; protected set; }

        //---

        private enum ValidateTargetResult
        {
            Success,
            GenericFailure,
            PowerFailure,
        }

        private float _orbRadiusSquared;

        public override void PostProcess()
        {
            base.PostProcess();

            _orbRadiusSquared = OrbRadius * OrbRadius;
        }

        public override void Init(Agent agent)
        {
            base.Init(agent);

            if (!Verify.IsNotNull(agent)) return;

            Game game = agent.Game;
            if (!Verify.IsNotNull(game)) return;

            AIController ownerController = agent.AIController;
            if (!Verify.IsNotNull(ownerController)) return;

            // Delay AI activation to let the drop animation finish before an avatar can pick up this orb
            // NOTE: For some reason AIStartsEnabled is not set to false for some orb prototypes, so we force set it here.
            ownerController.Blackboard.PropertyCollection[PropertyEnum.AIStartsEnabled] = false;
            EventPointer<AIController.EnableAIEvent> enableEvent = new();
            ownerController.ScheduleAIEvent(enableEvent, TimeSpan.FromMilliseconds(InitialMoveToDelayMS));

            agent.Properties[PropertyEnum.AICustomTimeVal1] = game.CurrentTime;

            if (EffectPower != PrototypeId.Invalid)
                InitPower(agent, EffectPower);
        }

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            Game game = agent.Game;
            if (!Verify.IsNotNull(game)) return;

            // Destroy this orb if it has finished shrinking
            if (ShrinkageDurationMS > 0)
            {
                TimeSpan shrinkageDurationRemaining = GetShrinkageDurationRemaining(agent);
                if (shrinkageDurationRemaining <= TimeSpan.Zero)
                {
                    agent.Kill(null, KillFlags.NoDeadEvent | KillFlags.NoExp | KillFlags.NoLoot);
                    return;
                }
            }

            // Find an avatar that can potentially pick this orb up
            Avatar avatar = null;

            ulong restrictedToPlayerGuid = agent.Properties[PropertyEnum.RestrictedToPlayerGuid];
            if (restrictedToPlayerGuid != 0)
            {
                // Get the current avatar for the player we are looking for
                Player player = game.EntityManager.GetEntityByDbGuid<Player>(restrictedToPlayerGuid);
                if (player != null)
                {
                    if (player.CurrentAvatar?.IsInWorld == true)
                        avatar = player.CurrentAvatar;
                }

                if (avatar == null)
                {
                    if (ShouldDestroyOrbOnUnSimOrTargetLoss(agent))
                        agent.Destroy();

                    return;
                }
            }
            else
            {
                // Find the nearest avatar belonging to any player
                avatar = FindNearestAvatar(agent);
            }

            // If we found an avatar, check if it can pick this orb up
            if (avatar != null)
            {
                Vector3 agentPosition = agent.RegionLocation.Position;
                Vector3 avatarPosition = avatar.RegionLocation.Position;

                if (Vector3.DistanceSquared2D(agentPosition, avatarPosition) < _orbRadiusSquared && TryGetPickedUp(agent, avatar))
                    return;
            }

            // Follow our avatar if needed
            if (MoveToTarget != null)
            {
                // NOTE: Health and endurance orbs follow players, credits and experience orbs do not

                BehaviorSensorySystem senses = ownerController.Senses;
                WorldEntity currentMoveTarget = ownerController.TargetEntity;

                if (senses.ShouldSense())
                {
                    switch (ValidateTarget(agent, avatar, true))
                    {
                        case ValidateTargetResult.Success:
                            agent.SetState(PrototypeId.Invalid);
                            if (currentMoveTarget != avatar)
                            {
                                ownerController.SetTargetEntity(avatar);
                                currentMoveTarget = avatar;
                            }
                            break;

                        case ValidateTargetResult.GenericFailure:
                            agent.SetState(PrototypeId.Invalid);
                            ownerController.ResetCurrentTargetState();
                            currentMoveTarget = null;
                            break;

                        case ValidateTargetResult.PowerFailure:
                            agent.ApplyStateFromPrototype(InvalidTargetState);  // Play pickup failure animation
                            ownerController.ResetCurrentTargetState();
                            currentMoveTarget = null;
                            break;
                    }
                }

                if (currentMoveTarget != null)
                    HandleMovementContext(proceduralAI, ownerController, agent.Locomotor, MoveToTarget, false, out _);
            }
        }

        public override void OnSetSimulated(AIController ownerController, bool simulated)
        {
            if (simulated)
                return;

            Agent agent = ownerController.Owner;
            if (agent == null) return;

            if (ShouldDestroyOrbOnUnSimOrTargetLoss(agent))
                agent.ScheduleDestroyEvent(TimeSpan.Zero);
        }

        private bool TryGetPickedUp(Agent agent, Avatar avatar)
        {
            OrbPrototype orbProto = agent.Prototype as OrbPrototype;
            if (!Verify.IsNotNull(orbProto)) return false;

            if (ValidateTarget(agent, avatar, false) != ValidateTargetResult.Success)
                return false;

            Player player = avatar.GetOwnerOfType<Player>();
            if (!Verify.IsNotNull(player)) return false;

            // Reduce the orb's effect based on its shrinkage progress if needed. XP rewards are reduced by lowering the orb's level.
            DoOrbShrink(agent);
            int levelDelta = agent.CharacterLevel - agent.Properties[PropertyEnum.InitialCharacterLevel];

            // Power (healing, endurance, boons)
            if (EffectPower != PrototypeId.Invalid)
                agent.AIController.AttemptActivatePower(EffectPower, avatar.Id, avatar.RegionLocation.Position);

            // Run OnOrbPickup procs
            KeywordPrototype orbEntityKeywordProto = GameDatabase.KeywordGlobalsPrototype.OrbEntityKeyword;
            if (orbProto.HasKeyword(orbEntityKeywordProto))
                avatar.TryActivateOnOrbPickupProcs(agent);

            // Experience
            // Scale exp based on avatar level rather than orb level, but apply the delta from orb shrinkage.
            int expLevel = Math.Max(avatar.CharacterLevel + levelDelta, 1);
            if (orbProto.GetXPAwarded(expLevel, out long xp, out long minXP, player.CanUseLiveTuneBonuses()))
            {
                TuningTable tuningTable = orbProto.IgnoreRegionDifficultyForXPCalc == false ? agent.Region?.TuningTable : null;
                xp = avatar.ApplyXPModifiers(xp, false, tuningTable);

                // Set xp to 1 if this is not the avatar this was intended for
                if (orbProto.XPAwardRestrictedToAvatar)
                {
                    ulong requiredDbGuid = agent.Properties[PropertyEnum.XPAwardRequiredDbGuid, avatar.PrototypeDataRef];
                    if (avatar.OwnerPlayerDbId != requiredDbGuid)
                    {
                        xp = 1;
                        minXP = 1;
                    }
                }

                avatar.AwardXP(xp, minXP, agent.Properties[PropertyEnum.ShowXPRewardText]);
            }

            // Alternate advancement experience
            if (avatar.Game.InfinitySystemEnabled)
            {
                long infinityXP = agent.Properties[PropertyEnum.InfinityXP];
                if (infinityXP > 0)
                    player.AwardInfinityXP(infinityXP, true);
            }
            else
            {
                long omegaXP = agent.Properties[PropertyEnum.OmegaXP];
                if (omegaXP > 0)
                    player.AwardOmegaXP(omegaXP, true);
            }

            // Credits / currency
            if (player.AcquireCurrencyItem(agent))
            {
                avatar.TryActivateOnLootPickupProcs(agent);

                if (agent.Properties.HasProperty(PropertyEnum.RunestonesAmount))
                    avatar.TryActivateOnRunestonePickupProcs();
            }

            // Invoke OrbPickUp event
            agent.Region?.OrbPickUpEvent.Invoke(new(player, agent));            

            // "Kill" this orb to play its pickup (death) animation
            agent.Kill(avatar, KillFlags.NoDeadEvent | KillFlags.NoExp | KillFlags.NoLoot);
            return true;
        }

        private ValidateTargetResult ValidateTarget(Agent agent, Avatar target, bool checkRange)
        {
            if (agent == null) return ValidateTargetResult.GenericFailure;
            if (target == null) return ValidateTargetResult.GenericFailure;

            // If this is an instanced orb, make sure the target belong to our player
            ulong restrictedToPlayerGuid = agent.Properties[PropertyEnum.RestrictedToPlayerGuid];
            if (restrictedToPlayerGuid != 0)
            {
                Player player = target.GetOwnerOfType<Player>();
                if (!Verify.IsNotNull(player)) return ValidateTargetResult.GenericFailure;

                if (player.DatabaseUniqueId != restrictedToPlayerGuid)
                    return ValidateTargetResult.GenericFailure;
            }

            // Make sure this orb is in the same region as the target
            if (agent.Region != target.Region)
                return ValidateTargetResult.GenericFailure;

            // Check aggro range for moving orbs
            if (MoveToTarget != null && checkRange)
            {
                float aggroRangeBase = agent.AIController.AggroRangeAlly;
                float aggroRange = aggroRangeBase;

                if (AcceptsAggroRangeBonus)
                {
                    aggroRange += aggroRangeBase * Avatar.GetOrbAggroRangeBonusPct(target.Properties);
                    aggroRange = MathF.Min(aggroRange, GameDatabase.AIGlobalsPrototype.OrbAggroRangeMax);
                }

                Vector3 agentPosition = agent.RegionLocation.Position;
                Vector3 targetPosition = target.RegionLocation.Position;

                if (Vector3.DistanceSquared2D(agentPosition, targetPosition) > MathHelper.Square(aggroRange))
                    return ValidateTargetResult.GenericFailure;
            }

            // Do not allow this orb to be picked up if the avatar is not a valid for its target
            // (e.g. trying to pick up a healing orb with full health).
            if (EffectPower != PrototypeId.Invalid)
            {
                Power power = agent.GetPower(EffectPower);
                if (!Verify.IsNotNull(power)) return ValidateTargetResult.GenericFailure;

                if (power.IsValidTarget(target) == false)
                    return ValidateTargetResult.PowerFailure;
            }

            return ValidateTargetResult.Success;
        }

        private bool ShouldDestroyOrbOnUnSimOrTargetLoss(Agent agent)
        {
            PropertyCollection properties = agent.Properties;

            // Do not destroy experience orbs
            if (agent.GetXPAwarded(out _, out _, false))
                return false;

            if (properties.HasProperty(PropertyEnum.OmegaXP) || properties.HasProperty(PropertyEnum.InfinityXP))
                return false;

            // Do not destroy currency
            if (properties.HasProperty(PropertyEnum.ItemCurrency) || properties.HasProperty(PropertyEnum.RunestonesAmount))
                return false;

            // We can add more filters here if needed

            return DestroyOrbOnUnSimOrTargetLoss;
        }

        private void DoOrbShrink(Agent agent)
        {
            // No shrinkage duration indicates that the orb does not shrink.
            if (ShrinkageDurationMS == 0)
                return;

            float shrinkageDurationRemainingMS = (float)GetShrinkageDurationRemaining(agent).TotalMilliseconds;

            // ShrinkageDurationRemaining can be higher than ShrinkageDuration because of ShrinkageDelay.
            if (shrinkageDurationRemainingMS >= ShrinkageDurationMS)
                return;

            float shrinkRatio = shrinkageDurationRemainingMS / ShrinkageDurationMS;

            // Update level
            int level = agent.Properties[PropertyEnum.InitialCharacterLevel];
            level = Math.Max((int)(level * shrinkRatio), 1);

            agent.CharacterLevel = level;
            agent.CombatLevel = level;

            // Update proc ranks
            const int MaxProcRank = 100;
            int procRank = (int)(MaxProcRank * shrinkRatio);

            using var procPowerRefsHandle = ListPool<PrototypeId>.Instance.Get(out List<PrototypeId> procPowerRefs);
            foreach (var kvp in agent.Properties.IteratePropertyRange(PropertyEnum.Proc))
            {
                Property.FromParam(kvp.Key, 1, out PrototypeId procPowerRef);
                if (!Verify.IsTrue(procPowerRef != PrototypeId.Invalid))
                    continue;

                procPowerRefs.Add(procPowerRef);
            }

            foreach (PrototypeId procPowerRef in procPowerRefs)
                agent.Properties[PropertyEnum.ProcPowerRank, procPowerRef] = procRank;
        }

        private TimeSpan GetShrinkageDurationRemaining(Agent agent)
        {
            return agent.Properties[PropertyEnum.AICustomTimeVal1]
                + TimeSpan.FromMilliseconds(ShrinkageDelayMS)
                + TimeSpan.FromMilliseconds(ShrinkageDurationMS)
                - agent.Game.CurrentTime;
        }

        private static Avatar FindNearestAvatar(Agent agent)
        {
            if (!Verify.IsTrue(agent.IsInWorld)) return null;

            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return null;

            Vector3 agentPosition = agent.RegionLocation.Position;
            float maxAggroRange = GameDatabase.AIGlobalsPrototype.OrbAggroRangeMax;

            float minDistance = float.MaxValue;
            Avatar target = null;
            foreach (Avatar avatar in region.IterateAvatarsInVolume(new(agentPosition, maxAggroRange)))
            {
                if (avatar?.IsInWorld != true)
                    continue;

                float distance = Vector3.DistanceSquared2D(agentPosition, avatar.RegionLocation.Position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    target = avatar;
                }
            }

            return target;
        }
    }

    public class ProceduralProfileFastballSpecialWolverinePrototype : ProceduralProfileWithTargetPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public WanderContextPrototype MoveToNoTarget { get; protected set; }
        public UsePowerContextPrototype Power { get; protected set; }
        public int PowerChangeTargetIntervalMS { get; protected set; }

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, Power);
        }

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            Game game = agent.Game;
            if (!Verify.IsNotNull(game)) return;

            long currentTime = (long)game.CurrentTime.TotalMilliseconds;

            if (!Verify.IsNotNull(Power)) return;
            if (!Verify.IsNotNull(Power.Power)) return;

            PropertyCollection properties = ownerController.Blackboard.PropertyCollection;
            WorldEntity target = ownerController.TargetEntity;
            CommonSimplifiedSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile);

            StaticBehaviorReturnType contextResult = HandleContext(proceduralAI, ownerController, Power, null);
            if (contextResult == StaticBehaviorReturnType.Running)
            {
                int changeTargetCount = properties[PropertyEnum.AICustomStateVal1];
                if (changeTargetCount == 0)
                {
                    properties[PropertyEnum.AICustomStateVal1] = 1;
                }
                else
                {
                    long powerStartTime = agent.Properties[PropertyEnum.PowerCooldownStartTime, Power.Power.DataRef];
                    if (currentTime > (powerStartTime + PowerChangeTargetIntervalMS * changeTargetCount))
                    {
                        SelectEntity.SelectEntityContext selectionContext = new(ownerController, SelectTarget);
                        target = SelectEntity.DoSelectEntity(selectionContext);
                        if (target != null)
                        {
                            if (!Verify.IsTrue(SelectEntity.RegisterSelectedEntity(ownerController, target, selectionContext.SelectionType)))
                                return;

                            properties[PropertyEnum.AICustomStateVal1] = changeTargetCount + 1;
                        }
                    }
                }

                proceduralAI.PushSubstate();
                if (target != null)
                    HandleContext(proceduralAI, ownerController, MoveToTarget);
                else
                    HandleContext(proceduralAI, ownerController, MoveToNoTarget);
                proceduralAI.PopSubstate();
            }
            else if (contextResult == StaticBehaviorReturnType.Completed)
            {
                properties[PropertyEnum.AICustomStateVal1] = 0;
            }
        }
    }

    public class ProceduralProfileSeekingMissilePrototype : ProceduralProfileWithTargetPrototype
    {
        public SelectEntityContextPrototype SecondaryTargetSelection { get; protected set; }
        public int SeekDelayMS { get; protected set; }
        public float SeekDelaySpeed { get; protected set; }

        //---

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            Game game = agent.Game;
            if (!Verify.IsNotNull(game)) return;

            long currentTime = (long)game.CurrentTime.TotalMilliseconds;

            Locomotor locomotor = agent.Locomotor;
            if (!Verify.IsNotNull(locomotor)) return;

            PropertyCollection properties = ownerController.Blackboard.PropertyCollection;
            int stateVal = properties[PropertyEnum.AICustomStateVal1];
            if (stateVal != 1 && SeekDelayMS > 0)
            {
                long seekDelayTime = properties[PropertyEnum.AICustomTimeVal1];
                if (seekDelayTime == 0)
                {
                    properties[PropertyEnum.AICustomTimeVal1] = currentTime;
                    return;
                }

                if (currentTime - seekDelayTime < SeekDelayMS)
                    return;

                properties[PropertyEnum.AICustomStateVal1] = 1;
                locomotor.SetMethod(LocomotorMethod.Default);
            }

            WorldEntity target = ownerController.TargetEntity;
            if (CommonSimplifiedSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false) 
            { 
                if (SecondaryTargetSelection != null)
                {
                    if (SelectTargetEntity(agent, ref target, ownerController, proceduralAI, SecondaryTargetSelection, CombatTargetType.Hostile) == false)
                        return;
                }
                else
                {
                    return;
                }
            }

            ulong targetId = target != null ? target.Id : 0;
            if (locomotor.FollowEntityId != targetId)
            {
                locomotor.FollowEntity(targetId, 0.0f);
                locomotor.FollowEntityMissingEvent.AddActionBack(ownerController.MissileReturnAction);
            }
        }

        public override void OnMissileReturnEvent(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

            ownerController.Blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AINextSensoryUpdate);

            WorldEntity target = ownerController.TargetEntity;
            if (CommonSimplifiedSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false)
            {
                if (SecondaryTargetSelection != null)
                {
                    if (SelectTargetEntity(agent, ref target, ownerController, proceduralAI, SecondaryTargetSelection, CombatTargetType.Hostile) == false)
                        return;
                }
                else
                {
                    return;
                }
            }

            Locomotor locomotor = agent.Locomotor;
            if (!Verify.IsNotNull(locomotor)) return;

            if (target != null)
            {
                locomotor.FollowEntity(target.Id, 0.0f);
                locomotor.FollowEntityMissingEvent.AddActionFront(ownerController.MissileReturnAction);
            }
        }
    }

    public class ProceduralProfileSeekingMissileUniqueTargetPrototype : ProceduralProfileWithTargetPrototype
    {
        //---

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            Game game = agent.Game;
            if (!Verify.IsNotNull(game)) return;

            WorldEntity target = ownerController.TargetEntity;
            if (CommonSimplifiedSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false) return;

            Locomotor locomotor = agent.Locomotor;
            if (!Verify.IsNotNull(locomotor)) return;

            if (target != null)
            {
                ulong targetId = target.Id;
                if (locomotor.FollowEntityId != targetId)
                {
                    locomotor.FollowEntity(targetId, 0.0f);
                    target.Properties[PropertyEnum.FocusTargetedOnByID] = agent.Id;
                    ownerController.Blackboard.PropertyCollection[PropertyEnum.AIFocusTargetingID] = targetId;
                }
            }
        }

        public override void OnOwnerKilled(AIController ownerController)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

            ulong focusTargetId = ownerController.Blackboard.PropertyCollection[PropertyEnum.AIFocusTargetingID];
            WorldEntity focusTarget = agent.Game.EntityManager.GetEntity<WorldEntity>(focusTargetId);
            focusTarget?.Properties.RemoveProperty(PropertyEnum.FocusTargetedOnByID);
        }

        public override void OnOwnerTargetSwitch(AIController ownerController, ulong oldTarget, ulong newTarget)
        {
            OnOwnerKilled(ownerController); // same code
        }
    }

    public class ProceduralProfileMoveToUniqueTargetNoPowerPrototype : ProceduralProfileWithTargetPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }

        //---

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            Game game = agent.Game;
            if (!Verify.IsNotNull(game)) return;

            WorldEntity target = ownerController.TargetEntity;
            if (CommonSimplifiedSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false) return;
            HandleContext(proceduralAI, ownerController, MoveToTarget);
        }

        public override void OnOwnerTargetSwitch(AIController ownerController, ulong oldTarget, ulong newTarget)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

            WorldEntity target = agent.Game.EntityManager.GetEntity<WorldEntity>(newTarget);
            if (target != null && oldTarget != 0)
            {
                target.Properties[PropertyEnum.FocusTargetedOnByID] = agent.Id;
                ownerController.Blackboard.PropertyCollection[PropertyEnum.AIFocusTargetingID] = newTarget;
            }

            if (oldTarget != 0)
            {
                WorldEntity focusTarget = agent.Game.EntityManager.GetEntity<WorldEntity>(oldTarget);
                focusTarget?.Properties.RemoveProperty(PropertyEnum.FocusTargetedOnByID);
            }
        }

        public override void OnOwnerExitWorld(AIController ownerController)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

            ulong focusTargetId = ownerController.Blackboard.PropertyCollection[PropertyEnum.AIFocusTargetingID];
            WorldEntity focusTarget = agent.Game.EntityManager.GetEntity<WorldEntity>(focusTargetId);
            focusTarget?.Properties.RemoveProperty(PropertyEnum.FocusTargetedOnByID);
        }
    }

    public class ProceduralProfileVanityPetPrototype : ProceduralProfileWithTargetPrototype
    {
        public MoveToContextPrototype PetFollow { get; protected set; }
        public TeleportContextPrototype TeleportToMasterIfTooFarAway { get; protected set; }
        public int MinTimerWhileNotMovingFidgetMS { get; protected set; }
        public int MaxTimerWhileNotMovingFidgetMS { get; protected set; }
        public float MaxDistToMasterBeforeTeleport { get; protected set; }

        //---

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            Game game = agent.Game;
            if (!Verify.IsNotNull(game)) return;

            if (agent.IsDormant)
                return;

            WorldEntity master = ownerController.AssistedEntity;
            if (master != null && master.IsInWorld)
            {                
                float distanceToMasterSq = Vector3.DistanceSquared2D(agent.RegionLocation.Position, master.RegionLocation.Position);
                if (distanceToMasterSq > MaxDistToMasterBeforeTeleport * MaxDistToMasterBeforeTeleport)
                {
                    HandleContext(proceduralAI, ownerController, TeleportToMasterIfTooFarAway);
                }
            }

            HandleMovementContext(proceduralAI, ownerController, agent.Locomotor, PetFollow, false, out _);
        }
    }

    public class ProceduralProfileControlledMobOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public MoveToContextPrototype ControlFollow { get; protected set; }
        public TeleportContextPrototype TeleportToMasterIfTooFarAway { get; protected set; }
        public float MaxDistToMasterBeforeTeleport { get; protected set; }
        public int MaxDistToMasterBeforeFollow { get; protected set; }

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            if (!Verify.IsNotNull(agent)) return;

            AIController ownerController = agent.AIController;
            if (!Verify.IsNotNull(ownerController)) return;

            // Disable leashing and clear full override behavior
            ownerController.Senses.CanLeash = false;
            ownerController.Brain?.ClearOverrideBehavior(OverrideType.Full);
        }

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

            if (agent.IsDormant)
                return;

            PropertyCollection properties = ownerController.Blackboard.PropertyCollection;
            WorldEntity master = ownerController.AssistedEntity;

            if (master != null && master.IsInWorld)
            {
                float distanceToMasterSq = Vector3.DistanceSquared2D(agent.RegionLocation.Position, master.RegionLocation.Position);
                if (distanceToMasterSq > MaxDistToMasterBeforeTeleport * MaxDistToMasterBeforeTeleport)
                {
                    properties[PropertyEnum.AILastAttackerID] = 0;
                    HandleContext(proceduralAI, ownerController, TeleportToMasterIfTooFarAway);
                    ownerController.ResetCurrentTargetState();
                }
                else if (master.Locomotor != null && master.Locomotor.IsMoving)
                {
                    if (properties[PropertyEnum.AIAggroState] == false)
                    {
                        MoveToContextPrototype controlFollowProto = ControlFollow;
                        Verify.IsNotNull(controlFollowProto);
                        if (distanceToMasterSq > MaxDistToMasterBeforeFollow * MaxDistToMasterBeforeFollow)
                        {
                            agent.Properties[PropertyEnum.AIControlPowerLock] = true;
                            HandleMovementContext(proceduralAI, ownerController, agent.Locomotor, ControlFollow, false, out var movetoResult);
                            if (movetoResult == StaticBehaviorReturnType.Running) return;
                        }
                    }
                }
            }

            Locomotor locomotor = agent.Locomotor;
            if (locomotor != null && locomotor.IsFollowingEntity == false && agent.HasAIControlPowerLock)
            {
                ownerController.ResetCurrentTargetState();
                agent.Properties.RemoveProperty(PropertyEnum.AIControlPowerLock);
            }
        }
    }

    public class ProceduralProfileSpikedBallPrototype : ProceduralProfileWithTargetPrototype
    {
        public float MoveToSummonerDistance { get; protected set; }
        public float IdleDistanceFromSummoner { get; protected set; }
        public RotateContextPrototype Rotate { get; protected set; }
        public int SeekDelayMS { get; protected set; }
        public float Acceleration { get; protected set; }
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public WanderContextPrototype Wander { get; protected set; }
        public TeleportContextPrototype TeleportToMasterIfTooFarAway { get; protected set; }
        public int MaxDistToMasterBeforeTeleport { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }

        //---

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            Game game = agent.Game;
            if (!Verify.IsNotNull(game)) return;

            long currentTime = (long)game.CurrentTime.TotalMilliseconds;

            Avatar summoner = agent.GetMostResponsiblePowerUser<Avatar>();
            if (!Verify.IsNotNull(summoner, "The summoner of this AI Profile must be an avatar!"))
                return;

            PropertyCollection properties = ownerController.Blackboard.PropertyCollection;
            long lastTime = properties[PropertyEnum.AICustomTimeVal1];
            if (lastTime == 0)
            {
                properties[PropertyEnum.AICustomTimeVal1] = currentTime;
                return;
            }

            if (summoner.IsInWorld)
            {
                if (Vector3.DistanceSquared(agent.RegionLocation.Position, summoner.RegionLocation.Position) > MaxDistToMasterBeforeTeleport * MaxDistToMasterBeforeTeleport)
                {
                    ResetTarget(properties);
                    HandleContext(proceduralAI, ownerController, TeleportToMasterIfTooFarAway);
                }
            }

            properties[PropertyEnum.AICustomTimeVal1] = currentTime;
            float delay = (float)TimeSpan.FromMilliseconds(currentTime - lastTime).TotalSeconds;
            Vector3 currentPos = agent.RegionLocation.Position;
            float distanceSummonerSq = Vector3.DistanceSquared(currentPos, summoner.RegionLocation.Position);

            bool summonerTooFar = false;
            WorldEntity newTarget = null;
            if (distanceSummonerSq > MoveToSummonerDistance * MoveToSummonerDistance)
            {
                summonerTooFar = true;
                newTarget = summoner;
                properties[PropertyEnum.AIFocusTargetingID] = summoner.Id;
            }
            else
            {
                ulong targetId = properties[PropertyEnum.AIFocusTargetingID];
                if (targetId != 0)
                    newTarget = game.EntityManager.GetEntity<WorldEntity>(targetId);

                if (newTarget == null || newTarget.IsInWorld == false)
                    newTarget = TrySelectNewTarget(ownerController, properties, currentTime);
            }

            if (newTarget == null)
            {
                HandleMovementContext(proceduralAI, ownerController, agent.Locomotor, Wander, false, out _);
                return;
            }

            float idleDistanceSq = IdleDistanceFromSummoner * IdleDistanceFromSummoner;
            Vector3 distanceTarget = newTarget.RegionLocation.Position - currentPos;
            float distanceTargetSq = Vector3.LengthSquared(distanceTarget);

            float speedRate;
            if (newTarget == summoner && distanceTargetSq < idleDistanceSq)
            {
                speedRate = Math.Min(1.0f, distanceTargetSq / idleDistanceSq);
                speedRate *= speedRate < 0.05f ? 0.0f : speedRate;
                properties.RemoveProperty(PropertyEnum.AIFocusTargetingID);
            }
            else
            {
                speedRate = Math.Min(1.0f, agent.MovementSpeedRate + Acceleration * delay);
            }

            Locomotor locomotor = agent.Locomotor;
            if (!Verify.IsNotNull(locomotor)) return;

            float baseMoveSpeed = locomotor.DefaultRunSpeed;
            agent.Properties[PropertyEnum.MovementSpeedRate] = speedRate;
            agent.Properties[PropertyEnum.MovementSpeedOverride] = speedRate * baseMoveSpeed;

            if (Segment.IsNearZero(speedRate) == false)
            {
                ownerController.SetTargetEntity(newTarget);
                HandleMovementContext(proceduralAI, ownerController, locomotor, MoveToTarget, false, out var movetoResult, null);
                if (movetoResult == StaticBehaviorReturnType.Running) return;

                if (newTarget == summoner)
                {
                    if (movetoResult == StaticBehaviorReturnType.Failed && summonerTooFar)
                    {
                        ResetTarget(properties);
                        HandleContext(proceduralAI, ownerController, TeleportToMasterIfTooFarAway, null);
                    }
                    return;
                }
                else if (movetoResult == StaticBehaviorReturnType.Completed)
                {
                    TrySelectNewTarget(ownerController, properties, currentTime);
                }
                else if (movetoResult == StaticBehaviorReturnType.Failed)
                {
                    HandleMovementContext(proceduralAI, ownerController, locomotor, OrbitTarget, false, out _);
                }
            }
        }

        private WorldEntity TrySelectNewTarget(AIController ownerController, PropertyCollection properties, long currentTime)
        {
            SelectEntity.SelectEntityContext selectionContext = new(ownerController, SelectTarget);
            WorldEntity selectedEntity = SelectEntity.DoSelectEntity(selectionContext);

            if (selectedEntity == null || selectedEntity.Id == properties[PropertyEnum.AILastAttackerID])
            {
                long seekTime = properties[PropertyEnum.AICustomTimeVal2];
                long seekDelay = currentTime - seekTime;
                if (seekTime != 0 && seekDelay > SeekDelayMS)
                    ResetTarget(properties);
                return null;
            }
            ResetTarget(properties);
            properties[PropertyEnum.AIFocusTargetingID] = selectedEntity.Id;
            return selectedEntity;
        }

        private static void ResetTarget(PropertyCollection properties)
        {
            properties.RemoveProperty(PropertyEnum.AICustomTimeVal2);
            properties.RemoveProperty(PropertyEnum.AILastAttackerID);
        }

        public override void OnOwnerOverlapBegin(AIController ownerController, WorldEntity attacker)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

            Avatar summoner = agent.GetMostResponsiblePowerUser<Avatar>();
            if (attacker == summoner)
                return;

            WorldEntity target = ownerController.TargetEntity;
            if (target == attacker)
            {
                PropertyCollection properties = ownerController.Blackboard.PropertyCollection;
                properties.RemoveProperty(PropertyEnum.AIFocusTargetingID);

                Game game = ownerController.Game;
                if (!Verify.IsNotNull(game)) return;

                long currentTime = (long)game.CurrentTime.TotalMilliseconds;
                properties[PropertyEnum.AILastAttackerID] = attacker.Id;
                properties[PropertyEnum.AICustomTimeVal2] = currentTime;
                TrySelectNewTarget(ownerController, properties, currentTime);
            }
        }
    }

    public class ProceduralProfileTaserTrapPrototype : ProceduralProfileWithTargetPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public HotspotPrototype TaserHotspot { get; protected set; }

        //---

        private class RuntimeData : ProceduralProfileRuntimeData
        {
            public Dictionary<ulong, ulong> TaserHotspotIds { get; } = new();
        }

        public override void Init(Agent agent)
        {
            base.Init(agent);

            AIController ownerController = agent.AIController;
            if (!Verify.IsNotNull(ownerController)) return;

            ownerController.Blackboard.SetProceduralProfileRuntimeData(new RuntimeData());
        }

        public override void Think(AIController ownerController)
        {
            BehaviorSensorySystem senses = ownerController.Senses;
            if (senses.ShouldSense() == false)
                return;

            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            Game game = agent.Game;
            if (!Verify.IsNotNull(game)) return;

            EntityManager entityManager = game.EntityManager;

            RuntimeData profileData = ownerController.Blackboard.GetProceduralProfileRuntimeData<RuntimeData>();
            if (!Verify.IsNotNull(profileData)) return;

            // NOTE: We don't need a temporary collection here like the client because C# dictionaries allow removal during iteration
            foreach (var kvp in profileData.TaserHotspotIds)
            {
                ulong otherTrapId = kvp.Key;
                ulong taserHotspotId = kvp.Value;

                WorldEntity otherTrap = entityManager.GetEntity<WorldEntity>(otherTrapId);
                if (otherTrap == null || otherTrap.IsAliveInWorld == false)
                {
                    WorldEntity taserHotspot = entityManager.GetEntity<WorldEntity>(taserHotspotId);
                    taserHotspot?.Destroy();

                    profileData.TaserHotspotIds.Remove(otherTrapId);
                }
            }

            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return;

            Sphere volume = new(agent.RegionLocation.Position, ownerController.AggroRangeAlly);
            foreach (WorldEntity entity in region.IterateEntitiesInVolume(volume, new(EntityRegionSPContextFlags.PrimaryPartition)))
            {
                if (entity is not Agent otherTrap)
                    continue;

                if (otherTrap.Id == agent.Id)
                    continue;

                if (otherTrap.PrototypeDataRef != agent.PrototypeDataRef)
                    continue;

                if (IsTaserTrapPaired(agent, otherTrap))
                    continue;
                
                AddTaserHotspot(agent, otherTrap);
            }
        }

        private void AddTaserHotspot(Agent trap, Agent otherTrap)
        {
            AIController controller = trap.AIController;
            if (!Verify.IsNotNull(controller)) return;
            AIController otherController = otherTrap.AIController;
            if (!Verify.IsNotNull(otherController)) return;
            Game game = trap.Game;
            if (!Verify.IsNotNull(game)) return;
            EntityManager entityMan = game.EntityManager;
            if (!Verify.IsNotNull(entityMan)) return;

            using EntitySettings taserHotspotSettings = ObjectPoolManager.Instance.Get<EntitySettings>();
            Vector3 distance = trap.RegionLocation.Position - otherTrap.RegionLocation.Position;
            Vector3 center = distance * 0.5f;
            Vector3 delta = Vector3.Normalize2D(Vector3.AxisAngleRotate(center, Vector3.Up, MathHelper.ToRadians(90.0f)));
            taserHotspotSettings.EntityRef = TaserHotspot.DataRef;
            taserHotspotSettings.Orientation = Orientation.FromDeltaVector(delta);
            taserHotspotSettings.Position = trap.RegionLocation.ProjectToFloor() - center;
            taserHotspotSettings.RegionId = trap.RegionLocation.RegionId;

            using PropertyCollection properties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            properties.FlattenCopyFrom(trap.Properties, false);
            taserHotspotSettings.Properties = properties;

            TimeSpan trapLifespan = trap.GetRemainingLifespan();
            TimeSpan otherTrapLifespan = otherTrap.GetRemainingLifespan();
            taserHotspotSettings.Lifespan = trapLifespan > otherTrapLifespan ? otherTrapLifespan : trapLifespan;
            if (!Verify.IsTrue(taserHotspotSettings.Lifespan > TimeSpan.Zero, $"Taser Trap AI Profile does not support being used by entities with infinite lifespans! Offending owner: [{trap}]"))
                return;

            BoxBoundsPrototype taserHotspotBoxBounds = TaserHotspot.Bounds as BoxBoundsPrototype;
            if (!Verify.IsTrue(taserHotspotBoxBounds != null && taserHotspotBoxBounds.Length > 0, $"TaserHotspot bounds must be box bounds with a valid Length! Trap: {trap}"))
                return;

            WorldEntity taserHotspot = entityMan.CreateEntity(taserHotspotSettings) as WorldEntity;
            if (!Verify.IsNotNull(taserHotspot)) return;

            float dist = Math.Max(1.0f, Vector3.Length(distance));
            Bounds bounds = taserHotspot.Bounds;    // copy
            bounds.InitializeBox(taserHotspotBoxBounds.Width, dist, taserHotspotBoxBounds.Height, false, taserHotspotBoxBounds.CollisionType);
            taserHotspot.Bounds = bounds;

            RuntimeData runtimeData = controller.Blackboard.GetProceduralProfileRuntimeData<RuntimeData>();
            if (!Verify.IsNotNull(runtimeData)) return;

            runtimeData.TaserHotspotIds[otherTrap.Id] = taserHotspot.Id;
        }

        private static bool IsTaserTrapPaired(Agent trap, Agent otherTrap)
        {
            static bool IsTaserTrapPaired(Agent agent, ulong otherTrapId)
            {
                AIController controller = agent.AIController;
                if (!Verify.IsNotNull(controller)) return false;

                RuntimeData runtimeData = controller.Blackboard.GetProceduralProfileRuntimeData<RuntimeData>();
                if (runtimeData == null)
                    return false;

                return runtimeData.TaserHotspotIds.ContainsKey(otherTrapId);
            }

            return IsTaserTrapPaired(trap, otherTrap.Id) || IsTaserTrapPaired(otherTrap, trap.Id);
        }

        public override void OnOwnerExitWorld(AIController ownerController)
        {
            RuntimeData runtimeData = ownerController.Blackboard.GetProceduralProfileRuntimeData<RuntimeData>();
            if (!Verify.IsNotNull(runtimeData)) return;

            EntityManager entityManager = ownerController.Game.EntityManager;
            foreach (var kvp in runtimeData.TaserHotspotIds)
            {
                ulong taserHotspotId = kvp.Value;
                WorldEntity taserHotspot = entityManager.GetEntity<WorldEntity>(taserHotspotId);
                taserHotspot?.Destroy();
            }

            runtimeData.TaserHotspotIds.Clear();
        }
    }
}
