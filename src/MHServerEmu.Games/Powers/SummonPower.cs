using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;

namespace MHServerEmu.Games.Powers
{
    public struct SummonContext
    {
        public Game Game;
        public WorldEntity Target;
        public SummonPowerPrototype PowerProto;
        public ulong RegionId;
        public Vector3 Position;
        public Vector3 TargetPosition;
        public ulong PowerOwnerId;
        public ulong UltimateOwnerId;
        public AlliancePrototype OwnerAlliance;
        public TimeSpan VariableActivationTime;
        public AssetId EntityAsset;
        public PropertyCollection Properties;
        public int MaxNumSimultaneous;
        public bool KillPrevious;
    }

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

        public override void OnPayloadInit(PowerPayload payload)
        {
            payload.DeliverAction = OnDeliverPayload;

            if (Owner != null && Owner.Properties.HasProperty(PropertyEnum.ParentSpawnerGroupId))
                payload.Properties[PropertyEnum.ParentSpawnerGroupId] = Owner.Properties[PropertyEnum.ParentSpawnerGroupId];
        }

        private void OnDeliverPayload(PowerPayload payload)
        {
            var game = payload.Game;
            if (game == null) return;

            var manager = game.EntityManager;
            if (manager == null) return;

            if (payload.PowerPrototype is not SummonPowerPrototype powerProto) return;

            if (powerProto.SummonsLiveWhilePowerActive)
            {
                var owner = manager.GetEntity<WorldEntity>(payload.PowerOwnerId);
                if (owner == null || owner.IsInWorld == false) return;

                var power = owner.GetPower(powerProto.DataRef);
                if (power == null || power.IsActive == false) return;
            }


            if (powerProto.AttachSummonsToTarget || powerProto.UseTargetAsSource)
            {
                List<WorldEntity> targetList = ListPool<WorldEntity>.Instance.Get();
                GetTargets(targetList, payload);

                foreach (var target in targetList)
                    SummonPayloadEntity(manager, powerProto, payload, target);

                if (targetList.Count == 0 && powerProto.UseTargetAsSource)
                    SummonPayloadEntity(manager, powerProto, payload, null);

                ListPool<WorldEntity>.Instance.Return(targetList);
            }
            else
            {
                WorldEntity target = null;
                if (powerProto.AttachSummonsToCaster)
                    target = manager.GetEntity<WorldEntity>(payload.PowerOwnerId);
                
                SummonPayloadEntity(manager, powerProto, payload, target);
            }
        }

        private static void SummonPayloadEntity(EntityManager manager, SummonPowerPrototype powerProto, PowerPayload payload, WorldEntity target)
        {
            var payloadProperties = payload.Properties;
            int summonNum = payloadProperties[PropertyEnum.SummonNumPerActivation];
            if (summonNum < 1) return;

            int maxSummons = powerProto.GetMaxNumSimultaneousSummons(payloadProperties);
            if (maxSummons != 0 && summonNum > maxSummons) return;

            if (payload.OwnerAlliance == null) return;

            if (payload.PowerOwnerId == Entity.InvalidId) return;
            var owner = manager.GetEntity<WorldEntity>(payload.PowerOwnerId);

            int count = 0;

            if (powerProto.TrackInInventory)
                count = GetExistingSummonedEntitiesCount(owner, powerProto);

            bool killPrevious = powerProto.KillPreviousSummons;

            if (killPrevious == false && maxSummons > 0 && count >= maxSummons)
                Logger.Warn($"Summoned more than allowed {count} of {maxSummons}");

            SummonContext context = new()
            {
                Game = payload.Game,
                Target = target,
                PowerProto = powerProto,
                RegionId = payload.RegionId,
                Position = payload.PowerOwnerPosition,
                TargetPosition = payload.TargetPosition,
                PowerOwnerId = payload.PowerOwnerId,
                UltimateOwnerId = payload.UltimateOwnerId,
                OwnerAlliance = payload.OwnerAlliance,
                VariableActivationTime = payload.VariableActivationTime,
                EntityAsset = payloadProperties[PropertyEnum.CreatorEntityAssetRefCurrent],
                Properties = payloadProperties,
                MaxNumSimultaneous = maxSummons,
                KillPrevious = killPrevious
            };            

            for (int i = 0; i < maxSummons; i++)
            {
                context.KillPrevious = killPrevious && maxSummons > 0 && count >= maxSummons;

                var result = SummonEntityContext(manager, context, i);
                switch (result)
                {
                    case PowerUseResult.Success:

                        count++;

                        if (killPrevious == false && maxSummons > 0 && count >= maxSummons)
                            return;

                        break;

                    case PowerUseResult.RestrictiveCondition:
                    case PowerUseResult.DisabledByLiveTuning:
                        break;

                    default:
                        return;
                }
            }
        }

