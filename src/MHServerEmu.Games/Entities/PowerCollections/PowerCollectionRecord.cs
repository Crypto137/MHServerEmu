using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Entities.PowerCollections
{
    public class PowerCollectionRecord
    {
        [Flags]
        private enum SerializationFlags
        {
            // These serialization flags were introduced in version 1.25, and they are used to reduce the size of power
            // collections in world entity creation net messages by omitting common values (0 or 1) and values that repeat
            // in multiple records in a row (such as character / combat level).
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

        private PrototypeId _powerRef;
        private PowerIndexProperties _indexProps = new();
        private uint _powerRefCount;

        public PrototypeId PowerRef { get => _powerRef; set => _powerRef = value; }
        public PowerIndexProperties IndexProps { get => _indexProps; }
        public uint PowerRefCount { get => _powerRefCount; set => _powerRefCount = value; }

        public PowerCollectionRecord() { }

        public bool ShouldSerializeRecordForPacking(Archive archive = null)
        {
            return true;
        }

        public bool SerializeTo(Archive archive, PowerCollectionRecord previousRecord)
        {
            throw new NotImplementedException();
        }

        public bool SerializeFrom(Archive archive, PowerCollectionRecord previousRecord)
        {
            throw new NotImplementedException();
        }

        public void Decode(CodedInputStream stream, PowerCollectionRecord previousRecord)
        {
            _powerRef = stream.ReadPrototypeRef<PowerPrototype>();

            var flags = (SerializationFlags)stream.ReadRawVarint32();

            _indexProps.PowerRank = flags.HasFlag(SerializationFlags.PowerRankIsZero) ? 0 : (int)stream.ReadRawVarint32();

            // CharacterLevel
            if (flags.HasFlag(SerializationFlags.CharacterLevelIsOne))
                _indexProps.CharacterLevel = 1;
            else if (flags.HasFlag(SerializationFlags.CharacterLevelIsFromPreviousRecord))
                _indexProps.CharacterLevel = previousRecord._indexProps.CharacterLevel;
            else
                _indexProps.CharacterLevel = (int)stream.ReadRawVarint32();

            // CombatLevel
            if (flags.HasFlag(SerializationFlags.CombatLevelIsSameAsCharacterLevel))
                _indexProps.CombatLevel = _indexProps.CharacterLevel;
            else if (flags.HasFlag(SerializationFlags.CombatLevelIsOne))
                _indexProps.CombatLevel = 1;
            else if (flags.HasFlag(SerializationFlags.CombatLevelIsFromPreviousRecord))
                _indexProps.CombatLevel = previousRecord._indexProps.CombatLevel;
            else
                _indexProps.CombatLevel = (int)stream.ReadRawVarint32();

            _indexProps.ItemLevel = flags.HasFlag(SerializationFlags.ItemLevelIsOne) ? 1 : (int)stream.ReadRawVarint32();
            _indexProps.ItemVariation = flags.HasFlag(SerializationFlags.ItemVariationIsOne) ? 1.0f : stream.ReadRawFloat();
            
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
            stream.WritePrototypeRef<PowerPrototype>(PowerRef);
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
            sb.AppendLine($"{nameof(_powerRef)}: {GameDatabase.GetPrototypeName(_powerRef)}");
            sb.AppendLine($"{nameof(_indexProps)}: {_indexProps}");
            sb.AppendLine($"{nameof(_powerRefCount)}: {_powerRefCount}");
            return sb.ToString();
        }
    }
}
