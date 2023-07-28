using System;

namespace MHServerEmu.GameServer.Data
{
    public class Prototype
    {
        public ulong Id { get; }
        public ulong Field1 { get; }
        public ulong ParentId { get; }
        public byte Flag { get; }
        public string StringValue { get; }

        public Prototype(ulong id, ulong field1, ulong parentId, byte flag, string stringValue)
        {
            Id = id;
            Field1 = field1;
            ParentId = parentId;
            Flag = flag;
            StringValue = stringValue;
        }
    }
}
