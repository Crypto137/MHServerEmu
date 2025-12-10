using Gazillion;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateRegionPlayerAccess : MetaState, IPropertyChangeWatcher
    {
	    private MetaStateRegionPlayerAccessPrototype _proto;
        private PropertyCollection _properties;
        private bool _setAccess;

        public MetaStateRegionPlayerAccess(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateRegionPlayerAccessPrototype;
        }

        public override void OnApply()
        {
            if (_proto.EvalTrigger != null)
                Attach(MetaGame.Properties);
            else
                SetAccess();
        }

        private void SetAccess()
        {
            if (Region == null) return;
            ServiceMessage.SetRegionPlayerAccess message = new(Region.Id, (RegionPlayerAccessVar)_proto.Access);
            ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, message);
            _setAccess = true;
        }

        public override void OnRemove()
        {
            if (_proto.EvalTrigger != null) 
                Detach(true);
        }

        public void Attach(PropertyCollection propertyCollection)
        {
            if (_properties != null && _properties == propertyCollection) return;
            _properties = propertyCollection;
            _properties.AttachWatcher(this);
        }

        public void Detach(bool removeFromAttachedCollection)
        {
            if (removeFromAttachedCollection)
                _properties?.DetachWatcher(this);
        }

        public void OnPropertyChange(PropertyId id, PropertyValue newValue, PropertyValue oldValue, SetPropertyFlags flags)
        {
            if (_setAccess || _proto.EvalTrigger == null) return;

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetVar_PropertyCollectionPtr(EvalContext.Default, MetaGame.Properties);

            if (Eval.RunBool(_proto.EvalTrigger, evalContext))
                SetAccess();
        }
    }
}
