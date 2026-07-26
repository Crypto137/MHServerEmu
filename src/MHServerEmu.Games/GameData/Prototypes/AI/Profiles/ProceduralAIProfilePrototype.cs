using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Behavior;
using MHServerEmu.Games.Behavior.ProceduralAI;
using MHServerEmu.Games.Behavior.StaticAI;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Entities.PowerCollections;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Populations;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class ProceduralAIProfilePrototype : BrainPrototype
    {
        //---

        public virtual void Init(Agent agent) { }

        public virtual void Think(AIController ownerController)
        {
            Verify.IsTrue(false, "ProceduralAIProfilePrototype::THINK() - BASE CLASS SHOULD NOT BE CALLED");
        }

        public bool HandleOverrideBehavior(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return false;

            ProceduralAIProfilePrototype fullOverrideBehavior = proceduralAI.FullOverrideBehavior;
            if (fullOverrideBehavior != null && fullOverrideBehavior.GetType() != GetType())
            {
                fullOverrideBehavior.Think(ownerController);

                if (ownerController.IsOwnerValid() == false)
                    return true;

                return proceduralAI.FullOverrideBehavior != null;
            }

            return false;
        }

        protected virtual StaticBehaviorReturnType HandleUsePowerContext(AIController ownerController, ProceduralAI proceduralAI, GRandom random,
            long currentTime, UsePowerContextPrototype powerContext, ProceduralContextPrototype proceduralContext = null)
        {
            return HandleContext(proceduralAI, ownerController, powerContext, proceduralContext);
        }

        public static StaticBehaviorReturnType HandleContext<TContextProto>(ProceduralAI proceduralAI, AIController ownerController,
            TContextProto contextProto, ProceduralContextPrototype proceduralContext = null)
            where TContextProto : Prototype
        {
            (IAIState instance, IStateContext context) = IStateContext.Create(ownerController, contextProto);
            return proceduralAI.HandleContext(instance, context, proceduralContext);
        }

        public static bool HandleMovementContext<TContextProto>(ProceduralAI proceduralAI, AIController ownerController, 
            Locomotor locomotor, TContextProto contextProto, bool checkPower, out StaticBehaviorReturnType movementResult, ProceduralContextPrototype proceduralContext = null)
             where TContextProto : Prototype
        {
            movementResult = StaticBehaviorReturnType.None;

            if (!Verify.IsNotNull(locomotor, $"Can't move without a locomotor! {ownerController}"))
                return false;

            (IAIState instance, IStateContext context) = IStateContext.Create(ownerController, contextProto);
            movementResult = proceduralAI.HandleContext(instance, context, proceduralContext);

            if (ResetTargetAndStateIfPathFails(proceduralAI, ownerController, locomotor, context, checkPower))
                return false;

            return true;
        }

        private static bool ResetTargetAndStateIfPathFails(ProceduralAI proceduralAI, AIController ownerController, Locomotor locomotor, 
            in IStateContext context, bool checkPower)
        {
            Agent owner = ownerController.Owner;
            if (!Verify.IsNotNull(owner)) return false;

            if (!Verify.IsNotNull(locomotor, $"Agent [{owner}] doesn't have a locomotor and should not be calling this function"))
                return false;

            if (locomotor.LastGeneratedPathResult == NaviPathResult.FailedNoPathFound)
            {
                bool resetTarget = true;
                if (checkPower)
                    resetTarget = proceduralAI.LastPowerResult == StaticBehaviorReturnType.Failed;

                if (resetTarget)
                    ownerController.ResetCurrentTargetState();

                proceduralAI.SwitchProceduralState(null, context, StaticBehaviorReturnType.Failed);
                return true;
            }

            return false;
        }

        public static bool ValidateContext(ProceduralAI proceduralAI, AIController ownerController, UsePowerContextPrototype contextProto)
        {
            IStateContext context = new UsePowerContext(ownerController, contextProto);
            return proceduralAI.ValidateContext(UsePower.Instance, context);
        }

        protected static bool ValidateUsePowerContext(AIController ownerController, ProceduralAI proceduralAI, UsePowerContextPrototype powerContext)
        {
            return ValidateContext(proceduralAI, ownerController, powerContext);
        }

        protected static void HandleEnticerBehaviorResultStatus(Game game, BehaviorBlackboard blackboard, bool completed)
        {
            PropertyCollection properties = blackboard.PropertyCollection;

            ulong enticerId = properties[PropertyEnum.AIEnticedToID];
            Agent enticer = game.EntityManager.GetEntity<Agent>(enticerId);

            if (enticer != null)
            {
                AIController enticerController = enticer.AIController;
                if (!Verify.IsNotNull(enticerController)) return;

                ProceduralAI enticersBrain = enticerController.Brain as ProceduralAI;
                if (!Verify.IsNotNull(enticersBrain)) return;

                ProceduralProfileEnticerPrototype enticersProfile = enticersBrain.Behavior as ProceduralProfileEnticerPrototype;
                if (!Verify.IsNotNull(enticersProfile)) return;

                enticersProfile.HandleEnticementBehaviorCompletion(enticerController, completed);
            }

            properties.RemoveProperty(PropertyEnum.AIFullOverride);
            properties.RemoveProperty(PropertyEnum.AIEnticedToID);
        }

        protected static void InitPowers(Agent agent, ProceduralUsePowerContextPrototype[] proceduralPowers)
        {
            if (proceduralPowers.HasValue())
            {
                foreach (ProceduralUsePowerContextPrototype proceduralPower in proceduralPowers)
                    InitPower(agent, proceduralPower);
            }
        }

        protected static void InitPowers(Agent agent, PrototypeId[] powers)
        {
            if (powers.HasValue())
            {
                foreach (PrototypeId powerProtoRef in powers)
                    InitPower(agent, powerProtoRef);
            }
        }

        protected static void InitPower(Agent agent, ProceduralUsePowerContextPrototype proceduralPower)
        {
            if (!Verify.IsNotNull(proceduralPower)) return;
            if (!Verify.IsNotNull(proceduralPower.PowerContext)) return;
            if (!Verify.IsNotNull(proceduralPower.PowerContext.Power)) return;

            InitPower(agent, proceduralPower.PowerContext);
            
            if (proceduralPower.InitialCooldownMaxMS > 0)
            {
                AIController ownerController = agent.AIController;
                if (!Verify.IsNotNull(ownerController)) return;

                Game game = agent.Game;
                if (!Verify.IsNotNull(game)) return;
                
                int cooldown = game.Random.Next(proceduralPower.InitialCooldownMinMS, proceduralPower.InitialCooldownMaxMS);
                ownerController.Blackboard.PropertyCollection[PropertyEnum.AIInitialCooldownMSForPower, proceduralPower.PowerContext.Power.DataRef] = cooldown;
            }
        }

        protected static void InitPower(Agent agent, UsePowerContextPrototype powerContext)
        {
            if (!Verify.IsNotNull(agent)) return;
            if (!Verify.IsNotNull(powerContext)) return;

            PrototypeId powerProtoRef = powerContext.Power != null ? powerContext.Power.DataRef : PrototypeId.Invalid;
            InitPower(agent, powerProtoRef);
        }

        protected static void InitPower(Agent agent, PrototypeId powerProtoRef)
        {
            if (!Verify.IsNotNull(agent)) return;
            if (!Verify.IsTrue(powerProtoRef != PrototypeId.Invalid)) return;

            if (agent.HasPowerInPowerCollection(powerProtoRef) == false)
            {               
                AIController ownerController = agent.AIController;
                if (!Verify.IsNotNull(ownerController)) return;

                ownerController.FindMaxLOSPowerRadius(powerProtoRef);

                PowerIndexProperties indexPowerProps = new(agent.Properties[PropertyEnum.PowerRank], agent.CharacterLevel, agent.CombatLevel);
                Verify.IsNotNull(agent.AssignPower(powerProtoRef, indexPowerProps));
            }
        }

        public virtual void OnOwnerExitWorld(AIController ownerController) { }

        public virtual void OnOwnerKilled(AIController ownerController) { }

        public virtual void OnOwnerAllyDeath(AIController ownerController) { }

        public virtual void OnOwnerTargetSwitch(AIController ownerController, ulong oldTarget, ulong newTarget) { }

        public virtual void OnOwnerOverlapBegin(AIController ownerController, WorldEntity attacker) { }

        public virtual void ProcessInterrupts(AIController ownerController, BehaviorInterruptType interrupt) { }

        public virtual void OnEntityDeadEvent(AIController ownerController, in EntityDeadGameEvent deadEvent) { }

        public virtual void OnAIBroadcastBlackboardEvent(AIController ownerController, in AIBroadcastBlackboardGameEvent broadcastEvent) { }

        public virtual void OnPlayerInteractEvent(AIController ownerController, in PlayerInteractGameEvent interactEvent) { }

        public virtual void OnEntityAggroedEvent(AIController ownerController, in EntityAggroedGameEvent aggroedEvent) { }

        public virtual void OnMissileReturnEvent(AIController ownerController) { }

        public virtual void OnSetSimulated(AIController ownerController, bool simulated) { }

        public virtual void OnOwnerGotDamaged(AIController ownerController) { }

        public virtual void OnOwnerCollide(AIController ownerController, WorldEntity whom) { }
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

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            GRandom random = game.Random;

            int subscriptions = blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            int availableSubscriptions = Math.Min(MaxSubscriptionsPerActivation, MaxSubscriptions - subscriptions);
            int subscribed = 0;

            using var potentialTargetsHandle = ListPool<WorldEntity>.Instance.Get(out List<WorldEntity> potentialTargets);
            Combat.GetTargetsInRange(agent, potentialTargets, Radius, 0.0f, CombatTargetType.Ally, CombatTargetFlags.IgnoreHostile, EnticeeAttributes);
            foreach (WorldEntity potentialTarget in potentialTargets)
            {
                if (!Verify.IsNotNull(potentialTarget))
                    continue;

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

        public void HandleEnticementBehaviorCompletion(AIController enticerController, bool completed)
        {
            if (completed == false)
            {
                enticerController.Blackboard.PropertyCollection.AdjustProperty(-1, PropertyEnum.AICustomStateVal1);
                if (enticerController.IsEnabled == false)
                    enticerController.SetIsEnabled(true);
            }
        }

        private bool Subscribe(WorldEntity subscriber, Agent enticed, GRandom random, long currentTime)
        {
            if (subscriber is not Agent subscriberAgent || subscriberAgent.IsExecutingPower)
                return false;

            AIController aiController = subscriberAgent.AIController;
            if (aiController == null)
                return false;
                
            PropertyCollection properties = aiController.Blackboard.PropertyCollection;

            if (properties.HasProperty(PropertyEnum.AIEnticedToID))
                return false;
            
            long globalNextAvailableTime = properties[PropertyEnum.AIEnticerGlobalNextAvailableTime];
            if (globalNextAvailableTime > 0 && currentTime < globalNextAvailableTime)
                return false;
            
            long nextAvailableTime = properties[PropertyEnum.AIEnticerTypeNextAvailableTime, enticed.PrototypeDataRef];
            if (nextAvailableTime > 0 && currentTime < nextAvailableTime)
                return false;
            
            properties[PropertyEnum.AIEnticedToID] = enticed.Id;
            properties[PropertyEnum.AIFullOverride] = EnticedBehavior;

            globalNextAvailableTime = currentTime + random.Next(EnticeeGlobalEnticerCDMinMS, EnticeeGlobalEnticerCDMaxMS);
            properties[PropertyEnum.AIEnticerGlobalNextAvailableTime] = globalNextAvailableTime;

            nextAvailableTime = currentTime + random.Next(EnticeeEnticerCooldownMinMS, EnticeeEnticerCooldownMaxMS);
            properties[PropertyEnum.AIEnticerTypeNextAvailableTime, enticed.PrototypeDataRef] = nextAvailableTime;

            return true;
        }
    }

    public class ProceduralProfileEnticedBehaviorPrototype : ProceduralAIProfilePrototype
    {
        public FlankContextPrototype FlankToEnticer { get; protected set; }
        public MoveToContextPrototype MoveToEnticer { get; protected set; }
        public PrototypeId DynamicBehavior { get; protected set; }
        public bool OrientToEnticerOrientation { get; protected set; }

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            AIController ownerController = agent.AIController;
            if (!Verify.IsNotNull(ownerController)) return;

            PropertyCollection properties = ownerController.Blackboard.PropertyCollection;
            properties[PropertyEnum.AIInteractEntityId] = properties[PropertyEnum.AIEnticedToID];
        }

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            Game game = agent.Game;
            if (!Verify.IsNotNull(game)) return;

            bool notCompleted = false;

            if (MoveToEnticer != null)
            {
                StaticBehaviorReturnType interactResult = HandleContext(proceduralAI, ownerController, MoveToEnticer);
                if (interactResult == StaticBehaviorReturnType.Running) return;

                notCompleted = interactResult != StaticBehaviorReturnType.Completed;
            } 
            else if (FlankToEnticer != null)
            {
                StaticBehaviorReturnType interactResult = HandleContext(proceduralAI, ownerController, FlankToEnticer);
                if (interactResult == StaticBehaviorReturnType.Running) return;
                
                notCompleted = interactResult != StaticBehaviorReturnType.Completed;
            }

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            if (notCompleted)
            {
                HandleEnticerBehaviorResultStatus(game, blackboard, false);
            }
            else
            {
                if (OrientToEnticerOrientation && agent.CanRotate())
                {
                    Locomotor locomotor = agent.Locomotor;
                    if (locomotor != null)
                    {
                        ulong enticerId = blackboard.PropertyCollection[PropertyEnum.AIEnticedToID];
                        WorldEntity enticer = game.EntityManager.GetEntity<WorldEntity>(enticerId);
                        if (!Verify.IsNotNull(enticer)) return;
                        locomotor.LookAt(enticer.Forward + agent.RegionLocation.Position);
                    }
                }

                blackboard.PropertyCollection[PropertyEnum.AIFullOverride] = DynamicBehavior;
            }
        }
    }

    public class ProceduralProfileInteractEnticerOverridePrototype : ProceduralAIProfilePrototype
    {
        public InteractContextPrototype Interact { get; protected set; }

        //---

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            Game game = agent.Game;
            if (!Verify.IsNotNull(game)) return;

            if (!Verify.IsNotNull(Interact)) return;

            StaticBehaviorReturnType interactResult = HandleContext(proceduralAI, ownerController, Interact);
            if (interactResult == StaticBehaviorReturnType.Running) return;

            HandleEnticerBehaviorResultStatus(game, ownerController.Blackboard, interactResult == StaticBehaviorReturnType.Completed);
        }
    }

    public class ProceduralProfileUsePowerEnticerOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public UsePowerContextPrototype Power { get; protected set; }
        public new SelectEntityContextPrototype SelectTarget { get; protected set; }

        //---

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            Game game = agent.Game;
            if (!Verify.IsNotNull(game)) return;

            if (!Verify.IsNotNull(Power)) return;

            if (SelectTarget != null)
            {
                CombatTargetFlags flags = CombatTargetFlags.IgnoreHostile;
                WorldEntity target = ownerController.TargetEntity;
                SelectTargetEntity(agent, ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile, SelectTargetFlags.None, flags);
            }

            StaticBehaviorReturnType interactResult = HandleContext(proceduralAI, ownerController, Power);
            if (interactResult == StaticBehaviorReturnType.Running) return;

            HandleEnticerBehaviorResultStatus(game, ownerController.Blackboard, interactResult == StaticBehaviorReturnType.Completed);
        }
    }

    public class ProceduralProfileSenseOnlyPrototype : ProceduralAIProfilePrototype
    {
        public AIEntityAttributePrototype[] AttributeList { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public AlliancePrototype AllianceOverride { get; protected set; }

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

            if (HandleOverrideBehavior(ownerController))
                return;

            PropertyCollection properties = ownerController.Blackboard.PropertyCollection;

            long thinkTime = properties[PropertyEnum.AICustomTimeVal1];
            if (thinkTime == 0)
            {
                properties[PropertyEnum.AICustomTimeVal1] = currentTime;
                return;
            }

            AlliancePrototype alliance = AllianceOverride ?? agent.Alliance;

            float proximityRangeOverride = properties[PropertyEnum.AIProximityRangeOverride];
            float aggroRangeHostile = ownerController.AggroRangeHostile;
            float aggroRangeAlly = ownerController.AggroRangeAlly;

            bool enemyDetect = agent.CanEntityActionTrigger(EntitySelectorActionEventType.OnDetectedEnemy);
            bool enemyProximity = (proximityRangeOverride > aggroRangeHostile) && agent.CanEntityActionTrigger(EntitySelectorActionEventType.OnEnemyProximity);
            bool playerDetect = agent.CanEntityActionTrigger(EntitySelectorActionEventType.OnDetectedPlayer);
            bool playerProximity = (proximityRangeOverride > aggroRangeAlly) && agent.CanEntityActionTrigger(EntitySelectorActionEventType.OnPlayerProximity);
            bool friendDetect = agent.CanEntityActionTrigger(EntitySelectorActionEventType.OnDetectedFriend);

            float aggroRange = 0.0f;
            if (playerDetect || enemyDetect)
                aggroRange = Math.Max(aggroRange, aggroRangeHostile);
            if (playerDetect || friendDetect)
                aggroRange = Math.Max(aggroRange, aggroRangeAlly);

            float maxRange = Math.Max(aggroRange, proximityRangeOverride);
            if (maxRange == 0.0f)
                return;

            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return;

            if (!Verify.IsNotNull(game.EntityManager)) return;

            Vector3 position = agent.RegionLocation.Position;

            bool foundEnemy = false;
            bool foundEnemyProximity = false;
            bool foundPlayer = false;
            bool foundPlayerProximity = false;
            bool foundFriendlyEntity = false;

            Sphere volume = new(position, maxRange);

            if ((playerDetect || playerProximity) && 
                enemyDetect == false && enemyProximity == false && friendDetect == false)
            {
                foreach (Avatar worldEntity in region.IterateAvatarsInVolume(volume))
                {
                    if (!Verify.IsTrue(worldEntity != null && worldEntity.IsInWorld))
                        continue;

                    if (playerDetect &&
                        Combat.ValidTarget(game, agent, worldEntity, CombatTargetType.Ally, false, CombatTargetFlags.None, alliance, aggroRange) &&
                        CheckAttributes(ownerController, AttributeList, worldEntity))
                    {
                        foundPlayer = true;
                        foundPlayerProximity = true;
                        break;
                    }

                    if (playerProximity && foundPlayerProximity == false &&
                        Combat.ValidTarget(game, agent, worldEntity, CombatTargetType.Ally, false, CombatTargetFlags.None, alliance, proximityRangeOverride) &&
                        CheckAttributes(ownerController, AttributeList, worldEntity))
                    {
                        foundPlayerProximity = true;
                    }
                }
            }
            else
            {
                foreach (WorldEntity worldEntity in region.IterateEntitiesInVolume(volume, new(EntityRegionSPContextFlags.PrimaryPartition)))
                {
                    if (!Verify.IsNotNull(worldEntity))
                        continue;

                    if (enemyDetect && foundEnemy == false &&
                        Combat.ValidTarget(game, agent, worldEntity, CombatTargetType.Hostile, false, CombatTargetFlags.CheckAgent, alliance, aggroRangeHostile) &&
                        CheckAttributes(ownerController, AttributeList, worldEntity))
                    {
                        foundEnemy = true;
                        foundEnemyProximity = true;
                        break;
                    }                    

                    if (enemyProximity && foundEnemyProximity == false &&
                        Combat.ValidTarget(game, agent, worldEntity, CombatTargetType.Hostile, false, CombatTargetFlags.CheckAgent, alliance, proximityRangeOverride) &&
                        CheckAttributes(ownerController, AttributeList, worldEntity))
                    {
                        foundEnemyProximity = true;
                    }

                    if (foundEnemy == false && foundEnemyProximity == false)
                    {
                        if ((playerDetect || friendDetect) &&
                            (foundPlayer == false || foundFriendlyEntity == false) &&
                            Combat.ValidTarget(game, agent, worldEntity, CombatTargetType.Ally, false, CombatTargetFlags.CheckAgent, alliance, aggroRangeAlly) &&
                            CheckAttributes(ownerController, AttributeList, worldEntity))
                        {
                            if (worldEntity is Avatar)
                            {
                                foundPlayer = true;
                                foundPlayerProximity = true;
                            }
                            else
                            {
                                foundFriendlyEntity = true;
                            }
                        }

                        if (playerProximity && foundPlayerProximity == false &&
                            worldEntity is Avatar &&
                            Combat.ValidTarget(game, agent, worldEntity, CombatTargetType.Ally, false, CombatTargetFlags.CheckAgent, alliance, proximityRangeOverride) &&
                            CheckAttributes(ownerController, AttributeList, worldEntity))
                        {
                            foundPlayerProximity = true;
                        }
                    }
                }
            }

            if (foundEnemy)
                agent.TriggerEntityActionEvent(EntitySelectorActionEventType.OnDetectedEnemy);

            if (foundEnemyProximity)
                agent.TriggerEntityActionEvent(EntitySelectorActionEventType.OnEnemyProximity);

            if (foundEnemy == false && foundEnemyProximity == false)
            {
                if (foundPlayer)
                {
                    agent.TriggerEntityActionEvent(EntitySelectorActionEventType.OnDetectedPlayer);
                    SpawnGroup spawnGroup = agent.SpawnGroup;
                    if (Verify.IsNotNull(spawnGroup) && alliance != null)
                    {
                        SpawnGroupEntityQueryFilterFlags filterFlags = SpawnGroupEntityQueryFilterFlags.Allies | SpawnGroupEntityQueryFilterFlags.NotDeadDestroyedControlled;
                        if (spawnGroup.GetEntities(out List <WorldEntity> allies, filterFlags, agent.Alliance))
                        {
                            foreach (WorldEntity ally in allies)
                            {
                                if (ally != agent)
                                    ally.TriggerEntityActionEventAlly(EntitySelectorActionEventType.OnAllyDetectedPlayer);
                            }
                        }
                    }
                }

                if (foundPlayer == false && foundPlayerProximity)
                    agent.TriggerEntityActionEvent(EntitySelectorActionEventType.OnPlayerProximity);

                if (foundFriendlyEntity)
                    agent.TriggerEntityActionEvent(EntitySelectorActionEventType.OnDetectedFriend);
            }
        }

        private static bool CheckAttributes(AIController ownerController, AIEntityAttributePrototype[] attributeList, WorldEntity target)
        {
            Agent ownerAgent = ownerController.Owner;
            if (!Verify.IsNotNull(ownerAgent)) return false;

            if (target.IsInWorld == false)
                return false;

            // NOTE: Because this is initialized to true (same as the client), this will never fail under normal circumstances, which seems like a bug.
            bool check = true;

            if (attributeList.HasValue())
            {
                foreach (AIEntityAttributePrototype attrib in attributeList)
                {
                    if (!Verify.IsNotNull(attrib)) return false;

                    if (attrib.Check(ownerAgent, target))
                        return true;
                }
            }

            return check;
        }
    }

    public class ProceduralProfileLeashOverridePrototype : ProceduralAIProfilePrototype
    {
        public PrototypeId LeashReturnHeal { get; protected set; }
        public PrototypeId LeashReturnImmunity { get; protected set; }
        public MoveToContextPrototype MoveToSpawn { get; protected set; }
        public TeleportContextPrototype TeleportToSpawn { get; protected set; }
        public PrototypeId LeashReturnTeleport { get; protected set; }
        public PrototypeId LeashReturnInvulnerability { get; protected set; }

        //---

        private enum State
        {
            Default,
            Move,
            Teleport,
        }

        public override void Init(Agent agent)
        {
            base.Init(agent);

            if (LeashReturnHeal != PrototypeId.Invalid)
                InitPower(agent, LeashReturnHeal);

            if (LeashReturnImmunity != PrototypeId.Invalid)
                InitPower(agent, LeashReturnImmunity);

            if (LeashReturnTeleport != PrototypeId.Invalid)
                InitPower(agent, LeashReturnTeleport);

            if (LeashReturnInvulnerability != PrototypeId.Invalid)
                InitPower(agent, LeashReturnInvulnerability);
        }

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

            if (agent.CanMove() == false || agent.IsExecutingPower)
                return;

            PropertyCollection properties = ownerController.Blackboard.PropertyCollection;

            State state = (State)(int)properties[PropertyEnum.AICustomOverrideStateVal1];

            if (state == State.Default)
            {
                if (LeashReturnHeal != PrototypeId.Invalid)
                {
                    if (!Verify.IsTrue(ownerController.AttemptActivatePower(LeashReturnHeal, agent.Id, agent.RegionLocation.Position)))
                        return;
                }

                if (LeashReturnImmunity != PrototypeId.Invalid)
                {
                    if (!Verify.IsTrue(ownerController.AttemptActivatePower(LeashReturnImmunity, agent.Id, agent.RegionLocation.Position)))
                        return;
                }

                if (LeashReturnInvulnerability != PrototypeId.Invalid)
                {
                    if (!Verify.IsTrue(ownerController.AttemptActivatePower(LeashReturnInvulnerability, agent.Id, agent.RegionLocation.Position)))
                        return;
                }
            }

            if (state == State.Default || state == State.Move)
            {
                if (MoveToSpawn != null)
                {
                    StaticBehaviorReturnType moveResult = HandleContext(proceduralAI, ownerController, MoveToSpawn, null);

                    if (moveResult == StaticBehaviorReturnType.Running)
                    {
                        properties[PropertyEnum.AICustomOverrideStateVal1] = (int)State.Move;
                        return;
                    }
                    
                    if (moveResult == StaticBehaviorReturnType.Completed)
                    {
                        if (LeashReturnHeal != PrototypeId.Invalid)
                        {
                            if (!Verify.IsTrue(ownerController.AttemptActivatePower(LeashReturnHeal, agent.Id, agent.RegionLocation.Position)))
                                return;
                        }

                        if (LeashReturnImmunity != PrototypeId.Invalid)
                        {
                            if (!Verify.IsTrue(ownerController.AttemptActivatePower(LeashReturnImmunity, agent.Id, agent.RegionLocation.Position)))
                                return;
                        }

                        properties[PropertyEnum.AIIsLeashing] = false;
                        return;
                    }
                }

                if (LeashReturnTeleport != PrototypeId.Invalid)
                {
                    if (!Verify.IsTrue(ownerController.AttemptActivatePower(LeashReturnTeleport, agent.Id, agent.RegionLocation.Position)))
                        return;

                    properties[PropertyEnum.AICustomOverrideStateVal1] = (int)State.Teleport;
                    return;
                }
            }

            if (state == State.Teleport && agent.ActivePowerRef == LeashReturnTeleport)
                return;

            Region agentsRegion = agent.Region;
            if (!Verify.IsNotNull(agentsRegion)) return;

            if (LeashReturnHeal != PrototypeId.Invalid)
            {
                if (!Verify.IsTrue(ownerController.AttemptActivatePower(LeashReturnHeal, agent.Id, agent.RegionLocation.Position)))
                    return;
            }

            if (LeashReturnImmunity != PrototypeId.Invalid)
            {
                if (!Verify.IsTrue(ownerController.AttemptActivatePower(LeashReturnImmunity, agent.Id, agent.RegionLocation.Position)))
                    return;
            }

            if (LeashReturnInvulnerability != PrototypeId.Invalid)
            {
                if (!Verify.IsTrue(ownerController.AttemptActivatePower(LeashReturnInvulnerability, agent.Id, agent.RegionLocation.Position)))
                    return;
            }

            HandleContext(proceduralAI, ownerController, TeleportToSpawn, null);

            properties[PropertyEnum.AIIsLeashing] = false;
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

        //---

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

            WorldEntity target = ownerController.TargetEntity;
            if (target == null || target is not Transition)
            {
                SelectEntity.SelectEntityContext selectionContext = new(ownerController, SelectPortalToExitFrom);
                selectionContext.NotAffectedByPowers = true;
                WorldEntity selectedEntity = SelectEntity.DoSelectEntity(selectionContext);
                if (selectedEntity != null)
                    SelectEntity.RegisterSelectedEntity(ownerController, selectedEntity, selectionContext.SelectionType);
            }

            PropertyCollection properties = ownerController.Blackboard.PropertyCollection;

            if (DelayBeforeRunToExit != null && properties[PropertyEnum.AIRunToExitDelayFired] == false)
            {
                StaticBehaviorReturnType delayResult = HandleContext(proceduralAI, ownerController, DelayBeforeRunToExit);
                if (delayResult == StaticBehaviorReturnType.Running) return;
                
                if (delayResult == StaticBehaviorReturnType.Completed)
                    properties[PropertyEnum.AIRunToExitDelayFired] = true;
            }

            HandleMovementContext(proceduralAI, ownerController, agent.Locomotor, RunToExit, false, out StaticBehaviorReturnType movementResult);
            if (movementResult == StaticBehaviorReturnType.Running) return;

            if (movementResult == StaticBehaviorReturnType.Completed)
            {
                agent.Destroy();
                return;
            }

            if (VanishesIfMoveToExitFails)
            {
                StaticBehaviorReturnType delayResult = HandleContext(proceduralAI, ownerController, DelayBeforeDestroyOnMoveExitFail);
                if (delayResult == StaticBehaviorReturnType.Running) return;

                if (delayResult == StaticBehaviorReturnType.Completed)
                    agent.Destroy();
            }
        }
    }

    public class ProceduralProfileRotatingTurretPrototype : ProceduralAIProfilePrototype
    {
        public UsePowerContextPrototype Power { get; protected set; }
        public RotateContextPrototype Rotate { get; protected set; }

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

            if (HandleOverrideBehavior(ownerController))
                return;

            BehaviorSensorySystem senses = ownerController.Senses;
            if (senses.ShouldSense())
                senses.UpdateAvatarSensory();

            if (agent.IsDormant == false)
            {
                if (HandleContext(proceduralAI, ownerController, Power) == StaticBehaviorReturnType.Running)
                {
                    proceduralAI.PushSubstate();
                    HandleContext(proceduralAI, ownerController, Rotate);
                    proceduralAI.PopSubstate();
                }
            }
        }
    }

    public class ProceduralProfileWanderNoPowerPrototype : ProceduralAIProfilePrototype
    {
        public WanderContextPrototype WanderMovement { get; protected set; }

        //---

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            Game game = agent.Game;
            if (!Verify.IsNotNull(game)) return;

            HandleMovementContext(proceduralAI, ownerController, agent.Locomotor, WanderMovement, false, out _);
        }
    }

    public class ProceduralProfilePetFidgetPrototype : ProceduralProfilePetPrototype
    {
        public ProceduralUsePowerContextPrototype Fidget { get; protected set; }

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, Fidget);
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

            if (HandleOverrideBehavior(ownerController))
                return;

            if (agent.IsDormant)
                return;

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
                HandleMovementContext(proceduralAI, ownerController, agent.Locomotor, PetFollow, false, out var movementResult);
                
                if (movementResult != StaticBehaviorReturnType.Running) 
                {
                    if (Verify.IsTrue(Fidget != null && Fidget.PowerContext != null) &&
                        ownerController.Blackboard.PropertyCollection.HasProperty(PropertyEnum.AIAggroTime))
                    {
                        HandleUsePowerCheckCooldown(ownerController, proceduralAI, game.Random, currentTime, Fidget.PowerContext, Fidget);
                    }                        
                }

                return;
            }

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running) return;

            HandleDefaultPetMovement(proceduralAI, ownerController, currentTime, target);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            if (!Verify.IsTrue(Fidget != null && Fidget.PowerContext != null && Fidget.PowerContext.Power != null)) return;

            PropertyCollection properties = ownerController.Blackboard.PropertyCollection;

            if (properties.HasProperty(new PropertyId(PropertyEnum.AIPowerStarted, Fidget.PowerContext.Power.DataRef)))
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

            PropertyCollection blackboardProperties = ownerController.Blackboard.PropertyCollection;

            long lastUpdateTime = blackboardProperties[PropertyEnum.AICustomTimeVal1];
            int updateInterval = blackboardProperties[PropertyEnum.AICustomStateVal1];

            if (currentTime >= (lastUpdateTime + updateInterval))
            {
                blackboardProperties[PropertyEnum.AICustomTimeVal1] = currentTime;

                GRandom random = game.Random;

                updateInterval = random.Next(MinSpeedDegreeUpdateIntervalMS, MaxSpeedDegreeUpdateIntervalMS);
                blackboardProperties[PropertyEnum.AICustomStateVal1] = updateInterval;

                Vector3 direction = Vector3.Normalize(agent.Forward);
                float angle = MathHelper.ToRadians(random.Next(-RandomDegreeFromForward, RandomDegreeFromForward));
                direction = Vector3.AxisAngleRotate(direction, Vector3.ZAxis, angle);
                Orientation orientation = Orientation.FromDeltaVector(direction);
                agent.ChangeRegionPosition(null, orientation);

                Locomotor locomotor = agent.Locomotor;
                if (!Verify.IsNotNull(locomotor)) return;

                float speed = locomotor.GetCurrentSpeed() + random.Next(-MovementSpeedVariance, MovementSpeedVariance);
                agent.Properties[PropertyEnum.MovementSpeedOverride] = Math.Abs(speed);

                LocomotionOptions locomotionOptions = new() { BaseMoveSpeed = speed };
                locomotor.MoveForward(ref locomotionOptions);
            }
        }
    }

    public class ProceduralProfileFrozenOrbPrototype : ProceduralAIProfilePrototype
    {
        public int ShardBurstsPerSecond { get; protected set; }
        public int ShardsPerBurst { get; protected set; }
        public int ShardRotationSpeed { get; protected set; }
        public PrototypeId ShardPower { get; protected set; }

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, ShardPower);
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

            PropertyCollection properties = ownerController.Blackboard.PropertyCollection;

            long lastBurstTime = properties[PropertyEnum.AICustomTimeVal1];
            int burstDelta = 1000 / ShardBurstsPerSecond;

            if ((currentTime - lastBurstTime) < burstDelta)
                return;

            properties[PropertyEnum.AICustomTimeVal1] = currentTime;

            float delta = (float)game.FixedTimeBetweenUpdates.TotalSeconds;
            int lastAngle = properties[PropertyEnum.AICustomStateVal1];
            int angle = (lastAngle + (int)(Math.Abs(ShardRotationSpeed) * delta)) % 360;
            properties[PropertyEnum.AICustomStateVal1] = angle;

            int shardStep = 360 / ShardsPerBurst;
            Vector3 shardDirection = Vector3.Flatten(agent.Forward, Axis.Z);

            for (int i = 0; i < ShardsPerBurst; i++)
            {
                int shardAngle = angle + i * shardStep;
                Transform3 transform = Transform3.BuildTransform(Vector3.Zero, new Orientation(MathHelper.ToRadians(shardAngle), 0.0f, 0.0f));
                shardDirection = transform * shardDirection;

                ownerController.AttemptActivatePower(ShardPower, 0, agent.RegionLocation.Position + shardDirection * 100.0f);
            }
        }
    }

    public class ProceduralProfilePetDirectedPrototype : ProceduralProfilePetPrototype
    {
        public ProceduralUsePowerContextPrototype[] DirectedPowers { get; protected set; }

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPowers(agent, DirectedPowers);
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

            if (HandleOverrideBehavior(ownerController))
                return;

            if (agent.IsDormant)
                return;

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            WorldEntity target = ownerController.TargetEntity;
            WorldEntity master = ownerController.AssistedEntity;

            Queue<CustomPowerQueueEntry> powerQueue = blackboard.CustomPowerQueue;

            if (powerQueue != null && powerQueue.Count > 0)
            {
                CustomPowerQueueEntry customPowerEntry = powerQueue.Peek();

                PrototypeId customPowerDataRef = customPowerEntry.PowerRef;
                if (!Verify.IsTrue(customPowerDataRef != PrototypeId.Invalid)) return;

                ProceduralUsePowerContextPrototype procUsePowerContextProto = GetDirectedPowerUseContext(customPowerDataRef);
                if (!Verify.IsNotNull(procUsePowerContextProto, $"Failed to get directed power use context [{customPowerDataRef.GetName()}] for agent [{agent}]"))
                    return;

                UsePowerContextPrototype usePowerContextProto = procUsePowerContextProto.PowerContext;
                if (!Verify.IsNotNull(usePowerContextProto)) return;

                bool customPowerUse = false;

                if (proceduralAI.GetState(0) == UsePower.Instance)
                {
                    if (ownerController.ActivePowerRef != customPowerDataRef)
                    {
                        proceduralAI.SwitchProceduralState(null, null, StaticBehaviorReturnType.Interrupted);
                        blackboard.UsePowerTargetPos = customPowerEntry.TargetPos;
                    }
                    else
                    {
                        customPowerUse = true;
                    }
                }
                else
                {
                    blackboard.UsePowerTargetPos = customPowerEntry.TargetPos;
                }

                if (customPowerEntry.TargetId != 0 && (target == null || target.Id != customPowerEntry.TargetId))
                {
                    WorldEntity targetEntity = game.EntityManager.GetEntity<WorldEntity>(customPowerEntry.TargetId);
                    ownerController.ResetCurrentTargetState();
                    ownerController.SetTargetEntity(targetEntity);
                }

                StaticBehaviorReturnType powerResult = HandleUsePowerContext(ownerController, proceduralAI, game.Random, currentTime, usePowerContextProto, procUsePowerContextProto);
                if (powerResult == StaticBehaviorReturnType.Failed && customPowerUse == false)
                {
                    if (Verify.IsTrue(powerQueue.Count > 0, $"Custom power queue already empty when handling failed power use [{customPowerDataRef.GetName()}] for agent [{agent}]"))
                        powerQueue.Dequeue();
                }

                if (powerResult == StaticBehaviorReturnType.Running) return;
            }

            CommonSimplifiedSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile);

            if (master != null && master.IsInWorld)
            {
                if (blackboard.PropertyCollection.HasProperty(PropertyEnum.AICustomStateVal1) == true)
                {
                    StaticBehaviorReturnType movetoResult = HandleContext(proceduralAI, ownerController, PetFollow);
                    if (movetoResult == StaticBehaviorReturnType.Completed || movetoResult == StaticBehaviorReturnType.Failed)
                    {
                        blackboard.PropertyCollection[PropertyEnum.AILastAttackerID] = 0;
                        blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = false;
                        ownerController.ResetCurrentTargetState();
                    }
                    else if (movetoResult == StaticBehaviorReturnType.Running) return;
                }

                float distanceToMasterSq = Vector3.DistanceSquared2D(agent.RegionLocation.Position, master.RegionLocation.Position);
                if (distanceToMasterSq > MaxDistToMasterBeforeTeleport * MaxDistToMasterBeforeTeleport)
                {
                    blackboard.PropertyCollection[PropertyEnum.AILastAttackerID] = 0;
                    HandleContext(proceduralAI, ownerController, TeleportToMasterIfTooFarAway);
                    ownerController.ResetCurrentTargetState();
                }
            }

            if (target == null)
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

        public override void OnPowerEnded(AIController ownerController, ProceduralUsePowerContextPrototype proceduralPowerContext)
        {
            base.OnPowerEnded(ownerController, proceduralPowerContext);

            UsePowerContextPrototype powerContext = proceduralPowerContext.PowerContext;
            if (!Verify.IsNotNull(powerContext)) return;

            BehaviorBlackboard blackboard = ownerController.Blackboard;

            Queue<CustomPowerQueueEntry> powerQueue = blackboard.CustomPowerQueue;
            if (powerQueue != null && powerQueue.Count > 0)
            {
                PrototypeId customPowerDataRef = powerQueue.Peek().PowerRef;
                if (powerContext.Power != null && powerContext.Power.DataRef == customPowerDataRef)
                {
                    powerQueue.Dequeue();
                    if (powerQueue.Count == 0)
                        blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AICustomThinkRateMS);
                }
            }
        }

        private ProceduralUsePowerContextPrototype GetDirectedPowerUseContext(PrototypeId directedPowerDataRef)
        {
            if (DirectedPowers.HasValue())
            {
                foreach (ProceduralUsePowerContextPrototype proceduralPowerContext in DirectedPowers)
                {
                    if (!Verify.IsNotNull(proceduralPowerContext))
                        continue;

                    if (!Verify.IsNotNull(proceduralPowerContext.PowerContext))
                        continue;

                    if (!Verify.IsNotNull(proceduralPowerContext.PowerContext.Power))
                        continue;

                    if (proceduralPowerContext.PowerContext.Power.DataRef == directedPowerDataRef)
                        return proceduralPowerContext;
                }
            }

            return null;
        }
    }

    // SkrullThorProfile
    public class ProceduralProfileSyncAttackPrototype : ProceduralProfileBasicMeleePrototype
    {
        public ProceduralSyncAttackContextPrototype[] SyncAttacks { get; protected set; }

        //---

        private const int IDPropertiesLength = 4;
        private readonly PropertyEnum[] IDProperties = new PropertyEnum[IDPropertiesLength]
        {
            PropertyEnum.AICustomEntityId1, 
            PropertyEnum.AICustomEntityId2, 
            PropertyEnum.AICustomEntityId3, 
            PropertyEnum.AICustomEntityId4
        };

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            Game game = agent.Game;
            if (!Verify.IsNotNull(game)) return;

            long currentTime = (long)game.CurrentTime.TotalMilliseconds;

            if (HandleOverrideBehavior(ownerController))
                return;

            PropertyCollection properties = ownerController.Blackboard.PropertyCollection;
            EntityManager entityManager = game.EntityManager;
            bool updateSync = false;
            for (int i = 0; i < IDPropertiesLength && i < SyncAttacks.Length; i++)
            {
                ulong targetId = properties[IDProperties[i]];
                Agent targetAgent = null;
                if (targetId != 0) targetAgent = entityManager.GetEntity<Agent>(targetId);
                if (targetAgent == null || targetAgent.IsDead)
                {
                    properties[IDProperties[i]] = 0;
                    updateSync = true;
                    break;
                }
            }

            if (updateSync)
            {
                Region region = agent.Region;
                if (!Verify.IsNotNull(region)) return;

                float maxRange = ownerController.AggroRangeAlly;
                Sphere volume = new(agent.RegionLocation.Position, maxRange);
                foreach (WorldEntity worldEntity in region.IterateEntitiesInVolume(volume, new(EntityRegionSPContextFlags.PrimaryPartition)))
                {
                    if (worldEntity is not Agent targetAgent)
                        continue;

                    for (int index = 0; index < SyncAttacks.Length; index++)
                    {
                        ProceduralSyncAttackContextPrototype syncAttack = SyncAttacks[index];
                        if (!Verify.IsNotNull(syncAttack))
                            continue;

                        if (syncAttack.TargetEntity == targetAgent.PrototypeDataRef)
                        {
                            InitPower(agent, syncAttack.LeaderPower);
                            InitPower(targetAgent, syncAttack.TargetEntityPower);
                            AIController targetController = targetAgent.AIController;
                            if (targetController != null)
                                targetController.Blackboard.PropertyCollection[PropertyEnum.AISyncAttackTargetPower] = syncAttack.TargetEntityPower.DataRef;
                            properties[IDProperties[index]] = targetAgent.Id;
                        }
                    }
                }
            }

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false
                && proceduralAI.PartialOverrideBehavior == null) return;

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running) return;

            DefaultRangedMovement(proceduralAI, ownerController, agent, target, MoveToTarget, OrbitTarget);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);

            PrototypeId startedPowerRef = ownerController.ActivePowerRef;
            if (startedPowerRef != PrototypeId.Invalid)
            {
                foreach (ProceduralSyncAttackContextPrototype itSyncAttackProto in SyncAttacks)
                {
                    if (!Verify.IsTrue(itSyncAttackProto != null && itSyncAttackProto.LeaderPower != null && itSyncAttackProto.LeaderPower.PowerContext != null))
                        continue;

                    UsePowerContextPrototype powerContext = itSyncAttackProto.LeaderPower.PowerContext;
                    if (powerContext.Power.DataRef == startedPowerRef)
                    {
                        ownerController.AddPowersToPicker(powerPicker, itSyncAttackProto.LeaderPower);
                        return;
                    }
                }
            }

            Agent leader = ownerController.Owner;
            if (!Verify.IsNotNull(leader)) return;

            Game game = leader.Game;
            if (!Verify.IsNotNull(game)) return;

            BehaviorBlackboard ownerBlackboard = ownerController.Blackboard;

            int syncAttackIndex = GetRandomSyncAttackIndex(ownerBlackboard, game);
            if (!Verify.IsTrue(syncAttackIndex >= 0 && syncAttackIndex < IDPropertiesLength)) return;

            ulong targetId = ownerBlackboard.PropertyCollection[IDProperties[syncAttackIndex]];            
            Agent target = game.EntityManager.GetEntity<Agent>(targetId);
            if (!Verify.IsNotNull(target)) return;

            ProceduralSyncAttackContextPrototype syncAttackProto = SyncAttacks[syncAttackIndex];
            if (!Verify.IsNotNull(syncAttackProto)) return;

            AIController targetController = target.AIController;
            if (!Verify.IsNotNull(targetController)) return;

            BehaviorBlackboard targetBlackboard = targetController.Blackboard;

            ProceduralAI targetAI = targetController.Brain;
            if (!Verify.IsNotNull(targetAI)) return;

            ulong tempEntityId = targetBlackboard.PropertyCollection[PropertyEnum.AIRawTargetEntityID];
            targetBlackboard.PropertyCollection[PropertyEnum.AIRawTargetEntityID] = leader.Id;

            if (ValidateUsePowerContext(targetController, targetAI, syncAttackProto.TargetEntityPower.PowerContext))
            {
                ownerBlackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = syncAttackIndex;
                ownerController.AddPowersToPicker(powerPicker, syncAttackProto.LeaderPower);
            }

            targetBlackboard.PropertyCollection[PropertyEnum.AIRawTargetEntityID] = tempEntityId;
        }

        private int GetRandomSyncAttackIndex(BehaviorBlackboard blackboard, Game game)
        {
            if (SyncAttacks.IsNullOrEmpty())
                return -1;

            if (!Verify.IsTrue(IDPropertiesLength >= SyncAttacks.Length, $"AI has more SyncAttacks than supported! Max supported is {IDPropertiesLength}! AI: {this}"))
                return -1;

            EntityManager entityManager = game.EntityManager;
            using var syncAttackIndicesHandle = ListPool<int>.Instance.Get(out List<int> syncAttackIndices);

            for (int i = 0; i < IDPropertiesLength && i < SyncAttacks.Length; i++)
            {
                ulong targetId = blackboard.PropertyCollection[IDProperties[i]];
                Agent target = entityManager.GetEntity<Agent>(targetId);
                if (target != null && target.IsDead == false)
                    syncAttackIndices.Add(i);
            }

            int index = -1;
            if (syncAttackIndices.Count > 0)
            {
                int randomIndex = game.Random.Next(0, syncAttackIndices.Count);
                index = syncAttackIndices[randomIndex];
            }

            return index;
        }

        public override void OnPowerStarted(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            PropertyCollection properties = ownerController.Blackboard.PropertyCollection;

            int lastSyncAttackIndex = properties[PropertyEnum.AICustomStateVal1];
            if (lastSyncAttackIndex == -1)
                return;

            if (!Verify.IsTrue(SyncAttacks.HasValue())) return;
            if (!Verify.IsTrue(lastSyncAttackIndex >= 0 && lastSyncAttackIndex < SyncAttacks.Length)) return;
            
            ProceduralSyncAttackContextPrototype syncAttackProto = SyncAttacks[lastSyncAttackIndex];
            if (!Verify.IsNotNull(syncAttackProto)) return;

            if (syncAttackProto.LeaderPower != powerContext)
            {
                properties[PropertyEnum.AICustomStateVal1] = -1;
                return;
            }

            if (!Verify.IsTrue(lastSyncAttackIndex >= 0 && lastSyncAttackIndex < IDPropertiesLength)) return;

            ulong targetId = properties[IDProperties[lastSyncAttackIndex]];
            Agent leader = ownerController.Owner;
            if (!Verify.IsNotNull(leader)) return;
            Game game = leader.Game;
            if (!Verify.IsNotNull(game)) return;

            Agent target = game.EntityManager.GetEntity<Agent>(targetId);
            if (!Verify.IsNotNull(target)) return;
            AIController targetController = target.AIController;
            if (!Verify.IsNotNull(targetController)) return;
            ProceduralAI targetAI = targetController.Brain;
            if (!Verify.IsNotNull(targetAI)) return;

            targetController.SetTargetEntity(leader);

            ProceduralUsePowerContextPrototype targetEntityPowerProto = syncAttackProto.TargetEntityPower;
            if (!Verify.IsNotNull(targetEntityPowerProto)) return;
            if (!Verify.IsNotNull(targetEntityPowerProto.PowerContext)) return;
            if (!Verify.IsNotNull(targetEntityPowerProto.PowerContext.Power)) return;

            Power targetEntityPower = target.GetPower(targetEntityPowerProto.PowerContext.Power.DataRef);
            if (!Verify.IsNotNull(targetEntityPower, $"SyncAttack target doesn't have TargetEntityPower assigned! \n Target: {target}\n Leader: {leader}\n Power: {targetEntityPowerProto.PowerContext.Power}"))
                return;

            TimeSpan nextUpdateTime = game.CurrentTime + targetEntityPower.GetFullExecutionTime();
            targetController.Blackboard.PropertyCollection[PropertyEnum.AINextSensoryUpdate] = (long)nextUpdateTime.TotalMilliseconds;

            target.OrientToward(leader.RegionLocation.Position);
            long currentTime = (long)game.CurrentTime.TotalMilliseconds;
            HandleUsePowerContext(targetController, targetAI, game.Random, currentTime, targetEntityPowerProto.PowerContext, targetEntityPowerProto);
        }

        public override bool OnPowerPicked(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            if (base.OnPowerPicked(ownerController, powerContext) == false)
                return false;

            PropertyCollection properties = ownerController.Blackboard.PropertyCollection;
            int lastSyncAttackIndex = properties[PropertyEnum.AICustomStateVal1];
            if (lastSyncAttackIndex == -1)
                return true;

            if (SyncAttacks.IsNullOrEmpty() || lastSyncAttackIndex < 0 || lastSyncAttackIndex >= SyncAttacks.Length) return false;

            if (!Verify.IsTrue(SyncAttacks.HasValue())) return false;
            if (!Verify.IsTrue(lastSyncAttackIndex >= 0 && lastSyncAttackIndex < SyncAttacks.Length)) return false;

            ProceduralSyncAttackContextPrototype syncAttackProto = SyncAttacks[lastSyncAttackIndex];
            if (!Verify.IsNotNull(syncAttackProto)) return false;

            if (syncAttackProto.LeaderPower != powerContext)
            {
                properties[PropertyEnum.AICustomStateVal1] = -1;
                return true;
            }

            ulong targetId = properties[IDProperties[lastSyncAttackIndex]];
            Game game = ownerController.Game;
            if (!Verify.IsNotNull(game)) return false;

            Agent target = game.EntityManager.GetEntity<Agent>(targetId);
            if (!Verify.IsNotNull(target)) return false;
            Agent leader = ownerController.Owner;
            if (!Verify.IsNotNull(leader)) return false;

            ownerController.SetTargetEntity(target);
            leader.OrientToward(target.RegionLocation.Position);

            return true;
        }
    }

    public class ProceduralProfileLOSRangedPrototype : ProceduralProfileBasicRangePrototype
    {
        public ProceduralUsePowerContextPrototype LOSChannelPower { get; protected set; }

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, LOSChannelPower);
        }

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            Game game = agent.Game;
            if (!Verify.IsNotNull(game)) return;

            Power activePower = agent.ActivePower;

            UsePowerContextPrototype powerContext = LOSChannelPower?.PowerContext;
            if (!Verify.IsNotNull(powerContext)) return;
            if (!Verify.IsNotNull(powerContext.Power)) return;

            if (activePower != null && activePower.PrototypeDataRef == powerContext.Power.DataRef)
            {
                WorldEntity target = ownerController.TargetEntity;
                if (target == null || activePower.IsInRange(target, RangeCheckType.Application) == false || agent.LineOfSightTo(target) == false)
                    proceduralAI.SwitchProceduralState(null, null, StaticBehaviorReturnType.Interrupted);
                else
                    HandleRotateToTarget(agent, target);

                return;
            } 

            base.Think(ownerController);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            ownerController.AddPowersToPicker(powerPicker, LOSChannelPower);
            base.PopulatePowerPicker(ownerController, powerPicker);
        }
    }

    public class ProcProfileSpikeDanceControllerPrototype : ProceduralAIProfilePrototype
    {
#if GAME_VERSION_1_53
        public PrototypeId OwnerAgent { get; protected set; }
#else
        public PrototypeId Onslaught { get; protected set; }
#endif
        public PrototypeId SpikeDanceMob { get; protected set; }
        public int MaxSpikeDanceActivations { get; protected set; }
        public float SpikeDanceMobSearchRadius { get; protected set; }

        //---

        private enum State
        {
            Default,
            SpikeDance,
            SpikeDanceSingle,
        }

        public override void Init(Agent agent)
        {
            base.Init(agent);

            AIController ownerController = agent.AIController;
            if (!Verify.IsNotNull(ownerController)) return;

            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return;

            ownerController.RegisterForAIBroadcastBlackboardEvents(region, true);
            ownerController.SetIsEnabled(false);
        }

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            Game game = agent.Game;
            if (!Verify.IsNotNull(game)) return;

            if (ownerController.TargetEntity == null)
                SelectEntity.RegisterSelectedEntity(ownerController, agent, SelectEntityType.SelectTarget);

            PropertyCollection properties = ownerController.Blackboard.PropertyCollection;
            State state = (State)(int)properties[PropertyEnum.AICustomStateVal1];
            using var targetsHandle = ListPool<Agent>.Instance.Get(out List<Agent> targets);

            if (state == State.SpikeDance)
            {
                Game ownerGame = ownerController.Game;
                if (!Verify.IsNotNull(ownerGame)) return;

                int numSpikes = ownerGame.Random.Next(1, MaxSpikeDanceActivations + 1);
                GetSpikeDanceMobTargets(ownerController, targets, numSpikes);
            }
            else if (state == State.SpikeDanceSingle)
            {
                GetSpikeDanceMobTargets(ownerController, targets, 1);
            }

            foreach (Agent spikeDanceMob in targets)
            {
                if (!Verify.IsNotNull(spikeDanceMob))
                    continue;

                AIController mobController = spikeDanceMob.AIController;
                if (!Verify.IsNotNull(mobController))
                    continue;

                mobController.SetIsEnabled(true);
                mobController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = (int)State.SpikeDance;
            }

            properties[PropertyEnum.AICustomStateVal1] = (int)State.Default;
            ownerController.SetIsEnabled(false);
        }

        private void GetSpikeDanceMobTargets(AIController ownerController, List<Agent> targets, int numSpikes)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return;
            Game game = ownerController.Game;
            if (!Verify.IsNotNull(game)) return;

            Picker<Agent> targetPicker = new(game.Random);
            Sphere volume = new (agent.RegionLocation.Position, SpikeDanceMobSearchRadius);
            foreach (WorldEntity entity in region.IterateEntitiesInVolume(volume, new(EntityRegionSPContextFlags.UnrestrictedPartitions)))
            {
                if (entity is not Agent entityAgent)
                    continue;

                if (GameDatabase.DataDirectory.PrototypeIsAPrototype(entityAgent.PrototypeDataRef, SpikeDanceMob) == false)
                    continue;

                targetPicker.Add(entityAgent);
            }

            for (int i = 0; i < numSpikes && targetPicker.Empty() == false; i++)
            {
                if (targetPicker.PickRemove(out Agent randomAgent))
                    targets.Add(randomAgent);
            }
        }

        public override void OnAIBroadcastBlackboardEvent(AIController ownerController, in AIBroadcastBlackboardGameEvent broadcastEvent)
        {
            WorldEntity broadcaster = broadcastEvent.Broadcaster;
            if (!Verify.IsNotNull(broadcaster)) return;
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            BehaviorBlackboard broadcasterBlackboard = broadcastEvent.Blackboard;
            if (!Verify.IsNotNull(broadcasterBlackboard)) return;

            State stateVal = (State)(int)broadcasterBlackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
#if GAME_VERSION_1_53
            if (broadcaster.PrototypeDataRef == OwnerAgent &&
#else
            if (broadcaster.PrototypeDataRef == Onslaught &&
#endif
                (stateVal == State.SpikeDance || stateVal == State.SpikeDanceSingle))
            {
                ownerController.SetIsEnabled(true);
                ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = (int)stateVal;
            }
        }
    }

