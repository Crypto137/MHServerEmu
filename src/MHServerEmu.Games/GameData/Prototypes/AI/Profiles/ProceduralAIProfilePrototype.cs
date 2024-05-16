using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.Behavior;
using MHServerEmu.Games.Behavior.ProceduralAI;
using MHServerEmu.Games.Behavior.StaticAI;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Entities.PowerCollections;
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

        protected virtual StaticBehaviorReturnType HandleUsePowerContext(AIController ownerController, ProceduralAI proceduralAI, GRandom random,
            long currentTime, UsePowerContextPrototype powerContext, ProceduralContextPrototype proceduralContext = null)
        {
            return HandleContext(proceduralAI, ownerController, powerContext, proceduralContext);
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
                proceduralAI.SwitchProceduralState(null, context, StaticBehaviorReturnType.Failed);
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

        public virtual void Init(Agent agent){ }

        protected static void InitPowers(Agent agent, ProceduralUsePowerContextPrototype[] proceduralPowers)
        {
            if (proceduralPowers.HasValue())
                foreach(var proceduralPower in proceduralPowers)
                    InitPower(agent, proceduralPower);
        }

        protected static void InitPowers(Agent agent, PrototypeId[] powers)
        {
            if (powers.HasValue())
                foreach (var power in powers)
                    InitPower(agent, power);
        }

        protected static void InitPower(Agent agent, ProceduralUsePowerContextPrototype proceduralPower)
        {
            InitPower(agent, proceduralPower?.PowerContext);
        }

        protected static void InitPower(Agent agent, UsePowerContextPrototype powerContext)
        {
            if (powerContext == null) return;
            InitPower(agent, powerContext.Power);
        }

        protected static void InitPower(Agent agent, PrototypeId power)
        {
            if (power == PrototypeId.Invalid) return;
            if (agent.HasPowerInPowerCollection(power) == false)
            {
                var indexPowerProps = new PowerIndexProperties(agent.CharacterLevel, agent.CombatLevel, agent.Properties[PropertyEnum.PowerRank]);
                // TODO PropertyEnum.AILOSMaxPowerRadius
                agent.AssignPower(power, indexPowerProps);
            }
        }

        public virtual void OnOwnerExitWorld(AIController ownerController) { }
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

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (proceduralAI == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Game game = agent.Game;
            if (game == null) return;
            long currentTime = (long)game.GetCurrentTime().TotalMilliseconds;

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            GRandom random = game.Random;

            int subscriptions = blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            int availableSubscriptions = Math.Min(MaxSubscriptionsPerActivation, MaxSubscriptions - subscriptions);
            int subscribed = 0;

            List<WorldEntity> potentialTargets = Combat.GetTargetsInRange(agent, Radius, 0.0f, CombatTargetType.Ally, CombatTargetFlags.IgnoreHostile, EnticeeAttributes);
            foreach (WorldEntity potentialTarget in potentialTargets)
            {
                if (potentialTarget == null) return;
                if (Subscribe(potentialTarget, agent, random, currentTime))
                {
                    availableSubscriptions--;
                    subscribed++;
                    if (availableSubscriptions <= 0)
                        break;
                }
            }

            blackboard.PropertyCollection.AdjustProperty(subscribed, PropertyEnum.AICustomStateVal1);

            if (MaxSubscriptions > 0 && (subscriptions + subscribed) >= MaxSubscriptions)
            {
                ownerController.SetIsEnabled(false);
                return;
            }

            if (subscribed > 0)
            {
                ownerController.ClearScheduledThinkEvent();
                ownerController.ScheduleAIThinkEvent(TimeSpan.FromMilliseconds(random.Next(CooldownMinMS, CooldownMaxMS)));
            }
        }

        private bool Subscribe(WorldEntity subscriber, Agent enticed, GRandom random, long currentTime)
        {
            if (subscriber is not Agent subscriberAgent || subscriberAgent.IsExecutingPower) return false;

            AIController controller = subscriberAgent.AIController;
            if (controller == null) return false;
                
            var collection = controller.Blackboard.PropertyCollection;
            if (collection.HasProperty(PropertyEnum.AIEnticedToID)) return false;
            
            long globalNextAvailableTime = collection[PropertyEnum.AIEnticerGlobalNextAvailableTime];
            if (globalNextAvailableTime > 0 && currentTime < globalNextAvailableTime) return false;
            
            long nextAvailableTime = collection[PropertyEnum.AIEnticerTypeNextAvailableTime, enticed.PrototypeDataRef];
            if (nextAvailableTime > 0 && currentTime < nextAvailableTime) return false;
            
            collection[PropertyEnum.AIEnticedToID] = enticed.Id;
            collection[PropertyEnum.AIFullOverride] = EnticedBehavior;

            globalNextAvailableTime = currentTime + random.Next(EnticeeGlobalEnticerCDMinMS, EnticeeGlobalEnticerCDMaxMS);
            collection[PropertyEnum.AIEnticerGlobalNextAvailableTime] = globalNextAvailableTime;

            nextAvailableTime = currentTime + random.Next(EnticeeEnticerCooldownMinMS, EnticeeEnticerCooldownMaxMS);
            collection[PropertyEnum.AIEnticerTypeNextAvailableTime, enticed.PrototypeDataRef] = nextAvailableTime;
            return true;
        }
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


    public class ProceduralProfileLeashOverridePrototype : ProceduralAIProfilePrototype
    {
        public PrototypeId LeashReturnHeal { get; protected set; }
        public PrototypeId LeashReturnImmunity { get; protected set; }
        public MoveToContextPrototype MoveToSpawn { get; protected set; }
        public TeleportContextPrototype TeleportToSpawn { get; protected set; }
        public PrototypeId LeashReturnTeleport { get; protected set; }
        public PrototypeId LeashReturnInvulnerability { get; protected set; }


        public override void Init(Agent agent)
        {
            base.Init(agent);
            InitPower(agent, LeashReturnHeal);
            InitPower(agent, LeashReturnImmunity);
            InitPower(agent, LeashReturnTeleport);
            InitPower(agent, LeashReturnInvulnerability);
        }
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

    public class ProceduralProfileRotatingTurretPrototype : ProceduralAIProfilePrototype
    {
        public UsePowerContextPrototype Power { get; protected set; }
        public RotateContextPrototype Rotate { get; protected set; }

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

            if (HandleOverrideBehavior(ownerController)) return;

            BehaviorSensorySystem senses = ownerController.Senses;
            if (senses.ShouldSense())
                senses.UpdateAvatarSensory();

            if (agent.IsDormant == false)
                if (HandleContext(proceduralAI, ownerController, Power) == StaticBehaviorReturnType.Running)
                {
                    proceduralAI.PushSubstate();
                    HandleContext(proceduralAI, ownerController, Rotate);
                    proceduralAI.PopSubstate();
                }
        }
    }

    public class ProceduralProfileWanderNoPowerPrototype : ProceduralAIProfilePrototype
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

            HandleMovementContext(proceduralAI, ownerController, agent.Locomotor, WanderMovement, false, out _);
        }
    }

    public class ProceduralProfilePetFidgetPrototype : ProceduralProfilePetPrototype
    {
        public ProceduralUsePowerContextPrototype Fidget { get; protected set; }

        public override void Init(Agent agent)
        {
            base.Init(agent);
            InitPower(agent, Fidget);
        }

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

    public class ProceduralProfileRollingGrenadesPrototype : ProceduralAIProfilePrototype
    {
        public int MaxSpeedDegreeUpdateIntervalMS { get; protected set; }
        public int MinSpeedDegreeUpdateIntervalMS { get; protected set; }
        public int MovementSpeedVariance { get; protected set; }
        public int RandomDegreeFromForward { get; protected set; }
    }

    public class ProceduralProfileFrozenOrbPrototype : ProceduralAIProfilePrototype
    {
        public int ShardBurstsPerSecond { get; protected set; }
        public int ShardsPerBurst { get; protected set; }
        public int ShardRotationSpeed { get; protected set; }
        public PrototypeId ShardPower { get; protected set; }

        public override void Init(Agent agent)
        {
            base.Init(agent);
            InitPower(agent, ShardPower);
        }
    }

    public class ProceduralProfilePetDirectedPrototype : ProceduralProfilePetPrototype
    {
        public ProceduralUsePowerContextPrototype[] DirectedPowers { get; protected set; }

        public override void Init(Agent agent)
        {
            base.Init(agent);
            InitPowers(agent, DirectedPowers);
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

    public class ProceduralProfileLOSRangedPrototype : ProceduralProfileBasicRangePrototype
    {
        public ProceduralUsePowerContextPrototype LOSChannelPower { get; protected set; }

        public override void Init(Agent agent)
        {
            base.Init(agent);
            InitPower(agent, LOSChannelPower);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            ownerController.AddPowersToPicker(powerPicker, LOSChannelPower);
            base.PopulatePowerPicker(ownerController, powerPicker);
        }
    }

    public class ProcProfileSpikeDanceControllerPrototype : ProceduralAIProfilePrototype
    {
        public PrototypeId Onslaught { get; protected set; }
        public PrototypeId SpikeDanceMob { get; protected set; }
        public int MaxSpikeDanceActivations { get; protected set; }
        public float SpikeDanceMobSearchRadius { get; protected set; }
    }

    public class ProceduralProfileMeleeRevengePrototype : ProceduralProfileBasicMeleePrototype
    {
        public ProceduralUsePowerContextPrototype RevengePower { get; protected set; }
        public PrototypeId RevengeSupport { get; protected set; }

        public override void Init(Agent agent)
        {
            base.Init(agent);
            InitPower(agent, RevengePower);
        }

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

        public override void Init(Agent agent)
        {
            base.Init(agent);
            InitPower(agent, RevengePower);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);
            int stateVal = ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            if (stateVal == 1)
                ownerController.AddPowersToPicker(powerPicker, RevengePower);
        }
    }

}
