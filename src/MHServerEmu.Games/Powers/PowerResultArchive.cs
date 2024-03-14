using System.Text;
using Gazillion;
using Google.ProtocolBuffers;
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

    [Flags]
    public enum PowerResultFlags    // PowerResults::getStringForFlag
    {
        None            = 0,
        Hostile         = 1 << 0,
        Proc            = 1 << 1,
        OverTime        = 1 << 2,
        Critical        = 1 << 3,
        Dodged          = 1 << 4,
        Resisted        = 1 << 5,
        Blocked         = 1 << 6,
        SuperCritical   = 1 << 7,   // Brutal Strike
        Unaffected      = 1 << 8,
        Teleport        = 1 << 9,
        NoDamage        = 1 << 10,
        Resurrect       = 1 << 11,
        InstantKill     = 1 << 12
    }

    public class PowerResultArchive
    {
        private static readonly Random Random = new();  // For testing, remove this later

        public AOINetworkPolicyValues ReplicationPolicy { get; set; }
        public PowerResultMessageFlags Flags { get; set; }
        public PrototypeId PowerPrototypeId { get; set; }
        public ulong TargetEntityId { get; set; }
        public ulong PowerOwnerEntityId { get; set; }
        public ulong UltimateOwnerEntityId { get; set; }
        public PowerResultFlags ResultFlags { get; set; }
        public uint DamagePhysical { get; set; }
        public uint DamageEnergy { get; set; }
        public uint DamageMental { get; set; }
        public uint Healing { get; set; }
        public AssetId PowerAssetRefOverride { get; set; }
        public Vector3 PowerOwnerPosition { get; set; }
        public ulong TransferToEntityId { get; set; }

        public PowerResultArchive(ByteString data)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data.ToByteArray());

            ReplicationPolicy = (AOINetworkPolicyValues)stream.ReadRawVarint32();
            Flags = (PowerResultMessageFlags)stream.ReadRawVarint32();
            PowerPrototypeId = stream.ReadPrototypeRef<PowerPrototype>();
            TargetEntityId = stream.ReadRawVarint64();

            if (Flags.HasFlag(PowerResultMessageFlags.IsSelfTarget))
                PowerOwnerEntityId = TargetEntityId;
            else if (Flags.HasFlag(PowerResultMessageFlags.NoPowerOwnerEntityId) == false)
                PowerOwnerEntityId = stream.ReadRawVarint64();
            
            if (Flags.HasFlag(PowerResultMessageFlags.UltimateOwnerIsPowerOwner))
                UltimateOwnerEntityId = PowerOwnerEntityId;
            else if (Flags.HasFlag(PowerResultMessageFlags.NoUltimateOwnerEntityId) == false)
                UltimateOwnerEntityId = stream.ReadRawVarint64();

            if (Flags.HasFlag(PowerResultMessageFlags.HasResultFlags))
                ResultFlags = (PowerResultFlags)stream.ReadRawVarint64();

            if (Flags.HasFlag(PowerResultMessageFlags.HasDamagePhysical))
                DamagePhysical = stream.ReadRawVarint32();

            if (Flags.HasFlag(PowerResultMessageFlags.HasDamageEnergy))
                DamageEnergy = stream.ReadRawVarint32();

            if (Flags.HasFlag(PowerResultMessageFlags.HasDamageMental))
                DamageMental = stream.ReadRawVarint32();

            if (Flags.HasFlag(PowerResultMessageFlags.HasHealing))
                Healing = stream.ReadRawVarint32();

            if (Flags.HasFlag(PowerResultMessageFlags.HasPowerAssetRefOverride))
                PowerAssetRefOverride = (AssetId)stream.ReadRawVarint64();

            if (Flags.HasFlag(PowerResultMessageFlags.HasPowerOwnerPosition))
                PowerOwnerPosition = new(stream, 2);

            if (Flags.HasFlag(PowerResultMessageFlags.HasTransferToEntityId))
                TransferToEntityId = stream.ReadRawVarint64();
        }

        public PowerResultArchive(NetMessageTryActivatePower tryActivatePower)
        {
            // Damage test
            ReplicationPolicy = AOINetworkPolicyValues.AOIChannelProximity;
            Flags = PowerResultMessageFlags.None;
            PowerPrototypeId = (PrototypeId)tryActivatePower.PowerPrototypeId;
            TargetEntityId = tryActivatePower.IdTargetEntity;
            PowerOwnerEntityId = tryActivatePower.IdUserEntity;
            Flags |= PowerResultMessageFlags.UltimateOwnerIsPowerOwner;    // UltimateOwnerEntityId same as PowerOwnerEntityId

            if (tryActivatePower.IdTargetEntity != 0)
            {
                ResultFlags = PowerResultFlags.Hostile;
                Flags |= PowerResultMessageFlags.HasResultFlags;

                PowerOwnerPosition = new(tryActivatePower.TargetPosition);
                Flags |= PowerResultMessageFlags.HasPowerOwnerPosition;

                if (TargetEntityId != PowerOwnerEntityId)
                {
                    DamagePhysical = (uint)Random.NextInt64(80, 120);
                    Flags |= PowerResultMessageFlags.HasDamagePhysical;

                    if (Random.NextSingle() < 0.35f)
                    {
                        DamagePhysical = (uint)(DamagePhysical * 1.5f);
                        ResultFlags |= PowerResultFlags.Critical;
                        if (Random.NextSingle() < 0.35f)
                        {
                            DamagePhysical = (uint)(DamagePhysical * 1.5f);
                            ResultFlags |= PowerResultFlags.SuperCritical;
                        }
                    }
                }
            }
        }

        public PowerResultArchive(NetMessageContinuousPowerUpdateToServer continuousPowerUpdate)
        {
            // damage test
            ReplicationPolicy = AOINetworkPolicyValues.AOIChannelProximity;
            Flags = PowerResultMessageFlags.None;
            PowerPrototypeId = (PrototypeId)continuousPowerUpdate.PowerPrototypeId;
            TargetEntityId = continuousPowerUpdate.IdTargetEntity;
            Flags |= PowerResultMessageFlags.UltimateOwnerIsPowerOwner;    // UltimateOwnerEntityId same as PowerOwnerEntityId

            if (continuousPowerUpdate.IdTargetEntity != 0)
            {
                ResultFlags = PowerResultFlags.Hostile;
                Flags |= PowerResultMessageFlags.HasResultFlags;

                PowerOwnerPosition = new(continuousPowerUpdate.TargetPosition);
                Flags |= PowerResultMessageFlags.HasPowerOwnerPosition;

                DamagePhysical = 100;
                Flags |= PowerResultMessageFlags.HasDamagePhysical;
            }
        }

        public PowerResultArchive() { }

        public ByteString Serialize()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint32((uint)ReplicationPolicy);
                cos.WriteRawVarint32((uint)Flags);
                cos.WritePrototypeRef<PowerPrototype>(PowerPrototypeId);
                cos.WriteRawVarint64(TargetEntityId);

                if (Flags.HasFlag(PowerResultMessageFlags.IsSelfTarget) == false && Flags.HasFlag(PowerResultMessageFlags.NoPowerOwnerEntityId) == false)
                    cos.WriteRawVarint64(PowerOwnerEntityId);

                if (Flags.HasFlag(PowerResultMessageFlags.UltimateOwnerIsPowerOwner) == false && Flags.HasFlag(PowerResultMessageFlags.NoUltimateOwnerEntityId) == false)
                    cos.WriteRawVarint64(UltimateOwnerEntityId);

                if (Flags.HasFlag(PowerResultMessageFlags.HasResultFlags))
                    cos.WriteRawVarint64((ulong)ResultFlags);

                if (Flags.HasFlag(PowerResultMessageFlags.HasDamagePhysical))
                    cos.WriteRawVarint32(DamagePhysical);

                if (Flags.HasFlag(PowerResultMessageFlags.HasDamageEnergy))
                    cos.WriteRawVarint32(DamageEnergy);

                if (Flags.HasFlag(PowerResultMessageFlags.HasDamageMental))
                    cos.WriteRawVarint32(DamageMental);

                if (Flags.HasFlag(PowerResultMessageFlags.HasHealing))
                    cos.WriteRawVarint32(Healing);

                if (Flags.HasFlag(PowerResultMessageFlags.HasPowerAssetRefOverride))
                    cos.WriteRawVarint64((ulong)PowerAssetRefOverride);

                if (Flags.HasFlag(PowerResultMessageFlags.HasPowerOwnerPosition))
                    PowerOwnerPosition.Encode(cos, 2);

                if (Flags.HasFlag(PowerResultMessageFlags.HasTransferToEntityId))
                    cos.WriteRawVarint64(TransferToEntityId);

                cos.Flush();
                return ByteString.CopyFrom(ms.ToArray());
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"ReplicationPolicy: {ReplicationPolicy}");
            sb.AppendLine($"Flags: {Flags}");
            sb.AppendLine($"PowerPrototypeId: {GameDatabase.GetPrototypeName(PowerPrototypeId)}");
            sb.AppendLine($"TargetEntityId: {TargetEntityId}");
            sb.AppendLine($"PowerOwnerEntityId: {PowerOwnerEntityId}");
            sb.AppendLine($"UltimateOwnerEntityId: {UltimateOwnerEntityId}");
            sb.AppendLine($"ResultFlags: {ResultFlags}");
            sb.AppendLine($"DamagePhysical: {DamagePhysical}");
            sb.AppendLine($"DamageEnergy: {DamageEnergy}");
            sb.AppendLine($"DamageMental: {DamageMental}");
            sb.AppendLine($"Healing: {Healing}");
            sb.AppendLine($"PowerAssetRefOverride: {PowerAssetRefOverride}");
            sb.AppendLine($"PowerOwnerPosition: {PowerOwnerPosition}");
            sb.AppendLine($"TransferToEntityId: {TransferToEntityId}");

            return sb.ToString();
        }
    }
}
