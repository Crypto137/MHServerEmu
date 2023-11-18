using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData.Prototypes.Markers;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class EncounterResourcePrototype : Prototype
    {
        public uint Header { get; }
        public uint Version { get; }
        public uint ClassId { get; }
        public ulong PopulationMarkerGuid { get; }
        public string ClientMap { get; }
        public MarkerSetPrototype MarkerSet { get; }
        public NaviPatchSourcePrototype NaviPatchSource { get; }

        public EncounterResourcePrototype(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = reader.ReadUInt32();
                Version = reader.ReadUInt32();
                ClassId = reader.ReadUInt32();
                PopulationMarkerGuid = reader.ReadUInt64();
                ClientMap = reader.ReadFixedString32();

                MarkerSet = new(reader); 
                NaviPatchSource = new(reader);
            }
        }

    }
}
