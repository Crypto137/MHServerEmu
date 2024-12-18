﻿using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;

using StackId = MHServerEmu.Games.Entities.ConditionCollection.StackId;

namespace MHServerEmu.Games.Powers
{
    [Flags]
    public enum ConditionSerializationFlags : uint
    {
        // These serialization flags are used to reduce the size of serialized conditions
        // by omitting data that can be derived from the prototype and the owner.
        None                        = 0,
        CreatorIsOwner              = 1 << 0,
        CreatorIsUltimateCreator    = 1 << 1,

        // The condition prototype is identified either by a condition prototype ref if it's a standalone condition,
        // or a creator power prototype ref + index in the AppliesConditions mixin list if this condition's prototype
        // is mixed into a PowerPrototype.
        NoConditionPrototypeRef     = 1 << 2,   // _conditionPrototypeRef != PrototypeId.Invalid
        NoCreatorPowerPrototypeRef  = 1 << 3,   // _creatorPowerPrototypeRef != PrototypeId.Invalid
        HasCreatorPowerIndex        = 1 << 4,   // _creatorPowerIndex >= 0

        HasOwnerAssetRef            = 1 << 5,   // _ownerAssetRef != AssetId.Invalid (defaults to owner.EntityWorldAsset if OwnerAssetRefOverride is not set)
        HasPauseTime                = 1 << 6,   // _pauseTime != TimeSpan.Zero
        HasDuration                 = 1 << 7,   // _duration != 0
        IsDisabled                  = 1 << 8,   // _isEnabled == false
        HasOwnerAssetRefOverride    = 1 << 9,   // owner == null || owner.Id != _ultimateCreatorId || _ownerAssetRef != owner.EntityWorldAsset

        // Normally, _updateInterval and _cancelOnFlags are taken from the ConditionPrototype, but if any of these two flags
        // are set, it means that the default values are overriden.
        HasUpdateIntervalOverride   = 1 << 10,
        HasCancelOnFlagsOverride    = 1 << 11
    }

    [Flags]
    public enum ConditionCancelOnFlags : uint
    {
        None                        = 0,
        OnHit                       = 1 << 0,
        OnKilled                    = 1 << 1,
        OnPowerUse                  = 1 << 2,
        OnPowerUsePost              = 1 << 3,
        OnTransfer                  = 1 << 4,
        OnIntraRegionTeleport       = 1 << 5
    }

    public class Condition
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly KeywordsMask EmptyMask = new();

        private ConditionSerializationFlags _serializationFlags;
        private ulong _conditionId;                                          // Condition id
        private ulong _creatorId;                                   // Entity id
        private ulong _ultimateCreatorId;                           // Entity id, the highest entity in the creation hierarchy (i.e. creator of creator, or the creator itself)

        private PrototypeId _conditionPrototypeRef;
        private ConditionPrototype _conditionPrototype;
        private PrototypeId _creatorPowerPrototypeRef;
        private PowerPrototype _creatorPowerPrototype;
        private int _creatorPowerIndex = -1;

        private AssetId _ownerAssetRef;
        private TimeSpan _startTime;
        private TimeSpan _pauseTime;
        private long _durationMS;
        private bool _isEnabled = true;
        private int _updateIntervalMS;
        private ReplicatedPropertyCollection _properties = new();
        private ConditionCancelOnFlags _cancelOnFlags;

        private ulong _creatorPlayerId;                             // Player entity guid

        // Accessors
        public ConditionSerializationFlags SerializationFlags { get => _serializationFlags; }
        public ulong Id { get => _conditionId; }
        public ulong CreatorId { get => _creatorId; }
        public ulong UltimateCreatorId { get => _ultimateCreatorId; }

        public PrototypeId ConditionPrototypeRef { get => _conditionPrototypeRef; }
        public ConditionPrototype ConditionPrototype { get => _conditionPrototype; }

        public PrototypeId CreatorPowerPrototypeRef { get => _creatorPowerPrototypeRef; }
        public PowerPrototype CreatorPowerPrototype { get => _creatorPowerPrototype; }
        public int CreatorPowerIndex { get => _creatorPowerIndex; set => _creatorPowerIndex = value; }

