using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateStartTargetOverride : MetaState
    {
	    private MetaStateStartTargetOverridePrototype _proto;
		
        public MetaStateStartTargetOverride(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateStartTargetOverridePrototype;
        }

        public override void OnApply()
        {
            var region = Region;
            if (region == null) return;

            if (_proto.StartTarget != PrototypeId.Invalid)
                region.Properties[PropertyEnum.RegionStartTargetOverride] = _proto.StartTarget;
            else
                region.Properties.RemoveProperty(PropertyEnum.RegionStartTargetOverride);
        }

        public override void OnRemove()
        {
            var region = Region;
            if (region == null) return;

            if (_proto.StartTarget != PrototypeId.Invalid && _proto.OnRemoveClearOverride)
                region.Properties.RemoveProperty(PropertyEnum.RegionStartTargetOverride);

            base.OnRemove();
        }
    }
}
