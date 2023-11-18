using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class CellPrototype : Prototype
    {
        public CalligraphyHeader Header { get; }
        public uint Version { get; }
        public uint ClassId { get; }
        public Aabb BoundingBox { get; }
        public Cell.Type Type { get; }
        public uint Walls { get; }
        public Cell.Filler FillerEdges { get; }
        public Cell.Type RoadConnections { get; }
        public string ClientMap { get; }
        public MarkerSetPrototype InitializeSet { get; }
        public MarkerSetPrototype MarkerSet { get; }
        public NaviPatchSourcePrototype NaviPatchSource { get; }
        public byte IsOffsetInMapFile { get; }
        public HeightMapPrototype HeightMap { get; }
        public ulong[] HotspotPrototypes { get; }

        public ulong DataRef { get { throw new NotImplementedException(); } }

        public CellPrototype(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            { 
                Header = reader.ReadCalligraphyHeader();
                Version = reader.ReadUInt32();
                ClassId = reader.ReadUInt32();
                Vector3 max = reader.ReadVector3();
                Vector3 min = reader.ReadVector3();
                BoundingBox = new(min, max);
                Type = (Cell.Type)reader.ReadUInt32();
                Walls = reader.ReadUInt32();
                FillerEdges = (Cell.Filler)reader.ReadUInt32();
                RoadConnections = (Cell.Type)reader.ReadUInt32();
                ClientMap = reader.ReadFixedString32();

                InitializeSet = new(reader);
                MarkerSet = new(reader);

                NaviPatchSource = new(reader);
                IsOffsetInMapFile = reader.ReadByte();
                HeightMap = new(reader);

                HotspotPrototypes = new ulong[reader.ReadUInt32()];
                for (int i = 0; i < HotspotPrototypes.Length; i++)
                    HotspotPrototypes[i] = reader.ReadUInt64();
            }
        }
    }

    public class HeightMapPrototype : Prototype
    {
        public Vector2 HeightMapSize { get; }
        public short[] HeightMapData { get; }
        public byte[] HotspotData { get; }

        public HeightMapPrototype(BinaryReader reader)
        {
            HeightMapSize = new(reader.ReadUInt32(), reader.ReadUInt32());

            HeightMapData = new short[reader.ReadUInt32()];
            for (int i = 0; i < HeightMapData.Length; i++)
                HeightMapData[i] = reader.ReadInt16();

            HotspotData = new byte[reader.ReadUInt32()];
            for (int i = 0; i < HotspotData.Length; i++)
                HotspotData[i] = reader.ReadByte();
        }
    }
}
