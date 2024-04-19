using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Powers
{
    [Flags]
    public enum ConditionSerializationFlags : uint
    {
        None                        = 0,
        NoCreatorId                 = 1 << 0,   // _creatorId == owner.Id
        NoUltimateCreatorId         = 1 << 1,   // _ultimateCreatorId == _creatorId
        NoConditionPrototypeRef     = 1 << 2,   // _conditionPrototypeRef == PrototypeId.Invalid
        NoCreatorPowerPrototypeRef  = 1 << 3,   // _creatorPowerPrototypeRef == PrototypeId.Invalid
        HasCreatorPowerIndex        = 1 << 4,   // _creatorPowerIndex == -1
        HasOwnerAssetRef            = 1 << 5,   // _ownerAssetRef != AssetId.Invalid
        HasPauseTime                = 1 << 6,   // _pauseTime == TimeSpan.Zero
        HasDuration                 = 1 << 7,   // _duration == 0
        IsDisabled                  = 1 << 8,   // _isEnabled == false
        OwnerAssetRefOverride       = 1 << 9,   // owner == null || owner.Id != _ultimateCreatorId || _ownerAssetRef != owner.EntityWorldAsset
        HasUpdateInterval           = 1 << 10,  // updateInterval from ConditionPrototype
        HasCancelOnFlags            = 1 << 11,  // cancelOnFalgs from ConditionPrototype
    }

    public class Condition
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private ConditionSerializationFlags _serializationFlags;
        private ulong _id;                                          // Condition id
        private ulong _creatorId;                                   // Entity id
        private ulong _ultimateCreatorId;                           // Entity id

        private PrototypeId _conditionPrototypeRef;
        private ConditionPrototype _conditionPrototype;
        private PrototypeId _creatorPowerPrototypeRef;
        private PowerPrototype _creatorPowerPrototype;
        private int _creatorPowerIndex = -1;

        private AssetId _ownerAssetRef;
        private TimeSpan _startTime;
        private TimeSpan _pauseTime;
        private long _duration;                                     // milliseconds, 7200000 == 2 hours
        private bool _isEnabled = true;
        private int _updateIntervalMS;                              // milliseconds
        private ReplicatedPropertyCollection _properties = new();
        private UInt32Flags _cancelOnFlags;

        private ulong _creatorPlayerId;                             // Player guid

        // Accessors
        // TODO: Replace setters with Initialize()
        public ConditionSerializationFlags SerializationFlags { get => _serializationFlags; set => _serializationFlags = value; }
        public ulong Id { get => _id; set => _id = value; }
        public ulong CreatorId { get => _creatorId; set => _creatorId = value; }
        public ulong UltimateCreatorId { get => _ultimateCreatorId; set => _ultimateCreatorId = value; }

        public PrototypeId ConditionPrototypeRef {
            get => _conditionPrototypeRef;
            set { _conditionPrototypeRef = value; _conditionPrototype = value.As<ConditionPrototype>(); } }
        public ConditionPrototype ConditionPrototype { get => _conditionPrototype; }

        public PrototypeId CreatorPowerPrototypeRef {
            get => _creatorPowerPrototypeRef;
            set { _creatorPowerPrototypeRef = value; _creatorPowerPrototype = value.As<PowerPrototype>(); } }
        public PowerPrototype CreatorPowerPrototype { get => _creatorPowerPrototype; }

        public int CreatorPowerIndex { get => _creatorPowerIndex; set => _creatorPowerIndex = value; }
        public AssetId OwnerAssetRef { get => _ownerAssetRef; set => _ownerAssetRef = value; }
        public TimeSpan StartTime { get => _startTime; set => _startTime = value; }
        public TimeSpan PauseTime { get => _pauseTime; set => _pauseTime = value; }
        public TimeSpan Duration { get => TimeSpan.FromMilliseconds(_duration); set => _duration = (long)value.TotalMilliseconds; }
        public bool IsEnabled { get => _isEnabled; set => _isEnabled = value; }
        public TimeSpan UpdateInterval { get => TimeSpan.FromMilliseconds(_updateIntervalMS); set => _updateIntervalMS = (int)value.TotalMilliseconds; }
        public ReplicatedPropertyCollection Properties { get => _properties; }
        public UInt32Flags CancelOnFlags { get => _cancelOnFlags; set => _cancelOnFlags = value; }

        public ulong CreatorPlayerId { get => _creatorPlayerId; set => _creatorPlayerId = value; }

        public bool IsPaused { get => _pauseTime != TimeSpan.Zero; }
        public TimeSpan ElapsedTime { get => IsPaused ? _pauseTime - _startTime : Clock.GameTime - _startTime; }
        public TimeSpan TimeRemaining { get => Duration - ElapsedTime; }

        public Condition() { }

        public bool Serialize(Archive archive, WorldEntity owner)
        {
            // NOTE: Rather than implementing ISerialize, conditions have their own serialization
            // thing that also requires an owner WorldEntity reference to determine serialization
            // flags.

            bool success = true;
            
            // if (archive.IsTransient) -> This wasn't originally used for persistent condition serialization

            if (archive.IsPacking)
            {
                // TODO: build serialization flags here
                uint serializationFlags = (uint)_serializationFlags;
                success &= Serializer.Transfer(archive, ref serializationFlags);

                success &= Serializer.Transfer(archive, ref _id);

                if (_serializationFlags.HasFlag(ConditionSerializationFlags.NoCreatorId) == false)
                    success &= Serializer.Transfer(archive, ref _creatorId);

                if (_serializationFlags.HasFlag(ConditionSerializationFlags.NoUltimateCreatorId) == false)
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

                if (_serializationFlags.HasFlag(ConditionSerializationFlags.OwnerAssetRefOverride))
                    success &= Serializer.Transfer(archive, ref _ownerAssetRef);

                success &= Serializer.TransferTimeAsDelta(archive, null, ref _startTime);

                if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasPauseTime))
                    success &= Serializer.TransferTimeAsDelta(archive, null, ref _pauseTime);

                if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasDuration))
                    success &= Serializer.Transfer(archive, ref _duration);

                if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasUpdateInterval))
                    success &= Serializer.Transfer(archive, ref _updateIntervalMS);

                success &= Serializer.Transfer(archive, ref _properties);

                if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasCancelOnFlags))
                {
                    uint cancelOnFlags = (uint)_cancelOnFlags;
                    success &= Serializer.Transfer(archive, ref cancelOnFlags);
                }
            }
            else
            {
                // Enabling this check is going to break individual message parsing
                //if (owner == null) return Logger.WarnReturn(false, "Serialize(): owner == null");

                uint serializationFlags = 0;
                success &= Serializer.Transfer(archive, ref serializationFlags);
                _serializationFlags = (ConditionSerializationFlags)serializationFlags;

                success &= Serializer.Transfer(archive, ref _id);

                // Default creator is the owner
                if (_serializationFlags.HasFlag(ConditionSerializationFlags.NoCreatorId))
                    _creatorId = owner != null ? owner.Id : 0;
                else
                    success &= Serializer.Transfer(archive, ref _creatorId);

                // Default ultimate creator is the creator
                _ultimateCreatorId = _creatorId;
                if (_serializationFlags.HasFlag(ConditionSerializationFlags.NoUltimateCreatorId) == false)
                    success &= Serializer.Transfer(archive, ref _ultimateCreatorId);

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

                // If a condition has a creator index, it means its prototype is a list mixin of the creator power
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

                // Make sure we have a prototype, or we won't be able to figure out the rest of data
                if (_conditionPrototype == null)
                    return Logger.ErrorReturn(false, $"Serialize(): _conditionPrototype == null");

                // Default owner asset is AssetId.Invalid
                _ownerAssetRef = AssetId.Invalid;

                if (_serializationFlags.HasFlag(ConditionSerializationFlags.OwnerAssetRefOverride))
                    success &= Serializer.Transfer(archive, ref _ownerAssetRef);
                else if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasOwnerAssetRef))
                    _ownerAssetRef = owner != null ? owner.EntityWorldAsset : AssetId.Invalid;

                // _startTime should always be present
                success &= Serializer.TransferTimeAsDelta(archive, null, ref _startTime);

                // Default pause time is TimeSpan.Zero (i.e. not paused)
                _pauseTime = TimeSpan.Zero;
                if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasPauseTime))
                    success &= Serializer.TransferTimeAsDelta(archive, null, ref _pauseTime);

                // Default duration is 0 ms, which means unlimited duration? (to be confirmed)
                _duration = 0;
                if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasDuration))
                    success &= Serializer.Transfer(archive, ref _duration);

                // For some reason _isEnabled is not updated during deserialization in the client.
                // ConditionCollection does call Condition::serializationFlagIsDisabled() during OnUnpackComplete() though.

                // Default update interval is taken from the condition prototype
                _updateIntervalMS = _conditionPrototype.UpdateIntervalMS;
                if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasUpdateInterval))
                    success &= Serializer.Transfer(archive, ref _updateIntervalMS);

                // Default cancel on flags are taken from the prototype
                _cancelOnFlags = _conditionPrototype.CancelOnFlags;
                if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasCancelOnFlags))
                {
                    uint cancelOnFlags = 0;
                    success &= Serializer.Transfer(archive, ref cancelOnFlags);
                    _cancelOnFlags = (UInt32Flags)cancelOnFlags;
                }
            }

            return success;
        }

        public void Decode(CodedInputStream stream)
        {
            _serializationFlags = (ConditionSerializationFlags)stream.ReadRawVarint32();
            _id = stream.ReadRawVarint64();

            if (_serializationFlags.HasFlag(ConditionSerializationFlags.NoCreatorId) == false)
                _creatorId = stream.ReadRawVarint64();

            if (_serializationFlags.HasFlag(ConditionSerializationFlags.NoUltimateCreatorId) == false)
                _ultimateCreatorId = stream.ReadRawVarint64();

            if (_serializationFlags.HasFlag(ConditionSerializationFlags.NoConditionPrototypeRef) == false)
                _conditionPrototypeRef = stream.ReadPrototypeRef<Prototype>();

            if (_serializationFlags.HasFlag(ConditionSerializationFlags.NoCreatorPowerPrototypeRef) == false)
                _creatorPowerPrototypeRef = stream.ReadPrototypeRef<Prototype>();

            if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasCreatorPowerIndex))
                _creatorPowerIndex = (int)stream.ReadRawVarint32();

            if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasOwnerAssetRef)
                && SerializationFlags.HasFlag(ConditionSerializationFlags.OwnerAssetRefOverride))
                _ownerAssetRef = (AssetId)stream.ReadRawVarint64();

            _startTime = TimeSpan.FromMilliseconds(stream.ReadRawInt64() + 1);  // 1 == gamestarttime

            if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasPauseTime))
                _pauseTime = TimeSpan.FromMilliseconds(stream.ReadRawInt64() + 1);  // 1 == gamestarttime

            if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasDuration))
                _duration = stream.ReadRawInt64();

            // For some reason _isEnabled is not updated during deserialization in the client.
            // ConditionCollection does call Condition::serializationFlagIsDisabled() during OnUnpackComplete() though.
            //_isEnabled = _serializationFlags.HasFlag(ConditionSerializationFlags.IsDisabled) == false;

            if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasUpdateInterval))
                _updateIntervalMS = stream.ReadRawInt32();

            _properties.Decode(stream);

            if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasCancelOnFlags))
                _cancelOnFlags = (UInt32Flags)stream.ReadRawVarint32();
        }

        public void Encode(CodedOutputStream stream)
        {
            // TODO: Generate serialization flags on serialization

            stream.WriteRawVarint32((uint)_serializationFlags);
            stream.WriteRawVarint64(_id);

            if (_serializationFlags.HasFlag(ConditionSerializationFlags.NoCreatorId) == false)
                stream.WriteRawVarint64(_creatorId);

            if (_serializationFlags.HasFlag(ConditionSerializationFlags.NoUltimateCreatorId) == false)
                stream.WriteRawVarint64(_ultimateCreatorId);

            if (_serializationFlags.HasFlag(ConditionSerializationFlags.NoConditionPrototypeRef) == false)
                stream.WritePrototypeRef<Prototype>(_conditionPrototypeRef);

            if (_serializationFlags.HasFlag(ConditionSerializationFlags.NoCreatorPowerPrototypeRef) == false)
                stream.WritePrototypeRef<Prototype>(_creatorPowerPrototypeRef);

            if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasCreatorPowerIndex))
                stream.WriteRawVarint32((uint)_creatorPowerIndex);

            if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasOwnerAssetRef)
                && _serializationFlags.HasFlag(ConditionSerializationFlags.OwnerAssetRefOverride))
                stream.WriteRawVarint64((ulong)_ownerAssetRef);

            stream.WriteRawInt64((long)_startTime.TotalMilliseconds - 1);   // 1 == gamestarttime

            if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasPauseTime))
                stream.WriteRawInt64((long)_pauseTime.TotalMilliseconds - 1);    // 1 == gamestarttime

            if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasDuration))
                stream.WriteRawInt64(_duration);

            if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasUpdateInterval))
                stream.WriteRawInt32(_updateIntervalMS);

            _properties.Encode(stream);

            if (_serializationFlags.HasFlag(ConditionSerializationFlags.HasCancelOnFlags))
                stream.WriteRawVarint32((uint)CancelOnFlags);
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            sb.AppendLine($"{nameof(_serializationFlags)}: {_serializationFlags}");
            sb.AppendLine($"{nameof(_id)}: {_id}");
            sb.AppendLine($"{nameof(_creatorId)}: {_creatorId}");
            sb.AppendLine($"{nameof(_ultimateCreatorId)}: {_ultimateCreatorId}");
            sb.AppendLine($"{nameof(_conditionPrototypeRef)}: {GameDatabase.GetPrototypeName(_conditionPrototypeRef)}");
            sb.AppendLine($"{nameof(_creatorPowerPrototypeRef)}: {GameDatabase.GetPrototypeName(_creatorPowerPrototypeRef)}");
            sb.AppendLine($"{nameof(_creatorPowerIndex)}: {_creatorPowerIndex}");
            sb.AppendLine($"{nameof(_ownerAssetRef)}: {GameDatabase.GetAssetName(_ownerAssetRef)}");
            sb.AppendLine($"{nameof(_startTime)}: {Clock.GameTimeToDateTime(_startTime)}");
            sb.AppendLine($"{nameof(_pauseTime)}: {(_pauseTime != TimeSpan.Zero ? Clock.GameTimeToDateTime(_pauseTime) : 0)}");
            sb.AppendLine($"{nameof(_duration)}: {TimeSpan.FromMilliseconds(_duration)}");
            sb.AppendLine($"{nameof(_isEnabled)}: {_isEnabled}");
            sb.AppendLine($"{nameof(_updateIntervalMS)}: {_updateIntervalMS}");
            sb.AppendLine($"{nameof(_properties)}: {_properties}");
            sb.AppendLine($"{nameof(_cancelOnFlags)}: {_cancelOnFlags}");

            return sb.ToString();
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
                PrototypeId propertyPrototypeRef = propertyInfo.PropertyInfoPrototypeRef;

                if (IsANegativeStatusEffectProperty(propertyPrototypeRef))
                {
                    containsNegativeStatusEffects = true;
                    outputList?.Add(propertyPrototypeRef);      // This can be null
                }
            }

            return containsNegativeStatusEffects;
        }
    }
}
