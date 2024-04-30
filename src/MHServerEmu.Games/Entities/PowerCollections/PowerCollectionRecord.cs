using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Powers;

namespace MHServerEmu.Games.Entities.PowerCollections
{
    public class PowerCollectionRecord
    {
        [Flags]
        private enum SerializationFlags
        {
            // These serialization flags were introduced in version 1.25 (to be confirmed), and
            // they are used to reduce the size of power collections in world entity creation
            // net messages by omitting common values (0 or 1) and values that repeat in multiple
            // records in a row (such as character / combat level).
            None = 0,
            PowerRefCountIsOne                  = 1 << 0,
            PowerRankIsZero                     = 1 << 1,
            CharacterLevelIsOne                 = 1 << 2,
            CharacterLevelIsFromPreviousRecord  = 1 << 3,
            CombatLevelIsOne                    = 1 << 4,
            CombatLevelIsFromPreviousRecord     = 1 << 5,
            CombatLevelIsSameAsCharacterLevel   = 1 << 6,
            ItemLevelIsOne                      = 1 << 7,
            ItemVariationIsOne                  = 1 << 8
        }

        private static readonly Logger Logger = LogManager.CreateLogger();

        private PowerPrototype _powerProto = null;
        private PowerIndexProperties _indexProps = new();
        private uint _powerRefCount;

        public PrototypeId PowerPrototypeRef { get => _powerProto != null ? _powerProto.DataRef : PrototypeId.Invalid; }
        public PowerPrototype PowerPrototype { get => _powerProto; }
        public PowerIndexProperties IndexProps { get => _indexProps; }
        public uint PowerRefCount { get => _powerRefCount; set => _powerRefCount = value; }

        // The rest of data is not serialized
        public Power Power { get; private set; }
        public bool IsPowerProgressionPower { get; private set; }
        public bool IsTeamUpPassiveWhileAway { get; private set; }

        public PowerCollectionRecord() { }

        public void Initialize(Power power, PrototypeId powerPrototypeRef, PowerIndexProperties indexProps, uint powerRefCount,
            bool isPowerProgressionPower, bool isTeamUpPassiveWhileAway)
        {
            _powerProto = powerPrototypeRef.As<PowerPrototype>();
            _indexProps = indexProps;
            _powerRefCount = powerRefCount;

            Power = power;
            IsPowerProgressionPower = isPowerProgressionPower;
            IsTeamUpPassiveWhileAway = isTeamUpPassiveWhileAway;
        }

        public bool ShouldSerializeRecordForPacking(Archive archive)
        {
            if (archive.IsPacking == false)
                return Logger.WarnReturn(false, "ShouldSerializeRecordForPacking(): archive.IsPacking == false");

            // TODO: Disable record serialization for non-replication archives
            //if (archive.IsReplication == false) return false;

            if (_powerProto == null)
                return Logger.WarnReturn(false, "ShouldSerializeRecordForPacking(): _powerProto == null");

            if (_powerProto.PowerCategory == PowerCategoryType.ComboEffect)
                return false;

            return true;
        }

