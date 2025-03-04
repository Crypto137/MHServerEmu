using MHServerEmu.Core.Collections;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.Behavior.ProceduralAI;
using MHServerEmu.Games.Behavior.StaticAI;
using MHServerEmu.Games.Behavior;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Properties;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Games.Powers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.GameData.Prototypes
{

    public class ProceduralProfileWithAttackPrototype : ProceduralProfileWithTargetPrototype
    {
        public int AttackRateMaxMS { get; protected set; }
        public int AttackRateMinMS { get; protected set; }
        public ProceduralUsePowerContextPrototype[] GenericProceduralPowers { get; protected set; }
        public ProceduralUseAffixPowerContextPrototype AffixSettings { get; protected set; }

        public override void Init(Agent agent)
        {
            base.Init(agent);
            Game game = agent.Game;
            if (game == null) return;
            AIController ownerController = agent.AIController;
            if (ownerController == null) return;

            long nextAttackThinkTime = (long)game.CurrentTime.TotalMilliseconds + game.Random.Next(AttackRateMinMS, AttackRateMaxMS);
            ownerController.Blackboard.PropertyCollection[PropertyEnum.AIProceduralNextAttackTime] = nextAttackThinkTime;
            InitPowers(agent, GenericProceduralPowers);
        }

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
            long currentTime, Picker<ProceduralUsePowerContextPrototype> powerPicker, bool affixPower = true)
        {
            Agent agent = ownerController.Owner;
            if (agent == null)
            {
                ProceduralAI.Logger.Warn($"[{agent}]");
                return StaticBehaviorReturnType.None;
            }

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            StaticBehaviorReturnType contextResult = StaticBehaviorReturnType.None;

            if (proceduralAI.GetState(0) == UsePower.Instance)
            {
                PrototypeId powerStartedRef = ownerController.ActivePowerRef;
                if (powerStartedRef == PrototypeId.Invalid)
                {
                    ProceduralAI.Logger.Warn($"In UsePower state, but no power was recorded as started! agent=[{agent}]");
                    return StaticBehaviorReturnType.Failed;
                }

                ProceduralUsePowerContextPrototype proceduralUsePowerProto = null;
                UsePowerContextPrototype powerContextProtoToRun = null;
                int numPowers = powerPicker.GetNumElements();
                for (int i = 0; i < numPowers; ++i)
                {
                    if (powerPicker.GetElementAt(i, out proceduralUsePowerProto) == false)
                    {
                        ProceduralAI.Logger.Warn($"failed to GetElementAt i=[{i}] agent=[{agent}]");
                        return StaticBehaviorReturnType.Failed;
                    }
                    if (proceduralUsePowerProto == null)
                    {
                        ProceduralAI.Logger.Warn($"proceduralUsePowerProto is NULL! agent=[{agent}]");
                        return StaticBehaviorReturnType.Failed;
                    }
                    UsePowerContextPrototype powerContextProto = proceduralUsePowerProto.PowerContext;
                    if (powerContextProto == null)
                    {
                        ProceduralAI.Logger.Warn($"powerContextProto is NULL! agent=[{agent}]");
                        return StaticBehaviorReturnType.Failed;
                    }
                    if (powerContextProto.Power != PrototypeId.Invalid && powerStartedRef == powerContextProto.Power)
                    {
                        powerContextProtoToRun = powerContextProto;
                        break;
                    }
                }

                if (powerContextProtoToRun == null)
                {
                    PrototypeId syncPowerRef = blackboard.PropertyCollection[PropertyEnum.AISyncAttackTargetPower];
                    if (syncPowerRef != PrototypeId.Invalid)
                    {
                        proceduralUsePowerProto = GameDatabase.GetPrototype<ProceduralUsePowerContextPrototype>(syncPowerRef);
                        if (proceduralUsePowerProto == null)
                        {
                            ProceduralAI.Logger.Warn($"proceduralUsePowerProto is NULL! agent=[{agent}]");
                            return StaticBehaviorReturnType.Failed;
                        }
                        powerContextProtoToRun = proceduralUsePowerProto.PowerContext;
                        if (powerContextProtoToRun == null || powerContextProtoToRun.Power == PrototypeId.Invalid)
                        {
                            ProceduralAI.Logger.Warn($"powerContextProtoToRun or Power is NULL! agent=[{agent}]");
                            return StaticBehaviorReturnType.Failed;
                        }
                        if (powerContextProtoToRun.Power != powerStartedRef)
                        {
                            ProceduralAI.Logger.Warn($"SyncPower doesn't match power running!\n AI: {agent}\n Power Running: {GameDatabase.GetFormattedPrototypeName(powerStartedRef)}");
                            return StaticBehaviorReturnType.Failed;
                        }
                    }
                }

                if (proceduralUsePowerProto == null || powerContextProtoToRun == null)
                {
                    ProceduralAI.Logger.Warn($"proceduralUsePowerProto or powerContextProtoToRun is NULL! powerStartedRef=[{powerStartedRef}] numPowers=[{numPowers}] agent=[{agent}]");
                    return StaticBehaviorReturnType.Failed;
                }

                contextResult = HandleUsePowerContext(ownerController, proceduralAI, random, currentTime, powerContextProtoToRun, proceduralUsePowerProto);
            }
            else if (proceduralAI.GetState(0) == UseAffixPower.Instance)
            {
                contextResult = HandleUseAffixPowerContext(ownerController, proceduralAI, random, currentTime);
            }
            else if (currentTime >= blackboard.PropertyCollection[PropertyEnum.AIProceduralNextAttackTime])
            {
                if (affixPower && agent.Properties.HasProperty(PropertyEnum.EnemyBoost))
                {
                    if (AffixSettings == null)
                    {
                        ProceduralAI.Logger.Warn($"Agent [{agent}] has enemy affix(es), but no AffixSettings data in its procedural profile!");
                        return StaticBehaviorReturnType.Failed;
                    }
                    powerPicker.Add(null, AffixSettings.PickWeight);
                }

                while (powerPicker.Empty() == false)
                {
                    powerPicker.PickRemove(out var randomProceduralPowerProto);
                    if (affixPower && randomProceduralPowerProto == null)
                    {
                        contextResult = HandleUseAffixPowerContext(ownerController, proceduralAI, random, currentTime);
                    }
                    else
                    {
                        UsePowerContextPrototype randomPowerContextProto = randomProceduralPowerProto.PowerContext;
                        if (randomPowerContextProto == null || randomPowerContextProto.Power == PrototypeId.Invalid)
                        {
                            ProceduralAI.Logger.Warn($"Agent [{agent}] has a NULL PowerContext or PowerContext.Power");
                            return StaticBehaviorReturnType.Failed;
                        }

                        if (randomPowerContextProto.HasDifficultyTierRestriction((PrototypeId)agent.Properties[PropertyEnum.DifficultyTier]))
                            continue;

                        contextResult = HandleUsePowerCheckCooldown(ownerController, proceduralAI, random, currentTime, randomPowerContextProto, randomProceduralPowerProto);
                        if (contextResult == StaticBehaviorReturnType.Completed)
                            break;
                    }

                    if (contextResult == StaticBehaviorReturnType.Running || contextResult == StaticBehaviorReturnType.Completed)
                        break;
                }
            }

            proceduralAI.LastPowerResult = contextResult;
            return contextResult;
        }

        public StaticBehaviorReturnType HandleUsePowerCheckCooldown(AIController ownerController, ProceduralAI proceduralAI, GRandom random,
            long currentTime, UsePowerContextPrototype powerContext, ProceduralUsePowerContextPrototype proceduralPowerContext)
        {
            var collection = ownerController.Blackboard.PropertyCollection;
            long agroTime = (long)collection[PropertyEnum.AIAggroTime] + (long)collection[PropertyEnum.AIInitialCooldownMSForPower, powerContext.Power];
            if (currentTime >= agroTime)
            {
                if (currentTime >= collection[PropertyEnum.AIProceduralPowerSpecificCDTime, powerContext.Power])
                {
                    if (OnPowerPicked(ownerController, proceduralPowerContext))
                    {
                        StaticBehaviorReturnType contextResult = HandleUsePowerContext(ownerController, proceduralAI, random, currentTime, powerContext, proceduralPowerContext);
                        OnPowerAttempted(ownerController, proceduralPowerContext, contextResult);
                        return contextResult;
                    }
                }
            }
            return StaticBehaviorReturnType.Failed;
        }
       
        public StaticBehaviorReturnType HandleUseAffixPowerContext(AIController ownerController, ProceduralAI proceduralAI, GRandom random, long currentTime)
        {
            BehaviorBlackboard blackboard = ownerController.Blackboard;
            IStateContext useAffixPowerContext = new UseAffixPowerContext(ownerController, null);
            var contextResult = proceduralAI.HandleContext(UseAffixPower.Instance, useAffixPowerContext, AffixSettings);
            UpdateNextAttackThinkTime(blackboard, random, currentTime, contextResult);
            return contextResult;
        }

        protected override StaticBehaviorReturnType HandleUsePowerContext(AIController ownerController, ProceduralAI proceduralAI, GRandom random,
            long currentTime, UsePowerContextPrototype powerContext, ProceduralContextPrototype proceduralContext = null)
        {
            var contextResult = base.HandleUsePowerContext(ownerController, proceduralAI, random, currentTime, powerContext, proceduralContext);
            UpdateNextAttackThinkTime(ownerController.Blackboard, random, currentTime, contextResult);
            return contextResult;
        }

        public override void ProcessInterrupts(AIController ownerController, BehaviorInterruptType interrupt)
        {
            if (interrupt.HasFlag(BehaviorInterruptType.Alerted))
            {
                var proceduralAI = ownerController.Brain;
                if (proceduralAI == null) return;
                if (ownerController.Senses.GetCurrentTarget() != null)
                    proceduralAI.ClearOverrideBehavior(OverrideType.Full);
            }
        }

        private void UpdateNextAttackThinkTime(BehaviorBlackboard blackboard, GRandom random, long currentTime, StaticBehaviorReturnType contextResult)
        {
            if (contextResult == StaticBehaviorReturnType.Completed)
                blackboard.PropertyCollection[PropertyEnum.AIProceduralNextAttackTime] = currentTime + random.Next(AttackRateMinMS, AttackRateMaxMS);
        }

        protected static bool IsProceduralPowerContextOnCooldown(BehaviorBlackboard blackboard, ProceduralUsePowerContextPrototype powerContext, long currentTime)
        {
            if (powerContext.PowerContext == null
                || powerContext.PowerContext.Power == PrototypeId.Invalid) return false;

            var specificTimeProp = new PropertyId(PropertyEnum.AIProceduralPowerSpecificCDTime, powerContext.PowerContext.Power);
            var collection = blackboard.PropertyCollection;
            if (collection.HasProperty(specificTimeProp))
                return currentTime < collection[specificTimeProp];
            else
            {
                int agroTime = collection[PropertyEnum.AIAggroTime] + collection[PropertyEnum.AIInitialCooldownMSForPower, powerContext.PowerContext.Power];
                return currentTime < agroTime;
            }
        }

        public virtual bool OnPowerPicked(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            if (powerContext.TargetSwitch != null)
            {
                var selectionContext = new SelectEntity.SelectEntityContext(ownerController, powerContext.TargetSwitch.SelectTarget);
                WorldEntity selectedEntity = SelectEntity.DoSelectEntity(selectionContext);
                if (selectedEntity == null)
                {
                    if (powerContext.TargetSwitch.UsePowerOnCurTargetIfSwitchFails) return true;
                    return false;
                }

                if (powerContext.TargetSwitch.SwitchPermanently == false)
                {
                    WorldEntity targetEntity = ownerController.TargetEntity;
                    if (targetEntity != null)
                        ownerController.Blackboard.PropertyCollection[PropertyEnum.AIProceduralPowerPrevTargetId] = targetEntity.Id;
                }

                if (SelectEntity.RegisterSelectedEntity(ownerController, selectedEntity, selectionContext.SelectionType) == false)
                    return false;
            }

            return true;
        }

        public virtual void OnPowerAttempted(AIController ownerController, ProceduralUsePowerContextPrototype powerContext, StaticBehaviorReturnType contextResult) { }
        public virtual void OnPowerStarted(AIController ownerController, ProceduralUsePowerContextPrototype powerContext) { }
        public virtual void OnPowerEnded(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            if (powerContext.TargetSwitch != null && powerContext.TargetSwitch.SwitchPermanently == false)
            {
                var selectTargetContext = powerContext.TargetSwitch.SelectTarget;
                if (selectTargetContext == null) return;

                var blackboard = ownerController.Blackboard;
                var prevTargetId = blackboard.PropertyCollection[PropertyEnum.AIProceduralPowerPrevTargetId];
                var prevTarget = ownerController.Game.EntityManager.GetEntity<WorldEntity>(prevTargetId);
                if (prevTarget == null) return;

                var targetType = CombatTargetType.Hostile;
                switch (selectTargetContext.PoolType)
                {
                    case SelectEntityPoolType.PotentialEnemiesOfAgent:
                        targetType = CombatTargetType.Hostile;
                        break;
                    case SelectEntityPoolType.PotentialAlliesOfAgent:
                        targetType = CombatTargetType.Ally;
                        break;
                }

                if (Combat.ValidTarget(ownerController.Game, ownerController.Owner, prevTarget, targetType, false))
                    SelectEntity.RegisterSelectedEntity(ownerController, prevTarget, selectTargetContext.SelectEntityType);
            }
        }

        public virtual void OnPowerEnded(AIController ownerController, ProceduralUseAffixPowerContextPrototype powerContext) { }

    }

    public class ProceduralProfileStationaryTurretPrototype : ProceduralProfileWithAttackPrototype
    {
        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Game game = agent.Game;
            if (game == null) return;
            long currentTime = (long)game.CurrentTime.TotalMilliseconds;

            if (HandleOverrideBehavior(ownerController)) return;

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false
                && proceduralAI.PartialOverrideBehavior == null) return;

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true);

            HandleRotateToTarget(agent, target);
        }
    }

    public class ProceduralProfileRotatingTurretWithTargetPrototype : ProceduralProfileWithAttackPrototype
    {
        public RotateContextPrototype Rotate { get; protected set; }

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Game game = agent.Game;
            if (game == null) return;
            long currentTime = (long)game.CurrentTime.TotalMilliseconds;

            if (HandleOverrideBehavior(ownerController)) return;

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false
                && proceduralAI.PartialOverrideBehavior == null) return;

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
            {
                proceduralAI.PushSubstate();
                HandleContext(proceduralAI, ownerController, Rotate);
                proceduralAI.PopSubstate();
            }
        }
    }

    public class ProceduralProfileBasicMeleePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype PrimaryPower { get; protected set; }

        public override void Init(Agent agent)
        {
            base.Init(agent);
            InitPower(agent, PrimaryPower);
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

        public override void Init(Agent agent)
        {
            base.Init(agent);
            InitPower(agent, Power1);
            InitPower(agent, Power2);
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

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Game game = agent.Game;
            if (game == null) return;
            long currentTime = (long)game.CurrentTime.TotalMilliseconds;

            if (HandleOverrideBehavior(ownerController)) return;

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false
                && proceduralAI.PartialOverrideBehavior == null) return;

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running) return;

            proceduralAI.PartialOverrideBehavior?.Think(ownerController);

            DefaultRangedMovement(proceduralAI, ownerController, agent, target, MoveToTarget, OrbitTarget);
        }
    }

    public class ProceduralProfileAlternateRange2Prototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public ProceduralFlankContextPrototype FlankTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype Power1 { get; protected set; }
        public ProceduralUsePowerContextPrototype Power2 { get; protected set; }
        public ProceduralUsePowerContextPrototype PowerSwap { get; protected set; }

        public override void Init(Agent agent)
        {
            base.Init(agent);
            InitPower(agent, Power1);
            InitPower(agent, Power2);
            InitPower(agent, PowerSwap);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            int stateVal = ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            if (stateVal == 0)
                ownerController.AddPowersToPicker(powerPicker, Power1);
            else if (stateVal == 1)
                ownerController.AddPowersToPicker(powerPicker, Power2);
            ownerController.AddPowersToPicker(powerPicker, PowerSwap);
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

            if (HandleOverrideBehavior(ownerController)) return;

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false
                && proceduralAI.PartialOverrideBehavior == null) return;

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running) return;

            proceduralAI.PartialOverrideBehavior?.Think(ownerController);

            DefaultRangedFlankerMovement(proceduralAI, ownerController, agent, target, currentTime, MoveToTarget, FlankTarget);
        }

        public override void OnPowerEnded(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            base.OnPowerEnded(ownerController, powerContext);
            if (powerContext == PowerSwap)
            {
                var collection = ownerController.Blackboard.PropertyCollection;
                int stateVal = collection[PropertyEnum.AICustomStateVal1];
                    collection[PropertyEnum.AICustomStateVal1] = stateVal ^ 1;
            }
        }

    }

    public class ProceduralProfileMultishotRangePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype MultishotPower { get; protected set; }
        public int NumShots { get; protected set; }
        public bool RetargetPerShot { get; protected set; }

        public override void Init(Agent agent)
        {
            base.Init(agent);
            InitPower(agent, MultishotPower);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            ownerController.AddPowersToPicker(powerPicker, MultishotPower);
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

            if (HandleOverrideBehavior(ownerController)) return;

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false
                && proceduralAI.PartialOverrideBehavior == null) return;

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            int numShotsProp = blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            if (numShotsProp > 0)
            {
                if (MultishotLooper(ownerController, proceduralAI, agent, game.Random, currentTime, numShotsProp) == StaticBehaviorReturnType.Running)
                    return;
            }
            else
            {
                GRandom random = game.Random;
                Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
                PopulatePowerPicker(ownerController, powerPicker);
                StaticBehaviorReturnType powerResult = HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true);
                if (powerResult == StaticBehaviorReturnType.Running) return;
                if (powerResult == StaticBehaviorReturnType.Completed)
                {
                    numShotsProp = 1;
                    blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = numShotsProp;

                    if (MultishotLooper(ownerController, proceduralAI, agent, game.Random, currentTime, numShotsProp) == StaticBehaviorReturnType.Running)
                        return;
                }
            }

            proceduralAI.PartialOverrideBehavior?.Think(ownerController);

            DefaultRangedMovement(proceduralAI, ownerController, agent, target, MoveToTarget, OrbitTarget);
        }

        protected StaticBehaviorReturnType MultishotLooper(AIController ownerController, ProceduralAI proceduralAI, Agent agent, GRandom random, long currentTime, int numShotsProp)
        {
            var collection = ownerController.Blackboard.PropertyCollection;

            if (numShotsProp >= NumShots)
            {
                collection.RemoveProperty(PropertyEnum.AICustomStateVal1);
                return StaticBehaviorReturnType.Completed;
            }

            while (numShotsProp < NumShots)
            {
                var powerResult = HandleUsePowerContext(ownerController, proceduralAI, random, currentTime, MultishotPower.PowerContext);
                if (powerResult == StaticBehaviorReturnType.Running)
                    return powerResult;
                else if (powerResult == StaticBehaviorReturnType.Completed)
                {
                    ++numShotsProp;
                    if (numShotsProp >= NumShots)
                        collection.RemoveProperty(PropertyEnum.AICustomStateVal1);
                    else
                    {
                        collection.AdjustProperty(1, PropertyEnum.AICustomStateVal1);
                        if (RetargetPerShot)
                        {
                            var selectionContext = new SelectEntity.SelectEntityContext(ownerController, SelectTarget);
                            WorldEntity selectedEntity = SelectEntity.DoSelectEntity(selectionContext);
                            if (selectedEntity != null && selectedEntity != agent)
                                SelectEntity.RegisterSelectedEntity(ownerController, selectedEntity, selectionContext.SelectionType);
                        }
                    }
                }
                else if (powerResult == StaticBehaviorReturnType.Failed)
                {
                    collection.RemoveProperty(PropertyEnum.AICustomStateVal1);
                    return powerResult;
                }
            }

            return StaticBehaviorReturnType.Completed;
        }

    }

    public class ProceduralProfileMultishotFlankerPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralFlankContextPrototype FlankTarget { get; protected set; }
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype MultishotPower { get; protected set; }
        public int NumShots { get; protected set; }
        public bool RetargetPerShot { get; protected set; }

        public override void Init(Agent agent)
        {
            base.Init(agent);
            InitPower(agent, MultishotPower);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            ownerController.AddPowersToPicker(powerPicker, MultishotPower);
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

            if (HandleOverrideBehavior(ownerController)) return;

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false
                && proceduralAI.PartialOverrideBehavior == null) return;

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            int numShotsProp = blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            if (numShotsProp > 0)
            {
                if (MultishotLooper(ownerController, proceduralAI, agent, game.Random, currentTime, numShotsProp) == StaticBehaviorReturnType.Running)
                    return;
            }
            else
            {
                GRandom random = game.Random;
                Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
                PopulatePowerPicker(ownerController, powerPicker);
                StaticBehaviorReturnType powerResult = HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true);
                if (powerResult == StaticBehaviorReturnType.Running) return;
                if (powerResult == StaticBehaviorReturnType.Completed)
                {
                    numShotsProp = 1;
                    blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = numShotsProp;

                    if (MultishotLooper(ownerController, proceduralAI, agent, game.Random, currentTime, numShotsProp) == StaticBehaviorReturnType.Running)
                        return;
                }
            }

            proceduralAI.PartialOverrideBehavior?.Think(ownerController);

            DefaultRangedFlankerMovement(proceduralAI, ownerController, agent, target, currentTime, MoveToTarget, FlankTarget);
        }

        protected StaticBehaviorReturnType MultishotLooper(AIController ownerController, ProceduralAI proceduralAI, Agent agent, GRandom random, long currentTime, int numShotsProp)
        {
            var collection = ownerController.Blackboard.PropertyCollection;

            while (numShotsProp < NumShots)
            {
                var powerResult = HandleUsePowerContext(ownerController, proceduralAI, random, currentTime, MultishotPower.PowerContext);
                if (powerResult == StaticBehaviorReturnType.Running)
                    return powerResult;
                else if (powerResult == StaticBehaviorReturnType.Completed)
                {
                    ++numShotsProp;
                    if (numShotsProp >= NumShots)
                        collection.RemoveProperty(PropertyEnum.AICustomStateVal1);
                    else
                    {
                        collection.AdjustProperty(1, PropertyEnum.AICustomStateVal1);
                        if (RetargetPerShot)
                        {
                            var selectionContext = new SelectEntity.SelectEntityContext(ownerController, SelectTarget);
                            WorldEntity selectedEntity = SelectEntity.DoSelectEntity(selectionContext);
                            if (selectedEntity != null && selectedEntity != agent)
                                SelectEntity.RegisterSelectedEntity(ownerController, selectedEntity, selectionContext.SelectionType);
                        }
                    }
                }
                else if (powerResult == StaticBehaviorReturnType.Failed)
                {
                    collection.RemoveProperty(PropertyEnum.AICustomStateVal1);
                    return powerResult;
                }
            }

            return StaticBehaviorReturnType.Completed;
        }
    }

    public class ProceduralProfileMultishotHiderPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralUsePowerContextPrototype HidePower { get; protected set; }
        public ProceduralUsePowerContextPrototype MultishotPower { get; protected set; }
        public int NumShots { get; protected set; }
        public bool RetargetPerShot { get; protected set; }

        public override void Init(Agent agent)
        {
            base.Init(agent);
            InitPower(agent, HidePower);
            InitPower(agent, MultishotPower);
        }

        private enum State
        {
            Hide,
            Multishot,
            Unhide
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            int stateVal = ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal2];
            if ((State)stateVal == State.Multishot)
                ownerController.AddPowersToPicker(powerPicker, MultishotPower);
            else
                ownerController.AddPowersToPicker(powerPicker, HidePower);
            base.PopulatePowerPicker(ownerController, powerPicker);
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

            if (HandleOverrideBehavior(ownerController)) return;

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false
                && proceduralAI.PartialOverrideBehavior == null) return;

            var powerContext = HidePower?.PowerContext;
            if (powerContext == null || powerContext.Power == PrototypeId.Invalid) return;
            Power hidePower = agent.GetPower(powerContext.Power);
            if (hidePower == null) return;

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            int state = blackboard.PropertyCollection[PropertyEnum.AICustomStateVal2];
            switch ((State)state)
            {
                case State.Hide:
                    GRandom random = game.Random;
                    Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
                    PopulatePowerPicker(ownerController, powerPicker);
                    if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Completed)
                        blackboard.PropertyCollection[PropertyEnum.AICustomStateVal2] = (int)State.Multishot;
                    break;

                case State.Multishot:
                    int numShotsProp = blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
                    random = game.Random;
                    if (numShotsProp > 0)
                        MultishotLooper(ownerController, proceduralAI, agent, random, currentTime, numShotsProp);
                    else
                    {
                        powerPicker = new(random);
                        PopulatePowerPicker(ownerController, powerPicker);
                        if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Completed)
                        {
                            numShotsProp = 1;
                            blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = numShotsProp;
                            MultishotLooper(ownerController, proceduralAI, agent, game.Random, currentTime, numShotsProp);
                        }
                    }
                    break;

                case State.Unhide:
                    random = game.Random;
                    if (HandleUsePowerContext(ownerController, proceduralAI, random, currentTime, HidePower.PowerContext) == StaticBehaviorReturnType.Completed)
                        blackboard.PropertyCollection[PropertyEnum.AICustomStateVal2] = (int)State.Hide;
                    break;
            }
        }

        protected StaticBehaviorReturnType MultishotLooper(AIController ownerController, ProceduralAI proceduralAI, Agent agent, GRandom random, long currentTime, int numShotsProp)
        {
            var collection = ownerController.Blackboard.PropertyCollection;

            while (numShotsProp < NumShots)
            {
                var powerResult = HandleUsePowerContext(ownerController, proceduralAI, random, currentTime, MultishotPower.PowerContext);
                if (powerResult == StaticBehaviorReturnType.Running)
                    return powerResult;
                else if (powerResult == StaticBehaviorReturnType.Completed)
                {
                    ++numShotsProp;
                    if (numShotsProp >= NumShots)
                    {
                        collection.RemoveProperty(PropertyEnum.AICustomStateVal1);
                        collection[PropertyEnum.AICustomStateVal2] = (int)State.Unhide;
                    }
                    else
                    {
                        collection.AdjustProperty(1, PropertyEnum.AICustomStateVal1);
                        if (RetargetPerShot)
                        {
                            var selectionContext = new SelectEntity.SelectEntityContext(ownerController, SelectTarget);
                            WorldEntity selectedEntity = SelectEntity.DoSelectEntity(selectionContext);
                            if (selectedEntity != null && selectedEntity != agent)
                                SelectEntity.RegisterSelectedEntity(ownerController, selectedEntity, selectionContext.SelectionType);
                        }
                    }
                }
                else if (powerResult == StaticBehaviorReturnType.Failed)
                {
                    collection.RemoveProperty(PropertyEnum.AICustomStateVal1);
                    collection[PropertyEnum.AICustomStateVal2] = (int)State.Unhide;
                    return powerResult;
                }
            }

            return StaticBehaviorReturnType.Completed;
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

        public override void Init(Agent agent)
        {
            base.Init(agent);
            InitPower(agent, PrimaryPower);
            InitPower(agent, ExtraSpeedPower);
            InitPower(agent, SpeedRemovalPower);
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

            if (HandleOverrideBehavior(ownerController)) return;

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false
                && proceduralAI.PartialOverrideBehavior == null) return;

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running) return;

            proceduralAI.PartialOverrideBehavior?.Think(ownerController);

            BehaviorBlackboard blackboard = ownerController.Blackboard;

            if (proceduralAI.LastPowerResult == StaticBehaviorReturnType.Completed)
            {
                if (blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] == 1)
                {
                    if (SpeedRemovalPower == null || SpeedRemovalPower.Power == PrototypeId.Invalid) return;
                    ownerController.AttemptActivatePower(SpeedRemovalPower.Power, agent.Id, agent.RegionLocation.Position);
                    blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = 0;
                }
            }

            if (proceduralAI.GetState(0) != Orbit.Instance && target != null)
            {
                if (blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] == 0)
                {
                    float distabceFromTargetSq = Vector3.Distance2D(agent.RegionLocation.Position, target.RegionLocation.Position);
                    distabceFromTargetSq = Math.Abs(distabceFromTargetSq - (agent.Bounds.Radius + target.Bounds.Radius));

                    if (distabceFromTargetSq > DistanceFromTargetForSpeedBonus)
                    {
                        if (ExtraSpeedPower == null || ExtraSpeedPower.Power == PrototypeId.Invalid) return;
                        ownerController.AttemptActivatePower(ExtraSpeedPower.Power, agent.Id, agent.RegionLocation.Position);
                        blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = 1;
                    }
                }

                HandleMovementContext(proceduralAI, ownerController, agent.Locomotor, MoveToTarget, false, out var movetoResult, null);
                if (movetoResult == StaticBehaviorReturnType.Running || movetoResult == StaticBehaviorReturnType.Completed) return;
            }

            HandleMovementContext(proceduralAI, ownerController, agent.Locomotor, OrbitTarget, false, out _);
        }

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

        public override void Init(Agent agent)
        {
            base.Init(agent);
            InitPower(agent, PrimaryPower);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            ownerController.AddPowersToPicker(powerPicker, PrimaryPower);
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

            if (HandleOverrideBehavior(ownerController)) return;

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false
                && proceduralAI.PartialOverrideBehavior == null) return;

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running) return;

            proceduralAI.PartialOverrideBehavior?.Think(ownerController);

            DefaultRangedFlankerMovement(proceduralAI, ownerController, agent, target, currentTime, MoveToTarget, FlankTarget);
        }
    }

    public class ProceduralProfileSkirmisherPrototype : ProceduralProfileWithAttackPrototype
    {
        public WanderContextPrototype SkirmishMovement { get; protected set; }
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype PrimaryPower { get; protected set; }
        public float MoveToSpeedBonus { get; protected set; }

        public override void Init(Agent agent)
        {
            base.Init(agent);
            InitPower(agent, PrimaryPower);
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

            if (HandleOverrideBehavior(ownerController)) return;

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false
                && proceduralAI.PartialOverrideBehavior == null) return;

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running) return;

            proceduralAI.PartialOverrideBehavior?.Think(ownerController);

            IAIState state = proceduralAI.GetState(0);
            bool toMove = state == MoveTo.Instance;
            if (toMove == false && state != Wander.Instance)
                toMove = !IsProceduralPowerContextOnCooldown(ownerController.Blackboard, PrimaryPower, currentTime);

            if (toMove)
            {
                if (proceduralAI.GetState(0) != MoveTo.Instance)
                    agent.Properties.AdjustProperty(MoveToSpeedBonus, PropertyEnum.MovementSpeedIncrPct);
                if (HandleMovementContext(proceduralAI, ownerController, agent.Locomotor, MoveToTarget, false, out var moveToResult) == false) return;
                if (moveToResult == StaticBehaviorReturnType.Running || moveToResult == StaticBehaviorReturnType.Completed)
                {
                    if (moveToResult == StaticBehaviorReturnType.Completed)
                        agent.Properties.AdjustProperty(-MoveToSpeedBonus, PropertyEnum.MovementSpeedIncrPct);
                    return;
                }
            }

            HandleMovementContext(proceduralAI, ownerController, agent.Locomotor, SkirmishMovement, false, out _);
        }

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

        public override void Init(Agent agent)
        {
            base.Init(agent);
            InitPower(agent, MeleePower);
            InitPower(agent, RangedPower);
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

            if (HandleOverrideBehavior(ownerController)) return;

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false
                && proceduralAI.PartialOverrideBehavior == null) return;

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            GRandom random = game.Random;

            PrototypeId startedPowerRef = ownerController.ActivePowerRef;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            if (MeleePower == null) return;
            UsePowerContextPrototype meleePowerContext = MeleePower.PowerContext;
            if (meleePowerContext == null) return;
            PrototypeId meleePowerRef = meleePowerContext.Power;
            if (meleePowerRef == PrototypeId.Invalid) return;
            bool startedMeleePower = meleePowerRef == startedPowerRef;

            if (startedPowerRef == PrototypeId.Invalid || startedMeleePower)
            {
                Power power = agent.GetPower(meleePowerRef);
                if (power == null) return;

                if (startedMeleePower || power.GetCooldownTimeRemaining() <= TimeSpan.Zero)
                {
                    ownerController.AddPowersToPicker(powerPicker, MeleePower);
                    var contextResult = HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker);
                    if (contextResult == StaticBehaviorReturnType.Running) return;
                    else if (contextResult == StaticBehaviorReturnType.Failed)
                    {
                        if (target != null)
                        {
                            float distanceSq = Vector3.DistanceSquared2D(agent.RegionLocation.Position, target.RegionLocation.Position);
                            if (distanceSq <= MaxDistToMoveIntoMelee * MaxDistToMoveIntoMelee)
                            {
                                if (HandleMovementContext(proceduralAI, ownerController, agent.Locomotor, MoveToTarget, true, out var movementResult) == false)
                                    return;
                                if (movementResult == StaticBehaviorReturnType.Running)
                                {
                                    blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = 1;
                                    return;
                                }
                            }
                        }
                    }
                    else if (contextResult == StaticBehaviorReturnType.Completed)
                        blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = 0;
                }
            }

            if (blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] != 1)
            {
                powerPicker.Clear();
                ownerController.AddPowersToPicker(powerPicker, RangedPower);
                if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker) == StaticBehaviorReturnType.Running)
                    return;
            }

            proceduralAI.PartialOverrideBehavior?.Think(ownerController);

            DefaultRangedMovement(proceduralAI, ownerController, agent, target, MoveToTarget, OrbitTarget);
        }
    }

    public class ProfMeleePwrSpecialAtHealthPctPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public float SpecialAtHealthChunkPct { get; protected set; }
        public UsePowerContextPrototype SpecialPowerAtHealthChunkPct { get; protected set; }

        public override void Init(Agent agent)
        {
            base.Init(agent);
            InitPower(agent, SpecialPowerAtHealthChunkPct);
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

            if (HandleOverrideBehavior(ownerController)) return;

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false
                && proceduralAI.PartialOverrideBehavior == null) return;

            if (CheckAgentHealthAndUsePower(ownerController, proceduralAI, currentTime, agent)) return;

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running) return;

            proceduralAI.PartialOverrideBehavior?.Think(ownerController);

            DefaultMeleeMovement(proceduralAI, ownerController, agent.Locomotor, target, MoveToTarget, OrbitTarget);
        }

        protected bool CheckAgentHealthAndUsePower(AIController ownerController, ProceduralAI proceduralAI, long currentTime, Agent agent)
        {
            Game game = agent.Game;
            if (game == null) return false;
            BehaviorBlackboard blackboard = ownerController.Blackboard;

            if (blackboard.PropertyCollection[PropertyEnum.AICustomStateVal2] == 1)
            {
                if (HandleUsePowerContext(ownerController, proceduralAI, game.Random, currentTime, SpecialPowerAtHealthChunkPct) == StaticBehaviorReturnType.Running)
                    return true;
                blackboard.PropertyCollection[PropertyEnum.AICustomStateVal2] = 2;
            }
            else
            {
                if (proceduralAI.GetState(0) != UsePower.Instance)
                {
                    long health = agent.Properties[PropertyEnum.Health];
                    long maxHealth = agent.Properties[PropertyEnum.HealthMax];
                    long healthChunk = MathHelper.RoundToInt64(SpecialAtHealthChunkPct * maxHealth);
                    int lastHealth = blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
                    int nextHealth = lastHealth + 1;

                    if (health <= (maxHealth - healthChunk * nextHealth))
                    {
                        StaticBehaviorReturnType powerResult = HandleUsePowerContext(ownerController, proceduralAI, game.Random, currentTime, SpecialPowerAtHealthChunkPct);
                        if (powerResult == StaticBehaviorReturnType.Running || powerResult == StaticBehaviorReturnType.Completed)
                        {
                            blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = nextHealth;
                            if (powerResult == StaticBehaviorReturnType.Running)
                            {
                                blackboard.PropertyCollection[PropertyEnum.AICustomStateVal2] = 1;
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

    }

    public class ProceduralProfileNoMoveDefaultSensoryPrototype : ProceduralProfileWithAttackPrototype
    {
        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Game game = agent.Game;
            if (game == null) return;
            long currentTime = (long)game.CurrentTime.TotalMilliseconds;

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false
                && proceduralAI.PartialOverrideBehavior == null) return;

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true);
        }
    }

    public class ProceduralProfileNoMoveSimplifiedSensoryPrototype : ProceduralProfileWithAttackPrototype
    {
        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Game game = agent.Game;
            if (game == null) return;
            long currentTime = (long)game.CurrentTime.TotalMilliseconds;

            WorldEntity target = ownerController.TargetEntity;
            if (CommonSimplifiedSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false) return;

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true);
        }
    }

    public class ProceduralProfileNoMoveSimplifiedAllySensoryPrototype : ProceduralProfileWithAttackPrototype
    {
        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Game game = agent.Game;
            if (game == null) return;
            long currentTime = (long)game.CurrentTime.TotalMilliseconds;

            WorldEntity target = ownerController.TargetEntity;
            if (CommonSimplifiedSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Ally) == false) return;

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true);
        }
    }

    public class ProfKillSelfAfterOnePowerNoMovePrototype : ProceduralProfileWithAttackPrototype
    {
        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Game game = agent.Game;
            if (game == null) return;
            long currentTime = (long)game.CurrentTime.TotalMilliseconds;

            WorldEntity target = ownerController.TargetEntity;
            if (CommonSimplifiedSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false) return;

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Completed) 
                agent.Kill(null);
        }
    }

    public class ProceduralProfileNoMoveNoSensePrototype : ProceduralProfileWithAttackPrototype
    {
        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Game game = agent.Game;
            if (game == null) return;
            long currentTime = (long)game.CurrentTime.TotalMilliseconds;

            if (ownerController.TargetEntity == null)
                SelectEntity.RegisterSelectedEntity(ownerController, agent, SelectEntityType.SelectTarget);

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true);
        }
    }

    public class ProceduralProfileBasicWanderPrototype : ProceduralProfileWithAttackPrototype
    {
        public WanderContextPrototype WanderMovement { get; protected set; }

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Game game = agent.Game;
            if (game == null) return;
            long currentTime = (long)game.CurrentTime.TotalMilliseconds;

            if (HandleOverrideBehavior(ownerController)) return;

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false
                && proceduralAI.PartialOverrideBehavior == null) return;

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running) return;

            proceduralAI.PartialOverrideBehavior?.Think(ownerController);

            HandleMovementContext(proceduralAI, ownerController, agent.Locomotor, WanderMovement, false, out _);
        }
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

        public override void Init(Agent agent)
        {
            base.Init(agent);

            var owningController = agent.AIController;
            if (owningController == null) return;
            var collection = owningController.Blackboard.PropertyCollection;
            collection[PropertyEnum.AIAggroDropRange] = AggroDropRadius;
            collection[PropertyEnum.AIAggroDropByLOSChance] = AggroDropByLOSChance / 100.0f;
            collection[PropertyEnum.AIAggroRangeHostile] = AggroRadius;

            InitPower(agent, PrimaryPower);
        }

        public override void Think(AIController ownerController)
        {
            Agent agent = ownerController.Owner;
            var collection = ownerController.Blackboard.PropertyCollection;
            var game = ownerController.Game;

            if (agent.HasPowerPreventionStatus())
            {
                agent.Locomotor.Stop();
                return;
            }

            EntityManager manager = game.EntityManager;
            bool validTarget = false;

            ulong targetId = collection[PropertyEnum.AIRawTargetEntityID];
            WorldEntity target = manager.GetEntity<WorldEntity>(targetId);
            if (target != null)
            {
                TimeSpan markTime = game.CurrentTime - (TimeSpan)collection[PropertyEnum.AIAttentionMarkTime];
                if (markTime < TimeSpan.FromMilliseconds(AttentionSpanMS))
                    validTarget = Combat.ValidTarget(game, agent, target, CombatTargetType.Hostile, true);
            }

            if (targetId != 0 && validTarget == false)
            {
                target = null;
                targetId = 0;
                collection[PropertyEnum.AIAssistOverrideTargetId] = 0;
            }

            if (validTarget == false)
            {
                targetId = Combat.GetClosestValidHostileTarget(agent, AggroRadius);
                if (targetId != 0)
                {
                    collection[PropertyEnum.AIRawTargetEntityID] = targetId;
                    collection[PropertyEnum.AIAttentionMarkTime] = game.CurrentTime;
                    target = manager.GetEntity<WorldEntity>(targetId);
                }
                else
                    target = null;
            }

            if (target != null)
            {
                var targetPosition = target.RegionLocation.Position;
                bool activatePower = agent.IsExecutingPower;
                IsInPositionForPowerResult positionResult = IsInPositionForPowerResult.Error;
                float powerRange = 0.0f;

                if (activatePower == false)
                {
                    var powerRef = PrimaryPower;
                    var power = agent.GetPower(powerRef);
                    if (power == null) return;

                    powerRange = power.GetRange();
                    positionResult = agent.IsInPositionForPower(power, target, targetPosition);
                    if (positionResult == IsInPositionForPowerResult.Success)
                    {
                        PowerUseResult activatePowerResult = agent.CanActivatePower(power, targetId, targetPosition);
                        if (activatePowerResult == PowerUseResult.Success)
                        {
                            agent.Locomotor.Stop();
                            activatePower = ownerController.AttemptActivatePower(powerRef, targetId, targetPosition);
                            if (activatePower)
                                collection[PropertyEnum.AIAttentionMarkTime] = game.CurrentTime;
                        }
                        else if (activatePowerResult == PowerUseResult.Cooldown || activatePowerResult == PowerUseResult.RestrictiveCondition)
                        {
                            agent.Locomotor.Stop();
                            return;
                        }
                    }
                }

                if (activatePower == false)
                {
                    if (positionResult != IsInPositionForPowerResult.NoPowerLOS || positionResult == IsInPositionForPowerResult.OutOfRange)
                    {
                        float range = Math.Max(powerRange, agent.Bounds.Radius + target.Bounds.Radius);
                        FastMoveToTarget.Update(agent, range);
                        return;
                    }
                }
            }
            else
            {
                collection[PropertyEnum.AIMoveToCurrentPathNodeIndex] = -1;

                if (agent.Properties.HasProperty(PropertyEnum.AIMoveToPathNodeSetGroup))
                    collection[PropertyEnum.AIMoveToPathNodeSetGroup] = agent.Properties[PropertyEnum.AIMoveToPathNodeSetGroup];
                else
                    collection[PropertyEnum.AIMoveToPathNodeSetGroup] = PathGroup;

                if (agent.Properties.HasProperty(PropertyEnum.AIMoveToPathNodeSetMethod))
                    collection[PropertyEnum.AIMoveToPathNodeSetMethod] = agent.Properties[PropertyEnum.AIMoveToPathNodeSetMethod];
                else
                    collection[PropertyEnum.AIMoveToPathNodeSetMethod] = (int)PathMethod;

                collection[PropertyEnum.AIMoveToPathNodeAdvanceThres] = PathThreshold;
                collection[PropertyEnum.AIMoveToPathNodeWalk] = agent.Properties[PropertyEnum.AIMoveToPathNodeWalk];

                FastMoveToPath.Update(agent);
                return;
            }
        }

    }

    public class ProceduralProfilePvPTowerPrototype : ProceduralProfileWithAttackPrototype
    {
        public SelectEntityContextPrototype SelectTarget2 { get; protected set; }
        public SelectEntityContextPrototype SelectTarget3 { get; protected set; }
        public SelectEntityContextPrototype SelectTarget4 { get; protected set; }

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Game game = agent.Game;
            if (game == null) return;
            long currentTime = (long)game.CurrentTime.TotalMilliseconds;

            var flags = CombatTargetFlags.IgnoreStealth;
            WorldEntity target = ownerController.TargetEntity;
            if (Combat.ValidTarget(game, agent, target, CombatTargetType.Hostile, true, flags) == false)
            {
                var selectionContext = new SelectEntity.SelectEntityContext(ownerController, SelectTarget);
                WorldEntity selectedEntity = SelectEntity.DoSelectEntity(selectionContext, flags);
                if (selectedEntity == null)
                {
                    var selection2Context = new SelectEntity.SelectEntityContext(ownerController, SelectTarget2);
                    selectedEntity = SelectEntity.DoSelectEntity(selection2Context, flags);
                    if (selectedEntity == null)
                    {
                        var selection3Context = new SelectEntity.SelectEntityContext(ownerController, SelectTarget3);
                        selectedEntity = SelectEntity.DoSelectEntity(selection3Context, flags);
                        if (selectedEntity == null)
                        {
                            var selection4Context = new SelectEntity.SelectEntityContext(ownerController, SelectTarget4);
                            selectedEntity = SelectEntity.DoSelectEntity(selection4Context, flags);
                        }
                    }
                }

                if (selectedEntity == null)
                    ownerController.ResetCurrentTargetState();
                else if (selectedEntity != target) 
                    SelectEntity.RegisterSelectedEntity(ownerController, selectedEntity, selectionContext.SelectionType);
            }

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true);
        }
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

        public override void Init(Agent agent)
        {
            base.Init(agent);
            InitPower(agent, PowerMeleeWithWeapon);
            InitPower(agent, PowerMeleeNoWeapon);
            InitPower(agent, PowerDropWeapon);
            InitPower(agent, PowerPickupWeapon);
        }
        private enum State
        {
            Attack = 0,
            Drop = 1,
            Pickup = 2,
            NoWeapon = 4
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

            if (HandleOverrideBehavior(ownerController)) return;

            var collection = ownerController.Blackboard.PropertyCollection;

            if (PowerDropWeapon?.PowerContext == null || PowerDropWeapon.PowerContext.Power == PrototypeId.Invalid) return;
            if (PowerPickupWeapon?.PowerContext == null || PowerPickupWeapon.PowerContext.Power == PrototypeId.Invalid) return;

            State state = (State)(int)collection[PropertyEnum.AICustomStateVal1];
            bool weaponSwap = true;

            WorldEntity target = ownerController.TargetEntity;

            if (state != State.NoWeapon)
            {
                PropertyId dropWeaponCooldownTimeId = new (PropertyEnum.AIProceduralPowerSpecificCDTime, PowerDropWeapon.PowerContext.Power);
                if (collection.HasProperty(dropWeaponCooldownTimeId))
                {                
                    long dropWeaponCooldownTime = collection[dropWeaponCooldownTimeId];
                    long pickupWeaponCooldownTime = collection[PropertyEnum.AIProceduralPowerSpecificCDTime, PowerPickupWeapon.PowerContext.Power];
                    weaponSwap = currentTime < Math.Max(dropWeaponCooldownTime, pickupWeaponCooldownTime);
                }
                else if (target != null)
                {
                    long initialDropCooldownTime = collection[PropertyEnum.AIInitialCooldownMSForPower, PowerDropWeapon.PowerContext.Power];
                    weaponSwap = currentTime < collection[PropertyEnum.AIAggroTime] + initialDropCooldownTime;
                }
            }

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new (random);

            if (state == State.Attack || state == State.NoWeapon)
            {
                if (weaponSwap)
                {
                    if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false
                        && proceduralAI.PartialOverrideBehavior == null) return;

                    if (collection[PropertyEnum.AILostWeapon])
                    {
                        if (PowerMeleeNoWeapon == null) return;
                        powerPicker.Add(PowerMeleeNoWeapon, PowerMeleeNoWeapon.PickWeight);
                    }
                    else
                    {
                        if (PowerMeleeWithWeapon == null) return;
                        powerPicker.Add(PowerMeleeWithWeapon, PowerMeleeWithWeapon.PickWeight);
                    }

                    StaticBehaviorReturnType powerResult = HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker);
                    if (powerResult == StaticBehaviorReturnType.Running) return;
                }
                else
                {
                    if (collection[PropertyEnum.AILostWeapon])
                    {
                        var selectionContext = new SelectEntity.SelectEntityContext(ownerController, SelectWeaponAsTarget);
                        WorldEntity selectedEntity = SelectEntity.DoSelectEntity(selectionContext);
                        if (selectedEntity != null && SelectEntity.RegisterSelectedEntity(ownerController, selectedEntity, selectionContext.SelectionType))
                            collection[PropertyEnum.AICustomStateVal1] = (int)State.Pickup;
                        else
                            collection[PropertyEnum.AICustomStateVal1] = (int)State.NoWeapon;
                    }
                    else
                    {
                        collection[PropertyEnum.AIRawTargetEntityID] = agent.Id;
                        collection[PropertyEnum.AICustomStateVal1] = (int)State.Drop;
                    }
                }
            }
            else if (state == State.Drop)
            {
                powerPicker.Add(PowerDropWeapon, PowerDropWeapon.PickWeight);
                StaticBehaviorReturnType powerResult = HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker);
                if (powerResult == StaticBehaviorReturnType.Running) return;
                else if (powerResult == StaticBehaviorReturnType.Completed)
                    collection[PropertyEnum.AICustomStateVal1] = (int)State.Attack;
            }
            else if (state == State.Pickup)
            {
                powerPicker.Add(PowerPickupWeapon, PowerPickupWeapon.PickWeight);
                StaticBehaviorReturnType powerResult = HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker);
                if (powerResult == StaticBehaviorReturnType.Running) return;
                else if (powerResult == StaticBehaviorReturnType.Completed)
                    collection[PropertyEnum.AICustomStateVal1] = (int)State.Attack;
                else if (target == null)
                    collection[PropertyEnum.AICustomStateVal1] = (int)State.NoWeapon;
            }

            proceduralAI.PartialOverrideBehavior?.Think(ownerController);

            target = ownerController.TargetEntity;
            DefaultMeleeMovement(proceduralAI, ownerController, agent.Locomotor, target, MoveToTarget, OrbitTarget);
        }

        public override void OnOwnerTargetSwitch(AIController ownerController, ulong oldTarget, ulong newTarget)
        {
            BehaviorBlackboard blackboard = ownerController.Blackboard;
            int state = blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            if (state == (int)State.Pickup)
                blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = (int)State.Attack;
        }
    }

    public class ProceduralProfileMeleeAllyDeathFleePrototype : ProceduralProfileWithAttackPrototype
    {
        public FleeContextPrototype FleeFromTargetOnAllyDeath { get; protected set; }
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }

        public override void OnOwnerAllyDeath(AIController ownerController)
        {
            ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = 1;
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

            if (HandleOverrideBehavior(ownerController)) return;

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false
                && proceduralAI.PartialOverrideBehavior == null) return;

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running) return;

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            if (blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] == 1)
            {
                StaticBehaviorReturnType fleeResult = HandleContext(proceduralAI, ownerController, FleeFromTargetOnAllyDeath);
                if (fleeResult == StaticBehaviorReturnType.Running) return;
                blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AICustomStateVal1);
            }

            proceduralAI.PartialOverrideBehavior?.Think(ownerController);

            DefaultMeleeMovement(proceduralAI, ownerController, agent.Locomotor, target, MoveToTarget, OrbitTarget);
        }
    }

    public class ProceduralProfileRangedFlankerAllyDeathFleePrototype : ProceduralProfileWithAttackPrototype
    {
        public FleeContextPrototype FleeFromTargetOnAllyDeath { get; protected set; }
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public ProceduralFlankContextPrototype FlankTarget { get; protected set; }

        public override void OnOwnerAllyDeath(AIController ownerController)
        {
            ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = 1;
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

            if (HandleOverrideBehavior(ownerController)) return;

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false
                && proceduralAI.PartialOverrideBehavior == null) return;

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running) return;

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            if (blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] == 1)
            {
                StaticBehaviorReturnType fleeResult = HandleContext(proceduralAI, ownerController, FleeFromTargetOnAllyDeath);
                if (fleeResult == StaticBehaviorReturnType.Running) return;
                blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AICustomStateVal1);
            }

            proceduralAI.PartialOverrideBehavior?.Think(ownerController);

            DefaultRangedFlankerMovement(proceduralAI, ownerController, agent, target, currentTime, MoveToTarget, FlankTarget);
        }
    }

    public class ProceduralProfileRangedHotspotDropperPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype RangedPower { get; protected set; }
        public ProceduralUsePowerContextPrototype HotspotPower { get; protected set; }
        public WanderContextPrototype HotspotDroppingMovement { get; protected set; }

        public override void Init(Agent agent)
        {
            base.Init(agent);
            InitPower(agent, RangedPower);
            InitPower(agent, HotspotPower);
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

            if (HandleOverrideBehavior(ownerController)) return;

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false
                && proceduralAI.PartialOverrideBehavior == null) return;

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
            {
                PrototypeId activePower = ownerController.ActivePowerRef;
                if (activePower == PrototypeId.Invalid) return;
                var powerContext = HotspotPower?.PowerContext;
                if (powerContext == null || powerContext.Power == PrototypeId.Invalid) return;
                if (activePower == powerContext.Power)
                {
                    proceduralAI.PushSubstate();
                    HandleContext(proceduralAI, ownerController, HotspotDroppingMovement);
                    proceduralAI.PopSubstate();
                }
                return;
            }

            DefaultRangedMovement(proceduralAI, ownerController, agent, target, MoveToTarget, OrbitTarget);
        }

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

        public override void Init(Agent agent)
        {
            base.Init(agent);
            InitPowers(agent, TeamUpPowerProgressionPowers);
        }

        private const int StateMoveToOwner = 1;

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Game game = agent.Game;
            if (game == null) return;
            long currentTime = (long)game.CurrentTime.TotalMilliseconds;

            if (HandleOverrideBehavior(ownerController)) return;
            if (agent.IsDormant) return;

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            WorldEntity master = ownerController.AssistedEntity;
            if (master != null && master.IsInWorld)
            {                
                float distanceToMasterSq = Vector3.DistanceSquared2D(agent.RegionLocation.Position, master.RegionLocation.Position);
                if (distanceToMasterSq > MaxDistToMasterBeforeTeleport * MaxDistToMasterBeforeTeleport)
                {
                    if (ownerController.ActivePowerRef == PrototypeId.Invalid)
                    {
                        blackboard.PropertyCollection[PropertyEnum.AILastAttackerID] = 0;
                        HandleContext(proceduralAI, ownerController, TeleportToMasterIfTooFarAway, null);
                        ownerController.ResetCurrentTargetState();
                    }
                }
            }

            WorldEntity target = ownerController.TargetEntity;

            if (blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] == StateMoveToOwner
                || CommonSimplifiedSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false)
            {
                MoveToOwner(ownerController, master);
                return;
            }

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running) return;

            if (IsRanged && FlankTarget != null)
                DefaultRangedFlankerMovement(proceduralAI, ownerController, agent, target, currentTime, MoveToTarget, FlankTarget);
            else if (IsRanged)
                DefaultRangedMovement(proceduralAI, ownerController, agent, target, MoveToTarget, OrbitTarget);
            else
                DefaultMeleeMovement(proceduralAI, ownerController, agent.Locomotor, target, MoveToTarget, OrbitTarget);
        }

        public virtual void MoveToOwner(AIController ownerController, WorldEntity owner)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            if (owner != null && owner.IsInWorld)
                HandleMovementContext(proceduralAI, ownerController, agent.Locomotor, MoveToMaster, false, out _);
        }

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

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Game game = agent.Game;
            if (game == null) return;
            long currentTime = (long)game.CurrentTime.TotalMilliseconds;

            if (HandleOverrideBehavior(ownerController)) return;
            if (agent.IsDormant) return;

            WorldEntity master = ownerController.AssistedEntity;
            if (master != null && master.IsInWorld)
            {
                float distanceToMasterSq = Vector3.DistanceSquared2D(agent.RegionLocation.Position, master.RegionLocation.Position);
                if (distanceToMasterSq > MaxDistToMasterBeforeTeleport * MaxDistToMasterBeforeTeleport)
                {
                    if (ownerController.ActivePowerRef == PrototypeId.Invalid)
                    {
                        ownerController.Blackboard.PropertyCollection[PropertyEnum.AILastAttackerID] = 0;
                        HandleContext(proceduralAI, ownerController, TeleportToMasterIfTooFarAway, null);
                        ownerController.ResetCurrentTargetState();
                    }
                }
            }

            WorldEntity target = ownerController.TargetEntity;

            if (CommonSimplifiedSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false)
            {
                HandleMovementContext(proceduralAI, ownerController, agent.Locomotor, PetFollow, false, out _);
                return;
            }

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running) return;

            HandleDefaultPetMovement(proceduralAI, ownerController, currentTime, target);
        }

        protected void HandleDefaultPetMovement(ProceduralAI proceduralAI, AIController ownerController, long currentTime, WorldEntity target)
        {
            Agent agent = ownerController.Owner;

            if (IsRanged && FlankTarget != null)
                DefaultRangedFlankerMovement(proceduralAI, ownerController, agent, target, currentTime, MoveToTarget, FlankTarget);
            else if (IsRanged)
                DefaultRangedMovement(proceduralAI, ownerController, agent, target, MoveToTarget, OrbitTarget);
            else
                DefaultMeleeMovement(proceduralAI, ownerController, agent.Locomotor, target, MoveToTarget, OrbitTarget);
        }
    }

    public class ProceduralProfileMeleePowerOnHitPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype PowerOnHit { get; protected set; }

        public override void Init(Agent agent)
        {
            base.Init(agent);
            InitPower(agent, PowerOnHit);
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

        public override void OnPowerStarted(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            if (powerContext == PowerOnHit)
            {
                Agent agent = ownerController.Owner;
                if (agent == null) return;
                ulong attackerId = ownerController.Blackboard.PropertyCollection[PropertyEnum.AILastAttackerID];
                WorldEntity attacker = agent.Game.EntityManager.GetEntity<WorldEntity>(attackerId);
                if (attacker != null)
                    agent.OrientToward(attacker.RegionLocation.Position);
                ownerController.Blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AICustomStateVal1);
            }
        }

        public override void OnOwnerGotDamaged(AIController ownerController)
        {
            ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = 1;
        }
    }

    public class ProceduralProfileBotAIPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public SelectEntityContextPrototype SelectTargetItem { get; protected set; }
        public WanderContextPrototype WanderMovement { get; protected set; }
        public ProceduralUsePowerContextPrototype[] SlottedAbilities { get; protected set; }

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Game game = agent.Game;
            if (game == null) return;
            long currentTime = (long)game.CurrentTime.TotalMilliseconds;

            WorldEntity target = ownerController.TargetEntity;
            DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile);

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running) return;

            if (target != null) 
            {
                DefaultMeleeMovement(proceduralAI, ownerController, agent.Locomotor, target, MoveToTarget, OrbitTarget);
                return;
            }

            HandleMovementContext(proceduralAI, ownerController, agent.Locomotor, WanderMovement, false, out _);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
        }
    }

    public class ProceduralProfileMeleeFlockerPrototype : ProceduralProfileWithAttackPrototype
    {
        public FlockContextPrototype FlockContext { get; protected set; }
        public PrototypeId FleeOnAllyDeathOverride { get; protected set; }

        public override void OnOwnerAllyDeath(AIController ownerController)
        {
            if (FleeOnAllyDeathOverride != PrototypeId.Invalid)
            {
                ProceduralAI proceduralAI = ownerController.Brain;
                if (proceduralAI == null) return;
                var overrideProfile = FleeOnAllyDeathOverride.As<ProceduralAIProfilePrototype>();
                if (proceduralAI.FullOverrideBehavior != overrideProfile)
                    proceduralAI.SetOverride(overrideProfile, OverrideType.Full);
            }
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

            if (HandleOverrideBehavior(ownerController)) return;

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false
                && proceduralAI.PartialOverrideBehavior == null) return;

            if (proceduralAI.GetState(0) == UsePower.Instance)
            {
                GRandom random = game.Random;
                Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
                PopulatePowerPicker(ownerController, powerPicker);
                if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running) return;
            }

            if (HandleContext(proceduralAI, ownerController, FlockContext) == StaticBehaviorReturnType.Completed)
            {
                GRandom random = game.Random;
                Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
                PopulatePowerPicker(ownerController, powerPicker);
                if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running) return;
            }

            proceduralAI.PartialOverrideBehavior?.Think(ownerController);
        }
    }

    public class ProceduralProfileWithEnragePrototype : ProceduralProfileWithAttackPrototype
    {
        public int EnrageTimerInMinutes { get; protected set; }
        public ProceduralUsePowerContextPrototype EnragePower { get; protected set; }
        public float EnrageTimerAvatarSearchRadius { get; protected set; }
        public ProceduralUsePowerContextPrototype[] PostEnragePowers { get; protected set; }

        public override void Init(Agent agent)
        {
            base.Init(agent);
            InitPower(agent, EnragePower);
            InitPowers(agent, PostEnragePowers);
        }

        protected enum EnrageState
        {
            Default = 0,
            Enrage = 1,
            Enraging = 2,
            Enraged = 3,
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
            HandleEnrage(ownerController);

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            EnrageState state = (EnrageState)(int)blackboard.PropertyCollection[PropertyEnum.AIEnrageState];
            if( state != EnrageState.Enraging)
            {
                WorldEntity target = ownerController.TargetEntity;
                if (CommonSimplifiedSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false) return;

                GRandom random = game.Random;
                Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
                PopulatePowerPicker(ownerController, powerPicker);
                HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true);
            }
        }

        public void HandleEnrage(AIController ownerController)
        {
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            BehaviorBlackboard blackboard = ownerController.Blackboard;
            EnrageState state = (EnrageState)(int)blackboard.PropertyCollection[PropertyEnum.AIEnrageState];

            if (state != EnrageState.Enraged)
            {
                Game game = agent.Game;
                if (game == null) return;
                TimeSpan currentTime = game.CurrentTime;

                if (agent.Properties.HasProperty(PropertyEnum.EnrageStartTime) == false)
                {
                    Region region = agent.Region;
                    if (region == null) return;
                    Vector3 position = agent.RegionLocation.Position;

                    foreach (Avatar avatar in region.IterateAvatarsInVolume(new Sphere(position, EnrageTimerAvatarSearchRadius)))
                        if (avatar != null)
                        {
                            agent.Properties[PropertyEnum.EnrageStartTime] = currentTime + TimeSpan.FromMinutes(EnrageTimerInMinutes);
                            break;
                        }
                }
                else if (currentTime > agent.Properties[PropertyEnum.EnrageStartTime])
                {
                    ProceduralAI proceduralAI = ownerController.Brain;
                    if (proceduralAI == null) return;

                    if (state == EnrageState.Enraging || proceduralAI.GetState(0) != UsePower.Instance)
                    {
                        if (EnragePower == null) return;
                        UsePowerContextPrototype enragePowerContextProto = EnragePower.PowerContext;
                        if (enragePowerContextProto == null) return;

                        var powerResult = HandleUsePowerContext(ownerController, proceduralAI, game.Random, (long)currentTime.TotalMilliseconds, enragePowerContextProto, EnragePower);
                        if (powerResult == StaticBehaviorReturnType.Running)
                            blackboard.PropertyCollection[PropertyEnum.AIEnrageState] = (int)EnrageState.Enraging;
                        else if (powerResult == StaticBehaviorReturnType.Completed)
                            blackboard.PropertyCollection[PropertyEnum.AIEnrageState] = (int)EnrageState.Enraged;
                        else
                            blackboard.PropertyCollection[PropertyEnum.AIEnrageState] = (int)EnrageState.Enrage;
                    }
                    else
                        blackboard.PropertyCollection[PropertyEnum.AIEnrageState] = (int)EnrageState.Enrage;
                }
            }
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            int stateVal = ownerController.Blackboard.PropertyCollection[PropertyEnum.AIEnrageState];
            if (stateVal == (int)EnrageState.Enraged)
                ownerController.AddPowersToPicker(powerPicker, PostEnragePowers);
        }

        public override void OnOwnerGotDamaged(AIController ownerController)
        {
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            if (agent.Properties.HasProperty(PropertyEnum.EnrageStartTime) == false)
            {
                Game game = agent.Game;
                if (game == null) return;
                TimeSpan currentTime = game.CurrentTime;
                agent.Properties[PropertyEnum.EnrageStartTime] = currentTime + TimeSpan.FromMinutes(EnrageTimerInMinutes);
            }
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

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Game game = agent.Game;
            if (game == null) return;
            long currentTime = (long)game.CurrentTime.TotalMilliseconds;

            if (HandleOverrideBehavior(ownerController)) return;
            if (agent.IsDormant) return;

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            long nextFindBestTime = blackboard.PropertyCollection[PropertyEnum.AICustomTimeVal1];
            WorldEntity avatarAlly = ownerController.AssistedEntity;

            if (avatarAlly == null || (nextFindBestTime != 0 && currentTime > nextFindBestTime))
                FindBestAvatarAllyToFollow(ownerController);
            else if (avatarAlly != null && avatarAlly.IsInWorld)
            {                
                if (avatarAlly.Region != agent.Region)
                {
                    avatarAlly.Properties.AdjustProperty(-1, PropertyEnum.NumMissionAllies);
                    agent.Properties.RemoveProperty(PropertyEnum.PowerUserOverrideID);
                    agent.Properties.RemoveProperty(PropertyEnum.MissionAllyOfAvatarDbGuid);
                    return;
                }

                float distanceToAvatarAllySq = Vector3.DistanceSquared2D(agent.RegionLocation.Position, avatarAlly.RegionLocation.Position);
                if (distanceToAvatarAllySq > MaxDistToAvatarAllyBeforeTele * MaxDistToAvatarAllyBeforeTele)
                {
                    if (ownerController.ActivePowerRef == PrototypeId.Invalid)
                    {
                        blackboard.PropertyCollection[PropertyEnum.AILastAttackerID] = 0;
                        HandleContext(proceduralAI, ownerController, TeleportToAvatarAllyIfTooFarAway, null);
                        ownerController.ResetCurrentTargetState();
                    }
                }
            }

            WorldEntity target = ownerController.TargetEntity;
            if (CommonSimplifiedSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false)
            {
                HandleMovementContext(proceduralAI, ownerController, agent.Locomotor, MoveToAvatarAlly, false, out _);
                return;
            }

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running) return;

            if (IsRanged && FlankTarget != null)
                DefaultRangedFlankerMovement(proceduralAI, ownerController, agent, target, currentTime, MoveToTarget, FlankTarget);
            else if (IsRanged)
                DefaultRangedMovement(proceduralAI, ownerController, agent, target, MoveToTarget, OrbitTarget);
            else
                DefaultMeleeMovement(proceduralAI, ownerController, agent.Locomotor, target, MoveToTarget, OrbitTarget);
        }

        public void FindBestAvatarAllyToFollow(AIController ownerController)
        {
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Region region = agent.Region;
            if (region == null) 
            {
                ProceduralAI.Logger.Warn($"Entity is not in a valid region! Entity: {agent}");
                return;
            }

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            Vector3 position = agent.RegionLocation.Position;
            WorldEntity bestAvatar = ownerController.AssistedEntity;

            foreach (var avatar in region.IterateAvatarsInVolume(new Sphere(position, AvatarAllySearchRadius)))
            {
                if (avatar == null || avatar.IsDead || avatar == bestAvatar) continue;

                int currentMissionAllies = avatar.Properties[PropertyEnum.NumMissionAllies];
                if (currentMissionAllies == 0)
                {
                    bestAvatar = avatar;
                    break;
                }
                else
                {
                    if (bestAvatar == null || currentMissionAllies < bestAvatar.Properties[PropertyEnum.NumMissionAllies])
                        bestAvatar = avatar;
                }
            }

            if (bestAvatar != null)
            {
                if (bestAvatar.Properties[PropertyEnum.NumMissionAllies] > 0)
                {
                    Game game = ownerController.Game;
                    if (game == null) return;
                    long nextFindBestTime = (long)game.CurrentTime.TotalMilliseconds + 3000;
                    blackboard.PropertyCollection[PropertyEnum.AICustomTimeVal1] = nextFindBestTime;
                }
                else
                    blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AICustomTimeVal1);

                bestAvatar.Properties.AdjustProperty(1, PropertyEnum.NumMissionAllies);
                blackboard.PropertyCollection[PropertyEnum.AIAssistedEntityID] = bestAvatar.Id;
            }

            agent.Properties[PropertyEnum.MissionAllyOfAvatarDbGuid] = bestAvatar?.DatabaseUniqueId ?? 0;
            agent.Properties[PropertyEnum.PowerUserOverrideID] = bestAvatar?.Id ?? 0;
        }

        public override void OnOwnerExitWorld(AIController ownerController)
        {
            Agent agent = ownerController.Owner;
            WorldEntity master = ownerController.AssistedEntity;

            if (agent != null && master != null)
            {
                master.Properties.AdjustProperty(-1, PropertyEnum.NumMissionAllies);
                agent.Properties.RemoveProperty(PropertyEnum.PowerUserOverrideID);
                agent.Properties.RemoveProperty(PropertyEnum.MissionAllyOfAvatarDbGuid);
            }
        }

    }

    public class ProceduralProfileSpikeDanceMobPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralUsePowerContextPrototype SpikeDanceMissile { get; protected set; }

        public override void Init(Agent agent)
        {
            base.Init(agent);
            InitPower(agent, SpikeDanceMissile);

            AIController ownerController = agent.AIController;
            if (ownerController == null) return;
            ownerController.SetIsEnabled(false);
        }

        private enum State
        {
            Default,
            SpikeDance
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

            if (ownerController.TargetEntity == null)
                SelectEntity.RegisterSelectedEntity(ownerController, agent, SelectEntityType.SelectTarget);

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            State stateVal = (State)(int)blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            if (stateVal == State.SpikeDance && HandleUsePowerContext(ownerController, proceduralAI, game.Random, currentTime, SpikeDanceMissile.PowerContext, SpikeDanceMissile) != StaticBehaviorReturnType.Running)
            {
                blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = (int)State.Default;
                ownerController.SetIsEnabled(false);
                return;
            }
        }
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

        private enum RestrictedMode
        {
            Default,
            StartPower = 1,
            ProceduralPowers = 2,
            EndPower = 3,
        }

        public override void Init(Agent agent)
        {
            base.Init(agent);
            InitPower(agent, RestrictedModeStartPower);
            InitPower(agent, RestrictedModeEndPower);
            InitPowers(agent, RestrictedModeProceduralPowers);

            Game game = agent.Game;
            var blackboard = agent.AIController?.Blackboard;
            if (game == null || blackboard == null) return;
            long currentTime = (long)game.CurrentTime.TotalMilliseconds;
            long restrictedCooldown = currentTime + game.Random.Next(RestrictedModeMinCooldownMS, RestrictedModeMaxCooldownMS);
            blackboard.PropertyCollection[PropertyEnum.AICustomTimeVal1] = restrictedCooldown;
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
            
            if (HandleOverrideBehavior(ownerController)) return;
            if (agent.IsDormant) return;
           
            BehaviorBlackboard blackboard = ownerController.Blackboard;
            RestrictedMode state = (RestrictedMode)(int)blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false
                && proceduralAI.PartialOverrideBehavior == null) return;

            switch (state)
            {
                case RestrictedMode.Default:

                    GRandom random = game.Random;
                    Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
                    PopulatePowerPicker(ownerController, powerPicker);
                    if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running) return;

                    if (proceduralAI.GetState(0) != UsePower.Instance && currentTime > blackboard.PropertyCollection[PropertyEnum.AICustomTimeVal1])
                    {
                        if (RestrictedModeStartPower != null)
                            blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = (int)RestrictedMode.StartPower;
                        else
                            SwitchStates(ownerController, RestrictedMode.ProceduralPowers);
                        return;
                    }
                    break;

                case RestrictedMode.StartPower:

                    if (RestrictedModeStartPower != null)
                    {
                        var powerResult = HandleUsePowerContext(ownerController, proceduralAI, game.Random, currentTime, RestrictedModeStartPower.PowerContext, RestrictedModeStartPower);
                        if (powerResult == StaticBehaviorReturnType.Running) return;
                    }
                    else
                        ProceduralAI.Logger.Warn($"Power Restricted Agent {agent} is trying to play their RestrictedModeStartPower which is NULL!!");

                    SwitchStates(ownerController, RestrictedMode.ProceduralPowers);
                    break;

                case RestrictedMode.ProceduralPowers:

                    random = game.Random;
                    powerPicker = new(random);
                    PopulatePowerPicker(ownerController, powerPicker);
                    if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running) return;

                    if (proceduralAI.GetState(0) != UsePower.Instance && currentTime > blackboard.PropertyCollection[PropertyEnum.AICustomTimeVal2])
                    {
                        if (RestrictedModeEndPower != null)
                            blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = (int)RestrictedMode.EndPower;
                        else
                            SwitchStates(ownerController, RestrictedMode.Default);
                    }
                    break;

                case RestrictedMode.EndPower:

                    if (RestrictedModeEndPower != null)
                    {
                        var powerResult = HandleUsePowerContext(ownerController, proceduralAI, game.Random, currentTime, RestrictedModeEndPower.PowerContext, RestrictedModeEndPower);
                        if (powerResult == StaticBehaviorReturnType.Running) return;
                    }
                    else
                        ProceduralAI.Logger.Warn($"Power Restricted Agent {agent} is trying to play their RestrictedModeEndPower which is NULL!!");

                    SwitchStates(ownerController, RestrictedMode.Default);
                    break;
            }

            if (state == RestrictedMode.Default || NoMoveInRestrictedMode == false)
            {
                if (IsRanged && FlankTarget != null)
                    DefaultRangedFlankerMovement(proceduralAI, ownerController, agent, target, currentTime, MoveToTarget, FlankTarget);
                else if (IsRanged)
                    DefaultRangedMovement(proceduralAI, ownerController, agent, target, MoveToTarget, OrbitTarget);
                else
                    DefaultMeleeMovement(proceduralAI, ownerController, agent.Locomotor, target, MoveToTarget, OrbitTarget);
            }
        }

        private void SwitchStates(AIController ownerController, RestrictedMode state)
        {
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Game game = agent.Game;
            if (game == null) return;
            long currentTime = (long)game.CurrentTime.TotalMilliseconds;
            BehaviorBlackboard blackboard = ownerController.Blackboard;

            switch (state)
            {
                case RestrictedMode.Default:
                    blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = (int)RestrictedMode.Default;
                    blackboard.PropertyCollection[PropertyEnum.AICustomTimeVal1] = currentTime + game.Random.Next(RestrictedModeMinCooldownMS, RestrictedModeMaxCooldownMS);
                    break;

                case RestrictedMode.ProceduralPowers:
                    blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = (int)RestrictedMode.ProceduralPowers;
                    blackboard.PropertyCollection[PropertyEnum.AICustomTimeVal2] = currentTime + RestrictedModeTimerMS;
                    break;
            }
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            int state = ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            if ((RestrictedMode)state == RestrictedMode.ProceduralPowers)
                ownerController.AddPowersToPicker(powerPicker, RestrictedModeProceduralPowers);
            else
                base.PopulatePowerPicker(ownerController, powerPicker);
        }
    }

}
