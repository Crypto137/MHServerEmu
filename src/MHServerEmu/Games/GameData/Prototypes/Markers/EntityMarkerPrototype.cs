using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData.Resources;

namespace MHServerEmu.Games.GameData.Prototypes.Markers
{
    public class EntityMarkerPrototype : MarkerPrototype
    {
        public PrototypeGuid EntityGuid { get; }
        public string LastKnownEntityName { get; }
        public PrototypeGuid Modifier1Guid { get; }
        public string Modifier1Text { get; }
        public PrototypeGuid Modifier2Guid { get; }
        public string Modifier2Text { get; }
        public PrototypeGuid Modifier3Guid { get; }
        public string Modifier3Text { get; }
        public uint EncounterSpawnPhase { get; }
        public byte OverrideSnapToFloor { get; }
        public byte OverrideSnapToFloorValue { get; }
        public PrototypeGuid FilterGuid { get; }
        public string LastKnownFilterName { get; }

        public EntityMarkerPrototype(BinaryReader reader)
        {
            ProtoNameHash = ResourcePrototypeHash.EntityMarkerPrototype;

            EntityGuid = (PrototypeGuid)reader.ReadUInt64();
            LastKnownEntityName = reader.ReadFixedString32();
            Modifier1Guid = (PrototypeGuid)reader.ReadUInt64();
            if (Modifier1Guid != PrototypeGuid.Invalid) Modifier1Text = reader.ReadFixedString32();
            Modifier2Guid = (PrototypeGuid)reader.ReadUInt64();
            if (Modifier2Guid != PrototypeGuid.Invalid) Modifier2Text = reader.ReadFixedString32();
            Modifier3Guid = (PrototypeGuid)reader.ReadUInt64();
            if (Modifier3Guid != PrototypeGuid.Invalid) Modifier3Text = reader.ReadFixedString32();
            EncounterSpawnPhase = reader.ReadUInt32();
            OverrideSnapToFloor = reader.ReadByte();
            OverrideSnapToFloorValue = reader.ReadByte();
            FilterGuid = (PrototypeGuid)reader.ReadUInt64();
            LastKnownFilterName = reader.ReadFixedString32();

            Position = reader.ReadVector3();
            Rotation = reader.ReadVector3();
        }
    }
}
