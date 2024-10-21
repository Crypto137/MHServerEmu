using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionEntityPerformPower : MissionActionEntityTarget
    {
        private MissionActionEntityPerformPowerPrototype _proto;

        private static readonly Logger Logger = LogManager.CreateLogger();

        public MissionActionEntityPerformPower(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype) 
        {
            _proto = prototype as MissionActionEntityPerformPowerPrototype;
        }

        public override bool Evaluate(WorldEntity entity)
        {
            // GenoshaHubNPCAnimController
            if (entity is not Agent) return false;
            return base.Evaluate(entity);
        }

        public override bool RunEntity(WorldEntity entity)
        {
            if (entity is not Agent agent) return false;
            bool canRun = true;
            if (agent.IsControlledEntity == false)
            {
                if (_proto.MissionReferencedPowerRemove)
                    agent.RemoveMissionActionReferencedPowers(MissionRef);

                if (_proto.PowerPrototype != PrototypeId.Invalid)
                {
                    if (_proto.PowerRemove)
                        canRun &= agent.UnassignPower(_proto.PowerPrototype);
                    else
                        canRun &= ActivatePerformPower(agent, _proto.PowerPrototype);
                }

                if (_proto.BrainOverride != PrototypeId.Invalid)
                {
                    if (_proto.BrainOverrideRemove)
                        canRun &= RemoveOverrideBrain(agent);
                    else
                        canRun &= OverrideBrain(agent, _proto.BrainOverride);
                }

                if (_proto.EvalProperties != null)
                {
                    using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                    evalContext.Game = agent.Game;
                    evalContext.SetVar_EntityPtr(EvalContext.Default, agent);
                    evalContext.SetVar_PropertyCollectionPtr(EvalContext.Other, agent.Region.Properties);
                    Eval.RunBool(_proto.EvalProperties, evalContext);
                }
            }
            return canRun;
        }

        private bool ActivatePerformPower(Agent agent, PrototypeId powerPrototype)
        {
            // Logger.Debug($"[{Mission.PrototypeName}] Try ActivatePerformPower {GameDatabase.GetFormattedPrototypeName(powerPrototype)} for {agent.PrototypeName} simulate is {agent.IsSimulated}");
            if (agent.IsSimulated == false) return false;
            return agent.ActivatePerformPower(powerPrototype) == PowerUseResult.Success;
        }

        private bool OverrideBrain(Agent agent, PrototypeId brainOverride)
        {
            var controller = agent.AIController;
            if (controller != null)
                controller.Blackboard.PropertyCollection[PropertyEnum.AIFullOverride] = brainOverride;
            else
            {
                var brain = GameDatabase.GetPrototype<BrainPrototype>(brainOverride);
                if (brain is not ProceduralAIProfilePrototype profile) return false;
                using PropertyCollection collection = ObjectPoolManager.Instance.Get<PropertyCollection>();
                agent.InitAIOverride(profile, collection);
            }
            return true;
        }

        private bool RemoveOverrideBrain(Agent agent)
        {
            var controller = agent.AIController;
            if (controller == null) return false;
            controller.Blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AIFullOverride);
            return true;
        }
    }
}
