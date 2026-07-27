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
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Powers.Conditions;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.GameData.Prototypes
{
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

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, MeleePower);
            InitPower(agent, SpecialPower);
            InitPower(agent, SpecialSummonPower);
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

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                proceduralAI.PartialOverrideBehavior == null)
            {
                return;
            }

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                return;

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            int state = blackboardProps[PropertyEnum.AICustomStateVal1];
            if (state == 0)
                DefaultRangedMovement(proceduralAI, ownerController, agent, target, MoveToTarget, OrbitTarget);
            else
                DefaultRangedMovement(proceduralAI, ownerController, agent, target, MoveIntoMeleeRange, OrbitTarget);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);

            ownerController.AddPowersToPicker(powerPicker, MeleePower);
            ownerController.AddPowersToPicker(powerPicker, SpecialPower);
        }

        public override bool OnPowerPicked(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            if (base.OnPowerPicked(ownerController, powerContext) == false)
                return false;

            int state = ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            if (powerContext != MeleePower && state == 1)
                return false;

            return true;
        }

        public override void OnPowerStarted(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            if (powerContext == SpecialPower)
            {
                Agent agent = ownerController.Owner;
                if (!Verify.IsNotNull(agent)) return;

                Region region = agent.Region;
                if (!Verify.IsNotNull(region)) return;

                const int MaxTargets = 32;
                using var validTargetsHandle = ListPool<WorldEntity>.Instance.Get(out List<WorldEntity> validTargets);

                Sphere volume = new(agent.RegionLocation.Position, SpecialPowerMaxRadius);
                foreach (WorldEntity targetInSphere in region.IterateEntitiesInVolume(volume, new(EntityRegionSPContextFlags.PrimaryPartition)))
                {
                    if (!Verify.IsNotNull(targetInSphere))
                        continue;

                    if (Combat.ValidTarget(agent.Game, agent, targetInSphere, CombatTargetType.Hostile, false, CombatTargetFlags.IgnoreAggroDistance))
                        validTargets.Add(targetInSphere);

                    if (validTargets.Count >= MaxTargets)
                        break;
                }

                int targetsSummoned = 0;

                foreach (WorldEntity validTarget in validTargets)
                {
                    if (!Verify.IsNotNull(validTarget))
                        continue;

                    ref RegionLocation targetRegionLoc = ref validTarget.RegionLocation;
                    if (!Verify.IsTrue(ownerController.AttemptActivatePower(SpecialSummonPower, validTarget.Id, targetRegionLoc.ProjectToFloor())))
                        return;

                    targetsSummoned++;
                    if (targetsSummoned >= SpecialPowerNumSummons)
                        break;
                }

                if (targetsSummoned < SpecialPowerNumSummons)
                {
                    for (int i = targetsSummoned; i < SpecialPowerNumSummons; i++)
                    {
                        Bounds bounds = agent.Bounds;   // copy
                        bounds.Center = agent.RegionLocation.ProjectToFloor();

                        region.ChooseRandomPositionNearPoint(
                            ref bounds,
                            Region.GetPathFlagsForEntity(agent.WorldEntityPrototype),
                            PositionCheckFlags.CanBeBlockedEntity | PositionCheckFlags.CanSweepTo | PositionCheckFlags.PreferNoEntity,
                            BlockingCheckFlags.None,
                            SpecialPowerMinRadius,
                            SpecialPowerMaxRadius,
                            out Vector3 randomPosition);

                        if (!Verify.IsTrue(ownerController.AttemptActivatePower(SpecialSummonPower, 0, randomPosition)))
                            return;
                    }
                }
            }
        }

        public override void OnPowerEnded(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            base.OnPowerEnded(ownerController, powerContext);

            if (powerContext == MeleePower)
                ownerController.Blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AICustomStateVal1);
        }

        public override void OnPowerAttempted(AIController ownerController, ProceduralUsePowerContextPrototype powerContext, StaticBehaviorReturnType contextResult)
        {
            if (contextResult == StaticBehaviorReturnType.Failed && powerContext == MeleePower)
            {
                ProceduralAI proceduralAI = ownerController.Brain;
                if (!Verify.IsNotNull(proceduralAI)) return;
                Agent agent = ownerController.Owner;
                if (!Verify.IsNotNull(agent)) return;

                PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;

                WorldEntity target = ownerController.TargetEntity;
                if (target == null)
                    return;

                float distanceSq = Vector3.DistanceSquared2D(agent.RegionLocation.Position, target.RegionLocation.Position);
                if (distanceSq <= MaxDistToMoveIntoMelee * MaxDistToMoveIntoMelee)
                {
                    if (HandleMovementContext(proceduralAI, ownerController, agent.Locomotor, MoveIntoMeleeRange, true, out StaticBehaviorReturnType movementResult) == false)
                        return;

                    if (movementResult == StaticBehaviorReturnType.Running)
                    {
                        blackboardProps[PropertyEnum.AICustomStateVal1] = 1;
                        return;
                    }
                }
            }
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

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, HealingPower);
            InitPower(agent, SpecialPower);
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

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                proceduralAI.PartialOverrideBehavior == null)
            {
                return;
            }

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            StaticBehaviorReturnType powerResult = HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true);

            if (powerResult == StaticBehaviorReturnType.Running || powerResult == StaticBehaviorReturnType.Completed)
            {
                PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
                PrototypeId activePowerRef = blackboardProps[PropertyEnum.AILastPowerActivated];

                if (!Verify.IsNotNull(SpecialPower)) return;
                if (!Verify.IsNotNull(SpecialPower.PowerContext)) return;
                if (!Verify.IsNotNull(SpecialPower.PowerContext.Power)) return;

                if (activePowerRef == PrototypeId.Invalid || SpecialPower.PowerContext.Power.DataRef != activePowerRef)
                    return;            
                
                if (powerResult == StaticBehaviorReturnType.Running)
                {
                    int changeTargetCount = blackboardProps[PropertyEnum.AICustomStateVal1];
                    if (changeTargetCount == 0)
                    {
                        blackboardProps[PropertyEnum.AICustomStateVal1] = 1;
                    }
                    else
                    {
                        long powerStartTime = agent.Properties[PropertyEnum.PowerCooldownStartTime, activePowerRef];
                        if (currentTime > powerStartTime + SpecialPowerChangeTgtIntervalMS * changeTargetCount)
                        {
                            SelectEntity.SelectEntityContext selectionContext = new(ownerController, SpecialPowerSelectTarget);
                            WorldEntity selectedEntity = SelectEntity.DoSelectEntity(selectionContext);
                            if (selectedEntity == null)
                                return;

                            if (!Verify.IsTrue(SelectEntity.RegisterSelectedEntity(ownerController, selectedEntity, selectionContext.SelectionType)))
                                return;

                            blackboardProps[PropertyEnum.AICustomStateVal1] = changeTargetCount + 1;
                        }
                    }

                    target = ownerController.TargetEntity;
                    if (!Verify.IsNotNull(target)) return;

                    Locomotor locomotor = agent.Locomotor;
                    ulong targetId = target.Id;
                    locomotor.FollowEntity(targetId, agent.Bounds.Radius);
                }
                else
                {
                    Locomotor locomotor = agent.Locomotor;
                    locomotor.Stop();
                    blackboardProps[PropertyEnum.AICustomStateVal1] = 0;
                }
                
                return;
            }

            DefaultMeleeMovement(proceduralAI, ownerController, agent.Locomotor, target, MoveToTarget, OrbitTarget);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);

            ownerController.AddPowersToPicker(powerPicker, SpecialPower);

            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

            if (!Verify.IsNotNull(HealingPower)) return;
            if (!Verify.IsNotNull(HealingPower.PowerContext)) return;
            if (!Verify.IsNotNull(HealingPower.PowerContext.Power)) return;

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            if (blackboardProps.HasProperty(new PropertyId(PropertyEnum.AIPowerStarted, HealingPower.PowerContext.Power.DataRef)))
            {
                ownerController.AddPowersToPicker(powerPicker, HealingPower);
            }
            else
            {
                CombatTargetFlags flags = CombatTargetFlags.IgnoreAggroDistance | CombatTargetFlags.IgnoreStealth | CombatTargetFlags.IgnoreLOS;
                if (Combat.GetNumTargetsInRange(agent, 200.0f, 0.0f, CombatTargetType.Hostile, flags) == 0)
                    ownerController.AddPowersToPicker(powerPicker, HealingPower);
            }
        }
    }

    public class ProceduralProfileMagnetoPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralFlankContextPrototype FlankTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype ChasePower { get; protected set; }

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, ChasePower);
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

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                proceduralAI.PartialOverrideBehavior == null)
            {
                return;
            }

            if (proceduralAI.GetState(0) == Flank.Instance)
            {
                HandleProceduralFlank(proceduralAI, ownerController, agent.Locomotor, currentTime, FlankTarget, true);
                return;
            }

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            StaticBehaviorReturnType powerResult = HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true);

            if (powerResult == StaticBehaviorReturnType.Running || powerResult == StaticBehaviorReturnType.Completed)
            {
                PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
                PrototypeId lastPowerRef = blackboardProps[PropertyEnum.AILastPowerActivated];

                UsePowerContextPrototype chasePowerContext = ChasePower?.PowerContext;
                if (!Verify.IsNotNull(chasePowerContext)) return;
                if (!Verify.IsNotNull(chasePowerContext.Power)) return;

                if (lastPowerRef != chasePowerContext.Power.DataRef || target == null)
                    return;
                
                ulong targetId = target.Id;
                Locomotor locomotor = agent.Locomotor;
                if (powerResult == StaticBehaviorReturnType.Running)
                {
                    if (locomotor.FollowEntityId != targetId)
                        locomotor.FollowEntity(targetId);
                }
                else
                {
                    locomotor.Stop();
                }              

                return;
            }

            DefaultRangedFlankerMovement(proceduralAI, ownerController, agent, target, currentTime, MoveToTarget, FlankTarget);
        }

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

        //---

        private enum WaveState
        {
            Wave1,
            Wave2,
            Wave3,
        }

        private enum SummonState
        {
            NotSummoning,
            Summoning,
        }

        private const int NumClonesPerWave = 3;

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, CloneCylSummonFXPower);
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

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                proceduralAI.PartialOverrideBehavior == null)
            {
                return;
            }

            GRandom random = game.Random;
            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            if (blackboardProps[PropertyEnum.AICustomStateVal2] == (int)SummonState.Summoning)
            {
                if (HandleUsePowerContext(ownerController, proceduralAI, random, currentTime, CloneCylSummonFXPower) == StaticBehaviorReturnType.Running)
                    return;

                blackboardProps[PropertyEnum.AICustomStateVal2] = (int)SummonState.NotSummoning;
            }
            else if (proceduralAI.GetState(0) != UsePower.Instance)
            {
                long health = agent.Properties[PropertyEnum.Health];
                long maxHealth = agent.Properties[PropertyEnum.HealthMax];
                float healthPct = maxHealth != 0 ? MathHelper.Ratio(health, maxHealth) : 0f;

                bool triggerSpawner = false;
                int waveState = blackboardProps[PropertyEnum.AICustomStateVal1];
                switch ((WaveState)waveState)
                {
                    case WaveState.Wave1:
                        triggerSpawner = healthPct < CloneCylHealthPctThreshWave1;
                        break;
                    case WaveState.Wave2:
                        triggerSpawner = healthPct < CloneCylHealthPctThreshWave2;
                        break;
                    case WaveState.Wave3:
                        triggerSpawner = healthPct < CloneCylHealthPctThreshWave3;
                        break;
                }

                if (triggerSpawner)
                {
                    for (int i = 0; i < NumClonesPerWave; i++)
                    {
                        StaticBehaviorReturnType triggerSpawnerResult = HandleContext(proceduralAI, ownerController, TriggerCylinderSpawnerAction);
                        Verify.IsTrue(triggerSpawnerResult == StaticBehaviorReturnType.Completed);
                    }

                    StaticBehaviorReturnType clonePowerResult = HandleUsePowerContext(ownerController, proceduralAI, random, currentTime, CloneCylSummonFXPower);
                    if (clonePowerResult == StaticBehaviorReturnType.Running || clonePowerResult == StaticBehaviorReturnType.Completed)
                    {
                        blackboardProps.AdjustProperty(1, PropertyEnum.AICustomStateVal1);

                        if (clonePowerResult == StaticBehaviorReturnType.Running)
                        {
                            blackboardProps[PropertyEnum.AICustomStateVal2] = (int)SummonState.Summoning;
                            return;
                        }
                    }
                }
            }

            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                return;

            DefaultRangedMovement(proceduralAI, ownerController, agent, target, MoveToTarget, OrbitTarget);
        }
    }

    public class ProcProfMrSinisterCloneCylinderPrototype : ProceduralProfileWithAttackPrototype
    {
        public UsePowerContextPrototype CylinderOpenPower { get; protected set; }
        public DespawnContextPrototype DespawnAction { get; protected set; }
        public int PreOpenDelayMS { get; protected set; }
        public int PostOpenDelayMS { get; protected set; }

        //---

        private enum State
        {
            PreOpen,
            PreOpenDelayCompleted,
            CylinderOpen,
            CylinderOpenCompleted,
        }

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, CylinderOpenPower);
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
           
            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;

            if (ownerController.TargetEntity != agent)
                SelectEntity.RegisterSelectedEntity(ownerController, agent, SelectEntityType.SelectTarget);

            State state = (State)(int)blackboardProps[PropertyEnum.AICustomStateVal1];
            switch (state)
            {
                case State.PreOpen:
                    if (blackboardProps[PropertyEnum.AICustomTimeVal1] == 0L)
                        blackboardProps[PropertyEnum.AICustomTimeVal1] = currentTime;

                    TimeSpan elapsedTime = TimeSpan.FromMilliseconds(currentTime - blackboardProps[PropertyEnum.AICustomTimeVal1]);
                    if ((long)elapsedTime.TotalMilliseconds >= PreOpenDelayMS)
                        blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.PreOpenDelayCompleted;

                    break;

                case State.PreOpenDelayCompleted:
                    GRandom random = game.Random;
                    Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
                    PopulatePowerPicker(ownerController, powerPicker);
                    if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, false) == StaticBehaviorReturnType.Completed)
                        blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.CylinderOpen;
                    break;

                case State.CylinderOpen:
                    if (HandleUsePowerContext(ownerController, proceduralAI, game.Random, currentTime, CylinderOpenPower) == StaticBehaviorReturnType.Completed)
                    {
                        blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.CylinderOpenCompleted;
                        blackboardProps[PropertyEnum.AICustomTimeVal1] = currentTime;
                    }
                    break;

                case State.CylinderOpenCompleted:
                    elapsedTime = TimeSpan.FromMilliseconds(currentTime - blackboardProps[PropertyEnum.AICustomTimeVal1]);
                    if ((long)elapsedTime.TotalMilliseconds > PostOpenDelayMS)
                        HandleContext(proceduralAI, ownerController, DespawnAction);
                    break;
            }
        }
    }

    public class ProceduralProfileBlobPrototype : ProceduralProfileBasicRangePrototype
    {
        public ProceduralUsePowerContextPrototype SummonToadPower { get; protected set; }
        public PrototypeId ToadPrototype { get; protected set; }

        //---

        private enum State
        {
            ToadSummoned,
            NoToad,
        }

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, SummonToadPower);
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

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                proceduralAI.PartialOverrideBehavior == null)
            {
                return;
            }

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;

            if (blackboardProps[PropertyEnum.AICustomStateVal1] == (int)State.ToadSummoned)
            {
                bool toadSummoned = false;

                foreach (WorldEntity summoned in new SummonedEntityIterator(agent))
                {
                    if (!Verify.IsNotNull(summoned))
                        continue;

                    if (summoned.PrototypeDataRef == ToadPrototype)
                    {
                        toadSummoned = true;
                        break;
                    }
                }

                if (toadSummoned == false)
                {
                    UsePowerContextPrototype summonToadPowerContext = SummonToadPower.PowerContext;
                    if (!Verify.IsNotNull(summonToadPowerContext)) return;
                    if (!Verify.IsNotNull(summonToadPowerContext.Power)) return;

                    long cooldownTime = currentTime + game.Random.Next(SummonToadPower.MinCooldownMS, SummonToadPower.MaxCooldownMS);
                    blackboardProps[PropertyEnum.AIProceduralPowerSpecificCDTime] = cooldownTime;
                    blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.NoToad;
                }
            }

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                return;

            DefaultRangedMovement(proceduralAI, ownerController, agent, target, MoveToTarget, OrbitTarget);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);

            int stateVal = ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            if ((State)stateVal == State.NoToad)
                ownerController.AddPowersToPicker(powerPicker, SummonToadPower);
        }
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

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);
            
            InitPower(agent, DeathStun);
            InitPower(agent, SummonTurretPowerAnimOnly);
            InitPower(agent, SummonDoombotBlockades);
            InitPower(agent, SummonDoombotInfernos);
            InitPower(agent, SummonDoombotFlyers);
            InitPower(agent, SummonDoombotAnimOnly);
            InitPower(agent, SummonOrbSpawners);
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

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                proceduralAI.PartialOverrideBehavior == null)
            {
                return;
            }

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;

            int turretCount = blackboardProps[PropertyEnum.AICustomStateVal2];
            if (turretCount != 0)
            {
                StaticBehaviorReturnType result = HandleContext(proceduralAI, ownerController, SpawnTurrets);
                if (!Verify.IsTrue(result == StaticBehaviorReturnType.Completed)) return;
                blackboardProps.RemoveProperty(PropertyEnum.AICustomStateVal2);
            }

            int doombotPhase = blackboardProps[PropertyEnum.AIUltActivationPhase];
            if (doombotPhase != 0)
            {
                bool summonDoombot = true;

                if (proceduralAI.GetState(0) == UsePower.Instance)
                {
                    PrototypeId activePowerRef = ownerController.ActivePowerRef;

                    if (!Verify.IsNotNull(SummonDoombotFlyers)) return;
                    if (!Verify.IsNotNull(SummonDoombotBlockades)) return;
                    if (!Verify.IsNotNull(SummonDoombotInfernos)) return;

                    if (SummonDoombotFlyers.Power != null && activePowerRef != SummonDoombotFlyers.Power.DataRef &&
                        SummonDoombotBlockades.Power != null && activePowerRef != SummonDoombotBlockades.Power.DataRef &&
                        SummonDoombotInfernos.Power != null && activePowerRef != SummonDoombotInfernos.Power.DataRef)
                    {
                        summonDoombot = false;
                    }
                }

                if (summonDoombot)
                {
                    int numSummoned = blackboardProps[PropertyEnum.AICustomStateVal3];

                    if (doombotPhase > 0 && numSummoned == 0 && proceduralAI.GetState(0) != UsePower.Instance)
                    {
                        long nextSummonWaveTime = blackboardProps[PropertyEnum.AICustomTimeVal1];
                        if (currentTime < nextSummonWaveTime)
                            summonDoombot = false;
                    }

                    if (summonDoombot)
                    {
                        doombotPhase = Math.Max(0, doombotPhase);
                        StaticBehaviorReturnType powerResult = StaticBehaviorReturnType.None;
                        Curve numSummonsScaledByHostileEntitiesCurve = null;

                        switch (doombotPhase)
                        {
                            case 0:
                                powerResult = HandleContext(proceduralAI, ownerController, SummonDoombotFlyers, null);
                                numSummonsScaledByHostileEntitiesCurve = GameDatabase.GetCurve(SummonDoombotFlyersCurve);
                                break;
                            case 1:
                                powerResult = HandleContext(proceduralAI, ownerController, SummonDoombotBlockades, null);
                                numSummonsScaledByHostileEntitiesCurve = GameDatabase.GetCurve(SummonDoombotBlockadesCurve);
                                break;
                            case 2:
                                powerResult = HandleContext(proceduralAI, ownerController, SummonDoombotInfernos, null);
                                numSummonsScaledByHostileEntitiesCurve = GameDatabase.GetCurve(SummonDoombotInfernosCurve);
                                break;
                        }

                        if (powerResult == StaticBehaviorReturnType.Running)
                        {
                            return;
                        }
                        else if (powerResult == StaticBehaviorReturnType.Completed)
                        {
                            if (!Verify.IsNotNull(numSummonsScaledByHostileEntitiesCurve)) return;

                            if (++numSummoned >= numSummonsScaledByHostileEntitiesCurve.GetAt(blackboardProps[PropertyEnum.AINumHostilePlayersNearby]))
                            {
                                if (++doombotPhase >= 3)
                                {
                                    blackboardProps.RemoveProperty(PropertyEnum.AIUltActivationPhase);
                                    blackboardProps.RemoveProperty(PropertyEnum.AICustomStateVal3);
                                }
                                else
                                {
                                    blackboardProps[PropertyEnum.AIUltActivationPhase] = doombotPhase;
                                    blackboardProps.RemoveProperty(PropertyEnum.AICustomStateVal3);
                                    blackboardProps[PropertyEnum.AICustomTimeVal1] = currentTime + SummonDoombotWaveIntervalMS;
                                    return;
                                }
                            }
                            else
                            {
                                blackboardProps.AdjustProperty(1, PropertyEnum.AICustomStateVal3);
                                return;
                            }
                        }
                    }
                }
            }

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                return;

            DefaultRangedMovement(proceduralAI, ownerController, agent, target, MoveToTarget, OrbitTarget);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);

            ownerController.AddPowersToPicker(powerPicker, SummonTurretPowerAnimOnly);
            ownerController.AddPowersToPicker(powerPicker, SummonDoombotAnimOnly);
            ownerController.AddPowersToPicker(powerPicker, SummonOrbSpawners);
        }

        public override void OnPowerEnded(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            base.OnPowerEnded(ownerController, powerContext);

            if (powerContext == SummonTurretPowerAnimOnly)
                ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal2] = -1;
            else if (powerContext == SummonDoombotAnimOnly)
                ownerController.Blackboard.PropertyCollection[PropertyEnum.AIUltActivationPhase] = -1;
        }

        public override void OnOwnerKilled(AIController ownerController)
        {
            Agent agent = ownerController.Owner;
            if (Verify.IsNotNull(agent))
                Verify.IsTrue(ownerController.AttemptActivatePower(DeathStun, agent.Id, agent.RegionLocation.Position));

            ProceduralAI proceduralAI = ownerController.Brain;
            if (Verify.IsNotNull(proceduralAI))
            {
                HandleContext(proceduralAI, ownerController, DestroyTurretsOnDeath);
                HandleContext(proceduralAI, ownerController, SpawnDrDoomPhase2);
            }
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

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, DeathStun);
            InitPower(agent, StarryExpanseAnimOnly);
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

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                proceduralAI.PartialOverrideBehavior == null)
            {
                return;
            }

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            if (blackboardProps[PropertyEnum.AICustomStateVal1] == 0)
            {
                if (!Verify.IsTrue(ownerController.AttemptActivatePower(StarryExpanseAnimOnly, agent.Id, agent.RegionLocation.Position)))
                    return;

                StaticBehaviorReturnType result = HandleContext(proceduralAI, ownerController, SpawnStarryExpanseAreas);
                if (!Verify.IsTrue(result == StaticBehaviorReturnType.Completed)) return;

                blackboardProps[PropertyEnum.AICustomStateVal1] = 1;
            }

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                return;

            DefaultRangedMovement(proceduralAI, ownerController, agent, target, MoveToTarget, OrbitTarget);
        }

        public override void OnOwnerKilled(AIController ownerController)
        {
            Agent agent = ownerController.Owner;
            if (Verify.IsNotNull(agent))
                Verify.IsTrue(ownerController.AttemptActivatePower(DeathStun, agent.Id, agent.RegionLocation.Position));

            ProceduralAI proceduralAI = ownerController.Brain;
            if (Verify.IsNotNull(proceduralAI))
                HandleContext(proceduralAI, ownerController, SpawnDrDoomPhase3);
        }
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

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, RapidFirePower);
            InitPower(agent, StarryExpanseAnimOnly);
            InitPower(agent, CosmicSummonsAnimOnly);
            InitPower(agent, CosmicSummonsPower);
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

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                proceduralAI.PartialOverrideBehavior == null)
            {
                return;
            }

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            if (blackboardProps[PropertyEnum.AICustomStateVal1] == 0)
            {
                if (!Verify.IsTrue(ownerController.AttemptActivatePower(StarryExpanseAnimOnly, agent.Id, agent.RegionLocation.Position)))
                    return;

                StaticBehaviorReturnType result = HandleContext(proceduralAI, ownerController, SpawnStarryExpanseAreas);
                if (!Verify.IsTrue(result == StaticBehaviorReturnType.Completed)) return;

                blackboardProps[PropertyEnum.AICustomStateVal1] = 1;
            }
            else if (blackboardProps[PropertyEnum.AICustomStateVal2] == 1)
            {
                if (proceduralAI.GetState(0) != UsePower.Instance)
                {
                    Picker<PrototypeId> cosmicSummonPicker = new(game.Random);

                    if (!Verify.IsTrue(CosmicSummonEntities.HasValue())) return;

                    foreach (PrototypeId cosmicSummon in CosmicSummonEntities)
                        cosmicSummonPicker.Add(cosmicSummon);

                    PrototypeId summonRef = cosmicSummonPicker.Pick();
                    if (!Verify.IsTrue(summonRef != PrototypeId.Invalid)) return;

                    agent.Properties[PropertyEnum.SummonEntityOverrideRef] = summonRef;
                }

                StaticBehaviorReturnType powerResult = HandleContext(proceduralAI, ownerController, CosmicSummonsPower);
                if (powerResult == StaticBehaviorReturnType.Running)
                {
                    return;
                }
                else if (powerResult == StaticBehaviorReturnType.Completed)
                {
                    Curve maxSummonsCurve = GameDatabase.GetCurve(CosmicSummonsNumEntities);
                    if (!Verify.IsNotNull(maxSummonsCurve)) return;

                    int maxSummons = (int)maxSummonsCurve.GetAt(blackboardProps[PropertyEnum.AINumHostilePlayersNearby]);
                    if (!Verify.IsTrue(maxSummons != 0)) return;

                    int numSummoned = blackboardProps[PropertyEnum.AICustomStateVal3];
                    if (++numSummoned >= maxSummons)
                    {
                        agent.Properties.RemoveProperty(PropertyEnum.SummonEntityOverrideRef);
                        blackboardProps.RemoveProperty(PropertyEnum.AICustomStateVal2);
                        blackboardProps.RemoveProperty(PropertyEnum.AICustomStateVal3);
                    }
                    else
                    {
                        blackboardProps[PropertyEnum.AICustomStateVal3] = numSummoned;
                        return;
                    }
                }
            }

            if (proceduralAI.GetState(1) == Rotate.Instance)
            {
                proceduralAI.PushSubstate();
                HandleContext(proceduralAI, ownerController, RapidFireRotate);
                proceduralAI.PopSubstate();
            }

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                return;

            DefaultRangedMovement(proceduralAI, ownerController, agent, target, MoveToTarget, OrbitTarget);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);

            ownerController.AddPowersToPicker(powerPicker, RapidFirePower);
            ownerController.AddPowersToPicker(powerPicker, CosmicSummonsAnimOnly);
        }

        public override void OnPowerStarted(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            if (powerContext == RapidFirePower)
            {
                ProceduralAI proceduralAI = ownerController.Brain;
                if (!Verify.IsNotNull(proceduralAI)) return;

                proceduralAI.PushSubstate();
                HandleContext(proceduralAI, ownerController, RapidFireRotate);
                proceduralAI.PopSubstate();
            }
        }

        public override void OnPowerEnded(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            base.OnPowerEnded(ownerController, powerContext);

            if (powerContext == CosmicSummonsAnimOnly)
                ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal2] = 1;
        }
    }

    public class ProceduralProfileDrDoomPhase1OrbSpawnerPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveTo { get; protected set; }
        public ProceduralFlankContextPrototype Flank { get; protected set; }

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

            WorldEntity target = ownerController.TargetEntity;
            if (CommonSimplifiedSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Ally) == false)
                return;

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                return;

            DefaultRangedFlankerMovement(proceduralAI, ownerController, agent, target, currentTime, MoveTo, Flank);
        }
    }

    public class ProceduralProfileMODOKPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralUsePowerContextPrototype TeleportToEntityPower { get; protected set; }
        public SelectEntityContextPrototype SelectTeleportTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype[] SummonProceduralPowers { get; protected set; }

        //---

        private enum State
        {
            TeleportToEntity,
            SummonProcedural,
            GenericProcedural,
        }

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, TeleportToEntityPower);
            InitPowers(agent, SummonProceduralPowers);
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

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            WorldEntity target = ownerController.TargetEntity;
            BehaviorSensorySystem senses = ownerController.Senses;
            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker;

            State state = (State)(int)blackboardProps[PropertyEnum.AICustomStateVal1];
            switch (state)
            {
                case State.TeleportToEntity:
                    if (senses.ShouldSense())
                    {
                        senses.Sense();

                        if (agent.IsDormant)
                            return;

                        if (proceduralAI.GetState(0) != UsePower.Instance)
                        {
                            SelectEntity.SelectEntityContext selectionContext = new(ownerController, SelectTeleportTarget);
                            WorldEntity selectedEntity = SelectEntity.DoSelectEntity(selectionContext, CombatTargetFlags.IgnoreLOS);
                            if (selectedEntity == null)
                            {
                                ownerController.ResetCurrentTargetState();
                                return;
                            }

                            if (selectedEntity != target && selectedEntity.Id != blackboardProps[PropertyEnum.AIAssistedEntityID])
                            {
                                SelectEntity.RegisterSelectedEntity(ownerController, selectedEntity, selectionContext.SelectionType);
                                target = selectedEntity;
                                blackboardProps[PropertyEnum.AIAssistedEntityID] = selectedEntity.Id;
                            }
                        }
                    }

                    if (target != null && Combat.ValidTarget(game, agent, target, CombatTargetType.Ally, true))
                    {                        
                        powerPicker = new(random);
                        PopulatePowerPicker(ownerController, powerPicker);
                        if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                            return;
                    }

                    if (proceduralAI.LastPowerResult == StaticBehaviorReturnType.Completed)
                    {
                        ownerController.OnAIBehaviorChange();
                        if (target != null)
                            HandleRotateToTarget(agent, target);

                        blackboardProps[PropertyEnum.AICustomTimeVal1] = currentTime + game.Random.Next(TeleportToEntityPower.MinCooldownMS, TeleportToEntityPower.MaxCooldownMS);
                        blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.SummonProcedural;
                    }

                    break;

                case State.SummonProcedural:
                    if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile, CombatTargetFlags.IgnoreLOS) == false)
                        return;

                    powerPicker = new(random);
                    PopulatePowerPicker(ownerController, powerPicker);
                    if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                        return;

                    if (proceduralAI.LastPowerResult == StaticBehaviorReturnType.Completed || proceduralAI.LastPowerResult == StaticBehaviorReturnType.Failed)
                        blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.GenericProcedural;

                    break;

                case State.GenericProcedural:
                    if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false)
                        return;

                    powerPicker = new(random);
                    PopulatePowerPicker(ownerController, powerPicker);
                    if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                        return;

                    if (currentTime > blackboardProps[PropertyEnum.AICustomTimeVal1])
                        blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.TeleportToEntity;

                    break;
            }
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            int stateVal = ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            switch ((State)stateVal)
            {
                case State.TeleportToEntity:
                    ownerController.AddPowersToPicker(powerPicker, TeleportToEntityPower);
                    break;

                case State.SummonProcedural:
                    if (!Verify.IsTrue(SummonProceduralPowers.HasValue())) return;
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

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, MovementPower);
            InitPower(agent, LowHealthPower);
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

            Locomotor locomotor = agent.Locomotor;
            if (!Verify.IsNotNull(locomotor)) return;

            if (HandleOverrideBehavior(ownerController))
            {
                if (locomotor.Method != LocomotorMethod.Airborne)
                    locomotor.SetMethod(LocomotorMethod.Airborne);

                return;
            }

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                proceduralAI.PartialOverrideBehavior == null)
            {
                return;
            }

            if (locomotor.Method != LocomotorMethod.Ground)
                locomotor.SetMethod(LocomotorMethod.Ground);

            Power activePower = agent.ActivePower;
            PowerPrototype lowHealthPowerProto = LowHealthPower.PowerContext.Power;
            PrototypeId lowHealthPowerProtoRef = lowHealthPowerProto != null ? lowHealthPowerProto.DataRef : PrototypeId.Invalid;

            if (activePower != null && activePower.PrototypeDataRef == lowHealthPowerProtoRef)
            {
                if (target == null || target.IsInWorld == false || target.IsDead || agent.IsAliveInWorld == false ||
                    activePower.IsInRange(target, RangeCheckType.Application) == false ||
                    agent.LineOfSightTo(target) == false)
                {
                    proceduralAI.SwitchProceduralState(null, null, StaticBehaviorReturnType.Interrupted);
                }
                else
                {
                    HandleRotateToTarget(agent, target);
                }
            }

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                return;

            if (target != null)
            {
                PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
                float radius = blackboardProps[PropertyEnum.AILOSMaxPowerRadius];
                if (IsPastMaxDistanceOrLostLOS(agent, target, MoveToTarget.RangeMax, MoveToTarget.EnforceLOS, radius, MoveToTarget.LOSSweepPadding))
                {
                    if (locomotor.Method != LocomotorMethod.Airborne)
                        locomotor.SetMethod(LocomotorMethod.Airborne);

                    HandleMovementContext(proceduralAI, ownerController, agent.Locomotor, MoveToTarget, true, out StaticBehaviorReturnType moveToResult);
                    if (moveToResult == StaticBehaviorReturnType.Running) return;
                }

                HandleRotateToTarget(agent, target);
            }
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            PrototypeId startedPowerRef = ownerController.ActivePowerRef;
            if (startedPowerRef != PrototypeId.Invalid)
            {
                if (AddPowerToPickerIfStartedPowerIsContextPower(ownerController, LowHealthPower, startedPowerRef, powerPicker) ||
                    AddPowerToPickerIfStartedPowerIsContextPower(ownerController, MovementPower, startedPowerRef, powerPicker))
                {
                    return;
                }
            }

            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

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

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPowers(agent, SequencePowers);
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

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                proceduralAI.PartialOverrideBehavior == null)
            {
                return;
            }

            if (proceduralAI.GetState(0) == Flank.Instance)
            {
                HandleProceduralFlank(proceduralAI, ownerController, agent.Locomotor, currentTime, FlankTarget, true);
                return;
            }

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                return;

            if (SequencePowers.HasValue())
            {
                if (proceduralAI.LastPowerResult == StaticBehaviorReturnType.Completed)
                {
                    PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
                    PrototypeId lastPowerRef = blackboardProps[PropertyEnum.AILastPowerActivated];
                    if (lastPowerRef != PrototypeId.Invalid)
                    {
                        int numPowers = SequencePowers.Length;
                        foreach (ProceduralUsePowerContextPrototype context in SequencePowers)
                        {
                            if (!Verify.IsNotNull(context))
                                continue;

                            if (!Verify.IsNotNull(context.PowerContext))
                                continue;

                            if (!Verify.IsNotNull(context.PowerContext.Power))
                                continue;

                            if (lastPowerRef == context.PowerContext.Power.DataRef)
                            {
                                int powerIndex = blackboardProps[PropertyEnum.AICustomStateVal1];
                                powerIndex = ++powerIndex % numPowers;
                                blackboardProps[PropertyEnum.AICustomStateVal1] = powerIndex;
                                break;
                            }
                        }
                    }
                }
            }

            DefaultRangedFlankerMovement(proceduralAI, ownerController, agent, target, currentTime, MoveToTarget, FlankTarget);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);

            if (SequencePowers.HasValue())
            {
                int powerIndex = ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
                if (powerIndex < 0 || powerIndex >= SequencePowers.Length)
                    return;

                ProceduralUsePowerContextPrototype sequencePowerContext = SequencePowers[powerIndex];
                if (Verify.IsNotNull(sequencePowerContext))
                    ownerController.AddPowersToPicker(powerPicker, sequencePowerContext);
            }
        }

        public override bool OnPowerPicked(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            if (base.OnPowerPicked(ownerController, powerContext) == false)
                return false;

            WorldEntity selectedEntity = null;
            SelectEntityType selectionType = SelectEntityType.None;

            if (SequencePowers.HasValue())
            {
                foreach (ProceduralUsePowerContextPrototype context in SequencePowers)
                {
                    if (context == powerContext)
                    {
                        SelectEntity.SelectEntityContext selectionContext = new(ownerController, SequencePowerSelectTarget);
                        selectionType = selectionContext.SelectionType;
                        selectedEntity = SelectEntity.DoSelectEntity(selectionContext);
                        break;
                    }
                }
            }

            if (selectedEntity == null || selectedEntity.IsDead)
            {
                SelectEntity.SelectEntityContext selectionContext = new(ownerController, SelectTarget);
                selectionType = selectionContext.SelectionType;
                selectedEntity = SelectEntity.DoSelectEntity(selectionContext);
            }

            if (selectedEntity == null || selectedEntity.IsDead)
                return false;

            if (!Verify.IsTrue(SelectEntity.RegisterSelectedEntity(ownerController, selectedEntity, selectionType)))
                return false;

            return true;
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

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, MovementPower);
            InitPower(agent, LowHealthPower);
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

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                proceduralAI.PartialOverrideBehavior == null)
            {
                return;
            }

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                return;

            DefaultMeleeMovement(proceduralAI, ownerController, agent.Locomotor, target, MoveToTarget, OrbitTarget);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            PrototypeId startedPowerRef = ownerController.ActivePowerRef;
            if (startedPowerRef != PrototypeId.Invalid)
            {
                if (AddPowerToPickerIfStartedPowerIsContextPower(ownerController, LowHealthPower, startedPowerRef, powerPicker) ||
                    AddPowerToPickerIfStartedPowerIsContextPower(ownerController, MovementPower, startedPowerRef, powerPicker))
                {
                    return;
                }
            }

            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

            long health = agent.Properties[PropertyEnum.Health];
            long maxHealth = agent.Properties[PropertyEnum.HealthMax];

            if (MathHelper.IsBelowOrEqual(health, maxHealth, LowHealthPowerThresholdPct))
                ownerController.AddPowersToPicker(powerPicker, LowHealthPower);

            CombatTargetFlags flags = CombatTargetFlags.IgnoreAggroDistance | CombatTargetFlags.IgnoreStealth;
            if (Combat.GetNumTargetsInRange(agent, 1600f, 150f, CombatTargetType.Hostile, flags) > 0)
                ownerController.AddPowersToPicker(powerPicker, MovementPower);

            base.PopulatePowerPicker(ownerController, powerPicker);
        }
    }

    public class ProceduralProfileGrimReaperPrototype : ProfMeleePwrSpecialAtHealthPctPrototype
    {
        public ProceduralUsePowerContextPrototype TripleShotPower { get; protected set; }
        public ProceduralUsePowerContextPrototype SpecialPower { get; protected set; }
        public SelectEntityContextPrototype SpecialSelectTarget { get; protected set; }
        public int SpecialPowerChangeTgtIntervalMS { get; protected set; }

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);
            
            InitPower(agent, TripleShotPower);
            InitPower(agent, SpecialPower);
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

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                proceduralAI.PartialOverrideBehavior == null)
            {
                return;
            }

            if (CheckAgentHealthAndUsePower(ownerController, proceduralAI, currentTime, agent))
                return;

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            StaticBehaviorReturnType powerResult = HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true);

            if (powerResult == StaticBehaviorReturnType.Running || powerResult == StaticBehaviorReturnType.Completed)
            {
                PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
                PrototypeId activePowerRef = blackboardProps[PropertyEnum.AILastPowerActivated];

                if (!Verify.IsNotNull(SpecialPower)) return;
                if (!Verify.IsNotNull(SpecialPower.PowerContext)) return;
                if (!Verify.IsNotNull(SpecialPower.PowerContext.Power)) return;

                if (activePowerRef == PrototypeId.Invalid || SpecialPower.PowerContext.Power.DataRef != activePowerRef)
                    return;

                if (powerResult == StaticBehaviorReturnType.Running)
                {
                    int changeTargetCount = blackboardProps[PropertyEnum.AICustomStateVal3];
                    if (changeTargetCount == 0)
                    {
                        blackboardProps[PropertyEnum.AICustomStateVal3] = 1;
                    }
                    else
                    {
                        long powerStartTime = agent.Properties[PropertyEnum.PowerCooldownStartTime, activePowerRef];
                        if (currentTime > powerStartTime + SpecialPowerChangeTgtIntervalMS * changeTargetCount)
                        {
                            SelectEntity.SelectEntityContext selectionContext = new(ownerController, SpecialSelectTarget);
                            WorldEntity selectedEntity = SelectEntity.DoSelectEntity(selectionContext);
                            if (selectedEntity == null)
                                return;

                            if (!Verify.IsTrue(SelectEntity.RegisterSelectedEntity(ownerController, selectedEntity, selectionContext.SelectionType)))
                                return;

                            blackboardProps[PropertyEnum.AICustomStateVal3] = changeTargetCount + 1;
                        }
                    }

                    target = ownerController.TargetEntity;
                    if (!Verify.IsNotNull(target)) return;

                    Locomotor locomotor = agent.Locomotor;
                    ulong targetId = target.Id;
                    locomotor.FollowEntity(targetId, agent.Bounds.Radius);
                }
                else
                {
                    Locomotor locomotor = agent.Locomotor;
                    locomotor.Stop();

                    blackboardProps[PropertyEnum.AICustomStateVal3] = 0;
                }

                return;
            }

            DefaultMeleeMovement(proceduralAI, ownerController, agent.Locomotor, target, MoveToTarget, OrbitTarget);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);

            ownerController.AddPowersToPicker(powerPicker, TripleShotPower);
            ownerController.AddPowersToPicker(powerPicker, SpecialPower);
        }

        public override bool OnPowerPicked(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            if (base.OnPowerPicked(ownerController, powerContext) == false)
                return false;

            if (powerContext == TripleShotPower)
            {
                BehaviorSensorySystem senses = ownerController.Senses;
                PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
                ulong oldTargetId = blackboardProps[PropertyEnum.AIRawTargetEntityID];

                if (senses.PotentialHostileTargetIds.Count > 1 && oldTargetId != Entity.InvalidId)
                {
                    EntityManager entityManager = ownerController.Game.EntityManager;

                    foreach (ulong targetId in senses.PotentialHostileTargetIds)
                    {
                        if (targetId != oldTargetId)
                        {
                            WorldEntity selectedEntity = entityManager.GetEntity<WorldEntity>(targetId);
                            if (selectedEntity == null || !selectedEntity.IsInWorld)
                                return false;

                            blackboardProps[PropertyEnum.AIRawTargetEntityID] = targetId;

                            if (!Verify.IsTrue(SelectEntity.RegisterSelectedEntity(ownerController, selectedEntity, SelectEntityType.SelectTarget)))
                                return false;

                            break;
                        }
                    }
                }
            }

            return true;
        }
    }

    public class ProceduralProfileMoleManPrototype : ProceduralProfileBasicRangePrototype
    {
        public TriggerSpawnersContextPrototype[] GigantoSpawners { get; protected set; }
        public ProceduralUsePowerContextPrototype MoloidInvasionPower { get; protected set; }
        public TriggerSpawnersContextPrototype MoloidInvasionSpawner { get; protected set; }
        public ProceduralUsePowerContextPrototype SummonGigantoAnimPower { get; protected set; }

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, MoloidInvasionPower);
            InitPower(agent, SummonGigantoAnimPower);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            ownerController.AddPowersToPicker(powerPicker, MoloidInvasionPower);
            ownerController.AddPowersToPicker(powerPicker, SummonGigantoAnimPower);

            base.PopulatePowerPicker(ownerController, powerPicker);
        }

        public override bool OnPowerPicked(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            if (base.OnPowerPicked(ownerController, powerContext) == false)
                return false;

            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return false;

            if (powerContext == MoloidInvasionPower)
            {
                StaticBehaviorReturnType triggerSpawnerResult = HandleContext(proceduralAI, ownerController, MoloidInvasionSpawner);
                if (!Verify.IsTrue(triggerSpawnerResult == StaticBehaviorReturnType.Completed)) return false;
            }
            else if (powerContext == SummonGigantoAnimPower)
            {
                Game game = ownerController.Game;
                if (!Verify.IsNotNull(game)) return false;

                if (!Verify.IsTrue(GigantoSpawners.HasValue())) return false;

                int randomIndex = game.Random.Next(0, GigantoSpawners.Length);
                TriggerSpawnersContextPrototype triggerSpawnerProto = GigantoSpawners[randomIndex];

                StaticBehaviorReturnType triggerSpawnerResult = HandleContext(proceduralAI, ownerController, triggerSpawnerProto);
                if (!Verify.IsTrue(triggerSpawnerResult == StaticBehaviorReturnType.Completed)) return false;
            }

            return true;
        }
    }

    public class ProceduralProfileVenomPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralFlankContextPrototype FlankTarget { get; protected set; }
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public UsePowerContextPrototype VenomMad { get; protected set; }
        public float VenomMadThreshold1 { get; protected set; }
        public float VenomMadThreshold2 { get; protected set; }

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, VenomMad);
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

            GRandom random = game.Random;

            if (proceduralAI.GetState(0) != UsePower.Instance)
            {
                PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
                long health = agent.Properties[PropertyEnum.Health];
                long maxHealth = agent.Properties[PropertyEnum.HealthMax];

                if (MathHelper.IsBelowOrEqual(health, maxHealth, VenomMadThreshold1))
                {
                    if (blackboardProps[PropertyEnum.AICustomStateVal1] < 1)
                    {
                        StaticBehaviorReturnType powerResult = HandleUsePowerContext(ownerController, proceduralAI, random, currentTime, VenomMad);
                        if (powerResult == StaticBehaviorReturnType.Running || powerResult == StaticBehaviorReturnType.Completed)
                        {
                            blackboardProps[PropertyEnum.AICustomStateVal1] = 1;
                            if (powerResult == StaticBehaviorReturnType.Running)
                                return;
                        }
                    }
                    else if (MathHelper.IsBelowOrEqual(health, maxHealth, VenomMadThreshold2) && blackboardProps[PropertyEnum.AICustomStateVal2] < 1)
                    {
                        StaticBehaviorReturnType powerResult = HandleUsePowerContext(ownerController, proceduralAI, random, currentTime, VenomMad);
                        if (powerResult == StaticBehaviorReturnType.Running || powerResult == StaticBehaviorReturnType.Completed)
                        {
                            blackboardProps[PropertyEnum.AICustomStateVal2] = 1;
                            if (powerResult == StaticBehaviorReturnType.Running)
                                return;
                        }
                    }
                }
            }
            else
            {
                PrototypeId venomMadPowerRef = VenomMad.Power != null ? VenomMad.Power.DataRef : PrototypeId.Invalid;
                if (ownerController.ActivePowerRef == venomMadPowerRef)
                {
                    StaticBehaviorReturnType powerResult = HandleUsePowerContext(ownerController, proceduralAI, random, currentTime, VenomMad);
                    if (powerResult == StaticBehaviorReturnType.Running)
                        return;
                }
            }

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                proceduralAI.PartialOverrideBehavior == null)
            {
                return;
            }

            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                return;

            DefaultRangedFlankerMovement(proceduralAI, ownerController, agent, target, currentTime, MoveToTarget, FlankTarget);
        }
    }

    public class ProceduralProfileDoopPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralFleeContextPrototype Flee { get; protected set; }
        public ProceduralUsePowerContextPrototype DisappearPower { get; protected set; }
        public int LifeTimeMinMS { get; protected set; }
        public int LifeTimeMaxMS { get; protected set; }
