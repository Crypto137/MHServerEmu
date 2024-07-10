using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Behavior;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
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
        protected static readonly Logger Logger = LogManager.CreateLogger();
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

        public bool AllowedInDifficulty(PrototypeId difficultyRef)
        {
            return DifficultyTierPrototype.InRange(difficultyRef, RestrictToDifficultyMin, RestrictToDifficultyMax);
        }

        public override void OnStart(AIController ownerController, ProceduralAIProfilePrototype proceduralProfile)
        {
            if (proceduralProfile is not ProceduralProfileWithAttackPrototype attackProto) return;
            attackProto.OnPowerStarted(ownerController, this);
        }

        public override void OnEnd(AIController ownerController, ProceduralAIProfilePrototype proceduralProfile)
        {
            if (proceduralProfile is not ProceduralProfileWithAttackPrototype attackProto) return;

            attackProto.OnPowerEnded(ownerController, this);
            if (PowerContext == null || PowerContext.Power == PrototypeId.Invalid) return;

            var collection = ownerController.Blackboard.PropertyCollection;
            var game = ownerController.Game;
            if (game == null) return;

            long currentTime = (long)game.CurrentTime.TotalMilliseconds;
            long cooldownTime = currentTime + game.Random.Next(MinCooldownMS, MaxCooldownMS);
            collection[PropertyEnum.AIProceduralPowerSpecificCDTime, PowerContext.Power] = cooldownTime;
        }
    }

    public class ProceduralUseAffixPowerContextPrototype : ProceduralContextPrototype
    {
        public UseAffixPowerContextPrototype AffixContext { get; protected set; }
        public int PickWeight { get; protected set; }

        public override void OnEnd(AIController ownerController, ProceduralAIProfilePrototype proceduralProfile)
        {
            if (proceduralProfile is not ProceduralProfileWithAttackPrototype attackProto) return;

            attackProto.OnPowerEnded(ownerController, this);

            var agent = ownerController.Owner;
            if (agent == null) return;

            var blackboard = ownerController.Blackboard;
            var powerProto = GameDatabase.GetPrototype<PowerPrototype>(blackboard.PropertyCollection[PropertyEnum.AIAffixPowerToActivate]);

            if (powerProto == null)
            {
                Logger.Warn($"Unable to set cooldown time for affix power on entity! Entity: {agent}");
                return;
            }

            var game = ownerController.Game;
            if (game == null) return;

            var cooldownTime = game.CurrentTime + agent.GetAbilityCooldownDuration(powerProto);
            blackboard.PropertyCollection[PropertyEnum.AIProceduralPowerSpecificCDTime, powerProto.DataRef] = (long)cooldownTime.TotalMilliseconds; 
        }
    }

    public class ProceduralFlankContextPrototype : ProceduralContextPrototype
    {
        public int MaxFlankCooldownMS { get; protected set; }
        public int MinFlankCooldownMS { get; protected set; }
        public FlankContextPrototype FlankContext { get; protected set; }

        public override void OnEnd(AIController ownerController, ProceduralAIProfilePrototype proceduralProfile)
        {
            var blackboard = ownerController.Blackboard;
            var game = ownerController.Game;
            if (game == null) return;

            long currentTime = (long)game.CurrentTime.TotalMilliseconds;
            long nextFlankTime = currentTime + game.Random.Next(MinFlankCooldownMS, MaxFlankCooldownMS);
            blackboard.PropertyCollection[PropertyEnum.AIProceduralNextFlankTime] = nextFlankTime;
        }
    }

    public class ProceduralInteractContextPrototype : ProceduralContextPrototype
    {
        public InteractContextPrototype InteractContext { get; protected set; }

        public override void OnEnd(AIController ownerController, ProceduralAIProfilePrototype proceduralProfile)
        {
            if (proceduralProfile is not ProceduralProfileWithTargetPrototype attackProto) return;
            attackProto.OnInteractEnded(ownerController, this);
        }
    }

    public class ProceduralFleeContextPrototype : ProceduralContextPrototype
    {
        public int MaxFleeCooldownMS { get; protected set; }
        public int MinFleeCooldownMS { get; protected set; }
        public FleeContextPrototype FleeContext { get; protected set; }

        public override void OnEnd(AIController ownerController, ProceduralAIProfilePrototype proceduralProfile)
        {
            var blackboard = ownerController.Blackboard;
            var game = ownerController.Game;
            if (game == null) return;

            long currentTime = (long)game.CurrentTime.TotalMilliseconds;
            long nextFleeTime = currentTime + game.Random.Next(MinFleeCooldownMS, MaxFleeCooldownMS);
            blackboard.PropertyCollection[PropertyEnum.AIProceduralNextFlankTime] = nextFleeTime;
        }
    }

    public class ProceduralSyncAttackContextPrototype : Prototype
    {
        public PrototypeId TargetEntity { get; protected set; }
        public PrototypeId TargetEntityPower { get; protected set; }
        public ProceduralUsePowerContextPrototype LeaderPower { get; protected set; }
    }

    public class ProceduralThresholdPowerContextPrototype : ProceduralUsePowerContextPrototype
    {
        public float HealthThreshold { get; protected set; }
    }

    public class ProceduralPowerWithSpecificTargetsPrototype : Prototype
    {
        public float HealthThreshold { get; protected set; }
        public PrototypeId PowerToUse { get; protected set; }
        public PrototypeId[] Targets { get; protected set; }   // VectorPrototypeRefPtr AgentPrototype

        public bool InitTargets(Agent agent, bool addToBlackboard)
        {
            if (Targets.IsNullOrEmpty()) return false;
            return SearchForTargets(agent, addToBlackboard, false);
        }

        public bool SearchForTargets(Agent agent, bool addToBlackboard, bool clearFirst) // ProfileKaecilius only
        {
            AIController ownerController = agent.AIController;
            if (ownerController == null) return false;
            BehaviorBlackboard blackboard = ownerController.Blackboard;
            Region region = agent.Region;
            if (region == null) return false;

            int targetsFound = 0;
            Sphere volume = new (agent.RegionLocation.Position, ownerController.AggroRangeHostile);
            foreach (WorldEntity targetEntity in region.IterateEntitiesInVolume(volume, new(EntityRegionSPContextFlags.ActivePartition)))
            {
                if (targetEntity != null)
                    foreach (var target in Targets)
                    {
                        if (target == targetEntity.PrototypeDataRef)
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

            return (targetsFound == Targets.Length);
        }

        private static bool AddTargetEntityToBlackboard(WorldEntity targetEntity, BehaviorBlackboard blackboard, bool clearFirst)
        {
            if (clearFirst)
            {
                blackboard.PropertyCollection[PropertyEnum.AICustomEntityId1] = 0;
                blackboard.PropertyCollection[PropertyEnum.AICustomEntityId2] = 0;
                blackboard.PropertyCollection[PropertyEnum.AICustomEntityId3] = 0;
            }
            if (blackboard.PropertyCollection[PropertyEnum.AICustomEntityId1] == 0)
                blackboard.PropertyCollection[PropertyEnum.AICustomEntityId1] = targetEntity.Id;
            else if (blackboard.PropertyCollection[PropertyEnum.AICustomEntityId2] == 0)
                blackboard.PropertyCollection[PropertyEnum.AICustomEntityId2] = targetEntity.Id;
            else if (blackboard.PropertyCollection[PropertyEnum.AICustomEntityId3] == 0)
                blackboard.PropertyCollection[PropertyEnum.AICustomEntityId3] = targetEntity.Id;
            else
                return false;
            return true;
        }
    }


}
