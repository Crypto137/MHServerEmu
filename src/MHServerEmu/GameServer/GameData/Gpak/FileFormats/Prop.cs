using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.Common;
using MHServerEmu.GameServer.GameData.Prototypes;
using MHServerEmu.GameServer.GameData.Prototypes.Markers;

namespace MHServerEmu.GameServer.GameData.Gpak.FileFormats
{
    public class Prop
    {
        public uint Header { get; }
        public uint Version { get; }
        public uint ClassId { get; }
        public ProceduralPropGroupPrototype[] PropGroups { get; }

        public Prop(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = reader.ReadUInt32();
                Version = reader.ReadUInt32();
                ClassId = reader.ReadUInt32();

                PropGroups = new ProceduralPropGroupPrototype[reader.ReadUInt32()];
                for (int i = 0; i < PropGroups.Length; i++)
                    PropGroups[i] = new(reader);
            }
        }
    }

    public class ProceduralPropGroupPrototype
    {
        public ResourcePrototypeHash ProtoNameHash { get; }
        public string NameId { get; }
        public string PrefabPath { get; }
        public Vector3 MarkerPosition { get; }
        public Vector3 MarkerRotation { get; }
        public MarkerPrototype[] Objects { get; }   // MarkerSetPrototype
        public NaviPatchSourcePrototype NaviPatchSource { get; }
        public ushort RandomRotationDegrees { get; }
        public ushort RandomPosition { get; }

        public ProceduralPropGroupPrototype(BinaryReader reader)
        {
            ProtoNameHash = (ResourcePrototypeHash)reader.ReadUInt32();
            NameId = reader.ReadFixedString32();
            PrefabPath = reader.ReadFixedString32();
            MarkerPosition = reader.ReadVector3();
            MarkerRotation = reader.ReadVector3();

            Objects = new MarkerPrototype[reader.ReadUInt32()];
            for (int i = 0; i < Objects.Length; i++)
                Objects[i] = ReadMarkerPrototype(reader);

            NaviPatchSource = new(reader);
            RandomRotationDegrees = reader.ReadUInt16();
            RandomPosition = reader.ReadUInt16();
        }

        private MarkerPrototype ReadMarkerPrototype(BinaryReader reader)
        {
            MarkerPrototype markerPrototype;
            ResourcePrototypeHash hash = (ResourcePrototypeHash)reader.ReadUInt32();

            switch (hash)
            {
                case ResourcePrototypeHash.CellConnectorMarkerPrototype:
                    markerPrototype = new CellConnectorMarkerPrototype(reader);
                    break;
                case ResourcePrototypeHash.DotCornerMarkerPrototype:
                    markerPrototype = new DotCornerMarkerPrototype(reader);
                    break;
                case ResourcePrototypeHash.EntityMarkerPrototype:
                    markerPrototype = new EntityMarkerPrototype(reader);
                    break;
                case ResourcePrototypeHash.RoadConnectionMarkerPrototype:
                    markerPrototype = new RoadConnectionMarkerPrototype(reader);
                    break;
                case ResourcePrototypeHash.ResourceMarkerPrototype:
                    markerPrototype = new ResourceMarkerPrototype(reader);
                    break;
                case ResourcePrototypeHash.UnrealPropMarkerPrototype:
                    markerPrototype = new UnrealPropMarkerPrototype(reader);
                    break;
                default:
                    throw new($"Unknown ResourcePrototypeHash {(uint)hash}");   // Throw an exception if there's a hash for a type we didn't expect
            }

            return markerPrototype;
        }
    }
}