#if GAME_VERSION_1_48
        public WanderContextPrototype WanderWhenFleeFails { get; protected set; }
        public float DisappearTimerAvatarSearchRadius { get; protected set; }
#endif

        //---

        private enum State
        {
            Default,
            Flee,
            Disappear,
        }

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, DisappearPower);
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

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            StaticBehaviorReturnType contextResult;
            GRandom random = game.Random;

            int stateVal = blackboardProps[PropertyEnum.AICustomStateVal1];

            if (blackboardProps[PropertyEnum.AICustomTimeVal1] == 0 && stateVal == (int)State.Flee)
                blackboardProps[PropertyEnum.AICustomTimeVal1] = currentTime + random.Next(LifeTimeMinMS, LifeTimeMaxMS);

            switch ((State)stateVal)
            {
                case State.Flee:
                    WorldEntity target = ownerController.TargetEntity;
                    if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                        proceduralAI.PartialOverrideBehavior == null)
                    {
                        return;
                    }

                    contextResult = HandleProceduralFlee(proceduralAI, ownerController, currentTime, Flee);
                    long disappearTime = blackboardProps[PropertyEnum.AICustomTimeVal1];
                    if ((contextResult == StaticBehaviorReturnType.Completed || contextResult == StaticBehaviorReturnType.None) &&
                        disappearTime > 0 && currentTime > disappearTime)
                    {
                        blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.Disappear;
                    }

                    break;

                case State.Disappear:
                    Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
                    PopulatePowerPicker(ownerController, powerPicker);
                    if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                        return;

                    if (proceduralAI.LastPowerResult == StaticBehaviorReturnType.Completed)
                        agent.Destroy();

                    break;
            }
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);

            ownerController.AddPowersToPicker(powerPicker, DisappearPower);
        }

        public override void OnOwnerGotDamaged(AIController ownerController)
        {
            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            if (blackboardProps[PropertyEnum.AICustomStateVal1] == 0)
                blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.Flee;
        }
    }

    public class ProceduralProfileGorgonPrototype : ProceduralProfileWithAttackPrototype
    {
        public RotateContextPrototype RotateInStoneGaze { get; protected set; }
        public ProceduralUsePowerContextPrototype StoneGaze { get; protected set; }
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, StoneGaze);
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

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                proceduralAI.PartialOverrideBehavior == null)
            {
                return;
            }

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            StaticBehaviorReturnType proceduralPowerResult = HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true);

            if (proceduralPowerResult == StaticBehaviorReturnType.Running)
            {
                PrototypeId activePowerRef = ownerController.ActivePowerRef;
                if (activePowerRef != PrototypeId.Invalid)
                {
                    if (!Verify.IsNotNull(StoneGaze)) return;
                    if (!Verify.IsNotNull(StoneGaze.PowerContext)) return;
                    if (!Verify.IsNotNull(StoneGaze.PowerContext.Power)) return;

                    if (StoneGaze.PowerContext.Power.DataRef == activePowerRef)
                    {
                        proceduralAI.PushSubstate();
                        HandleContext(proceduralAI, ownerController, RotateInStoneGaze);
                        proceduralAI.PopSubstate();                       
                    }
                }

                return; 
            }

            DefaultMeleeMovement(proceduralAI, ownerController, agent.Locomotor, target, MoveToTarget, OrbitTarget);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);

            ownerController.AddPowersToPicker(powerPicker, StoneGaze);
        }
    }

    public class ProceduralProfileBullseyePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public ProceduralFlankContextPrototype FlankTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype MarkForDeath { get; protected set; }

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);
            
            InitPower(agent, MarkForDeath);
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

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                proceduralAI.PartialOverrideBehavior == null)
            {
                return;
            }

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            StaticBehaviorReturnType proceduralPowerResult = HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true);

            if (proceduralPowerResult == StaticBehaviorReturnType.Running) 
            {
                PrototypeId activePowerRef = ownerController.ActivePowerRef;
                if (activePowerRef == PrototypeId.Invalid)
                    return;

                if (MarkForDeath == null)
                    return;

                if (!Verify.IsNotNull(MarkForDeath.PowerContext)) return;
                if (!Verify.IsNotNull(MarkForDeath.PowerContext.Power)) return;

                if (MarkForDeath.PowerContext.Power.DataRef == activePowerRef)
                {
                    if (target == null || target.IsInWorld == false || target.IsDead || agent.IsAliveInWorld == false || agent.LineOfSightTo(target) == false)
                        proceduralAI.SwitchProceduralState(null, null, StaticBehaviorReturnType.Interrupted);
                    else
                        HandleRotateToTarget(agent, target);
                }
                
                return; 
            }

            DefaultRangedFlankerMovement(proceduralAI, ownerController, agent, target, currentTime, MoveToTarget, FlankTarget);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);

            ownerController.AddPowersToPicker(powerPicker, MarkForDeath);
        }
    }

    public class ProceduralProfileRhinoPrototype : ProceduralProfileBasicMeleePrototype
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

            long currentTime = (long)game.CurrentTime.TotalMilliseconds;

            if (HandleOverrideBehavior(ownerController))
                return;

            if (proceduralAI.GetState(0) != UsePower.Instance)
            {
                WorldEntity target = ownerController.TargetEntity;
                if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                    proceduralAI.PartialOverrideBehavior == null)
                {
                    return;
                }
            }

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                return;

            DefaultMeleeMovement(proceduralAI, ownerController, agent.Locomotor, ownerController.TargetEntity, MoveToTarget, OrbitTarget);
        }
    }

    public class ProceduralProfileBlackCatPrototype : ProceduralProfileBasicMeleePrototype
    {
        public ProceduralUsePowerContextPrototype TumblePower { get; protected set; }
        public ProceduralUsePowerContextPrototype TumbleComboPower { get; protected set; }
        public SelectEntityContextPrototype SelectEntityForTumbleCombo { get; protected set; }

        //---

        private enum State
        {
            Default,
            ComboPower,
        }

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, TumblePower);
            InitPower(agent, TumbleComboPower);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            int stateVal = blackboardProps[PropertyEnum.AICustomStateVal1];
            PrototypeId startedPowerRef = ownerController.ActivePowerRef;

            if (startedPowerRef != PrototypeId.Invalid)
            {
                if (AddPowerToPickerIfStartedPowerIsContextPower(ownerController, TumbleComboPower, startedPowerRef, powerPicker))
                    return;
            }
            else if ((State)stateVal == State.ComboPower)
            {
                SelectEntity.SelectEntityContext selectionContext = new(ownerController, SelectEntityForTumbleCombo);
                WorldEntity selectedEntity = SelectEntity.DoSelectEntity(selectionContext);
                if (selectedEntity != null)
                {
                    if (!Verify.IsTrue(SelectEntity.RegisterSelectedEntity(ownerController, selectedEntity, selectionContext.SelectionType))) return;

                    ownerController.AddPowersToPicker(powerPicker, TumbleComboPower);
                    return;
                }
                else
                {
                    blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.Default;
                }
            }

            ownerController.AddPowersToPicker(powerPicker, TumblePower);

            base.PopulatePowerPicker(ownerController, powerPicker);
        }

        public override void OnPowerEnded(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            base.OnPowerEnded(ownerController, powerContext);
            
            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;

            if (powerContext == TumblePower)
                blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.ComboPower;
            else if (powerContext == TumbleComboPower)
                blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.Default;            
        }
    }

    public class ProceduralProfileLivingLaserPrototype : ProceduralProfileBasicRangePrototype
    {
        public ProceduralUsePowerContextPrototype SweepingBeamPowerClock { get; protected set; }
        public ProceduralUsePowerContextPrototype SweepingBeamPowerCounterClock { get; protected set; }
        public RotateContextPrototype SweepingBeamClock { get; protected set; }
        public RotateContextPrototype SweepingBeamCounterClock { get; protected set; }

        //---

        private enum State
        {
            Default,
            Clock,
            CounterClock,
        }

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, SweepingBeamPowerClock);
            InitPower(agent, SweepingBeamPowerCounterClock);
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

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                proceduralAI.PartialOverrideBehavior == null)
            {
                return;
            }

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            StaticBehaviorReturnType proceduralPowerResult = HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true);

            if (proceduralPowerResult == StaticBehaviorReturnType.Running) 
            {
                PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
                int state = blackboardProps[PropertyEnum.AICustomStateVal1];
                if (state == (int)State.Clock)
                {
                    proceduralAI.PushSubstate();
                    HandleContext(proceduralAI, ownerController, SweepingBeamClock);
                    proceduralAI.PopSubstate();
                }
                else if (state == (int)State.CounterClock)
                {
                    proceduralAI.PushSubstate();
                    HandleContext(proceduralAI, ownerController, SweepingBeamCounterClock);
                    proceduralAI.PopSubstate();
                }

                return;
            }

            DefaultRangedMovement(proceduralAI, ownerController, agent, target, MoveToTarget, OrbitTarget);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);

            ownerController.AddPowersToPicker(powerPicker, SweepingBeamPowerClock);
            ownerController.AddPowersToPicker(powerPicker, SweepingBeamPowerCounterClock);
        }

        public override bool OnPowerPicked(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            if (base.OnPowerPicked(ownerController, powerContext) == false)
                return false;

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            
            if (powerContext == SweepingBeamPowerClock)
                blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.Clock;
            else if (powerContext == SweepingBeamPowerCounterClock)
                blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.CounterClock;

            return true;
        }

        public override void OnPowerEnded(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            base.OnPowerEnded(ownerController, powerContext);

            if (powerContext == SweepingBeamPowerClock || powerContext == SweepingBeamPowerCounterClock)
                ownerController.Blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AICustomStateVal1);
        }
    }

    public class ProceduralProfileLizardBossPrototype : ProceduralProfileBasicMeleePrototype
    {
        public ProceduralUsePowerContextPrototype LizardSwarmPower { get; protected set; }

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, LizardSwarmPower);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);

            ownerController.AddPowersToPicker(powerPicker, LizardSwarmPower);
        }

        public override void OnPowerEnded(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            base.OnPowerEnded(ownerController, powerContext);

            if (powerContext == LizardSwarmPower)
            {
                Agent agent = ownerController.Owner;
                if (!Verify.IsNotNull(agent)) return;
                Game game = agent.Game;
                if (!Verify.IsNotNull(game)) return;
                
                foreach (WorldEntity summoned in new SummonedEntityIterator(agent))
                {
                    if (summoned is Agent summonedAgent)
                    {
                        WorldEntity target = ownerController.TargetEntity;
                        if (!Verify.IsNotNull(target)) return;

                        summonedAgent.Properties[PropertyEnum.TauntersID] = target.Id;
                    }
                }
            }
        }
    }

    public class ProceduralProfileLokiPhase1Prototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public ProceduralFlankContextPrototype FlankTarget { get; protected set; }

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

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                proceduralAI.PartialOverrideBehavior == null)
            {
                return;
            }

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                return;

            DefaultRangedFlankerMovement(proceduralAI, ownerController, agent, target, currentTime, MoveToTarget, FlankTarget);
        }

        public override void OnOwnerKilled(AIController ownerController)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;
        }
    }

    public class ProceduralProfileLokiPhase2Prototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype InverseRings { get; protected set; }
        public PrototypeId InverseRingsTargetedVFXOnly { get; protected set; }
        public PrototypeId LokiBossSafeZoneKeyword { get; protected set; }
        public PrototypeId InverseRingsVFXRemoval { get; protected set; }

        //---

        private enum State
        {
            Default,
            InverseRings,
        }

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, InverseRings);
            InitPower(agent, InverseRingsTargetedVFXOnly);
            InitPower(agent, InverseRingsVFXRemoval);
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

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                proceduralAI.PartialOverrideBehavior == null)
            {
                return;
            }

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            int state = blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            if (state == (int)State.InverseRings)
            {
                Region region = agent.Region;
                if (!Verify.IsNotNull(region)) return;

                Sphere volume = new(agent.RegionLocation.Position, ownerController.AggroRangeHostile);
                foreach (Avatar avatar in region.IterateAvatarsInVolume(volume))
                {
                    if (!Verify.IsNotNull(avatar))
                        continue;

                    if (avatar.HasConditionWithKeyword(LokiBossSafeZoneKeyword) == false)
                        ownerController.AttemptActivatePower(InverseRingsTargetedVFXOnly, avatar.Id, avatar.RegionLocation.Position);
                }
            }

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                return;

            DefaultMeleeMovement(proceduralAI, ownerController, agent.Locomotor, target, MoveToTarget, OrbitTarget);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            ownerController.AddPowersToPicker(powerPicker, InverseRings);

            base.PopulatePowerPicker(ownerController, powerPicker);
        }

        public override void OnOwnerKilled(AIController ownerController)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;
        }

        public override void OnPowerStarted(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            if (powerContext == InverseRings)
            {
                PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
                blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.InverseRings;
            }
        }

        public override void OnPowerEnded(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            Game game = ownerController.Game;
            if (!Verify.IsNotNull(game)) return;
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

            if (powerContext == InverseRings)
            {
                PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
                blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.Default;

                Region region = agent.Region;
                if (!Verify.IsNotNull(region)) return;

                Sphere volume = new(agent.RegionLocation.Position, ownerController.AggroRangeHostile);
                foreach (Avatar avatar in region.IterateAvatarsInVolume(volume))
                {
                    if (!Verify.IsNotNull(avatar))
                        continue;

                    ownerController.AttemptActivatePower(InverseRingsVFXRemoval, avatar.Id, avatar.RegionLocation.Position);
                }
            }
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

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPowers(agent, ProjectionPowers);
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
            PropertyCollection blackboardProps = blackboard.PropertyCollection;
            WorldEntity master = ownerController.AssistedEntity;

            Queue<CustomPowerQueueEntry> powerQueue = blackboard.CustomPowerQueue;

            if (powerQueue != null && powerQueue.Count > 0)
            {
                CustomPowerQueueEntry customPowerEntry = powerQueue.Peek();
                PrototypeId customPowerDataRef = customPowerEntry.PowerRef;
                if (!Verify.IsTrue(customPowerDataRef != PrototypeId.Invalid)) return;

                ProceduralUsePowerContextPrototype procUsePowerContextProto = GetProjectionPowerUseContext(customPowerDataRef);
                if (!Verify.IsNotNull(procUsePowerContextProto)) return;

                UsePowerContextPrototype usePowerContextProto = procUsePowerContextProto.PowerContext;
                if (!Verify.IsNotNull(usePowerContextProto)) return;

                bool isUsingCustomPower = false;
                if (proceduralAI.GetState(0) == UsePower.Instance)
                {
                    if (ownerController.ActivePowerRef != customPowerDataRef)
                    {
                        proceduralAI.SwitchProceduralState(null, null, StaticBehaviorReturnType.Interrupted);
                        blackboard.UsePowerTargetPos = customPowerEntry.TargetPos;
                    }
                    else
                    {
                        isUsingCustomPower = true;
                    }
                }
                else
                {
                    blackboard.UsePowerTargetPos = customPowerEntry.TargetPos;
                }

                StaticBehaviorReturnType powerResult = HandleUsePowerContext(ownerController, proceduralAI, game.Random, currentTime, usePowerContextProto, procUsePowerContextProto);
                if (powerResult == StaticBehaviorReturnType.Failed && isUsingCustomPower == false)
                {
                    if (Verify.IsTrue(powerQueue.Count > 0, $"Custom power queue already empty when handling failed power use [{customPowerDataRef.GetName()}] for agent [{agent}]"))
                        powerQueue.Dequeue();
                }

                if (powerResult == StaticBehaviorReturnType.Running)
                    return;
            }
            else if (master != null && master.IsInWorld)
            {
                TimeSpan lastTime = TimeSpan.FromSeconds(currentTime - blackboard.PropertyCollection[PropertyEnum.AICustomTimeVal1]);
                if ((long)lastTime.TotalMilliseconds >= FlankToMasterDelayMS)
                {
                    float distanceToMasterSq = Vector3.DistanceSquared2D(agent.RegionLocation.Position, master.RegionLocation.Position);
                    if (distanceToMasterSq > MaxDistToMasterBeforeTeleport * MaxDistToMasterBeforeTeleport)
                    {
                        blackboard.PropertyCollection[PropertyEnum.AILastAttackerID] = 0;
                        HandleContext(proceduralAI, ownerController, TeleportToMasterIfTooFarAway);
                        ownerController.ResetCurrentTargetState();
                    }

                    if (proceduralAI.GetState(0) == Flank.Instance ||
                        Segment.IsNearZero(distanceToMasterSq, DeadzoneAroundFlankTarget * DeadzoneAroundFlankTarget) == false)
                    {
                        HandleProceduralFlank(proceduralAI, ownerController, agent.Locomotor, currentTime, FlankMaster, false);
                    }
                }
            }
        }

        private ProceduralUsePowerContextPrototype GetProjectionPowerUseContext(PrototypeId projectionPowerDataRef)
        {
            if (ProjectionPowers.HasValue())
            {
                foreach (ProceduralUsePowerContextPrototype proceduralPowerContext in ProjectionPowers)
                {
                    if (!Verify.IsNotNull(proceduralPowerContext))
                        continue;

                    if (!Verify.IsNotNull(proceduralPowerContext.PowerContext))
                        continue;

                    if (!Verify.IsNotNull(proceduralPowerContext.PowerContext.Power))
                        continue;

                    if (proceduralPowerContext.PowerContext.Power.DataRef == projectionPowerDataRef)
                        return proceduralPowerContext;
                }
            }

            return null;
        }

        public override void OnPowerEnded(AIController ownerController, ProceduralUsePowerContextPrototype proceduralPowerContext)
        {
            base.OnPowerEnded(ownerController, proceduralPowerContext);

            if (!Verify.IsNotNull(proceduralPowerContext.PowerContext)) return;

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            PropertyCollection blackboardProps = blackboard.PropertyCollection;
            Queue<CustomPowerQueueEntry> powerQueue = blackboard.CustomPowerQueue;
            if (powerQueue != null && powerQueue.Count > 0)
            {
                PrototypeId customPowerDataRef = powerQueue.Peek().PowerRef;

                if (!Verify.IsNotNull(proceduralPowerContext.PowerContext.Power)) return;
                if (!Verify.IsTrue(proceduralPowerContext.PowerContext.Power.DataRef == customPowerDataRef)) return;

                powerQueue.Dequeue();

                if (powerQueue.Count == 0)
                    blackboardProps.RemoveProperty(PropertyEnum.AICustomThinkRateMS);

                Game game = ownerController.Game;
                if (!Verify.IsNotNull(game)) return;

                long currentTime = (long)game.CurrentTime.TotalMilliseconds;
                blackboardProps[PropertyEnum.AICustomTimeVal1] = currentTime;
                ownerController.ResetCurrentTargetState();
            }
        }
    }

    public class ProceduralProfileEyeOfAgamottoPrototype : ProceduralProfileStationaryTurretPrototype
    {
        public RotateContextPrototype IdleRotation { get; protected set; }

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
            CommonSimplifiedSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile);
            if (target == null)
            {
                HandleContext(proceduralAI, ownerController,IdleRotation);
                return;
            }

            base.Think(ownerController);
        }
    }

    public class ProceduralProfileBrimstonePrototype : ProceduralProfileWithEnragePrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public MoveToContextPrototype MoveIntoMeleeRange { get; protected set; }
        public ProceduralUsePowerContextPrototype MeleePower { get; protected set; }
        public PrototypeId HellfireProtoRef { get; protected set; }

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, MeleePower);

            AIController ownerController = agent.AIController;
            if (!Verify.IsNotNull(ownerController)) return;
            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return;

            ownerController.RegisterForEntityAggroedEvents(region, true);
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

            HandleEnrage(ownerController);

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            EnrageState state = (EnrageState)(int)blackboardProps[PropertyEnum.AIEnrageState];
            if (state != EnrageState.Enraging)
            {
                WorldEntity target = ownerController.TargetEntity;
                if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false
                    && proceduralAI.PartialOverrideBehavior == null) return;

                GRandom random = game.Random;
                Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
                PopulatePowerPicker(ownerController, powerPicker);
                if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                    return;

                // MoveIntoMeleeRange and MoveToTarget appear to be the same thing, and AICustomStateVal1 is never set? (at least as of 1.52)
                int stateVal = blackboardProps[PropertyEnum.AICustomStateVal1];
                if (stateVal != 0)
                    DefaultRangedMovement(proceduralAI, ownerController, agent, target, MoveIntoMeleeRange, OrbitTarget);
                else
                    DefaultRangedMovement(proceduralAI, ownerController, agent, target, MoveToTarget, OrbitTarget);
            }
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);

            ownerController.AddPowersToPicker(powerPicker, MeleePower);
        }

        public override void OnEntityAggroedEvent(AIController ownerController, in EntityAggroedGameEvent aggroedEvent)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            Game game = agent.Game;
            if (!Verify.IsNotNull(game)) return;

            Agent aggroAgent = aggroedEvent.AggroEntity as Agent;
            if (!Verify.IsNotNull(aggroAgent)) return;

            if (aggroAgent.PrototypeDataRef == HellfireProtoRef && agent.Properties.HasProperty(PropertyEnum.EnrageStartTime) == false)
                agent.Properties[PropertyEnum.EnrageStartTime] = game.CurrentTime + TimeSpan.FromMinutes(EnrageTimerInMinutes);
        }
    }

    public class ProceduralProfileSlagPrototype : ProceduralProfileWithEnragePrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype PrimaryPower { get; protected set; }

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

            HandleEnrage(ownerController);

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;

            EnrageState state = (EnrageState)(int)blackboardProps[PropertyEnum.AIEnrageState];
            if (state != EnrageState.Enraging)
            {
                WorldEntity target = ownerController.TargetEntity;
                if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                    proceduralAI.PartialOverrideBehavior == null)
                {
                    return;
                }

                GRandom random = game.Random;
                Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
                PopulatePowerPicker(ownerController, powerPicker);
                if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                    return;

                DefaultMeleeMovement(proceduralAI, ownerController, agent.Locomotor, target, MoveToTarget, OrbitTarget);
            }
        }
    }

    public class ProceduralProfileMonolithPrototype : ProceduralProfileWithEnragePrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public KeywordPrototype ObeliskKeyword { get; protected set; }
        public PrototypeId[] ObeliskDamageMonolithPowers { get; protected set; }
        public PrototypeId DisableShield { get; protected set; }

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            AIController ownerController = agent.AIController;
            if (!Verify.IsNotNull(ownerController)) return;
            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return;

            ownerController.RegisterForAIBroadcastBlackboardEvents(region, true);

            InitPower(agent, DisableShield);
            InitPowers(agent, ObeliskDamageMonolithPowers);
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

            HandleEnrage(ownerController);

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            EnrageState state = (EnrageState)(int)blackboardProps[PropertyEnum.AIEnrageState];
            if (state != EnrageState.Enraging)
            {
                WorldEntity target = ownerController.TargetEntity;
                if (CommonSimplifiedSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false)
                    return;

                GRandom random = game.Random;
                Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
                PopulatePowerPicker(ownerController, powerPicker);
                HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true);
            }
        }

        public override void OnAIBroadcastBlackboardEvent(AIController ownerController, in AIBroadcastBlackboardGameEvent broadcastEvent)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            WorldEntity broadcaster = broadcastEvent.Broadcaster;
            if (!Verify.IsNotNull(broadcaster)) return;
            BehaviorBlackboard broadcasterBlackboard = broadcastEvent.Blackboard;
            if (!Verify.IsNotNull(broadcasterBlackboard)) return;

            PropertyCollection broadcasterBlackboardProps = broadcasterBlackboard.PropertyCollection;

            int obeliskState = broadcasterBlackboardProps[PropertyEnum.AICustomStateVal1];
            if (broadcaster.HasKeyword(ObeliskKeyword) && obeliskState == 1)
            {
                PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
                int powersActivated = blackboardProps[PropertyEnum.AICustomStateVal1];

                if (ObeliskDamageMonolithPowers.HasValue())
                {
                    int numDamagePowers = ObeliskDamageMonolithPowers.Length;
                    if (numDamagePowers > 0 && powersActivated < numDamagePowers)
                    {
                        PrototypeId obeliskPowerRef = ObeliskDamageMonolithPowers[Math.Min(powersActivated, numDamagePowers - 1)];

                        if (!Verify.IsTrue(ownerController.AttemptActivatePower(obeliskPowerRef, agent.Id, agent.RegionLocation.Position)))
                            return;

                        blackboardProps[PropertyEnum.AICustomStateVal1] = powersActivated + 1;

                        if (powersActivated + 1 >= numDamagePowers)
                        {
                            if (!Verify.IsTrue(ownerController.AttemptActivatePower(DisableShield, agent.Id, agent.RegionLocation.Position)))
                                return;
                        }
                    }
                }
            }
        }
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

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, PrimaryPower);
            InitPower(agent, SpecialPower);
            InitPower(agent, SpecialSummonPower);

            AIController ownerController = agent.AIController;
            if (!Verify.IsNotNull(ownerController)) return;
            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return;

            ownerController.RegisterForEntityAggroedEvents(region, true);
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

            HandleEnrage(ownerController);

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            EnrageState state = (EnrageState)(int)blackboardProps[PropertyEnum.AIEnrageState];
            if (state != EnrageState.Enraging)
            {
                WorldEntity target = ownerController.TargetEntity;
                if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                    proceduralAI.PartialOverrideBehavior == null)
                {
                    return;
                }

                GRandom random = game.Random;
                Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
                PopulatePowerPicker(ownerController, powerPicker);
                if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                    return;

                DefaultRangedFlankerMovement(proceduralAI, ownerController, agent, target, currentTime, MoveToTarget, FlankTarget);
            }
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);

            ownerController.AddPowersToPicker(powerPicker, PrimaryPower);
            ownerController.AddPowersToPicker(powerPicker, SpecialPower);
        }

        public override void OnPowerStarted(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            if (powerContext == SpecialPower)
            {
                Agent agent = ownerController.Owner;
                if (!Verify.IsNotNull(agent)) return;

                Region region = agent.Region;
                if (!Verify.IsNotNull(region)) return;

                const int MaxTargets = 32;
                using var validTargetsHandle = ListPool<WorldEntity>.Instance.Get(out List<WorldEntity> validTargets);

                Sphere volume = new(agent.RegionLocation.Position, SpecialPowerMaxRadius);
                foreach (WorldEntity targetInSphere in region.IterateEntitiesInVolume(volume, new(EntityRegionSPContextFlags.PrimaryPartition)))
                {
                    if (!Verify.IsNotNull(targetInSphere))
                        continue;

                    if (Combat.ValidTarget(agent.Game, agent, targetInSphere, CombatTargetType.Hostile, false, CombatTargetFlags.IgnoreAggroDistance))
                        validTargets.Add(targetInSphere);

                    if (validTargets.Count >= MaxTargets)
                        break;
                }

                int targetsSummoned = 0;
                foreach (WorldEntity validTarget in validTargets)
                {
                    if (!Verify.IsNotNull(validTarget))
                        continue;

                    ref RegionLocation targetRegionLoc = ref validTarget.RegionLocation;
                    if (!Verify.IsTrue(ownerController.AttemptActivatePower(SpecialSummonPower, validTarget.Id, targetRegionLoc.ProjectToFloor())))
                        return;

                    targetsSummoned++;
                    if (targetsSummoned >= SpecialPowerNumSummons)
                        break;
                }

                if (targetsSummoned < SpecialPowerNumSummons)
                {
                    for (int i = targetsSummoned; i < SpecialPowerNumSummons; i++)
                    {
                        Bounds bounds = agent.Bounds;  // copy
                        bounds.Center = agent.RegionLocation.ProjectToFloor();

                        region.ChooseRandomPositionNearPoint(
                            ref bounds,
                            Region.GetPathFlagsForEntity(agent.WorldEntityPrototype),
                            PositionCheckFlags.CanBeBlockedEntity | PositionCheckFlags.CanSweepTo | PositionCheckFlags.PreferNoEntity,
                            BlockingCheckFlags.None,
                            SpecialPowerMinRadius,
                            SpecialPowerMaxRadius,
                            out Vector3 randomPosition);

                        if (!Verify.IsTrue(ownerController.AttemptActivatePower(SpecialSummonPower, 0, randomPosition)))
                            return;
                    }
                }
            }
        }

        public override void OnEntityAggroedEvent(AIController ownerController, in EntityAggroedGameEvent aggroedEvent)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            Game game = agent.Game;
            if (!Verify.IsNotNull(game)) return;

            Agent aggroAgent = aggroedEvent.AggroEntity as Agent;
            if (!Verify.IsNotNull(aggroAgent)) return;

            if (aggroAgent.PrototypeDataRef == BrimstoneProtoRef && agent.Properties.HasProperty(PropertyEnum.EnrageStartTime) == false)
                agent.Properties[PropertyEnum.EnrageStartTime] = game.CurrentTime + TimeSpan.FromMinutes(EnrageTimerInMinutes);
        }
    }

    public class ProceduralProfileMistressOfMagmaPrototype : ProceduralProfileWithEnragePrototype
    {
        public ProceduralFlankContextPrototype FlankTarget { get; protected set; }
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype BombDancePower { get; protected set; }

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, BombDancePower);
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

            HandleEnrage(ownerController);

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            EnrageState state = (EnrageState)(int)blackboardProps[PropertyEnum.AIEnrageState];
            if (state != EnrageState.Enraging)
            {
                if (blackboardProps[PropertyEnum.AICustomStateVal1] == 1)
                {
                    if (!Verify.IsNotNull(BombDancePower.PowerContext)) return;
                    if (!Verify.IsNotNull(BombDancePower.PowerContext.Power)) return;

                    HandleUsePowerContext(ownerController, proceduralAI, game.Random, currentTime, BombDancePower.PowerContext, BombDancePower);
                }
                else
                {
                    WorldEntity target = ownerController.TargetEntity;
                    DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile);

                    GRandom random = game.Random;
                    Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
                    PopulatePowerPicker(ownerController, powerPicker);
                    if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                        return;

                    DefaultRangedFlankerMovement(proceduralAI, ownerController, agent, target, currentTime, MoveToTarget, FlankTarget);
                }
            }

            long totalHealth = agent.Properties[PropertyEnum.Health];
            int numTargets = 1;
            ConditionCollection conditions = agent.ConditionCollection;
            if (!Verify.IsNotNull(conditions)) return;

            EntityManager entityManager = game.EntityManager;

            foreach (Condition condition in conditions.IterateConditions(true))
            {
                if (!Verify.IsNotNull(condition)) return;

                ulong transferId = condition.Properties[PropertyEnum.DamageTransferID];
                if (transferId != Entity.InvalidId)
                {
                    WorldEntity transferEntity = entityManager.GetEntity<WorldEntity>(transferId);
                    if (transferEntity != null)
                    {
                        ++numTargets;
                        totalHealth += transferEntity.Properties[PropertyEnum.Health];
                    }
                }
            }

            // Math.Max() is needed here because directly setting health to 0 will skip Kill() / OnKilled() and turn the boss into a "zombie".
            if (numTargets > 1)
                agent.Properties[PropertyEnum.Health] = Math.Max(totalHealth / numTargets, 1);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);

            ownerController.AddPowersToPicker(powerPicker, BombDancePower);
        }

        public override bool OnPowerPicked(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            if (base.OnPowerPicked(ownerController, powerContext) == false)
                return false;

            if (powerContext == BombDancePower)
                ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = 1;

            return true;
        }

        public override void OnPowerEnded(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            base.OnPowerEnded(ownerController, powerContext);

            if (powerContext == BombDancePower)
                ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = 0;
        }
    }

    public class MistressOfMagmaTeleportDestPrototype : Prototype
    {
        public SelectEntityContextPrototype DestinationSelector { get; protected set; }
        public PrototypeId ImmunityBoost { get; protected set; }
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

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, FirePillarPower);

            AIController ownerController = agent.AIController;
            if (!Verify.IsNotNull(ownerController)) return;
            Game game = agent.Game;
            if (!Verify.IsNotNull(game)) return;

            long currentTime = (long)game.CurrentTime.TotalMilliseconds;

            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return;

            ownerController.RegisterForEntityDeadEvents(region, true);

            long cooldown = currentTime + game.Random.Next(FirePillarMinCooldownMS, FirePillarMaxCooldownMS);
            ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomTimeVal1] = cooldown;
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

            HandleEnrage(ownerController);

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            EnrageState state = (EnrageState)(int)blackboardProps[PropertyEnum.AIEnrageState];
            if (state != EnrageState.Enraging)
            {
                AttemptFirePillarPower(ownerController, currentTime);

                WorldEntity target = ownerController.TargetEntity;
                if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                    proceduralAI.PartialOverrideBehavior == null)
                {
                    return;
                }

                GRandom random = game.Random;
                Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
                PopulatePowerPicker(ownerController, powerPicker);
                HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true);
            }
        }

        private void AttemptFirePillarPower(AIController ownerController, long currentTime)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;

            long cooldown = blackboardProps[PropertyEnum.AICustomTimeVal1];
            if (agent.Properties[PropertyEnum.SinglePowerLock, FirePillarPower] == false && currentTime > cooldown)
            {
                Game game = ownerController.Game;
                if (!Verify.IsNotNull(game)) return;

                Region region = agent.Region;
                if (!Verify.IsNotNull(region, $"Entity is not in a valid region! Entity: {agent}"))
                    return;

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
                TuningPrototype difficultyProto = region.TuningTable?.Prototype;
#else
                DifficultyPrototype difficultyProto = region.DifficultyTable?.Prototype;
#endif
                if (!Verify.IsNotNull(difficultyProto)) return;

                Sphere volume = new(agent.RegionLocation.Position, difficultyProto.PlayerNearbyRange);
                using var targetsHandle = ListPool<(float, WorldEntity)>.Instance.Get(out List<(float, WorldEntity)> targets);
                foreach (WorldEntity target in region.IterateAvatarsInVolume(volume))
                {
                    if (target == null)
                        continue;

                    float distanceToTarget = agent.GetDistanceTo(target, true);
                    targets.Add((distanceToTarget, target));
                }

                // NOTE: Using a list will theoretically allow two targets at the exact same distance to be both affected by FirePillar,
                // which was not possible with the original map-based C++ implementation, but seems to be the intention.
                targets.Sort((a, b) => b.Item1.CompareTo(a.Item1));

                int numTargets = 0;
                foreach ((_, WorldEntity target) in targets)
                {
                    if (numTargets > FirePillarPowerMaxTargets)
                        break;

                    if (!Verify.IsNotNull(target))
                        continue;

                    ownerController.AttemptActivatePower(FirePillarPower, target.Id, target.RegionLocation.Position);
                    numTargets++;
                }

                cooldown = currentTime + game.Random.Next(FirePillarMinCooldownMS, FirePillarMaxCooldownMS);
                blackboardProps[PropertyEnum.AICustomTimeVal1] = cooldown;
            }
        }

        public override void OnEntityDeadEvent(AIController ownerController, in EntityDeadGameEvent deadEvent)
        {
            if (!Verify.IsNotNull(deadEvent.Defender)) return;

            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

            PrototypeId defenderRef = deadEvent.Defender.PrototypeDataRef;
            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;

            if (defenderRef == MiniBossBrimstone)
            {
                agent.Properties[PropertyEnum.SinglePowerLock, PowerUnlockBrimstone] = false;
                blackboardProps.AdjustProperty(1, PropertyEnum.AICustomStateVal1);
            }
            else if (defenderRef == MiniBossHellfire)
            {
                agent.Properties[PropertyEnum.SinglePowerLock, PowerUnlockHellfire] = false;
                blackboardProps.AdjustProperty(1, PropertyEnum.AICustomStateVal1);
            }
            else if (defenderRef == MiniBossMistress)
            {
                agent.Properties[PropertyEnum.SinglePowerLock, PowerUnlockMistress] = false;
                blackboardProps.AdjustProperty(1, PropertyEnum.AICustomStateVal1);
            }
            else if (defenderRef == MiniBossMonolith)
            {
                agent.Properties[PropertyEnum.SinglePowerLock, PowerUnlockMonolith] = false;
                blackboardProps.AdjustProperty(1, PropertyEnum.AICustomStateVal1);
            }
            else if (defenderRef == MiniBossSlag)
            {
                agent.Properties[PropertyEnum.SinglePowerLock, PowerUnlockSlag] = false;
                blackboardProps.AdjustProperty(1, PropertyEnum.AICustomStateVal1);
            }

            if (blackboardProps[PropertyEnum.AICustomStateVal1] >= 5)
            {
                Region region = agent.Region;
                if (!Verify.IsNotNull(region)) return;

                ownerController.RegisterForEntityDeadEvents(region, false);
            }
        }
    }

    public class ProceduralProfileSurturPortalPrototype : ProceduralProfileNoMoveNoSensePrototype
    {
        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            // No base call here, same as the client.

            if (GenericProceduralPowers.HasValue())
            {
                int index = ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
                if (Verify.IsTrue(index >= 0 && index < GenericProceduralPowers.Length))
                    ownerController.AddPowersToPicker(powerPicker, GenericProceduralPowers[index]);
            }
        }

        public override void OnPowerEnded(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            ownerController.Blackboard.PropertyCollection.AdjustProperty(1, PropertyEnum.AICustomStateVal1);
        }
    }

    public class ProceduralProfileObeliskHealerPrototype : ProceduralProfileBasicMeleePrototype
    {
        public PrototypeId[] ObeliskTargets { get; protected set; }

        //---

        private enum State
        {
            TargetObelisk,
            MoveToObelisk,
            HealObelisk,
            AllObelisksHealed,
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

            if (agent.IsDormant)
                return;

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            int stateVal = blackboardProps[PropertyEnum.AICustomStateVal1];
            int obeliskHealed = blackboardProps[PropertyEnum.AICustomStateVal2];
            switch ((State)stateVal)
            {
                case State.TargetObelisk:
                    bool obeliskTargetFound = false;
                    if (ObeliskTargets.HasValue())
                    {
                        PrototypeId obeliskDataRef = ObeliskTargets[Math.Min(obeliskHealed, ObeliskTargets.Length - 1)];
                        if (!Verify.IsTrue(obeliskDataRef != PrototypeId.Invalid)) return;

                        Region region = agent.Region;
                        if (!Verify.IsNotNull(region)) return;

                        Sphere volume = new(agent.RegionLocation.Position, 3200.0f);
                        foreach (WorldEntity targetEntity in region.IterateEntitiesInVolume(volume, new (EntityRegionSPContextFlags.PrimaryPartition)))
                        {
                            if (targetEntity != null && targetEntity.PrototypeDataRef == obeliskDataRef)
                            {
                                blackboardProps[PropertyEnum.AIAssistedEntityID] = targetEntity.Id;
                                ownerController.SetTargetEntity(targetEntity);
                                obeliskTargetFound = true;
                                blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.MoveToObelisk;
                                break;
                            }
                        }             
                    }

                    Verify.IsTrue(obeliskTargetFound, $"The obelisk healer cannot find an obelisk to heal! {agent}");
                    break;

                case State.MoveToObelisk:
                    WorldEntity obeliskTarget = ownerController.AssistedEntity;
                    if (obeliskTarget != null)
                    {
                        StaticBehaviorReturnType contextResult = HandleContext(proceduralAI, ownerController, MoveToTarget);
                        Verify.IsTrue(contextResult != StaticBehaviorReturnType.Failed && contextResult != StaticBehaviorReturnType.Interrupted,
                            $"The obelisk healer {agent} cannot move to an obelisk {obeliskTarget} to heal!");

                        if (contextResult == StaticBehaviorReturnType.Completed)
                            blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.HealObelisk;
                        else if (contextResult == StaticBehaviorReturnType.Running)
                            return;
                    }
                    else
                    {
                        Verify.IsTrue(false, $"The obelisk healer cannot find an obelisk to move to because its AssistedEntity is NULL! {agent}");
                    }

                    break;

                case State.HealObelisk:
                    WorldEntity target = ownerController.TargetEntity;
                    WorldEntity assistedEntity = ownerController.AssistedEntity;

                    Verify.IsNotNull(target);
                    Verify.IsNotNull(assistedEntity);
                    Verify.IsTrue(target == assistedEntity, $"The obelisk healer's target {target} is not the same as it's assisted entity {assistedEntity}!");

                    GRandom random = game.Random;
                    Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
                    PopulatePowerPicker(ownerController, powerPicker);
                    StaticBehaviorReturnType proceduralPowerResult = HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true);

                    // TODO: Figure out why this verify gets spammed, there is probably an underlying issue with the way we handle UsePower state or something.
                    //Verify.IsTrue(proceduralPowerResult != StaticBehaviorReturnType.Failed && proceduralPowerResult != StaticBehaviorReturnType.Interrupted,
                    //    $"The obelisk healer's power failed or was interrupted when trying to heal the obelisk!\nResult: {proceduralPowerResult}\nHealer: {agent}\nObelisk: {assistedEntity}");

                    if (proceduralPowerResult == StaticBehaviorReturnType.Completed)
                    {
                        long health = assistedEntity.Properties[PropertyEnum.Health];
                        long maxHealth = assistedEntity.Properties[PropertyEnum.HealthMaxOther];
                        if (health == maxHealth)
                        {
                            obeliskHealed = blackboardProps[PropertyEnum.AICustomStateVal2];
                            if (obeliskHealed == ObeliskTargets.Length)
                            {
                                blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.AllObelisksHealed;
                            }
                            else
                            {
                                blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.TargetObelisk;
                                blackboardProps[PropertyEnum.AICustomStateVal2] = obeliskHealed + 1;
                            }
                        }
                    }

                    break;
            }
        }
    }

    public class ProceduralProfileObeliskPrototype : ProceduralProfileNoMoveDefaultSensoryPrototype
    {
        public PrototypeId DeadEntityForDetonateIslandPower { get; protected set; }
        public PrototypeId DetonateIslandPower { get; protected set; }
        public PrototypeId FullyHealedPower { get; protected set; }

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, DetonateIslandPower);
            InitPower(agent, FullyHealedPower);

            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return;
            AIController ownerController = agent.AIController;
            if (!Verify.IsNotNull(ownerController)) return;

            ownerController.RegisterForEntityDeadEvents(region, true);
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

            if (agent.IsDormant)
                return;

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            PropertyCollection blackboardProps = blackboard.PropertyCollection;
            int stateVal = blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            if (stateVal == 0)
            {
                WorldEntity target = ownerController.TargetEntity;
                if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                    proceduralAI.PartialOverrideBehavior == null)
                {
                    return;
                }

                GRandom random = game.Random;
                Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
                PopulatePowerPicker(ownerController, powerPicker);
                if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                    return;

                long health = agent.Properties[PropertyEnum.Health];
                long maxHealth = agent.Properties[PropertyEnum.HealthMaxOther];

                if (health == maxHealth)
                {
                    if (!Verify.IsTrue(ownerController.AttemptActivatePower(FullyHealedPower, agent.Id, agent.RegionLocation.Position)))
                        return;

                    blackboardProps[PropertyEnum.AICustomStateVal1] = 1;

                    Region region = agent.Region;
                    if (!Verify.IsNotNull(region)) return;

                    ownerController.RegisterForEntityDeadEvents(region, false);
                    
                    AIBroadcastBlackboardGameEvent evt = new(agent, blackboard);
                    region.AIBroadcastBlackboardEvent.Invoke(evt);
                }
            }
        }

        public override void OnEntityDeadEvent(AIController ownerController, in EntityDeadGameEvent deadEvent)
        {
            if (!Verify.IsNotNull(deadEvent.Defender)) return;
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

            PrototypeId defenderRef = deadEvent.Defender.PrototypeDataRef;
            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;

            int stateVal = blackboardProps[PropertyEnum.AICustomStateVal1];
            if (stateVal != 1 && defenderRef == DeadEntityForDetonateIslandPower)
            {
                WorldEntity deadArcher = deadEvent.Defender;
                if (!Verify.IsNotNull(deadArcher)) return;

                Verify.IsTrue(ownerController.AttemptActivatePower(DetonateIslandPower, deadArcher.Id, deadArcher.RegionLocation.Position));
            }
        }
    }

    public class ProceduralProfileFireGiantChaserPrototype : ProceduralProfileBasicMeleePrototype
    {
        public ProceduralUsePowerContextPrototype MarkTargetPower { get; protected set; }
        public PrototypeId MarkTargetVFXRemoval { get; protected set; }

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, MarkTargetPower);
            InitPower(agent, MarkTargetVFXRemoval);
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

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            int stateVal = blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            if (stateVal == 0)
            {
                WorldEntity target = ownerController.TargetEntity;
                if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                    proceduralAI.PartialOverrideBehavior == null)
                {
                    return;
                }

                GRandom random = game.Random;
                Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
                PopulatePowerPicker(ownerController, powerPicker);
                if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                    return;

                DefaultMeleeMovement(proceduralAI, ownerController, agent.Locomotor, target, MoveToTarget, OrbitTarget);
            }
            else
            {
                GRandom random = game.Random;
                Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
                PopulatePowerPicker(ownerController, powerPicker);
                if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                    return;

                HandleContext(proceduralAI, ownerController, MoveToTarget);
            }
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);

            ownerController.AddPowersToPicker(powerPicker, PrimaryPower);
            ownerController.AddPowersToPicker(powerPicker, MarkTargetPower);
        }

        public override bool OnPowerPicked(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            if (base.OnPowerPicked(ownerController, powerContext) == false)
                return false;

            if (powerContext == MarkTargetPower)
                ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = 1;

            return true;
        }

        public override void OnOwnerKilled(AIController ownerController)
        {
            WorldEntity target = ownerController.TargetEntity;
            if (target != null)
                Verify.IsTrue(ownerController.AttemptActivatePower(MarkTargetVFXRemoval, target.Id, target.RegionLocation.Position));
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

        //---

        private enum State
        {
            Default,
            ActivateHulkBuster,
            MoveToCrate,
            UseWeaponCrate,
        }

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, ActivateHulkBusterAnimOnly);
            InitPowers(agent, WeaponsCratesAnimOnlyPowers);

            AIController ownerController = agent.AIController;
            if (!Verify.IsNotNull(ownerController)) return;

            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return;

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;

            if (!Verify.IsTrue(WeaponsCrate != PrototypeId.Invalid)) return;

            Sphere volume = new(agent.RegionLocation.Position, ownerController.AggroRangeHostile);
            foreach (WorldEntity target in region.IterateEntitiesInVolume(volume, new(EntityRegionSPContextFlags.PrimaryPartition)))
            {
                if (target != null && target.PrototypeDataRef == WeaponsCrate)
                {
                    blackboardProps[PropertyEnum.AIAssistedEntityID] = target.Id;
                    break;
                }
            }
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

            if (agent.IsDormant)
                return;
            
            BehaviorBlackboard blackboard = ownerController.Blackboard;
            PropertyCollection blackboardProps = blackboard.PropertyCollection;
            int state = blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            switch ((State)state)
            {
                case State.Default:
                    if (proceduralAI.GetState(0) != UsePower.Instance)
                    {
                        int hulkBusterState = blackboardProps[PropertyEnum.AICustomStateVal2];
                        long health = agent.Properties[PropertyEnum.Health];
                        long maxHealth = agent.Properties[PropertyEnum.HealthMax];

                        if (MathHelper.IsBelowOrEqual(health, maxHealth, HulkBusterHealthThreshold1) && hulkBusterState < 1)
                            blackboardProps[PropertyEnum.AICustomStateVal2] = 1;
                        else if (MathHelper.IsBelowOrEqual(health, maxHealth, HulkBusterHealthThreshold2) && hulkBusterState < 2)
                            blackboardProps[PropertyEnum.AICustomStateVal2] = 2;
                        else if (MathHelper.IsBelowOrEqual(health, maxHealth, HulkBusterHealthThreshold3) && hulkBusterState < 3)
                            blackboardProps[PropertyEnum.AICustomStateVal2] = 3;
                        else if (MathHelper.IsBelowOrEqual(health, maxHealth, HulkBusterHealthThreshold4) && hulkBusterState < 4)
                            blackboardProps[PropertyEnum.AICustomStateVal2] = 4;

                        if (hulkBusterState != blackboardProps[PropertyEnum.AICustomStateVal2])
                        {
                            blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.ActivateHulkBuster;
                            return;
                        }
                    }

                    WorldEntity target = ownerController.TargetEntity;
                    if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                        proceduralAI.PartialOverrideBehavior == null)
                    {
                        return;
                    }

                    GRandom random = game.Random;
                    Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
                    PopulatePowerPicker(ownerController, powerPicker);
                    if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                        return;

                    DefaultRangedFlankerMovement(proceduralAI, ownerController, agent, target, currentTime, MoveToTarget, FlankTarget);
                    break;

                case State.ActivateHulkBuster:
                    if (proceduralAI.GetState(0) != UsePower.Instance)
                    {
                        if (HulkBustersToActivate.HasValue())
                        {
                            int numHulkBusters = HulkBustersToActivate.Length;
                            if (numHulkBusters > 0)
                            {
                                int hulkBusterIndex = blackboard.PropertyCollection[PropertyEnum.AICustomStateVal2] - 1;
                                if (!Verify.IsTrue(hulkBusterIndex >= 0 && hulkBusterIndex <= numHulkBusters - 1,
                                    $"Red Skull {agent} is trying to activate a Hulk Buster outside the bounds of his HulkBustersToActivate list {hulkBusterIndex}!"))
                                {
                                    return;
                                }

                                PrototypeId hulkBusterDataRef = HulkBustersToActivate[Math.Min(hulkBusterIndex, numHulkBusters - 1)];
                                if (!Verify.IsTrue(hulkBusterDataRef != PrototypeId.Invalid)) return;

                                blackboard.PropertyCollection[PropertyEnum.AICustomProtoRef1] = hulkBusterDataRef;

                                Region region = agent.Region;
                                if (!Verify.IsNotNull(region)) return;

                                AIBroadcastBlackboardGameEvent evt = new(agent, blackboard);
                                region.AIBroadcastBlackboardEvent.Invoke(evt);
                            }
                        }

                        StaticBehaviorReturnType powerResult = HandleUsePowerContext(ownerController, proceduralAI, game.Random, currentTime, ActivateHulkBusterAnimOnly.PowerContext, ActivateHulkBusterAnimOnly);
                        if (powerResult == StaticBehaviorReturnType.Failed || powerResult == StaticBehaviorReturnType.Interrupted)
                        {
                            Verify.IsTrue(false, $"Red Skull failed to play his ActivateHulkBusterAnimOnly power! Reason: {powerResult}  RedSkull: {agent}");
                            blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = (int)State.MoveToCrate;
                            return;
                        }
                    }
                    else
                    {
                        if (!Verify.IsNotNull(ActivateHulkBusterAnimOnly)) return;

                        UsePowerContextPrototype hulkBusterUsePowerContext = ActivateHulkBusterAnimOnly.PowerContext;
                        if (!Verify.IsNotNull(hulkBusterUsePowerContext)) return;

                        PowerPrototype hulkBusterPowerProto = hulkBusterUsePowerContext.Power;
                        if (!Verify.IsNotNull(hulkBusterPowerProto)) return;
                        if (!Verify.IsTrue(hulkBusterPowerProto.DataRef != PrototypeId.Invalid)) return;

                        if (!Verify.IsTrue(hulkBusterPowerProto.DataRef == ownerController.ActivePowerRef,
                            $"Red Skull {agent} should be activating his Hulk Buster power but is currently activating another power [{ownerController.ActivePowerRef.GetName()}]!"))
                        {
                            return;
                        }

                        StaticBehaviorReturnType powerResult = HandleUsePowerContext(ownerController, proceduralAI, game.Random, currentTime, hulkBusterUsePowerContext, ActivateHulkBusterAnimOnly);
                        if (powerResult == StaticBehaviorReturnType.Running)
                            return;
                    }

                    break;

                case State.MoveToCrate:
                    WorldEntity weaponsCrateTarget = ownerController.AssistedEntity;
                    if (weaponsCrateTarget == null)
                    {
                        Verify.IsTrue(false, $"Red Skull {agent} cannot find a weapon crate!");
                        blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = (int)State.UseWeaponCrate;
                        return;
                    }

                    ownerController.SetTargetEntity(weaponsCrateTarget);

                    StaticBehaviorReturnType contextResult = HandleContext(proceduralAI, ownerController, MoveToWeaponsCrate);
                    Verify.IsTrue(contextResult != StaticBehaviorReturnType.Failed && contextResult != StaticBehaviorReturnType.Interrupted,
                        $"Red Skull cannot move to a weapon crate! Reason: {contextResult}  RedSkull: [{agent}]  WeaponCrate: [{weaponsCrateTarget}]");

                    if (contextResult != StaticBehaviorReturnType.Running) 
                        blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = (int)State.UseWeaponCrate;

                    return;

                case State.UseWeaponCrate:
                    weaponsCrateTarget = ownerController.AssistedEntity;
                    if (weaponsCrateTarget == null)
                    {
                        Verify.IsTrue(false, $"Red Skull {agent} is trying to play his WeaponsCratesAnimOnly when he doesn't have a target crate!");
                        OnActivatedWeaponCrate(ownerController);
                        return;
                    }

                    int weaponCrateIndex = blackboard.PropertyCollection[PropertyEnum.AICustomStateVal3];
                    ProceduralUsePowerContextPrototype weaponsCrateUsePowerContext = GetWeaponCrateAnimPowerByIndex(ownerController, weaponCrateIndex);

                    if (!Verify.IsNotNull(weaponsCrateUsePowerContext)) return;
                    if (!Verify.IsNotNull(weaponsCrateUsePowerContext.PowerContext)) return;
                    if (!Verify.IsNotNull(weaponsCrateUsePowerContext.PowerContext.Power)) return;

                    if (proceduralAI.GetState(0) != UsePower.Instance)
                    {
                        StaticBehaviorReturnType powerResult = HandleUsePowerContext(ownerController, proceduralAI, game.Random, currentTime, weaponsCrateUsePowerContext.PowerContext, weaponsCrateUsePowerContext);
                        if (powerResult == StaticBehaviorReturnType.Failed || powerResult == StaticBehaviorReturnType.Interrupted)
                        {
                            Verify.IsTrue(false, $"Red Skull {agent} failed to play his WeaponsCratesAnimOnly power on a crate {weaponsCrateTarget}!");
                            OnActivatedWeaponCrate(ownerController);
                        }
                    }
                    else
                    {
                        PowerPrototype weaponsCratePowerProto = weaponsCrateUsePowerContext.PowerContext.Power;
                        if (!Verify.IsNotNull(weaponsCratePowerProto)) return;
                        if (!Verify.IsTrue(weaponsCratePowerProto.DataRef != PrototypeId.Invalid)) return;

                        if (!Verify.IsTrue(weaponsCratePowerProto.DataRef == ownerController.ActivePowerRef,
                            $"Red Skull {agent} should be activating his Weapons Crate power but is currently activating another power [{ownerController.ActivePowerRef.GetName()}]!"))
                        {
                            return;
                        }

                        HandleUsePowerContext(ownerController, proceduralAI, game.Random, currentTime, weaponsCrateUsePowerContext.PowerContext, weaponsCrateUsePowerContext);
                    }

                    break;
            }            
        }

        private ProceduralUsePowerContextPrototype GetWeaponCrateAnimPowerByIndex(AIController ownerController, int index)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return null;

            if (!Verify.IsTrue(WeaponsCratesAnimOnlyPowers.HasValue())) return null;

            int numPowers = WeaponsCratesAnimOnlyPowers.Length;
            if (!Verify.IsTrue(numPowers > 0)) return null;
            if (!Verify.IsTrue(index >= 0 && index <= numPowers - 1)) return null;

            return WeaponsCratesAnimOnlyPowers[index];
        }

        private void OnActivatedWeaponCrate(AIController ownerController)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;

            WorldEntity weaponsCrateTarget = ownerController.AssistedEntity;
            Verify.IsNotNull(weaponsCrateTarget, $"Red Skull {agent} is trying to unlock a new power but when he doesn't have a target crate!");

            int weaponsUnlockIndex = blackboardProps[PropertyEnum.AICustomStateVal3];
            switch (weaponsUnlockIndex)
            {
                case 0:
                    agent.Properties[PropertyEnum.SinglePowerLock, WeaponCrate1UnlockPower] = false;
                    break;
                case 1:
                    agent.Properties[PropertyEnum.SinglePowerLock, WeaponCrate2UnlockPower] = false;
                    break;
                case 2:
                    agent.Properties[PropertyEnum.SinglePowerLock, WeaponCrate3UnlockPower] = false;
                    break;
                case 3:
                    agent.Properties[PropertyEnum.SinglePowerLock, WeaponCrate4UnlockPower] = false;
                    break;
                default:
                    // NOTE: When weaponsUnlockIndex is 4, nothing will happen and there won't be a verify fail. This is client-accurate.
                    Verify.IsTrue(weaponsUnlockIndex <= 4, $"Red Skull {agent} is trying to unlock a new power with an invalid index {weaponsUnlockIndex}!");
                    break;
            }

            blackboardProps.AdjustProperty(1, PropertyEnum.AICustomStateVal3);
            blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.Default;
        }

        public override void OnPowerEnded(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            int state = blackboardProps[PropertyEnum.AICustomStateVal1];

            base.OnPowerEnded(ownerController, powerContext);

            if (powerContext == ActivateHulkBusterAnimOnly)
            {
                blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.MoveToCrate;
            }
            else if (state == (int)State.UseWeaponCrate)
            {
                int index = blackboardProps[PropertyEnum.AICustomStateVal3];
                ProceduralUsePowerContextPrototype weaponsCrateUsePowerContext = GetWeaponCrateAnimPowerByIndex(ownerController, index);
                if (powerContext == weaponsCrateUsePowerContext)
                    OnActivatedWeaponCrate(ownerController);
            }
        }
    }

    public class ProceduralProfileHulkBusterOSPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public PrototypeId RedSkullAxis { get; protected set; }
        public ProceduralUsePowerContextPrototype ShieldRedSkull { get; protected set; }
        public ProceduralUsePowerContextPrototype DeactivatedAnimOnly { get; protected set; }
        public ProceduralUsePowerContextPrototype ActivatingAnimOnly { get; protected set; }

        //---

        private enum State
        {
            Default,
            Deactivated,
            Activating,
            ShieldRedSkull,
            MoveToTarget,
        }

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, ShieldRedSkull);
            InitPower(agent, DeactivatedAnimOnly);
            InitPower(agent, ActivatingAnimOnly);

            AIController ownerController = agent.AIController;
            if (!Verify.IsNotNull(ownerController)) return;
            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return;

            ownerController.RegisterForAIBroadcastBlackboardEvents(region, true);
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

            if (agent.IsDormant)
                return;

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            int state = blackboardProps[PropertyEnum.AICustomStateVal1];
            switch ((State)state)
            {
                case State.Default:
                    Region region = agent.Region;
                    if (!Verify.IsNotNull(region, $"Entity is not in a valid region! Entity: {agent}"))
                        return;

                    Sphere volume = new(agent.RegionLocation.Position, 2500.0f);
                    foreach (Avatar nearbyAvatar in region.IterateAvatarsInVolume(volume))
                    {
                        if (nearbyAvatar != null && nearbyAvatar.IsAliveInWorld)
                        {
                            blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.Deactivated;
                            break;
                        }
                    }

                    break;

                case State.Deactivated:
                    HandleUsePowerContext(ownerController, proceduralAI, game.Random, currentTime, DeactivatedAnimOnly.PowerContext, DeactivatedAnimOnly);
                    break;

                case State.Activating:
                    HandleUsePowerContext(ownerController, proceduralAI, game.Random, currentTime, ActivatingAnimOnly.PowerContext, ActivatingAnimOnly);
                    break;

                case State.ShieldRedSkull:
                    WorldEntity redSkull = ownerController.TargetEntity;
                    if (!Verify.IsNotNull(redSkull, $"Hulk Buster {agent} pending target is NULL!"))
                        return;

                    if (!Verify.IsTrue(redSkull.PrototypeDataRef == RedSkullAxis, $"Hulk Buster {agent} pending target is not RedSkull, it's {redSkull}!"))
                        return;

                    HandleRotateToTarget(agent, redSkull);
                    HandleUsePowerContext(ownerController, proceduralAI, game.Random, currentTime, ShieldRedSkull.PowerContext, ShieldRedSkull);
                    break;

                case State.MoveToTarget:
                    WorldEntity target = ownerController.TargetEntity;
                    if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                        proceduralAI.PartialOverrideBehavior == null)
                    {
                        return;
                    }

                    GRandom random = game.Random;
                    Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
                    PopulatePowerPicker(ownerController, powerPicker);
                    if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                        return;

                    DefaultMeleeMovement(proceduralAI, ownerController, agent.Locomotor, target, MoveToTarget, OrbitTarget);
                    break;
            }
        }

        public override void OnPowerEnded(AIController ownerController, ProceduralUsePowerContextPrototype proceduralPowerContext)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

            base.OnPowerEnded(ownerController, proceduralPowerContext);

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            if (proceduralPowerContext == ActivatingAnimOnly)
            {
                Game game = ownerController.Game;
                if (!Verify.IsNotNull(game)) return;

                ulong redSkullId = blackboardProps[PropertyEnum.AICustomEntityId1];
                WorldEntity redSkull = game.EntityManager.GetEntity<WorldEntity>(redSkullId);
                if (!Verify.IsNotNull(redSkull, $"Hulk Buster {agent} pending target is NULL!"))
                    return;

                if (!Verify.IsTrue(redSkull.PrototypeDataRef == RedSkullAxis, $"Hulk Buster {agent} pending target is not RedSkull, it's {redSkull}!"))
                    return;

                ownerController.SetTargetEntity(redSkull);

                blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.ShieldRedSkull;
            }
            else if (proceduralPowerContext == ShieldRedSkull)
            {
                blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.MoveToTarget;
            }
        }

        public override void OnAIBroadcastBlackboardEvent(AIController ownerController, in AIBroadcastBlackboardGameEvent broadcastEvent)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            WorldEntity broadcaster = broadcastEvent.Broadcaster;
            if (!Verify.IsNotNull(broadcaster)) return;
            BehaviorBlackboard broadcasterBlackboard = broadcastEvent.Blackboard;
            if (!Verify.IsNotNull(broadcasterBlackboard)) return;

            if (broadcaster.PrototypeDataRef == RedSkullAxis &&
                broadcasterBlackboard.PropertyCollection[PropertyEnum.AICustomProtoRef1] == agent.PrototypeDataRef)
            {
                PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;

                ProceduralAI proceduralAI = ownerController.Brain;
                if (!Verify.IsNotNull(proceduralAI)) return;

                proceduralAI.SwitchProceduralState(null, null, StaticBehaviorReturnType.Interrupted);
                blackboardProps[PropertyEnum.AICustomEntityId1] = broadcaster.Id;
                blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.Activating;
            }
        }
    }

    public class ProceduralProfileSymbioteDrainPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralUsePowerContextPrototype SymbiotePower1 { get; protected set; }
        public ProceduralUsePowerContextPrototype SymbiotePower2 { get; protected set; }
        public ProceduralUsePowerContextPrototype SymbiotePower3 { get; protected set; }

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, SymbiotePower1);
            InitPower(agent, SymbiotePower2);
            InitPower(agent, SymbiotePower3);

            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return;
            AIController ownerController = agent.AIController;
            if (!Verify.IsNotNull(ownerController)) return;

            ownerController.RegisterForEntityDeadEvents(region, true);
        }

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            Game game = agent.Game;
            if (!Verify.IsNotNull(game)) return;

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            ulong targetId1 = blackboardProps[PropertyEnum.AICustomEntityId1];
            ulong targetId2 = blackboardProps[PropertyEnum.AICustomEntityId2];
            ulong targetId3 = blackboardProps[PropertyEnum.AICustomEntityId3];

            if (targetId1 == Entity.InvalidId)
            {
                ulong targetId = GetSymbioteTargetId(ownerController);
                if (targetId != Entity.InvalidId)
                {
                    StaticBehaviorReturnType powerResult = ActivateSymbiotePowerOnTarget(ownerController, SymbiotePower1, targetId);
                    if (powerResult == StaticBehaviorReturnType.Running || powerResult == StaticBehaviorReturnType.Completed)
                        blackboardProps[PropertyEnum.AICustomEntityId1] = targetId;
                }
            }

            if (targetId2 == Entity.InvalidId)
            {
                ulong targetId = GetSymbioteTargetId(ownerController);
                if (targetId != Entity.InvalidId)
                {
                    StaticBehaviorReturnType powerResult = ActivateSymbiotePowerOnTarget(ownerController, SymbiotePower2, targetId);
                    if (powerResult == StaticBehaviorReturnType.Running || powerResult == StaticBehaviorReturnType.Completed)
                        blackboardProps[PropertyEnum.AICustomEntityId2] = targetId;
                }
            }

            if (targetId3 == Entity.InvalidId)
            {
                ulong targetId = GetSymbioteTargetId(ownerController);
                if (targetId != Entity.InvalidId)
                {
                    StaticBehaviorReturnType powerResult = ActivateSymbiotePowerOnTarget(ownerController, SymbiotePower3, targetId);
                    if (powerResult == StaticBehaviorReturnType.Running || powerResult == StaticBehaviorReturnType.Completed)
                        blackboardProps[PropertyEnum.AICustomEntityId3] = targetId;
                }
            }
        }

        private StaticBehaviorReturnType ActivateSymbiotePowerOnTarget(AIController ownerController, ProceduralUsePowerContextPrototype symboitePower, ulong targetId)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return StaticBehaviorReturnType.Failed;

            if (!Verify.IsNotNull(symboitePower)) return StaticBehaviorReturnType.Failed;

            Game game = ownerController.Game;
            if (!Verify.IsNotNull(game)) return StaticBehaviorReturnType.Failed;

            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return StaticBehaviorReturnType.Failed;

            long currentTime = (long)game.CurrentTime.TotalMilliseconds;

            ownerController.Blackboard.PropertyCollection[PropertyEnum.AIRawTargetEntityID] = targetId;

            return HandleUsePowerContext(ownerController, proceduralAI, game.Random, currentTime, symboitePower.PowerContext, symboitePower);
        }

        private static ulong GetSymbioteTargetId(AIController ownerController)
        {
            ulong targetId = Entity.InvalidId;

            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return targetId;

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;

            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return Entity.InvalidId;

            Game game = ownerController.Game;
            if (!Verify.IsNotNull(game)) return Entity.InvalidId;

            EntityManager entityManager = game.EntityManager;
            WorldEntity target1 = entityManager.GetEntity<WorldEntity>(blackboardProps[PropertyEnum.AICustomEntityId1]);
            WorldEntity target2 = entityManager.GetEntity<WorldEntity>(blackboardProps[PropertyEnum.AICustomEntityId2]);
            WorldEntity target3 = entityManager.GetEntity<WorldEntity>(blackboardProps[PropertyEnum.AICustomEntityId3]);

            Picker<ulong> targetPicker = new(game.Random);
            Sphere volume = new(agent.RegionLocation.Position, ownerController.AggroRangeHostile);
            foreach (WorldEntity target in region.IterateEntitiesInVolume(volume, new(EntityRegionSPContextFlags.PrimaryPartition)))
            {
                if (target != null && target.IsHostileTo(agent) && target.IsDead == false &&
                    target != target1 && target != target2 && target != target3)
                {
                    targetPicker.Add(target.Id);
                    if (targetPicker.GetNumElements() >= 20)
                        break;
                }
            }

            if (targetPicker.Empty() == false)
                targetPicker.Pick(out targetId);

            return targetId;
        }

        public override void OnEntityDeadEvent(AIController ownerController, in EntityDeadGameEvent deadEvent)
        {
            if (!Verify.IsNotNull(deadEvent.Defender)) return;
            
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

            ulong deadEntityId = deadEvent.Defender.Id;
            if (!Verify.IsTrue(deadEntityId != Entity.InvalidId)) return;

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;

            if (deadEntityId == blackboardProps[PropertyEnum.AICustomEntityId1])
            {
                if (!Verify.IsNotNull(SymbiotePower1.PowerContext)) return;
                if (!Verify.IsNotNull(SymbiotePower1.PowerContext.Power)) return;

                Power symbiotePower = agent.GetPower(SymbiotePower1.PowerContext.Power.DataRef);
                symbiotePower?.EndPower(EndPowerFlags.Force | EndPowerFlags.ExplicitCancel);
                blackboardProps[PropertyEnum.AICustomEntityId1] = 0;
            }
            else if (deadEntityId == blackboardProps[PropertyEnum.AICustomEntityId2])
            {
                if (!Verify.IsNotNull(SymbiotePower2.PowerContext)) return;
                if (!Verify.IsNotNull(SymbiotePower2.PowerContext.Power)) return;

                Power symbiotePower = agent.GetPower(SymbiotePower2.PowerContext.Power.DataRef);
                symbiotePower?.EndPower(EndPowerFlags.Force | EndPowerFlags.ExplicitCancel);
                blackboardProps[PropertyEnum.AICustomEntityId2] = 0;
            }
            else if (deadEntityId == blackboardProps[PropertyEnum.AICustomEntityId3])
            {
                if (!Verify.IsNotNull(SymbiotePower3.PowerContext)) return;
                if (!Verify.IsNotNull(SymbiotePower3.PowerContext.Power)) return;

                Power symbiotePower = agent.GetPower(SymbiotePower3.PowerContext.Power.DataRef);
                symbiotePower?.EndPower(EndPowerFlags.Force | EndPowerFlags.ExplicitCancel);
                blackboardProps[PropertyEnum.AICustomEntityId3] = 0;
            }
        }
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
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public KeywordPrototype PrisonKeyword { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public KeywordPrototype CenterPlatformKeyword { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public KeywordPrototype RightPlatformKeyword { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public KeywordPrototype LeftPlatformKeyword { get; protected set; }

        //---

        private enum PlatformEnum
        {
            Left,
            Right,
            Center,
            None,
        }

        private enum State
        {
            Default,
            SpikeDance,
            SpikeDanceSingle,
        }

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, PsionicBlastLeft);
            InitPower(agent, PsionicBlastCenter);
            InitPower(agent, PsionicBlastRight);
            InitPower(agent, SpikeDanceVFXOnly);
            InitPower(agent, PrisonBeamPowerCenter);
            InitPower(agent, PrisonPowerCenter);
            InitPower(agent, SpikeDanceSingleVFXOnly);
            InitPower(agent, CallSentinelPower);
            InitPower(agent, CallSentinelPowerVFXOnly);
            InitPower(agent, PrisonBeamPowerLeft);
            InitPower(agent, PrisonBeamPowerRight);
            InitPower(agent, PrisonPowerLeft);
            InitPower(agent, PrisonPowerRight);

            if (!Verify.IsTrue(PlatformMarkerLeft != PrototypeId.Invalid)) return;
            if (!Verify.IsTrue(PlatformMarkerCenter != PrototypeId.Invalid)) return;
            if (!Verify.IsTrue(PlatformMarkerRight != PrototypeId.Invalid)) return;

            AIController ownerController = agent.AIController;
            if (!Verify.IsNotNull(ownerController)) return;

            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return;

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;

            Sphere volume = new(agent.RegionLocation.Position, ownerController.AggroRangeHostile);
            foreach (WorldEntity target in region.IterateEntitiesInVolume(volume, new(EntityRegionSPContextFlags.PrimaryPartition)))
            {
                if (target == null)
                    continue;

                if (target.PrototypeDataRef == PlatformMarkerLeft)
                    blackboardProps[PropertyEnum.AICustomEntityId1] = target.Id;
                else if (target.PrototypeDataRef == PlatformMarkerCenter)
                    blackboardProps[PropertyEnum.AICustomEntityId2] = target.Id;
                else if (target.PrototypeDataRef == PlatformMarkerRight)
                    blackboardProps[PropertyEnum.AICustomEntityId3] = target.Id;
            }
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

            HandleEnrage(ownerController);

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            EnrageState enrageState = (EnrageState)(int)blackboardProps[PropertyEnum.AIEnrageState];
            if (enrageState != EnrageState.Enraging)
            {
                long health = agent.Properties[PropertyEnum.Health];
                long maxHealth = agent.Properties[PropertyEnum.HealthMax];
                if (MathHelper.IsBelowOrEqual(health, maxHealth, SummonPowerThreshold1))
                {
                    if (blackboardProps[PropertyEnum.AICustomStateVal3] < 1)
                    {
                        AttemptCallSentinelPower(ownerController);
                        blackboardProps[PropertyEnum.AICustomStateVal3] = 1;                       
                    }
                    else if (MathHelper.IsBelowOrEqual(health, maxHealth, SummonPowerThreshold2))
                    {
                        if (blackboardProps[PropertyEnum.AICustomStateVal3] < 2)
                        {
                            AttemptCallSentinelPower(ownerController);
                            blackboardProps[PropertyEnum.AICustomStateVal3] = 2;
                        }
                    }
                }
          
                int state = blackboardProps[PropertyEnum.AICustomStateVal1];
                switch ((State)state)
                {
                    case State.Default:
                        WorldEntity target = ownerController.TargetEntity;
                        if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                            proceduralAI.PartialOverrideBehavior == null)
                        {
                            return;
                        }

                        GRandom random = game.Random;
                        Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
                        PopulatePowerPicker(ownerController, powerPicker);
                        HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true);                        
                        break;

                    case State.SpikeDance:
                        StaticBehaviorReturnType powerResult = HandleUsePowerContext(ownerController, proceduralAI, game.Random, currentTime, SpikeDanceVFXOnly.PowerContext, SpikeDanceVFXOnly);
                        if (powerResult != StaticBehaviorReturnType.Running)
                            blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.Default;
                        break;

                    case State.SpikeDanceSingle:
                        powerResult = HandleUsePowerContext(ownerController, proceduralAI, game.Random, currentTime, SpikeDanceSingleVFXOnly.PowerContext, SpikeDanceSingleVFXOnly);
                        if (powerResult != StaticBehaviorReturnType.Running)
                            blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.Default;
                        break;
                }
            }
        }

        private void AttemptCallSentinelPower(AIController ownerController)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;

            Game game = ownerController.Game;
            if (!Verify.IsNotNull(game)) return;

            EntityManager entityManager = game.EntityManager;

            Verify.IsTrue(ownerController.AttemptActivatePower(CallSentinelPowerVFXOnly, agent.Id, agent.RegionLocation.Position),
                $"Onslaught's CallSentinelPowerVFXOnly has failed.");

            ulong leftPlatformMarkerId = blackboardProps[PropertyEnum.AICustomEntityId1];
            if (!Verify.IsTrue(leftPlatformMarkerId != Entity.InvalidId, "Onslaught does not have a left platform marker."))
                return;

            WorldEntity leftMarker = entityManager.GetEntity<WorldEntity>(leftPlatformMarkerId);
            if (!Verify.IsNotNull(leftMarker, "Onslaught's left platform marker doesn't exist."))
                return;

            ulong centerPlatformMarkerId = blackboardProps[PropertyEnum.AICustomEntityId2];
            if (!Verify.IsTrue(centerPlatformMarkerId != Entity.InvalidId, "Onslaught does not have a center platform marker."))
                return;

            WorldEntity centerMarker = entityManager.GetEntity<WorldEntity>(centerPlatformMarkerId);
            if (!Verify.IsNotNull(centerMarker, "Onslaught's center platform marker doesn't exist."))
                return;

            ulong rightPlatformMarkerId = blackboardProps[PropertyEnum.AICustomEntityId3];
            if (!Verify.IsTrue(rightPlatformMarkerId != Entity.InvalidId, "Onslaught does not have a right platform marker."))
                return;

            WorldEntity rightMarker = entityManager.GetEntity<WorldEntity>(rightPlatformMarkerId);
            if (!Verify.IsNotNull(rightMarker, "Onslaught's right platform marker doesn't exist."))
                return;

            Verify.IsTrue(ownerController.AttemptActivatePower(CallSentinelPower, 0, leftMarker.RegionLocation.Position),
                "Onslaught's CallSentinelPower has failed for the left platform.");

            Verify.IsTrue(ownerController.AttemptActivatePower(CallSentinelPower, 0, centerMarker.RegionLocation.Position),
                "Onslaught's CallSentinelPower has failed for the center platform.");

            Verify.IsTrue(ownerController.AttemptActivatePower(CallSentinelPower, 0, rightMarker.RegionLocation.Position),
                "Onslaught's CallSentinelPower has failed for the right platform.");
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);

            ownerController.AddPowersToPicker(powerPicker, PsionicBlastLeft);
            ownerController.AddPowersToPicker(powerPicker, PsionicBlastCenter);
            ownerController.AddPowersToPicker(powerPicker, PsionicBlastRight);
            ownerController.AddPowersToPicker(powerPicker, SpikeDanceVFXOnly);
            ownerController.AddPowersToPicker(powerPicker, SpikeDanceSingleVFXOnly);

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            ulong entityId = blackboardProps[PropertyEnum.AICustomEntityId4];
            if (entityId != 0)
            {
                PlatformEnum randomPlatformIndex = (PlatformEnum)(int)blackboardProps[PropertyEnum.AICustomStateVal2];
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
                        Verify.IsTrue(false, $"Onslaught has an invalid platform enum stored in AICustomStateVal2: Index = {randomPlatformIndex}");
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

        public override bool OnPowerPicked(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            if (base.OnPowerPicked(ownerController, powerContext) == false)
                return false;

            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return false;
            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return false;

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            PropertyCollection blackboardProps = blackboard.PropertyCollection;

            WorldEntity targetAvatar;

            if (powerContext == PsionicBlastLeft)
            {
                targetAvatar = GetRandomAvatarNearPlatformMarker(ownerController, PlatformEnum.Left);
                if (targetAvatar == null)
                    return false;

                ownerController.SetTargetEntity(targetAvatar);
            }
            else if (powerContext == PsionicBlastCenter)
            {
                targetAvatar = GetRandomAvatarNearPlatformMarker(ownerController, PlatformEnum.Center);
                if (targetAvatar == null)
                    return false;

                ownerController.SetTargetEntity(targetAvatar);
            }
            else if (powerContext == PsionicBlastRight)
            {
                targetAvatar = GetRandomAvatarNearPlatformMarker(ownerController, PlatformEnum.Right);
                if (targetAvatar == null)
                    return false;

                ownerController.SetTargetEntity(targetAvatar);
            }
            else if (powerContext == SpikeDanceVFXOnly)
            {
                blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.SpikeDance;
                AIBroadcastBlackboardGameEvent evt = new(agent, blackboard);
                region.AIBroadcastBlackboardEvent.Invoke(evt);
            }
            else if (powerContext == SpikeDanceSingleVFXOnly)
            {
                blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.SpikeDanceSingle;
                AIBroadcastBlackboardGameEvent evt = new(agent, blackboard);
                region.AIBroadcastBlackboardEvent.Invoke(evt);
            }
            else if (powerContext == PrisonPowerCenter || powerContext == PrisonPowerLeft || powerContext == PrisonPowerRight)
            {
                PlatformEnum platform = PlatformEnum.None;
                if (powerContext == PrisonPowerLeft)
                    platform = PlatformEnum.Left;
                else if (powerContext == PrisonPowerCenter)
                    platform = PlatformEnum.Center;
                else if (powerContext == PrisonPowerRight)
                    platform = PlatformEnum.Right;

                targetAvatar = GetRandomAvatarNearPlatformMarker(ownerController, platform);
                if (targetAvatar == null)
                    return false;

                blackboardProps[PropertyEnum.AICustomStateVal2] = (int)platform;
                ownerController.SetTargetEntity(targetAvatar);
            }
            else if (powerContext == PrisonBeamPowerCenter || powerContext == PrisonBeamPowerLeft || powerContext == PrisonBeamPowerRight)
            {
                PlatformEnum platform = PlatformEnum.None;
                if (powerContext == PrisonBeamPowerLeft)
                    platform = PlatformEnum.Left;
                else if (powerContext == PrisonBeamPowerCenter)
                    platform = PlatformEnum.Center;
                else if (powerContext == PrisonBeamPowerRight)
                    platform = PlatformEnum.Right;

                ulong prisonId = blackboardProps[PropertyEnum.AICustomEntityId4];
                targetAvatar = GetRandomAvatarNearPlatformMarker(ownerController, platform, prisonId);
                if (targetAvatar == null)
                    return false;

                ownerController.SetTargetEntity(targetAvatar);
            }

            return true;
        }

        private WorldEntity GetRandomAvatarNearPlatformMarker(AIController ownerController, PlatformEnum platform, ulong ignoreId = 0)
        {
            WorldEntity agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return null;
            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return null;
            Game game = ownerController.Game;
            if (!Verify.IsNotNull(game)) return null;

            WorldEntity target = null;
            Picker<WorldEntity> targetPicker = new (game.Random);
            Sphere volume = new(agent.RegionLocation.Position, 3000.0f);
            foreach (WorldEntity targetAvatar in region.IterateAvatarsInVolume(volume))
            {
                if (targetAvatar != null && targetAvatar.IsDead == false && targetAvatar.Id != ignoreId)
                {
                    bool onPlatform;
                    switch (platform)
                    {
                        case PlatformEnum.Left:
                            onPlatform = targetAvatar.HasConditionWithKeyword(LeftPlatformKeyword);
                            break;
                        case PlatformEnum.Right:
                            onPlatform = targetAvatar.HasConditionWithKeyword(RightPlatformKeyword);
                            break;
                        case PlatformEnum.Center:
                            onPlatform = targetAvatar.HasConditionWithKeyword(CenterPlatformKeyword) == false;
                            break;
                        default:
                            Verify.IsTrue(false, $"invalid platform enum passed into GetRandomAvatarNearPlatformMarker: Enum = {platform}");
                            return null;
                    }

                    if (onPlatform)
                        targetPicker.Add(targetAvatar);
                }
            }

            if (targetPicker.Empty() == false)
                targetPicker.Pick(out target);

            return target;
        }

        public override void OnPowerEnded(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;

            if (powerContext == PrisonPowerCenter || powerContext == PrisonPowerLeft || powerContext == PrisonPowerRight)
            {
                WorldEntity target = ownerController.TargetEntity;
                if (!Verify.IsNotNull(target)) return;

                if (target.HasConditionWithKeyword(PrisonKeyword))
                    blackboardProps[PropertyEnum.AICustomEntityId4] = target.Id;
                else
                    blackboardProps.RemoveProperty(PropertyEnum.AICustomStateVal2);
            }
            else if (powerContext == PrisonBeamPowerCenter || powerContext == PrisonBeamPowerLeft || powerContext == PrisonBeamPowerRight)
            {
                ulong prisonId = blackboardProps[PropertyEnum.AICustomEntityId4];
                WorldEntity prisoner = ownerController.Game.EntityManager.GetEntity<WorldEntity>(prisonId);
                if (prisoner == null || prisoner.IsInWorld == false || prisoner.HasConditionWithKeyword(PrisonKeyword) == false)
                {
                    blackboardProps.RemoveProperty(PropertyEnum.AICustomEntityId4);
                    blackboardProps.RemoveProperty(PropertyEnum.AICustomStateVal2);
                }
            }
        }
    }

    public class ProceduralProfileNullifierPrototype : ProceduralProfileNoMoveNoSensePrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public KeywordPrototype ShieldEngineerKeyword { get; protected set; }
        public ProceduralUsePowerContextPrototype BeamPower { get; protected set; }
        public PrototypeId NullifierAntiShield { get; protected set; }

        //---

        private enum State
        {
            Default,
            Charging,
            Ready,
            Beam,
        }

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, BeamPower);

            AIController ownerController = agent.AIController;
            if (!Verify.IsNotNull(ownerController)) return;
            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return;

            ownerController.RegisterForAIBroadcastBlackboardEvents(region, true);
            ownerController.RegisterForEntityDeadEvents(region, true);
            ownerController.RegisterForPlayerInteractEvents(region, true);
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

            if (ownerController.TargetEntity == null)
                SelectEntity.RegisterSelectedEntity(ownerController, agent, SelectEntityType.SelectTarget);

           PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            int state = blackboardProps[PropertyEnum.AICustomStateVal1];
            switch ((State)state)
            {
                case State.Charging:
                    long health = agent.Properties[PropertyEnum.Health];
                    long maxHealth = agent.Properties[PropertyEnum.HealthMaxOther];

                    if (health == maxHealth)
                    {
                        blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.Ready;
                        agent.Properties[PropertyEnum.Interactable] = true;
                        SetNullifierEntityState(ownerController, true);
                    }

                    break;

                case State.Beam:
                    StaticBehaviorReturnType powerResult = HandleUsePowerContext(ownerController, proceduralAI, game.Random, currentTime, BeamPower.PowerContext, BeamPower);
                    if (powerResult != StaticBehaviorReturnType.Running)
                    {
                        if (powerResult == StaticBehaviorReturnType.Failed)
                            Verify.IsTrue(false, $"The nullifier failed activating his BeamPower {agent}");

                        agent.Properties[PropertyEnum.Health] = 1;
                        blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.Charging; 
                    }

                    break;
            }

        }

        private static void SetNullifierEntityState(AIController ownerController, bool enabled)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

            TransitionGlobalsPrototype transitionGlobalsProto = GameDatabase.TransitionGlobalsPrototype;
            Verify.IsNotNull(transitionGlobalsProto);

            if (enabled)
            {
                Verify.IsTrue(transitionGlobalsProto.EnabledState != PrototypeId.Invalid);
                agent.Properties[PropertyEnum.EntityState] = transitionGlobalsProto.EnabledState;
            }
            else
            {
                Verify.IsTrue(transitionGlobalsProto.DisabledState != PrototypeId.Invalid);
                agent.Properties[PropertyEnum.EntityState] = transitionGlobalsProto.DisabledState;
            }
        }

        public override void OnAIBroadcastBlackboardEvent(AIController ownerController, in AIBroadcastBlackboardGameEvent broadcastEvent)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            WorldEntity broadcaster = broadcastEvent.Broadcaster;
            if (!Verify.IsNotNull(broadcaster)) return;
            BehaviorBlackboard broadcasterBlackboard = broadcastEvent.Blackboard;
            if (!Verify.IsNotNull(broadcasterBlackboard)) return;

            ulong engineerId = broadcaster.Properties[PropertyEnum.AICustomEntityId1];
            ulong nullifierId = broadcasterBlackboard.PropertyCollection[PropertyEnum.AICustomEntityId1];

            if (engineerId == Entity.InvalidId && nullifierId == agent.Id && broadcaster.HasKeyword(ShieldEngineerKeyword))
            {
                if (!Verify.IsTrue(broadcaster.Id != Entity.InvalidId)) return;

                PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
                blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.Charging;
                blackboardProps[PropertyEnum.AICustomEntityId1] = broadcaster.Id;
            }
        }

        public override void OnEntityDeadEvent(AIController ownerController, in EntityDeadGameEvent deadEvent)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

            if (!Verify.IsNotNull(deadEvent.Defender)) return;

            ulong deadEntityId = deadEvent.Defender.Id;
            if (!Verify.IsTrue(deadEntityId != Entity.InvalidId)) return;

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;

            if (deadEntityId == blackboardProps[PropertyEnum.AICustomEntityId1])
            {
                blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.Default;
                blackboardProps[PropertyEnum.AICustomEntityId1] = Entity.InvalidId;

                agent.Properties[PropertyEnum.Interactable] = false;
                agent.Properties[PropertyEnum.Enabled] = false;

                SetNullifierEntityState(ownerController, false);
            } 
            else if (deadEvent.Defender.PrototypeDataRef == NullifierAntiShield)
            {
                if (!Verify.IsNotNull(BeamPower)) return;
                if (!Verify.IsNotNull(BeamPower.PowerContext)) return;
                if (!Verify.IsNotNull(BeamPower.PowerContext.Power)) return;

                agent.ConditionCollection.RemoveConditionsOfPower(BeamPower.PowerContext.Power.DataRef);

                SetNullifierEntityState(ownerController, false);

                ProceduralAI proceduralAI = ownerController.Brain;
                if (!Verify.IsNotNull(proceduralAI)) return;

                proceduralAI.SwitchProceduralState(null, null, StaticBehaviorReturnType.Interrupted);

                agent.Properties[PropertyEnum.Interactable] = false;
                agent.Properties[PropertyEnum.Enabled] = false;
                agent.Properties[PropertyEnum.Untargetable] = true;
            }
        }

        public override void OnPlayerInteractEvent(AIController ownerController, in PlayerInteractGameEvent interactEvent)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

            if (interactEvent.InteractableObject is not Agent interactableAgent)
                return;

            if (interactableAgent == agent)
            {
                BehaviorBlackboard blackboard = ownerController.Blackboard;
                PropertyCollection blackboardProps = blackboard.PropertyCollection;

                blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.Beam;
                agent.Properties[PropertyEnum.Interactable] = false;

                SetNullifierEntityState(ownerController, false);

                Region region = agent.Region;
                if (!Verify.IsNotNull(region)) return;

                AIBroadcastBlackboardGameEvent evt = new(agent, blackboard);
                region.AIBroadcastBlackboardEvent.Invoke(evt);
            }
        }
    }

    public class ProceduralProfileShieldEngineerPrototype : ProceduralProfileMissionAllyPrototype
    {
        [PrototypeField(PrototypeFieldType.VectorPrototypeRefPtr)]
        public AgentPrototype[] PsychicNullifierTargets { get; protected set; }
        public ProceduralUsePowerContextPrototype ChargeNullifierPower { get; protected set; }
        public float NullifierSearchRadius { get; protected set; }
        public PrototypeId NullifierAntiShield { get; protected set; }

        //---

        private enum State
        {
            Default,
            MoveToNullifier,
            Charging,
            Ready,
        }

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, ChargeNullifierPower);

            AIController ownerController = agent.AIController;
            if (!Verify.IsNotNull(ownerController)) return;

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            long currentTime = (long)ownerController.Game.CurrentTime.TotalMilliseconds;
            blackboardProps[PropertyEnum.AICustomTimeVal2] = currentTime + 3000;

            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return;

            ownerController.RegisterForAIBroadcastBlackboardEvents(region, true);
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

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            State state = (State)(int)blackboardProps[PropertyEnum.AICustomStateVal1];

            long lastTime = blackboardProps[PropertyEnum.AICustomTimeVal2];
            if (state == State.Default && lastTime != 0 && currentTime > lastTime)
            {
                blackboardProps[PropertyEnum.AICustomTimeVal2] = currentTime + 3000;
                FindNullifierTarget(ownerController);
            }

            state = (State)(int)blackboardProps[PropertyEnum.AICustomStateVal1];
            switch (state)
            {
                case State.Default:
                    long lastSearchTime = blackboardProps[PropertyEnum.AICustomTimeVal1];

                    WorldEntity avatarAlly = ownerController.AssistedEntity;

                    if (avatarAlly == null || (lastSearchTime != 0 && currentTime > lastSearchTime) || (avatarAlly != null && avatarAlly.IsDead))
                        FindBestAvatarAllyToFollow(ownerController);
                    else if (avatarAlly != null)
                        HandleMovementContext(proceduralAI, ownerController, agent.Locomotor, MoveToAvatarAlly, true, out _);

                    break;
                    
                case State.MoveToNullifier:
                    WorldEntity nullifierTarget = ownerController.TargetEntity;
                    if (nullifierTarget != null)
                    {
                        if (PsychicNullifierTargets.HasValue())
                        {
                            AgentPrototype[] psychicNullifierTargets = PsychicNullifierTargets;
                            Verify.IsTrue(psychicNullifierTargets.Contains(nullifierTarget.Prototype),
                                $"The shield engineer's target {nullifierTarget} is not a nullifier! {agent}");                                    

                            StaticBehaviorReturnType contextResult = HandleContext(proceduralAI, ownerController, MoveToTarget);
                            Verify.IsTrue(contextResult != StaticBehaviorReturnType.Failed && contextResult != StaticBehaviorReturnType.Interrupted,
                                $"The shield engineer {agent} cannot move to a nullifier {nullifierTarget} to charge!");

                            if (contextResult == StaticBehaviorReturnType.Completed)
                                blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.Charging;
                        }
                    }
                    else
                    {
                        Verify.IsTrue(false, $"The shield engineer cannot find a nullifier to move to because its Target is NULL! {agent}");
                    }

                    break;
                    
                case State.Charging:
                    HandleUsePowerContext(ownerController, proceduralAI, game.Random, currentTime, ChargeNullifierPower.PowerContext, ChargeNullifierPower);
                    break;
            }
        }

        private void FindNullifierTarget(AIController ownerController)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

            if (PsychicNullifierTargets.HasValue())
            {
                Region region = agent.Region;
                if (!Verify.IsNotNull(region)) return;
                Game game = ownerController.Game;
                if (!Verify.IsNotNull(game)) return;

                BehaviorBlackboard blackboard = ownerController.Blackboard;
                PropertyCollection blackboardProps = blackboard.PropertyCollection;

                Sphere volume = new(agent.RegionLocation.Position, NullifierSearchRadius);
                foreach (WorldEntity target in region.IterateEntitiesInVolume(volume, new(EntityRegionSPContextFlags.PrimaryPartition)))
                {
                    if (target is Agent nullifier && PsychicNullifierTargets.Contains(nullifier.Prototype))
                    {
                        AIController nullifierController = nullifier.AIController;
                        if (!Verify.IsNotNull(nullifierController))
                            continue;

                        BehaviorBlackboard nullifierBlackboard = nullifierController.Blackboard;
                        if (nullifierBlackboard.PropertyCollection[PropertyEnum.AICustomEntityId1] == Entity.InvalidId)
                        {
                            blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.MoveToNullifier;
                            blackboardProps[PropertyEnum.AICustomEntityId1] = nullifier.Id;

                            ProceduralAI proceduralAI = ownerController.Brain;
                            if (!Verify.IsNotNull(proceduralAI)) return;

                            proceduralAI.SwitchProceduralState(null, null, StaticBehaviorReturnType.Interrupted);
                            ownerController.SetTargetEntity(nullifier);

                            AIBroadcastBlackboardGameEvent evt = new(agent, blackboard);
                            region.AIBroadcastBlackboardEvent.Invoke(evt);
                        }
                    }
                }
            }
        }

        public override void OnAIBroadcastBlackboardEvent(AIController ownerController, in AIBroadcastBlackboardGameEvent broadcastEvent)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            WorldEntity broadcaster = broadcastEvent.Broadcaster;
            if (!Verify.IsNotNull(broadcaster)) return;

            if (broadcaster.PrototypeDataRef == NullifierAntiShield)
            {
                PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
                blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.Ready;
            }
        }
    }

    public class ProcProfileNullifierAntiShieldPrototype : ProceduralProfileWithEnragePrototype
    {
        [PrototypeField(PrototypeFieldType.VectorPrototypeRefPtr)]
        public AgentPrototype[] Nullifiers { get; protected set; }
        public PrototypeId ShieldDamagePower { get; protected set; }
        public PrototypeId ShieldEngineerSpawner { get; protected set; }
        public float SpawnerSearchRadius { get; protected set; }

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, ShieldDamagePower);

            AIController ownerController = agent.AIController;
            if (!Verify.IsNotNull(ownerController)) return;
            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return;

            ownerController.RegisterForAIBroadcastBlackboardEvents(region, true);
        }

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            Game game = agent.Game;
            if (!Verify.IsNotNull(game)) return;

            HandleEnrage(ownerController);

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            EnrageState enrageState = (EnrageState)(int)blackboardProps[PropertyEnum.AIEnrageState];
            int stateVal = blackboardProps[PropertyEnum.AICustomStateVal1];
            if (enrageState == EnrageState.Enraged && stateVal == 0)
            {
                Region region = agent.Region;
                if (!Verify.IsNotNull(region)) return;

                using var spawnersToDestroyHandle = ListPool<WorldEntity>.Instance.Get(out List<WorldEntity> spawnersToDestroy);

                Sphere volume = new(agent.RegionLocation.Position, SpawnerSearchRadius);
                foreach (WorldEntity spawnerTarget in region.IterateEntitiesInVolume(volume, new(EntityRegionSPContextFlags.PrimaryPartition)))
                {
                    if (spawnerTarget != null && spawnerTarget.PrototypeDataRef == ShieldEngineerSpawner)
                        spawnersToDestroy.Add(spawnerTarget);
                }

                foreach (WorldEntity spawner in spawnersToDestroy)
                {
                    if (!Verify.IsNotNull(spawner))
                        continue;

                    if (spawner.IsDestroyed == false && spawner.TestStatus(EntityStatus.PendingDestroy) == false)
                        spawner.Destroy();
                }

                blackboardProps[PropertyEnum.AICustomStateVal1] = 1;
            }
        }

        public override void OnAIBroadcastBlackboardEvent(AIController ownerController, in AIBroadcastBlackboardGameEvent broadcastEvent)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            WorldEntity broadcaster = broadcastEvent.Broadcaster;
            if (!Verify.IsNotNull(broadcaster)) return;
            BehaviorBlackboard broadcasterBlackboard = broadcastEvent.Blackboard;
            if (!Verify.IsNotNull(broadcasterBlackboard)) return;

            if (Nullifiers.HasValue() && Nullifiers.Contains(broadcaster.Prototype))
            {
                Verify.IsTrue(ownerController.AttemptActivatePower(ShieldDamagePower, agent.Id, agent.RegionLocation.Position),
                    "NullifierAntiShield has failed.");
            }
        }

        public override void OnOwnerKilled(AIController ownerController)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return;

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            AIBroadcastBlackboardGameEvent evt = new(agent, blackboard);
            region.AIBroadcastBlackboardEvent.Invoke(evt);
        }
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

        //---

        private enum State
        {
            Default,
            SummonHydra,
            Invulnerable,
        }

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, SummonHydraPower);
            InitPower(agent, InvulnerablePower);
            InitPower(agent, TeleportPower);

            Game game = agent.Game;
            if (!Verify.IsNotNull(game)) return;

            AIController ownerController = agent.AIController;
            if (!Verify.IsNotNull(ownerController)) return;

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;

            long currentTime = (long)game.CurrentTime.TotalMilliseconds;
            long summonCooldown = currentTime + game.Random.Next(SummonHydraMinCooldownMS, SummonHydraMaxCooldownMS);
            blackboardProps[PropertyEnum.AICustomTimeVal1] = summonCooldown;
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

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                proceduralAI.PartialOverrideBehavior == null)
            {
                return;
            }

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            int state = blackboardProps[PropertyEnum.AICustomStateVal1];

            switch ((State)state)
            {
                case State.Default:
                    if (proceduralAI.GetState(0) == Flank.Instance)
                    {
                        HandleProceduralFlank(proceduralAI, ownerController, agent.Locomotor, currentTime, FlankTarget, true);
                        return;
                    }

                    if (proceduralAI.GetState(0) != UsePower.Instance && currentTime > blackboardProps[PropertyEnum.AICustomTimeVal1])
                    {
                        blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.SummonHydra;
                        return;
                    }

                    GRandom random = game.Random;
                    Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
                    PopulatePowerPicker(ownerController, powerPicker);
                    if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                        return;

                    DefaultRangedFlankerMovement(proceduralAI, ownerController, agent, target, currentTime, MoveToTarget, FlankTarget);

                    break;

                case State.SummonHydra:
                    int numPlayers = Power.ComputeNearbyPlayers(agent.Region, agent.RegionLocation.Position);
                    int numSummons = 0;
                    for (int i = 0; i < numPlayers; i++)
                    {
                        if (ownerController.AttemptActivatePower(SummonHydraPower, agent.Id, agent.RegionLocation.Position))
                            numSummons++;
                    }

                    if (numSummons > 0)
                    {
                        blackboardProps[PropertyEnum.AICustomStateVal2] = numSummons;

                        Region region = agent.Region;
                        if (!Verify.IsNotNull(region)) return;

                        ownerController.RegisterForEntityDeadEvents(region, true);
                        ownerController.AttemptActivatePower(InvulnerablePower, agent.Id, agent.RegionLocation.Position);

                        blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.Invulnerable;
                    }

                    break;

                case State.Invulnerable:
                    random = game.Random;
                    powerPicker = new(random);
                    PopulatePowerPicker(ownerController, powerPicker);
                    HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true);

                    break;
            }
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            base.PopulatePowerPicker(ownerController, powerPicker);

            State state = (State)(int)ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            if (state == State.Default)
                ownerController.AddPowersToPicker(powerPicker, TeleportPower);
        }

        public override void OnEntityDeadEvent(AIController ownerController, in EntityDeadGameEvent deadEvent)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            WorldEntity deadEntity = deadEvent.Defender;
            if (!Verify.IsNotNull(deadEntity)) return;

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;

            if (deadEntity.Properties[PropertyEnum.PowerUserOverrideID] == agent.Id)
            {
                blackboardProps.AdjustProperty(-1, PropertyEnum.AICustomStateVal2);
                if (blackboardProps[PropertyEnum.AICustomStateVal2] <= 0)
                {
                    ownerController.AttemptActivatePower(InvulnerablePower, agent.Id, agent.RegionLocation.Position);
                    ResetInvulnerableState(ownerController, agent);
                }
            }
        }

        private void ResetInvulnerableState(AIController ownerController, Agent agent)
        {
            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;

            blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.Default;
            agent.Properties[PropertyEnum.PowerToggleOn, InvulnerablePower] = false;
            blackboardProps[PropertyEnum.AICustomStateVal2] = 0;

            Game game = ownerController.Game;
            if (!Verify.IsNotNull(game)) return;

            GRandom random = game.Random;
            long currentTime = (long)game.CurrentTime.TotalMilliseconds;
            long summonCooldown = currentTime + random.Next(SummonHydraMinCooldownMS, SummonHydraMaxCooldownMS);
            blackboardProps[PropertyEnum.AICustomTimeVal1] = summonCooldown;

            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return;

            ownerController.RegisterForEntityDeadEvents(region, false);
        }
    }

    public class ProceduralProfileStarktechSentinelPrototype : ProceduralProfileWithEnragePrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype SummonSentinels { get; protected set; }
        public float SummonPowerThreshold1 { get; protected set; }
        public float SummonPowerThreshold2 { get; protected set; }

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, SummonSentinels);
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

            HandleEnrage(ownerController);

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            EnrageState enrageState = (EnrageState)(int)blackboardProps[PropertyEnum.AIEnrageState];
            if (enrageState != EnrageState.Enraging)
            {
                if (proceduralAI.GetState(0) != UsePower.Instance)
                {
                    long health = agent.Properties[PropertyEnum.Health];
                    long maxHealth = agent.Properties[PropertyEnum.HealthMax];

                    if (MathHelper.IsBelowOrEqual(health, maxHealth, SummonPowerThreshold1))
                    {
                        if (blackboardProps[PropertyEnum.AICustomStateVal1] < 1)
                        {
                            StaticBehaviorReturnType powerResult = HandleUsePowerContext(ownerController, proceduralAI, game.Random, currentTime, SummonSentinels.PowerContext, SummonSentinels);
                            if (powerResult == StaticBehaviorReturnType.Running || powerResult == StaticBehaviorReturnType.Completed)
                            {
                                blackboardProps[PropertyEnum.AICustomStateVal1] = 1;
                                if (powerResult == StaticBehaviorReturnType.Running)
                                    return;
                            }
                        }
                        else if (MathHelper.IsBelowOrEqual(health, maxHealth, SummonPowerThreshold2))
                        {
                            if (blackboardProps[PropertyEnum.AICustomStateVal2] < 1)
                            {
                                StaticBehaviorReturnType powerResult = HandleUsePowerContext(ownerController, proceduralAI, game.Random, currentTime, SummonSentinels.PowerContext, SummonSentinels);
                                if (powerResult == StaticBehaviorReturnType.Running || powerResult == StaticBehaviorReturnType.Completed)
                                {
                                    blackboardProps[PropertyEnum.AICustomStateVal2] = 1;
                                    if (powerResult == StaticBehaviorReturnType.Running)
                                        return;
                                }
                            }
                        }
                    }
                }

                WorldEntity target = ownerController.TargetEntity;
                if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                    proceduralAI.PartialOverrideBehavior == null)
                {
                    return;
                }

                GRandom random = game.Random;
                Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
                PopulatePowerPicker(ownerController, powerPicker);
                if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                    return;

                DefaultRangedMovement(proceduralAI, ownerController, agent, target, MoveToTarget, OrbitTarget);
            }
        }
    }

    public class ProceduralProfileKingpinPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype SummonElektra { get; protected set; }
        public ProceduralUsePowerContextPrototype SummonBullseye { get; protected set; }
        public float SummonElektraThreshold { get; protected set; }
        public float SummonBullseyeThreshold { get; protected set; }

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, SummonElektra);
            InitPower(agent, SummonBullseye);
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

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            PrototypeId activePowerRef = blackboardProps[PropertyEnum.AILastPowerActivated];

            if (!Verify.IsNotNull(SummonElektra)) return;
            if (!Verify.IsNotNull(SummonElektra.PowerContext)) return;
            if (!Verify.IsNotNull(SummonElektra.PowerContext.Power)) return;

            if (!Verify.IsNotNull(SummonBullseye)) return;
            if (!Verify.IsNotNull(SummonBullseye.PowerContext)) return;
            if (!Verify.IsNotNull(SummonBullseye.PowerContext.Power)) return;

            if (proceduralAI.GetState(0) != UsePower.Instance ||
                activePowerRef == SummonElektra.PowerContext.Power.DataRef ||
                activePowerRef == SummonBullseye.PowerContext.Power.DataRef)
            {
                long health = agent.Properties[PropertyEnum.Health];
                long maxHealth = agent.Properties[PropertyEnum.HealthMax];

                if (MathHelper.IsBelowOrEqual(health, maxHealth, SummonElektraThreshold))
                {
                    if (blackboardProps[PropertyEnum.AICustomStateVal1] < 1)
                    {
                        StaticBehaviorReturnType powerResult = HandleUsePowerContext(ownerController, proceduralAI, game.Random, currentTime, SummonElektra.PowerContext, SummonElektra);
                        if (powerResult == StaticBehaviorReturnType.Running)
                            return;
                        else if (powerResult == StaticBehaviorReturnType.Failed || powerResult == StaticBehaviorReturnType.Interrupted)
                            Verify.IsTrue(false, $"Kingpin failed to play his SummonElektra power! Reason: {powerResult}  Kingpin: {agent}");

                        blackboardProps[PropertyEnum.AICustomStateVal1] = 1;
                    }
                    else if (MathHelper.IsBelowOrEqual(health, maxHealth, SummonBullseyeThreshold) &&
                        blackboardProps[PropertyEnum.AICustomStateVal1] < 2)
                    {
                        StaticBehaviorReturnType powerResult = HandleUsePowerContext(ownerController, proceduralAI, game.Random, currentTime, SummonBullseye.PowerContext, SummonBullseye);
                        if (powerResult == StaticBehaviorReturnType.Running)
                            return;
                        else if (powerResult == StaticBehaviorReturnType.Failed || powerResult == StaticBehaviorReturnType.Interrupted)
                            Verify.IsTrue(false, $"Kingpin failed to play his SummonBullseye power! Reason: {powerResult}  Kingpin: {agent}");
                        
                        blackboardProps[PropertyEnum.AICustomStateVal1] = 2;
                    }
                }
            }

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                proceduralAI.PartialOverrideBehavior == null)
            {
                return;
            }

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                return;

            DefaultMeleeMovement(proceduralAI, ownerController, agent.Locomotor, target, MoveToTarget, OrbitTarget);
        }
    }

    public class ProceduralProfileUltronEMPPrototype : ProceduralProfileNoMoveNoSensePrototype
    {
        public ProceduralUsePowerContextPrototype EMPPower { get; protected set; }

        //---

        private enum State
        {
            Default,
            EMPPower,
            Activated,
        }

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, EMPPower);

            AIController ownerController = agent.AIController;
            if (!Verify.IsNotNull(ownerController)) return;
            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return;

            ownerController.RegisterForPlayerInteractEvents(region, true);
            agent.Properties[PropertyEnum.Interactable] = true;

            TransitionGlobalsPrototype transitionGlobalsProto = GameDatabase.TransitionGlobalsPrototype;
            if (Verify.IsNotNull(transitionGlobalsProto) && Verify.IsTrue(transitionGlobalsProto.EnabledState != PrototypeId.Invalid))
                agent.Properties[PropertyEnum.EntityState] = transitionGlobalsProto.EnabledState;
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
            
            if (ownerController.TargetEntity == null)
                SelectEntity.RegisterSelectedEntity(ownerController, agent, SelectEntityType.SelectTarget);

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            State state = (State)(int)blackboardProps[PropertyEnum.AICustomStateVal1];
            if (state == State.EMPPower)
            {
                StaticBehaviorReturnType powerResult = HandleUsePowerContext(ownerController, proceduralAI, game.Random, currentTime, EMPPower.PowerContext, EMPPower);
                if (powerResult != StaticBehaviorReturnType.Running)
                {
                    if (powerResult == StaticBehaviorReturnType.Failed)
                        Verify.IsTrue(false, $"The EMP failed activating his EMPPower {agent}");

                    blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.Activated;
                }
            }
        }

        public override void OnPlayerInteractEvent(AIController ownerController, in PlayerInteractGameEvent interactEvent)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

            if (interactEvent.InteractableObject is not Agent interactableAgent || interactableAgent != agent)
                return;

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.EMPPower;
            agent.Properties[PropertyEnum.Interactable] = false;
            
            TransitionGlobalsPrototype transitionGlobalsProto = GameDatabase.TransitionGlobalsPrototype;
            if (Verify.IsNotNull(transitionGlobalsProto) && Verify.IsTrue(transitionGlobalsProto.DisabledState != PrototypeId.Invalid))
                agent.Properties[PropertyEnum.EntityState] = transitionGlobalsProto.DisabledState;
        }
    }

    public class ProcProfileQuicksilverTeamUpPrototype : ProceduralProfileTeamUpPrototype
    {
        public ProceduralUsePowerContextPrototype SpecialMovementPower { get; protected set; }

        //---

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, SpecialMovementPower);
        }
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

        //---

        private enum State
        {
            Default,
            OpenRocketCrate,
            OpenMinigunCrate,
            UseRocket,
            UseMinigun,
            DiscardWeapon,
        }

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, OpenRocketCratePower);
            InitPower(agent, OpenMinigunCratePower);
            InitPower(agent, UseRocketPower);
            InitPower(agent, UseMinigunPower);
            InitPower(agent, CommandTurretPower);
            InitPower(agent, DiscardWeaponPower);
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

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            PropertyCollection blackboardProps = blackboard.PropertyCollection;

            bool hasRocketCrates = blackboardProps.HasProperty(PropertyEnum.AICustomStateVal3) == false;
            bool hasMinigunCrates = blackboardProps.HasProperty(PropertyEnum.AICustomStateVal4) == false;
            bool switchToCrateState =
                proceduralAI.GetState(0) != UsePower.Instance &&
                blackboardProps.HasProperty(PropertyEnum.AIAggroTime) &&
                blackboardProps[PropertyEnum.AICustomStateVal1] == (int)State.Default &&
                IsProceduralPowerContextOnCooldown(blackboard, OpenMinigunCratePower, currentTime) == false &&
                IsProceduralPowerContextOnCooldown(blackboard, OpenRocketCratePower, currentTime) == false &&
                (hasRocketCrates || hasMinigunCrates);

            if (switchToCrateState)
            {
                Picker<int> picker = new(game.Random);

                if (hasMinigunCrates)
                    picker.Add((int)State.OpenMinigunCrate);

                if (hasRocketCrates)
                    picker.Add((int)State.OpenRocketCrate);

                if (!Verify.IsTrue(picker.Pick(out int openCrateState))) return;

                blackboardProps[PropertyEnum.AICustomStateVal1] = openCrateState;
                blackboardProps[PropertyEnum.AIAlwaysAggroed] = true;
                ownerController.SetTargetEntity(null);
                blackboardProps.RemoveProperty(PropertyEnum.AIProceduralNextAttackTime);
            }

            State state = (State)(int)blackboardProps[PropertyEnum.AICustomStateVal1];
            if (switchToCrateState || state == State.OpenRocketCrate || state == State.OpenMinigunCrate)
            {
                GRandom random = game.Random;
                Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
                PopulatePowerPicker(ownerController, powerPicker);
                StaticBehaviorReturnType powerResult = HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true);

                if (switchToCrateState)
                    blackboardProps[PropertyEnum.AIAlwaysAggroed] = false;

                WorldEntity target = ownerController.TargetEntity;
                if (target == null || target.IsDead)
                {
                    blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.Default;
                    if (target == null && powerResult == StaticBehaviorReturnType.Failed)
                    {
                        if (state == State.OpenRocketCrate)
                            blackboardProps[PropertyEnum.AICustomStateVal3] = 1;
                        else if (state == State.OpenMinigunCrate)
                            blackboardProps[PropertyEnum.AICustomStateVal4] = 1;
                    }
                }
                else
                {
                    HandleMovementContext(proceduralAI, ownerController, agent.Locomotor, MoveToCrate, false, out _);
                    return;
                }
            }

            base.Think(ownerController);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            State state = (State)(int)ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            switch (state)
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

        public override void OnPowerStarted(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            if (powerContext == OpenRocketCratePower || powerContext == OpenMinigunCratePower)
            {
                WorldEntity target = ownerController.TargetEntity;
                if (!Verify.IsNotNull(target)) return;
                if (!Verify.IsTrue(target != ownerController.Owner)) return;
                if (!Verify.IsTrue(target.CanBePlayerOwned() == false)) return;

                target.SetState(CrateUsedState);
            }
        }

        public override void OnPowerEnded(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            BehaviorBlackboard blackboard = ownerController.Blackboard;
            PropertyCollection blackboardProps = blackboard.PropertyCollection;

            if (powerContext == OpenRocketCratePower)
            {
                blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.UseRocket;
                blackboardProps[PropertyEnum.AICustomStateVal2] = CratePowerUseCount;
            }
            else if (powerContext == OpenMinigunCratePower)
            {
                blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.UseMinigun;
                blackboardProps[PropertyEnum.AICustomStateVal2] = CratePowerUseCount;
            }
            else if (powerContext == UseRocketPower || powerContext == UseMinigunPower)
            {
                if (!Verify.IsTrue(blackboardProps[PropertyEnum.AICustomStateVal2] > 0)) return;

                blackboardProps.AdjustProperty(-1, PropertyEnum.AICustomStateVal2);
                int count = blackboardProps[PropertyEnum.AICustomStateVal2];
                if (count == 0)
                    blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.DiscardWeapon;
            }
            else if (powerContext == DiscardWeaponPower)
            {
                Agent agent = ownerController.Owner;
                if (!Verify.IsNotNull(agent)) return;

                ConditionCollection conditions = agent.ConditionCollection;
                if (!Verify.IsNotNull(conditions)) return;

                conditions.RemoveConditionsOfPower(OpenRocketCratePower.PowerContext.Power.DataRef);
                conditions.RemoveConditionsOfPower(OpenMinigunCratePower.PowerContext.Power.DataRef);

                blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = (int)State.Default;
            }
            else if (powerContext == CommandTurretPower)
            {
                Agent agent = ownerController.Owner;
                if (!Verify.IsNotNull(agent)) return;

                Region region = agent.Region;
                if (!Verify.IsNotNull(region)) return;

                AIBroadcastBlackboardGameEvent evt = new(agent, blackboard);
                region.AIBroadcastBlackboardEvent.Invoke(evt);
            }
        }
    }

    public class ProceduralProfileNickFuryTurretPrototype : ProceduralProfileRotatingTurretWithTargetPrototype
    {
        public ProceduralUsePowerContextPrototype SpecialCommandPower { get; protected set; }
        public PrototypeId SkrullNickFuryRef { get; protected set; }

        //---

        private enum State
        {
            Default,
            Ready,
            SpecialPower,
        }

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, SpecialCommandPower);

            AIController ownerController = agent.AIController;
            if (!Verify.IsNotNull(ownerController)) return;
            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return;

            ownerController.RegisterForAIBroadcastBlackboardEvents(region, true);
        }

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            Game game = agent.Game;
            if (!Verify.IsNotNull(game)) return;

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            if (proceduralAI.GetState(0) != UsePower.Instance &&
                blackboardProps[PropertyEnum.AICustomStateVal1] == (int)State.Ready)
            {
                blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.SpecialPower;
            }

            base.Think(ownerController);
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            State state = (State)(int)ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1];
            if (state == State.SpecialPower)
                ownerController.AddPowersToPicker(powerPicker, SpecialCommandPower);
            else
                base.PopulatePowerPicker(ownerController, powerPicker);
        }

        public override void OnPowerEnded(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            if (powerContext == SpecialCommandPower)
                ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = (int)State.Default;
        }

        public override void OnAIBroadcastBlackboardEvent(AIController ownerController, in AIBroadcastBlackboardGameEvent broadcastEvent)
        {
            WorldEntity broadcaster = broadcastEvent.Broadcaster;
            if (!Verify.IsNotNull(broadcaster)) return;

            if (broadcaster.PrototypeDataRef == SkrullNickFuryRef)
            {
                Agent broadcasterAgent = broadcaster as Agent;
                if (!Verify.IsNotNull(broadcasterAgent)) return;
                AIController broadcasterController = broadcasterAgent.AIController;
                if (!Verify.IsNotNull(broadcasterController)) return;

                // Fix to prevent bad state in situations when RNG rolls lower cooldown values and things overlap poorly
                if (!Verify.IsTrue(ownerController.Brain?.GetState(0) != UsePower.Instance))
                    return;

                ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = (int)State.Ready;
                ownerController.SetTargetEntity(broadcasterController.TargetEntity);
            }
        }
    }

    public class ProceduralProfileStrangeCauldronPrototype : ProceduralProfileNoMoveNoSensePrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public EntityPrototype KaeciliusPrototype { get; protected set; }

        //---

        private enum State
        {
            Default,
            Enabled,
            Interactable,
            DestroyAgent,
        }

        public override void Init(Agent agent)
        {
            base.Init(agent);

            AIController ownerController = agent.AIController;
            if (!Verify.IsNotNull(ownerController)) return;
            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return;

            ownerController.RegisterForAIBroadcastBlackboardEvents(region, true);
            ownerController.RegisterForPlayerInteractEvents(region, true);
        }

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

            if (ownerController.TargetEntity == null)
                SelectEntity.RegisterSelectedEntity(ownerController, agent, SelectEntityType.SelectTarget);

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
            
            State state = (State)(int)blackboardProps[PropertyEnum.AICustomStateVal1];
            switch (state)
            {
                case State.Enabled:
                    blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.Interactable;
                    agent.Properties[PropertyEnum.Interactable] = true;
                    SetCauldronEntityState(ownerController, true);
                    break;

                case State.DestroyAgent:
                    blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.Default;
                    agent.Destroy();
                    break;
            }
        }

        private static void SetCauldronEntityState(AIController ownerController, bool enabled)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

            TransitionGlobalsPrototype transitionGlobalsProto = GameDatabase.TransitionGlobalsPrototype;
            Verify.IsNotNull(transitionGlobalsProto);

            if (enabled)
            {
                Verify.IsTrue(transitionGlobalsProto.EnabledState != PrototypeId.Invalid);
                agent.Properties[PropertyEnum.EntityState] = transitionGlobalsProto.EnabledState;
            }
            else
            {
                Verify.IsTrue(transitionGlobalsProto.DisabledState != PrototypeId.Invalid);
                agent.Properties[PropertyEnum.EntityState] = transitionGlobalsProto.DisabledState;
            }
        }

        public override void OnPlayerInteractEvent(AIController ownerController, in PlayerInteractGameEvent interactEvent)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

            if (interactEvent.InteractableObject is not Agent interactableAgent)
                return;

            if (interactableAgent != agent)
                return;

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            PropertyCollection blackboardProps = blackboard.PropertyCollection;

            blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.DestroyAgent;
            agent.Properties[PropertyEnum.Interactable] = false;
            SetCauldronEntityState(ownerController, false);

            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return;

            AIBroadcastBlackboardGameEvent evt = new(agent, blackboard);
            region.AIBroadcastBlackboardEvent.Invoke(evt);
        }

        public override void OnAIBroadcastBlackboardEvent(AIController ownerController, in AIBroadcastBlackboardGameEvent broadcastEvent)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            WorldEntity broadcaster = broadcastEvent.Broadcaster;
            if (!Verify.IsNotNull(broadcaster)) return;

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;

            if (broadcaster.PrototypeDataRef == KaeciliusPrototype.DataRef)
            {
                blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.Enabled;
                blackboardProps[PropertyEnum.AICustomEntityId1] = broadcaster.Id;
            }
        }
    }

    public class ProceduralProfileKaeciliusPrototype : ProceduralProfileBasicRangePrototype
    {
        public ProceduralPowerWithSpecificTargetsPrototype[] HotspotSpawners { get; protected set; }
        public ProceduralThresholdPowerContextPrototype FalseDeathPower { get; protected set; }
        public ProceduralUsePowerContextPrototype HealFinalFormPower { get; protected set; }
        public ProceduralUsePowerContextPrototype DeathPreventerPower { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public EntityPrototype Cauldron { get; protected set; }

        //---

        private enum State
        {
            Default,
            HotspotSpawners,
            FalseDeath,
            DeathPreventer,
            FinalForm,
        }

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, FalseDeathPower);
            InitPower(agent, HealFinalFormPower);
            InitPower(agent, DeathPreventerPower);

            AIController ownerController = agent.AIController;
            if (!Verify.IsNotNull(ownerController)) return;
            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return;

            ownerController.RegisterForAIBroadcastBlackboardEvents(region, true);

            if (HotspotSpawners.HasValue())
            {
                ProceduralPowerWithSpecificTargetsPrototype firstSpawner = HotspotSpawners[0];
                foreach (ProceduralPowerWithSpecificTargetsPrototype hotspotSpawner in HotspotSpawners)
                {
                    if (!Verify.IsTrue(hotspotSpawner.InitTargets(agent, firstSpawner == hotspotSpawner))) return;

                    InitPower(agent, hotspotSpawner.PowerToUse.DataRef);
                }
            }
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

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            PropertyCollection blackboardProps = blackboard.PropertyCollection;
            
            PrototypeId activePowerRef = blackboardProps[PropertyEnum.AILastPowerActivated];

            if (!Verify.IsTrue(HotspotSpawners.HasValue())) return;

            if (!Verify.IsNotNull(FalseDeathPower)) return;
            if (!Verify.IsNotNull(FalseDeathPower.PowerContext)) return;
            if (!Verify.IsNotNull(FalseDeathPower.PowerContext.Power)) return;

            if (!Verify.IsNotNull(HealFinalFormPower)) return;
            if (!Verify.IsNotNull(HealFinalFormPower.PowerContext)) return;
            if (!Verify.IsNotNull(HealFinalFormPower.PowerContext.Power)) return;

            long health = agent.Properties[PropertyEnum.Health];
            long maxHealth = agent.Properties[PropertyEnum.HealthMax];

            if (activePowerRef == FalseDeathPower.PowerContext.Power.DataRef)
            {
                if (blackboardProps[PropertyEnum.AICustomStateVal2] == (int)State.DeathPreventer)
                {
                    Verify.IsTrue(agent.UnassignPower(activePowerRef));
                    blackboardProps[PropertyEnum.AILastPowerActivated] = PrototypeId.Invalid;
                }
                else
                {
                    return;
                }
            }

            GRandom random = game.Random;
            if (blackboardProps[PropertyEnum.AICustomStateVal2] == (int)State.Default)
            {
                if (proceduralAI.GetState(0) != UsePower.Instance ||
                    activePowerRef == DeathPreventerPower.PowerContext.Power.DataRef)
                {
                    StaticBehaviorReturnType powerResult = HandleUsePowerContext(ownerController, proceduralAI, random, currentTime, DeathPreventerPower.PowerContext, DeathPreventerPower);
                    if (powerResult == StaticBehaviorReturnType.Running)
                        return;
                    else if (powerResult == StaticBehaviorReturnType.Completed)
                        blackboardProps[PropertyEnum.AICustomStateVal2] = (int)State.HotspotSpawners;
                }
            }
            else if (blackboardProps[PropertyEnum.AICustomStateVal2] == (int)State.HotspotSpawners)
            {
                if ((proceduralAI.GetState(0) != UsePower.Instance || activePowerRef == FalseDeathPower.PowerContext.Power.DataRef) &&
                    MathHelper.IsBelowOrEqual(health, maxHealth, FalseDeathPower.HealthThreshold))
                {
                    StaticBehaviorReturnType powerResult = HandleUsePowerContext(ownerController, proceduralAI, random, currentTime, FalseDeathPower.PowerContext, FalseDeathPower);
                    if (powerResult == StaticBehaviorReturnType.Running)
                    {
                        return;
                    }
                    else if (powerResult == StaticBehaviorReturnType.Completed)
                    {
                        blackboardProps[PropertyEnum.AICustomStateVal2] = (int)State.FalseDeath;
                        agent.SetDormant(true);

                        Region region = agent.Region;
                        if (!Verify.IsNotNull(region)) return;

                        AIBroadcastBlackboardGameEvent broadcastEvent = new(agent, blackboard);
                        region.AIBroadcastBlackboardEvent.Invoke(broadcastEvent);
                    }
                }
                else if (proceduralAI.GetState(0) != UsePower.Instance)
                {
                    int index = 1;
                    foreach (ProceduralPowerWithSpecificTargetsPrototype targetedPower in HotspotSpawners)
                    {
                        if (blackboardProps[PropertyEnum.AICustomStateVal1] < index &&
                            MathHelper.IsBelowOrEqual(health, maxHealth, targetedPower.HealthThreshold))
                        {
                            AttemptHotspotSpawnerPower(ownerController, targetedPower);
                            blackboardProps[PropertyEnum.AICustomStateVal1] = index;

                            ProceduralPowerWithSpecificTargetsPrototype nextTarget = HotspotSpawners.ElementAtOrDefault(index);
                            nextTarget?.SearchForTargets(agent, true, true);
                        }
                        index++;
                    }
                }
            }
            else if ((proceduralAI.GetState(0) != UsePower.Instance || activePowerRef == HealFinalFormPower.PowerContext.Power.DataRef) &&
                blackboardProps[PropertyEnum.AICustomStateVal2] == (int)State.DeathPreventer)
            {
                StaticBehaviorReturnType powerResult = HandleUsePowerContext(ownerController, proceduralAI, random, currentTime, HealFinalFormPower.PowerContext, HealFinalFormPower);
                if (powerResult == StaticBehaviorReturnType.Running)
                    return;
                else if (powerResult != StaticBehaviorReturnType.Completed)
                    agent.Properties[PropertyEnum.Health] = agent.Properties[PropertyEnum.HealthMax];

                Verify.IsTrue(agent.UnassignPower(DeathPreventerPower.PowerContext.Power.DataRef));

                blackboardProps[PropertyEnum.AICustomStateVal2] = (int)State.FinalForm;
            }

            base.Think(ownerController);
        }

        private static void AttemptHotspotSpawnerPower(AIController ownerController, ProceduralPowerWithSpecificTargetsPrototype power)
        {
            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;

            Game game = ownerController.Game;
            if (game == null) return;

            EntityManager entityManager = game.EntityManager;

            ulong leftMarkerId = blackboardProps[PropertyEnum.AICustomEntityId1];
            if (leftMarkerId != Entity.InvalidId)
            {
                WorldEntity leftMarker = entityManager.GetEntity<WorldEntity>(leftMarkerId);
                if (leftMarker != null)
                {
                    Verify.IsTrue(ownerController.AttemptActivatePower(power.PowerToUse.DataRef, Entity.InvalidId, leftMarker.RegionLocation.Position),
                        "Kaecilius's hotspot spawner has failed for the first marker.");
                }
            }

            ulong centerMarkerId = blackboardProps[PropertyEnum.AICustomEntityId2];
            if (centerMarkerId != Entity.InvalidId)
            {
                WorldEntity centerMarker = entityManager.GetEntity<WorldEntity>(centerMarkerId);
                if (centerMarker != null)
                {
                    Verify.IsTrue(ownerController.AttemptActivatePower(power.PowerToUse.DataRef, Entity.InvalidId, centerMarker.RegionLocation.Position),
                        "Kaecilius's hotspot spawner has failed for the second platform.");
                }
            }

            ulong rightMarkerId = blackboardProps[PropertyEnum.AICustomEntityId3];
            if (rightMarkerId != Entity.InvalidId)
            {
                WorldEntity rightMarker = entityManager.GetEntity<WorldEntity>(rightMarkerId);
                if (rightMarker != null)
                {
                    Verify.IsTrue(ownerController.AttemptActivatePower(power.PowerToUse.DataRef, Entity.InvalidId, rightMarker.RegionLocation.Position),
                        "Kaecilius's hotspot spawner has failed for the third platform.");
                }
            }
        }

        public override void OnAIBroadcastBlackboardEvent(AIController ownerController, in AIBroadcastBlackboardGameEvent broadcastEvent)
        {
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            WorldEntity broadcaster = broadcastEvent.Broadcaster;
            if (!Verify.IsNotNull(broadcaster)) return;

            if (broadcaster.PrototypeDataRef == Cauldron.DataRef)
            {
                ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal2] = (int)State.DeathPreventer;
                agent.SetDormant(false);
            }
        }
    }

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
    public class ProceduralProfileVulturePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; protected set; }
        public OrbitContextPrototype OrbitTarget { get; protected set; }
        public ProceduralUsePowerContextPrototype LungePower { get; protected set; }
        public int MaxLungeActivations { get; protected set; }

        //---

        private enum State
        {
            Default,
            LungePower,
        }

        public override void Init(Agent agent)
        {
            base.Init(agent);

            InitPower(agent, LungePower);
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

            WorldEntity target = ownerController.TargetEntity;
            if (DefaultSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false &&
                proceduralAI.PartialOverrideBehavior == null)
            {
                return;
            }

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;

            if (blackboardProps[PropertyEnum.AICustomStateVal1] == (int)State.Default)
            {
                GRandom random = game.Random;
                Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
                PopulatePowerPicker(ownerController, powerPicker);
                if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                    return;

                DefaultMeleeMovement(proceduralAI, ownerController, agent.Locomotor, target, MoveToTarget, OrbitTarget);
            }
            else
            {
                StaticBehaviorReturnType powerResult = HandleUsePowerContext(ownerController, proceduralAI, game.Random, currentTime, LungePower.PowerContext, LungePower);
                if (powerResult == StaticBehaviorReturnType.Running)
                    return;
                else if (powerResult == StaticBehaviorReturnType.Failed && IsProceduralPowerContextOnCooldown(ownerController.Blackboard, LungePower, currentTime))
                    return;

                blackboardProps.AdjustProperty(1, PropertyEnum.AICustomStateVal2);

                if (blackboardProps[PropertyEnum.AICustomStateVal2] >= MaxLungeActivations)
                {
                    blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.Default;
                    blackboardProps[PropertyEnum.AICustomStateVal2] = 0;
                }
            }
        }

        public override void PopulatePowerPicker(AIController ownerController, Picker<ProceduralUsePowerContextPrototype> powerPicker)
        {
            ownerController.AddPowersToPicker(powerPicker, LungePower);

            base.PopulatePowerPicker(ownerController, powerPicker);
        }

        public override bool OnPowerPicked(AIController ownerController, ProceduralUsePowerContextPrototype powerContext)
        {
            if (base.OnPowerPicked(ownerController, powerContext) == false)
                return false;

            if (powerContext == LungePower)
                ownerController.Blackboard.PropertyCollection[PropertyEnum.AICustomStateVal1] = (int)State.LungePower;

            return true;
        }
    }
