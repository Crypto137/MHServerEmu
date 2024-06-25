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
    [Flags]
    public enum PowerResultMessageFlags
    {
        None                        = 0,
        NoPowerOwnerEntityId        = 1 << 0,
        IsSelfTarget                = 1 << 1,
        NoUltimateOwnerEntityId     = 1 << 2,
        UltimateOwnerIsPowerOwner   = 1 << 3,
        HasResultFlags              = 1 << 4,
        HasPowerOwnerPosition       = 1 << 5,
        HasDamagePhysical           = 1 << 6,
        HasDamageEnergy             = 1 << 7,
        HasDamageMental             = 1 << 8,
        HasHealing                  = 1 << 9,
        HasPowerAssetRefOverride    = 1 << 10,
        HasTransferToEntityId       = 1 << 11
    }

    // Inherits from PowerEffectsPacket class
    // Related to PowerPayload?
    public class PowerResults : ISerialize
    {
        private static readonly Random Random = new();  // For testing, remove this later

        private AOINetworkPolicyValues _replicationPolicy;
        private PowerResultMessageFlags _messageFlags;
        private PrototypeId _powerPrototypeRef;
        private ulong _targetEntityId;
        private ulong _powerOwnerEntityId;
        private ulong _ultimateOwnerEntityId;
        private PowerResultFlags _resultFlags;

        // Note: these are fake "ForClient" numbers that don't take into account scaling for the number of players in a region
        // See PowerResults::SetDamageForClient(), PowerResults::SetHealingForClient(), PowerResults::GetTotalDamageForClient(), PowerResults::HasDamageForClient()
        // TODO: Combine damage types to a float array, cast to uint on serialization
        private uint _damagePhysical;
        private uint _damageEnergy;
        private uint _damageMental;
        private uint _healing;

        private AssetId _powerAssetRefOverride;
        private Vector3 _powerOwnerPosition = Vector3.Zero;
        private ulong _transferToEntityId;

        // TODO: Replace accessors with proper init implementation
        public AOINetworkPolicyValues ReplicationPolicy { get => _replicationPolicy; set => _replicationPolicy = value; }
        public PowerResultMessageFlags MessageFlags { get => _messageFlags; set => _messageFlags = value; }
        public PrototypeId PowerPrototypeRef { get => _powerPrototypeRef; set => _powerPrototypeRef = value; }          // PowerEffectsPacket::GetPowerPrototype()
        public ulong TargetEntityId { get => _targetEntityId; set => _targetEntityId = value; }                         // PowerEffectsPacket::GetTargetId()
        public ulong PowerOwnerEntityId { get => _powerOwnerEntityId; set => _powerOwnerEntityId = value; }             // PowerEffectsPacket::GetPowerOwnerId()
        public ulong UltimateOwnerEntityId { get => _ultimateOwnerEntityId; set => _ultimateOwnerEntityId = value; }    // PowerEffectsPacket::GetUltimateOwnerId()
        public PowerResultFlags ResultFlags { get => _resultFlags; set => _resultFlags = value; }
        public uint DamagePhysical { get => _damagePhysical; set => _damagePhysical = value; }
        public uint DamageEnergy { get => _damageEnergy; set => _damageEnergy = value; }
        public uint DamageMental { get => _damageMental; set => _damageMental = value; }
        public uint Healing { get => _healing; set => _healing = value; }
        public AssetId PowerAssetRefOverride { get => _powerAssetRefOverride; set => _powerAssetRefOverride = value; }
        public Vector3 PowerOwnerPosition { get => _powerOwnerPosition; set => _powerOwnerPosition = value; }
        public ulong TransferToEntityId { get => _transferToEntityId; set => _transferToEntityId = value; }

        public PowerResults() { }

        // This should be the only init method
        public void Init(ulong powerOwnerEntityId, ulong ultimateOwnerEntityId, ulong targetEntityId, Vector3 powerOwnerPosition,
            PrototypeId powerPrototypeRef, AssetId powerAssetRefOverride, bool isHostile)
        {
            _powerOwnerEntityId = powerOwnerEntityId;
            _ultimateOwnerEntityId = ultimateOwnerEntityId;
            _targetEntityId = targetEntityId;
            _powerOwnerPosition = powerOwnerPosition;
            _powerPrototypeRef = powerPrototypeRef;
            _powerAssetRefOverride = powerAssetRefOverride;

            SetFlag(PowerResultFlags.Hostile, isHostile);

            // TODO: Init damage array
            _damagePhysical = 0;
            _damageEnergy = 0;
            _damageMental = 0;
        }

        // TODO: These two Init implementations are temporary to simplify our damage hacks
        public void Init(NetMessageTryActivatePower tryActivatePower)
        {
            // Damage test
            _replicationPolicy = AOINetworkPolicyValues.AOIChannelProximity;
            _messageFlags = PowerResultMessageFlags.None;
            _powerPrototypeRef = (PrototypeId)tryActivatePower.PowerPrototypeId;
            _targetEntityId = tryActivatePower.IdTargetEntity;
            _powerOwnerEntityId = tryActivatePower.IdUserEntity;
            _messageFlags |= PowerResultMessageFlags.UltimateOwnerIsPowerOwner;    // UltimateOwnerEntityId same as PowerOwnerEntityId

            if (tryActivatePower.IdTargetEntity != 0)
            {
                _resultFlags = PowerResultFlags.Hostile;
                _messageFlags |= PowerResultMessageFlags.HasResultFlags;

                _powerOwnerPosition = new(tryActivatePower.TargetPosition);
                _messageFlags |= PowerResultMessageFlags.HasPowerOwnerPosition;

                if (_targetEntityId != _powerOwnerEntityId)
                {
                    _damagePhysical = (uint)Random.NextInt64(80, 120);
                    _messageFlags |= PowerResultMessageFlags.HasDamagePhysical;

                    if (Random.NextSingle() < 0.35f)
                    {
                        _damagePhysical = (uint)(_damagePhysical * 1.5f);
                        _resultFlags |= PowerResultFlags.Critical;
                        if (Random.NextSingle() < 0.35f)
                        {
                            _damagePhysical = (uint)(_damagePhysical * 1.5f);
                            _resultFlags |= PowerResultFlags.SuperCritical;
                        }
                    }
                }
            }
        }

        public void Init(NetMessageContinuousPowerUpdateToServer continuousPowerUpdate)
        {
            // Damage test
            _replicationPolicy = AOINetworkPolicyValues.AOIChannelProximity;
            _messageFlags = PowerResultMessageFlags.None;
            _powerPrototypeRef = (PrototypeId)continuousPowerUpdate.PowerPrototypeId;
            _targetEntityId = continuousPowerUpdate.IdTargetEntity;
            _messageFlags |= PowerResultMessageFlags.UltimateOwnerIsPowerOwner;    // UltimateOwnerEntityId same as PowerOwnerEntityId

            if (continuousPowerUpdate.IdTargetEntity != 0)
            {
                _resultFlags = PowerResultFlags.Hostile;
                _messageFlags |= PowerResultMessageFlags.HasResultFlags;

                _powerOwnerPosition = new(continuousPowerUpdate.TargetPosition);
                _messageFlags |= PowerResultMessageFlags.HasPowerOwnerPosition;

                _damagePhysical = 100;
                _messageFlags |= PowerResultMessageFlags.HasDamagePhysical;
            }
        }

        public bool Serialize(Archive archive)
        {
            // NOTE: PowerResults are deserialized in the client in GameConnection::handlePowerResultMessage()

            bool success = true;

            if (archive.IsPacking)
            {
                // build flags here
                uint messageFlags = (uint)_messageFlags;
                success &= Serializer.Transfer(archive, ref messageFlags);

                success &= Serializer.TransferPrototypeEnum<PowerPrototype>(archive, ref _powerPrototypeRef);
                success &= Serializer.Transfer(archive, ref _targetEntityId);

                if (_messageFlags.HasFlag(PowerResultMessageFlags.IsSelfTarget) == false && _messageFlags.HasFlag(PowerResultMessageFlags.NoPowerOwnerEntityId) == false)
                    success &= Serializer.Transfer(archive, ref _powerOwnerEntityId);

                if (_messageFlags.HasFlag(PowerResultMessageFlags.UltimateOwnerIsPowerOwner) == false && _messageFlags.HasFlag(PowerResultMessageFlags.NoUltimateOwnerEntityId) == false)
                    success &= Serializer.Transfer(archive, ref _ultimateOwnerEntityId);

                if (_messageFlags.HasFlag(PowerResultMessageFlags.HasResultFlags))
                {
                    ulong resultFlags = (ulong)_resultFlags;
                    success &= Serializer.Transfer(archive, ref resultFlags);
                }

                if (_messageFlags.HasFlag(PowerResultMessageFlags.HasDamagePhysical))
                    success &= Serializer.Transfer(archive, ref _damagePhysical);

                if (_messageFlags.HasFlag(PowerResultMessageFlags.HasDamageEnergy))
                    success &= Serializer.Transfer(archive, ref _damageEnergy);

                if (_messageFlags.HasFlag(PowerResultMessageFlags.HasDamageMental))
                    success &= Serializer.Transfer(archive, ref _damageMental);

                if (_messageFlags.HasFlag(PowerResultMessageFlags.HasHealing))
                    success &= Serializer.Transfer(archive, ref _healing);

                if (_messageFlags.HasFlag(PowerResultMessageFlags.HasPowerAssetRefOverride))
                    success &= Serializer.Transfer(archive, ref _powerAssetRefOverride);

                if (_messageFlags.HasFlag(PowerResultMessageFlags.HasPowerOwnerPosition))
                    success &= Serializer.TransferVectorFixed(archive, ref _powerOwnerPosition, 2);

                if (_messageFlags.HasFlag(PowerResultMessageFlags.HasTransferToEntityId))
                    success &= Serializer.Transfer(archive, ref _transferToEntityId);
            }
            else
            {
                uint messageFlags = 0;
                success &= Serializer.Transfer(archive, ref messageFlags);
                _messageFlags = (PowerResultMessageFlags)messageFlags;

                success &= Serializer.TransferPrototypeEnum<PowerPrototype>(archive, ref _powerPrototypeRef);
                success &= Serializer.Transfer(archive, ref _targetEntityId);

                if (_messageFlags.HasFlag(PowerResultMessageFlags.IsSelfTarget))
                    _powerOwnerEntityId = _targetEntityId;
                else if (_messageFlags.HasFlag(PowerResultMessageFlags.NoPowerOwnerEntityId) == false)
                    success &= Serializer.Transfer(archive, ref _powerOwnerEntityId);

                if (_messageFlags.HasFlag(PowerResultMessageFlags.UltimateOwnerIsPowerOwner))
                    _ultimateOwnerEntityId = _powerOwnerEntityId;
                else if (_messageFlags.HasFlag(PowerResultMessageFlags.NoUltimateOwnerEntityId) == false)
                    success &= Serializer.Transfer(archive, ref _ultimateOwnerEntityId);

                if (_messageFlags.HasFlag(PowerResultMessageFlags.HasResultFlags))
                {
                    ulong resultFlags = 0;
                    success &= Serializer.Transfer(archive, ref resultFlags);
                    _resultFlags = (PowerResultFlags)resultFlags;
                }

                if (_messageFlags.HasFlag(PowerResultMessageFlags.HasDamagePhysical))
                    success &= Serializer.Transfer(archive, ref _damagePhysical);

                if (_messageFlags.HasFlag(PowerResultMessageFlags.HasDamageEnergy))
                    success &= Serializer.Transfer(archive, ref _damageEnergy);

                if (_messageFlags.HasFlag(PowerResultMessageFlags.HasDamageMental))
                    success &= Serializer.Transfer(archive, ref _damageMental);

                if (_messageFlags.HasFlag(PowerResultMessageFlags.HasHealing))
                    success &= Serializer.Transfer(archive, ref _healing);

                if (_messageFlags.HasFlag(PowerResultMessageFlags.HasPowerAssetRefOverride))
                    success &= Serializer.Transfer(archive, ref _powerAssetRefOverride);

                if (_messageFlags.HasFlag(PowerResultMessageFlags.HasPowerOwnerPosition))
                    success &= Serializer.Transfer(archive, ref _powerOwnerPosition);

                if (_messageFlags.HasFlag(PowerResultMessageFlags.HasTransferToEntityId))
                    success &= Serializer.Transfer(archive, ref _transferToEntityId);
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

        public NetMessagePowerResult ToProtobuf()
        {
            return NetMessagePowerResult.CreateBuilder().SetArchiveData(ToByteString()).Build();
        }

        public void SetFlag(PowerResultFlags flag, bool value)
        {
            if (value)
                _resultFlags |= flag;
            else
                _resultFlags &= ~flag;
        }

        public void SetFlags(PowerResultFlags flags)
        {
            _resultFlags = flags;
        }

        public bool TestFlag(PowerResultFlags flag)
        {
            return _resultFlags.HasFlag(flag);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(_replicationPolicy)}: {_replicationPolicy}");
            sb.AppendLine($"{nameof(_messageFlags)}: {_messageFlags}");
            sb.AppendLine($"{nameof(_powerPrototypeRef)}: {GameDatabase.GetPrototypeName(_powerPrototypeRef)}");
            sb.AppendLine($"{nameof(_targetEntityId)}: {_targetEntityId}");
            sb.AppendLine($"{nameof(_powerOwnerEntityId)}: {_powerOwnerEntityId}");
            sb.AppendLine($"{nameof(_ultimateOwnerEntityId)}: {_ultimateOwnerEntityId}");
            sb.AppendLine($"{nameof(_resultFlags)}: {_resultFlags}");
            sb.AppendLine($"{nameof(_damagePhysical)}: {_damagePhysical}");
            sb.AppendLine($"{nameof(_damageEnergy)}: {_damageEnergy}");
            sb.AppendLine($"{nameof(_damageMental)}: {_damageMental}");
            sb.AppendLine($"{nameof(_healing)}: {_healing}");
            sb.AppendLine($"{nameof(_powerAssetRefOverride)}: {GameDatabase.GetAssetName(_powerAssetRefOverride)}");
            sb.AppendLine($"{nameof(_powerOwnerPosition)}: {_powerOwnerPosition}");
            sb.AppendLine($"{nameof(_transferToEntityId)}: {_transferToEntityId}");
            return sb.ToString();
        }
    }
}
