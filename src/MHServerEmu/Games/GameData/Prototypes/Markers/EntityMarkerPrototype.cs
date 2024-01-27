using MHServerEmu.Common.Extensions;

namespace MHServerEmu.Games.GameData.Prototypes.Markers
{
    public class EntityMarkerPrototype : MarkerPrototype
    {
        public PrototypeGuid EntityGuid { get; }
        public string LastKnownEntityName { get; }
        public PrototypeGuid Modifier1Guid { get; }
        //    public string Modifier1Text { get; } // has eFlagDontCook set
        public PrototypeGuid Modifier2Guid { get; }
        //    public string Modifier2Text { get; } // has eFlagDontCook set
        public PrototypeGuid Modifier3Guid { get; }
        //    public string Modifier3Text { get; } // has eFlagDontCook set
        public uint EncounterSpawnPhase { get; }
        public byte OverrideSnapToFloor { get; }
        public byte OverrideSnapToFloorValue { get; }
        public PrototypeGuid FilterGuid { get; }
        public string LastKnownFilterName { get; }

        public EntityMarkerPrototype(BinaryReader reader)
        {
            EntityGuid = (PrototypeGuid)reader.ReadUInt64();
            LastKnownEntityName = reader.ReadFixedString32();
            Modifier1Guid = (PrototypeGuid)reader.ReadUInt64();
            // eFlagDontCook Modifier1Text = reader.ReadFixedString32();
            Modifier2Guid = (PrototypeGuid)reader.ReadUInt64();
            // eFlagDontCook Modifier2Text = reader.ReadFixedString32();
            Modifier3Guid = (PrototypeGuid)reader.ReadUInt64();
            // eFlagDontCook Modifier3Text = reader.ReadFixedString32();
            EncounterSpawnPhase = reader.ReadUInt32();
            OverrideSnapToFloor = reader.ReadByte();
            OverrideSnapToFloorValue = reader.ReadByte();
            FilterGuid = (PrototypeGuid)reader.ReadUInt64();
            LastKnownFilterName = reader.ReadFixedString32();

            Position = reader.ReadVector3();
            Rotation = reader.ReadVector3();
        }

        public T GetMarkedPrototype<T>() where T : Prototype
        {
            PrototypeId dataRef = GameDatabase.GetDataRefByPrototypeGuid(EntityGuid);
            if (dataRef == 0)
            {
                Logger.Warn($"Unable to get a data ref from MarkerEntityPrototype. Prototype: {ToString()}.");
                return default;
            }
            return GameDatabase.GetPrototype<T>(dataRef);
        }
    }
}