        public bool SerializeTo(Archive archive, PowerCollectionRecord previousRecord)
        {
            // TODO: Also check for replication mode
            if (archive.IsPacking == false) return Logger.WarnReturn(false, "SerializeTo(): archive.IsPacking == false");

            bool success = true;

            // Build serialization flags
            SerializationFlags flags = SerializationFlags.None;

            if (_powerRefCount == 1)
                flags |= SerializationFlags.PowerRefCountIsOne;

            if (_indexProps.PowerRank == 0)
                flags |= SerializationFlags.PowerRankIsZero;

            if (_indexProps.CharacterLevel == 1)
                flags |= SerializationFlags.CharacterLevelIsOne;
            else if (previousRecord != null && _indexProps.CharacterLevel == previousRecord._indexProps.CharacterLevel)
                flags |= SerializationFlags.CharacterLevelIsFromPreviousRecord;

            if (_indexProps.CombatLevel == _indexProps.CharacterLevel)
                flags |= SerializationFlags.CombatLevelIsSameAsCharacterLevel;
            else if (_indexProps.CombatLevel == 1)
                flags |= SerializationFlags.CombatLevelIsOne;
            else if (previousRecord != null && _indexProps.CombatLevel == previousRecord._indexProps.CombatLevel)
                flags |= SerializationFlags.CombatLevelIsFromPreviousRecord;

            if (_indexProps.ItemLevel == 1)
                flags |= SerializationFlags.ItemLevelIsOne;

            if (_indexProps.ItemVariation == 1.0f)
                flags |= SerializationFlags.ItemVariationIsOne;

            // Write data
            PrototypeId powerProtoRef = _powerProto.DataRef;
            success &= Serializer.TransferPrototypeEnum<PowerPrototype>(archive, ref powerProtoRef);

            uint rawFlags = (uint)flags;
            success &= Serializer.Transfer(archive, ref rawFlags);

            if (flags.HasFlag(SerializationFlags.PowerRankIsZero) == false)
            {
                uint powerRank = (uint)_indexProps.PowerRank;
                success &= Serializer.Transfer(archive, ref powerRank);
            }

            if (flags.HasFlag(SerializationFlags.CharacterLevelIsOne) == false && flags.HasFlag(SerializationFlags.CharacterLevelIsFromPreviousRecord) == false)
            {
                uint characterLevel = (uint)_indexProps.CharacterLevel;
                success &= Serializer.Transfer(archive, ref characterLevel);
            }

            if (flags.HasFlag(SerializationFlags.CombatLevelIsOne) == false && flags.HasFlag(SerializationFlags.CombatLevelIsFromPreviousRecord) == false
                && flags.HasFlag(SerializationFlags.CombatLevelIsSameAsCharacterLevel) == false)
            {
                uint combatLevel = (uint)_indexProps.CombatLevel;
                success &= Serializer.Transfer(archive, ref combatLevel);
            }

            if (flags.HasFlag(SerializationFlags.ItemLevelIsOne) == false)
            {
                uint itemLevel = (uint)_indexProps.ItemLevel;
                success &= Serializer.Transfer(archive, ref itemLevel);
            }

            if (flags.HasFlag(SerializationFlags.ItemVariationIsOne) == false)
            {
                float itemVariation = _indexProps.ItemVariation;
                success &= Serializer.Transfer(archive, ref itemVariation);
            }

            if (flags.HasFlag(SerializationFlags.PowerRefCountIsOne) == false)
                Serializer.Transfer(archive, ref _powerRefCount);

            return success;
        }

        public bool SerializeFrom(Archive archive, PowerCollectionRecord previousRecord)
        {
            if (archive.IsUnpacking == false) return Logger.WarnReturn(false, "SerializeFrom(): archive.IsUnpacking == false");

            bool success = true;

            PrototypeId powerProtoRef = PrototypeId.Invalid;
            success &= Serializer.TransferPrototypeEnum<PowerPrototype>(archive, ref powerProtoRef);
            _powerProto = powerProtoRef.As<PowerPrototype>();

            uint rawFlags = 0;
            success &= Serializer.Transfer(archive, ref rawFlags);
            var flags = (SerializationFlags)rawFlags;

            uint powerRank = 0;
            if (flags.HasFlag(SerializationFlags.PowerRankIsZero) == false)
                success &= Serializer.Transfer(archive, ref powerRank);

            uint characterLevel = 1;
            if (flags.HasFlag(SerializationFlags.CharacterLevelIsFromPreviousRecord))
            {
                if (previousRecord == null) return Logger.ErrorReturn(false, "SerializeFrom(): CharacterLevelIsFromPreviousRecord flag is set, but previousRecord is null");
                characterLevel = (uint)previousRecord._indexProps.CharacterLevel;
            }
            else if (flags.HasFlag(SerializationFlags.CharacterLevelIsOne) == false)
                success &= Serializer.Transfer(archive, ref characterLevel);

            uint combatLevel = 1;
            if (flags.HasFlag(SerializationFlags.CombatLevelIsSameAsCharacterLevel))
                combatLevel = characterLevel;
            else if (flags.HasFlag(SerializationFlags.CombatLevelIsFromPreviousRecord))
            {
                if (previousRecord == null) return Logger.ErrorReturn(false, "SerializeFrom(): CombatLevelIsFromPreviousRecord flag is set, but previousRecord is null");
                combatLevel = (uint)previousRecord._indexProps.CombatLevel;
            }
            else
                success &= Serializer.Transfer(archive, ref combatLevel);

            uint itemLevel = 1;
            if (flags.HasFlag(SerializationFlags.ItemLevelIsOne) == false)
                success &= Serializer.Transfer(archive, ref itemLevel);

            float itemVariation = 1.0f;
            if (flags.HasFlag(SerializationFlags.ItemVariationIsOne) == false)
                success &= Serializer.Transfer(archive, ref itemVariation);

            _indexProps = new((int)powerRank, (int)characterLevel, (int)combatLevel, (int)itemLevel, itemVariation);

            _powerRefCount = 1;
            if (flags.HasFlag(SerializationFlags.PowerRefCountIsOne) == false)
                success &= Serializer.Transfer(archive, ref _powerRefCount);

            return success;
        }

