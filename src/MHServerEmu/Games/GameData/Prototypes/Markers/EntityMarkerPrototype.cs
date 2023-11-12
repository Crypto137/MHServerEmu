using System.Text.Json.Serialization;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData.Resources;

namespace MHServerEmu.Games.GameData.Prototypes.Markers
{
    public class EntityMarkerPrototype : MarkerPrototype
    {
        [JsonPropertyOrder(2)]
        public PrototypeGuid EntityGuid { get; }
        [JsonPropertyOrder(3)]
        public string LastKnownEntityName { get; }
        [JsonPropertyOrder(4)]
        public ulong Modifier1Guid { get; }
        [JsonPropertyOrder(5)]
        public string Modifier1Text { get; }
        [JsonPropertyOrder(6)]
        public ulong Modifier2Guid { get; }
        [JsonPropertyOrder(7)]
        public string Modifier2Text { get; }
        [JsonPropertyOrder(8)]
        public ulong Modifier3Guid { get; }
        [JsonPropertyOrder(9)]
        public string Modifier3Text { get; }
        [JsonPropertyOrder(10)]
        public uint EncounterSpawnPhase { get; }
        [JsonPropertyOrder(11)]
        public byte OverrideSnapToFloor { get; }
        [JsonPropertyOrder(12)]
        public byte OverrideSnapToFloorValue { get; }
        [JsonPropertyOrder(13)]
        public ulong FilterGuid { get; }
        [JsonPropertyOrder(14)]
        public string LastKnownFilterName { get; }

        public EntityMarkerPrototype(BinaryReader reader)
        {
            ProtoNameHash = ResourcePrototypeHash.EntityMarkerPrototype;

            EntityGuid = (PrototypeGuid)reader.ReadUInt64();
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