        public AssetId OwnerAssetRef { get => _ownerAssetRef; }
        public TimeSpan StartTime { get => _startTime; }
        public TimeSpan PauseTime { get => _pauseTime; }
        public TimeSpan Duration { get => TimeSpan.FromMilliseconds(_durationMS); }
        public bool IsEnabled { get => _isEnabled; set => _isEnabled = value; }
        public TimeSpan UpdateInterval { get => TimeSpan.FromMilliseconds(_updateIntervalMS); }
        public ReplicatedPropertyCollection Properties { get => _properties; }
        public ConditionCancelOnFlags CancelOnFlags { get => _cancelOnFlags; }

        public ulong CreatorPlayerId { get => _creatorPlayerId; }
        public ConditionCollection Collection { get; set; }
        public bool IsInCollection { get => Collection != null; }
        public StackId StackId { get; private set; } = StackId.Invalid;

        public bool IsPaused { get => _pauseTime != TimeSpan.Zero; }
        public TimeSpan ElapsedTime { get => IsPaused ? _pauseTime - _startTime : Game.Current.CurrentTime - _startTime; }
        public TimeSpan TimeRemaining { get => Duration - ElapsedTime; }

        public PowerIndexPropertyFlags PowerIndexPropertyFlags { get => _conditionPrototype != null ? _conditionPrototype.PowerIndexPropertyFlags : default; }

        public EventPointer<ConditionCollection.RemoveConditionEvent> RemoveEvent { get; set; }

        public Condition() { }

        public override string ToString()
        {
            if (_conditionPrototypeRef != PrototypeId.Invalid)
                return _conditionPrototypeRef.GetName();

            return $"{_creatorPowerPrototype}[{_conditionPrototype.BlueprintCopyNum}]";
        }

        public bool Serialize(Archive archive, WorldEntity owner)
        {
            // NOTE: Rather than implementing ISerialize, conditions have their own serialization
            // thing that also requires an owner WorldEntity reference to determine serialization
            // flags.

            bool success = true;

            // This wasn't originally used for persistent condition serialization,
            // need to implement something like PropertyStore and restore persistent conditions.
            if (archive.IsTransient == false)
                return success;

            if (archive.IsPacking)
            {
                // Build serialization flags
                _serializationFlags = ConditionSerializationFlags.None;
                if (owner != null && owner.Id == _creatorId)
                    _serializationFlags |= ConditionSerializationFlags.CreatorIsOwner;

                if (_ultimateCreatorId == _creatorId)
                    _serializationFlags |= ConditionSerializationFlags.CreatorIsUltimateCreator;

                if (_conditionPrototypeRef == PrototypeId.Invalid)
                    _serializationFlags |= ConditionSerializationFlags.NoConditionPrototypeRef;

                if (_creatorPowerPrototypeRef == PrototypeId.Invalid)
                    _serializationFlags |= ConditionSerializationFlags.NoCreatorPowerPrototypeRef;

                if (_creatorPowerIndex >= 0)
                    _serializationFlags |= ConditionSerializationFlags.HasCreatorPowerIndex;

                if (_ownerAssetRef == AssetId.Invalid)
                    _serializationFlags |= ConditionSerializationFlags.HasOwnerAssetRef;

                if (_pauseTime.TotalMilliseconds != 0)
                    _serializationFlags |= ConditionSerializationFlags.HasPauseTime;

                if (_isEnabled == false)
                    _serializationFlags |= ConditionSerializationFlags.IsDisabled;

                if (_ownerAssetRef != AssetId.Invalid)
                {
                    _serializationFlags |= ConditionSerializationFlags.HasOwnerAssetRef;
                    if (owner == null || owner.Id != _ultimateCreatorId || _ownerAssetRef != owner.GetOriginalWorldAsset())
                        _serializationFlags |= ConditionSerializationFlags.HasOwnerAssetRefOverride;
                }

                if (_durationMS != 0)
                    _serializationFlags |= ConditionSerializationFlags.HasDuration;

                if (_updateIntervalMS != (int)_conditionPrototype.UpdateInterval.TotalMilliseconds)
                    _serializationFlags |= ConditionSerializationFlags.HasUpdateIntervalOverride;

                if (_cancelOnFlags != _conditionPrototype.CancelOnFlags)
                    _serializationFlags |= ConditionSerializationFlags.HasCancelOnFlagsOverride;

                // Write data
                uint serializationFlags = (uint)_serializationFlags;
                success &= Serializer.Transfer(archive, ref serializationFlags);

                // Pack all the necessary data according to our flags
                success &= Serializer.Transfer(archive, ref _conditionId);

                if (_serializationFlags.HasFlag(ConditionSerializationFlags.CreatorIsOwner) == false)
                    success &= Serializer.Transfer(archive, ref _creatorId);

                if (_serializationFlags.HasFlag(ConditionSerializationFlags.CreatorIsUltimateCreator) == false)
                    success &= Serializer.Transfer(archive, ref _ultimateCreatorId);

                if (_serializationFlags.HasFlag(ConditionSerializationFlags.NoConditionPrototypeRef) == false)
                    success &= Serializer.Transfer(archive, ref _conditionPrototypeRef);

                if (_serializationFlags.HasFlag(ConditionSerializationFlags.NoCreatorPowerPrototypeRef) == false)
                    success &= Serializer.Transfer(archive, ref _creatorPowerPrototypeRef);

                if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasCreatorPowerIndex))
                {
                    uint index = (uint)_creatorPowerIndex;
                    success &= Serializer.Transfer(archive, ref index);
                }

