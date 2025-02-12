using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Powers
{
    public class SummonPower : Power
    {
        public SummonPowerPrototype SummonPowerPrototype => Prototype as SummonPowerPrototype;

        private static readonly Logger Logger = LogManager.CreateLogger();
        private int _totalSummonedEntities;
        private EventPointer<SummonEvent> _summonEvent;

        public SummonPower(Game game, PrototypeId prototypeDataRef) : base(game, prototypeDataRef)
        {
            _totalSummonedEntities = 0;
        }

        public override bool ApplyPower(PowerApplication powerApplication)
        {
            CheckSummonedEntities();

            if (base.ApplyPower(powerApplication) == false) 
                return false;

            ScheduleSummonEntity(1);
            UpdateAndCheckTotalSummonedEntities();

            return true;
        }

        private void CheckSummonedEntities()
        {
            if (Owner == null) return;

            var summonPowerProto = SummonPowerPrototype;
            if (summonPowerProto == null || summonPowerProto.SummonEntityContexts.IsNullOrEmpty()) return;

            var inventory = Owner.GetInventory(InventoryConvenienceLabel.Summoned);
            if (inventory == null) return;

            var manager = Owner.Game?.EntityManager;
            if (manager == null) return;

            foreach (var context in summonPowerProto.SummonEntityContexts)
            {
                if (context == null) return;

                var removalProto = context.SummonEntityRemoval;
                if (removalProto == null) continue;

                bool removalKeywords = removalProto.Keywords.HasValue();
                bool removalPowers = removalProto.FromPowers.HasValue();

                if (removalKeywords == false && removalPowers == false) continue;

                List<WorldEntity> killList = [];

                foreach (var entry in inventory)
                {
                    var summoned = manager.GetEntity<WorldEntity>(entry.Id);
                    if (summoned == null || summoned.IsDead) continue;

                    bool found = false;
                    if (removalKeywords)
                        found |= SummonedHasKeywords(summoned, removalProto.Keywords);
                    
                    if (found == false && removalPowers)
                        found |= SummonedHasCreatorPower(summoned, removalProto.FromPowers);

                    if (found) killList.Add(summoned);
                }

                foreach (var summoned in killList) KillSummoned(summoned, Owner);
            }
        }

        public static void KillSummoned(WorldEntity summoned, WorldEntity owner)
        {
            if (summoned.IsAliveInWorld)
            {
                summoned.TryActivateOnDeathProcs(new());
                summoned.Kill(null, KillFlags.NoExp | KillFlags.NoLoot | KillFlags.NoDeadEvent);
            }
            else
            {
                if (summoned.IsSummonedPet()) owner?.TryActivateOnPetDeathProcs(summoned);
                summoned.Destroy();
            }
        }

        private static bool SummonedHasCreatorPower(WorldEntity summoned, PrototypeId[] fromPowers)
        {
            var creatorPowerRef = summoned.Properties[PropertyEnum.CreatorPowerPrototype];

            foreach (var power in fromPowers)
                if (creatorPowerRef == power) return true;

            return false;
        }

        private static bool SummonedHasKeywords(WorldEntity summoned, PrototypeId[] keywords)
        {
            foreach (var keyword in keywords)
                if (summoned.HasKeyword(keyword)) return true;

            return false;
        }

        public override PowerUseResult CanActivate(WorldEntity target, Vector3 targetPosition, PowerActivationSettingsFlags flags)
        {
            if (IsOnExtraActivation() == false)
            {
                PowerUseResult result = CanSummonEntity();
                if (result != PowerUseResult.Success)
                    return result;
            }

            return base.CanActivate(target, targetPosition, flags);
        }

        private PowerUseResult CanSummonEntity()
        {
            var prototype = SummonPowerPrototype;
            if (prototype == null) return PowerUseResult.GenericError;

            if (prototype.TrackInInventory && !prototype.KillPreviousSummons)
            {
                int maxNumSimultaneousSummons = prototype.GetMaxNumSimultaneousSummons(Properties);
                if (maxNumSimultaneousSummons > 0)
                {
                    int summonedEntityCount = GetExistingSummonedEntitiesCount(Owner, prototype);
                    if (summonedEntityCount >= maxNumSimultaneousSummons)
                        return PowerUseResult.SummonSimultaneousLimit;
                }
            }

            int maxNumSummons = prototype.GetMaxNumSummons(Properties);
            if (maxNumSummons > 0 && _totalSummonedEntities >= maxNumSummons)
                return PowerUseResult.SummonLifetimeLimit;

            return PowerUseResult.Success;
        }

        private static int GetExistingSummonedEntitiesCount(WorldEntity owner, PowerPrototype summonPowerProto)
        {
            if (owner != null && owner.IsInWorld)
                return owner.Properties[PropertyEnum.PowerSummonedEntityCount, summonPowerProto.DataRef];

            return 0;
        }

        public void UpdateAndCheckTotalSummonedEntities()
        {
            var proto = SummonPowerPrototype;
            if (proto == null) return;

            int maxNumSummons = proto.GetMaxNumSummons(Properties);
            if (maxNumSummons > 0)
            { 
                if (_totalSummonedEntities < maxNumSummons) _totalSummonedEntities++;

                if (proto.SummonMaxReachedDestroyOwner && _totalSummonedEntities >= maxNumSummons)
                    Owner?.Kill();
            }
        }

        private void ScheduleSummonEntity(int index)
        {
            var summonPowerProto = SummonPowerPrototype;
            if (summonPowerProto.SummonIntervalMS <= 0) return;

            var scheduler = Game?.GameEventScheduler;
            if (scheduler == null) return;

            if (_summonEvent.IsValid) scheduler.CancelEvent(_summonEvent);

            var timeOffset = TimeSpan.FromMilliseconds(summonPowerProto.SummonIntervalMS);
            scheduler.ScheduleEvent(_summonEvent, timeOffset, _pendingEvents);
            _summonEvent.Get().Initialize(this, index);
        }

        private void SummonEntity(int index)
        {
            if (index <= 0 || CanSummonEntity() != PowerUseResult.Success) return;

            // TODO SummonEntityContext
        }

        private class SummonEvent : CallMethodEventParam1<SummonPower, int>
        {
            protected override CallbackDelegate GetCallback() => (t, p1) => t.SummonEntity(p1);
        }
    }
}
