using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.Behavior;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.Generators;
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
        public virtual void OnStart(AIController owningController, ProceduralAIProfilePrototype procedurealProfile) { }
        public virtual void OnEnd(AIController owningController, ProceduralAIProfilePrototype procedurealProfile) { }
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
    }

    public class ProceduralUseAffixPowerContextPrototype : ProceduralContextPrototype
    {
        public UseAffixPowerContextPrototype AffixContext { get; protected set; }
        public int PickWeight { get; protected set; }
    }

    public class ProceduralFlankContextPrototype : ProceduralContextPrototype
    {
        public int MaxFlankCooldownMS { get; protected set; }
        public int MinFlankCooldownMS { get; protected set; }
        public FlankContextPrototype FlankContext { get; protected set; }
    }

    public class ProceduralInteractContextPrototype : ProceduralContextPrototype
    {
        public InteractContextPrototype InteractContext { get; protected set; }
    }

    public class ProceduralFleeContextPrototype : ProceduralContextPrototype
    {
        public int MaxFleeCooldownMS { get; protected set; }
        public int MinFleeCooldownMS { get; protected set; }
        public FleeContextPrototype FleeContext { get; protected set; }
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
