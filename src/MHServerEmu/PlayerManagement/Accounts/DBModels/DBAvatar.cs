using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;

namespace MHServerEmu.PlayerManagement.Accounts.DBModels
{
    /// <summary>
    /// Represents an avatar entity stored in the account database.
    /// </summary>
    public class DBAvatar
    {
        // We are currently using System.Data.SQLite + Dapper for storing our persistent data.
        // Because SQLite internally stores all 64 bit integers as signed, this causes Dapper
        // overflow errors when parsing ulong values larger than 2^63. To avoid this, we create
        // "raw" long properties for each ulong property that actually get saved to and loaded
        // from the SQLite database.

        // This isn't required for AccountId, because the first byte in our id is type, and it
        // never goes over 127 to make the account id value larger than 2^63.

        public ulong AccountId { get; set; }

        public AvatarPrototypeId Prototype { get; set; }
        public long RawPrototype { get => (long)Prototype; private set => Prototype = (AvatarPrototypeId)value; }

        public ulong Costume { get; set; } = 0;
        public long RawCostume { get => (long)Costume; private set => Costume = (ulong)value; }

        public AbilityKeyMapping AbilityKeyMapping { get; set; }
        public byte[] RawAbilityKeyMapping { get => EncodeAbilityKeyMapping(AbilityKeyMapping); set => AbilityKeyMapping = DecodeAbilityKeyMapping(value); }

        public DBAvatar(ulong accountId, AvatarPrototypeId prototype)
        {
            AccountId = accountId;
            Prototype = prototype;
        }

        public DBAvatar() { }

        private byte[] EncodeAbilityKeyMapping(AbilityKeyMapping abilityKeyMapping)
        {
            BoolEncoder boolEncoder = new();
            boolEncoder.EncodeBool(abilityKeyMapping.ShouldPersist);
            boolEncoder.Cook();

            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);
                abilityKeyMapping.Encode(cos, boolEncoder);
                cos.Flush();
                return ms.ToArray();
            }
        }

        private AbilityKeyMapping DecodeAbilityKeyMapping(byte[] data)
        {
            CodedInputStream cis = CodedInputStream.CreateInstance(data);
            return new(cis, new());
        }
    }
}