        private void SummonEntityIndex(int index)
        {
            if (index < 0 || CanSummonEntity() != PowerUseResult.Success) return;

            if (Owner == null || Owner.IsInWorld == false) return;

            var manager = Game?.EntityManager;
            if (manager == null) return;

            var powerProto = SummonPowerPrototype;
            if (powerProto.SummonEntityContexts.IsNullOrEmpty()) return;

            var regionId = Owner.RegionLocation.RegionId;
            var position = Owner.RegionLocation.Position;

            ulong ultimateOwnerId = Entity.InvalidId;
            AssetId entityAsset;

            var ultimateOwner = GetUltimateOwner();
            if (ultimateOwner != null)
            {
                ultimateOwnerId = ultimateOwner.Id;
                entityAsset = ultimateOwner.GetEntityWorldAsset();
            }
            else
            {
                entityAsset = Owner.GetEntityWorldAsset();
            }

            using PropertyCollection properties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            WorldEntity propertySourceEntity = GetPayloadPropertySourceEntity();
            SerializeEntityPropertiesForPowerPayload(propertySourceEntity, properties);
            SerializePowerPropertiesForPowerPayload(this, properties);

            SummonContext context = new()
            {
                Game = Game,
                Target = Owner,
                PowerProto = powerProto,
                RegionId = regionId,
                Position = position,
                TargetPosition = position,
                PowerOwnerId = Owner.Id,
                UltimateOwnerId = ultimateOwnerId,
                OwnerAlliance = Owner.Alliance,
                VariableActivationTime = TimeSpan.Zero,
                EntityAsset = entityAsset,
                Properties = properties,
                MaxNumSimultaneous = 0,
                KillPrevious = false
            };

            SummonEntityContext(manager, context, index);

            int nextIndex = (index + 1) % powerProto.SummonEntityContexts.Length;
            ScheduleSummonEntity(nextIndex);
        }

