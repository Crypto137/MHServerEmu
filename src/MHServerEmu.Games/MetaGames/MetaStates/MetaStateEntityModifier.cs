using MHServerEmu.Core.Memory;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateEntityModifier : MetaState
    {
        private MetaStateEntityModifierPrototype _proto;
        private Action<EntityEnteredWorldGameEvent> _entityEnteredWorldAction;

        public MetaStateEntityModifier(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateEntityModifierPrototype;
            _entityEnteredWorldAction = OnEntityEnteredWorld;
        }

        public override void OnApply()
        {
            var region = Region;
            if (region == null) return;

            if (_proto.EntityFilter != null)
                region.EntityEnteredWorldEvent.AddActionBack(_entityEnteredWorldAction);
        }

        public override void OnRemove()
        {
            var region = Region;
            if (region == null) return;

            if (_proto.EntityFilter != null)
                region.EntityEnteredWorldEvent.RemoveAction(_entityEnteredWorldAction);

            base.OnRemove();
        }

        private void OnEntityEnteredWorld(EntityEnteredWorldGameEvent evt)
        {
            var entity = evt.Entity;
            if (entity == null || _proto.EntityFilter == null) return;

            if (_proto.EntityFilter.Evaluate(entity, new()) && _proto.Eval != null)
            {
                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                evalContext.Game = Game;
                evalContext.SetVar_EntityPtr(EvalContext.Default, entity);
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, MetaGame.Properties);
                Eval.RunBool(_proto.Eval, evalContext);
            }
        }
    }
}
