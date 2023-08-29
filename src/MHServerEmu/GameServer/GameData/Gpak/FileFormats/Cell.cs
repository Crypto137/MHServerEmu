using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.Common;

namespace MHServerEmu.GameServer.GameData.Gpak.FileFormats
{
    public class Cell
    {
        public uint Header { get; }
        public uint Version { get; }
        public uint ClassId { get; }
        public Aabb Boundbox { get; }
        public uint Type { get; }
        public uint Walls { get; }
        public uint FillerEdges { get; }
        public uint RoadConnections { get; }
        public string ClientMap { get; }
        public MarkerPrototype[] InitializeSet { get; }
        public MarkerPrototype[] MarkerSet { get; }
        public CellNaviPatchSource NaviPatchSource { get; }
        public byte IsOffsetInMapFile { get; }
        public CellHeightMap HeightMap { get; }
        public ulong[] HotspotPrototypes { get; }

        public Cell(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = reader.ReadUInt32();
                Version = reader.ReadUInt32();
                ClassId = reader.ReadUInt32();
                Vector3 max = reader.ReadVector3();
                Vector3 min = reader.ReadVector3();
                Boundbox = new(max, min);
                Type = reader.ReadUInt32();
                Walls = reader.ReadUInt32();
                FillerEdges = reader.ReadUInt32();
                RoadConnections = reader.ReadUInt32();
                ClientMap = reader.ReadFixedString32();

                InitializeSet = new MarkerPrototype[reader.ReadInt32()];
                for (int i = 0; i < InitializeSet.Length; i++)
                    InitializeSet[i] = new(reader);

                MarkerSet = new MarkerPrototype[reader.ReadInt32()];
                for (int i = 0; i < MarkerSet.Length; i++)
                    MarkerSet[i] = new(reader);

                NaviPatchSource = new(reader);
                IsOffsetInMapFile = reader.ReadByte();
                HeightMap = new(reader);

                HotspotPrototypes = new ulong[reader.ReadUInt32()];
                for (int i = 0; i < HotspotPrototypes.Length; i++)
                    HotspotPrototypes[i] = reader.ReadUInt64();
            }
        }
    }

    public class MarkerPrototype
    {
        public uint ProtoNamehash { get; }
        public Vector3 Position { get; }
        public Vector3 Rotation { get; }

        public object Marker;
        public MarkerPrototype(BinaryReader reader)
        {
            ProtoNamehash = reader.ReadUInt32();

            if (ProtoNamehash == 3862899546)
                Marker = new EntityMarkerPrototype(reader);
            else if (ProtoNamehash == 2901607432)
                Marker = new CellConnectorMarkerPrototype(reader);
            else if (ProtoNamehash == 468664301)
                Marker = new DotCornerMarkerPrototype(reader);
            else if (ProtoNamehash == 576407411)
                Marker = new RoadConnectionMarkerPrototype(reader);
           // else
           //     ResourceStorage.Logger.Fatal($"Failed read {ProtoNamehash}"); 

            Position = reader.ReadVector3();
            Rotation = reader.ReadVector3();
        }
    }
    public class RoadConnectionMarkerPrototype
    {
        public Vector3 Extents { get; }

        public RoadConnectionMarkerPrototype(BinaryReader reader)
        {
            Extents = reader.ReadVector3();
        }
    }
    public class CellConnectorMarkerPrototype
    {
        public Vector3 Extents { get; }

        public CellConnectorMarkerPrototype(BinaryReader reader)
        {
            Extents = reader.ReadVector3();
        }
    }
    public class DotCornerMarkerPrototype
    {
        public Vector3 Extents { get; }

        public DotCornerMarkerPrototype(BinaryReader reader)
        {
            Extents = reader.ReadVector3();
        }
    }
    public class EntityMarkerPrototype
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
        }
    }

    public class CellNaviPatchSource
    {
        // PatchFragments
        public uint NaviPatchCrc { get; }
        public NaviPatchPrototype NaviPatch { get; }
        public NaviPatchPrototype PropPatch { get; }
        public float PlayableArea { get; }
        public float SpawnableArea { get; }

        public CellNaviPatchSource(BinaryReader reader)
        {
            NaviPatchCrc = reader.ReadUInt32();
            NaviPatch = new(reader);
            PropPatch = new(reader);
            PlayableArea = reader.ReadSingle();
            SpawnableArea = reader.ReadSingle();
        }
    }

    public class NaviPatchPrototype
    {
        public Vector3[] Points { get; }
        public NaviPatchEdgePrototype[] Edges { get; }

        public NaviPatchPrototype(BinaryReader reader)
        {
            Points = new Vector3[reader.ReadUInt32()];
            for (int i = 0; i < Points.Length; i++)
                Points[i] = reader.ReadVector3();

            Edges = new NaviPatchEdgePrototype[reader.ReadUInt32()];
            for (int i = 0; i < Edges.Length; i++)
                Edges[i] = new(reader);
        }
    }

    public class NaviPatchEdgePrototype
    {
        public uint ProtoNameHash { get; }
        public uint Index0 { get; }
        public uint Index1 { get; }
        public byte[] Flags0 { get; }
        public byte[] Flags1 { get; }

        public NaviPatchEdgePrototype(BinaryReader reader)
        {
            ProtoNameHash = reader.ReadUInt32();
            Index0 = reader.ReadUInt32();
            Index1 = reader.ReadUInt32();

            Flags0 = new byte[reader.ReadUInt32()];
            for (int i = 0; i < Flags0.Length; i++)
                Flags0[i] = reader.ReadByte();

            Flags1 = new byte[reader.ReadUInt32()];
            for (int i = 0; i < Flags1.Length; i++)
                Flags1[i] = reader.ReadByte();
        }
    }

    public class CellHeightMap
    {
        public Vector2 HeightMapSize { get; }
        public ushort[] HeightMapData { get; }
        public byte[] HotspotData { get; }

        public CellHeightMap(BinaryReader reader)
        {
            HeightMapSize = new(reader.ReadUInt32(), reader.ReadUInt32());

            HeightMapData = new ushort[reader.ReadUInt32()];
            for (int i = 0; i < HeightMapData.Length; i++)
                HeightMapData[i] = reader.ReadUInt16();

            HotspotData = new byte[reader.ReadUInt32()];
            for (int i = 0; i < HotspotData.Length; i++)
                HotspotData[i] = reader.ReadByte();
        }
    }
}
