using System.Text;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Powers
{
    // TODO: This probably belongs in Power

    public class ActivatePowerArchive : ISerialize
    {
        private AOINetworkPolicyValues _replicationPolicy;
        private ActivatePowerMessageFlags _flags;
        private ulong _userEntityId;
        private ulong _targetEntityId;
        private PrototypeId _powerPrototypeRef;
        private PrototypeId _triggeringPowerPrototypeRef;   // previously parentPowerPrototypeId
        private Vector3 _userPosition = Vector3.Zero;
        private Vector3 _targetPosition = Vector3.Zero;
        private TimeSpan _movementTime;                       // previously float moveTimeSeconds
        private TimeSpan _variableActivationTime;
        private uint _powerRandomSeed;
        private uint _fxRandomSeed;

        // TODO: Remove accessors and implement PowerActivationSettings
        public AOINetworkPolicyValues ReplicationPolicy { get => _replicationPolicy; set => _replicationPolicy = value; }
        public ActivatePowerMessageFlags Flags { get => _flags; set => _flags = value; }
        public ulong UserEntityId { get => _userEntityId; set => _userEntityId = value; }
        public ulong TargetEntityId { get => _targetEntityId; set => _targetEntityId = value; }
        public PrototypeId PowerPrototypeRef { get => _powerPrototypeRef; set => _powerPrototypeRef = value; }
        public PrototypeId TriggeringPowerPrototypeRef { get => _triggeringPowerPrototypeRef; set => _triggeringPowerPrototypeRef = value; }     
        public Vector3 UserPosition { get => _userPosition; set => _userPosition = value; }
        public Vector3 TargetPosition { get => _targetPosition; set => _targetPosition = value; }
        public TimeSpan MovementTime { get => _movementTime; set => _movementTime = value; }
        public TimeSpan VariableActivationTime { get => _variableActivationTime; set => _variableActivationTime = value; }
        public uint PowerRandomSeed { get => _powerRandomSeed; set => _powerRandomSeed = value; }
        public uint FXRandomSeed { get => _fxRandomSeed; set => _fxRandomSeed = value; }

        public ActivatePowerArchive() { }

        public void Initialize(NetMessageTryActivatePower tryActivatePower, Vector3 userPosition)
        {
            _replicationPolicy = AOINetworkPolicyValues.AOIChannelProximity;
            _flags = ActivatePowerMessageFlags.None;

            _userEntityId = tryActivatePower.IdUserEntity;
            _powerPrototypeRef = (PrototypeId)tryActivatePower.PowerPrototypeId;
            _userPosition = userPosition;        // derive this from tryActivatePower.MovementSpeed?

            // IdTargetEntity
            if (tryActivatePower.HasIdTargetEntity)
            {
                if (tryActivatePower.IdTargetEntity == _userEntityId)
                    _flags |= ActivatePowerMessageFlags.TargetIsUser;    // flag0 means the user is the target
                else
                    _targetEntityId = tryActivatePower.IdTargetEntity;
            }

            // TriggeringPowerPrototypeId
            if (tryActivatePower.HasTriggeringPowerPrototypeId)
            {
                _triggeringPowerPrototypeRef = (PrototypeId)tryActivatePower.TriggeringPowerPrototypeId;
                _flags |= ActivatePowerMessageFlags.HasTriggeringPowerPrototypeRef;
            }

            // TargetPosition
            if (tryActivatePower.HasTargetPosition)
            {
                _targetPosition = new(tryActivatePower.TargetPosition);
                _flags |= ActivatePowerMessageFlags.HasTargetPosition;
            }
            else
            {
                _flags |= ActivatePowerMessageFlags.TargetPositionIsUserPosition;    // TargetPosition == UserPosition
            }

            // MovementTimeMS
            if (tryActivatePower.HasMovementTimeMS)
            {
                _movementTime = TimeSpan.FromMilliseconds(tryActivatePower.MovementTimeMS);
                _flags |= ActivatePowerMessageFlags.HasMovementTime;
            }

            // VariableActivationTime - where does this come from?

            // PowerRandomSeed
            if (tryActivatePower.HasPowerRandomSeed)
            {
                _powerRandomSeed = tryActivatePower.PowerRandomSeed;
                _flags |= ActivatePowerMessageFlags.HasPowerRandomSeed;
            }

            // FXRandomSeed - NOTE: FXRandomSeed is marked as required in protobuf, so it should always be present
            if (tryActivatePower.HasFxRandomSeed)
            {
                _fxRandomSeed = tryActivatePower.FxRandomSeed;
                _flags |= ActivatePowerMessageFlags.HasFXRandomSeed;
            }
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            if (archive.IsPacking)
            {
                // TODO: build serialization flags here
                uint flags = (uint)_flags;
                success &= Serializer.Transfer(archive, ref flags);

                // User and target entity ids
                success &= Serializer.Transfer(archive, ref _userEntityId);
                if (_flags.HasFlag(ActivatePowerMessageFlags.TargetIsUser) == false)
                    success &= Serializer.Transfer(archive, ref _targetEntityId);

                // Power prototype refs
                success &= Serializer.TransferPrototypeEnum<PowerPrototype>(archive, ref _powerPrototypeRef);
                if (_flags.HasFlag(ActivatePowerMessageFlags.HasTriggeringPowerPrototypeRef))
                    success &= Serializer.TransferPrototypeEnum<PowerPrototype>(archive, ref _triggeringPowerPrototypeRef);

                // Positions
                success &= Serializer.TransferVectorFixed(archive, ref _userPosition, 2);
                if (_flags.HasFlag(ActivatePowerMessageFlags.HasTargetPosition))
                {
                    Vector3 offset = _targetPosition - _userPosition;   // Target position is relative to user when encoded
                    success &= Serializer.TransferVectorFixed(archive, ref offset, 2);
                }

                // Movement time for travel powers
                if (_flags.HasFlag(ActivatePowerMessageFlags.HasMovementTime))
                {
                    uint movementTimeMS = (uint)_movementTime.TotalMilliseconds;
                    success &= Serializer.Transfer(archive, ref movementTimeMS);
                }

                // Variable activation time - what is this? For chargeable / holdable powers?
                if (_flags.HasFlag(ActivatePowerMessageFlags.HasVariableActivationTime))
                {
                    uint variableActivationTimeMS = (uint)_variableActivationTime.TotalMilliseconds;
                    success &= Serializer.Transfer(archive, ref variableActivationTimeMS);
                }

                // Random seeds for keeping server / client in sync
                if (_flags.HasFlag(ActivatePowerMessageFlags.HasPowerRandomSeed))
                    success &= Serializer.Transfer(archive, ref _powerRandomSeed);

                // NOTE: FXRandomSeed is marked as required in the NetMessageTryActivatePower protobuf, so it should probably always be present
                if (_flags.HasFlag(ActivatePowerMessageFlags.HasFXRandomSeed))
                    success &= Serializer.Transfer(archive, ref _fxRandomSeed);
            }
            else
            {
                uint flags = 0;
                success &= Serializer.Transfer(archive, ref flags);
                _flags = (ActivatePowerMessageFlags)flags;

                success &= Serializer.Transfer(archive, ref _userEntityId);
                if (_flags.HasFlag(ActivatePowerMessageFlags.TargetIsUser))
                    _targetEntityId = _userEntityId;
                else
                    success &= Serializer.Transfer(archive, ref _targetEntityId);

                success &= Serializer.TransferPrototypeEnum<PowerPrototype>(archive, ref _powerPrototypeRef);
                if (_flags.HasFlag(ActivatePowerMessageFlags.HasTriggeringPowerPrototypeRef))
                    success &= Serializer.TransferPrototypeEnum<PowerPrototype>(archive, ref _triggeringPowerPrototypeRef);

                success &= Serializer.TransferVectorFixed(archive, ref _userPosition, 2);
                if (_flags.HasFlag(ActivatePowerMessageFlags.TargetPositionIsUserPosition))
                    _targetPosition = _userPosition;
                else if (_flags.HasFlag(ActivatePowerMessageFlags.HasTargetPosition))
                {
                    // Target position is encoded as an offset from user position
                    Vector3 offset = Vector3.Zero;
                    success &= Serializer.TransferVectorFixed(archive, ref offset, 2);
                    _targetPosition = offset + _userPosition;
                }

                if (_flags.HasFlag(ActivatePowerMessageFlags.HasMovementTime))
                {
                    uint movementTimeMS = 0;
                    success &= Serializer.Transfer(archive, ref movementTimeMS);
                    _movementTime = TimeSpan.FromMilliseconds(movementTimeMS);
                }

                if (_flags.HasFlag(ActivatePowerMessageFlags.HasVariableActivationTime))
                {
                    uint variableActivationTimeMS = 0;
                    success &= Serializer.Transfer(archive, ref variableActivationTimeMS);
                    _variableActivationTime = TimeSpan.FromMilliseconds(variableActivationTimeMS);
                }

                if (_flags.HasFlag(ActivatePowerMessageFlags.HasPowerRandomSeed))
                    success &= Serializer.Transfer(archive, ref _powerRandomSeed);

                if (_flags.HasFlag(ActivatePowerMessageFlags.HasFXRandomSeed))
                    success &= Serializer.Transfer(archive, ref _fxRandomSeed);
            }

            return success;
        }

        public ByteString ToByteString()
        {
            using (Archive archive = new(ArchiveSerializeType.Replication, (ulong)_replicationPolicy))
            {
                Serialize(archive);
                return archive.ToByteString();
            }
        }

        public NetMessageActivatePower ToProtobuf()
        {
            return NetMessageActivatePower.CreateBuilder().SetArchiveData(ToByteString()).Build();
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(_replicationPolicy)}: {_replicationPolicy}");
            sb.AppendLine($"{nameof(_flags)}: {_flags}");
            sb.AppendLine($"{nameof(_userEntityId)}: {_userEntityId}");
            sb.AppendLine($"{nameof(_targetEntityId)}: {_targetEntityId}");
            sb.AppendLine($"{nameof(_powerPrototypeRef)}: {GameDatabase.GetPrototypeName(_powerPrototypeRef)}");
            sb.AppendLine($"{nameof(_triggeringPowerPrototypeRef)}: {GameDatabase.GetPrototypeName(_triggeringPowerPrototypeRef)}");
            sb.AppendLine($"{nameof(_userPosition)}: {_userPosition}");
            sb.AppendLine($"{nameof(_targetPosition)}: {_targetPosition}");
            sb.AppendLine($"{nameof(_movementTime)}: {_movementTime}");
            sb.AppendLine($"{nameof(_variableActivationTime)}: {_variableActivationTime}");
            sb.AppendLine($"{nameof(_powerRandomSeed)}: {_powerRandomSeed}");
            sb.AppendLine($"{nameof(_fxRandomSeed)}: {_fxRandomSeed}");
            return sb.ToString();
        }
    }
}
