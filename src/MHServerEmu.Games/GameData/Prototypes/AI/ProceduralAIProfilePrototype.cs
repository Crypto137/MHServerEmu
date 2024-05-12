using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.Behavior;
using MHServerEmu.Games.Behavior.ProceduralAI;
using MHServerEmu.Games.Behavior.StaticAI;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class ProceduralAIProfilePrototype : BrainPrototype
    {
        public static StaticBehaviorReturnType HandleContext<TContextProto>(ProceduralAI proceduralAI, AIController ownerController,
            TContextProto contextProto, ProceduralContextPrototype proceduralContext = null)
            where TContextProto : Prototype
        {
            (IAIState instance, IStateContext context) = IStateContext.Create(ownerController, contextProto);
            return proceduralAI.HandleContext(instance, ref context, proceduralContext);
        }

        public static bool HandleMovementContext<TContextProto>(ProceduralAI proceduralAI, AIController ownerController, 
            Locomotor locomotor, TContextProto contextProto, bool checkPower, out StaticBehaviorReturnType movementResult, ProceduralContextPrototype proceduralContext = null)
             where TContextProto : Prototype
        {
            movementResult = StaticBehaviorReturnType.None;
            if (locomotor == null)
            {
                ProceduralAI.Logger.Warn($"Can't move without a locomotor! {locomotor}");
                return false;
            }
            (IAIState instance, IStateContext context) = IStateContext.Create(ownerController, contextProto);
            movementResult = proceduralAI.HandleContext(instance, ref context, proceduralContext);
            if (ResetTargetAndStateIfPathFails(proceduralAI, ownerController, locomotor, ref context, checkPower))
                return false;
            return true;
        }

        private static bool ResetTargetAndStateIfPathFails(ProceduralAI proceduralAI, AIController ownerController, Locomotor locomotor, 
            ref IStateContext context, bool checkPower)
        {
            Agent owner = ownerController.Owner;
            if (owner == null) return false;
            if (locomotor == null)
            {
                ProceduralAI.Logger.Warn($"Agent [{owner}] doesn't have a locomotor and should not be calling this function");
                return false;
            }

            if (locomotor.LastGeneratedPathResult == NaviPathResult.FailedNoPathFound)
            {
                bool resetTarget = true;
                if (checkPower) resetTarget = proceduralAI.LastPowerResult == StaticBehaviorReturnType.Failed;
                if (resetTarget) ownerController.ResetCurrentTargetState();
                proceduralAI.SwitchProceduralState(null, ref context, StaticBehaviorReturnType.Failed);
                return true;
            }

            return false;
        }

        public static bool ValidateContext(ProceduralAI proceduralAI, AIController ownerController, UsePowerContextPrototype contextProto)
        {
            IStateContext context = new UsePowerContext(ownerController, contextProto);
            return proceduralAI.ValidateContext(UsePower.Instance, ref context);
        }

        protected static bool ValidateUsePowerContext(AIController ownerController, ProceduralAI proceduralAI, UsePowerContextPrototype powerContext)
        {
            return ValidateContext(proceduralAI, ownerController, powerContext);
        }

        public bool HandleOverrideBehavior(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return false;

            ProceduralAIProfilePrototype fullOverrideBehavior = proceduralAI.FullOverrideBehavior;
            if (fullOverrideBehavior != null && fullOverrideBehavior.GetType() != GetType())
            {
                fullOverrideBehavior.Think(ownerController);
                if (ownerController.IsOwnerValid() == false) return true;
                return proceduralAI.FullOverrideBehavior != null;
            }
            return false;
        }

        public virtual void Think(AIController ownerController)
        {
            ProceduralAI.Logger.Error("ProceduralAIProfilePrototype.THINK() - BASE CLASS SHOULD NOT BE CALLED");
        }
    }

    public class ProceduralProfileWithTargetPrototype : ProceduralAIProfilePrototype
    {
        public SelectEntityContextPrototype SelectTarget { get; protected set; }
        public PrototypeId NoTargetOverrideProfile { get; protected set; }

        [Flags]
        protected enum SelectTargetFlags
        {
            None,
            NoTargetOverride = 1 << 0,
            NotifyAllies = 1 << 1,
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

        protected void SelectTargetEntity(Agent agent, ref WorldEntity target, AIController ownerController, ProceduralAI proceduralAI, SelectEntityContextPrototype selectTarget, CombatTargetType targetType, SelectTargetFlags selectTargetFlags, CombatTargetFlags flags)
        {
            throw new NotImplementedException();
        }

        public void DefaultMeleeMovement(ProceduralAI proceduralAI, AIController ownerController, Locomotor locomotor, 
            WorldEntity target, MoveToContextPrototype moveToTarget, OrbitContextPrototype orbitTarget)
        {
            if (target == null) return;
            if (proceduralAI.GetState(0) != Orbit.Instance)
            {
                HandleMovementContext(proceduralAI, ownerController, locomotor, moveToTarget, false, out var movementResult); 
                if (movementResult == StaticBehaviorReturnType.Running || movementResult == StaticBehaviorReturnType.Completed) 
                    return;
            }

            HandleMovementContext(proceduralAI, ownerController, locomotor, orbitTarget, false, out var orbitResult);
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

    }

    public class ProceduralProfileWithAttackPrototype : ProceduralProfileWithTargetPrototype
    {
        public int AttackRateMaxMS { get; protected set; }
        public int AttackRateMinMS { get; protected set; }
        public ProceduralUsePowerContextPrototype[] GenericProceduralPowers { get; protected set; }
        public ProceduralUseAffixPowerContextPrototype AffixSettings { get; protected set; }

        public virtual void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            ownerController.AddPowersToPicker(powerPicker, GenericProceduralPowers);
        }

        protected static bool AddPowerToPickerIfStartedPowerIsContextPower(AIController ownerController, 
            ProceduralUsePowerContextPrototype powerToAdd, PrototypeId startedPowerRef, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {  
            var powerContext = powerToAdd?.PowerContext;
            if (powerContext == null 
                || powerContext.Power == PrototypeId.Invalid 
                || startedPowerRef != powerContext.Power) return false;

            ownerController.AddPowersToPicker(powerPicker, powerToAdd);
            return true;
        }

        protected StaticBehaviorReturnType HandleProceduralPower(AIController ownerController, ProceduralAI proceduralAI, GRandom random, 
            long currentTime, Picker<ProceduralUsePowerContextPrototype> powerPicker, bool affixes)
        {
            throw new NotImplementedException();
        }
    }

    public class ProceduralProfileEnticerPrototype : ProceduralAIProfilePrototype
    {
        public int CooldownMinMS { get; protected set; }
        public int CooldownMaxMS { get; protected set; }
        public int EnticeeEnticerCooldownMaxMS { get; protected set; }
        public int EnticeeEnticerCooldownMinMS { get; protected set; }
        public int EnticeeGlobalEnticerCDMaxMS { get; protected set; }
        public int EnticeeGlobalEnticerCDMinMS { get; protected set; }
        public int MaxSubscriptions { get; protected set; }
        public int MaxSubscriptionsPerActivation { get; protected set; }
        public float Radius { get; protected set; }
        public AIEntityAttributePrototype[] EnticeeAttributes { get; protected set; }
        public PrototypeId EnticedBehavior { get; protected set; }
    }

    public class ProceduralProfileEnticedBehaviorPrototype : ProceduralAIProfilePrototype
    {
        public FlankContextPrototype FlankToEnticer { get; protected set; }
        public MoveToContextPrototype MoveToEnticer { get; protected set; }
        public PrototypeId DynamicBehavior { get; protected set; }
        public bool OrientToEnticerOrientation { get; protected set; }
    }

    public class ProceduralProfileSenseOnlyPrototype : ProceduralAIProfilePrototype
    {
        public AIEntityAttributePrototype[] AttributeList { get; protected set; }
        public PrototypeId AllianceOverride { get; protected set; }
    }

    public class ProceduralProfileInteractEnticerOverridePrototype : ProceduralAIProfilePrototype
    {
        public InteractContextPrototype Interact { get; protected set; }
    }

    public class ProceduralProfileUsePowerEnticerOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public UsePowerContextPrototype Power { get; protected set; }
        public new SelectEntityContextPrototype SelectTarget { get; protected set; }
    }

    public class ProceduralProfileFearOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public FleeContextPrototype FleeFromTarget { get; protected set; }
        public WanderContextPrototype WanderIfNoTarget { get; protected set; }
    }

    public class ProceduralProfileLeashOverridePrototype : ProceduralAIProfilePrototype
    {
        public PrototypeId LeashReturnHeal { get; protected set; }
        public PrototypeId LeashReturnImmunity { get; protected set; }
        public MoveToContextPrototype MoveToSpawn { get; protected set; }
        public TeleportContextPrototype TeleportToSpawn { get; protected set; }
        public PrototypeId LeashReturnTeleport { get; protected set; }
        public PrototypeId LeashReturnInvulnerability { get; protected set; }
    }

    public class ProceduralProfileRunToExitAndDespawnOverridePrototype : ProceduralAIProfilePrototype
    {
        public MoveToContextPrototype RunToExit { get; protected set; }
        public int NumberOfWandersBeforeDestroy { get; protected set; }
        public DelayContextPrototype DelayBeforeRunToExit { get; protected set; }
        public SelectEntityContextPrototype SelectPortalToExitFrom { get; protected set; }
        public DelayContextPrototype DelayBeforeDestroyOnMoveExitFail { get; protected set; }
        public bool VanishesIfMoveToExitFails { get; protected set; }
    }

    public class ProceduralProfileRunToTargetAndDespawnOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public PrototypeId Invulnerability { get; protected set; }
        public int NumberOfWandersBeforeDestroy { get; protected set; }
        public MoveToContextPrototype RunToTarget { get; protected set; }
        public WanderContextPrototype WanderIfMoveFails { get; protected set; }
    }

    public class ProceduralProfileDefaultActiveOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public DelayContextPrototype DelayAfterWander { get; protected set; }
        public WanderContextPrototype Wander { get; protected set; }
        public WanderContextPrototype WanderInPlace { get; protected set; }
    }

    public class ProceduralProfileFleeOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public FleeContextPrototype FleeFromTarget { get; protected set; }
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
    }

    public class ProceduralProfileStationaryTurretPrototype : ProceduralProfileWithAttackPrototype
    {
    }

    public class ProceduralProfileRotatingTurretPrototype : ProceduralAIProfilePrototype
    {
        public UsePowerContextPrototype Power { get; protected set; }
        public RotateContextPrototype Rotate { get; protected set; }
    }

    public class ProceduralProfileRotatingTurretWithTargetPrototype : ProceduralProfileWithAttackPrototype
    {
        public RotateContextPrototype Rotate { get; protected set; }
    }

    public class ProceduralProfileBasicMeleePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype PrimaryPower { get; protected set; }

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Game game = agent.Game;
            if (game == null) return;
            long currentTime = (long)game.GetCurrentTime().TotalMilliseconds;

            if (HandleOverrideBehavior(ownerController)) return;

            WorldEntity target = ownerController.TargetEntity; 
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false 
                && proceduralAI.PartialOverrideBehavior == null) return;

            GRandom random = game.Random; 
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running) return;

            proceduralAI.PartialOverrideBehavior?.Think(ownerController);

            DefaultMeleeMovement(proceduralAI, ownerController, agent.Locomotor, target, MoveToTarget, OrbitTarget);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            ownerController.AddPowersToPicker(powerPicker, PrimaryPower);
        }
    }

    public class ProceduralProfileBasicMelee2PowerPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype Power1 { get; protected set; }
        public ProceduralUsePowerContextPrototype Power2 { get; protected set; }

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Game game = agent.Game;
            if (game == null) return;
            long currentTime = (long)game.GetCurrentTime().TotalMilliseconds;

            if (HandleOverrideBehavior(ownerController)) return;

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false
                && proceduralAI.PartialOverrideBehavior == null) return;

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running) return;

            proceduralAI.PartialOverrideBehavior?.Think(ownerController);

            DefaultMeleeMovement(proceduralAI, ownerController, agent.Locomotor, target, MoveToTarget, OrbitTarget);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            ownerController.AddPowersToPicker(powerPicker, Power1);
            ownerController.AddPowersToPicker(powerPicker, Power2);
        }
    }

    public class ProceduralProfileBasicRangePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
    }

    public class ProceduralProfileAlternateRange2Prototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public ProceduralFlankContextPrototype FlankTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype Power1 { get; protected set; }
        public ProceduralUsePowerContextPrototype Power2 { get; protected set; }
        public ProceduralUsePowerContextPrototype PowerSwap { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {            
            int stateVal = ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            if (stateVal == 0)
                ownerController.AddPowersToPicker(powerPicker, Power1);
            else if (stateVal == 1)
                ownerController.AddPowersToPicker(powerPicker, Power2);
            ownerController.AddPowersToPicker(powerPicker, PowerSwap);
        }
    }

    public class ProceduralProfileMultishotRangePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype MultishotPower { get; protected set; }
        public int NumShots { get; protected set; }
        public bool RetargetPerShot { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            ownerController.AddPowersToPicker(powerPicker, MultishotPower);
        }
    }

    public class ProceduralProfileMultishotFlankerPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralFlankContextPrototype FlankTarget { get; protected set; }
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype MultishotPower { get; protected set; }
        public int NumShots { get; protected set; }
        public bool RetargetPerShot { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            ownerController.AddPowersToPicker(powerPicker, MultishotPower);
        }
    }

    public class ProceduralProfileMultishotHiderPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralUsePowerContextPrototype HidePower { get; protected set; }
        public ProceduralUsePowerContextPrototype MultishotPower { get; protected set; }
        public int NumShots { get; protected set; }
        public bool RetargetPerShot { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            int stateVal = ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal2];
            if (stateVal == 1)
                ownerController.AddPowersToPicker(powerPicker, MultishotPower);
            else
                ownerController.AddPowersToPicker(powerPicker, HidePower);
            base.PopulatePowerPicker(ownerController, powerPicker);
        }
    }

    public class ProceduralProfileMeleeSpeedByDistancePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype PrimaryPower { get; protected set; }
        public UsePowerContextPrototype ExtraSpeedPower { get; protected set; }
        public UsePowerContextPrototype SpeedRemovalPower { get; protected set; }
        public float DistanceFromTargetForSpeedBonus { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            ownerController.AddPowersToPicker(powerPicker, PrimaryPower);
        }
    }

    public class ProceduralProfileRangeFlankerPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralFlankContextPrototype FlankTarget { get; protected set; }
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype PrimaryPower { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            ownerController.AddPowersToPicker(powerPicker, PrimaryPower);
        }
    }

    public class ProceduralProfileSkirmisherPrototype : ProceduralProfileWithAttackPrototype
    {
        public WanderContextPrototype SkirmishMovement { get; protected set; }
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype PrimaryPower { get; protected set; }
        public float MoveToSpeedBonus { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            ownerController.AddPowersToPicker(powerPicker, PrimaryPower);
        }
    }

    public class ProceduralProfileRangedWithMeleePriority2PowerPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype MeleePower { get; protected set; }
        public ProceduralUsePowerContextPrototype RangedPower { get; protected set; }
        public float MaxDistToMoveIntoMelee { get; protected set; }
        public MoveToContextPrototype MoveIntoMeleeRange { get; protected set; }
    }

    public class ProfMeleePwrSpecialAtHealthPctPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public float SpecialAtHealthChunkPct { get; protected set; }
        public UsePowerContextPrototype SpecialPowerAtHealthChunkPct { get; protected set; }
    }

    public class ProceduralProfileShockerPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public MoveToContextPrototype MoveIntoMeleeRange { get; protected set; }
        public ProceduralUsePowerContextPrototype MeleePower { get; protected set; }
        public ProceduralUsePowerContextPrototype SpecialPower { get; protected set; }
        public PrototypeId SpecialSummonPower { get; protected set; }
        public float MaxDistToMoveIntoMelee { get; protected set; }
        public int SpecialPowerNumSummons { get; protected set; }
        public float SpecialPowerMaxRadius { get; protected set; }
        public float SpecialPowerMinRadius { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            ownerController.AddPowersToPicker(powerPicker, MeleePower);
            ownerController.AddPowersToPicker(powerPicker, SpecialPower);
        }
    }

    public class ProceduralProfileLadyDeathstrikePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype HealingPower { get; protected set; }
        public ProceduralUsePowerContextPrototype SpecialPower { get; protected set; }
        public SelectEntityContextPrototype SpecialPowerSelectTarget { get; protected set; }
        public int SpecialPowerChangeTgtIntervalMS { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            ownerController.AddPowersToPicker(powerPicker, SpecialPower);

            Agent agent = ownerController.Owner;
            var powerContext = HealingPower?.PowerContext;
            if (agent == null || powerContext == null || powerContext.Power == PrototypeId.Invalid) return;

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            if (blackboard.PropertyCollection.HasProperty(new PropertyId(PropertyEnum.AIPowerStarted, powerContext.Power)))
                ownerController.AddPowersToPicker(powerPicker, HealingPower);
            else
            {
                CombatTargetFlags flags = CombatTargetFlags.IgnoreAggroDistance | CombatTargetFlags.IgnoreStealth | CombatTargetFlags.IgnoreLOS;
                if (Combat.GetNumTargetsInRange(agent, 200.0f, 0.0f, CombatTargetType.Hostile, flags) == 0)
                    ownerController.AddPowersToPicker(powerPicker, HealingPower);
            }
        }
    }

    public class ProceduralProfileFastballSpecialWolverinePrototype : ProceduralProfileWithTargetPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public WanderContextPrototype MoveToNoTarget { get; protected set; }
        public UsePowerContextPrototype Power { get; protected set; }
        public int PowerChangeTargetIntervalMS { get; protected set; }
    }

    public class ProceduralProfileSeekingMissilePrototype : ProceduralProfileWithTargetPrototype
    {
        public SelectEntityContextPrototype SecondaryTargetSelection { get; protected set; }
        public int SeekDelayMS { get; protected set; }
        public float SeekDelaySpeed { get; protected set; }
    }

    public class ProceduralProfileSeekingMissileUniqueTargetPrototype : ProceduralProfileWithTargetPrototype
    {
    }

    public class ProceduralProfileNoMoveDefaultSensoryPrototype : ProceduralProfileWithAttackPrototype
    {
    }

    public class ProceduralProfileNoMoveSimplifiedSensoryPrototype : ProceduralProfileWithAttackPrototype
    {
    }

    public class ProceduralProfileNoMoveSimplifiedAllySensoryPrototype : ProceduralProfileWithAttackPrototype
    {
    }

    public class ProfKillSelfAfterOnePowerNoMovePrototype : ProceduralProfileWithAttackPrototype
    {
    }

    public class ProceduralProfileNoMoveNoSensePrototype : ProceduralProfileWithAttackPrototype
    {
    }

    public class ProceduralProfileMoveToUniqueTargetNoPowerPrototype : ProceduralProfileWithTargetPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
    }

    public class ProceduralProfileWanderNoPowerPrototype : ProceduralAIProfilePrototype
    {
        public WanderContextPrototype WanderMovement { get; protected set; }
    }

    public class ProceduralProfileBasicWanderPrototype : ProceduralProfileWithAttackPrototype
    {
        public WanderContextPrototype WanderMovement { get; protected set; }
    }

    public class ProceduralProfilePvPMeleePrototype : ProceduralProfileWithAttackPrototype
    {
        public float AggroRadius { get; protected set; }
        public float AggroDropRadius { get; protected set; }
        public float AggroDropByLOSChance { get; protected set; }
        public long AttentionSpanMS { get; protected set; }
        public PrototypeId PrimaryPower { get; protected set; }
        public int PathGroup { get; protected set; }
        public PathMethod PathMethod { get; protected set; }
        public float PathThreshold { get; protected set; }
    }

    public class ProceduralProfilePvPTowerPrototype : ProceduralProfileWithAttackPrototype
    {
        public SelectEntityContextPrototype SelectTarget2 { get; protected set; }
        public SelectEntityContextPrototype SelectTarget3 { get; protected set; }
        public SelectEntityContextPrototype SelectTarget4 { get; protected set; }
    }

    public class ProceduralProfileMeleeDropWeaponPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype PowerMeleeWithWeapon { get; protected set; }
        public ProceduralUsePowerContextPrototype PowerMeleeNoWeapon { get; protected set; }
        public ProceduralUsePowerContextPrototype PowerDropWeapon { get; protected set; }
        public ProceduralUsePowerContextPrototype PowerPickupWeapon { get; protected set; }
        public SelectEntityContextPrototype SelectWeaponAsTarget { get; protected set; }
        public int DropPickupTimeMax { get; protected set; }
        public int DropPickupTimeMin { get; protected set; }
    }

    public class ProceduralProfileMeleeAllyDeathFleePrototype : ProceduralProfileWithAttackPrototype
    {
        public FleeContextPrototype FleeFromTargetOnAllyDeath { get; protected set; }
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
    }

    public class ProceduralProfileRangedFlankerAllyDeathFleePrototype : ProceduralProfileWithAttackPrototype
    {
        public FleeContextPrototype FleeFromTargetOnAllyDeath { get; protected set; }
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public ProceduralFlankContextPrototype FlankTarget { get; protected set; }
    }

    public class ProceduralProfileMagnetoPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralFlankContextPrototype FlankTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype ChasePower { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            ownerController.AddPowersToPicker(powerPicker, ChasePower);
            base.PopulatePowerPicker(ownerController, powerPicker);
        }
    }

    public class ProcProfMrSinisterPrototype : ProceduralProfileWithAttackPrototype
    {
        public float CloneCylHealthPctThreshWave1 { get; protected set; }
        public float CloneCylHealthPctThreshWave2 { get; protected set; }
        public float CloneCylHealthPctThreshWave3 { get; protected set; }
        public UsePowerContextPrototype CloneCylSummonFXPower { get; protected set; }
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public TriggerSpawnersContextPrototype TriggerCylinderSpawnerAction { get; protected set; }
    }

    public class ProcProfMrSinisterCloneCylinderPrototype : ProceduralProfileWithAttackPrototype
    {
        public UsePowerContextPrototype CylinderOpenPower { get; protected set; }
        public DespawnContextPrototype DespawnAction { get; protected set; }
        public int PreOpenDelayMS { get; protected set; }
        public int PostOpenDelayMS { get; protected set; }
    }

    public class ProceduralProfileBlobPrototype : ProceduralProfileBasicRangePrototype
    {
        public ProceduralUsePowerContextPrototype SummonToadPower { get; protected set; }
        public PrototypeId ToadPrototype { get; protected set; }

        private const int ToadSummoned = 0;
        private const int NoToad = 1;

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            int stateVal = ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            if (stateVal == NoToad)
                ownerController.AddPowersToPicker(powerPicker, SummonToadPower);
        }
    }

    public class ProceduralProfileRangedHotspotDropperPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype RangedPower { get; protected set; }
        public ProceduralUsePowerContextPrototype HotspotPower { get; protected set; }
        public WanderContextPrototype HotspotDroppingMovement { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            ownerController.AddPowersToPicker(powerPicker, RangedPower);
            ownerController.AddPowersToPicker(powerPicker, HotspotPower);
        }
    }

    public class ProceduralProfileTeamUpPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralFlankContextPrototype FlankTarget { get; protected set; }
        public bool IsRanged { get; protected set; }
        public MoveToContextPrototype MoveToMaster { get; protected set; }
        public TeleportContextPrototype TeleportToMasterIfTooFarAway { get; protected set; }
        public int MaxDistToMasterBeforeTeleport { get; protected set; }
        public ProceduralUsePowerContextPrototype[] TeamUpPowerProgressionPowers { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            Agent agent = ownerController.Owner;
            if (agent == null) return;

            if (TeamUpPowerProgressionPowers.HasValue())
            {
                PrototypeId activePowerRef = ownerController.ActivePowerRef;
                foreach (var proceduralPower in TeamUpPowerProgressionPowers)
                {
                    var powerContext = proceduralPower?.PowerContext;
                    if (powerContext == null || powerContext.Power == PrototypeId.Invalid) continue;

                    PrototypeId powerRef = proceduralPower.PowerContext.Power;
                    var rank = agent.GetPowerRank(powerRef);
                    bool isActivePower = activePowerRef != PrototypeId.Invalid && powerRef == activePowerRef;
                    if (rank > 0 || isActivePower)
                        ownerController.AddPowersToPicker(powerPicker, proceduralPower);
                }
            }
        }

    }

    public class ProceduralProfilePetPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype PetFollow { get; protected set; }
        public TeleportContextPrototype TeleportToMasterIfTooFarAway { get; protected set; }
        public int MaxDistToMasterBeforeTeleport { get; protected set; }
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralFlankContextPrototype FlankTarget { get; protected set; }
        public bool IsRanged { get; protected set; }
    }

    public class ProceduralProfilePetFidgetPrototype : ProceduralProfilePetPrototype
    {
        public ProceduralUsePowerContextPrototype Fidget { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            var powerContext = Fidget?.PowerContext;
            if (powerContext == null || powerContext.Power == PrototypeId.Invalid) return;

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            if (blackboard.PropertyCollection.HasProperty(new PropertyId(PropertyEnum.AIPowerStarted, powerContext.Power)))
                ownerController.AddPowersToPicker(powerPicker, Fidget);
            else
                base.PopulatePowerPicker(ownerController, powerPicker);
        }
    }

    public class ProceduralProfileSquirrelGirlSquirrelPrototype : ProceduralProfilePetPrototype
    {
        public ProceduralFlankContextPrototype FlankMaster { get; protected set; }
        public float DeadzoneAroundFlankTarget { get; protected set; }
    }

    public class ProceduralProfileRollingGrenadesPrototype : ProceduralAIProfilePrototype
    {
        public int MaxSpeedDegreeUpdateIntervalMS { get; protected set; }
        public int MinSpeedDegreeUpdateIntervalMS { get; protected set; }
        public int MovementSpeedVariance { get; protected set; }
        public int RandomDegreeFromForward { get; protected set; }
    }

    public class ProceduralProfileSquirrelTriplePrototype : ProceduralAIProfilePrototype
    {
        public int JumpDistanceMax { get; protected set; }
        public int JumpDistanceMin { get; protected set; }
        public DelayContextPrototype PauseSettings { get; protected set; }
        public int RandomDirChangeDegrees { get; protected set; }
    }

    public class ProceduralProfileFrozenOrbPrototype : ProceduralAIProfilePrototype
    {
        public int ShardBurstsPerSecond { get; protected set; }
        public int ShardsPerBurst { get; protected set; }
        public int ShardRotationSpeed { get; protected set; }
        public PrototypeId ShardPower { get; protected set; }
    }

    public class ProceduralProfileDrDoomPhase1Prototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public PrototypeId DeathStun { get; protected set; }
        public ProceduralUsePowerContextPrototype SummonTurretPowerAnimOnly { get; protected set; }
        public UsePowerContextPrototype SummonDoombotBlockades { get; protected set; }
        public UsePowerContextPrototype SummonDoombotInfernos { get; protected set; }
        public UsePowerContextPrototype SummonDoombotFlyers { get; protected set; }
        public ProceduralUsePowerContextPrototype SummonDoombotAnimOnly { get; protected set; }
        public CurveId SummonDoombotBlockadesCurve { get; protected set; }
        public CurveId SummonDoombotInfernosCurve { get; protected set; }
        public CurveId SummonDoombotFlyersCurve { get; protected set; }
        public int SummonDoombotWaveIntervalMS { get; protected set; }
        public ProceduralUsePowerContextPrototype SummonOrbSpawners { get; protected set; }
        public TriggerSpawnersContextPrototype SpawnTurrets { get; protected set; }
        public TriggerSpawnersContextPrototype SpawnDrDoomPhase2 { get; protected set; }
        public TriggerSpawnersContextPrototype DestroyTurretsOnDeath { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            ownerController.AddPowersToPicker(powerPicker, SummonTurretPowerAnimOnly);
            ownerController.AddPowersToPicker(powerPicker, SummonDoombotAnimOnly);
            ownerController.AddPowersToPicker(powerPicker, SummonOrbSpawners);
        }
    }

    public class ProceduralProfileDrDoomPhase2Prototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public PrototypeId DeathStun { get; protected set; }
        public PrototypeId StarryExpanseAnimOnly { get; protected set; }
        public TriggerSpawnersContextPrototype SpawnDrDoomPhase3 { get; protected set; }
        public TriggerSpawnersContextPrototype SpawnStarryExpanseAreas { get; protected set; }
    }

    public class ProceduralProfileDrDoomPhase3Prototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public RotateContextPrototype RapidFireRotate { get; protected set; }
        public ProceduralUsePowerContextPrototype RapidFirePower { get; protected set; }
        public PrototypeId StarryExpanseAnimOnly { get; protected set; }
        public ProceduralUsePowerContextPrototype CosmicSummonsAnimOnly { get; protected set; }
        public UsePowerContextPrototype CosmicSummonsPower { get; protected set; }
        public PrototypeId[] CosmicSummonEntities { get; protected set; }
        public CurveId CosmicSummonsNumEntities { get; protected set; }
        public TriggerSpawnersContextPrototype SpawnStarryExpanseAreas { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            ownerController.AddPowersToPicker(powerPicker, RapidFirePower);
            ownerController.AddPowersToPicker(powerPicker, CosmicSummonsAnimOnly);
        }
    }

    public class ProceduralProfileDrDoomPhase1OrbSpawnerPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveTo { get; protected set; }
        public ProceduralFlankContextPrototype Flank { get; protected set; }
    }

    public class ProceduralProfileMODOKPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralUsePowerContextPrototype TeleportToEntityPower { get; protected set; }
        public SelectEntityContextPrototype SelectTeleportTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype[] SummonProceduralPowers { get; protected set; }

        private enum State
        {
            TeleportToEntity,
            SummonProcedural,
            GenericProcedural,
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            int stateVal = ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            switch ((State)stateVal) {
                case State.TeleportToEntity: 
                    ownerController.AddPowersToPicker(powerPicker, TeleportToEntityPower);
                    break;
                case State.SummonProcedural:
                    if (SummonProceduralPowers.IsNullOrEmpty()) return;
                    ownerController.AddPowersToPicker(powerPicker, SummonProceduralPowers);
                    break;
                case State.GenericProcedural:
                    base.PopulatePowerPicker(ownerController, powerPicker);
                    break;
            }           
        }
    }

    public class ProceduralProfileSauronPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype MovementPower { get; protected set; }
        public ProceduralUsePowerContextPrototype LowHealthPower { get; protected set; }
        public float LowHealthPowerThresholdPct { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            PrototypeId startedPowerRef = ownerController.ActivePowerRef;
            if (startedPowerRef != PrototypeId.Invalid)
            {
                if (AddPowerToPickerIfStartedPowerIsContextPower(ownerController, LowHealthPower, startedPowerRef, powerPicker)
                    || AddPowerToPickerIfStartedPowerIsContextPower(ownerController, MovementPower, startedPowerRef, powerPicker))
                    return;
            }
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            long health = agent.Properties[PropertyEnum.Health];
            long maxHealth = agent.Properties[PropertyEnum.HealthMax];
            if (MathHelper.IsBelowOrEqual(health, maxHealth, LowHealthPowerThresholdPct))
                ownerController.AddPowersToPicker(powerPicker, LowHealthPower);
            ownerController.AddPowersToPicker(powerPicker, MovementPower);
            base.PopulatePowerPicker(ownerController, powerPicker);
        }
    }

    public class ProceduralProfileMandarinPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public ProceduralFlankContextPrototype FlankTarget { get; protected set; }
        public SelectEntityContextPrototype SequencePowerSelectTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype[] SequencePowers { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            if (SequencePowers.HasValue())
            {
                int powerIndex = ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
                if (powerIndex < 0 || powerIndex >= SequencePowers.Length) return;
                ProceduralUsePowerContextPrototype sequencePowerContext = SequencePowers[powerIndex];
                if (sequencePowerContext != null)
                    ownerController.AddPowersToPicker(powerPicker, sequencePowerContext);
            }
        }
    }

    public class ProceduralProfileSabretoothPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype MovementPower { get; protected set; }
        public SelectEntityContextPrototype MovementPowerSelectTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype LowHealthPower { get; protected set; }
        public float LowHealthPowerThresholdPct { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            PrototypeId startedPowerRef = ownerController.ActivePowerRef;
            if (startedPowerRef != PrototypeId.Invalid)
            {
                if (AddPowerToPickerIfStartedPowerIsContextPower(ownerController, LowHealthPower, startedPowerRef, powerPicker)
                    || AddPowerToPickerIfStartedPowerIsContextPower(ownerController, MovementPower, startedPowerRef, powerPicker))
                    return;
            }
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            long health = agent.Properties[PropertyEnum.Health];
            long maxHealth = agent.Properties[PropertyEnum.HealthMax];
            if (MathHelper.IsBelowOrEqual(health, maxHealth, LowHealthPowerThresholdPct))
                ownerController.AddPowersToPicker(powerPicker, LowHealthPower);

            CombatTargetFlags flags = CombatTargetFlags.IgnoreAggroDistance | CombatTargetFlags.IgnoreStealth;
            if (Combat.GetNumTargetsInRange(agent, 1600, 150.0f, CombatTargetType.Hostile, flags) > 0)
                ownerController.AddPowersToPicker(powerPicker, MovementPower);
            base.PopulatePowerPicker(ownerController, powerPicker);
        }
    }

    public class ProceduralProfileMeleePowerOnHitPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype PowerOnHit { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            if (PowerOnHit == null) return;
            PrototypeId startedPowerRef = ownerController.ActivePowerRef;
            if (startedPowerRef != PrototypeId.Invalid)
            {
                if (AddPowerToPickerIfStartedPowerIsContextPower(ownerController, PowerOnHit, startedPowerRef, powerPicker))
                    return;
            }
            else
            {
                int stateVal = ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
                if (stateVal != 0)
                    ownerController.AddPowersToPicker(powerPicker, PowerOnHit);
            }
            base.PopulatePowerPicker(ownerController, powerPicker);
        }
    }

    public class ProceduralProfileGrimReaperPrototype : ProfMeleePwrSpecialAtHealthPctPrototype
    {
        public ProceduralUsePowerContextPrototype TripleShotPower { get; protected set; }
        public ProceduralUsePowerContextPrototype SpecialPower { get; protected set; }
        public SelectEntityContextPrototype SpecialSelectTarget { get; protected set; }
        public int SpecialPowerChangeTgtIntervalMS { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            ownerController.AddPowersToPicker(powerPicker, TripleShotPower);
            ownerController.AddPowersToPicker(powerPicker, SpecialPower);
        }
    }

    public class ProceduralProfileMoleManPrototype : ProceduralProfileBasicRangePrototype
    {
        public TriggerSpawnersContextPrototype[] GigantoSpawners { get; protected set; }
        public ProceduralUsePowerContextPrototype MoloidInvasionPower { get; protected set; }
        public TriggerSpawnersContextPrototype MoloidInvasionSpawner { get; protected set; }
        public ProceduralUsePowerContextPrototype SummonGigantoAnimPower { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            ownerController.AddPowersToPicker(powerPicker, MoloidInvasionPower);
            ownerController.AddPowersToPicker(powerPicker, SummonGigantoAnimPower);
            base.PopulatePowerPicker(ownerController, powerPicker);
        }
    }

    public class ProceduralProfileVenomPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralFlankContextPrototype FlankTarget { get; protected set; }
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public UsePowerContextPrototype VenomMad { get; protected set; }
        public float VenomMadThreshold1 { get; protected set; }
        public float VenomMadThreshold2 { get; protected set; }
    }

    public class ProceduralProfileVanityPetPrototype : ProceduralProfileWithTargetPrototype
    {
        public MoveToContextPrototype PetFollow { get; protected set; }
        public TeleportContextPrototype TeleportToMasterIfTooFarAway { get; protected set; }
        public int MinTimerWhileNotMovingFidgetMS { get; protected set; }
        public int MaxTimerWhileNotMovingFidgetMS { get; protected set; }
        public float MaxDistToMasterBeforeTeleport { get; protected set; }
    }

    public class ProceduralProfileDoopPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralFleeContextPrototype Flee { get; protected set; }
        public ProceduralUsePowerContextPrototype DisappearPower { get; protected set; }
        public int LifeTimeMinMS { get; protected set; }
        public int LifeTimeMaxMS { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            ownerController.AddPowersToPicker(powerPicker, DisappearPower);
        }
    }

    public class ProceduralProfileGorgonPrototype : ProceduralProfileWithAttackPrototype
    {
        public RotateContextPrototype RotateInStoneGaze { get; protected set; }
        public ProceduralUsePowerContextPrototype StoneGaze { get; protected set; }
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            ownerController.AddPowersToPicker(powerPicker, StoneGaze);
        }
    }

    public class ProceduralProfileBotAIPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public SelectEntityContextPrototype SelectTargetItem { get; protected set; }
        public WanderContextPrototype WanderMovement { get; protected set; }
        public ProceduralUsePowerContextPrototype[] SlottedAbilities { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
        }
    }

    public class ProceduralProfileBullseyePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public ProceduralFlankContextPrototype FlankTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype MarkForDeath { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            ownerController.AddPowersToPicker(powerPicker, MarkForDeath);
        }
    }

    public class ProceduralProfileRhinoPrototype : ProceduralProfileBasicMeleePrototype
    {
    }

    public class ProceduralProfileBlackCatPrototype : ProceduralProfileBasicMeleePrototype
    {
        public ProceduralUsePowerContextPrototype TumblePower { get; protected set; }
        public ProceduralUsePowerContextPrototype TumbleComboPower { get; protected set; }
        public SelectEntityContextPrototype SelectEntityForTumbleCombo { get; protected set; }

        private enum State
        {
            Default,
            ComboPower
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            var blackboard = ownerController.Blackboard;
            int stateVal = blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            PrototypeId startedPowerRef = ownerController.ActivePowerRef;

            if (startedPowerRef != PrototypeId.Invalid)
            {
                if (AddPowerToPickerIfStartedPowerIsContextPower(ownerController, TumbleComboPower, startedPowerRef, powerPicker))
                    return;
            }
            else
            {
                if ((State)stateVal == State.ComboPower)
                {
                    var selectionContext = new SelectEntity.SelectEntityContext(ownerController, SelectEntityForTumbleCombo);
                    WorldEntity selectedEntity = SelectEntity.DoSelectEntity(selectionContext);
                    if (selectedEntity != null)
                    {
                        if (SelectEntity.RegisterSelectedEntity(ownerController, selectedEntity, selectionContext.SelectEntityType) == false)
                            return;
                        ownerController.AddPowersToPicker(powerPicker, TumbleComboPower);
                        return;
                    }
                    else 
                        blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = (int)State.Default;
                }
            }

            ownerController.AddPowersToPicker(powerPicker, TumblePower);
            base.PopulatePowerPicker(ownerController, powerPicker);
        }
    }

    public class ProceduralProfileControlledMobOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public MoveToContextPrototype ControlFollow { get; protected set; }
        public TeleportContextPrototype TeleportToMasterIfTooFarAway { get; protected set; }
        public float MaxDistToMasterBeforeTeleport { get; protected set; }
        public int MaxDistToMasterBeforeFollow { get; protected set; }
    }

    public class ProceduralProfileLivingLaserPrototype : ProceduralProfileBasicRangePrototype
    {
        public ProceduralUsePowerContextPrototype SweepingBeamPowerClock { get; protected set; }
        public ProceduralUsePowerContextPrototype SweepingBeamPowerCounterClock { get; protected set; }
        public RotateContextPrototype SweepingBeamClock { get; protected set; }
        public RotateContextPrototype SweepingBeamCounterClock { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            ownerController.AddPowersToPicker(powerPicker, SweepingBeamPowerClock);
            ownerController.AddPowersToPicker(powerPicker, SweepingBeamPowerCounterClock);
        }
    }

    public class ProceduralProfileMeleeFlockerPrototype : ProceduralProfileWithAttackPrototype
    {
        public FlockContextPrototype FlockContext { get; protected set; }
        public PrototypeId FleeOnAllyDeathOverride { get; protected set; }
    }

    public class ProceduralProfileLizardBossPrototype : ProceduralProfileBasicMeleePrototype
    {
        public ProceduralUsePowerContextPrototype LizardSwarmPower { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            ownerController.AddPowersToPicker(powerPicker, LizardSwarmPower);
        }
    }

    public class ProceduralProfilePetDirectedPrototype : ProceduralProfilePetPrototype
    {
        public ProceduralUsePowerContextPrototype[] DirectedPowers { get; protected set; }
    }

    public class ProceduralProfileLokiPhase1Prototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public ProceduralFlankContextPrototype FlankTarget { get; protected set; }
    }

    public class ProceduralProfileLokiPhase2Prototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype InverseRings { get; protected set; }
        public PrototypeId InverseRingsTargetedVFXOnly { get; protected set; }
        public PrototypeId LokiBossSafeZoneKeyword { get; protected set; }
        public PrototypeId InverseRingsVFXRemoval { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            ownerController.AddPowersToPicker(powerPicker, InverseRings);
            base.PopulatePowerPicker(ownerController, powerPicker);
        }
    }

    public class ProceduralProfileDrStrangeProjectionPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralUsePowerContextPrototype[] ProjectionPowers { get; protected set; }
        public ProceduralFlankContextPrototype FlankMaster { get; protected set; }
        public float DeadzoneAroundFlankTarget { get; protected set; }
        public int FlankToMasterDelayMS { get; protected set; }
        public TeleportContextPrototype TeleportToMasterIfTooFarAway { get; protected set; }
        public int MaxDistToMasterBeforeTeleport { get; protected set; }
    }

    public class ProceduralProfileEyeOfAgamottoPrototype : ProceduralProfileStationaryTurretPrototype
    {
        public RotateContextPrototype IdleRotation { get; protected set; }
    }

    public class MistressOfMagmaTeleportDestPrototype : Prototype
    {
        public SelectEntityContextPrototype DestinationSelector { get; protected set; }
        public PrototypeId ImmunityBoost { get; protected set; }
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

    public class ProceduralProfileWithEnragePrototype : ProceduralProfileWithAttackPrototype
    {
        public int EnrageTimerInMinutes { get; protected set; }
        public ProceduralUsePowerContextPrototype EnragePower { get; protected set; }
        public float EnrageTimerAvatarSearchRadius { get; protected set; }
        public ProceduralUsePowerContextPrototype[] PostEnragePowers { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            int stateVal = ownerController.Blackboard.PropertyCollection[PropertyEnum.AIEnrageState];
            if (stateVal == 3)
                ownerController.AddPowersToPicker(powerPicker, PostEnragePowers);
        }
    }

    public class ProceduralProfileSyncAttackPrototype : ProceduralProfileBasicMeleePrototype
    {
        public ProceduralSyncAttackContextPrototype[] SyncAttacks { get; protected set; }

        private const int IDPropertiesLength = 4;
        private readonly PropertyEnum[] IDProperties = new PropertyEnum[IDPropertiesLength]
        {
            PropertyEnum.AICustomEntityId1, 
            PropertyEnum.AICustomEntityId2, 
            PropertyEnum.AICustomEntityId3, 
            PropertyEnum.AICustomEntityId4
        };

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            PrototypeId startedPowerRef = ownerController.ActivePowerRef;
            if (startedPowerRef != PrototypeId.Invalid)
                foreach (var syncAttack in SyncAttacks)
                {
                    var powerContext = syncAttack?.LeaderPower?.PowerContext;
                    if (powerContext != null && powerContext.Power == startedPowerRef)
                    {
                        ownerController.AddPowersToPicker(powerPicker, syncAttack.LeaderPower);
                        return;
                    }
                }

            Agent leader = ownerController.Owner;
            if (leader == null) return;
            Game game = leader.Game;
            if (game == null) return;
            var blackboard = ownerController.Blackboard;

            int syncAttackIndex = GetRandomSyncAttackIndex(blackboard, game);
            if (syncAttackIndex < 0 || syncAttackIndex >= IDPropertiesLength) return;

            ulong targetId = blackboard.PropertyCollection[IDProperties[syncAttackIndex]];            
            var target = game.EntityManager.GetEntity<Agent>(targetId);
            if (target == null) return;

            var syncAttackProto = SyncAttacks[syncAttackIndex];
            if (syncAttackProto == null) return;

            var targetController = target.AIController;
            if (targetController == null) return;

            var targetBlackboard = targetController.Blackboard;
            if (targetController.Brain is not ProceduralAI targetAI) return;

            ulong tempEntityId = targetBlackboard.PropertyCollection[PropertyEnum.AIRawTargetEntityID];
            targetBlackboard.PropertyCollection[PropertyEnum.AIRawTargetEntityID] = leader.Id;

            var targetEntityPowerProto = syncAttackProto.TargetEntityPower.As<ProceduralUsePowerContextPrototype>();
            if (ValidateUsePowerContext(targetController, targetAI, targetEntityPowerProto.PowerContext))
            {
                blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = syncAttackIndex;
                ownerController.AddPowersToPicker(powerPicker, syncAttackProto.LeaderPower);
            }
            targetBlackboard.PropertyCollection[PropertyEnum.AIRawTargetEntityID] = tempEntityId;
        }

        private int GetRandomSyncAttackIndex(BehaviorBlackboard blackboard, Game game)
        {
            if (SyncAttacks.IsNullOrEmpty()) return -1;

            if (IDPropertiesLength < SyncAttacks.Length)
            {
                ProceduralAI.Logger.Warn($"AI has more SyncAttacks than supported! Max supported is {IDPropertiesLength}! AI: {ToString()}");
                return -1;
            }

            List<int> syncAttackIndices = new ();
            for (int i = 0; i < IDPropertiesLength && i < SyncAttacks.Length; i++)
            {
                ulong targetId = blackboard.PropertyCollection[IDProperties[i]];
                Agent target = game.EntityManager.GetEntity<Agent>(targetId);
                if (target != null && target.IsDead == false)
                    syncAttackIndices.Add(i);
            }
            if (syncAttackIndices.Count == 0) return -1;

            int randomIndex = game.Random.Next(0, syncAttackIndices.Count);
            return syncAttackIndices[randomIndex];
        }

    }

    public class ProceduralProfileBrimstonePrototype : ProceduralProfileWithEnragePrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public MoveToContextPrototype MoveIntoMeleeRange { get; protected set; }
        public ProceduralUsePowerContextPrototype MeleePower { get; protected set; }
        public PrototypeId HellfireProtoRef { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            ownerController.AddPowersToPicker(powerPicker, MeleePower);
        }
    }

    public class ProceduralProfileSlagPrototype : ProceduralProfileWithEnragePrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype PrimaryPower { get; protected set; }
    }

    public class ProceduralProfileMonolithPrototype : ProceduralProfileWithEnragePrototype
    {
        public PrototypeId ObeliskKeyword { get; protected set; }
        public PrototypeId[] ObeliskDamageMonolithPowers { get; protected set; }
        public PrototypeId DisableShield { get; protected set; }
    }

    public class ProceduralProfileHellfirePrototype : ProceduralProfileWithEnragePrototype
    {
        public ProceduralFlankContextPrototype FlankTarget { get; protected set; }
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype PrimaryPower { get; protected set; }
        public PrototypeId BrimstoneProtoRef { get; protected set; }
        public ProceduralUsePowerContextPrototype SpecialPower { get; protected set; }
        public PrototypeId SpecialSummonPower { get; protected set; }
        public int SpecialPowerNumSummons { get; protected set; }
        public float SpecialPowerMaxRadius { get; protected set; }
        public float SpecialPowerMinRadius { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            ownerController.AddPowersToPicker(powerPicker, PrimaryPower);
            ownerController.AddPowersToPicker(powerPicker, SpecialPower);
        }
    }

    public class ProceduralProfileMistressOfMagmaPrototype : ProceduralProfileWithEnragePrototype
    {
        public ProceduralFlankContextPrototype FlankTarget { get; protected set; }
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype BombDancePower { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            ownerController.AddPowersToPicker(powerPicker, BombDancePower);
        }
    }

    public class ProceduralProfileSurturPrototype : ProceduralProfileWithEnragePrototype
    {
        public PrototypeId FirePillarPower { get; protected set; }
        public int FirePillarMinCooldownMS { get; protected set; }
        public int FirePillarMaxCooldownMS { get; protected set; }
        public int FirePillarPowerMaxTargets { get; protected set; }
        public PrototypeId PowerUnlockBrimstone { get; protected set; }
        public PrototypeId PowerUnlockHellfire { get; protected set; }
        public PrototypeId PowerUnlockMistress { get; protected set; }
        public PrototypeId PowerUnlockMonolith { get; protected set; }
        public PrototypeId PowerUnlockSlag { get; protected set; }
        public PrototypeId MiniBossBrimstone { get; protected set; }
        public PrototypeId MiniBossHellfire { get; protected set; }
        public PrototypeId MiniBossMistress { get; protected set; }
        public PrototypeId MiniBossMonolith { get; protected set; }
        public PrototypeId MiniBossSlag { get; protected set; }
    }

    public class ProceduralProfileSurturPortalPrototype : ProceduralProfileNoMoveNoSensePrototype
    {
        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            if (GenericProceduralPowers.HasValue()) 
            {
                int index = ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
                if (index >= 0 && index < GenericProceduralPowers.Length)
                    ownerController.AddPowersToPicker(powerPicker, GenericProceduralPowers[index]);
            }
        }
    }

    public class ProceduralProfileObeliskHealerPrototype : ProceduralProfileBasicMeleePrototype
    {
        public PrototypeId[] ObeliskTargets { get; protected set; }
    }

    public class ProceduralProfileObeliskPrototype : ProceduralProfileNoMoveDefaultSensoryPrototype
    {
        public PrototypeId DeadEntityForDetonateIslandPower { get; protected set; }
        public PrototypeId DetonateIslandPower { get; protected set; }
        public PrototypeId FullyHealedPower { get; protected set; }
    }

    public class ProceduralProfileFireGiantChaserPrototype : ProceduralProfileBasicMeleePrototype
    {
        public ProceduralUsePowerContextPrototype MarkTargetPower { get; protected set; }
        public PrototypeId MarkTargetVFXRemoval { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            ownerController.AddPowersToPicker(powerPicker, PrimaryPower);
            ownerController.AddPowersToPicker(powerPicker, MarkTargetPower);
        }
    }

    public class ProceduralProfileMissionAllyPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralFlankContextPrototype FlankTarget { get; protected set; }
        public MoveToContextPrototype MoveToAvatarAlly { get; protected set; }
        public TeleportContextPrototype TeleportToAvatarAllyIfTooFarAway { get; protected set; }
        public int MaxDistToAvatarAllyBeforeTele { get; protected set; }
        public bool IsRanged { get; protected set; }
        public float AvatarAllySearchRadius { get; protected set; }
    }

    public class ProceduralProfileLOSRangedPrototype : ProceduralProfileBasicRangePrototype
    {
        public ProceduralUsePowerContextPrototype LOSChannelPower { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            ownerController.AddPowersToPicker(powerPicker, LOSChannelPower);
            base.PopulatePowerPicker(ownerController, powerPicker);
        }
    }

    public class ProceduralProfileRedSkullOneShotPrototype : ProceduralProfileWithAttackPrototype
    {
        public PrototypeId[] HulkBustersToActivate { get; protected set; }
        public ProceduralUsePowerContextPrototype ActivateHulkBusterAnimOnly { get; protected set; }
        public float HulkBusterHealthThreshold1 { get; protected set; }
        public float HulkBusterHealthThreshold2 { get; protected set; }
        public float HulkBusterHealthThreshold3 { get; protected set; }
        public float HulkBusterHealthThreshold4 { get; protected set; }
        public PrototypeId WeaponsCrate { get; protected set; }
        public ProceduralUsePowerContextPrototype[] WeaponsCratesAnimOnlyPowers { get; protected set; }
        public MoveToContextPrototype MoveToWeaponsCrate { get; protected set; }
        public PrototypeId WeaponCrate1UnlockPower { get; protected set; }
        public PrototypeId WeaponCrate2UnlockPower { get; protected set; }
        public PrototypeId WeaponCrate3UnlockPower { get; protected set; }
        public PrototypeId WeaponCrate4UnlockPower { get; protected set; }
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public ProceduralFlankContextPrototype FlankTarget { get; protected set; }
    }

    public class ProceduralProfileHulkBusterOSPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public PrototypeId RedSkullAxis { get; protected set; }
        public ProceduralUsePowerContextPrototype ShieldRedSkull { get; protected set; }
        public ProceduralUsePowerContextPrototype DeactivatedAnimOnly { get; protected set; }
        public ProceduralUsePowerContextPrototype ActivatingAnimOnly { get; protected set; }
    }

    public class ProceduralProfileSymbioteDrainPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralUsePowerContextPrototype SymbiotePower1 { get; protected set; }
        public ProceduralUsePowerContextPrototype SymbiotePower2 { get; protected set; }
        public ProceduralUsePowerContextPrototype SymbiotePower3 { get; protected set; }
    }

    public class ProceduralProfileOnslaughtPrototype : ProceduralProfileWithEnragePrototype
    {
        public PrototypeId PlatformMarkerLeft { get; protected set; }
        public PrototypeId PlatformMarkerCenter { get; protected set; }
        public PrototypeId PlatformMarkerRight { get; protected set; }
        public ProceduralUsePowerContextPrototype PsionicBlastLeft { get; protected set; }
        public ProceduralUsePowerContextPrototype PsionicBlastCenter { get; protected set; }
        public ProceduralUsePowerContextPrototype PsionicBlastRight { get; protected set; }
        public ProceduralUsePowerContextPrototype SpikeDanceVFXOnly { get; protected set; }
        public ProceduralUsePowerContextPrototype PrisonBeamPowerCenter { get; protected set; }
        public ProceduralUsePowerContextPrototype PrisonPowerCenter { get; protected set; }
        public ProceduralUsePowerContextPrototype SpikeDanceSingleVFXOnly { get; protected set; }
        public PrototypeId CallSentinelPower { get; protected set; }
        public PrototypeId CallSentinelPowerVFXOnly { get; protected set; }
        public float SummonPowerThreshold1 { get; protected set; }
        public float SummonPowerThreshold2 { get; protected set; }
        public ProceduralUsePowerContextPrototype PrisonBeamPowerLeft { get; protected set; }
        public ProceduralUsePowerContextPrototype PrisonBeamPowerRight { get; protected set; }
        public ProceduralUsePowerContextPrototype PrisonPowerLeft { get; protected set; }
        public ProceduralUsePowerContextPrototype PrisonPowerRight { get; protected set; }
        public PrototypeId PrisonKeyword { get; protected set; }
        public PrototypeId CenterPlatformKeyword { get; protected set; }
        public PrototypeId RightPlatformKeyword { get; protected set; }
        public PrototypeId LeftPlatformKeyword { get; protected set; }

        private enum PlatformEnum
        {
            Left,
            Right,
            Center            
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            ownerController.AddPowersToPicker(powerPicker, PsionicBlastLeft);
            ownerController.AddPowersToPicker(powerPicker, PsionicBlastCenter);
            ownerController.AddPowersToPicker(powerPicker, PsionicBlastRight);
            ownerController.AddPowersToPicker(powerPicker, SpikeDanceVFXOnly);
            ownerController.AddPowersToPicker(powerPicker, SpikeDanceSingleVFXOnly);

            BehaviorBlackboard blackboard = ownerController.Blackboard;            
            ulong entityId = blackboard.PropertyCollection[PropertyEnum.AICustomEntityId4];
            if (entityId != 0)
            {
                PlatformEnum randomPlatformIndex = (PlatformEnum)(int)blackboard.PropertyCollection[PropertyEnum.AICustomStateVal2];
                switch (randomPlatformIndex)
                {
                    case PlatformEnum.Left:
                        ownerController.AddPowersToPicker(powerPicker, PrisonBeamPowerLeft);
                        break;
                    case PlatformEnum.Right:
                        ownerController.AddPowersToPicker(powerPicker, PrisonBeamPowerRight);
                        break;
                    case PlatformEnum.Center:
                        ownerController.AddPowersToPicker(powerPicker, PrisonBeamPowerCenter);
                        break;
                    default:
                        ProceduralAI.Logger.Warn($"Onslaught has an invalid platform enum stored in AICustomStateVal2: Index = {randomPlatformIndex}");
                        break;
                }
            }
            else
            {
                ownerController.AddPowersToPicker(powerPicker, PrisonPowerCenter);
                ownerController.AddPowersToPicker(powerPicker, PrisonPowerLeft);
                ownerController.AddPowersToPicker(powerPicker, PrisonPowerRight);
            }
        }
    }

    public class ProcProfileSpikeDanceControllerPrototype : ProceduralAIProfilePrototype
    {
        public PrototypeId Onslaught { get; protected set; }
        public PrototypeId SpikeDanceMob { get; protected set; }
        public int MaxSpikeDanceActivations { get; protected set; }
        public float SpikeDanceMobSearchRadius { get; protected set; }
    }

    public class ProceduralProfileSpikeDanceMobPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralUsePowerContextPrototype SpikeDanceMissile { get; protected set; }
    }

    public class ProceduralProfileNullifierPrototype : ProceduralProfileNoMoveNoSensePrototype
    {
        public PrototypeId ShieldEngineerKeyword { get; protected set; }
        public ProceduralUsePowerContextPrototype BeamPower { get; protected set; }
        public PrototypeId NullifierAntiShield { get; protected set; }
    }

    public class ProceduralProfileStrangeCauldronPrototype : ProceduralProfileNoMoveNoSensePrototype
    {
        public PrototypeId KaeciliusPrototype { get; protected set; }
    }

    public class ProceduralProfileShieldEngineerPrototype : ProceduralProfileMissionAllyPrototype
    {
        public PrototypeId[] PsychicNullifierTargets { get; protected set; }   // VectorPrototypeRefPtr AgentPrototype
        public ProceduralUsePowerContextPrototype ChargeNullifierPower { get; protected set; }
        public float NullifierSearchRadius { get; protected set; }
        public PrototypeId NullifierAntiShield { get; protected set; }
    }

    public class ProcProfileNullifierAntiShieldPrototype : ProceduralProfileWithEnragePrototype
    {
        public PrototypeId[] Nullifiers { get; protected set; }    // VectorPrototypeRefPtr AgentPrototype
        public PrototypeId ShieldDamagePower { get; protected set; }
        public PrototypeId ShieldEngineerSpawner { get; protected set; }
        public float SpawnerSearchRadius { get; protected set; }
    }

    public class ProceduralProfileMadameHydraPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public ProceduralFlankContextPrototype FlankTarget { get; protected set; }
        public PrototypeId SummonHydraPower { get; protected set; }
        public PrototypeId InvulnerablePower { get; protected set; }
        public ProceduralUsePowerContextPrototype TeleportPower { get; protected set; }
        public int SummonHydraMinCooldownMS { get; protected set; }
        public int SummonHydraMaxCooldownMS { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            int stateVal = ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            if (stateVal == 0)
                ownerController.AddPowersToPicker(powerPicker, TeleportPower);
        }
    }

    public class ProceduralProfileStarktechSentinelPrototype : ProceduralProfileWithEnragePrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype SummonSentinels { get; protected set; }
        public float SummonPowerThreshold1 { get; protected set; }
        public float SummonPowerThreshold2 { get; protected set; }
    }

    public class ProceduralProfileKingpinPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype SummonElektra { get; protected set; }
        public ProceduralUsePowerContextPrototype SummonBullseye { get; protected set; }
        public float SummonElektraThreshold { get; protected set; }
        public float SummonBullseyeThreshold { get; protected set; }
    }

    public class ProceduralProfilePowerRestrictedPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralFlankContextPrototype FlankTarget { get; protected set; }
        public bool IsRanged { get; protected set; }
        public ProceduralUsePowerContextPrototype RestrictedModeStartPower { get; protected set; }
        public ProceduralUsePowerContextPrototype RestrictedModeEndPower { get; protected set; }
        public ProceduralUsePowerContextPrototype[] RestrictedModeProceduralPowers { get; protected set; }
        public int RestrictedModeMinCooldownMS { get; protected set; }
        public int RestrictedModeMaxCooldownMS { get; protected set; }
        public int RestrictedModeTimerMS { get; protected set; }
        public bool NoMoveInRestrictedMode { get; protected set; }

        private enum State
        {
            Default,
            StartPower = 1,
            ProceduralPowers = 2,
            EndPower = 3,
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            int stateVal = ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            if ((State)stateVal == State.ProceduralPowers)
                ownerController.AddPowersToPicker(powerPicker, RestrictedModeProceduralPowers);
            else
                base.PopulatePowerPicker(ownerController, powerPicker);
        }
    }

    public class ProceduralProfileUltronEMPPrototype : ProceduralProfileNoMoveNoSensePrototype
    {
        public ProceduralUsePowerContextPrototype EMPPower { get; protected set; }
    }

    public class ProcProfileQuicksilverTeamUpPrototype : ProceduralProfileTeamUpPrototype
    {
        public ProceduralUsePowerContextPrototype SpecialMovementPower { get; protected set; }
    }

    public class ProceduralProfileSkrullNickFuryPrototype : ProceduralProfileRangeFlankerPrototype
    {
        public ProceduralUsePowerContextPrototype OpenRocketCratePower { get; protected set; }
        public ProceduralUsePowerContextPrototype OpenMinigunCratePower { get; protected set; }
        public ProceduralUsePowerContextPrototype UseRocketPower { get; protected set; }
        public ProceduralUsePowerContextPrototype UseMinigunPower { get; protected set; }
        public MoveToContextPrototype MoveToCrate { get; protected set; }
        public ProceduralUsePowerContextPrototype CommandTurretPower { get; protected set; }
        public int CratePowerUseCount { get; protected set; }
        public ProceduralUsePowerContextPrototype DiscardWeaponPower { get; protected set; }
        public PrototypeId CrateUsedState { get; protected set; }

        private enum State
        {
            Default,
            OpenRocketCrate,
            OpenMinigunCrate,
            UseRocket,
            UseMinigun,
            DiscardWeapon
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            int stateVal = ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            switch ((State)stateVal)
            {
                case State.OpenRocketCrate:
                    ownerController.AddPowersToPicker(powerPicker, OpenRocketCratePower);
                    break;                
                case State.OpenMinigunCrate:
                    ownerController.AddPowersToPicker(powerPicker, OpenMinigunCratePower);
                    break;
                case State.UseRocket:
                    ownerController.AddPowersToPicker(powerPicker, UseRocketPower);
                    break;
                case State.UseMinigun:
                    ownerController.AddPowersToPicker(powerPicker, UseMinigunPower);
                    break;
                case State.DiscardWeapon:
                    ownerController.AddPowersToPicker(powerPicker, DiscardWeaponPower);
                    break;
                default:
                    ownerController.AddPowersToPicker(powerPicker, CommandTurretPower);
                    base.PopulatePowerPicker(ownerController, powerPicker);
                    break;
            }            
        }
    }

    public class ProceduralProfileNickFuryTurretPrototype : ProceduralProfileRotatingTurretWithTargetPrototype
    {
        public ProceduralUsePowerContextPrototype SpecialCommandPower { get; protected set; }
        public PrototypeId SkrullNickFuryRef { get; protected set; }

        private enum State
        {
            Default,
            Ready,
            SpecialPower
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            int stateVal = ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            if ((State)stateVal == State.SpecialPower)
                ownerController.AddPowersToPicker(powerPicker, SpecialCommandPower);
            else
                base.PopulatePowerPicker(ownerController, powerPicker);
        }
    }

    public class ProceduralProfileKaeciliusPrototype : ProceduralProfileBasicRangePrototype
    {
        public ProceduralPowerWithSpecificTargetsPrototype[] HotspotSpawners { get; protected set; }
        public ProceduralThresholdPowerContextPrototype FalseDeathPower { get; protected set; }
        public ProceduralUsePowerContextPrototype HealFinalFormPower { get; protected set; }
        public ProceduralUsePowerContextPrototype DeathPreventerPower { get; protected set; }
        public PrototypeId Cauldron { get; protected set; }
    }

    public class ProceduralProfileMeleeRevengePrototype : ProceduralProfileBasicMeleePrototype
    {
        public ProceduralUsePowerContextPrototype RevengePower { get; protected set; }
        public PrototypeId RevengeSupport { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            int stateVal = ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            if (stateVal == 1)
                ownerController.AddPowersToPicker(powerPicker, RevengePower);
        }
    }

    public class ProceduralProfileRangedRevengePrototype : ProceduralProfileBasicRangePrototype
    {
        public ProceduralUsePowerContextPrototype RevengePower { get; protected set; }
        public PrototypeId RevengeSupport { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            int stateVal = ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            if (stateVal == 1)
                ownerController.AddPowersToPicker(powerPicker, RevengePower);
        }
    }

    public class ProceduralProfileTaserTrapPrototype : ProceduralProfileWithTargetPrototype
    {
        public PrototypeId TaserHotspot { get; protected set; }
    }

    public class ProceduralProfileVulturePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype LungePower { get; protected set; }
        public int MaxLungeActivations { get; protected set; }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {            
            ownerController.AddPowersToPicker(powerPicker, LungePower);
            base.PopulatePowerPicker(ownerController, powerPicker);
        }
    }
}