#endif

    public class ProceduralProfileSquirrelGirlSquirrelPrototype : ProceduralProfilePetPrototype
    {
        public ProceduralFlankContextPrototype FlankMaster { get; protected set; }
        public float DeadzoneAroundFlankTarget { get; protected set; }

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

            if (agent.IsDormant)
                return;

            PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;

            WorldEntity master = ownerController.AssistedEntity;
            if (master != null && master.IsInWorld)
            {
                if (blackboardProps.HasProperty(PropertyEnum.AICustomStateVal1) == true)
                {
                    StaticBehaviorReturnType moveToResult = HandleContext(proceduralAI, ownerController, PetFollow);
                    if (moveToResult == StaticBehaviorReturnType.Completed || moveToResult == StaticBehaviorReturnType.Failed) 
                    {
                        blackboardProps[PropertyEnum.AILastAttackerID] = Entity.InvalidId;
                        blackboardProps[PropertyEnum.AICustomStateVal1] = false;
                        ownerController.ResetCurrentTargetState();
                    }
                    else if (moveToResult == StaticBehaviorReturnType.Running)
                    {
                        return;
                    }
                }

                float distanceToMasterSq = Vector3.DistanceSquared2D(agent.RegionLocation.Position, master.RegionLocation.Position);
                if (distanceToMasterSq > MaxDistToMasterBeforeTeleport * MaxDistToMasterBeforeTeleport)
                {
                    if (ownerController.ActivePowerRef == PrototypeId.Invalid)
                    {
                        blackboardProps[PropertyEnum.AILastAttackerID] = Entity.InvalidId;
                        HandleContext(proceduralAI, ownerController, TeleportToMasterIfTooFarAway);
                        ownerController.ResetCurrentTargetState();
                    }
                }
            }

            WorldEntity target = ownerController.TargetEntity;
            if (CommonSimplifiedSensory(ref target, ownerController, proceduralAI, SelectTarget, CombatTargetType.Hostile) == false)
            {
                int distToMasterSq = 0;
                if (master != null && master.IsInWorld)
                    distToMasterSq = (int)Vector3.DistanceSquared2D(agent.RegionLocation.Position, master.RegionLocation.Position);

                StaticBehaviorReturnType flankResult;
                if (proceduralAI.GetState(0) == Flank.Instance)
                {
                    flankResult = HandleProceduralFlank(proceduralAI, ownerController, agent.Locomotor, currentTime, FlankMaster, false);
                    if (flankResult == StaticBehaviorReturnType.Running)
                        return;
                    else if (flankResult == StaticBehaviorReturnType.Completed)
                        blackboardProps[PropertyEnum.AICustomStateVal2] = distToMasterSq;
                }
                else
                {
                    int lastDistToMasterSq = blackboardProps[PropertyEnum.AICustomStateVal2];
                    if (lastDistToMasterSq == 0 || Segment.IsNearZero(distToMasterSq - lastDistToMasterSq, DeadzoneAroundFlankTarget * DeadzoneAroundFlankTarget) == false)
                    {
                        if (lastDistToMasterSq != 0)
                            blackboardProps.RemoveProperty(PropertyEnum.AICustomStateVal2);

                        flankResult = HandleProceduralFlank(proceduralAI, ownerController, agent.Locomotor, currentTime, FlankMaster, false);
                        if (flankResult == StaticBehaviorReturnType.Running)
                            return;
                        else if (flankResult == StaticBehaviorReturnType.Completed)
                            blackboardProps[PropertyEnum.AICustomStateVal2] = distToMasterSq;
                    }
                }
            }

            GRandom random = game.Random;
            Picker<ProceduralUsePowerContextPrototype> powerPicker = new(random);
            PopulatePowerPicker(ownerController, powerPicker);
            if (HandleProceduralPower(ownerController, proceduralAI, random, currentTime, powerPicker, true) == StaticBehaviorReturnType.Running)
                return;

            HandleDefaultPetMovement(proceduralAI, ownerController, currentTime, target);
        }

        public override void OnOwnerTargetSwitch(AIController ownerController, ulong oldTarget, ulong newTarget)
        {
            if (oldTarget == Entity.InvalidId && newTarget != Entity.InvalidId)
                ownerController.Blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AICustomStateVal2);
        }
    }

    public class ProceduralProfileSquirrelTriplePrototype : ProceduralAIProfilePrototype
    {
        public int JumpDistanceMax { get; protected set; }
        public int JumpDistanceMin { get; protected set; }
        public DelayContextPrototype PauseSettings { get; protected set; }
        public int RandomDirChangeDegrees { get; protected set; }

        //---

        private enum State
        {
            Default,
            Pause,
        }

        public override void Think(AIController ownerController)
        {
            ProceduralAI proceduralAI = ownerController.Brain;
            if (!Verify.IsNotNull(proceduralAI)) return;
            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;
            Game game = agent.Game;
            if (!Verify.IsNotNull(game)) return;

            Locomotor locomotor = agent.Locomotor;
            if (!Verify.IsNotNull(locomotor)) return;

            if (locomotor.IsPathComplete())
            {
                PropertyCollection blackboardProps = ownerController.Blackboard.PropertyCollection;
                bool isDefault = blackboardProps[PropertyEnum.AICustomStateVal1] == (int)State.Default;
                if (isDefault)
                {
                    blackboardProps[PropertyEnum.AICustomStateVal1] = (int)State.Pause;
                }
                else
                {
                    if (HandleContext(proceduralAI, ownerController, PauseSettings) == StaticBehaviorReturnType.Running)
                        return;
                }

                Vector3 direction = Vector3.Normalize(agent.Forward);
                if (isDefault == false)
                {
                    float randomAngle = MathHelper.ToRadians(game.Random.Next(-RandomDirChangeDegrees, RandomDirChangeDegrees + 1));
                    direction = Vector3.AxisAngleRotate(direction, Vector3.ZAxis, randomAngle);
                }

                float jumpDistance = game.Random.Next(JumpDistanceMin, JumpDistanceMax + 1);
                LocomotionOptions locomotionOptions = new();
                locomotionOptions.PathGenerationFlags |= PathGenerationFlags.IncompletedPath;
                locomotor.MoveTo(agent.RegionLocation.Position + (direction * jumpDistance), ref locomotionOptions);
            }
        }
    }

