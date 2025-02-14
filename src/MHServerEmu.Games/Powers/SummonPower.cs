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

        // REMOVEME
        private static readonly HashSet<PrototypeId> OnDeathSummonProcs = [
            (PrototypeId)2106971016502847711,
            (PrototypeId)2307265150892711295,
            (PrototypeId)3546293262353180156,
            (PrototypeId)5414562982452403703,
            (PrototypeId)7908228357593113141,
            (PrototypeId)12369052742477423808,
            (PrototypeId)12679251810370591997,
            (PrototypeId)15261901601421273503,
            (PrototypeId)15273839860189830062,
            (PrototypeId)15976842786056317493,
            (PrototypeId)16550637434795860683,
            (PrototypeId)17408445236831923988,
            (PrototypeId)17675087605881512697,

            (PrototypeId)8316043623727311458,
            (PrototypeId)14386906015355968513,
            (PrototypeId)15949525972715445694
        ];

        // REMOVEME
        private static readonly HashSet<PrototypeId> SpawnMetalOrbCombos = [
            (PrototypeId)7346548612187493478,
            (PrototypeId)10512701425740682877,
            (PrototypeId)13542089336699950947,
            (PrototypeId)9968471862826768614,
            (PrototypeId)16334726522730453455
        ];

        public SummonPower(Game game, PrototypeId prototypeDataRef) : base(game, prototypeDataRef)
        {
        }

        public override PowerUseResult Activate(ref PowerActivationSettings settings)
        {
            // HACK/REMOVEME: Remove these hacks when we get summon powers working properly

            // Special handling for OnDeath summon procs
            if (OnDeathSummonProcs.Contains(PrototypeDataRef))
            {
                EntityHelper.OnDeathSummonFromPowerPrototype(Owner, (SummonPowerPrototype)Prototype);
                return PowerUseResult.Success;
            }

            // Special handling for Magneto's metal orb spawning combos
            if (SpawnMetalOrbCombos.Contains(PrototypeDataRef))
            {
                WorldEntity target = Game.EntityManager.GetEntity<WorldEntity>(settings.TargetEntityId);
                EntityHelper.CreateMetalOrbFromPowerPrototype(Owner, target, settings.TargetPosition, (SummonPowerPrototype)Prototype);
                return PowerUseResult.Success;
            }

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
