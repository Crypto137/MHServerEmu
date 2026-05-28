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
    public class SummonPower : Power, IPowerPayloadDeliverCallback
    {
        private readonly EventPointer<SummonIntervalEvent> _summonIntervalEvent = new();

        private int _totalSummonedEntities = 0;

        public SummonPowerPrototype SummonPowerPrototype { get => Prototype as SummonPowerPrototype; }

        public SummonPower(Game game, PrototypeId prototypeDataRef) : base(game, prototypeDataRef)
        {
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
            if (!Verify.IsNotNull(Owner)) return;

            SummonPowerPrototype summonPowerProto = SummonPowerPrototype;
            if (!Verify.IsNotNull(summonPowerProto)) return;
            if (!Verify.IsTrue(summonPowerProto.SummonEntityContexts.HasValue())) return;

            Inventory inventory = Owner.SummonedInventory;
            if (inventory == null)
                return;

            using var killListHandle = ListPool<WorldEntity>.Instance.Get(out List<WorldEntity> killList);

            foreach (SummonEntityContextPrototype context in summonPowerProto.SummonEntityContexts)
            {
                if (!Verify.IsNotNull(context)) return;

                SummonRemovalPrototype removalProto = context.SummonEntityRemoval;
                if (removalProto == null)
                    continue;

                bool removalKeywords = removalProto.Keywords.HasValue();
                bool removalPowers = removalProto.FromPowers.HasValue();

                if (removalKeywords == false && removalPowers == false) continue;

                killList.Clear();

                foreach (WorldEntity summoned in new SummonedEntityIterator(Owner))
                {
                    if (summoned.IsDead)
                        continue;

                    bool found = false;
                    if (removalKeywords)
                        found |= SummonedHasKeywords(summoned, removalProto.Keywords);

                    if (found == false && removalPowers)
                        found |= SummonedHasCreatorPower(summoned, removalProto.FromPowers);

                    if (found)
                        killList.Add(summoned);
                }

                foreach (WorldEntity summoned in killList)
                    KillSummoned(summoned, Owner);
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
                if (summoned.IsSummonedPet())
                    owner?.TryActivateOnPetDeathProcs(summoned);

                summoned.Destroy();
            }
        }

        private static bool SummonedHasCreatorPower(WorldEntity summoned, PrototypeId[] creatorPowers)
        {
            PrototypeId creatorPowerRef = summoned.Properties[PropertyEnum.CreatorPowerPrototype];

            foreach (PrototypeId powerRef in creatorPowers)
            {
                if (creatorPowerRef == powerRef)
                    return true;
            }

            return false;
        }

        private static bool SummonedHasKeywords(WorldEntity summoned, PrototypeId[] keywords)
        {
            foreach (PrototypeId keywordRef in keywords)
            {
                if (summoned.HasKeyword(keywordRef))
                    return true;
            }

            return false;
        }

        protected override void EndPowerInternal(EndPowerFlags flags)
        {
            base.EndPowerInternal(flags);

            SummonPowerPrototype powerProto = SummonPowerPrototype;
            if (!Verify.IsNotNull(powerProto)) return;

            bool normalEnd = (flags & (EndPowerFlags.ExplicitCancel | EndPowerFlags.ChanneledLoopEnd | EndPowerFlags.PowerEventAction)) != 0;
            bool forcedEnd = (flags & (EndPowerFlags.Force | EndPowerFlags.ExitWorld | EndPowerFlags.Unassign)) != 0;

            if (forcedEnd || (normalEnd && powerProto.SummonsLiveWhilePowerActive))
            {
                if (powerProto.TrackInInventory)
                    DestroySummoned(flags.HasFlag(EndPowerFlags.ExitWorld));
            }

            Game.GameEventScheduler.CancelEvent(_summonIntervalEvent);
        }

        protected override void OnEndChannelingPhase()
        {
            base.OnEndChannelingPhase();

            SummonPowerPrototype powerProto = SummonPowerPrototype;
            if (!Verify.IsNotNull(powerProto)) return;

            if (powerProto.SummonsLiveWhilePowerActive)
                DestroySummoned(false);
        }

        protected override void SetToggleState(bool value, bool doNotStartCooldown = false)
        {
            base.SetToggleState(value, doNotStartCooldown);

            if (IsToggledOn() == false)
            {
                SummonPowerPrototype powerProto = SummonPowerPrototype;
                if (!Verify.IsNotNull(powerProto)) return;

                if (powerProto.SummonsLiveWhilePowerActive)
                    DestroySummoned(false);
            }
        }

        protected override PowerUseResult RunExtraActivation(ref PowerActivationSettings settings)
        {
            SummonPowerPrototype powerProto = SummonPowerPrototype;
            if (!Verify.IsNotNull(powerProto)) return PowerUseResult.ExtraActivationFailed;

            if (powerProto.ExtraActivation is ExtraActivateOnSubsequentPrototype extraActivateOnSubsequent &&
                extraActivateOnSubsequent.ExtraActivateEffect == SubsequentActivateType.DestroySummonedEntity)
            {
                if (DestroySummoned(false) == 0)
                    return PowerUseResult.ExtraActivationFailed;
            }

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
            SummonPowerPrototype prototype = SummonPowerPrototype;
            if (!Verify.IsNotNull(prototype)) return PowerUseResult.GenericError;

            if (prototype.TrackInInventory && prototype.KillPreviousSummons == false)
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
            SummonPowerPrototype proto = SummonPowerPrototype;
            if (!Verify.IsNotNull(proto)) return;

            int maxNumSummons = proto.GetMaxNumSummons(Properties);
            if (maxNumSummons > 0)
            {
                if (_totalSummonedEntities < maxNumSummons)
                    _totalSummonedEntities++;

                if (proto.SummonMaxReachedDestroyOwner && _totalSummonedEntities >= maxNumSummons)
                {
                    WorldEntity owner = Owner;
                    if (Verify.IsNotNull(owner))
                        owner.Kill();
                }
            }
        }

        public override void OnPayloadInit(PowerPayload payload)
        {
            payload.DeliverCallback = this;

            if (Owner != null && Owner.Properties.HasProperty(PropertyEnum.ParentSpawnerGroupId))
                payload.Properties[PropertyEnum.ParentSpawnerGroupId] = Owner.Properties[PropertyEnum.ParentSpawnerGroupId];
        }

        public void OnDeliverPayload(PowerPayload payload)
        {
            Game game = payload.Game;
            if (!Verify.IsNotNull(game)) return;

            EntityManager entityManager = game.EntityManager;
            if (entityManager == null) return;

            if (payload.PowerPrototype is not SummonPowerPrototype powerProto) return;

            if (powerProto.SummonsLiveWhilePowerActive)
            {
                WorldEntity owner = entityManager.GetEntity<WorldEntity>(payload.PowerOwnerId);
                if (owner == null || owner.IsInWorld == false)
                    return;

                Power power = owner.GetPower(powerProto.DataRef);
                if (power == null || power.IsActive == false)
                    return;
            }

            if (powerProto.AttachSummonsToTarget || powerProto.UseTargetAsSource)
            {
                using var targetListHandle = ListPool<WorldEntity>.Instance.Get(out List<WorldEntity> targetList);
                GetTargets(targetList, payload);

                foreach (WorldEntity target in targetList)
                    SummonPayloadEntity(entityManager, powerProto, payload, target);

                if (targetList.Count == 0 && powerProto.UseTargetAsSource)
                    SummonPayloadEntity(entityManager, powerProto, payload, null);
            }
            else
            {
                WorldEntity target = null;
                if (powerProto.AttachSummonsToCaster)
                    target = entityManager.GetEntity<WorldEntity>(payload.PowerOwnerId);

                SummonPayloadEntity(entityManager, powerProto, payload, target);
            }
        }

        private static void SummonPayloadEntity(EntityManager entityManager, SummonPowerPrototype powerProto, PowerPayload payload, WorldEntity target)
        {
            PropertyCollection payloadProperties = payload.Properties;

            int summonNum = payloadProperties[PropertyEnum.SummonNumPerActivation];
            if (!Verify.IsTrue(summonNum >= 1)) return;

            int maxSummons = powerProto.GetMaxNumSimultaneousSummons(payloadProperties);
            if (!Verify.IsTrue(maxSummons == 0 || (summonNum <= maxSummons))) return;

            if (!Verify.IsNotNull(payload.OwnerAlliance)) return;

            if (!Verify.IsTrue(payload.PowerOwnerId != Entity.InvalidId)) return;

            if (payload.PowerOwnerId == Entity.InvalidId) return;
            WorldEntity owner = entityManager.GetEntity<WorldEntity>(payload.PowerOwnerId);

            int count = 0;

            if (powerProto.TrackInInventory)
                count = GetExistingSummonedEntitiesCount(owner, powerProto);

            bool killPrevious = powerProto.KillPreviousSummons;

            Verify.IsTrue(killPrevious || maxSummons <= 0 || count < maxSummons, $"Summoning more than allowed {count} of {maxSummons}");

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

                PowerUseResult result = SummonEntity(entityManager, ref context, i);
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

        private void SummonIntervalCallback(int index)
        {
            if (!Verify.IsTrue(index >= 0)) return;

            if (CanSummonEntity() != PowerUseResult.Success)
                return;

            if (!Verify.IsNotNull(Owner)) return;
            if (!Verify.IsTrue(Owner.IsInWorld)) return;

            EntityManager entityManager = Game?.EntityManager;
            if (!Verify.IsNotNull(entityManager)) return;

            SummonPowerPrototype powerProto = SummonPowerPrototype;
            if (!Verify.IsNotNull(powerProto)) return;
            if (!Verify.IsTrue(powerProto.SummonEntityContexts.HasValue())) return;

            ulong regionId = Owner.RegionLocation.RegionId;
            Vector3 position = Owner.RegionLocation.Position;

            ulong ultimateOwnerId = Entity.InvalidId;
            AssetId entityAsset;

            WorldEntity ultimateOwner = GetUltimateOwner();
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

            SummonEntity(entityManager, ref context, index);

            int nextIndex = (index + 1) % powerProto.SummonEntityContexts.Length;
            ScheduleSummonInterval(nextIndex);
        }

        private static PowerUseResult SummonEntity(EntityManager entityManager, ref SummonContext context, int index)
        {
            Game game = context.Game;
            SummonPowerPrototype powerProto = context.PowerProto;

            if (!Verify.IsTrue(powerProto.SummonEntityContexts.HasValue())) return PowerUseResult.GenericError;

            int contextLength = powerProto.SummonEntityContexts.Length;

            WorldEntity owner = entityManager.GetEntity<WorldEntity>(context.PowerOwnerId);
            WorldEntity ultimateOwner = null;

            if (context.UltimateOwnerId != Entity.InvalidId)
                ultimateOwner = entityManager.GetEntity<WorldEntity>(context.UltimateOwnerId);

            WorldEntity target = context.Target;

            // NOTE: This is similar to CreateMissileLooper() in MissilePower.
            int contextIndex;
            SummonEntityContextPrototype contextProto;

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

            if (!Verify.IsTrue(contextIndex >= 0 && contextIndex < contextLength)) return PowerUseResult.GenericError;

            contextProto = powerProto.SummonEntityContexts[contextIndex];
            if (!Verify.IsNotNull(contextProto)) return PowerUseResult.GenericError;

            if (contextProto.SummonEntity == PrototypeId.Invalid)
            {
                if (!Verify.IsNotNull(contextProto.SummonEntityRemoval)) return PowerUseResult.GenericError;
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

            // Start populating EntitySettings
            using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();

            // EntityRef
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

            if (!Verify.IsNotNull(summonProto)) return PowerUseResult.GenericError;

            if (summonProto.IsLiveTuningEnabled() == false)
                return PowerUseResult.DisabledByLiveTuning;

            // Region
            RegionManager regionManager = game.RegionManager;
            if (!Verify.IsNotNull(regionManager)) return PowerUseResult.GenericError;

            Region region = regionManager.GetRegion(context.RegionId);
            if (!Verify.IsNotNull(region)) return PowerUseResult.GenericError;

            settings.RegionId = region.Id;

            // Position
            if (IsOwnerCenteredAOE(context.PowerProto) || GetTargetingShape(powerProto) == TargetingShapeType.Self)
            {
                settings.Position = context.Position;
            }
            else
            {
                if (target != null && TargetsSummonedInventory(powerProto))
                    settings.Position = target.RegionLocation.Position;
                else
                    settings.Position = context.TargetPosition;
            }

            // Apply offset vector
            if (contextProto.SummonOffsetVector != null)
            {
                Vector3 offsetVector = contextProto.SummonOffsetVector.ToVector3();

                if (owner != null && owner.IsInWorld)
                {
                    ref RegionLocation regionLocation = ref owner.RegionLocation;
                    Transform3 transform = Transform3.BuildTransform(regionLocation.Position, regionLocation.Orientation);
                    offsetVector = transform * offsetVector;
                }

                settings.Position += offsetVector;
            }

            // Orientation
            bool orientToTarget = ShouldOrientToTarget(powerProto);
            if (owner != null)
            {
                if (orientToTarget && owner.IsInWorld)
                    settings.Orientation = owner.Orientation;

                settings.SourceEntityId = owner.Id;
            }

            if (orientToTarget == false)
                settings.Orientation = Orientation.FromDeltaVector2D(context.TargetPosition - context.Position);

            // Source position
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

            // Summon positions
            using var summonPositionsHandle = ListPool<Vector3>.Instance.Get(out List<Vector3> summonPositions);

            Orientation orientation = settings.Orientation; 
            bool hasSummonPositions = GetSummonPositions(owner, powerProto, summonProto, contextProto, region, context.Properties,
                settings.Position, ref orientation, summonPositions);

            if (!Verify.IsTrue(hasSummonPositions)) return PowerUseResult.GenericError;

            settings.Orientation = orientation; // TODO: pass by ref to GetSummonPositions directly if we change EntitySettings to struct?

            // Hotspot size
            float boundsScale = GetAOESizePctModifier(powerProto, ultimateOwner?.Properties);
            if (boundsScale != 1.0f && powerProto.IsHotspotSummoningPower())
                settings.BoundsScaleOverride = boundsScale;

            // OptionFlags
            if (contextProto.SnapToFloor)
                settings.OptionFlags |= EntitySettingsOptionFlags.HasOverrideSnapToFloor | EntitySettingsOptionFlags.OverrideSnapToFloorValue;

            if (contextProto.HideEntityOnSummon)
                settings.OptionFlags |= EntitySettingsOptionFlags.IsClientEntityHidden;

            // Lifespan
            settings.Lifespan = TimeSpan.FromMilliseconds((int)context.Properties[PropertyEnum.SummonLifespanMS]);
            if (settings.Lifespan > TimeSpan.Zero && powerProto.OmniDurationBonusExclude == false)
            {
                WorldEntity omniDurationBonusSource = null;
                if (owner != null && owner.Properties.HasProperty(PropertyEnum.OmniDurationBonusPct))
                {
                    omniDurationBonusSource = owner;
                }
                else if (ultimateOwner != null && (owner == null || owner.IsTeamUpAgent == false))
                {
                    omniDurationBonusSource = ultimateOwner;
                }

                if (omniDurationBonusSource != null)
                {
                    settings.Lifespan *= 1.0f + (float)omniDurationBonusSource.Properties[PropertyEnum.OmniDurationBonusPct];
                    settings.Lifespan = Clock.Max(settings.Lifespan, TimeSpan.FromMilliseconds(1));
                }

                if (powerProto.IsPetSummoningPower())
                    settings.Lifespan *= 1.0f + (float)context.Properties[PropertyEnum.PetLifetimeChangePct];
            }

            // InventoryLocation
            if (powerProto.TrackInInventory)
            {
                ulong containerId;
                Inventory inventory;

                if (powerProto.AttachSummonsToTarget)
                {
                    if (target == null)
                        return PowerUseResult.TargetIsMissing;

                    containerId = target.Id;
                    inventory = target.SummonedInventory;
                }
                else
                {
                    if (owner == null)
                        return PowerUseResult.GenericError;

                    containerId = owner.Id;
                    inventory = owner.SummonedInventory;
                }

                if (!Verify.IsNotNull(inventory)) return PowerUseResult.GenericError;
                if (!Verify.IsTrue(containerId != Entity.InvalidId)) return PowerUseResult.GenericError;

                settings.InventoryLocation = new(containerId, inventory.PrototypeDataRef, Inventory.InvalidSlot);
            }

            // Properties
            using PropertyCollection properties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            SetSummonProperties(properties, ref context, summonProto, contextProto, owner, ultimateOwner, contextIndex);

            // EvalOnSummon
            if (contextProto.EvalOnSummon.HasValue())
            {
                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                evalContext.Game = game;
                evalContext.SetVar_PropertyCollectionPtr(EvalContext.Default, properties);
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, ultimateOwner?.Properties);
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, context.Properties);
                evalContext.SetReadOnlyVar_EntityPtr(EvalContext.Var1, ultimateOwner);

                bool evalSuccess = true;

                foreach (EvalPrototype evalProto in contextProto.EvalOnSummon)
                {
                    bool curEvalSucceeded = Eval.RunBool(evalProto, evalContext);
                    evalSuccess &= curEvalSucceeded;
                    Verify.IsTrue(curEvalSucceeded, $"The following EvalOnSummon Eval in a power failed:\nEval: [{evalProto.ExpressionString()}]\nPower: [{powerProto}]");
                }

                if (!Verify.IsTrue(evalSuccess)) return PowerUseResult.GenericError;
            }

            settings.Properties = properties;

            // KillPrevious
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

            // At long last we can finally create the summoned entity.
            PopulationManager populationManager = region.PopulationManager;
            ulong spawnGroupId = context.Properties[PropertyEnum.SpawnGroupId];

            foreach (Vector3 summonPos in summonPositions)
            {
                settings.Position = summonPos;

                SpawnGroup spawnGroup = null;

                if (owner != null)
                    spawnGroup = owner.SpawnGroup;
                else if (powerProto.SummonAsPopulation)
                    spawnGroup = populationManager.GetSpawnGroup(spawnGroupId);

                if (spawnGroup != null && powerProto.SummonAsPopulation)
                {
                    SpawnSpec spawnSpec = populationManager.CreateSpawnSpec(spawnGroup);
                    spawnSpec.EntityRef = settings.EntityRef;
                    spawnSpec.Transform = Transform3.BuildTransform(settings.Position - spawnGroup.Transform.Translation, settings.Orientation);
                    spawnSpec.Properties.FlattenCopyFrom(settings.Properties, false);
                    spawnSpec.Spawn();
                }
                else
                {
                    if (entityManager.CreateEntity(settings) is not WorldEntity summoned)
                        continue;

                    if (powerProto.AttachSummonsToCaster || powerProto.AttachSummonsToTarget)
                    {
                        if (target != null && target.IsInWorld && target.TestStatus(EntityStatus.ExitingWorld) == false)
                            summoned.AttachToEntity(target);
                    }

                    if (contextProto.VisibleWhileAttached == false)
                        summoned.SetVisible(false);

                    if (owner != null && owner.IsInWorld)
                    {
                        Power power = owner.GetPower(powerProto.DataRef);
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

            WorldEntity owner = Owner;
            if (!Verify.IsNotNull(owner)) return count;

            SummonPowerPrototype powerProto = SummonPowerPrototype;
            if (!Verify.IsNotNull(powerProto)) return count;

            Inventory inventory = Owner.SummonedInventory;
            if (inventory == null)
                return count;

            using var summonsHandle = ListPool<WorldEntity>.Instance.Get(out List<WorldEntity> summons);

            foreach (WorldEntity summoned in new SummonedEntityIterator(Owner))
            {
                if (summoned.IsDead)
                    continue;

                PrototypeId powerRef = summoned.Properties[PropertyEnum.CreatorPowerPrototype];
                if (powerRef == powerProto.DataRef)
                    summons.Add(summoned);
            }

            foreach (WorldEntity summoned in summons)
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

            return count;
        }

        private static void KillPreviousSummons(WorldEntity owner, SummonPowerPrototype powerProto, int summonsCount)
        {
            Inventory inventory = owner.SummonedInventory;
            if (!Verify.IsNotNull(inventory)) return;
            
            using var killListHandle = ListPool<WorldEntity>.Instance.Get(out List<WorldEntity> killList);

            foreach (WorldEntity summoned in new SummonedEntityIterator(owner))
            {
                if (summoned.IsDead)
                    continue;

                PrototypeId powerRef = summoned.Properties[PropertyEnum.CreatorPowerPrototype];
                if (powerRef == powerProto.DataRef || powerProto.InSummonMaxCountWithOthers(powerRef))
                    killList.Add(summoned);
            }
            
            // Oldest summoned entities will have lowest entity ids.
            killList.Sort(static (a, b) => a.Id.CompareTo(b.Id));
            for (int i = 0; i < killList.Count && i < summonsCount; i++)
                KillSummoned(killList[i], owner);
        }

        private static bool GetSummonPositions(WorldEntity owner, SummonPowerPrototype powerProto, WorldEntityPrototype summonProto, SummonEntityContextPrototype contextProto, 
            Region region, PropertyCollection properties, Vector3 position, ref Orientation orientation, List<Vector3> resultPositions)
        {
            BoundsPrototype boundsProto = summonProto.Bounds;
            if (!Verify.IsNotNull(boundsProto)) return false;

            Verify.IsTrue(position != Vector3.Zero);

            float radius = 0.0f;
            Vector3 resultPosition = position;

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
                        Verify.IsTrue(false);
                        return false;
                }
            }

            PathFlags pathFlags = Region.GetPathFlagsForEntity(summonProto);
            if (contextProto.PathFilterOverride != LocomotorMethod.None)
                pathFlags = Locomotor.GetPathFlags(contextProto.PathFilterOverride);

            PositionCheckFlags posFlags = PositionCheckFlags.None;
            BlockingCheckFlags blockingFlags = BlockingCheckFlags.None;

            if ((bounds.CollisionType == BoundsCollisionType.Blocking && contextProto.IgnoreBlockingOnSpawn == false) ||
                contextProto.ForceBlockingCollisionForSpawn)
            {
                posFlags = PositionCheckFlags.CanBeBlockedEntity | PositionCheckFlags.CanPathToEntities | PositionCheckFlags.PreferNoEntity;
                if (contextProto.ForceBlockingCollisionForSpawn)
                    blockingFlags = BlockingCheckFlags.CheckSpawns | BlockingCheckFlags.CheckSelf;
            }

            if (contextProto.RandomSpawnLocation)
            {
                float summonRadius = contextProto.SummonRadius;
                float maxRadius = Segment.IsNearZero(summonRadius) ? powerProto.Radius : summonRadius;

                bool foundPosition = region.ChooseRandomPositionNearPoint(ref bounds, pathFlags, posFlags, blockingFlags, 0, maxRadius, out resultPosition);
                if (!Verify.IsTrue(foundPosition, $"Failed to find RandomSpawnLocation for summon power.\nPower=[{powerProto}]"))
                    return false;
            }
            else if (pathFlags != PathFlags.None && region.IsLocationClear(ref bounds, pathFlags, posFlags, blockingFlags) == false)
            {
                if (!Verify.IsTrue(contextProto.EnforceExactSummonPos == false, $"Summon power is flagged as EnforceExactSummonPos, but the location is not clear.\nPower=[{powerProto}]"))
                    return false;

                bool foundPosition = false;

                if (owner != null)
                {
                    Vector3 ownerPosition = owner.RegionLocation.Position;
                    foundPosition = region.NaviMesh.FindPointOnLineToOccupy(ref resultPosition, ownerPosition, bounds.Center,
                        Vector3.Distance2D(ownerPosition, bounds.Center), ref bounds, pathFlags, blockingFlags, true) != PointOnLineResult.Failed;
                }

                if (foundPosition == false)
                {
                    float maxRadius = MathF.Max(radius * 2.0f, contextProto.SummonRadius);

                    foundPosition = region.ChooseRandomPositionNearPoint(ref bounds, pathFlags, posFlags, blockingFlags, 0, maxRadius, out resultPosition);
                    if (!Verify.IsTrue(foundPosition, $"Failed to find fallback position for summon power.\nPower=[{powerProto}]"))
                        return false;
                }
            }

            if (!Verify.IsNotNull(region.GetCellAtPosition(resultPosition), $"No cell at target position for summon power.\nPower=[{powerProto}]\nRegion=[{region}]\nPosition=[{resultPosition}]"))
                return false;

            float summonWidthMax = properties[PropertyEnum.SummonWidthMax];
            if (summonWidthMax > 0.0f)
            {
                // Multiple positions
                bounds.Center = resultPosition;
                return GetSummonPositionsAlongLine(ref bounds, summonWidthMax, ref orientation, contextProto.SummonOffsetAngle, region, pathFlags, posFlags, resultPositions);
            }
            else
            {
                // Single position
                if (Segment.IsNearZero(contextProto.SummonOffsetAngle) == false)
                    orientation.Yaw += MathHelper.ToRadians(contextProto.SummonOffsetAngle);

                resultPositions.Add(resultPosition);
                return true;
            }
        }

        private static bool GetSummonPositionsAlongLine(ref Bounds bounds, float summonWidthMax, ref Orientation orientation, float summonOffsetAngle, 
            Region region, PathFlags pathFlags, PositionCheckFlags posFlags, List<Vector3> resultPositions)
        {
            Vector3 position = bounds.Center;
            float radius = bounds.GetRadius();
            float offsetAngle = MathHelper.ToRadians(summonOffsetAngle);
            Vector3 angleDir = orientation.GetMatrix3() * Vector3.Forward;
            Vector3 rotate = Vector3.AxisAngleRotate(angleDir, Vector3.Up, offsetAngle);
            float halfWidth = summonWidthMax / 2.0f;

            Vector3? sweepL = position - rotate * halfWidth;
            Vector3? sweepR = position + rotate * halfWidth;
            Vector3? normal = null;

            NaviMesh navi = region.NaviMesh;
            navi.Sweep(position, sweepL.Value, radius, pathFlags, ref sweepL, ref normal);
            navi.Sweep(position, sweepR.Value, radius, pathFlags, ref sweepR, ref normal);

            Vector3 dir = sweepR.Value - sweepL.Value;
            orientation.Yaw = MathF.Atan2(dir.Y, dir.X);

            Vector3 centerPos = sweepL.Value + dir / 2.0f;
            resultPositions.Add(centerPos);

            float length = Vector3.Distance2D(sweepL.Value, sweepR.Value);
            float width = radius * 2.0f;

            bool blockedL = false;
            bool blockedR = false;
            int steps = ((int)MathF.Floor(length / width) - 1) / 2;

            for (int i = 0; i < steps; i++)
            {
                float distance = (i + 1) * width;
                Vector3 leftPos = centerPos + rotate * distance;
                Vector3 rightPos = centerPos - rotate * distance;

                bounds.Center = leftPos;
                if (blockedL == false)
                {
                    if (region.IsLocationClear(ref bounds, pathFlags, posFlags))
                        resultPositions.Add(leftPos);
                    else
                        blockedL = true;
                }

                bounds.Center = rightPos;
                if (blockedR == false)
                {
                    if (region.IsLocationClear(ref bounds, pathFlags, posFlags))
                        resultPositions.Add(rightPos);
                    else
                        blockedR = true;
                }
            }

            return true;
        }

        private static void SetSummonProperties(PropertyCollection properties, ref SummonContext context, WorldEntityPrototype summonProto, SummonEntityContextPrototype contextProto, 
            WorldEntity owner, WorldEntity ultimateOwner, int contextIndex)
        {
            // NOTE: This is similar to SetExtraProperties in MissilePower.

            SummonPowerPrototype powerProto = context.PowerProto;

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

            RegionManager regionManager = context.Game.RegionManager;
            if (Verify.IsNotNull(regionManager))
            {
                Region region = regionManager.GetRegion(context.RegionId);
                properties[PropertyEnum.NumOfPlayersInSameRegion] = ComputeNearbyPlayers(region, context.TargetPosition, 1);
            }

            if (summonProto.IsVacuumable == false && Verify.IsNotNull(context.OwnerAlliance))
                properties[PropertyEnum.AllianceOverride] = context.OwnerAlliance.DataRef;

            if (summonProto.ModifiersGuaranteed.HasValue())
            {
                foreach (PrototypeId boostRef in summonProto.ModifiersGuaranteed)
                    properties[PropertyEnum.EnemyBoost, boostRef] = true;
            }

            if (summonProto.Properties != null && summonProto.Properties[PropertyEnum.RestrictedToPlayer])
            {
                Player player = owner?.GetOwnerOfType<Player>();
                if (!Verify.IsNotNull(player)) return;

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
            SummonPowerPrototype summonPowerProto = SummonPowerPrototype;
            if (summonPowerProto.SummonIntervalMS <= 0)
                return;

            EventScheduler scheduler = Game?.GameEventScheduler;
            if (!Verify.IsNotNull(scheduler)) return;

            if (_summonIntervalEvent.IsValid)
                scheduler.CancelEvent(_summonIntervalEvent);

            TimeSpan summonInterval = TimeSpan.FromMilliseconds(summonPowerProto.SummonIntervalMS);
            scheduler.ScheduleEvent(_summonIntervalEvent, summonInterval, _pendingEvents);
            _summonIntervalEvent.Get().Initialize(this, index);
        }

        private struct SummonContext
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

        private class SummonIntervalEvent : CallMethodEventParam1<SummonPower, int>
        {
            protected override CallbackDelegate GetCallback() => (t, p1) => t.SummonIntervalCallback(p1);
        }
    }
}
