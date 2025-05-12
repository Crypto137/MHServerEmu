using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Populations;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Regions;

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
        public int MaxSummons;
        public bool KillPrevious;
    }

    public class SummonPower : Power
    {
        public SummonPowerPrototype SummonPowerPrototype => Prototype as SummonPowerPrototype;

        private static readonly Logger Logger = LogManager.CreateLogger();
        private int _totalSummonedEntities;
        private readonly EventPointer<SummonIntervalEvent> _summonIntervalEvent = new();

        public SummonPower(Game game, PrototypeId prototypeDataRef) : base(game, prototypeDataRef)
        {
            _totalSummonedEntities = 0;
        }

        public override bool ApplyPower(PowerApplication powerApplication)
        {
            CheckSummonEntityRemoval();

            if (base.ApplyPower(powerApplication) == false)
                return false;

            ScheduleSummonInterval(1);
            UpdateAndCheckTotalSummonedEntities();

            return true;
        }

        private void CheckSummonEntityRemoval()
        {
            if (Owner == null) return;

            var summonPowerProto = SummonPowerPrototype;
            if (summonPowerProto == null || summonPowerProto.SummonEntityContexts.IsNullOrEmpty()) return;

            var inventory = Owner.SummonedInventory;
            if (inventory == null) return;

            List<WorldEntity> killList = [];

            foreach (var context in summonPowerProto.SummonEntityContexts)
            {
                if (context == null) return;

                var removalProto = context.SummonEntityRemoval;
                if (removalProto == null) continue;

                bool removalKeywords = removalProto.Keywords.HasValue();
                bool removalPowers = removalProto.FromPowers.HasValue();

                if (removalKeywords == false && removalPowers == false) continue;

                killList.Clear();

                foreach (var summoned in new SummonedEntityIterator(Owner))
                {
                    if (summoned.IsDead) continue;

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
            PrototypeId creatorPowerRef = summoned.Properties[PropertyEnum.CreatorPowerPrototype];

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

        protected override bool EndPowerInternal(EndPowerFlags flags)
        {
            base.EndPowerInternal(flags);

            var powerProto = SummonPowerPrototype;
            if (powerProto == null) return false;

            bool goodEnd = (flags & (EndPowerFlags.ExplicitCancel | EndPowerFlags.ChanneledLoopEnd | EndPowerFlags.PowerEventAction)) != 0;
            bool badEnd = (flags & (EndPowerFlags.Force | EndPowerFlags.ExitWorld | EndPowerFlags.Unassign)) != 0;
            if (badEnd || (goodEnd && powerProto.SummonsLiveWhilePowerActive))
                if (powerProto.TrackInInventory)
                    DestroySummoned(flags.HasFlag(EndPowerFlags.ExitWorld));

            Game.GameEventScheduler.CancelEvent(_summonIntervalEvent);
            return true;
        }

        protected override void OnEndChannelingPhase()
        {
            base.OnEndChannelingPhase();

            if (SummonPowerPrototype?.SummonsLiveWhilePowerActive == true)
                DestroySummoned(false);
        }

        protected override bool SetToggleState(bool value, bool doNotStartCooldown = false)
        {
            base.SetToggleState(value, doNotStartCooldown);

            if (IsToggledOn() == false)
            {
                if (SummonPowerPrototype?.SummonsLiveWhilePowerActive == true)
                    DestroySummoned(false);
            }

            return true;
        }

        protected override PowerUseResult RunExtraActivation(ref PowerActivationSettings settings)
        {
            var powerProto = SummonPowerPrototype;
            if (powerProto == null) return PowerUseResult.ExtraActivationFailed;

            if (powerProto.ExtraActivation is ExtraActivateOnSubsequentPrototype extraActivate) 
                if (extraActivate.ExtraActivateEffect == SubsequentActivateType.DestroySummonedEntity)
                    if (DestroySummoned(false) == 0)
                        return PowerUseResult.ExtraActivationFailed;

            return base.RunExtraActivation(ref settings);
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
                MaxSummons = maxSummons,
                KillPrevious = killPrevious
            };

            for (int i = 0; i < summonNum; i++)
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
            WorldEntity propertySourceEntity = GetPayloadPropertySourceEntity(ultimateOwner);
            SerializeEntityPropertiesForPowerPayload(propertySourceEntity, properties);
            SerializePowerPropertiesForPowerPayload(this, properties);

            properties[PropertyEnum.IsTeamUpAwaySource] = IsTeamUpPassivePowerWhileAway;

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
                MaxSummons = 0,
                KillPrevious = false
            };

            SummonEntityContext(manager, context, index);

            int nextIndex = (index + 1) % powerProto.SummonEntityContexts.Length;
            ScheduleSummonInterval(nextIndex);
        }

        private static PowerUseResult SummonEntityContext(EntityManager manager, SummonContext context, int index)
        {
            var game = context.Game;
            var powerProto = context.PowerProto;

            if (powerProto.SummonEntityContexts.IsNullOrEmpty()) return PowerUseResult.GenericError;
            int contextLength = powerProto.SummonEntityContexts.Length;

            var owner = manager.GetEntity<WorldEntity>(context.PowerOwnerId);
            WorldEntity ultimateOwner = null;

            if (context.UltimateOwnerId != Entity.InvalidId)
                ultimateOwner = manager.GetEntity<WorldEntity>(context.UltimateOwnerId);

            var target = context.Target;

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
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, owner?.Properties);
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
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, owner?.Properties);
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
                if (target != null && IsSummoned(powerProto))
                    settings.Position = target.RegionLocation.Position;
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
                if (target != null)
                {
                    settings.SourceEntityId = target.Id;
                    settings.SourcePosition = target.RegionLocation.Position;
                }
                else
                {
                    settings.SourceEntityId = Entity.InvalidId;
                    settings.SourcePosition = context.TargetPosition;
                }
            }

            // get summon positions
            var orientation = settings.Orientation;
            var summonPositions = GetSummonPositions(owner, powerProto, summonProto, contextProto, region, context.Properties, 
                settings.Position, ref orientation);
            if (summonPositions == null) return PowerUseResult.GenericError;
            settings.Orientation = orientation;

            // hotspot size
            float boundsScale = GetAOESizePctModifier(powerProto, ultimateOwner?.Properties);
            if (boundsScale != 1.0f && powerProto.IsHotspotSummoningPower())
                settings.BoundsScaleOverride = boundsScale;

            // set OptionFlags
            if (contextProto.SnapToFloor)
                settings.OptionFlags |= EntitySettingsOptionFlags.HasOverrideSnapToFloor | EntitySettingsOptionFlags.OverrideSnapToFloorValue;

            if (contextProto.HideEntityOnSummon)
                settings.OptionFlags |= EntitySettingsOptionFlags.IsClientEntityHidden;

            // set lifespan
            settings.Lifespan = TimeSpan.FromMilliseconds((int)context.Properties[PropertyEnum.SummonLifespanMS]);
            if (settings.Lifespan > TimeSpan.Zero && powerProto.OmniDurationBonusExclude == false)
            {
                WorldEntity omni = null;
                if (owner != null && owner.Properties.HasProperty(PropertyEnum.OmniDurationBonusPct))
                {
                    omni = owner;
                }
                else if (ultimateOwner != null && (owner == null || owner.IsTeamUpAgent == false))
                {
                    omni = ultimateOwner;
                }

                if (omni != null)
                {
                    settings.Lifespan *= 1.0f + (float)omni.Properties[PropertyEnum.OmniDurationBonusPct];
                    settings.Lifespan = Clock.Max(settings.Lifespan, TimeSpan.FromMilliseconds(1));
                }

                if (powerProto.IsPetSummoningPower())
                    settings.Lifespan *= 1.0f + (float)context.Properties[PropertyEnum.PetLifetimeChangePct];
            }

            // set inventory
            if (powerProto.TrackInInventory)
            {
                Inventory inventory = null;
                ulong conteinerId = Entity.InvalidId;
                if (powerProto.AttachSummonsToTarget)
                {
                    if (target != null)
                    {
                        conteinerId = target.Id;
                        inventory = target.SummonedInventory;
                    }
                    else return PowerUseResult.TargetIsMissing;
                }
                else
                {
                    if (owner != null)
                    {
                        conteinerId = owner.Id;
                        inventory = owner.SummonedInventory;
                    }
                }

                if (inventory == null || conteinerId == Entity.InvalidId) return PowerUseResult.GenericError;

                settings.InventoryLocation = new(conteinerId, inventory.PrototypeDataRef, Inventory.InvalidSlot);
            }

            // set properties
            using PropertyCollection properties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            SetSummonProperties(properties, context, summonProto, contextProto, owner, ultimateOwner, contextIndex);

            // check EvalOnSummon
            if (contextProto.EvalOnSummon.HasValue())
            {
                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                evalContext.Game = game;
                evalContext.SetVar_PropertyCollectionPtr(EvalContext.Default, properties);
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, ultimateOwner?.Properties);
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, context.Properties);
                evalContext.SetReadOnlyVar_EntityPtr(EvalContext.Var1, ultimateOwner);

                foreach (var evalProto in contextProto.EvalOnSummon)
                    if (Eval.RunBool(evalProto, evalContext) == false)
                        return PowerUseResult.GenericError;
            }

            settings.Properties = properties;

            // kill previous
            int summonsCount = summonPositions.Count;
            if (summonsCount > 1)
            {
                int maxSummons = context.MaxSummons;
                if (maxSummons > 0 && powerProto.KillPreviousSummons)
                {
                    int count = GetExistingSummonedEntitiesCount(owner, powerProto);
                    context.KillPrevious = count >= maxSummons;
                }
            }

            if (owner != null && context.KillPrevious)
                KillPreviousSummons(owner, powerProto, summonsCount);

            // create entity
            var populationManager = region.PopulationManager;
            var spawnGroupId = context.Properties[PropertyEnum.SpawnGroupId];
            foreach (var summonPos in summonPositions)
            {
                settings.Position = summonPos;

                SpawnGroup group = null;
                if (owner != null)
                {
                    group = owner.SpawnGroup;
                }
                else if (powerProto.SummonAsPopulation)
                {
                    group = populationManager.GetSpawnGroup(spawnGroupId);
                }

                if (group != null && powerProto.SummonAsPopulation)
                {
                    var spec = populationManager.CreateSpawnSpec(group);
                    spec.EntityRef = settings.EntityRef;
                    spec.Transform = Transform3.BuildTransform(settings.Position - group.Transform.Translation, settings.Orientation);
                    spec.Properties.FlattenCopyFrom(settings.Properties, false);
                    spec.Spawn();
                }
                else
                {
                    if (manager.CreateEntity(settings) is not WorldEntity summoned) continue;

                    if (powerProto.AttachSummonsToCaster || powerProto.AttachSummonsToTarget)
                        summoned.AttachToEntity(target);

                    if (contextProto.VisibleWhileAttached == false)
                        summoned.SetVisible(false);

                    if (owner != null && owner.IsInWorld)
                    {
                        var power = owner.GetPower(powerProto.DataRef);
                        power?.HandleTriggerPowerEventOnSummonEntity(summoned.Id);

                        if (powerProto.IsPetSummoningPower())
                            owner.TryActivateOnSummonPetProcs(summoned);
                    }
                }

                settings.Id = Entity.InvalidId;
                settings.DbGuid = 0;
            }

            return PowerUseResult.Success;
        }

        private int DestroySummoned(bool exitWorld)
        {
            int count = 0;
            var powerProto = SummonPowerPrototype;

            var inventory = Owner.SummonedInventory;
            if (inventory == null) return count;

            List<WorldEntity> summons = ListPool<WorldEntity>.Instance.Get();

            foreach (var summoned in new SummonedEntityIterator(Owner))
            {
                if (summoned.IsDead) continue;

                PrototypeId powerRef = summoned.Properties[PropertyEnum.CreatorPowerPrototype];
                if (powerRef == powerProto.DataRef)
                    summons.Add(summoned);
            }

            foreach (var summoned in summons)
            {
                if (exitWorld && summoned.Properties[PropertyEnum.SummonedEntityIsRegionPersisted])
                {
                    summoned.ExitWorld();
                }
                else
                {
                    if (exitWorld)
                        summoned.Destroy();
                    else
                        KillSummoned(summoned, Owner);

                    count++;
                }
            }

            ListPool<WorldEntity>.Instance.Return(summons);

            return count;
        }

        private static void KillPreviousSummons(WorldEntity owner, SummonPowerPrototype powerProto, int summonsCount)
        {
            var inventory = owner.SummonedInventory;
            if (inventory == null) return;
            
            List<WorldEntity> summons = ListPool<WorldEntity>.Instance.Get();

            foreach (var summoned in new SummonedEntityIterator(owner))
            {
                if (summoned.IsDead) continue;

                PrototypeId powerRef = summoned.Properties[PropertyEnum.CreatorPowerPrototype];
                if (powerRef == powerProto.DataRef || powerProto.InSummonMaxCountWithOthers(powerRef))
                    summons.Add(summoned);
            }
            
            summons.Sort((a, b) => a.Id.CompareTo(b.Id));
            if (summons.Count > summonsCount)
                summons = summons.Take(summonsCount).ToList();

            foreach (var summoned in summons)
                KillSummoned(summoned, owner);

            ListPool<WorldEntity>.Instance.Return(summons);
        }

        private static List<Vector3> GetSummonPositions(WorldEntity owner, SummonPowerPrototype powerProto, WorldEntityPrototype summonProto, SummonEntityContextPrototype contextProto, 
            Region region, PropertyCollection properties, Vector3 position, ref Orientation orientation)
        {
            var boundsProto = summonProto.Bounds;
            if (boundsProto == null || position == Vector3.Zero) return null;

            float radius = 0.0f;
            var resultPosition = position;

            Bounds bounds = new(boundsProto, position);

            if (bounds.CollisionType == BoundsCollisionType.Blocking)
            {
                switch (bounds.Geometry)
                {
                    case GeometryType.OBB:
                    case GeometryType.AABB:
                    case GeometryType.Capsule:
                    case GeometryType.Sphere:
                        radius = bounds.Radius;
                        break;
                    default:
                        return null;
                }
            }

            var pathFlags = Region.GetPathFlagsForEntity(summonProto);
            if (contextProto.PathFilterOverride != LocomotorMethod.None)
                pathFlags = Locomotor.GetPathFlags(contextProto.PathFilterOverride);

            var posFlags = PositionCheckFlags.None;
            var blockingFlags = BlockingCheckFlags.None;

            if ((bounds.CollisionType == BoundsCollisionType.Blocking && contextProto.IgnoreBlockingOnSpawn == false) 
                || contextProto.ForceBlockingCollisionForSpawn)
            {
                posFlags = PositionCheckFlags.CanBeBlockedEntity | PositionCheckFlags.CanPathToEntities | PositionCheckFlags.PreferNoEntity;
                if (contextProto.ForceBlockingCollisionForSpawn)
                    blockingFlags = BlockingCheckFlags.CheckSpawns | BlockingCheckFlags.CheckSelf;
            }

            if (contextProto.RandomSpawnLocation)
            {
                float summonRadius = contextProto.SummonRadius;
                float maxRadius = Segment.IsNearZero(summonRadius) ? powerProto.Radius : summonRadius;
                if (region.ChooseRandomPositionNearPoint(bounds, pathFlags, posFlags, blockingFlags, 0, maxRadius, out resultPosition) == false)
                    return null;
            }
            else if (pathFlags != PathFlags.None && region.IsLocationClear(bounds, pathFlags, posFlags, blockingFlags) == false)
            {
                if (contextProto.EnforceExactSummonPos == false)
                {
                    bool foundPosition = false;
                    if (owner != null)
                    {
                        var ownerPosition = owner.RegionLocation.Position;
                        foundPosition = region.NaviMesh.FindPointOnLineToOccupy(ref resultPosition, ownerPosition, bounds.Center,
                            Vector3.Distance2D(ownerPosition, bounds.Center), bounds, pathFlags, blockingFlags, true) != PointOnLineResult.Failed;
                    }

                    if (foundPosition == false)
                    {
                        float maxRadius = MathF.Max(radius * 2.0f, contextProto.SummonRadius);
                        if (region.ChooseRandomPositionNearPoint(bounds, pathFlags, posFlags, blockingFlags, 0, maxRadius, out resultPosition) == false)
                            return null;
                    }
                }
                else return null;
            }

            if (region.GetCellAtPosition(resultPosition) == null) return null;

            float summonWidthMax = properties[PropertyEnum.SummonWidthMax];
            if (summonWidthMax > 0.0f)
            {
                bounds.Center = resultPosition;
                return GenerateSummonPositions(bounds, summonWidthMax, ref orientation, contextProto.SummonOffsetAngle, region, pathFlags, posFlags);
            }

            if (Segment.IsNearZero(contextProto.SummonOffsetAngle) == false)
                orientation.Yaw += MathHelper.ToRadians(contextProto.SummonOffsetAngle);

            List<Vector3> positionList = [];
            positionList.Add(resultPosition);
            return positionList;
        }

        private static List<Vector3> GenerateSummonPositions(Bounds bounds, float summonWidthMax, ref Orientation orientation, float summonOffsetAngle, 
            Region region, PathFlags pathFlags, PositionCheckFlags posFlags)
        {
            var position = bounds.Center;
            var radius = bounds.GetRadius();
            var offsetAngle = MathHelper.ToRadians(summonOffsetAngle);
            var angleDir = orientation.GetMatrix3() * Vector3.Forward;
            var rotate = Vector3.AxisAngleRotate(angleDir, Vector3.Up, offsetAngle);
            var halfWidth = summonWidthMax / 2.0f;

            Vector3? sweetL = position - rotate * halfWidth;
            Vector3? sweetR = position + rotate * halfWidth;
            Vector3? normal = null;

            var navi = region.NaviMesh;
            navi.Sweep(position, sweetL.Value, radius, pathFlags, ref sweetL, ref normal);
            navi.Sweep(position, sweetR.Value, radius, pathFlags, ref sweetR, ref normal);

            var dir = sweetR.Value - sweetL.Value;
            orientation.Yaw = MathF.Atan2(dir.Y, dir.X);

            List<Vector3> resultPositions = [];

            var centerPos = sweetL.Value + dir / 2.0f;
            resultPositions.Add(centerPos);

            var length = Vector3.Distance2D(sweetL.Value, sweetR.Value);
            var width = radius * 2.0f;

            bool blockedL = false;
            bool blockedR = false;
            int steps = ((int)MathF.Floor(length / width) - 1) / 2;

            for (int i = 0; i < steps; i++)
            {
                var distance = (i + 1) * width;
                var leftPos = centerPos + rotate * distance;
                var rightPos = centerPos - rotate * distance;

                bounds.Center = leftPos;
                if (blockedL == false)
                {
                    if (region.IsLocationClear(bounds, pathFlags, posFlags) == true)
                        resultPositions.Add(leftPos);
                    else
                        blockedL = true;
                }

                bounds.Center = rightPos;
                if (blockedR == false)
                {
                    if (region.IsLocationClear(bounds, pathFlags, posFlags) == true)
                        resultPositions.Add(rightPos);
                    else
                        blockedR = true;
                }
            }

            return resultPositions;
        }

        private static void SetSummonProperties(PropertyCollection properties, SummonContext context, WorldEntityPrototype summonProto, SummonEntityContextPrototype contextProto, 
            WorldEntity owner, WorldEntity ultimateOwner, int contextIndex)
        {
            var powerProto = context.PowerProto;

            properties[PropertyEnum.SummonedByPower] = true;

            if (powerProto.IsPetSummoningPower())
            {
                properties.AdjustProperty(context.Properties[PropertyEnum.PetHealthPctBonus], PropertyEnum.PetHealthPctBonus);
                properties.AdjustProperty(context.Properties[PropertyEnum.PetDamagePctBonus], PropertyEnum.PetDamagePctBonus);
            }

            if (powerProto.PersistAcrossRegions)
                properties[PropertyEnum.SummonedEntityIsRegionPersisted] = true;

            properties[PropertyEnum.SummonContextIndex] = contextIndex;

            if (owner != null && owner.IsDead == false && owner is not Missile)
                properties[PropertyEnum.PowerUserOverrideID] = context.PowerOwnerId;
            else
                properties[PropertyEnum.PowerUserOverrideID] = context.UltimateOwnerId;

            if (ultimateOwner != null)
            {
                properties[PropertyEnum.CreatorEntityAssetRefBase] = ultimateOwner.GetOriginalWorldAsset();
                properties[PropertyEnum.CreatorEntityAssetRefCurrent] = ultimateOwner.GetEntityWorldAsset();
            }

            if (summonProto is HotspotPrototype)
                properties[PropertyEnum.CreatorRank] = context.Properties[PropertyEnum.Rank];

            properties.CopyProperty(context.Properties, PropertyEnum.DifficultyTier);
            properties.CopyProperty(context.Properties, PropertyEnum.DangerRoomScenarioItemDbGuid);
            properties.CopyProperty(context.Properties, PropertyEnum.ItemRarity);

            properties.CopyProperty(context.Properties, PropertyEnum.ParentSpawnerGroupId);
            properties.CopyProperty(context.Properties, PropertyEnum.SpawnGroupId);
            properties.CopyProperty(context.Properties, PropertyEnum.IsTeamUpAwaySource);

            properties[PropertyEnum.VariableActivationTimeMS] = context.VariableActivationTime;
            properties.CopyProperty(context.Properties, PropertyEnum.VariableActivationTimePct);

            if (contextProto.TransferMissionPrototype)
                properties[PropertyEnum.MissionPrototype] = context.Properties[PropertyEnum.MissionPrototype];
           
            if (contextProto.CopyOwnerProperties)
                SerializePropertiesForSummonEntity(context.Properties, properties);
            else
                CopyPowerIndexProperties(context.Properties, properties);

            properties[PropertyEnum.CreatorPowerPrototype] = powerProto.DataRef;
            properties[PropertyEnum.DetachOnContainerDestroyed] = true;

            var regionManager = context.Game.RegionManager;
            if (regionManager != null)
            {
                var region = regionManager.GetRegion(context.RegionId);
                properties[PropertyEnum.NumOfPlayersInSameRegion] = ComputeNearbyPlayers(region, context.TargetPosition, 1);
            }

            if (summonProto.IsVacuumable == false && context.OwnerAlliance != null)
                properties[PropertyEnum.AllianceOverride] = context.OwnerAlliance.DataRef;

            if (summonProto.ModifiersGuaranteed.HasValue())
                foreach (var boostRef in summonProto.ModifiersGuaranteed) 
                    properties[PropertyEnum.EnemyBoost, boostRef] = true;

            if (summonProto.Properties != null && summonProto.Properties[PropertyEnum.RestrictedToPlayer])
            {
                var player = owner?.GetOwnerOfType<Player>();
                if (player == null) return;
                properties[PropertyEnum.RestrictedToPlayerGuid] = player.DatabaseUniqueId;
            }

            if (owner != null)
            {
                float damageRatingBonusMvmt = (float)owner.Properties[PropertyEnum.DamageRatingBonusMvmtSpeed] * MathF.Max(0.0f, owner.BonusMovementSpeed);
                properties.AdjustProperty(damageRatingBonusMvmt, PropertyEnum.DamageRating);
            }
        }

        private void ScheduleSummonInterval(int index)
        {
            var summonPowerProto = SummonPowerPrototype;
            if (summonPowerProto.SummonIntervalMS <= 0) return;

            var scheduler = Game?.GameEventScheduler;
            if (scheduler == null) return;

            if (_summonIntervalEvent.IsValid) scheduler.CancelEvent(_summonIntervalEvent);

            var timeOffset = TimeSpan.FromMilliseconds(summonPowerProto.SummonIntervalMS);
            scheduler.ScheduleEvent(_summonIntervalEvent, timeOffset, _pendingEvents);
            _summonIntervalEvent.Get().Initialize(this, index);
        }

        private class SummonIntervalEvent : CallMethodEventParam1<SummonPower, int>
        {
            protected override CallbackDelegate GetCallback() => (t, p1) => t.SummonEntityIndex(p1);
        }
    }
}