                if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasOwnerAssetRefOverride))
                    success &= Serializer.Transfer(archive, ref _ownerAssetRef);

                success &= Serializer.TransferTimeAsDelta(archive, null, ref _startTime);

                if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasPauseTime))
                    success &= Serializer.TransferTimeAsDelta(archive, null, ref _pauseTime);

                if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasDuration))
                    success &= Serializer.Transfer(archive, ref _durationMS);

                if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasUpdateIntervalOverride))
                    success &= Serializer.Transfer(archive, ref _updateIntervalMS);

                success &= Serializer.Transfer(archive, ref _properties);

                if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasCancelOnFlagsOverride))
                {
                    uint cancelOnFlags = (uint)_cancelOnFlags;
                    success &= Serializer.Transfer(archive, ref cancelOnFlags);
                }
            }
            else
            {
                if (owner == null) return Logger.WarnReturn(false, "Serialize(): owner == null");

                uint serializationFlags = 0;
                success &= Serializer.Transfer(archive, ref serializationFlags);
                _serializationFlags = (ConditionSerializationFlags)serializationFlags;

                success &= Serializer.Transfer(archive, ref _conditionId);

                // Default creator is the owner
                if (_serializationFlags.HasFlag(ConditionSerializationFlags.CreatorIsOwner))
                    _creatorId = owner != null ? owner.Id : 0;
                else
                    success &= Serializer.Transfer(archive, ref _creatorId);

                // Default ultimate creator is the creator
                _ultimateCreatorId = _creatorId;
                if (_serializationFlags.HasFlag(ConditionSerializationFlags.CreatorIsUltimateCreator) == false)
                    success &= Serializer.Transfer(archive, ref _ultimateCreatorId);

                // There MUST be a ConditionPrototype, either a directly referenced one,
                // or a creator power ref + index in the AppliesConditions mixin list.

                // Default condition prototype ref is invalid
                _conditionPrototypeRef = PrototypeId.Invalid;
                _conditionPrototype = null;
                if (_serializationFlags.HasFlag(ConditionSerializationFlags.NoConditionPrototypeRef) == false)
                {
                    success &= Serializer.Transfer(archive, ref _conditionPrototypeRef);
                    _conditionPrototype = _conditionPrototypeRef.As<ConditionPrototype>();
                }

                // Default creator power prototype ref is invalid
                _creatorPowerPrototypeRef = PrototypeId.Invalid;
                _creatorPowerPrototype = null;
                if (_serializationFlags.HasFlag(ConditionSerializationFlags.NoCreatorPowerPrototypeRef) == false)
                {
                    success &= Serializer.Transfer(archive, ref _creatorPowerPrototypeRef);
                    _creatorPowerPrototype = _creatorPowerPrototypeRef.As<PowerPrototype>();
                }

                // If a condition has a creator power index, it means its prototype is a list mixin of the creator power
                if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasCreatorPowerIndex))
                {
                    uint index = 0;
                    success &= Serializer.Transfer(archive, ref index);
                    _creatorPowerIndex = (int)index;

                    // Get condition prototype from the creator power's mixin list
                    for (int i = 0; i < _creatorPowerPrototype.AppliesConditions.Count; i++)
                    {
                        if (i == _creatorPowerIndex)
                        {
                            _conditionPrototype = _creatorPowerPrototype.AppliesConditions[i].Prototype as ConditionPrototype;
                            break;
                        }
                    }
                }

                // Make sure we were able to find our prototype, or things are going to go very wrong
                if (_conditionPrototype == null)
                    return Logger.ErrorReturn(false, $"Serialize(): Failed to find the ConditionPrototype reference during unpacking");

                // Default owner asset is AssetId.Invalid
                _ownerAssetRef = AssetId.Invalid;
                if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasOwnerAssetRefOverride))
                    success &= Serializer.Transfer(archive, ref _ownerAssetRef);                // Get asset override if we have one
                else if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasOwnerAssetRef))
                    _ownerAssetRef = owner != null ? owner.GetEntityWorldAsset() : AssetId.Invalid;  // Fall back to the owner asset

                // _startTime should always be present
                success &= Serializer.TransferTimeAsDelta(archive, null, ref _startTime);

                // Default pause time is TimeSpan.Zero (i.e. not paused)
                _pauseTime = TimeSpan.Zero;
                if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasPauseTime))
                    success &= Serializer.TransferTimeAsDelta(archive, null, ref _pauseTime);

                // Default duration is 0 ms, which means unlimited duration? (to be confirmed)
                _durationMS = 0;
                if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasDuration))
                    success &= Serializer.Transfer(archive, ref _durationMS);

                // For some reason _isEnabled is not updated during deserialization in the client.
                // ConditionCollection does call Condition::serializationFlagIsDisabled() during OnUnpackComplete() though.

                // Default update interval is taken from the ConditionPrototype
                _updateIntervalMS = _conditionPrototype.UpdateIntervalMS;
                if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasUpdateIntervalOverride))
                    success &= Serializer.Transfer(archive, ref _updateIntervalMS);

                success &= Serializer.Transfer(archive, ref _properties);

                // Default cancel on flags are taken from the ConditionPrototype
                _cancelOnFlags = _conditionPrototype.CancelOnFlags;
                if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasCancelOnFlagsOverride))
                {
                    uint cancelOnFlags = 0;
                    success &= Serializer.Transfer(archive, ref cancelOnFlags);
                    _cancelOnFlags = (ConditionCancelOnFlags)cancelOnFlags;
                }
            }

            return success;
        }

        public bool InitializeFromPowerMixinPrototype(ulong conditionId, PowerPayload payload, ConditionPrototype conditionProto, TimeSpan duration, PropertyCollection properties = null)
        {
            _conditionId = conditionId;

            _creatorId = payload.PowerOwnerId;
            _ultimateCreatorId = payload.UltimateOwnerId != Entity.InvalidId ? payload.UltimateOwnerId : payload.PowerOwnerId;
            
            WorldEntity ultimateCreator = payload.Game.EntityManager.GetEntity<WorldEntity>(_ultimateCreatorId);
            if (ultimateCreator != null)
            {
                if (ultimateCreator is Avatar avatar)
                {
                    Player player = avatar.GetOwnerOfType<Player>();
                    if (player != null)
                        _creatorPlayerId = player.DatabaseUniqueId;
                }

                _ownerAssetRef = DetermineAssetRefByOwner(ultimateCreator, conditionProto);
            }

            _conditionPrototype = conditionProto;
            _conditionPrototypeRef = PrototypeId.Invalid;

            _creatorPowerPrototype = payload.PowerPrototype;
            _creatorPowerPrototypeRef = payload.PowerProtoRef;
            _creatorPowerIndex = conditionProto.BlueprintCopyNum;

            _durationMS = (long)duration.TotalMilliseconds;
            _cancelOnFlags = conditionProto.CancelOnFlags;

            if (properties != null)
            {
                Properties.FlattenCopyFrom(properties, true);
            }
            else
            {
                WorldEntity creator = payload.Game.EntityManager.GetEntity<WorldEntity>(_creatorId);
                WorldEntity target = payload.Game.EntityManager.GetEntity<WorldEntity>(payload.TargetId);

                if (GenerateConditionProperties(Properties, conditionProto, payload.Properties, creator, target, payload.Game) == false)
                    Logger.Warn($"InitializeFromPowerMixinPrototype(): Failed to generate properties for [{this}]");
            }

            return true;
        }

        public bool CacheStackId()
        {
            // Non-power conditions cannot stack
            PowerPrototype powerProto = CreatorPowerPrototype;
            if (powerProto == null)
                return true;

            if (StackId.PrototypeRef != PrototypeId.Invalid)
                return true;

            ConditionPrototype conditionProto = ConditionPrototype;
            if (conditionProto == null) return Logger.WarnReturn(false, "CacheStackId(): conditionProto == null");

            StackId = ConditionCollection.MakeConditionStackId(powerProto, conditionProto, UltimateCreatorId, CreatorPlayerId, out _);
            return true;
        }

        public bool CanStackWith(in StackId stackId)
        {
            return StackId == stackId;
        }

        public StackingBehaviorPrototype GetStackingBehaviorPrototype()
        {
            // Non-power conditions cannot stack
            PowerPrototype powerProto = CreatorPowerPrototype;
            if (powerProto == null)
                return null;

            ConditionPrototype conditionProto = ConditionPrototype;
            if (conditionProto == null) return Logger.WarnReturn<StackingBehaviorPrototype>(null, "GetStackingBehaviorProto(): conditionProto == null");

            return conditionProto.StackingBehavior != null ? conditionProto.StackingBehavior : powerProto.StackingBehaviorLEGACY;
        }

        public void ResetStartTime()
        {
            _startTime = Game.Current.CurrentTime;
        }

        public bool IsPersistToDB()
        {
            if (_conditionPrototype == null)
                return Logger.WarnReturn(false, "IsPersistToDB(): _conditionPrototype == null");

            return _conditionPrototype.PersistToDB;
        }

        public bool IsBoost()
        {
            if (_conditionPrototype == null)
                return Logger.WarnReturn(false, "IsBoost(): _conditionPrototype == null");

            return _conditionPrototype.IsBoost;
        }

        public bool IsHitReactCondition()
        {
            if (_conditionPrototype == null)
                return Logger.WarnReturn(false, "IsHitReactCondition(): _conditionPrototype == null");

            return _conditionPrototype.IsHitReactCondition;
        }

        public PrototypeId[] GetKeywords()
        {
            if (_conditionPrototype == null)
                return Logger.WarnReturn<PrototypeId[]>(null, "GetKeywords(): _conditionPrototype == null");

            return _conditionPrototype.Keywords;
        }

        public KeywordsMask GetKeywordsMask()
        {
            ConditionPrototype conditionProto = ConditionPrototype;
            if (conditionProto == null) return Logger.WarnReturn(EmptyMask, "GetKeywordsMask(): conditionProto == null");

            return conditionProto.KeywordsMask;
        }

        public bool HasKeyword(KeywordPrototype keywordProto)
        {
            return _conditionPrototype.HasKeyword(keywordProto);
        }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="Condition"/> includes any negative status effects.
        /// Negative effect prototype refs are added to the provided <see cref="List{T}"/> if it's not <see langword="null"/>.
        /// </summary>
        public bool IsANegativeStatusEffect(List<PrototypeId> outputList = null)
        {
            return IsANegativeStatusEffect(Properties, outputList);
        }

        /// <summary>
        /// Returns <see langword="true"/> if the specified <see cref="PrototypeId"/> refers to a negative status effect <see cref="PropertyInfoPrototype"/>.
        /// </summary>
        public static bool IsANegativeStatusEffectProperty(PrototypeId propertyPrototypeRef)
        {
            // The client implementation contains additional null checks that we can safely skip (probably).

            // Properties designated as "negative status effects" as of 1.52:
            // Immobilized, Knockback, Knockdown, Mesmerized, MovementSpeedDecrPct,
            // Stunned, Taunted, Feared, Knockup, AllianceOverride, StunnedByHitReact,
            // Confused, CastSpeedDecrPct.

            return GameDatabase.GlobalsPrototype.NegStatusEffectList.Contains(propertyPrototypeRef);
        }

        /// <summary>
        /// Returns <see langword="true"/> if the provided <see cref="PropertyCollection"/> contains any negative status effects.
        /// Negative effect prototype refs are added to the provided <see cref="List{T}"/> if it's not <see langword="null"/>.
        /// </summary>
        public static bool IsANegativeStatusEffect(PropertyCollection propertyCollection, List<PrototypeId> outputList)
        {
            bool containsNegativeStatusEffects = false;

            foreach (var kvp in propertyCollection)
            {
                PropertyInfo propertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(kvp.Key.Enum);
                PrototypeId propertyPrototypeRef = propertyInfo.PrototypeDataRef;

                if (IsANegativeStatusEffectProperty(propertyPrototypeRef))
                {
                    containsNegativeStatusEffects = true;
                    outputList?.Add(propertyPrototypeRef);      // This can be null
                }
            }

            return containsNegativeStatusEffects;
        }

        public static bool GenerateConditionProperties(PropertyCollection conditionProperties, ConditionPrototype conditionProto,
            PropertyCollection initializeProperties, WorldEntity sourceEntity, WorldEntity destEntity, Game game)
        {
            bool success = true;

            // Copy base properties from the prototype
            if (conditionProto.Properties != null)
                conditionProperties.FlattenCopyFrom(conditionProto.Properties, true);

            // Assign extra properties from the creator
            if (initializeProperties != null)
            {
                foreach (var kvp in initializeProperties.IteratePropertyRange(PropertyEnumFilter.SerializeConditionSrcToConditionFunc))
                    conditionProperties[kvp.Key] = kvp.Value;
            }

            // Run eval
            if (conditionProto.EvalOnCreate.HasValue())
            {
                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                evalContext.Game = game;
                evalContext.SetVar_PropertyCollectionPtr(EvalContext.Default, conditionProperties);
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, sourceEntity?.Properties);
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, destEntity?.Properties);
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Var1, initializeProperties);
                evalContext.SetReadOnlyVar_EntityPtr(EvalContext.Var2, sourceEntity);
                evalContext.SetReadOnlyVar_EntityPtr(EvalContext.Var3, destEntity);

                Eval.InitTeamUpEvalContext(evalContext, sourceEntity);

                foreach (EvalPrototype evalProto in conditionProto.EvalOnCreate)
                {
                    bool evalSuccess = Eval.RunBool(evalProto, evalContext);
                    success &= evalSuccess;
                    if (evalSuccess == false)
                        Logger.Warn($"GenerateConditionProperties(): The following EvalOnCreate Eval in a condition failed:\nEval: [{evalProto.ExpressionString()}]\nCondition: [{conditionProto}]\nSource entity: [{sourceEntity}]\nDest entity: [{destEntity}]");
                }
            }

            // Assign proc properties
            List<PrototypeId> procPowerRefList = ListPool<PrototypeId>.Instance.Get();
            foreach (var kvp in conditionProperties.IteratePropertyRange(Property.ProcPropertyTypesAll))
            {
                Property.FromParam(kvp.Key, 1, out PrototypeId procPowerRef);
                procPowerRefList.Add(procPowerRef);
            }

            foreach (PrototypeId procPowerRef in procPowerRefList)
            {
                conditionProperties[PropertyEnum.ProcPowerItemLevel, procPowerRef] = conditionProperties[PropertyEnum.ItemLevel];
                conditionProperties[PropertyEnum.ProcPowerItemVariation, procPowerRef] = conditionProperties[PropertyEnum.ItemVariation];
            }

            ListPool<PrototypeId>.Instance.Return(procPowerRefList);

            return success;
        }

        private bool UpdateOwnerAssetRef(WorldEntity owner)
        {
            if (_conditionPrototype == null) return Logger.WarnReturn(false, "UpdateOwnerAssetRef(): _conditionPrototype == null");

            AssetId assetRef = DetermineAssetRefByOwner(owner, _conditionPrototype);
            if (assetRef == _ownerAssetRef)
                return false;

            _ownerAssetRef = assetRef;
            return true;
        }

        private static AssetId DetermineAssetRefByOwner(WorldEntity owner, ConditionPrototype conditionProto)
        {
            AssetId entityWorldAssetRef = owner.GetEntityWorldAsset();
            if (conditionProto.GetUnrealClass(entityWorldAssetRef, false) != AssetId.Invalid)
                return entityWorldAssetRef;

            return owner.GetOriginalWorldAsset();
        }
    }
}
