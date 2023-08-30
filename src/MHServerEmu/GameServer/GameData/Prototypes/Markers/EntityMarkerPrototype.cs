using MHServerEmu.Common.Extensions;

namespace MHServerEmu.GameServer.GameData.Prototypes.Markers
{
    public class EntityMarkerPrototype : MarkerPrototype
    {
        public ulong EntityGuid { get; }
        public string LastKnownEntityName { get; }
        public ulong Modifier1Guid { get; }
        public string Modifier1Text { get; }
        public ulong Modifier2Guid { get; }
        public string Modifier2Text { get; }
        public ulong Modifier3Guid { get; }
        public string Modifier3Text { get; }
        public uint EncounterSpawnPhase { get; }
        public byte OverrideSnapToFloor { get; }
        public byte OverrideSnapToFloorValue { get; }
        public ulong FilterGuid { get; }
        public string LastKnownFilterName { get; }

        public EntityMarkerPrototype(BinaryReader reader)
        {
            ProtoNameHash = MarkerPrototypeHash.EntityMarkerPrototype;

            EntityGuid = reader.ReadUInt64();
            LastKnownEntityName = reader.ReadFixedString32();
            Modifier1Guid = reader.ReadUInt64();
            if (Modifier1Guid != 0) Modifier1Text = reader.ReadFixedString32();
            Modifier2Guid = reader.ReadUInt64();
            if (Modifier2Guid != 0) Modifier2Text = reader.ReadFixedString32();
            Modifier3Guid = reader.ReadUInt64();
            if (Modifier3Guid != 0) Modifier3Text = reader.ReadFixedString32();
            EncounterSpawnPhase = reader.ReadUInt32();
            OverrideSnapToFloor = reader.ReadByte();
            OverrideSnapToFloorValue = reader.ReadByte();
            FilterGuid = reader.ReadUInt64();
            LastKnownFilterName = reader.ReadFixedString32();

            Position = reader.ReadVector3();
            Rotation = reader.ReadVector3();
        }
    }
}
