using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Powers
{
    public class SummonPower : Power
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public SummonPower(Game game, PrototypeId prototypeDataRef) : base(game, prototypeDataRef)
        {
        }

        public override PowerUseResult Activate(ref PowerActivationSettings settings)
        {
            // HACK/REMOVEME: Remove these hacks when we get summon powers working properly

            // Pass non-avatar and non-item activations to the base implementation
            if (Owner is not Avatar avatar)
                return base.Activate(ref settings);

            if (settings.ItemSourceId == Entity.InvalidId)
                return base.Activate(ref settings);

            Item item = Game.EntityManager.GetEntity<Item>(settings.ItemSourceId);
            if (item == null)
                return base.Activate(ref settings);

            // Also pass passive summons from items, we don't have proper cleanup for those
            if (GetActivationType() == PowerActivationType.Passive || IsItemPower() == false)
                return base.Activate(ref settings);

            // Do the hackery
            SummonPowerPrototype summonPowerProto = Prototype as SummonPowerPrototype;

            PropertyId summonedEntityCountProp = new(PropertyEnum.PowerSummonedEntityCount, PrototypeDataRef);
            if (avatar.Properties[PropertyEnum.PowerToggleOn, PrototypeDataRef])
            {
                EntityHelper.DestroySummonerFromPowerPrototype(avatar, summonPowerProto);

                if (IsToggled())  // Check for Danger Room scenarios that are not toggled
                    avatar.Properties[PropertyEnum.PowerToggleOn, PrototypeDataRef] = false;

                avatar.Properties.AdjustProperty(-1, summonedEntityCountProp);
            }
            else
            {
                // Fix for holo crafter / vendor spam
                if (avatar.Properties[summonedEntityCountProp] > 0)
                {
                    EntityHelper.DestroySummonerFromPowerPrototype(avatar, summonPowerProto);
                    avatar.Properties.AdjustProperty(-1, summonedEntityCountProp);
                }

                EntityHelper.SummonEntityFromPowerPrototype(avatar, summonPowerProto, item);

                if (IsToggled())  // Check for Danger Room scenarios that are not toggled
                    avatar.Properties[PropertyEnum.PowerToggleOn, PrototypeDataRef] = true;

                avatar.Properties.AdjustProperty(1, summonedEntityCountProp);
            }

            item.OnUsePowerActivated();

            return PowerUseResult.Success;
        }
    }
}