#if GAME_VERSION_1_53
    public class ProceduralProfilePsylockeWarPrototype : ProceduralProfileBasicMeleePrototype
    {
        public PrototypeId AreaA { get; protected set; }
        public PrototypeId AreaB { get; protected set; }
        public TriggerSpawnersContextPrototype[] BallistaSpawnersAreaA { get; protected set; }
        public TriggerSpawnersContextPrototype[] BallistaSpawnersAreaB { get; protected set; }
        public int BallistaActivateMinCooldownMS { get; protected set; }
        public int BallistaActivateMaxCooldownMS { get; protected set; }
        public int BallistaSpawnTimerMS { get; protected set; }

        //---

        public override void Think(AIController ownerController)
        {
            // V53_TODO
            base.Think(ownerController);
        }
    }
#endif

#if GAME_VERSION_1_53
    public class ProceduralProfileBallistaMobPrototype : ProceduralProfileWithAttackPrototype
    {
        public PrototypeId PsylockeWar { get; protected set; }
        public ProceduralUsePowerContextPrototype BallistaMisslePower { get; protected set; }

        //---

        public override void Think(AIController ownerController)
        {
            // V53_TODO
            base.Think(ownerController);
        }
    }
#endif

#if GAME_VERSION_1_53
    public class ProceduralProfileFiveManHellfirePrototype : ProceduralProfileWithEnragePrototype
    {
        public ProceduralUsePowerContextPrototype FireSpoutPower { get; protected set; }
        public float FireSpoutPowerAllSearchRadius { get; protected set; }
        public float FireSpoutPowerSafeChoiceRadius { get; protected set; }
        public ProceduralProfileSurturEnc5FireSpoutPrototype FireSpoutProceduralProfile { get; protected set; }
        public PrototypeId FireSpout { get; protected set; }
        public ProceduralUsePowerContextPrototype PrimaryPower { get; protected set; }

        //---

        public override void Think(AIController ownerController)
        {
            // V53_TODO
            base.Think(ownerController);
        }
    }