        public void Decode(CodedInputStream stream, PowerCollectionRecord previousRecord)
        {
            _powerProto = stream.ReadPrototypeRef<PowerPrototype>().As<PowerPrototype>();

            var flags = (SerializationFlags)stream.ReadRawVarint32();

            int powerRank = flags.HasFlag(SerializationFlags.PowerRankIsZero) ? 0 : (int)stream.ReadRawVarint32();

            // CharacterLevel
            int characterLevel;
            if (flags.HasFlag(SerializationFlags.CharacterLevelIsOne))
                characterLevel = 1;
            else if (flags.HasFlag(SerializationFlags.CharacterLevelIsFromPreviousRecord))
                characterLevel = previousRecord._indexProps.CharacterLevel;
            else
                characterLevel = (int)stream.ReadRawVarint32();

            // CombatLevel
            int combatLevel;
            if (flags.HasFlag(SerializationFlags.CombatLevelIsSameAsCharacterLevel))
                combatLevel = _indexProps.CharacterLevel;
            else if (flags.HasFlag(SerializationFlags.CombatLevelIsOne))
                combatLevel = 1;
            else if (flags.HasFlag(SerializationFlags.CombatLevelIsFromPreviousRecord))
                combatLevel = previousRecord._indexProps.CombatLevel;
            else
                combatLevel = (int)stream.ReadRawVarint32();

            int itemLevel = flags.HasFlag(SerializationFlags.ItemLevelIsOne) ? 1 : (int)stream.ReadRawVarint32();
            float itemVariation = flags.HasFlag(SerializationFlags.ItemVariationIsOne) ? 1.0f : stream.ReadRawFloat();

            _indexProps = new(powerRank, characterLevel, combatLevel, itemLevel, itemVariation);

            _powerRefCount = flags.HasFlag(SerializationFlags.PowerRefCountIsOne) ? 1 : stream.ReadRawVarint32();
        }

        public void Encode(CodedOutputStream stream, PowerCollectionRecord previousRecord)
        {
            // Build serialization flags
            SerializationFlags flags = SerializationFlags.None;

            if (_powerRefCount == 1)
                flags |= SerializationFlags.PowerRefCountIsOne;

            if (_indexProps.PowerRank == 0)
                flags |= SerializationFlags.PowerRankIsZero;

            if (_indexProps.CharacterLevel == 1)
                flags |= SerializationFlags.CharacterLevelIsOne;
            else if (previousRecord != null && _indexProps.CharacterLevel == previousRecord._indexProps.CharacterLevel)
                flags |= SerializationFlags.CharacterLevelIsFromPreviousRecord;

            if (_indexProps.CombatLevel == _indexProps.CharacterLevel)
                flags |= SerializationFlags.CombatLevelIsSameAsCharacterLevel;
            else if (_indexProps.CombatLevel == 1)
                flags |= SerializationFlags.CombatLevelIsOne;
            else if (previousRecord != null && _indexProps.CombatLevel == previousRecord._indexProps.CombatLevel)
                flags |= SerializationFlags.CombatLevelIsFromPreviousRecord;

            if (_indexProps.ItemLevel == 1)
                flags |= SerializationFlags.ItemLevelIsOne;

            if (_indexProps.ItemVariation == 1.0f)
                flags |= SerializationFlags.ItemVariationIsOne;

            // Write data
            stream.WritePrototypeRef<PowerPrototype>(PowerPrototypeRef);
            stream.WriteRawVarint32((uint)flags);

            if (flags.HasFlag(SerializationFlags.PowerRankIsZero) == false)
                stream.WriteRawVarint32((uint)_indexProps.PowerRank);

            if (flags.HasFlag(SerializationFlags.CharacterLevelIsOne) == false && flags.HasFlag(SerializationFlags.CharacterLevelIsFromPreviousRecord) == false)
                stream.WriteRawVarint32((uint)_indexProps.CharacterLevel);

            if (flags.HasFlag(SerializationFlags.CombatLevelIsOne) == false && flags.HasFlag(SerializationFlags.CombatLevelIsFromPreviousRecord) == false
                && flags.HasFlag(SerializationFlags.CombatLevelIsSameAsCharacterLevel) == false)
                stream.WriteRawVarint32((uint)_indexProps.CombatLevel);

            if (flags.HasFlag(SerializationFlags.ItemLevelIsOne) == false)
                stream.WriteRawVarint32((uint)_indexProps.ItemLevel);

            if (flags.HasFlag(SerializationFlags.ItemVariationIsOne) == false)
                stream.WriteRawFloat(_indexProps.ItemVariation);

            if (flags.HasFlag(SerializationFlags.PowerRefCountIsOne) == false)
                stream.WriteRawVarint32(_powerRefCount);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(_powerProto)}: {_powerProto}");
            sb.AppendLine($"{nameof(_indexProps)}: {_indexProps}");
            sb.AppendLine($"{nameof(_powerRefCount)}: {_powerRefCount}");
            return sb.ToString();
        }
    }
}
