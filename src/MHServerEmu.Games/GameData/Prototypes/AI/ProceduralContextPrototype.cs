using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Behavior;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)Invalid)]
    public enum PathMethod  // AI/Misc/Types/MoveToPathMethodType.type
    {
        Invalid = 0,
        Forward = 1,
        ForwardLoop = 5,
        ForwardBackAndForth = 3,
        Reverse = 2,
        ReverseLoop = 6,
        ReverseBackAndForth = 4,
    }

    #endregion

    public class ProceduralContextPrototype : Prototype
    {
        //---

        public virtual void OnStart(AIController ownerController, ProceduralAIProfilePrototype proceduralProfile) { }

        public virtual void OnEnd(AIController ownerController, ProceduralAIProfilePrototype proceduralProfile) { }
    }

    public class ProceduralUsePowerContextSwitchTargetPrototype : Prototype
    {
        public SelectEntityContextPrototype SelectTarget { get; protected set; }
        public bool SwitchPermanently { get; protected set; }
        public bool UsePowerOnCurTargetIfSwitchFails { get; protected set; }
    }

    public class ProceduralUsePowerContextPrototype : ProceduralContextPrototype
    {
        public int InitialCooldownMinMS { get; protected set; }
        public int MaxCooldownMS { get; protected set; }
        public int MinCooldownMS { get; protected set; }
        public UsePowerContextPrototype PowerContext { get; protected set; }
        public int PickWeight { get; protected set; }
        public ProceduralUsePowerContextSwitchTargetPrototype TargetSwitch { get; protected set; }
        public int InitialCooldownMaxMS { get; protected set; }
        public PrototypeId RestrictToDifficultyMin { get; protected set; }
        public PrototypeId RestrictToDifficultyMax { get; protected set; }

        //---

        public override void OnStart(AIController ownerController, ProceduralAIProfilePrototype proceduralProfile)
        {
            ProceduralProfileWithAttackPrototype attackProto = proceduralProfile as ProceduralProfileWithAttackPrototype;
            if (!Verify.IsNotNull(attackProto)) return;

            attackProto.OnPowerStarted(ownerController, this);
        }

        public override void OnEnd(AIController ownerController, ProceduralAIProfilePrototype proceduralProfile)
        {
            ProceduralProfileWithAttackPrototype attackProto = proceduralProfile as ProceduralProfileWithAttackPrototype;
            if (!Verify.IsNotNull(attackProto)) return;

            attackProto.OnPowerEnded(ownerController, this);

            if (!Verify.IsNotNull(PowerContext)) return;
            if (!Verify.IsNotNull(PowerContext.Power)) return;

            PropertyCollection properties = ownerController.Blackboard.PropertyCollection;

            Game game = ownerController.Game;
            if (!Verify.IsNotNull(game)) return;

            long currentTime = (long)game.CurrentTime.TotalMilliseconds;
            long cooldownTime = currentTime + game.Random.Next(MinCooldownMS, MaxCooldownMS);
            properties[PropertyEnum.AIProceduralPowerSpecificCDTime, PowerContext.Power.DataRef] = cooldownTime;
        }

        public bool AllowedInDifficulty(PrototypeId difficultyRef)
        {
            return DifficultyTierPrototype.InRange(difficultyRef, RestrictToDifficultyMin, RestrictToDifficultyMax);
        }
    }

    public class ProceduralUseAffixPowerContextPrototype : ProceduralContextPrototype
    {
        public UseAffixPowerContextPrototype AffixContext { get; protected set; }
        public int PickWeight { get; protected set; }

        //---

        public override void OnEnd(AIController ownerController, ProceduralAIProfilePrototype proceduralProfile)
        {
            ProceduralProfileWithAttackPrototype attackProto = proceduralProfile as ProceduralProfileWithAttackPrototype;
            if (!Verify.IsNotNull(attackProto)) return;

            attackProto.OnPowerEnded(ownerController, this);

            Agent agent = ownerController.Owner;
            if (!Verify.IsNotNull(agent)) return;

            PropertyCollection properties = ownerController.Blackboard.PropertyCollection;

            PowerPrototype powerProto = GameDatabase.GetPrototype<PowerPrototype>(properties[PropertyEnum.AIAffixPowerToActivate]);
            if (!Verify.IsNotNull(powerProto, $"Unable to set cooldown time for affix power on entity! Entity: {agent}"))
                return;

            Game game = ownerController.Game;
            if (!Verify.IsNotNull(game)) return;

            TimeSpan cooldownTime = game.CurrentTime + agent.GetAbilityCooldownDuration(powerProto);
            properties[PropertyEnum.AIProceduralPowerSpecificCDTime, powerProto.DataRef] = (long)cooldownTime.TotalMilliseconds; 
        }
    }

    public class ProceduralFlankContextPrototype : ProceduralContextPrototype
    {
        public int MaxFlankCooldownMS { get; protected set; }
        public int MinFlankCooldownMS { get; protected set; }
        public FlankContextPrototype FlankContext { get; protected set; }

        //---

        public override void OnEnd(AIController ownerController, ProceduralAIProfilePrototype proceduralProfile)
        {
            PropertyCollection properties = ownerController.Blackboard.PropertyCollection;

            Game game = ownerController.Game;
            if (!Verify.IsNotNull(game)) return;

            long currentTime = (long)game.CurrentTime.TotalMilliseconds;
            long nextFlankTime = currentTime + game.Random.Next(MinFlankCooldownMS, MaxFlankCooldownMS);
            properties[PropertyEnum.AIProceduralNextFlankTime] = nextFlankTime;
        }
    }

    public class ProceduralInteractContextPrototype : ProceduralContextPrototype
    {
        public InteractContextPrototype InteractContext { get; protected set; }

        //---

        public override void OnEnd(AIController ownerController, ProceduralAIProfilePrototype proceduralProfile)
        {
            ProceduralProfileWithAttackPrototype attackProto = proceduralProfile as ProceduralProfileWithAttackPrototype;
            if (!Verify.IsNotNull(attackProto)) return;

            attackProto.OnInteractEnded(ownerController, this);
        }
    }

    public class ProceduralFleeContextPrototype : ProceduralContextPrototype
    {
        public int MaxFleeCooldownMS { get; protected set; }
        public int MinFleeCooldownMS { get; protected set; }
        public FleeContextPrototype FleeContext { get; protected set; }

        //---

        public override void OnEnd(AIController ownerController, ProceduralAIProfilePrototype proceduralProfile)
        {
            PropertyCollection properties = ownerController.Blackboard.PropertyCollection;

            Game game = ownerController.Game;
            if (!Verify.IsNotNull(game)) return;

            long currentTime = (long)game.CurrentTime.TotalMilliseconds;
            long nextFleeTime = currentTime + game.Random.Next(MinFleeCooldownMS, MaxFleeCooldownMS);
            properties[PropertyEnum.AIProceduralNextFleeTime] = nextFleeTime;
        }
    }

    public class ProceduralSyncAttackContextPrototype : Prototype
    {
        public PrototypeId TargetEntity { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public ProceduralUsePowerContextPrototype TargetEntityPower { get; protected set; }
        public ProceduralUsePowerContextPrototype LeaderPower { get; protected set; }
    }

    public class ProceduralThresholdPowerContextPrototype : ProceduralUsePowerContextPrototype
    {
        public float HealthThreshold { get; protected set; }
    }

    public class ProceduralPowerWithSpecificTargetsPrototype : Prototype
    {
        public float HealthThreshold { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public PowerPrototype PowerToUse { get; protected set; }
        [PrototypeField(PrototypeFieldType.VectorPrototypeRefPtr)]
        public AgentPrototype[] Targets { get; protected set; }

        //---

        public bool InitTargets(Agent agent, bool addToBlackboard)
        {
            if (!Verify.IsNotNull(agent)) return false;
            if (!Verify.IsTrue(Targets.HasValue())) return false;

            return SearchForTargets(agent, addToBlackboard, false);
        }

        public bool SearchForTargets(Agent agent, bool addToBlackboard, bool clearFirst) // ProfileKaecilius only
        {
            AIController ownerController = agent.AIController;
            if (!Verify.IsNotNull(ownerController)) return false;

            BehaviorBlackboard blackboard = ownerController.Blackboard;

            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return false;

            int targetsFound = 0;
            Sphere volume = new(agent.RegionLocation.Position, ownerController.AggroRangeHostile);
            foreach (WorldEntity targetEntity in region.IterateEntitiesInVolume(volume, new(EntityRegionSPContextFlags.PrimaryPartition)))
            {
                if (targetEntity == null)
                    continue;

                foreach (AgentPrototype targetProto in Targets)
                {
                    if (targetProto.DataRef == targetEntity.PrototypeDataRef)
                    {
                        if (addToBlackboard)
                        {
                            AddTargetEntityToBlackboard(targetEntity, blackboard, clearFirst);
                            clearFirst = false;
                        }
                        targetsFound++;
                        break;
                    }
                }
            }

            return targetsFound == Targets.Length;
        }

        private static bool AddTargetEntityToBlackboard(WorldEntity targetEntity, BehaviorBlackboard blackboard, bool clearFirst)
        {
            PropertyCollection properties = blackboard.PropertyCollection;

            if (clearFirst)
            {
                properties[PropertyEnum.AICustomEntityId1] = 0;
                properties[PropertyEnum.AICustomEntityId2] = 0;
                properties[PropertyEnum.AICustomEntityId3] = 0;
            }

            if (properties[PropertyEnum.AICustomEntityId1] == 0)
                properties[PropertyEnum.AICustomEntityId1] = targetEntity.Id;
            else if (properties[PropertyEnum.AICustomEntityId2] == 0)
                properties[PropertyEnum.AICustomEntityId2] = targetEntity.Id;
            else if (properties[PropertyEnum.AICustomEntityId3] == 0)
                properties[PropertyEnum.AICustomEntityId3] = targetEntity.Id;
            else
                return false;

            return true;
        }
    }
}