#endif

#if GAME_VERSION_1_53
    public class ProceduralProfileSurturEnc5FireSpoutPrototype : ProceduralProfileWithAttackPrototype
    {
        public PrototypeId SafeSpotPowerRef { get; protected set; }
        public ProceduralUsePowerContextPrototype SummonFirePower { get; protected set; }

        //---

        public override void Think(AIController ownerController)
        {
            // V53_TODO
            base.Think(ownerController);
        }
    }
#endif

#if GAME_VERSION_1_53
    public class ProceduralProfileSurturFiveManPrototype : ProceduralProfileWithEnragePrototype
    {
        public ProceduralUsePowerContextPrototype AdvanceIntoFight { get; protected set; }
        public PrototypeId RetreatFromFight { get; protected set; }
        public PrototypeId SummonWave { get; protected set; }
        public PrototypeId PhasePower1 { get; protected set; }
        public PrototypeId PhasePower3 { get; protected set; }
        public int PhasePowerCooldownMS { get; protected set; }
        public float Phase1HealthThreshold { get; protected set; }
        public float Phase2HealthThreshold { get; protected set; }
        public float Phase3HealthThreshold { get; protected set; }
        public float Phase4HealthThreshold { get; protected set; }
        public ProceduralUsePowerContextPrototype[] SwordPowersInitial { get; protected set; }
        public ProceduralUsePowerContextPrototype[] SwordPowersPhase4 { get; protected set; }
        public int NumSummonWaveEntities { get; protected set; }

        //---

        public override void Think(AIController ownerController)
        {
            // V53_TODO
            base.Think(ownerController);
        }
    }
#endif
}
