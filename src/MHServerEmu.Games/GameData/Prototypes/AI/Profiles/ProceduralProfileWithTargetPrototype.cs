using MHServerEmu.Core.Helpers;
using MHServerEmu.Games.Behavior.ProceduralAI;
using MHServerEmu.Games.Behavior.StaticAI;
using MHServerEmu.Games.Behavior;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Properties;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.System.Random;

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
            WorldEntity selectedEntity = SelectEntity.DoSelectEntity(ref selectionContext, flags);
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

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            StaticBehaviorReturnType contextResult = StaticBehaviorReturnType.None;
            float flankTime = blackboard.PropertyCollection[PropertyEnum.AIProceduralNextFlankTime];
            if (proceduralAI.GetState(0) == Flank.Instance || currentTime > flankTime)
                HandleMovementContext(proceduralAI, ownerController, locomotor, proceduralFlankContext.FlankContext, checkPower, out contextResult, proceduralFlankContext);

            return contextResult;
        }

        protected static void HandleRotateToTarget(Agent agent, WorldEntity target)
        {
            if (agent.CanRotate && target != null && target.IsInWorld)
            {
                Locomotor locomotor = agent.Locomotor;
                if (locomotor == null)
                {
                    ProceduralAI.Logger.Warn($"Agent [{agent}] does not have a locomotor and should not be calling this function");
                    return;
                }
                locomotor.LookAt(target.RegionLocation.Position);
            }
        }

        private static bool IsPastMaxDistanceOrLostLOS(Agent agent, WorldEntity target, float rangeMax, bool enforceLOS, float radius, float padding)
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
                WorldEntity selectedEntity = SelectEntity.DoSelectEntity(ref selectionContext);
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
                WorldEntity selectedEntity = SelectEntity.DoSelectEntity(ref selectionContext);
                if (selectedEntity != null && proceduralAI.GetState(0) != UsePower.Instance)
                {
                    blackboard.PropertyCollection[PropertyEnum.AIDefaultActiveOverrideStateVal] = (int)State.WanderInPlace;
                    SelectEntity.RegisterSelectedEntity(ownerController, selectedEntity, selectionContext.SelectionType);
                    senses.NotifyAlliesOnTargetAcquired();
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

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, EffectPower);
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
            long currentTime = (long)game.GetCurrentTime().TotalMilliseconds;

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
                        target = SelectEntity.DoSelectEntity(ref selectionContext);
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

            ulong targetId = target.Id;
            if (target != null)
            {
                if (locomotor.FollowEntityId != targetId)
                {
                    locomotor.FollowEntity(targetId, 0.0f);
                    target.Properties[PropertyEnum.FocusTargetedOnByID] = agent.Id;
                    ownerController.Blackboard.PropertyCollection[PropertyEnum.AIFocusTargetingID] = target.Id;
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
                    HandleContext(proceduralAI, ownerController, TeleportToMasterIfTooFarAway);
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
    }

    public class ProceduralProfileTaserTrapPrototype : ProceduralProfileWithTargetPrototype
    {
        public PrototypeId TaserHotspot { get; protected set; }
    }

}
