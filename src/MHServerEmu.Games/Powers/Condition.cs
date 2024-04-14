using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core;
using MHServerEmu.Core.Extensions;
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
        HasIndex                    = 1 << 4,
        HasAssetDataRef             = 1 << 5,   // AssetDataRef != g_AssetDataRefInvalid
        HasPauseTime                = 1 << 6,
        HasDuration                 = 1 << 7,
        Disabled                    = 1 << 8,
        AssetDataRefIsNotFromOwner  = 1 << 9,   // owner == null || owner.GetId() != UltimateCreatorId || AssetDataRef != owner.GetOriginalWorldAsset()
        HasUpdateInterval           = 1 << 10,
        HasCancelOnFlags            = 1 << 11,
    }

    public class Condition
    {
        public ConditionSerializationFlags SerializationFlags { get; set; }
        public ulong Id { get; set; }
        public ulong CreatorId { get; set; }
        public ulong UltimateCreatorId { get; set; }
        public PrototypeId ConditionPrototypeId { get; set; }
        public PrototypeId CreatorPowerPrototypeId { get; set; }
        public uint Index { get; set; }
        public AssetId AssetDataRef { get; set; }
        public long StartTime { get; set; }
        public long PauseTime { get; set; }
        public long Duration { get; set; }  // 7200000 == 2 hours
        public int UpdateInterval { get; set; }
        public ReplicatedPropertyCollection Properties { get; set; }
        public UInt32Flags CancelOnFlags { get; set; }

        public Condition(CodedInputStream stream)
        {
            SerializationFlags = (ConditionSerializationFlags)stream.ReadRawVarint32();
            Id = stream.ReadRawVarint64();

            if (SerializationFlags.HasFlag(ConditionSerializationFlags.NoCreatorId) == false)
                CreatorId = stream.ReadRawVarint64();
            
            if (SerializationFlags.HasFlag(ConditionSerializationFlags.NoUltimateCreatorId) == false)
                UltimateCreatorId = stream.ReadRawVarint64();

            if (SerializationFlags.HasFlag(ConditionSerializationFlags.NoConditionPrototypeId) == false)
                ConditionPrototypeId = stream.ReadPrototypeRef<Prototype>();

            if (SerializationFlags.HasFlag(ConditionSerializationFlags.NoCreatorPowerPrototypeId) == false)
                CreatorPowerPrototypeId = stream.ReadPrototypeRef<Prototype>();

            if (SerializationFlags.HasFlag(ConditionSerializationFlags.HasIndex))
                Index = stream.ReadRawVarint32();

            if (SerializationFlags.HasFlag(ConditionSerializationFlags.HasAssetDataRef)
                && SerializationFlags.HasFlag(ConditionSerializationFlags.AssetDataRefIsNotFromOwner))
                AssetDataRef = (AssetId)stream.ReadRawVarint64();

            StartTime = stream.ReadRawInt64();

            if (SerializationFlags.HasFlag(ConditionSerializationFlags.HasPauseTime))
                PauseTime = stream.ReadRawInt64();

            if (SerializationFlags.HasFlag(ConditionSerializationFlags.HasDuration))
                Duration = stream.ReadRawInt64();

            if (SerializationFlags.HasFlag(ConditionSerializationFlags.HasUpdateInterval))
                UpdateInterval = stream.ReadRawInt32();

            Properties = new(stream);

            if (SerializationFlags.HasFlag(ConditionSerializationFlags.HasCancelOnFlags))
                CancelOnFlags = (UInt32Flags)stream.ReadRawVarint32();
        }

        public Condition() 
        {
            StartTime = (long)Clock.GameTime.TotalMilliseconds;
            Properties = new();
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint32((uint)SerializationFlags);
            stream.WriteRawVarint64(Id);

            if (SerializationFlags.HasFlag(ConditionSerializationFlags.NoCreatorId) == false)
                stream.WriteRawVarint64(CreatorId);

            if (SerializationFlags.HasFlag(ConditionSerializationFlags.NoUltimateCreatorId) == false)
                stream.WriteRawVarint64(UltimateCreatorId);

            if (SerializationFlags.HasFlag(ConditionSerializationFlags.NoConditionPrototypeId) == false)
                stream.WritePrototypeRef<Prototype>(ConditionPrototypeId);

            if (SerializationFlags.HasFlag(ConditionSerializationFlags.NoCreatorPowerPrototypeId) == false)
                stream.WritePrototypeRef<Prototype>(CreatorPowerPrototypeId);

            if (SerializationFlags.HasFlag(ConditionSerializationFlags.HasIndex))
                stream.WriteRawVarint64(Index);

            if (SerializationFlags.HasFlag(ConditionSerializationFlags.HasAssetDataRef)
                && SerializationFlags.HasFlag(ConditionSerializationFlags.AssetDataRefIsNotFromOwner))
                stream.WriteRawVarint64((ulong)AssetDataRef);

            stream.WriteRawInt64(StartTime);

            if (SerializationFlags.HasFlag(ConditionSerializationFlags.HasPauseTime))
                stream.WriteRawInt64(PauseTime);

            if (SerializationFlags.HasFlag(ConditionSerializationFlags.HasDuration))
                stream.WriteRawInt64(Duration);

            if (SerializationFlags.HasFlag(ConditionSerializationFlags.HasUpdateInterval))
                stream.WriteRawInt32(UpdateInterval);

            Properties.Encode(stream);

            if (SerializationFlags.HasFlag(ConditionSerializationFlags.HasCancelOnFlags))
                stream.WriteRawVarint32((uint)CancelOnFlags);
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            sb.AppendLine($"SerializationFlags: {SerializationFlags}");
            sb.AppendLine($"Id: {Id}");
            sb.AppendLine($"CreatorId: {CreatorId}");
            sb.AppendLine($"UltimateCreatorId: {UltimateCreatorId}");
            sb.AppendLine($"ConditionPrototypeId: {GameDatabase.GetPrototypeName(ConditionPrototypeId)}");
            sb.AppendLine($"CreatorPowerPrototypeId: {GameDatabase.GetPrototypeName(CreatorPowerPrototypeId)}");
            sb.AppendLine($"Index: 0x{Index:X}");
            sb.AppendLine($"AssetDataRef: {GameDatabase.GetAssetName(AssetDataRef)}");
            sb.AppendLine($"StartTime: {StartTime}");
            sb.AppendLine($"PauseTime: {PauseTime}");
            sb.AppendLine($"Duration: {Duration}");
            sb.AppendLine($"Properties: {Properties}");
            sb.AppendLine($"CancelOnFlags: {CancelOnFlags}");

            return sb.ToString();
        }

        public bool IsANegativeStatusEffect()
        {
            // TODO for Crypto
            throw new NotImplementedException();
        }
    }
}
