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
        public MissionActionEntityPerformPower(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype) { }

        public override bool Evaluate(WorldEntity entity)
        {
            if (entity is not Agent) return false;
            return base.Evaluate(entity);
        }

        public override bool RunEntity(WorldEntity entity)
        {
            if (entity is not Agent agent) return false;
            if (Prototype is not MissionActionEntityPerformPowerPrototype proto) return false;
            bool canRun = true;
            if (agent.IsControlledEntity == false)
            {
                if (proto.MissionReferencedPowerRemove)
                    agent.RemoveMissionActionReferencedPowers(MissionRef);

                if (proto.PowerPrototype != PrototypeId.Invalid)
                {
                    if (proto.PowerRemove)
                        canRun &= agent.UnassignPower(proto.PowerPrototype);
                    else
                        canRun &= ActivatePerformPower(agent, proto.PowerPrototype);
                }

                if (proto.BrainOverride != PrototypeId.Invalid)
                {
                    if (proto.BrainOverrideRemove)
                        canRun &= RemoveOverrideBrain(agent);
                    else
                        canRun &= OverrideBrain(agent, proto.BrainOverride);
                }

                if (proto.EvalProperties != null)
                {
                    EvalContextData evalContext = new(agent.Game);
                    evalContext.SetVar_EntityPtr(EvalContext.Default, agent);
                    evalContext.SetVar_PropertyCollectionPtr(EvalContext.Other, agent.Region.Properties);
                    Eval.RunBool(proto.EvalProperties, evalContext);
                }
            }
            return canRun;
        }

        private bool ActivatePerformPower(Agent agent, PrototypeId powerPrototype)
        {
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
                agent.InitAIOverride(profile, new());
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