#if GAME_VERSION_1_53
    public class ProfLavaWaveRaidControllerPrototype : ProcProfileSpikeDanceControllerPrototype
    {
        //---

        public override void OnAIBroadcastBlackboardEvent(AIController ownerController, in AIBroadcastBlackboardGameEvent broadcastEvent)
        {
            // V53_TODO
        }
    }
#endif

    public class ProceduralProfileMeleeRevengePrototype : ProceduralProfileBasicMeleePrototype
    {
        public ProceduralUsePowerContextPrototype RevengePower { get; protected set; }
        public PrototypeId RevengeSupport { get; protected set; }

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);
            
            InitPower(agent, RevengePower);

            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return;
            AIController ownerController = agent.AIController;
            if (!Verify.IsNotNull(ownerController)) return;
            
            ownerController.RegisterForEntityDeadEvents(region, true);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);

            int stateVal = ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            if (stateVal == 1)
                ownerController.AddPowersToPicker(powerPicker, RevengePower);
        }

        public override void OnEntityDeadEvent(AIController ownerController, in EntityDeadGameEvent deadEvent)
        {
            if (!Verify.IsNotNull(deadEvent.Defender)) return;

            if (deadEvent.Defender.PrototypeDataRef == RevengeSupport)
                ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = 1;
        }
    }

    public class ProceduralProfileRangedRevengePrototype : ProceduralProfileBasicRangePrototype
    {
        public ProceduralUsePowerContextPrototype RevengePower { get; protected set; }
        public PrototypeId RevengeSupport { get; protected set; }

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, RevengePower);

            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return;
            AIController ownerController = agent.AIController;
            if (!Verify.IsNotNull(ownerController)) return;
            
            ownerController.RegisterForEntityDeadEvents(region, true);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);

            int stateVal = ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            if (stateVal == 1)
                ownerController.AddPowersToPicker(powerPicker, RevengePower);
        }

        public override void OnEntityDeadEvent(AIController ownerController, in EntityDeadGameEvent deadEvent)
        {
            if (!Verify.IsNotNull(deadEvent.Defender)) return;

            if (deadEvent.Defender.PrototypeDataRef == RevengeSupport)
                ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = 1;
        }
    }

#if GAME_VERSION_1_53
    public class ProceduralProfileLeashCoopPlayerPrototype : ProceduralAIProfilePrototype
    {
        public MoveToContextPrototype AvatarFollow { get; protected set; }

        //---

        public override void Think(AIController ownerController)
        {
            // V53_TODO
            base.Think(ownerController);
        }
    }
#endif
}
