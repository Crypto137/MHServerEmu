using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Powers
{
    [Flags]
    public enum ConditionSerializationFlags : uint
    {
        None                        = 0,
        NoCreatorId                 = 1 << 0,
        NoUltimateCreatorId         = 1 << 1,
        NoConditionPrototypeId      = 1 << 2,
        NoCreatorPowerPrototypeId   = 1 << 3,
        HasCreatorPowerIndex        = 1 << 4,
        HasOwnerAssetRef            = 1 << 5,   // _ownerAssetRef != AssetId.Invalid
        HasPauseTime                = 1 << 6,
        HasDuration                 = 1 << 7,
        IsDisabled                  = 1 << 8,
        OwnerAssetRefOverride       = 1 << 9,   // owner == null || owner.GetId() != UltimateCreatorId || AssetDataRef != owner.GetOriginalWorldAsset()
        HasUpdateInterval           = 1 << 10,
        HasCancelOnFlags            = 1 << 11,
    }

    public class Condition
    {
        private ConditionSerializationFlags _serializationFlags;
        private ulong _id;                                          // Condition id
        private ulong _creatorId;                                   // Entity id
        private ulong _ultimateCreatorId;                           // Entity id
        private PrototypeId _conditionPrototypeRef;
        private PrototypeId _creatorPowerPrototypeRef;
        private int _creatorPowerIndex;
        private AssetId _ownerAssetRef;
        private TimeSpan _startTime;
        private TimeSpan _pauseTime;
        private long _duration;                                     // ms, 7200000 == 2 hours
        private bool _isEnabled = true;
        private int _updateInterval;                                // ms
        private ReplicatedPropertyCollection _properties = new();
        private UInt32Flags _cancelOnFlags;

        private ulong _creatorPlayerId;                             // Player guid

        // Accessors
        // TODO: Replace setters with Initialize()
        public ConditionSerializationFlags SerializationFlags { get => _serializationFlags; set => _serializationFlags = value; }
        public ulong Id { get => _id; set => _id = value; }
        public ulong CreatorId { get => _creatorId; set => _creatorId = value; }
        public ulong UltimateCreatorId { get => _ultimateCreatorId; set => _ultimateCreatorId = value; }
        public PrototypeId ConditionPrototypeRef { get => _conditionPrototypeRef; set => _conditionPrototypeRef = value; }
        public PrototypeId CreatorPowerPrototypeRef { get => _creatorPowerPrototypeRef; set => _creatorPowerPrototypeRef = value; }
        public int CreatorPowerIndex { get => _creatorPowerIndex; set => _creatorPowerIndex = value; }
        public AssetId OwnerAssetRef { get => _ownerAssetRef; set => _ownerAssetRef = value; }
        public TimeSpan StartTime { get => _startTime; set => _startTime = value; }
        public TimeSpan PauseTime { get => _pauseTime; set => _pauseTime = value; }
        public TimeSpan Duration { get => TimeSpan.FromMilliseconds(_duration); set => _duration = (long)value.TotalMilliseconds; }
        public bool IsEnabled { get => _isEnabled; set => _isEnabled = value; }
        public TimeSpan UpdateInterval { get => TimeSpan.FromMilliseconds(_updateInterval); set => _updateInterval = (int)value.TotalMilliseconds; }
        public ReplicatedPropertyCollection Properties { get => _properties; }
        public UInt32Flags CancelOnFlags { get => _cancelOnFlags; set => _cancelOnFlags = value; }

        public ulong CreatorPlayerId { get => _creatorPlayerId; set => _creatorPlayerId = value; }

        public bool IsPaused { get => _pauseTime != TimeSpan.Zero; }
        public TimeSpan ElapsedTime { get => IsPaused ? _pauseTime - _startTime : Clock.GameTime - _startTime; }
        public TimeSpan TimeRemaining { get => Duration - ElapsedTime; }

        public Condition() { }

        public void Decode(CodedInputStream stream)
        {
            _serializationFlags = (ConditionSerializationFlags)stream.ReadRawVarint32();
            _id = stream.ReadRawVarint64();

            if (_serializationFlags.HasFlag(ConditionSerializationFlags.NoCreatorId) == false)
                _creatorId = stream.ReadRawVarint64();

            if (_serializationFlags.HasFlag(ConditionSerializationFlags.NoUltimateCreatorId) == false)
                _ultimateCreatorId = stream.ReadRawVarint64();

            if (_serializationFlags.HasFlag(ConditionSerializationFlags.NoConditionPrototypeId) == false)
                _conditionPrototypeRef = stream.ReadPrototypeRef<Prototype>();

            if (_serializationFlags.HasFlag(ConditionSerializationFlags.NoCreatorPowerPrototypeId) == false)
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
                _updateInterval = stream.ReadRawInt32();

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

            if (_serializationFlags.HasFlag(ConditionSerializationFlags.NoConditionPrototypeId) == false)
                stream.WritePrototypeRef<Prototype>(_conditionPrototypeRef);

            if (_serializationFlags.HasFlag(ConditionSerializationFlags.NoCreatorPowerPrototypeId) == false)
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
                stream.WriteRawInt32(_updateInterval);

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
