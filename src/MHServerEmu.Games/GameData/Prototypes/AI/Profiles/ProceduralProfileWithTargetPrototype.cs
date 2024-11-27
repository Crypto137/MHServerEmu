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
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.GameData.Prototypes
{

    public class ProceduralProfileWithTargetPrototype : ProceduralAIProfilePrototype
    {
        public SelectEntityContextPrototype SelectTarget { get; protected set; }
        public PrototypeId NoTargetOverrideProfile { get; protected set; }

        [Flags]
        protected enum SelectTargetFlags
        {
            None = 0,
            NoTargetOverride = 1 << 0,
            NotifyAllies = 1 << 1,
        }

        public override void Init(Agent agent)
        {
            base.Init(agent);
            AIController ownerController = agent.AIController;
            if (ownerController == null) return;
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
            if (agent == null) return false;

            if (senses.ShouldSense())
            {
                senses.Sense();
                if (agent.IsDormant == false)
                {
                    if (target == null || target.IsAliveInWorld == false ||
                        (selectTarget.LockEntityOnceSelected == false && ownerController.ActivePowerRef == PrototypeId.Invalid))
                        SelectTargetEntity(agent, ref target, ownerController, proceduralAI, selectTarget, targetType,
                                     SelectTargetFlags.NoTargetOverride | SelectTargetFlags.NotifyAllies, flags);
                    else
                        senses.ValidateCurrentTarget(targetType);
                }
                else
                    return false;
            }

            if (target == null || target.IsInWorld == false || agent.IsDormant) return false;

            return true;
        }

        protected bool SelectTargetEntity(Agent agent, ref WorldEntity target, AIController ownerController, ProceduralAI proceduralAI,
            SelectEntityContextPrototype selectTarget, CombatTargetType targetType, SelectTargetFlags targetFlags = SelectTargetFlags.None, 
            CombatTargetFlags flags = CombatTargetFlags.None)
        {
            WorldEntity currentTarget = target;
            var selectionContext = new SelectEntity.SelectEntityContext(ownerController, selectTarget);
            WorldEntity selectedEntity = SelectEntity.DoSelectEntity(selectionContext, flags);
            if (selectedEntity == null)
            {
                if (currentTarget != null && Combat.ValidTarget(agent.Game, agent, currentTarget, targetType, true)) return true;
                target = null;
                ownerController.SetTargetEntity(null);
                if (targetFlags.HasFlag(SelectTargetFlags.NoTargetOverride))
                    if (NoTargetOverrideProfile != PrototypeId.Invalid && ownerController.Blackboard.PropertyCollection[PropertyEnum.AIIgnoreNoTgtOverrideProfile] == false)
                    {
                        var profile = GameDatabase.GetPrototype<ProceduralAIProfilePrototype>(NoTargetOverrideProfile);
                        proceduralAI.SetOverride(profile, OverrideType.Full);
                    }
                return false;
            }
            else
            {
                if (selectedEntity == currentTarget) return true;
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
            if (target == null) return;
            if (proceduralAI.GetState(0) != Orbit.Instance)
            {
                HandleMovementContext(proceduralAI, ownerController, locomotor, moveToContextProto, false, out var movementResult);
                if (movementResult == StaticBehaviorReturnType.Running || movementResult == StaticBehaviorReturnType.Completed)
                    return;
            }

            HandleMovementContext(proceduralAI, ownerController, locomotor, orbitContextProto, false, out var orbitResult);
            if (orbitResult == StaticBehaviorReturnType.Running) return;

            if (orbitResult == StaticBehaviorReturnType.Failed)
            {
                if (NoTargetOverrideProfile == PrototypeId.Invalid
                    || ownerController.Blackboard.PropertyCollection[PropertyEnum.AIIgnoreNoTgtOverrideProfile] == true)
                    return;

                var profile = GameDatabase.GetPrototype<ProceduralProfileDefaultActiveOverridePrototype>(NoTargetOverrideProfile);
                if (profile == null)
                {
                    ProceduralAI.Logger.Warn($"default melee movement for [{ToString()}] requires NoTargetOverrideProfile to be a ProceduralProfileDefaultActiveOverridePrototype");
                    return;
                }

                HandleMovementContext(proceduralAI, ownerController, locomotor, profile.Wander, false, out _);
            }
        }

        protected static void DefaultRangedMovement(ProceduralAI proceduralAI, AIController ownerController, Agent agent, WorldEntity target,
            MoveToContextPrototype moveToContextProto, OrbitContextPrototype orbitContextProto)
        {
            if (moveToContextProto == null || orbitContextProto == null || target == null) return;

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
                    HandleMovementContext(proceduralAI, ownerController, agent.Locomotor, moveToContextProto, true, out var moveToResult);
                    if (moveToResult == StaticBehaviorReturnType.Running || moveToResult == StaticBehaviorReturnType.Completed)
                        return;
                }

                HandleMovementContext(proceduralAI, ownerController, agent.Locomotor, orbitContextProto, true, out var orbitResult);
                if (orbitResult == StaticBehaviorReturnType.Running || orbitResult == StaticBehaviorReturnType.Completed)
                    return;
            }

            HandleRotateToTarget(agent, target);
        }

        protected static void DefaultRangedFlankerMovement(ProceduralAI proceduralAI, AIController ownerController, Agent agent, WorldEntity target,
            long currentTime, MoveToContextPrototype moveToContextProto, ProceduralFlankContextPrototype flankContextProto)
        {
            if (target == null) return;

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
            if (proceduralFlankContext == null)
            {
                ProceduralAI.Logger.Warn($"AI profile trying to flank without a flank context!\nEntity: {ownerController.Owner}");
                return StaticBehaviorReturnType.None;
            }

            StaticBehaviorReturnType contextResult = StaticBehaviorReturnType.None;
            float flankTime = ownerController.Blackboard.PropertyCollection[PropertyEnum.AIProceduralNextFlankTime];
            if (proceduralAI.GetState(0) == Flank.Instance || currentTime > flankTime)
                HandleMovementContext(proceduralAI, ownerController, locomotor, proceduralFlankContext.FlankContext, checkPower, out contextResult, proceduralFlankContext);

            return contextResult;
        }

        protected static StaticBehaviorReturnType HandleProceduralFlee(ProceduralAI proceduralAI, AIController ownerController, 
            long currentTime, ProceduralFleeContextPrototype proceduralFleeContext)
        {
            if (proceduralFleeContext == null) return StaticBehaviorReturnType.None;

            StaticBehaviorReturnType contextResult = StaticBehaviorReturnType.None;
            float fleeTime = ownerController.Blackboard.PropertyCollection[PropertyEnum.AIProceduralNextFleeTime];
            if (proceduralAI.GetState(0) == Flee.Instance || currentTime > fleeTime)
                contextResult = HandleContext(proceduralAI, ownerController, proceduralFleeContext.FleeContext, proceduralFleeContext);

            return contextResult;
        }

        protected static void HandleRotateToTarget(Agent agent, WorldEntity target)
        {
            if (agent.CanRotate() && target != null && target.IsInWorld)
            {
                Locomotor locomotor = agent.Locomotor;
                if (locomotor == null)
                {
                    // ProceduralAI.Logger.Warn($"Agent [{agent}] does not have a locomotor and should not be calling this function");
                    return;
                }
                locomotor.LookAt(target.RegionLocation.Position);
            }
        }

        protected static bool IsPastMaxDistanceOrLostLOS(Agent agent, WorldEntity target, float rangeMax, bool enforceLOS, float radius, float padding)
        {
            if (target == null || target.IsInWorld == false) return false;
            float boundsRadius = agent.Bounds.Radius + target.Bounds.Radius;
            float distanceSq = Vector3.DistanceSquared2D(agent.RegionLocation.Position, target.RegionLocation.Position);
            if (distanceSq > MathHelper.Square(boundsRadius + rangeMax)) return true;
            if (enforceLOS && agent.LineOfSightTo(target, radius, padding) == false) return true;
            return false;
        }

        protected bool CommonSimplifiedSensory(WorldEntity target, AIController ownerController, ProceduralAI proceduralAI, 
            SelectEntityContextPrototype selectTarget, CombatTargetType targetType)
        {
            BehaviorSensorySystem senses = ownerController.Senses;
            if (senses.ShouldSense())
            {
                if (selectTarget == null) return false;
                Agent agent = ownerController.Owner;
                if (agent == null) return false;

                if (target == null || target.IsAliveInWorld == false || selectTarget.LockEntityOnceSelected == false)
                {
                    if (SelectTargetEntity(agent, ref target, ownerController, proceduralAI, selectTarget, targetType))
                        return true;
                }
                else
                    senses.ValidateCurrentTarget(targetType);
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

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;

            WorldEntity target = ownerController.TargetEntity;
            if (target == null || target.IsInWorld == false)
            {
                var selectionContext = new SelectEntity.SelectEntityContext(ownerController, SelectTarget);
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

        public override void Init(Agent agent)
        {
            base.Init(agent);
            InitPower(agent, Invulnerability);
        }

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false
                && proceduralAI.PartialOverrideBehavior == null) return;

            if (proceduralAI.GetState(0) != MoveTo.Instance 
                && proceduralAI.GetState(0) != Wander.Instance 
                && ownerController.AttemptActivatePower(Invulnerability, agent.Id, agent.RegionLocation.Position) == false) return;            

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

            if (ownerController.AttemptActivatePower(Invulnerability, agent.Id, agent.RegionLocation.Position))
                agent.Destroy();
        }
    }

    public class ProceduralProfileDefaultActiveOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public DelayContextPrototype DelayAfterWander { get; protected set; }
        public WanderContextPrototype Wander { get; protected set; }
        public WanderContextPrototype WanderInPlace { get; protected set; }

        private enum State
        {
            WanderInPlace = 0,
            Delay = 1,
            Wander = 2
        }

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            BehaviorSensorySystem senses = ownerController.Senses;

            if (senses.ShouldSense())
            {
                senses.Sense();
                ProceduralAIProfilePrototype baseProfile = proceduralAI.Behavior;
                if (baseProfile == null) return;
                if (baseProfile is not ProceduralProfileWithTargetPrototype targetProfile)
                {
                    ProceduralAI.Logger.Warn($"Agent {ownerController.Owner} has {baseProfile} which contains an invalid select target. " +
                        $"Make sure {baseProfile} derives from ProceduralProfileWithTargetPrototype");
                    return;
                }

                var selectionContext = new SelectEntity.SelectEntityContext(ownerController, targetProfile.SelectTarget);
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

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;

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
            PowerFailure
        }

        private static readonly Logger Logger = LogManager.CreateLogger();

        private float _orbRadiusSquared;

        public override void PostProcess()
        {
            base.PostProcess();

            _orbRadiusSquared = OrbRadius * OrbRadius;
        }

        public override void Init(Agent agent)
        {
            base.Init(agent);

            Game game = agent?.Game;
            if (game == null) return;

            AIController aiController = agent.AIController;
            if (aiController == null) return;

            BehaviorBlackboard blackboard = aiController.Blackboard;
            if (blackboard == null) return;

            // Delay AI activation to let the drop animation finish before an avatar can pick up this orb
            // NOTE: For some reason AIStartsEnabled is not set to false for some orb prototypes, so we force set it here.
            blackboard.PropertyCollection[PropertyEnum.AIStartsEnabled] = false;
            EventPointer<AIController.EnableAIEvent> enableEvent = new();
            aiController.ScheduleAIEvent(enableEvent, TimeSpan.FromMilliseconds(InitialMoveToDelayMS));

            agent.Properties[PropertyEnum.AICustomTimeVal1] = game.CurrentTime;

            InitPower(agent, EffectPower);
        }

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Game game = agent.Game;
            if (game == null) return;

            // Destroy this orb if it has finished shrinking
            if (ShrinkageDurationMS > 0)
            {
                TimeSpan shrinkageEndTime = agent.Properties[PropertyEnum.AICustomTimeVal1] 
                    + TimeSpan.FromMilliseconds(ShrinkageDelayMS) 
                    + TimeSpan.FromMilliseconds(ShrinkageDurationMS);

                if (game.CurrentTime >= shrinkageEndTime)
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
            // TODO: Orbs should shrink and have their effect be reduced over time, see CAgent::onEnterWorldScheduleOrbShrink for reference.

            OrbPrototype orbProto = agent.Prototype as OrbPrototype;
            if (orbProto == null) return Logger.WarnReturn(false, "TryGetPickedUp(): orbProto == null");

            if (ValidateTarget(agent, avatar, false) != ValidateTargetResult.Success)
                return false;

            Player player = avatar.GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(false, "TryGetPickedUp(): player == null");

            // Power (healing, endurance, boons)
            if (EffectPower != PrototypeId.Invalid)
                agent.AIController.AttemptActivatePower(EffectPower, avatar.Id, avatar.RegionLocation.Position);

            // Experience
            // Scale exp based on avatar level rather than orb level
            if (orbProto.GetXPAwarded(avatar.CharacterLevel, out long xp, out long minXP, player.CanUseLiveTuneBonuses()))
            {
                TuningTable tuningTable = orbProto.IgnoreRegionDifficultyForXPCalc == false ? agent.Region?.TuningTable : null;
                xp = avatar.ApplyXPModifiers(xp, false, tuningTable);
                avatar.AwardXP(xp, agent.Properties[PropertyEnum.ShowXPRewardText]);
            }

            // Credits / currency
            player.AcquireCurrencyItem(agent);

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

            // TODO: Other restrictions?

            // If this is an instanced orb, make sure the target belong to our player
            ulong restrictedToPlayerGuid = agent.Properties[PropertyEnum.RestrictedToPlayerGuid];
            if (restrictedToPlayerGuid != 0)
            {
                Player player = target.GetOwnerOfType<Player>();
                if (player == null) return Logger.WarnReturn(ValidateTargetResult.GenericFailure, "ValidateTarget(): player == null");

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
                if (power == null) return Logger.WarnReturn(ValidateTargetResult.GenericFailure, "ValidateTarget(): power == null");

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

        private static Avatar FindNearestAvatar(Agent agent)
        {
            Avatar target = null;

            if (agent.IsInWorld == false) return Logger.WarnReturn(target, "FindNearestAvatar(): agent.IsInWorld == false");

            Region region = agent.Region;
            if (region == null) return Logger.WarnReturn(target, "FindNearestAvatar(): region == null");

            Vector3 agentPosition = agent.RegionLocation.Position;
            float maxAggroRange = GameDatabase.AIGlobalsPrototype.OrbAggroRangeMax;

            float minDistance = float.MaxValue;
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

        public override void Init(Agent agent)
        {
            base.Init(agent);
            InitPower(agent, Power);
        }

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Game game = agent.Game;
            if (game == null) return;
            long currentTime = (long)game.CurrentTime.TotalMilliseconds;

            if (Power == null || Power.Power == PrototypeId.Invalid) return;
            BehaviorBlackboard blackboard = ownerController.Blackboard;
            WorldEntity target = ownerController.TargetEntity;
            CommonSimplifiedSensory(target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile);

            StaticBehaviorReturnType contextResult = HandleContext(proceduralAI, ownerController, Power, null);
            if (contextResult == StaticBehaviorReturnType.Running)
            {
                int changeTargetCount = blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
                if (changeTargetCount == 0)
                    blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = 1;
                else
                {
                    long powerStartTime = agent.Properties[PropertyEnum.PowerCooldownStartTime, Power.Power];
                    if (currentTime > (powerStartTime + PowerChangeTargetIntervalMS * changeTargetCount))
                    {
                        var selectionContext = new SelectEntity.SelectEntityContext(ownerController, SelectTarget);
                        target = SelectEntity.DoSelectEntity(selectionContext);
                        if (target != null)
                        {
                            if (SelectEntity.RegisterSelectedEntity(ownerController, target, selectionContext.SelectionType) == false) return;
                            blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = changeTargetCount + 1;
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
                blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = 0;
        }
    }

    public class ProceduralProfileSeekingMissilePrototype : ProceduralProfileWithTargetPrototype
    {
        public SelectEntityContextPrototype SecondaryTargetSelection { get; protected set; }
        public int SeekDelayMS { get; protected set; }
        public float SeekDelaySpeed { get; protected set; }

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Game game = agent.Game;
            if (game == null) return;
            long currentTime = (long)game.CurrentTime.TotalMilliseconds;

            Locomotor locomotor = agent.Locomotor;
            if (locomotor == null) return;

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            int stateVal = blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            if (stateVal != 1 && SeekDelayMS > 0)
            {
                long seekDelayTime = blackboard.PropertyCollection[PropertyEnum.AICustomTimeVal1];
                if (seekDelayTime == 0)
                {
                    blackboard.PropertyCollection[PropertyEnum.AICustomTimeVal1] = currentTime;
                    return;
                }

                if (currentTime - seekDelayTime < SeekDelayMS)
                    return;
                else
                {
                    blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = 1;
                    locomotor.SetMethod(LocomotorMethod.Default);
                }
            }

            WorldEntity target = ownerController.TargetEntity;
            if (CommonSimplifiedSensory(target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false) 
            { 
                if (SecondaryTargetSelection != null)
                {
                    if (SelectTargetEntity(agent, ref target, ownerController, proceduralAI, SecondaryTargetSelection, CombatTargetType.Hostile) == false) return;
                }
                else
                    return; 
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
            if (proceduralAI == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AINextSensoryUpdate);

            WorldEntity target = ownerController.TargetEntity;
            if (CommonSimplifiedSensory(target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false)
            {
                if (SecondaryTargetSelection != null)
                {
                    if (SelectTargetEntity(agent, ref target, ownerController, proceduralAI, SecondaryTargetSelection, CombatTargetType.Hostile) == false) return;
                }
                else
                    return;
            }

            Locomotor locomotor = agent.Locomotor;
            if (locomotor == null) return;

            if (target != null)
            {
                locomotor.FollowEntity(target.Id, 0.0f);
                locomotor.FollowEntityMissingEvent.AddActionFront(ownerController.MissileReturnAction);
            }
        }
    }

    public class ProceduralProfileSeekingMissileUniqueTargetPrototype : ProceduralProfileWithTargetPrototype
    {
        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Game game = agent.Game;
            if (game == null) return;

            WorldEntity target = ownerController.TargetEntity;
            if (CommonSimplifiedSensory(target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false) return;

            Locomotor locomotor = agent.Locomotor;
            if (locomotor == null) return;

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
            if (agent == null) return;
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

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Game game = agent.Game;
            if (game == null) return;

            WorldEntity target = ownerController.TargetEntity;
            if (CommonSimplifiedSensory(target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false) return;
            HandleContext(proceduralAI, ownerController, MoveToTarget);
        }

        public override void OnOwnerTargetSwitch(AIController ownerController, ulong oldTarget, ulong newTarget)
        {
            Agent agent = ownerController.Owner;
            if (agent == null) return;

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
            if (agent == null) return;

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

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Game game = agent.Game;
            if (game == null) return;

            if (agent.IsDormant) return;

            WorldEntity master = ownerController.AssistedEntity;
            if (master != null && master.IsInWorld)
            {                
                float distanceToMasterSq = Vector3.DistanceSquared2D(agent.RegionLocation.Position, master.RegionLocation.Position);
                if (distanceToMasterSq > MaxDistToMasterBeforeTeleport * MaxDistToMasterBeforeTeleport)
                {
                    ProceduralAI.Logger.Debug($"Teleport agent {agent.RegionLocation.Position} to master {master.RegionLocation.Position}");
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

        public override void Init(Agent agent)
        {
            base.Init(agent);

            AIController ownerController = agent?.AIController;
            if (ownerController == null) return;

            // Off leash and clear Full override behavior
            ownerController.Senses.CanLeash = false;
            ownerController.Brain?.ClearOverrideBehavior(OverrideType.Full);
        }

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;

            if (agent.IsDormant) return;

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            WorldEntity master = ownerController.AssistedEntity;

            if (master != null && master.IsInWorld)
            {
                float distanceToMasterSq = Vector3.DistanceSquared2D(agent.RegionLocation.Position, master.RegionLocation.Position);
                if (distanceToMasterSq > MaxDistToMasterBeforeTeleport * MaxDistToMasterBeforeTeleport)
                {
                    blackboard.PropertyCollection[PropertyEnum.AILastAttackerID] = 0;
                    HandleContext(proceduralAI, ownerController, TeleportToMasterIfTooFarAway);
                    ownerController.ResetCurrentTargetState();
                }
                else if (master.Locomotor != null && master.Locomotor.IsMoving)
                {
                    if (blackboard.PropertyCollection[PropertyEnum.AIAggroState] == false)
                    {
                        MoveToContextPrototype controlFollowProto = ControlFollow;
                        if (controlFollowProto == null) return;
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

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Game game = agent.Game;
            if (game == null) return;
            long currentTime = (long)game.CurrentTime.TotalMilliseconds;

            var summoner = agent.GetMostResponsiblePowerUser<Avatar>();
            if (summoner == null)
            {
                ProceduralAI.Logger.Warn("The summoner of this AI Profile must be an avatar!");
                return;
            }

            var blackboard = ownerController.Blackboard;
            long lastTime = blackboard.PropertyCollection[PropertyEnum.AICustomTimeVal1];
            if (lastTime == 0)
            {
                blackboard.PropertyCollection[PropertyEnum.AICustomTimeVal1] = currentTime;
                return;
            }

            if (summoner.IsInWorld)
                if (Vector3.DistanceSquared(agent.RegionLocation.Position, summoner.RegionLocation.Position) > MaxDistToMasterBeforeTeleport * MaxDistToMasterBeforeTeleport)
                {
                    ResetTarget(blackboard);
                    HandleContext(proceduralAI, ownerController, TeleportToMasterIfTooFarAway);
                }

            blackboard.PropertyCollection[PropertyEnum.AICustomTimeVal1] = currentTime;
            float dalay = (float)TimeSpan.FromMilliseconds(currentTime - lastTime).TotalSeconds;
            Vector3 currentPos = agent.RegionLocation.Position;
            float distanceSummonerSq = Vector3.DistanceSquared(currentPos, summoner.RegionLocation.Position);

            bool summonerTooFar = false;
            WorldEntity newTarget = null;
            if (distanceSummonerSq > MoveToSummonerDistance * MoveToSummonerDistance)
            {
                summonerTooFar = true;
                newTarget = summoner;
                blackboard.PropertyCollection[PropertyEnum.AIFocusTargetingID] = summoner.Id;
            }
            else
            {
                var targetId = blackboard.PropertyCollection[PropertyEnum.AIFocusTargetingID];
                if (targetId != 0)
                    newTarget = game.EntityManager.GetEntity<WorldEntity>(targetId);

                if (newTarget == null || newTarget.IsInWorld == false)
                    newTarget = TrySelectNewTarget(ownerController, blackboard, currentTime);
            }

            if (newTarget == null)
            {
                HandleMovementContext(proceduralAI, ownerController, agent.Locomotor, Wander, false, out _);
                return;
            }

            float idleDistanceSq = IdleDistanceFromSummoner * IdleDistanceFromSummoner;
            float speedRate;
            Vector3 distanceTarget = newTarget.RegionLocation.Position - currentPos;
            float distanceTargetSq = Vector3.LengthSquared(distanceTarget);
            if (newTarget == summoner && distanceTargetSq < idleDistanceSq)
            {
                speedRate = Math.Min(1.0f, distanceTargetSq / idleDistanceSq);
                speedRate *= speedRate < 0.05f ? 0.0f : speedRate;
                blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AIFocusTargetingID);
            }
            else
            {
                speedRate = Math.Min(1.0f, agent.MovementSpeedRate + Acceleration * dalay);
            }

            var locomotor = agent.Locomotor;
            if (locomotor == null) return;
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
                        ResetTarget(blackboard);
                        HandleContext(proceduralAI, ownerController, TeleportToMasterIfTooFarAway, null);
                    }
                    return;
                }
                else if (movetoResult == StaticBehaviorReturnType.Completed)
                    TrySelectNewTarget(ownerController, blackboard, currentTime);
                else if (movetoResult == StaticBehaviorReturnType.Failed)
                    HandleMovementContext(proceduralAI, ownerController, locomotor, OrbitTarget, false, out _);
            }
        }

        private WorldEntity TrySelectNewTarget(AIController ownerController, BehaviorBlackboard blackboard, long currentTime)
        {
            var selectionContext = new SelectEntity.SelectEntityContext(ownerController, SelectTarget);
            var selectedEntity = SelectEntity.DoSelectEntity(selectionContext);
            if (selectedEntity == null || selectedEntity.Id == blackboard.PropertyCollection[PropertyEnum.AILastAttackerID])
            {
                long seekTime = blackboard.PropertyCollection[PropertyEnum.AICustomTimeVal2];
                long seekDelay = currentTime - seekTime;
                if (seekTime != 0 && seekDelay > SeekDelayMS)
                    ResetTarget(blackboard);
                return null;
            }
            ResetTarget(blackboard);
            blackboard.PropertyCollection[PropertyEnum.AIFocusTargetingID] = selectedEntity.Id;
            return selectedEntity;
        }

        private static void ResetTarget(BehaviorBlackboard blackboard)
        {
            blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AICustomTimeVal2);
            blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AILastAttackerID);
        }

        public override void OnOwnerOverlapBegin(AIController ownerController, WorldEntity attacker)
        {
            var agent = ownerController.Owner;
            if (agent == null) return;
            var summoner = agent.GetMostResponsiblePowerUser<Avatar>();
            if (attacker == summoner) return;

            var target = ownerController.TargetEntity;
            if (target == attacker)
            {
                var blackboard = ownerController.Blackboard;
                blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AIFocusTargetingID);
                var game = ownerController.Game;
                if (game == null) return;

                long currentTime = (long)game.CurrentTime.TotalMilliseconds;
                blackboard.PropertyCollection[PropertyEnum.AILastAttackerID] = attacker.Id;
                blackboard.PropertyCollection[PropertyEnum.AICustomTimeVal2] = currentTime;
                TrySelectNewTarget(ownerController, blackboard, currentTime);
            }
        }

    }


    public class ProceduralProfileTaserTrapPrototype : ProceduralProfileWithTargetPrototype
    {
        public PrototypeId TaserHotspot { get; protected set; }

        public class RuntimeData : ProceduralProfileRuntimeData
        {
            public Dictionary<ulong, ulong> TaserHotspotIds { get; } = new ();
        }

        public override void Init(Agent agent)
        {
            base.Init(agent);

            var ownerController = agent.AIController;
            if (ownerController == null) return;
            ownerController.Blackboard.SetProceduralProfileRuntimeData(new RuntimeData());
        }

        public override void Think(AIController ownerController)
        {
            var senses = ownerController.Senses;
            if (senses.ShouldSense() == false) return;

            var proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;
            var agent = ownerController.Owner;
            if (agent == null) return;
            var game = agent.Game;
            if (game == null) return;
            var manager = game.EntityManager;

            var profileData = ownerController.Blackboard.GetProceduralProfileRuntimeData<RuntimeData>();
            if (profileData == null) return;

            List<ulong> trapsToRemove = new ();
            foreach (var kvp in profileData.TaserHotspotIds)
            {
                var otherTrapId = kvp.Key;
                var taserHotspotId = kvp.Value;

                var otherTrap = manager.GetEntity<WorldEntity>(otherTrapId);
                if (otherTrap == null || !otherTrap.IsAliveInWorld)
                {
                    var taserHotspot = manager.GetEntity<WorldEntity>(taserHotspotId);
                    taserHotspot?.Destroy();
                    trapsToRemove.Add(otherTrapId);
                }
            }

            foreach (var otherTrapId in trapsToRemove)
                profileData.TaserHotspotIds.Remove(otherTrapId);

            var region = agent.Region;
            if (region == null) return;
            var volume = new Sphere(agent.RegionLocation.Position, ownerController.AggroRangeAlly);
            foreach (var entity in region.IterateEntitiesInVolume(volume, new (EntityRegionSPContextFlags.ActivePartition)))
            {
                if (entity is not Agent otherTrap 
                    || otherTrap.Id == agent.Id 
                    || otherTrap.PrototypeDataRef != agent.PrototypeDataRef) continue;

                if (IsTaserTrapPaired(agent, otherTrap) == false)
                    AddTaserHotspot(agent, otherTrap);
            }
        }

        private void AddTaserHotspot(Agent trap, Agent otherTrap)
        {
            var controller = trap.AIController;
            var otherController = otherTrap.AIController;
            if (controller == null || otherController == null) return;
            var game = trap.Game;
            if (game == null) return;
            var entityMan = game.EntityManager;
            if (entityMan == null) return;

            using EntitySettings taserHotspotSettings = ObjectPoolManager.Instance.Get<EntitySettings>();
            var distance = trap.RegionLocation.Position - otherTrap.RegionLocation.Position;
            var center =  distance * 0.5f;
            var delta = Vector3.Normalize2D(Vector3.AxisAngleRotate(center, Vector3.Up, MathHelper.ToRadians(90.0f)));
            taserHotspotSettings.EntityRef = TaserHotspot;
            taserHotspotSettings.Orientation = Orientation.FromDeltaVector(delta);
            taserHotspotSettings.Position = trap.RegionLocation.ProjectToFloor() - center;
            taserHotspotSettings.RegionId = trap.RegionLocation.RegionId;

            using PropertyCollection properties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            properties.FlattenCopyFrom(trap.Properties, false);
            taserHotspotSettings.Properties = properties;

            TimeSpan trapLifespan = trap.GetRemainingLifespan();
            TimeSpan otherTrapLifespan = otherTrap.GetRemainingLifespan();
            taserHotspotSettings.Lifespan = trapLifespan > otherTrapLifespan ? otherTrapLifespan : trapLifespan;
            if (taserHotspotSettings.Lifespan <= TimeSpan.Zero)
            {
                ProceduralAI.Logger.Warn($"Taser Trap AI Profile does not support being used by entities with infinite lifespans! Offending owner: {trap}");
                return;
            }
            var taserHotspotProto = TaserHotspot.As<HotspotPrototype>();
            if (taserHotspotProto.Bounds is not BoxBoundsPrototype taserHotspotBoxBounds || taserHotspotBoxBounds.Length <= 0) 
            {
                ProceduralAI.Logger.Warn($"TaserHotspot bounds must be box bounds with a valid Length! Trap: {trap}");
                return; 
            }

            if (entityMan.CreateEntity(taserHotspotSettings) is not WorldEntity taserHotspot) return;

            float dist = Math.Max(1.0f, Vector3.Length(distance));
            Bounds bounds = new(taserHotspot.Bounds);
            bounds.InitializeBox(taserHotspotBoxBounds.Width, dist, taserHotspotBoxBounds.Height, false, taserHotspotBoxBounds.CollisionType);
            taserHotspot.Bounds = bounds;

            var runtimeData = controller.Blackboard.GetProceduralProfileRuntimeData<RuntimeData>();
            if (runtimeData == null) return;
            runtimeData.TaserHotspotIds[otherTrap.Id] = taserHotspot.Id;
        }

        private static bool IsTaserTrapPaired(Agent trap, Agent otherTrap)
        {
            bool IsTaserTrapPaired(Agent agent, ulong otherTrapId)
            {
                var controller = agent.AIController;
                if (controller == null) return false;
                var runtimeData = controller.Blackboard.GetProceduralProfileRuntimeData<RuntimeData>();
                if (runtimeData == null) return false;

                return runtimeData.TaserHotspotIds.ContainsKey(otherTrapId);
            }

            return IsTaserTrapPaired(trap, otherTrap.Id) || IsTaserTrapPaired(otherTrap, trap.Id);
        }

        public override void OnOwnerExitWorld(AIController ownerController)
        {
            var profileData = ownerController.Blackboard.GetProceduralProfileRuntimeData<RuntimeData>();
            if (profileData == null) return;
            var manager = ownerController.Game.EntityManager;
            foreach (var kvp in profileData.TaserHotspotIds)
            {
                var taserHotspotId = kvp.Value;
                var taserHotspot = manager.GetEntity<WorldEntity>(taserHotspotId);
                taserHotspot?.Destroy();
            }
            profileData.TaserHotspotIds.Clear();
        }
    }

}
