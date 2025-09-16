using MHServerEmu.Core.Extensions;
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
using MHServerEmu.Games.Regions;

using StackId = MHServerEmu.Games.Entities.ConditionCollection.StackId;

namespace MHServerEmu.Games.Powers.Conditions
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
        None                    = 0,
        OnHit                   = 1 << 0,
        OnKilled                = 1 << 1,
        OnPowerUse              = 1 << 2,
        OnPowerUsePost          = 1 << 3,
        OnTransfer              = 1 << 4,
        OnIntraRegionTeleport   = 1 << 5
    }

    public class Condition : IKeyworded
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        // NOTE: If you add any new fields here, also add them to Clear()

        private ConditionSerializationFlags _serializationFlags;
        private ulong _conditionId;
        private ulong _creatorId;               // Entity id
        private ulong _ultimateCreatorId;       // Entity id of the highest entity in the creation hierarchy (i.e. creator of creator, or the creator itself)

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

        public ulong CreatorPlayerId { get; private set; }  // PlayerGuid
        public ConditionCollection Collection { get; set; }
        public ulong PropertyTickerId { get; set; } = PropertyTicker.InvalidId;
        public PowerPayload PropertyTickerPayload { get; set; }
        public StackId StackId { get; private set; } = StackId.Invalid;

        public EventPointer<ConditionCollection.RemoveConditionEvent> RemoveEvent { get; set; }

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
        public TimeSpan PauseTime { get => _pauseTime; set => _pauseTime = value; }
        public TimeSpan Duration { get => TimeSpan.FromMilliseconds(_durationMS); }
        public bool IsEnabled { get => _isEnabled; set => _isEnabled = value; }
        public TimeSpan UpdateInterval { get => TimeSpan.FromMilliseconds(_updateIntervalMS); }
        public ReplicatedPropertyCollection Properties { get => _properties; }
        public ConditionCancelOnFlags CancelOnFlags { get => _cancelOnFlags; }

        public bool IsInCollection { get => Collection != null; }
        public bool IsInPool { get; set; }

        public bool IsPaused { get => _pauseTime != TimeSpan.Zero; }
        public TimeSpan ElapsedTime { get => IsPaused ? _pauseTime - _startTime : Game.Current.CurrentTime - _startTime; }
        public TimeSpan TimeRemaining { get => Duration - ElapsedTime; }
        public bool IsFinite { get => Duration > TimeSpan.Zero || Properties.HasProperty(PropertyEnum.ConditionKillCountLimit); }
        public int Rank { get => Properties[PropertyEnum.PowerRank]; }

        public PowerIndexPropertyFlags PowerIndexPropertyFlags { get => _conditionPrototype != null ? _conditionPrototype.PowerIndexPropertyFlags : default; }

        public Condition() { }

        public override string ToString()
        {
            if (_conditionPrototypeRef != PrototypeId.Invalid)
                return _conditionPrototypeRef.GetName();

            if (_creatorPowerPrototype != null)
                return $"{_creatorPowerPrototype}[{_conditionPrototype.BlueprintCopyNum}]";

            return "INVALID";
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

        public void RestoreCreatorIdIfPossible(ulong entityId, ulong playerDbId)
        {
            PowerPrototype creatorPowerProto = CreatorPowerPrototype;
            if (creatorPowerProto == null)
                return;

            if (Power.GetTargetingShape(creatorPowerProto) != TargetingShapeType.Self)
                return;

            _creatorId = entityId;
            _ultimateCreatorId = entityId;
            CreatorPlayerId = playerDbId;
        }

        public void Clear()
        {
            // Clear all data from this condition instance for later reuse via pooling
            _serializationFlags = default;
            _conditionId = default;
            _creatorId = default;
            _ultimateCreatorId = default;

            _conditionPrototypeRef = default;
            _conditionPrototype = default;
            _creatorPowerPrototypeRef = default;
            _creatorPowerPrototype = default;
            _creatorPowerIndex = -1;

            _ownerAssetRef = default;
            _startTime = default;
            _pauseTime = default;
            _durationMS = default;
            _isEnabled = true;
            _updateIntervalMS = default;
            _properties.Clear();
            _cancelOnFlags = default;

            CreatorPlayerId = default;
            Collection = default;
            PropertyTickerId = PropertyTicker.InvalidId;
            PropertyTickerPayload = null;
            StackId = StackId.Invalid;

            RemoveEvent = default;
        }

        public bool InitializeFromPower(ulong conditionId, PowerPayload payload, ConditionPrototype conditionProto, TimeSpan duration,
            PropertyCollection properties = null, bool linkToPower = true)
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
                        CreatorPlayerId = player.DatabaseUniqueId;
                }

                _ownerAssetRef = DetermineAssetRefByOwner(ultimateCreator, conditionProto);
            }

            _conditionPrototype = conditionProto;

            // For some power-induced conditions (e.g. hit reacts) we don't want to link them to the creator power to avoid stacking
            if (linkToPower)
            {
                _creatorPowerPrototype = payload.PowerPrototype;
                _creatorPowerPrototypeRef = payload.PowerProtoRef;
            }

            if (conditionProto.DataRef == PrototypeId.Invalid)
            {
                _conditionPrototypeRef = PrototypeId.Invalid;
                _creatorPowerIndex = conditionProto.BlueprintCopyNum;
            }
            else
            {
                _conditionPrototypeRef = conditionProto.DataRef;
                _creatorPowerIndex = -1;
            }

            _durationMS = (long)duration.TotalMilliseconds;
            _updateIntervalMS = conditionProto.UpdateIntervalMS;
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

        public bool InitializeFromConditionPrototype(ulong conditionId, Game game, ulong creatorId, ulong ultimateCreatorId, ulong targetId,
            ConditionPrototype conditionProto, TimeSpan duration, PropertyCollection properties = null)
        {
            _conditionId = conditionId;

            _creatorId = creatorId;
            _ultimateCreatorId = ultimateCreatorId != Entity.InvalidId ? ultimateCreatorId : creatorId;

            WorldEntity ultimateCreator = game.EntityManager.GetEntity<WorldEntity>(_ultimateCreatorId);
            if (ultimateCreator != null)
            {
                if (ultimateCreator is Avatar avatar)
                {
                    Player player = avatar.GetOwnerOfType<Player>();
                    if (player != null)
                        CreatorPlayerId = player.DatabaseUniqueId;
                }

                _ownerAssetRef = DetermineAssetRefByOwner(ultimateCreator, conditionProto);
            }

            _conditionPrototype = conditionProto;
            _creatorPowerPrototype = null;
            _creatorPowerPrototypeRef = PrototypeId.Invalid;

            _conditionPrototypeRef = conditionProto.DataRef;
            _creatorPowerIndex = -1;

            _durationMS = (long)duration.TotalMilliseconds;
            _updateIntervalMS = conditionProto.UpdateIntervalMS;
            _cancelOnFlags = conditionProto.CancelOnFlags;

            if (properties != null)
            {
                Properties.FlattenCopyFrom(properties, true);
            }
            else
            {
                WorldEntity creator = game.EntityManager.GetEntity<WorldEntity>(_creatorId);
                WorldEntity target = game.EntityManager.GetEntity<WorldEntity>(targetId);

                if (GenerateConditionProperties(Properties, conditionProto, null, creator, target, game) == false)
                    Logger.Warn($"InitializeFromConditionPrototype(): Failed to generate properties for [{this}]");
            }

            return true;
        }

        public bool InitializeFromOtherCondition(ulong conditionId, Condition other, WorldEntity owner)
        {
            _conditionId = conditionId;

            // This method is used to copy a condition from one avatar to another, so the owner changes
            _creatorId = owner.Id;
            _ultimateCreatorId = owner.Id;

            _conditionPrototypeRef = other._conditionPrototypeRef;
            _conditionPrototype = other._conditionPrototype;
            _creatorPowerPrototypeRef = other._creatorPowerPrototypeRef;
            _creatorPowerPrototype = other._creatorPowerPrototype;
            _creatorPowerIndex = other._creatorPowerIndex;

            _ownerAssetRef = owner.GetEntityWorldAsset();
            // _startTime and _pauseTime is set when this condition is added to a collection
            _durationMS = (long)other.TimeRemaining.TotalMilliseconds;
            _isEnabled = other._isEnabled;
            _updateIntervalMS = other._updateIntervalMS;
            _properties.FlattenCopyFrom(other._properties, true);
            _cancelOnFlags = other._cancelOnFlags;

            CreatorPlayerId = other.CreatorPlayerId;

            return true;
        }

        public bool InitializeFromConditionStore(ulong conditionId, ref ConditionStore conditionStore, WorldEntity owner)
        {
            TimeSpan currentTime = Game.Current.CurrentTime;

            // Restore prototype-derivable data
            _conditionId = conditionId;
            _creatorId = 0;
            _ultimateCreatorId = 0;

            _conditionPrototypeRef = conditionStore.ConditionProtoRef;
            _conditionPrototype = _conditionPrototypeRef.As<ConditionPrototype>();
            if (_conditionPrototype == null) return Logger.ErrorReturn(false, "InitializeFromConditionStore(): _conditionPrototype == null");

            _creatorPowerPrototypeRef = conditionStore.CreatorPowerPrototypeRef;
            _creatorPowerPrototype = _creatorPowerPrototypeRef.As<PowerPrototype>();
            _creatorPowerIndex = -1;

            _startTime = currentTime;

            if (conditionStore.IsPaused)
                _pauseTime = currentTime;

            _updateIntervalMS = _conditionPrototype.UpdateIntervalMS;
            _cancelOnFlags = _conditionPrototype.CancelOnFlags;

            // Restore duration
            TimeSpan duration = TimeSpan.FromMilliseconds(conditionStore.TimeRemaining);

            int killCount = conditionStore.Properties[PropertyEnum.ConditionKillCountLimit];
            if (duration <= TimeSpan.Zero && killCount <= 0)
            {
                Logger.Warn($"InitializeFromConditionStore(): Found infinite condition [{this}] without a kill count (owner=[{owner}])");
                duration = TimeSpan.FromMilliseconds(1);
            }

            if (conditionStore.SerializeGameTime != 0)
            {
                if (IsRealTime())
                {
                    TimeSpan timeSinceSerialize = currentTime - TimeSpan.FromMilliseconds(conditionStore.SerializeGameTime);
                    duration -= timeSinceSerialize;

                    // Expire ASAP if this condition ran out while it was stored
                    if (duration <= TimeSpan.Zero)
                        duration = TimeSpan.FromMilliseconds(1);
                }
                else
                {
                    Logger.Warn($"InitializeFromConditionStore(): Condition [{this}] was saved as a real-time condition, but it's not flagged as real-time in the prototype (owner=[{owner}])");
                }
            }

            _durationMS = (long)duration.TotalMilliseconds;

            // Restore properties
            using PropertyCollection initializeProperties = ObjectPoolManager.Instance.Get<PropertyCollection>();

            PropertyInfoTable infoTable = GameDatabase.PropertyInfoTable;

            foreach (var kvp in conditionStore.Properties)
            {
                PropertyInfoPrototype propertyInfoProto = infoTable.LookupPropertyInfo(kvp.Key.Enum)?.Prototype;
                if (propertyInfoProto == null)
                {
                    Logger.Warn("StoreCondition(): propertyInfoProto == null");
                    continue;
                }

                if (propertyInfoProto.SerializeConditionSrcToCondition == false)
                    continue;

                switch (kvp.Key.Enum)
                {
                    case PropertyEnum.ConditionItemLevel:
                        initializeProperties[PropertyEnum.ItemLevel] = kvp.Value;
                        break;

                    case PropertyEnum.CharacterLevel:
                        initializeProperties.CopyProperty(conditionStore.Properties, kvp.Key);
                        initializeProperties[PropertyEnum.CombatLevel] = kvp.Value;
                        break;

                    default:
                        initializeProperties.CopyProperty(conditionStore.Properties, kvp.Key);
                        break;
                }
            }

            if (GenerateConditionProperties(Properties, _conditionPrototype, initializeProperties, null, owner, owner.Game) == false)
                return Logger.ErrorReturn(false, $"InitializeFromConditionStore(): Failed to generate properties for [{this}]");

            return true;
        }

        public bool SaveToConditionStore(ref ConditionStore conditionStore)
        {
            conditionStore.ConditionProtoRef = ConditionPrototypeRef;
            conditionStore.CreatorPowerPrototypeRef = CreatorPowerPrototypeRef;
            conditionStore.IsPaused = IsPaused;

            // Special handling for conditions that go away after a certain number of kills
            long timeRemaining = (long)TimeRemaining.TotalMilliseconds;
            int killCount = Properties[PropertyEnum.ConditionKillCountLimit];

            if (timeRemaining < 0 && killCount > 0)
                timeRemaining = 0;
            else if (timeRemaining <= 0 && killCount <= 0)
                timeRemaining = 1;  // Do not allow conditions without kill counts to become "infinite"

            conditionStore.TimeRemaining = (ulong)timeRemaining;

            // Serialize current game time for conditions that expire in real time when the owner is logged out
            conditionStore.SerializeGameTime = IsRealTime() ? (ulong)Game.Current.CurrentTime.TotalMilliseconds : 0;

            // Copy properties
            PropertyCollection propertiesToCopy = Properties;
            PropertyInfoTable infoTable = GameDatabase.PropertyInfoTable;

            foreach (var kvp in propertiesToCopy)
            {
                PropertyInfoPrototype propertyInfoProto = infoTable.LookupPropertyInfo(kvp.Key.Enum)?.Prototype;
                if (propertyInfoProto == null)
                {
                    Logger.Warn("SaveToConditionStore(): propertyInfoProto == null");
                    continue;
                }

                if (propertyInfoProto.SerializeConditionSrcToCondition == false)
                    continue;

                // Store ItemLevel, which usually does not persist, in a separate property
                if (kvp.Key.Enum == PropertyEnum.ItemLevel)
                {
                    conditionStore.Properties[PropertyEnum.ConditionItemLevel] = kvp.Value;
                    continue;
                }

                // Skip non-persistent properties
                if (propertyInfoProto.ReplicateToDatabase == DatabasePolicy.None)
                    continue;

                // Copy persistent properties for serialization
                conditionStore.Properties.CopyProperty(propertiesToCopy, kvp.Key);
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

        public void ResetStartTimeFromPaused()
        {
            if (IsPaused == false)
                return;

            _startTime += Game.Current.CurrentTime - _pauseTime;
        }

        public void SetDuration(long duration)
        {
            if (duration <= 0)
                Logger.Warn("SetDuration(): duration <= 0");

            _durationMS = duration < 0 ? 0 : duration;
        }

        public void RunEvalPartyBoost()
        {
            EvalPrototype[] evals = _conditionPrototype?.EvalPartyBoost;
            if (evals.HasValue() == false)
                return;

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetVar_PropertyCollectionPtr(EvalContext.Default, Properties);

            foreach (EvalPrototype evalPartyBoost in evals)
            {
                if (Eval.RunBool(evalPartyBoost, evalContext) == false)
                    Logger.Warn($"RunEvalPartyBoost(): EvalPartyBoost failed in condition [{this}]");
            }
        }

        public bool ShouldStartPaused(Region region)
        {
            if (IsPauseDurationCountdown())
                return true;

            if (region?.PausesBoostConditions() == true && IsBoost())
                return true;

            return false;
        }

        public bool IsPersistToDB()
        {
            if (_conditionPrototype == null) return Logger.WarnReturn(false, "IsPersistToDB(): _conditionPrototype == null");
            return _conditionPrototype.PersistToDB;
        }

        public bool IsRealTime()
        {
            if (_conditionPrototype == null) return Logger.WarnReturn(false, "IsRealTime(): _conditionPrototype == null");
            return _conditionPrototype.RealTime;
        }

        public bool IsBoost()
        {
            if (_conditionPrototype == null) return Logger.WarnReturn(false, "IsBoost(): _conditionPrototype == null");
            return _conditionPrototype.IsBoost;
        }

        public bool IsPartyBoost()
        {
            if (_conditionPrototype == null) return Logger.WarnReturn(false, "IsPartyBoost(): _conditionPrototype == null");
            return _conditionPrototype.IsPartyBoost;
        }

        public bool IsHitReactCondition()
        {
            if (_conditionPrototype == null) return Logger.WarnReturn(false, "IsHitReactCondition(): _conditionPrototype == null");
            return _conditionPrototype.IsHitReactCondition;
        }

        public bool OverridesHitReactConditions()
        {
            return Properties[PropertyEnum.Knockback] ||
                   Properties[PropertyEnum.Knockdown] ||
                   Properties[PropertyEnum.Knockup] ||
                   Properties[PropertyEnum.Stunned] ||
                   Properties[PropertyEnum.Mesmerized] ||
                   Properties[PropertyEnum.NPCAmbientLock];
        }

        public bool IsTransferToCurrentAvatar()
        {
            if (_conditionPrototype == null) return Logger.WarnReturn(false, "IsTransferToCurrentAvatar(): _conditionPrototype == null");
            return _conditionPrototype.TransferToCurrentAvatar;
        }

        public bool IsPauseDurationCountdown()
        {
            if (_conditionPrototype == null) return Logger.WarnReturn(false, "IsPauseDurationCountdown(): _conditionPrototype == null");
            return _conditionPrototype.PauseDurationCountdown;
        }

        public bool ShouldApplyOverTimeEffectsToOriginator()
        {
            if (_conditionPrototype == null) return Logger.WarnReturn(false, "IsPauseDurationCountdown(): _conditionPrototype == null");
            return _conditionPrototype.ApplyOverTimeEffectsToOriginator;
        }

        public bool ShouldApplyInitialTickImmediately()
        {
            if (_conditionPrototype == null) return Logger.WarnReturn(false, "ShouldApplyInitialTickImmediately(): _conditionPrototype == null");
            return _conditionPrototype.ApplyInitialTickImmediately;
        }

        public PrototypeId[] GetKeywords()
        {
            if (_conditionPrototype == null) return Logger.WarnReturn<PrototypeId[]>(null, "GetKeywords(): _conditionPrototype == null");
            return _conditionPrototype.Keywords;
        }

        public KeywordsMask GetKeywordsMask()
        {
            ConditionPrototype conditionProto = ConditionPrototype;
            if (conditionProto == null) return Logger.WarnReturn(KeywordsMask.Empty, "GetKeywordsMask(): conditionProto == null");

            return conditionProto.KeywordsMask;
        }

        public bool HasKeyword(KeywordPrototype keywordProto)
        {
            return _conditionPrototype.HasKeyword(keywordProto);
        }

        public bool HasKeyword(PrototypeId keywordProtoRef)
        {
            return _conditionPrototype.HasKeyword(keywordProtoRef);
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