        private static PowerUseResult SummonEntityContext(EntityManager manager, in SummonContext context, int index)
        {
            var game = context.Game; 
            var powerProto = context.PowerProto;

            if (powerProto.SummonEntityContexts.IsNullOrEmpty()) return PowerUseResult.GenericError;
            int contextLength = powerProto.SummonEntityContexts.Length;

            var owner = manager.GetEntity<WorldEntity>(context.PowerOwnerId);
            WorldEntity ultimateOwner = null;

            if (context.UltimateOwnerId != Entity.InvalidId)
                ultimateOwner = manager.GetEntity<WorldEntity>(context.UltimateOwnerId);

            // get context index
            int contextIndex;
            if (powerProto.SummonRandomSelection)
            {
                contextIndex = game.Random.Next(0, contextLength);
            }
            else if (powerProto.EvalSelectSummonContextIndex != null)
            {
                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                evalContext.Game = game;
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, context.Properties);
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, owner.Properties);
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, ultimateOwner?.Properties);
                evalContext.SetReadOnlyVar_EntityPtr(EvalContext.Var1, owner);
                evalContext.SetReadOnlyVar_EntityPtr(EvalContext.Var2, ultimateOwner);

                contextIndex = Eval.RunInt(powerProto.EvalSelectSummonContextIndex, evalContext);
            }
            else
            {
                contextIndex = index % contextLength;
            }

            if (contextIndex < 0 || contextIndex >= contextLength) return PowerUseResult.GenericError;

            // check summon entity context prototype
            var contextProto = powerProto.SummonEntityContexts[contextIndex];
            if (contextProto == null) return PowerUseResult.GenericError;

            if (contextProto.SummonEntity == PrototypeId.Invalid)
            {
                if (contextProto.SummonEntityRemoval == null) return PowerUseResult.GenericError;
                return PowerUseResult.RestrictiveCondition;
            }

            // check EvalCanSummon
            if (contextProto.EvalCanSummon != null)
            {
                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                evalContext.Game = game;
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, context.Properties);
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, owner.Properties);
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, ultimateOwner?.Properties);
                evalContext.SetReadOnlyVar_EntityPtr(EvalContext.Var1, owner);
                evalContext.SetReadOnlyVar_EntityPtr(EvalContext.Var2, ultimateOwner);

                if (Eval.RunBool(contextProto.EvalCanSummon, evalContext) == false)
                    return PowerUseResult.RestrictiveCondition;
            }

            // entity settings
            using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();

            // get entityRef and summon prototype
            if (ultimateOwner != null)
                settings.EntityRef = ultimateOwner.Properties[PropertyEnum.SummonEntityOverrideRef];

            WorldEntityPrototype summonProto = null;
            if (settings.EntityRef != PrototypeId.Invalid)
            {
                summonProto = GameDatabase.GetPrototype<WorldEntityPrototype>(settings.EntityRef);
            }
            else
            {
                summonProto = powerProto.GetSummonEntity(contextIndex, context.EntityAsset);
                if (summonProto != null)
                    settings.EntityRef = summonProto.DataRef;
            }

            if (summonProto == null) return PowerUseResult.GenericError;

            if (summonProto.IsLiveTuningEnabled() == false) 
                return PowerUseResult.DisabledByLiveTuning;

            // check region
            var regionManager = game.RegionManager;
            if (regionManager == null) return PowerUseResult.GenericError;
            var region = regionManager.GetRegion(context.RegionId);
            if (region == null) return PowerUseResult.GenericError;

            settings.RegionId = region.Id;

            // get region position
            if (IsOwnerCenteredAOE(context.PowerProto) || GetTargetingShape(powerProto) == TargetingShapeType.Self)
            {
                settings.Position = context.Position;
            }
            else
            {
                if (context.Target != null && IsSummoned(powerProto))
                    settings.Position = context.Target.RegionLocation.Position;
                else
                    settings.Position = context.TargetPosition;
            }

            // get offset vector
            if (contextProto.SummonOffsetVector != null) 
            {
                var offsetVector = contextProto.SummonOffsetVector.ToVector3();
                if (owner != null && owner.IsInWorld)
                {
                    var regionLocation = owner.RegionLocation;
                    var transform = Transform3.BuildTransform(regionLocation.Position, regionLocation.Orientation);
                    offsetVector = transform * offsetVector;
                }
                settings.Position += offsetVector;
            }

            // get orientation
            var toTarget = ShouldOrientToTarget(powerProto);
            if (owner != null)
            {
                if (toTarget && owner.IsInWorld)
                    settings.Orientation = owner.Orientation;

                settings.SourceEntityId = owner.Id;
            }

            // fix orientation
            if (toTarget == false)
                settings.Orientation = Orientation.FromDeltaVector2D(context.TargetPosition - context.Position);

            // get source position
            if (powerProto.UseTargetAsSource)
            {
                if (context.Target != null)
                {
                    settings.SourceEntityId = context.Target.Id;
                    settings.SourcePosition = context.Target.RegionLocation.Position;
                }
                else
                {
                    settings.SourceEntityId = Entity.InvalidId;
                    settings.SourcePosition = context.TargetPosition;
                }
            }

            // TODO Fix position

            settings.Lifespan = TimeSpan.FromMilliseconds((int)context.Properties[PropertyEnum.SummonLifespanMS]);

            using PropertyCollection properties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            settings.Properties = properties;

            // TODO CreateEntity

            return PowerUseResult.Success;
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

        private class SummonEvent : CallMethodEventParam1<SummonPower, int>
        {
            protected override CallbackDelegate GetCallback() => (t, p1) => t.SummonEntityIndex(p1);
        }
    }
}
